// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;

namespace NoMercyBot.Infrastructure.BackgroundServices;

public class ChannelManagerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChannelManagerService> _logger;

    public ChannelManagerService(IServiceProvider serviceProvider, ILogger<ChannelManagerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Channel manager service starting — joining enabled channels.");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var chatService = scope.ServiceProvider.GetRequiredService<ITwitchChatService>();

        var channels = await db.Channels
            .Where(c => c.Enabled && c.IsOnboarded)
            .Select(c => c.Name)
            .ToListAsync(stoppingToken);

        foreach (var name in channels)
        {
            try
            {
                await chatService.JoinChannelAsync(name, stoppingToken);
                _logger.LogInformation("Joined channel #{ChannelName}", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join channel #{ChannelName}", name);
            }
        }
    }
}
