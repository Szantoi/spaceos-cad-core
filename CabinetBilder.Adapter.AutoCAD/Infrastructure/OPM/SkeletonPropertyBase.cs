using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PropertyInspector;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.OPM
{
    /// <summary>
    /// Base class for Skeleton properties shown in AutoCAD Properties Palette.
    /// </summary>
    public abstract class SkeletonPropertyBase : IDynamicProperty
    {
        protected readonly ICadSkeletonStore _skeletonStore;

        protected SkeletonPropertyBase(ICadSkeletonStore skeletonStore)
        {
            _skeletonStore = skeletonStore;
        }

        public abstract string PropertyName { get; }
        public abstract string Description { get; }
        public string Category => "CabinetBilder - Skeleton";
        
        // This GUID should be unique for each property type
        public abstract string GUID { get; }

        public void GetPropertyValue(object pObject, ref object pVar)
        {
            if (pObject is not Entity entity) return;

            var skeleton = _skeletonStore.GetSkeleton(entity.Id);
            if (skeleton == null) return;

            pVar = GetValue(skeleton);
        }

        public void SetPropertyValue(object pObject, object var)
        {
            if (pObject is not Entity entity) return;

            var skeleton = _skeletonStore.GetSkeleton(entity.Id);
            if (skeleton == null) return;

            if (TrySetValue(skeleton, var))
            {
                skeleton.Rebuild();
                _skeletonStore.SaveSkeleton(entity.Id, skeleton);
                
                // Sync geometry
                CabinetBilder.Adapter.AutoCAD.Infrastructure.Geometry.SkeletonSyncService.Sync(entity.Id, skeleton);

                // Trigger graphics update
                entity.RecordGraphicsModified(true);
            }
        }

        public abstract object GetValue(Skeleton skeleton);
        public abstract bool TrySetValue(Skeleton skeleton, object value);

        public void GetPropertyType(object pObject, ref ushort pVar)
        {
            // 5 = double, 8 = string, etc. (COM types)
            pVar = GetPropertyCOMType();
        }

        protected abstract ushort GetPropertyCOMType();

        public void IsPropertyReadOnly(object pObject, ref int bIsReadOnly)
        {
            bIsReadOnly = 0; // Not read-only
        }

        public void Connect(IDynamicPropertyNotify pNotify) { }
        public void Disconnect() { }
    }
}
