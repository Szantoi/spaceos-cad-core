using Microsoft.Data.Sqlite;

namespace CabinetBilder.SpaceOsBridge.Persistence;

/// <summary>
/// Interface for individual schema migrations.
/// </summary>
public interface ISchemaMigration
{
    int Version { get; }
    string Description { get; }
    Task UpAsync(SqliteConnection conn, SqliteTransaction tx, CancellationToken ct);
}
