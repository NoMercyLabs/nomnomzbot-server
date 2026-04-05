// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Application.DTOs.Music;

/// <summary>Music configuration for a channel.</summary>
public sealed record MusicConfigDto(
    bool IsEnabled,
    string PreferredProvider,
    int MaxQueueSize,
    int MaxRequestsPerUser,
    bool AllowYouTube,
    bool AllowSpotify,
    string MinTrustLevel
);

/// <summary>Request to update music configuration.</summary>
public sealed record UpdateMusicConfigDto
{
    public bool? IsEnabled { get; init; }

    [RegularExpression("^(auto|spotify|youtube)$")]
    public string? PreferredProvider { get; init; }

    [Range(1, 500)]
    public int? MaxQueueSize { get; init; }

    [Range(1, 50)]
    public int? MaxRequestsPerUser { get; init; }

    public bool? AllowYouTube { get; init; }
    public bool? AllowSpotify { get; init; }

    [RegularExpression("^(everyone|subscribers|vip|moderators|broadcaster)$")]
    public string? MinTrustLevel { get; init; }
}
