using System.Security.Cryptography;
using Merito.Server.Data;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Families;

/// <summary>Creates families, manages membership through invite codes and lists members.</summary>
public sealed class FamilyService(MeritoDbContext db, TimeProvider clock)
{
    /// <summary>Active memberships of an account, oldest first.</summary>
    public async Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        await db.Members
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new MembershipDto(m.FamilyId, m.Family.Name, m.Id, m.Role))
            .ToListAsync(ct);

    /// <summary>Creates a family with the caller as its first parent, optionally with the example catalog.</summary>
    public async Task<MembershipDto> CreateAsync(Guid userId, CreateFamilyRequest request, CancellationToken ct = default)
    {
        var name = Guard.Required(request.Name, Limits.NameMaxLength, "Название семьи");
        var now = clock.GetUtcNow().UtcDateTime;

        var family = new Family { Id = Guid.NewGuid(), Name = name, CreatedAt = now };
        var member = new FamilyMember
        {
            Id = Guid.NewGuid(), FamilyId = family.Id, UserId = userId, Role = FamilyRole.Parent, JoinedAt = now,
        };
        db.Families.Add(family);
        db.Members.Add(member);
        if (request.SeedExample) ExampleCatalog.AddTo(db, family.Id, now);

        await db.SaveChangesAsync(ct);
        return new MembershipDto(family.Id, family.Name, member.Id, member.Role);
    }

    /// <summary>Renames the family.</summary>
    public async Task RenameAsync(FamilyMember parent, RenameFamilyRequest request, CancellationToken ct = default)
    {
        var name = Guard.Required(request.Name, Limits.NameMaxLength, "Название семьи");
        var family = await db.Families.FirstAsync(f => f.Id == parent.FamilyId, ct);
        family.Name = name;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The family with its active members; a child sees only their own balance.</summary>
    public async Task<FamilyDto> GetAsync(FamilyMember caller, CancellationToken ct = default)
    {
        var family = await db.Families.AsNoTracking().FirstAsync(f => f.Id == caller.FamilyId, ct);
        var members = await db.Members.AsNoTracking()
            .Where(m => m.FamilyId == caller.FamilyId && m.IsActive)
            .Select(m => new { m.Id, m.User.DisplayName, m.Role, m.Balance, m.User.UserName })
            .ToListAsync(ct);

        // Role is stored as text, so the database would sort "Child" before "Parent".
        var isParent = caller.Role == FamilyRole.Parent;
        return new FamilyDto(family.Id, family.Name, members
            .OrderBy(m => m.Role).ThenBy(m => m.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(m => new MemberDto(
                m.Id,
                m.DisplayName,
                m.Role,
                m.Role == FamilyRole.Child && (isParent || m.Id == caller.Id) ? m.Balance : null,
                isParent && m.Role == FamilyRole.Child ? m.UserName : null))
            .ToList());
    }

    /// <summary>Creates an invite code for the role; codes stay valid for <see cref="Limits.InviteLifetimeDays"/> days.</summary>
    public async Task<InviteDto> CreateInviteAsync(FamilyMember parent, CreateInviteRequest request, CancellationToken ct = default)
    {
        var role = Guard.Defined(request.Role, "Роль");
        var invite = new FamilyInvite
        {
            Id = Guid.NewGuid(),
            FamilyId = parent.FamilyId,
            Code = NewCode(),
            Role = role,
            ExpiresAt = clock.GetUtcNow().UtcDateTime.AddDays(Limits.InviteLifetimeDays),
            CreatedByMemberId = parent.Id,
        };
        db.Invites.Add(invite);
        await db.SaveChangesAsync(ct);
        return new InviteDto(invite.Code, invite.Role, invite.ExpiresAt);
    }

    /// <summary>Joins the family behind an invite code; a removed member is brought back with the new role.</summary>
    public async Task<MembershipDto> JoinAsync(Guid userId, JoinFamilyRequest request, CancellationToken ct = default)
    {
        var code = Guard.Required(request.Code, 32, "Код приглашения").Replace("-", "").Replace(" ", "").ToUpperInvariant();
        var now = clock.GetUtcNow().UtcDateTime;
        var invite = await db.Invites.Include(i => i.Family).FirstOrDefaultAsync(i => i.Code == code, ct);
        if (invite is null || invite.ExpiresAt <= now)
            throw DomainException.Invalid("Код не найден или срок его действия истек.");

        var member = await db.Members.FirstOrDefaultAsync(m => m.FamilyId == invite.FamilyId && m.UserId == userId, ct);
        if (member is { IsActive: true })
            throw DomainException.Conflict("Вы уже состоите в этой семье.");

        if (member is null)
        {
            member = new FamilyMember { Id = Guid.NewGuid(), FamilyId = invite.FamilyId, UserId = userId };
            db.Members.Add(member);
        }
        member.Role = invite.Role;
        member.IsActive = true;
        member.JoinedAt = now;

        await db.SaveChangesAsync(ct);
        return new MembershipDto(invite.FamilyId, invite.Family.Name, member.Id, member.Role);
    }

    /// <summary>Removes a member from the family; the last parent cannot leave it without a parent.</summary>
    public async Task RemoveMemberAsync(FamilyMember parent, Guid memberId, CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == memberId && m.FamilyId == parent.FamilyId && m.IsActive, ct)
            ?? throw DomainException.NotFound("Участник не найден.");

        if (member.Role == FamilyRole.Parent)
        {
            var parents = await db.Members.CountAsync(m => m.FamilyId == parent.FamilyId && m.IsActive && m.Role == FamilyRole.Parent, ct);
            if (parents <= 1) throw DomainException.Conflict("В семье должен остаться хотя бы один родитель.");
        }

        member.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private static string NewCode() =>
        string.Create(Limits.InviteCodeLength, 0, static (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = Limits.InviteAlphabet[RandomNumberGenerator.GetInt32(Limits.InviteAlphabet.Length)];
        });
}
