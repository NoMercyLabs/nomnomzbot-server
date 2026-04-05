namespace NoMercyBot.Application.Contracts.Notifications;

/// <summary>
/// Abstraction over real-time push notifications to connected clients (SignalR).
/// Used to push events to dashboards, overlays, and widgets.
/// </summary>
public interface IRealTimeNotifier
{
    /// <summary>Send a message to all clients connected to a specific channel.</summary>
    Task SendToChannelAsync(
        string broadcasterId,
        string eventType,
        object payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>Send a message to a specific connected user.</summary>
    Task SendToUserAsync(
        string userId,
        string eventType,
        object payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>Send a message to all connected clients.</summary>
    Task SendToAllAsync(
        string eventType,
        object payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>Send a message to a specific overlay/widget connection group.</summary>
    Task SendToGroupAsync(
        string groupName,
        string eventType,
        object payload,
        CancellationToken cancellationToken = default
    );
}
