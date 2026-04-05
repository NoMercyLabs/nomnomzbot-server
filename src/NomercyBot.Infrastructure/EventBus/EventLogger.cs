using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Events;

namespace NoMercyBot.Infrastructure.EventBus;

/// <summary>
/// Structured logging for domain events. Logs event metadata for debugging and tracing.
/// Registered as a singleton.
/// </summary>
public sealed class EventLogger
{
    private readonly ILogger<EventLogger> _logger;

    public EventLogger(ILogger<EventLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs a domain event with structured properties for correlation and filtering.
    /// </summary>
    public void Log<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        _logger.LogInformation(
            "DomainEvent {EventType} published. EventId={EventId}, BroadcasterId={BroadcasterId}, Timestamp={Timestamp}",
            typeof(TEvent).Name,
            @event.EventId,
            @event.BroadcasterId ?? "(platform)",
            @event.Timestamp);
    }
}
