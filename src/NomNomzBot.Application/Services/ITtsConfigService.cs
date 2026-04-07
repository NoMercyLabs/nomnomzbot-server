// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Tts;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Manages per-channel TTS configuration and voice enumeration.
/// </summary>
public interface ITtsConfigService
{
    Task<Result<TtsConfigDto>> GetConfigAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    );
    Task<Result<TtsConfigDto>> UpdateConfigAsync(
        string broadcasterId,
        UpdateTtsConfigDto request,
        CancellationToken cancellationToken = default
    );
    Task<Result<IReadOnlyList<TtsVoiceDto>>> GetVoicesAsync(
        CancellationToken cancellationToken = default
    );
    Task<Result<TtsTestResultDto>> TestVoiceAsync(
        string broadcasterId,
        TtsTestRequestDto request,
        CancellationToken cancellationToken = default
    );
}
