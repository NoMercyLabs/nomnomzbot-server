// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.Features.Features.Queries.GetFeatures;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Application.Features.Features.Commands.ToggleFeature;

public class ToggleFeatureCommandHandler
{
    private readonly IApplicationDbContext _db;

    public ToggleFeatureCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<FeatureStatusDto>> HandleAsync(
        string channelId,
        string featureKey,
        CancellationToken ct = default
    )
    {
        ChannelFeature? feature = await _db.ChannelFeatures.FirstOrDefaultAsync(
            f => f.BroadcasterId == channelId && f.FeatureKey == featureKey,
            ct
        );

        if (feature is null)
        {
            feature = new()
            {
                BroadcasterId = channelId,
                FeatureKey = featureKey,
                IsEnabled = true,
                EnabledAt = DateTime.UtcNow,
            };
            _db.ChannelFeatures.Add(feature);
        }
        else
        {
            feature.IsEnabled = !feature.IsEnabled;
            feature.EnabledAt = feature.IsEnabled ? DateTime.UtcNow : null;
        }

        await _db.SaveChangesAsync(ct);

        return Result.Success(
            new FeatureStatusDto(
                feature.FeatureKey,
                feature.IsEnabled,
                feature.EnabledAt,
                feature.RequiredScopes
            )
        );
    }
}
