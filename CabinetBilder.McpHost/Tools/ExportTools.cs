using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Export;
using CabinetBilder.McpHost.Serialization;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost.Tools;

/// <summary>
/// Projekt-export MCP tool: a Doorstar dokumentum-négyest a valós PQ-lánc
/// céloszlop-sémájával a projekt-mappába írja (Power Query-fogyasztható CSV-k + md + json).
/// </summary>
[McpServerToolType]
public static class ExportTools
{
    [McpServerTool(Name = "skeleton_export_project"), Description("A Doorstar dokumentum-négyes kiírása a megadott mappába a valós PQ-séma szerint: Szabaszat.csv, Mennyisegek.csv, Kalkulacio.csv, Muszaki-Leiras.md, export.json. Visszaadja az írt fájlok útvonalát.")]
    public static async Task<McpToolResponse<object>> ExportProject(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        [Description("Célmappa (létrejön, ha nincs).")] string outputDir,
        [Description("DSMR projektazonosító (pl. 26144).")] string dsmr,
        [Description("Ráhagyás élenként, mm.")] double allowanceMm = 0,
        [Description("Tapasztalati munkaóra a kalkulációhoz.")] double laborHours = 0,
        [Description("Bruttó órabér, Ft/óra.")] double hourlyRate = 5000,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");
        if (string.IsNullOrWhiteSpace(outputDir))
            return Fail("Invalid", "A célmappa (outputDir) kötelező.");
        if (string.IsNullOrWhiteSpace(dsmr))
            return Fail("Invalid", "A DSMR kötelező.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);
        var opt = new ExportOptions(dsmr, allowanceMm, laborHours, hourlyRate);

        System.Collections.Generic.IReadOnlyDictionary<string, string> files;
        lock (entry.Lock)
        {
            files = ProjectExporter.BuildFiles(entry.Skeleton, entry.Intents, lookup, opt);
        }

        try
        {
            var written = ProjectExporter.WriteAll(outputDir, files);
            return Ok(new
            {
                outputDir,
                dsmr,
                files = written,
                fileCount = written.Count,
                note = "A CSV-k a valós Doorstar PQ-séma oszlopfejléceit követik (docs/knowledge/doorstar_power_query_semak.md) — a Power Query-lánc be tudja húzni őket."
            });
        }
        catch (Exception ex)
        {
            return Fail("Error", $"Írási hiba: {ex.Message}");
        }
    }

    private static McpToolResponse<object> Ok(object value) =>
        new() { IsSuccess = true, Status = "Ok", Value = value };

    private static McpToolResponse<object> Fail(string status, string message) =>
        new() { IsSuccess = false, Status = status, Errors = { message }, Value = null };
}
