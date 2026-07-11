namespace CabinetBilder.Core.FrontMatter;

/// <summary>
/// Builds canonical front matter value dictionaries from plain inputs.
/// </summary>
public static class FrontMatterValueBuilder
{
    /// <summary>
    /// Creates a front matter key-value payload from block and dynamic property values.
    /// </summary>
    /// <param name="selectedType">Selected type value.</param>
    /// <param name="blockId">Block handle identifier.</param>
    /// <param name="blockName">Block name.</param>
    /// <param name="dynamicProperties">Dynamic property values keyed by property name.</param>
    /// <returns>A dictionary that can be serialized by <see cref="FrontMatterTextService"/>.</returns>
    public static Dictionary<string, string> BuildValues(
        string selectedType,
        string blockId,
        string blockName,
        IReadOnlyDictionary<string, string> dynamicProperties)
    {
        if (string.IsNullOrWhiteSpace(selectedType))
        {
            throw new ArgumentException("Type value cannot be empty.", nameof(selectedType));
        }

        if (string.IsNullOrWhiteSpace(blockId))
        {
            throw new ArgumentException("Block id cannot be empty.", nameof(blockId));
        }

        ArgumentNullException.ThrowIfNull(blockName);
        ArgumentNullException.ThrowIfNull(dynamicProperties);

        Dictionary<string, string> replacementValues = new(StringComparer.OrdinalIgnoreCase)
        {
            [FrontMatterKeys.Type] = selectedType,
            [FrontMatterKeys.BlockId] = blockId,
            [FrontMatterKeys.Name] = blockName,
            [FrontMatterKeys.Material] = string.Empty,
            [FrontMatterKeys.Quantity] = string.Empty,
            [FrontMatterKeys.LengthCut] = GetPreferredValue(dynamicProperties, "Length", "Hosszusag", "HosszĂşsĂˇg", "Tavolsag", "TĂˇvolsĂˇg"),
            [FrontMatterKeys.WidthCut] = GetPreferredValue(dynamicProperties, "Width", "Szelesseg", "SzĂ©lessĂ©g")
        };

        foreach ((string key, string value) in dynamicProperties)
        {
            replacementValues[key] = value;
        }

        return replacementValues;
    }

    private static string GetPreferredValue(IReadOnlyDictionary<string, string> values, params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (values.TryGetValue(candidate, out string? value))
            {
                return value;
            }

            foreach ((string key, string candidateValue) in values)
            {
                if (string.Equals(key, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return candidateValue;
                }
            }
        }

        return string.Empty;
    }
}

