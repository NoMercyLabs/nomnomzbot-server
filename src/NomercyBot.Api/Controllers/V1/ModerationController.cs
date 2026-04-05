// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/moderation")]
[Authorize]
public class ModerationController : BaseController
{
    [HttpGet("rules")]
    public IActionResult GetRules(string channelId) => Ok(new { data = Array.Empty<object>() });

    [HttpGet("rules/{ruleId}")]
    public IActionResult GetRule(string channelId, int ruleId) => NotFoundResponse();

    [HttpPost("rules")]
    public IActionResult CreateRule(string channelId, [FromBody] object request) => StatusCode(501);

    [HttpPut("rules/{ruleId}")]
    public IActionResult UpdateRule(string channelId, int ruleId, [FromBody] object request) => StatusCode(501);

    [HttpDelete("rules/{ruleId}")]
    public IActionResult DeleteRule(string channelId, int ruleId) => StatusCode(501);

    [HttpPost("actions")]
    public IActionResult PerformAction(string channelId, [FromBody] object request) => StatusCode(501);
}
