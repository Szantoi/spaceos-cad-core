using System;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.OPM
{
    public class SkeletonWidthProperty : SkeletonPropertyBase
    {
        public SkeletonWidthProperty(ICadSkeletonStore store) : base(store) { }

        public override string PropertyName => "Cabinet Width";
        public override string Description => "The total width of the cabinet skeleton.";
        public override string GUID => "B9B8A1C1-8B6C-4F7B-8B1B-1C1C1C1C1C1C"; // Unique GUID

        public override object GetValue(Skeleton skeleton) => skeleton.Width;

        public override bool TrySetValue(Skeleton skeleton, object value)
        {
            if (value is double d)
            {
                skeleton.ApplyParameter("Width", d);
                return true;
            }
            return false;
        }

        protected override ushort GetPropertyCOMType() => 5; // VT_R8 (double)
    }

    public class SkeletonHeightProperty : SkeletonPropertyBase
    {
        public SkeletonHeightProperty(ICadSkeletonStore store) : base(store) { }

        public override string PropertyName => "Cabinet Height";
        public override string Description => "The total height of the cabinet skeleton.";
        public override string GUID => "C9B8A1C1-8B6C-4F7B-8B1B-1C1C1C1C1C1C";

        public override object GetValue(Skeleton skeleton) => skeleton.Height;

        public override bool TrySetValue(Skeleton skeleton, object value)
        {
            if (value is double d)
            {
                skeleton.ApplyParameter("Height", d);
                return true;
            }
            return false;
        }

        protected override ushort GetPropertyCOMType() => 5;
    }

    public class SkeletonDepthProperty : SkeletonPropertyBase
    {
        public SkeletonDepthProperty(ICadSkeletonStore store) : base(store) { }

        public override string PropertyName => "Cabinet Depth";
        public override string Description => "The total depth of the cabinet skeleton.";
        public override string GUID => "D9B8A1C1-8B6C-4F7B-8B1B-1C1C1C1C1C1C";

        public override object GetValue(Skeleton skeleton) => skeleton.Depth;

        public override bool TrySetValue(Skeleton skeleton, object value)
        {
            if (value is double d)
            {
                skeleton.ApplyParameter("Depth", d);
                return true;
            }
            return false;
        }

        protected override ushort GetPropertyCOMType() => 5;
    }

    public class SkeletonThicknessProperty : SkeletonPropertyBase
    {
        public SkeletonThicknessProperty(ICadSkeletonStore store) : base(store) { }

        public override string PropertyName => "Material Thickness";
        public override string Description => "The thickness of the panels.";
        public override string GUID => "E9B8A1C1-8B6C-4F7B-8B1B-1C1C1C1C1C1C";

        public override object GetValue(Skeleton skeleton) => skeleton.Thickness;

        public override bool TrySetValue(Skeleton skeleton, object value)
        {
            if (value is double d)
            {
                skeleton.ApplyParameter("Thickness", d);
                return true;
            }
            return false;
        }

        protected override ushort GetPropertyCOMType() => 5;
    }

    public class SkeletonBackPanelOffsetProperty : SkeletonPropertyBase
    {
        public SkeletonBackPanelOffsetProperty(ICadSkeletonStore store) : base(store) { }

        public override string PropertyName => "Back Panel Offset";
        public override string Description => "The offset of the back panel from the rear.";
        public override string GUID => "F9B8A1C1-8B6C-4F7B-8B1B-1C1C1C1C1C1C";

        public override object GetValue(Skeleton skeleton) => skeleton.BackPanelOffset;

        public override bool TrySetValue(Skeleton skeleton, object value)
        {
            if (value is double d)
            {
                skeleton.ApplyParameter("BackPanelOffset", d);
                return true;
            }
            return false;
        }

        protected override ushort GetPropertyCOMType() => 5;
    }
}
