using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Edging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class EdgingCalculatorTests
{
    private static Dictionary<string, MaterialDto> Catalog() => new()
    {
        ["ABS2_WHITE"] = new("ABS2_WHITE", "ABS élzáró fehér 2mm", "Élzáró", 2, "{\"unit\":\"fm\"}", 200m),
    };

    [TestMethod]
    public void EdgingLengthM_UsesLongEdge_TimesQuantity()
    {
        // 720x560, 2 db, élzáróval: 0.72 m * 2 = 1.44 m
        var line = new BomLine("Side", 720, 560, 18, "LAM18", 2, EdgingId: "ABS2_WHITE");
        Assert.AreEqual(1.44, EdgingCalculator.EdgingLengthM(line), 0.0001);
    }

    [TestMethod]
    public void EdgingLengthM_NoEdging_ReturnsZero()
    {
        var line = new BomLine("Back", 710, 590, 3, "HDF3", 1);
        Assert.AreEqual(0, EdgingCalculator.EdgingLengthM(line), 0.0001);
    }

    [TestMethod]
    public void Summarize_GroupsByEdging_ComputesCost()
    {
        var lines = new List<BomLine>
        {
            new("Side Left", 720, 560, 18, "LAM18", 1, EdgingId: "ABS2_WHITE"),
            new("Side Right", 720, 560, 18, "LAM18", 1, EdgingId: "ABS2_WHITE"),
            new("Back", 710, 590, 3, "HDF3", 1), // nincs élzáró
        };

        var summary = EdgingCalculator.Summarize(lines, Catalog());

        Assert.AreEqual(1, summary.Count);
        var abs = summary.Single();
        Assert.AreEqual(2, abs.PieceCount);
        Assert.AreEqual(1.44, abs.TotalLengthM, 0.0001);  // 2 * 0.72
        Assert.AreEqual(288m, abs.EstimatedCost);          // 1.44 * 200
    }

    [TestMethod]
    public void Skeleton_ComputeBom_AssignsEdging_ToCarcassOnly()
    {
        var s = new Skeleton(SkeletonId.New());
        var bom = s.ComputeBom().ToList();

        Assert.IsTrue(bom.Where(b => b.Name != "Back").All(b => b.EdgingId == "ABS2_WHITE"));
        Assert.IsNull(bom.Single(b => b.Name == "Back").EdgingId);
    }

    [TestMethod]
    public void Skeleton_SetEdgingParameter_ChangesBomEdging()
    {
        var s = new Skeleton(SkeletonId.New());
        var result = s.ApplyParameter("EdgingId", "ABS2_SONOMA");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(s.ComputeBom().Where(b => b.Name != "Back").All(b => b.EdgingId == "ABS2_SONOMA"));
    }
}
