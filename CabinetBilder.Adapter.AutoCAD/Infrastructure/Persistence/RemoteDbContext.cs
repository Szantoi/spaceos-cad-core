using Microsoft.EntityFrameworkCore;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence;

/// <summary>
/// Remote database context for centralized data (PostgreSQL).
/// </summary>
public class RemoteDbContext : DbContext
{
    public RemoteDbContext(DbContextOptions<RemoteDbContext> options) : base(options)
    {
    }

    public DbSet<CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence.Entities.SmartObjectEntity> SmartObjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence.Entities.SmartObjectEntity>(entity =>
        {
            entity.HasKey(e => new { e.DrawingId, e.Handle });
        });
    }
}

