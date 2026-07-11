namespace CabinetBilder.Core.FrontMatter;

/// <summary>
/// Parses user input for front matter type selection.
/// </summary>
public static class FrontMatterTypeInputParser
{
    /// <summary>
    /// Parses a raw picker input against known type values.
    /// </summary>
    /// <param name="input">Raw user input.</param>
    /// <param name="knownTypes">Known type values in display order.</param>
    /// <param name="allowCreateNewType">Whether new type creation is enabled.</param>
    /// <returns>A parse result that describes the intended action.</returns>
    public static FrontMatterTypeInputParseResult Parse(string? input, IReadOnlyList<string> knownTypes, bool allowCreateNewType)
    {
        ArgumentNullException.ThrowIfNull(knownTypes);

        string normalized = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (knownTypes.Count == 0)
            {
                return FrontMatterTypeInputParseResult.Invalid("No known types are available.");
            }

            return FrontMatterTypeInputParseResult.Select(knownTypes[0]);
        }

        if (string.Equals(normalized, "R", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Refresh", StringComparison.OrdinalIgnoreCase))
        {
            return FrontMatterTypeInputParseResult.Refresh();
        }

        if (string.Equals(normalized, "N", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "New", StringComparison.OrdinalIgnoreCase))
        {
            return allowCreateNewType
                ? FrontMatterTypeInputParseResult.CreateNewType()
                : FrontMatterTypeInputParseResult.Invalid("Unknown Type input. Use an index, full name, or R.");
        }

        if (int.TryParse(normalized, out int selectedIndex))
        {
            if (selectedIndex >= 1 && selectedIndex <= knownTypes.Count)
            {
                return FrontMatterTypeInputParseResult.Select(knownTypes[selectedIndex - 1]);
            }

            return FrontMatterTypeInputParseResult.Invalid($"Invalid index. Use a value between 1 and {knownTypes.Count}.");
        }

        string? exactMatch = knownTypes.FirstOrDefault(type => string.Equals(type, normalized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exactMatch))
        {
            return FrontMatterTypeInputParseResult.Select(exactMatch);
        }

        List<string> partialMatches = knownTypes
            .Where(type => type.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (partialMatches.Count == 1)
        {
            return FrontMatterTypeInputParseResult.Select(partialMatches[0]);
        }

        if (partialMatches.Count > 1)
        {
            return FrontMatterTypeInputParseResult.Invalid("Input matches multiple Type values. Please type a more specific name or use index.");
        }

        string unknownMessage = allowCreateNewType
            ? "Unknown Type input. Use an index, full name, N, or R."
            : "Unknown Type input. Use an index, full name, or R.";

        return FrontMatterTypeInputParseResult.Invalid(unknownMessage);
    }
}

/// <summary>
/// Represents the parsed intent of a type picker input.
/// </summary>
/// <param name="Action">Parsed action.</param>
/// <param name="SelectedType">Resolved type value when selection succeeded.</param>
/// <param name="Message">Optional user-facing validation message.</param>
public sealed record FrontMatterTypeInputParseResult(
    FrontMatterTypeInputAction Action,
    string? SelectedType,
    string? Message)
{
    public static FrontMatterTypeInputParseResult Select(string selectedType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedType);
        return new FrontMatterTypeInputParseResult(FrontMatterTypeInputAction.SelectExisting, selectedType, null);
    }

    public static FrontMatterTypeInputParseResult Refresh() =>
        new FrontMatterTypeInputParseResult(FrontMatterTypeInputAction.Refresh, null, null);

    public static FrontMatterTypeInputParseResult CreateNewType() =>
        new FrontMatterTypeInputParseResult(FrontMatterTypeInputAction.CreateNew, null, null);

    public static FrontMatterTypeInputParseResult Invalid(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new FrontMatterTypeInputParseResult(FrontMatterTypeInputAction.Invalid, null, message);
    }
}

/// <summary>
/// Enumerates supported actions in type picker parsing.
/// </summary>
public enum FrontMatterTypeInputAction
{
    SelectExisting,
    Refresh,
    CreateNew,
    Invalid
}

