// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Api.Hubs.Dtos;

// Dashboard DTOs
public record DashboardChatMessageDto(
    string MessageId, string UserId, string UserDisplayName, string UserLogin,
    string Message, bool IsSubscriber, bool IsVip, bool IsModerator,
    string[] Badges, int Bits, string? Color, string Timestamp);

public record ChatBadgeDto(string SetId, string Id);
public record ChannelEventDto(string Type, string BroadcasterId, string? UserId, string? UserDisplayName, object? Data, string Timestamp);
public record PermissionChangedDto(string SubjectType, string SubjectId, string ResourceType, string ResourceId, int Value);
public record MusicStateDto(bool IsPlaying, MusicTrackDto? CurrentTrack);
public record MusicTrackDto(string TrackName, string Artist, string Album, string? AlbumArtUrl, int DurationMs, string Provider);
public record ModActionDto(string Action, string ModeratorId, string TargetUserId, string? Reason, int? DurationSeconds);
public record CommandExecutedDto(string CommandName, string TriggeredByUserId, bool Succeeded, string Timestamp);
public record RewardRedeemedDto(string RewardId, string RewardTitle, string RedemptionId, string UserId, string UserDisplayName, int Cost, string? UserInput);
public record StreamStatusDto(bool IsLive, string? StreamId, string? Title, string? GameName, string? StartedAt);
public record AlertDto(string Type, string? Message, object? Data);

// Overlay DTOs
public record WidgetEventDto(string WidgetId, string EventType, object? Data);
public record WidgetSettingsDto(string WidgetId, object Settings);

// OBS Relay DTOs
public record OBSCommandDto(string RequestId, string Command, object? Params);
public record OBSResponseDto(string RequestId, bool Success, object? Data, string? Error);
public record OBSStateUpdateDto(string State, object? Data);
public record OBSConnectedDto(string BroadcasterId, string Version);

// Hub response DTOs
public record JoinChannelResponse(bool Success, string? Error, StreamStatusDto? StreamStatus);
public record SendMessageResponse(bool Success, string? Error, string? MessageId);
public record ActionResponse(bool Success, string? Error);
public record JoinWidgetResponse(bool Success, string? Error, object? InitialState);
