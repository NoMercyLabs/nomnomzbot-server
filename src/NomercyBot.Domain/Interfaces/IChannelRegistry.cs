// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Registry for active channels the bot is connected to.
/// Provides fast in-memory access to channel state without hitting the database.
/// </summary>
public interface IChannelRegistry
{
    /// <summary>Gets all currently active channel IDs.</summary>
    Task<IReadOnlyList<string>> GetActiveChannelsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets channel context by broadcaster ID. Returns null if not registered.</summary>
    Task<ChannelContext?> GetChannelContextAsync(string broadcasterId, CancellationToken cancellationToken = default);

    /// <summary>Registers a channel as active.</summary>
    Task RegisterChannelAsync(string broadcasterId, string channelName, CancellationToken cancellationToken = default);

    /// <summary>Unregisters a channel (bot leaving or channel disabled).</summary>
    Task UnregisterChannelAsync(string broadcasterId, CancellationToken cancellationToken = default);

    /// <summary>Checks if a channel is currently registered and active.</summary>
    Task<bool> IsChannelActiveAsync(string broadcasterId, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory representation of a channel's current state.
/// </summary>
public class ChannelContext
{
    public required string BroadcasterId { get; init; }
    public required string ChannelName { get; init; }
    public bool IsLive { get; set; }
    public string? CurrentStreamId { get; set; }
    public string? GameName { get; set; }
    public string? Title { get; set; }
}
