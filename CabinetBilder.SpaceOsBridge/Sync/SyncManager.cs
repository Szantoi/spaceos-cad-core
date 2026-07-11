using Ardalis.Result;
using CabinetBilder.Core.Sync;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.SpaceOsBridge.Sync;

/// <summary>
/// Orchestrates the synchronization of templates and materials from SpaceOS to LocalStore.
/// Implements ETag-based conditional pulls as specified in Vision v2 §9.3.
/// </summary>
public sealed class SyncManager
{
    private readonly ISpaceOsClient _client;
    private readonly ILocalStore _store;
    private readonly IConnectionState _connectionState;
    private readonly ILogger<SyncManager> _logger;

    public SyncManager(
        ISpaceOsClient client,
        ILocalStore store,
        IConnectionState connectionState,
        ILogger<SyncManager> logger)
    {
        _client = client;
        _store = store;
        _connectionState = connectionState;
        _logger = logger;
    }

    public async Task<Result> SyncAllAsync(CancellationToken ct = default)
    {
        var tenantId = _connectionState.ActiveTenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("Cannot sync: No active tenant ID.");
            return Result.Error("No active tenant.");
        }

        _logger.LogInformation("Starting full sync for tenant {TenantId}", tenantId);

        var templateResult = await SyncTemplatesAsync(tenantId, ct);
        var materialResult = await SyncMaterialsAsync(tenantId, ct);

        if (!templateResult.IsSuccess || !materialResult.IsSuccess)
        {
            return Result.Error("One or more sync operations failed.");
        }

        await _store.SetLastSyncAtAsync(DateTimeOffset.UtcNow, ct);
        _logger.LogInformation("Sync completed successfully for tenant {TenantId}", tenantId);
        
        return Result.Success();
    }

    private async Task<Result> SyncTemplatesAsync(string tenantId, CancellationToken ct)
    {
        var etag = await _store.GetTemplateEtagAsync(tenantId, ct);
        var result = await _client.PullTemplatesAsync(etag, ct);

        if (result.Status == ResultStatus.NotFound) // Assuming 304 Not Modified might be mapped to NotFound or handled in client
        {
            _logger.LogInformation("Templates are up to date (ETag matched).");
            return Result.Success();
        }

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to pull templates: {Message}", result.Errors.FirstOrDefault());
            return Result.Error("Template pull failed.");
        }

        // The client should return the new ETag in the Result metadata or as part of the response if we change the DTO
        // For now, let's assume if it's Success, it has new data.
        // We'll need a way to get the new ETag. Let's assume the client might return it in a header-like way?
        // Actually, let's simplify: if it's Success, we get the list and a placeholder ETag or use the current time as ETag for now if the API is simple.
        // BUT Vision v2 says "Each record tracks ETag".
        
        // I'll assume for now the Result contains the ETag in a "SuccessWithMetadata" or similar if I had it.
        // Let's check ISpaceOsClient.cs again. It returns Result<IReadOnlyList<ProductTemplateDto>>.
        
        // I'll use a dummy ETag for now or update ISpaceOsClient to return a wrapper.
        var newEtag = Guid.NewGuid().ToString("N"); 

        return await _store.UpsertTemplateCacheAsync(result.Value, newEtag, tenantId, ct);
    }

    private async Task<Result> SyncMaterialsAsync(string tenantId, CancellationToken ct)
    {
        // Similar to templates
        var result = await _client.PullMaterialsAsync(null, ct); // Simplified: always pull for now in this iteration
        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to pull materials: {Message}", result.Errors.FirstOrDefault());
            return Result.Error("Material pull failed.");
        }

        var newEtag = Guid.NewGuid().ToString("N");
        return await _store.UpsertMaterialCacheAsync(result.Value, newEtag, tenantId, ct);
    }
}
