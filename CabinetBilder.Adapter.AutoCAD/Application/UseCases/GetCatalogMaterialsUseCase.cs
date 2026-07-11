using CabinetBilder.Core.Catalog;

namespace CabinetBilder.Adapter.AutoCAD.Application.UseCases;

/// <summary>
/// Port for retrieving all available materials from the catalog.
/// </summary>
public interface IGetCatalogMaterialsUseCase
{
    Task<IEnumerable<Material>> ExecuteAsync();
}

/// <summary>
/// Implementation of the GetCatalogMaterialsUseCase.
/// </summary>
public sealed class GetCatalogMaterialsUseCase(IMaterialRepository repository) : IGetCatalogMaterialsUseCase
{
    private readonly IMaterialRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<Material>> ExecuteAsync()
    {
        return await _repository.GetAllAsync();
    }
}

