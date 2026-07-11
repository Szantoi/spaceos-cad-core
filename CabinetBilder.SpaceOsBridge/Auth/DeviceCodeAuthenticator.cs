using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;
using CabinetBilder.Core.Sync;
using CabinetBilder.SpaceOsBridge.TokenStorage;

namespace CabinetBilder.SpaceOsBridge.Auth;

public class DeviceCodeAuthenticator : ISpaceOsAuthenticator
{
    private readonly HttpClient _httpClient;
    private readonly string _authority;
    private readonly string _clientId;
    private readonly string _scope;
    private readonly DpapiTokenStore _tokenStore;
    private readonly TenantManifestManager _manifestManager;
    private readonly ILogger<DeviceCodeAuthenticator> _logger;
    private IConnectionState _currentState;

    public IConnectionState CurrentState => _currentState;

    public DeviceCodeAuthenticator(
        HttpClient httpClient,
        string authority,
        string clientId,
        string scope,
        DpapiTokenStore tokenStore,
        TenantManifestManager manifestManager,
        ILogger<DeviceCodeAuthenticator> logger)
    {
        _httpClient = httpClient;
        _authority = authority;
        _clientId = clientId;
        _scope = scope;
        _tokenStore = tokenStore;
        _manifestManager = manifestManager;
        _logger = logger;
        _currentState = new DefaultConnectionState(ConnectionStatus.Unauthenticated, null, null, null);
    }

    private async Task<DiscoveryDocumentResponse> GetDiscoveryDocumentAsync(CancellationToken ct)
    {
        var disco = await _httpClient.GetDiscoveryDocumentAsync(_authority, ct);
        if (disco.IsError)
        {
            _logger.LogError("Discovery error: {Error}", disco.Error);
            throw new Exception($"Could not discover endpoints at {_authority}");
        }
        return disco;
    }

    public async Task<Ardalis.Result.Result<DeviceCodeResponse>> StartLoginAsync(CancellationToken ct = default)
    {
        try
        {
            var disco = await GetDiscoveryDocumentAsync(ct);
            
            var response = await _httpClient.RequestDeviceAuthorizationAsync(new DeviceAuthorizationRequest
            {
                Address = disco.DeviceAuthorizationEndpoint,
                ClientId = _clientId,
                Scope = _scope
            }, ct);

            if (response.IsError)
            {
                _logger.LogError("Device authorization error: {Error}", response.Error);
                return Ardalis.Result.Result.Error(response.Error);
            }

            return Ardalis.Result.Result.Success(new DeviceCodeResponse(
                response.UserCode!,
                response.VerificationUri!,
                response.DeviceCode!,
                response.Interval,
                response.ExpiresIn ?? 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start device code login");
            return Ardalis.Result.Result.Error("Internal error during login initiation");
        }
    }

    public async Task<Ardalis.Result.Result<AuthToken>> CompleteLoginAsync(DeviceCodeResponse response, CancellationToken ct = default)
    {
        try
        {
            var disco = await GetDiscoveryDocumentAsync(ct);
            var interval = response.Interval > 0 ? response.Interval : 5;

            while (!ct.IsCancellationRequested)
            {
                var tokenResponse = await _httpClient.RequestDeviceTokenAsync(new DeviceTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = _clientId,
                    DeviceCode = response.DeviceCode
                }, ct);

                if (tokenResponse.IsError)
                {
                    if (tokenResponse.Error == "authorization_pending" || tokenResponse.Error == "slow_down")
                    {
                        await Task.Delay(interval * 1000, ct);
                        continue;
                    }

                    _logger.LogError("Token request error: {Error}", tokenResponse.Error);
                    return Ardalis.Result.Result.Error(tokenResponse.Error);
                }

                // Success!
                var tenantId = "default"; // Simplified for now
                var displayName = "User";

                var token = new AuthToken(
                    tokenResponse.AccessToken!,
                    tokenResponse.RefreshToken!,
                    DateTimeOffset.Now.AddSeconds(tokenResponse.ExpiresIn),
                    tenantId);

                await _tokenStore.WriteTokenAsync(tenantId, token);
                await _manifestManager.AddOrUpdateTenantAsync(new TenantEntry(
                    tenantId,
                    displayName,
                    _authority,
                    DateTimeOffset.Now));

                _currentState = new DefaultConnectionState(ConnectionStatus.Online, tenantId, displayName, DateTimeOffset.Now);
                
                return Ardalis.Result.Result.Success(token);
            }

            return Ardalis.Result.Result.Error("Login cancelled or timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete device code login");
            return Ardalis.Result.Result.Error("Internal error during login completion");
        }
    }

    public async Task LogoutAsync(string? tenantId = null, CancellationToken ct = default)
    {
        var manifest = await _manifestManager.GetManifestAsync();
        var idToDelete = tenantId ?? manifest.ActiveTenantId;
        
        if (idToDelete != null)
        {
            await _tokenStore.DeleteTokenAsync(idToDelete);
            await _manifestManager.RemoveTenantAsync(idToDelete);
        }

        if (idToDelete == manifest.ActiveTenantId)
        {
            _currentState = new DefaultConnectionState(ConnectionStatus.Unauthenticated, null, null, null);
        }
    }

    public async Task<Ardalis.Result.Result<string>> GetValidTokenAsync(CancellationToken ct = default)
    {
        var manifest = await _manifestManager.GetManifestAsync();
        if (manifest.ActiveTenantId == null) return Ardalis.Result.Result.Unauthorized();

        var token = await _tokenStore.ReadTokenAsync(manifest.ActiveTenantId);
        if (token == null) return Ardalis.Result.Result.Unauthorized();

        if (token.ExpiresAt > DateTimeOffset.Now.AddMinutes(1))
        {
            return Ardalis.Result.Result.Success(token.AccessToken);
        }

        try
        {
            var disco = await GetDiscoveryDocumentAsync(ct);
            var result = await _httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = _clientId,
                RefreshToken = token.RefreshToken
            }, ct);

            if (result.IsError)
            {
                _logger.LogWarning("Token refresh failed for tenant {TenantId}: {Error}", manifest.ActiveTenantId, result.Error);
                return Ardalis.Result.Result.Unauthorized();
            }

            var newToken = new AuthToken(
                result.AccessToken!,
                result.RefreshToken!,
                DateTimeOffset.Now.AddSeconds(result.ExpiresIn),
                manifest.ActiveTenantId);

            await _tokenStore.WriteTokenAsync(manifest.ActiveTenantId, newToken);
            return Ardalis.Result.Result.Success(newToken.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh for tenant {TenantId}", manifest.ActiveTenantId);
            return Ardalis.Result.Result.Error("Refresh failed");
        }
    }
}

internal record DefaultConnectionState(
    ConnectionStatus Status,
    string? ActiveTenantId,
    string? UserDisplayName,
    DateTimeOffset? LastSyncTime) : IConnectionState;
