// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Music;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Infrastructure.Collections;
using NoMercyBot.Infrastructure.Services.Trust;

namespace NoMercyBot.Infrastructure.Services.Music;

/// <summary>
/// Orchestrates music playback using the registered IMusicProvider implementations.
/// Maintains a per-channel fair queue for song requests and enforces trust-level limits.
/// </summary>
public sealed class MusicService : IMusicService
{
    private readonly IEnumerable<IMusicProvider> _providers;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<MusicService> _logger;

    // Per-channel song request queues (channelId → fair queue)
    private readonly Dictionary<string, FairQueue<SongRequestEntry>> _queues = new();
    private readonly Lock _queueLock = new();

    public MusicService(
        IEnumerable<IMusicProvider> providers,
        IApplicationDbContext db,
        ILogger<MusicService> logger
    )
    {
        _providers = providers;
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MusicTrack>> SearchAsync(
        string broadcasterId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default
    )
    {
        var provider = await GetActiveProviderAsync(broadcasterId, cancellationToken);
        if (provider is null)
            return [];

        var results = await provider.SearchAsync(
            broadcasterId,
            query,
            maxResults,
            cancellationToken
        );

        return results
            .Select(t => new MusicTrack(
                t.TrackUri,
                t.TrackName,
                t.Artist,
                t.Album,
                t.AlbumArtUrl,
                t.DurationMs,
                t.Provider
            ))
            .ToList();
    }

    public async Task<bool> PlayAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        var provider = await GetActiveProviderAsync(broadcasterId, cancellationToken);
        if (provider is null)
            return false;

        await provider.PlayAsync(broadcasterId, cancellationToken);
        return true;
    }

    public async Task<bool> PauseAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        var provider = await GetActiveProviderAsync(broadcasterId, cancellationToken);
        if (provider is null)
            return false;

