using System;

namespace CabinetBilder.Core.Skeletons;

/// <summary>
/// Unique identifier for a Skeleton aggregate.
/// </summary>
public record struct SkeletonId(Guid Value)
{
    public static SkeletonId New() => new(Guid.NewGuid());
    public static readonly SkeletonId Empty = new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
