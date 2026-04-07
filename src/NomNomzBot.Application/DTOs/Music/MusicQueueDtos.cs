// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Application.DTOs.Music;

/// <summary>Request to add a song to the queue.</summary>
public sealed record SongRequestDto
{
    [Required, MaxLength(500)]
    public required string Query { get; init; }

    [MaxLength(50)]
    public string? RequestedBy { get; init; }
}

/// <summary>A queue item with its position.</summary>
public sealed record QueueItemDto(
    int Position,
    string TrackName,
    string Artist,
    string? ImageUrl,
    int DurationMs,
    string? RequestedBy
);

/// <summary>Current now-playing state.</summary>
public sealed record NowPlayingDto(
    string? TrackName,
    string? Artist,
    string? Album,
    string? ImageUrl,
    int DurationMs,
    int ProgressMs,
    bool IsPlaying,
    int Volume,
    string? RequestedBy,
    string Provider
);

/// <summary>Full music queue including now playing and upcoming tracks.</summary>
public sealed record MusicQueueDto(NowPlayingDto? NowPlaying, IReadOnlyList<QueueItemDto> Queue);
