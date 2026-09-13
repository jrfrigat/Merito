using System.Text.RegularExpressions;
using Merito.Server.Data;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Families;

/// <summary>Creates login-based child accounts inside a family and resets their passwords.</summary>
public sealed partial class ChildAccountService(MeritoDbContext db, UserManager<AppUser> users, TimeProvider clock)
{
    /// <summary>Creates a child account and its membership in the parent's family.</summary>
    public async Task<MemberDto> CreateAsync(FamilyMember parent, CreateChildRequest request, CancellationToken ct = default)
    {
        var name = Guard.Required(request.DisplayName, Limits.NameMaxLength, "Имя");
        var login = Guard.Required(request.Login, Limits.LoginMaxLength, "Логин").ToLowerInvariant();
        if (login.Length < Limits.LoginMinLength || !LoginPattern().IsMatch(login))
        {
            throw DomainException.Invalid(
                $"Логин: от {Limits.LoginMinLength} до {Limits.LoginMaxLength} символов, латинские буквы, цифры, точка, дефис и подчеркивание.");
        }
        if (await users.FindByNameAsync(login) is not null)
            throw DomainException.Conflict("Такой логин уже занят.");

        var now = clock.GetUtcNow().UtcDateTime;
        // UserManager saves on its own; the transaction keeps a failed membership from leaving an orphan account.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var user = new AppUser { Id = Guid.NewGuid(), UserName = login, DisplayName = name, CreatedAt = now };
        var created = await users.CreateAsync(user, request.Password ?? "");
        if (!created.Succeeded) throw IdentityErrors.ToException(created);

        var member = new FamilyMember
        {
            Id = Guid.NewGuid(), FamilyId = parent.FamilyId, UserId = user.Id, Role = FamilyRole.Child, JoinedAt = now,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new MemberDto(member.Id, name, FamilyRole.Child, 0, login);
    }

    /// <summary>Sets a new password for a child account of the parent's family.</summary>
    public async Task ResetPasswordAsync(FamilyMember parent, Guid memberId, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var member = await db.Members.Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.FamilyId == parent.FamilyId && m.IsActive && m.Role == FamilyRole.Child, ct)
            ?? throw DomainException.NotFound("Ребенок не найден в этой семье.");
        if (member.User.Email is not null)
            throw DomainException.Conflict("Пароль аккаунта с почтой меняет только его владелец.");

        var token = await users.GeneratePasswordResetTokenAsync(member.User);
        var result = await users.ResetPasswordAsync(member.User, token, request.Password ?? "");
        if (!result.Succeeded) throw IdentityErrors.ToException(result);
    }

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex LoginPattern();
}
