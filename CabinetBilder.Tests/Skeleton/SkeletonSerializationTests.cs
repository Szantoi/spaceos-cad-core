using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Linq;
using CabinetBilder.Core.Skeletons;

namespace CabinetBilder.Tests.Skeleton;

[TestClass]
public class SkeletonSerializationTests
{
    [TestMethod]
    public void Skeleton_ShouldRoundTripSerialize()
    {
        // Arrange
        var id = SkeletonId.New();
        var skeleton = new CabinetBilder.Core.Skeletons.Skeleton(id);
        skeleton.ApplyParameter("Width", 1200.5);
        skeleton.ClearComponents();
        skeleton.AddComponent(new SkeletonComponent 
        { 
            Name = "Side Panel", 
            Width = 720, 
            Height = 560, 
            Thickness = 18 
        });

        var options = new JsonSerializerOptions { IncludeFields = true };

        // Act
        var json = JsonSerializer.Serialize(skeleton, options);
        var deserialized = JsonSerializer.Deserialize<CabinetBilder.Core.Skeletons.Skeleton>(json, options);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(skeleton.Id.Value, deserialized.Id.Value);
        Assert.AreEqual(1200.5, (double)deserialized.Parameters.First(p => p.Key == "Width").Value, 0.001);
        Assert.AreEqual(1, deserialized.Components.Count);
        Assert.AreEqual("Side Panel", deserialized.Components.First().Name);
    }
}
