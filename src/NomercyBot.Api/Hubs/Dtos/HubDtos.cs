// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Api.Hubs.Dtos;

// ─── Chat message DTOs ────────────────────────────────────────────────────────

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
    string Timestamp);

/// <summary>A single fragment of a chat message.</summary>
public record ChatFragmentDto(
    string Type,
    string Text,
    ChatEmoteDto? Emote,
    ChatCheermoteDto? Cheermote,
    ChatMentionDto? Mention);

/// <summary>Emote fragment data.</summary>
public record ChatEmoteDto(
    string EmoteId,
    string? EmoteSetId,
    string? OwnerId,
    string[] Formats);

/// <summary>Cheermote fragment data.</summary>
public record ChatCheermoteDto(
    string Prefix,
    int Bits,
    int Tier);

/// <summary>Mention fragment data (@user).</summary>
public record ChatMentionDto(
    string UserId,
    string UserLogin,
    string UserName);

/// <summary>A chat badge (subscriber, moderator, etc.).</summary>
public record ChatBadgeDto(string SetId, string Id, string? Info = null);

// ─── Other hub DTOs ───────────────────────────────────────────────��────────────

public record ChannelEventDto(string Type, string BroadcasterId, string? UserId, string? UserDisplayName, object? Data, string Timestamp);
public record PermissionChangedDto(string SubjectType, string SubjectId, string ResourceType, string ResourceId, int Value);
public record MusicStateDto(bool IsPlaying, MusicTrackDto? CurrentTrack);
public record MusicTrackDto(string TrackName, string Artist, string Album, string? AlbumArtUrl, int DurationMs, string Provider);
public record ModActionDto(string Action, string ModeratorId, string TargetUserId, string? Reason, int? DurationSeconds);
public record CommandExecutedDto(string CommandName, string TriggeredByUserId, bool Succeeded, string Timestamp);
public record RewardRedeemedDto(string RewardId, string RewardTitle, string RedemptionId, string UserId, string UserDisplayName, int Cost, string? UserInput);
public record StreamStatusDto(bool IsLive, string? StreamId, string? Title, string? GameName, string? StartedAt);
public record AlertDto(string Type, string? Message, object? Data);

// ─── Overlay DTOs ────────────────────────────────��────────────────────────────
public record WidgetEventDto(string WidgetId, string EventType, object? Data);
public record WidgetSettingsDto(string WidgetId, object Settings);

// ─── OBS Relay DTOs ───────────────────────────────────────────────────────────
public record OBSCommandDto(string RequestId, string Command, object? Params);
public record OBSResponseDto(string RequestId, bool Success, object? Data, string? Error);
public record OBSStateUpdateDto(string State, object? Data);
public record OBSConnectedDto(string BroadcasterId, string Version);

// ─── Hub response DTOs ────────────────────────────────────────────────────────
public record JoinChannelResponse(bool Success, string? Error, StreamStatusDto? StreamStatus);
public record SendMessageResponse(bool Success, string? Error, string? MessageId);
public record ActionResponse(bool Success, string? Error);
public record JoinWidgetResponse(bool Success, string? Error, object? InitialState);
