using System;
using Autodesk.AutoCAD.DatabaseServices;
using CabinetBilder.Core.Skeletons;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Geometry
{
    /// <summary>
    /// Synchronizes AutoCAD entities with Skeleton domain data.
    /// </summary>
    public static class SkeletonSyncService
    {
        public const string GeometryLayer = "CB_Geometry";
        public const string DrillingsLayer = "CB_Drillings";

        public static void Sync(ObjectId objectId, Skeleton skeleton)
        {
            var db = objectId.Database;
            using var tr = db.TransactionManager.StartTransaction();
            
            if (tr.GetObject(objectId, OpenMode.ForRead) is not BlockReference blkRef)
            {
                return;
            }

            var btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
            if (btr == null) return;

            // Ensure layers exist
            EnsureLayer(db, tr, GeometryLayer, 7);
            EnsureLayer(db, tr, DrillingsLayer, 1); // Red for drillings

            // 1. Remove old generated geometry and drillings
            foreach (ObjectId entId in btr)
            {
                var ent = tr.GetObject(entId, OpenMode.ForWrite);
                if (ent is Solid3d || (ent is Circle && ent.Layer == DrillingsLayer))
                {
                    ent.Erase();
                }
            }

            // 2. Generate new geometry and drillings
            foreach (var component in skeleton.Components)
            {
                try
                {
                    // Main solid
                    var solid = SkeletonGeometryGenerator.CreateSolid(component);
                    solid.Layer = GeometryLayer;
                    btr.AppendEntity(solid);
                    tr.AddNewlyCreatedDBObject(solid, true);

                    // Drillings
                    var drillings = SkeletonGeometryGenerator.CreateDrillingGraphics(component);
                    foreach (var drill in drillings)
                    {
                        drill.Layer = DrillingsLayer;
                        btr.AppendEntity(drill);
                        tr.AddNewlyCreatedDBObject(drill, true);
                    }
                }
                catch (Exception)
                {
                    // Log or handle geometry errors
                }
            }

            tr.Commit();
        }

        private static void EnsureLayer(Database db, Transaction tr, string name, short colorIndex)
        {
            var lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            if (lt != null && !lt.Has(name))
            {
                lt.UpgradeOpen();
                var ltr = new LayerTableRecord
                {
                    Name = name,
                    Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex)
                };
                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }
        }
    }
}
