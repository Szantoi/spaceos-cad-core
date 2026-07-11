using Microsoft.Extensions.Logging.Abstractions;
using CabinetBilder.Core.Sync;
using CabinetBilder.SpaceOsBridge.Persistence;
using CabinetBilder.SpaceOsBridge.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.Persistence;

[TestClass]
public class SqliteLocalStoreTests
{
    private string _dbPath = string.Empty;
    private SqliteLocalStore? _store;

    [TestInitialize]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_client_{Guid.NewGuid()}.db");
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_store != null)
        {
            await _store.DisposeAsync();
            _store = null;
        }

        // Add a small delay to ensure SQLite release the file handle
        await Task.Delay(100);

        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* Ignore cleanup errors in tests */ }
        }
    }

    [TestMethod]
    public async Task InitializeAsync_CreatesExpectedTablesV2()
    {
        // Arrange
        var migrator = new SchemaMigrator(NullLogger<SchemaMigrator>.Instance);
        var security = new DpapiSecurityService(NullLogger<DpapiSecurityService>.Instance);
        _store = new SqliteLocalStore(_dbPath, migrator, security, NullLogger<SqliteLocalStore>.Instance);

        // Act
        await _store.InitializeAsync();

        // Assert
        using var connection = _store.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('OutboxQueue', 'TemplateCache', 'MaterialCache', 'SeenSmartObjectGuids');";
        
        var tables = new List<string>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        Assert.IsTrue(tables.Contains("OutboxQueue"), "OutboxQueue table should exist");
        Assert.IsTrue(tables.Contains("TemplateCache"), "TemplateCache table should exist");
        Assert.IsTrue(tables.Contains("MaterialCache"), "MaterialCache table should exist");
        Assert.IsTrue(tables.Contains("SeenSmartObjectGuids"), "SeenSmartObjectGuids table should exist");
    }

    [TestMethod]
    public async Task UpsertMaterialAsync_EncryptsPrice()
    {
        // Arrange
        var migrator = new SchemaMigrator(NullLogger<SchemaMigrator>.Instance);
        var security = new DpapiSecurityService(NullLogger<DpapiSecurityService>.Instance);
        _store = new SqliteLocalStore(_dbPath, migrator, security, NullLogger<SqliteLocalStore>.Instance);
        await _store.InitializeAsync();

        var materials = new List<MaterialDto>
        {
            new MaterialDto("MAT001", "Oak", "Wood", 18.0, "{}", 1500.50m)
        };

        // Act
        await _store.UpsertMaterialCacheAsync(materials, "etag-1", "tenant-1");

        // Assert
        using var connection = _store.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT PriceEncryptedDpapi FROM MaterialCache WHERE MaterialCode = 'MAT001'";
        var encrypted = await cmd.ExecuteScalarAsync() as byte[];

        Assert.IsNotNull(encrypted, "Price should be stored as encrypted blob");
        
        // Act - Read back
        var cached = await _store.GetCachedMaterialsAsync();
        var material = cached.Value.FirstOrDefault(m => m.MaterialCode == "MAT001");

        Assert.IsNotNull(material, "Material should be retrieved from cache");
        Assert.AreEqual(1500.50m, material.Price, "Price should be decrypted correctly");
    }
}
