// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Published for every chat message received across all channels.
/// This is the HOT PATH event -- handlers must be fast.
/// </summary>
public sealed class ChatMessageReceivedEvent : DomainEventBase
{
    public required string MessageId { get; init; }
    public required string UserId { get; init; }
    public required string UserDisplayName { get; init; }
    public required string UserLogin { get; init; }
    public required string Message { get; init; }
    public required bool IsSubscriber { get; init; }
    public required bool IsVip { get; init; }
    public required bool IsModerator { get; init; }
    public required bool IsBroadcaster { get; init; }

    /// <summary>Parsed badges from Twitch IRC tags.</summary>
    public required IReadOnlyDictionary<string, string> Badges { get; init; }

    /// <summary>Bits cheered in this message, or 0.</summary>
    public int Bits { get; init; }

    /// <summary>If this is a reply, the parent message ID.</summary>
    public string? ReplyParentMessageId { get; init; }
}
