using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;

namespace CabinetBilder.McpHost.Production;

/// <summary>Egy művelet becsült ráfordítása a szekrényre.</summary>
public sealed record OperationEstimate(
    string OperationId,
    string OperationName,
    string Role,
    int AppliedPieceCount,
    double ProcessHours,   // egységidő × darab (fal-idő, humánerő nélkül)
    double ManHours);      // egységidő × humánerő × darab (a bérköltség alapja)

/// <summary>A munkaidő-becslés eredménye.</summary>
public sealed record LaborEstimate(
    IReadOnlyList<OperationEstimate> Operations,
    double TotalProcessHours,
    double TotalManHours,
    IReadOnlyDictionary<string, double> ManHoursByRole);

/// <summary>
/// Munkaidő-becslés a BOM-ból, a modern műveleti katalógus alapján. A mancsóra
/// (UnitTimeHours × Headcount × darab) az árkalkuláció 2. lépésének (Bérköltség) alapja.
/// Pure static. Az ütemezési DAG (FS/SS) NEM része — az az átfutás-tervezésé.
/// </summary>
public static class LaborEstimator
{
    public static LaborEstimate Estimate(
        IEnumerable<BomLine> bom,
        IReadOnlyDictionary<string, MaterialDto> catalog,
        IReadOnlyList<Operation> operations)
    {
        var lines = bom.ToList();

        // Alkatrész-kategória + felület kikeresése a katalógusból
        (string? cat, string surface) Meta(BomLine b)
        {
            catalog.TryGetValue(b.MaterialId, out var mat);
            return (mat?.Category, mat != null ? MaterialFinish.FromBodyJson(mat.BodyJson) : MaterialFinish.Unknown);
        }

        var estimates = new List<OperationEstimate>();
        foreach (var op in operations)
        {
            int pieces;
            if (op.PerCabinet)
            {
                pieces = 1;
            }
            else
            {
                pieces = lines
                    .Where(b =>
                    {
                        var (cat, surface) = Meta(b);
                        if (op.MatchCategory != null && !string.Equals(cat, op.MatchCategory, System.StringComparison.OrdinalIgnoreCase)) return false;
                        if (op.MatchSurface != null && !string.Equals(surface, op.MatchSurface, System.StringComparison.OrdinalIgnoreCase)) return false;
                        return true;
                    })
                    .Sum(b => b.Quantity);
            }

            if (pieces == 0) continue;

            double processHours = op.UnitTimeHours * pieces;
            double manHours = processHours * op.Headcount;
            estimates.Add(new OperationEstimate(op.Id, op.Name, op.Role, pieces, processHours, manHours));
        }

        var byRole = estimates
            .GroupBy(e => e.Role)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.ManHours));

        return new LaborEstimate(
            estimates,
            estimates.Sum(e => e.ProcessHours),
            estimates.Sum(e => e.ManHours),
            byRole);
    }
}
