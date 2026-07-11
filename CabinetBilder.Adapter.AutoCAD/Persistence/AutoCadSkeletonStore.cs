using System;
using System.Text.Json;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using CabinetBilder.Core.Common;
using CabinetBilder.Core.Ports;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Core.SmartObjects;

namespace CabinetBilder.Adapter.AutoCAD.Persistence;

/// <summary>
/// Implements ICadSkeletonStore using AutoCAD XRecords in the object's Extension Dictionary.
/// </summary>
public sealed class AutoCadSkeletonStore : ICadSkeletonStore
{
    private const string SkeletonDictionaryName = "CB_SKELETON";
    private const string SkeletonDataKey = "Data";

    public Task<Skeleton?> ReadSkeletonAsync(string objectHandle)
    {
        return Task.Run(() =>
        {
            var db = HostApplicationServices.WorkingDatabase;
            using var transaction = db.TransactionManager.StartTransaction();
            try
            {
                if (!db.TryGetObjectId(new Handle(Convert.ToInt64(objectHandle, 16)), out ObjectId id))
                {
                    return null;
                }

                var dbObject = transaction.GetObject(id, OpenMode.ForRead);
                if (dbObject.ExtensionDictionary == ObjectId.Null)
                {
                    return null;
                }

                var extDict = (DBDictionary)transaction.GetObject(dbObject.ExtensionDictionary, OpenMode.ForRead);
                if (!extDict.Contains(SkeletonDictionaryName))
                {
                    return null;
                }

                var skeletonDict = (DBDictionary)transaction.GetObject(extDict.GetAt(SkeletonDictionaryName), OpenMode.ForRead);
                if (!skeletonDict.Contains(SkeletonDataKey))
                {
                    return null;
                }

                var xrecord = (Xrecord)transaction.GetObject(skeletonDict.GetAt(SkeletonDataKey), OpenMode.ForRead);
                var json = ExtractJsonFromXrecord(xrecord);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<Skeleton>(json, new JsonSerializerOptions { IncludeFields = true });
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<Result> WriteSkeletonAsync(string objectHandle, Skeleton skeleton)
    {
        return Task.Run(() =>
        {
            var db = HostApplicationServices.WorkingDatabase;
            using var transaction = db.TransactionManager.StartTransaction();
            try
            {
                if (!db.TryGetObjectId(new Handle(Convert.ToInt64(objectHandle, 16)), out ObjectId id))
                {
                    return Result.Failure("Object not found.");
                }

                var dbObject = transaction.GetObject(id, OpenMode.ForWrite);
                if (dbObject.ExtensionDictionary == ObjectId.Null)
                {
                    dbObject.CreateExtensionDictionary();
                }

                var extDict = (DBDictionary)transaction.GetObject(dbObject.ExtensionDictionary, OpenMode.ForWrite);
                
                DBDictionary skeletonDict;
                if (extDict.Contains(SkeletonDictionaryName))
                {
                    skeletonDict = (DBDictionary)transaction.GetObject(extDict.GetAt(SkeletonDictionaryName), OpenMode.ForWrite);
                }
                else
                {
                    skeletonDict = new DBDictionary();
                    extDict.SetAt(SkeletonDictionaryName, skeletonDict);
                    transaction.AddNewlyCreatedDBObject(skeletonDict, true);
                }

                var json = JsonSerializer.Serialize(skeleton, new JsonSerializerOptions { IncludeFields = true });
                var xrecord = new Xrecord
                {
                    Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, json))
                };

                if (skeletonDict.Contains(SkeletonDataKey))
                {
                    var existing = (Xrecord)transaction.GetObject(skeletonDict.GetAt(SkeletonDataKey), OpenMode.ForWrite);
                    existing.Data = xrecord.Data;
                }
                else
                {
                    skeletonDict.SetAt(SkeletonDataKey, xrecord);
                    transaction.AddNewlyCreatedDBObject(xrecord, true);
                }

                transaction.Commit();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to write skeleton: {ex.Message}");
            }
        });
    }

    private static string? ExtractJsonFromXrecord(Xrecord xrecord)
    {
        if (xrecord.Data == null) return null;
        
        foreach (TypedValue item in xrecord.Data)
        {
            if (item.TypeCode == (int)DxfCode.Text)
            {
                return item.Value as string;
            }
        }
        return null;
    }
}
