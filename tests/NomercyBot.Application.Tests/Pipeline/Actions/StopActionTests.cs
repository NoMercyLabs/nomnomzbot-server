// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Actions;

namespace NomercyBot.Application.Tests.Pipeline.Actions;

public class StopActionTests
{
    private static ActionContext BuildCtx()
        => new()
        {
            BroadcasterId = "chan1",
            TriggeredByUserId = "user1",
            TriggeredByDisplayName = "User1",
            Parameters = new Dictionary<string, object?>(),
            Variables = new Dictionary<string, string>()
        };

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessWithStopFlag()
    {
        var action = new StopAction();
        var ctx = BuildCtx();

        var result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeTrue();
        result.StopPipeline.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_HasNoErrorMessage()
    {
        var action = new StopAction();
        var result = await action.ExecuteAsync(BuildCtx());

        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Type_IsStop()
    {
        var action = new StopAction();
        action.Type.Should().Be("stop");
    }

    [Fact]
    public void Category_IsControl()
    {
        var action = new StopAction();
        action.Category.Should().Be("control");
    }
}
