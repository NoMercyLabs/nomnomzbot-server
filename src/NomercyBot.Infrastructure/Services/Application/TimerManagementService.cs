// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Timers;
using NoMercyBot.Application.Services;
using DomainTimer = NoMercyBot.Domain.Entities.Timer;

namespace NoMercyBot.Infrastructure.Services.Application;

public class TimerManagementService : ITimerManagementService
{
    private readonly IApplicationDbContext _db;

    public TimerManagementService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedList<TimerListItem>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    )
    {
        var query = _db.Timers.Where(t => t.BroadcasterId == broadcasterId);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(t => new TimerListItem(
                t.Id,
                t.Name,
                t.IntervalMinutes,
                t.IsEnabled,
                t.LastFiredAt,
                t.Messages.Count,
                t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(
            new PagedList<TimerListItem>(items, total, pagination.Page, pagination.PageSize)
        );
    }

    public async Task<Result<TimerDto>> GetAsync(
        string broadcasterId,
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var timer = await _db.Timers.FirstOrDefaultAsync(
            t => t.BroadcasterId == broadcasterId && t.Id == id,
            cancellationToken
        );

        if (timer is null)
            return Errors.NotFound<TimerDto>("Timer", id.ToString());

        return Result.Success(ToDto(timer));
    }

    public async Task<Result<TimerDto>> CreateAsync(
        string broadcasterId,
        CreateTimerDto request,
        CancellationToken cancellationToken = default
    )
    {
        var exists = await _db.Timers.AnyAsync(
            t => t.BroadcasterId == broadcasterId && t.Name == request.Name,
            cancellationToken
        );

        if (exists)
            return Errors.AlreadyExists("timer", request.Name).ToTyped<TimerDto>();

        var timer = new DomainTimer
        {
            BroadcasterId = broadcasterId,
            Name = request.Name,
            Messages = request.Messages,
            IntervalMinutes = request.IntervalMinutes,
            MinChatActivity = request.MinChatActivity,
            IsEnabled = request.IsEnabled,
        };

        _db.Timers.Add(timer);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(timer));
    }

    public async Task<Result<TimerDto>> UpdateAsync(
        string broadcasterId,
        int id,
        UpdateTimerDto request,
        CancellationToken cancellationToken = default
    )
    {
        var timer = await _db.Timers.FirstOrDefaultAsync(
            t => t.BroadcasterId == broadcasterId && t.Id == id,
            cancellationToken
        );

        if (timer is null)
            return Errors.NotFound<TimerDto>("Timer", id.ToString());

        if (request.Name is not null)
            timer.Name = request.Name;
        if (request.Messages is not null)
            timer.Messages = request.Messages;
        if (request.IntervalMinutes.HasValue)
            timer.IntervalMinutes = request.IntervalMinutes.Value;
        if (request.MinChatActivity.HasValue)
            timer.MinChatActivity = request.MinChatActivity.Value;
        if (request.IsEnabled.HasValue)
            timer.IsEnabled = request.IsEnabled.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(timer));
    }

    public async Task<Result> DeleteAsync(
        string broadcasterId,
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var timer = await _db.Timers.FirstOrDefaultAsync(
            t => t.BroadcasterId == broadcasterId && t.Id == id,
            cancellationToken
        );

        if (timer is null)
            return Result.Failure($"Timer '{id}' was not found.", "NOT_FOUND");

        _db.Timers.Remove(timer);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<TimerDto>> ToggleAsync(
        string broadcasterId,
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var timer = await _db.Timers.FirstOrDefaultAsync(
            t => t.BroadcasterId == broadcasterId && t.Id == id,
            cancellationToken
        );

        if (timer is null)
            return Errors.NotFound<TimerDto>("Timer", id.ToString());

        timer.IsEnabled = !timer.IsEnabled;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(timer));
    }

    private static TimerDto ToDto(DomainTimer t) =>
        new(
            t.Id,
            t.Name,
            t.Messages,
            t.IntervalMinutes,
            t.MinChatActivity,
            t.IsEnabled,
            t.LastFiredAt,
            t.NextMessageIndex,
            t.CreatedAt,
            t.UpdatedAt
        );
}
