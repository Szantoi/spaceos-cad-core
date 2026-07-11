using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Core.SmartObjects.Requests;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CabinetBilder.Adapter.AutoCAD.Application.Handlers;

/// <summary>
/// Handler for pulling metadata from the remote database to the local drawing.
/// </summary>
public class PullMetadataHandler : IRequestHandler<PullMetadataCommand, Result<SmartObjectSyncResult>>
{
    private readonly RemoteDbContext _dbContext;
    private readonly ISmartObjectMetadataService _localMetadataService;
    private readonly ILogger<PullMetadataHandler> _logger;

    public PullMetadataHandler(
        RemoteDbContext dbContext,
        ISmartObjectMetadataService localMetadataService,
        ILogger<PullMetadataHandler> logger)
    {
        _dbContext = dbContext;
        _localMetadataService = localMetadataService;
        _logger = logger;
    }

    public async Task<Result<SmartObjectSyncResult>> Handle(PullMetadataCommand request, CancellationToken cancellationToken)
    {
        var drawingId = _localMetadataService.GetCurrentDrawingId();
        var handle = request.Handle;

        try
        {
            var serverEntity = await _dbContext.SmartObjects
                .FirstOrDefaultAsync(e => e.DrawingId == drawingId && e.Handle == handle, cancellationToken);

            if (serverEntity == null)
            {
                _logger.LogWarning("Pull failed: Object {Handle} not found on server for drawing {DrawingId}", handle, drawingId);
                return Result.NotFound($"Object {handle} was not found on the server.");
            }

            var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(serverEntity.MetadataJson);
            if (fields == null)
            {
                return Result.Error("Failed to deserialize server metadata.");
            }

            // Create metadata object with the server's version
            var serverMetadata = SmartObjectMetadata.From(fields, serverEntity.Version);

            // Overwrite local metadata
            var updateResult = _localMetadataService.WriteMetadata(handle, serverMetadata);

            if (updateResult.IsFailure)
            {
                return Result.Error($"Failed to update local drawing: {updateResult.ErrorMessage}");
            }

            _logger.LogInformation("Successfully pulled metadata for {Handle}. Version: {Version}", handle, serverEntity.Version);

            return Result.Success(new SmartObjectSyncResult(handle, SyncStatus.UpToDate, serverEntity.Version));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pull metadata for object {Handle}", handle);
            return Result.Error($"Pull failed: {ex.Message}");
        }
    }
}

