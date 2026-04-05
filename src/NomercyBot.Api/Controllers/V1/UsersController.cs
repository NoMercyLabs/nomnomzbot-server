// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Users;
using NoMercyBot.Application.Services;
using NoMercyBot.Infrastructure.Services.Application;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
[Tags("Users")]
public class UsersController : BaseController
{
    private readonly IUserService _userService;
    private readonly GdprService _gdpr;

    public UsersController(IUserService userService, GdprService gdpr)
    {
        _userService = userService;
        _gdpr = gdpr;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<UserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? query,
        [FromQuery] PageRequestDto request,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequestResponse("A search query is required.");

        var pagination = new PaginationParams(
            request.Page,
            request.Take,
            request.Sort,
            request.Order
        );
        var result = await _userService.SearchAsync(query, pagination, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpGet("{userId}")]
    [ProducesResponseType<StatusResponseDto<UserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(string userId, CancellationToken ct)
    {
        var result = await _userService.GetAsync(userId, ct);
        return ResultResponse(result);
    }

    [HttpGet("{userId}/profile")]
    [ProducesResponseType<StatusResponseDto<UserProfileDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserProfile(string userId, CancellationToken ct)
    {
        var result = await _userService.GetProfileAsync(userId, ct);
        return ResultResponse(result);
    }

    [HttpPut("{userId}/profile")]
    [ProducesResponseType<StatusResponseDto<UserProfileDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUserProfile(
        string userId,
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct
    )
    {
        var result = await _userService.UpdateProfileAsync(userId, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<UserProfileDto> { Data = result.Value });
    }

    // ─── GDPR endpoints ───────────────────────────────────────────────────────

    /// <summary>
    /// Export all personal data for the specified user (GDPR right of access).
    /// Returns a JSON file download containing all data we hold for this user.
    /// </summary>
    [HttpGet("{userId}/data-export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportUserData(string userId, CancellationToken ct)
    {
        // Only the user themselves or admins may export
        var callerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (callerId != userId)
            return UnauthorizedResponse("You may only export your own data.");

        var result = await _gdpr.ExportUserDataAsync(userId, ct);
        if (result.IsFailure)
            return ResultResponse(result);

        var bytes = System.Text.Encoding.UTF8.GetBytes(result.Value);
        return File(
            bytes,
            "application/json",
            $"user-data-export-{userId}-{DateTime.UtcNow:yyyyMMdd}.json"
        );
    }

    /// <summary>
    /// Delete all personal data for the specified user (GDPR right to erasure).
    /// This action is irreversible.
    /// </summary>
    [HttpDelete("{userId}/data")]
    [ProducesResponseType<StatusResponseDto<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUserData(string userId, CancellationToken ct)
    {
        var callerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (callerId != userId)
            return UnauthorizedResponse("You may only delete your own data.");

        var result = await _gdpr.DeleteUserDataAsync(userId, ct);
        return ResultResponse(result);
    }
}
