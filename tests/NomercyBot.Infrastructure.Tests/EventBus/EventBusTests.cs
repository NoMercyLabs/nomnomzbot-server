// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Infrastructure.EventBus;
using NSubstitute;
// ReSharper disable InconsistentNaming

using InfraEventBus = NoMercyBot.Infrastructure.EventBus.EventBus;

namespace NomercyBot.Infrastructure.Tests.EventBusTests;

// ─── Test event ──────────────────────────────────────────────────────────────

internal sealed class TestEvent : DomainEventBase
{
    public string Payload { get; init; } = string.Empty;
}

// ─── Failing handler ─────────────────────────────────────────────────────────

internal sealed class FailingHandler : IEventHandler<TestEvent>
{
    public Task HandleAsync(TestEvent @event, CancellationToken ct) =>
        throw new InvalidOperationException("intentional failure");
}

// ─── Tracking handler ────────────────────────────────────────────────────────

internal sealed class TrackingHandler : IEventHandler<TestEvent>
{
    public List<string> Received { get; } = [];

    public Task HandleAsync(TestEvent @event, CancellationToken ct)
    {
        Received.Add(@event.Payload);
        return Task.CompletedTask;
    }
}

// ─── Slow handler ────────────────────────────────────────────────────────────

internal sealed class SlowCancellableHandler : IEventHandler<TestEvent>
{
    public async Task HandleAsync(TestEvent @event, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(5000, ct);
    }
}

// ─── Tests ───────────────────────────────────────────────────────────────────

public class EventBusTests
{
    private static (InfraEventBus bus, TrackingHandler tracker) BuildBus(
        bool addFailingHandler = false
    )
    {
        TrackingHandler tracker = new();
        ServiceCollection services = new();
        services.AddScoped<IEventHandler<TestEvent>>(_ => tracker);

        if (addFailingHandler)
            services.AddScoped<IEventHandler<TestEvent>>(_ => new FailingHandler());

        ServiceProvider sp = services.BuildServiceProvider();
        EventLogger eventLogger = new(NullLogger<EventLogger>.Instance);
        InfraEventBus bus = new(sp, NullLogger<InfraEventBus>.Instance, eventLogger);

        return (bus, tracker);
    }

    [Fact]
    public async Task PublishAsync_SingleHandler_IsInvoked()
    {
        (InfraEventBus bus, TrackingHandler tracker) = BuildBus();

        await bus.PublishAsync(new TestEvent { Payload = "hello" });

        tracker.Received.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public async Task PublishAsync_NoHandlers_DoesNotThrow()
    {
        ServiceCollection services = new();
        ServiceProvider sp = services.BuildServiceProvider();
        EventLogger eventLogger = new(NullLogger<EventLogger>.Instance);
        InfraEventBus bus = new(sp, NullLogger<InfraEventBus>.Instance, eventLogger);

        Func<Task> act = async () => await bus.PublishAsync(new TestEvent { Payload = "nobody home" });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_FailingHandler_DoesNotPropagateException()
    {
        // Failing handler + tracking handler — tracking handler must still receive the event
        TrackingHandler tracker = new();
        ServiceCollection services = new();
        services.AddScoped<IEventHandler<TestEvent>>(_ => new FailingHandler());
        services.AddScoped<IEventHandler<TestEvent>>(_ => tracker);
        ServiceProvider sp = services.BuildServiceProvider();
        EventLogger eventLogger = new(NullLogger<EventLogger>.Instance);
        InfraEventBus bus = new(sp, NullLogger<InfraEventBus>.Instance, eventLogger);

        Func<Task> act = async () => await bus.PublishAsync(new TestEvent { Payload = "resilient" });

        await act.Should().NotThrowAsync();
        tracker.Received.Should().ContainSingle().Which.Should().Be("resilient");
    }

    [Fact]
    public async Task PublishAsync_MultipleHandlers_AllInvoked()
    {
        TrackingHandler tracker1 = new();
        TrackingHandler tracker2 = new();
        ServiceCollection services = new();
        services.AddScoped<IEventHandler<TestEvent>>(_ => tracker1);
        services.AddScoped<IEventHandler<TestEvent>>(_ => tracker2);
        ServiceProvider sp = services.BuildServiceProvider();
        EventLogger eventLogger = new(NullLogger<EventLogger>.Instance);
        InfraEventBus bus = new(sp, NullLogger<InfraEventBus>.Instance, eventLogger);

        await bus.PublishAsync(new TestEvent { Payload = "broadcast" });

        tracker1.Received.Should().ContainSingle();
        tracker2.Received.Should().ContainSingle();
    }

    [Fact]
    public void PublishFireAndForget_DoesNotBlock()
    {
        (InfraEventBus bus, _) = BuildBus();

        // Should return essentially instantly — fire-and-forget
        Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        bus.PublishFireAndForget(new TestEvent { Payload = "async" });
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public async Task PublishAsync_Cancelled_DoesNotPropagateHandlerCancellation()
    {
        // Handler that respects cancellation
        SlowCancellableHandler slowHandler = new();

        ServiceCollection services = new();
        services.AddScoped<IEventHandler<TestEvent>>(_ => slowHandler);
        ServiceProvider sp = services.BuildServiceProvider();
        EventLogger eventLogger = new(NullLogger<EventLogger>.Instance);
        InfraEventBus bus = new(sp, NullLogger<InfraEventBus>.Instance, eventLogger);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        Func<Task> act = async () =>
            await bus.PublishAsync(new TestEvent { Payload = "cancelled" }, cts.Token);

        // The bus swallows OperationCanceledException from handlers
        await act.Should().NotThrowAsync();
    }
}
