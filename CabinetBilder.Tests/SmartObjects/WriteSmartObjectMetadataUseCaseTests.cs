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
public class WriteSmartObjectMetadataUseCaseTests
{
    private Mock<ISmartObjectMetadataService> _serviceMock = null!;
    private WriteSmartObjectMetadataUseCase _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _serviceMock = new Mock<ISmartObjectMetadataService>(MockBehavior.Strict);
        _sut = new WriteSmartObjectMetadataUseCase(
            _serviceMock.Object,
            NullLogger<WriteSmartObjectMetadataUseCase>.Instance);
    }

    [TestMethod]
    public void Execute_ShouldReturnFailure_WhenHandlesListIsEmpty()
    {
        Result result = _sut.Execute(Enumerable.Empty<string>(), SmartObjectMetadata.Empty);

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "Object handles collection cannot be empty");
    }

    [TestMethod]
    public void Execute_ShouldDelegateToService_ForSingleHandle()
    {
        SmartObjectMetadata metadata = SmartObjectMetadata.From(
            new Dictionary<string, string> { { SmartObjectMetadataKeys.ObjectType, "Asztalos" } });

        _serviceMock
            .Setup(s => s.WriteMetadata("H1", metadata))
            .Returns(Result.Success());

        Result result = _sut.Execute(new[] { "H1" }, metadata);

        Assert.IsTrue(result.IsSuccess);
        _serviceMock.Verify(s => s.WriteMetadata("H1", metadata), Times.Once);
    }

    [TestMethod]
    public void Execute_ShouldSkipMixedValues_InBulkWrite()
    {
        // One field is mixed, one is fixed
        SmartObjectMetadata metadata = SmartObjectMetadata.From(
            new Dictionary<string, string> 
            { 
                { SmartObjectMetadataKeys.ObjectType, "*VĂˇltozĂł*" }, 
                { SmartObjectMetadataKeys.Material, "TĂ¶lgy" } 
            });

        // We expect only Material to be written
        _serviceMock
            .Setup(s => s.WriteMetadata(It.IsAny<string>(), It.Is<SmartObjectMetadata>(m => !m.Fields.ContainsKey(SmartObjectMetadataKeys.ObjectType) && m.Fields[SmartObjectMetadataKeys.Material] == "TĂ¶lgy")))
            .Returns(Result.Success());

        var result = _sut.Execute(new[] { "H1", "H2" }, metadata);

        Assert.IsTrue(result.IsSuccess);
        _serviceMock.Verify(s => s.WriteMetadata("H1", It.IsAny<SmartObjectMetadata>()), Times.Once);
        _serviceMock.Verify(s => s.WriteMetadata("H2", It.IsAny<SmartObjectMetadata>()), Times.Once);
    }

    [TestMethod]
    public void Execute_ShouldReturnFailure_WhenServiceFails()
    {
        SmartObjectMetadata metadata = SmartObjectMetadata.From(
            new Dictionary<string, string>
            {
                { SmartObjectMetadataKeys.ObjectType, "Asztalos" }
            });

        _serviceMock
            .Setup(s => s.WriteMetadata("HANDLE_ERR", metadata))
            .Returns(Result.Failure("Storage error."));

        Result result = _sut.Execute(new[] { "HANDLE_ERR" }, metadata);

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "Failed to write metadata to all selected objects.");
        StringAssert.Contains(result.ErrorMessage, "Storage error.");
    }

    [TestMethod]
    public void Execute_ShouldSucceed_WithEmptyMetadata()
    {
        _serviceMock
            .Setup(s => s.WriteMetadata("HANDLE_01", SmartObjectMetadata.Empty))
            .Returns(Result.Success());

        Result result = _sut.Execute(new[] { "HANDLE_01" }, SmartObjectMetadata.Empty);

        Assert.IsTrue(result.IsSuccess);
    }
}

