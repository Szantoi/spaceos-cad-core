using Ardalis.Result;

namespace CabinetBilder.Core.Sync;

/// <summary>
/// Defines the port for protecting sensitive data using machine-specific or user-specific encryption.
/// As specified in Architecture Vision v2 §8.
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Protects the given data.
    /// </summary>
    Task<Result<byte[]>> ProtectAsync(byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Unprotects the given data.
    /// </summary>
    Task<Result<byte[]>> UnprotectAsync(byte[] protectedData, CancellationToken ct = default);

    /// <summary>
    /// Helper to protect a string (e.g. JSON or a decimal string).
    /// </summary>
    Task<Result<byte[]>> ProtectStringAsync(string data, CancellationToken ct = default);

    /// <summary>
    /// Helper to unprotect data back to a string.
    /// </summary>
    Task<Result<string>> UnprotectStringAsync(byte[] protectedData, CancellationToken ct = default);
}
