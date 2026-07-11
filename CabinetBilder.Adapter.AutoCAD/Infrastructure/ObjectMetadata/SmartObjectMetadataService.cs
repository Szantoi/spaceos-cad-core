using CabinetBilder.Core.Common;
using CabinetBilder.Core.SmartObjects;
using Autodesk.AutoCAD.DatabaseServices;
using Microsoft.Extensions.Logging;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;

/// <summary>
/// AutoCAD-backed implementation of <see cref="ISmartObjectMetadataService"/>.
/// Reads and writes metadata via XRecord stored in the object's Extension Dictionary.
/// </summary>
/// <remarks>
/// AutoCAD is treated as a black-box UI layer. All data persists in the DWG via XRecord;
/// this service is the only component allowed to cross the AutoCAD API boundary for metadata I/O.
/// </remarks>
internal sealed class SmartObjectMetadataService(
    IDrawingObjectMetadataStore store,
    ILogger<SmartObjectMetadataService> logger) : ISmartObjectMetadataService
{
    private readonly IDrawingObjectMetadataStore _store = store
        ?? throw new ArgumentNullException(nameof(store));
    private readonly ILogger<SmartObjectMetadataService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public string GetCurrentDrawingId()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        return doc?.Database.FingerprintGuid.ToString() ?? "no-active-document";
    }

    /// <inheritdoc/>
    public Result<SmartObjectMetadata> ReadMetadata(string objectHandle)
    {
        if (string.IsNullOrWhiteSpace(objectHandle))
        {
            return Result.Failure<SmartObjectMetadata>("Object handle cannot be empty.");
        }

        try
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc is null)
            {
                return Result.Failure<SmartObjectMetadata>("No active AutoCAD document.");
            }

            Database db = doc.Database;

            using Transaction tr = db.TransactionManager.StartTransaction();

            if (!db.TryGetObjectId(new Handle(Convert.ToInt64(objectHandle, 16)), out ObjectId objectId))
            {
                _logger.LogWarning("ReadMetadata: handle '{Handle}' not found in active document.", objectHandle);
                return Result.Failure<SmartObjectMetadata>($"Object with handle '{objectHandle}' was not found.");
            }

            using DBObject dbObject = tr.GetObject(objectId, OpenMode.ForRead);

            IReadOnlyDictionary<string, string> fields = _store.ReadFields(dbObject, tr);
            string version = fields.TryGetValue("__Version", out string? v) ? v : string.Empty;

            tr.Commit();

            _logger.LogInformation("ReadMetadata: read {Count} field(s) for handle '{Handle}'.",
                fields.Count, objectHandle);

            return Result.Success(SmartObjectMetadata.From(fields, version));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadMetadata failed for handle '{Handle}'.", objectHandle);
            return Result.Failure<SmartObjectMetadata>($"Failed to read metadata: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result WriteMetadata(string objectHandle, SmartObjectMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(objectHandle))
        {
            return Result.Failure("Object handle cannot be empty.");
        }

        if (metadata is null)
        {
            return Result.Failure("Metadata cannot be null.");
        }

        try
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc is null)
            {
                return Result.Failure("No active AutoCAD document.");
            }

            Database db = doc.Database;

            using Autodesk.AutoCAD.ApplicationServices.DocumentLock docLock = doc.LockDocument();
            using Transaction tr = db.TransactionManager.StartTransaction();

            if (!db.TryGetObjectId(new Handle(Convert.ToInt64(objectHandle, 16)), out ObjectId objectId))
            {
                _logger.LogWarning("WriteMetadata: handle '{Handle}' not found in active document.", objectHandle);
                return Result.Failure($"Object with handle '{objectHandle}' was not found.");
            }

            using DBObject dbObject = tr.GetObject(objectId, OpenMode.ForWrite);

            var fieldsCopy = new Dictionary<string, string>(metadata.Fields, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(metadata.Version))
            {
                fieldsCopy["__Version"] = metadata.Version;
            }

            _store.WriteFields(dbObject, tr, fieldsCopy);

            tr.Commit();

            _logger.LogInformation("WriteMetadata: wrote {Count} field(s) for handle '{Handle}'.",
                metadata.Fields.Count, objectHandle);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WriteMetadata failed for handle '{Handle}'.", objectHandle);
            return Result.Failure($"Failed to write metadata: {ex.Message}");
        }
    }
}

