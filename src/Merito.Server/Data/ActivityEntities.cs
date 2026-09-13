using Merito.Shared;

namespace Merito.Server.Data;

/// <summary>Something a child reported as done, waiting for or carrying a parent's review.</summary>
public sealed class TaskSubmission
{
    /// <summary>Submission id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>The child who reported it.</summary>
    public Guid ChildMemberId { get; set; }

    /// <summary>The child who reported it.</summary>
    public FamilyMember ChildMember { get; set; } = null!;

    /// <summary>Catalog task, or null for custom work.</summary>
    public Guid? TaskId { get; set; }

    /// <summary>Catalog task, or null for custom work.</summary>
    public FamilyTask? Task { get; set; }

    /// <summary>What was done, copied from the task so renaming the task does not rewrite history.</summary>
    public string Title { get; set; } = "";

    /// <summary>The child's note.</summary>
    public string? ChildComment { get; set; }

    /// <summary>Review state.</summary>
    public SubmissionStatus Status { get; set; }

    /// <summary>UTC moment of the report.</summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>Parent who reviewed it.</summary>
    public Guid? ReviewedByMemberId { get; set; }

    /// <summary>Parent who reviewed it.</summary>
    public FamilyMember? ReviewedBy { get; set; }

    /// <summary>UTC moment of the review.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Points credited on approval.</summary>
    public int? AwardedPoints { get; set; }

    /// <summary>The parent's note.</summary>
    public string? ReviewComment { get; set; }

    /// <summary>Concurrency token, replaced on review so two parents cannot approve the same report twice.</summary>
    public Guid Version { get; set; } = Guid.NewGuid();
}

/// <summary>An immutable ledger entry; the sum of a child's entries is their balance.</summary>
public sealed class PointTransaction
{
    /// <summary>Entry id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>The child whose balance changed.</summary>
    public Guid ChildMemberId { get; set; }

    /// <summary>The child whose balance changed.</summary>
    public FamilyMember ChildMember { get; set; } = null!;

    /// <summary>Signed change of the balance.</summary>
    public int Amount { get; set; }

    /// <summary>The balance right after this entry.</summary>
    public int BalanceAfter { get; set; }

    /// <summary>Why the balance changed.</summary>
    public TransactionKind Kind { get; set; }

    /// <summary>What the entry is about.</summary>
    public string Title { get; set; } = "";

    /// <summary>Note left by the author.</summary>
    public string? Comment { get; set; }

    /// <summary>Who caused the change.</summary>
    public Guid? AuthorMemberId { get; set; }

    /// <summary>Who caused the change.</summary>
    public FamilyMember? Author { get; set; }

    /// <summary>UTC moment of the entry.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The approved submission behind a task entry.</summary>
    public Guid? SubmissionId { get; set; }

    /// <summary>The purchase behind a purchase or refund entry.</summary>
    public Guid? PurchaseId { get; set; }

    /// <summary>The catalog penalty behind a penalty entry.</summary>
    public Guid? PenaltyId { get; set; }
}

/// <summary>A reward a child bought; the points are paid at once.</summary>
public sealed class Purchase
{
    /// <summary>Purchase id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>The child who bought it.</summary>
    public Guid ChildMemberId { get; set; }

    /// <summary>The child who bought it.</summary>
    public FamilyMember ChildMember { get; set; } = null!;

    /// <summary>The reward bought.</summary>
    public Guid RewardId { get; set; }

    /// <summary>Reward title at the moment of purchase.</summary>
    public string Title { get; set; } = "";

    /// <summary>Points paid.</summary>
    public int Cost { get; set; }

    /// <summary>Lifecycle state.</summary>
    public PurchaseStatus Status { get; set; }

    /// <summary>UTC moment of purchase.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Who handed it over or cancelled it.</summary>
    public Guid? ResolvedByMemberId { get; set; }

    /// <summary>Who handed it over or cancelled it.</summary>
    public FamilyMember? ResolvedBy { get; set; }

    /// <summary>UTC moment it was handed over or cancelled.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Concurrency token, replaced on resolution so a purchase cannot be refunded twice.</summary>
    public Guid Version { get; set; } = Guid.NewGuid();
}
