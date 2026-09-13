using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.JSInterop;

namespace Merito.Client.Services;

/// <summary>Who is signed in and which of their families the app is showing.</summary>
public sealed class Session(ApiClient api, TokenStore tokens, AuthHandler auth, IJSRuntime js)
{
    private const string FamilyKey = "merito.family";
    private Task? _loading;

    /// <summary>The signed-in account; null when signed out.</summary>
    public MeResponse? Me { get; private set; }

    /// <summary>The family on screen; null when the account has none yet.</summary>
    public MembershipDto? Family { get; private set; }

    /// <summary>True once an account is signed in.</summary>
    public bool IsSignedIn => Me is not null;

    /// <summary>True when the caller is a parent in the family on screen.</summary>
    public bool IsParent => Family?.Role == FamilyRole.Parent;

    /// <summary>Id of the family on screen; throws when there is none.</summary>
    public Guid FamilyId => Family?.FamilyId ?? throw new InvalidOperationException("No family is selected.");

    /// <summary>Raised when the account, the family or the role on screen changes.</summary>
    public event Action? Changed;

    /// <summary>Loads the account once per app start; later calls wait for the same load.</summary>
    public Task EnsureLoadedAsync()
    {
        if (_loading is null)
        {
            auth.SessionExpired += OnSessionExpired;
            _loading = ReloadAsync();
        }
        return _loading;
    }

    /// <summary>Re-reads the account and its families from the server.</summary>
    public async Task ReloadAsync()
    {
        if (await tokens.GetAsync() is null)
        {
            SetSignedOut();
            return;
        }

        try
        {
            Me = await api.GetMeAsync();
        }
        catch (ApiException e) when (e.Status is System.Net.HttpStatusCode.Unauthorized)
        {
            await tokens.ClearAsync();
            SetSignedOut();
            return;
        }

        var saved = await js.InvokeAsync<string?>("localStorage.getItem", FamilyKey);
        Family = Me.Families.FirstOrDefault(f => f.FamilyId.ToString() == saved) ?? Me.Families.FirstOrDefault();
        Changed?.Invoke();
    }

    /// <summary>Stores the tokens of a fresh sign-in and loads the account.</summary>
    public async Task SignInAsync(TokenResponse response)
    {
        await tokens.SetAsync(response);
        _loading ??= Task.CompletedTask;
        await ReloadAsync();
    }

    /// <summary>Switches the app to another family of the account and remembers the choice.</summary>
    public async Task SelectFamilyAsync(Guid familyId)
    {
        await js.InvokeVoidAsync("localStorage.setItem", FamilyKey, familyId.ToString());
        await ReloadAsync();
    }

    /// <summary>Forgets the tokens and the account.</summary>
    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();
        SetSignedOut();
    }

    private void OnSessionExpired() => SetSignedOut();

    private void SetSignedOut()
    {
        Me = null;
        Family = null;
        Changed?.Invoke();
    }
}
