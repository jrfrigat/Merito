namespace Merito.Shared.Contracts;

/// <summary>Creates a family whose creator becomes its first parent.</summary>
/// <param name="Name">Family name.</param>
/// <param name="SeedExample">Fills the tasks, penalties and shop with the example catalog.</param>
public sealed record CreateFamilyRequest(string Name, bool SeedExample);

/// <summary>Renames a family.</summary>
/// <param name="Name">New family name.</param>
public sealed record RenameFamilyRequest(string Name);

/// <summary>Joins a family with an invite code; the role comes from the invite.</summary>
/// <param name="Code">Invite code as shown to the parent who created it.</param>
public sealed record JoinFamilyRequest(string Code);

/// <summary>Creates an invite code for a role.</summary>
/// <param name="Role">Role the person joining with this code receives.</param>
public sealed record CreateInviteRequest(FamilyRole Role);

/// <summary>An invite code a parent can pass on.</summary>
/// <param name="Code">The code to type on the join screen.</param>
/// <param name="Role">Role the person joining receives.</param>
/// <param name="ExpiresAt">UTC moment after which the code no longer works.</param>
public sealed record InviteDto(string Code, FamilyRole Role, DateTime ExpiresAt);

/// <summary>Creates a child account with a login and puts it into the family.</summary>
/// <param name="DisplayName">Name shown to the family.</param>
/// <param name="Login">Login the child signs in with (latin letters, digits, dot, dash, underscore).</param>
/// <param name="Password">Initial password.</param>
public sealed record CreateChildRequest(string DisplayName, string Login, string Password);

/// <summary>Sets a new password for a child account of the family.</summary>
/// <param name="Password">The new password.</param>
public sealed record ResetPasswordRequest(string Password);

/// <summary>A family with its active members.</summary>
/// <param name="Id">Family id.</param>
/// <param name="Name">Family name.</param>
/// <param name="Members">Parents first, then children, each group by name.</param>
public sealed record FamilyDto(Guid Id, string Name, IReadOnlyList<MemberDto> Members);

/// <summary>A person in a family.</summary>
/// <param name="Id">Member id.</param>
/// <param name="DisplayName">Name shown to the family.</param>
/// <param name="Role">Role in the family.</param>
/// <param name="Balance">Point balance of a child; null for parents and for other children when a child asks.</param>
/// <param name="Login">Login of a child account, shown to parents only; null otherwise.</param>
public sealed record MemberDto(Guid Id, string DisplayName, FamilyRole Role, int? Balance, string? Login);

/// <summary>What the home screen needs in one request.</summary>
/// <param name="Family">The family and its members.</param>
/// <param name="Me">The caller's own membership.</param>
/// <param name="PendingSubmissions">Submissions awaiting review (a child sees only their own).</param>
/// <param name="PendingPurchases">Purchases not handed over yet (a child sees only their own).</param>
/// <param name="RecentTransactions">Latest ledger entries (a child sees only their own).</param>
public sealed record DashboardDto(
    FamilyDto Family,
    MemberDto Me,
    int PendingSubmissions,
    int PendingPurchases,
    IReadOnlyList<TransactionDto> RecentTransactions);
