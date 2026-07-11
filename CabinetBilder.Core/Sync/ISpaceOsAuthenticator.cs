using Ardalis.Result;

namespace CabinetBilder.Core.Sync;

public interface ISpaceOsAuthenticator
{
    IConnectionState CurrentState { get; }
    
    /// <summary>
    /// Starts the device code flow login process.
    /// Returns a DeviceCodeResponse containing the user code and verification URI.
    /// </summary>
    Task<Result<DeviceCodeResponse>> StartLoginAsync(CancellationToken ct = default);

    /// <summary>
    /// Completes the login process by polling for the token.
    /// </summary>
    Task<Result<AuthToken>> CompleteLoginAsync(DeviceCodeResponse response, CancellationToken ct = default);

    /// <summary>
    /// Logs out the specified tenant.
    /// </summary>
    Task LogoutAsync(string? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Ensures a valid token exists for the active tenant, refreshing if necessary.
    /// </summary>
    Task<Result<string>> GetValidTokenAsync(CancellationToken ct = default);
}

public record DeviceCodeResponse(string UserCode, string VerificationUri, string DeviceCode, int Interval, int ExpiresIn);

public record AuthToken(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, string? TenantId);
