using CabinetBilder.Core.Common;
using CabinetBilder.Core.SmartObjects;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.Adapter.AutoCAD.Application.UseCases;

/// <summary>
/// Writes domain metadata fields to a smart AutoCAD object.
/// </summary>
/// <remarks>
/// Only canonical keys defined in <see cref="SmartObjectMetadataKeys"/> are accepted.
/// Non-canonical keys are rejected to prevent schema drift.
/// </remarks>
internal sealed class WriteSmartObjectMetadataUseCase(
    ISmartObjectMetadataService metadataService,
    ILogger<WriteSmartObjectMetadataUseCase> logger) : IWriteSmartObjectMetadataUseCase
{
    private readonly ISmartObjectMetadataService _metadataService = metadataService
        ?? throw new ArgumentNullException(nameof(metadataService));
    private readonly ILogger<WriteSmartObjectMetadataUseCase> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Writes <paramref name="metadata"/> to the objects identified by <paramref name="objectHandles"/>.
    /// Fields with value "*VĂˇltozĂł*" are skipped to prevent overwriting unique values with a mixed-state marker.
    /// </summary>
    public Result Execute(IEnumerable<string> objectHandles, SmartObjectMetadata metadata)
    {
        if (objectHandles == null || !objectHandles.Any())
        {
            _logger.LogWarning("WriteSmartObjectMetadata failed: object handles collection is empty.");
            return Result.Failure("Object handles collection cannot be empty.");
        }

        if (metadata is null)
        {
            _logger.LogWarning("WriteSmartObjectMetadata failed: metadata is null.");
            return Result.Failure("Metadata cannot be null.");
        }

        // Filter out mixed values and validate canonical keys
        var fieldsToWrite = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in metadata.Fields)
        {
            if (field.Value == "*VĂˇltozĂł*")
            {
                continue;
            }

            if (!SmartObjectMetadataKeys.IsCanonical(field.Key))
            {
                _logger.LogWarning("WriteSmartObjectMetadata rejected non-canonical key '{Key}'.", field.Key);
                return Result.Failure($"Key '{field.Key}' is not a canonical metadata key.");
            }

            fieldsToWrite[field.Key] = field.Value;
        }

        if (fieldsToWrite.Count == 0)
        {
            _logger.LogInformation("No changes to write (all fields were mixed or empty).");
            return Result.Success();
        }

        var effectiveMetadata = SmartObjectMetadata.From(fieldsToWrite);
        var handlesList = objectHandles.ToList();
        var failures = new List<string>();

        _logger.LogInformation("Writing {Count} field(s) to {ObjectCount} object(s).", 
            fieldsToWrite.Count, handlesList.Count);

        foreach (var handle in handlesList)
        {
            var result = _metadataService.WriteMetadata(handle, effectiveMetadata);
            if (result.IsFailure)
            {
                failures.Add($"{handle}: {result.ErrorMessage}");
                _logger.LogWarning("Failed to write metadata for object {Handle}: {Error}", handle, result.ErrorMessage);
            }
        }

        if (failures.Count > 0)
        {
            if (failures.Count == handlesList.Count)
            {
                return Result.Failure($"Failed to write metadata to all selected objects. Errors: {string.Join(", ", failures)}");
            }
            return Result.Failure($"Partial success. Failed to write to {failures.Count} objects.");
        }

        return Result.Success();
    }
}

