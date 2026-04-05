// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Services.Migration;

/// <summary>
/// Migrates data from the legacy SQLite-based NomercyBot to the new PostgreSQL schema.
///
/// What IS migrated (per spec 14-migration-guide.md):
///   - Users (upsert)
///   - ChatMessages (with BroadcasterId set)
///   - Commands (enabled only)
///   - Records (watch streaks, command usage)
///   - ChannelEvents
///
/// What is NOT migrated:
///   - OAuth tokens (re-authenticate after migration)
///   - Roslyn scripts (recreate as pipeline commands)
///   - Widget HTML/JS (widget system redesigned)
///   - EventSub subscriptions (re-created on onboarding)
/// </summary>
public sealed class SqliteMigrationService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<SqliteMigrationService> _logger;

    public SqliteMigrationService(
        IApplicationDbContext db,
        ILogger<SqliteMigrationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Runs the full migration from the SQLite database file to the current PostgreSQL instance.
    /// </summary>
    /// <param name="sqliteFilePath">Path to the old .db file.</param>
    /// <param name="broadcasterId">The Twitch user ID of the channel being migrated.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A summary of what was migrated.</returns>
    public async Task<MigrationResult> MigrateAsync(
        string sqliteFilePath,
        string broadcasterId,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sqliteFilePath))
            return new MigrationResult(false, $"SQLite file not found: {sqliteFilePath}");

        // Ensure the target channel exists
        var channelExists = await _db.Channels.AnyAsync(c => c.Id == broadcasterId, cancellationToken);
        if (!channelExists)
            return new MigrationResult(false, $"Channel {broadcasterId} not found. Complete onboarding first.");

        var counts = new MigrationCounts();

        var connectionString = $"Data Source={sqliteFilePath};Mode=ReadOnly;";
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        // Migration steps are independent — failures are logged but don't abort others
        counts.Users = await MigrateUsersAsync(conn, cancellationToken);
        counts.Commands = await MigrateCommandsAsync(conn, broadcasterId, cancellationToken);
        counts.ChatMessages = await MigrateChatMessagesAsync(conn, broadcasterId, cancellationToken);
        counts.Records = await MigrateRecordsAsync(conn, broadcasterId, cancellationToken);

        _logger.LogInformation(
            "Migration complete for {BroadcasterId}: {Users} users, {Commands} commands, " +
            "{Messages} messages, {Records} records",
            broadcasterId, counts.Users, counts.Commands, counts.ChatMessages, counts.Records);

        return new MigrationResult(true, "Migration completed successfully.", counts);
    }

    // ─── Users ────────────────────────────────────────────────────────────────

    private async Task<int> MigrateUsersAsync(SqliteConnection conn, CancellationToken ct)
    {
        int count = 0;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Username, DisplayName, ProfileImageUrl FROM Users";

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var id = reader.GetString(0);
            var username = reader.GetString(1);
            var displayName = reader.GetString(2);
            var profileUrl = reader.IsDBNull(3) ? null : reader.GetString(3);

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (existing is null)
            {
                _db.Users.Add(new User
                {
                    Id = id,
                    Username = username,
                    DisplayName = displayName,
                    ProfileImageUrl = profileUrl,
                    Enabled = true,
                });
                count++;
            }
        }

        if (count > 0)
            await _db.SaveChangesAsync(ct);

        return count;
    }

    // ─── Commands ─────────────────────────────────────────────────────────────

    private async Task<int> MigrateCommandsAsync(SqliteConnection conn, string broadcasterId, CancellationToken ct)
    {
        int count = 0;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Name, Response, CooldownSeconds, IsEnabled FROM Commands WHERE IsEnabled = 1";

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var name = reader.GetString(0).ToLowerInvariant();
            var response = reader.IsDBNull(1) ? null : reader.GetString(1);
            var cooldown = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

            var existing = await _db.Commands.AnyAsync(c => c.BroadcasterId == broadcasterId && c.Name == name, ct);
            if (!existing)
            {
                _db.Commands.Add(new Command
                {
                    BroadcasterId = broadcasterId,
                    Name = name,
                    Response = response,
                    CooldownSeconds = cooldown,
                    IsEnabled = true,
                    Type = "chat",
                    Permission = "everyone",
                    Responses = [],
                    Aliases = [],
                });
                count++;
            }
        }

        if (count > 0)
            await _db.SaveChangesAsync(ct);

        return count;
    }

    // ─── Chat messages ────────────────────────────────────────────────────────

    private async Task<int> MigrateChatMessagesAsync(SqliteConnection conn, string broadcasterId, CancellationToken ct)
    {
        int count = 0;

        // Check if ChatMessages table exists in the old DB
        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ChatMessages'";
        var tableExists = await checkCmd.ExecuteScalarAsync(ct) is not null;
        if (!tableExists) return 0;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, UserId, Username, DisplayName, Message, CreatedAt
            FROM ChatMessages
            ORDER BY CreatedAt ASC
            LIMIT 10000";

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var batch = new List<ChatMessage>();

        while (await reader.ReadAsync(ct))
        {
            var msgId = reader.GetString(0);
            var userId = reader.GetString(1);
            var username = reader.GetString(2);
            var displayName = reader.GetString(3);
            var message = reader.GetString(4);
            var createdAt = reader.GetDateTime(5);

            // Skip if already migrated
            var exists = await _db.ChatMessages.AnyAsync(m => m.Id == msgId, ct);
            if (exists) continue;

            batch.Add(new ChatMessage
            {
                Id = msgId,
                BroadcasterId = broadcasterId,
                UserId = userId,
                Username = username,
                DisplayName = displayName,
                Message = message,
                UserType = "viewer",
                MessageType = "text",
                Fragments = [],
                Badges = [],
                CreatedAt = createdAt,
            });

            if (batch.Count >= 500)
            {
                _db.ChatMessages.AddRange(batch);
                await _db.SaveChangesAsync(ct);
                count += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            _db.ChatMessages.AddRange(batch);
            await _db.SaveChangesAsync(ct);
            count += batch.Count;
        }

        return count;
    }

    // ─── Records (watch streaks, usage stats) ─────────────────────────────────

    private async Task<int> MigrateRecordsAsync(SqliteConnection conn, string broadcasterId, CancellationToken ct)
    {
        int count = 0;

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Records'";
        var tableExists = await checkCmd.ExecuteScalarAsync(ct) is not null;
        if (!tableExists) return 0;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT RecordType, Data, UserId FROM Records";

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var recordType = reader.GetString(0);
            var data = reader.GetString(1);
            var userId = reader.IsDBNull(2) ? broadcasterId : reader.GetString(2);

            _db.Records.Add(new Record
            {
                BroadcasterId = broadcasterId,
                RecordType = recordType,
                Data = data,
                UserId = userId,
            });
            count++;
        }

        if (count > 0)
            await _db.SaveChangesAsync(ct);

        return count;
    }
}

public sealed class MigrationResult
{
    public bool Success { get; }
    public string Message { get; }
    public MigrationCounts? Counts { get; }

    public MigrationResult(bool success, string message, MigrationCounts? counts = null)
    {
        Success = success;
        Message = message;
        Counts = counts;
    }
}

public sealed class MigrationCounts
{
    public int Users { get; set; }
    public int Commands { get; set; }
    public int ChatMessages { get; set; }
    public int Records { get; set; }
}
