using Microsoft.VisualStudio.TestTools.UnitTesting;
using CabinetBilder.Core.Skeletons;
using System.Linq;

namespace CabinetBilder.Tests.Skeletons;

[TestClass]
public class SkeletonLogicTests
{
    [TestMethod]
    public void Skeleton_Initializes_WithDefaultComponents()
    {
        var id = new CabinetBilder.Core.Skeletons.SkeletonId(Guid.NewGuid());
        var skeleton = new CabinetBilder.Core.Skeletons.Skeleton(id);

        Assert.AreEqual(5, skeleton.Components.Count);
        Assert.IsTrue(skeleton.Components.Any(c => c.Name == "Side Left"));
        Assert.IsTrue(skeleton.Components.Any(c => c.Name == "Bottom"));
    }

    [TestMethod]
    public void Skeleton_ApplyingWidth_UpdatesComponentDimensions()
    {
        var id = new CabinetBilder.Core.Skeletons.SkeletonId(Guid.NewGuid());
        var skeleton = new CabinetBilder.Core.Skeletons.Skeleton(id);
        
        // Initial width is 600
        var bottom = skeleton.Components.First(c => c.Name == "Bottom");
        var thickness = 18.0;
        Assert.AreEqual(600 - (2 * thickness), bottom.Width);

        // Update width to 1000
        skeleton.ApplyParameter("Width", 1000.0);
        
        var updatedBottom = skeleton.Components.First(c => c.Name == "Bottom");
        Assert.AreEqual(1000 - (2 * thickness), updatedBottom.Width);
        
        var updatedRightSide = skeleton.Components.First(c => c.Name == "Side Right");
        Assert.AreEqual(1000 - thickness, updatedRightSide.PosX);
    }

    [TestMethod]
    public void Skeleton_ComputeBom_ReturnsCorrectLines()
    {
        var id = new CabinetBilder.Core.Skeletons.SkeletonId(Guid.NewGuid());
        var skeleton = new CabinetBilder.Core.Skeletons.Skeleton(id);
        
        var bom = skeleton.ComputeBom().ToList();
        
        Assert.AreEqual(5, bom.Count);
        Assert.IsTrue(bom.All(b => b.Quantity == 1));
        var leftSide = bom.First(b => b.Name == "Side Left");
        Assert.AreEqual(720.0, leftSide.Length); // Height
        Assert.AreEqual(560.0, leftSide.Width);  // Depth
    }
}
