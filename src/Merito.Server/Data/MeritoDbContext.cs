using Merito.Shared;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Merito.Server.Data;

/// <summary>The application database: identity tables, data protection keys and the family domain.</summary>
public sealed class MeritoDbContext(DbContextOptions<MeritoDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    /// <summary>Families.</summary>
    public DbSet<Family> Families => Set<Family>();

    /// <summary>Family memberships.</summary>
    public DbSet<FamilyMember> Members => Set<FamilyMember>();

    /// <summary>Invite codes.</summary>
    public DbSet<FamilyInvite> Invites => Set<FamilyInvite>();

    /// <summary>Catalog tasks.</summary>
    public DbSet<FamilyTask> Tasks => Set<FamilyTask>();

    /// <summary>Catalog penalties.</summary>
    public DbSet<Penalty> Penalties => Set<Penalty>();

    /// <summary>Shop rewards.</summary>
    public DbSet<Reward> Rewards => Set<Reward>();

    /// <summary>Reports of done work.</summary>
    public DbSet<TaskSubmission> Submissions => Set<TaskSubmission>();

    /// <summary>Ledger entries.</summary>
    public DbSet<PointTransaction> Transactions => Set<PointTransaction>();

    /// <summary>Bought rewards.</summary>
    public DbSet<Purchase> Purchases => Set<Purchase>();

    /// <summary>Durable notifications addressed to family members.</summary>
    public DbSet<AppNotification> Notifications => Set<AppNotification>();

    /// <summary>Data protection key ring, so bearer tokens survive a container restart.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e => e.Property(u => u.DisplayName).HasMaxLength(Limits.NameMaxLength));

        builder.Entity<Family>(e =>
        {
            e.Property(f => f.Name).HasMaxLength(Limits.NameMaxLength);
            e.HasMany(f => f.Members).WithOne(m => m.Family).HasForeignKey(m => m.FamilyId);
        });

        builder.Entity<FamilyMember>(e =>
        {
            e.HasIndex(m => new { m.FamilyId, m.UserId }).IsUnique();
            e.HasIndex(m => m.UserId);
            e.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
            e.Property(m => m.Version).IsConcurrencyToken();
            e.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId);
        });

        builder.Entity<FamilyInvite>(e =>
        {
            e.HasIndex(i => i.Code).IsUnique();
            e.Property(i => i.Code).HasMaxLength(Limits.InviteCodeLength);
            e.Property(i => i.Role).HasConversion<string>().HasMaxLength(16);
            e.HasOne(i => i.Family).WithMany().HasForeignKey(i => i.FamilyId);
        });

        builder.Entity<FamilyTask>(e =>
        {
            e.HasIndex(t => new { t.FamilyId, t.IsArchived });
            e.Property(t => t.Title).HasMaxLength(Limits.TitleMaxLength);
            e.Property(t => t.Description).HasMaxLength(Limits.TextMaxLength);
            e.Property(t => t.TimeOfDay).HasMaxLength(Limits.TimeOfDayMaxLength);
            e.Property(t => t.Category).HasConversion<string>().HasMaxLength(16);
            e.HasOne<Family>().WithMany().HasForeignKey(t => t.FamilyId);
        });

        builder.Entity<Penalty>(e =>
        {
            e.HasIndex(p => new { p.FamilyId, p.IsArchived });
            e.Property(p => p.Title).HasMaxLength(Limits.TitleMaxLength);
            e.HasOne<Family>().WithMany().HasForeignKey(p => p.FamilyId);
        });

        builder.Entity<Reward>(e =>
        {
            e.HasIndex(r => new { r.FamilyId, r.IsArchived });
            e.Property(r => r.Title).HasMaxLength(Limits.TitleMaxLength);
            e.Property(r => r.Description).HasMaxLength(Limits.TextMaxLength);
            e.HasOne<Family>().WithMany().HasForeignKey(r => r.FamilyId);
        });

        builder.Entity<TaskSubmission>(e =>
        {
            e.HasIndex(s => new { s.FamilyId, s.Status, s.SubmittedAt });
            e.HasIndex(s => new { s.ChildMemberId, s.SubmittedAt });
            e.Property(s => s.Title).HasMaxLength(Limits.TitleMaxLength);
            e.Property(s => s.ChildComment).HasMaxLength(Limits.TextMaxLength);
            e.Property(s => s.ReviewComment).HasMaxLength(Limits.TextMaxLength);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(s => s.Version).IsConcurrencyToken();
            e.HasOne<Family>().WithMany().HasForeignKey(s => s.FamilyId);
            e.HasOne(s => s.ChildMember).WithMany().HasForeignKey(s => s.ChildMemberId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.ReviewedBy).WithMany().HasForeignKey(s => s.ReviewedByMemberId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Task).WithMany().HasForeignKey(s => s.TaskId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PointTransaction>(e =>
        {
            e.HasIndex(t => new { t.FamilyId, t.CreatedAt });
            e.HasIndex(t => new { t.ChildMemberId, t.CreatedAt });
            e.Property(t => t.Title).HasMaxLength(Limits.TitleMaxLength);
            e.Property(t => t.Comment).HasMaxLength(Limits.TextMaxLength);
            e.Property(t => t.Kind).HasConversion<string>().HasMaxLength(16);
            e.HasOne<Family>().WithMany().HasForeignKey(t => t.FamilyId);
            e.HasOne(t => t.ChildMember).WithMany().HasForeignKey(t => t.ChildMemberId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Author).WithMany().HasForeignKey(t => t.AuthorMemberId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Purchase>(e =>
        {
            e.HasIndex(p => new { p.FamilyId, p.Status, p.CreatedAt });
            e.HasIndex(p => new { p.ChildMemberId, p.CreatedAt });
            e.Property(p => p.Title).HasMaxLength(Limits.TitleMaxLength);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(p => p.Version).IsConcurrencyToken();
            e.HasOne<Family>().WithMany().HasForeignKey(p => p.FamilyId);
            e.HasOne<Reward>().WithMany().HasForeignKey(p => p.RewardId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.ChildMember).WithMany().HasForeignKey(p => p.ChildMemberId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.ResolvedBy).WithMany().HasForeignKey(p => p.ResolvedByMemberId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AppNotification>(e =>
        {
            e.HasIndex(n => new { n.RecipientMemberId, n.CreatedAt });
            e.Property(n => n.Kind).HasConversion<string>().HasMaxLength(32);
            e.Property(n => n.Title).HasMaxLength(Limits.TitleMaxLength);
            e.Property(n => n.Message).HasMaxLength(Limits.TextMaxLength);
            e.HasOne(n => n.Family).WithMany().HasForeignKey(n => n.FamilyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.Recipient).WithMany().HasForeignKey(n => n.RecipientMemberId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

/// <summary>Builds the context for <c>dotnet ef</c> without a running database or the web host.</summary>
public sealed class MeritoDbContextFactory : IDesignTimeDbContextFactory<MeritoDbContext>
{
    /// <inheritdoc />
    public MeritoDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<MeritoDbContext>()
            .UseNpgsql("Host=localhost;Database=merito_design")
            .Options);
}
