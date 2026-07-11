using System.Linq;
using CabinetBilder.McpHost.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreResult = CabinetBilder.Core.Common.Result;
using ArdalisResult = Ardalis.Result.Result;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class ResultMarshallingTests
{
    [TestMethod]
    public void CoreResult_Success_MapsToOk()
    {
        var resp = CoreResult.Success().ToMcpResponse();
        Assert.IsTrue(resp.IsSuccess);
        Assert.AreEqual("Ok", resp.Status);
        Assert.AreEqual(0, resp.Errors.Count);
    }

    [TestMethod]
    public void CoreResult_Failure_MapsToErrorWithMessage()
    {
        var resp = CoreResult.Failure("valami hiba").ToMcpResponse();
        Assert.IsFalse(resp.IsSuccess);
        Assert.AreEqual("Error", resp.Status);
        CollectionAssert.Contains(resp.Errors, "valami hiba");
    }

    [TestMethod]
    public void CoreResultGeneric_Success_CarriesValue()
    {
        var resp = CoreResult.Success(42).ToMcpResponse();
        Assert.IsTrue(resp.IsSuccess);
        Assert.AreEqual(42, resp.Value);
    }

    [TestMethod]
    public void ArdalisResult_NotFound_MapsStatus()
    {
        var resp = ArdalisResult.NotFound("nincs").ToMcpResponse();
        Assert.IsFalse(resp.IsSuccess);
        Assert.AreEqual("NotFound", resp.Status);
    }

    [TestMethod]
    public void ArdalisResult_Invalid_MapsValidationErrors()
    {
        var ve = new Ardalis.Result.ValidationError
        {
            Identifier = "Width",
            ErrorMessage = "kötelező"
        };
        var resp = ArdalisResult.Invalid(ve).ToMcpResponse();

        Assert.IsFalse(resp.IsSuccess);
        Assert.AreEqual("Invalid", resp.Status);
        Assert.AreEqual(1, resp.ValidationErrors.Count);
        Assert.AreEqual("Width", resp.ValidationErrors[0].Identifier);
        Assert.AreEqual("kötelező", resp.ValidationErrors[0].ErrorMessage);
    }
}
