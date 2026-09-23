using Merito.Server.Data;
using Merito.Server.Infrastructure;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Notifications;

/// <summary>Registers browser push subscriptions for the current family member.</summary>
public sealed class WebPushSubscriptionService(MeritoDbContext db, TimeProvider clock)
{
    private const int EndpointMaxLength = 2048;
    private const int KeyMaxLength = 512;

    /// <summary>Creates or refreshes a subscription owned by the member.</summary>
    public async Task SubscribeAsync(FamilyMember member, WebPushSubscriptionRequest request, CancellationToken ct = default)
    {
        Validate(request.Endpoint, request.P256dh, request.Auth);

        var existing = await db.WebPushSubscriptions.SingleOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);
        if (existing is not null && existing.MemberId != member.Id)
            throw DomainException.Conflict("Эта push-подписка уже принадлежит другому участнику.");

        var now = clock.GetUtcNow().UtcDateTime;
        if (existing is null)
        {
            db.WebPushSubscriptions.Add(new WebPushSubscription
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Removes a subscription only when it belongs to the member.</summary>
    public async Task UnsubscribeAsync(FamilyMember member, string endpoint, CancellationToken ct = default)
    {
        ValidateEndpoint(endpoint);
        var existing = await db.WebPushSubscriptions.SingleOrDefaultAsync(
            s => s.Endpoint == endpoint && s.MemberId == member.Id, ct);
        if (existing is null) return;

        db.WebPushSubscriptions.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    private static void Validate(string endpoint, string p256dh, string auth)
    {
        ValidateEndpoint(endpoint);
        if (string.IsNullOrWhiteSpace(p256dh) || p256dh.Length > KeyMaxLength)
            throw DomainException.Invalid("Некорректный ключ p256dh push-подписки.");
        if (string.IsNullOrWhiteSpace(auth) || auth.Length > KeyMaxLength)
            throw DomainException.Invalid("Некорректный ключ auth push-подписки.");
    }

    private static void ValidateEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Length > EndpointMaxLength
            || !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
            throw DomainException.Invalid("Некорректный endpoint push-подписки.");
    }
}
