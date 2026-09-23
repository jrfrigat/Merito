using Merito.Server.Data;
using Merito.Server.Features.Notifications;
using Merito.Server.Features.Points;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Submissions;

/// <summary>Children report done work; parents approve it with points or reject it.</summary>
public sealed class SubmissionService(MeritoDbContext db, LedgerService ledger, NotificationService notifications, TimeProvider clock)
{
    /// <summary>The largest page a list request returns.</summary>
    public const int MaxTake = 200;

    /// <summary>Submissions newest first; a child always gets only their own.</summary>
    public async Task<IReadOnlyList<SubmissionDto>> ListAsync(
        FamilyMember caller, SubmissionStatus? status, Guid? childId, int take, CancellationToken ct = default)
    {
        var query = db.Submissions.AsNoTracking().Where(s => s.FamilyId == caller.FamilyId);
        if (caller.Role == FamilyRole.Child) query = query.Where(s => s.ChildMemberId == caller.Id);
        else if (childId is { } child) query = query.Where(s => s.ChildMemberId == child);
        if (status is { } st) query = query.Where(s => s.Status == st);

        return await query
            .OrderByDescending(s => s.SubmittedAt)
            .Take(Math.Clamp(take, 1, MaxTake))
            .Select(s => new SubmissionDto(
                s.Id, s.ChildMemberId, s.ChildMember.User.DisplayName, s.TaskId, s.Title, s.ChildComment,
                s.Task == null ? null : s.Task.Points,
                s.Task == null ? null : s.Task.MaxPoints,
                s.Status, s.SubmittedAt, s.ReviewedAt,
                s.ReviewedBy == null ? null : s.ReviewedBy.User.DisplayName,
                s.AwardedPoints, s.ReviewComment))
            .ToListAsync(ct);
    }

    /// <summary>A child reports a catalog task or custom work as done; it waits for a parent.</summary>
    public async Task<SubmissionDto> SubmitAsync(FamilyMember child, SubmitRequest request, CancellationToken ct = default)
    {
        var comment = Guard.Optional(request.Comment, Limits.TextMaxLength, "Комментарий");
        FamilyTask? task = null;
        string title;
        if (request.TaskId is { } taskId)
        {
            task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId && t.FamilyId == child.FamilyId && !t.IsArchived, ct)
                ?? throw DomainException.NotFound("Задание не найдено.");
            title = task.Title;
        }
        else
        {
            title = Guard.Required(request.Title, Limits.TitleMaxLength, "Что сделано");
        }

        var submission = new TaskSubmission
        {
            Id = Guid.NewGuid(),
            FamilyId = child.FamilyId,
            ChildMemberId = child.Id,
            TaskId = task?.Id,
            Title = title,
            ChildComment = comment,
            Status = SubmissionStatus.Pending,
            SubmittedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync(ct);

        return new SubmissionDto(submission.Id, child.Id, child.User.DisplayName, task?.Id, title, comment,
            task?.Points, task?.MaxPoints, submission.Status, submission.SubmittedAt, null, null, null, null);
    }

    /// <summary>A child withdraws their own report while it is still pending.</summary>
    public async Task WithdrawAsync(FamilyMember child, Guid submissionId, CancellationToken ct = default)
    {
        var submission = await db.Submissions
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.FamilyId == child.FamilyId && s.ChildMemberId == child.Id, ct)
            ?? throw DomainException.NotFound("Отметка не найдена.");
        if (submission.Status != SubmissionStatus.Pending)
            throw DomainException.Conflict("Родитель уже проверил эту отметку.");

        db.Submissions.Remove(submission);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// A parent approves a pending report and credits the points; custom work can be saved into the
    /// catalog in the same step, so it is in the list next time.
    /// </summary>
    public async Task ApproveAsync(FamilyMember parent, Guid submissionId, ApproveRequest request, CancellationToken ct = default)
    {
        var points = Guard.Points(request.Points, "Баллы");
        var comment = Guard.Optional(request.Comment, Limits.TextMaxLength, "Комментарий");
        var submission = await FindPendingAsync(parent, submissionId, ct);
        var now = clock.GetUtcNow().UtcDateTime;

        if (request.SaveAsTask is { } category && submission.TaskId is null)
        {
            Guard.Defined(category, "Раздел");
            var order = await db.Tasks.Where(t => t.FamilyId == parent.FamilyId && t.Category == category)
                .Select(t => (int?)t.SortOrder).MaxAsync(ct) + 1 ?? 0;
            var task = new FamilyTask
            {
                Id = Guid.NewGuid(), FamilyId = parent.FamilyId, Title = submission.Title, Points = points,
                Category = category, SortOrder = order, CreatedAt = now,
            };
            db.Tasks.Add(task);
            submission.TaskId = task.Id;
        }

        submission.Status = SubmissionStatus.Approved;
        submission.AwardedPoints = points;
        submission.ReviewComment = comment;
        submission.ReviewedAt = now;
        submission.ReviewedByMemberId = parent.Id;
        submission.Version = Guid.NewGuid();

        await ledger.PostAsync(submission.ChildMember, points, TransactionKind.Task, submission.Title, comment, parent,
            submissionId: submission.Id, ct: ct);
        var approvedMessage = $"{submission.Title}: +{points} баллов.";
        if (!string.IsNullOrWhiteSpace(comment)) approvedMessage += $" {comment}";
        await notifications.AddAsync(submission.ChildMember, NotificationKind.SubmissionApproved, "Дело засчитано", approvedMessage, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>A parent rejects a pending report; no points move.</summary>
    public async Task RejectAsync(FamilyMember parent, Guid submissionId, RejectRequest request, CancellationToken ct = default)
    {
        var comment = Guard.Optional(request.Comment, Limits.TextMaxLength, "Комментарий");
        var submission = await FindPendingAsync(parent, submissionId, ct);

        submission.Status = SubmissionStatus.Rejected;
        submission.ReviewComment = comment;
        submission.ReviewedAt = clock.GetUtcNow().UtcDateTime;
        submission.ReviewedByMemberId = parent.Id;
        submission.Version = Guid.NewGuid();
        var rejectedMessage = submission.Title + ".";
        if (!string.IsNullOrWhiteSpace(comment)) rejectedMessage += $" {comment}";
        await notifications.AddAsync(submission.ChildMember, NotificationKind.SubmissionRejected, "Дело не засчитано", rejectedMessage, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<TaskSubmission> FindPendingAsync(FamilyMember parent, Guid submissionId, CancellationToken ct)
    {
        var submission = await db.Submissions
            .Include(s => s.ChildMember)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.FamilyId == parent.FamilyId, ct)
            ?? throw DomainException.NotFound("Отметка не найдена.");
        if (submission.Status != SubmissionStatus.Pending)
            throw DomainException.Conflict("Эта отметка уже проверена.");
        if (!submission.ChildMember.IsActive)
            throw DomainException.Conflict("Ребенок больше не состоит в семье.");
        return submission;
    }
}
