// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>
/// Handles channel point reward redemptions.
/// Looks up the Reward entity by its Twitch reward ID and executes the
/// configured PipelineJson. If the reward has a simple Response text,
/// that is used directly. Falls back to the generic "reward_redeemed"
/// event_response Record if no specific reward pipeline is configured.
/// </summary>
public sealed class RewardRedeemedHandler : IEventHandler<RewardRedeemedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPipelineEngine _pipeline;
    private readonly ILogger<RewardRedeemedHandler> _logger;

    public RewardRedeemedHandler(
        IServiceScopeFactory scopeFactory,
        IPipelineEngine pipeline,
        ILogger<RewardRedeemedHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task HandleAsync(RewardRedeemedEvent @event, CancellationToken cancellationToken = default)
    {
        var broadcasterId = @event.BroadcasterId;
        if (string.IsNullOrEmpty(broadcasterId)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = @event.UserDisplayName,
            ["user.id"] = @event.UserId,
            ["reward"] = @event.RewardTitle,
            ["reward.id"] = @event.RewardId,
            ["redemption.id"] = @event.RedemptionId,
            ["cost"] = @event.Cost.ToString(),
            ["input"] = @event.UserInput ?? string.Empty,
        };

        // Try to find a Reward entity with a pipeline or response configured
        // Rewards are matched by their Twitch reward ID stored in the RewardId field.
        // The Reward.Id is a Guid (internal); the Twitch reward ID comes from the event.
        // We match by searching for a reward whose title matches or by scanning platform rewards.
        // For now, search Records with RecordType = "reward:{rewardId}" for the pipeline.
        var rewardConfig = await db.Records
            .FirstOrDefaultAsync(r =>
                r.BroadcasterId == broadcasterId
                && r.RecordType == $"reward:{@event.RewardId}", cancellationToken);

        if (rewardConfig is not null && !string.IsNullOrWhiteSpace(rewardConfig.Data))
        {
            _logger.LogDebug("Executing reward pipeline for reward {RewardId} in channel {Channel}",
                @event.RewardId, broadcasterId);

            await ExecutePipelineAsync(broadcasterId, rewardConfig.Data,
                @event.UserId, @event.UserDisplayName,
                @event.RedemptionId, @event.RewardId,
                variables, cancellationToken);
            return;
        }

        // Fall back to generic event_response:reward_redeemed
        var genericConfig = await db.Records
            .FirstOrDefaultAsync(r =>
                r.BroadcasterId == broadcasterId
                && r.RecordType == "event_response:reward_redeemed", cancellationToken);

        if (genericConfig is null || string.IsNullOrWhiteSpace(genericConfig.Data)) return;

        _logger.LogDebug("Executing generic reward_redeemed pipeline for channel {Channel}", broadcasterId);
        await ExecutePipelineAsync(broadcasterId, genericConfig.Data,
            @event.UserId, @event.UserDisplayName,
            @event.RedemptionId, @event.RewardId,
            variables, cancellationToken);
    }

    private async Task ExecutePipelineAsync(
        string broadcasterId,
        string pipelineJson,
        string userId,
        string displayName,
        string redemptionId,
        string rewardId,
        Dictionary<string, string> variables,
        CancellationToken ct)
    {
        try
        {
            await _pipeline.ExecuteAsync(new PipelineRequest
            {
                BroadcasterId = broadcasterId,
                PipelineJson = pipelineJson,
                TriggeredByUserId = userId,
                TriggeredByDisplayName = displayName,
                RedemptionId = redemptionId,
                RewardId = rewardId,
                RawMessage = string.Empty,
                InitialVariables = variables,
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute reward pipeline in channel {Channel}", broadcasterId);
        }
    }
}
