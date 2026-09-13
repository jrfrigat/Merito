namespace Merito.Shared.Contracts;

/// <summary>A child reports something as done: a catalog task or a custom title.</summary>
/// <param name="TaskId">Catalog task; null for something that is not in the list.</param>
/// <param name="Title">Title of the custom work; required when <paramref name="TaskId"/> is null.</param>
/// <param name="Comment">Optional note for the parent.</param>
public sealed record SubmitRequest(Guid? TaskId, string? Title, string? Comment);

/// <summary>Something a child reported as done, with its review.</summary>
/// <param name="Id">Submission id.</param>
/// <param name="ChildId">Member id of the child.</param>
/// <param name="ChildName">Name of the child.</param>
/// <param name="TaskId">Catalog task, or null for custom work.</param>
/// <param name="Title">What was done.</param>
/// <param name="ChildComment">The child's note.</param>
/// <param name="SuggestedPoints">Catalog points of the task; null for custom work.</param>
/// <param name="MaxPoints">Upper bound of the task's range; null otherwise.</param>
/// <param name="Status">Review state.</param>
/// <param name="SubmittedAt">UTC moment of the report.</param>
/// <param name="ReviewedAt">UTC moment of the review; null while pending.</param>
/// <param name="ReviewerName">Parent who reviewed it; null while pending.</param>
/// <param name="AwardedPoints">Points credited on approval; null otherwise.</param>
/// <param name="ReviewComment">The parent's note.</param>
public sealed record SubmissionDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    Guid? TaskId,
    string Title,
    string? ChildComment,
    int? SuggestedPoints,
    int? MaxPoints,
    SubmissionStatus Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? ReviewerName,
    int? AwardedPoints,
    string? ReviewComment);

/// <summary>A parent approves a submission.</summary>
/// <param name="Points">Points to credit, at least 1.</param>
/// <param name="Comment">Optional note for the child.</param>
/// <param name="SaveAsTask">For custom work: adds it to the catalog in this category with these points.</param>
public sealed record ApproveRequest(int Points, string? Comment, TaskCategory? SaveAsTask);

/// <summary>A parent rejects a submission.</summary>
/// <param name="Comment">Optional reason for the child.</param>
public sealed record RejectRequest(string? Comment);

/// <summary>A parent grants or deducts points by hand.</summary>
/// <param name="ChildId">Member id of the child.</param>
/// <param name="Amount">Positive to grant, negative to deduct; never zero.</param>
/// <param name="Comment">Why; required.</param>
public sealed record AdjustPointsRequest(Guid ChildId, int Amount, string Comment);

/// <summary>A parent applies a penalty from the catalog.</summary>
/// <param name="ChildId">Member id of the child.</param>
/// <param name="PenaltyId">Catalog penalty.</param>
/// <param name="Comment">Optional details.</param>
public sealed record ApplyPenaltyRequest(Guid ChildId, Guid PenaltyId, string? Comment);

/// <summary>A child's balance after a manual change.</summary>
/// <param name="Balance">The balance right after the change.</param>
public sealed record BalanceDto(int Balance);

/// <summary>A ledger entry that changed a child's balance.</summary>
/// <param name="Id">Entry id.</param>
/// <param name="ChildId">Member id of the child.</param>
/// <param name="ChildName">Name of the child.</param>
/// <param name="Amount">Signed change of the balance.</param>
/// <param name="Kind">Why the balance changed.</param>
/// <param name="Title">What the entry is about.</param>
/// <param name="Comment">Note left by the author.</param>
/// <param name="AuthorName">Who caused the change; null when it was the system.</param>
/// <param name="CreatedAt">UTC moment of the entry.</param>
/// <param name="BalanceAfter">The child's balance right after this entry.</param>
public sealed record TransactionDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    int Amount,
    TransactionKind Kind,
    string Title,
    string? Comment,
    string? AuthorName,
    DateTime CreatedAt,
    int BalanceAfter);

/// <summary>A child buys a reward.</summary>
/// <param name="RewardId">Reward from the shop.</param>
public sealed record PurchaseRequest(Guid RewardId);

/// <summary>A reward a child bought.</summary>
/// <param name="Id">Purchase id.</param>
/// <param name="ChildId">Member id of the child.</param>
/// <param name="ChildName">Name of the child.</param>
/// <param name="RewardId">The reward bought.</param>
/// <param name="Title">Reward title at the moment of purchase.</param>
/// <param name="Cost">Points paid.</param>
/// <param name="Status">Lifecycle state.</param>
/// <param name="CreatedAt">UTC moment of purchase.</param>
/// <param name="ResolvedAt">UTC moment it was handed over or cancelled; null while pending.</param>
/// <param name="ResolverName">Who handed it over or cancelled it; null while pending.</param>
public sealed record PurchaseDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    Guid RewardId,
    string Title,
    int Cost,
    PurchaseStatus Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    string? ResolverName);
