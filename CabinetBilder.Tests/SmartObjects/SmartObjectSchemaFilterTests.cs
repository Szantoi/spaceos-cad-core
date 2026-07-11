using CabinetBilder.Core.SmartObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.SmartObjects;

[TestClass]
public sealed class SmartObjectSchemaFilterTests
{
    [TestMethod]
    public void ShouldInclude_NoSchemaMarker_ReturnsTrue()
    {
        bool result = SmartObjectSchemaFilter.ShouldInclude(hasSchemaMarker: false, schemaId: null);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldInclude_WithSchemaMarkerAndMatchingDefaultSchema_ReturnsTrue()
    {
        bool result = SmartObjectSchemaFilter.ShouldInclude(hasSchemaMarker: true, schemaId: "butor_v1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldInclude_WithSchemaMarkerAndMismatchingSchema_ReturnsFalse()
    {
        bool result = SmartObjectSchemaFilter.ShouldInclude(hasSchemaMarker: true, schemaId: "legacy_v1");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldInclude_WithCustomExpectedSchema_UsesExpectedValue()
    {
        bool matched = SmartObjectSchemaFilter.ShouldInclude(hasSchemaMarker: true, schemaId: "schema_a", expectedSchemaId: "Schema_A");
        bool mismatched = SmartObjectSchemaFilter.ShouldInclude(hasSchemaMarker: true, schemaId: "schema_b", expectedSchemaId: "Schema_A");

        Assert.IsTrue(matched);
        Assert.IsFalse(mismatched);
    }
}

