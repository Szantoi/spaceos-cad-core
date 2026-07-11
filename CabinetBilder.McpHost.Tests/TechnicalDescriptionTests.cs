using System;
using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Docs;
using CabinetBilder.McpHost.Skeletons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class TechnicalDescriptionTests
{
    private static Dictionary<string, MaterialDto> Catalog() => new()
    {
        ["LAM18_W1000"] = new("LAM18_W1000", "Fehér laminált bútorlap 18mm", "Bútorlap", 18, "{\"finish\":\"laminalt\"}", 5200m),
        ["HDF3_WHITE"] = new("HDF3_WHITE", "Fehér HDF hátlap 3mm", "Hátlap", 3, "{\"finish\":\"hdf\"}", 1250m),
        ["ABS2_WHITE"] = new("ABS2_WHITE", "ABS élzáró fehér 2mm", "Élzáró", 2, "{\"unit\":\"fm\"}", 180m),
        ["MDF18_PAINT"] = new("MDF18_PAINT", "Festett MDF front 18mm", "Front", 18, "{\"finish\":\"festett\"}", 9400m),
    };

    private static TechnicalDescription GenerateDefault(out Skeleton s, params DesignIntent[] intents)
    {
        s = new Skeleton(SkeletonId.New()) { Name = "Teszt szekrény" };
        return TechnicalDescriptionGenerator.Generate(s, intents.ToList(), Catalog());
    }

    [TestMethod]
    public void Generate_OverallSize_UsesCorpusConvention()
    {
        var doc = GenerateDefault(out _);
        // Default: 600 × 720 × 560 (Szélesség × Magasság × Mélység)
        StringAssert.StartsWith(doc.OverallSizeMm, "600 × 720 × 560 mm");
    }

    [TestMethod]
    public void Generate_ListsMaterials_WithRoles()
    {
        var doc = GenerateDefault(out _);

        var roles = doc.Materials.Select(m => m.Role).ToList();
        CollectionAssert.Contains(roles, "Korpusz");
        CollectionAssert.Contains(roles, "Hátlap");
        CollectionAssert.Contains(roles, "Élzáró");
        Assert.AreEqual("Fehér laminált bútorlap 18mm", doc.Materials.First(m => m.Role == "Korpusz").MaterialName);
    }

    [TestMethod]
    public void Generate_StructuralDescription_MentionsBackOffsetAndEdging()
    {
        var doc = GenerateDefault(out _);

        StringAssert.Contains(doc.StructuralDescription, "5 mm-es beütéssel");
        StringAssert.Contains(doc.StructuralDescription, "élzártak");
        StringAssert.Contains(doc.StructuralDescription, "átmenő");
    }

    [TestMethod]
    public void Generate_SurfaceTreatment_LaminatedNeedsNone()
    {
        var doc = GenerateDefault(out _);
        StringAssert.Contains(doc.SurfaceTreatment.ToLowerInvariant(), "laminált");
        StringAssert.Contains(doc.SurfaceTreatment.ToLowerInvariant(), "nem igényelnek");
    }

    [TestMethod]
    public void Generate_PaintedCarcass_SurfaceTreatmentMentionsPaint()
    {
        var s = new Skeleton(SkeletonId.New());
        s.ApplyParameter("CarcassMaterialId", "MDF18_PAINT");

        var doc = TechnicalDescriptionGenerator.Generate(s, new List<DesignIntent>(), Catalog());

        StringAssert.Contains(doc.SurfaceTreatment.ToLowerInvariant(), "festett");
    }

    [TestMethod]
    public void Generate_IncludesDesignIntents_InMarkdown()
    {
        var doc = GenerateDefault(out _,
            new DesignIntent(DateTime.UtcNow, "Konyhai alsó elem", null),
            new DesignIntent(DateTime.UtcNow, "Vásárlói kérésre szélesítve", "Width"));

        Assert.AreEqual(2, doc.DesignIntents.Count);
        StringAssert.Contains(doc.DesignIntents[1], "(paraméter: Width)");
        StringAssert.Contains(doc.Markdown, "## Tervezői szándékok");
        StringAssert.Contains(doc.Markdown, "Konyhai alsó elem");
    }

    [TestMethod]
    public void Generate_Markdown_HasAllTextbookSections()
    {
        var doc = GenerateDefault(out _);

        StringAssert.Contains(doc.Markdown, "# Műszaki leírás — Teszt szekrény");
        StringAssert.Contains(doc.Markdown, "**Befoglaló méret:**");
        StringAssert.Contains(doc.Markdown, "## Felhasznált anyagok");
        StringAssert.Contains(doc.Markdown, "## Szerkezeti felépítés");
        StringAssert.Contains(doc.Markdown, "## Felületkezelés");
    }
}
