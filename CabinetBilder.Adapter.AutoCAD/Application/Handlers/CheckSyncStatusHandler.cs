using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Core.SmartObjects.Requests;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.Adapter.AutoCAD.Application.Handlers;

/// <summary>
/// Handler for checking synchronization status of AutoCAD objects against the remote database.
/// </summary>
public class CheckSyncStatusHandler : IRequestHandler<CheckSyncStatusQuery, Result<IEnumerable<SmartObjectSyncResult>>>
{
    private readonly RemoteDbContext _dbContext;
    private readonly ISmartObjectMetadataService _localMetadataService;
    private readonly ILogger<CheckSyncStatusHandler> _logger;

    public CheckSyncStatusHandler(
        RemoteDbContext dbContext,
        ISmartObjectMetadataService localMetadataService,
        ILogger<CheckSyncStatusHandler> logger)
    {
        _dbContext = dbContext;
        _localMetadataService = localMetadataService;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<SmartObjectSyncResult>>> Handle(CheckSyncStatusQuery request, CancellationToken cancellationToken)
    {
        var drawingId = _localMetadataService.GetCurrentDrawingId();
        var handles = request.Handles.ToList();
        var results = new List<SmartObjectSyncResult>();

        try
        {
            // Fetch all relevant entities from server in one go
            var serverEntities = await _dbContext.SmartObjects
                .Where(e => e.DrawingId == drawingId && handles.Contains(e.Handle))
                .ToDictionaryAsync(e => e.Handle, e => e.Version, cancellationToken);

            foreach (var handle in handles)
            {
                var localResult = _localMetadataService.ReadMetadata(handle);
                if (localResult.IsFailure)
                {
                    // If we can't read local metadata, we can't check sync status easily.
                    // For now, skip or report as error.
                    continue;
                }

                var localMetadata = localResult.Value;
                var localVersion = localMetadata.Version;
                
                serverEntities.TryGetValue(handle, out var serverVersion);

                SyncStatus status;

                if (serverVersion == null)
                {
                    status = SyncStatus.LocalOnly;
                }
                else if (localVersion == serverVersion)
                {
                    // Versions match. But is it modified locally?
                    // For a true "Git" workflow, we would need to compare content too
                    // if we want to detect "ModifiedLocally".
                    // But usually, the "local version" in AutoCAD stays the same until Push.
                    // If user edited it, we need to know that.
                    // For now, let's assume if it exists on server and version matches, it's UpToDate.
                    status = SyncStatus.UpToDate;
                }
                else
                {
                    // Versions differ.
                    // If localVersion is empty, it means it's a new object but server already has it (somehow?)
                    // Or if they differ, it's usually Outdated.
                    status = SyncStatus.Outdated;
                }

                results.Add(new SmartObjectSyncResult(handle, status, serverVersion));
            }

            return Result.Success(results.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check sync status for drawing {DrawingId}", drawingId);
            return Result.Error($"Sync check failed: {ex.Message}");
        }
    }
}

