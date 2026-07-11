using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Overrules;

/// <summary>
/// Intercepts open/close operations on smart BlockReferences to keep the
/// WPF palette in sync when objects are modified through the standard AutoCAD API
/// (e.g. Properties panel, CHANGE command, ATTEDIT).
/// </summary>
/// <remarks>
/// This overrule does NOT modify object data. It acts as an observer.
/// All data remains in the DWG (XRecord) â€” this class only fires a notification
/// so that subscribers (e.g. the palette ViewModel) can refresh.
///
/// Why ObjectOverrule vs PropertyOverrule?
///   <c>PropertyOverrule</c> is available in ObjectARX (C++) but not fully exposed
///   in the managed API. <c>ObjectOverrule.Close()</c> fires after any write-mode
///   close, which is the reliable managed equivalent for change detection.
/// </remarks>
internal sealed class SmartObjectChangeObserver : ObjectOverrule
{
    private static SmartObjectChangeObserver? _instance;
    private static bool _registered;
    private readonly DrawingObjectMetadataStore _store = new();

    // â”€â”€ Event for palette refresh â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Fired when a smart object is closed after being modified (write-mode).
    /// Payload: the AutoCAD handle string of the modified object.
    /// </summary>
    public static event Action<string>? SmartObjectModified;

    // â”€â”€ Singleton lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static void Register()
    {
        if (_registered) return;
        _instance = new SmartObjectChangeObserver();
        AddOverrule(RXObject.GetClass(typeof(BlockReference)), _instance, false);
        _registered = true;
    }

    public static void Unregister()
    {
        if (!_registered || _instance is null) return;
        RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), _instance);
        _instance = null;
        _registered = false;
    }

    // â”€â”€ Filter â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Only intercept BlockReferences that carry our smart object schema marker.
    /// Non-smart objects pass through unchanged and with zero overhead.
    /// </summary>
    public override bool IsApplicable(RXObject overruledSubject)
    {
        if (overruledSubject is not BlockReference blkRef) return false;
        if (blkRef.Database is null) return false;

        using var tr = blkRef.Database.TransactionManager.StartOpenCloseTransaction();
        bool result = _store.TryGetSchemaId(blkRef, tr, out _);
        tr.Commit();
        return result;
    }

    // â”€â”€ Change detection â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Called by AutoCAD when the object is closed. If it was open for write,
    /// it may have been modified â€” notify the palette to refresh.
    /// </summary>
    public override void Close(DBObject dbObject)
    {
        // Only notify for write-mode closes (IsModifiedXData catches attribute/xdata edits too)
        if (dbObject.IsWriteEnabled && (dbObject.IsModified || dbObject.IsModifiedXData))
        {
            string handle = dbObject.Handle.ToString();
            SmartObjectModified?.Invoke(handle);
        }

        base.Close(dbObject);
    }
}

