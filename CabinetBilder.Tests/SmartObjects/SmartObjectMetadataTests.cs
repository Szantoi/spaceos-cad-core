using CabinetBilder.Core.SmartObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CabinetBilder.Tests.SmartObjects;

[TestClass]
public class SmartObjectMetadataTests
{
    [TestMethod]
    public void Empty_ShouldHaveNoFields()
    {
        SmartObjectMetadata metadata = SmartObjectMetadata.Empty;
        Assert.AreEqual(0, metadata.Fields.Count);
    }

    [TestMethod]
    public void From_ShouldCopyAllFields()
    {
        var source = new Dictionary<string, string>
        {
            { SmartObjectMetadataKeys.ObjectType, "Asztalos" },
            { SmartObjectMetadataKeys.Label, "AlsĂł szekrĂ©ny" }
        };

        SmartObjectMetadata metadata = SmartObjectMetadata.From(source);

        Assert.AreEqual(2, metadata.Fields.Count);
        Assert.IsTrue(metadata.TryGetValue(SmartObjectMetadataKeys.ObjectType, out string typeVal));
        Assert.AreEqual("Asztalos", typeVal);
    }

    [TestMethod]
    public void TryGetValue_ShouldBeCaseInsensitive()
    {
        SmartObjectMetadata metadata = SmartObjectMetadata.From(
            new Dictionary<string, string> { { "objecttype", "FurnĂ©r" } });

        Assert.IsTrue(metadata.TryGetValue("ObjectType", out string val));
        Assert.AreEqual("FurnĂ©r", val);
    }

    [TestMethod]
    public void TryGetValue_ShouldReturnFalse_WhenKeyMissing()
    {
        SmartObjectMetadata metadata = SmartObjectMetadata.Empty;
        bool found = metadata.TryGetValue(SmartObjectMetadataKeys.Label, out string val);

        Assert.IsFalse(found);
        Assert.IsNull(val);
    }

    [TestMethod]
    public void With_ShouldReturnNewInstance_AndNotMutateOriginal()
    {
        SmartObjectMetadata original = SmartObjectMetadata.Empty;
        SmartObjectMetadata updated = original.With(SmartObjectMetadataKeys.Label, "FelsĹ‘ szekrĂ©ny");

        Assert.IsFalse(original.TryGetValue(SmartObjectMetadataKeys.Label, out _));
        Assert.IsTrue(updated.TryGetValue(SmartObjectMetadataKeys.Label, out string val));
        Assert.AreEqual("FelsĹ‘ szekrĂ©ny", val);
    }

    [TestMethod]
    public void With_ShouldOverwriteExistingKey()
    {
        SmartObjectMetadata metadata = SmartObjectMetadata.From(
            new Dictionary<string, string> { { SmartObjectMetadataKeys.ObjectType, "Original" } });

        SmartObjectMetadata updated = metadata.With(SmartObjectMetadataKeys.ObjectType, "Updated");

        Assert.IsTrue(updated.TryGetValue(SmartObjectMetadataKeys.ObjectType, out string val));
        Assert.AreEqual("Updated", val);
    }

    [TestMethod]
    public void From_ShouldThrow_WhenFieldsIsNull()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            SmartObjectMetadata.From(null!));
    }
}

