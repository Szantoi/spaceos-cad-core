using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CabinetBilder.Core.Machining;
using CabinetBilder.Core.Skeletons;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Geometry
{
    /// <summary>
    /// Generates AutoCAD 3D geometry (Solid3d) from Skeleton components.
    /// </summary>
    public static class SkeletonGeometryGenerator
    {
        public static Solid3d CreateSolid(SkeletonComponent component)
        {
            var solid = new Solid3d();
            
            // CreateBox creates a box centered at origin with dimensions along X, Y, Z
            // We map: X -> Width, Y -> Height, Z -> Thickness
            solid.CreateBox(component.Width, component.Height, component.Thickness);

            // Calculate transformation matrix
            var normal = new Vector3d(component.NormalX, component.NormalY, component.NormalZ);
            var dirX = new Vector3d(component.DirX, component.DirY, component.DirZ);
            var dirY = normal.CrossProduct(dirX); // "Up" direction for the panel

            // Origin of the component (corner)
            var origin = new Point3d(component.PosX, component.PosY, component.PosZ);

            // The box is centered, so we shift it by half-dimensions to align its corner with (0,0,0)
            // then transform to the target orientation and position.
            var moveCenter = Matrix3d.Displacement(new Vector3d(component.Width / 2, component.Height / 2, component.Thickness / 2));
            
            var orient = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                Point3d.Origin, dirX, dirY, normal
            );

            var position = Matrix3d.Displacement(origin.GetAsVector());

            // Apply transformations: Center -> Corner -> Orient -> Position
            var fullTransform = position * orient * moveCenter;
            solid.TransformBy(fullTransform);

            return solid;
        }

        public static System.Collections.Generic.IEnumerable<Entity> CreateDrillingGraphics(SkeletonComponent component)
        {
            var graphics = new System.Collections.Generic.List<Entity>();

            // Calculate transformation matrix for the panel
            var normal = new Vector3d(component.NormalX, component.NormalY, component.NormalZ);
            var dirX = new Vector3d(component.DirX, component.DirY, component.DirZ);
            var dirY = normal.CrossProduct(dirX);
            var origin = new Point3d(component.PosX, component.PosY, component.PosZ);
            
            var panelTransform = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                origin, dirX, dirY, normal
            );

            foreach (var op in component.Operations)
            {
                if (op is DrillOperation drill)
                {
                    // Local position of the hole
                    var holePos = new Point3d(drill.X, drill.Y, drill.Z);
                    var holeNormal = new Vector3d(drill.NormalX, drill.NormalY, drill.NormalZ);
                    
                    // Create a circle to represent the hole
                    var circle = new Circle(holePos, holeNormal, drill.Diameter / 2);
                    
                    // Transform from local panel space to WCS
                    circle.TransformBy(panelTransform);
                    
                    graphics.Add(circle);
                }
            }

            return graphics;
        }

        /// <summary>
        /// Creates one Solid3d body per GrooveOperation of the component, in world
        /// coordinates. The groove follows the domain Z-plane rule: the operation's
        /// Z is the machined face level and the cut extends towards -Z by the
        /// effective depth (full panel thickness for through grooves).
        /// Only face grooves are visualized: the direction is projected onto the
        /// panel's XY plane; a groove running parallel to the panel normal is skipped.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<Entity> CreateGrooveGraphics(SkeletonComponent component)
        {
            var graphics = new System.Collections.Generic.List<Entity>();

            // Same panel-local -> WCS transform as the drilling graphics.
            var normal = new Vector3d(component.NormalX, component.NormalY, component.NormalZ);
            var dirX = new Vector3d(component.DirX, component.DirY, component.DirZ);
            var dirY = normal.CrossProduct(dirX);
            var origin = new Point3d(component.PosX, component.PosY, component.PosZ);

            var panelTransform = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                origin, dirX, dirY, normal
            );

            foreach (var op in component.Operations)
            {
                if (op is not GrooveOperation groove)
                {
                    continue;
                }

                double depth = groove.GetEffectiveDepth(component.Thickness);
                if (depth <= 0 || groove.Length <= 0 || groove.Width <= 0)
                {
                    continue; // Degenerate groove: nothing to draw.
                }

                // Groove length direction projected onto the panel face (Z = const plane).
                var grooveDir = new Vector3d(groove.DirectionX, groove.DirectionY, 0);
                if (grooveDir.Length < 1e-9)
                {
                    continue; // Direction parallel to the panel normal: not a face groove.
                }
                grooveDir = grooveDir.GetNormal();
                var grooveSide = Vector3d.ZAxis.CrossProduct(grooveDir); // Width direction, in-plane.

                // CreateBox is centered at the origin: X -> Length, Y -> Width, Z -> Depth.
                var solid = new Solid3d();
                solid.CreateBox(groove.Length, groove.Width, depth);

                // Body center: centerline midpoint, half depth below the face (cut goes to -Z).
                var center = new Point3d(
                    groove.X + grooveDir.X * groove.Length / 2,
                    groove.Y + grooveDir.Y * groove.Length / 2,
                    groove.Z - depth / 2);

                var orient = Matrix3d.AlignCoordinateSystem(
                    Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                    center, grooveDir, grooveSide, Vector3d.ZAxis
                );

                solid.TransformBy(panelTransform * orient);
                graphics.Add(solid);
            }

            return graphics;
        }
    }
}
