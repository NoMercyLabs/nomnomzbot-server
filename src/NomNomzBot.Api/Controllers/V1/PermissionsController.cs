// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/permissions")]
[Authorize]
[Tags("Permissions")]
public class PermissionsController : BaseController
{
    private readonly IPermissionService _permissionService;
    private readonly IApplicationDbContext _db;

    public PermissionsController(IPermissionService permissionService, IApplicationDbContext db)
    {
        _permissionService = permissionService;
        _db = db;
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    public record PermissionDto(
        int Id,
        string SubjectType,
        string SubjectId,
        string? SubjectName,
        string ResourceType,
        string? ResourceId,
        string PermissionValue,
        DateTime CreatedAt
    );

    public record GrantPermissionRequest(
        string UserId,
        string Permission
    );

    // ── List permissions ─────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType<StatusResponseDto<List<PermissionDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPermissions(string channelId, CancellationToken ct)
    {
        var permissions = await _db.Permissions
            .Where(p => p.BroadcasterId == channelId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var userIds = permissions
            .Where(p => p.SubjectType == "user")
            .Select(p => p.SubjectId)
            .Distinct()
            .ToList();

        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var result = permissions.Select(p =>
        {
            string? subjectName = null;
            if (p.SubjectType == "user" && users.TryGetValue(p.SubjectId, out var user))
                subjectName = user.DisplayName;

            return new PermissionDto(
                p.Id,
                p.SubjectType,
                p.SubjectId,
                subjectName,
                p.ResourceType,
                p.ResourceId,
                p.PermissionValue,
                p.CreatedAt
            );
        }).ToList();

        return Ok(new StatusResponseDto<List<PermissionDto>> { Data = result });
    }

    // ── Grant permission ─────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType<StatusResponseDto<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GrantPermission(
        string channelId,
        [FromBody] GrantPermissionRequest request,
        CancellationToken ct
    )
    {
        Result grantResult = await _permissionService.GrantAsync(
            channelId,
            request.UserId,
            request.Permission,
            ct
        );

        return ResultResponse(grantResult);
    }

    // ── Revoke permission ────────────────────────────────────────────────────

    [HttpDelete("{userId}")]
    [ProducesResponseType<StatusResponseDto<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokePermission(
        string channelId,
        string userId,
        [FromQuery] string permission,
        CancellationToken ct
    )
    {
        Result revokeResult = await _permissionService.RevokeAsync(
            channelId,
            userId,
            permission,
            ct
        );

        return ResultResponse(revokeResult);
    }
}
