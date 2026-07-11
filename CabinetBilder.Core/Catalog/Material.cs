namespace CabinetBilder.Core.Catalog;

/// <summary>
/// Domain model for a Material in the catalog.
/// Clean aggregate root - no dependencies on persistence frameworks.
/// </summary>
public sealed class Material
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public double Thickness { get; private set; }
    public double Density { get; private set; } // kg/m3

    // EF Core constructor or simple init
    private Material(Guid id, string code, string name, double thickness, double density)
    {
        Id = id;
        Code = code;
        Name = name;
        Thickness = thickness;
        Density = density;
    }

    /// <summary>
    /// Factory method for creating a new material.
    /// </summary>
    public static Material Create(string code, string name, double thickness, double density)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (thickness <= 0) throw new ArgumentException("Thickness must be positive.");

        return new Material(Guid.NewGuid(), code, name, thickness, density);
    }

    /// <summary>
    /// Reconstitutes a material from storage.
    /// </summary>
    public static Material Reconstitute(Guid id, string code, string name, double thickness, double density)
    {
        return new Material(id, code, name, thickness, density);
    }
}