        await provider.PauseAsync(broadcasterId, cancellationToken);
        return true;
    }

    public async Task<bool> SkipAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        var provider = await GetActiveProviderAsync(broadcasterId, cancellationToken);
        if (provider is null)
            return false;

        // Dequeue next from fair queue and add to provider queue
        var next = DequeueNext(broadcasterId);
        if (next is not null)
        {
            await provider.AddToQueueAsync(broadcasterId, next.TrackUri, cancellationToken);
        }

        await provider.SkipAsync(broadcasterId, cancellationToken);
        return true;
    }

    public async Task<MusicQueue> GetQueueAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        var nowPlaying = await GetNowPlayingAsync(broadcasterId, cancellationToken);

        FairQueue<SongRequestEntry>? queue;
        lock (_queueLock)
        {
            _queues.TryGetValue(broadcasterId, out queue);
        }

        IReadOnlyList<MusicQueueItem> items = queue is null
            ? []
            : queue
                .GetSnapshot()
                .Select(e => new MusicQueueItem(
                    e.Item.TrackName,
                    e.Item.Artist,
                    e.Item.ImageUrl,
                    e.Item.DurationMs,
                    e.Item.RequestedBy
                ))
                .ToList();

        return new MusicQueue(nowPlaying, items);
    }

    public async Task<bool> AddToQueueAsync(
        string broadcasterId,
        string trackUri,
        string? requestedBy = null,
        CancellationToken cancellationToken = default
    )
    {
        var provider = await GetActiveProviderAsync(broadcasterId, cancellationToken);
        if (provider is null)
            return false;

        // Look up track info for the queue display
        var track = await provider.SearchAsync(broadcasterId, trackUri, 1, cancellationToken);
        var trackInfo =
            track.FirstOrDefault(t => t.TrackUri == trackUri)
            ?? new TrackInfo
            {
                TrackName = trackUri,
                Artist = "Unknown",
                Album = string.Empty,
                TrackUri = trackUri,
                Provider = "unknown",
            };

        var entry = new SongRequestEntry(
            trackUri,
            trackInfo.TrackName,
            trackInfo.Artist,
            trackInfo.AlbumArtUrl,
            trackInfo.DurationMs,
            requestedBy ?? "anonymous"
        );

        // Add to fair queue
        FairQueue<SongRequestEntry> queue;
        lock (_queueLock)
        {
            if (!_queues.TryGetValue(broadcasterId, out queue!))
            {
                queue = new FairQueue<SongRequestEntry>();
                _queues[broadcasterId] = queue;
            }
        }

        queue.Enqueue(requestedBy ?? "anonymous", entry);

        // If nothing is in the provider's queue, add immediately
        var queueSize = queue.Count;
        if (queueSize <= 1)
        {
            await provider.AddToQueueAsync(broadcasterId, trackUri, cancellationToken);
        }

        _logger.LogInformation(
            "Queued track '{Track}' for {BroadcasterId} (requested by {RequestedBy})",
            trackInfo.TrackName,
            broadcasterId,
            requestedBy
        );

        return true;
    }

    public async Task<bool> SetVolumeAsync(
        string broadcasterId,
        int volume,
        CancellationToken cancellationToken = default
    )
    {
        // Volume control is Spotify-specific; try Spotify provider first
        var spotifyProvider = _providers.OfType<SpotifyMusicProvider>().FirstOrDefault();
        if (spotifyProvider is null)
            return false;

        // Direct Spotify volume — not in IMusicProvider interface, logged only
        _logger.LogDebug(
            "SetVolumeAsync({Volume}) called for {BroadcasterId}",
            volume,
            broadcasterId
        );
        return await Task.FromResult(false);
    }

    public async Task<NowPlaying?> GetNowPlayingAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        var provider = await GetActiveProviderAsync(broadcasterId, cancellationToken);
        if (provider is null)
            return null;

        var track = await provider.GetCurrentTrackAsync(broadcasterId, cancellationToken);
        if (track is null)
            return null;

        return new NowPlaying(
            track.TrackName,
            track.Artist,
            track.Album,
            track.AlbumArtUrl,
            track.DurationMs,
            0,
            true,
            100,
            null,
            track.Provider
        );
    }

    // ─── Trust-level enforcement ──────────────────────────────────────────────

    /// <summary>
    /// Validates that a user's trust tier permits queuing music.
    /// Returns null if allowed, or an error message if blocked.
    /// </summary>
    public string? CheckTrustPermission(double trustScore, bool isYouTubeContent)
    {
        var tier = TrustScoreCalculator.GetTier(trustScore);

        return tier switch
        {
            TrustTier.Untrusted =>
                "Your trust score is too low. Requests require moderator approval.",
            TrustTier.Low when isYouTubeContent =>
                "YouTube requests are not available at your trust level. Try Spotify.",
            _ => null,
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<IMusicProvider?> GetActiveProviderAsync(
        string broadcasterId,
        CancellationToken cancellationToken
    )
    {
        // Look up which services are connected for this broadcaster
        var services = await _db
            .Services.Where(s =>
                s.BroadcasterId == broadcasterId && s.Enabled && s.AccessToken != null
            )
            .Select(s => s.Name)
            .ToListAsync(cancellationToken);

        // Priority: Spotify > YouTube
        if (services.Contains("spotify"))
        {
            var spotify = _providers.OfType<SpotifyMusicProvider>().FirstOrDefault();
            if (spotify is not null)
                return spotify;
        }

        if (services.Contains("youtube"))
        {
            var youtube = _providers.OfType<YouTubeMusicProvider>().FirstOrDefault();
            if (youtube is not null)
                return youtube;
        }

        _logger.LogDebug("No active music provider for broadcaster {BroadcasterId}", broadcasterId);
        return null;
    }

    public Task<bool> RemoveFromQueueAsync(
        string broadcasterId,
        int position,
        CancellationToken cancellationToken = default
    )
    {
        lock (_queueLock)
        {
            if (!_queues.TryGetValue(broadcasterId, out var queue))
                return Task.FromResult(false);

            return Task.FromResult(queue.RemoveAt(position));
        }
    }

    private SongRequestEntry? DequeueNext(string broadcasterId)
    {
        lock (_queueLock)
        {
            return _queues.TryGetValue(broadcasterId, out var queue) ? queue.Dequeue() : null;
        }
    }
}

/// <summary>An item in the per-channel song request queue.</summary>
internal sealed record SongRequestEntry(
    string TrackUri,
    string TrackName,
    string Artist,
    string? ImageUrl,
    int DurationMs,
    string RequestedBy
);
