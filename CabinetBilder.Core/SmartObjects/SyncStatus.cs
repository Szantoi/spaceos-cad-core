namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Represents the synchronization state of a smart object compared to the central server.
/// </summary>
public enum SyncStatus
{
    /// <summary>Local metadata is identical to server version.</summary>
    UpToDate,
    
    /// <summary>Object exists locally but not on the central server.</summary>
    LocalOnly,
    
    /// <summary>Local metadata has been modified since the last sync.</summary>
    ModifiedLocally,
    
    /// <summary>Server has a newer version than what is currently in the drawing.</summary>
    Outdated,
    
    /// <summary>Both local and server metadata have changed independently (Git conflict).</summary>
    Conflict
}

