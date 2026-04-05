// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Contracts.Twitch;

public interface ITwitchChatService
{
    Task SendMessageAsync(string channelId, string message, CancellationToken ct = default);
    Task SendReplyAsync(string channelId, string replyToMessageId, string message, CancellationToken ct = default);
    Task JoinChannelAsync(string channelName, CancellationToken ct = default);
    Task LeaveChannelAsync(string channelName, CancellationToken ct = default);
}
