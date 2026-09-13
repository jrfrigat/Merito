using Merito.Server.Infrastructure;
using Merito.Server.Tests.Support;
using Merito.Shared;
using Merito.Shared.Contracts;

namespace Merito.Server.Tests;

public sealed class FamilyTests
{
    [Fact]
    public async Task TheExampleSeedMirrorsTheUsersCatalog()
    {
        await using var t = await TestDb.CreateAsync();
        var (_, child) = await t.AddFamilyAsync(seedExample: true);

        var tasks = await t.Catalog.GetTasksAsync(child.FamilyId);
        Assert.Equal(15, tasks.Count(x => x.Category == TaskCategory.Daily));
        Assert.Equal(6, tasks.Count(x => x.Category == TaskCategory.Extra));
        Assert.Contains(tasks, x => x is { Points: 3, MaxPoints: 5 });
        Assert.Equal(7, (await t.Catalog.GetPenaltiesAsync(child.FamilyId)).Count);
        Assert.Equal(7, (await t.Catalog.GetRewardsAsync(child.FamilyId)).Count);
        Assert.Equal(TaskCategory.Daily, tasks[0].Category);
    }

    [Fact]
    public async Task AnInviteCodeJoinsWithItsRoleUntilItExpires()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, _) = await t.AddFamilyAsync();
        var invite = await t.Families.CreateInviteAsync(parent, new CreateInviteRequest(FamilyRole.Parent));
        Assert.Equal(Limits.InviteCodeLength, invite.Code.Length);

        var secondParent = await t.AddUserAsync("Mom");
        var joined = await t.Families.JoinAsync(secondParent, new JoinFamilyRequest(invite.Code.ToLowerInvariant()));
        Assert.Equal(FamilyRole.Parent, joined.Role);

        var again = await Assert.ThrowsAsync<DomainException>(() => t.Families.JoinAsync(secondParent, new JoinFamilyRequest(invite.Code)));
        Assert.Equal(DomainError.Conflict, again.Error);

        t.Clock.Advance(TimeSpan.FromDays(Limits.InviteLifetimeDays + 1));
        var late = await t.AddUserAsync("Late");
        var expired = await Assert.ThrowsAsync<DomainException>(() => t.Families.JoinAsync(late, new JoinFamilyRequest(invite.Code)));
        Assert.Equal(DomainError.Validation, expired.Error);
    }

    [Fact]
    public async Task TheLastParentCannotBeRemoved()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();

        var error = await Assert.ThrowsAsync<DomainException>(() => t.Families.RemoveMemberAsync(parent, parent.Id));
        Assert.Equal(DomainError.Conflict, error.Error);

        await t.Families.RemoveMemberAsync(parent, child.Id);
        await Assert.ThrowsAsync<DomainException>(() => t.Access.RequireAsync(child.FamilyId, child.UserId));
    }

    [Fact]
    public async Task AccessHidesFamiliesFromStrangersAndChecksRoles()
    {
        await using var t = await TestDb.CreateAsync();
        var (_, child) = await t.AddFamilyAsync();
        var stranger = await t.AddUserAsync("Stranger");

        var hidden = await Assert.ThrowsAsync<DomainException>(() => t.Access.RequireAsync(child.FamilyId, stranger));
        Assert.Equal(DomainError.NotFound, hidden.Error);

        var forbidden = await Assert.ThrowsAsync<DomainException>(() => t.Access.RequireAsync(child.FamilyId, child.UserId, FamilyRole.Parent));
        Assert.Equal(DomainError.Forbidden, forbidden.Error);
    }

    [Fact]
    public async Task ATaskRangeMustGrowUpwards()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, _) = await t.AddFamilyAsync();

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            t.Catalog.AddTaskAsync(parent.FamilyId, new TaskRequest("Рисунок", null, 5, 3, TaskCategory.Extra, null)));
        Assert.Equal(DomainError.Validation, error.Error);
    }
}
