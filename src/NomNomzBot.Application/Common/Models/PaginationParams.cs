// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Common.Models;

public record PaginationParams(
    int Page = 1,
    int PageSize = 25,
    string? SortBy = null,
    string? Order = "asc"
);
