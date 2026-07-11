using CabinetBilder.Core.Catalog;
using Microsoft.EntityFrameworkCore;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Catalog;

/// <summary>
/// SQLite implementation of the Material repository.
/// Concrete adapter for the IMaterialRepository port.
/// </summary>
internal sealed class SqliteMaterialRepository(CatalogDbContext context) : IMaterialRepository
{
    private readonly CatalogDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Material?> GetByIdAsync(Guid id)
    {
        return await _context.Materials.FindAsync(id);
    }

    public async Task<Material?> GetByCodeAsync(string code)
    {
        return await _context.Materials.FirstOrDefaultAsync(m => m.Code == code);
    }

    public async Task<IEnumerable<Material>> GetAllAsync()
    {
        return await _context.Materials.ToListAsync();
    }

    public async Task AddAsync(Material material)
    {
        await _context.Materials.AddAsync(material);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Material material)
    {
        _context.Materials.Update(material);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var material = await GetByIdAsync(id);
        if (material != null)
        {
            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();
        }
    }
}

