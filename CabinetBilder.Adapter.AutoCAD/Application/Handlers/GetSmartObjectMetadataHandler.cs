using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Core.SmartObjects.Requests;
using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CabinetBilder.Adapter.AutoCAD.Application.Handlers;

/// <summary>
/// Handler for reading and merging metadata from multiple AutoCAD objects.
/// </summary>
public class GetSmartObjectMetadataHandler : IRequestHandler<GetSmartObjectMetadataQuery, Result<SmartObjectMetadata>>
{
    private readonly ISmartObjectMetadataService _metadataService;
    private readonly ILogger<GetSmartObjectMetadataHandler> _logger;

    public GetSmartObjectMetadataHandler(ISmartObjectMetadataService metadataService, ILogger<GetSmartObjectMetadataHandler> logger)
    {
        _metadataService = metadataService;
        _logger = logger;
    }

    public async Task<Result<SmartObjectMetadata>> Handle(GetSmartObjectMetadataQuery request, CancellationToken cancellationToken)
    {
        if (request.Handles == null || !request.Handles.Any())
        {
            return Result.Invalid(new ValidationError("Object handles collection cannot be empty."));
        }

        var handlesList = request.Handles.ToList();
        
        // Single object optimization
        if (handlesList.Count == 1)
        {
            var result = _metadataService.ReadMetadata(handlesList[0]);
            return result.IsSuccess 
                ? Result.Success(result.Value) 
                : Result.Error(result.ErrorMessage ?? "Unknown error");
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
            return Result.Error("Could not read metadata for any of the selected objects.");
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

            foreach (var m in allMetadata)
            {
                if (m.TryGetValue(key, out string value))
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
                    isMixed = true;
                    break;
                }
            }

            mergedFields[key] = isMixed ? "*VĂˇltozĂł*" : (firstValue ?? string.Empty);
        }

        return Result.Success(SmartObjectMetadata.From(mergedFields));
    }
}

