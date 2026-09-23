using Merito.Shared.Contracts;
using Microsoft.JSInterop;

namespace Merito.Client.Services;

/// <summary>Keeps the in-app unread count current and mirrors new events to browser notifications.</summary>
public sealed class NotificationCenter(ApiClient api, IJSRuntime js) : IAsyncDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly HashSet<Guid> _seenIds = [];
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private Guid? _familyId;
    private bool _hasBaseline;

    public event Action? Changed;

    public int UnreadCount { get; private set; }
    public string BrowserPermission { get; private set; } = "unknown";

    public async Task StartAsync(Guid familyId)
    {
        if (_familyId == familyId && _loopTask is { IsCompleted: false }) return;

        await StopAsync();
        _familyId = familyId;
        _seenIds.Clear();
        _hasBaseline = false;
        SetUnreadCount(0);
        await RefreshPermissionAsync();

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

        _seenIds.Clear();
        _hasBaseline = false;
        SetUnreadCount(0);
    }

    public async Task RefreshAsync()
    {
        if (_familyId is not { } familyId) return;

        try
        {
            var notifications = await api.GetNotificationsAsync(familyId);
            var count = await api.GetUnreadNotificationCountAsync(familyId);
            if (_familyId != familyId) return;
            SetUnreadCount(count.Count);

            if (_hasBaseline)
            {
                foreach (var item in notifications.Where(x => x.ReadAt is null && !_seenIds.Contains(x.Id)).Reverse())
                    await ShowBrowserNotificationAsync(item);
            }

            foreach (var item in notifications) _seenIds.Add(item.Id);
            _hasBaseline = true;
        }
        catch (ApiException)
        {
            // A later poll retries; a transient outage must not break the application shell.
        }
    }

    public async Task<string> EnableBrowserNotificationsAsync()
    {
        try
        {
            BrowserPermission = await js.InvokeAsync<string>("meritoNotifications.requestPermission");
        }
        catch (JSException)
        {
            BrowserPermission = "unsupported";
        }

        Changed?.Invoke();
        return BrowserPermission;
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

    private async Task RefreshPermissionAsync()
    {
        try
        {
            BrowserPermission = await js.InvokeAsync<string>("meritoNotifications.getPermission");
        }
        catch (JSException)
        {
            BrowserPermission = "unsupported";
        }
        Changed?.Invoke();
    }

    private async Task ShowBrowserNotificationAsync(NotificationDto item)
    {
        if (BrowserPermission != "granted") return;

        try
        {
            await js.InvokeVoidAsync("meritoNotifications.show", item.Title, item.Message);
        }
        catch (JSException)
        {
            BrowserPermission = "unsupported";
            Changed?.Invoke();
        }
    }

    private void SetUnreadCount(int count)
    {
        if (UnreadCount == count) return;
        UnreadCount = count;
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
