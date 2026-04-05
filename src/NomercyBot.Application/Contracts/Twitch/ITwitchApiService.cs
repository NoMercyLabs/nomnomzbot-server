// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Contracts.Twitch;

public interface ITwitchApiService
{
    Task<TwitchUserInfo?> GetUserInfoAsync(string userId, CancellationToken ct = default);
    Task<TwitchStreamInfo?> GetStreamInfoAsync(string broadcasterId, CancellationToken ct = default);
    Task<bool> TimeoutUserAsync(string broadcasterId, string userId, int durationSeconds, string? reason = null, CancellationToken ct = default);
    Task<bool> BanUserAsync(string broadcasterId, string userId, string? reason = null, CancellationToken ct = default);
    Task<bool> ShoutoutAsync(string broadcasterId, string toUserId, string moderatorId, CancellationToken ct = default);
}

public record TwitchUserInfo(string Id, string Login, string DisplayName, string? ProfileImageUrl, string BroadcasterType);
public record TwitchStreamInfo(string Id, string UserId, string? GameId, string? GameName, string? Title, bool IsLive, int ViewerCount);
