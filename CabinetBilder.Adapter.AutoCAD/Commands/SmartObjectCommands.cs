using CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;
using Autodesk.AutoCAD.Runtime;

namespace CabinetBilder.Adapter.AutoCAD.Commands;

/// <summary>
/// Provides AutoCAD commands for the Smart Object palette.
/// </summary>
public class SmartObjectCommands
{
    /// <summary>
    /// Toggles the Smart Object metadata palette (show/hide).
    /// </summary>
    /// <remarks>
    /// Command name in AutoCAD: <c>CBSmartPanel</c>.
    /// </remarks>
    [CommandMethod("CBSmartPanel")]
    public void ToggleSmartPanel()
    {
        SmartObjectPaletteManager.Instance.Toggle();
    }
}

