using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Cutting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class CuttingPlannerTests
{
    private static Dictionary<string, MaterialDto> Catalog() => new()
    {
        ["LAM18"] = new("LAM18", "Laminált 18mm", "Bútorlap", 18, "{\"finish\":\"laminalt\"}", 5000m),
        ["HDF3"] = new("HDF3", "HDF 3mm", "Hátlap", 3, "{\"finish\":\"hdf\"}", 1000m),
    };

    [TestMethod]
    public void Plan_AppliesAllowance_ToCutDimensions()
    {
        var lines = new List<BomLine> { new("Panel", 1000, 500, 18, "LAM18", 1) };

        var plan = CuttingPlanner.Plan(lines, Catalog(), allowanceMm: 10);

        var piece = plan.Pieces.Single();
        Assert.AreEqual(1000, piece.FinishedLengthMm, 0.001);
        Assert.AreEqual(1020, piece.CutLengthMm, 0.001);   // +2*10
        Assert.AreEqual(520, piece.CutWidthMm, 0.001);
    }

    [TestMethod]
    public void Plan_ZeroAllowance_CutEqualsFinished()
    {
        var lines = new List<BomLine> { new("Panel", 720, 560, 18, "LAM18", 1) };

        var plan = CuttingPlanner.Plan(lines, Catalog(), allowanceMm: 0);

        var piece = plan.Pieces.Single();
        Assert.AreEqual(piece.FinishedLengthMm, piece.CutLengthMm, 0.001);
        Assert.AreEqual(piece.FinishedWidthMm, piece.CutWidthMm, 0.001);
    }

    [TestMethod]
    public void Plan_DerivesGrain_FromCategory()
    {
        var lines = new List<BomLine>
        {
            new("Side", 720, 560, 18, "LAM18", 1),
            new("Back", 710, 590, 3, "HDF3", 1),
        };

        var plan = CuttingPlanner.Plan(lines, Catalog());

        Assert.AreEqual("hossz", plan.Pieces.First(p => p.MaterialId == "LAM18").Grain);
        Assert.AreEqual("nincs", plan.Pieces.First(p => p.MaterialId == "HDF3").Grain);
    }

    [TestMethod]
    public void Plan_EstimatesBoards_FromCutArea()
    {
        // 3 db 2000x1000 laminált = 6 m² vágási terület.
        // Tábla 2800x2070 = 5.796 m², kihasználható 0.8 => 4.6368 m² => ceil(6/4.6368) = 2 tábla.
        var lines = new List<BomLine> { new("BigPanel", 2000, 1000, 18, "LAM18", 3) };

        var plan = CuttingPlanner.Plan(lines, Catalog());

        var sum = plan.ByMaterial.Single(s => s.MaterialId == "LAM18");
        Assert.AreEqual(3, sum.PieceCount);
        Assert.AreEqual(6.0, sum.TotalCutAreaM2, 0.0001);
        Assert.AreEqual(2, sum.EstimatedBoards);
        Assert.AreEqual(2800, sum.BoardLengthMm, 0.001);
    }
}
