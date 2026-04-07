// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Infrastructure.Pipeline;
using NoMercyBot.Infrastructure.Pipeline.Actions;

namespace NomNomzBot.Infrastructure.Tests.Pipeline.Actions;

public class InfraSetVariableActionTests
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

    private static ActionDefinition MakeDef(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<ActionDefinition>(json)!;

    [Fact]
    public async Task ExecuteAsync_WithNameAndValue_SetsVariable()
    {
        SetVariableAction action = new();
        PipelineExecutionContext ctx = BuildCtx();
        ActionDefinition def = MakeDef("""{"type":"set_variable","name":"greeting","value":"Hello"}""");

        ActionResult result = await action.ExecuteAsync(ctx, def);

        result.Succeeded.Should().BeTrue();
        ctx.Variables["greeting"].Should().Be("Hello");
    }

    [Fact]
    public async Task ExecuteAsync_MissingName_ReturnsFail()
    {
        SetVariableAction action = new();
        PipelineExecutionContext ctx = BuildCtx();
        ActionDefinition def = MakeDef("""{"type":"set_variable","value":"Hello"}""");

        ActionResult result = await action.ExecuteAsync(ctx, def);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("name");
    }

    [Fact]
    public async Task ExecuteAsync_MissingValue_SetsEmptyString()
    {
        SetVariableAction action = new();
        PipelineExecutionContext ctx = BuildCtx();
        ActionDefinition def = MakeDef("""{"type":"set_variable","name":"myvar"}""");

        ActionResult result = await action.ExecuteAsync(ctx, def);

        result.Succeeded.Should().BeTrue();
        ctx.Variables["myvar"].Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_OutputContainsNameAndValue()
    {
        SetVariableAction action = new();
        PipelineExecutionContext ctx = BuildCtx();
        ActionDefinition def = MakeDef("""{"type":"set_variable","name":"x","value":"42"}""");

        ActionResult result = await action.ExecuteAsync(ctx, def);

        result.Output.Should().Contain("x").And.Contain("42");
    }

    [Fact]
    public void ActionType_IsSetVariable()
    {
        SetVariableAction action = new();
        action.ActionType.Should().Be("set_variable");
    }
}
