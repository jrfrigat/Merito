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
}
