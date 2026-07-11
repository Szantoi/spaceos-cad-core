using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Export;
using CabinetBilder.McpHost.Skeletons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class ProjectExporterTests
{
    private static Dictionary<string, MaterialDto> Catalog() => new()
    {
        ["LAM18_W1000"] = new("LAM18_W1000", "Fehér laminált bútorlap 18mm", "Bútorlap", 18, "{\"color\":\"feher\",\"finish\":\"laminalt\"}", 5200m),
        ["HDF3_WHITE"] = new("HDF3_WHITE", "Fehér HDF hátlap 3mm", "Hátlap", 3, "{\"color\":\"feher\",\"finish\":\"hdf\"}", 1250m),
        ["ABS2_WHITE"] = new("ABS2_WHITE", "ABS élzáró fehér 2mm", "Élzáró", 2, "{\"unit\":\"fm\"}", 180m),
    };

    private static IReadOnlyDictionary<string, string> Build(out Skeleton s)
    {
        s = new Skeleton(SkeletonId.New()) { Name = "Teszt" };
        return ProjectExporter.BuildFiles(s, new List<DesignIntent>(), Catalog(),
            new ExportOptions("26144", AllowanceMm: 10, LaborHours: 16, HourlyRate: 5000));
    }

    [TestMethod]
    public void BuildFiles_ProducesAllFiveFiles()
    {
        var files = Build(out _);
        CollectionAssert.AreEquivalent(
            new[] { "Szabaszat.csv", "Mennyisegek.csv", "Kalkulacio.csv", "Muszaki-Leiras.md", "export.json" },
            files.Keys.ToArray());
    }

    [TestMethod]
    public void Szabaszat_Header_MatchesRealPqSchema()
    {
        var files = Build(out _);
        var firstLine = files["Szabaszat.csv"].Split("\r\n")[0];
        Assert.AreEqual(
            "DSMR;Sorszám;Hosszúság;Szélesség;Darab;Név;Megjegyzés;Tipus;Alkatrész Megnevezése;Anyag;Vastagság;Felület tipus;Szín;Minta",
            firstLine);
    }

    [TestMethod]
    public void Szabaszat_UsesHungarianDecimalComma_AndAllowance()
    {
        var files = Build(out _);
        var lines = files["Szabaszat.csv"].Split("\r\n");
        // Bal oldal: kész 560 mély × 720 magas; a cutLength = max? A ComputeBom Length=Height=720 -> +2*10 = 740
        var sideLeft = lines.First(l => l.Contains("Bal oldal"));
        var cols = sideLeft.Split(';');
        // Hosszúság oszlop (index 2) = 740 (cut), Alkatrész (8) = Bal oldal
        Assert.AreEqual("740", cols[2]);
        Assert.AreEqual("Bal oldal", cols[8]);
        Assert.AreEqual("laminált", cols[11]);   // Felület tipus
        Assert.AreEqual("Fehér", cols[12]);       // Szín
    }

    [TestMethod]
    public void Szabaszat_DecimalUsesComma_NotDot()
    {
        // Vastagság 2.5-öt vessződ formában kell kiírni HU-ban
        var s = new Skeleton(SkeletonId.New());
        s.ApplyParameter("Thickness", 2.5);
        var files = ProjectExporter.BuildFiles(s, new List<DesignIntent>(), Catalog(), new ExportOptions("X"));
        var side = files["Szabaszat.csv"].Split("\r\n").First(l => l.Contains("Bal oldal"));
        Assert.IsTrue(side.Contains("2,5"), "A vastagságnak tizedesvesszővel kell szerepelnie (2,5).");
        Assert.IsFalse(side.Contains("2.5"), "Nem lehet tizedespont.");
    }

    [TestMethod]
    public void Mennyisegek_Header_MatchesRealPqSchema()
    {
        var files = Build(out _);
        var firstLine = files["Mennyisegek.csv"].Split("\r\n")[0];
        Assert.AreEqual("DSMR;Sorszám;Alkatrész Megnevezése;Anyag;Vastagság;Szélesség;Hosszúság;Darab;Szín", firstLine);
    }

    [TestMethod]
    public void Kalkulacio_ContainsElevenSteps_WithNetPrice()
    {
        var files = Build(out _);
        var lines = files["Kalkulacio.csv"].Split("\r\n").Where(l => l.Length > 0).ToArray();
        Assert.AreEqual(12, lines.Length); // fejléc + 11 lépés
        Assert.IsTrue(lines.Any(l => l.StartsWith("11;Bruttó eladási ár;")));
    }

    [TestMethod]
    public void PartHu_MapsEnglishNamesToHungarian()
    {
        Assert.AreEqual("Hátlap", ProjectExporter.PartHu("Back"));
        Assert.AreEqual("Fedél", ProjectExporter.PartHu("Top"));
        Assert.AreEqual("Ismeretlen", ProjectExporter.PartHu("Ismeretlen")); // fallback: változatlan
    }
}
