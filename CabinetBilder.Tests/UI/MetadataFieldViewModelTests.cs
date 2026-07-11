using CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.UI;

/// <summary>
/// Unit tests for <see cref="MetadataFieldViewModel"/>.
/// Covers: construction, Value mutation, IsModified lifecycle, AcceptChanges, PropertyChanged events.
/// </summary>
[TestClass]
public class MetadataFieldViewModelTests
{
    // â”€â”€ Construction â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [TestMethod]
    public void Constructor_SetsKeyAndValue()
    {
        var vm = new MetadataFieldViewModel("ObjectType", "Cabinet");

        Assert.AreEqual("ObjectType", vm.Key);
        Assert.AreEqual("Cabinet", vm.Value);
    }

    [TestMethod]
    public void Constructor_IsModifiedIsFalse()
    {
        var vm = new MetadataFieldViewModel("Label", "AlsĂł szekrĂ©ny");

        Assert.IsFalse(vm.IsModified);
    }

    [TestMethod]
    public void Constructor_NullValue_TreatedAsEmpty()
    {
        var vm = new MetadataFieldViewModel("Label", null!);

        Assert.AreEqual(string.Empty, vm.Value);
        Assert.IsFalse(vm.IsModified);
    }

    [TestMethod]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        // ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        Assert.ThrowsException<ArgumentNullException>(() =>
            new MetadataFieldViewModel(null!, "value"));
    }

    [TestMethod]
    public void Constructor_EmptyKey_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new MetadataFieldViewModel("   ", "value"));
    }

    // â”€â”€ Value mutation â†’ IsModified â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [TestMethod]
    public void SetValue_ToDifferentValue_SetsIsModifiedTrue()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");

        vm.Value = "MĂłdosĂ­tott";

        Assert.IsTrue(vm.IsModified);
    }

    [TestMethod]
    public void SetValue_ToSameValue_DoesNotSetIsModified()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");

        vm.Value = "Eredeti";

        Assert.IsFalse(vm.IsModified);
    }

    [TestMethod]
    public void SetValue_MultipleChanges_IsModifiedRemainsTrue()
    {
        var vm = new MetadataFieldViewModel("Label", "A");

        vm.Value = "B";
        vm.Value = "C";

        Assert.IsTrue(vm.IsModified);
        Assert.AreEqual("C", vm.Value);
    }

    [TestMethod]
    public void SetValue_ToEmpty_SetsIsModifiedTrue()
    {
        var vm = new MetadataFieldViewModel("Label", "HasValue");

        vm.Value = string.Empty;

        Assert.IsTrue(vm.IsModified);
    }

    // â”€â”€ AcceptChanges â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [TestMethod]
    public void AcceptChanges_AfterModification_SetsIsModifiedFalse()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        vm.Value = "MĂłdosĂ­tott";
        Assert.IsTrue(vm.IsModified);

        vm.AcceptChanges();

        Assert.IsFalse(vm.IsModified);
    }

    [TestMethod]
    public void AcceptChanges_PreservesValue()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        vm.Value = "MĂłdosĂ­tott";

        vm.AcceptChanges();

        Assert.AreEqual("MĂłdosĂ­tott", vm.Value);
    }

    [TestMethod]
    public void AcceptChanges_WhenNotModified_IsIdempotent()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        Assert.IsFalse(vm.IsModified);

        vm.AcceptChanges(); // should not throw, no-op

        Assert.IsFalse(vm.IsModified);
    }

    [TestMethod]
    public void AcceptChanges_ThenModifyAgain_SetsIsModifiedTrue()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        vm.Value = "MĂłdosĂ­tott";
        vm.AcceptChanges();
        Assert.IsFalse(vm.IsModified);

        vm.Value = "IsmĂ©t mĂłdosĂ­tva";

        Assert.IsTrue(vm.IsModified);
    }

    // â”€â”€ INotifyPropertyChanged â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [TestMethod]
    public void SetValue_RaisesPropertyChangedForValue()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.Value = "MĂłdosĂ­tott";

        CollectionAssert.Contains(raised, nameof(vm.Value));
    }

    [TestMethod]
    public void SetValue_RaisesPropertyChangedForIsModified()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.Value = "MĂłdosĂ­tott";

        CollectionAssert.Contains(raised, nameof(vm.IsModified));
    }

    [TestMethod]
    public void SetValue_ToSameValue_DoesNotRaisePropertyChanged()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.Value = "Eredeti"; // same value

        Assert.AreEqual(0, raised.Count);
    }

    [TestMethod]
    public void AcceptChanges_RaisesPropertyChangedForIsModified()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        vm.Value = "MĂłdosĂ­tott"; // IsModified = true
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.AcceptChanges();

        CollectionAssert.Contains(raised, nameof(vm.IsModified));
    }

    [TestMethod]
    public void AcceptChanges_WhenNotModified_DoesNotRaisePropertyChanged()
    {
        var vm = new MetadataFieldViewModel("Label", "Eredeti");
        // IsModified already false â€” AcceptChanges should be a true no-op
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.AcceptChanges();

        Assert.AreEqual(0, raised.Count);
    }

    // â”€â”€ Key immutability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [TestMethod]
    public void Key_IsImmutable_AfterConstruction()
    {
        var vm = new MetadataFieldViewModel("ObjectType", "Cabinet");

        // Key has no setter â€” verify it stays as initialized
        Assert.AreEqual("ObjectType", vm.Key);
    }
}

