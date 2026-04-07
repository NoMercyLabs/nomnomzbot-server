// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json.Serialization;

namespace NoMercyBot.Api.Models;

/// <summary>
/// Snapshot of a channel's current state, returned by GET /api/v1/dashboard/{broadcasterId}/stats.
/// Live fields come from the in-memory ChannelContext; counts are session totals since bot join.
/// </summary>
public sealed record DashboardStatsDto
{
    [JsonPropertyName("isLive")]
    public bool IsLive { get; init; }

    [JsonPropertyName("streamTitle")]
    public string? StreamTitle { get; init; }

    [JsonPropertyName("gameName")]
    public string? GameName { get; init; }

    /// <summary>Last known viewer count (0 if offline or not yet received from Twitch).</summary>
    [JsonPropertyName("viewerCount")]
    public int ViewerCount { get; init; }

    /// <summary>Follower count (not tracked server-side; always 0 in this implementation).</summary>
    [JsonPropertyName("followerCount")]
    public int FollowerCount { get; init; }

    /// <summary>Commands successfully executed this session.</summary>
    [JsonPropertyName("commandsUsed")]
    public long CommandsUsed { get; init; }

    /// <summary>Chat messages received this session.</summary>
    [JsonPropertyName("messagesCount")]
    public long MessagesCount { get; init; }

    /// <summary>Stream uptime in whole seconds, null if offline.</summary>
    [JsonPropertyName("uptime")]
    public long? Uptime { get; init; }
}
