using CabinetBilder.Core.FrontMatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.FrontMatter;

[TestClass]
public sealed class FrontMatterTextServiceTests
{
    [TestMethod]
    public void ParseEntries_ValidFrontMatter_ReturnsExpectedValues()
    {
        string input = "---\nType: Szabaszat;\nBlock_Id: AB12;\nMaterial: Bukk;\n---";

        Dictionary<string, string> result = FrontMatterTextService.ParseEntries(input);

        Assert.AreEqual("Szabaszat", result[FrontMatterKeys.Type]);
        Assert.AreEqual("AB12", result[FrontMatterKeys.BlockId]);
        Assert.AreEqual("Bukk", result[FrontMatterKeys.Material]);
    }

    [TestMethod]
    public void BuildTemplate_MissingOptionalValues_UsesEmptyStrings()
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase)
        {
            [FrontMatterKeys.Type] = "Szabaszat",
            [FrontMatterKeys.BlockId] = "AB12"
        };

        string result = FrontMatterTextService.BuildTemplate(values);

        Assert.IsTrue(result.Contains("Type: Szabaszat;", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("Block_Id: AB12;", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("Material: ;", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildColumns_WithKnownAndCustomKeys_OrdersAsExpected()
    {
        List<IReadOnlyDictionary<string, string>> rows =
        [
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [FrontMatterKeys.Name] = "Elem1",
                [FrontMatterKeys.Type] = "Szabaszat",
                ["Supplier"] = "A"
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [FrontMatterKeys.Layer] = "L1",
                [FrontMatterKeys.Material] = "Bukk"
            }
        ];

        List<string> columns = FrontMatterTextService.BuildColumns(rows);

        Assert.AreEqual(5, columns.Count);
        Assert.AreEqual(FrontMatterKeys.Type, columns[0]);
        Assert.AreEqual(FrontMatterKeys.Layer, columns[1]);
        Assert.AreEqual(FrontMatterKeys.Name, columns[2]);
        Assert.AreEqual(FrontMatterKeys.Material, columns[3]);
        Assert.AreEqual("Supplier", columns[4]);
    }

    [TestMethod]
    [DataRow(12.3456, 2, "12.35")]
    [DataRow(12.0, 0, "12")]
    public void FormatNumeric_UsesInvariantFixedPrecision(double value, int precision, string expected)
    {
        string result = FrontMatterTextService.FormatNumeric(value, precision);

        Assert.AreEqual(expected, result);
    }
}

