using Autodesk.AutoCAD.DatabaseServices;
using CabinetBilder.Core.FrontMatter;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.TypeStore;

/// <summary>
/// Defines read and write operations for drawing type values.
/// </summary>
internal interface IDrawingTypeStore
{
    /// <summary>
    /// Gets known type values by using cache, store and optional full refresh scan.
    /// </summary>
    /// <param name="db">An AutoCAD drawing database.</param>
    /// <param name="forceRefresh">A value that indicates whether to force layout rescan.</param>
    /// <returns>A sorted list of known type values.</returns>
    List<string> GetKnownTypes(Database db, bool forceRefresh);

    /// <summary>
    /// Adds a type value to store and cache if it does not already exist.
    /// </summary>
    /// <param name="db">An AutoCAD drawing database.</param>
    /// <param name="type">A type value.</param>
    void AddType(Database db, string type);
}

/// <summary>
/// Provides XRecord-backed type value persistence for a drawing.
/// </summary>
internal sealed class DrawingTypeStore : IDrawingTypeStore
{
    private const string TypeDictionaryName = "CB_FRONT_MATTER";
    private const string TypeListRecordName = "TYPE_LIST";
    private const string DefaultTypeValue = "SzabĂˇszat";
    private static readonly Dictionary<string, List<string>> CachedTypesByDrawing = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets known type values by using cache, store and optional full refresh scan.
    /// </summary>
    /// <param name="db">An AutoCAD drawing database.</param>
    /// <param name="forceRefresh">A value that indicates whether to force layout rescan.</param>
    /// <returns>A sorted list of known type values.</returns>
    public List<string> GetKnownTypes(Database db, bool forceRefresh)
    {
        ArgumentNullException.ThrowIfNull(db);

        string cacheKey = GetDrawingCacheKey(db);
        if (!forceRefresh && CachedTypesByDrawing.TryGetValue(cacheKey, out List<string>? cached))
        {
            return new List<string>(cached);
        }

        List<string> typesFromStore = LoadTypesFromStore(db);
        if (typesFromStore.Count > 0 && !forceRefresh)
        {
            CachedTypesByDrawing[cacheKey] = new List<string>(typesFromStore);
            return typesFromStore;
        }

        List<string> scannedTypes = ScanTypesFromAllLayouts(db);
        HashSet<string> merged = new(scannedTypes, StringComparer.OrdinalIgnoreCase);
        foreach (string storedType in typesFromStore)
        {
            merged.Add(storedType);
        }

        List<string> finalTypes = new(merged);
        finalTypes.Sort(StringComparer.OrdinalIgnoreCase);

        if (finalTypes.Count == 0)
        {
            finalTypes.Add(DefaultTypeValue);
        }

        StoreTypes(db, finalTypes);
        CachedTypesByDrawing[cacheKey] = new List<string>(finalTypes);
        return finalTypes;
    }

