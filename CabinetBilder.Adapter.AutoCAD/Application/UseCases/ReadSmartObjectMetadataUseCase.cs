using CabinetBilder.Core.Common;
using CabinetBilder.Core.SmartObjects;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.Adapter.AutoCAD.Application.UseCases;

/// <summary>
/// Reads the domain metadata fields of a smart AutoCAD object.
/// </summary>
/// <remarks>
/// This use-case is AutoCAD-independent: it delegates persistence to
/// <see cref="ISmartObjectMetadataService"/>, which is implemented in the Infrastructure layer.
/// </remarks>
internal sealed class ReadSmartObjectMetadataUseCase(
    ISmartObjectMetadataService metadataService,
    ILogger<ReadSmartObjectMetadataUseCase> logger) : IReadSmartObjectMetadataUseCase
{
    private readonly ISmartObjectMetadataService _metadataService = metadataService
        ?? throw new ArgumentNullException(nameof(metadataService));
    private readonly ILogger<ReadSmartObjectMetadataUseCase> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Reads and merges metadata for the objects identified by <paramref name="objectHandles"/>.
    /// </summary>
    public Result<SmartObjectMetadata> Execute(IEnumerable<string> objectHandles)
    {
        if (objectHandles == null || !objectHandles.Any())
        {
            _logger.LogWarning("ReadSmartObjectMetadata failed: object handles collection is empty.");
            return Result.Failure<SmartObjectMetadata>("Object handles collection cannot be empty.");
        }

        var handlesList = objectHandles.ToList();
        if (handlesList.Count == 1)
        {
            return _metadataService.ReadMetadata(handlesList[0]);
        }

        _logger.LogInformation("Reading and merging metadata for {Count} objects.", handlesList.Count);

        var allMetadata = new List<SmartObjectMetadata>();
        foreach (var handle in handlesList)
        {
            var result = _metadataService.ReadMetadata(handle);
            if (result.IsSuccess)
            {
                allMetadata.Add(result.Value);
            }
            else
            {
                _logger.LogWarning("Failed to read metadata for object {Handle}: {Error}", handle, result.ErrorMessage);
            }
        }

        if (allMetadata.Count == 0)
        {
            return Result.Failure<SmartObjectMetadata>("Could not read metadata for any of the selected objects.");
        }

        if (allMetadata.Count == 1)
        {
            return Result.Success(allMetadata[0]);
        }

        // Merging logic
        var mergedFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var allKeys = allMetadata.SelectMany(m => m.Fields.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var key in allKeys)
        {
            string? firstValue = null;
            bool isFirst = true;
            bool isMixed = false;

            foreach (var metadata in allMetadata)
            {
                if (metadata.TryGetValue(key, out string value))
                {
                    if (isFirst)
                    {
                        firstValue = value;
                        isFirst = false;
                    }
                    else if (firstValue != value)
                    {
                        isMixed = true;
                        break;
                    }
                }
                else
                {
                    // Field missing in one of the objects
                    isMixed = true;
                    break;
                }
            }

            mergedFields[key] = isMixed ? "*VĂˇltozĂł*" : (firstValue ?? string.Empty);
        }

        return Result.Success(SmartObjectMetadata.From(mergedFields));
    }
}

