using Microsoft.AspNetCore.Identity;

namespace Merito.Server.Data;

/// <summary>An account. Parents sign in with their email as the user name; child accounts have a login and no email.</summary>
public sealed class AppUser : IdentityUser<Guid>
{
    /// <summary>Name shown to the family.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>UTC moment the account was created.</summary>
    public DateTime CreatedAt { get; set; }
}
