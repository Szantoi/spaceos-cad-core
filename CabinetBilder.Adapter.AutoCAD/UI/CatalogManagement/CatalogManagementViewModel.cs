using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using CabinetBilder.Core.Catalog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CabinetBilder.Adapter.AutoCAD.UI.CatalogManagement;

/// <summary>
/// ViewModel for managing a single material in the catalog management UI.
/// </summary>
internal sealed class MaterialManagementViewModel : INotifyPropertyChanged
{
    private string _code;
    private string _name;
    private double _thickness;
    private double _density;
    private bool _isModified;

    public MaterialManagementViewModel(Material material, bool isNew = false)
    {
        Id = material.Id;
        _code = material.Code;
        _name = material.Name;
        _thickness = material.Thickness;
        _density = material.Density;
        IsNew = isNew;
        _isModified = isNew;
    }

    public Guid Id { get; }
    public bool IsNew { get; }

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public double Thickness
    {
        get => _thickness;
        set => SetProperty(ref _thickness, value);
    }

    public double Density
    {
        get => _density;
        set => SetProperty(ref _density, value);
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (_isModified != value)
            {
                _isModified = value;
                OnPropertyChanged();
            }
        }
    }

    public Material ToDomain()
    {
        return Material.Reconstitute(Id, Code, Name, Thickness, Density);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return;
        storage = value;
        IsModified = true;
        OnPropertyChanged(propertyName);
    }
}

/// <summary>
/// ViewModel for the Catalog Management window.
/// </summary>
internal sealed class CatalogManagementViewModel : INotifyPropertyChanged
{
    private readonly IGetCatalogMaterialsUseCase _getMaterialsUseCase;
    private readonly ISaveMaterialUseCase _saveUseCase;
    private readonly IDeleteMaterialUseCase _deleteUseCase;
    
    private MaterialManagementViewModel? _selectedMaterial;
    private bool _isBusy;
    private string _statusMessage = string.Empty;
    private string _searchText = string.Empty;
    
    public CatalogManagementViewModel(
        IGetCatalogMaterialsUseCase getMaterialsUseCase,
        ISaveMaterialUseCase saveUseCase,
        IDeleteMaterialUseCase deleteUseCase)
    {
        _getMaterialsUseCase = getMaterialsUseCase ?? throw new ArgumentNullException(nameof(getMaterialsUseCase));
        _saveUseCase = saveUseCase ?? throw new ArgumentNullException(nameof(saveUseCase));
        _deleteUseCase = deleteUseCase ?? throw new ArgumentNullException(nameof(deleteUseCase));

        Materials = new ObservableCollection<MaterialManagementViewModel>();
        
        // Setup Filtered View
        var materialsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Materials);
        materialsView.Filter = m => 
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var vm = (MaterialManagementViewModel)m;
            return vm.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                   vm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        };
        FilteredMaterials = materialsView;
        
        AddCommand = new RelayCommand(AddMaterial);
        DeleteCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => SelectedMaterial != null);
        SaveCommand = new RelayCommand(async () => await SaveChangesAsync(), () => Materials.Any(m => m.IsModified));
        RefreshCommand = new RelayCommand(async () => await LoadMaterialsAsync());
    }

    public ObservableCollection<MaterialManagementViewModel> Materials { get; }
    public ICollectionView FilteredMaterials { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                FilteredMaterials.Refresh();
            }
        }
    }

    public MaterialManagementViewModel? SelectedMaterial
    {
        get => _selectedMaterial;
        set
        {
            if (_selectedMaterial != value)
            {
                _selectedMaterial = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand RefreshCommand { get; }

    public async Task LoadMaterialsAsync()
    {
        IsBusy = true;
        StatusMessage = "Loading materials...";
        try
        {
            var domainMaterials = await _getMaterialsUseCase.ExecuteAsync();
            Materials.Clear();
            foreach (var m in domainMaterials)
            {
                Materials.Add(new MaterialManagementViewModel(m));
            }
            StatusMessage = $"Loaded {Materials.Count} materials.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddMaterial()
    {
        var newMaterial = Material.Create("NEW", "New Material", 18.0, 650.0);
        var vm = new MaterialManagementViewModel(newMaterial, isNew: true);
        Materials.Add(vm);
        SelectedMaterial = vm;
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedMaterial == null) return;

        if (!SelectedMaterial.IsNew)
        {
            IsBusy = true;
            try
            {
                await _deleteUseCase.ExecuteAsync(SelectedMaterial.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete error: {ex.Message}";
                return;
            }
            finally
            {
                IsBusy = false;
            }
        }

        Materials.Remove(SelectedMaterial);
        StatusMessage = "Material removed.";
    }

    private async Task SaveChangesAsync()
    {
        IsBusy = true;
        StatusMessage = "Saving changes...";
        int savedCount = 0;
        try
        {
            foreach (var m in Materials.Where(m => m.IsModified).ToList())
            {
                await _saveUseCase.ExecuteAsync(m.ToDomain());
                m.IsModified = false;
                savedCount++;
            }
            StatusMessage = $"Saved {savedCount} changes.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

// Simple RelayCommand implementation if not available elsewhere
internal class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}

internal class RelayCommandAsync(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Func<Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public async void Execute(object? parameter) => await _execute();
}

