// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController : BaseController
{
    [HttpGet]
    public IActionResult SearchUsers([FromQuery] string? query) => Ok(new { data = Array.Empty<object>() });

    [HttpGet("{userId}")]
    public IActionResult GetUser(string userId) => NotFoundResponse();
}
