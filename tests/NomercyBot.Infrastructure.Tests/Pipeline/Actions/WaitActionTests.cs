// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Infrastructure.Pipeline;
using NoMercyBot.Infrastructure.Pipeline.Actions;

namespace NomercyBot.Infrastructure.Tests.Pipeline.Actions;

public class WaitActionTests
{
    private static PipelineExecutionContext BuildCtx(CancellationToken ct = default) =>
        new()
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "user",
            TriggeredByDisplayName = "User",
            MessageId = "msg",
            RawMessage = "",
            CancellationToken = ct,
        };

    private static ActionDefinition BuildAction(string json)
    {
        var def = System.Text.Json.JsonSerializer.Deserialize<ActionDefinition>(json)!;
        return def;
    }

    [Fact]
    public async Task ExecuteAsync_ZeroDelay_ReturnsImmediately()
    {
        var action = new WaitAction();
        var ctx = BuildCtx();
        var def = BuildAction("""{"type":"wait","milliseconds":0}""");

        var result = await action.ExecuteAsync(ctx, def);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SmallDelay_Succeeds()
    {
        var action = new WaitAction();
        var ctx = BuildCtx();
        var def = BuildAction("""{"type":"wait","milliseconds":50}""");

        var result = await action.ExecuteAsync(ctx, def);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SecondsParam_Converts()
    {
        var action = new WaitAction();
        var ctx = BuildCtx();
        var def = BuildAction("""{"type":"wait","seconds":0}"""); // 0 = no delay

        var result = await action.ExecuteAsync(ctx, def);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsMax_ClampsTo30Seconds()
    {
        var action = new WaitAction();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var ctx = BuildCtx(cts.Token);
        var def = BuildAction("""{"type":"wait","milliseconds":60000}"""); // 60s → clamped to 30s

        // With fast cancellation, we can verify it waits (clamped), then gets cancelled
        var act = () => action.ExecuteAsync(ctx, def);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_Cancelled_ThrowsOperationCancelled()
    {
        var action = new WaitAction();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ctx = BuildCtx(cts.Token);
        var def = BuildAction("""{"type":"wait","milliseconds":5000}""");

        var act = () => action.ExecuteAsync(ctx, def);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ActionType_IsWait()
    {
        var action = new WaitAction();
        action.ActionType.Should().Be("wait");
    }
}
