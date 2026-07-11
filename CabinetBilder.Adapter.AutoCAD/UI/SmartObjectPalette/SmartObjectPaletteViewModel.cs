using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using CabinetBilder.Core.Common;
using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Core.SmartObjects.Requests;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;

/// <summary>
/// ViewModel for the Smart Object metadata palette.
/// AutoCAD-independent: all data operations go through MediatR requests or use-cases.
/// </summary>
internal sealed class SmartObjectPaletteViewModel : INotifyPropertyChanged
{
    private readonly IMediator _mediator;
    private readonly IGetCatalogMaterialsUseCase _getMaterialsUseCase;
    private readonly CabinetBilder.Core.Infrastructure.IRedisService _redisService;
    private readonly ILogger<SmartObjectPaletteViewModel> _logger;

    private List<string> _objectHandles = [];
    private List<string> _lockedHandles = [];
    private string _statusMessage = string.Empty;
    private bool _hasSmartObject;
    private bool _isBusy;
    private bool _isReadOnly;
    private readonly string _lockValue = $"{Environment.MachineName}_{Guid.NewGuid():N}";
    private List<CabinetBilder.Core.Catalog.Material> _catalogMaterials = [];
    private SyncStatus _syncStatus = SyncStatus.UpToDate;

    internal SmartObjectPaletteViewModel(
        IMediator mediator,
        IGetCatalogMaterialsUseCase getMaterialsUseCase,
        CabinetBilder.Core.Infrastructure.IRedisService redisService,
        ILogger<SmartObjectPaletteViewModel> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _getMaterialsUseCase = getMaterialsUseCase ?? throw new ArgumentNullException(nameof(getMaterialsUseCase));
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Fields = [];
        RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => HasSmartObject && !IsBusy && !IsReadOnly && Fields.Any(f => f.IsModified));
        PushCommand = new RelayCommand(async () => await PushAsync(), () => HasSmartObject && !IsBusy && !IsReadOnly);
        PullCommand = new RelayCommand(async () => await PullAsync(), () => HasSmartObject && !IsBusy);
        ManageCatalogCommand = new RelayCommand(() => RequestManageCatalog?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>Raised when user wants to open the catalog management window.</summary>
    public event EventHandler? RequestManageCatalog;

    // â”€â”€ Bindable properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>All metadata fields currently displayed.</summary>
    public ObservableCollection<MetadataFieldViewModel> Fields { get; }

    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand PushCommand { get; }
    public ICommand PullCommand { get; }
    public ICommand ManageCatalogCommand { get; }

    /// <summary>Combined sync status of selected objects.</summary>
    public SyncStatus SyncStatus
    {
        get => _syncStatus;
        private set => SetField(ref _syncStatus, value);
    }

    /// <summary>The AutoCAD handles of the currently selected objects.</summary>
    public IEnumerable<string> ObjectHandles
    {
        get => _objectHandles;
        private set
        {
            _objectHandles = value?.ToList() ?? [];
            OnPropertyChanged(nameof(ObjectHandles));
            OnPropertyChanged(nameof(ObjectHandle)); // For backward compatibility if needed
        }
    }

    /// <summary>Convenience property returning the first handle or empty.</summary>
    public string ObjectHandle => _objectHandles.FirstOrDefault() ?? string.Empty;

    /// <summary>Status message shown in the palette footer.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>True when one or more smart objects with metadata are selected.</summary>
    public bool HasSmartObject
    {
        get => _hasSmartObject;
        private set => SetField(ref _hasSmartObject, value);
    }

    /// <summary>True while an async operation is in progress (disables buttons).</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    /// <summary>True if one or more objects are locked by someone else.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set => SetField(ref _isReadOnly, value);
    }

    // â”€â”€ Public methods â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Called by the PaletteManager when the AutoCAD selection changes.
    /// Loads metadata for the given handles, or clears the panel.
    /// </summary>
    public async Task LoadForHandlesAsync(IEnumerable<string>? handles)
    {
        if (handles == null || !handles.Any())
        {
            ClearPanel("Nincs kijelĂ¶lt smart object.");
            return;
        }

        ObjectHandles = handles;
        await RefreshAsync();
    }

    /// <summary>Non-awaitable version for event handlers.</summary>
    public void LoadForHandles(IEnumerable<string>? handles) => _ = LoadForHandlesAsync(handles);

    /// <summary>Backward compatibility for single handle notifications.</summary>
    public void LoadForHandle(string? handle) => LoadForHandles(handle == null ? null : [handle]);

