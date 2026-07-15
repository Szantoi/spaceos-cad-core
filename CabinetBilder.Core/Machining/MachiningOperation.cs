using System;
using System.Text.Json.Serialization;

namespace CabinetBilder.Core.Machining
{
    /// <summary>
    /// Base record for all machining operations on a panel.
    /// Positions are relative to the panel's local coordinate system.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(DrillOperation), "drill")]
    [JsonDerivedType(typeof(GrooveOperation), "groove")]
    public abstract record MachiningOperation
    {
        public string Name { get; init; } = string.Empty;
        
        // Local position on the panel (relative to panel origin)
        public double X { get; init; }
        public double Y { get; init; }
        public double Z { get; init; }
    }

    /// <summary>
    /// Represents a single drilling operation.
    /// </summary>
    public record DrillOperation : MachiningOperation
    {
        public double Diameter { get; init; }
        public double Depth { get; init; }
        
        // Normal vector of the drilling (usually (0,0,1) for face drilling, 
        // or (1,0,0)/(0,1,0) for edge drilling)
        public double NormalX { get; init; } = 0;
        public double NormalY { get; init; } = 0;
        public double NormalZ { get; init; } = 1;

        public bool IsThroughHole => Depth <= 0;
    }
}
