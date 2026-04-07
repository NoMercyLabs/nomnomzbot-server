// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Tts;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/tts")]
[Authorize]
[Tags("TTS")]
public class TtsController : BaseController
{
    private readonly ITtsConfigService _ttsService;
    private readonly IApplicationDbContext _db;

    public TtsController(ITtsConfigService ttsService, IApplicationDbContext db)
    {
        _ttsService = ttsService;
        _db = db;
    }

    // ── Get TTS config ───────────────────────────────────────────────────────

    [HttpGet("config")]
    [ProducesResponseType<StatusResponseDto<TtsConfigDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig(string channelId, CancellationToken ct)
    {
        Result<TtsConfigDto> result = await _ttsService.GetConfigAsync(channelId, ct);
        return ResultResponse(result);
    }

    // ── Update TTS config ────────────────────────────────────────────────────

    [HttpPut("config")]
    [ProducesResponseType<StatusResponseDto<TtsConfigDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConfig(
        string channelId,
        [FromBody] UpdateTtsConfigDto request,
        CancellationToken ct
    )
    {
        Result<TtsConfigDto> result = await _ttsService.UpdateConfigAsync(channelId, request, ct);
        return ResultResponse(result);
    }

    // ── List available voices ────────────────────────────────────────────────

    [HttpGet("voices")]
    [ProducesResponseType<StatusResponseDto<IReadOnlyList<TtsVoiceDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVoices(CancellationToken ct)
    {
        Result<IReadOnlyList<TtsVoiceDto>> result = await _ttsService.GetVoicesAsync(ct);
        return ResultResponse(result);
    }

    // ── Test a voice ─────────────────────────────────────────────────────────

    [HttpPost("test")]
    [ProducesResponseType<StatusResponseDto<TtsTestResultDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestVoice(
        string channelId,
        [FromBody] TtsTestRequestDto request,
        CancellationToken ct
    )
    {
        Result<TtsTestResultDto> result = await _ttsService.TestVoiceAsync(channelId, request, ct);
        return ResultResponse(result);
    }
}
