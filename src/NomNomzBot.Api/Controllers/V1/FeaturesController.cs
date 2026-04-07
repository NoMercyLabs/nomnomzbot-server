// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.Features.Features.Commands.ToggleFeature;
using NoMercyBot.Application.Features.Features.Queries.GetFeatures;
using FeatureStatusDto = NoMercyBot.Application.Features.Features.Queries.GetFeatures.FeatureStatusDto;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/features")]
[Authorize]
[Tags("Features")]
public class FeaturesController : BaseController
{
    private readonly GetFeaturesQueryHandler _getFeatures;
    private readonly ToggleFeatureCommandHandler _toggleFeature;

    public FeaturesController(
        GetFeaturesQueryHandler getFeatures,
        ToggleFeatureCommandHandler toggleFeature
    )
    {
        _getFeatures = getFeatures;
        _toggleFeature = toggleFeature;
    }

    [HttpGet]
    [ProducesResponseType<StatusResponseDto<List<FeatureStatusDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatures(string channelId, CancellationToken ct)
    {
        Result<List<FeatureStatusDto>> result = await _getFeatures.HandleAsync(channelId, ct);
        return ResultResponse(result);
    }

    [HttpPost("{featureKey}/toggle")]
    [ProducesResponseType<StatusResponseDto<FeatureStatusDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleFeature(
        string channelId,
        string featureKey,
        CancellationToken ct
    )
    {
        Result<FeatureStatusDto> result = await _toggleFeature.HandleAsync(
            channelId,
            featureKey,
            ct
        );
        return ResultResponse(result);
    }
}
