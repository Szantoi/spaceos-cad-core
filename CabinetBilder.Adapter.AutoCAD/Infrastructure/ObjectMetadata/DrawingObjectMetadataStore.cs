using Autodesk.AutoCAD.DatabaseServices;
using CabinetBilder.Core.SmartObjects;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;

/// <summary>
/// Defines read and write operations for AutoCAD object metadata.
/// </summary>
public interface IDrawingObjectMetadataStore
{
    /// <summary>
    /// Writes a schema marker to an AutoCAD object metadata dictionary.
    /// </summary>
    /// <param name="dbObject">A target AutoCAD database object.</param>
    /// <param name="transaction">An active transaction.</param>
    /// <param name="schemaId">An optional schema identifier.</param>
    void MarkAsSmartObject(DBObject dbObject, Transaction transaction, string? schemaId = null);

    /// <summary>
    /// Tries to read a schema marker from an AutoCAD object metadata dictionary.
    /// </summary>
    /// <param name="dbObject">A target AutoCAD database object.</param>
    /// <param name="transaction">An active transaction.</param>
    /// <param name="schemaId">When this method returns, contains the schema identifier if it exists.</param>
    /// <returns><see langword="true" /> if a schema marker was found; otherwise, <see langword="false" />.</returns>
    bool TryGetSchemaId(DBObject dbObject, Transaction transaction, out string? schemaId);

    /// <summary>
    /// Reads all text metadata fields stored under the smart object metadata dictionary.
    /// </summary>
    /// <param name="dbObject">A target AutoCAD database object.</param>
    /// <param name="transaction">An active transaction.</param>
    /// <returns>A read-only dictionary of key-value pairs, or an empty dictionary if none found.</returns>
    IReadOnlyDictionary<string, string> ReadFields(DBObject dbObject, Transaction transaction);

    /// <summary>
    /// Writes a collection of text metadata fields to the smart object metadata dictionary.
    /// Existing values for matching keys are overwritten.
    /// </summary>
    /// <param name="dbObject">A target AutoCAD database object.</param>
    /// <param name="transaction">An active transaction.</param>
    /// <param name="fields">Key-value pairs to persist.</param>
    void WriteFields(DBObject dbObject, Transaction transaction, IReadOnlyDictionary<string, string> fields);
}

/// <summary>
/// Provides extension dictionary metadata persistence for AutoCAD objects.
/// </summary>
internal sealed class DrawingObjectMetadataStore : IDrawingObjectMetadataStore
{
    /// <summary>
    /// Writes a schema marker to an AutoCAD object metadata dictionary.
    /// </summary>
    /// <param name="dbObject">A target AutoCAD database object.</param>
    /// <param name="transaction">An active transaction.</param>
    /// <param name="schemaId">An optional schema identifier.</param>
    public void MarkAsSmartObject(DBObject dbObject, Transaction transaction, string? schemaId = null)
    {
        ArgumentNullException.ThrowIfNull(dbObject);
        ArgumentNullException.ThrowIfNull(transaction);

        string effectiveSchemaId = string.IsNullOrWhiteSpace(schemaId)
            ? SmartObjectSchema.DefaultSchemaId
            : schemaId;

        SetTextValue(
            dbObject,
            transaction,
            SmartObjectSchema.MetadataDictionaryName,
            SmartObjectSchema.SchemaMarkerKey,
            effectiveSchemaId);
    }

