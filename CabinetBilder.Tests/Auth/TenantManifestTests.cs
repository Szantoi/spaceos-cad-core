using CabinetBilder.SpaceOsBridge.TokenStorage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.Auth;

[TestClass]
public class TenantManifestTests
{
    private string _tempDir = string.Empty;
    private TenantManifestManager? _manager;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"manifest_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _manager = new TenantManifestManager(_tempDir, NullLogger<TenantManifestManager>.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public async Task AddOrUpdateTenant_SavesToDisk()
    {
        // Arrange
        var entry = new TenantEntry("tenant1", "My Tenant", "https://auth.com", DateTimeOffset.Now);

        // Act
        await _manager!.AddOrUpdateTenantAsync(entry);
        var manifest = await _manager.GetManifestAsync();

        // Assert
        Assert.AreEqual(1, manifest.Tenants.Count);
        Assert.AreEqual("tenant1", manifest.ActiveTenantId);
        Assert.AreEqual("My Tenant", manifest.Tenants[0].DisplayName);
    }

    [TestMethod]
    public async Task RemoveTenant_UpdatesManifest()
    {
        // Arrange
        var entry = new TenantEntry("tenant1", "My Tenant", "https://auth.com", DateTimeOffset.Now);
        await _manager!.AddOrUpdateTenantAsync(entry);

        // Act
        await _manager.RemoveTenantAsync("tenant1");
        var manifest = await _manager.GetManifestAsync();

        // Assert
        Assert.AreEqual(0, manifest.Tenants.Count);
        Assert.IsNull(manifest.ActiveTenantId);
    }
}
