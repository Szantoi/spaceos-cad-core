using CabinetBilder.Core.SmartObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.SmartObjects;

[TestClass]
public sealed class SmartObjectSchemaTests
{
    [TestMethod]
    public void IsSchemaMatch_DefaultSchema_MatchesIgnoringCase()
    {
        bool result = SmartObjectSchema.IsSchemaMatch("butor_v1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsSchemaMatch_NullOrWhiteSpace_ReturnsFalse()
    {
        Assert.IsFalse(SmartObjectSchema.IsSchemaMatch(null));
        Assert.IsFalse(SmartObjectSchema.IsSchemaMatch(string.Empty));
        Assert.IsFalse(SmartObjectSchema.IsSchemaMatch("   "));
    }

    [TestMethod]
    [DataRow("Schema_A", "Schema_A", true)]
    [DataRow("schema_a", "Schema_A", true)]
    [DataRow("Schema_B", "Schema_A", false)]
    public void IsSchemaMatch_CustomExpectedSchema_ReturnsExpected(string actualSchema, string expectedSchema, bool expected)
    {
        bool result = SmartObjectSchema.IsSchemaMatch(actualSchema, expectedSchema);

        Assert.AreEqual(expected, result);
    }
}

