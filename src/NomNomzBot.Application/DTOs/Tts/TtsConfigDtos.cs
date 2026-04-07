// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Application.DTOs.Tts;

/// <summary>TTS configuration for a channel.</summary>
public sealed record TtsConfigDto(
    bool IsEnabled,
    string DefaultVoiceId,
    int MaxLength,
    string MinPermission,
    bool SkipBotMessages,
    bool ReadUsernames
);

/// <summary>Request to update TTS configuration.</summary>
public sealed record UpdateTtsConfigDto
{
    public bool? IsEnabled { get; init; }

    [MaxLength(255)]
    public string? DefaultVoiceId { get; init; }

    [Range(1, 500)]
    public int? MaxLength { get; init; }

    [RegularExpression("^(everyone|subscribers|vip|moderators|broadcaster)$")]
    public string? MinPermission { get; init; }

    public bool? SkipBotMessages { get; init; }
    public bool? ReadUsernames { get; init; }
}
