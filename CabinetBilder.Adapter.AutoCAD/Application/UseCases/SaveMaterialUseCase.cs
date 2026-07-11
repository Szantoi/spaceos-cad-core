using CabinetBilder.Core.Catalog;

namespace CabinetBilder.Adapter.AutoCAD.Application.UseCases;

public interface ISaveMaterialUseCase
{
    Task ExecuteAsync(Material material);
}

public sealed class SaveMaterialUseCase(IMaterialRepository repository) : ISaveMaterialUseCase
{
    private readonly IMaterialRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task ExecuteAsync(Material material)
    {
        if (material.Id == Guid.Empty)
        {
            // NEW: In a real app we'd probably use a factory or set ID here if not set
            // For now assume Material.Id is initialized in constructor as Guid.NewGuid()
            await _repository.AddAsync(material);
        }
        else
        {
            await _repository.UpdateAsync(material);
        }
    }
}

