using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using CabinetBilder.SpaceOsBridge.Persistence.Migrations;
using Ardalis.Result;

namespace CabinetBilder.SpaceOsBridge.Persistence;

public sealed class SchemaMigrator
{
    private readonly ILogger<SchemaMigrator> _logger;
    private static readonly IReadOnlyList<ISchemaMigration> Migrations = new ISchemaMigration[]
    {
        new M001_InitialSchema()
    };

    public SchemaMigrator(ILogger<SchemaMigrator> logger)
    {
        _logger = logger;
    }

    public async Task<Result> MigrateAsync(SqliteConnection conn, CancellationToken ct = default)
    {
        var currentVersion = await GetUserVersionAsync(conn, ct);
        var targetVersion = Migrations.Count;

        _logger.LogInformation("Database version check: Current={Current}, Target={Target}", currentVersion, targetVersion);

        if (currentVersion > targetVersion)
        {
            return Result.Error($"Database version {currentVersion} is newer than plugin expects ({targetVersion}). Downgrade is not supported.");
        }

        for (int i = currentVersion; i < targetVersion; i++)
        {
            var migration = Migrations[i];
            _logger.LogInformation("Applying migration v{Version}: {Description}", migration.Version, migration.Description);

            using var transaction = conn.BeginTransaction();
            try
            {
                await migration.UpAsync(conn, transaction, ct);
                await SetUserVersionAsync(conn, migration.Version, ct);
                await transaction.CommitAsync(ct);
                _logger.LogInformation("Successfully applied migration v{Version}", migration.Version);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Migration to v{Version} failed", migration.Version);
                return Result.Error($"Migration to v{migration.Version} failed: {ex.Message}");
            }
        }

        return Result.Success();
    }

    private async Task<int> GetUserVersionAsync(SqliteConnection conn, CancellationToken ct)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    private async Task SetUserVersionAsync(SqliteConnection conn, int version, CancellationToken ct)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA user_version = {version};";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
