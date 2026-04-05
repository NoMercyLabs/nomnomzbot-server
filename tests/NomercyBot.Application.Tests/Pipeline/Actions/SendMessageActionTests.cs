// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Actions;
using NoMercyBot.Domain.Interfaces;
using NSubstitute;

namespace NomercyBot.Application.Tests.Pipeline.Actions;

public class SendMessageActionTests
{
    private static ActionContext BuildCtx(
        Dictionary<string, object?>? parameters = null,
        Dictionary<string, string>? variables = null)
        => new()
        {
            BroadcasterId = "chan1",
            TriggeredByUserId = "user1",
            TriggeredByDisplayName = "User1",
            Parameters = parameters ?? new Dictionary<string, object?>(),
            Variables = variables ?? new Dictionary<string, string>()
        };

    [Fact]
    public async Task ExecuteAsync_WithMessage_SendsToChat()
    {
        var chat = Substitute.For<IChatProvider>();
        var action = new SendMessageAction(chat);

        var ctx = BuildCtx(new Dictionary<string, object?> { { "message", "Hello world" } });
        var result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeTrue();
        await chat.Received(1).SendMessageAsync("chan1", "Hello world", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MissingMessage_ReturnsFail()
    {
        var chat = Substitute.For<IChatProvider>();
        var action = new SendMessageAction(chat);

        var ctx = BuildCtx(); // no message param
        var result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("message");
        await chat.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_BlankMessage_ReturnsFail()
    {
        var chat = Substitute.For<IChatProvider>();
        var action = new SendMessageAction(chat);

        var ctx = BuildCtx(new Dictionary<string, object?> { { "message", "   " } });
        var result = await action.ExecuteAsync(ctx);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_MessageWithVariable_ResolvesBeforeSend()
    {
        string? sentMessage = null;
        var chat = Substitute.For<IChatProvider>();
        chat.SendMessageAsync(Arg.Any<string>(), Arg.Do<string>(m => sentMessage = m), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var action = new SendMessageAction(chat);
        var ctx = BuildCtx(
            new Dictionary<string, object?> { { "message", "Hi {{user}}!" } },
            new Dictionary<string, string> { { "user", "Alice" } });

        await action.ExecuteAsync(ctx);

        sentMessage.Should().Be("Hi Alice!");
    }

    [Fact]
    public void Type_IsCorrect()
    {
        var chat = Substitute.For<IChatProvider>();
        var action = new SendMessageAction(chat);

        action.Type.Should().Be("send_message");
    }

    [Fact]
    public void Category_IsChat()
    {
        var chat = Substitute.For<IChatProvider>();
        var action = new SendMessageAction(chat);

        action.Category.Should().Be("chat");
    }
}
