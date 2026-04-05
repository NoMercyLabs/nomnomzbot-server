// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;

namespace NoMercyBot.Application.Features.Commands.Queries.GetCommands;

public class GetCommandsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetCommandsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<CommandListItemDto>>> HandleAsync(GetCommandsQuery query, CancellationToken ct = default)
    {
        var q = _db.Commands.Where(c => c.BroadcasterId == query.ChannelId);
        if (!query.IncludeDisabled)
            q = q.Where(c => c.IsEnabled);

        var commands = await q
            .OrderBy(c => c.Name)
            .Select(c => new CommandListItemDto(
                c.Id, c.Name, c.Type, c.Permission, c.IsEnabled,
                c.IsPlatform, c.CooldownSeconds, c.Description, c.Aliases, c.CreatedAt))
            .ToListAsync(ct);

        return Result.Success(commands);
    }
}
