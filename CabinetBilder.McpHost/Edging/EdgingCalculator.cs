using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Sync;

namespace CabinetBilder.McpHost.Edging;

/// <summary>Élzárónkénti összesítő: darab, élhossz (fm), egységár (fm), becsült költség.</summary>
public sealed record EdgingSummaryLine(
    string EdgingId,
    string? EdgingName,
    int PieceCount,
    double TotalLengthM,
    decimal? UnitPricePerM,
    decimal? EstimatedCost);

/// <summary>
/// Élzárás-számítás. PoC-szabály: panelenként EGY hosszú él kap élzárót
/// (élhossz = max(Length, Width) * darabszám). A valós élzárás-térkép
/// (mely élek látszanak) későbbi finomítás. Pure static.
/// </summary>
public static class EdgingCalculator
{
    /// <summary>Egy BOM-sor élzáró-hossza méterben (0, ha nincs élzáró).</summary>
    public static double EdgingLengthM(BomLine line)
    {
        if (string.IsNullOrEmpty(line.EdgingId)) return 0;
        return System.Math.Max(line.Length, line.Width) / 1000.0 * line.Quantity;
    }

    public static IReadOnlyList<EdgingSummaryLine> Summarize(
        IEnumerable<BomLine> lines,
        IReadOnlyDictionary<string, MaterialDto> catalog)
    {
        return lines
            .Where(l => !string.IsNullOrEmpty(l.EdgingId))
            .GroupBy(l => l.EdgingId!)
            .Select(g =>
            {
                catalog.TryGetValue(g.Key, out var mat);
                double lengthM = g.Sum(EdgingLengthM);
                decimal? cost = mat?.Price != null ? (decimal)lengthM * mat.Price : null;

                return new EdgingSummaryLine(
                    EdgingId: g.Key,
                    EdgingName: mat?.DisplayName,
                    PieceCount: g.Sum(l => l.Quantity),
                    TotalLengthM: lengthM,
                    UnitPricePerM: mat?.Price,
                    EstimatedCost: cost);
            })
            .OrderBy(s => s.EdgingId)
            .ToList();
    }
}
