using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PropertyInspector;
using Autodesk.AutoCAD.Runtime;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;
using Microsoft.Extensions.DependencyInjection;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.OPM
{
    /// <summary>
    /// Handles registration of Skeleton properties with AutoCAD's Object Property Manager (OPM).
    /// </summary>
    public static class SkeletonPropertyManager
    {
        private static readonly List<IDynamicProperty> _properties = new();

        public static void Register()
        {
            try
            {
                var store = AutoCadPlugin.ServiceProvider.GetRequiredService<ICadSkeletonStore>();
                
                // Define properties to register
                _properties.Add(new SkeletonWidthProperty(store));
                _properties.Add(new SkeletonHeightProperty(store));
                _properties.Add(new SkeletonDepthProperty(store));
                _properties.Add(new SkeletonThicknessProperty(store));
                _properties.Add(new SkeletonBackPanelOffsetProperty(store));

                // Get Property Manager for BlockReference
                var blockRefClass = RXObject.GetClass(typeof(BlockReference));
                var pm = PropertyManager.GetPropertyManager(blockRefClass);

                foreach (var prop in _properties)
                {
                    pm.AddProperty(prop);
                }
            }
            catch (Exception ex)
            {
                // In AutoCAD, we should probably log this to the editor
                // AcadApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\n[CabinetBilder] Error registering OPM properties: {ex.Message}\n");
            }
        }

        public static void Unregister()
        {
            try
            {
                var blockRefClass = RXObject.GetClass(typeof(BlockReference));
                var pm = PropertyManager.GetPropertyManager(blockRefClass);

                foreach (var prop in _properties)
                {
                    pm.RemoveProperty(prop);
                }
                
                _properties.Clear();
            }
            catch
            {
                // Ignore errors during termination
            }
        }
    }
}
