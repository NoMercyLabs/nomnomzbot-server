// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Channels;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
[Tags("Dashboard")]
public class DashboardController : BaseController
{
    private readonly IChannelRegistry _registry;
    private readonly IChannelService _channelService;

    public DashboardController(IChannelRegistry registry, IChannelService channelService)
    {
        _registry = registry;
        _channelService = channelService;
    }

    /// <summary>
    /// Returns a live stats snapshot for the given channel.
    /// Uses the in-memory ChannelContext when the bot is connected; falls back to DB otherwise.
    /// </summary>
    [HttpGet("{broadcasterId}/stats")]
    [ProducesResponseType<StatusResponseDto<DashboardStatsDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(string broadcasterId, CancellationToken ct)
    {
        ChannelContext? ctx = _registry.Get(broadcasterId);

        if (ctx is not null)
        {
            long? uptime =
                ctx.IsLive && ctx.WentLiveAt.HasValue
                    ? (long)(DateTimeOffset.UtcNow - ctx.WentLiveAt.Value).TotalSeconds
                    : null;

            DashboardStatsDto stats = new()
            {
                IsLive = ctx.IsLive,
                StreamTitle = ctx.CurrentTitle,
                GameName = ctx.CurrentGame,
                ViewerCount = 0,
                FollowerCount = 0,
                CommandsUsed = ctx.CommandsUsed,
                MessagesCount = ctx.MessageCount,
                Uptime = uptime,
            };

            return Ok(new StatusResponseDto<DashboardStatsDto> { Data = stats });
        }

        // Channel not currently active in registry — fall back to DB for basic info.
        Result<ChannelDto> result = await _channelService.GetAsync(broadcasterId, ct);
        if (result.IsFailure)
            return ResultResponse(result);

        ChannelDto channel = result.Value;
        DashboardStatsDto fallback = new()
        {
            IsLive = channel.IsLive,
            StreamTitle = channel.Title,
            GameName = channel.GameName,
            ViewerCount = channel.ViewerCount ?? 0,
            FollowerCount = 0,
            CommandsUsed = 0,
            MessagesCount = 0,
            Uptime = null,
        };

        return Ok(new StatusResponseDto<DashboardStatsDto> { Data = fallback });
    }
}
