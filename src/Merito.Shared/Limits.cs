namespace Merito.Shared;

/// <summary>Input limits enforced by the server and mirrored by the client forms.</summary>
public static class Limits
{
    /// <summary>Longest display name of a person or a family.</summary>
    public const int NameMaxLength = 60;

    /// <summary>Longest title of a task, penalty, reward or custom submission.</summary>
    public const int TitleMaxLength = 160;

    /// <summary>Longest free-text description or comment.</summary>
    public const int TextMaxLength = 500;

    /// <summary>Longest "time of day" hint on a task (e.g. "утро/вечер").</summary>
    public const int TimeOfDayMaxLength = 30;

    /// <summary>The largest number of points a single task, penalty, reward or adjustment may carry.</summary>
    public const int PointsMax = 10_000;

    /// <summary>Shortest login of a child account.</summary>
    public const int LoginMinLength = 3;

    /// <summary>Longest login of a child account.</summary>
    public const int LoginMaxLength = 32;

    /// <summary>Shortest password accepted for any account.</summary>
    public const int PasswordMinLength = 6;

    /// <summary>Days an invite code stays valid after it is created.</summary>
    public const int InviteLifetimeDays = 7;

    /// <summary>Characters an invite code is made of; look-alike letters and digits are left out.</summary>
    public const string InviteAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Length of an invite code.</summary>
    public const int InviteCodeLength = 8;
}