    /// <summary>
    /// Adds a type value to store and cache if it does not already exist.
    /// </summary>
    /// <param name="db">An AutoCAD drawing database.</param>
    /// <param name="type">A type value.</param>
    public void AddType(Database db, string type)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type value cannot be empty.", nameof(type));
        }

        List<string> currentTypes = LoadTypesFromStore(db);
        if (!currentTypes.Exists(existing => string.Equals(existing, type, StringComparison.OrdinalIgnoreCase)))
        {
            currentTypes.Add(type);
            currentTypes.Sort(StringComparer.OrdinalIgnoreCase);
            StoreTypes(db, currentTypes);
        }

        string cacheKey = GetDrawingCacheKey(db);
        if (!CachedTypesByDrawing.TryGetValue(cacheKey, out List<string>? cached))
        {
            CachedTypesByDrawing[cacheKey] = new List<string>(currentTypes);
            return;
        }

        if (!cached.Exists(existing => string.Equals(existing, type, StringComparison.OrdinalIgnoreCase)))
        {
            cached.Add(type);
            cached.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static List<string> ScanTypesFromAllLayouts(Database db)
    {
        List<string> scannedTypes = new();

        using OpenCloseTransaction transaction = db.TransactionManager.StartOpenCloseTransaction();
        BlockTable? blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        if (blockTable is null)
        {
            return scannedTypes;
        }

        foreach (ObjectId blockRecordId in blockTable)
        {
            BlockTableRecord? blockTableRecord = transaction.GetObject(blockRecordId, OpenMode.ForRead) as BlockTableRecord;
            if (blockTableRecord is null || !blockTableRecord.IsLayout)
            {
                continue;
            }

            foreach (ObjectId objectId in blockTableRecord)
            {
                Entity? textEntity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
                if (textEntity is null || textEntity is not MText && textEntity is not DBText)
                {
                    continue;
                }

                Dictionary<string, string> entries = FrontMatterTextService.ParseEntries(ReadTextContent(textEntity));
                if (!entries.TryGetValue(FrontMatterKeys.Type, out string? typeValue) || string.IsNullOrWhiteSpace(typeValue))
                {
                    continue;
                }

                if (!scannedTypes.Exists(existing => string.Equals(existing, typeValue, StringComparison.OrdinalIgnoreCase)))
                {
                    scannedTypes.Add(typeValue);
                }
            }
        }

        scannedTypes.Sort(StringComparer.OrdinalIgnoreCase);
        return scannedTypes;
    }

    private static List<string> LoadTypesFromStore(Database db)
    {
        List<string> values = new();

        using OpenCloseTransaction transaction = db.TransactionManager.StartOpenCloseTransaction();

        DBDictionary? rootDictionary = transaction.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
        if (rootDictionary is null || !rootDictionary.Contains(TypeDictionaryName))
        {
            return values;
        }

        DBDictionary? typeDictionary = transaction.GetObject(rootDictionary.GetAt(TypeDictionaryName), OpenMode.ForRead) as DBDictionary;
        if (typeDictionary is null || !typeDictionary.Contains(TypeListRecordName))
        {
            return values;
        }

        Xrecord? xrecord = transaction.GetObject(typeDictionary.GetAt(TypeListRecordName), OpenMode.ForRead) as Xrecord;
        if (xrecord?.Data is null)
        {
            return values;
        }

        foreach (TypedValue item in xrecord.Data)
        {
            string? text = item.Value as string;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (!values.Exists(existing => string.Equals(existing, text, StringComparison.OrdinalIgnoreCase)))
            {
                values.Add(text);
            }
        }

        values.Sort(StringComparer.OrdinalIgnoreCase);
        return values;
    }

    private static void StoreTypes(Database db, IReadOnlyList<string> values)
    {
        using OpenCloseTransaction transaction = db.TransactionManager.StartOpenCloseTransaction();

        DBDictionary? rootDictionary = transaction.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
        if (rootDictionary is null)
        {
            return;
        }

        DBDictionary typeDictionary;
        if (rootDictionary.Contains(TypeDictionaryName))
        {
            typeDictionary = (DBDictionary)transaction.GetObject(rootDictionary.GetAt(TypeDictionaryName), OpenMode.ForWrite);
        }
        else
        {
            rootDictionary.UpgradeOpen();
            typeDictionary = new DBDictionary();
            rootDictionary.SetAt(TypeDictionaryName, typeDictionary);
            transaction.AddNewlyCreatedDBObject(typeDictionary, true);
        }

        TypedValue[] typedValues = new TypedValue[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            typedValues[index] = new TypedValue((int)DxfCode.Text, values[index]);
        }

        if (typeDictionary.Contains(TypeListRecordName))
        {
            Xrecord existing = (Xrecord)transaction.GetObject(typeDictionary.GetAt(TypeListRecordName), OpenMode.ForWrite);
            existing.Data = new ResultBuffer(typedValues);
            transaction.Commit();
            return;
        }

        Xrecord xrecord = new()
        {
            Data = new ResultBuffer(typedValues)
        };

        typeDictionary.SetAt(TypeListRecordName, xrecord);
        transaction.AddNewlyCreatedDBObject(xrecord, true);
        transaction.Commit();
    }

    private static string GetDrawingCacheKey(Database db)
    {
        string? fingerprint = db.FingerprintGuid;
        if (!string.IsNullOrWhiteSpace(fingerprint))
        {
            return fingerprint;
        }

        return db.UnmanagedObject.ToString();
    }

    private static string ReadTextContent(Entity textEntity)
    {
        if (textEntity is MText mText)
        {
            return FrontMatterTextService.NormalizeLineBreaks(mText.Contents.Replace("\\P", "\n", StringComparison.Ordinal));
        }

        if (textEntity is DBText dbText)
        {
            return FrontMatterTextService.NormalizeLineBreaks(dbText.TextString);
        }

        return string.Empty;
    }
}

