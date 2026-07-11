using System;
using CabinetBilder.McpHost.Production;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class WorkCalendarTests
{
    // 2026-07-13 hétfő; 07-17 péntek; 07-11 szombat (a rendszer szerint 07-10 péntek)
    private static readonly DateTime Mon = new(2026, 7, 13, 8, 0, 0);
    private static readonly DateTime Fri = new(2026, 7, 17, 8, 0, 0);
    private static readonly DateTime Sat = new(2026, 7, 11, 8, 0, 0);

    [TestMethod]
    public void AtWorkHours_WithinDay()
    {
        var cal = new WorkCalendar(Mon, 8);
        Assert.AreEqual(new DateTime(2026, 7, 13, 10, 0, 0), cal.AtWorkHours(2));
    }

    [TestMethod]
    public void AtWorkHours_EndOfDay_IsExactlyWorkdayEnd()
    {
        var cal = new WorkCalendar(Mon, 8);
        Assert.AreEqual(new DateTime(2026, 7, 13, 16, 0, 0), cal.AtWorkHours(8));
    }

    [TestMethod]
    public void AtWorkHours_CrossesToNextDay()
    {
        var cal = new WorkCalendar(Mon, 8);
        // 8h kitölti a hétfőt, +2h kedd
        Assert.AreEqual(new DateTime(2026, 7, 14, 10, 0, 0), cal.AtWorkHours(10));
    }

    [TestMethod]
    public void AtWorkHours_SkipsWeekend()
    {
        var cal = new WorkCalendar(Fri, 8);
        // 8h kitölti a pénteket, +2h a következő MUNKANAP (hétfő), nem szombat
        Assert.AreEqual(new DateTime(2026, 7, 20, 10, 0, 0), cal.AtWorkHours(10));
    }

    [TestMethod]
    public void Start_OnWeekend_AlignsToNextMonday()
    {
        var cal = new WorkCalendar(Sat, 8);
        Assert.AreEqual(new DateTime(2026, 7, 13, 8, 0, 0), cal.Start);
    }
}
