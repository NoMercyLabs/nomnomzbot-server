// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Pipelines;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/pipelines")]
[Authorize]
[Tags("Pipelines")]
public class PipelinesController : BaseController
{
    private readonly IPipelineService _pipelineService;

    public PipelinesController(IPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<PipelineListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPipelines(
        string channelId,
        [FromQuery] PageRequestDto request,
        CancellationToken ct
    )
    {
        PaginationParams pagination = new(request.Page, request.Take, request.Sort, request.Order);
        Result<PagedList<PipelineListItemDto>> result = await _pipelineService.ListAsync(channelId, pagination, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<StatusResponseDto<PipelineDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipeline(string channelId, int id, CancellationToken ct)
    {
        Result<PipelineDto> result = await _pipelineService.GetAsync(channelId, id, ct);
        return ResultResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<StatusResponseDto<PipelineDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePipeline(
        string channelId,
        [FromBody] CreatePipelineDto request,
        CancellationToken ct
    )
    {
        Result<PipelineDto> result = await _pipelineService.CreateAsync(channelId, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);

        return CreatedAtAction(
            nameof(GetPipeline),
            new { channelId, id = result.Value.Id },
            new StatusResponseDto<PipelineDto>
            {
                Data = result.Value,
                Message = "Pipeline created successfully.",
            }
        );
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<StatusResponseDto<PipelineDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePipeline(
        string channelId,
        int id,
        [FromBody] UpdatePipelineDto request,
        CancellationToken ct
    )
    {
        Result<PipelineDto> result = await _pipelineService.UpdateAsync(channelId, id, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<PipelineDto> { Data = result.Value });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePipeline(string channelId, int id, CancellationToken ct)
    {
        Result result = await _pipelineService.DeleteAsync(channelId, id, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return NoContent();
    }
}
