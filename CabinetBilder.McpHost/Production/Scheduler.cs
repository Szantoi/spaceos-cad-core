using System;
using System.Collections.Generic;
using System.Linq;

namespace CabinetBilder.McpHost.Production;

/// <summary>Egy ütemezett művelet a CPM eredményében (idők órában).</summary>
public sealed record ScheduledOperation(
    string OperationId,
    string Name,
    string Role,
    double DurationHours,
    double EarliestStartHours,
    double EarliestFinishHours,
    double LateStartHours,
    double LateFinishHours,
    double SlackHours,
    bool Critical);

/// <summary>A CPM-ütemezés eredménye.</summary>
public sealed record Schedule(
    IReadOnlyList<ScheduledOperation> Operations,
    double LeadTimeHours,
    IReadOnlyList<string> CriticalPath);

/// <summary>
/// Kritikus út módszer (CPM) az ütemezési DAG-ra. A művelet időtartama a hívó által megadott
/// ProcessHours (egységidő × darab); a Humánerő kapacitás, nem időtartam-osztó. Pure static.
/// Ciklus esetén InvalidOperationException. A 0-darabszámú (kihagyott) műveleteket a hívó
/// hagyja ki a durations szótárból; az azokra mutató függőségeket a scheduler figyelmen kívül hagyja.
/// </summary>
public static class Scheduler
{
    private const double Eps = 1e-6;

    public static Schedule Build(IReadOnlyList<Operation> operations, IReadOnlyDictionary<string, double> durationsByOpId)
    {
        // Csak azok a műveletek, amikhez van időtartam (a 0-darabszámúak kimaradnak)
        var ops = operations.Where(o => durationsByOpId.ContainsKey(o.Id)).ToList();
        var byId = ops.ToDictionary(o => o.Id, o => o);

        // Élek: csak a jelenlévő műveletek közti függőségek
        IEnumerable<OperationDependency> DepsOf(Operation o) =>
            (o.DependsOn ?? Array.Empty<OperationDependency>()).Where(d => byId.ContainsKey(d.OnOperationId));

        // Kahn topo-rendezés (ciklus-detektálással)
        var indeg = ops.ToDictionary(o => o.Id, o => DepsOf(o).Count());
        var successors = ops.ToDictionary(o => o.Id, _ => new List<string>());
        foreach (var o in ops)
            foreach (var d in DepsOf(o))
                successors[d.OnOperationId].Add(o.Id);

        var queue = new Queue<string>(indeg.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var topo = new List<string>();
        var indegWork = new Dictionary<string, int>(indeg);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            topo.Add(id);
            foreach (var s in successors[id])
                if (--indegWork[s] == 0) queue.Enqueue(s);
        }
        if (topo.Count != ops.Count)
            throw new InvalidOperationException("Ciklus a művelet-függőségekben — a DAG nem ütemezhető.");

        // Forward pass: ES/EF
        var es = ops.ToDictionary(o => o.Id, _ => 0.0);
        var ef = ops.ToDictionary(o => o.Id, _ => 0.0);
        foreach (var id in topo)
        {
            double start = 0;
            foreach (var d in DepsOf(byId[id]))
            {
                double c = d.Type == DependencyType.FinishStart
                    ? ef[d.OnOperationId] + d.LagHours
                    : es[d.OnOperationId] + d.LagHours;
                start = Math.Max(start, c);
            }
            es[id] = start;
            ef[id] = start + durationsByOpId[id];
        }

        double leadTime = ops.Count > 0 ? ef.Values.Max() : 0;

        // Backward pass: LF/LS
        var lf = ops.ToDictionary(o => o.Id, _ => leadTime);
        var ls = ops.ToDictionary(o => o.Id, _ => leadTime);
        foreach (var id in Enumerable.Reverse(topo))
        {
            // ha nincs utódja, LF = leadTime
            var succs = successors[id];
            if (succs.Count > 0)
            {
                double late = double.PositiveInfinity;
                foreach (var s in succs)
                {
                    // az s művelet 'id'-től való függése
                    var dep = DepsOf(byId[s]).First(d => d.OnOperationId == id);
                    double c = dep.Type == DependencyType.FinishStart
                        ? ls[s] - dep.LagHours                         // id.LF <= s.LS - lag
                        : ls[s] - dep.LagHours + durationsByOpId[id];  // SS: id.LS <= s.LS - lag → id.LF <= s.LS - lag + dur(id)
                    late = Math.Min(late, c);
                }
                lf[id] = late;
            }
            ls[id] = lf[id] - durationsByOpId[id];
        }

        var scheduled = topo
            .Select(id =>
            {
                double slack = ls[id] - es[id];
                return new ScheduledOperation(
                    id, byId[id].Name, byId[id].Role, durationsByOpId[id],
                    es[id], ef[id], ls[id], lf[id], slack, Math.Abs(slack) < Eps);
            })
            .OrderBy(s => s.EarliestStartHours).ThenBy(s => s.OperationId)
            .ToList();

        var criticalPath = scheduled.Where(s => s.Critical)
            .OrderBy(s => s.EarliestStartHours)
            .Select(s => s.OperationId)
            .ToList();

        return new Schedule(scheduled, leadTime, criticalPath);
    }
}
