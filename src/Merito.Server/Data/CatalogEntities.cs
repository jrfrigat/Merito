using Merito.Shared;

namespace Merito.Server.Data;

/// <summary>A task in the family catalog.</summary>
public sealed class FamilyTask
{
    /// <summary>Task id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>What has to be done.</summary>
    public string Title { get; set; } = "";

    /// <summary>Optional details.</summary>
    public string? Description { get; set; }

    /// <summary>Points suggested on approval.</summary>
    public int Points { get; set; }

    /// <summary>Upper bound when the task is worth a range; null otherwise.</summary>
    public int? MaxPoints { get; set; }

    /// <summary>Daily routine or extra work.</summary>
    public TaskCategory Category { get; set; }

    /// <summary>Optional time-of-day hint.</summary>
    public string? TimeOfDay { get; set; }

    /// <summary>Position within its category.</summary>
    public int SortOrder { get; set; }

    /// <summary>Hidden from the catalog; kept so old submissions still point at it.</summary>
    public bool IsArchived { get; set; }

    /// <summary>UTC moment the task was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>A penalty in the family catalog.</summary>
public sealed class Penalty
{
    /// <summary>Penalty id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>The violation.</summary>
    public string Title { get; set; } = "";

    /// <summary>Points deducted, a positive number.</summary>
    public int Points { get; set; }

    /// <summary>Position in the list.</summary>
    public int SortOrder { get; set; }

    /// <summary>Hidden from the catalog; kept for the history.</summary>
    public bool IsArchived { get; set; }

    /// <summary>UTC moment the penalty was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>A reward in the family shop.</summary>
public sealed class Reward
{
    /// <summary>Reward id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>What the child gets.</summary>
    public string Title { get; set; } = "";

    /// <summary>Optional details or conditions.</summary>
    public string? Description { get; set; }

    /// <summary>Price in points.</summary>
    public int Cost { get; set; }

    /// <summary>Position in the shop.</summary>
    public int SortOrder { get; set; }

    /// <summary>Hidden from the shop; kept so old purchases still point at it.</summary>
    public bool IsArchived { get; set; }

    /// <summary>UTC moment the reward was created.</summary>
    public DateTime CreatedAt { get; set; }
}
