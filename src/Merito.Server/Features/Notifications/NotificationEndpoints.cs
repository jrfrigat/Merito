using Merito.Server.Features.Families;
using Merito.Server.Infrastructure;
using Merito.Server.Features.Shop;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.Extensions.Options;

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

        family.MapPost("/{id:guid}/purchase/fulfill", async (Guid familyId, Guid id, HttpContext http,
            FamilyAccess access, ShopService service, CancellationToken ct) =>
        {
            await service.FulfillFromNotificationAsync(
                await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), id, ct);
            return Results.NoContent();
        });

        family.MapPost("/{id:guid}/purchase/cancel", async (Guid familyId, Guid id, HttpContext http,
            FamilyAccess access, ShopService service, CancellationToken ct) =>
        {
            await service.CancelFromNotificationAsync(
                await access.RequireAsync(familyId, http.User.GetUserId(), FamilyRole.Parent, ct), id, ct);
            return Results.NoContent();
        });

        family.MapPost("/read-all", async (Guid familyId, HttpContext http, FamilyAccess access, NotificationService service, CancellationToken ct) =>
        {
            await service.MarkAllReadAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), ct);
            return Results.NoContent();
        });

        family.MapGet("/push/public-key", async (Guid familyId, HttpContext http, FamilyAccess access,
            IOptions<WebPushOptions> options, CancellationToken ct) =>
        {
            await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct);
            return new WebPushPublicKeyDto(
                WebPushDeliveryWorker.IsConfigured(options.Value) ? options.Value.PublicKey : "");
        });

        family.MapPost("/push/subscriptions", async (Guid familyId, WebPushSubscriptionRequest request,
            HttpContext http, FamilyAccess access, WebPushSubscriptionService service, CancellationToken ct) =>
        {
            await service.SubscribeAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), request, ct);
            return Results.NoContent();
        });

        family.MapPost("/push/unsubscribe", async (Guid familyId, WebPushUnsubscribeRequest request,
            HttpContext http, FamilyAccess access, WebPushSubscriptionService service, CancellationToken ct) =>
        {
            await service.UnsubscribeAsync(await access.RequireAsync(familyId, http.User.GetUserId(), ct: ct), request.Endpoint, ct);
            return Results.NoContent();
        });

        return app;
    }
}
