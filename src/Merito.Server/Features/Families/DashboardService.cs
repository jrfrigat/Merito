using Merito.Server.Data;
using Merito.Server.Features.Points;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Families;

/// <summary>Assembles the home screen in one request: members, pending counters and the latest ledger entries.</summary>
public sealed class DashboardService(MeritoDbContext db, FamilyService families, PointsService points)
{
    /// <summary>How many ledger entries the home screen shows.</summary>
    public const int RecentCount = 5;

    /// <summary>The home screen for the caller; a child's counters and entries are their own.</summary>
    public async Task<DashboardDto> GetAsync(FamilyMember caller, CancellationToken ct = default)
    {
        var family = await families.GetAsync(caller, ct);
        var me = family.Members.First(m => m.Id == caller.Id);
        var isChild = caller.Role == FamilyRole.Child;

        var pendingSubmissions = await db.Submissions.CountAsync(s => s.FamilyId == caller.FamilyId
            && s.Status == SubmissionStatus.Pending && (!isChild || s.ChildMemberId == caller.Id), ct);
        var pendingPurchases = await db.Purchases.CountAsync(p => p.FamilyId == caller.FamilyId
            && p.Status == PurchaseStatus.Pending && (!isChild || p.ChildMemberId == caller.Id), ct);
        var recent = await points.ListAsync(caller, null, RecentCount, ct);

        return new DashboardDto(family, me, pendingSubmissions, pendingPurchases, recent);
    }
}
