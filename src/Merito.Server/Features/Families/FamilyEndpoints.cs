using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;

namespace Merito.Server.Features.Families;

/// <summary>Family creation, joining, members and the home dashboard.</summary>
public static class FamilyEndpoints
{
    /// <summary>Maps <c>/api/families</c> and its member routes.</summary>
    public static IEndpointRouteBuilder MapFamilyEndpoints(this IEndpointRouteBuilder app)
    {
        var families = app.MapGroup("/api/families").RequireAuthorization();

        families.MapPost("/", (CreateFamilyRequest request, HttpContext http, FamilyService service, CancellationToken ct) =>
            service.CreateAsync(http.User.GetUserId(), request, ct));

        families.MapPost("/join", (JoinFamilyRequest request, HttpContext http, FamilyService service, CancellationToken ct) =>
            service.JoinAsync(http.User.GetUserId(), request, ct));

        families.MapGet("/{familyId:guid}", async (Guid familyId, HttpContext http, FamilyAccess access, FamilyService service, CancellationToken ct) =>
            await service.GetAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), ct));

        families.MapPut("/{familyId:guid}", async (Guid familyId, RenameFamilyRequest request, HttpContext http, FamilyAccess access, FamilyService service, CancellationToken ct) =>
        {
            await service.RenameAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), request, ct);
            return Results.NoContent();
        });

        families.MapGet("/{familyId:guid}/dashboard", async (Guid familyId, HttpContext http, FamilyAccess access, DashboardService service, CancellationToken ct) =>
            await service.GetAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), ct));

        families.MapPost("/{familyId:guid}/invites", async (Guid familyId, CreateInviteRequest request, HttpContext http, FamilyAccess access, FamilyService service, CancellationToken ct) =>
            await service.CreateInviteAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), request, ct));

        families.MapPost("/{familyId:guid}/children", async (Guid familyId, CreateChildRequest request, HttpContext http, FamilyAccess access, ChildAccountService service, CancellationToken ct) =>
            await service.CreateAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), request, ct));

        families.MapDelete("/{familyId:guid}/members/{memberId:guid}", async (Guid familyId, Guid memberId, HttpContext http, FamilyAccess access, FamilyService service, CancellationToken ct) =>
        {
            await service.RemoveMemberAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), memberId, ct);
            return Results.NoContent();
        });

        families.MapPut("/{familyId:guid}/members/{memberId:guid}/password", async (Guid familyId, Guid memberId, ResetPasswordRequest request, HttpContext http, FamilyAccess access, ChildAccountService service, CancellationToken ct) =>
        {
            await service.ResetPasswordAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), memberId, request, ct);
            return Results.NoContent();
        });

        return app;
    }
}
