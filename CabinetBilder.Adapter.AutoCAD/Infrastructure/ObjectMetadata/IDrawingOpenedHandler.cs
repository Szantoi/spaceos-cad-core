using Autodesk.AutoCAD.ApplicationServices;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;

/// <summary>
/// Handles GUID collision detection and resolution when a drawing is opened.
/// </summary>
public interface IDrawingOpenedHandler
{
    /// <summary>
    /// Processes all SmartObjects in the specified document to detect and resolve GUID collisions.
    /// </summary>
    void HandleDocument(Document doc);
}
