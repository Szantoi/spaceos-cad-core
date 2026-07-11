using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Overrules;

/// <summary>
/// Intercepts AutoCAD's object snap / selection to gate REFEDIT on smart objects.
/// </summary>
/// <remarks>
/// AutoCAD's <c>Overrule</c> mechanism allows managed code to intercept and extend
/// the behaviour of standard AutoCAD operations without modifying the entities themselves.
///
/// Design constraints:
/// - AutoCAD = black-box UI. This overrule does NOT store or derive data; it only reads
///   from the DWG (via <see cref="DrawingObjectMetadataStore"/>) to make gate decisions.
/// - Overrule is registered per <c>RXClass</c> (here: BlockReference).
/// - <see cref="IsApplicable"/> limits interception to objects that carry our smart marker,
///   keeping the performance overhead negligible for non-smart objects.
/// </remarks>
internal sealed class SmartObjectGripOverrule : GripOverrule
{
    private static SmartObjectGripOverrule? _instance;
    private static bool _registered;
    private readonly DrawingObjectMetadataStore _store = new();

    // â”€â”€ Singleton lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static void Register()
    {
        if (_registered) return;
        _instance = new SmartObjectGripOverrule();
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

    // â”€â”€ Overrule filter â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Called by AutoCAD to check whether this overrule applies to a given entity.
    /// We apply only to BlockReferences that carry our smart object schema ID,
    /// avoiding any performance hit on ordinary blocks.
    /// </summary>
    public override bool IsApplicable(RXObject overruledSubject)
    {
        if (overruledSubject is not BlockReference blkRef) return false;

        // Use a short-lived open-close transaction (lightest possible read)
        using var tr = blkRef.Database.TransactionManager.StartOpenCloseTransaction();
        bool hasSchema = _store.TryGetSchemaId(blkRef, tr, out _);
        tr.Commit();
        return hasSchema;
    }

    // â”€â”€ Grip overrides â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    // GripOverrule currently passes through all grip operations unchanged.
    // Future: restrict specific grip points to prevent accidental geometry edits
    // that would invalidate metadata (e.g. dynamic property stretch).
}

