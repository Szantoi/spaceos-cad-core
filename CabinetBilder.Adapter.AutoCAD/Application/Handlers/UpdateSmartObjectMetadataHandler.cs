using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Core.SmartObjects.Requests;
using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.Adapter.AutoCAD.Application.Handlers;

/// <summary>
/// Handler for updating metadata on multiple AutoCAD objects.
/// </summary>
public class UpdateSmartObjectMetadataHandler : IRequestHandler<UpdateSmartObjectMetadataCommand, Result>
{
    private readonly ISmartObjectMetadataService _metadataService;
    private readonly ILogger<UpdateSmartObjectMetadataHandler> _logger;

    public UpdateSmartObjectMetadataHandler(ISmartObjectMetadataService metadataService, ILogger<UpdateSmartObjectMetadataHandler> logger)
    {
        _metadataService = metadataService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateSmartObjectMetadataCommand request, CancellationToken cancellationToken)
    {
        if (request.Handles == null || !request.Handles.Any())
        {
            return Result.Invalid(new ValidationError("Object handles collection cannot be empty."));
        }

        if (request.Metadata == null)
        {
            return Result.Invalid(new ValidationError("Metadata cannot be null."));
        }

        // Filter out mixed values and validate canonical keys (matches legacy UseCase)
        var fieldsToWrite = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in request.Metadata.Fields)
        {
            if (field.Value == "*VĂˇltozĂł*")
            {
                continue;
            }

            if (!SmartObjectMetadataKeys.IsCanonical(field.Key))
            {
                _logger.LogWarning("UpdateSmartObjectMetadata rejected non-canonical key '{Key}'.", field.Key);
                return Result.Error($"Key '{field.Key}' is not a canonical metadata key.");
            }

            fieldsToWrite[field.Key] = field.Value;
        }

        if (fieldsToWrite.Count == 0)
        {
            _logger.LogInformation("No changes to write (all fields were mixed or empty).");
            return Result.Success();
        }

        var effectiveMetadata = SmartObjectMetadata.From(fieldsToWrite);
        var handlesList = request.Handles.ToList();
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
                return Result.Error($"Failed to write metadata to all selected objects. Errors: {string.Join(", ", failures)}");
            }
            return Result.Error($"Partial success. Failed to write to {failures.Count} objects.");
        }

        return Result.Success();
    }
}

