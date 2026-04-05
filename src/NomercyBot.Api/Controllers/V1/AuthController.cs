// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Features.Auth.Queries.GetCurrentUser;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Tags("Auth")]
public class AuthController : BaseController
{
    private readonly GetCurrentUserQueryHandler _getCurrentUser;

    public AuthController(GetCurrentUserQueryHandler getCurrentUser)
    {
        _getCurrentUser = getCurrentUser;
    }

    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<StatusResponseDto<CurrentUserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var result = await _getCurrentUser.HandleAsync(ct);
        return ResultResponse(result);
    }
}
