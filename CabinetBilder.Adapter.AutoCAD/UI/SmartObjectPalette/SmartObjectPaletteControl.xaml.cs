using System.Globalization;
using System.Windows.Data;

// Explicit alias to avoid ambiguity: WPF UserControl vs WinForms UserControl
using WpfUserControl = System.Windows.Controls.UserControl;
using Visibility = System.Windows.Visibility;

namespace CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;

/// <summary>
/// Code-behind for SmartObjectPaletteControl.xaml.
/// Keeps the code-behind minimal â€” all logic lives in the ViewModel.
/// </summary>
public partial class SmartObjectPaletteControl : WpfUserControl
{
    public SmartObjectPaletteControl()
    {
        InitializeComponent();
    }

    internal SmartObjectPaletteControl(SmartObjectPaletteViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}

// â”€â”€ Inline converters (no external framework needed) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>Converts bool to Visibility (true â†’ Visible, false â†’ Collapsed).</summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Converts bool to Visibility (true â†’ Collapsed, false â†’ Visible) â€” inverse.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not Visibility.Visible;
}

