using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost.Docs;

/// <summary>Egy felhasznált anyag a műszaki leírásban.</summary>
public sealed record UsedMaterial(string Role, string MaterialId, string? MaterialName, string Surface);

/// <summary>A műszaki leírás strukturált szekciói (tankönyv §2.1) + kész markdown.</summary>
public sealed record TechnicalDescription(
    string Name,
    string OverallSizeMm,          // Szélesség × Magasság × Mélység (korpusz-konvenció)
    IReadOnlyList<UsedMaterial> Materials,
    string StructuralDescription,
    string SurfaceTreatment,
    IReadOnlyList<string> DesignIntents,
    string Markdown);

/// <summary>
/// Műszaki leírás generátor a tankönyvi séma szerint (faipari_muszaki_dokumentacio_rag.md §2.1):
/// név, befoglaló méret, felhasznált anyagok (segédanyag nélkül), szerkezeti felépítés,
/// felületkezelés — kiegészítve a gyűjtött tervezői szándékokkal (REQ-008). Pure static.
/// </summary>
public static class TechnicalDescriptionGenerator
{
    public static TechnicalDescription Generate(
        Skeleton skeleton,
        IReadOnlyList<DesignIntent> intents,
        IReadOnlyDictionary<string, MaterialDto> catalog)
    {
        var p = skeleton.Parameters.ToDictionary(x => x.Key, x => x.Value);
        double w = ToDouble(p, "Width"), h = ToDouble(p, "Height"), d = ToDouble(p, "Depth");
        double t = ToDouble(p, "Thickness"), bo = ToDouble(p, "BackOffset");
        string carcassId = ToStr(p, "CarcassMaterialId");
        string backId = ToStr(p, "BackMaterialId");
        string edgingId = ToStr(p, "EdgingId");

        // Befoglaló méret — korpusz-konvenció: frontfelület (Szélesség × Magasság), majd Mélység
        string overall = $"{Fmt(w)} × {Fmt(h)} × {Fmt(d)} mm (Szélesség × Magasság × Mélység)";

        // Felhasznált anyagok (segédanyag/ragasztó nélkül — tankönyv)
        var materials = new List<UsedMaterial>();
        AddMaterial(materials, "Korpusz", carcassId, catalog);
        AddMaterial(materials, "Hátlap", backId, catalog);
        if (!string.IsNullOrEmpty(edgingId)) AddMaterial(materials, "Élzáró", edgingId, catalog);

        // Szerkezeti felépítés — a Rebuild-logika leírása
        string carcassName = materials.FirstOrDefault(m => m.Role == "Korpusz")?.MaterialName ?? carcassId;
        string backName = materials.FirstOrDefault(m => m.Role == "Hátlap")?.MaterialName ?? backId;
        string edgingName = materials.FirstOrDefault(m => m.Role == "Élzáró")?.MaterialName ?? edgingId;
        var sb = new StringBuilder();
        sb.Append($"Lapraszerelt korpuszbútor, {Fmt(t)} mm vastag lapanyagból ({carcassName}). ");
        sb.Append("Az oldalak átmenő kialakításúak, a fedél- és fenéklap az oldalak közé kerül. ");
        sb.Append($"A hátlap {backName}, {Fmt(bo)} mm-es beütéssel a korpusz síkja mögött. ");
        if (!string.IsNullOrEmpty(edgingId))
            sb.Append($"A korpuszpanelek látszó élei élzártak ({edgingName}).");
        string structural = sb.ToString().TrimEnd();

        // Felületkezelés — a felület-attribútumból
        string surfaceTreatment = DeriveSurfaceTreatment(materials);

        var intentTexts = intents.Select(i =>
            i.ParameterKey != null ? $"{i.Intent} (paraméter: {i.ParameterKey})" : i.Intent).ToList();

        string markdown = BuildMarkdown(skeleton.Name, overall, materials, structural, surfaceTreatment, intentTexts);

        return new TechnicalDescription(
            skeleton.Name, overall, materials, structural, surfaceTreatment, intentTexts, markdown);
    }

    private static void AddMaterial(List<UsedMaterial> list, string role, string materialId,
        IReadOnlyDictionary<string, MaterialDto> catalog)
    {
        if (string.IsNullOrEmpty(materialId)) return;
        catalog.TryGetValue(materialId, out var mat);
        string surface = mat != null ? MaterialFinish.FromBodyJson(mat.BodyJson) : MaterialFinish.Unknown;
        list.Add(new UsedMaterial(role, materialId, mat?.DisplayName, surface));
    }

    private static string DeriveSurfaceTreatment(IReadOnlyList<UsedMaterial> materials)
    {
        var surfaces = materials.Where(m => m.Role != "Élzáró").Select(m => m.Surface).Distinct().ToList();
        var parts = new List<string>();
        foreach (var s in surfaces)
        {
            switch (s)
            {
                case "laminált": parts.Add("a laminált felületek további felületkezelést nem igényelnek"); break;
                case "festett": parts.Add("festett felület (a festés a gyártás része)"); break;
                case "fóliás": parts.Add("fóliázott felület (a fóliázás a gyártás része)"); break;
                case "hdf hátlap": parts.Add("a HDF hátlap gyárilag felületkezelt"); break;
                default: parts.Add($"felületkezelés: {s}"); break;
            }
        }
        if (parts.Count == 0) return "Nincs felületkezelési igény.";
        string joined = string.Join("; ", parts);
        return char.ToUpper(joined[0]) + joined.Substring(1) + ".";
    }

    private static string BuildMarkdown(string name, string overall, IReadOnlyList<UsedMaterial> materials,
        string structural, string surfaceTreatment, IReadOnlyList<string> intents)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Műszaki leírás — {name}");
        sb.AppendLine();
        sb.AppendLine($"**Befoglaló méret:** {overall}");
        sb.AppendLine();
        sb.AppendLine("## Felhasznált anyagok");
        foreach (var m in materials)
            sb.AppendLine($"- **{m.Role}:** {m.MaterialName ?? m.MaterialId} ({m.MaterialId}, felület: {m.Surface})");
        sb.AppendLine();
        sb.AppendLine("## Szerkezeti felépítés");
        sb.AppendLine(structural);
        sb.AppendLine();
        sb.AppendLine("## Felületkezelés");
        sb.AppendLine(surfaceTreatment);
        if (intents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Tervezői szándékok");
            foreach (var i in intents) sb.AppendLine($"- {i}");
        }
        return sb.ToString();
    }

    private static double ToDouble(IReadOnlyDictionary<string, object> p, string key) =>
        p.TryGetValue(key, out var v) ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : 0;

    private static string ToStr(IReadOnlyDictionary<string, object> p, string key) =>
        p.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

    private static string Fmt(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
}
