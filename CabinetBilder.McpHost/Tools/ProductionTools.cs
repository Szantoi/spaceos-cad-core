using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Production;
using CabinetBilder.McpHost.Serialization;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost.Tools;

/// <summary>
/// Gyártási munkaidő-becslés MCP tool — a modern műveleti katalógus (a legacy Egység_idő
/// modernizált, munkanaplóval mért adatokból). A mancsóra táplálja az árkalkuláció bérköltségét.
/// </summary>
[McpServerToolType]
public static class ProductionTools
{
    [McpServerTool(Name = "skeleton_labor_estimate"), Description("Gyártási munkaidő-becslés a BOM-ból a modern műveleti katalógus szerint (mért egységidők tiszta órában). Műveletenként + szakmánként mancsóra; ez a kalkuláció bérköltségének alapja.")]
    public static async Task<McpToolResponse<object>> LaborEstimate(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        LaborEstimate est;
        lock (entry.Lock)
        {
            est = LaborEstimator.Estimate(entry.Skeleton.ComputeBom().ToList(), lookup, OperationCatalog.CarcassOperations);
        }

        return Ok(new
        {
            operations = est.Operations.Select(o => new
            {
                operationId = o.OperationId,
                name = o.OperationName,
                role = o.Role,
                appliedPieceCount = o.AppliedPieceCount,
                processHours = Math.Round(o.ProcessHours, 4),
                manHours = Math.Round(o.ManHours, 4)
            }).ToArray(),
            manHoursByRole = est.ManHoursByRole.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 4)),
            totalProcessHours = Math.Round(est.TotalProcessHours, 4),
            totalManHours = Math.Round(est.TotalManHours, 4),
            note = "Modern műveleti katalógus (a legacy Egység_idő modernizált, munkanaplóval mért adatokból). A mancsóra táplálja a cost_calculation bérköltségét (autoLabor)."
        });
    }

    [McpServerTool(Name = "skeleton_production_schedule"), Description("Gyártási ütemezés (CPM) a művelet-DAG-ból: átfutási idő + kritikus út + műveletenkénti kezdés/befejezés/tartalék. A legacy 02 Folyamatok modern kiváltása (FS/SS függőségek).")]
    public static async Task<McpToolResponse<object>> ProductionSchedule(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A skeleton egyedi azonosítója (UUID).")] string skeletonId,
        [Description("Munkanap órában (a nap-konverzióhoz). Alapértelmezett 8.")] double workdayHours = 8,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(skeletonId, out var id))
            return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{skeletonId}'.");
        if (!registry.TryGet(id, out var entry))
            return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {skeletonId}.");
        if (workdayHours <= 0)
            return Fail("Invalid", "A munkanap órában pozitív kell legyen.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        Schedule schedule;
        try
        {
            lock (entry.Lock)
            {
                var est = LaborEstimator.Estimate(entry.Skeleton.ComputeBom().ToList(), lookup, OperationCatalog.CarcassOperations);
                var durations = est.Operations.ToDictionary(o => o.OperationId, o => o.ProcessHours);
                schedule = Scheduler.Build(OperationCatalog.CarcassOperations, durations);
            }
        }
        catch (InvalidOperationException ex)
        {
            return Fail("Error", ex.Message);
        }

        return Ok(new
        {
            leadTimeHours = Math.Round(schedule.LeadTimeHours, 4),
            leadTimeDays = Math.Round(schedule.LeadTimeHours / workdayHours, 3),
            criticalPath = schedule.CriticalPath,
            operations = schedule.Operations.Select(o => new
            {
                operationId = o.OperationId,
                name = o.Name,
                role = o.Role,
                durationHours = Math.Round(o.DurationHours, 4),
                earliestStartHours = Math.Round(o.EarliestStartHours, 4),
                earliestFinishHours = Math.Round(o.EarliestFinishHours, 4),
                slackHours = Math.Round(o.SlackHours, 4),
                critical = o.Critical
            }).ToArray(),
            note = "CPM a mért egységidőkből (a legacy Egység_idő FS/SS függőségei modernizálva). Az időtartam ProcessHours; a Humánerő kapacitás. A kritikus úton nincs tartalék."
        });
    }

    [McpServerTool(Name = "skeleton_schedule_projects"), Description("Naptári + erőforrás-korlátos ütemezés TÖBB projektre közös szakma-kapacitással (list scheduling + munkanaptár): projektenkénti/műveletenkénti naptári kezdés/vég, makespan-dátum, szakma-kihasználtság. A legacy 02 Folyamatok kapacitás-tervezése modernizálva.")]
    public static async Task<McpToolResponse<object>> ScheduleProjects(
        SkeletonRegistry registry,
        ILocalStore store,
        [Description("A projektek skeleton-azonosítói (UUID-k).")] string[] skeletonIds,
        [Description("Kezdő dátum (ISO, pl. 2026-07-13T08:00:00). A legközelebbi munkanapra igazítjuk.")] string startDate,
        [Description("Asztalos dolgozók száma.")] int asztalosCount = 1,
        [Description("CNC dolgozók száma.")] int cncCount = 1,
        [Description("Összeszerelő dolgozók száma.")] int osszeszereloCount = 1,
        [Description("Napi munkaóra.")] double workdayHours = 8,
        CancellationToken ct = default)
    {
        if (skeletonIds == null || skeletonIds.Length == 0)
            return Fail("Invalid", "Legalább egy skeletonId kell.");
        if (!DateTime.TryParse(startDate, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var start))
            return Fail("Invalid", $"Érvénytelen kezdő dátum: '{startDate}'.");
        if (workdayHours <= 0)
            return Fail("Invalid", "A napi munkaóra pozitív kell legyen.");

        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var lookup = materials.ToDictionary(m => m.MaterialCode, m => m);

        var jobs = new List<ScheduleJob>();
        foreach (var sid in skeletonIds)
        {
            if (!Guid.TryParse(sid, out var id))
                return Fail("Invalid", $"A skeletonId nem érvényes UUID: '{sid}'.");
            if (!registry.TryGet(id, out var entry))
                return Fail("NotFound", $"Nincs skeleton ezzel az azonosítóval: {sid}.");
            lock (entry.Lock)
            {
                var est = LaborEstimator.Estimate(entry.Skeleton.ComputeBom().ToList(), lookup, OperationCatalog.CarcassOperations);
                var durations = est.Operations.ToDictionary(o => o.OperationId, o => o.ProcessHours);
                jobs.Add(new ScheduleJob(entry.Skeleton.Name + " (" + sid.Substring(0, 8) + ")", OperationCatalog.CarcassOperations, durations));
            }
        }

        var capacity = new Dictionary<string, int>
        {
            ["Asztalos"] = asztalosCount,
            ["CNC"] = cncCount,
            ["Összeszerelő"] = osszeszereloCount,
        };

        ResourceSchedule schedule;
        try
        {
            schedule = ResourceScheduler.Build(jobs, capacity);
        }
        catch (InvalidOperationException ex)
        {
            return Fail("Error", ex.Message);
        }

        var cal = new WorkCalendar(start, workdayHours);

        return Ok(new
        {
            startDate = cal.Start.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            projectCount = jobs.Count,
            makespanHours = Math.Round(schedule.MakespanHours, 4),
            makespanDays = Math.Round(schedule.MakespanHours / workdayHours, 3),
            finishDate = cal.AtWorkHours(schedule.MakespanHours).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            roleCapacity = schedule.RoleCapacity,
            roleUtilization = schedule.RoleUtilization.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value * 100, 1)),
            tasks = schedule.Tasks.Select(t => new
            {
                project = t.JobId,
                operationId = t.OperationId,
                name = t.Name,
                role = t.Role,
                worker = t.Worker + 1,
                startHours = Math.Round(t.StartHours, 4),
                finishHours = Math.Round(t.FinishHours, 4),
                startDate = cal.AtWorkHours(t.StartHours).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                finishDate = cal.AtWorkHours(t.FinishHours).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
            }).ToArray(),
            note = "List scheduling (azonos dolgozók szakmánként) + munkanaptár (hétvége kimarad). A roleUtilization % a makespanre vetített foglaltság. A legacy 02 Folyamatok kapacitás-tervezése modernizálva."
        });
    }

    private static McpToolResponse<object> Ok(object value) =>
        new() { IsSuccess = true, Status = "Ok", Value = value };

    private static McpToolResponse<object> Fail(string status, string message) =>
        new() { IsSuccess = false, Status = status, Errors = { message }, Value = null };
}
