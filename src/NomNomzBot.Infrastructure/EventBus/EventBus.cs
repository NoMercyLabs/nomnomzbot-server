using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventBus;

/// <summary>
/// IEventBus implementation that resolves handlers from DI,
/// executes them in parallel, and isolates individual handler failures.
/// Registered as a singleton.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBus> _logger;
    private readonly EventLogger _eventLogger;

    public EventBus(
        IServiceProvider serviceProvider,
        ILogger<EventBus> logger,
        EventLogger eventLogger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _eventLogger = eventLogger;
    }

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default
    )
        where TEvent : class, IDomainEvent
    {
        string eventType = typeof(TEvent).Name;
        _logger.LogDebug("Publishing event {EventType} ({EventId})", eventType, @event.EventId);

        _eventLogger.Log(@event);

        // Scope lives for the full duration of handler execution so that scoped
        // services (DbContext, IPipelineEngine, IChatProvider, …) remain valid.
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        List<IEventHandler<TEvent>> handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().ToList();

        if (handlers.Count == 0)
        {
            _logger.LogTrace("No handlers registered for {EventType}", eventType);
            return;
        }

        // Execute all handlers in parallel with failure isolation
        IEnumerable<Task> tasks = handlers.Select(handler => ExecuteHandler(handler, @event, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public void PublishFireAndForget<TEvent>(TEvent @event)
        where TEvent : class, IDomainEvent
    {
        string eventType = typeof(TEvent).Name;
        _logger.LogDebug(
            "Publishing fire-and-forget event {EventType} ({EventId})",
            eventType,
            @event.EventId
        );

        _eventLogger.Log(@event);

        // Use Task.Run to ensure execution happens on the thread pool,
        // completely detached from the caller's context
        _ = Task.Run(async () =>
        {
            await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
            List<IEventHandler<TEvent>> handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().ToList();

            if (handlers.Count == 0)
                return;

            IEnumerable<Task> tasks = handlers.Select(handler =>
                ExecuteHandler(handler, @event, CancellationToken.None)
            );
            await Task.WhenAll(tasks);
        });
    }

    private async Task ExecuteHandler<TEvent>(
        IEventHandler<TEvent> handler,
        TEvent @event,
        CancellationToken cancellationToken
    )
        where TEvent : class, IDomainEvent
    {
        string handlerName = handler.GetType().Name;
        try
        {
            _logger.LogTrace(
                "Executing handler {Handler} for {EventType}",
                handlerName,
                typeof(TEvent).Name
            );

            await handler.HandleAsync(@event, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Handler {Handler} cancelled for {EventType}",
                handlerName,
                typeof(TEvent).Name
            );
        }
        catch (Exception ex)
        {
            // Critical: one handler's failure must NOT affect other handlers
            _logger.LogError(
                ex,
                "Handler {Handler} failed for event {EventType} ({EventId})",
                handlerName,
                typeof(TEvent).Name,
                @event.EventId
            );
        }
    }
}
