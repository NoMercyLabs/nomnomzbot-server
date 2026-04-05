// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Application.Features.Features.Queries.GetFeatures;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/features")]
[Authorize]
public class FeaturesController : BaseController
{
    private readonly GetFeaturesQueryHandler _getFeatures;

    public FeaturesController(GetFeaturesQueryHandler getFeatures)
    {
        _getFeatures = getFeatures;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeatures(string channelId, CancellationToken ct)
    {
        var result = await _getFeatures.HandleAsync(channelId, ct);
        return ResultResponse(result);
    }
}
