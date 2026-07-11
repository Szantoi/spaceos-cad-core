using Ardalis.Result;

namespace CabinetBilder.Core.Sync;

/// <summary>
/// Defines the local storage port for SQLite persistence, 
/// as specified in Architecture Vision v2 §7.3.
/// </summary>
public interface ILocalStore
{
    // Template cache
    Task<Result<IReadOnlyList<ProductTemplateDto>>> GetCachedTemplatesAsync(CancellationToken ct = default);
    Task<Result> UpsertTemplateCacheAsync(IReadOnlyList<ProductTemplateDto> templates, string etag, string tenantId, CancellationToken ct = default);
    Task<string?> GetTemplateEtagAsync(string tenantId, CancellationToken ct = default);

    // Material cache
    Task<Result<IReadOnlyList<MaterialDto>>> GetCachedMaterialsAsync(CancellationToken ct = default);
    Task<Result> UpsertMaterialCacheAsync(IReadOnlyList<MaterialDto> materials, string etag, string tenantId, CancellationToken ct = default);

    // Outbox
    Task<Result<Guid>> EnqueueOutboxAsync(OutboxEntry entry, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OutboxEntry>>> ClaimPendingOutboxAsync(int maxCount, CancellationToken ct = default);
    Task<Result> MarkOutboxSuccessAsync(Guid entryId, CancellationToken ct = default);
    Task<Result> MarkOutboxFailedAsync(Guid entryId, string errorMessage, CancellationToken ct = default);
    Task<Result> CleanupOutboxAsync(int retentionDays, CancellationToken ct = default);

    // SmartObject Guid tracking (DB-05)
    Task<Result<SeenGuidInfo?>> TryFindSeenGuidAsync(Guid smartObjectId, CancellationToken ct = default);
    Task<Result> RegisterSeenGuidAsync(Guid smartObjectId, string drawingPath, string drawingHash, CancellationToken ct = default);

    // Meta
    Task<Result<DateTimeOffset?>> GetLastSyncAtAsync(CancellationToken ct = default);
    Task<Result> SetLastSyncAtAsync(DateTimeOffset timestamp, CancellationToken ct = default);

    // Diagnostics (DB-12)
    Task<Result<LocalStoreStats>> GetStoreStatsAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<OutboxEntry>>> GetOutboxEntriesAsync(int days, CancellationToken ct = default);
}

public sealed record LocalStoreStats(
    int SchemaVersion,
    string IntegrityCheck,
    int TemplateCacheCount,
    int MaterialCacheCount,
    int SeenGuidsCount,
    int OutboxPending,
    int OutboxSucceededLast30d,
    int OutboxFailed);

public sealed record OutboxEntry(
    Guid Id,
    OutboxOperation Operation,
    string? PayloadJson,
    byte[]? EncryptedPayloadDpapi,
    OutboxStatus Status = OutboxStatus.Pending,
    int RetryCount = 0,
    DateTimeOffset CreatedAt = default,
    DateTimeOffset? LastAttemptAt = null,
    string? LastErrorMessage = null,
    DateTimeOffset? CompletedAt = null);

public enum OutboxOperation
{
    SubmitCuttingSheet,
    AnchorHash,
    UploadBom
}

public enum OutboxStatus
{
    Pending,
    InProgress,
    Succeeded,
    Failed
}

public sealed record SeenGuidInfo(
    Guid SmartObjectId,
    string DrawingPath,
    string DrawingHash,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt);

// Placeholder DTOs to be refined when SpaceOS Contracts are integrated
public sealed record ProductTemplateDto(Guid Id, string Name, int Version, string BodyJson);
public sealed record MaterialDto(string MaterialCode, string DisplayName, string Category, double? Thickness, string BodyJson, decimal? Price = null);
