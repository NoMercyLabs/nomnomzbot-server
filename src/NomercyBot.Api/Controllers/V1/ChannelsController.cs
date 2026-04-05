// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Application.Features.Channels.Queries.GetChannel;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels")]
[Authorize]
public class ChannelsController : BaseController
{
    private readonly GetChannelQueryHandler _getChannel;

    public ChannelsController(GetChannelQueryHandler getChannel)
    {
        _getChannel = getChannel;
    }

    [HttpGet("{channelId}")]
    public async Task<IActionResult> GetChannel(string channelId, CancellationToken ct)
    {
        var result = await _getChannel.HandleAsync(new GetChannelQuery(channelId), ct);
        return ResultResponse(result);
    }
}
