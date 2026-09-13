using Merito.Server.Data;
using Merito.Shared;

namespace Merito.Server.Features.Points;

/// <summary>
/// The only writer of a child's balance. Each call adds one ledger entry and updates the balance and
/// the member's concurrency token together; the caller saves both in the same SaveChanges.
/// </summary>
public sealed class LedgerService(MeritoDbContext db, TimeProvider clock)
{
    /// <summary>Stages a balance change for <paramref name="child"/> and returns the staged entry.</summary>
    public PointTransaction Post(
        FamilyMember child,
        int amount,
        TransactionKind kind,
        string title,
        string? comment,
        FamilyMember? author,
        Guid? submissionId = null,
        Guid? purchaseId = null,
        Guid? penaltyId = null)
    {
        if (child.Role != FamilyRole.Child)
            throw new InvalidOperationException("Only a child has a balance.");
        if (amount == 0)
            throw new InvalidOperationException("A ledger entry must change the balance.");

        child.Balance += amount;
        child.Version = Guid.NewGuid();

        var entry = new PointTransaction
        {
            Id = Guid.NewGuid(),
            FamilyId = child.FamilyId,
            ChildMemberId = child.Id,
            Amount = amount,
            BalanceAfter = child.Balance,
            Kind = kind,
            Title = title,
            Comment = comment,
            AuthorMemberId = author?.Id,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            SubmissionId = submissionId,
            PurchaseId = purchaseId,
            PenaltyId = penaltyId,
        };
        db.Transactions.Add(entry);
        return entry;
    }
}
