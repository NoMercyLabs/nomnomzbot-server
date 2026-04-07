// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Contracts.Twitch;

namespace NoMercyBot.Infrastructure.Stubs;

public class TwitchChatServiceStub : ITwitchChatService
{
    private readonly ILogger<TwitchChatServiceStub> _logger;

    public TwitchChatServiceStub(ILogger<TwitchChatServiceStub> logger)
    {
        _logger = logger;
    }

    public Task SendMessageAsync(string channelId, string message, CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] SendMessage to {ChannelId}: {Message}", channelId, message);
        return Task.CompletedTask;
    }

    public Task SendReplyAsync(
        string channelId,
        string replyToMessageId,
        string message,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug(
            "[STUB] SendReply to {ChannelId} re:{ReplyId}: {Message}",
            channelId,
            replyToMessageId,
            message
        );
        return Task.CompletedTask;
    }

    public Task JoinChannelAsync(string channelName, CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] JoinChannel: #{ChannelName}", channelName);
        return Task.CompletedTask;
    }

    public Task LeaveChannelAsync(string channelName, CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] LeaveChannel: #{ChannelName}", channelName);
        return Task.CompletedTask;
    }
}
