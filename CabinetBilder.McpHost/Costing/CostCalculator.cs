using System;

namespace CabinetBilder.McpHost.Costing;

/// <summary>A 11 lépéses összetett árkalkuláció bemenete. MINDEN százalék konfigurálható
/// (tankönyv: a járulék/áfa a mindenkori jogszabály szerint változik — nem drótozzuk be).</summary>
public sealed record CostInput(
    decimal MaterialCost,                 // 1. anyagköltség (nettó): lap + élzáró + szerelvény
    double LaborHours,                    // 2.-höz: tapasztalati munkaóra
    decimal HourlyRate,                   // 2.-höz: bruttó órabér (Ft/óra)
    decimal OtherCosts = 0m,              // 4. egyéb (külső bérmunka: felület/kárpit/üveg)
    double SocialContributionPct = 13.0,  // 3. bérjárulék % (szocho, 2023: 13%)
    double OverheadPct = 20.0,            // 6. általános költségek % (a közvetlenre)
    double ProfitPct = 15.0,              // 8. nyereség % (az önköltségre)
    double VatPct = 27.0,                 // 11. áfa %
    int RoundToHuf = 1000);               // 10. nettó eladási ár kerekítési egysége (lefelé)

/// <summary>A 11 lépés eredménye — a tankönyvi (fig-2.21) sorszámozás szerint.</summary>
public sealed record CostResult(
    decimal Step01MaterialCost,
    decimal Step02LaborCost,
    decimal Step03LaborContributions,
    decimal Step04OtherCosts,
    decimal Step05DirectCosts,
    decimal Step06OverheadCosts,
    decimal Step07PrimeCost,
    decimal Step08Profit,
    decimal Step09CalculatedPrice,
    decimal Step10NetSellingPrice,
    decimal Step11GrossSellingPrice);

/// <summary>
/// Összetett árkalkuláció a faipari tankönyv 11 lépéses sémája szerint
/// (docs/knowledge/woodwork_domain.md §10, fig-2.21). Minden lépés egész Ft-ra
/// kerekül (a tankönyvi példával egyezően); a nettó eladási ár a megadott
/// egységre LEFELÉ kerekül ("kerekítés, akár lefelé is").
/// </summary>
public static class CostCalculator
{
    public static CostResult Calculate(CostInput input)
    {
        if (input.LaborHours < 0) throw new ArgumentOutOfRangeException(nameof(input), "A munkaóra nem lehet negatív.");
        if (input.RoundToHuf <= 0) throw new ArgumentOutOfRangeException(nameof(input), "A kerekítési egység pozitív kell legyen.");

        decimal s1 = Math.Round(input.MaterialCost);
        decimal s2 = Math.Round((decimal)input.LaborHours * input.HourlyRate);
        decimal s3 = Math.Round(s2 * (decimal)input.SocialContributionPct / 100m);
        decimal s4 = Math.Round(input.OtherCosts);
        decimal s5 = s1 + s2 + s3 + s4;
        decimal s6 = Math.Round(s5 * (decimal)input.OverheadPct / 100m);
        decimal s7 = s5 + s6;
        decimal s8 = Math.Round(s7 * (decimal)input.ProfitPct / 100m);
        decimal s9 = s7 + s8;
        decimal s10 = Math.Floor(s9 / input.RoundToHuf) * input.RoundToHuf;
        decimal s11 = Math.Round(s10 * (1m + (decimal)input.VatPct / 100m));

        return new CostResult(s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11);
    }
}
