using System.Text.Json;
using Ardalis.Result;
using CabinetBilder.Core.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.SpaceOsBridge.Persistence;

public sealed class SqliteLocalStore : ILocalStore, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteLocalStore> _logger;
    private readonly SchemaMigrator _migrator;
    private readonly ISecurityService _securityService;
    private SqliteConnection? _connection;

    public SqliteLocalStore(
        string dbPath, 
        SchemaMigrator migrator, 
        ISecurityService securityService,
        ILogger<SqliteLocalStore> logger)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
            Cache = SqliteCacheMode.Shared
        }.ToString();
        _migrator = migrator;
        _securityService = securityService;
        _logger = logger;
    }

    public async Task<Result> InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing local store at {Path}", _connectionString);
        
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync(ct);

        // WAL mode + PRAGMAs (DB-04, DB-09)
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA busy_timeout = 5000;
                PRAGMA synchronous = NORMAL;
                PRAGMA foreign_keys = ON;
            ";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        return await _migrator.MigrateAsync(_connection, ct);
    }

    #region Template Cache

    public async Task<Result<IReadOnlyList<ProductTemplateDto>>> GetCachedTemplatesAsync(CancellationToken ct = default)
    {
        var list = new List<ProductTemplateDto>();
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Version, BodyJson FROM TemplateCache";
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ProductTemplateDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3)));
        }
        return list;
    }

    public async Task<Result> UpsertTemplateCacheAsync(IReadOnlyList<ProductTemplateDto> templates, string etag, string tenantId, CancellationToken ct = default)
    {
        using var transaction = _connection!.BeginTransaction();
        try
        {
            // Simple approach: delete all and insert new for the tenant
            // Or more complex ETag logic if needed. Vision v2 §9.3 suggests conditional GET.
            using (var cmdDel = _connection.CreateCommand())
            {
                cmdDel.Transaction = transaction;
                cmdDel.CommandText = "DELETE FROM TemplateCache WHERE TenantId = @tenantId";
                cmdDel.Parameters.AddWithValue("@tenantId", tenantId);
                await cmdDel.ExecuteNonQueryAsync(ct);
            }

            foreach (var t in templates)
            {
                using var cmdIns = _connection.CreateCommand();
                cmdIns.Transaction = transaction;
                cmdIns.CommandText = @"
                    INSERT INTO TemplateCache (Id, Name, Version, BodyJson, ETag, FetchedAt, ExpiresAt, TenantId)
                    VALUES (@id, @name, @version, @body, @etag, @fetched, @expires, @tenantId)";
                cmdIns.Parameters.AddWithValue("@id", t.Id.ToString("N").ToUpper());
                cmdIns.Parameters.AddWithValue("@name", t.Name);
                cmdIns.Parameters.AddWithValue("@version", t.Version);
                cmdIns.Parameters.AddWithValue("@body", t.BodyJson);
                cmdIns.Parameters.AddWithValue("@etag", etag);
                cmdIns.Parameters.AddWithValue("@fetched", DateTimeOffset.UtcNow.ToString("O"));
                cmdIns.Parameters.AddWithValue("@expires", DateTimeOffset.UtcNow.AddHours(24).ToString("O"));
                cmdIns.Parameters.AddWithValue("@tenantId", tenantId);
                await cmdIns.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return Result.Error(ex.Message);
        }
    }

    public async Task<string?> GetTemplateEtagAsync(string tenantId, CancellationToken ct = default)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT ETag FROM TemplateCache WHERE TenantId = @tenantId LIMIT 1";
        cmd.Parameters.AddWithValue("@tenantId", tenantId);
        return await cmd.ExecuteScalarAsync(ct) as string;
    }

    #endregion

    #region Material Cache

    public async Task<Result<IReadOnlyList<MaterialDto>>> GetCachedMaterialsAsync(CancellationToken ct = default)
    {
        var list = new List<MaterialDto>();
        try
        {
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "SELECT MaterialCode, DisplayName, Category, Thickness, BodyJson, PriceEncryptedDpapi FROM MaterialCache";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var price = reader.IsDBNull(5) ? null : await DecryptPriceAsync(reader.GetFieldValue<byte[]>(5), ct);

                list.Add(new MaterialDto(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetDouble(3),
                    reader.GetString(4),
                    price
                ));
            }
            return list;
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    private async Task<decimal?> DecryptPriceAsync(byte[] encrypted, CancellationToken ct)
    {
        var result = await _securityService.UnprotectStringAsync(encrypted, ct);
        if (result.IsSuccess && decimal.TryParse(result.Value, out var price))
        {
            return price;
        }
        return null;
    }

    public async Task<Result> UpsertMaterialCacheAsync(IReadOnlyList<MaterialDto> materials, string etag, string tenantId, CancellationToken ct = default)
    {
        using var transaction = _connection!.BeginTransaction();
        try
        {
            using (var cmdDel = _connection.CreateCommand())
            {
                cmdDel.Transaction = transaction;
                cmdDel.CommandText = "DELETE FROM MaterialCache WHERE TenantId = @tenantId";
                cmdDel.Parameters.AddWithValue("@tenantId", tenantId);
                await cmdDel.ExecuteNonQueryAsync(ct);
            }

            foreach (var m in materials)
            {
                byte[]? encryptedPrice = null;
                if (m.Price.HasValue)
                {
                    var protResult = await _securityService.ProtectStringAsync(m.Price.Value.ToString("F4"), ct);
                    if (protResult.IsSuccess)
                    {
                        encryptedPrice = protResult.Value;
                    }
                }

                using var cmdIns = _connection.CreateCommand();
                cmdIns.Transaction = transaction;
                cmdIns.CommandText = @"
                    INSERT INTO MaterialCache (MaterialCode, DisplayName, Category, Thickness, BodyJson, PriceEncryptedDpapi, ETag, FetchedAt, ExpiresAt, TenantId)
                    VALUES (@code, @name, @cat, @thick, @body, @price, @etag, @fetched, @expires, @tenantId)";
                cmdIns.Parameters.AddWithValue("@code", m.MaterialCode);
                cmdIns.Parameters.AddWithValue("@name", m.DisplayName);
                cmdIns.Parameters.AddWithValue("@cat", m.Category);
                cmdIns.Parameters.AddWithValue("@thick", (object?)m.Thickness ?? DBNull.Value);
                cmdIns.Parameters.AddWithValue("@body", m.BodyJson);
                cmdIns.Parameters.AddWithValue("@price", (object?)encryptedPrice ?? DBNull.Value);
                cmdIns.Parameters.AddWithValue("@etag", etag);
                cmdIns.Parameters.AddWithValue("@fetched", DateTimeOffset.UtcNow.ToString("O"));
                cmdIns.Parameters.AddWithValue("@expires", DateTimeOffset.UtcNow.AddHours(24).ToString("O"));
                cmdIns.Parameters.AddWithValue("@tenantId", tenantId);
                await cmdIns.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return Result.Error(ex.Message);
        }
    }

    #endregion

    #region Outbox

    public async Task<Result<Guid>> EnqueueOutboxAsync(OutboxEntry entry, CancellationToken ct = default)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO OutboxQueue (Id, Operation, PayloadJson, EncryptedPayloadDpapi, Status, CreatedAt)
            VALUES (@id, @op, @payload, @encrypted, @status, @created)";
        
        cmd.Parameters.AddWithValue("@id", entry.Id.ToString("N").ToUpper());
        cmd.Parameters.AddWithValue("@op", entry.Operation.ToString());
        cmd.Parameters.AddWithValue("@payload", (object?)entry.PayloadJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@encrypted", (object?)entry.EncryptedPayloadDpapi ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", entry.Status.ToString());
        cmd.Parameters.AddWithValue("@created", entry.CreatedAt.ToString("O"));

        await cmd.ExecuteNonQueryAsync(ct);
        return entry.Id;
    }

    public async Task<Result<IReadOnlyList<OutboxEntry>>> ClaimPendingOutboxAsync(int maxCount, CancellationToken ct = default)
    {
        var list = new List<OutboxEntry>();
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            UPDATE OutboxQueue 
            SET Status = 'InProgress' 
            WHERE Id IN (
                SELECT Id FROM OutboxQueue 
                WHERE Status = 'Pending' 
                ORDER BY CreatedAt ASC 
                LIMIT @limit
            )
            RETURNING Id, Operation, PayloadJson, EncryptedPayloadDpapi, Status, RetryCount, CreatedAt, LastAttemptAt, LastErrorMessage";
        
        cmd.Parameters.AddWithValue("@limit", maxCount);
        
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new OutboxEntry(
                reader.GetGuid(0),
                Enum.Parse<OutboxOperation>(reader.GetString(1)),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : (byte[])reader.GetValue(3),
                Enum.Parse<OutboxStatus>(reader.GetString(4)),
                reader.GetInt32(5),
                DateTimeOffset.Parse(reader.GetString(6)),
                reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7)),
                reader.IsDBNull(8) ? null : reader.GetString(8)
            ));
        }
        return list;
    }

    public async Task<Result> MarkOutboxSuccessAsync(Guid entryId, CancellationToken ct = default)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE OutboxQueue SET Status = 'Succeeded', CompletedAt = @now WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", entryId.ToString("N").ToUpper());
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
        return Result.Success();
    }

    public async Task<Result> MarkOutboxFailedAsync(Guid entryId, string errorMessage, CancellationToken ct = default)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            UPDATE OutboxQueue 
            SET Status = 'Failed', 
                RetryCount = RetryCount + 1, 
                LastAttemptAt = @now, 
                LastErrorMessage = @msg 
            WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", entryId.ToString("N").ToUpper());
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@msg", errorMessage);
        await cmd.ExecuteNonQueryAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CleanupOutboxAsync(int retentionDays, CancellationToken ct = default)
    {
        try
        {
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "DELETE FROM OutboxQueue WHERE Status = 'Succeeded' AND CompletedAt < @cutoff";
            cmd.Parameters.AddWithValue("@cutoff", DateTimeOffset.UtcNow.AddDays(-retentionDays).ToString("O"));
            
            var deleted = await cmd.ExecuteNonQueryAsync(ct);
            if (deleted > 0)
            {
                _logger.LogInformation("Cleaned up {Count} succeeded outbox entries older than {Days} days.", deleted, retentionDays);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup outbox.");
            return Result.Error(ex.Message);
        }
    }

    #endregion

    #region Guid Tracking

    public async Task<Result<SeenGuidInfo?>> TryFindSeenGuidAsync(Guid smartObjectId, CancellationToken ct = default)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT SmartObjectId, DrawingPath, DrawingHash, FirstSeenAt, LastSeenAt FROM SeenSmartObjectGuids WHERE SmartObjectId = @id";
        cmd.Parameters.AddWithValue("@id", smartObjectId.ToString("N").ToUpper());
        
        using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new SeenGuidInfo(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3)),
                DateTimeOffset.Parse(reader.GetString(4)));
        }
        return (SeenGuidInfo?)null;
    }

    public async Task<Result> RegisterSeenGuidAsync(Guid smartObjectId, string drawingPath, string drawingHash, CancellationToken ct = default)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO SeenSmartObjectGuids (SmartObjectId, DrawingPath, DrawingHash, FirstSeenAt, LastSeenAt)
            VALUES (@id, @path, @hash, @now, @now)
            ON CONFLICT(SmartObjectId) DO UPDATE SET 
                DrawingPath = excluded.DrawingPath, 
                LastSeenAt = excluded.LastSeenAt";
        
        cmd.Parameters.AddWithValue("@id", smartObjectId.ToString("N").ToUpper());
        cmd.Parameters.AddWithValue("@path", drawingPath);
        cmd.Parameters.AddWithValue("@hash", drawingHash);
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("O"));
        
        await cmd.ExecuteNonQueryAsync(ct);
        return Result.Success();
    }

    #endregion

    #region Meta

    public async Task<Result<DateTimeOffset?>> GetLastSyncAtAsync(CancellationToken ct = default)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT Value FROM AppSettings WHERE Key = 'LastSyncAt'";
        var val = await cmd.ExecuteScalarAsync(ct) as string;
        return val != null ? DateTimeOffset.Parse(val) : (DateTimeOffset?)null;
    }

    public async Task<Result> SetLastSyncAtAsync(DateTimeOffset timestamp, CancellationToken ct = default)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT INTO AppSettings (Key, Value) VALUES ('LastSyncAt', @val) ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value";
        cmd.Parameters.AddWithValue("@val", timestamp.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
        return Result.Success();
    }

    // Diagnostics (DB-12)
    public async Task<Result<LocalStoreStats>> GetStoreStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var schemaVersion = 0;
            using (var cmd = _connection!.CreateCommand())
            {
                cmd.CommandText = "PRAGMA user_version";
                schemaVersion = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            }

            var integrity = "ok";
            using (var cmd = _connection!.CreateCommand())
            {
                cmd.CommandText = "PRAGMA integrity_check";
                integrity = await cmd.ExecuteScalarAsync(ct) as string ?? "unknown";
            }

            int templates = await CountTableAsync("TemplateCache", ct);
            int materials = await CountTableAsync("MaterialCache", ct);
            int seenGuids = await CountTableAsync("SeenSmartObjectGuids", ct);
            
            int pending = await CountWhereAsync("OutboxQueue", "Status = 'Pending'", ct);
            int failed = await CountWhereAsync("OutboxQueue", "Status = 'Failed'", ct);
            int succeeded30d = await CountWhereAsync("OutboxQueue", "Status = 'Succeeded' AND CompletedAt > @cutoff", ct, ("@cutoff", DateTimeOffset.UtcNow.AddDays(-30).ToString("O")));

            return new LocalStoreStats(schemaVersion, integrity, templates, materials, seenGuids, pending, succeeded30d, failed);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    private async Task<int> CountTableAsync(string table, CancellationToken ct)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    private async Task<int> CountWhereAsync(string table, string where, CancellationToken ct, params (string, object)[] parameters)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table} WHERE {where}";
        foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Item1, p.Item2);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<Result<IReadOnlyList<OutboxEntry>>> GetOutboxEntriesAsync(int days, CancellationToken ct = default)
    {
        var list = new List<OutboxEntry>();
        try
        {
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Operation, PayloadJson, EncryptedPayloadDpapi, Status, RetryCount, CreatedAt, LastAttemptAt, LastErrorMessage, CompletedAt
                FROM OutboxQueue 
                WHERE CreatedAt > @cutoff 
                ORDER BY CreatedAt DESC";
            cmd.Parameters.AddWithValue("@cutoff", DateTimeOffset.UtcNow.AddDays(-days).ToString("O"));

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new OutboxEntry(
                    reader.GetGuid(0),
                    Enum.Parse<OutboxOperation>(reader.GetString(1)),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : (byte[])reader.GetValue(3),
                    Enum.Parse<OutboxStatus>(reader.GetString(4)),
                    reader.GetInt32(5),
                    DateTimeOffset.Parse(reader.GetString(6)),
                    reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7)),
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9))
                ));
            }
            return list;
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    #endregion

    public SqliteConnection GetConnection() => _connection ?? throw new InvalidOperationException("Store not initialized");

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}
