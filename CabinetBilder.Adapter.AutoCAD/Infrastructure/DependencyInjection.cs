using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;
using CabinetBilder.Adapter.AutoCAD.Application.Handlers;
using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using CabinetBilder.Core.Infrastructure;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.Caching;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.Storage;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Minio;
using CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;
using System.Reflection;
using CabinetBilder.SpaceOsBridge;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure;

public static class DependencyInjection
{
    public static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // New Bridge Integration
        services.AddLogging(); // Ensure logging is available for the bridge
        services.AddSpaceOsBridge();

        // 0. Configuration
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(assemblyLocation)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // 1. Infrastructure - Smart Objects
        services.AddSingleton<IDrawingObjectMetadataStore, DrawingObjectMetadataStore>();
        services.AddSingleton<ISmartObjectMetadataService, SmartObjectMetadataService>(sp => 
            new SmartObjectMetadataService(
                sp.GetRequiredService<IDrawingObjectMetadataStore>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmartObjectMetadataService>>()));

        services.AddSingleton<IDrawingOpenedHandler, DrawingOpenedHandler>();

        // 1. Infrastructure - Skeleton (DWG Persistence)
        services.AddSingleton<CabinetBilder.Core.Ports.ICadSkeletonStore, CabinetBilder.Adapter.AutoCAD.Persistence.AutoCadSkeletonStore>();

        // 1. Infrastructure - Local Catalog (SQLite)
        services.AddSingleton<CabinetBilder.Adapter.AutoCAD.Infrastructure.Catalog.CatalogDbContext>(_ => 
            CabinetBilder.Adapter.AutoCAD.Infrastructure.Catalog.CatalogDatabaseFactory.Create());
        services.AddSingleton<CabinetBilder.Core.Catalog.IMaterialRepository, CabinetBilder.Adapter.AutoCAD.Infrastructure.Catalog.SqliteMaterialRepository>();

        // 1. Infrastructure - Remote Persistence (PostgreSQL)
        var pgConnString = configuration.GetConnectionString("RemotePostgres");
        if (!string.IsNullOrEmpty(pgConnString))
        {
            services.AddDbContext<RemoteDbContext>(options =>
                options.UseNpgsql(pgConnString));
        }

        // 1. Infrastructure - Redis
        var redisConnString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnString))
        {
            services.AddSingleton<IRedisService>(new RedisService(redisConnString));
        }

        // 1. Infrastructure - Minio
        var minioSection = configuration.GetSection("Minio");
        if (minioSection.Exists())
        {
            var endpoint = minioSection["Endpoint"] ?? "localhost:9000";
            var accessKey = minioSection["AccessKey"] ?? "";
            var secretKey = minioSection["SecretKey"] ?? "";
            var secure = bool.TryParse(minioSection["Secure"], out var s) && s;
            var bucketName = minioSection["BucketName"] ?? "default";

            services.AddSingleton<IMinioClient>(sp => 
                new MinioClient()
                    .WithEndpoint(endpoint)
                    .WithCredentials(accessKey, secretKey)
                    .WithSSL(secure)
                    .Build());

            services.AddSingleton<IBlobStorageService>(sp => 
                new MinioStorageService(sp.GetRequiredService<IMinioClient>(), bucketName));
        }

        // 2. UseCases (Legacy/Transition)
        services.AddTransient<IGetCatalogMaterialsUseCase, GetCatalogMaterialsUseCase>();
        services.AddTransient<ISaveMaterialUseCase, SaveMaterialUseCase>();
        services.AddTransient<IDeleteMaterialUseCase, DeleteMaterialUseCase>();

        // 3. MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // 4. UI
        services.AddTransient<SmartObjectPaletteViewModel>();
        services.AddTransient<CabinetBilder.Adapter.AutoCAD.UI.CatalogManagement.CatalogManagementViewModel>();

        return services.BuildServiceProvider();
    }
}

