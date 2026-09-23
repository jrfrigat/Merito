using System.Net;
using Merito.Server.Data;
using Microsoft.Extensions.Options;
using WebPush;

namespace Merito.Server.Features.Notifications;

/// <summary>Sends one prepared payload to one browser subscription.</summary>
public interface IWebPushSender
{
    /// <summary>Sends a payload or throws a classified delivery exception.</summary>
    Task SendAsync(WebPushSubscription subscription, string payload, CancellationToken ct);
}

/// <summary>A push failure classified for durable queue processing.</summary>
public sealed class WebPushSendException(bool subscriptionExpired, string message, Exception? inner = null)
    : Exception(message, inner)
{
    /// <summary>Whether the push service says the subscription no longer exists.</summary>
    public bool SubscriptionExpired { get; } = subscriptionExpired;
}

/// <summary>WebPush package adapter using server-side VAPID credentials.</summary>
public sealed class WebPushSender(IOptionsMonitor<WebPushOptions> options) : IWebPushSender, IDisposable
{
    private readonly WebPushClient client = new();

    /// <inheritdoc />
    public async Task SendAsync(WebPushSubscription subscription, string payload, CancellationToken ct)
    {
        var configured = options.CurrentValue;
        var target = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
        var vapid = new VapidDetails(configured.Subject, configured.PublicKey, configured.PrivateKey);
        try
        {
            await client.SendNotificationAsync(target, payload, vapid, ct);
        }
        catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            throw new WebPushSendException(true, ex.Message, ex);
        }
        catch (WebPushException ex)
        {
            throw new WebPushSendException(false, ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}
