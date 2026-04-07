// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Infrastructure.Pipeline;
using NoMercyBot.Infrastructure.Pipeline.Actions;

namespace NomNomzBot.Infrastructure.Tests.Pipeline.Actions;

public class InfraStopActionTests
{
    private static PipelineExecutionContext BuildCtx() =>
        new()
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "user",
            TriggeredByDisplayName = "User",
            MessageId = "msg",
            RawMessage = "",
        };

    [Fact]
    public async Task ExecuteAsync_SetsShouldStopOnContext()
    {
        StopAction action = new();
        PipelineExecutionContext ctx = BuildCtx();
        ActionDefinition def = System.Text.Json.JsonSerializer.Deserialize<ActionDefinition>(
            """{"type":"stop"}"""
        )!;

        await action.ExecuteAsync(ctx, def);

        ctx.ShouldStop.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess()
    {
        StopAction action = new();
        PipelineExecutionContext ctx = BuildCtx();
        ActionDefinition def = System.Text.Json.JsonSerializer.Deserialize<ActionDefinition>(
            """{"type":"stop"}"""
        )!;

        ActionResult result = await action.ExecuteAsync(ctx, def);

        result.Succeeded.Should().BeTrue();
        result.Output.Should().Contain("stopped");
    }

    [Fact]
    public void ActionType_IsStop()
    {
        StopAction action = new();
        action.ActionType.Should().Be("stop");
    }
}
