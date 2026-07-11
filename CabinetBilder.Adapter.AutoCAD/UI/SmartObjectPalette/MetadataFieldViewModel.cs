using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;

/// <summary>
/// Represents a single editable metadata field row in the palette DataGrid.
/// </summary>
internal sealed class MetadataFieldViewModel : INotifyPropertyChanged
{
    private string _value;
    private bool _isModified;
    private bool _isMaterial;
    private ObservableCollection<string>? _possibleValues;

    public MetadataFieldViewModel(string key, string value, bool isMaterial = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        _value = value ?? string.Empty;
        _isModified = false;
        _isMaterial = isMaterial;
    }

    /// <summary>The canonical metadata key (read-only in the UI).</summary>
    public string Key { get; }

    /// <summary>The current field value. Setting this marks the field as modified.</summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            IsModified = true;
            OnPropertyChanged();
        }
    }

    /// <summary>True if the value has been changed since the last load or save.</summary>
    public bool IsModified
    {
        get => _isModified;
        private set
        {
            if (_isModified == value) return;
            _isModified = value;
            OnPropertyChanged();
        }
    }

    /// <summary>True if this field should display a material picker.</summary>
    public bool IsMaterial
    {
        get => _isMaterial;
        set
        {
            _isMaterial = value;
            OnPropertyChanged();
        }
    }

    /// <summary>List of available values (e.g. from catalog) if this is a selection field.</summary>
    public ObservableCollection<string>? PossibleValues
    {
        get => _possibleValues;
        set
        {
            _possibleValues = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Resets the modified flag without changing the value (called after save).</summary>
    internal void AcceptChanges() => IsModified = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

