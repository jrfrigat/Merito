using Merito.Server.Data;
using Merito.Server.Features.Catalog;
using Merito.Server.Features.Families;
using Merito.Server.Features.Points;
using Merito.Server.Features.Shop;
using Merito.Server.Features.Submissions;
using Merito.Shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Tests.Support;

/// <summary>A fresh SQLite in-memory database with the domain services wired to it.</summary>
public sealed class TestDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDb(SqliteConnection connection, MeritoDbContext db)
    {
        _connection = connection;
        Db = db;
        Clock = new ManualClock();
        Ledger = new LedgerService(db, Clock);
        Access = new FamilyAccess(db);
        Families = new FamilyService(db, Clock);
        Catalog = new CatalogService(db, Clock);
        Submissions = new SubmissionService(db, Ledger, Clock);
        Points = new PointsService(db, Access, Ledger);
        Shop = new ShopService(db, Ledger, Clock);
    }

    public MeritoDbContext Db { get; }
    public ManualClock Clock { get; }
    public LedgerService Ledger { get; }
    public FamilyAccess Access { get; }
    public FamilyService Families { get; }
    public CatalogService Catalog { get; }
    public SubmissionService Submissions { get; }
    public PointsService Points { get; }
    public ShopService Shop { get; }

    public static async Task<TestDb> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var db = new MeritoDbContext(new DbContextOptionsBuilder<MeritoDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        return new TestDb(connection, db);
    }

    /// <summary>Adds an account and returns its id.</summary>
    public async Task<Guid> AddUserAsync(string name)
    {
        var user = new AppUser { Id = Guid.NewGuid(), UserName = name.ToLowerInvariant(), DisplayName = name, CreatedAt = Clock.GetUtcNow().UtcDateTime };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>A family with one parent and one child, returned as tracked memberships.</summary>
    public async Task<(FamilyMember Parent, FamilyMember Child)> AddFamilyAsync(bool seedExample = false)
    {
        var parentUser = await AddUserAsync("Parent" + Guid.NewGuid().ToString("N")[..6]);
        var membership = await Families.CreateAsync(parentUser, new(Name: "Test", SeedExample: seedExample));
        var childUser = await AddUserAsync("Child" + Guid.NewGuid().ToString("N")[..6]);
        Db.Members.Add(new FamilyMember
        {
            Id = Guid.NewGuid(), FamilyId = membership.FamilyId, UserId = childUser, Role = FamilyRole.Child,
            JoinedAt = Clock.GetUtcNow().UtcDateTime,
        });
        await Db.SaveChangesAsync();

        return (await Access.RequireAsync(membership.FamilyId, parentUser),
                await Access.RequireAsync(membership.FamilyId, childUser));
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

/// <summary>A clock tests move by hand.</summary>
public sealed class ManualClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
