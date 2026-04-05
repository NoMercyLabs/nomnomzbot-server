// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/rewards")]
[Authorize]
public class RewardsController : BaseController
{
    [HttpGet]
    public IActionResult GetRewards(string channelId) => Ok(new { data = Array.Empty<object>() });

    [HttpGet("{rewardId}")]
    public IActionResult GetReward(string channelId, string rewardId) => NotFoundResponse();

    [HttpPost]
    public IActionResult CreateReward(string channelId, [FromBody] object request) => StatusCode(501);

    [HttpPut("{rewardId}")]
    public IActionResult UpdateReward(string channelId, string rewardId, [FromBody] object request) => StatusCode(501);

    [HttpDelete("{rewardId}")]
    public IActionResult DeleteReward(string channelId, string rewardId) => StatusCode(501);
}
