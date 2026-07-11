using System;
using System.Collections.Generic;
using System.Linq;
using CabinetBilder.McpHost.Production;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class SchedulerTests
{
    private static Operation Op(string id, params OperationDependency[] deps) =>
        new(id, id, "R", 1, 1, DependsOn: deps.Length == 0 ? null : deps);

    private static OperationDependency FS(string on, double lag = 0) => new(on, DependencyType.FinishStart, lag);
    private static OperationDependency SS(string on, double lag = 0) => new(on, DependencyType.StartStart, lag);

    [TestMethod]
    public void Chain_FS_LeadTimeIsSumAndAllCritical()
    {
        var ops = new[] { Op("A"), Op("B", FS("A")), Op("C", FS("B")) };
        var dur = new Dictionary<string, double> { ["A"] = 1, ["B"] = 2, ["C"] = 3 };

        var sch = Scheduler.Build(ops, dur);

        Assert.AreEqual(6, sch.LeadTimeHours, 1e-9);
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, sch.CriticalPath.ToArray());
        Assert.IsTrue(sch.Operations.All(o => o.Critical));
    }

    [TestMethod]
    public void ParallelMerge_ShorterBranchHasSlack()
    {
        var ops = new[] { Op("A"), Op("B"), Op("C", FS("A"), FS("B")) };
        var dur = new Dictionary<string, double> { ["A"] = 1, ["B"] = 3, ["C"] = 2 };

        var sch = Scheduler.Build(ops, dur);

        Assert.AreEqual(5, sch.LeadTimeHours, 1e-9);
        var a = sch.Operations.Single(o => o.OperationId == "A");
        var b = sch.Operations.Single(o => o.OperationId == "B");
        Assert.AreEqual(2, a.SlackHours, 1e-9);   // rövidebb ág → 2 óra tartalék
        Assert.IsFalse(a.Critical);
        Assert.AreEqual(0, b.SlackHours, 1e-9);
        Assert.IsTrue(b.Critical);
        CollectionAssert.AreEqual(new[] { "B", "C" }, sch.CriticalPath.ToArray());
    }

    [TestMethod]
    public void StartStart_DependentStartsWithLagAfterParentStart()
    {
        var ops = new[] { Op("A"), Op("B", SS("A", 2)) };
        var dur = new Dictionary<string, double> { ["A"] = 5, ["B"] = 3 };

        var sch = Scheduler.Build(ops, dur);

        var b = sch.Operations.Single(o => o.OperationId == "B");
        Assert.AreEqual(2, b.EarliestStartHours, 1e-9);   // A.ES(0) + lag 2
        Assert.AreEqual(5, sch.LeadTimeHours, 1e-9);       // A: 0..5, B: 2..5
    }

    [TestMethod]
    public void Cycle_Throws()
    {
        var ops = new[] { Op("A", FS("B")), Op("B", FS("A")) };
        var dur = new Dictionary<string, double> { ["A"] = 1, ["B"] = 1 };

        Assert.ThrowsExactly<InvalidOperationException>(() => Scheduler.Build(ops, dur));
    }

    [TestMethod]
    public void MissingDurationOp_IsExcluded_AndItsDepsIgnored()
    {
        // B-nek nincs időtartama (kihagyott művelet) → C csak A-tól függjön effektíve
        var ops = new[] { Op("A"), Op("B", FS("A")), Op("C", FS("B"), FS("A")) };
        var dur = new Dictionary<string, double> { ["A"] = 2, ["C"] = 3 }; // B kimarad

        var sch = Scheduler.Build(ops, dur);

        Assert.AreEqual(2, sch.Operations.Count);
        Assert.AreEqual(5, sch.LeadTimeHours, 1e-9); // A(0..2) → C(2..5)
    }

    [TestMethod]
    public void CarcassCatalog_CriticalPathSkipsParallelBackPanel()
    {
        // A valós korpusz-lánc időtartamai (default szekrény, 4 korpuszpanel + 1 hátlap)
        var dur = new Dictionary<string, double>
        {
            ["SZABAS"] = 0.0375 * 4,
            ["CNC_FURAT"] = 0.0953 * 4,
            ["ELZARAS"] = 0.0500 * 4,
            ["CSISZOLAS"] = 0.0756 * 4,
            ["HATLAP_SZABAS"] = 0.0375 * 1,
            ["OSSZEALLITAS"] = 0.5,
        };

        var sch = Scheduler.Build(OperationCatalog.CarcassOperations, dur);

        double chain = dur["SZABAS"] + dur["CNC_FURAT"] + dur["ELZARAS"] + dur["CSISZOLAS"] + dur["OSSZEALLITAS"];
        Assert.AreEqual(chain, sch.LeadTimeHours, 1e-6);
        CollectionAssert.AreEqual(
            new[] { "SZABAS", "CNC_FURAT", "ELZARAS", "CSISZOLAS", "OSSZEALLITAS" },
            sch.CriticalPath.ToArray());
        // a hátlap-szabás párhuzamos → van tartaléka, nem kritikus
        var hatlap = sch.Operations.Single(o => o.OperationId == "HATLAP_SZABAS");
        Assert.IsFalse(hatlap.Critical);
        Assert.IsTrue(hatlap.SlackHours > 0);
    }
}
