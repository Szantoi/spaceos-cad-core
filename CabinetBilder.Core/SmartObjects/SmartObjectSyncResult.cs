namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Result of a synchronization status check for a smart object.
/// </summary>
public record SmartObjectSyncResult(string Handle, SyncStatus Status, string? ServerVersion = null);

