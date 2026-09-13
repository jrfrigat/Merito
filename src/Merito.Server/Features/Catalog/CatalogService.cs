using Merito.Server.Data;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Catalog;

/// <summary>Reads and edits the family's tasks, penalties and shop rewards. Deleting archives, so history keeps its references.</summary>
public sealed class CatalogService(MeritoDbContext db, TimeProvider clock)
{
    /// <summary>Active tasks: daily first, then extra, each in catalog order.</summary>
    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(Guid familyId, CancellationToken ct = default)
    {
        var tasks = await db.Tasks.AsNoTracking()
            .Where(t => t.FamilyId == familyId && !t.IsArchived)
            .ToListAsync(ct);
        return tasks
            .OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ThenBy(t => t.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    /// <summary>Adds a task at the end of its category.</summary>
    public async Task<TaskDto> AddTaskAsync(Guid familyId, TaskRequest request, CancellationToken ct = default)
    {
        var task = new FamilyTask { Id = Guid.NewGuid(), FamilyId = familyId, CreatedAt = clock.GetUtcNow().UtcDateTime };
        Apply(task, request);
        task.SortOrder = await NextTaskOrderAsync(familyId, task.Category, ct);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return ToDto(task);
    }

    /// <summary>Updates a task; moving it to another category puts it at the end there.</summary>
    public async Task<TaskDto> UpdateTaskAsync(Guid familyId, Guid taskId, TaskRequest request, CancellationToken ct = default)
    {
        var task = await FindTaskAsync(familyId, taskId, ct);
        var category = task.Category;
        Apply(task, request);
        if (task.Category != category) task.SortOrder = await NextTaskOrderAsync(familyId, task.Category, ct);
        await db.SaveChangesAsync(ct);
        return ToDto(task);
    }

    /// <summary>Archives a task.</summary>
    public async Task ArchiveTaskAsync(Guid familyId, Guid taskId, CancellationToken ct = default)
    {
        (await FindTaskAsync(familyId, taskId, ct)).IsArchived = true;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Active penalties in catalog order.</summary>
    public async Task<IReadOnlyList<PenaltyDto>> GetPenaltiesAsync(Guid familyId, CancellationToken ct = default) =>
        await db.Penalties.AsNoTracking()
            .Where(p => p.FamilyId == familyId && !p.IsArchived)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.CreatedAt)
            .Select(p => new PenaltyDto(p.Id, p.Title, p.Points))
            .ToListAsync(ct);

    /// <summary>Adds a penalty at the end of the list.</summary>
    public async Task<PenaltyDto> AddPenaltyAsync(Guid familyId, PenaltyRequest request, CancellationToken ct = default)
    {
        var penalty = new Penalty
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Title = Guard.Required(request.Title, Limits.TitleMaxLength, "Нарушение"),
            Points = Guard.Points(request.Points, "Баллы"),
            SortOrder = await db.Penalties.Where(p => p.FamilyId == familyId).Select(p => (int?)p.SortOrder).MaxAsync(ct) + 1 ?? 0,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.Penalties.Add(penalty);
        await db.SaveChangesAsync(ct);
        return new PenaltyDto(penalty.Id, penalty.Title, penalty.Points);
    }

    /// <summary>Updates a penalty.</summary>
    public async Task<PenaltyDto> UpdatePenaltyAsync(Guid familyId, Guid penaltyId, PenaltyRequest request, CancellationToken ct = default)
    {
        var penalty = await FindPenaltyAsync(familyId, penaltyId, ct);
        penalty.Title = Guard.Required(request.Title, Limits.TitleMaxLength, "Нарушение");
        penalty.Points = Guard.Points(request.Points, "Баллы");
        await db.SaveChangesAsync(ct);
        return new PenaltyDto(penalty.Id, penalty.Title, penalty.Points);
    }

    /// <summary>Archives a penalty.</summary>
    public async Task ArchivePenaltyAsync(Guid familyId, Guid penaltyId, CancellationToken ct = default)
    {
        (await FindPenaltyAsync(familyId, penaltyId, ct)).IsArchived = true;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Active rewards in shop order.</summary>
    public async Task<IReadOnlyList<RewardDto>> GetRewardsAsync(Guid familyId, CancellationToken ct = default) =>
        await db.Rewards.AsNoTracking()
            .Where(r => r.FamilyId == familyId && !r.IsArchived)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.CreatedAt)
            .Select(r => new RewardDto(r.Id, r.Title, r.Description, r.Cost))
            .ToListAsync(ct);

    /// <summary>Adds a reward at the end of the shop.</summary>
    public async Task<RewardDto> AddRewardAsync(Guid familyId, RewardRequest request, CancellationToken ct = default)
    {
        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            SortOrder = await db.Rewards.Where(r => r.FamilyId == familyId).Select(r => (int?)r.SortOrder).MaxAsync(ct) + 1 ?? 0,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        Apply(reward, request);
        db.Rewards.Add(reward);
        await db.SaveChangesAsync(ct);
        return new RewardDto(reward.Id, reward.Title, reward.Description, reward.Cost);
    }

    /// <summary>Updates a reward; purchases already made keep the title and price they were bought at.</summary>
    public async Task<RewardDto> UpdateRewardAsync(Guid familyId, Guid rewardId, RewardRequest request, CancellationToken ct = default)
    {
        var reward = await FindRewardAsync(familyId, rewardId, ct);
        Apply(reward, request);
        await db.SaveChangesAsync(ct);
        return new RewardDto(reward.Id, reward.Title, reward.Description, reward.Cost);
    }

    /// <summary>Archives a reward.</summary>
    public async Task ArchiveRewardAsync(Guid familyId, Guid rewardId, CancellationToken ct = default)
    {
        (await FindRewardAsync(familyId, rewardId, ct)).IsArchived = true;
        await db.SaveChangesAsync(ct);
    }

    internal static TaskDto ToDto(FamilyTask t) =>
        new(t.Id, t.Title, t.Description, t.Points, t.MaxPoints, t.Category, t.TimeOfDay);

    private static void Apply(FamilyTask task, TaskRequest request)
    {
        task.Title = Guard.Required(request.Title, Limits.TitleMaxLength, "Задание");
        task.Description = Guard.Optional(request.Description, Limits.TextMaxLength, "Описание");
        task.Points = Guard.Points(request.Points, "Баллы");
        task.MaxPoints = request.MaxPoints is { } max ? Guard.Points(max, "Баллы до") : null;
        if (task.MaxPoints <= task.Points)
            throw DomainException.Invalid("Верхняя граница баллов должна быть больше нижней.");
        task.Category = Guard.Defined(request.Category, "Раздел");
        task.TimeOfDay = Guard.Optional(request.TimeOfDay, Limits.TimeOfDayMaxLength, "Когда");
    }

    private static void Apply(Reward reward, RewardRequest request)
    {
        reward.Title = Guard.Required(request.Title, Limits.TitleMaxLength, "Награда");
        reward.Description = Guard.Optional(request.Description, Limits.TextMaxLength, "Описание");
        reward.Cost = Guard.Points(request.Cost, "Цена");
    }

    private async Task<int> NextTaskOrderAsync(Guid familyId, TaskCategory category, CancellationToken ct) =>
        await db.Tasks.Where(t => t.FamilyId == familyId && t.Category == category).Select(t => (int?)t.SortOrder).MaxAsync(ct) + 1 ?? 0;

    private async Task<FamilyTask> FindTaskAsync(Guid familyId, Guid id, CancellationToken ct) =>
        await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.FamilyId == familyId && !t.IsArchived, ct)
        ?? throw DomainException.NotFound("Задание не найдено.");

    private async Task<Penalty> FindPenaltyAsync(Guid familyId, Guid id, CancellationToken ct) =>
        await db.Penalties.FirstOrDefaultAsync(p => p.Id == id && p.FamilyId == familyId && !p.IsArchived, ct)
        ?? throw DomainException.NotFound("Штраф не найден.");

    private async Task<Reward> FindRewardAsync(Guid familyId, Guid id, CancellationToken ct) =>
        await db.Rewards.FirstOrDefaultAsync(r => r.Id == id && r.FamilyId == familyId && !r.IsArchived, ct)
        ?? throw DomainException.NotFound("Награда не найдена.");
}
