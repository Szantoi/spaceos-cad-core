using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Bom;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Edging;
using CabinetBilder.McpHost.Serialization;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost.Tools;

/// <summary>
/// Parametrikus skeleton-tervezés MCP toolok (REQ-006: paraméter → Rebuild → komponensek,
/// NEM direkt geometria). A SkeletonRegistry DI-ből injektálódik a tool-metódusokba.
/// </summary>
[McpServerToolType]
public static class SkeletonTools
{
    [McpServerTool(Name = "skeleton_create"), Description("Létrehoz egy új paraméteres szekrény-skeletont alapértelmezett paraméterekkel, és Rebuild-eli.")]
    public static McpToolResponse<object> CreateSkeleton(
        SkeletonRegistry registry,
        [Description("A szekrény megnevezése.")] string name = "New Cabinet",
        [Description("Opcionális egyedi azonosító (UUID). Ha nincs megadva, a host generál.")] string? skeletonId = null,
        [Description("A létrehozás tervezői szándéka (pl. 'Konyhai alsó fiókos elem').")] string? intent = null)
    {
        Guid? id = null;
        if (!string.IsNullOrWhiteSpace(skeletonId))
        {
            if (!Guid.TryParse(skeletonId, out var parsed))
                return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
            id = parsed;
        }

        var entry = registry.Create(id, name, intent);
        return Ok(SkeletonRegistry.ToDto(entry));
    }

