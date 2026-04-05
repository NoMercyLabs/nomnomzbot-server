// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Music;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Manages per-channel music configuration stored in the Configuration key-value store.
/// </summary>
public interface IMusicConfigService
{
    Task<Result<MusicConfigDto>> GetConfigAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    );
    Task<Result<MusicConfigDto>> UpdateConfigAsync(
        string broadcasterId,
        UpdateMusicConfigDto request,
        CancellationToken cancellationToken = default
    );
}
