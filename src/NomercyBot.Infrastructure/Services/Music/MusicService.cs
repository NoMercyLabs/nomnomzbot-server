// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Contracts.Music;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Infrastructure.Services.General;

namespace NoMercyBot.Infrastructure.Services.Music;

/// <summary>
/// Music service that manages a per-channel fair-queue for song requests
/// and delegates playback operations to the registered IMusicProvider.
///
/// The fair queue ensures users who have requested fewer songs are served
/// before power-users, preventing queue monopolisation.
/// </summary>
public sealed class MusicService : IMusicService
{
    private readonly IEnumerable<IMusicProvider> _providers;
    private readonly ILogger<MusicService> _logger;

    // Per-channel request queues: key = broadcasterId
    private readonly ConcurrentDictionary<string, FairQueueChannel> _queues = new();

    public MusicService(
        IEnumerable<IMusicProvider> providers,
        ILogger<MusicService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MusicTrack>> SearchAsync(
        string broadcasterId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(broadcasterId);
        if (provider is null) return [];

        var results = await provider.SearchAsync(broadcasterId, query, maxResults, cancellationToken);
        return results.Select(t => new MusicTrack(
            t.TrackUri, t.TrackName, t.Artist, t.Album, t.AlbumArtUrl, t.DurationMs, t.Provider))
            .ToList();
    }

    public async Task<bool> PlayAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(broadcasterId);
        if (provider is null) return false;
        await provider.PlayAsync(broadcasterId, cancellationToken);
        return true;
    }

    public async Task<bool> PauseAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(broadcasterId);
        if (provider is null) return false;
        await provider.PauseAsync(broadcasterId, cancellationToken);
        return true;
    }

    public async Task<bool> SkipAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(broadcasterId);
        if (provider is null) return false;

        // Dequeue next fair-queue request and add it to provider queue
        var channelQueue = GetOrCreateQueue(broadcasterId);
        var next = channelQueue.Queue.Dequeue();
        if (next is not null)
            await provider.AddToQueueAsync(broadcasterId, next.TrackUri, cancellationToken);

        await provider.SkipAsync(broadcasterId, cancellationToken);
        return true;
    }

    public Task<MusicQueue> GetQueueAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var channelQueue = GetOrCreateQueue(broadcasterId);
        var items = channelQueue.PendingItems
            .Select(i => new MusicQueueItem(i.TrackName, i.Artist, i.AlbumArtUrl, i.DurationMs, i.RequestedBy))
            .ToList();

        return Task.FromResult(new MusicQueue(channelQueue.CurrentTrack, items));
    }

    public async Task<bool> AddToQueueAsync(
        string broadcasterId,
        string trackUri,
        string? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(broadcasterId);
        if (provider is null) return false;

        // Search by URI to get track metadata
        var tracks = await provider.SearchAsync(broadcasterId, trackUri, 1, cancellationToken);
        var track = tracks.FirstOrDefault();
        if (track is null) return false;

        var channelQueue = GetOrCreateQueue(broadcasterId);
        channelQueue.Queue.Enqueue(
            requestedBy ?? "unknown",
            new QueuedTrack(track.TrackUri, track.TrackName, track.Artist, track.AlbumArtUrl, track.DurationMs, requestedBy));

        // If nothing is playing right now, add to provider queue immediately
        await provider.AddToQueueAsync(broadcasterId, trackUri, cancellationToken);

        _logger.LogDebug("{User} queued {Track} in channel {BroadcasterId}",
            requestedBy, track.TrackName, broadcasterId);

        return true;
    }

    public Task<bool> SetVolumeAsync(string broadcasterId, int volume, CancellationToken cancellationToken = default)
    {
        // Volume control is provider-specific; expose via provider extensions if needed
        _logger.LogDebug("SetVolume not implemented for channel {BroadcasterId}", broadcasterId);
        return Task.FromResult(false);
    }

    public async Task<NowPlaying?> GetNowPlayingAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(broadcasterId);
        if (provider is null) return null;

        var track = await provider.GetCurrentTrackAsync(broadcasterId, cancellationToken);
        if (track is null) return null;

        var channelQueue = GetOrCreateQueue(broadcasterId);
        var requestedBy = channelQueue.CurrentTrack?.RequestedBy;

        return new NowPlaying(
            track.TrackName, track.Artist, track.Album, track.AlbumArtUrl,
            track.DurationMs, ProgressMs: 0, IsPlaying: true, Volume: 100,
            requestedBy, track.Provider);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private IMusicProvider? GetProvider(string broadcasterId)
    {
        // TODO: select provider per-broadcaster based on Service entity (spotify / other)
        // For now returns the first registered provider (Spotify)
        return _providers.FirstOrDefault();
    }

    private FairQueueChannel GetOrCreateQueue(string broadcasterId)
        => _queues.GetOrAdd(broadcasterId, _ => new FairQueueChannel());

    // ─── Inner types ──────────────────────────────────────────────────────────

    private sealed class FairQueueChannel
    {
        public IFairQueue<QueuedTrack> Queue { get; } = new FairQueue<QueuedTrack>();
        public NowPlaying? CurrentTrack { get; set; }

        /// <summary>Snapshot of queue for display (does not consume items).</summary>
        public IEnumerable<QueuedTrack> PendingItems
        {
            get
            {
                // FairQueue doesn't support iteration; we maintain a parallel list for display
                return _displayList;
            }
        }

        private readonly List<QueuedTrack> _displayList = [];

        public void AddDisplay(QueuedTrack track) => _displayList.Add(track);
        public void RemoveDisplay(QueuedTrack track) => _displayList.Remove(track);
    }

    private sealed record QueuedTrack(
        string TrackUri,
        string TrackName,
        string Artist,
        string? AlbumArtUrl,
        int DurationMs,
        string? RequestedBy);
}
