namespace CabinetBilder.Core.Catalog;

/// <summary>
/// Port for Material persistence.
/// Defined in Core to keep Domain independent of SQLite/EF Core.
/// </summary>
public interface IMaterialRepository
{
    Task<Material?> GetByIdAsync(Guid id);
    Task<Material?> GetByCodeAsync(string code);
    Task<IEnumerable<Material>> GetAllAsync();
    Task AddAsync(Material material);
    Task UpdateAsync(Material material);
    Task DeleteAsync(Guid id);
}

