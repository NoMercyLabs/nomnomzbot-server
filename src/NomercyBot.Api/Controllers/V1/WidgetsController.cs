// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/widgets")]
[Authorize]
public class WidgetsController : BaseController
{
    [HttpGet]
    public IActionResult GetWidgets(string channelId) => Ok(new { data = Array.Empty<object>() });

    [HttpGet("{widgetId}")]
    public IActionResult GetWidget(string channelId, string widgetId) => NotFoundResponse();

    [HttpPost]
    public IActionResult CreateWidget(string channelId, [FromBody] object request) => StatusCode(501);

    [HttpPut("{widgetId}")]
    public IActionResult UpdateWidget(string channelId, string widgetId, [FromBody] object request) => StatusCode(501);

    [HttpDelete("{widgetId}")]
    public IActionResult DeleteWidget(string channelId, string widgetId) => StatusCode(501);
}
