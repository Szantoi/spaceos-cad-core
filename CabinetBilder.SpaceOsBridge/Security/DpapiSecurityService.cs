using System.Security.Cryptography;
using System.Text;
using Ardalis.Result;
using CabinetBilder.Core.Sync;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace CabinetBilder.SpaceOsBridge.Security;

/// <summary>
/// Implementation of ISecurityService using Windows DPAPI (Data Protection API).
/// The data is protected for the current user on the current machine.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiSecurityService : ISecurityService
{
    private readonly ILogger<DpapiSecurityService> _logger;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CabinetBilder.Vision.V2.Entropy");

    public DpapiSecurityService(ILogger<DpapiSecurityService> logger)
    {
        _logger = logger;
    }

    public Task<Result<byte[]>> ProtectAsync(byte[] data, CancellationToken ct = default)
    {
        try
        {
            var protectedData = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
            return Task.FromResult(Result<byte[]>.Success(protectedData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to protect data using DPAPI.");
            return Task.FromResult(Result<byte[]>.Error("Encryption failed."));
        }
    }

    public Task<Result<byte[]>> UnprotectAsync(byte[] protectedData, CancellationToken ct = default)
    {
        try
        {
            var data = ProtectedData.Unprotect(protectedData, Entropy, DataProtectionScope.CurrentUser);
            return Task.FromResult(Result<byte[]>.Success(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unprotect data using DPAPI. This usually means the data was encrypted by a different user or on a different machine.");
            return Task.FromResult(Result<byte[]>.Error("Decryption failed."));
        }
    }

    public async Task<Result<byte[]>> ProtectStringAsync(string data, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(data)) return Result.Error("Cannot protect empty string.");
        var bytes = Encoding.UTF8.GetBytes(data);
        return await ProtectAsync(bytes, ct);
    }

    public async Task<Result<string>> UnprotectStringAsync(byte[] protectedData, CancellationToken ct = default)
    {
        var result = await UnprotectAsync(protectedData, ct);
        if (!result.IsSuccess) return result.Map(_ => string.Empty);
        
        return Result.Success(Encoding.UTF8.GetString(result.Value));
    }
}
