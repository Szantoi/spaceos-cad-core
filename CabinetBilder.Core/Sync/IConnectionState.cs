namespace CabinetBilder.Core.Sync;

public enum ConnectionStatus
{
    Unauthenticated,
    Authenticating,
    Online,
    Offline
}

public interface IConnectionState
{
    ConnectionStatus Status { get; }
    string? ActiveTenantId { get; }
    string? UserDisplayName { get; }
    DateTimeOffset? LastSyncTime { get; }
}
