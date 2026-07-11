using CabinetBilder.Core.Common;
using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CabinetBilder.Tests.Application;

[TestClass]
public class CreateFrontMatterFromBlockUseCaseTests
{
    private CreateFrontMatterFromBlockUseCase _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _sut = new CreateFrontMatterFromBlockUseCase(NullLogger<CreateFrontMatterFromBlockUseCase>.Instance);
    }

    [TestMethod]
    public void Execute_ShouldReturnFailure_WhenRequestIsNull()
    {
        Result<CreateFrontMatterFromBlockResult> result = _sut.Execute(null!);

        Assert.IsTrue(result.IsFailure);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void Execute_ShouldReturnFailure_WhenTypeIsEmpty()
    {
        var request = new CreateFrontMatterFromBlockRequest(
            "1A", "TestBlock", new Dictionary<string, object>(), 4, string.Empty);

        Result<CreateFrontMatterFromBlockResult> result = _sut.Execute(request);

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "empty");
    }

    [TestMethod]
    public void Execute_ShouldReturnSuccess_WithDynamicProperties()
    {
        var dynamicProps = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { "Length", 601.12345 },
            { "Width", 500.0 },
            { "Thickness", 18 }
        };
        var request = new CreateFrontMatterFromBlockRequest("1A2B", "CabinetBase", dynamicProps, 2, "Asztalos");

        Result<CreateFrontMatterFromBlockResult> result = _sut.Execute(request);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        StringAssert.Contains(result.Value.FrontMatterText, "Length_cut");
    }

    [TestMethod]
    public void Execute_ShouldReturnSuccess_AndContainBlockHandle()
    {
        var request = new CreateFrontMatterFromBlockRequest(
            "DEADBEEF", "TestBlock",
            new Dictionary<string, object> { { "Length", 300.0 }, { "Width", 200.0 } },
            2, "Asztalos");

        Result<CreateFrontMatterFromBlockResult> result = _sut.Execute(request);

        Assert.IsTrue(result.IsSuccess);
        StringAssert.Contains(result.Value.FrontMatterText, "DEADBEEF");
    }

    [TestMethod]
    public void Execute_ShouldReturnSuccess_WithEmptyDynamicProperties()
    {
        var request = new CreateFrontMatterFromBlockRequest(
            "AB12", "SimpleBlock",
            new Dictionary<string, object>(),
            4, "FurnĂ©r");

        Result<CreateFrontMatterFromBlockResult> result = _sut.Execute(request);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Value.FrontMatterText));
    }
}

