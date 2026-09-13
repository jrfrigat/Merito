using Merito.Shared;

namespace Merito.Server.Infrastructure;

/// <summary>Input checks shared by the domain services; each throws a 400 with a message for the user.</summary>
public static class Guard
{
    /// <summary>Trims a required text and checks its length.</summary>
    public static string Required(string? value, int maxLength, string field)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) throw DomainException.Invalid($"Заполните поле \"{field}\".");
        if (text.Length > maxLength) throw DomainException.Invalid($"Поле \"{field}\" длиннее {maxLength} символов.");
        return text;
    }

    /// <summary>Trims an optional text, turns blank into null and checks its length.</summary>
    public static string? Optional(string? value, int maxLength, string field)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length > maxLength) throw DomainException.Invalid($"Поле \"{field}\" длиннее {maxLength} символов.");
        return text;
    }

    /// <summary>Checks a number of points is between 1 and <see cref="Limits.PointsMax"/>.</summary>
    public static int Points(int value, string field)
    {
        if (value < 1 || value > Limits.PointsMax)
            throw DomainException.Invalid($"Поле \"{field}\" должно быть от 1 до {Limits.PointsMax}.");
        return value;
    }

    /// <summary>Checks a value is a defined member of its enum.</summary>
    public static T Defined<T>(T value, string field) where T : struct, Enum =>
        Enum.IsDefined(value) ? value : throw DomainException.Invalid($"Недопустимое значение поля \"{field}\".");
}
