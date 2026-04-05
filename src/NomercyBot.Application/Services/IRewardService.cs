using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Rewards;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Application service for managing channel point rewards and their actions.
/// </summary>
public interface IRewardService
{
    /// <summary>Create a new reward.</summary>
    Task<Result<RewardDetail>> CreateAsync(
        string broadcasterId,
        CreateRewardRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Update an existing reward.</summary>
    Task<Result<RewardDetail>> UpdateAsync(
        string broadcasterId,
        string rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Delete a reward.</summary>
    Task<Result> DeleteAsync(
        string broadcasterId,
        string rewardId,
        CancellationToken cancellationToken = default
    );

    /// <summary>List all rewards for a channel with pagination.</summary>
    Task<Result<PagedList<RewardListItem>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get a single reward by ID.</summary>
    Task<Result<RewardDetail>> GetAsync(
        string broadcasterId,
        string rewardId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Sync local rewards with Twitch channel point rewards.</summary>
    Task<Result> SyncWithTwitchAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    );
}
