using System.Collections.Generic;
using System.Linq;
using CabinetBilder.McpHost.Production;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class ResourceSchedulerTests
{
    private static ScheduleJob Job(string id, string role, double dur) =>
        new(id, new[] { new Operation("A", "A", role, dur, 1) },
            new Dictionary<string, double> { ["A"] = dur });

    [TestMethod]
    public void TwoJobs_SharedRole_Capacity1_Serializes()
    {
        var jobs = new[] { Job("J1", "R", 5), Job("J2", "R", 5) };
        var s = ResourceScheduler.Build(jobs, new Dictionary<string, int> { ["R"] = 1 });

        Assert.AreEqual(10, s.MakespanHours, 1e-9); // 5 + 5 sorban
        var starts = s.Tasks.Select(t => t.StartHours).OrderBy(x => x).ToArray();
        CollectionAssert.AreEqual(new[] { 0.0, 5.0 }, starts);
    }

    [TestMethod]
    public void TwoJobs_SharedRole_Capacity2_RunInParallel()
    {
        var jobs = new[] { Job("J1", "R", 5), Job("J2", "R", 5) };
        var s = ResourceScheduler.Build(jobs, new Dictionary<string, int> { ["R"] = 2 });

        Assert.AreEqual(5, s.MakespanHours, 1e-9); // párhuzamosan, két dolgozó
        Assert.IsTrue(s.Tasks.All(t => t.StartHours == 0));
        // két külön dolgozóra kerültek
        Assert.AreEqual(2, s.Tasks.Select(t => t.Worker).Distinct().Count());
    }

    [TestMethod]
    public void ChainWithinJob_RespectsPrecedence()
    {
        var ops = new[]
        {
            new Operation("A", "A", "R", 2, 1),
            new Operation("B", "B", "R", 3, 1, DependsOn: new[] { new OperationDependency("A", DependencyType.FinishStart) }),
        };
        var job = new ScheduleJob("J", ops, new Dictionary<string, double> { ["A"] = 2, ["B"] = 3 });

        var s = ResourceScheduler.Build(new[] { job }, new Dictionary<string, int> { ["R"] = 5 });

        var a = s.Tasks.Single(t => t.OperationId == "A");
        var b = s.Tasks.Single(t => t.OperationId == "B");
        Assert.AreEqual(0, a.StartHours, 1e-9);
        Assert.AreEqual(2, b.StartHours, 1e-9);   // B csak A befejezése után (FS)
        Assert.AreEqual(5, s.MakespanHours, 1e-9);
    }

    [TestMethod]
    public void Utilization_FullyBusySingleWorker_Is100Percent()
    {
        var jobs = new[] { Job("J1", "R", 5), Job("J2", "R", 5) };
        var s = ResourceScheduler.Build(jobs, new Dictionary<string, int> { ["R"] = 1 });

        // 10 óra munka / (1 dolgozó × 10 makespan) = 100%
        Assert.AreEqual(1.0, s.RoleUtilization["R"], 1e-9);
    }

    [TestMethod]
    public void CarcassTwoProjects_Capacity1_LongerThanSingle()
    {
        var dur = new Dictionary<string, double>
        {
            ["SZABAS"] = 0.15, ["CNC_FURAT"] = 0.3812, ["ELZARAS"] = 0.2,
            ["CSISZOLAS"] = 0.3024, ["HATLAP_SZABAS"] = 0.0375, ["OSSZEALLITAS"] = 0.5,
        };
        var one = new[] { new ScheduleJob("A", OperationCatalog.CarcassOperations, dur) };
        var two = new[]
        {
            new ScheduleJob("A", OperationCatalog.CarcassOperations, dur),
            new ScheduleJob("B", OperationCatalog.CarcassOperations, dur),
        };
        var cap = new Dictionary<string, int> { ["Asztalos"] = 1, ["CNC"] = 1, ["Összeszerelő"] = 1 };

        var s1 = ResourceScheduler.Build(one, cap);
        var s2 = ResourceScheduler.Build(two, cap);

        // két projekt közös 1-1 dolgozóval hosszabb, mint egy projekt
        Assert.IsTrue(s2.MakespanHours > s1.MakespanHours);
    }
}
