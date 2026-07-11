namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Provides schema-aware include/exclude decisions for smart object filtering.
/// </summary>
public static class SmartObjectSchemaFilter
{
    /// <summary>
    /// Determines whether an object should be included based on schema marker presence and schema match.
    /// </summary>
    /// <param name="hasSchemaMarker">Indicates whether the object has a schema marker.</param>
    /// <param name="schemaId">The actual schema identifier when marker exists.</param>
    /// <param name="expectedSchemaId">An optional expected schema identifier. Defaults to <see cref="SmartObjectSchema.DefaultSchemaId"/> behavior.</param>
    /// <returns><see langword="true"/> when object should be included; otherwise <see langword="false"/>.</returns>
    public static bool ShouldInclude(bool hasSchemaMarker, string? schemaId, string? expectedSchemaId = null)
    {
        if (!hasSchemaMarker)
        {
            return true;
        }

        return SmartObjectSchema.IsSchemaMatch(schemaId, expectedSchemaId);
    }
}

