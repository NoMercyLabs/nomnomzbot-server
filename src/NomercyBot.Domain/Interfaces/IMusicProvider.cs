// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Abstraction for music playback providers (Spotify, YouTube Music, etc.).
/// </summary>
public interface IMusicProvider
{
    Task PlayAsync(string broadcasterId, CancellationToken cancellationToken = default);

    Task PauseAsync(string broadcasterId, CancellationToken cancellationToken = default);

    Task SkipAsync(string broadcasterId, CancellationToken cancellationToken = default);

    Task<TrackInfo?> GetCurrentTrackAsync(string broadcasterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrackInfo>> SearchAsync(string broadcasterId, string query, int maxResults = 5, CancellationToken cancellationToken = default);

    Task<bool> AddToQueueAsync(string broadcasterId, string trackUri, CancellationToken cancellationToken = default);
}

public class TrackInfo
{
    public required string TrackName { get; init; }
    public required string Artist { get; init; }
    public required string Album { get; init; }
    public required string TrackUri { get; init; }
    public string? AlbumArtUrl { get; init; }
    public int DurationMs { get; init; }
    public required string Provider { get; init; }
}
