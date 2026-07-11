namespace CabinetBilder.SpaceOsModule.Kernel;

/// <summary>
/// Placeholder for the Kernel-supplied geometry engine passed into
/// <see cref="IParametricProduct.GenerateGeometry"/>. CabinetBilder's Skeleton
/// already computes its own geometry (panels/positions) via Rebuild(), so
/// <see cref="CabinetSkeletonProduct"/> does not currently call into this —
/// it's here only so the method signature matches the documented Kernel contract.
/// </summary>
public interface IGeometryEngine
{
}
