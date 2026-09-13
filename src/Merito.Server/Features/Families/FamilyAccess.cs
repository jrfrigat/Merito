using Merito.Server.Data;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Families;

/// <summary>Resolves the caller's active membership in a family and enforces the role an action needs.</summary>
public sealed class FamilyAccess(MeritoDbContext db)
{
    /// <summary>
    /// Returns the caller's tracked membership. A non-member gets 404 rather than 403, so family ids
    /// cannot be probed; a member with the wrong role gets 403.
    /// </summary>
    public async Task<FamilyMember> RequireAsync(Guid familyId, Guid userId, FamilyRole? role = null, CancellationToken ct = default)
    {
        var member = await db.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId && m.IsActive, ct)
            ?? throw DomainException.NotFound("Семья не найдена.");

        if (role is { } required && member.Role != required)
        {
            throw DomainException.Forbidden(required == FamilyRole.Parent
                ? "Это действие доступно только родителю."
                : "Это действие доступно только ребенку.");
        }

        return member;
    }

    /// <summary>Returns a tracked active child of the family, or 404.</summary>
    public async Task<FamilyMember> RequireChildAsync(Guid familyId, Guid childMemberId, CancellationToken ct = default) =>
        await db.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == childMemberId && m.FamilyId == familyId && m.IsActive && m.Role == FamilyRole.Child, ct)
        ?? throw DomainException.NotFound("Ребенок не найден в этой семье.");
}
