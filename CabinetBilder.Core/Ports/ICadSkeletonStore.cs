using System.Threading.Tasks;
using CabinetBilder.Core.Common;
using CabinetBilder.Core.Skeletons;

namespace CabinetBilder.Core.Ports;

/// <summary>
/// Port for persisting and retrieving Skeleton data from CAD objects.
/// </summary>
public interface ICadSkeletonStore
{
    /// <summary>
    /// Reads a Skeleton from an AutoCAD object's extension dictionary.
    /// </summary>
    /// <param name="objectHandle">The handle of the CAD object (string format).</param>
    /// <returns>The Skeleton if found, otherwise null.</returns>
    Task<Skeleton?> ReadSkeletonAsync(string objectHandle);

    /// <summary>
    /// Writes a Skeleton to an AutoCAD object's extension dictionary.
    /// </summary>
    /// <param name="objectHandle">The handle of the CAD object (string format).</param>
    /// <param name="skeleton">The Skeleton instance to persist.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> WriteSkeletonAsync(string objectHandle, Skeleton skeleton);
}
