// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.EventResponses;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Manages per-channel event response configurations.
/// </summary>
public interface IEventResponseService
{
    Task<Result<PagedList<EventResponseListItem>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    );
    Task<Result<EventResponseDto>> GetByEventTypeAsync(
        string broadcasterId,
        string eventType,
        CancellationToken cancellationToken = default
    );
    Task<Result<EventResponseDto>> UpsertAsync(
        string broadcasterId,
        string eventType,
        UpdateEventResponseDto request,
        CancellationToken cancellationToken = default
    );
    Task<Result> DeleteAsync(
        string broadcasterId,
        string eventType,
        CancellationToken cancellationToken = default
    );
}
