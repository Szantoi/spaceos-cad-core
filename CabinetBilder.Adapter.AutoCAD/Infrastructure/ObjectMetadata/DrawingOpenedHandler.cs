using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CabinetBilder.Core.SmartObjects;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.IO;
using CabinetBilder.Core.Sync;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;

/// <summary>
/// Implements GUID collision detection by scanning drawing entities on open.
/// </summary>
public sealed class DrawingOpenedHandler : IDrawingOpenedHandler
{
    private readonly IDrawingObjectMetadataStore _metadataStore;
    private readonly ILocalStore _localStore;
    private readonly ILogger<DrawingOpenedHandler> _logger;

    public DrawingOpenedHandler(
        IDrawingObjectMetadataStore metadataStore, 
        ILocalStore localStore,
        ILogger<DrawingOpenedHandler> logger)
    {
        _metadataStore = metadataStore;
        _localStore = localStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void HandleDocument(Document doc)
    {
        if (doc == null) return;
        
        // Use a background task to avoid blocking the AutoCAD UI thread unnecessarily
        // although we need to lock the document if we modify it.
        // For simplicity and since 'open' is a safe time to scan, we run it synchronously here.
        
        Database db = doc.Database;
        string drawingPath = db.Filename;

        bool isSaved = !string.IsNullOrEmpty(drawingPath) && File.Exists(drawingPath);
        string drawingHash = isSaved ? ComputeFileHash(drawingPath) : "unsaved-" + Guid.NewGuid();

        _logger.LogInformation("Scanning drawing for GUID collisions: {Path} (Hash: {Hash})", 
            isSaved ? drawingPath : "[Unsaved]", drawingHash);

        using var docLock = doc.LockDocument();
        using var transaction = db.TransactionManager.StartTransaction();
        
        try
        {
            var bt = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
            
            int collisionsFound = 0;
            int objectsProcessed = 0;
            int newGuidsAssigned = 0;

            foreach (ObjectId btrId in bt)
            {
                var btr = (BlockTableRecord)transaction.GetObject(btrId, OpenMode.ForRead);
                
                foreach (ObjectId entId in btr)
                {
                    if (!entId.IsValid) continue;

                    using var ent = transaction.GetObject(entId, OpenMode.ForRead);
                    if (_metadataStore.TryGetSchemaId(ent, transaction, out _))
                    {
                        objectsProcessed++;
                        var fields = _metadataStore.ReadFields(ent, transaction);
                        
                        if (fields.TryGetValue(SmartObjectMetadataKeys.Guid, out var guidStr) && 
                            Guid.TryParse(guidStr, out var currentGuid))
                        {
                            // Check DB for this GUID
                            var seenResult = _localStore.TryFindSeenGuidAsync(currentGuid).GetAwaiter().GetResult();
                            
                            if (seenResult.IsSuccess && seenResult.Value != null)
                            {
                                var existing = seenResult.Value;
                                // Collision Rule: Same GUID, but different Path OR (same path but different content hash)
                                if (!string.Equals(existing.DrawingPath, drawingPath, StringComparison.OrdinalIgnoreCase) || 
                                    (isSaved && !string.Equals(existing.DrawingHash, drawingHash, StringComparison.OrdinalIgnoreCase)))
                                {
                                    // If same path but different hash, it's an update to the SAME file.
                                    // Collision only if DIFFERENT path.
                                    if (!string.Equals(existing.DrawingPath, drawingPath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        var newGuid = Guid.NewGuid();
                                        var updatedFields = new Dictionary<string, string>(fields, StringComparer.OrdinalIgnoreCase)
                                        {
                                            [SmartObjectMetadataKeys.Guid] = newGuid.ToString()
                                        };

                                        ent.UpgradeOpen();
                                        _metadataStore.WriteFields(ent, transaction, updatedFields);
                                        
                                        _logger.LogWarning("GUID Collision! Handle {Handle} in {File} had {OldGuid}. Reassigned to {NewGuid}.", 
                                            ent.Handle, drawingPath, currentGuid, newGuid);
                                        
                                        currentGuid = newGuid;
                                        collisionsFound++;
                                    }
                                }
                            }
                            
                            // Register/Update seen state
                            _localStore.RegisterSeenGuidAsync(currentGuid, drawingPath, drawingHash).GetAwaiter().GetResult();
                        }
                        else
                        {
                            // Missing or invalid GUID -> Assign one
                            var newGuid = Guid.NewGuid();
                            var updatedFields = new Dictionary<string, string>(fields, StringComparer.OrdinalIgnoreCase)
                            {
                                [SmartObjectMetadataKeys.Guid] = newGuid.ToString()
                            };

                            ent.UpgradeOpen();
                            _metadataStore.WriteFields(ent, transaction, updatedFields);
                            _localStore.RegisterSeenGuidAsync(newGuid, drawingPath, drawingHash).GetAwaiter().GetResult();
                            
                            newGuidsAssigned++;
                        }
                    }
                }
            }

            transaction.Commit();
            
            if (collisionsFound > 0 || newGuidsAssigned > 0)
            {
                doc.Editor.WriteMessage($"\n[CabinetBilder] Integritás ellenőrzés kész: {collisionsFound} ütközés feloldva, {newGuidsAssigned} új azonosító kiosztva.\n");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GUID collision check for {Path}", drawingPath);
            transaction.Abort();
        }
    }
    
    private string ComputeFileHash(string path)
    {
        try
        {
            using var sha256 = SHA256.Create();
            // Use FileShare.ReadWrite because AutoCAD has the file open
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute hash for {Path}", path);
            return "hash-failed-" + Guid.NewGuid();
        }
    }
}
