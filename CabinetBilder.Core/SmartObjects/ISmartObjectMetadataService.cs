using CabinetBilder.Core.Common;

namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Port interface for reading and writing smart object metadata.
/// Implementations live in the Infrastructure layer and are AutoCAD-aware.
/// </summary>
public interface ISmartObjectMetadataService
{
    /// <summary>
    /// Gets a unique identifier for the currently active drawing (e.g. fingerprint or path).
    /// </summary>
    string GetCurrentDrawingId();

    /// <summary>
    /// Reads all metadata fields for the object identified by <paramref name="objectHandle"/>.
    /// </summary>
    /// <param name="objectHandle">The AutoCAD handle string of the target object.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the metadata on success,
    /// or a failure description if the object cannot be found or has no metadata.
    /// </returns>
    Result<SmartObjectMetadata> ReadMetadata(string objectHandle);

    /// <summary>
    /// Writes the supplied <paramref name="metadata"/> fields to the object identified by <paramref name="objectHandle"/>.
    /// </summary>
    /// <param name="objectHandle">The AutoCAD handle string of the target object.</param>
    /// <param name="metadata">The metadata fields to persist.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or a failure description.
    /// </returns>
    Result WriteMetadata(string objectHandle, SmartObjectMetadata metadata);
}

