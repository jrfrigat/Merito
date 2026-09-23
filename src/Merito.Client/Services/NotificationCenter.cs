using System.Text.Json;
using Merito.Shared.Contracts;
using Microsoft.JSInterop;

namespace Merito.Client.Services;

/// <summary>Keeps the unread count current and manages this browser's Web Push subscription.</summary>
public sealed class NotificationCenter(ApiClient api, IJSRuntime js) : IAsyncDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private Guid? _familyId;

    public event Action? Changed;

    public int UnreadCount { get; private set; }
    public string PushState { get; private set; } = "loading";
    public string? PushError { get; private set; }

    public async Task StartAsync(Guid familyId)
    {
        if (_familyId == familyId && _loopTask is { IsCompleted: false }) return;

        await StopAsync();
        _familyId = familyId;
        SetUnreadCount(0);
        await RefreshPushStateAsync();

        var cts = new CancellationTokenSource();
        _loopCts = cts;
        await RefreshAsync();
        _loopTask = PollAsync(familyId, cts.Token);
    }

    public async Task StopAsync()
    {
        var cts = _loopCts;
        var loop = _loopTask;
        _loopCts = null;
        _loopTask = null;
        _familyId = null;

        if (cts is not null)
        {
            await cts.CancelAsync();
            if (loop is not null)
            {
                try { await loop; }
                catch (OperationCanceledException) { }
            }
            cts.Dispose();
        }

        SetUnreadCount(0);
        SetPushState("loading");
    }

    public async Task RefreshAsync()
    {
        if (_familyId is not { } familyId) return;

        try
        {
            var count = await api.GetUnreadNotificationCountAsync(familyId);
            if (_familyId != familyId) return;
            SetUnreadCount(count.Count);
        }
        catch (ApiException)
        {
            // A later poll retries; a transient outage must not break the application shell.
        }
    }

    public async Task RefreshPushStateAsync()
    {
        if (_familyId is not { } familyId) return;

        try
        {
            SetPushState("loading");
            var configuration = await api.GetWebPushPublicKeyAsync(familyId);
            if (_familyId != familyId) return;
            if (string.IsNullOrWhiteSpace(configuration.PublicKey))
            {
                SetPushState("unavailable");
                return;
            }

            var state = await js.InvokeAsync<string>("meritoNotifications.getState");
            if (_familyId != familyId) return;
            if (state == "enabled")
            {
                var request = await ReadSubscriptionAsync();
                if (request is null)
                {
                    SetPushState("disabled");
                    return;
                }

                await api.SubscribeWebPushAsync(familyId, request);
            }

            SetPushState(NormalizeState(state));
        }
        catch (JSException)
        {
            SetPushState("unsupported");
        }
        catch (ApiException e)
        {
            SetPushState("error", e.Message);
        }
        catch (JsonException)
        {
            SetPushState("error", "Браузер вернул некорректные данные подписки.");
        }
    }

    public async Task EnablePushAsync()
    {
        if (_familyId is not { } familyId) return;

        try
        {
            SetPushState("loading");
            var configuration = await api.GetWebPushPublicKeyAsync(familyId);
            if (_familyId != familyId) return;
            if (string.IsNullOrWhiteSpace(configuration.PublicKey))
            {
                SetPushState("unavailable");
                return;
            }

            var json = await js.InvokeAsync<string>("meritoNotifications.subscribe", configuration.PublicKey);
            if (_familyId != familyId) return;
            if (string.IsNullOrWhiteSpace(json))
            {
                SetPushState(NormalizeState(await js.InvokeAsync<string>("meritoNotifications.getState")));
                return;
            }

            var request = DeserializeSubscription(json);
            await api.SubscribeWebPushAsync(familyId, request);
            if (_familyId != familyId) return;
            SetPushState("enabled");
        }
        catch (JSException)
        {
            SetPushState("unsupported");
        }
        catch (ApiException e)
        {
            SetPushState("error", e.Message);
        }
        catch (JsonException)
        {
            SetPushState("error", "Браузер вернул некорректные данные подписки.");
        }
    }

    public async Task DisablePushAsync()
    {
        if (_familyId is not { } familyId) return;

        try
        {
            SetPushState("loading");
            var request = await ReadSubscriptionAsync();
            if (_familyId != familyId) return;
            if (request is not null)
                await api.UnsubscribeWebPushAsync(familyId, new WebPushUnsubscribeRequest(request.Endpoint));

            if (_familyId != familyId) return;
            await js.InvokeAsync<bool>("meritoNotifications.unsubscribe");
            SetPushState("disabled");
        }
        catch (JSException)
        {
            SetPushState("unsupported");
        }
        catch (ApiException e)
        {
            SetPushState("error", e.Message);
        }
        catch (JsonException)
        {
            SetPushState("error", "Браузер вернул некорректные данные подписки.");
        }
    }

    private async Task PollAsync(Guid familyId, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (_familyId != familyId) return;
            await RefreshAsync();
        }
    }

    private async Task<WebPushSubscriptionRequest?> ReadSubscriptionAsync()
    {
        var json = await js.InvokeAsync<string>("meritoNotifications.getSubscription");
        return string.IsNullOrWhiteSpace(json) ? null : DeserializeSubscription(json);
    }

    private static WebPushSubscriptionRequest DeserializeSubscription(string json) =>
        JsonSerializer.Deserialize(json, MeritoJsonContext.Default.WebPushSubscriptionRequest)
        ?? throw new JsonException("Empty Web Push subscription.");

    private static string NormalizeState(string state) => state switch
    {
        "enabled" => "enabled",
        "denied" => "denied",
        "unsupported" => "unsupported",
        _ => "disabled",
    };

    private void SetPushState(string state, string? error = null)
    {
        if (PushState == state && PushError == error) return;
        PushState = state;
        PushError = error;
        Changed?.Invoke();
    }

    private void SetUnreadCount(int count)
    {
        if (UnreadCount == count) return;
        UnreadCount = count;
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
