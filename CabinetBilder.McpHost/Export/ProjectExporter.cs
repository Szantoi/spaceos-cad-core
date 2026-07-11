using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Bom;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Costing;
using CabinetBilder.McpHost.Cutting;
using CabinetBilder.McpHost.Docs;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost.Export;

/// <summary>Projekt-export beállításai (DSMR + kalkulációs input).</summary>
public sealed record ExportOptions(
    string Dsmr,
    double AllowanceMm = 0,
    double LaborHours = 0,
    double HourlyRate = 5000,
    double OtherCosts = 0,
    double SocialContributionPct = 13,
    double OverheadPct = 20,
    double ProfitPct = 15,
    double VatPct = 27);

/// <summary>
/// A Doorstar dokumentum-négyes exportja a valós PQ-lánc céloszlop-sémájával
/// (docs/knowledge/doorstar_power_query_semak.md). A BuildFiles pure függvény
/// (név → tartalom); a WriteAll az IO. A CSV-k magyar Excel-konvencióval:
/// pontosvessző elválasztó, tizedesvessző, UTF-8 BOM, CRLF.
/// </summary>
public static class ProjectExporter
{
    private static readonly CultureInfo Hu = CultureInfo.GetCultureInfo("hu-HU");

    /// <summary>Az alkatrész-nevek magyarítása a Doorstar "Alkatrész Megnevezése" oszlophoz.</summary>
    private static readonly Dictionary<string, string> PartNameHu = new()
    {
        ["Side Left"] = "Bal oldal",
        ["Side Right"] = "Jobb oldal",
        ["Bottom"] = "Fenék",
        ["Top"] = "Fedél",
        ["Back"] = "Hátlap",
    };

    public static string PartHu(string name) => PartNameHu.TryGetValue(name, out var hu) ? hu : name;

    /// <summary>Névtől független fájltartalom-építés. A hívó felelőssége a lock (ha szükséges).</summary>
    public static IReadOnlyDictionary<string, string> BuildFiles(
        Skeleton skeleton,
        IReadOnlyList<DesignIntent> intents,
        IReadOnlyDictionary<string, MaterialDto> catalog,
        ExportOptions opt)
    {
        var bom = skeleton.ComputeBom().ToList();
        var plan = CuttingPlanner.Plan(bom, catalog, opt.AllowanceMm);

        var files = new Dictionary<string, string>
        {
            ["Szabaszat.csv"] = BuildSzabaszatCsv(plan, catalog, opt.Dsmr),
            ["Mennyisegek.csv"] = BuildMennyisegekCsv(bom, catalog, opt.Dsmr),
            ["Kalkulacio.csv"] = BuildKalkulacioCsv(bom, catalog, opt),
            ["Muszaki-Leiras.md"] = TechnicalDescriptionGenerator.Generate(skeleton, intents, catalog).Markdown,
            ["export.json"] = BuildJson(skeleton, plan, catalog, opt),
        };
        return files;
    }

    /// <summary>A fájlok kiírása a célmappába (létrehozza, ha kell). Visszaadja az abszolút útvonalakat.</summary>
    public static IReadOnlyList<string> WriteAll(string outputDir, IReadOnlyDictionary<string, string> files)
    {
        Directory.CreateDirectory(outputDir);
        var written = new List<string>();
        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        foreach (var (name, content) in files)
        {
            var path = Path.Combine(outputDir, name);
            File.WriteAllText(path, content, utf8Bom);
            written.Add(path);
        }
        return written;
    }

    // ─── Szabászat (valós PQ-séma) ───────────────────────────────────────────
    private static string BuildSzabaszatCsv(CuttingPlan plan, IReadOnlyDictionary<string, MaterialDto> catalog, string dsmr)
    {
        var header = new[] { "DSMR", "Sorszám", "Hosszúság", "Szélesség", "Darab", "Név", "Megjegyzés",
            "Tipus", "Alkatrész Megnevezése", "Anyag", "Vastagság", "Felület tipus", "Szín", "Minta" };
        var sb = new StringBuilder();
        sb.Append(Row(header));
        int i = 1;
        foreach (var p in plan.Pieces)
        {
            catalog.TryGetValue(p.MaterialId, out var mat);
            sb.Append(Row(new[]
            {
                dsmr, i.ToString(Hu),
                Num(p.CutLengthMm), Num(p.CutWidthMm), p.Quantity.ToString(Hu),
                p.Name, "", mat?.Category ?? "",
                PartHu(p.Name), mat?.DisplayName ?? p.MaterialId, Num(p.ThicknessMm),
                p.Surface, MaterialAttributes.ColorLabel(mat?.BodyJson), ""
            }));
            i++;
        }
        return sb.ToString();
    }

