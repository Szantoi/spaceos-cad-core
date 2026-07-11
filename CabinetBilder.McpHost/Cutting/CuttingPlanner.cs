using System;
using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;

namespace CabinetBilder.McpHost.Cutting;

/// <summary>Egy szabászati tétel: kész méret + vágási méret (ráhagyással), rostirány, él.</summary>
public sealed record CutPiece(
    string Name,
    string MaterialId,
    string? MaterialName,
    string Surface,
    double FinishedLengthMm,
    double FinishedWidthMm,
    double CutLengthMm,
    double CutWidthMm,
    double ThicknessMm,
    int Quantity,
    string Grain,
    string? EdgingId);

/// <summary>Anyagonkénti szabászati összesítő + tábla-becslés.</summary>
public sealed record CuttingMaterialSummary(
    string MaterialId,
    string? MaterialName,
    string Surface,
    int PieceCount,
    double TotalCutAreaM2,
    double BoardLengthMm,
    double BoardWidthMm,
    int EstimatedBoards);

public sealed record CuttingPlan(
    double AllowanceMm,
    IReadOnlyList<CutPiece> Pieces,
    IReadOnlyList<CuttingMaterialSummary> ByMaterial);

/// <summary>
/// Szabásjegyzék-előállító. Pure static. A ráhagyás (sizing allowance) per-él mm; a
/// vágási méret = kész méret + 2*ráhagyás. A tábla-becslés procurement-célú, nem nesting.
/// </summary>
public static class CuttingPlanner
{
    public static CuttingPlan Plan(
        IEnumerable<BomLine> lines,
        IReadOnlyDictionary<string, MaterialDto> catalog,
        double allowanceMm = 0.0)
    {
        var lineList = lines.ToList();

        var pieces = lineList.Select(b =>
        {
            catalog.TryGetValue(b.MaterialId, out var mat);
            var board = StandardBoards.ForCategory(mat?.Category);
            string surface = mat != null ? MaterialFinish.FromBodyJson(mat.BodyJson) : MaterialFinish.Unknown;

            return new CutPiece(
                Name: b.Name,
                MaterialId: b.MaterialId,
                MaterialName: mat?.DisplayName,
                Surface: surface,
                FinishedLengthMm: b.Length,
                FinishedWidthMm: b.Width,
                CutLengthMm: b.Length + 2 * allowanceMm,
                CutWidthMm: b.Width + 2 * allowanceMm,
                ThicknessMm: b.Thickness,
                Quantity: b.Quantity,
                Grain: board.Grain,
                EdgingId: b.EdgingId);
        }).ToList();

        var byMaterial = pieces
            .GroupBy(p => p.MaterialId)
            .Select(g =>
            {
                catalog.TryGetValue(g.Key, out var mat);
                var board = StandardBoards.ForCategory(mat?.Category);
                double cutArea = g.Sum(p => p.CutLengthMm * p.CutWidthMm / 1_000_000.0 * p.Quantity);
                double boardArea = board.LengthMm * board.WidthMm / 1_000_000.0;
                double usable = boardArea * StandardBoards.UsableFactor;
                int boards = usable > 0 ? (int)Math.Ceiling(cutArea / usable) : 0;

                return new CuttingMaterialSummary(
                    MaterialId: g.Key,
                    MaterialName: mat?.DisplayName,
                    Surface: g.First().Surface,
                    PieceCount: g.Sum(p => p.Quantity),
                    TotalCutAreaM2: cutArea,
                    BoardLengthMm: board.LengthMm,
                    BoardWidthMm: board.WidthMm,
                    EstimatedBoards: boards);
            })
            .OrderBy(s => s.MaterialId)
            .ToList();

        return new CuttingPlan(allowanceMm, pieces, byMaterial);
    }
}
