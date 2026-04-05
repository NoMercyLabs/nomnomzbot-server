// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Admin;

namespace NoMercyBot.Application.Services;

public interface IAdminService
{
    Task<Result<AdminStatsDto>> GetStatsAsync(CancellationToken ct = default);

    Task<Result<PagedList<AdminChannelDto>>> ListChannelsAsync(
        PaginationParams pagination,
        CancellationToken ct = default
    );

    Task<Result<AdminSystemDto>> GetSystemHealthAsync(CancellationToken ct = default);
}
