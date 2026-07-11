using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using CabinetBilder.Core.Catalog;
using CabinetBilder.Adapter.AutoCAD.UI.CatalogManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CabinetBilder.Tests.UI;

[TestClass]
public class CatalogManagementViewModelTests
{
    private Mock<IGetCatalogMaterialsUseCase> _getMaterialsMock = null!;
    private Mock<ISaveMaterialUseCase> _saveMock = null!;
    private Mock<IDeleteMaterialUseCase> _deleteMock = null!;
    private CatalogManagementViewModel _viewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _getMaterialsMock = new Mock<IGetCatalogMaterialsUseCase>();
        _saveMock = new Mock<ISaveMaterialUseCase>();
        _deleteMock = new Mock<IDeleteMaterialUseCase>();

        _getMaterialsMock.Setup(x => x.ExecuteAsync())
            .ReturnsAsync(new List<Material>
            {
                Material.Reconstitute(Guid.NewGuid(), "M1", "Material 1", 18, 650)
            });

        _viewModel = new CatalogManagementViewModel(
            _getMaterialsMock.Object,
            _saveMock.Object,
            _deleteMock.Object);
    }

    [TestMethod]
    public async Task LoadMaterialsAsync_PopulatesMaterialsCollection()
    {
        // Act
        await _viewModel.LoadMaterialsAsync();

        // Assert
        Assert.AreEqual(1, _viewModel.Materials.Count);
        Assert.AreEqual("M1", _viewModel.Materials[0].Code);
        Assert.IsFalse(_viewModel.Materials[0].IsModified);
    }

    [TestMethod]
    public void AddCommand_AddsNewMaterialWithDefaultValues()
    {
        // Act
        _viewModel.AddCommand.Execute(null);

        // Assert
        Assert.AreEqual(1, _viewModel.Materials.Count);
        Assert.AreEqual("NEW", _viewModel.Materials[0].Code);
        Assert.IsTrue(_viewModel.Materials[0].IsNew);
        Assert.IsTrue(_viewModel.Materials[0].IsModified);
    }

    [TestMethod]
    public async Task DeleteCommand_RemovesNewMaterialWithoutCallingUseCase()
    {
        // Arrange
        _viewModel.AddCommand.Execute(null);
        var newMaterial = _viewModel.Materials[0];
        _viewModel.SelectedMaterial = newMaterial;

        // Act
        _viewModel.DeleteCommand.Execute(null);

        // Assert
        Assert.AreEqual(0, _viewModel.Materials.Count);
        _deleteMock.Verify(x => x.ExecuteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteCommand_CallsUseCaseForExistingMaterial()
    {
        // Arrange
        await _viewModel.LoadMaterialsAsync();
        var existing = _viewModel.Materials[0];
        _viewModel.SelectedMaterial = existing;

        // Act
        _viewModel.DeleteCommand.Execute(null);

        // Assert
        Assert.AreEqual(0, _viewModel.Materials.Count);
        _deleteMock.Verify(x => x.ExecuteAsync(existing.Id), Times.Once);
    }

    [TestMethod]
    public async Task SaveCommand_CallsSaveUseCaseForModifiedMaterials()
    {
        // Arrange
        await _viewModel.LoadMaterialsAsync();
        _viewModel.Materials[0].Name = "Updated Name"; // Triggers IsModified

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        _saveMock.Verify(x => x.ExecuteAsync(It.Is<Material>(m => m.Name == "Updated Name")), Times.Once);
        Assert.IsFalse(_viewModel.Materials[0].IsModified);
    }
}

