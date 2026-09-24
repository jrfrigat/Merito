using Merito.Shared;

namespace Merito.Server.Data;

/// <summary>A durable notification addressed to one family member.</summary>
public sealed class AppNotification
{
    /// <summary>Notification id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family in which the event happened.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>The family.</summary>
    public Family Family { get; set; } = null!;

    /// <summary>The member who receives the notification.</summary>
    public Guid RecipientMemberId { get; set; }

    /// <summary>The member who receives the notification.</summary>
    public FamilyMember Recipient { get; set; } = null!;

    /// <summary>The event represented by this notification.</summary>
    public NotificationKind Kind { get; set; }

    /// <summary>Short heading.</summary>
    public string Title { get; set; } = "";

    /// <summary>Human-readable details.</summary>
    public string Message { get; set; } = "";

    /// <summary>UTC moment when the notification was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC moment when the recipient read the notification.</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>The purchase that can be resolved from this notification.</summary>
    public Guid? PurchaseId { get; set; }

    /// <summary>The purchase that can be resolved from this notification.</summary>
    public Purchase? Purchase { get; set; }

    /// <summary>Web Push delivery attempts created with this notification.</summary>
    public ICollection<WebPushDelivery> PushDeliveries { get; set; } = [];
}

/// <summary>A Push API subscription owned by one family member.</summary>
public sealed class WebPushSubscription
{
    /// <summary>Subscription id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning family member id.</summary>
    public Guid MemberId { get; set; }

    /// <summary>Owning family member.</summary>
    public FamilyMember Member { get; set; } = null!;

    /// <summary>Push service endpoint.</summary>
    public string Endpoint { get; set; } = "";

    /// <summary>Client public encryption key.</summary>
    public string P256dh { get; set; } = "";

    /// <summary>Client authentication secret.</summary>
    public string Auth { get; set; } = "";

    /// <summary>UTC creation time.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC time of the latest browser refresh.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Deliveries targeting this subscription.</summary>
    public ICollection<WebPushDelivery> Deliveries { get; set; } = [];
}

/// <summary>A durable attempt to deliver one notification to one subscription.</summary>
public sealed class WebPushDelivery
{
    /// <summary>Delivery id.</summary>
    public Guid Id { get; set; }

    /// <summary>Notification id.</summary>
    public Guid NotificationId { get; set; }

    /// <summary>Notification payload source.</summary>
    public AppNotification Notification { get; set; } = null!;

    /// <summary>Target subscription id.</summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>Target subscription.</summary>
    public WebPushSubscription Subscription { get; set; } = null!;

    /// <summary>Number of completed attempts.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Earliest UTC time for the next attempt.</summary>
    public DateTime NextAttemptAt { get; set; }

    /// <summary>UTC success time.</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>Last delivery failure, truncated for storage.</summary>
    public string? LastError { get; set; }
}
