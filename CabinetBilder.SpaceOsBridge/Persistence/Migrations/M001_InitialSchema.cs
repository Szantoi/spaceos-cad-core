using Microsoft.Data.Sqlite;

namespace CabinetBilder.SpaceOsBridge.Persistence.Migrations;

public sealed class M001_InitialSchema : ISchemaMigration
{
    public int Version => 1;
    public string Description => "Initial schema: OutboxQueue, TemplateCache, MaterialCache, SeenSmartObjectGuids";

    public async Task UpAsync(SqliteConnection conn, SqliteTransaction tx, CancellationToken ct)
    {
        // OutboxQueue (DB-01)
        var cmd1 = conn.CreateCommand();
        cmd1.Transaction = tx;
        cmd1.CommandText = @"
            CREATE TABLE OutboxQueue (
                Id                       TEXT PRIMARY KEY,
                Operation                TEXT NOT NULL,
                PayloadJson              TEXT,
                EncryptedPayloadDpapi    BLOB,
                Status                   TEXT NOT NULL DEFAULT 'Pending',
                RetryCount               INTEGER NOT NULL DEFAULT 0,
                CreatedAt                TEXT NOT NULL,
                LastAttemptAt            TEXT,
                LastErrorMessage         TEXT,
                CompletedAt              TEXT,
                CHECK (Status IN ('Pending','InProgress','Succeeded','Failed')),
                CHECK (PayloadJson IS NOT NULL OR EncryptedPayloadDpapi IS NOT NULL),
                CHECK (LENGTH(COALESCE(PayloadJson, '')) + LENGTH(COALESCE(EncryptedPayloadDpapi, X'')) <= 1048576)
            );
            CREATE INDEX IX_OutboxQueue_Status_CreatedAt ON OutboxQueue(Status, CreatedAt) WHERE Status = 'Pending';
            CREATE INDEX IX_OutboxQueue_CompletedAt ON OutboxQueue(CompletedAt) WHERE Status = 'Succeeded';
        ";
        await cmd1.ExecuteNonQueryAsync(ct);

        // TemplateCache (DB-02)
        var cmd2 = conn.CreateCommand();
        cmd2.Transaction = tx;
        cmd2.CommandText = @"
            CREATE TABLE TemplateCache (
                Id                       TEXT PRIMARY KEY,
                Name                     TEXT NOT NULL,
                Version                  INTEGER NOT NULL,
                BodyJson                 TEXT NOT NULL,
                ETag                     TEXT NOT NULL,
                FetchedAt                TEXT NOT NULL,
                ExpiresAt                TEXT NOT NULL,
                TenantId                 TEXT NOT NULL
            );
            CREATE UNIQUE INDEX UX_TemplateCache_Name_TenantId ON TemplateCache(Name, TenantId);
            CREATE INDEX IX_TemplateCache_ExpiresAt ON TemplateCache(ExpiresAt);
        ";
        await cmd2.ExecuteNonQueryAsync(ct);

        // MaterialCache
        var cmd3 = conn.CreateCommand();
        cmd3.Transaction = tx;
        cmd3.CommandText = @"
            CREATE TABLE MaterialCache (
                MaterialCode             TEXT PRIMARY KEY,
                DisplayName              TEXT NOT NULL,
                Category                 TEXT NOT NULL,
                Thickness                REAL,
                BodyJson                 TEXT NOT NULL,
                PriceEncryptedDpapi      BLOB,
                ETag                     TEXT NOT NULL,
                FetchedAt                TEXT NOT NULL,
                ExpiresAt                TEXT NOT NULL,
                TenantId                 TEXT NOT NULL
            );
            CREATE UNIQUE INDEX UX_MaterialCache_MaterialCode_TenantId ON MaterialCache(MaterialCode, TenantId);
            CREATE INDEX IX_MaterialCache_ExpiresAt ON MaterialCache(ExpiresAt);
        ";
        await cmd3.ExecuteNonQueryAsync(ct);

        // SeenSmartObjectGuids (DB-05)
        var cmd4 = conn.CreateCommand();
        cmd4.Transaction = tx;
        cmd4.CommandText = @"
            CREATE TABLE SeenSmartObjectGuids (
                SmartObjectId            TEXT PRIMARY KEY,
                DrawingPath              TEXT NOT NULL,
                DrawingHash              TEXT NOT NULL,
                FirstSeenAt              TEXT NOT NULL,
                LastSeenAt               TEXT NOT NULL
            );
        ";
        await cmd4.ExecuteNonQueryAsync(ct);

        // AppSettings table for global metadata (Sync info)
        var cmd5 = conn.CreateCommand();
        cmd5.Transaction = tx;
        cmd5.CommandText = @"
            CREATE TABLE AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
        ";
        await cmd5.ExecuteNonQueryAsync(ct);
    }
}
