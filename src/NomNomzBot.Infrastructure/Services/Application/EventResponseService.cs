// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.EventResponses;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Services.Application;

public class EventResponseService : IEventResponseService
{
    private readonly IApplicationDbContext _db;

    public EventResponseService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedList<EventResponseListItem>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<EventResponse> query = _db.EventResponses.Where(e => e.BroadcasterId == broadcasterId);
        int total = await query.CountAsync(cancellationToken);

        List<EventResponseListItem> items = await query
            .OrderBy(e => e.EventType)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(e => new EventResponseListItem(
                e.Id,
                e.EventType,
                e.IsEnabled,
                e.ResponseType,
                e.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(
            new PagedList<EventResponseListItem>(items, total, pagination.Page, pagination.PageSize)
        );
    }

    public async Task<Result<EventResponseDto>> GetByEventTypeAsync(
        string broadcasterId,
        string eventType,
        CancellationToken cancellationToken = default
    )
    {
        EventResponse? entity = await _db.EventResponses.FirstOrDefaultAsync(
            e => e.BroadcasterId == broadcasterId && e.EventType == eventType,
            cancellationToken
        );

        if (entity is null)
            return Errors.NotFound<EventResponseDto>("EventResponse", eventType);

        return Result.Success(ToDto(entity));
    }

    public async Task<Result<EventResponseDto>> UpsertAsync(
        string broadcasterId,
        string eventType,
        UpdateEventResponseDto request,
        CancellationToken cancellationToken = default
    )
    {
        EventResponse? entity = await _db.EventResponses.FirstOrDefaultAsync(
            e => e.BroadcasterId == broadcasterId && e.EventType == eventType,
            cancellationToken
        );

        if (entity is null)
        {
            entity = new()
            {
                BroadcasterId = broadcasterId,
                EventType = eventType,
                ResponseType = request.ResponseType ?? "chat_message",
                IsEnabled = request.IsEnabled ?? true,
                Message = request.Message,
                PipelineJson = request.PipelineJson,
                Metadata = request.Metadata ?? new Dictionary<string, string>(),
            };
            _db.EventResponses.Add(entity);
        }
        else
        {
            if (request.IsEnabled.HasValue)
                entity.IsEnabled = request.IsEnabled.Value;
            if (request.ResponseType is not null)
                entity.ResponseType = request.ResponseType;
            if (request.Message is not null)
                entity.Message = request.Message;
            if (request.PipelineJson is not null)
                entity.PipelineJson = request.PipelineJson;
            if (request.Metadata is not null)
                entity.Metadata = request.Metadata;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(entity));
    }

    public async Task<Result> DeleteAsync(
        string broadcasterId,
        string eventType,
        CancellationToken cancellationToken = default
    )
    {
        EventResponse? entity = await _db.EventResponses.FirstOrDefaultAsync(
            e => e.BroadcasterId == broadcasterId && e.EventType == eventType,
            cancellationToken
        );

        if (entity is null)
            return Result.Failure($"EventResponse for '{eventType}' was not found.", "NOT_FOUND");

        _db.EventResponses.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static EventResponseDto ToDto(EventResponse e) =>
        new(
            e.Id,
            e.EventType,
            e.IsEnabled,
            e.ResponseType,
            e.Message,
            e.PipelineJson,
            e.Metadata,
            e.CreatedAt,
            e.UpdatedAt
        );
}
