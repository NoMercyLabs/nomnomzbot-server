// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Widgets;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Services.Application;

public class WidgetService : IWidgetService
{
    private readonly IApplicationDbContext _db;

    public WidgetService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<WidgetDetail>> CreateAsync(
        string broadcasterId,
        CreateWidgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var channelExists = await _db.Channels
            .AnyAsync(c => c.Id == broadcasterId, cancellationToken);

        if (!channelExists)
            return Errors.ChannelNotFound<WidgetDetail>(broadcasterId);

        var widget = new Widget
        {
            Id = Guid.NewGuid().ToString(),
            BroadcasterId = broadcasterId,
            Name = request.Name,
            Framework = request.Type,
            IsEnabled = true,
            EventSubscriptions = request.EventSubscriptions ?? [],
            Settings = request.Settings?.ToDictionary(k => k.Key, v => v.Value ?? (object)"")
                ?? new Dictionary<string, object>(),
        };

        _db.Widgets.Add(widget);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDetail(widget));
    }

    public async Task<Result<WidgetDetail>> UpdateAsync(
        string broadcasterId,
        string widgetId,
        UpdateWidgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var widget = await _db.Widgets
            .FirstOrDefaultAsync(w => w.Id == widgetId && w.BroadcasterId == broadcasterId, cancellationToken);

        if (widget is null)
            return Errors.NotFound<WidgetDetail>("Widget", widgetId);

        if (request.Name is not null) widget.Name = request.Name;
        if (request.IsEnabled.HasValue) widget.IsEnabled = request.IsEnabled.Value;
        if (request.EventSubscriptions is not null) widget.EventSubscriptions = request.EventSubscriptions;
        if (request.Settings is not null)
            widget.Settings = request.Settings.ToDictionary(k => k.Key, v => v.Value ?? (object)"");

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDetail(widget));
    }

    public async Task<Result> DeleteAsync(
        string broadcasterId,
        string widgetId,
        CancellationToken cancellationToken = default)
    {
        var widget = await _db.Widgets
            .FirstOrDefaultAsync(w => w.Id == widgetId && w.BroadcasterId == broadcasterId, cancellationToken);

        if (widget is null)
            return Result.Failure($"Widget '{widgetId}' was not found.", "NOT_FOUND");

        _db.Widgets.Remove(widget);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PagedList<WidgetListItem>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Widgets.Where(w => w.BroadcasterId == broadcasterId);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(w => w.Name)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(w => new WidgetListItem(
                w.Id,
                w.Name,
                w.Framework,
                w.IsEnabled,
                w.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedList<WidgetListItem>(items, total, pagination.Page, pagination.PageSize));
    }

    public async Task<Result<WidgetDetail>> GetAsync(
        string broadcasterId,
        string widgetId,
        CancellationToken cancellationToken = default)
    {
        var widget = await _db.Widgets
            .FirstOrDefaultAsync(w => w.Id == widgetId && w.BroadcasterId == broadcasterId, cancellationToken);

        if (widget is null)
            return Errors.NotFound<WidgetDetail>("Widget", widgetId);

        return Result.Success(ToDetail(widget));
    }

    public async Task<Result<WidgetDetail>> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Widgets are accessed via channel overlay token; look up the channel first
        var channel = await _db.Channels
            .FirstOrDefaultAsync(c => c.OverlayToken == token, cancellationToken);

        if (channel is null)
            return Result.Failure<WidgetDetail>("No channel found for the provided token.", "NOT_FOUND");

        // Return the first enabled widget for that channel as a representative
        var widget = await _db.Widgets
            .Where(w => w.BroadcasterId == channel.Id && w.IsEnabled)
            .OrderBy(w => w.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (widget is null)
            return Result.Failure<WidgetDetail>("No enabled widget found for the provided token.", "NOT_FOUND");

        return Result.Success(ToDetail(widget));
    }

    private static WidgetDetail ToDetail(Widget w) => new(
        w.Id,
        w.Name,
        w.Framework,
        w.IsEnabled,
        null,
        w.Settings.ToDictionary(k => k.Key, v => (object?)v.Value),
        w.EventSubscriptions,
        w.CreatedAt,
        w.UpdatedAt);
}
