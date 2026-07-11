using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Production;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class LaborEstimatorTests
{
    private static Dictionary<string, MaterialDto> Catalog() => new()
    {
        ["LAM18_W1000"] = new("LAM18_W1000", "Fehér laminált 18mm", "Bútorlap", 18, "{\"finish\":\"laminalt\"}", 5200m),
        ["HDF3_WHITE"] = new("HDF3_WHITE", "Fehér HDF 3mm", "Hátlap", 3, "{\"finish\":\"hdf\"}", 1250m),
    };

    [TestMethod]
    public void Estimate_DefaultCabinet_AppliesCarcassOpsToFourPanels()
    {
        var s = new Skeleton(SkeletonId.New());
        var est = LaborEstimator.Estimate(s.ComputeBom(), Catalog(), OperationCatalog.CarcassOperations);

        // 4 korpuszpanel (Side L/R, Bottom, Top) a Bútorlap-műveletekre; 1 hátlap; 1 összeállítás
        var szabas = est.Operations.Single(o => o.OperationId == "SZABAS");
        Assert.AreEqual(4, szabas.AppliedPieceCount);
        var hatlap = est.Operations.Single(o => o.OperationId == "HATLAP_SZABAS");
        Assert.AreEqual(1, hatlap.AppliedPieceCount);
        var ossze = est.Operations.Single(o => o.OperationId == "OSSZEALLITAS");
        Assert.AreEqual(1, ossze.AppliedPieceCount);
    }

    [TestMethod]
    public void Estimate_ManHours_IncludeHeadcount()
    {
        var s = new Skeleton(SkeletonId.New());
        var est = LaborEstimator.Estimate(s.ComputeBom(), Catalog(), OperationCatalog.CarcassOperations);

        // SZABAS: 0.0375 h × 4 db = 0.15 process; × 2 fő = 0.30 man-hour
        var szabas = est.Operations.Single(o => o.OperationId == "SZABAS");
        Assert.AreEqual(0.15, szabas.ProcessHours, 0.0001);
        Assert.AreEqual(0.30, szabas.ManHours, 0.0001);
    }

    [TestMethod]
    public void Estimate_TotalManHours_SumsAllOperations()
    {
        var s = new Skeleton(SkeletonId.New());
        var est = LaborEstimator.Estimate(s.ComputeBom(), Catalog(), OperationCatalog.CarcassOperations);

        double expected = est.Operations.Sum(o => o.ManHours);
        Assert.AreEqual(expected, est.TotalManHours, 0.0001);
        Assert.IsTrue(est.TotalManHours > 0);
        // szakmánkénti bontás összege = teljes mancsóra
        Assert.AreEqual(est.TotalManHours, est.ManHoursByRole.Values.Sum(), 0.0001);
    }

    [TestMethod]
    public void Estimate_ManHoursByRole_HasExpectedRoles()
    {
        var s = new Skeleton(SkeletonId.New());
        var est = LaborEstimator.Estimate(s.ComputeBom(), Catalog(), OperationCatalog.CarcassOperations);

        CollectionAssert.Contains(est.ManHoursByRole.Keys.ToArray(), "Asztalos");
        CollectionAssert.Contains(est.ManHoursByRole.Keys.ToArray(), "CNC");
        CollectionAssert.Contains(est.ManHoursByRole.Keys.ToArray(), "Összeszerelő");
    }
}
