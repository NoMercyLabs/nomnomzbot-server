// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;

namespace NoMercyBot.Infrastructure.BackgroundServices;

/// <summary>
/// Runs timed chat messages (e.g. !socials every 30 minutes) for each channel.
/// Timer commands are stored in Records with RecordType="timer_command".
/// Activity check: timer commands only fire if chat has been active in the interval window.
/// </summary>
public sealed class TimerSchedulerService : BackgroundService
{
    private const string TimerRecordType = "timer_command";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TimerSchedulerService> _logger;

    // Per-channel last message timestamps for activity checks
    private readonly Dictionary<string, DateTimeOffset> _lastChatActivity = new();
    private readonly Lock _activityLock = new();

    public TimerSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<TimerSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>Called by chat message handlers to track activity per channel.</summary>
    public void RecordActivity(string broadcasterId)
    {
        lock (_activityLock)
        {
            _lastChatActivity[broadcasterId] = DateTimeOffset.UtcNow;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TimerSchedulerService starting.");

        // Check timers every 60 seconds
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "TimerSchedulerService: Error processing timer tick");
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var chatService = scope.ServiceProvider.GetRequiredService<ITwitchChatService>();

        var now = DateTimeOffset.UtcNow;

        // Load all timer commands
        var timerRecords = await db.Records
            .Where(r => r.RecordType == TimerRecordType)
            .ToListAsync(ct);

        foreach (var record in timerRecords)
        {
            try
            {
                var timer = JsonSerializer.Deserialize<TimerCommandData>(record.Data);
                if (timer is null || !timer.IsEnabled) continue;

                // Check if interval has elapsed since last fire
                var elapsed = now - timer.LastFiredAt;
                if (elapsed.TotalSeconds < timer.IntervalSeconds) continue;

                // Check chat activity: if no messages in the last interval window, skip
                DateTimeOffset lastActivity;
                lock (_activityLock)
                {
                    _lastChatActivity.TryGetValue(record.BroadcasterId, out var act);
                    lastActivity = act;
                }

                if (timer.RequiresActivity && lastActivity < now.AddSeconds(-timer.IntervalSeconds))
                {
                    _logger.LogDebug("TimerScheduler: Skipping timer '{Name}' for {BroadcasterId} — no chat activity",
                        timer.Name, record.BroadcasterId);
                    continue;
                }

                // Fire the timer message
                if (!string.IsNullOrWhiteSpace(timer.Message))
                {
                    await chatService.SendMessageAsync(record.BroadcasterId, timer.Message, ct);
                    _logger.LogDebug("TimerScheduler: Fired '{Name}' for {BroadcasterId}", timer.Name, record.BroadcasterId);
                }

                // Update last fired timestamp
                timer.LastFiredAt = now;
                record.Data = JsonSerializer.Serialize(timer);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "TimerScheduler: Error processing timer record {RecordId}", record.Id);
            }
        }
    }

    private sealed class TimerCommandData
    {
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int IntervalSeconds { get; set; } = 1800; // 30 minutes default
        public bool IsEnabled { get; set; } = true;
        public bool RequiresActivity { get; set; } = true;
        public DateTimeOffset LastFiredAt { get; set; } = DateTimeOffset.MinValue;
    }
}