    [McpServerTool(Name = "skeleton_apply_parameter"), Description("Módosít egy paramétert a megadott skeletonon, majd lefuttatja a Rebuild-et. Visszaadja a frissített állapotot.")]
    public static McpToolResponse<object> ApplyParameter(
        SkeletonRegistry registry,
        [Description("A módosítandó skeleton egyedi azonosítója (UUID).")] string skeletonId,
        [Description("A paraméter kulcsa (Width, Height, Depth, Thickness, BackOffset).")] string key,
        [Description("Az új érték (a paraméter típusához igazodva).")] double value,
        [Description("A módosítás szakmai indoklása (tervezői szándék).")] string? intent = null)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        lock (entry.Lock)
        {
            var result = entry.Skeleton.ApplyParameter(key, value);
            if (result.IsFailure)
                return result.ToMcpResponse();

            if (!string.IsNullOrWhiteSpace(intent))
                entry.Intents.Add(new DesignIntent(DateTime.UtcNow, intent!, key));

            return Ok(SkeletonRegistry.ToDto(entry));
        }
    }

    [McpServerTool(Name = "skeleton_set_material"), Description("Beállítja a skeleton anyagát (carcass = korpusz, back = hátlap, edging = élzáró) a katalógus egy érvényes anyagkódjára, majd Rebuild-el.")]
    public static async Task<McpToolResponse<object>> SetMaterial(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        [Description("A cél: 'carcass' (oldalak/fedél/fenék) vagy 'back' (hátlap).")] string target,
        [Description("A katalógus anyagkódja (lásd list_materials, pl. LAM18_W1000).")] string materialCode,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        string key = target?.Trim().ToLowerInvariant() switch
        {
            "carcass" => "CarcassMaterialId",
            "back" => "BackMaterialId",
            "edging" => "EdgingId",
            _ => ""
        };
        if (key.Length == 0)
            return Fail("Invalid", $"Ismeretlen target: '{target}'. Érvényes: 'carcass', 'back' vagy 'edging'.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        if (materials.All(m => m.MaterialCode != materialCode))
            return Fail("NotFound", $"Nincs ilyen anyagkód a katalógusban: '{materialCode}'. Lásd list_materials.");

        lock (entry.Lock)
        {
            var result = entry.Skeleton.ApplyParameter(key, materialCode);
            if (result.IsFailure)
                return result.ToMcpResponse();
            return Ok(SkeletonRegistry.ToDto(entry));
        }
    }

    [McpServerTool(Name = "skeleton_compute_bom"), Description("Kiszámítja és lapos listaként visszaadja a skeleton BOM-ját, a katalógusból dúsított anyaginfóval (webre kész JSON).")]
    public static async Task<McpToolResponse<object>> ComputeBom(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        object[] bom;
        lock (entry.Lock)
        {
            bom = entry.Skeleton.ComputeBom().Select(b =>
            {
                lookup.TryGetValue(b.MaterialId, out var mat);
                return new
                {
                    name = b.Name,
                    length = b.Length,
                    width = b.Width,
                    thickness = b.Thickness,
                    materialId = b.MaterialId,
                    materialName = mat?.DisplayName,
                    materialCategory = mat?.Category,
                    surface = mat != null ? MaterialFinish.FromBodyJson(mat.BodyJson) : MaterialFinish.Unknown,
                    unitPrice = mat?.Price,
                    quantity = b.Quantity,
                    edgingId = b.EdgingId,
                    comments = b.Comments
                };
            }).ToArray();
        }
        return Ok(bom);
    }

    [McpServerTool(Name = "skeleton_material_summary"), Description("Anyagszükséglet-összesítő a Doorstar 'Menyíségek' séma szerint: anyagonként/felületenként darab, terület (m²), egységár, becsült költség.")]
    public static async Task<McpToolResponse<object>> MaterialSummary(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        object[] summary;
        object[] edgingSummary;
        double totalArea;
        decimal? totalCost;
        lock (entry.Lock)
        {
            var lines = entry.Skeleton.ComputeBom().ToList();
            var agg = BomAggregator.Summarize(lines, lookup);
            var edging = EdgingCalculator.Summarize(lines, lookup);
            totalArea = agg.Sum(s => s.TotalAreaM2);
            bool anyCost = agg.Any(s => s.EstimatedCost != null) || edging.Any(e => e.EstimatedCost != null);
            totalCost = anyCost
                ? agg.Sum(s => s.EstimatedCost ?? 0m) + edging.Sum(e => e.EstimatedCost ?? 0m)
                : (decimal?)null;
            summary = agg.Select(s => new
            {
                materialId = s.MaterialId,
                materialName = s.MaterialName,
                category = s.Category,
                surface = s.Surface,
                pieceCount = s.PieceCount,
                totalAreaM2 = s.TotalAreaM2,
                unitPrice = s.UnitPrice,
                estimatedCost = s.EstimatedCost
            }).ToArray();
            edgingSummary = edging.Select(e => new
            {
                edgingId = e.EdgingId,
                edgingName = e.EdgingName,
                pieceCount = e.PieceCount,
                totalLengthM = e.TotalLengthM,
                unitPricePerM = e.UnitPricePerM,
                estimatedCost = e.EstimatedCost
            }).ToArray();
        }

        return Ok(new { lines = summary, edging = edgingSummary, totalAreaM2 = totalArea, totalEstimatedCost = totalCost });
    }

    [McpServerTool(Name = "record_design_intent"), Description("Utólag rögzít egy tervezői szándékot / indoklást egy skeletonhoz (REQ-008).")]
    public static McpToolResponse<object> RecordDesignIntent(
        SkeletonRegistry registry,
        [Description("A cél skeleton azonosítója (UUID).")] string skeletonId,
        [Description("A tervezői döntés / szándék leírása.")] string intent,
        [Description("Opcionális: ha a szándék egy konkrét paraméterhez kapcsolódik.")] string? parameterKey = null)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        lock (entry.Lock)
        {
            entry.Intents.Add(new DesignIntent(DateTime.UtcNow, intent, parameterKey));
            return Ok(SkeletonRegistry.ToDto(entry));
        }
    }

    private static McpToolResponse<object> Ok(object value) =>
        new() { IsSuccess = true, Status = "Ok", Value = value };

    private static McpToolResponse<object> Fail(string status, string message) =>
        new() { IsSuccess = false, Status = status, Errors = { message }, Value = null };
}
