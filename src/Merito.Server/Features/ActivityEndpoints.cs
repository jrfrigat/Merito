using Merito.Server.Features.Families;
using Merito.Server.Features.Points;
using Merito.Server.Features.Shop;
using Merito.Server.Features.Submissions;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;

namespace Merito.Server.Features;

/// <summary>Submissions, the ledger and the shop - everything that moves points.</summary>
public static class ActivityEndpoints
{
    /// <summary>Maps <c>/api/families/{familyId}/submissions|transactions|points|purchases</c>.</summary>
    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var family = app.MapGroup("/api/families/{familyId:guid}").RequireAuthorization();

        family.MapGet("/submissions", async (Guid familyId, SubmissionStatus? status, Guid? childId, int? take,
            HttpContext http, FamilyAccess access, SubmissionService service, CancellationToken ct) =>
            await service.ListAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), status, childId, take ?? 50, ct));

        family.MapPost("/submissions", async (Guid familyId, SubmitRequest request, HttpContext http, FamilyAccess access, SubmissionService service, CancellationToken ct) =>
            await service.SubmitAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Child, ct), request, ct));

        family.MapDelete("/submissions/{id:guid}", async (Guid familyId, Guid id, HttpContext http, FamilyAccess access, SubmissionService service, CancellationToken ct) =>
        {
            await service.WithdrawAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Child, ct), id, ct);
            return Results.NoContent();
        });

        family.MapPost("/submissions/{id:guid}/approve", async (Guid familyId, Guid id, ApproveRequest request, HttpContext http, FamilyAccess access, SubmissionService service, CancellationToken ct) =>
        {
            await service.ApproveAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), id, request, ct);
            return Results.NoContent();
        });

        family.MapPost("/submissions/{id:guid}/reject", async (Guid familyId, Guid id, RejectRequest request, HttpContext http, FamilyAccess access, SubmissionService service, CancellationToken ct) =>
        {
            await service.RejectAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), id, request, ct);
            return Results.NoContent();
        });

        family.MapGet("/transactions", async (Guid familyId, Guid? childId, int? take, HttpContext http, FamilyAccess access, PointsService service, CancellationToken ct) =>
            await service.ListAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), childId, take ?? 50, ct));

        family.MapPost("/points/adjust", async (Guid familyId, AdjustPointsRequest request, HttpContext http, FamilyAccess access, PointsService service, CancellationToken ct) =>
            new BalanceDto(await service.AdjustAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), request, ct)));

        family.MapPost("/points/penalty", async (Guid familyId, ApplyPenaltyRequest request, HttpContext http, FamilyAccess access, PointsService service, CancellationToken ct) =>
            new BalanceDto(await service.ApplyPenaltyAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), request, ct)));

        family.MapGet("/purchases", async (Guid familyId, PurchaseStatus? status, Guid? childId, int? take, HttpContext http, FamilyAccess access, ShopService service, CancellationToken ct) =>
            await service.ListAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), status, childId, take ?? 50, ct));

        family.MapPost("/purchases", async (Guid familyId, PurchaseRequest request, HttpContext http, FamilyAccess access, ShopService service, CancellationToken ct) =>
            await service.BuyAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Child, ct), request, ct));

        family.MapPost("/purchases/{id:guid}/fulfill", async (Guid familyId, Guid id, HttpContext http, FamilyAccess access, ShopService service, CancellationToken ct) =>
        {
            await service.FulfillAsync(await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), id, ct);
            return Results.NoContent();
        });

        family.MapPost("/purchases/{id:guid}/cancel", async (Guid familyId, Guid id, HttpContext http, FamilyAccess access, ShopService service, CancellationToken ct) =>
        {
            await service.CancelAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), id, ct);
            return Results.NoContent();
        });

        return app;
    }
}
