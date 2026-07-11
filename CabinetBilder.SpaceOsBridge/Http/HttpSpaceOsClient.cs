using CabinetBilder.Core.Sync;
using Ardalis.Result;

namespace CabinetBilder.SpaceOsBridge.Http;

public sealed class HttpSpaceOsClient : ISpaceOsClient
{
    public Task<Result<IReadOnlyList<ProductTemplateDto>>> PullTemplatesAsync(string? ifNoneMatch = null, CancellationToken ct = default)
    {
        return Task.FromResult(Result<IReadOnlyList<ProductTemplateDto>>.Success(new List<ProductTemplateDto>()));
    }

    public Task<Result<IReadOnlyList<MaterialDto>>> PullMaterialsAsync(string? ifNoneMatch = null, CancellationToken ct = default)
    {
        return Task.FromResult(Result<IReadOnlyList<MaterialDto>>.Success(new List<MaterialDto>()));
    }

    public Task<Result> SubmitCuttingSheetAsync(string payloadJson, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> AnchorHashAsync(string payloadJson, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SubmitBomAsync(string payloadJson, CancellationToken ct = default)
    {
        // TODO: Real HTTP call to SpaceOS API
        return Task.FromResult(Result.Success());
    }
}
