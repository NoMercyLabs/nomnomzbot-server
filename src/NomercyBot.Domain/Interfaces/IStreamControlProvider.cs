// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Abstraction for stream control operations (title, game, clips, commercials).
/// </summary>
public interface IStreamControlProvider
{
    Task UpdateTitleAsync(
        string broadcasterId,
        string title,
        CancellationToken cancellationToken = default
    );

    Task UpdateGameAsync(
        string broadcasterId,
        string gameId,
        CancellationToken cancellationToken = default
    );

    Task<string?> CreateClipAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    );

    Task StartCommercialAsync(
        string broadcasterId,
        int durationSeconds,
        CancellationToken cancellationToken = default
    );
}
