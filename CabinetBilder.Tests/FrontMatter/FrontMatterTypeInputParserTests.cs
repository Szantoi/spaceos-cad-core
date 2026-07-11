using CabinetBilder.Core.FrontMatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.FrontMatter;

[TestClass]
public sealed class FrontMatterTypeInputParserTests
{
    [TestMethod]
    public void Parse_EmptyInput_SelectsFirstType()
    {
        List<string> knownTypes = ["Szabaszat", "CNC"];

        FrontMatterTypeInputParseResult result = FrontMatterTypeInputParser.Parse(string.Empty, knownTypes, allowCreateNewType: true);

        Assert.AreEqual(FrontMatterTypeInputAction.SelectExisting, result.Action);
        Assert.AreEqual("Szabaszat", result.SelectedType);
    }

    [TestMethod]
    public void Parse_RefreshInput_ReturnsRefreshAction()
    {
        FrontMatterTypeInputParseResult result = FrontMatterTypeInputParser.Parse("R", ["Szabaszat"], allowCreateNewType: true);

        Assert.AreEqual(FrontMatterTypeInputAction.Refresh, result.Action);
    }

    [TestMethod]
    public void Parse_NewInput_WhenAllowed_ReturnsCreateNewAction()
    {
        FrontMatterTypeInputParseResult result = FrontMatterTypeInputParser.Parse("N", ["Szabaszat"], allowCreateNewType: true);

        Assert.AreEqual(FrontMatterTypeInputAction.CreateNew, result.Action);
    }

    [TestMethod]
    public void Parse_NewInput_WhenNotAllowed_ReturnsInvalid()
    {
        FrontMatterTypeInputParseResult result = FrontMatterTypeInputParser.Parse("N", ["Szabaszat"], allowCreateNewType: false);

        Assert.AreEqual(FrontMatterTypeInputAction.Invalid, result.Action);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }

    [TestMethod]
    public void Parse_IndexInput_ReturnsMatchingType()
    {
        FrontMatterTypeInputParseResult result = FrontMatterTypeInputParser.Parse("2", ["Szabaszat", "CNC"], allowCreateNewType: true);

        Assert.AreEqual(FrontMatterTypeInputAction.SelectExisting, result.Action);
        Assert.AreEqual("CNC", result.SelectedType);
    }

    [TestMethod]
    public void Parse_ExactNameInput_ReturnsMatchingType()
    {
        FrontMatterTypeInputParseResult result = FrontMatterTypeInputParser.Parse("cnc", ["Szabaszat", "CNC"], allowCreateNewType: true);

        Assert.AreEqual(FrontMatterTypeInputAction.SelectExisting, result.Action);
        Assert.AreEqual("CNC", result.SelectedType);
    }

    [TestMethod]
    public void Parse_PrefixInputWithSingleMatch_ReturnsMatchingType()
    {
        FrontMatterTypeInputParseResult result = FrontMatterTypeInputParser.Parse("Sza", ["Szabaszat", "CNC"], allowCreateNewType: true);

        Assert.AreEqual(FrontMatterTypeInputAction.SelectExisting, result.Action);
        Assert.AreEqual("Szabaszat", result.SelectedType);
    }

    [TestMethod]
    public void Parse_PrefixInputWithMultipleMatches_ReturnsInvalid()
    {
        FrontMatterTypeInputParseResult result = FrontMatterTypeInputParser.Parse("Sz", ["Szabaszat", "Szalag"], allowCreateNewType: true);

        Assert.AreEqual(FrontMatterTypeInputAction.Invalid, result.Action);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }
}

