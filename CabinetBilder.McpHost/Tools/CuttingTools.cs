using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Cutting;
using CabinetBilder.McpHost.Serialization;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost.Tools;

/// <summary>
/// Szabászati MCP toolok (Doorstar 'Szabászati Tételek'). A tényleges nesting/optimalizálás
/// a VPS lapszabász-modul dolga (EPIC-CUTTING-Q3); itt a szabásjegyzéket és a VPS-payloadot állítjuk elő.
/// </summary>
[McpServerToolType]
public static class CuttingTools
{
    [McpServerTool(Name = "skeleton_cutting_plan"), Description("Szabásjegyzék a skeleton BOM-jából: vágási tételek ráhagyással + rostirány, és anyagonkénti tábla-becslés. Webre kész JSON.")]
    public static async Task<McpToolResponse<object>> CuttingPlan(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        [Description("Ráhagyás élenként, mm (sizing allowance). Alapértelmezett 0 (végméretre vág).")] double allowanceMm = 0.0,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");
        if (allowanceMm < 0)
            return Fail("Invalid", "A ráhagyás nem lehet negatív.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        CuttingPlan plan;
        lock (entry.Lock)
        {
            plan = CuttingPlanner.Plan(entry.Skeleton.ComputeBom().ToList(), lookup, allowanceMm);
        }

        return Ok(new
        {
            allowanceMm = plan.AllowanceMm,
            pieces = plan.Pieces.Select(p => new
            {
                name = p.Name,
                materialId = p.MaterialId,
                materialName = p.MaterialName,
                surface = p.Surface,
                finishedLengthMm = p.FinishedLengthMm,
                finishedWidthMm = p.FinishedWidthMm,
                cutLengthMm = p.CutLengthMm,
                cutWidthMm = p.CutWidthMm,
                thicknessMm = p.ThicknessMm,
                quantity = p.Quantity,
                grain = p.Grain,
                edgingId = p.EdgingId
            }).ToArray(),
            byMaterial = plan.ByMaterial.Select(s => new
            {
                materialId = s.MaterialId,
                materialName = s.MaterialName,
                surface = s.Surface,
                pieceCount = s.PieceCount,
                totalCutAreaM2 = s.TotalCutAreaM2,
                boardLengthMm = s.BoardLengthMm,
                boardWidthMm = s.BoardWidthMm,
                estimatedBoards = s.EstimatedBoards
            }).ToArray(),
            note = "A nesting/optimalizálás a VPS lapszabász-modul dolga; a tábla-becslés procurement-célú."
        });
    }

    [McpServerTool(Name = "skeleton_cutting_sheet"), Description("VPS lapszabász-modul bemenő payloadja a skeleton szabásjegyzékéből (draft séma + metadata sha256/generatedAt). SubmitCuttingSheet-ready; a beküldés a VPS-API élesedésekor.")]
    public static async Task<McpToolResponse<object>> CuttingSheet(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        [Description("Ráhagyás élenként, mm. Alapértelmezett 0.")] double allowanceMm = 0.0,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        object payload;
        lock (entry.Lock)
        {
            (payload, _) = BuildCuttingSheetPayload(entry, lookup, allowanceMm);
        }

        return Ok(new
        {
            payload,
            submitted = false,
            note = "A VPS lapszabász BOM-submit API (POST /api/cutting/bom-submit) még nem él (Week 4-5); a payload SubmitCuttingSheet-re kész. Tartós beküldéshez: skeleton_submit_cutting_sheet."
        });
    }

    [McpServerTool(Name = "skeleton_submit_cutting_sheet"), Description("A szabásjegyzék-payload TARTÓS beküldése: a lokál SQLite outboxba kerül (SubmitCuttingSheet művelet); az éles VPS-kapcsolat élesedésekor az OutboxWorker automatikusan kiküldi.")]
    public static async Task<McpToolResponse<object>> SubmitCuttingSheet(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        [Description("Ráhagyás élenként, mm. Alapértelmezett 0.")] double allowanceMm = 0.0,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        object payload;
        string payloadSha;
        lock (entry.Lock)
        {
            (payload, payloadSha) = BuildCuttingSheetPayload(entry, lookup, allowanceMm);
        }

        string payloadJson = JsonSerializer.Serialize(payload);
        var enqueue = await store.EnqueueOutboxAsync(
            new OutboxEntry(Guid.NewGuid(), OutboxOperation.SubmitCuttingSheet, payloadJson, null), ct);

        if (!enqueue.IsSuccess)
        {
            return new McpToolResponse<object>
            {
                IsSuccess = false,
                Status = "Error",
                Errors = enqueue.Errors.ToList(),
                Value = null
            };
        }

        var stats = await store.GetStoreStatsAsync(ct);
        return Ok(new
        {
            outboxEntryId = enqueue.Value.ToString(),
            outboxPending = stats.IsSuccess ? stats.Value.OutboxPending : (int?)null,
            payloadSha256 = payloadSha,
            note = "A tétel az outboxban vár (Pending); az éles VPS BOM-submit API + OutboxWorker élesedésekor automatikusan kiküldésre kerül."
        });
    }

    /// <summary>A VPS lapszabász draft-séma szerinti payload (items + metadata sha256/generatedAt).
    /// A hívó felelőssége az entry.Lock megfogása.</summary>
    private static (object Payload, string Sha256) BuildCuttingSheetPayload(
        SkeletonEntry entry,
        System.Collections.Generic.IReadOnlyDictionary<string, MaterialDto> lookup,
        double allowanceMm)
    {
        var plan = CuttingPlanner.Plan(entry.Skeleton.ComputeBom().ToList(), lookup, allowanceMm);

        var items = plan.Pieces.Select(p => new
        {
            name = p.Name,
            length_mm = p.CutLengthMm,
            width_mm = p.CutWidthMm,
            thickness_mm = p.ThicknessMm,
            materialId = p.MaterialId,
            edgingId = p.EdgingId,
            quantity = p.Quantity,
            grain = p.Grain
        }).ToArray();

        // Kanonikus JSON a tételekről -> sha256 (a VPS metadata sémája szerint)
        string itemsJson = JsonSerializer.Serialize(items);
        string sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(itemsJson))).ToLowerInvariant();

        var payload = new
        {
            skeletonId = entry.Skeleton.Id.Value.ToString(),
            cabinetName = entry.Skeleton.Name,
            items,
            metadata = new
            {
                source = "CabinetBilder",
                sha256,
                allowanceMm = plan.AllowanceMm,
                generatedAt = DateTime.UtcNow
            }
        };
        return (payload, sha256);
    }

    private static McpToolResponse<object> Ok(object value) =>
        new() { IsSuccess = true, Status = "Ok", Value = value };

    private static McpToolResponse<object> Fail(string status, string message) =>
        new() { IsSuccess = false, Status = status, Errors = { message }, Value = null };
}
