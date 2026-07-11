using Ardalis.Result;
using CabinetBilder.Core.Sync;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CabinetBilder.SpaceOsBridge.Outbox;

/// <summary>
/// High-level service to enqueue operations into the Outbox.
/// Handles encryption for sensitive data and payload size limits.
/// </summary>
public sealed class OutboxQueue
{
    private readonly ILocalStore _store;
    private readonly ISecurityService _security;
    private readonly ILogger<OutboxQueue> _logger;
    private const int MaxPayloadBytes = 1024 * 1024; // 1MB limit (DB-01)

    public OutboxQueue(
        ILocalStore store,
        ISecurityService security,
        ILogger<OutboxQueue> logger)
    {
        _store = store;
        _security = security;
        _logger = logger;
    }

    /// <summary>
    /// Enqueues a new operation to be synchronized with the server.
    /// </summary>
    public async Task<Result<Guid>> EnqueueAsync(
        OutboxOperation operation, 
        string payloadJson, 
        bool isSensitive = false, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(payloadJson))
            return Result.Error("Payload cannot be empty.");

        // Size check (DB-01)
        if (Encoding.UTF8.GetByteCount(payloadJson) > MaxPayloadBytes)
        {
            return Result.Error($"Payload size exceeds the 1MB limit.");
        }

        string? finalJson = payloadJson;
        byte[]? encryptedPayload = null;

        if (isSensitive)
        {
            var protectResult = await _security.ProtectStringAsync(payloadJson, ct);
            if (!protectResult.IsSuccess)
            {
                return Result.Error("Failed to protect sensitive payload: " + string.Join(", ", protectResult.Errors));
            }
            encryptedPayload = protectResult.Value;
            finalJson = null; // DB-09 XOR rule: sensitive payload stored in encrypted field only
        }

        var entry = new OutboxEntry(
            Guid.NewGuid(),
            operation,
            finalJson,
            encryptedPayload,
            OutboxStatus.Pending,
            0,
            DateTimeOffset.UtcNow);

        _logger.LogInformation("Enqueuing outbox operation {Operation} (Sensitive: {IsSensitive}, ID: {Id})", operation, isSensitive, entry.Id);
        
        return await _store.EnqueueOutboxAsync(entry, ct);
    }
}
