namespace Merito.Shared.Contracts;

/// <summary>A notification addressed to the current family member.</summary>
/// <param name="Id">Notification id.</param>
/// <param name="Kind">The event represented by the notification.</param>
/// <param name="Title">Short heading.</param>
/// <param name="Message">Human-readable details.</param>
/// <param name="CreatedAt">UTC moment when the notification was created.</param>
/// <param name="ReadAt">UTC moment when it was read; null while unread.</param>
public sealed record NotificationDto(
    Guid Id,
    NotificationKind Kind,
    string Title,
    string Message,
    DateTime CreatedAt,
    DateTime? ReadAt);

/// <summary>Number of unread notifications for the current family member.</summary>
/// <param name="Count">Unread notification count.</param>
public sealed record UnreadCountDto(int Count);
