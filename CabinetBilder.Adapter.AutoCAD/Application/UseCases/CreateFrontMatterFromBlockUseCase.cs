using CabinetBilder.Core.Common;
using CabinetBilder.Core.FrontMatter;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CabinetBilder.Adapter.AutoCAD.Application.UseCases;

/// <summary>
/// Encapsulates front matter creation from a selected dynamic block's data.
/// </summary>
internal sealed class CreateFrontMatterFromBlockUseCase(ILogger<CreateFrontMatterFromBlockUseCase> logger)
{
    private readonly ILogger<CreateFrontMatterFromBlockUseCase> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Generates the front matter text based on the block's data.
    /// </summary>
    /// <param name="request">Creation request with extracted block data.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the generated front matter payload on success,
    /// or an error message on failure.
    /// </returns>
    public Result<CreateFrontMatterFromBlockResult> Execute(CreateFrontMatterFromBlockRequest request)
    {
        if (request is null)
        {
            return Result.Failure<CreateFrontMatterFromBlockResult>("Request cannot be null.");
        }

        _logger.LogInformation("Starting front matter creation for block {BlockHandle} and type {Type}.", request.BlockHandle, request.SelectedType);

        if (string.IsNullOrWhiteSpace(request.SelectedType))
        {
            _logger.LogWarning("Front matter creation failed: type value is empty for block {BlockHandle}.", request.BlockHandle);
            return Result.Failure<CreateFrontMatterFromBlockResult>("Type value cannot be empty.");
        }

        Dictionary<string, string> dynamicPropertyValues = ExtractDynamicPropertyValues(request.DynamicProperties, request.DrawingPrecision);
        Dictionary<string, string> replacementValues = FrontMatterValueBuilder.BuildValues(
            request.SelectedType,
            request.BlockHandle,
            request.BlockName,
            dynamicPropertyValues);

        string frontMatterText = FrontMatterTextService.BuildTemplate(replacementValues);

        _logger.LogInformation("Front matter text successfully generated.");

        return Result.Success(new CreateFrontMatterFromBlockResult(frontMatterText));
    }

    private static Dictionary<string, string> ExtractDynamicPropertyValues(IReadOnlyDictionary<string, object> properties, int drawingPrecision)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (var property in properties)
        {
            values[property.Key] = FrontMatterTextService.FormatDynamicValue(property.Value, drawingPrecision);
        }

        return values;
    }
}

/// <summary>
/// Request model for front matter creation from a dynamic block.
/// </summary>
/// <param name="BlockHandle">The handle string of the block.</param>
/// <param name="BlockName">The name of the block.</param>
/// <param name="DynamicProperties">The extracted dynamic properties from the block.</param>
/// <param name="DrawingPrecision">Current drawing linear precision.</param>
/// <param name="SelectedType">Type value selected by the user.</param>
internal sealed record CreateFrontMatterFromBlockRequest(
    string BlockHandle,
    string BlockName,
    IReadOnlyDictionary<string, object> DynamicProperties,
    int DrawingPrecision,
    string SelectedType);

/// <summary>
/// Result model for front matter creation.
/// </summary>
/// <param name="FrontMatterText">Generated front matter payload.</param>
internal sealed record CreateFrontMatterFromBlockResult(string FrontMatterText);

