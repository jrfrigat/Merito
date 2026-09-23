namespace Merito.Server.Features.Notifications;

/// <summary>VAPID credentials loaded from server configuration.</summary>
public sealed class WebPushOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "WebPush";

    /// <summary>URL-safe Base64 public key exposed to authenticated clients.</summary>
    public string PublicKey { get; set; } = "";

    /// <summary>Private VAPID key used only by the delivery worker.</summary>
    public string PrivateKey { get; set; } = "";

    /// <summary>Contact URI included in VAPID claims.</summary>
    public string Subject { get; set; } = "";
}
