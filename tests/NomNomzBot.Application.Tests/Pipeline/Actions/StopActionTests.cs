// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Actions;

namespace NomNomzBot.Application.Tests.Pipeline.Actions;

public class StopActionTests
{
    private static ActionContext BuildCtx() =>
        new()
        {
            BroadcasterId = "chan1",
            TriggeredByUserId = "user1",
            TriggeredByDisplayName = "User1",
            Parameters = new Dictionary<string, object?>(),
            Variables = new Dictionary<string, string>(),
        };

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessWithStopFlag()
    {
        StopAction action = new();
        ActionContext ctx = BuildCtx();

        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeTrue();
        result.StopPipeline.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_HasNoErrorMessage()
    {
        StopAction action = new();
        ActionResult result = await action.ExecuteAsync(BuildCtx());

        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Type_IsStop()
    {
        StopAction action = new();
        action.Type.Should().Be("stop");
    }

    [Fact]
    public void Category_IsControl()
    {
        StopAction action = new();
        action.Category.Should().Be("control");
    }
}
