using System.ComponentModel.DataAnnotations;
using Merito.Server.Data;
using Merito.Server.Features.Families;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Merito.Server.Features.Auth;

/// <summary>Registration, sign-in by email or login, token refresh and the current account.</summary>
public static class AuthEndpoints
{
    /// <summary>Maps <c>/api/auth/*</c> and <c>/api/me</c>.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/register", async Task<EmptyHttpResult> (
            RegisterRequest request, UserManager<AppUser> users, SignInManager<AppUser> signIn, TimeProvider clock) =>
        {
            var name = Guard.Required(request.DisplayName, Limits.NameMaxLength, "Имя");
            var email = Guard.Required(request.Email, 256, "Почта");
            if (!new EmailAddressAttribute().IsValid(email)) throw DomainException.Invalid("Проверьте адрес почты.");

            var user = new AppUser
            {
                Id = Guid.NewGuid(), UserName = email, Email = email, DisplayName = name,
                CreatedAt = clock.GetUtcNow().UtcDateTime,
            };
            var created = await users.CreateAsync(user, request.Password ?? "");
            if (!created.Succeeded) throw IdentityErrors.ToException(created);

            // The bearer handler writes the token response body during SignIn.
            signIn.AuthenticationScheme = IdentityConstants.BearerScheme;
            await signIn.SignInAsync(user, isPersistent: false);
            return TypedResults.Empty;
        });

        auth.MapPost("/login", async Task<Results<EmptyHttpResult, ProblemHttpResult>> (
            LoginRequest request, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
        {
            var login = (request.Login ?? "").Trim();
            var user = login.Length == 0 ? null : await users.FindByNameAsync(login);
            if (user is null)
                return TypedResults.Problem("Неверный логин или пароль.", statusCode: StatusCodes.Status401Unauthorized);

            var check = await signIn.CheckPasswordSignInAsync(user, request.Password ?? "", lockoutOnFailure: true);
            if (check.IsLockedOut)
                return TypedResults.Problem("Слишком много попыток. Попробуйте через несколько минут.", statusCode: StatusCodes.Status429TooManyRequests);
            if (!check.Succeeded)
                return TypedResults.Problem("Неверный логин или пароль.", statusCode: StatusCodes.Status401Unauthorized);

            signIn.AuthenticationScheme = IdentityConstants.BearerScheme;
            await signIn.SignInAsync(user, isPersistent: false);
            return TypedResults.Empty;
        });

        auth.MapPost("/refresh", async Task<Results<SignInHttpResult, ChallengeHttpResult>> (
            RefreshRequest request, SignInManager<AppUser> signIn, IOptionsMonitor<BearerTokenOptions> bearerOptions, TimeProvider clock) =>
        {
            var protector = bearerOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var ticket = protector.Unprotect(request.RefreshToken);

            if (ticket?.Properties.ExpiresUtc is not { } expiresUtc
                || clock.GetUtcNow() >= expiresUtc
                || await signIn.ValidateSecurityStampAsync(ticket.Principal) is not { } user)
            {
                return TypedResults.Challenge();
            }

            var principal = await signIn.CreateUserPrincipalAsync(user);
            return TypedResults.SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
        });

        app.MapGet("/api/me", async (HttpContext http, UserManager<AppUser> users, FamilyService families, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(http.User) ?? throw DomainException.Forbidden("Требуется вход в приложение.");
            var memberships = await families.GetMembershipsAsync(user.Id, ct);
            return new MeResponse(user.Id, user.DisplayName, user.UserName ?? "", memberships);
        }).RequireAuthorization();

        return app;
    }
}
