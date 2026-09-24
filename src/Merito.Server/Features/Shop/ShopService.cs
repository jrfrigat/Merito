using Merito.Server.Data;
using Merito.Server.Features.Points;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Shop;

/// <summary>Children buy rewards with points paid at once; parents hand them over or cancel with a refund.</summary>
public sealed class ShopService(MeritoDbContext db, LedgerService ledger, TimeProvider clock)
{
    /// <summary>The largest page a list request returns.</summary>
    public const int MaxTake = 200;

    /// <summary>Purchases newest first; a child always gets only their own.</summary>
    public async Task<IReadOnlyList<PurchaseDto>> ListAsync(
        FamilyMember caller, PurchaseStatus? status, Guid? childId, int take, CancellationToken ct = default)
    {
        var query = db.Purchases.AsNoTracking().Where(p => p.FamilyId == caller.FamilyId);
        if (caller.Role == FamilyRole.Child) query = query.Where(p => p.ChildMemberId == caller.Id);
        else if (childId is { } child) query = query.Where(p => p.ChildMemberId == child);
        if (status is { } st) query = query.Where(p => p.Status == st);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(Math.Clamp(take, 1, MaxTake))
            .Select(p => new PurchaseDto(
                p.Id, p.ChildMemberId, p.ChildMember.User.DisplayName, p.RewardId, p.Title, p.Cost, p.Status,
                p.CreatedAt, p.ResolvedAt, p.ResolvedBy == null ? null : p.ResolvedBy.User.DisplayName))
            .ToListAsync(ct);
    }

    /// <summary>A child buys a reward; refused when the balance does not cover the price.</summary>
    public async Task<PurchaseDto> BuyAsync(FamilyMember child, PurchaseRequest request, CancellationToken ct = default)
    {
        var reward = await db.Rewards.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RewardId && r.FamilyId == child.FamilyId && !r.IsArchived, ct)
            ?? throw DomainException.NotFound("Награда не найдена.");
        if (child.Balance < reward.Cost)
            throw DomainException.Conflict($"Не хватает баллов: нужно {reward.Cost}, на счету {child.Balance}.");

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            FamilyId = child.FamilyId,
            ChildMemberId = child.Id,
            RewardId = reward.Id,
            Title = reward.Title,
            Cost = reward.Cost,
            Status = PurchaseStatus.Pending,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.Purchases.Add(purchase);
        await ledger.PostAsync(child, -reward.Cost, TransactionKind.Purchase, reward.Title, null, child,
            purchaseId: purchase.Id, ct: ct);
        await db.SaveChangesAsync(ct);

        return new PurchaseDto(purchase.Id, child.Id, child.User.DisplayName, reward.Id, purchase.Title, purchase.Cost,
            purchase.Status, purchase.CreatedAt, null, null);
    }

    /// <summary>A parent marks a pending purchase as handed over.</summary>
    public async Task FulfillAsync(FamilyMember parent, Guid purchaseId, CancellationToken ct = default)
    {
        var purchase = await FindPendingAsync(parent, purchaseId, ct);
        Resolve(purchase, PurchaseStatus.Fulfilled, parent);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>A parent, or the child who bought it, cancels a pending purchase and the points come back.</summary>
    public async Task CancelAsync(FamilyMember caller, Guid purchaseId, CancellationToken ct = default)
    {
        var purchase = await FindPendingAsync(caller, purchaseId, ct);
        if (caller.Role == FamilyRole.Child && purchase.ChildMemberId != caller.Id)
            throw DomainException.NotFound("Покупка не найдена.");

        Resolve(purchase, PurchaseStatus.Cancelled, caller);
        await ledger.PostAsync(purchase.ChildMember, purchase.Cost, TransactionKind.Refund, purchase.Title, null, caller,
            purchaseId: purchase.Id, ct: ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>A parent hands over the purchase linked to their notification and reads it.</summary>
    public Task FulfillFromNotificationAsync(FamilyMember parent, Guid notificationId, CancellationToken ct = default) =>
        ResolveFromNotificationAsync(parent, notificationId, PurchaseStatus.Fulfilled, ct);

    /// <summary>A parent cancels the purchase linked to their notification, refunds it and reads the notification.</summary>
    public Task CancelFromNotificationAsync(FamilyMember parent, Guid notificationId, CancellationToken ct = default) =>
        ResolveFromNotificationAsync(parent, notificationId, PurchaseStatus.Cancelled, ct);

    private async Task ResolveFromNotificationAsync(
        FamilyMember parent, Guid notificationId, PurchaseStatus targetStatus, CancellationToken ct)
    {
        var notification = await db.Notifications
            .Include(n => n.Purchase!)
            .ThenInclude(p => p.ChildMember)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(n => n.Id == notificationId
                && n.RecipientMemberId == parent.Id
                && n.FamilyId == parent.FamilyId
                && n.PurchaseId != null, ct)
            ?? throw DomainException.NotFound("Уведомление о покупке не найдено.");
        var purchase = notification.Purchase!;

        if (purchase.Status != PurchaseStatus.Pending)
        {
            if (purchase.Status != targetStatus)
                throw DomainException.Conflict("По этой покупке уже принято другое решение.");

            if (notification.ReadAt is null)
            {
                notification.ReadAt = clock.GetUtcNow().UtcDateTime;
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        Resolve(purchase, targetStatus, parent);
        if (targetStatus == PurchaseStatus.Cancelled)
        {
            await ledger.PostAsync(purchase.ChildMember, purchase.Cost, TransactionKind.Refund,
                purchase.Title, null, parent, purchaseId: purchase.Id, ct: ct);
        }
        notification.ReadAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
    }

    private void Resolve(Purchase purchase, PurchaseStatus status, FamilyMember by)
    {
        purchase.Status = status;
        purchase.ResolvedAt = clock.GetUtcNow().UtcDateTime;
        purchase.ResolvedByMemberId = by.Id;
        purchase.Version = Guid.NewGuid();
    }

    private async Task<Purchase> FindPendingAsync(FamilyMember caller, Guid purchaseId, CancellationToken ct)
    {
        var purchase = await db.Purchases
            .Include(p => p.ChildMember)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == purchaseId && p.FamilyId == caller.FamilyId, ct)
            ?? throw DomainException.NotFound("Покупка не найдена.");
        if (purchase.Status != PurchaseStatus.Pending)
            throw DomainException.Conflict("Эта покупка уже закрыта.");
        return purchase;
    }
}
