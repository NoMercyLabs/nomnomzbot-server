// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Api.Hubs.Dtos;

/// <summary>
/// Rich chat message DTO sent to dashboard/overlay clients via SignalR.
/// Includes structured fragments for inline emote, mention, and cheermote rendering.
/// </summary>
public record DashboardChatMessageDto(
    string MessageId,
    string BroadcasterId,
    string UserId,
    string UserDisplayName,
    string UserLogin,
    /// <summary>Raw plain-text fallback (for clients that don't render fragments).</summary>
    string Message,
    /// <summary>Structured fragments: text, emote, cheermote, mention.</summary>
    IReadOnlyList<ChatFragmentDto> Fragments,
    bool IsSubscriber,
    bool IsVip,
    bool IsModerator,
    bool IsBroadcaster,
    IReadOnlyList<ChatBadgeDto> Badges,
    int Bits,
    /// <summary>User's chat color #RRGGBB — also used as accent color in dashboard.</summary>
    string? ColorHex,
    /// <summary>Message type: text | channel_points_highlighted | channel_points_sub_only | user_intro</summary>
    string MessageType,
    string? ReplyParentMessageId,
    string? ReplyParentMessageBody,
    string? ReplyParentUserName,
    string Timestamp
);

/// <summary>A single fragment of a chat message.</summary>
public record ChatFragmentDto(
    string Type,
    string Text,
    ChatEmoteDto? Emote,
    ChatCheermoteDto? Cheermote,
    ChatMentionDto? Mention
);

/// <summary>Emote fragment data.</summary>
public record ChatEmoteDto(string EmoteId, string? EmoteSetId, string? OwnerId, string[] Formats);

/// <summary>Cheermote fragment data.</summary>
public record ChatCheermoteDto(string Prefix, int Bits, int Tier);

/// <summary>Mention fragment data (@user).</summary>
public record ChatMentionDto(string UserId, string UserLogin, string UserName);

/// <summary>A chat badge (subscriber, moderator, etc.).</summary>
public record ChatBadgeDto(string SetId, string Id, string? Info = null);
