using System.Text.Json;
using Merito.Shared.Contracts;
using Microsoft.JSInterop;

namespace Merito.Client.Services;

/// <summary>Keeps the bearer tokens in localStorage so a family member stays signed in on their device.</summary>
public sealed class TokenStore(IJSRuntime js)
{
    private const string Key = "merito.tokens";
    private StoredTokens? _tokens;
    private bool _loaded;

    /// <summary>The stored tokens, or null when signed out.</summary>
    public async ValueTask<StoredTokens?> GetAsync()
    {
        if (_loaded) return _tokens;
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            _tokens = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize(json, MeritoJsonContext.Default.StoredTokens);
        }
        catch (JsonException)
        {
            _tokens = null;
        }
        _loaded = true;
        return _tokens;
    }

    /// <summary>Stores fresh tokens from a sign-in or refresh.</summary>
    public async ValueTask SetAsync(TokenResponse response)
    {
        // A minute of slack so a request never leaves with a token that expires on the way.
        _tokens = new StoredTokens(response.AccessToken, response.RefreshToken,
            DateTime.UtcNow.AddSeconds(Math.Max(0, response.ExpiresIn - 60)));
        _loaded = true;
        await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(_tokens, MeritoJsonContext.Default.StoredTokens));
    }

    /// <summary>Forgets the tokens.</summary>
    public async ValueTask ClearAsync()
    {
        _tokens = null;
        _loaded = true;
        await js.InvokeVoidAsync("localStorage.removeItem", Key);
    }
}
