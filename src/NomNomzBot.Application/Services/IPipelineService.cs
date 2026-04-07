// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Pipelines;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Manages per-channel pipeline definitions created by the visual node builder.
/// </summary>
public interface IPipelineService
{
    Task<Result<PagedList<PipelineListItemDto>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken ct = default
    );

    Task<Result<PipelineDto>> GetAsync(
        string broadcasterId,
        int id,
        CancellationToken ct = default
    );

    Task<Result<PipelineDto>> CreateAsync(
        string broadcasterId,
        CreatePipelineDto request,
        CancellationToken ct = default
    );

    Task<Result<PipelineDto>> UpdateAsync(
        string broadcasterId,
        int id,
        UpdatePipelineDto request,
        CancellationToken ct = default
    );

    Task<Result> DeleteAsync(string broadcasterId, int id, CancellationToken ct = default);
}
