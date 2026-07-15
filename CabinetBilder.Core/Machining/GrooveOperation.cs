using System;
using CabinetBilder.Core.Common;

namespace CabinetBilder.Core.Machining
{
    /// <summary>
    /// Represents a groove (slot) milling operation on a panel, e.g. a back-panel slot.
    /// Coordinates follow the Z-plane rule: Z = 0 is the panel face, the groove is cut
    /// into the material towards -Z, while <see cref="Depth"/> is the positive cutting
    /// depth measured from the face.
    /// (X, Y) is the start point of the groove centerline in the panel's local
    /// coordinate system; the centerline runs along the Direction vector for Length mm.
    /// </summary>
    public record GrooveOperation : MachiningOperation
    {
        /// <summary>Groove width (tool width) in mm, perpendicular to the direction.</summary>
        public double Width { get; init; }

        /// <summary>Positive cutting depth in mm, measured downwards from the face (Z = 0).</summary>
        public double Depth { get; init; }

        /// <summary>Length of the groove centerline in mm.</summary>
        public double Length { get; init; }

        // Direction of the groove's length in the panel's local coordinate system.
        // For face grooves this lies in the Z = 0 plane (DirectionZ = 0).
        public double DirectionX { get; init; } = 1;
        public double DirectionY { get; init; }
        public double DirectionZ { get; init; }

        /// <summary>True when the groove penetrates the full panel thickness.</summary>
        public bool IsThrough { get; init; }

        /// <summary>End point of the groove centerline in local coordinates.</summary>
        public (double X, double Y, double Z) GetEndPoint()
        {
            var (dx, dy, dz) = GetNormalizedDirection();
            return (X + dx * Length, Y + dy * Length, Z + dz * Length);
        }

        /// <summary>
        /// Bottom of the groove on the Z axis. Z = 0 is the face and the cut goes
        /// towards -Z, so the bottom is at Z - depth.
        /// </summary>
        public double GetBottomZ(double panelThickness)
            => Z - GetEffectiveDepth(panelThickness);

        /// <summary>Effective cutting depth: full thickness for through grooves.</summary>
        public double GetEffectiveDepth(double panelThickness)
            => IsThrough ? panelThickness : Depth;

        /// <summary>
        /// Validates the groove against the hosting panel's dimensions
        /// (panel local space: X in [0, panelWidth], Y in [0, panelHeight]).
        /// </summary>
        public Result Validate(double panelWidth, double panelHeight, double panelThickness)
        {
            if (Width <= 0)
            {
                return Result.Failure("Groove width must be positive.");
            }

            if (Length <= 0)
            {
                return Result.Failure("Groove length must be positive.");
            }

            if (!IsThrough && Depth <= 0)
            {
                return Result.Failure("Groove depth must be positive for non-through grooves.");
            }

            if (GetEffectiveDepth(panelThickness) > panelThickness + Tolerance)
            {
                return Result.Failure(
                    $"Groove depth ({GetEffectiveDepth(panelThickness)}) exceeds panel thickness ({panelThickness}).");
            }

            double dirLengthSq = DirectionX * DirectionX + DirectionY * DirectionY + DirectionZ * DirectionZ;
            if (dirLengthSq < Tolerance * Tolerance)
            {
                return Result.Failure("Groove direction vector must not be zero.");
            }

            if (!IsInsidePanel(X, Y, panelWidth, panelHeight))
            {
                return Result.Failure($"Groove start point ({X}, {Y}) lies outside the panel boundaries.");
            }

            var end = GetEndPoint();
            if (!IsInsidePanel(end.X, end.Y, panelWidth, panelHeight))
            {
                return Result.Failure($"Groove end point ({end.X}, {end.Y}) lies outside the panel boundaries.");
            }

            return Result.Success();
        }

        private const double Tolerance = 0.001;

        private static bool IsInsidePanel(double x, double y, double panelWidth, double panelHeight)
            => x >= -Tolerance && x <= panelWidth + Tolerance
            && y >= -Tolerance && y <= panelHeight + Tolerance;

        private (double X, double Y, double Z) GetNormalizedDirection()
        {
            double length = Math.Sqrt(DirectionX * DirectionX + DirectionY * DirectionY + DirectionZ * DirectionZ);
            if (length < Tolerance)
            {
                return (0, 0, 0);
            }
            return (DirectionX / length, DirectionY / length, DirectionZ / length);
        }
    }
}
