// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Admin;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Infrastructure.Services.Application;

public sealed class AdminService : IAdminService
{
    private readonly IApplicationDbContext _db;

    public AdminService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AdminStatsDto>> GetStatsAsync(CancellationToken ct = default)
    {
        int totalChannels = await _db.Channels.CountAsync(ct);
        int activeChannels = await _db.Channels.CountAsync(c => c.IsLive, ct);
        int totalUsers = await _db.Users.CountAsync(ct);

        DateTime today = DateTime.UtcNow.Date;
        int eventsToday = await _db.ChannelEvents.CountAsync(
            e => e.CreatedAt >= today,
            ct
        );

        Process process = Process.GetCurrentProcess();
        long uptimeSeconds = (long)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds;

        AdminStatsDto dto = new(
            totalChannels,
            activeChannels,
            totalUsers,
            "healthy",
            uptimeSeconds,
            eventsToday
        );

        return Result.Success(dto);
    }

    public async Task<Result<PagedList<AdminChannelDto>>> ListChannelsAsync(
        PaginationParams pagination,
        CancellationToken ct = default
    )
    {
        IQueryable<Domain.Entities.Channel> query = _db.Channels;

        int total = await query.CountAsync(ct);

        List<AdminChannelDto> items = await query
            .OrderBy(c => c.Name)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(c => new AdminChannelDto(
                c.Id,
                c.User.DisplayName,
                c.Name,
                c.IsLive,
                c.Enabled,
                0,
                _db
                    .ChannelSubscriptions.Where(s => s.BroadcasterId == c.Id)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.Tier)
                    .FirstOrDefault() ?? "free",
                c.CreatedAt
            ))
            .ToListAsync(ct);

        return Result.Success(
            new PagedList<AdminChannelDto>(items, total, pagination.Page, pagination.PageSize)
        );
    }

    public Task<Result<AdminSystemDto>> GetSystemHealthAsync(CancellationToken ct = default)
    {
        Process process = Process.GetCurrentProcess();
        long memoryMb = process.WorkingSet64 / (1024 * 1024);
        long uptimeSeconds = (long)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds;

        string version =
            Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";

        List<ServiceHealthDto> services =
        [
            new("api", "healthy", null),
            new("database", "healthy", null),
            new("bot", uptimeSeconds > 0 ? "healthy" : "degraded", null),
        ];

        AdminSystemDto dto = new(
            "healthy",
            services,
            version,
            memoryMb,
            0
        );

        return Task.FromResult(Result.Success(dto));
    }
}
