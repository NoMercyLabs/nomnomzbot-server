// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Actions;

namespace NomNomzBot.Application.Tests.Pipeline.Actions;

public class SetVariableActionTests
{
    private static ActionContext BuildCtx(
        Dictionary<string, object?>? parameters = null,
        Dictionary<string, string>? variables = null
    ) =>
        new()
        {
            BroadcasterId = "chan1",
            TriggeredByUserId = "user1",
            TriggeredByDisplayName = "User1",
            Parameters = parameters ?? new Dictionary<string, object?>(),
            Variables = variables ?? new Dictionary<string, string>(),
        };

    [Fact]
    public async Task ExecuteAsync_WithNameAndValue_Succeeds()
    {
        SetVariableAction action = new();
        ActionContext ctx = BuildCtx(
            new() { { "name", "greeting" }, { "value", "Hello" } }
        );

        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeTrue();
        result.VariablesSet.Should().ContainKey("greeting").WhoseValue.Should().Be("Hello");
    }

    [Fact]
    public async Task ExecuteAsync_MissingName_ReturnsFail()
    {
        SetVariableAction action = new();
        ActionContext ctx = BuildCtx(new() { { "value", "something" } });

        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("name");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyName_ReturnsFail()
    {
        SetVariableAction action = new();
        ActionContext ctx = BuildCtx(new() { { "name", "" } });

        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_MissingValue_SetsEmptyString()
    {
        SetVariableAction action = new();
        ActionContext ctx = BuildCtx(new() { { "name", "myvar" } });

        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeTrue();
        result.VariablesSet.Should().ContainKey("myvar").WhoseValue.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ValueWithVariable_ResolvesFromContext()
    {
        SetVariableAction action = new();
        ActionContext ctx = BuildCtx(
            new() { { "name", "copy" }, { "value", "{{original}}" } },
            new() { { "original", "hello" } }
        );

        ActionResult result = await action.ExecuteAsync(ctx);

        result.VariablesSet.Should().ContainKey("copy").WhoseValue.Should().Be("hello");
    }

    [Fact]
    public void Type_IsSetVariable()
    {
        SetVariableAction action = new();
        action.Type.Should().Be("set_variable");
    }

    [Fact]
    public void Category_IsControl()
    {
        SetVariableAction action = new();
        action.Category.Should().Be("control");
    }
}
