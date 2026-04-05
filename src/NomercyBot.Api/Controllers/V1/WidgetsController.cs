// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Widgets;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/widgets")]
[Authorize]
[Tags("Widgets")]
public class WidgetsController : BaseController
{
    private readonly IWidgetService _widgetService;

    public WidgetsController(IWidgetService widgetService)
    {
        _widgetService = widgetService;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<WidgetDetail>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListWidgets(
        string channelId,
        [FromQuery] PageRequestDto request,
        CancellationToken ct)
    {
        var pagination = new PaginationParams(request.Page, request.Take, request.Sort, request.Order);
        var result = await _widgetService.ListAsync(channelId, pagination, ct);
        if (result.IsFailure) return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpGet("{widgetId}")]
    [ProducesResponseType<StatusResponseDto<WidgetDetail>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWidget(string channelId, string widgetId, CancellationToken ct)
    {
        var result = await _widgetService.GetAsync(channelId, widgetId, ct);
        return ResultResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<StatusResponseDto<WidgetDetail>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWidget(
        string channelId,
        [FromBody] CreateWidgetRequest request,
        CancellationToken ct)
    {
        var result = await _widgetService.CreateAsync(channelId, request, ct);
        if (result.IsFailure) return ResultResponse(result);

        return CreatedAtAction(nameof(GetWidget), new { channelId, widgetId = result.Value.Id },
            new StatusResponseDto<WidgetDetail> { Data = result.Value, Message = "Widget created successfully." });
    }

    [HttpPut("{widgetId}")]
    [ProducesResponseType<StatusResponseDto<WidgetDetail>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateWidget(
        string channelId,
        string widgetId,
        [FromBody] UpdateWidgetRequest request,
        CancellationToken ct)
    {
        var result = await _widgetService.UpdateAsync(channelId, widgetId, request, ct);
        if (result.IsFailure) return ResultResponse(result);
        return Ok(new StatusResponseDto<WidgetDetail> { Data = result.Value });
    }

    [HttpDelete("{widgetId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteWidget(string channelId, string widgetId, CancellationToken ct)
    {
        var result = await _widgetService.DeleteAsync(channelId, widgetId, ct);
        if (result.IsFailure) return ResultResponse(result);
        return NoContent();
    }
}
