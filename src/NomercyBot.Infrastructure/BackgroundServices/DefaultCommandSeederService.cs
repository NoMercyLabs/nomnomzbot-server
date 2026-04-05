// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.BackgroundServices;

/// <summary>
/// Seeds default music commands (!sr, !skip, !queue, !volume, !song) for every enabled channel
/// that does not already have them. Runs once at startup.
/// All commands are of pipeline type so they are fully configurable per-channel.
/// </summary>
public sealed class DefaultCommandSeederService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DefaultCommandSeederService> _logger;

    private static readonly IReadOnlyList<DefaultCommand> Defaults =
    [
        new(
            "!sr",
            """{"steps":[{"action":{"type":"music_request"}}]}""",
            "everyone",
            5,
            "Request a song"
        ),
        new(
            "!skip",
            """{"steps":[{"action":{"type":"music_skip"}}]}""",
            "moderator",
            0,
            "Skip the current song"
        ),
        new(
            "!queue",
            """{"steps":[{"action":{"type":"music_queue"}}]}""",
            "everyone",
            10,
            "Show the song queue"
        ),
        new(
            "!volume",
            """{"steps":[{"action":{"type":"music_volume"}}]}""",
            "moderator",
            0,
            "Set the music volume"
        ),
        new(
            "!song",
            """{"steps":[{"action":{"type":"music_current"}}]}""",
            "everyone",
            5,
            "Show the current song"
        ),
    ];

    public DefaultCommandSeederService(
        IServiceScopeFactory scopeFactory,
        ILogger<DefaultCommandSeederService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var channelIds = await db
                .Channels.Where(c => c.DeletedAt == null)
                .Select(c => c.Id)
                .ToListAsync(stoppingToken);

            var seeded = 0;
            foreach (var channelId in channelIds)
            {
                foreach (var def in Defaults)
                {
                    var exists = await db.Commands.AnyAsync(
                        c => c.BroadcasterId == channelId && c.Name == def.Name,
                        stoppingToken
                    );

                    if (exists)
                        continue;

                    db.Commands.Add(
                        new Command
                        {
                            BroadcasterId = channelId,
                            Name = def.Name,
                            Type = "pipeline",
                            PipelineJson = def.PipelineJson,
                            Permission = def.Permission,
                            CooldownSeconds = def.CooldownSeconds,
                            Description = def.Description,
                            IsEnabled = true,
                        }
                    );
                    seeded++;
                }
            }

            if (seeded > 0)
            {
                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation(
                    "DefaultCommandSeeder: seeded {Count} default commands across {Channels} channels",
                    seeded,
                    channelIds.Count
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DefaultCommandSeeder failed");
        }
    }

    private sealed record DefaultCommand(
        string Name,
        string PipelineJson,
        string Permission,
        int CooldownSeconds,
        string Description
    );
}
