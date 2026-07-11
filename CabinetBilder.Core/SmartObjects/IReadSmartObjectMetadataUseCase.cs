using CabinetBilder.Core.Common;
using CabinetBilder.Core.SmartObjects;

namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Port interface for the read metadata use-case.
/// Defined in Core so the UI layer depends only on Core, not on Application internals.
/// </summary>
public interface IReadSmartObjectMetadataUseCase
{
    /// <summary>
    /// Reads and merges smart object metadata for the given object handles.
    /// </summary>
    Result<SmartObjectMetadata> Execute(IEnumerable<string> objectHandles);
}

