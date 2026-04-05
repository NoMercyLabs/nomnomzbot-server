// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Domain.ValueObjects;

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Published for every chat message received via EventSub channel.chat.message.
/// This is the HOT PATH event — handlers must be fast.
/// </summary>
public sealed class ChatMessageReceivedEvent : DomainEventBase
{
    public required string MessageId { get; init; }
    public required string BroadcasterId { get; init; }
    public required string UserId { get; init; }
    public required string UserDisplayName { get; init; }
    public required string UserLogin { get; init; }

    /// <summary>Raw plain-text content (concatenation of all fragment texts).</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Structured fragments from EventSub: text, emote, cheermote, mention.
    /// Enables inline emote rendering, colored mentions, animated cheermotes.
    /// </summary>
    public required IReadOnlyList<ChatMessageFragment> Fragments { get; init; }

    /// <summary>User's chat color as #RRGGBB hex (or null if unset).</summary>
    public string? ColorHex { get; init; }

    /// <summary>
    /// Message type from EventSub: "text" | "channel_points_highlighted" |
    /// "channel_points_sub_only" | "user_intro" | "power_ups_message_effect" |
    /// "power_ups_gigantified_emote"
    /// </summary>
    public string MessageType { get; init; } = "text";

    /// <summary>Parsed badges with their set ID, badge ID, and info field.</summary>
    public required IReadOnlyList<ChatBadge> Badges { get; init; }

    public required bool IsSubscriber { get; init; }
    public required bool IsVip { get; init; }
    public required bool IsModerator { get; init; }
    public required bool IsBroadcaster { get; init; }

    /// <summary>Bits cheered in this message, or 0.</summary>
    public int Bits { get; init; }

    /// <summary>If this is a reply, the parent message ID.</summary>
    public string? ReplyParentMessageId { get; init; }

    /// <summary>The parent reply message text (for display in the UI thread).</summary>
    public string? ReplyParentMessageBody { get; init; }

    /// <summary>Display name of the user being replied to.</summary>
    public string? ReplyParentUserName { get; init; }
}
