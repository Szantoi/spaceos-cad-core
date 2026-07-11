using System.Windows;

namespace CabinetBilder.Adapter.AutoCAD.UI.CatalogManagement;

/// <summary>
/// Interaction logic for CatalogManagementWindow.xaml
/// </summary>
public partial class CatalogManagementWindow : Window
{
    internal CatalogManagementWindow(CatalogManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Auto-load on open
        Loaded += async (s, e) => await viewModel.LoadMaterialsAsync();
    }
}

