// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Users;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? query,
        [FromQuery] PageRequestDto request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequestResponse("A search query is required.");

        var pagination = new PaginationParams(request.Page, request.Take, request.Sort, request.Order);
        var result = await _userService.SearchAsync(query, pagination, ct);
        if (result.IsFailure) return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId, CancellationToken ct)
    {
        var result = await _userService.GetAsync(userId, ct);
        return ResultResponse(result);
    }

    [HttpGet("{userId}/profile")]
    public async Task<IActionResult> GetUserProfile(string userId, CancellationToken ct)
    {
        var result = await _userService.GetProfileAsync(userId, ct);
        return ResultResponse(result);
    }

    [HttpPut("{userId}/profile")]
    public async Task<IActionResult> UpdateUserProfile(
        string userId,
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct)
    {
        var result = await _userService.UpdateProfileAsync(userId, request, ct);
        if (result.IsFailure) return ResultResponse(result);
        return Ok(new StatusResponseDto<UserProfileDto> { Data = result.Value });
    }
}
