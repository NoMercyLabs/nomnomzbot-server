namespace NoMercyBot.Application.Contracts.Notifications;

/// <summary>
/// Dispatches outbound webhook calls to external URLs.
/// Used for custom integrations and event forwarding.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>Dispatch a payload to a webhook URL via HTTP POST.</summary>
    Task<WebhookDispatchResult> DispatchAsync(
        string webhookUrl,
        object payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>Dispatch a payload with custom headers (e.g., HMAC signature).</summary>
    Task<WebhookDispatchResult> DispatchAsync(
        string webhookUrl,
        object payload,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default
    );
}

/// <summary>Result of a webhook dispatch attempt.</summary>
public sealed record WebhookDispatchResult(bool Success, int? StatusCode, string? ErrorMessage);
