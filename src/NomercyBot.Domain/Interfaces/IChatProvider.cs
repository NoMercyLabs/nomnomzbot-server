// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Abstraction for sending chat messages and performing moderation actions.
/// </summary>
public interface IChatProvider
{
    Task SendMessageAsync(string broadcasterId, string message, CancellationToken cancellationToken = default);

    Task SendReplyAsync(string broadcasterId, string replyToMessageId, string message, CancellationToken cancellationToken = default);

    Task TimeoutUserAsync(string broadcasterId, string userId, int durationSeconds, string? reason = null, CancellationToken cancellationToken = default);

    Task BanUserAsync(string broadcasterId, string userId, string? reason = null, CancellationToken cancellationToken = default);

    Task UnbanUserAsync(string broadcasterId, string userId, CancellationToken cancellationToken = default);

    Task DeleteMessageAsync(string broadcasterId, string messageId, CancellationToken cancellationToken = default);
}
