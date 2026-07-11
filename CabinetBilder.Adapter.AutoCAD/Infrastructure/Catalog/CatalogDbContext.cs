using CabinetBilder.Core.Catalog;
using Microsoft.EntityFrameworkCore;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Catalog;

/// <summary>
/// EF Core database context for the catalog module.
/// Configured for SQLite.
/// </summary>
internal sealed class CatalogDbContext : DbContext
{
    public DbSet<Material> Materials => Set<Material>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            
            // Map properties to private fields if necessary, 
            // but here we use simple auto-properties with private setters.
            entity.Property(e => e.Thickness).IsRequired();
            entity.Property(e => e.Density).IsRequired();
        });
    }
}

