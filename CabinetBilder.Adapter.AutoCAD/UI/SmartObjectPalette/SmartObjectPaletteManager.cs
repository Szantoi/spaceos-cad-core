using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;
using CabinetBilder.Adapter.AutoCAD.UI.CatalogManagement;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using System.Drawing;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms.Integration;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;

/// <summary>
/// Manages the lifecycle of the Smart Object PaletteSet.
/// This is the only class that crosses the AutoCAD UI boundary; all data operations
/// are delegated to use-cases which remain AutoCAD-independent.
/// </summary>
/// <remarks>
/// AutoCAD PaletteSet hosts a WPF UserControl via WindowsFormsHost/ElementHost bridge.
/// SelectionChanged is used ONLY as a UX convenience â€” it is not a reliable data source.
/// All data persists in the DWG via XRecord.
/// </remarks>
public sealed class SmartObjectPaletteManager : IDisposable
{
    private static SmartObjectPaletteManager? _instance;
    private static readonly object _lock = new();

    private readonly PaletteSet _paletteSet;
    private readonly SmartObjectPaletteViewModel _viewModel;
    private bool _disposed;

    // â”€â”€ Singleton â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static SmartObjectPaletteManager Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new SmartObjectPaletteManager();
                }
            }
            return _instance;
        }
    }

    // â”€â”€ Constructor â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private SmartObjectPaletteManager()
    {
        // 1. Use Centralized Service Provider
        var serviceProvider = AutoCadPlugin.ServiceProvider;

        // 2. Resolve ViewModel
        _viewModel = serviceProvider.GetRequiredService<SmartObjectPaletteViewModel>();

        // 3. Subscribe to catalog management request
        _viewModel.RequestManageCatalog += (s, e) =>
        {
            // Resolve fresh Catalog ViewModel from DI
            var catalogVm = serviceProvider.GetRequiredService<CabinetBilder.Adapter.AutoCAD.UI.CatalogManagement.CatalogManagementViewModel>();
            
            var window = new CatalogManagementWindow(catalogVm);
            AcadApp.ShowModalWindow(window);
            
            // Refresh palette materials after catalog changes
            _viewModel.LoadForHandle(_viewModel.ObjectHandle);
        };

        // Build the PaletteSet
        _paletteSet = new PaletteSet("CabinetBilder â€” Smart Object")
        {
            Style = PaletteSetStyles.ShowPropertiesMenu |
                    PaletteSetStyles.ShowAutoHideButton |
                    PaletteSetStyles.ShowCloseButton,
            DockEnabled = (DockSides)((int)DockSides.Left | (int)DockSides.Right | (int)DockSides.None),
            MinimumSize = new Size(260, 300),
        };

        // Bridge WPF â†’ WinForms via ElementHost
        var paletteControl = new SmartObjectPaletteControl(_viewModel);
        var elementHost = new ElementHost
        {
            Child = paletteControl,
            Dock = System.Windows.Forms.DockStyle.Fill,
        };

        _paletteSet.Add("Metaadatok", elementHost);

        // Subscribe to document events for UX-only refresh
        SubscribeToSelectionEvents();
    }

    // â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Shows the palette if hidden, hides it if visible.</summary>
    public void Toggle()
    {
        _paletteSet.Visible = !_paletteSet.Visible;
    }

    /// <summary>Shows the palette.</summary>
    public void Show() => _paletteSet.Visible = true;

    /// <summary>Hides the palette.</summary>
    public void Hide() => _paletteSet.Visible = false;

    /// <summary>
    /// Called by the <c>SmartObjectChangeObserver</c> overrule when a smart object is
    /// closed after modification. If the handle is among the currently displayed
    /// objects, the palette is refreshed automatically.
    /// </summary>
    /// <param name="modifiedHandle">The AutoCAD handle of the modified object.</param>
    public void NotifyObjectModified(string modifiedHandle)
    {
        if (!_paletteSet.Visible) return;
        if (string.IsNullOrWhiteSpace(modifiedHandle)) return;

        // Auto-refresh if any of the currently visible objects were modified.
        if (_viewModel.ObjectHandles.Any(h => h == modifiedHandle))
        {
            _viewModel.LoadForHandles(_viewModel.ObjectHandles);
        }
    }

    // â”€â”€ Selection event handling (UX only, not a data source) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SubscribeToSelectionEvents()
    {
        AcadApp.DocumentManager.DocumentCreated += OnDocumentCreated;
        AcadApp.DocumentManager.DocumentActivated += OnDocumentActivated;

        foreach (Document doc in AcadApp.DocumentManager)
        {
            SubscribeDocument(doc);
        }
    }

    private void UnsubscribeSelectionEvents()
    {
        AcadApp.DocumentManager.DocumentCreated -= OnDocumentCreated;
        AcadApp.DocumentManager.DocumentActivated -= OnDocumentActivated;

        foreach (Document doc in AcadApp.DocumentManager)
        {
            UnsubscribeDocument(doc);
        }
    }

    private void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
    {
        if (e.Document != null) SubscribeDocument(e.Document);
    }

    private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
    {
        // When switching documents, clear the panel to avoid stale data
        _viewModel.LoadForHandles(null);
    }

    private void SubscribeDocument(Document doc)
        => doc.ImpliedSelectionChanged += OnImpliedSelectionChanged;

    private void UnsubscribeDocument(Document doc)
        => doc.ImpliedSelectionChanged -= OnImpliedSelectionChanged;

    private void OnImpliedSelectionChanged(object? sender, EventArgs e)
    {
        // AutoCAD selection is used ONLY as a UX signal â€” not a data source.
        if (!_paletteSet.Visible) return;

        Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        Editor ed = doc.Editor;
        PromptSelectionResult selResult = ed.SelectImplied();

        if (selResult.Status != PromptStatus.OK
            || selResult.Value is null
            || selResult.Value.Count == 0)
        {
            _viewModel.LoadForHandles(null);
            return;
        }

        // Collect handles of all selected objects
        var handles = new List<string>();
        using (var tr = doc.Database.TransactionManager.StartOpenCloseTransaction())
        {
            foreach (ObjectId id in selResult.Value.GetObjectIds())
            {
                handles.Add(id.Handle.ToString());
            }
            tr.Commit();
        }

        _viewModel.LoadForHandles(handles);
    }

    // â”€â”€ IDisposable â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnsubscribeSelectionEvents();
        _paletteSet.Dispose();
    }
}

