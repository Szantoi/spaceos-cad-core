using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Docs;
using CabinetBilder.McpHost.Serialization;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost.Tools;

/// <summary>
/// Dokumentum-generáló MCP toolok (Doorstar dokumentum-négyes). A Műszaki leírás a
/// tankönyvi séma (§2.1) szerint épül, a record_design_intent-tel gyűjtött
/// tervezői szándékokkal kiegészítve.
/// </summary>
[McpServerToolType]
public static class DocsTools
{
    [McpServerTool(Name = "skeleton_technical_description"), Description("Műszaki leírás a tankönyvi séma szerint: név, befoglaló méret, felhasznált anyagok, szerkezeti felépítés, felületkezelés + a gyűjtött tervezői szándékok. Strukturált JSON + kész magyar markdown.")]
    public static async Task<McpToolResponse<object>> TechnicalDescription(
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

        TechnicalDescription doc;
        lock (entry.Lock)
        {
            doc = TechnicalDescriptionGenerator.Generate(entry.Skeleton, entry.Intents, lookup);
        }

        return Ok(new
        {
            name = doc.Name,
            overallSizeMm = doc.OverallSizeMm,
            materials = doc.Materials.Select(m => new
            {
                role = m.Role,
                materialId = m.MaterialId,
                materialName = m.MaterialName,
                surface = m.Surface
            }).ToArray(),
            structuralDescription = doc.StructuralDescription,
            surfaceTreatment = doc.SurfaceTreatment,
            designIntents = doc.DesignIntents,
            markdown = doc.Markdown
        });
    }

    private static McpToolResponse<object> Ok(object value) =>
        new() { IsSuccess = true, Status = "Ok", Value = value };

    private static McpToolResponse<object> Fail(string status, string message) =>
        new() { IsSuccess = false, Status = status, Errors = { message }, Value = null };
}
