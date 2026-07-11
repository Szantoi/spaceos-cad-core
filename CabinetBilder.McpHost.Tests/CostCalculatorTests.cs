using System;
using CabinetBilder.McpHost.Costing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class CostCalculatorTests
{
    [TestMethod]
    public void Calculate_TextbookExample_MatchesFig221()
    {
        // A tankönyvi példa (woodwork_domain.md §10, fig-2.21):
        // bér 16h × 5000 = 80 000; járulék 13% = 10 400; közvetlen = 157 986
        // => anyag+egyéb = 157 986 - 90 400 = 67 586 (egyéb=0 mellett anyag = 67 586)
        var input = new CostInput(
            MaterialCost: 67586m,
            LaborHours: 16,
            HourlyRate: 5000m,
            OtherCosts: 0m,
            SocialContributionPct: 13,
            OverheadPct: 20,
            ProfitPct: 15,
            VatPct: 27);

        var r = CostCalculator.Calculate(input);

        Assert.AreEqual(80000m, r.Step02LaborCost);
        Assert.AreEqual(10400m, r.Step03LaborContributions);
        Assert.AreEqual(157986m, r.Step05DirectCosts);
        Assert.AreEqual(31597m, r.Step06OverheadCosts);   // 157986 * 20% = 31597.2 -> 31597
        Assert.AreEqual(189583m, r.Step07PrimeCost);
        Assert.AreEqual(28437m, r.Step08Profit);          // 189583 * 15% = 28437.45 -> 28437
        Assert.AreEqual(218020m, r.Step09CalculatedPrice);
        Assert.AreEqual(218000m, r.Step10NetSellingPrice); // 1000-re lefelé
        Assert.AreEqual(276860m, r.Step11GrossSellingPrice); // 218000 * 1.27
    }

    [TestMethod]
    public void Calculate_PercentagesAreConfigurable()
    {
        // 0% mindenből: bruttó = nettó = kalkulált = közvetlen (kerekítve 1-re)
        var input = new CostInput(
            MaterialCost: 100000m, LaborHours: 0, HourlyRate: 0m,
            SocialContributionPct: 0, OverheadPct: 0, ProfitPct: 0, VatPct: 0, RoundToHuf: 1);

        var r = CostCalculator.Calculate(input);

        Assert.AreEqual(100000m, r.Step05DirectCosts);
        Assert.AreEqual(100000m, r.Step10NetSellingPrice);
        Assert.AreEqual(100000m, r.Step11GrossSellingPrice);
    }

    [TestMethod]
    public void Calculate_NegativeLaborHours_Throws()
    {
        var input = new CostInput(MaterialCost: 0m, LaborHours: -1, HourlyRate: 5000m);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CostCalculator.Calculate(input));
    }
}
