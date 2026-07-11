using Microsoft.VisualStudio.TestTools.UnitTesting;
using CabinetBilder.Core.Skeletons;
using System.Linq;

namespace CabinetBilder.Tests.Skeleton;

[TestClass]
public class SkeletonDomainTests
{
    [TestMethod]
    public void NewSkeleton_ShouldHaveDefaultParameters()
    {
        // Arrange
        var id = SkeletonId.New();

        // Act
        var skeleton = new CabinetBilder.Core.Skeletons.Skeleton(id);

        // Assert
        Assert.AreEqual(id, skeleton.Id);
        Assert.IsTrue(skeleton.Parameters.Any(p => p.Key == "Width"));
        Assert.IsTrue(skeleton.Parameters.Any(p => p.Key == "Height"));
        Assert.IsTrue(skeleton.Parameters.Any(p => p.Key == "Depth"));
    }

    [TestMethod]
    public void ApplyParameter_ShouldUpdateValue()
    {
        // Arrange
        var skeleton = new CabinetBilder.Core.Skeletons.Skeleton(SkeletonId.New());

        // Act
        var result = skeleton.ApplyParameter("Width", 1000.0);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        var widthParam = skeleton.Parameters.First(p => p.Key == "Width");
        Assert.AreEqual(1000.0, widthParam.Value);
    }

    [TestMethod]
    public void ApplyParameter_NonExistent_ShouldReturnFailure()
    {
        // Arrange
        var skeleton = new CabinetBilder.Core.Skeletons.Skeleton(SkeletonId.New());

        // Act
        var result = skeleton.ApplyParameter("InvalidKey", 123);

        // Assert
        Assert.IsFalse(result.IsSuccess);
    }
}
