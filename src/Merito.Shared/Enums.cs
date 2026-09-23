using System.Text.Json.Serialization;

namespace Merito.Shared;

/// <summary>Where a task sits in the catalog: the everyday routine or occasional extra work.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TaskCategory>))]
public enum TaskCategory
{
    /// <summary>A routine the child can do every day (make the bed, read for 30 minutes).</summary>
    Daily = 0,

    /// <summary>Occasional work worth more (a good grade, a finished book, a project done early).</summary>
    Extra = 1,
}

/// <summary>Review state of something a child reported as done.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SubmissionStatus>))]
public enum SubmissionStatus
{
    /// <summary>Waiting for a parent to approve or reject it.</summary>
    Pending = 0,

    /// <summary>A parent confirmed it and the awarded points were credited.</summary>
    Approved = 1,

    /// <summary>A parent declined it; no points were credited.</summary>
    Rejected = 2,
}

/// <summary>Why a ledger entry changed a child's balance.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TransactionKind>))]
public enum TransactionKind
{
    /// <summary>Points for an approved submission.</summary>
    Task = 0,

    /// <summary>Points deducted for a penalty from the family catalog.</summary>
    Penalty = 1,

    /// <summary>Points a parent granted by hand, with a comment.</summary>
    Bonus = 2,

    /// <summary>Points a parent deducted by hand, with a comment.</summary>
    Deduction = 3,

    /// <summary>Points spent on a reward from the shop.</summary>
    Purchase = 4,

    /// <summary>Points returned when a purchase was cancelled.</summary>
    Refund = 5,
}

/// <summary>Lifecycle of a reward bought in the family shop.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PurchaseStatus>))]
public enum PurchaseStatus
{
    /// <summary>Paid for; the parent has not handed the reward over yet.</summary>
    Pending = 0,

    /// <summary>The parent handed the reward over.</summary>
    Fulfilled = 1,

    /// <summary>Cancelled by the parent or the child; the points were refunded.</summary>
    Cancelled = 2,
}

/// <summary>Why an application notification was created.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<NotificationKind>))]
public enum NotificationKind
{
    /// <summary>A child's balance increased.</summary>
    PointsCredited = 0,

    /// <summary>A child's balance decreased.</summary>
    PointsDebited = 1,

    /// <summary>A parent approved the child's submission.</summary>
    SubmissionApproved = 2,

    /// <summary>A parent rejected the child's submission.</summary>
    SubmissionRejected = 3,
}
