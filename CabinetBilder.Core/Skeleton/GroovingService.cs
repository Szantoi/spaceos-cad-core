using System.Linq;
using CabinetBilder.Core.Machining;

namespace CabinetBilder.Core.Skeletons
{
    /// <summary>
    /// Generates the back-panel groove (hornyolt hátlap) on the carcass panels.
    ///
    /// Woodworking context: instead of an applied back (rátett hátlap) that sits on
    /// the rear edge, the back panel can be slotted into a groove milled into the
    /// inner face of the sides, top and bottom. This service produces that groove as
    /// a <see cref="GrooveOperation"/> on each carcass panel, mirroring the way
    /// <see cref="DrillingService"/> auto-generates dowel holes.
    ///
    /// Coordinate convention (identical to DrillingService):
    ///   - Side Left/Right: local X runs along the depth (front→back, 0..Width),
    ///     local Y runs along the height (0..Height). The back sits at the rear, so
    ///     the groove is a VERTICAL line at local X = Width − setback, running the
    ///     full height along local +Y.
    ///   - Bottom/Top: local X runs along the width (0..Width), local Y runs along
    ///     the depth (front→back, 0..Height). The groove is a HORIZONTAL line at
    ///     local Y = Height − setback, running the full width along local +X.
    /// </summary>
    public static class GroovingService
    {
        private const string BackComponentName = "Back";

        /// <summary>
        /// Adds a back-panel groove to every carcass panel (sides, top, bottom).
        /// No-op if the skeleton has no back panel to slot in.
        /// </summary>
        /// <param name="skeleton">The cabinet skeleton to machine.</param>
        /// <param name="grooveDepth">Cutting depth of the groove into the carcass panel (mm).</param>
        /// <param name="setback">Distance from the rear edge to the groove centerline (mm).</param>
        /// <param name="clearance">Extra groove width over the back thickness for a sliding fit (mm).</param>
        public static void ApplyBackPanelGrooving(Skeleton skeleton, double grooveDepth, double setback, double clearance)
        {
            var back = skeleton.Components.FirstOrDefault(c => c.Name == BackComponentName);
            if (back == null)
            {
                // No back panel modelled → nothing to slot; leave the carcass ungrooved.
                return;
            }

            double grooveWidth = back.Thickness + clearance;

            foreach (var panel in skeleton.Components)
            {
                switch (panel.Name)
                {
                    case "Side Left":
                    case "Side Right":
                        // Vertical groove near the rear, running the full panel height.
                        panel.Operations.Add(new GrooveOperation
                        {
                            Name = "Back Panel Groove",
                            X = panel.Width - setback, // Width == cabinet depth for a side
                            Y = 0,
                            Z = 0,
                            Width = grooveWidth,
                            Depth = grooveDepth,
                            Length = panel.Height,
                            DirectionX = 0,
                            DirectionY = 1,
                            DirectionZ = 0
                        });
                        break;

                    case "Bottom":
                    case "Top":
                        // Horizontal groove near the rear, running the full panel width.
                        panel.Operations.Add(new GrooveOperation
                        {
                            Name = "Back Panel Groove",
                            X = 0,
                            Y = panel.Height - setback, // Height == cabinet depth for top/bottom
                            Z = 0,
                            Width = grooveWidth,
                            Depth = grooveDepth,
                            Length = panel.Width,
                            DirectionX = 1,
                            DirectionY = 0,
                            DirectionZ = 0
                        });
                        break;
                }
            }
        }
    }
}
