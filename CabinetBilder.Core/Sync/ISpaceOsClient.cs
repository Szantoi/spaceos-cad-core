using Ardalis.Result;

namespace CabinetBilder.Core.Sync;

/// <summary>
/// Port for communicating with SpaceOS Backend API.
/// </summary>
public interface ISpaceOsClient
{
    Task<Result<IReadOnlyList<ProductTemplateDto>>> PullTemplatesAsync(string? ifNoneMatch = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MaterialDto>>> PullMaterialsAsync(string? ifNoneMatch = null, CancellationToken ct = default);
    
    Task<Result> SubmitCuttingSheetAsync(string payloadJson, CancellationToken ct = default);
    Task<Result> AnchorHashAsync(string payloadJson, CancellationToken ct = default);
    Task<Result> SubmitBomAsync(string payloadJson, CancellationToken ct = default);
}
