using CabinetBilder.McpHost.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class MaterialFinishTests
{
    [TestMethod]
    public void FromBodyJson_Festett_ReturnsFestett()
        => Assert.AreEqual("festett", MaterialFinish.FromBodyJson("{\"finish\":\"festett\"}"));

    [TestMethod]
    public void FromBodyJson_Folias_NormalizesToAccented()
        => Assert.AreEqual("fóliás", MaterialFinish.FromBodyJson("{\"finish\":\"folias\"}"));

    [TestMethod]
    public void FromBodyJson_Laminalt_NormalizesToAccented()
        => Assert.AreEqual("laminált", MaterialFinish.FromBodyJson("{\"color\":\"feher\",\"finish\":\"laminalt\"}"));

    [TestMethod]
    public void FromBodyJson_Hdf_ReturnsBackLabel()
        => Assert.AreEqual("hdf hátlap", MaterialFinish.FromBodyJson("{\"finish\":\"hdf\"}"));

    [TestMethod]
    public void FromBodyJson_NoFinishKey_ReturnsUnknown()
        => Assert.AreEqual(MaterialFinish.Unknown, MaterialFinish.FromBodyJson("{\"color\":\"feher\"}"));

    [TestMethod]
    public void FromBodyJson_InvalidJson_ReturnsUnknown()
        => Assert.AreEqual(MaterialFinish.Unknown, MaterialFinish.FromBodyJson("nem json"));

    [TestMethod]
    public void FromBodyJson_NullOrEmpty_ReturnsUnknown()
    {
        Assert.AreEqual(MaterialFinish.Unknown, MaterialFinish.FromBodyJson(null));
        Assert.AreEqual(MaterialFinish.Unknown, MaterialFinish.FromBodyJson(""));
    }
}
