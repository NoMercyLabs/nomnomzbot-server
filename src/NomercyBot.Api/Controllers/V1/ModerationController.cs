// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Moderation;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/moderation")]
[Authorize]
[Tags("Moderation")]
public class ModerationController : BaseController
{
    private readonly IModerationService _moderationService;

    public ModerationController(IModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    // ─── Rules ───────────────────────────────────────────────────────────────

    [HttpGet("rules")]
    [ProducesResponseType<PaginatedResponse<ModerationRuleDetail>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRules(
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
        var result = await _moderationService.ListRulesAsync(channelId, pagination, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpPost("rules")]
    [ProducesResponseType<StatusResponseDto<ModerationRuleDetail>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRule(
        string channelId,
        [FromBody] CreateModerationRuleRequest request,
        CancellationToken ct
    )
    {
        var result = await _moderationService.CreateRuleAsync(channelId, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);

        return CreatedAtAction(
            nameof(ListRules),
            new { channelId },
            new StatusResponseDto<ModerationRuleDetail>
            {
                Data = result.Value,
                Message = "Rule created successfully.",
            }
        );
    }

    [HttpPut("rules/{ruleId:int}")]
    [ProducesResponseType<StatusResponseDto<ModerationRuleDetail>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRule(
        string channelId,
        int ruleId,
        [FromBody] UpdateModerationRuleRequest request,
        CancellationToken ct
    )
    {
        var result = await _moderationService.UpdateRuleAsync(channelId, ruleId, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<ModerationRuleDetail> { Data = result.Value });
    }

    [HttpDelete("rules/{ruleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRule(string channelId, int ruleId, CancellationToken ct)
    {
        var result = await _moderationService.DeleteRuleAsync(channelId, ruleId, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return NoContent();
    }

    // ─── Actions ─────────────────────────────────────────────────────────────

    [HttpGet("actions")]
    [ProducesResponseType<PaginatedResponse<ModerationActionResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActions(
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
        var result = await _moderationService.GetActionsAsync(channelId, pagination, ct);
        if (result.IsFailure)
            return ResultResponse(result);
        return GetPaginatedResponse(result.Value, request);
    }

    [HttpPost("actions")]
    [ProducesResponseType<StatusResponseDto<ModerationActionResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PerformAction(
        string channelId,
        [FromBody] PerformModerationActionRequest request,
        CancellationToken ct
    )
    {
        Result<ModerationActionResult> result = request.Action switch
        {
            "timeout" => await _moderationService.TimeoutAsync(
                channelId,
                request.TargetUserId,
                request.DurationSeconds ?? 600,
                request.Reason,
                ct
            ),

            "ban" => await _moderationService.BanAsync(
                channelId,
                request.TargetUserId,
                request.Reason,
                ct
            ),

            "unban" => await _moderationService.UnbanAsync(channelId, request.TargetUserId, ct),

            _ => Result.Failure<ModerationActionResult>(
                $"Unknown action '{request.Action}'. Supported: timeout, ban, unban.",
                "VALIDATION_FAILED"
            ),
        };

        if (result.IsFailure)
            return ResultResponse(result);
        return Ok(new StatusResponseDto<ModerationActionResult> { Data = result.Value });
    }
}
