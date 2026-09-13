using System.Globalization;
using Merito.Shared;

namespace Merito.Client.Services;

/// <summary>Russian wording for numbers, dates and enum values shown on screen.</summary>
public static class Format
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>"1 балл", "3 балла", "5 баллов".</summary>
    public static string Points(int value) => $"{value} {PointsWord(value)}";

    /// <summary>"балл", "балла" or "баллов" to follow the number.</summary>
    public static string PointsWord(int value) => Plural(value, "балл", "балла", "баллов");

    /// <summary>Signed points for the ledger: "+5", "-20".</summary>
    public static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString(Ru);

    /// <summary>Points of a catalog task: "3" or "3-5".</summary>
    public static string Range(int points, int? max) => max is { } m ? $"{points}-{m}" : points.ToString(Ru);

    /// <summary>A UTC moment in local time: "сегодня, 18:05", "вчера, 09:10" or "12 сент., 18:05".</summary>
    public static string When(DateTime utc)
    {
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        var today = DateTime.Now.Date;
        var time = local.ToString("HH:mm", Ru);
        if (local.Date == today) return $"сегодня, {time}";
        if (local.Date == today.AddDays(-1)) return $"вчера, {time}";
        return local.ToString(local.Year == today.Year ? "d MMM, HH:mm" : "d MMM yyyy, HH:mm", Ru);
    }

    /// <summary>Name of a catalog section.</summary>
    public static string Category(TaskCategory category) => category switch
    {
        TaskCategory.Daily => "Ежедневные",
        _ => "Дополнительные",
    };

    /// <summary>Name of a family role.</summary>
    public static string Role(FamilyRole role) => role == FamilyRole.Parent ? "Родитель" : "Ребенок";

    /// <summary>Short label of a ledger entry kind.</summary>
    public static string Kind(TransactionKind kind) => kind switch
    {
        TransactionKind.Task => "Задание",
        TransactionKind.Penalty => "Штраф",
        TransactionKind.Bonus => "Бонус",
        TransactionKind.Deduction => "Списание",
        TransactionKind.Purchase => "Покупка",
        _ => "Возврат",
    };

    private static string Plural(int value, string one, string few, string many)
    {
        var n = Math.Abs(value) % 100;
        if (n is >= 11 and <= 14) return many;
        return (n % 10) switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many,
        };
    }
}
