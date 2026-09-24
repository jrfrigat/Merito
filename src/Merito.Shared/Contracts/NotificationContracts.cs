namespace Merito.Shared.Contracts;

/// <summary>A notification addressed to the current family member.</summary>
/// <param name="Id">Notification id.</param>
/// <param name="Kind">The event represented by the notification.</param>
/// <param name="Title">Short heading.</param>
/// <param name="Message">Human-readable details.</param>
/// <param name="CreatedAt">UTC moment when the notification was created.</param>
/// <param name="ReadAt">UTC moment when it was read; null while unread.</param>
/// <param name="Purchase">Current purchase state when this notification can resolve a purchase.</param>
public sealed record NotificationDto(
    Guid Id,
    NotificationKind Kind,
    string Title,
    string Message,
    DateTime CreatedAt,
    DateTime? ReadAt,
    NotificationPurchaseDto? Purchase);

/// <summary>A purchase linked to an actionable notification.</summary>
public sealed record NotificationPurchaseDto(
    Guid Id,
    string ChildName,
    string Title,
    int Cost,
    PurchaseStatus Status,
    DateTime? ResolvedAt,
    string? ResolverName,
    bool CanFulfill,
    bool CanCancel);

/// <summary>Number of unread notifications for the current family member.</summary>
/// <param name="Count">Unread notification count.</param>
public sealed record UnreadCountDto(int Count);

/// <summary>Public server configuration required to create a browser push subscription.</summary>
/// <param name="PublicKey">URL-safe Base64 VAPID public key, or an empty string when disabled.</param>
public sealed record WebPushPublicKeyDto(string PublicKey);

/// <summary>A browser Push API subscription.</summary>
/// <param name="Endpoint">Push service endpoint.</param>
/// <param name="P256dh">Client public encryption key.</param>
/// <param name="Auth">Client authentication secret.</param>
public sealed record WebPushSubscriptionRequest(string Endpoint, string P256dh, string Auth);

/// <summary>Identifies a browser Push API subscription to remove.</summary>
/// <param name="Endpoint">Push service endpoint.</param>
public sealed record WebPushUnsubscribeRequest(string Endpoint);