    /// <summary>
    /// Tries to read a schema marker from an AutoCAD object metadata dictionary.
    /// </summary>
    /// <param name="dbObject">A target AutoCAD database object.</param>
    /// <param name="transaction">An active transaction.</param>
    /// <param name="schemaId">When this method returns, contains the schema identifier if it exists.</param>
    /// <returns><see langword="true" /> if a schema marker was found; otherwise, <see langword="false" />.</returns>
    public bool TryGetSchemaId(DBObject dbObject, Transaction transaction, out string? schemaId)
    {
        return TryGetTextValue(
            dbObject,
            transaction,
            SmartObjectSchema.MetadataDictionaryName,
            SmartObjectSchema.SchemaMarkerKey,
            out schemaId);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> ReadFields(DBObject dbObject, Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(dbObject);
        ArgumentNullException.ThrowIfNull(transaction);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (dbObject.ExtensionDictionary == ObjectId.Null)
        {
            return result;
        }

        DBDictionary extensionDictionary = (DBDictionary)transaction.GetObject(
            dbObject.ExtensionDictionary, OpenMode.ForRead);

        if (!extensionDictionary.Contains(SmartObjectSchema.MetadataDictionaryName))
        {
            return result;
        }

        DBDictionary? metadataDictionary = transaction.GetObject(
            extensionDictionary.GetAt(SmartObjectSchema.MetadataDictionaryName), OpenMode.ForRead)
            as DBDictionary;

        if (metadataDictionary is null)
        {
            return result;
        }

        foreach (DBDictionaryEntry entry in metadataDictionary)
        {
            if (entry.Key == SmartObjectSchema.SchemaMarkerKey)
            {
                continue; // Skip the internal schema marker â€” it is not a domain field.
            }

            if (TryGetTextValue(dbObject, transaction,
                    SmartObjectSchema.MetadataDictionaryName, entry.Key, out string? value)
                && value is not null)
            {
                result[entry.Key] = value;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public void WriteFields(DBObject dbObject, Transaction transaction, IReadOnlyDictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(dbObject);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(fields);

        foreach (KeyValuePair<string, string> field in fields)
        {
            SetTextValue(
                dbObject,
                transaction,
                SmartObjectSchema.MetadataDictionaryName,
                field.Key,
                field.Value);
        }
    }

    private static void SetTextValue(DBObject dbObject, Transaction transaction, string dictionaryName, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(dictionaryName))
        {
            throw new ArgumentException("Dictionary name cannot be empty.", nameof(dictionaryName));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

        if (dbObject.ExtensionDictionary == ObjectId.Null)
        {
            dbObject.UpgradeOpen();
            dbObject.CreateExtensionDictionary();
        }

        DBDictionary extensionDictionary = (DBDictionary)transaction.GetObject(dbObject.ExtensionDictionary, OpenMode.ForWrite);

        DBDictionary metadataDictionary;
        if (extensionDictionary.Contains(dictionaryName))
        {
            metadataDictionary = (DBDictionary)transaction.GetObject(extensionDictionary.GetAt(dictionaryName), OpenMode.ForWrite);
        }
        else
        {
            metadataDictionary = new DBDictionary();
            extensionDictionary.SetAt(dictionaryName, metadataDictionary);
            transaction.AddNewlyCreatedDBObject(metadataDictionary, true);
        }

        ResultBuffer data = new(new TypedValue((int)DxfCode.Text, value));
        if (metadataDictionary.Contains(key))
        {
            Xrecord existing = (Xrecord)transaction.GetObject(metadataDictionary.GetAt(key), OpenMode.ForWrite);
            existing.Data = data;
            return;
        }

        Xrecord xrecord = new()
        {
            Data = data
        };

        metadataDictionary.SetAt(key, xrecord);
        transaction.AddNewlyCreatedDBObject(xrecord, true);
    }

    private static bool TryGetTextValue(DBObject dbObject, Transaction transaction, string dictionaryName, string key, out string? value)
    {
        ArgumentNullException.ThrowIfNull(dbObject);
        ArgumentNullException.ThrowIfNull(transaction);

        value = null;
        if (dbObject.ExtensionDictionary == ObjectId.Null)
        {
            return false;
        }

        DBDictionary extensionDictionary = (DBDictionary)transaction.GetObject(dbObject.ExtensionDictionary, OpenMode.ForRead);
        if (!extensionDictionary.Contains(dictionaryName))
        {
            return false;
        }

        DBDictionary? metadataDictionary = transaction.GetObject(extensionDictionary.GetAt(dictionaryName), OpenMode.ForRead) as DBDictionary;
        if (metadataDictionary is null || !metadataDictionary.Contains(key))
        {
            return false;
        }

        Xrecord? xrecord = transaction.GetObject(metadataDictionary.GetAt(key), OpenMode.ForRead) as Xrecord;
        if (xrecord?.Data is null)
        {
            return false;
        }

        foreach (TypedValue item in xrecord.Data)
        {
            if (item.TypeCode == (int)DxfCode.Text)
            {
                value = item.Value as string;
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        return false;
    }
}

