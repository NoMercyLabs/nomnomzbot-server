// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.EventResponses;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/event-responses")]
[Authorize]
[Tags("EventResponses")]
public class EventResponsesController : BaseController
{
    private readonly IEventResponseService _eventResponseService;

    public EventResponsesController(IEventResponseService eventResponseService)
    {
        _eventResponseService = eventResponseService;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<EventResponseListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListEventResponses(
        string channelId,
        [FromQuery] PageRequestDto request,
        CancellationToken ct
    )
    {
        PaginationParams pagination = new(
            request.Page,
            request.Take,
            request.Sort,
            request.Order
        );
        Result<PagedList<EventResponseListItem>> result = await _eventResponseService.ListAsync(channelId, pagination, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpGet("{eventType}")]
    [ProducesResponseType<StatusResponseDto<EventResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventResponse(
        string channelId,
        string eventType,
        CancellationToken ct
    )
    {
        Result<EventResponseDto> result = await _eventResponseService.GetByEventTypeAsync(channelId, eventType, ct);
        return ResultResponse(result);
    }

    [HttpPut("{eventType}")]
    [ProducesResponseType<StatusResponseDto<EventResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertEventResponse(
        string channelId,
        string eventType,
        [FromBody] UpdateEventResponseDto request,
        CancellationToken ct
    )
    {
        Result<EventResponseDto> result = await _eventResponseService.UpsertAsync(channelId, eventType, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<EventResponseDto> { Data = result.Value });
    }

    [HttpDelete("{eventType}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEventResponse(
        string channelId,
        string eventType,
        CancellationToken ct
    )
    {
        Result result = await _eventResponseService.DeleteAsync(channelId, eventType, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return NoContent();
    }
}
