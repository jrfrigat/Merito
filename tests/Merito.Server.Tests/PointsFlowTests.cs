using Merito.Server.Infrastructure;
using Merito.Server.Tests.Support;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Tests;

public sealed class PointsFlowTests
{
    [Fact]
    public async Task ApprovingACatalogTaskCreditsTheAwardedPoints()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync(seedExample: true);
        var task = (await t.Catalog.GetTasksAsync(child.FamilyId)).First();

        var submission = await t.Submissions.SubmitAsync(child, new SubmitRequest(task.Id, null, "сделал"));
        await t.Submissions.ApproveAsync(parent, submission.Id, new ApproveRequest(task.Points, null, null));

        await t.Db.Entry(child).ReloadAsync();
        Assert.Equal(task.Points, child.Balance);
        var entry = await t.Db.Transactions.SingleAsync();
        Assert.Equal(TransactionKind.Task, entry.Kind);
        Assert.Equal(task.Points, entry.BalanceAfter);
        Assert.Equal(parent.Id, entry.AuthorMemberId);
    }

    [Fact]
    public async Task ApprovingCustomWorkCanSaveItIntoTheCatalog()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();

        var submission = await t.Submissions.SubmitAsync(child, new SubmitRequest(null, "Помыл машину", null));
        await t.Submissions.ApproveAsync(parent, submission.Id, new ApproveRequest(6, "молодец", TaskCategory.Extra));

        var task = Assert.Single(await t.Catalog.GetTasksAsync(child.FamilyId));
        Assert.Equal("Помыл машину", task.Title);
        Assert.Equal(6, task.Points);
        Assert.Equal(TaskCategory.Extra, task.Category);
        Assert.Equal(task.Id, (await t.Db.Submissions.SingleAsync()).TaskId);
    }

    [Fact]
    public async Task ASubmissionCannotBeApprovedTwice()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        var submission = await t.Submissions.SubmitAsync(child, new SubmitRequest(null, "Дело", null));
        await t.Submissions.ApproveAsync(parent, submission.Id, new ApproveRequest(3, null, null));

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            t.Submissions.ApproveAsync(parent, submission.Id, new ApproveRequest(3, null, null)));
        Assert.Equal(DomainError.Conflict, error.Error);
        Assert.Equal(1, await t.Db.Transactions.CountAsync());
    }

    [Fact]
    public async Task RejectingMovesNoPoints()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        var submission = await t.Submissions.SubmitAsync(child, new SubmitRequest(null, "Дело", null));

        await t.Submissions.RejectAsync(parent, submission.Id, new RejectRequest("не сделано"));

        Assert.Equal(0, child.Balance);
        Assert.Empty(t.Db.Transactions);
        Assert.Equal(SubmissionStatus.Rejected, (await t.Db.Submissions.SingleAsync()).Status);
    }

    [Fact]
    public async Task CustomWorkNeedsATitle()
    {
        await using var t = await TestDb.CreateAsync();
        var (_, child) = await t.AddFamilyAsync();

        var error = await Assert.ThrowsAsync<DomainException>(() => t.Submissions.SubmitAsync(child, new SubmitRequest(null, "  ", null)));
        Assert.Equal(DomainError.Validation, error.Error);
    }

    [Fact]
    public async Task AManualAdjustmentRequiresAComment()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();

        var error = await Assert.ThrowsAsync<DomainException>(() => t.Points.AdjustAsync(parent, new AdjustPointsRequest(child.Id, 5, "")));
        Assert.Equal(DomainError.Validation, error.Error);
        Assert.Equal(5, await t.Points.AdjustAsync(parent, new AdjustPointsRequest(child.Id, 5, "за помощь бабушке")));
        Assert.Equal(TransactionKind.Bonus, (await t.Db.Transactions.SingleAsync()).Kind);
    }

    [Fact]
    public async Task APenaltyMayTakeTheBalanceBelowZero()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync(seedExample: true);
        var penalty = (await t.Catalog.GetPenaltiesAsync(child.FamilyId)).First(p => p.Points == 20);

        var balance = await t.Points.ApplyPenaltyAsync(parent, new ApplyPenaltyRequest(child.Id, penalty.Id, null));

        Assert.Equal(-20, balance);
        Assert.Equal(TransactionKind.Penalty, (await t.Db.Transactions.SingleAsync()).Kind);
    }

    [Fact]
    public async Task PointsCannotBeGivenToAParent()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, _) = await t.AddFamilyAsync();

        var error = await Assert.ThrowsAsync<DomainException>(() => t.Points.AdjustAsync(parent, new AdjustPointsRequest(parent.Id, 5, "себе")));
        Assert.Equal(DomainError.NotFound, error.Error);
    }

    [Fact]
    public async Task APurchaseNeedsEnoughPointsAndCancellingRefundsThem()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync(seedExample: true);
        var reward = (await t.Catalog.GetRewardsAsync(child.FamilyId)).First(r => r.Cost == 8);

        var poor = await Assert.ThrowsAsync<DomainException>(() => t.Shop.BuyAsync(child, new PurchaseRequest(reward.Id)));
        Assert.Equal(DomainError.Conflict, poor.Error);

        await t.Points.AdjustAsync(parent, new AdjustPointsRequest(child.Id, 10, "старт"));
        var purchase = await t.Shop.BuyAsync(child, new PurchaseRequest(reward.Id));
        Assert.Equal(2, child.Balance);

        await t.Shop.CancelAsync(parent, purchase.Id);
        Assert.Equal(10, child.Balance);
        var kinds = await t.Db.Transactions.OrderBy(x => x.CreatedAt).Select(x => x.Kind).ToListAsync();
        Assert.Contains(TransactionKind.Refund, kinds);

        var again = await Assert.ThrowsAsync<DomainException>(() => t.Shop.CancelAsync(parent, purchase.Id));
        Assert.Equal(DomainError.Conflict, again.Error);
        Assert.Equal(10, child.Balance);
    }

    [Fact]
    public async Task AStaleBalanceCannotBeSpentTwice()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync(seedExample: true);
        await t.Points.AdjustAsync(parent, new AdjustPointsRequest(child.Id, 10, "старт"));
        var reward = (await t.Catalog.GetRewardsAsync(child.FamilyId)).First(r => r.Cost == 8);

        // A second context holds the same balance, as a parallel request would.
        await using var other = new Merito.Server.Data.MeritoDbContext(
            new DbContextOptionsBuilder<Merito.Server.Data.MeritoDbContext>().UseSqlite(t.Db.Database.GetDbConnection()).Options);
        var stale = await other.Members.Include(m => m.User).SingleAsync(m => m.Id == child.Id);

        await t.Shop.BuyAsync(child, new PurchaseRequest(reward.Id));

        var otherNotifications = new Merito.Server.Features.Notifications.NotificationService(other, t.Clock);
        var otherLedger = new Merito.Server.Features.Points.LedgerService(other, otherNotifications, t.Clock);
        var otherShop = new Merito.Server.Features.Shop.ShopService(other, otherLedger, t.Clock);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => otherShop.BuyAsync(stale, new PurchaseRequest(reward.Id)));
    }

    [Fact]
    public async Task AChildSeesOnlyTheirOwnHistory()
    {
        await using var t = await TestDb.CreateAsync();
        var (parent, child) = await t.AddFamilyAsync();
        var siblingUser = await t.AddUserAsync("Sibling");
        t.Db.Members.Add(new Merito.Server.Data.FamilyMember { Id = Guid.NewGuid(), FamilyId = child.FamilyId, UserId = siblingUser, Role = FamilyRole.Child });
        await t.Db.SaveChangesAsync();
        var sibling = await t.Access.RequireAsync(child.FamilyId, siblingUser);

        await t.Points.AdjustAsync(parent, new AdjustPointsRequest(child.Id, 3, "a"));
        await t.Points.AdjustAsync(parent, new AdjustPointsRequest(sibling.Id, 4, "b"));

        var own = await t.Points.ListAsync(child, childId: sibling.Id, take: 50);
        Assert.All(own, e => Assert.Equal(child.Id, e.ChildId));
        Assert.Equal(2, (await t.Points.ListAsync(parent, null, 50)).Count);

        var family = await t.Families.GetAsync(child);
        Assert.Null(family.Members.Single(m => m.Id == sibling.Id).Balance);
        Assert.Equal(3, family.Members.Single(m => m.Id == child.Id).Balance);
    }
}
