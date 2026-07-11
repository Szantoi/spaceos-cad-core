using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Bom;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Costing;
using CabinetBilder.McpHost.Edging;
using CabinetBilder.McpHost.Production;
using CabinetBilder.McpHost.Serialization;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost.Tools;

/// <summary>
/// Árkalkulációs MCP tool — a faipari tankönyv 11 lépéses összetett sémája
/// (woodwork_domain.md §10, fig-2.21). Az anyagköltség a skeleton BOM-jából
/// automatikus (lap + élzáró); a többi tétel input, a százalékok konfigurálhatók.
/// </summary>
[McpServerToolType]
public static class CostingTools
{
    [McpServerTool(Name = "skeleton_cost_calculation"), Description("Összetett árkalkuláció a tankönyvi 11 lépéses séma szerint. Az anyagköltség a BOM-ból automatikus (lap+élzáró), a munkaóra/órabér/százalékok paraméterek. Webre kész JSON, mind a 11 lépés címkézve.")]
    public static async Task<McpToolResponse<object>> CostCalculation(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        [Description("Tapasztalati munkaóra (2. lépés alapja). Ha < 0, a modern folyamat-modellből (labor_estimate) számolt mancsóra kerül felhasználásra.")] double laborHours = -1,
        [Description("Bruttó órabér, Ft/óra.")] double hourlyRate = 5000,
        [Description("Egyéb költségek, Ft (külső bérmunka: felület/kárpit/üveg).")] double otherCosts = 0,
        [Description("Bérjárulék % (szocho; 2023: 13).")] double socialContributionPct = 13,
        [Description("Általános költségek %-a a közvetlen költségekre (rezsi/raktár/szállítás/admin).")] double overheadPct = 20,
        [Description("Nyereség %-a az önköltségre.")] double profitPct = 15,
        [Description("ÁFA % (2023: 27).")] double vatPct = 27,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        decimal materialCost;
        bool autoLabor = laborHours < 0;
        double laborSource = laborHours;
        lock (entry.Lock)
        {
            var lines = entry.Skeleton.ComputeBom().ToList();
            var panels = BomAggregator.Summarize(lines, lookup);
            var edging = EdgingCalculator.Summarize(lines, lookup);
            materialCost = panels.Sum(p => p.EstimatedCost ?? 0m) + edging.Sum(e => e.EstimatedCost ?? 0m);

            if (autoLabor)
            {
                var est = LaborEstimator.Estimate(lines, lookup, OperationCatalog.CarcassOperations);
                laborSource = est.TotalManHours;
            }
        }

        var result = CostCalculator.Calculate(new CostInput(
            MaterialCost: materialCost,
            LaborHours: laborSource,
            HourlyRate: (decimal)hourlyRate,
            OtherCosts: (decimal)otherCosts,
            SocialContributionPct: socialContributionPct,
            OverheadPct: overheadPct,
            ProfitPct: profitPct,
            VatPct: vatPct));

        return Ok(new
        {
            steps = new object[]
            {
                new { step = 1,  label = "Anyagköltség (lap + élzáró, a BOM-ból)", amountHuf = result.Step01MaterialCost },
                new { step = 2,  label = $"Bérköltség ({Math.Round(laborSource,2)} {(autoLabor ? "mancsóra (folyamat-modell)" : "óra")} × {hourlyRate} Ft)", amountHuf = result.Step02LaborCost },
                new { step = 3,  label = $"Bérköltség járulékai ({socialContributionPct}%)", amountHuf = result.Step03LaborContributions },
                new { step = 4,  label = "Egyéb költségek (külső bérmunka)", amountHuf = result.Step04OtherCosts },
                new { step = 5,  label = "Közvetlen költségek (1+2+3+4)", amountHuf = result.Step05DirectCosts },
                new { step = 6,  label = $"Általános költségek ({overheadPct}% a közvetlenre)", amountHuf = result.Step06OverheadCosts },
                new { step = 7,  label = "Önköltség (5+6)", amountHuf = result.Step07PrimeCost },
                new { step = 8,  label = $"Nyereség ({profitPct}% az önköltségre)", amountHuf = result.Step08Profit },
                new { step = 9,  label = "Kalkulált ár (7+8)", amountHuf = result.Step09CalculatedPrice },
                new { step = 10, label = "Nettó eladási ár (1000 Ft-ra lefelé kerekítve)", amountHuf = result.Step10NetSellingPrice },
                new { step = 11, label = $"Bruttó eladási ár (+{vatPct}% áfa)", amountHuf = result.Step11GrossSellingPrice }
            },
            laborHoursUsed = Math.Round(laborSource, 2),
            laborSource = autoLabor ? "folyamat-modell (auto)" : "kézi input",
            netSellingPriceHuf = result.Step10NetSellingPrice,
            grossSellingPriceHuf = result.Step11GrossSellingPrice,
            note = "Séma: faipari tankönyv 40-41. o. (összetett kalkuláció); a százalékok a mindenkori jogszabály szerint paraméterezendők. Munkaóra: kézi (laborHours>=0) vagy a modern folyamat-modellből (laborHours<0)."
        });
    }

    private static McpToolResponse<object> Ok(object value) =>
        new() { IsSuccess = true, Status = "Ok", Value = value };

    private static McpToolResponse<object> Fail(string status, string message) =>
        new() { IsSuccess = false, Status = status, Errors = { message }, Value = null };
}
