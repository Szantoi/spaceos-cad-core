using System;

namespace CabinetBilder.Core.Sync;

/// <summary>
/// Represents a single item in the Bill of Materials (BOM).
/// Used for exporting to production systems.
/// </summary>
public record BomLine(
    string Name,
    double Length,
    double Width,
    double Thickness,
    string MaterialId,
    int Quantity = 1,
    string? EdgingId = null,
    string? Comments = null
);
