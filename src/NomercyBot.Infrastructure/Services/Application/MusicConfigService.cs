// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Music;
using NoMercyBot.Application.Services;
using ChannelConfiguration = NoMercyBot.Domain.Entities.Configuration;

namespace NoMercyBot.Infrastructure.Services.Application;

public class MusicConfigService : IMusicConfigService
{
    private const string ConfigKey = "music:config";

    private readonly IApplicationDbContext _db;

    public MusicConfigService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<MusicConfigDto>> GetConfigAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        var config = await LoadConfigAsync(broadcasterId, cancellationToken);
        return Result.Success(config);
    }

    public async Task<Result<MusicConfigDto>> UpdateConfigAsync(
        string broadcasterId,
        UpdateMusicConfigDto request,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _db.Configurations.FirstOrDefaultAsync(
            c => c.BroadcasterId == broadcasterId && c.Key == ConfigKey,
            cancellationToken
        );

        var current = existing is not null
            ? JsonSerializer.Deserialize<MusicConfigData>(existing.Value ?? "{}")
                ?? new MusicConfigData()
            : new MusicConfigData();

        if (request.IsEnabled.HasValue)
            current.IsEnabled = request.IsEnabled.Value;
        if (request.PreferredProvider is not null)
            current.PreferredProvider = request.PreferredProvider;
        if (request.MaxQueueSize.HasValue)
            current.MaxQueueSize = request.MaxQueueSize.Value;
        if (request.MaxRequestsPerUser.HasValue)
            current.MaxRequestsPerUser = request.MaxRequestsPerUser.Value;
        if (request.AllowYouTube.HasValue)
            current.AllowYouTube = request.AllowYouTube.Value;
        if (request.AllowSpotify.HasValue)
            current.AllowSpotify = request.AllowSpotify.Value;
        if (request.MinTrustLevel is not null)
            current.MinTrustLevel = request.MinTrustLevel;

        var json = JsonSerializer.Serialize(current);

        if (existing is not null)
        {
            existing.Value = json;
        }
        else
        {
            _db.Configurations.Add(
                new ChannelConfiguration
                {
                    BroadcasterId = broadcasterId,
                    Key = ConfigKey,
                    Value = json,
                }
            );
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(current));
    }

    private async Task<MusicConfigDto> LoadConfigAsync(
        string broadcasterId,
        CancellationToken cancellationToken
    )
    {
        var entry = await _db.Configurations.FirstOrDefaultAsync(
            c => c.BroadcasterId == broadcasterId && c.Key == ConfigKey,
            cancellationToken
        );

        if (entry?.Value is null)
            return ToDto(new MusicConfigData());

        var data =
            JsonSerializer.Deserialize<MusicConfigData>(entry.Value) ?? new MusicConfigData();
        return ToDto(data);
    }

    private static MusicConfigDto ToDto(MusicConfigData d) =>
        new(
            d.IsEnabled,
            d.PreferredProvider,
            d.MaxQueueSize,
            d.MaxRequestsPerUser,
            d.AllowYouTube,
            d.AllowSpotify,
            d.MinTrustLevel
        );

    private sealed class MusicConfigData
    {
        public bool IsEnabled { get; set; } = true;
        public string PreferredProvider { get; set; } = "auto";
        public int MaxQueueSize { get; set; } = 50;
        public int MaxRequestsPerUser { get; set; } = 5;
        public bool AllowYouTube { get; set; } = true;
        public bool AllowSpotify { get; set; } = true;
        public string MinTrustLevel { get; set; } = "everyone";
    }
}
