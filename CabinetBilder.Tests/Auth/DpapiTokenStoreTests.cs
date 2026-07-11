using CabinetBilder.Core.Sync;
using CabinetBilder.SpaceOsBridge.TokenStorage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.Auth;

[TestClass]
public class DpapiTokenStoreTests
{
    private string _tempDir = string.Empty;
    private DpapiTokenStore? _store;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"token_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _store = new DpapiTokenStore(_tempDir, NullLogger<DpapiTokenStore>.Instance);
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
    public async Task WriteRead_RoundTrip_Success()
    {
        // Arrange
        var tenantId = "tenant_abc";
        var token = new AuthToken("access_123", "refresh_456", DateTimeOffset.Now.AddHours(1), tenantId);

        // Act
        await _store!.WriteTokenAsync(tenantId, token);
        var restored = await _store.ReadTokenAsync(tenantId);

        // Assert
        Assert.IsNotNull(restored);
        Assert.AreEqual(token.AccessToken, restored.AccessToken);
        Assert.AreEqual(token.RefreshToken, restored.RefreshToken);
        Assert.AreEqual(token.TenantId, restored.TenantId);
    }

    [TestMethod]
    public async Task DeleteToken_RemovesFile()
    {
        // Arrange
        var tenantId = "tenant_abc";
        var token = new AuthToken("access_123", "refresh_456", DateTimeOffset.Now.AddHours(1), tenantId);
        await _store!.WriteTokenAsync(tenantId, token);

        // Act
        await _store.DeleteTokenAsync(tenantId);
        var restored = await _store.ReadTokenAsync(tenantId);

        // Assert
        Assert.IsNull(restored);
    }
}
