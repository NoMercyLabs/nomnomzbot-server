// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Application.DTOs.Timers;

/// <summary>Full timer detail for viewing/editing.</summary>
public sealed record TimerDto(
    int Id,
    string Name,
    List<string> Messages,
    int IntervalMinutes,
    int MinChatActivity,
    bool IsEnabled,
    DateTime? LastFiredAt,
    int NextMessageIndex,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Lightweight timer info for list views.</summary>
public sealed record TimerListItem(
    int Id,
    string Name,
    int IntervalMinutes,
    bool IsEnabled,
    DateTime? LastFiredAt,
    int MessageCount,
    DateTime CreatedAt
);

/// <summary>Request to create a new timer.</summary>
public sealed record CreateTimerDto
{
    [Required, MaxLength(100)]
    public required string Name { get; init; }

    [Required, MinLength(1)]
    public required List<string> Messages { get; init; }

    [Range(1, 1440)]
    public int IntervalMinutes { get; init; } = 30;

    [Range(0, 10000)]
    public int MinChatActivity { get; init; } = 0;

    public bool IsEnabled { get; init; } = true;
}

/// <summary>Request to update an existing timer.</summary>
public sealed record UpdateTimerDto
{
    [MaxLength(100)]
    public string? Name { get; init; }

    public List<string>? Messages { get; init; }

    [Range(1, 1440)]
    public int? IntervalMinutes { get; init; }

    [Range(0, 10000)]
    public int? MinChatActivity { get; init; }

    public bool? IsEnabled { get; init; }
}
