namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Canonical metadata field keys for smart AutoCAD objects.
/// All metadata persisted via <c>DrawingObjectMetadataStore</c> must use these keys.
/// </summary>
public static class SmartObjectMetadataKeys
{
    /// <summary>Domain type identifier, e.g. "Asztalos", "FurnĂ©r".</summary>
    public const string ObjectType = "ObjectType";

    /// <summary>Human-readable label assigned to the object.</summary>
    public const string Label = "Label";

    /// <summary>Material name from catalog.</summary>
    public const string Material = "Material";

    /// <summary>Thickness of the part (usually derived from material).</summary>
    public const string Thickness = "Thickness";

    /// <summary>ISO-8601 creation timestamp (UTC).</summary>
    public const string CreatedAt = "CreatedAt";

    /// <summary>Unique identifier for the object across all drawings.</summary>
    public const string Guid = "Guid";

    /// <summary>Returns all canonical key names.</summary>
    public static IReadOnlyList<string> All { get; } =
        [ObjectType, Label, Material, Thickness, CreatedAt, Guid];

    /// <summary>Returns <see langword="true"/> if <paramref name="key"/> is a canonical key.</summary>
    public static bool IsCanonical(string key) =>
        All.Contains(key, StringComparer.OrdinalIgnoreCase);
}

