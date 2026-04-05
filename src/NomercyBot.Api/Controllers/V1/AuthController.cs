// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Application.Features.Auth.Queries.GetCurrentUser;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : BaseController
{
    private readonly GetCurrentUserQueryHandler _getCurrentUser;

    public AuthController(GetCurrentUserQueryHandler getCurrentUser)
    {
        _getCurrentUser = getCurrentUser;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var result = await _getCurrentUser.HandleAsync(ct);
        return ResultResponse(result);
    }
}
