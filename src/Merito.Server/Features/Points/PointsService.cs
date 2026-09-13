using Merito.Server.Data;
using Merito.Server.Features.Families;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Points;

/// <summary>Parents grant, deduct and fine by hand; everyone reads the ledger they are allowed to see.</summary>
public sealed class PointsService(MeritoDbContext db, FamilyAccess access, LedgerService ledger)
{
    /// <summary>The largest page a list request returns.</summary>
    public const int MaxTake = 200;

    /// <summary>Ledger entries newest first; a child always gets only their own.</summary>
    public async Task<IReadOnlyList<TransactionDto>> ListAsync(FamilyMember caller, Guid? childId, int take, CancellationToken ct = default)
    {
        var query = db.Transactions.AsNoTracking().Where(t => t.FamilyId == caller.FamilyId);
        if (caller.Role == FamilyRole.Child) query = query.Where(t => t.ChildMemberId == caller.Id);
        else if (childId is { } child) query = query.Where(t => t.ChildMemberId == child);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(Math.Clamp(take, 1, MaxTake))
            .Select(t => new TransactionDto(
                t.Id, t.ChildMemberId, t.ChildMember.User.DisplayName, t.Amount, t.Kind, t.Title, t.Comment,
                t.Author == null ? null : t.Author.User.DisplayName, t.CreatedAt, t.BalanceAfter))
            .ToListAsync(ct);
    }

    /// <summary>Grants (positive) or deducts (negative) points; a comment is required either way.</summary>
    public async Task<int> AdjustAsync(FamilyMember parent, AdjustPointsRequest request, CancellationToken ct = default)
    {
        if (request.Amount == 0 || Math.Abs(request.Amount) > Limits.PointsMax)
            throw DomainException.Invalid($"Укажите количество баллов от 1 до {Limits.PointsMax} со знаком плюс или минус.");
        var comment = Guard.Required(request.Comment, Limits.TextMaxLength, "Комментарий");
        var child = await access.RequireChildAsync(parent.FamilyId, request.ChildId, ct);

        var (kind, title) = request.Amount > 0
            ? (TransactionKind.Bonus, "Бонус от родителя")
            : (TransactionKind.Deduction, "Списание родителем");
        ledger.Post(child, request.Amount, kind, title, comment, parent);
        await db.SaveChangesAsync(ct);
        return child.Balance;
    }

    /// <summary>Applies a catalog penalty; it may take the balance below zero.</summary>
    public async Task<int> ApplyPenaltyAsync(FamilyMember parent, ApplyPenaltyRequest request, CancellationToken ct = default)
    {
        var comment = Guard.Optional(request.Comment, Limits.TextMaxLength, "Комментарий");
        var penalty = await db.Penalties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PenaltyId && p.FamilyId == parent.FamilyId && !p.IsArchived, ct)
            ?? throw DomainException.NotFound("Штраф не найден.");
        var child = await access.RequireChildAsync(parent.FamilyId, request.ChildId, ct);

        ledger.Post(child, -penalty.Points, TransactionKind.Penalty, penalty.Title, comment, parent, penaltyId: penalty.Id);
        await db.SaveChangesAsync(ct);
        return child.Balance;
    }
}
