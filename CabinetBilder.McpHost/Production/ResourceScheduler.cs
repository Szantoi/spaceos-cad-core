using System;
using System.Collections.Generic;
using System.Linq;

namespace CabinetBilder.McpHost.Production;

/// <summary>Egy ütemezendő projekt (job): műveletek + időtartamok (a LaborEstimator ProcessHours-ából).</summary>
public sealed record ScheduleJob(string JobId, IReadOnlyList<Operation> Operations, IReadOnlyDictionary<string, double> Durations);

/// <summary>Egy ütemezett feladat az erőforrás-korlátos tervben (idők munkaórában a kezdéstől).</summary>
public sealed record ScheduledTask(
    string JobId,
    string OperationId,
    string Name,
    string Role,
    int Worker,
    double StartHours,
    double FinishHours);

/// <summary>Az erőforrás-korlátos ütemezés eredménye.</summary>
public sealed record ResourceSchedule(
    IReadOnlyList<ScheduledTask> Tasks,
    double MakespanHours,
    IReadOnlyDictionary<string, double> RoleBusyHours,
    IReadOnlyDictionary<string, int> RoleCapacity,
    IReadOnlyDictionary<string, double> RoleUtilization);

/// <summary>
/// Erőforrás-korlátos ütemező (list scheduling) TÖBB projektre, közös szakma-kapacitással.
/// Egy szakmában N azonos dolgozó; a művelet a legkorábbi időben indul, amikor az elődei
/// készek ÉS van szabad dolgozó. A precedencia a job-on belüli DAG. Munkaóra-térben fut;
/// a naptárra a WorkCalendar vetíti. Pure static.
/// </summary>
public static class ResourceScheduler
{
    public static ResourceSchedule Build(
        IReadOnlyList<ScheduleJob> jobs,
        IReadOnlyDictionary<string, int> capacity)
    {
        // 1) Job-onként CPM-ES (a precedencia-helyes prioritáshoz)
        var esByGlobal = new Dictionary<string, double>();
        var flat = new List<(string JobId, Operation Op, double Dur, double Es, List<string> PredGlobals)>();
        foreach (var job in jobs)
        {
            var cpm = Scheduler.Build(job.Operations, job.Durations);
            var esById = cpm.Operations.ToDictionary(o => o.OperationId, o => o.EarliestStartHours);
            var present = new HashSet<string>(job.Durations.Keys);
            foreach (var op in job.Operations.Where(o => present.Contains(o.Id)))
            {
                string g = Global(job.JobId, op.Id);
                double es = esById.TryGetValue(op.Id, out var v) ? v : 0;
                esByGlobal[g] = es;
                var preds = (op.DependsOn ?? Array.Empty<OperationDependency>())
                    .Where(d => present.Contains(d.OnOperationId))
                    .Select(d => Global(job.JobId, d.OnOperationId))
                    .ToList();
                flat.Add((job.JobId, op, job.Durations[op.Id], es, preds));
            }
        }

        // 2) Prioritás: ES asc (precedencia-helyes, mert dur>0), majd job, opId
        var order = flat
            .OrderBy(x => x.Es).ThenBy(x => x.JobId).ThenBy(x => x.Op.Id)
            .ToList();

        // 3) List scheduling: szakmánként a dolgozók "szabaddá válik" időpontjai
        int Cap(string role) => capacity.TryGetValue(role, out var c) && c > 0 ? c : 1;
        var workerFree = new Dictionary<string, double[]>();
        foreach (var role in flat.Select(x => x.Op.Role).Distinct())
            workerFree[role] = Enumerable.Repeat(0.0, Cap(role)).ToArray();

        var finishByGlobal = new Dictionary<string, double>();
        var tasks = new List<ScheduledTask>();
        foreach (var x in order)
        {
            double ready = 0;
            foreach (var p in x.PredGlobals)
                if (finishByGlobal.TryGetValue(p, out var pf)) ready = Math.Max(ready, pf);

            // A leghamarabb szabaddá váló dolgozó az adott szakmában
            var free = workerFree[x.Op.Role];
            int w = 0;
            for (int i = 1; i < free.Length; i++)
                if (free[i] < free[w]) w = i;

            double start = Math.Max(ready, free[w]);
            double finish = start + x.Dur;
            free[w] = finish;

            string g = Global(x.JobId, x.Op.Id);
            finishByGlobal[g] = finish;
            tasks.Add(new ScheduledTask(x.JobId, x.Op.Id, x.Op.Name, x.Op.Role, w, start, finish));
        }

        double makespan = tasks.Count > 0 ? tasks.Max(t => t.FinishHours) : 0;
        var roleBusy = tasks.GroupBy(t => t.Role).ToDictionary(g => g.Key, g => g.Sum(t => t.FinishHours - t.StartHours));
        var roleCap = roleBusy.Keys.ToDictionary(r => r, Cap);
        var roleUtil = roleBusy.ToDictionary(
            kv => kv.Key,
            kv => makespan > 0 ? kv.Value / (roleCap[kv.Key] * makespan) : 0.0);

        return new ResourceSchedule(
            tasks.OrderBy(t => t.StartHours).ThenBy(t => t.JobId).ThenBy(t => t.OperationId).ToList(),
            makespan, roleBusy, roleCap, roleUtil);
    }

    private static string Global(string jobId, string opId) => jobId + "::" + opId;
}
