using Merito.Server.Data;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Notifications;

/// <summary>Creates and reads durable notifications addressed to family members.</summary>
public sealed class NotificationService(MeritoDbContext db, TimeProvider clock)
{
    /// <summary>The largest notification page returned by the API.</summary>
    public const int MaxTake = 200;

    /// <summary>Returns the recipient's notifications, newest first.</summary>
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(FamilyMember recipient, int take, CancellationToken ct = default)
    {
        var canResolvePurchases = recipient.Role == FamilyRole.Parent;
        return await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientMemberId == recipient.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(take, 1, MaxTake))
            .Select(n => new NotificationDto(
                n.Id, n.Kind, n.Title, n.Message, n.CreatedAt, n.ReadAt,
                n.Purchase == null ? null : new NotificationPurchaseDto(
                    n.Purchase.Id,
                    n.Purchase.ChildMember.User.DisplayName,
                    n.Purchase.Title,
                    n.Purchase.Cost,
                    n.Purchase.Status,
                    n.Purchase.ResolvedAt,
                    n.Purchase.ResolvedBy == null ? null : n.Purchase.ResolvedBy.User.DisplayName,
                    canResolvePurchases && n.Purchase.Status == PurchaseStatus.Pending,
                    canResolvePurchases && n.Purchase.Status == PurchaseStatus.Pending)))
            .ToListAsync(ct);
    }

    /// <summary>Returns the recipient's unread notification count.</summary>
    public Task<int> CountUnreadAsync(FamilyMember recipient, CancellationToken ct = default) =>
        db.Notifications.CountAsync(n => n.RecipientMemberId == recipient.Id && n.ReadAt == null, ct);

    /// <summary>Marks one notification belonging to the recipient as read.</summary>
    public async Task MarkReadAsync(FamilyMember recipient, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(
            n => n.Id == notificationId && n.RecipientMemberId == recipient.Id, ct)
            ?? throw DomainException.NotFound("Уведомление не найдено.");

        if (notification.ReadAt is null)
        {
            notification.ReadAt = clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Marks all notifications belonging to the recipient as read.</summary>
    public async Task MarkAllReadAsync(FamilyMember recipient, CancellationToken ct = default)
    {
        var unread = await db.Notifications
            .Where(n => n.RecipientMemberId == recipient.Id && n.ReadAt == null)
            .ToListAsync(ct);
        if (unread.Count == 0) return;

        var readAt = clock.GetUtcNow().UtcDateTime;
        foreach (var notification in unread) notification.ReadAt = readAt;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Stages a notification and deliveries; the caller saves them with its domain operation.</summary>
    public async Task<AppNotification> AddAsync(
        FamilyMember recipient, NotificationKind kind, string title, string message, CancellationToken ct = default)
    {
        var notification = Create(recipient, kind, title, message);
        var subscriptionIds = await db.WebPushSubscriptions
            .Where(s => s.MemberId == recipient.Id)
            .Select(s => s.Id)
            .ToListAsync(ct);
        AddDeliveries(notification, subscriptionIds);
        return notification;
    }

    /// <summary>Stages the same notification for every active parent in a family.</summary>
    public async Task AddForParentsAsync(Guid familyId, NotificationKind kind, string title, string message,
        Guid? purchaseId = null, CancellationToken ct = default)
    {
        var parents = await db.Members
            .Where(m => m.FamilyId == familyId && m.IsActive && m.Role == FamilyRole.Parent)
            .ToListAsync(ct);
        var parentIds = parents.Select(p => p.Id).ToArray();
        var subscriptions = await db.WebPushSubscriptions
            .Where(s => parentIds.Contains(s.MemberId))
            .Select(s => new { s.Id, s.MemberId })
            .ToListAsync(ct);

        foreach (var parent in parents)
        {
            var notification = Create(parent, kind, title, message, purchaseId);
            AddDeliveries(notification, subscriptions.Where(s => s.MemberId == parent.Id).Select(s => s.Id));
        }
    }

    private AppNotification Create(FamilyMember recipient, NotificationKind kind, string title, string message,
        Guid? purchaseId = null)
    {
        var notification = new AppNotification
        {
            Id = Guid.NewGuid(),
            FamilyId = recipient.FamilyId,
            RecipientMemberId = recipient.Id,
            Kind = kind,
            Title = title,
            Message = message,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            PurchaseId = purchaseId,
        };
        db.Notifications.Add(notification);
        return notification;
    }

    private void AddDeliveries(AppNotification notification, IEnumerable<Guid> subscriptionIds)
    {
        foreach (var subscriptionId in subscriptionIds)
        {
            db.WebPushDeliveries.Add(new WebPushDelivery
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.Id,
                SubscriptionId = subscriptionId,
                NextAttemptAt = notification.CreatedAt,
            });
        }
    }
}
