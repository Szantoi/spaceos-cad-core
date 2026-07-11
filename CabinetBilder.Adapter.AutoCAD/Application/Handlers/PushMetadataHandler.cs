using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Core.SmartObjects.Requests;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence.Entities;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CabinetBilder.Adapter.AutoCAD.Application.Handlers;

/// <summary>
/// Handler for pushing local metadata to the remote database with version checking.
/// </summary>
public class PushMetadataHandler : IRequestHandler<PushMetadataCommand, Result<SmartObjectSyncResult>>
{
    private readonly RemoteDbContext _dbContext;
    private readonly ISmartObjectMetadataService _localMetadataService;
    private readonly ILogger<PushMetadataHandler> _logger;

    public PushMetadataHandler(
        RemoteDbContext dbContext,
        ISmartObjectMetadataService localMetadataService,
        ILogger<PushMetadataHandler> logger)
    {
        _dbContext = dbContext;
        _localMetadataService = localMetadataService;
        _logger = logger;
    }

    public async Task<Result<SmartObjectSyncResult>> Handle(PushMetadataCommand request, CancellationToken cancellationToken)
    {
        var drawingId = _localMetadataService.GetCurrentDrawingId();
        var handle = request.Handle;
        var localMetadata = request.Metadata;

        try
        {
            var serverEntity = await _dbContext.SmartObjects
                .FirstOrDefaultAsync(e => e.DrawingId == drawingId && e.Handle == handle, cancellationToken);

            string currentServerVersion = serverEntity?.Version ?? string.Empty;

            // Conflict detection:
            // If the local BaseVersion doesn't match the current server version, someone else pushed in the meantime.
            if (localMetadata.Version != currentServerVersion)
            {
                _logger.LogWarning("Push conflict for {Handle} in drawing {DrawingId}. Local base version: {LocalVersion}, Server version: {ServerVersion}",
                    handle, drawingId, localMetadata.Version, currentServerVersion);
                
                return Result.Success(new SmartObjectSyncResult(handle, SyncStatus.Conflict, currentServerVersion));
            }

            // No conflict. Generate new version and save.
            string newVersion = localMetadata.ComputeHash();
            
            if (serverEntity == null)
            {
                serverEntity = new SmartObjectEntity
                {
                    DrawingId = drawingId,
                    Handle = handle,
                    MetadataJson = JsonSerializer.Serialize(localMetadata.Fields),
                    Version = newVersion,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = Environment.MachineName
                };
                _dbContext.SmartObjects.Add(serverEntity);
            }
            else
            {
                serverEntity.MetadataJson = JsonSerializer.Serialize(localMetadata.Fields);
                serverEntity.Version = newVersion;
                serverEntity.UpdatedAt = DateTime.UtcNow;
                serverEntity.UpdatedBy = Environment.MachineName;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // IMPORTANT: Update the local drawing with the NEW version we just generated.
            // This is like a "post-commit" update of the local workspace.
            var updateResult = _localMetadataService.WriteMetadata(handle, SmartObjectMetadata.From(localMetadata.Fields, newVersion));
            
            if (updateResult.IsFailure)
            {
                return Result.Error($"Metadata pushed to server but local drawing update failed: {updateResult.ErrorMessage}");
            }

            _logger.LogInformation("Successfully pushed metadata for {Handle}. New version: {Version}", handle, newVersion);

            return Result.Success(new SmartObjectSyncResult(handle, SyncStatus.UpToDate, newVersion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push metadata for object {Handle}", handle);
            return Result.Error($"Push failed: {ex.Message}");
        }
    }
}

