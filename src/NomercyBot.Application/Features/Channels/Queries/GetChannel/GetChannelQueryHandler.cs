// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;

namespace NoMercyBot.Application.Features.Channels.Queries.GetChannel;

public class GetChannelQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetChannelQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ChannelDto>> HandleAsync(GetChannelQuery query, CancellationToken ct = default)
    {
        var channel = await _db.Channels
            .Include(c => c.User)
            .Where(c => c.Id == query.ChannelId)
            .Select(c => new ChannelDto(
                c.Id,
                c.Name,
                c.User.DisplayName,
                c.User.ProfileImageUrl,
                c.IsLive,
                c.IsOnboarded,
                c.Title,
                c.GameName,
                c.BotJoinedAt,
                "free",
                c.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return channel is null
            ? Errors.ChannelNotFound<ChannelDto>(query.ChannelId)
            : Result.Success(channel);
    }
}
