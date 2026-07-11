using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.SpaceOsBridge.TokenStorage;

public record TenantEntry(string TenantId, string DisplayName, string EndpointUrl, DateTimeOffset LastLogin);

public record TenantManifest(int SchemaVersion, string? ActiveTenantId, List<TenantEntry> Tenants);

/// <summary>
/// Manages the tenants.manifest.json file in AppData.
/// </summary>
public class TenantManifestManager
{
    private readonly string _manifestPath;
    private readonly ILogger<TenantManifestManager> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public TenantManifestManager(string baseDir, ILogger<TenantManifestManager> logger)
    {
        var tokensDir = Path.Combine(baseDir, "tokens");
        if (!Directory.Exists(tokensDir)) Directory.CreateDirectory(tokensDir);
        
        _manifestPath = Path.Combine(tokensDir, "tenants.manifest.json");
        _logger = logger;
    }

    public async Task<TenantManifest> GetManifestAsync()
    {
        if (!File.Exists(_manifestPath))
        {
            return new TenantManifest(1, null, new List<TenantEntry>());
        }

        try
        {
            await using var stream = File.OpenRead(_manifestPath);
            return await JsonSerializer.DeserializeAsync<TenantManifest>(stream) 
                   ?? new TenantManifest(1, null, new List<TenantEntry>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read tenant manifest");
            return new TenantManifest(1, null, new List<TenantEntry>());
        }
    }

    public async Task SaveManifestAsync(TenantManifest manifest)
    {
        try
        {
            var tempPath = _manifestPath + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions);
            }
            
            if (File.Exists(_manifestPath)) File.Delete(_manifestPath);
            File.Move(tempPath, _manifestPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save tenant manifest");
        }
    }

    public async Task AddOrUpdateTenantAsync(TenantEntry entry, bool makeActive = true)
    {
        var manifest = await GetManifestAsync();
        var existing = manifest.Tenants.FirstOrDefault(t => t.TenantId == entry.TenantId);
        
        if (existing != null) manifest.Tenants.Remove(existing);
        manifest.Tenants.Add(entry);

        var activeId = makeActive ? entry.TenantId : manifest.ActiveTenantId;
        
        await SaveManifestAsync(manifest with { ActiveTenantId = activeId });
    }

    public async Task RemoveTenantAsync(string tenantId)
    {
        var manifest = await GetManifestAsync();
        var entry = manifest.Tenants.FirstOrDefault(t => t.TenantId == tenantId);
        if (entry == null) return;

        manifest.Tenants.Remove(entry);
        var activeId = manifest.ActiveTenantId == tenantId ? null : manifest.ActiveTenantId;
        
        await SaveManifestAsync(manifest with { ActiveTenantId = activeId });
    }
}
