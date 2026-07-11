using CabinetBilder.Core.Catalog;

namespace CabinetBilder.Adapter.AutoCAD.Application.UseCases;

public interface IDeleteMaterialUseCase
{
    Task ExecuteAsync(Guid id);
}

public sealed class DeleteMaterialUseCase(IMaterialRepository repository) : IDeleteMaterialUseCase
{
    private readonly IMaterialRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task ExecuteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}

