using Merito.Shared;

namespace Merito.Server.Data;

/// <summary>A family: the scope of every catalog, submission, purchase and ledger entry.</summary>
public sealed class Family
{
    /// <summary>Family id.</summary>
    public Guid Id { get; set; }

    /// <summary>Family name.</summary>
    public string Name { get; set; } = "";

    /// <summary>UTC moment the family was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Everyone who ever joined, including removed members kept for the history.</summary>
    public List<FamilyMember> Members { get; set; } = [];
}

/// <summary>A person's membership in a family, holding their role and, for a child, the point balance.</summary>
public sealed class FamilyMember
{
    /// <summary>Member id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>The family.</summary>
    public Family Family { get; set; } = null!;

    /// <summary>The account.</summary>
    public Guid UserId { get; set; }

    /// <summary>The account.</summary>
    public AppUser User { get; set; } = null!;

    /// <summary>Role in the family.</summary>
    public FamilyRole Role { get; set; }

    /// <summary>Current point balance; only <see cref="Features.Points.LedgerService"/> changes it.</summary>
    public int Balance { get; set; }

    /// <summary>Concurrency token, replaced on every balance change so two parallel spends cannot both succeed.</summary>
    public Guid Version { get; set; } = Guid.NewGuid();

    /// <summary>False once a parent removed the member; the history stays.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC moment the member joined.</summary>
    public DateTime JoinedAt { get; set; }
}

/// <summary>A reusable code that lets someone join a family in a given role until it expires.</summary>
public sealed class FamilyInvite
{
    /// <summary>Invite id.</summary>
    public Guid Id { get; set; }

    /// <summary>The family.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>The family.</summary>
    public Family Family { get; set; } = null!;

    /// <summary>The code, upper case, unique across all families.</summary>
    public string Code { get; set; } = "";

    /// <summary>Role given to whoever joins with the code.</summary>
    public FamilyRole Role { get; set; }

    /// <summary>UTC moment after which the code no longer works.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Parent who created the code.</summary>
    public Guid CreatedByMemberId { get; set; }
}
