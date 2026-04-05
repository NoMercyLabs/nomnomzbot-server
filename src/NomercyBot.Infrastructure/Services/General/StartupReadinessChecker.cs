// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;

namespace NoMercyBot.Infrastructure.Services.General;

public sealed class StartupReadinessChecker(
    IConfiguration configuration,
    ILogger<StartupReadinessChecker> logger
)
{
    private const int MaxAttempts = 10;

    public async Task WaitForPostgresAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning(
                "No PostgreSQL connection string configured — skipping readiness check"
            );
            return;
        }

        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken);
                logger.LogInformation("PostgreSQL is ready");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    "PostgreSQL not ready (attempt {Attempt}/{Max}): {Message}",
                    attempt,
                    MaxAttempts,
                    ex.Message
                );
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }

        throw new InvalidOperationException(
            "PostgreSQL is not reachable after multiple attempts. "
                + "Start postgres via 'docker-compose up -d postgres' "
                + "or check ConnectionStrings__DefaultConnection in your .env file."
        );
    }

    public async Task WaitForRedisAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("No Redis connection string configured — using in-memory cache");
            return;
        }

        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = true;
                options.ConnectTimeout = 3000;

                using var redis = await ConnectionMultiplexer.ConnectAsync(options);
                await redis.GetDatabase().PingAsync();
                logger.LogInformation("Redis is ready");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    "Redis not ready (attempt {Attempt}/{Max}): {Message}",
                    attempt,
                    MaxAttempts,
                    ex.Message
                );
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }

        throw new InvalidOperationException(
            "Redis is not reachable after multiple attempts. "
                + "Start redis via 'docker-compose up -d redis' "
                + "or check ConnectionStrings__Redis in your .env file."
        );
    }
}
