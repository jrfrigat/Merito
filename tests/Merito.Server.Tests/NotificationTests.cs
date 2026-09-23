using Merito.Server.Data;
using Merito.Server.Infrastructure;
using Merito.Server.Tests.Support;
using Merito.Shared;

namespace Merito.Server.Tests;

public sealed class NotificationTests
{
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
        t.Notifications.Add(parent, NotificationKind.PointsCredited, "Начисление", "+5");
        t.Notifications.Add(child, NotificationKind.SubmissionApproved, "Засчитано", "Дело принято");
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
        var notification = t.Notifications.Add(child, NotificationKind.SubmissionRejected, "Отклонено", "Попробуй еще раз");
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
        t.Notifications.Add(parent, NotificationKind.PointsCredited, "Начисление", "+5");
        t.Notifications.Add(parent, NotificationKind.PointsDebited, "Списание", "-2");
        t.Notifications.Add(child, NotificationKind.SubmissionApproved, "Засчитано", "Готово");
        await t.Db.SaveChangesAsync();

        await t.Notifications.MarkAllReadAsync(parent);

        Assert.Equal(0, await t.Notifications.CountUnreadAsync(parent));
        Assert.Equal(1, await t.Notifications.CountUnreadAsync(child));
    }
}
