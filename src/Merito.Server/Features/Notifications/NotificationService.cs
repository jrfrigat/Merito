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
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(FamilyMember recipient, int take, CancellationToken ct = default) =>
        await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientMemberId == recipient.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(take, 1, MaxTake))
            .Select(n => new NotificationDto(n.Id, n.Kind, n.Title, n.Message, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);

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

    /// <summary>Stages a notification; the caller saves it with its domain operation.</summary>
    public AppNotification Add(FamilyMember recipient, NotificationKind kind, string title, string message)
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
        };
        db.Notifications.Add(notification);
        return notification;
    }

    /// <summary>Stages the same notification for every active parent in a family.</summary>
    public async Task AddForParentsAsync(Guid familyId, NotificationKind kind, string title, string message, CancellationToken ct = default)
    {
        var parents = await db.Members
            .Where(m => m.FamilyId == familyId && m.IsActive && m.Role == FamilyRole.Parent)
            .ToListAsync(ct);
        foreach (var parent in parents) Add(parent, kind, title, message);
    }
}
