namespace Merito.Shared;

/// <summary>Russian wording of a point count: "1 балл", "3 балла", "5 баллов".</summary>
public static class PointText
{
    /// <summary>The number with its word: "1 балл", "3 балла", "5 баллов".</summary>
    public static string Of(int value) => $"{value} {Word(value)}";

    /// <summary>"балл", "балла" or "баллов" to follow the number; the sign does not matter.</summary>
    public static string Word(int value)
    {
        var n = Math.Abs(value) % 100;
        if (n is >= 11 and <= 14) return "баллов";
        return (n % 10) switch
        {
            1 => "балл",
            2 or 3 or 4 => "балла",
            _ => "баллов",
        };
    }
}
