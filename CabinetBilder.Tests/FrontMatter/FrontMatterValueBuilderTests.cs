using CabinetBilder.Core.FrontMatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.FrontMatter;

[TestClass]
public sealed class FrontMatterValueBuilderTests
{
    [TestMethod]
    public void BuildValues_ValidInput_ContainsCanonicalAndDynamicFields()
    {
        Dictionary<string, string> dynamicValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Length"] = "1200.00",
            ["Width"] = "400.00",
            ["Custom"] = "X"
        };

        Dictionary<string, string> result = FrontMatterValueBuilder.BuildValues("Szabaszat", "AB12", "PanelA", dynamicValues);

        Assert.AreEqual("Szabaszat", result[FrontMatterKeys.Type]);
        Assert.AreEqual("AB12", result[FrontMatterKeys.BlockId]);
        Assert.AreEqual("PanelA", result[FrontMatterKeys.Name]);
        Assert.AreEqual("1200.00", result[FrontMatterKeys.LengthCut]);
        Assert.AreEqual("400.00", result[FrontMatterKeys.WidthCut]);
        Assert.AreEqual("X", result["Custom"]);
    }

    [TestMethod]
    public void BuildValues_HungarianPropertyNames_MapsCutValues()
    {
        Dictionary<string, string> dynamicValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["HosszĂşsĂˇg"] = "845.5",
            ["SzĂ©lessĂ©g"] = "233.2"
        };

        Dictionary<string, string> result = FrontMatterValueBuilder.BuildValues("Szabaszat", "AB13", "PanelB", dynamicValues);

        Assert.AreEqual("845.5", result[FrontMatterKeys.LengthCut]);
        Assert.AreEqual("233.2", result[FrontMatterKeys.WidthCut]);
    }

    [TestMethod]
    public void BuildValues_MissingOptionalProperties_UsesEmptyDefaults()
    {
        Dictionary<string, string> dynamicValues = new(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> result = FrontMatterValueBuilder.BuildValues("Szabaszat", "AB14", "PanelC", dynamicValues);

        Assert.AreEqual(string.Empty, result[FrontMatterKeys.Material]);
        Assert.AreEqual(string.Empty, result[FrontMatterKeys.Quantity]);
        Assert.AreEqual(string.Empty, result[FrontMatterKeys.LengthCut]);
        Assert.AreEqual(string.Empty, result[FrontMatterKeys.WidthCut]);
    }
}

