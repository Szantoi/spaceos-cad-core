using CabinetBilder.Core.Sync;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.SpaceOsBridge.Outbox;

/// <summary>
/// Background service that periodically polls the OutboxQueue and submits entries to SpaceOS.
/// It uses OutboxLeader to ensure only one instance is active.
/// </summary>
public sealed class OutboxWorker : BackgroundService
{
    private readonly ILocalStore _store;
    private readonly ISpaceOsClient _client;
    private readonly OutboxLeader _leader;
    private readonly ISecurityService _security;
    private readonly ILogger<OutboxWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

    public OutboxWorker(
        ILocalStore store,
        ISpaceOsClient client,
        OutboxLeader leader,
        ISecurityService security,
        ILogger<OutboxWorker> logger)
    {
        _store = store;
        _client = client;
        _leader = leader;
        _security = security;
        _logger = logger;
    }

    private DateTimeOffset _lastCleanup = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_leader.TryBecomeLeader())
                {
                    // Run cleanup once a day (DB-08)
                    if (DateTimeOffset.UtcNow - _lastCleanup > TimeSpan.FromDays(1))
                    {
                        await _store.CleanupOutboxAsync(30, stoppingToken);
                        _lastCleanup = DateTimeOffset.UtcNow;
                    }

                    await ProcessOutboxAsync(stoppingToken);
                }
                else
                {
                    _logger.LogDebug("Not the leader. Skipping outbox processing.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxWorker loop.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxWorker stopping.");
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        var claimResult = await _store.ClaimPendingOutboxAsync(10, ct);
        if (!claimResult.IsSuccess)
        {
            _logger.LogError("Failed to claim outbox entries: {Error}", claimResult.Errors);
            return;
        }

        var entries = claimResult.Value;
        if (entries.Count == 0) return;

        _logger.LogInformation("Processing {Count} outbox entries.", entries.Count);

        foreach (var entry in entries)
        {
            try
            {
                string? payload = entry.PayloadJson;
                
                // Decrypt if sensitive (DB-09)
                if (entry.EncryptedPayloadDpapi != null)
                {
                    var decryptResult = await _security.UnprotectStringAsync(entry.EncryptedPayloadDpapi, ct);
                    if (!decryptResult.IsSuccess)
                    {
                        await _store.MarkOutboxFailedAsync(entry.Id, "Failed to decrypt sensitive payload.", ct);
                        continue;
                    }
                    payload = decryptResult.Value;
                }

                if (payload == null)
                {
                    await _store.MarkOutboxFailedAsync(entry.Id, "Empty payload.", ct);
                    continue;
                }

                var result = entry.Operation switch
                {
                    OutboxOperation.SubmitCuttingSheet => await _client.SubmitCuttingSheetAsync(payload, ct),
                    OutboxOperation.AnchorHash => await _client.AnchorHashAsync(payload, ct),
                    OutboxOperation.UploadBom => await _client.SubmitBomAsync(payload, ct),
                    _ => throw new NotSupportedException($"Operation {entry.Operation} is not supported.")
                };

                if (result.IsSuccess)
                {
                    await _store.MarkOutboxSuccessAsync(entry.Id, ct);
                    _logger.LogInformation("Successfully processed outbox entry {Id} ({Operation})", entry.Id, entry.Operation);
                }
                else
                {
                    var error = string.Join(", ", result.Errors);
                    await _store.MarkOutboxFailedAsync(entry.Id, error, ct);
                    _logger.LogWarning("Failed to process outbox entry {Id}: {Error}", entry.Id, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while processing outbox entry {Id}", entry.Id);
                await _store.MarkOutboxFailedAsync(entry.Id, ex.Message, ct);
            }
        }
    }
}
