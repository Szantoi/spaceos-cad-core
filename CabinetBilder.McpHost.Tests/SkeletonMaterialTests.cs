using System.Linq;
using CabinetBilder.Core.Skeletons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class SkeletonMaterialTests
{
    private static Skeleton NewSkeleton() => new(SkeletonId.New());

    [TestMethod]
    public void Create_AssignsDefaultMaterials_ToComponents()
    {
        var s = NewSkeleton();
        var comps = s.Components.ToDictionary(c => c.Name);

        Assert.AreEqual("LAM18_W1000", comps["Side Left"].MaterialId);
        Assert.AreEqual("LAM18_W1000", comps["Side Right"].MaterialId);
        Assert.AreEqual("LAM18_W1000", comps["Bottom"].MaterialId);
        Assert.AreEqual("LAM18_W1000", comps["Top"].MaterialId);
        Assert.AreEqual("HDF3_WHITE", comps["Back"].MaterialId);
    }

    [TestMethod]
    public void ApplyParameter_Width_RebuildsAndKeepsMaterials()
    {
        var s = NewSkeleton();
        var result = s.ApplyParameter("Width", 800.0);

        Assert.IsTrue(result.IsSuccess);
        // A jobb oldal pozíciója a szélességtől függ (parametrikus Rebuild): PosX = w - t = 800 - 18
        var right = s.Components.First(c => c.Name == "Side Right");
        Assert.AreEqual(782.0, right.PosX, 0.001);
        // Az anyag változatlan marad a Rebuild után
        Assert.AreEqual("LAM18_W1000", right.MaterialId);
    }

    [TestMethod]
    public void SetCarcassMaterial_UpdatesCarcassComponents_LeavesBackUnchanged()
    {
        var s = NewSkeleton();
        var result = s.ApplyParameter("CarcassMaterialId", "LAM18_SONOMA");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("LAM18_SONOMA", s.Components.First(c => c.Name == "Side Left").MaterialId);
        Assert.AreEqual("LAM18_SONOMA", s.Components.First(c => c.Name == "Top").MaterialId);
        Assert.AreEqual("HDF3_WHITE", s.Components.First(c => c.Name == "Back").MaterialId);
    }

    [TestMethod]
    public void ComputeBom_CarriesMaterialId_ForEachLine()
    {
        var s = NewSkeleton();
        var bom = s.ComputeBom().ToList();

        Assert.AreEqual(5, bom.Count);
        Assert.IsTrue(bom.All(b => !string.IsNullOrEmpty(b.MaterialId)));
        Assert.AreEqual("HDF3_WHITE", bom.First(b => b.Name == "Back").MaterialId);
    }

    [TestMethod]
    public void ApplyParameter_UnknownKey_ReturnsFailure()
    {
        var s = NewSkeleton();
        var result = s.ApplyParameter("Nonexistent", 1.0);

        Assert.IsTrue(result.IsFailure);
        Assert.IsNotNull(result.ErrorMessage);
    }
}
