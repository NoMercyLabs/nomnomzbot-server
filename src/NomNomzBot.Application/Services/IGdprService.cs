// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Application.Common.Models;

namespace NoMercyBot.Application.Services;

public interface IGdprService
{
    Task<Result<string>> ExportUserDataAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default);
}
