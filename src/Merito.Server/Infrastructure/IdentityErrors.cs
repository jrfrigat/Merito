using Merito.Shared;
using Microsoft.AspNetCore.Identity;

namespace Merito.Server.Infrastructure;

/// <summary>Translates Identity failures into one readable Russian message.</summary>
public static class IdentityErrors
{
    /// <summary>A 400 (or 409 for a taken name) carrying the first failure in plain words.</summary>
    public static DomainException ToException(IdentityResult result)
    {
        var codes = result.Errors.Select(e => e.Code).ToHashSet();
        if (codes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName)) || codes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail)))
            return DomainException.Conflict("Аккаунт с таким логином или почтой уже есть.");
        if (codes.Contains(nameof(IdentityErrorDescriber.PasswordTooShort)))
            return DomainException.Invalid($"Пароль должен быть не короче {Limits.PasswordMinLength} символов.");
        if (codes.Contains(nameof(IdentityErrorDescriber.InvalidEmail)))
            return DomainException.Invalid("Проверьте адрес почты.");
        if (codes.Contains(nameof(IdentityErrorDescriber.InvalidUserName)))
            return DomainException.Invalid("Недопустимые символы в логине.");
        return DomainException.Invalid("Не удалось сохранить аккаунт: " + string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
