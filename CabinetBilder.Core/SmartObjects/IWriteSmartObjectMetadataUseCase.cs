using CabinetBilder.Core.Common;
using CabinetBilder.Core.SmartObjects;

namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Port interface for the write metadata use-case.
/// Defined in Core so the UI layer depends only on Core, not on Application internals.
/// </summary>
public interface IWriteSmartObjectMetadataUseCase
{
    /// <summary>
    /// Persists the given metadata to the objects identified by <paramref name="objectHandles"/>.
    /// </summary>
    Result Execute(IEnumerable<string> objectHandles, SmartObjectMetadata metadata);
}

