using Merito.Server.Features.Families;
using Merito.Server.Infrastructure;
using Merito.Shared.Contracts;

namespace Merito.Server.Features.Notifications;

/// <summary>Maps the current family member's notification inbox.</summary>
public static class NotificationEndpoints
{
    /// <summary>Maps notification list, count and read-state endpoints.</summary>
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var family = app.MapGroup("/api/families/{familyId:guid}/notifications").RequireAuthorization();

        family.MapGet("", async (Guid familyId, int? take, HttpContext http, FamilyAccess access, NotificationService service, CancellationToken ct) =>
            await service.ListAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), take ?? 50, ct));

        family.MapGet("/unread-count", async (Guid familyId, HttpContext http, FamilyAccess access, NotificationService service, CancellationToken ct) =>
            new UnreadCountDto(await service.CountUnreadAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), ct)));

        family.MapPost("/{id:guid}/read", async (Guid familyId, Guid id, HttpContext http, FamilyAccess access, NotificationService service, CancellationToken ct) =>
        {
            await service.MarkReadAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), id, ct);
            return Results.NoContent();
        });

        family.MapPost("/read-all", async (Guid familyId, HttpContext http, FamilyAccess access, NotificationService service, CancellationToken ct) =>
        {
            await service.MarkAllReadAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), ct);
            return Results.NoContent();
        });

        return app;
    }
}
