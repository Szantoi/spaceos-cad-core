namespace CabinetBilder.SpaceOsModule.Kernel;

/// <summary>One generated panel/primitive, positioned in the cabinet's local coordinate space.</summary>
public sealed record GeometryPrimitive(
    string Name,
    string MaterialId,
    double Width,
    double Height,
    double Thickness,
    double PosX,
    double PosY,
    double PosZ);

public sealed record GeometryResult(
    bool Success,
    IReadOnlyList<GeometryPrimitive> Primitives,
    string? ErrorMessage = null)
{
    public static GeometryResult Ok(IReadOnlyList<GeometryPrimitive> primitives) => new(true, primitives);
    public static GeometryResult Failed(string errorMessage) => new(false, Array.Empty<GeometryPrimitive>(), errorMessage);
}
