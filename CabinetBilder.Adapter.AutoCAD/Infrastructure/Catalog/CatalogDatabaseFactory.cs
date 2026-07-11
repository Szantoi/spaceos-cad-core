using CabinetBilder.Core.Catalog;
using Microsoft.EntityFrameworkCore;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Catalog;

/// <summary>
/// Factory for creating and initializing the catalog database.
/// Used for the SQLite spike proof-of-concept.
/// </summary>
internal static class CatalogDatabaseFactory
{
    /// <summary>
    /// Creates a new CatalogDbContext pointing to the default writable SQLite file.
    /// Ensures the database schema is created.
    /// </summary>
    public static CatalogDbContext Create()
    {
        string dbPath = CatalogPathProvider.GetDatabasePath();
        
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        // Using WAL (Write-Ahead Logging) mode for better concurrency in multi-instance AutoCAD
        optionsBuilder.UseSqlite($"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;Pooling=True;");

        var context = new CatalogDbContext(optionsBuilder.Options);
        
        // Ensure database exists and schema is up to date
        context.Database.EnsureCreated();
        
        // Optimize performance for SQLite
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

        SeedIfEmpty(context);

        return context;
    }

    private static void SeedIfEmpty(CatalogDbContext context)
    {
        if (context.Materials.Any()) return;

        context.Materials.AddRange(
            Material.Create("18-WHITE", "FehĂ©r bĂştorlap 18mm", 18.0, 650.0),
            Material.Create("18-OAK", "TĂ¶lgy bĂştorlap 18mm", 18.0, 720.0),
            Material.Create("18-ANTHRACITE", "Antracit bĂştorlap 18mm", 18.0, 680.0),
            Material.Create("10-BACK", "HĂˇtfal lemez 10mm", 10.0, 600.0)
        );

        context.SaveChanges();
    }
}

