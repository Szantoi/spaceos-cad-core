using CabinetBilder.Core.Common;
using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace CabinetBilder.Tests.SmartObjects;

[TestClass]
public class ReadSmartObjectMetadataUseCaseTests
{
    private Mock<ISmartObjectMetadataService> _serviceMock = null!;
    private ReadSmartObjectMetadataUseCase _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _serviceMock = new Mock<ISmartObjectMetadataService>(MockBehavior.Strict);
        _sut = new ReadSmartObjectMetadataUseCase(
            _serviceMock.Object,
            NullLogger<ReadSmartObjectMetadataUseCase>.Instance);
    }

    [TestMethod]
    public void Execute_ShouldReturnFailure_WhenHandlesListIsEmpty()
    {
        Result<SmartObjectMetadata> result = _sut.Execute(Enumerable.Empty<string>());

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "Object handles collection cannot be empty");
    }

    [TestMethod]
    public void Execute_ShouldReturnSuccess_WhenServiceSucceedsForSingleHandle()
    {
        SmartObjectMetadata expectedMetadata = SmartObjectMetadata.From(
            new Dictionary<string, string>
            {
                { SmartObjectMetadataKeys.ObjectType, "Asztalos" }
            });

        _serviceMock
            .Setup(s => s.ReadMetadata("HANDLE_01"))
            .Returns(Result.Success(expectedMetadata));

        Result<SmartObjectMetadata> result = _sut.Execute(new[] { "HANDLE_01" });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Asztalos", result.Value.Fields[SmartObjectMetadataKeys.ObjectType]);
    }

    [TestMethod]
    public void Execute_ShouldMergeMetadata_WithMixedValues()
    {
        var m1 = SmartObjectMetadata.From(new Dictionary<string, string> { { "K1", "V1" }, { "K2", "Same" } });
        var m2 = SmartObjectMetadata.From(new Dictionary<string, string> { { "K1", "V2" }, { "K2", "Same" } });

        _serviceMock.Setup(s => s.ReadMetadata("H1")).Returns(Result.Success(m1));
        _serviceMock.Setup(s => s.ReadMetadata("H2")).Returns(Result.Success(m2));

        var result = _sut.Execute(new[] { "H1", "H2" });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("*VĂˇltozĂł*", result.Value.Fields["K1"]);
        Assert.AreEqual("Same", result.Value.Fields["K2"]);
    }

    [TestMethod]
    public void Execute_ShouldReturnFailure_WhenServiceFails()
    {
        _serviceMock
            .Setup(s => s.ReadMetadata("MISSING_HANDLE"))
            .Returns(Result.Failure<SmartObjectMetadata>("Object not found."));

        Result<SmartObjectMetadata> result = _sut.Execute(new[] { "MISSING_HANDLE" });

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "Object not found.");
    }

    [TestMethod]
    public void Execute_ShouldReturnEmptyMetadata_WhenObjectHasNoFields()
    {
        _serviceMock
            .Setup(s => s.ReadMetadata("EMPTY_HANDLE"))
            .Returns(Result.Success(SmartObjectMetadata.Empty));

        Result<SmartObjectMetadata> result = _sut.Execute(new[] { "EMPTY_HANDLE" });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.Value.Fields.Count);
    }
}

