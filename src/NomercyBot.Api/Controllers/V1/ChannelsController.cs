// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Channels;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels")]
[Authorize]
[Tags("Channels")]
public class ChannelsController : BaseController
{
    private readonly IChannelService _channelService;

    public ChannelsController(IChannelService channelService)
    {
        _channelService = channelService;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<ChannelDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChannels(
        [FromQuery] PageRequestDto request,
        CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return UnauthenticatedResponse();

        var pagination = new PaginationParams(request.Page, request.Take, request.Sort, request.Order);
        var result = await _channelService.GetChannelsAsync(userId, pagination, ct);
        if (result.IsFailure) return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpGet("{channelId}")]
    [ProducesResponseType<StatusResponseDto<ChannelDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChannel(string channelId, CancellationToken ct)
    {
        var result = await _channelService.GetAsync(channelId, ct);
        return ResultResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<StatusResponseDto<ChannelDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> OnboardChannel(
        [FromBody] CreateChannelRequest request,
        CancellationToken ct)
    {
        var result = await _channelService.OnboardAsync(request.BroadcasterId, request, ct);
        if (result.IsFailure) return ResultResponse(result);

        return CreatedAtAction(nameof(GetChannel), new { channelId = result.Value.Id },
            new StatusResponseDto<ChannelDto> { Data = result.Value, Message = "Channel onboarded successfully." });
    }

    [HttpPut("{channelId}")]
    [ProducesResponseType<StatusResponseDto<ChannelDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateChannelSettings(
        string channelId,
        [FromBody] UpdateChannelSettingsDto request,
        CancellationToken ct)
    {
        var result = await _channelService.UpdateSettingsAsync(channelId, request, ct);
        if (result.IsFailure) return ResultResponse(result);
        return Ok(new StatusResponseDto<ChannelDto> { Data = result.Value });
    }

    [HttpPost("{channelId}/join")]
    [ProducesResponseType<StatusResponseDto<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> JoinChannel(string channelId, CancellationToken ct)
    {
        var result = await _channelService.JoinAsync(channelId, ct);
        if (result.IsFailure) return ResultResponse(result);
        return Ok(new StatusResponseDto<object> { Message = "Bot joined channel." });
    }

    [HttpPost("{channelId}/leave")]
    [ProducesResponseType<StatusResponseDto<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> LeaveChannel(string channelId, CancellationToken ct)
    {
        var result = await _channelService.LeaveAsync(channelId, ct);
        if (result.IsFailure) return ResultResponse(result);
        return Ok(new StatusResponseDto<object> { Message = "Bot left channel." });
    }

    [HttpDelete("{channelId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteChannel(string channelId, CancellationToken ct)
    {
        var result = await _channelService.DeleteAsync(channelId, ct);
        if (result.IsFailure) return ResultResponse(result);
        return NoContent();
    }
}
