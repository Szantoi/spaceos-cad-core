using System.Globalization;
using System.Text;

namespace CabinetBilder.Core.FrontMatter;

/// <summary>
/// Provides parsing and serialization helpers for front matter text payloads.
/// </summary>
public static class FrontMatterTextService
{
    /// <summary>
    /// Builds a canonical front matter text from replacement values.
    /// </summary>
    /// <param name="replacementValues">A dictionary that contains field keys and values.</param>
    /// <returns>A front matter text block.</returns>
    public static string BuildTemplate(IReadOnlyDictionary<string, string> replacementValues)
    {
        ArgumentNullException.ThrowIfNull(replacementValues);

        StringBuilder builder = new();
        builder.AppendLine("---");
        builder.AppendLine($"{FrontMatterKeys.Type}: {GetValueOrEmpty(replacementValues, FrontMatterKeys.Type)};");
        builder.AppendLine($"{FrontMatterKeys.BlockId}: {GetValueOrEmpty(replacementValues, FrontMatterKeys.BlockId)};");
        builder.AppendLine($"{FrontMatterKeys.Name}: {GetValueOrEmpty(replacementValues, FrontMatterKeys.Name)};");
        builder.AppendLine($"{FrontMatterKeys.Material}: {GetValueOrEmpty(replacementValues, FrontMatterKeys.Material)};");
        builder.AppendLine($"{FrontMatterKeys.Quantity}: {GetValueOrEmpty(replacementValues, FrontMatterKeys.Quantity)};");
        builder.AppendLine($"{FrontMatterKeys.LengthCut}: {GetValueOrEmpty(replacementValues, FrontMatterKeys.LengthCut)};");
        builder.AppendLine($"{FrontMatterKeys.WidthCut}: {GetValueOrEmpty(replacementValues, FrontMatterKeys.WidthCut)};");
        builder.AppendLine("---");
        return builder.ToString();
    }

    /// <summary>
    /// Parses a front matter text payload to key-value entries.
    /// </summary>
    /// <param name="sourceText">A source text that may contain front matter lines.</param>
    /// <returns>A dictionary of parsed entries.</returns>
    public static Dictionary<string, string> ParseEntries(string sourceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        Dictionary<string, string> entries = new(StringComparer.OrdinalIgnoreCase);
        string[] lines = NormalizeLineBreaks(sourceText).Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || string.Equals(line, "---", StringComparison.Ordinal))
            {
                continue;
            }

            int colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
            {
                continue;
            }

            string key = line[..colonIndex].Trim();
            string value = line[(colonIndex + 1)..].Trim().TrimEnd(';').Trim();
            entries[key] = value;
        }

        return entries;
    }

    /// <summary>
    /// Builds table columns from front matter rows in preferred order.
    /// </summary>
    /// <param name="rows">A collection of front matter dictionaries.</param>
    /// <returns>An ordered list of column names.</returns>
    public static List<string> BuildColumns(IEnumerable<IReadOnlyDictionary<string, string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        HashSet<string> allColumns = new(StringComparer.OrdinalIgnoreCase);
        foreach (IReadOnlyDictionary<string, string> row in rows)
        {
            foreach (string key in row.Keys)
            {
                allColumns.Add(key);
            }
        }

        List<string> preferredOrder =
        [
            FrontMatterKeys.Type,
            FrontMatterKeys.Layer,
            FrontMatterKeys.BlockId,
            FrontMatterKeys.Name,
            FrontMatterKeys.Material,
            FrontMatterKeys.Quantity,
            FrontMatterKeys.LengthCut,
            FrontMatterKeys.WidthCut
        ];

        List<string> columns = new();
        foreach (string preferred in preferredOrder)
        {
            if (allColumns.Remove(preferred))
            {
                columns.Add(preferred);
            }
        }

        List<string> remaining = new(allColumns);
        remaining.Sort(StringComparer.OrdinalIgnoreCase);
        columns.AddRange(remaining);

        return columns;
    }

    /// <summary>
    /// Formats a numeric value with invariant culture and fixed precision.
    /// </summary>
    /// <param name="value">A numeric value.</param>
    /// <param name="precision">A fixed decimal precision.</param>
    /// <returns>A formatted number string.</returns>
    public static string FormatNumeric(double value, int precision)
    {
        return value.ToString("F" + precision, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts any value to a stable front matter string representation.
    /// </summary>
    /// <param name="value">An input value.</param>
    /// <param name="precision">A fixed decimal precision for numeric values.</param>
    /// <returns>A serialized value string.</returns>
    public static string FormatDynamicValue(object? value, int precision)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is double doubleValue)
        {
            return FormatNumeric(doubleValue, precision);
        }

        if (value is float floatValue)
        {
            return FormatNumeric(floatValue, precision);
        }

        if (value is decimal decimalValue)
        {
            return decimalValue.ToString("F" + precision, CultureInfo.InvariantCulture);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>
    /// Normalizes line endings to LF format.
    /// </summary>
    /// <param name="value">An input text.</param>
    /// <returns>A normalized text.</returns>
    public static string NormalizeLineBreaks(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static string GetValueOrEmpty(IReadOnlyDictionary<string, string> replacementValues, string key)
    {
        return replacementValues.TryGetValue(key, out string? value) ? value : string.Empty;
    }
}

