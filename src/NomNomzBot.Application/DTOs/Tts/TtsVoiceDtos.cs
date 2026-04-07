// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Application.DTOs.Tts;

/// <summary>An available TTS voice.</summary>
public sealed record TtsVoiceDto(
    string Id,
    string Name,
    string DisplayName,
    string Locale,
    string Gender,
    string Provider,
    bool IsDefault
);

/// <summary>Request to test a TTS voice.</summary>
public sealed record TtsTestRequestDto
{
    [Required, MaxLength(500)]
    public required string Text { get; init; }

    [Required, MaxLength(255)]
    public required string VoiceId { get; init; }
}

/// <summary>Result of a TTS test synthesis.</summary>
public sealed record TtsTestResultDto(
    string VoiceId,
    string Provider,
    int DurationMs,
    string AudioBase64
);
