using CabinetBilder.Core.Machining;

namespace CabinetBilder.Core.Skeletons;

/// <summary>
/// Represents a physical component derived from a Skeleton (e.g., Panel, Hardware).
/// </summary>
public record SkeletonComponent
{
    public string Name { get; init; } = string.Empty;
    public string MaterialId { get; init; } = string.Empty;
    public double Width { get; init; }
    public double Height { get; init; }
    public double Thickness { get; init; }
    
    // Position relative to Skeleton origin
    public double PosX { get; init; }
    public double PosY { get; init; }
    public double PosZ { get; init; }

    // Orientation: Normal vector of the panel surface (direction of Thickness)
    public double NormalX { get; init; } = 0;
    public double NormalY { get; init; } = 0;
    public double NormalZ { get; init; } = 1;

    // Optional: Direction of the 'Width' dimension (X-axis of the panel)
    public double DirX { get; init; } = 1;
    public double DirY { get; init; } = 0;
    public double DirZ { get; init; } = 0;

    public List<MachiningOperation> Operations { get; init; } = new();

    public Dictionary<string, string> Metadata { get; init; } = new();
}
