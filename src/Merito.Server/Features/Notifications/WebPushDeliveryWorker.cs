using Microsoft.Extensions.Options;

namespace Merito.Server.Features.Notifications;

/// <summary>Periodically drains the durable Web Push delivery queue.</summary>
public sealed class WebPushDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WebPushOptions> options,
    ILogger<WebPushDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (IsConfigured(options.CurrentValue))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<WebPushDeliveryProcessor>()
                        .ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Web Push delivery cycle failed.");
                }
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal static bool IsConfigured(WebPushOptions options) =>
        !string.IsNullOrWhiteSpace(options.PublicKey)
        && !string.IsNullOrWhiteSpace(options.PrivateKey)
        && Uri.TryCreate(options.Subject, UriKind.Absolute, out var subject)
        && subject.Scheme is "mailto" or "https";
}
