using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Merito.Shared.Contracts;

namespace Merito.Client.Services;

/// <summary>
/// Attaches the bearer token, refreshing it first when it is about to expire. Refreshing before the
/// request, not after a 401, means a request body never has to be sent twice.
/// </summary>
public sealed class AuthHandler(TokenStore store, Uri baseAddress) : DelegatingHandler
{
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    /// <summary>Raised when the server no longer accepts the session and the user has to sign in again.</summary>
    public event Action? SessionExpired;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokens = await store.GetAsync();
        if (tokens is not null && tokens.ExpiresAt <= DateTime.UtcNow)
            tokens = await RefreshAsync(tokens, cancellationToken);
        if (tokens is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized && tokens is not null)
        {
            await store.ClearAsync();
            SessionExpired?.Invoke();
        }
        return response;
    }

    private async Task<StoredTokens?> RefreshAsync(StoredTokens current, CancellationToken ct)
    {
        await _refreshGate.WaitAsync(ct);
        try
        {
            var latest = await store.GetAsync();
            if (latest is null || latest.ExpiresAt > DateTime.UtcNow) return latest;

            using var client = new HttpClient { BaseAddress = baseAddress };
            using var response = await client.PostAsJsonAsync("api/auth/refresh",
                new RefreshRequest(current.RefreshToken), MeritoJsonContext.Default.RefreshRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                await store.ClearAsync();
                SessionExpired?.Invoke();
                return null;
            }

            var fresh = await response.Content.ReadFromJsonAsync(MeritoJsonContext.Default.TokenResponse, ct);
            if (fresh is null) return null;
            await store.SetAsync(fresh);
            return await store.GetAsync();
        }
        finally
        {
            _refreshGate.Release();
        }
    }
}
