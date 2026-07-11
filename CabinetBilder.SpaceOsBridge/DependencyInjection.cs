using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using CabinetBilder.SpaceOsBridge.Persistence;
using CabinetBilder.SpaceOsBridge.Outbox;
using CabinetBilder.SpaceOsBridge.Http;
using CabinetBilder.Core.Sync;
using CabinetBilder.SpaceOsBridge.Security;
using CabinetBilder.SpaceOsBridge.Sync;

namespace CabinetBilder.SpaceOsBridge;

public static class DependencyInjection
{
    public class SpaceOsBridgeOptions
    {
        public string BaseDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CabinetBilder");
        public string Authority { get; set; } = "https://auth.spaceos.hu/realms/cabinet-bilder";
        public string ClientId { get; set; } = "cabinet-bilder-desktop";
        public string Scope { get; set; } = "openid profile email offline_access";
    }

    public static IServiceCollection AddSpaceOsBridge(this IServiceCollection services, Action<SpaceOsBridgeOptions>? configure = null)
    {
        var options = new SpaceOsBridgeOptions();
        configure?.Invoke(options);

        // Infrastructure & Persistence
        services.AddSingleton<SchemaMigrator>();
        services.AddSingleton<ISecurityService, DpapiSecurityService>();
        
        services.AddSingleton<ILocalStore>(sp =>
        {
            var baseDir = options.BaseDirectory;
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            var dbPath = Path.Combine(baseDir, "client.db");
            var migrator = sp.GetRequiredService<SchemaMigrator>();
            var security = sp.GetRequiredService<ISecurityService>();
            var logger = sp.GetRequiredService<ILogger<SqliteLocalStore>>();
            
            var store = new SqliteLocalStore(dbPath, migrator, security, logger);
            store.InitializeAsync().GetAwaiter().GetResult();
            
            return store;
        });

        // Token Management
        services.AddSingleton(sp => 
        {
            return new CabinetBilder.SpaceOsBridge.TokenStorage.TenantManifestManager(options.BaseDirectory, sp.GetRequiredService<ILogger<CabinetBilder.SpaceOsBridge.TokenStorage.TenantManifestManager>>());
        });

        services.AddSingleton(sp => 
        {
            return new CabinetBilder.SpaceOsBridge.TokenStorage.DpapiTokenStore(options.BaseDirectory, sp.GetRequiredService<ILogger<CabinetBilder.SpaceOsBridge.TokenStorage.DpapiTokenStore>>());
        });

        // HTTP Client & API
        services.AddHttpClient();
        services.AddSingleton<ISpaceOsClient, HttpSpaceOsClient>();

        // Auth
        services.AddSingleton<ISpaceOsAuthenticator>(sp => 
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            
            return new CabinetBilder.SpaceOsBridge.Auth.DeviceCodeAuthenticator(
                httpClient,
                options.Authority,
                options.ClientId,
                options.Scope,
                sp.GetRequiredService<CabinetBilder.SpaceOsBridge.TokenStorage.DpapiTokenStore>(),
                sp.GetRequiredService<CabinetBilder.SpaceOsBridge.TokenStorage.TenantManifestManager>(),
                sp.GetRequiredService<ILogger<CabinetBilder.SpaceOsBridge.Auth.DeviceCodeAuthenticator>>());
        });

        // Sync & Connection
        services.AddSingleton<IConnectionState>(sp => sp.GetRequiredService<ISpaceOsAuthenticator>().CurrentState);
        services.AddSingleton<SyncManager>();

        // Outbox & Background Sync
        services.AddSingleton<OutboxQueue>();
        services.AddSingleton<OutboxLeader>();
        services.AddHostedService<OutboxWorker>();

        return services;
    }
}
