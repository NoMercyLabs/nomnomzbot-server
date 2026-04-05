// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;

namespace NoMercyBot.Application.Features.Features.Queries.GetFeatures;

public class GetFeaturesQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetFeaturesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<FeatureStatusDto>>> HandleAsync(
        string channelId,
        CancellationToken ct = default
    )
    {
        var features = await _db
            .ChannelFeatures.Where(f => f.BroadcasterId == channelId)
            .Select(f => new FeatureStatusDto(
                f.FeatureKey,
                f.IsEnabled,
                f.EnabledAt,
                f.RequiredScopes
            ))
            .ToListAsync(ct);

        return Result.Success(features);
    }
}

public record FeatureStatusDto(
    string FeatureKey,
    bool IsEnabled,
    DateTime? EnabledAt,
    string[] RequiredScopes
);
