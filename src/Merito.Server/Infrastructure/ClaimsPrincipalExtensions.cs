using System.Security.Claims;

namespace Merito.Server.Infrastructure;

/// <summary>Reads the account id Identity puts into the bearer token.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in account id; throws when the principal carries none.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw DomainException.Forbidden("Требуется вход в приложение.");
}
