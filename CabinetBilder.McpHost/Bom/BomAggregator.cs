using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;

namespace CabinetBilder.McpHost.Bom;

/// <summary>
/// Egy anyagszükséglet-összesítő sor (Doorstar 'Menyíségek'): anyagonként/felületenként
/// darabszám, terület (m²), egységár és becsült költség.
/// </summary>
public sealed record MaterialSummaryLine(
    string MaterialId,
    string? MaterialName,
    string? Category,
    string Surface,
    int PieceCount,
    double TotalAreaM2,
    decimal? UnitPrice,
    decimal? EstimatedCost);

/// <summary>
/// A BOM-sorokat anyagonként összesíti. Pure static — könnyen tesztelhető.
/// </summary>
public static class BomAggregator
{
    /// <summary>Egy panel területe m²-ben (él/hulladék nélkül, PoC).</summary>
    public static double AreaM2(BomLine line) => line.Length * line.Width / 1_000_000.0 * line.Quantity;

    public static IReadOnlyList<MaterialSummaryLine> Summarize(
        IEnumerable<BomLine> lines,
        IReadOnlyDictionary<string, MaterialDto> catalog)
    {
        return lines
            .GroupBy(l => l.MaterialId)
            .Select(g =>
            {
                catalog.TryGetValue(g.Key, out var mat);
                double area = g.Sum(AreaM2);
                int pieces = g.Sum(l => l.Quantity);
                string surface = mat != null ? MaterialFinish.FromBodyJson(mat.BodyJson) : MaterialFinish.Unknown;
                decimal? cost = mat?.Price != null ? (decimal)area * mat.Price : null;

                return new MaterialSummaryLine(
                    MaterialId: g.Key,
                    MaterialName: mat?.DisplayName,
                    Category: mat?.Category,
                    Surface: surface,
                    PieceCount: pieces,
                    TotalAreaM2: area,
                    UnitPrice: mat?.Price,
                    EstimatedCost: cost);
            })
            .OrderBy(s => s.MaterialId)
            .ToList();
    }
}
