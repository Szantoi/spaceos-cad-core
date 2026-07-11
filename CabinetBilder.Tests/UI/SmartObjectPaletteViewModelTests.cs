using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Core.Catalog;
using CabinetBilder.Core.SmartObjects.Requests;
using CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MediatR;
using Ardalis.Result;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CabinetBilder.Tests.UI;

[TestClass]
public class SmartObjectPaletteViewModelTests
{
    private const string TestHandle = "1A2B";

    private static SmartObjectPaletteViewModel BuildViewModel(
        Mock<IMediator>? mediatorMock = null,
        Mock<IGetCatalogMaterialsUseCase>? catalogMock = null,
        Mock<CabinetBilder.Core.Infrastructure.IRedisService>? redisMock = null)
    {
        if (mediatorMock == null)
        {
            mediatorMock = new Mock<IMediator>();
        }
        if (catalogMock == null)
        {
            catalogMock = new Mock<IGetCatalogMaterialsUseCase>();
            catalogMock.Setup(c => c.ExecuteAsync()).ReturnsAsync(Enumerable.Empty<Material>());
        }
        if (redisMock == null)
        {
            redisMock = new Mock<CabinetBilder.Core.Infrastructure.IRedisService>();
            redisMock.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
        }

        // Mock CheckSyncStatusQuery to prevent null reference in ViewModel.RefreshAsync
        mediatorMock.Setup(m => m.Send(It.IsAny<CheckSyncStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Enumerable.Empty<SmartObjectSyncResult>()));
        
        return new SmartObjectPaletteViewModel(
            mediatorMock.Object,
            catalogMock.Object,
            redisMock.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SmartObjectPaletteViewModel>.Instance);
    }

    private static SmartObjectMetadata MetadataWith(params (string key, string value)[] fields)
    {
        var dict = fields.ToDictionary(f => f.key, f => f.value);
        return SmartObjectMetadata.From(dict);
    }

    [TestMethod]
    public async Task Load_ValidHandle_CallsMediatorSendQuery()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.Is<GetSmartObjectMetadataQuery>(q => q.Handles.Contains(TestHandle)), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success(MetadataWith(("Type", "Test"))));
        var vm = BuildViewModel(mediatorMock);
        
        await vm.LoadForHandlesAsync(new[] { TestHandle }); 

        mediatorMock.Verify(m => m.Send(It.Is<GetSmartObjectMetadataQuery>(q => q.Handles.Contains(TestHandle)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Load_Success_PopulatesFields()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetSmartObjectMetadataQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success(MetadataWith(("Key1", "Val1"))));
        var vm = BuildViewModel(mediatorMock);
        
        await vm.LoadForHandlesAsync(new[] { TestHandle });

        Assert.IsTrue(vm.HasSmartObject);
        Assert.IsTrue(vm.Fields.Any(f => f.Key == "Key1" && f.Value == "Val1"));
        Assert.IsTrue(vm.StatusMessage.Contains("1 mezĹ‘ betĂ¶ltve"));
    }

    [TestMethod]
    public async Task Load_MultiObject_Success_StatusShowsObjectCount()
    {
        var handles = new[] { "H1", "H2" };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetSmartObjectMetadataQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success(MetadataWith(("K1", "V1"))));
        var vm = BuildViewModel(mediatorMock);
        
        await vm.LoadForHandlesAsync(handles);

        Assert.AreEqual("1 mezĹ‘ betĂ¶ltve (2 elem kijelĂ¶lve).", vm.StatusMessage);
    }

    [TestMethod]
    public async Task Load_Failure_ClearsFieldsAndShowsError()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetSmartObjectMetadataQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Error("Error"));
        var vm = BuildViewModel(mediatorMock);
        
        await vm.LoadForHandlesAsync(new[] { TestHandle });

        Assert.IsFalse(vm.HasSmartObject);
        Assert.AreEqual(0, vm.Fields.Count);
        StringAssert.Contains(vm.StatusMessage, "Hiba:");
    }

    [TestMethod]
    public async Task Load_EmptyMetadata_SetsHasSmartObjectFalse()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetSmartObjectMetadataQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success(SmartObjectMetadata.From(new Dictionary<string, string>())));
        var vm = BuildViewModel(mediatorMock);
        
        await vm.LoadForHandlesAsync(new[] { TestHandle });

        Assert.IsFalse(vm.HasSmartObject);
        Assert.AreEqual("A kijelĂ¶lt objektumok nem tartalmaznak metaadatot.", vm.StatusMessage);
    }

    [TestMethod]
    public async Task Save_CallsMediatorSendCommand()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetSmartObjectMetadataQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success(MetadataWith(("Key1", "Val1"))));
        mediatorMock.Setup(m => m.Send(It.IsAny<UpdateSmartObjectMetadataCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success());
        
        var vm = BuildViewModel(mediatorMock);
        await vm.LoadForHandlesAsync(new[] { TestHandle });
        
        vm.Fields.First(f => f.Key == "Key1").Value = "NewVal";

        await vm.SaveAsync();

        mediatorMock.Verify(m => m.Send(It.Is<UpdateSmartObjectMetadataCommand>(c => 
                                        c.Handles.Contains(TestHandle) && 
                                        c.Metadata.Fields["Key1"] == "NewVal"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Save_Success_SetsSuccessStatusMessage()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetSmartObjectMetadataQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success(MetadataWith(("K1", "V1"))));
        mediatorMock.Setup(m => m.Send(It.IsAny<UpdateSmartObjectMetadataCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success());
        
        var vm = BuildViewModel(mediatorMock);
        await vm.LoadForHandlesAsync(new[] { TestHandle });
        vm.Fields[0].Value = "New";

        await vm.SaveAsync();

        Assert.AreEqual("Sikeresen mentve (helyi).", vm.StatusMessage);
    }

    [TestMethod]
    public async Task MaterialSelection_UpdatesThickness()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetSmartObjectMetadataQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success(MetadataWith(
                        (SmartObjectMetadataKeys.Material, "Old"),
                        (SmartObjectMetadataKeys.Thickness, "0")
                    )));

        var catalogMock = new Mock<IGetCatalogMaterialsUseCase>();
        catalogMock.Setup(c => c.ExecuteAsync()).ReturnsAsync(new[] {
            CabinetBilder.Core.Catalog.Material.Create("M1", "Material 1", 18.5, 600)
        });

        var vm = BuildViewModel(mediatorMock, catalogMock);
        await vm.LoadForHandlesAsync(new[] { TestHandle });

        vm.Fields.First(f => f.IsMaterial).Value = "Material 1";

        var thicknessField = vm.Fields.First(f => f.Key == SmartObjectMetadataKeys.Thickness);
        Assert.AreEqual("18.5", thicknessField.Value);
    }

    [TestMethod]
    public async Task Load_LockAcquisitionFails_SetsIsReadOnlyTrue()
    {
        // Arrange
        var redisMock = new Mock<CabinetBilder.Core.Infrastructure.IRedisService>();
        redisMock.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(false); // Simulate lock held by someone else

        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetSmartObjectMetadataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SmartObjectMetadata>.Success(MetadataWith(("Key", "Value"))));

        var viewModel = BuildViewModel(mediatorMock, null, redisMock);

        // Act
        await viewModel.LoadForHandlesAsync(new[] { TestHandle });

        // Assert
        Assert.IsTrue(viewModel.IsReadOnly, "ViewModel should be read-only when lock acquisition fails.");
        Assert.IsTrue(viewModel.StatusMessage.Contains("CSAK OLVASHATĂ“"), "Status message should indicate read-only state.");
    }
}

