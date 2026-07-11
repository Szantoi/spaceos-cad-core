using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Bom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class BomAggregatorTests
{
    private static Dictionary<string, MaterialDto> Catalog() => new()
    {
        ["LAM18_W1000"] = new("LAM18_W1000", "Fehér laminált 18mm", "Bútorlap", 18, "{\"finish\":\"laminalt\"}", 5000m),
        ["HDF3_WHITE"] = new("HDF3_WHITE", "Fehér HDF 3mm", "Hátlap", 3, "{\"finish\":\"hdf\"}", 1000m),
    };

    [TestMethod]
    public void AreaM2_ComputesLengthTimesWidthTimesQuantity()
    {
        // 1000mm x 500mm x 2db = 0.5 m² x 2 = 1.0 m²
        var line = new BomLine("Panel", 1000, 500, 18, "LAM18_W1000", 2);
        Assert.AreEqual(1.0, BomAggregator.AreaM2(line), 0.0001);
    }

    [TestMethod]
    public void Summarize_GroupsByMaterial_WithSurfaceAndTotals()
    {
        var lines = new List<BomLine>
        {
            new("Side Left", 720, 560, 18, "LAM18_W1000", 1),
            new("Side Right", 720, 560, 18, "LAM18_W1000", 1),
            new("Back", 710, 590, 3, "HDF3_WHITE", 1),
        };

        var summary = BomAggregator.Summarize(lines, Catalog());

        Assert.AreEqual(2, summary.Count); // két anyag
        var lam = summary.First(s => s.MaterialId == "LAM18_W1000");
        var hdf = summary.First(s => s.MaterialId == "HDF3_WHITE");

        Assert.AreEqual(2, lam.PieceCount);
        Assert.AreEqual("laminált", lam.Surface);
        Assert.AreEqual("hdf hátlap", hdf.Surface);

        // LAM terület: 2 * (0.72 * 0.56) = 0.8064 m²
        Assert.AreEqual(0.8064, lam.TotalAreaM2, 0.0001);
        // becsült költség: 0.8064 * 5000 = 4032
        Assert.AreEqual(4032m, lam.EstimatedCost);
    }

    [TestMethod]
    public void Summarize_UnknownMaterial_YieldsUnknownSurface_NoCost()
    {
        var lines = new List<BomLine> { new("X", 1000, 1000, 18, "NINCS_ILYEN", 1) };

        var summary = BomAggregator.Summarize(lines, Catalog());

        Assert.AreEqual(1, summary.Count);
        Assert.AreEqual("ismeretlen", summary[0].Surface);
        Assert.IsNull(summary[0].EstimatedCost);
        Assert.IsNull(summary[0].MaterialName);
    }
}
