using System;
using System.Linq;
using System.Text.Json;
using CabinetBilder.McpHost.Skeletons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class SkeletonRegistryTests
{
    [TestMethod]
    public void Create_And_TryGet_RoundTrips()
    {
        var reg = new SkeletonRegistry();
        var id = Guid.NewGuid();

        var entry = reg.Create(id, "Test Cabinet", null);

        Assert.AreEqual(id, entry.Skeleton.Id.Value);
        Assert.AreEqual(1, reg.Count);
        Assert.IsTrue(reg.TryGet(id, out var found));
        Assert.AreSame(entry, found);
    }

    [TestMethod]
    public void Create_WithIntent_StoresIntent()
    {
        var reg = new SkeletonRegistry();
        var entry = reg.Create(null, "X", "Konyhai alsó elem");

        Assert.AreEqual(1, entry.Intents.Count);
        Assert.AreEqual("Konyhai alsó elem", entry.Intents[0].Intent);
    }

    [TestMethod]
    public void TryGet_UnknownId_ReturnsFalse()
    {
        var reg = new SkeletonRegistry();
        Assert.IsFalse(reg.TryGet(Guid.NewGuid(), out _));
    }

    [TestMethod]
    public void ToDto_ExposesParametersComponentsIntents()
    {
        var reg = new SkeletonRegistry();
        var entry = reg.Create(null, "X", "szándék");

        var dto = SkeletonRegistry.ToDto(entry);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto));
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("parameters").GetArrayLength() >= 5);
        Assert.AreEqual(5, root.GetProperty("components").GetArrayLength());
        Assert.AreEqual(1, root.GetProperty("intents").GetArrayLength());
        Assert.AreEqual("szándék", root.GetProperty("intents")[0].GetProperty("intent").GetString());
    }
}
