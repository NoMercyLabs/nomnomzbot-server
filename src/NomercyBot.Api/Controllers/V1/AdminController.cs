// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Admin;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = "admin")]
[Tags("Admin")]
public class AdminController : BaseController
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>Returns aggregate statistics for the admin dashboard.</summary>
    [HttpGet("stats")]
    [ProducesResponseType<StatusResponseDto<AdminStatsDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminStats(CancellationToken ct)
    {
        Result<AdminStatsDto> result = await _adminService.GetStatsAsync(ct);
        return ResultResponse(result);
    }

    /// <summary>Returns all channels with their current status.</summary>
    [HttpGet("channels")]
    [ProducesResponseType<PaginatedResponse<AdminChannelDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChannels(
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
        Result<PagedList<AdminChannelDto>> result = await _adminService.ListChannelsAsync(
            pagination,
            ct
        );
        if (result.IsFailure)
            return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    /// <summary>Returns system health and process metrics.</summary>
    [HttpGet("system")]
    [ProducesResponseType<StatusResponseDto<AdminSystemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemHealth(CancellationToken ct)
    {
        Result<AdminSystemDto> result = await _adminService.GetSystemHealthAsync(ct);
        return ResultResponse(result);
    }
}