    // â”€â”€ Private logic â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Public version for testing that returns a Task.
    /// </summary>
    public async Task RefreshAsync()
    {
        if (!_objectHandles.Any())
        {
            ClearPanel("Nincs kijelĂ¶lt smart object.");
            return;
        }

        if (IsBusy) return; // Guard against concurrent operations

        IsBusy = true;
        StatusMessage = "BetĂ¶ltĂ©s...";

        try
        {
            // Release previous locks
            await ReleaseLocksAsync();

            // Load materials from catalog
            var materials = await _getMaterialsUseCase.ExecuteAsync();
            _catalogMaterials = materials.ToList();
            var materialNames = new ObservableCollection<string>(_catalogMaterials.Select(m => m.Name));

            // Try to acquire locks for the new selection
            bool allLocked = true;
            foreach (var handle in _objectHandles)
            {
                string lockKey = $"lock:autocad:object:{handle}";
                bool locked = await _redisService.AcquireLockAsync(lockKey, _lockValue, TimeSpan.FromMinutes(5));
                if (locked)
                {
                    _lockedHandles.Add(handle);
                }
                else
                {
                    allLocked = false;
                    _logger.LogInformation("Could not acquire lock for object {Handle}. Object is likely being edited by another instance.", handle);
                }
            }

            IsReadOnly = !allLocked;

            // Load metadata from object(s) via MediatR
            var result = await _mediator.Send(new GetSmartObjectMetadataQuery(_objectHandles));

            if (!result.IsSuccess)
            {
                var error = string.Join(", ", result.Errors);
                ClearPanel($"Hiba: {error}");
                _logger.LogWarning("Palette refresh failed for objects: {Error}", error);
                return;
            }

            Fields.Clear();
            foreach (var field in result.Value.Fields)
            {
                bool isMaterialField = field.Key.Equals(SmartObjectMetadataKeys.Material, StringComparison.OrdinalIgnoreCase);
                var fieldVm = new MetadataFieldViewModel(field.Key, field.Value, isMaterialField);

                if (isMaterialField)
                {
                    fieldVm.PossibleValues = materialNames;
                }

                fieldVm.PropertyChanged += OnFieldPropertyChanged;
                Fields.Add(fieldVm);
            }

            HasSmartObject = Fields.Count > 0;

            // Check synchronization status
            if (_objectHandles.Any())
            {
                var syncResult = await _mediator.Send(new CheckSyncStatusQuery(_objectHandles));
                if (syncResult.IsSuccess)
                {
                    // Aggregated status: if any is in conflict, then conflict. Else if any is outdated, then outdated.
                    var statuses = syncResult.Value.Select(r => r.Status).ToList();
                    if (statuses.Any(s => s == SyncStatus.Conflict)) SyncStatus = SyncStatus.Conflict;
                    else if (statuses.Any(s => s == SyncStatus.Outdated)) SyncStatus = SyncStatus.Outdated;
                    else if (statuses.Any(s => s == SyncStatus.LocalOnly)) SyncStatus = SyncStatus.LocalOnly;
                    else SyncStatus = SyncStatus.UpToDate;
                }
            }

            string countInfo = _objectHandles.Count > 1 ? $" ({_objectHandles.Count} elem kijelĂ¶lve)" : "";
            string lockInfo = IsReadOnly ? " [CSAK OLVASHATĂ“ - valaki mĂˇs szerkeszti]" : "";
            string syncInfo = SyncStatus switch
            {
                SyncStatus.Outdated => " [ELAVULT - frissĂ­tĂ©s szĂĽksĂ©ges]",
                SyncStatus.Conflict => " [KONFLIKTUS - manuĂˇlis megoldĂˇs szĂĽksĂ©ges]",
                SyncStatus.LocalOnly => " [CSAK HELYI - feltĂ¶ltĂ©sre vĂˇr]",
                _ => ""
            };

            StatusMessage = Fields.Count > 0
                ? $"{Fields.Count} mezĹ‘ betĂ¶ltve{countInfo}{lockInfo}{syncInfo}."
                : "A kijelĂ¶lt objektumok nem tartalmaznak metaadatot.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing metadata for selected objects");
            StatusMessage = $"Hiba: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MetadataFieldViewModel field) return;

        if (e.PropertyName == nameof(MetadataFieldViewModel.Value))
        {
            if (field.IsMaterial)
            {
                UpdateDependentFieldsFromMaterial(field.Value);
            }
        }
    }

