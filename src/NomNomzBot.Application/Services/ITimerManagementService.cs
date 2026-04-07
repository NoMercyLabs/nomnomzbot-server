// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Timers;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Application service for managing per-channel message timers via the REST API.
/// </summary>
public interface ITimerManagementService
{
    /// <summary>List all timers for a channel.</summary>
    Task<Result<PagedList<TimerListItem>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get a single timer by ID.</summary>
    Task<Result<TimerDto>> GetAsync(
        string broadcasterId,
        int id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Create a new timer.</summary>
    Task<Result<TimerDto>> CreateAsync(
        string broadcasterId,
        CreateTimerDto request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Update an existing timer.</summary>
    Task<Result<TimerDto>> UpdateAsync(
        string broadcasterId,
        int id,
        UpdateTimerDto request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Delete a timer.</summary>
    Task<Result> DeleteAsync(
        string broadcasterId,
        int id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Toggle a timer enabled/disabled.</summary>
    Task<Result<TimerDto>> ToggleAsync(
        string broadcasterId,
        int id,
        CancellationToken cancellationToken = default
    );
}
