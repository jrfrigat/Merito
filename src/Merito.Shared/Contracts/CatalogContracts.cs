namespace Merito.Shared.Contracts;

/// <summary>A task from the family catalog.</summary>
/// <param name="Id">Task id.</param>
/// <param name="Title">What has to be done.</param>
/// <param name="Description">Optional details.</param>
/// <param name="Points">Points suggested on approval (the lower bound of a range).</param>
/// <param name="MaxPoints">Upper bound when the task is worth a range such as 3-5; null otherwise.</param>
/// <param name="Category">Daily routine or extra work.</param>
/// <param name="TimeOfDay">Optional hint such as "утро" or "весь день".</param>
public sealed record TaskDto(Guid Id, string Title, string? Description, int Points, int? MaxPoints, TaskCategory Category, string? TimeOfDay);

/// <summary>Creates or updates a catalog task.</summary>
/// <param name="Title">What has to be done.</param>
/// <param name="Description">Optional details.</param>
/// <param name="Points">Points suggested on approval, at least 1.</param>
/// <param name="MaxPoints">Optional upper bound, greater than <paramref name="Points"/>.</param>
/// <param name="Category">Daily routine or extra work.</param>
/// <param name="TimeOfDay">Optional hint such as "утро".</param>
public sealed record TaskRequest(string Title, string? Description, int Points, int? MaxPoints, TaskCategory Category, string? TimeOfDay);

/// <summary>A penalty from the family catalog.</summary>
/// <param name="Id">Penalty id.</param>
/// <param name="Title">The violation.</param>
/// <param name="Points">Points deducted, a positive number.</param>
public sealed record PenaltyDto(Guid Id, string Title, int Points);

/// <summary>Creates or updates a penalty.</summary>
/// <param name="Title">The violation.</param>
/// <param name="Points">Points deducted, at least 1.</param>
public sealed record PenaltyRequest(string Title, int Points);

/// <summary>A reward in the family shop.</summary>
/// <param name="Id">Reward id.</param>
/// <param name="Title">What the child gets.</param>
/// <param name="Description">Optional details or conditions.</param>
/// <param name="Cost">Price in points.</param>
public sealed record RewardDto(Guid Id, string Title, string? Description, int Cost);

/// <summary>Creates or updates a reward.</summary>
/// <param name="Title">What the child gets.</param>
/// <param name="Description">Optional details or conditions.</param>
/// <param name="Cost">Price in points, at least 1.</param>
public sealed record RewardRequest(string Title, string? Description, int Cost);
