using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text.Json;
using CabinetBilder.Core.Machining;
using CabinetBilder.Core.Skeletons;

namespace CabinetBilder.Core.Tests.Machining;

[TestClass]
public class MachiningSerializationTests
{
    // Same options as AutoCadSkeletonStore uses for DWG XRecord persistence.
    private static readonly JsonSerializerOptions StoreOptions = new() { IncludeFields = true };

    [TestMethod]
    public void GrooveOperation_ShouldRoundTripSerialize()
    {
        var groove = new GrooveOperation
        {
            Name = "Backpanel Groove",
            X = 0, Y = 700, Z = 0,
            Width = 4.0, Depth = 10.0, Length = 600.0,
            DirectionX = 1, DirectionY = 0, DirectionZ = 0,
            IsThrough = false
        };

        var json = JsonSerializer.Serialize<MachiningOperation>(groove, StoreOptions);
        var deserialized = JsonSerializer.Deserialize<MachiningOperation>(json, StoreOptions);

        Assert.IsInstanceOfType(deserialized, typeof(GrooveOperation));
        Assert.AreEqual(groove, (GrooveOperation)deserialized!);
    }

    [TestMethod]
    public void Skeleton_WithMixedOperations_ShouldRoundTripThroughStoreSerialization()
    {
        var skeleton = new Skeleton(SkeletonId.New());
        skeleton.ClearComponents();

        var panel = new SkeletonComponent
        {
            Name = "Side Left",
            Width = 560, Height = 720, Thickness = 18
        };
        panel.Operations.Add(new DrillOperation
        {
            Name = "Dowel Face",
            X = 37, Y = 9, Z = 18,
            Diameter = 8.0, Depth = 12.0
        });
        panel.Operations.Add(new GrooveOperation
        {
            Name = "Backpanel Groove",
            X = 0, Y = 700, Z = 0,
            Width = 4.0, Depth = 10.0, Length = 560.0,
            DirectionX = 1
        });
        skeleton.AddComponent(panel);

        var json = JsonSerializer.Serialize(skeleton, StoreOptions);
        var deserialized = JsonSerializer.Deserialize<Skeleton>(json, StoreOptions);

        Assert.IsNotNull(deserialized);
        var operations = deserialized.Components.Single().Operations;
        Assert.AreEqual(2, operations.Count);

        var drill = operations.OfType<DrillOperation>().Single();
        Assert.AreEqual(8.0, drill.Diameter, 0.001);
        Assert.AreEqual(12.0, drill.Depth, 0.001);

        var groove = operations.OfType<GrooveOperation>().Single();
        Assert.AreEqual(4.0, groove.Width, 0.001);
        Assert.AreEqual(10.0, groove.Depth, 0.001);
        Assert.AreEqual(560.0, groove.Length, 0.001);
        Assert.IsFalse(groove.IsThrough);
    }

    [TestMethod]
    public void GrooveOperation_Json_ContainsTypeDiscriminator()
    {
        var groove = new GrooveOperation { Name = "G", Width = 4, Depth = 10, Length = 100 };

        var json = JsonSerializer.Serialize<MachiningOperation>(groove, StoreOptions);

        StringAssert.Contains(json, "\"$type\":\"groove\"");
    }
}
