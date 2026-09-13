namespace Merito.Shared.Contracts;

/// <summary>Creates a parent account identified by email.</summary>
/// <param name="DisplayName">Name shown to the family.</param>
/// <param name="Email">Email used as the login.</param>
/// <param name="Password">Account password.</param>
public sealed record RegisterRequest(string DisplayName, string Email, string Password);

/// <summary>Signs in with an email (parents) or a login (child accounts).</summary>
/// <param name="Login">Email or child login.</param>
/// <param name="Password">Account password.</param>
public sealed record LoginRequest(string Login, string Password);

/// <summary>Exchanges a refresh token for a new pair of tokens.</summary>
/// <param name="RefreshToken">The refresh token from the last sign-in or refresh.</param>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Bearer tokens issued on sign-in and refresh.</summary>
/// <param name="TokenType">Always "Bearer".</param>
/// <param name="AccessToken">Token sent in the Authorization header.</param>
/// <param name="ExpiresIn">Seconds until the access token expires.</param>
/// <param name="RefreshToken">Token that obtains a new access token without the password.</param>
public sealed record TokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);

/// <summary>The signed-in person and the families they belong to.</summary>
/// <param name="UserId">Account id.</param>
/// <param name="DisplayName">Name shown to the family.</param>
/// <param name="Login">Email or child login.</param>
/// <param name="Families">Active memberships, oldest first.</param>
public sealed record MeResponse(Guid UserId, string DisplayName, string Login, IReadOnlyList<MembershipDto> Families);

/// <summary>One family a person belongs to, with their role in it.</summary>
/// <param name="FamilyId">Family id.</param>
/// <param name="FamilyName">Family name.</param>
/// <param name="MemberId">The person's member id inside this family.</param>
/// <param name="Role">Their role in this family.</param>
public sealed record MembershipDto(Guid FamilyId, string FamilyName, Guid MemberId, FamilyRole Role);
