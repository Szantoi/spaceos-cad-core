using System;
using System.Collections.Generic;

namespace CabinetBilder.McpHost.Production;

/// <summary>
/// Munkanaptár: munkaóra → naptári dátum leképzés. A munka csak munkanapon, a napi
/// munkaidőn belül halad; a nem-munkanapok (alapból hétvége) kimaradnak. Pure, determinisztikus
/// (a kezdő dátum input — nincs "most" függés).
/// </summary>
public sealed class WorkCalendar
{
    private const double Eps = 1e-9;

    public DateTime Start { get; }
    public double WorkdayHours { get; }
    public IReadOnlySet<DayOfWeek> WorkingDays { get; }

    public WorkCalendar(DateTime start, double workdayHours = 8, IReadOnlySet<DayOfWeek>? workingDays = null)
    {
        if (workdayHours <= 0) throw new ArgumentOutOfRangeException(nameof(workdayHours));
        WorkdayHours = workdayHours;
        WorkingDays = workingDays ?? new HashSet<DayOfWeek>
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
        };
        if (WorkingDays.Count == 0) throw new ArgumentException("Legalább egy munkanap kell.", nameof(workingDays));
        // A kezdetet a legközelebbi munkanap munkaidő-kezdetére igazítjuk (a Start időpontja = napi kezdés).
        Start = AlignToWorkday(start);
    }

    private DateTime AlignToWorkday(DateTime d)
    {
        while (!WorkingDays.Contains(d.DayOfWeek))
            d = d.Date.AddDays(1).Add(d.TimeOfDay);
        return d;
    }

    private DateTime NextWorkdayStart(DateTime d)
    {
        var next = d.Date.AddDays(1).Add(Start.TimeOfDay);
        while (!WorkingDays.Contains(next.DayOfWeek))
            next = next.Date.AddDays(1).Add(Start.TimeOfDay);
        return next;
    }

    /// <summary>Adott eltelt munkaóra (a Start-tól) naptári időpontja.</summary>
    public DateTime AtWorkHours(double workHours)
    {
        if (workHours < 0) throw new ArgumentOutOfRangeException(nameof(workHours));
        var day = Start;
        double rem = workHours;
        while (rem > WorkdayHours + Eps)
        {
            rem -= WorkdayHours;
            day = NextWorkdayStart(day);
        }
        return day.AddHours(rem);
    }
}
