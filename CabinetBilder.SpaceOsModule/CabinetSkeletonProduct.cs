using CabinetBilder.Core.Common;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.SpaceOsModule.Kernel;

namespace CabinetBilder.SpaceOsModule;

/// <summary>
/// Adapts a CabinetBilder <see cref="Skeleton"/> to the SpaceOS Kernel's
/// <see cref="IParametricProduct"/> contract, so the platform's driver-module
/// layer (planned "Modules.Cabinet", see joinerytech-platform
/// docs/knowledge/architecture/ECOSYSTEM_MODULE_ARCHITECTURE.md) can treat a
/// cabinet the same way it treats any other parametric product.
/// </summary>
public sealed class CabinetSkeletonProduct : IParametricProduct
{
    private readonly Skeleton _skeleton;

    public Guid ProductId => _skeleton.Id.Value;
    public Guid TenantId { get; }

    public Dictionary<string, object> Parameters =>
        _skeleton.Parameters.ToDictionary(p => p.Key, p => p.Value);

    public CabinetSkeletonProduct(Skeleton skeleton, Guid tenantId)
    {
        _skeleton = skeleton ?? throw new ArgumentNullException(nameof(skeleton));
        TenantId = tenantId;
    }

    /// <summary>
    /// The Skeleton computes its own geometry (see Skeleton.Rebuild()), so the
    /// Kernel-supplied engine isn't needed here — it's accepted only to satisfy
    /// the interface signature.
    /// </summary>
    public Task<GeometryResult> GenerateGeometry(IGeometryEngine engine)
    {
        _skeleton.Rebuild();
        var primitives = _skeleton.Components
            .Select(c => new GeometryPrimitive(c.Name, c.MaterialId, c.Width, c.Height, c.Thickness, c.PosX, c.PosY, c.PosZ))
            .ToList();
        return Task.FromResult(GeometryResult.Ok(primitives));
    }

    public Task<ValidationResult> ValidateParameters()
    {
        var errors = new List<string>();

        double width = GetDouble("Width");
        double height = GetDouble("Height");
        double depth = GetDouble("Depth");
        double thickness = GetDouble("Thickness");

        if (width <= 0) errors.Add("Width must be positive.");
        if (height <= 0) errors.Add("Height must be positive.");
        if (depth <= 0) errors.Add("Depth must be positive.");
        if (thickness <= 0) errors.Add("Thickness must be positive.");
        if (thickness > 0 && width > 0 && thickness * 2 >= width)
            errors.Add("Thickness is too large for Width — the side panels would overlap.");

        return Task.FromResult(errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors));
    }

    /// <summary>Mutates the underlying Skeleton — not part of IParametricProduct, but
    /// needed by the eventual CQRS command handler (e.g. SetCabinetParameter) that
    /// wraps this adapter, mirroring the Command-per-use-case pattern used by the
    /// platform's CRM/EHS modules.</summary>
    public Result ApplyParameter(string key, object value) => _skeleton.ApplyParameter(key, value);

    private double GetDouble(string key) =>
        Convert.ToDouble(_skeleton.Parameters.First(p => p.Key == key).Value);
}
