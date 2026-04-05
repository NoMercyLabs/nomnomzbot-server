// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Api.Hubs.Dtos;

// ─── Stream / music state ─────────────────────────────────────────────────────

public record StreamStatusDto(
    bool IsLive,
    string? StreamId,
    string? Title,
    string? GameName,
    string? StartedAt
);

public record MusicStateDto(bool IsPlaying, MusicTrackDto? CurrentTrack);

public record MusicTrackDto(
    string TrackName,
    string Artist,
    string Album,
    string? AlbumArtUrl,
    int DurationMs,
    string Provider
);

// ─── Action DTOs ─────────────────────────────────────────────────────────────

public record ModActionDto(
    string Action,
    string ModeratorId,
    string TargetUserId,
    string? Reason,
    int? DurationSeconds
);

public record CommandExecutedDto(
    string CommandName,
    string TriggeredByUserId,
    bool Succeeded,
    string Timestamp
);

public record RewardRedeemedDto(
    string RewardId,
    string RewardTitle,
    string RedemptionId,
    string UserId,
    string UserDisplayName,
    int Cost,
    string? UserInput
);

public record PermissionChangedDto(
    string SubjectType,
    string SubjectId,
    string ResourceType,
    string ResourceId,
    int Value
);

// ─── Overlay / widget / OBS ──────────────────────────────────────────────────

public record WidgetEventDto(string WidgetId, string EventType, object? Data);

public record WidgetSettingsDto(string WidgetId, object Settings);

public record OBSCommandDto(string RequestId, string Command, object? Params);

public record OBSResponseDto(string RequestId, bool Success, object? Data, string? Error);

public record OBSStateUpdateDto(string State, object? Data);

public record OBSConnectedDto(string BroadcasterId, string Version);

// ─── Hub response DTOs ────────────────────────────────────────────────────────

public record JoinChannelResponse(bool Success, string? Error, StreamStatusDto? StreamStatus);

public record SendMessageResponse(bool Success, string? Error, string? MessageId);

public record ActionResponse(bool Success, string? Error);

public record JoinWidgetResponse(bool Success, string? Error, object? InitialState);
