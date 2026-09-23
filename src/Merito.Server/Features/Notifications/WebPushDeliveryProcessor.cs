using System.Text.Json;
using Merito.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Features.Notifications;

/// <summary>Processes durable Web Push deliveries in bounded batches.</summary>
public sealed class WebPushDeliveryProcessor(MeritoDbContext db, IWebPushSender sender, TimeProvider clock)
{
    internal const int MaxAttempts = 5;
    private const int BatchSize = 25;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6),
    ];

    /// <summary>Processes one batch and returns the number of claimed deliveries.</summary>
    public async Task<int> ProcessBatchAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var deliveries = await db.WebPushDeliveries
            .Include(d => d.Notification)
            .Include(d => d.Subscription)
            .Where(d => d.SentAt == null && d.AttemptCount < MaxAttempts && d.NextAttemptAt <= now)
            .OrderBy(d => d.NextAttemptAt)
            .Take(BatchSize)
            .ToListAsync(ct);
        if (deliveries.Count == 0) return 0;

        foreach (var delivery in deliveries) delivery.NextAttemptAt = now + LeaseDuration;
        await db.SaveChangesAsync(ct);

        foreach (var delivery in deliveries)
        {
            var payload = JsonSerializer.Serialize(new PushPayload(
                delivery.Notification.Title,
                delivery.Notification.Message,
                "/notifications",
                $"merito-notification-{delivery.NotificationId:N}"), JsonSerializerOptions.Web);
            try
            {
                await sender.SendAsync(delivery.Subscription, payload, ct);
                delivery.AttemptCount++;
                delivery.SentAt = clock.GetUtcNow().UtcDateTime;
                delivery.LastError = null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (WebPushSendException ex) when (ex.SubscriptionExpired)
            {
                db.WebPushSubscriptions.Remove(delivery.Subscription);
            }
            catch (Exception ex)
            {
                delivery.AttemptCount++;
                delivery.LastError = Truncate(ex.Message, 2048);
                delivery.NextAttemptAt = now + Backoff[Math.Min(delivery.AttemptCount - 1, Backoff.Length - 1)];
            }

            await db.SaveChangesAsync(ct);
        }

        return deliveries.Count;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record PushPayload(string Title, string Body, string Url, string Tag);
}
