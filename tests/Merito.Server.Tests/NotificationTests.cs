using System.Net.Http.Json;
using Merito.Server.Data;
using Merito.Server.Infrastructure;
using Merito.Server.Tests.Support;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Tests;

public sealed class NotificationTests
{
    private static readonly WebPushSubscriptionRequest PushRequest = new(
        "https://push.example.test/subscription-1", "p256dh-key", "auth-key");

    [Fact]
    public async Task Every_parent_receives_positive_and_negative_balance_changes()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        var secondParentUser = await t.AddUserAsync("SecondParent");
        var secondParent = new FamilyMember
        {
            Id = Guid.NewGuid(), FamilyId = parent.FamilyId, UserId = secondParentUser,
            Role = FamilyRole.Parent, JoinedAt = t.Clock.GetUtcNow().UtcDateTime,
        };
        t.Db.Members.Add(secondParent);
        await t.Db.SaveChangesAsync();
        secondParent = await t.Access.RequireAsync(parent.FamilyId, secondParentUser);

        await t.Points.AdjustAsync(parent, new(child.Id, 7, "За помощь"));
        await t.Points.AdjustAsync(parent, new(child.Id, -2, "За опоздание"));

        foreach (var recipient in new[] { parent, secondParent })
        {
            var items = await t.Notifications.ListAsync(recipient, 50);
            Assert.Equal(2, items.Count);
            Assert.Contains(items, n => n.Kind == NotificationKind.PointsCredited && n.Message.Contains("+7"));
            Assert.Contains(items, n => n.Kind == NotificationKind.PointsDebited && n.Message.Contains("-2"));
        }
        Assert.Empty(await t.Notifications.ListAsync(child, 50));
    }

    [Fact]
    public async Task Child_receives_approval_and_rejection_results()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        var approved = await t.Submissions.SubmitAsync(child, new(null, "Помыл посуду", null));
        await t.Submissions.ApproveAsync(parent, approved.Id, new(5, "Отлично", null));
        var rejected = await t.Submissions.SubmitAsync(child, new(null, "Убрал комнату", null));
        await t.Submissions.RejectAsync(parent, rejected.Id, new("Нужно убрать стол"));

        var childItems = await t.Notifications.ListAsync(child, 50);

        Assert.Equal(2, childItems.Count);
        Assert.Contains(childItems, n => n.Kind == NotificationKind.SubmissionApproved
            && n.Message.Contains("Помыл посуду") && n.Message.Contains("+5"));
        Assert.Contains(childItems, n => n.Kind == NotificationKind.SubmissionRejected
            && n.Message.Contains("Убрал комнату") && n.Message.Contains("Нужно убрать стол"));
        Assert.DoesNotContain(childItems, n => n.Kind is NotificationKind.PointsCredited or NotificationKind.PointsDebited);
    }

    [Fact]
    public async Task Inbox_and_unread_count_are_isolated_by_recipient()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        await t.Notifications.AddAsync(parent, NotificationKind.PointsCredited, "Начисление", "+5");
        await t.Notifications.AddAsync(child, NotificationKind.SubmissionApproved, "Засчитано", "Дело принято");
        await t.Db.SaveChangesAsync();

        var parentItems = await t.Notifications.ListAsync(parent, 50);
        var childItems = await t.Notifications.ListAsync(child, 50);

        Assert.Single(parentItems);
        Assert.Equal(NotificationKind.PointsCredited, parentItems[0].Kind);
        Assert.Single(childItems);
        Assert.Equal(NotificationKind.SubmissionApproved, childItems[0].Kind);
        Assert.Equal(1, await t.Notifications.CountUnreadAsync(parent));
        Assert.Equal(1, await t.Notifications.CountUnreadAsync(child));
    }

    [Fact]
    public async Task Mark_read_cannot_change_another_members_notification()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        var notification = await t.Notifications.AddAsync(child, NotificationKind.SubmissionRejected, "Отклонено", "Попробуй еще раз");
        await t.Db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            t.Notifications.MarkReadAsync(parent, notification.Id));

        Assert.Equal(DomainError.NotFound, error.Error);
        Assert.Equal(1, await t.Notifications.CountUnreadAsync(child));
    }

    [Fact]
    public async Task Mark_all_only_changes_current_members_notifications()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        await t.Notifications.AddAsync(parent, NotificationKind.PointsCredited, "Начисление", "+5");
        await t.Notifications.AddAsync(parent, NotificationKind.PointsDebited, "Списание", "-2");
        await t.Notifications.AddAsync(child, NotificationKind.SubmissionApproved, "Засчитано", "Готово");
        await t.Db.SaveChangesAsync();

        await t.Notifications.MarkAllReadAsync(parent);

        Assert.Equal(0, await t.Notifications.CountUnreadAsync(parent));
        Assert.Equal(1, await t.Notifications.CountUnreadAsync(child));
    }

    [Fact]
    public async Task Purchase_notification_exposes_current_state_and_cancel_is_idempotent()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync(seedExample: true);
        await t.Points.AdjustAsync(parent, new(child.Id, 10, "Старт"));
        var reward = (await t.Catalog.GetRewardsAsync(child.FamilyId)).First(r => r.Cost == 8);

        var purchase = await t.Shop.BuyAsync(child, new(reward.Id));
        var item = (await t.Notifications.ListAsync(parent, 50)).Single(n => n.Purchase?.Id == purchase.Id);

        Assert.NotNull(item.Purchase);
        Assert.Equal(child.User.DisplayName, item.Purchase.ChildName);
        Assert.Equal(reward.Title, item.Purchase.Title);
        Assert.Equal(8, item.Purchase.Cost);
        Assert.True(item.Purchase.CanFulfill);
        Assert.True(item.Purchase.CanCancel);

        await t.Shop.CancelFromNotificationAsync(parent, item.Id);
        await t.Shop.CancelFromNotificationAsync(parent, item.Id);

        var resolved = (await t.Notifications.ListAsync(parent, 50)).Single(n => n.Id == item.Id);
        Assert.Equal(PurchaseStatus.Cancelled, resolved.Purchase!.Status);
        Assert.False(resolved.Purchase.CanFulfill);
        Assert.False(resolved.Purchase.CanCancel);
        Assert.NotNull(resolved.ReadAt);
        Assert.Equal(10, child.Balance);
        Assert.Single(await t.Db.Transactions.Where(x => x.PurchaseId == purchase.Id && x.Kind == TransactionKind.Refund).ToListAsync());

        var conflict = await Assert.ThrowsAsync<DomainException>(() =>
            t.Shop.FulfillFromNotificationAsync(parent, item.Id));
        Assert.Equal(DomainError.Conflict, conflict.Error);
    }

    [Fact]
    public async Task Purchase_action_rejects_another_recipients_or_passive_notification()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        var passive = await t.Notifications.AddAsync(parent, NotificationKind.PointsDebited, "Списание", "-2");
        var childNotification = await t.Notifications.AddAsync(child, NotificationKind.SubmissionApproved, "Засчитано", "Готово");
        await t.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => t.Shop.FulfillFromNotificationAsync(parent, passive.Id));
        await Assert.ThrowsAsync<DomainException>(() => t.Shop.CancelFromNotificationAsync(parent, childNotification.Id));
    }

    [Fact]
    public async Task Notification_queues_delivery_only_for_existing_subscriptions()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        await t.PushSubscriptions.SubscribeAsync(child, PushRequest);

        var queued = await t.Notifications.AddAsync(child, NotificationKind.SubmissionApproved, "Засчитано", "Готово");
        var beforeSubscription = await t.Notifications.AddAsync(parent, NotificationKind.PointsCredited, "Начисление", "+5");
        await t.Db.SaveChangesAsync();
        await t.PushSubscriptions.SubscribeAsync(parent, PushRequest with { Endpoint = "https://push.example.test/late" });

        var delivery = await t.Db.WebPushDeliveries.SingleAsync();
        Assert.Equal(queued.Id, delivery.NotificationId);
        Assert.DoesNotContain(await t.Db.WebPushDeliveries.ToListAsync(), d => d.NotificationId == beforeSubscription.Id);
    }

    [Fact]
    public async Task Subscription_cannot_be_taken_or_removed_by_another_member()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        await t.PushSubscriptions.SubscribeAsync(parent, PushRequest);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            t.PushSubscriptions.SubscribeAsync(child, PushRequest));
        Assert.Equal(DomainError.Conflict, error.Error);

        await t.PushSubscriptions.UnsubscribeAsync(child, PushRequest.Endpoint);
        Assert.Single(await t.Db.WebPushSubscriptions.ToListAsync());
        await t.PushSubscriptions.UnsubscribeAsync(parent, PushRequest.Endpoint);
        Assert.Empty(await t.Db.WebPushSubscriptions.ToListAsync());
    }

    [Fact]
    public async Task Push_api_requires_family_access_and_returns_only_public_key()
    {
        using var factory = new MeritoApiFactory();
        var anonymous = factory.CreateClient();
        var missing = await anonymous.GetAsync($"/api/families/{Guid.NewGuid()}/notifications/push/public-key");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, missing.StatusCode);

        var parent = await factory.RegisterParentAsync("PushParent");
        var created = await parent.PostAsJsonAsync("/api/families", new CreateFamilyRequest("Push", false));
        created.EnsureSuccessStatusCode();
        var family = await created.Content.ReadFromJsonAsync<MembershipDto>();

        var key = await parent.GetFromJsonAsync<WebPushPublicKeyDto>(
            $"/api/families/{family!.FamilyId}/notifications/push/public-key");
        Assert.Equal("test-public-key", key!.PublicKey);

        var subscribe = await parent.PostAsJsonAsync(
            $"/api/families/{family.FamilyId}/notifications/push/subscriptions", PushRequest);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, subscribe.StatusCode);
    }
}
