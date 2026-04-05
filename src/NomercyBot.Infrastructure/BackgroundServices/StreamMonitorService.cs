// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;

namespace NoMercyBot.Infrastructure.BackgroundServices;

public class StreamMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StreamMonitorService> _logger;

    public StreamMonitorService(IServiceProvider serviceProvider, ILogger<StreamMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stream monitor service started.");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckStreamsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error checking stream statuses.");
            }
        }
    }

    private async Task CheckStreamsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var twitchApi = scope.ServiceProvider.GetRequiredService<ITwitchApiService>();

        var channels = await db.Channels
            .Where(c => c.Enabled)
            .Select(c => new { c.Id, c.IsLive })
            .ToListAsync(ct);

        foreach (var channel in channels)
        {
            var streamInfo = await twitchApi.GetStreamInfoAsync(channel.Id, ct);
            var isNowLive = streamInfo?.IsLive ?? false;

            if (channel.IsLive != isNowLive)
            {
                var entity = await db.Channels.FindAsync([channel.Id], ct);
                if (entity is not null)
                {
                    entity.IsLive = isNowLive;
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Channel {ChannelId} is now {Status}", channel.Id, isNowLive ? "LIVE" : "OFFLINE");
                }
            }
        }
    }
}
