using System.Text.Json;
using CabinetBilder.Core.Sync;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.SpaceOsBridge.TokenStorage;

/// <summary>
/// Secure token store using Windows DPAPI.
/// Stores tokens in {tenantId}.token.dpapi files.
/// </summary>
public class DpapiTokenStore
{
    private readonly string _tokensDir;
    private readonly ILogger<DpapiTokenStore> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DpapiTokenStore(string baseDir, ILogger<DpapiTokenStore> logger)
    {
        _tokensDir = Path.Combine(baseDir, "tokens");
        if (!Directory.Exists(_tokensDir)) Directory.CreateDirectory(_tokensDir);
        _logger = logger;
    }

    public async Task<AuthToken?> ReadTokenAsync(string tenantId)
    {
        var path = GetTokenPath(tenantId);
        if (!File.Exists(path)) return null;

        await _lock.WaitAsync();
        try
        {
            var encrypted = await File.ReadAllBytesAsync(path);
            var decrypted = DpapiHelper.DecryptBytes(encrypted);
            return JsonSerializer.Deserialize<AuthToken>(decrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read encrypted token for tenant {TenantId}", tenantId);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteTokenAsync(string tenantId, AuthToken token)
    {
        var path = GetTokenPath(tenantId);
        
        await _lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(token);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            var encrypted = DpapiHelper.EncryptBytes(data);
            
            var tempPath = path + ".tmp";
            await File.WriteAllBytesAsync(tempPath, encrypted);
            
            if (File.Exists(path)) File.Delete(path);
            File.Move(tempPath, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write encrypted token for tenant {TenantId}", tenantId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteTokenAsync(string tenantId)
    {
        var path = GetTokenPath(tenantId);
        await _lock.WaitAsync();
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetTokenPath(string tenantId) 
        => Path.Combine(_tokensDir, $"{tenantId}.token.dpapi");
}
