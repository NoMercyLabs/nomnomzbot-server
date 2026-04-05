// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Rewards;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Services.Application;

public class RewardService : IRewardService
{
    private readonly IApplicationDbContext _db;

    public RewardService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<RewardDetail>> CreateAsync(
        string broadcasterId,
        CreateRewardRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var channel = await _db.Channels.AnyAsync(c => c.Id == broadcasterId, cancellationToken);

        if (!channel)
            return Errors.ChannelNotFound<RewardDetail>(broadcasterId);

        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            BroadcasterId = broadcasterId,
            Title = request.Title,
            IsEnabled = true,
        };

        _db.Rewards.Add(reward);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDetail(reward));
    }

    public async Task<Result<RewardDetail>> UpdateAsync(
        string broadcasterId,
        string rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!Guid.TryParse(rewardId, out var guid))
            return Result.Failure<RewardDetail>(
                $"Invalid reward ID '{rewardId}'.",
                "VALIDATION_FAILED"
            );

        var reward = await _db.Rewards.FirstOrDefaultAsync(
            r => r.Id == guid && r.BroadcasterId == broadcasterId,
            cancellationToken
        );

        if (reward is null)
            return Errors.NotFound<RewardDetail>("Reward", rewardId);

        if (request.Title is not null)
            reward.Title = request.Title;
        if (request.IsEnabled.HasValue)
            reward.IsEnabled = request.IsEnabled.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDetail(reward));
    }

    public async Task<Result> DeleteAsync(
        string broadcasterId,
        string rewardId,
        CancellationToken cancellationToken = default
    )
    {
        if (!Guid.TryParse(rewardId, out var guid))
            return Result.Failure($"Invalid reward ID '{rewardId}'.", "VALIDATION_FAILED");

        var reward = await _db.Rewards.FirstOrDefaultAsync(
            r => r.Id == guid && r.BroadcasterId == broadcasterId,
            cancellationToken
        );

        if (reward is null)
            return Result.Failure($"Reward '{rewardId}' was not found.", "NOT_FOUND");

        _db.Rewards.Remove(reward);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PagedList<RewardListItem>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    )
    {
        var query = _db.Rewards.Where(r => r.BroadcasterId == broadcasterId);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Title)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(r => new RewardListItem(
                r.Id.ToString(),
                r.Title,
                0,
                r.IsEnabled,
                null,
                null,
                r.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(
            new PagedList<RewardListItem>(items, total, pagination.Page, pagination.PageSize)
        );
    }

    public async Task<Result<RewardDetail>> GetAsync(
        string broadcasterId,
        string rewardId,
        CancellationToken cancellationToken = default
    )
    {
        if (!Guid.TryParse(rewardId, out var guid))
            return Result.Failure<RewardDetail>(
                $"Invalid reward ID '{rewardId}'.",
                "VALIDATION_FAILED"
            );

        var reward = await _db.Rewards.FirstOrDefaultAsync(
            r => r.Id == guid && r.BroadcasterId == broadcasterId,
            cancellationToken
        );

        if (reward is null)
            return Errors.NotFound<RewardDetail>("Reward", rewardId);

        return Result.Success(ToDetail(reward));
    }

    public Task<Result> SyncWithTwitchAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        // Twitch sync requires an external Twitch API client — not available in this layer.
        // Return not-implemented so callers can handle gracefully.
        return Task.FromResult(
            Result.Failure("Twitch sync is not yet implemented.", "SERVICE_UNAVAILABLE")
        );
    }

    private static RewardDetail ToDetail(Reward r) =>
        new(
            r.Id.ToString(),
            r.Title,
            r.Description,
            0,
            r.IsEnabled,
            false,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            r.CreatedAt,
            r.UpdatedAt
        );
}
