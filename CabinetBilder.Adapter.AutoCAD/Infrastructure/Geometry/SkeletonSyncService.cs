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
        public const string GroovesLayer = "CB_Grooves";

        /// <summary>Alpha for groove bodies (0 = invisible, 255 = opaque): semi-transparent red.</summary>
        private const byte GrooveTransparencyAlpha = 127;

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
            EnsureLayer(db, tr, GroovesLayer, 1);   // Red for grooves (bodies are also semi-transparent)

            // 1. Remove old generated geometry and drillings
            foreach (ObjectId entId in btr)
            {
                var ent = tr.GetObject(entId, OpenMode.ForWrite);
                if (ent is Solid3d || (ent is Circle circle && circle.Layer == DrillingsLayer))
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

                    // Grooves: distinct semi-transparent red bodies so their position
                    // on the panel can be verified visually. Erased and regenerated
                    // together with the panel solids above (both are Solid3d).
                    var grooves = SkeletonGeometryGenerator.CreateGrooveGraphics(component);
                    foreach (var groove in grooves)
                    {
                        groove.Layer = GroovesLayer;
                        groove.Transparency = new Autodesk.AutoCAD.Colors.Transparency(GrooveTransparencyAlpha);
                        btr.AppendEntity(groove);
                        tr.AddNewlyCreatedDBObject(groove, true);
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
