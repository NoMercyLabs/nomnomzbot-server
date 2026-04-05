// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Rewards;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/rewards")]
[Authorize]
[Tags("Rewards")]
public class RewardsController : BaseController
{
    private readonly IRewardService _rewardService;

    public RewardsController(IRewardService rewardService)
    {
        _rewardService = rewardService;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<RewardDetail>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRewards(
        string channelId,
        [FromQuery] PageRequestDto request,
        CancellationToken ct
    )
    {
        PaginationParams pagination = new(
            request.Page,
            request.Take,
            request.Sort,
            request.Order
        );
        Result<PagedList<RewardListItem>> result = await _rewardService.ListAsync(channelId, pagination, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpGet("{rewardId}")]
    [ProducesResponseType<StatusResponseDto<RewardDetail>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReward(
        string channelId,
        string rewardId,
        CancellationToken ct
    )
    {
        Result<RewardDetail> result = await _rewardService.GetAsync(channelId, rewardId, ct);
        return ResultResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<StatusResponseDto<RewardDetail>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReward(
        string channelId,
        [FromBody] CreateRewardRequest request,
        CancellationToken ct
    )
    {
        Result<RewardDetail> result = await _rewardService.CreateAsync(channelId, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);

        return CreatedAtAction(
            nameof(GetReward),
            new { channelId, rewardId = result.Value.Id },
            new StatusResponseDto<RewardDetail>
            {
                Data = result.Value,
                Message = "Reward created successfully.",
            }
        );
    }

    [HttpPut("{rewardId}")]
    [ProducesResponseType<StatusResponseDto<RewardDetail>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateReward(
        string channelId,
        string rewardId,
        [FromBody] UpdateRewardRequest request,
        CancellationToken ct
    )
    {
        Result<RewardDetail> result = await _rewardService.UpdateAsync(channelId, rewardId, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<RewardDetail> { Data = result.Value });
    }

    [HttpDelete("{rewardId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteReward(
        string channelId,
        string rewardId,
        CancellationToken ct
    )
    {
        Result result = await _rewardService.DeleteAsync(channelId, rewardId, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return NoContent();
    }

    [HttpPost("sync")]
    [ProducesResponseType<StatusResponseDto<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncRewards(string channelId, CancellationToken ct)
    {
        Result result = await _rewardService.SyncWithTwitchAsync(channelId, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<object> { Message = "Rewards synced with Twitch." });
    }
}
