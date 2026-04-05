// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Timers;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/timers")]
[Authorize]
[Tags("Timers")]
public class TimersController : BaseController
{
    private readonly ITimerManagementService _timerService;

    public TimersController(ITimerManagementService timerService)
    {
        _timerService = timerService;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<TimerListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTimers(
        string channelId,
        [FromQuery] PageRequestDto request,
        CancellationToken ct
    )
    {
        var pagination = new PaginationParams(
            request.Page,
            request.Take,
            request.Sort,
            request.Order
        );
        var result = await _timerService.ListAsync(channelId, pagination, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<StatusResponseDto<TimerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimer(string channelId, int id, CancellationToken ct)
    {
        var result = await _timerService.GetAsync(channelId, id, ct);
        return ResultResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<StatusResponseDto<TimerDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTimer(
        string channelId,
        [FromBody] CreateTimerDto request,
        CancellationToken ct
    )
    {
        var result = await _timerService.CreateAsync(channelId, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);

        return CreatedAtAction(
            nameof(GetTimer),
            new { channelId, id = result.Value.Id },
            new StatusResponseDto<TimerDto>
            {
                Data = result.Value,
                Message = "Timer created successfully.",
            }
        );
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<StatusResponseDto<TimerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTimer(
        string channelId,
        int id,
        [FromBody] UpdateTimerDto request,
        CancellationToken ct
    )
    {
        var result = await _timerService.UpdateAsync(channelId, id, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<TimerDto> { Data = result.Value });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTimer(string channelId, int id, CancellationToken ct)
    {
        var result = await _timerService.DeleteAsync(channelId, id, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return NoContent();
    }

    [HttpPost("{id:int}/toggle")]
    [ProducesResponseType<StatusResponseDto<TimerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleTimer(string channelId, int id, CancellationToken ct)
    {
        var result = await _timerService.ToggleAsync(channelId, id, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<TimerDto> { Data = result.Value });
    }
}