    private void UpdateDependentFieldsFromMaterial(string materialName)
    {
        var material = _catalogMaterials.FirstOrDefault(m => m.Name == materialName);
        if (material == null) return;

        var thicknessField = Fields.FirstOrDefault(f => f.Key.Equals(SmartObjectMetadataKeys.Thickness, StringComparison.OrdinalIgnoreCase));
        if (thicknessField != null)
        {
            thicknessField.Value = material.Thickness.ToString("F1", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Public version for testing that returns a Task.
    /// </summary>
    public async Task SaveAsync()
    {
        if (!HasSmartObject || !_objectHandles.Any()) return;

        IsBusy = true;
        StatusMessage = "MentĂ©s...";

        try
        {
            var updatedFields = Fields.ToDictionary(f => f.Key, f => f.Value);
            
            // Note: We don't change the version hash here yet. 
            // In a Git-style workflow, "Save" is like editing the working copy.
            // "Push" is what creates a new version on the server.
            // However, we still use the existing version from the drawing.
            
            // We need to fetch the existing metadata to get its version
            var currentResult = await _mediator.Send(new GetSmartObjectMetadataQuery(_objectHandles));
            string baseVersion = currentResult.IsSuccess ? currentResult.Value.Version : string.Empty;

            SmartObjectMetadata metadata = SmartObjectMetadata.From(updatedFields, baseVersion);

            // Persist via MediatR
            var saveResult = await _mediator.Send(new UpdateSmartObjectMetadataCommand(_objectHandles, metadata));

            if (!saveResult.IsSuccess)
            {
                var error = string.Join(", ", saveResult.Errors);
                StatusMessage = $"MentĂ©si hiba: {error}";
                _logger.LogWarning("Palette save failed: {Error}", error);
                return;
            }

            // Accept changes (reset IsModified flags)
            foreach (MetadataFieldViewModel field in Fields)
            {
                field.AcceptChanges();
            }

            StatusMessage = "Sikeresen mentve (helyi).";
            _logger.LogInformation("Palette: saved {Count} field(s) for {ObjectCount} objects.", Fields.Count, _objectHandles.Count);
            
            // Update sync status after local save (it might become LocalOnly if it was UpToDate)
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving metadata for objects");
            StatusMessage = $"MentĂ©si hiba: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PushAsync()
    {
        if (!_objectHandles.Any()) return;
        
        IsBusy = true;
        StatusMessage = "FeltĂ¶ltĂ©s (Push)...";

        try
        {
            foreach (var handle in _objectHandles)
            {
                // Read latest local state
                var localResult = await _mediator.Send(new GetSmartObjectMetadataQuery([handle]));
                if (!localResult.IsSuccess) continue;

                var pushResult = await _mediator.Send(new PushMetadataCommand(handle, localResult.Value));
                
                if (!pushResult.IsSuccess || pushResult.Value.Status == SyncStatus.Conflict)
                {
                    StatusMessage = $"Konfliktus Ă©szlelve: {handle}. SzinkronizĂˇljon vagy oldja meg manuĂˇlisan.";
                    _logger.LogWarning("Push conflict for {Handle}", handle);
                    break;
                }
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push failed");
            StatusMessage = $"Hiba: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PullAsync()
    {
        if (!_objectHandles.Any()) return;

        IsBusy = true;
        StatusMessage = "FrissĂ­tĂ©s szerverrĹ‘l (Pull)...";

        try
        {
            foreach (var handle in _objectHandles)
            {
                var pullResult = await _mediator.Send(new PullMetadataCommand(handle));
                if (!pullResult.IsSuccess)
                {
                    _logger.LogWarning("Pull failed for {Handle}: {Error}", handle, string.Join(", ", pullResult.Errors));
                }
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull failed");
            StatusMessage = $"Hiba: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReleaseLocksAsync()
    {
        foreach (var handle in _lockedHandles)
        {
            string lockKey = $"lock:autocad:object:{handle}";
            await _redisService.ReleaseLockAsync(lockKey, _lockValue);
        }
        _lockedHandles.Clear();
    }

    private void ClearPanel(string message)
    {
        Fields.Clear();
        _objectHandles.Clear();
        _ = ReleaseLocksAsync(); // Fire and forget lock release on clear
        OnPropertyChanged(nameof(ObjectHandles));
        OnPropertyChanged(nameof(ObjectHandle));
        HasSmartObject = false;
        StatusMessage = message;
    }

    // â”€â”€ INotifyPropertyChanged â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

/// <summary>
/// Minimal ICommand implementation for MVVM without external framework dependencies.
/// </summary>
internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool>? _canExecute = canExecute;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged
    {
        add => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }
}

