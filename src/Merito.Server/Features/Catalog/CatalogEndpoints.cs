using Merito.Server.Features.Families;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;

namespace Merito.Server.Features.Catalog;

/// <summary>Tasks, penalties and rewards: every member reads them, only parents change them.</summary>
public static class CatalogEndpoints
{
    /// <summary>Maps <c>/api/families/{familyId}/tasks|penalties|rewards</c>.</summary>
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var family = app.MapGroup("/api/families/{familyId:guid}").RequireAuthorization();

        family.MapGet("/tasks", async (Guid familyId, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct);
            return await catalog.GetTasksAsync(familyId, ct);
        });
        family.MapPost("/tasks", async (Guid familyId, TaskRequest request, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            return await catalog.AddTaskAsync(familyId, request, ct);
        });
        family.MapPut("/tasks/{id:guid}", async (Guid familyId, Guid id, TaskRequest request, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            return await catalog.UpdateTaskAsync(familyId, id, request, ct);
        });
        family.MapDelete("/tasks/{id:guid}", async (Guid familyId, Guid id, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            await catalog.ArchiveTaskAsync(familyId, id, ct);
            return Results.NoContent();
        });

        family.MapGet("/penalties", async (Guid familyId, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct);
            return await catalog.GetPenaltiesAsync(familyId, ct);
        });
        family.MapPost("/penalties", async (Guid familyId, PenaltyRequest request, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            return await catalog.AddPenaltyAsync(familyId, request, ct);
        });
        family.MapPut("/penalties/{id:guid}", async (Guid familyId, Guid id, PenaltyRequest request, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            return await catalog.UpdatePenaltyAsync(familyId, id, request, ct);
        });
        family.MapDelete("/penalties/{id:guid}", async (Guid familyId, Guid id, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            await catalog.ArchivePenaltyAsync(familyId, id, ct);
            return Results.NoContent();
        });

        family.MapGet("/rewards", async (Guid familyId, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct);
            return await catalog.GetRewardsAsync(familyId, ct);
        });
        family.MapPost("/rewards", async (Guid familyId, RewardRequest request, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            return await catalog.AddRewardAsync(familyId, request, ct);
        });
        family.MapPut("/rewards/{id:guid}", async (Guid familyId, Guid id, RewardRequest request, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            return await catalog.UpdateRewardAsync(familyId, id, request, ct);
        });
        family.MapDelete("/rewards/{id:guid}", async (Guid familyId, Guid id, HttpContext http, FamilyAccess access, CatalogService catalog, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct);
            await catalog.ArchiveRewardAsync(familyId, id, ct);
            return Results.NoContent();
        });

        return app;
    }
}
