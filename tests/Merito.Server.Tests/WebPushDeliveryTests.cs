using Merito.Server.Data;
using Merito.Server.Features.Notifications;
using Merito.Server.Tests.Support;
using Merito.Shared;
using Merito.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Tests;

public sealed class WebPushDeliveryTests
{
    private static readonly WebPushSubscriptionRequest Request = new(
        "https://push.example.test/device", "p256dh-key", "auth-key");

    [Fact]
    public async Task Successful_delivery_is_marked_and_not_sent_again()
    {
        await using var t = await TestDb.CreateAsync();
        var (_, child) = await t.AddFamilyAsync();
        await QueueAsync(t, child);
        var sender = new FakeSender();
        var processor = new WebPushDeliveryProcessor(t.Db, sender, t.Clock);

        Assert.Equal(1, await processor.ProcessBatchAsync());
        Assert.Equal(0, await processor.ProcessBatchAsync());

        var delivery = await t.Db.WebPushDeliveries.SingleAsync();
        Assert.NotNull(delivery.SentAt);
        Assert.Equal(1, delivery.AttemptCount);
        Assert.Single(sender.Payloads);
        Assert.Contains("\"url\":\"/notifications\"", sender.Payloads[0]);
    }

    [Fact]
    public async Task Expired_subscription_and_its_deliveries_are_removed()
    {
        await using var t = await TestDb.CreateAsync();
        var (_, child) = await t.AddFamilyAsync();
        await QueueAsync(t, child);
        var sender = new FakeSender(new WebPushSendException(true, "Gone"));
        var processor = new WebPushDeliveryProcessor(t.Db, sender, t.Clock);

        await processor.ProcessBatchAsync();

        Assert.Empty(await t.Db.WebPushSubscriptions.ToListAsync());
        Assert.Empty(await t.Db.WebPushDeliveries.ToListAsync());
    }

    [Fact]
    public async Task Transient_failure_uses_backoff_and_stops_after_limit()
    {
        await using var t = await TestDb.CreateAsync();
        var (_, child) = await t.AddFamilyAsync();
        await QueueAsync(t, child);
        var sender = new FakeSender(new WebPushSendException(false, "Temporary"));
        var processor = new WebPushDeliveryProcessor(t.Db, sender, t.Clock);

        for (var attempt = 1; attempt <= WebPushDeliveryProcessor.MaxAttempts; attempt++)
        {
            Assert.Equal(1, await processor.ProcessBatchAsync());
            var current = await t.Db.WebPushDeliveries.SingleAsync();
            Assert.Equal(attempt, current.AttemptCount);
            Assert.Equal("Temporary", current.LastError);
            t.Clock.Advance(current.NextAttemptAt - t.Clock.GetUtcNow().UtcDateTime);
        }

        Assert.Equal(0, await processor.ProcessBatchAsync());
        Assert.Equal(WebPushDeliveryProcessor.MaxAttempts, sender.Payloads.Count);
    }

    [Theory]
    [InlineData("", "", "", false)]
    [InlineData("public", "private", "not-a-uri", false)]
    [InlineData("public", "private", "mailto:admin@example.com", true)]
    [InlineData("public", "private", "https://example.com/contact", true)]
    public void Worker_requires_complete_valid_vapid(string publicKey, string privateKey, string subject, bool expected)
    {
        Assert.Equal(expected, WebPushDeliveryWorker.IsConfigured(new WebPushOptions
        {
            PublicKey = publicKey,
            PrivateKey = privateKey,
            Subject = subject,
        }));
    }

    private static async Task QueueAsync(TestDb t, FamilyMember member)
    {
        await t.PushSubscriptions.SubscribeAsync(member, Request);
        await t.Notifications.AddAsync(member, NotificationKind.SubmissionApproved, "Засчитано", "Дело принято");
        await t.Db.SaveChangesAsync();
    }

    private sealed class FakeSender(Exception? exception = null) : IWebPushSender
    {
        public List<string> Payloads { get; } = [];

        public Task SendAsync(WebPushSubscription subscription, string payload, CancellationToken ct)
        {
            Payloads.Add(payload);
            return exception is null ? Task.CompletedTask : Task.FromException(exception);
        }
    }
}
