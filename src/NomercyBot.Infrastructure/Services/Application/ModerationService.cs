// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Moderation;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;
using System.Text.Json;

namespace NoMercyBot.Infrastructure.Services.Application;

public class ModerationService : IModerationService
{
    private const string RuleRecordType = "moderation_rule";
    private const string ActionRecordType = "moderation_action";

    private readonly IApplicationDbContext _db;

    public ModerationService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ModerationActionResult>> TimeoutAsync(
        string broadcasterId,
        string targetUserId,
        int durationSeconds,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return await RecordActionAsync(broadcasterId, "timeout", targetUserId, reason, durationSeconds, cancellationToken);
    }

    public async Task<Result<ModerationActionResult>> BanAsync(
        string broadcasterId,
        string targetUserId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return await RecordActionAsync(broadcasterId, "ban", targetUserId, reason, null, cancellationToken);
    }

    public async Task<Result<ModerationActionResult>> UnbanAsync(
        string broadcasterId,
        string targetUserId,
        CancellationToken cancellationToken = default)
    {
        return await RecordActionAsync(broadcasterId, "unban", targetUserId, null, null, cancellationToken);
    }

    public async Task<Result<ModerationRuleDetail>> CreateRuleAsync(
        string broadcasterId,
        CreateModerationRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var channelExists = await _db.Channels.AnyAsync(c => c.Id == broadcasterId, cancellationToken);
        if (!channelExists)
            return Errors.ChannelNotFound<ModerationRuleDetail>(broadcasterId);

        var ruleData = new ModerationRuleData
        {
            Name = request.Name,
            Type = request.Type,
            Action = request.Action,
            DurationSeconds = request.DurationSeconds,
            Reason = request.Reason,
            Settings = request.Settings ?? new Dictionary<string, object?>(),
            ExemptRoles = request.ExemptRoles ?? [],
            IsEnabled = true,
        };

        var record = new Record
        {
            BroadcasterId = broadcasterId,
            RecordType = RuleRecordType,
            Data = JsonSerializer.Serialize(ruleData),
            UserId = broadcasterId, // system record — use broadcaster as owner
        };

        _db.Records.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ModerationRuleDetail(
            record.Id,
            ruleData.Name,
            ruleData.Type,
            ruleData.IsEnabled,
            ruleData.Action,
            ruleData.DurationSeconds,
            ruleData.Reason,
            ruleData.Settings,
            ruleData.ExemptRoles,
            record.CreatedAt,
            record.UpdatedAt));
    }

    public async Task<Result> DeleteRuleAsync(
        string broadcasterId,
        int ruleId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Records
            .FirstOrDefaultAsync(r => r.Id == ruleId
                && r.BroadcasterId == broadcasterId
                && r.RecordType == RuleRecordType, cancellationToken);

        if (record is null)
            return Result.Failure($"Moderation rule '{ruleId}' was not found.", "NOT_FOUND");

        _db.Records.Remove(record);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<ModerationRuleDetail>> UpdateRuleAsync(
        string broadcasterId,
        int ruleId,
        UpdateModerationRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Records
            .FirstOrDefaultAsync(r => r.Id == ruleId
                && r.BroadcasterId == broadcasterId
                && r.RecordType == RuleRecordType, cancellationToken);

        if (record is null)
            return Errors.NotFound<ModerationRuleDetail>("Moderation rule", ruleId.ToString());

        var ruleData = JsonSerializer.Deserialize<ModerationRuleData>(record.Data)
            ?? new ModerationRuleData();

        if (request.Name is not null) ruleData.Name = request.Name;
        if (request.Action is not null) ruleData.Action = request.Action;
        if (request.DurationSeconds.HasValue) ruleData.DurationSeconds = request.DurationSeconds.Value;
        if (request.Reason is not null) ruleData.Reason = request.Reason;
        if (request.Settings is not null) ruleData.Settings = request.Settings;
        if (request.ExemptRoles is not null) ruleData.ExemptRoles = request.ExemptRoles;
        if (request.IsEnabled.HasValue) ruleData.IsEnabled = request.IsEnabled.Value;

        record.Data = JsonSerializer.Serialize(ruleData);
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ModerationRuleDetail(
            record.Id,
            ruleData.Name,
            ruleData.Type,
            ruleData.IsEnabled,
            ruleData.Action,
            ruleData.DurationSeconds,
            ruleData.Reason,
            ruleData.Settings,
            ruleData.ExemptRoles,
            record.CreatedAt,
            record.UpdatedAt));
    }

    public async Task<Result<PagedList<ModerationRuleListItem>>> ListRulesAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Records
            .Where(r => r.BroadcasterId == broadcasterId && r.RecordType == RuleRecordType);

        var total = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = records.Select(r =>
        {
            var data = JsonSerializer.Deserialize<ModerationRuleData>(r.Data) ?? new ModerationRuleData();
            return new ModerationRuleListItem(r.Id, data.Name, data.Type, data.IsEnabled, data.Action, data.DurationSeconds, r.CreatedAt);
        }).ToList();

        return Result.Success(new PagedList<ModerationRuleListItem>(items, total, pagination.Page, pagination.PageSize));
    }

    public async Task<Result<PagedList<ModerationActionLog>>> GetActionsAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Records
            .Include(r => r.User)
            .Where(r => r.BroadcasterId == broadcasterId && r.RecordType == ActionRecordType);

        var total = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = records.Select(r =>
        {
            var data = JsonSerializer.Deserialize<ModerationActionData>(r.Data) ?? new ModerationActionData();
            return new ModerationActionLog(
                r.Id.ToString(),
                data.Action,
                r.UserId,
                r.User?.Username ?? r.UserId,
                data.TargetUserId,
                data.TargetUsername,
                data.Reason,
                data.DurationSeconds,
                r.CreatedAt);
        }).ToList();

        return Result.Success(new PagedList<ModerationActionLog>(items, total, pagination.Page, pagination.PageSize));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Result<ModerationActionResult>> RecordActionAsync(
        string broadcasterId,
        string action,
        string targetUserId,
        string? reason,
        int? durationSeconds,
        CancellationToken cancellationToken)
    {
        var channelExists = await _db.Channels.AnyAsync(c => c.Id == broadcasterId, cancellationToken);
        if (!channelExists)
            return Errors.ChannelNotFound<ModerationActionResult>(broadcasterId);

        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);

        var actionData = new ModerationActionData
        {
            Action = action,
            TargetUserId = targetUserId,
            TargetUsername = targetUser?.Username,
            Reason = reason,
            DurationSeconds = durationSeconds,
        };

        var record = new Record
        {
            BroadcasterId = broadcasterId,
            RecordType = ActionRecordType,
            Data = JsonSerializer.Serialize(actionData),
            UserId = broadcasterId,
        };

        _db.Records.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ModerationActionResult(true, $"{action} applied successfully."));
    }

    // ─── Private data shapes stored in Record.Data ───────────────────────────

    private sealed class ModerationRuleData
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int? DurationSeconds { get; set; }
        public string? Reason { get; set; }
        public Dictionary<string, object?> Settings { get; set; } = new();
        public List<string> ExemptRoles { get; set; } = [];
        public bool IsEnabled { get; set; } = true;
    }

    private sealed class ModerationActionData
    {
        public string Action { get; set; } = string.Empty;
        public string? TargetUserId { get; set; }
        public string? TargetUsername { get; set; }
        public string? Reason { get; set; }
        public int? DurationSeconds { get; set; }
    }
}
