// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Actions;

namespace NomercyBot.Application.Tests.Pipeline.Actions;

public class DelayActionTests
{
    private static ActionContext BuildCtx(Dictionary<string, object?>? parameters = null) =>
        new()
        {
            BroadcasterId = "chan1",
            TriggeredByUserId = "user1",
            TriggeredByDisplayName = "User1",
            Parameters = parameters ?? new Dictionary<string, object?>(),
            Variables = new Dictionary<string, string>(),
        };

    [Fact]
    public async Task ExecuteAsync_ValidSeconds_Succeeds()
    {
        DelayAction action = new();
        ActionContext ctx = BuildCtx(new() { { "seconds", "0.1" } });

        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_MissingSeconds_ReturnsFail()
    {
        DelayAction action = new();
        ActionContext ctx = BuildCtx(); // no seconds

        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("seconds");
    }

    [Fact]
    public async Task ExecuteAsync_NonNumericSeconds_ReturnsFail()
    {
        DelayAction action = new();
        ActionContext ctx = BuildCtx(new() { { "seconds", "not-a-number" } });

        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ZeroSeconds_ClampsToMinimum()
    {
        // 0 is below 0.1 minimum, so delay should be clamped to 0.1
        DelayAction action = new();
        ActionContext ctx = BuildCtx(new() { { "seconds", "0" } });

        // Should complete quickly without error (clamps to 0.1s)
        ActionResult result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsMax_ClampsTo30Seconds()
    {
        // We can't wait 30s in a test, but we can verify cancellation works
        DelayAction action = new();
        ActionContext ctx = BuildCtx(new() { { "seconds", "9999" } });
        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(50));

        // Use a context with a cancellation token
        ActionContext cancelCtx = new()
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "u",
            TriggeredByDisplayName = "u",
            Parameters = ctx.Parameters,
            Variables = ctx.Variables,
            CancellationToken = cts.Token,
        };

        // With very short cancellation, should throw OperationCanceledException
        Func<Task<ActionResult>> act = () => action.ExecuteAsync(cancelCtx);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Type_IsDelay()
    {
        DelayAction action = new();
        action.Type.Should().Be("delay");
    }

    [Fact]
    public void Category_IsControl()
    {
        DelayAction action = new();
        action.Category.Should().Be("control");
    }
}