    // ─── Anyag Szükséglet (valós PQ-séma) ────────────────────────────────────
    private static string BuildMennyisegekCsv(IReadOnlyList<BomLine> bom, IReadOnlyDictionary<string, MaterialDto> catalog, string dsmr)
    {
        var header = new[] { "DSMR", "Sorszám", "Alkatrész Megnevezése", "Anyag", "Vastagság", "Szélesség", "Hosszúság", "Darab", "Szín" };
        var sb = new StringBuilder();
        sb.Append(Row(header));
        int i = 1;
        foreach (var b in bom)
        {
            catalog.TryGetValue(b.MaterialId, out var mat);
            sb.Append(Row(new[]
            {
                dsmr, i.ToString(Hu), PartHu(b.Name), mat?.DisplayName ?? b.MaterialId,
                Num(b.Thickness), Num(b.Width), Num(b.Length), b.Quantity.ToString(Hu),
                MaterialAttributes.ColorLabel(mat?.BodyJson)
            }));
            i++;
        }
        return sb.ToString();
    }

    // ─── Árkalkuláció (11 lépés) ─────────────────────────────────────────────
    private static string BuildKalkulacioCsv(IReadOnlyList<BomLine> bom, IReadOnlyDictionary<string, MaterialDto> catalog, ExportOptions opt)
    {
        decimal materialCost = BomAggregator.Summarize(bom, catalog).Sum(s => s.EstimatedCost ?? 0m)
                             + Edging.EdgingCalculator.Summarize(bom, catalog).Sum(e => e.EstimatedCost ?? 0m);
        var r = CostCalculator.Calculate(new CostInput(
            materialCost, opt.LaborHours, (decimal)opt.HourlyRate, (decimal)opt.OtherCosts,
            opt.SocialContributionPct, opt.OverheadPct, opt.ProfitPct, opt.VatPct));

        var rows = new (int Step, string Label, decimal Amount)[]
        {
            (1, "Anyagköltség", r.Step01MaterialCost),
            (2, "Bérköltség", r.Step02LaborCost),
            (3, "Bérköltség járulékai", r.Step03LaborContributions),
            (4, "Egyéb költségek", r.Step04OtherCosts),
            (5, "Közvetlen költségek", r.Step05DirectCosts),
            (6, "Általános költségek", r.Step06OverheadCosts),
            (7, "Önköltség", r.Step07PrimeCost),
            (8, "Nyereség", r.Step08Profit),
            (9, "Kalkulált ár", r.Step09CalculatedPrice),
            (10, "Nettó eladási ár", r.Step10NetSellingPrice),
            (11, "Bruttó eladási ár", r.Step11GrossSellingPrice),
        };
        var sb = new StringBuilder();
        sb.Append(Row(new[] { "Lépés", "Megnevezés", "Összeg (Ft)" }));
        foreach (var row in rows)
            sb.Append(Row(new[] { row.Step.ToString(Hu), row.Label, Num((double)row.Amount) }));
        return sb.ToString();
    }

    private static string BuildJson(Skeleton s, CuttingPlan plan, IReadOnlyDictionary<string, MaterialDto> catalog, ExportOptions opt)
    {
        var payload = new
        {
            dsmr = opt.Dsmr,
            cabinetName = s.Name,
            skeletonId = s.Id.Value.ToString(),
            allowanceMm = opt.AllowanceMm,
            pieces = plan.Pieces.Select(p => new
            {
                part = PartHu(p.Name), materialId = p.MaterialId,
                cutLengthMm = p.CutLengthMm, cutWidthMm = p.CutWidthMm,
                thicknessMm = p.ThicknessMm, quantity = p.Quantity,
                surface = p.Surface, grain = p.Grain, edgingId = p.EdgingId
            })
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    // ─── CSV segédek (HU-konvenció: ; elválasztó, tizedesvessző, CRLF) ────────
    private static string Num(double v) => v.ToString("0.##", Hu);

    private static string Row(IEnumerable<string> cells) =>
        string.Join(";", cells.Select(Escape)) + "\r\n";

    private static string Escape(string cell)
    {
        if (cell.Contains(';') || cell.Contains('"') || cell.Contains('\n'))
            return "\"" + cell.Replace("\"", "\"\"") + "\"";
        return cell;
    }
}
