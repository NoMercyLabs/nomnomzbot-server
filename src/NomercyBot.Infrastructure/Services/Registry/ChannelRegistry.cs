// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Registry;

/// <summary>
/// Singleton in-memory registry of all active channel contexts.
/// Implements <see cref="IHostedService"/> to manage the background eviction timer.
/// </summary>
public sealed class ChannelRegistry : IChannelRegistry, IHostedService
{
    private readonly ConcurrentDictionary<string, ChannelContext> _channels = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChannelRegistry> _logger;
    private Timer? _evictionTimer;

    // Eviction: remove channels that are offline AND have had no activity for 2 hours
    // Checked every 15 minutes
    private static readonly TimeSpan EvictionThreshold = TimeSpan.FromHours(2);
    private static readonly TimeSpan EvictionInterval = TimeSpan.FromMinutes(15);

    public ChannelRegistry(IServiceScopeFactory scopeFactory, ILogger<ChannelRegistry> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // IHostedService
    // -------------------------------------------------------------------------

    public Task StartAsync(CancellationToken ct)
    {
        _evictionTimer = new Timer(RunEviction, null, EvictionInterval, EvictionInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _evictionTimer?.Dispose();
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // IChannelRegistry
    // -------------------------------------------------------------------------

    public int Count => _channels.Count;

    public async Task<ChannelContext> GetOrCreateAsync(string broadcasterId, string channelName, CancellationToken ct = default)
    {
        if (_channels.TryGetValue(broadcasterId, out var existing))
        {
            existing.LastActivityAt = DateTimeOffset.UtcNow;
            return existing;
        }

        var ctx = new ChannelContext { BroadcasterId = broadcasterId, ChannelName = channelName };

        // Load commands from DB
        await LoadCommandsAsync(ctx, ct);

        _channels[broadcasterId] = ctx;
        _logger.LogInformation("Registered channel {BroadcasterId} ({ChannelName})", broadcasterId, channelName);
        return ctx;
    }

    public ChannelContext? Get(string broadcasterId)
        => _channels.TryGetValue(broadcasterId, out var ctx) ? ctx : null;

    public async Task RemoveAsync(string broadcasterId, CancellationToken ct = default)
    {
        if (!_channels.TryRemove(broadcasterId, out var ctx))
            return;

        // Cancel all active pipelines before releasing the context
        foreach (var (executionId, cts) in ctx.ActivePipelines)
        {
            try
            {
                await cts.CancelAsync();
                cts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cancelling pipeline {ExecutionId} for channel {BroadcasterId}", executionId, broadcasterId);
            }
        }

        ctx.ActivePipelines.Clear();
        _logger.LogInformation("Unregistered channel {BroadcasterId}", broadcasterId);
    }

    public IReadOnlyCollection<ChannelContext> GetAll()
        => _channels.Values.ToList().AsReadOnly();

    public IReadOnlyCollection<ChannelContext> GetLiveChannels()
        => _channels.Values.Where(c => c.IsLive).ToList().AsReadOnly();

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task LoadCommandsAsync(ChannelContext ctx, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var commands = await db.Commands
            .Where(c => c.BroadcasterId == ctx.BroadcasterId && c.IsEnabled && c.DeletedAt == null)
            .Select(c => new CachedCommand
            {
                Name = c.Name,
                Responses = c.Responses.ToArray(),
                // CooldownSeconds maps to GlobalCooldown; UserCooldown is 0 unless CooldownPerUser is true
                GlobalCooldown = c.CooldownPerUser ? 0 : c.CooldownSeconds,
                UserCooldown = c.CooldownPerUser ? c.CooldownSeconds : 0,
                Permission = c.Permission,
                Type = c.Type,
                PipelineJson = c.PipelineJson,
                Aliases = c.Aliases.ToArray()
            })
            .ToListAsync(ct);

        foreach (var cmd in commands)
        {
            ctx.Commands[cmd.Name] = cmd;
            foreach (var alias in cmd.Aliases)
                ctx.Commands[alias] = cmd;
        }

        _logger.LogDebug("Loaded {Count} commands for channel {BroadcasterId}", commands.Count, ctx.BroadcasterId);
    }

    private void RunEviction(object? state)
    {
        var threshold = DateTimeOffset.UtcNow - EvictionThreshold;
        var candidates = _channels.Values
            .Where(c => !c.IsLive && c.LastActivityAt < threshold)
            .ToList();

        foreach (var ctx in candidates)
        {
            if (_channels.TryRemove(ctx.BroadcasterId, out _))
                _logger.LogInformation("Evicted idle channel {BroadcasterId}", ctx.BroadcasterId);
        }
    }
}
