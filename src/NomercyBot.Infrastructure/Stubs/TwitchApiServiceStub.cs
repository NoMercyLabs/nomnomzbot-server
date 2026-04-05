// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Contracts.Twitch;

namespace NoMercyBot.Infrastructure.Stubs;

public class TwitchApiServiceStub : ITwitchApiService
{
    private readonly ILogger<TwitchApiServiceStub> _logger;

    public TwitchApiServiceStub(ILogger<TwitchApiServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<TwitchUserInfo?> GetUserInfoAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] GetUserInfo: {UserId}", userId);
        return Task.FromResult<TwitchUserInfo?>(null);
    }

    public Task<TwitchStreamInfo?> GetStreamInfoAsync(
        string broadcasterId,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug("[STUB] GetStreamInfo: {BroadcasterId}", broadcasterId);
        return Task.FromResult<TwitchStreamInfo?>(null);
    }

    public Task<bool> TimeoutUserAsync(
        string broadcasterId,
        string userId,
        int durationSeconds,
        string? reason = null,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug(
            "[STUB] TimeoutUser {UserId} in {ChannelId} for {Seconds}s",
            userId,
            broadcasterId,
            durationSeconds
        );
        return Task.FromResult(true);
    }

    public Task<bool> BanUserAsync(
        string broadcasterId,
        string userId,
        string? reason = null,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug("[STUB] BanUser {UserId} in {ChannelId}", userId, broadcasterId);
        return Task.FromResult(true);
    }

    public Task<bool> UnbanUserAsync(
        string broadcasterId,
        string userId,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug("[STUB] UnbanUser {UserId} in {ChannelId}", userId, broadcasterId);
        return Task.FromResult(true);
    }

    public Task<bool> ShoutoutAsync(
        string broadcasterId,
        string toUserId,
        string moderatorId,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug("[STUB] Shoutout to {ToUserId} in {ChannelId}", toUserId, broadcasterId);
        return Task.FromResult(true);
    }

    public Task<bool> SendChatMessageAsync(
        string broadcasterId,
        string senderUserId,
        string message,
        string? replyParentMessageId = null,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug(
            "[STUB] SendChatMessage to {ChannelId}: {Message}",
            broadcasterId,
            message
        );
        return Task.FromResult(true);
    }

    public Task<bool> DeleteChatMessageAsync(
        string broadcasterId,
        string messageId,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug(
            "[STUB] DeleteChatMessage {MessageId} in {ChannelId}",
            messageId,
            broadcasterId
        );
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<TwitchRewardInfo>> GetCustomRewardsAsync(
        string broadcasterId,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug("[STUB] GetCustomRewards for {BroadcasterId}", broadcasterId);
        return Task.FromResult<IReadOnlyList<TwitchRewardInfo>>([]);
    }

    public Task<bool> UpdateRedemptionStatusAsync(
        string broadcasterId,
        string rewardId,
        string redemptionId,
        string status,
        CancellationToken ct = default
    )
    {
        _logger.LogDebug(
            "[STUB] UpdateRedemptionStatus {RedemptionId} -> {Status}",
            redemptionId,
            status
        );
        return Task.FromResult(true);
    }
}
