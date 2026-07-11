namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Defines schema marker metadata for smart AutoCAD objects.
/// </summary>
public static class SmartObjectSchema
{
    public const string MetadataDictionaryName = "CB_SMART_OBJECT";
    public const string SchemaMarkerKey = "SchemaID";
    public const string DefaultSchemaId = "Butor_V1";

    /// <summary>
    /// Determines whether a schema value matches the expected schema identifier.
    /// </summary>
    /// <param name="schemaId">A stored schema identifier.</param>
    /// <param name="expectedSchemaId">An optional expected schema identifier.</param>
    /// <returns><see langword="true" /> if the schema matches; otherwise, <see langword="false" />.</returns>
    public static bool IsSchemaMatch(string? schemaId, string? expectedSchemaId = null)
    {
        if (string.IsNullOrWhiteSpace(schemaId))
        {
            return false;
        }

        string effectiveExpected = string.IsNullOrWhiteSpace(expectedSchemaId)
            ? DefaultSchemaId
            : expectedSchemaId;

        return string.Equals(schemaId, effectiveExpected, StringComparison.OrdinalIgnoreCase);
    }
}

