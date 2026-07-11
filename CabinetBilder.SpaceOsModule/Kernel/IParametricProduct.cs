namespace CabinetBilder.SpaceOsModule.Kernel;

/// <summary>
/// Local stand-in for the SpaceOS Kernel's driver-module contract, mirrored from
/// joinerytech-platform docs/knowledge/architecture/ADR_CATALOGUE.md (ADR-002,
/// "Modular Monolith — Kernel IParametricProduct interface"). The real spaceos-kernel
/// repo (src/spaceos-kernel in joinerytech-platform) is an empty gitlink as of
/// 2026-07-11 — swap this for the real Kernel package reference once it's available.
/// </summary>
public interface IParametricProduct
{
    Guid ProductId { get; }
    Guid TenantId { get; }
    Dictionary<string, object> Parameters { get; }
    Task<GeometryResult> GenerateGeometry(IGeometryEngine engine);
    Task<ValidationResult> ValidateParameters();
}
