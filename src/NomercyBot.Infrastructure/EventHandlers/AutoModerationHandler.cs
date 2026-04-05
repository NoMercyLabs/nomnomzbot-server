// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>
/// Auto-moderation handler that runs on every incoming chat message.
///
/// Supported rule types (stored in Record.Data JSON via ModerationService):
///   - "caps"           — timeout if caps percentage exceeds threshold
///   - "links"          — timeout/ban if message contains a URL
///   - "banned_phrases" — timeout/ban if message contains a banned phrase
///
/// Rules are loaded from the DB per-channel and cached for 5 minutes to avoid hot-path DB hits.
/// Exemptions: moderators and the broadcaster are never auto-moderated.
/// </summary>
public sealed partial class AutoModerationHandler : IEventHandler<ChatMessageReceivedEvent>
{
    private static readonly TimeSpan RuleCacheExpiry = TimeSpan.FromMinutes(5);

    // Per-channel rule cache: key = broadcasterId
    private readonly ConcurrentDictionary<string, CachedRules> _ruleCache = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoModerationHandler> _logger;

    public AutoModerationHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AutoModerationHandler> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(
        ChatMessageReceivedEvent @event,
        CancellationToken cancellationToken
    )
    {
        // Exempt moderators and broadcaster from auto-mod
        if (@event.IsModerator || @event.IsBroadcaster)
            return;

        var broadcasterId = @event.BroadcasterId;
        if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(@event.Message))
            return;

        var rules = await GetRulesAsync(broadcasterId, cancellationToken);
        if (rules.Count == 0)
            return;

        var message = @event.Message;

        foreach (var rule in rules)
        {
            if (!rule.IsEnabled)
                continue;
            if (!ShouldApply(rule, @event))
                continue;

            var triggered = rule.Type switch
            {
                "caps" => CheckCaps(message, rule),
                "links" => CheckLinks(message),
                "banned_phrases" => CheckBannedPhrases(message, rule),
                "emote_spam" => CheckEmoteSpam(@event.Fragments, rule),
                _ => false,
            };

            if (!triggered)
                continue;

            _logger.LogInformation(
                "AutoMod rule '{Rule}' ({Type}) triggered for user {User} in channel {Channel}: \"{Message}\"",
                rule.Name,
                rule.Type,
                @event.UserLogin,
                broadcasterId,
                message
            );

            await ApplyActionAsync(
                rule,
                broadcasterId,
                @event.UserId,
                @event.MessageId,
                cancellationToken
            );

            // Stop after first matching rule
            return;
        }
    }

    // ─── Rule checks ──────────────────────────────────────────────────────────

    private static bool CheckCaps(string message, AutoModRule rule)
    {
        // Only test alphabetic characters
        var letters = message.Count(char.IsLetter);
        if (letters < 5)
            return false; // Too short to enforce

        var upper = message.Count(char.IsUpper);
        var ratio = (double)upper / letters;

        var threshold =
            rule.Settings.TryGetValue("threshold", out var t)
            && t is JsonElement te
            && te.ValueKind == JsonValueKind.Number
                ? te.GetDouble()
                : 0.7; // Default: 70% caps

        var minLength =
            rule.Settings.TryGetValue("min_length", out var ml)
            && ml is JsonElement mle
            && mle.ValueKind == JsonValueKind.Number
                ? mle.GetInt32()
                : 10;

        return message.Length >= minLength && ratio >= threshold;
    }

    private static bool CheckLinks(string message) => UrlPattern().IsMatch(message);

    private static bool CheckBannedPhrases(string message, AutoModRule rule)
    {
        if (!rule.Settings.TryGetValue("phrases", out var phrasesObj))
            return false;
        if (
            phrasesObj is not JsonElement phrasesElem
            || phrasesElem.ValueKind != JsonValueKind.Array
        )
            return false;

        var lower = message.ToLowerInvariant();
        foreach (var phrase in phrasesElem.EnumerateArray())
        {
            var p = phrase.GetString();
            if (!string.IsNullOrEmpty(p) && lower.Contains(p.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static bool CheckEmoteSpam(
        IReadOnlyList<NoMercyBot.Domain.ValueObjects.ChatMessageFragment> fragments,
        AutoModRule rule
    )
    {
        var maxEmotes =
            rule.Settings.TryGetValue("max_emotes", out var maxObj)
            && maxObj is JsonElement maxElem
            && maxElem.ValueKind == JsonValueKind.Number
                ? maxElem.GetInt32()
                : 10; // Default: 10 emotes max

        var emoteCount = fragments.Count(f =>
            f.Type.Equals("emote", StringComparison.OrdinalIgnoreCase)
        );
        return emoteCount > maxEmotes;
    }

    private static bool ShouldApply(AutoModRule rule, ChatMessageReceivedEvent @event)
    {
        // Check exempt roles
        if (rule.ExemptRoles.Count == 0)
            return true;

        foreach (var role in rule.ExemptRoles)
        {
            var exempt = role.ToLowerInvariant() switch
            {
                "subscriber" or "sub" => @event.IsSubscriber,
                "vip" => @event.IsVip,
                "moderator" or "mod" => @event.IsModerator,
                "broadcaster" => @event.IsBroadcaster,
                _ => false,
            };
            if (exempt)
                return false;
        }

        return true;
    }

    // ─── Action dispatch ──────────────────────────────────────────────────────

    private async Task ApplyActionAsync(
        AutoModRule rule,
        string broadcasterId,
        string userId,
        string messageId,
        CancellationToken ct
    )
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var twitchApi = scope.ServiceProvider.GetRequiredService<ITwitchApiService>();

            switch (rule.Action.ToLowerInvariant())
            {
                case "timeout":
                    var duration = rule.DurationSeconds ?? 60;
                    await twitchApi.TimeoutUserAsync(
                        broadcasterId,
                        userId,
                        duration,
                        rule.Reason ?? rule.Name,
                        ct
                    );
                    break;

                case "ban":
                    await twitchApi.BanUserAsync(
                        broadcasterId,
                        userId,
                        rule.Reason ?? rule.Name,
                        ct
                    );
                    break;

                case "delete":
                    await twitchApi.DeleteChatMessageAsync(broadcasterId, messageId, ct);
                    break;

                default:
                    _logger.LogWarning("Unknown auto-mod action '{Action}'", rule.Action);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to apply auto-mod action '{Action}' for user {UserId}",
                rule.Action,
                userId
            );
        }
    }

    // ─── Rule loading (cached) ────────────────────────────────────────────────

    private async Task<IReadOnlyList<AutoModRule>> GetRulesAsync(
        string broadcasterId,
        CancellationToken ct
    )
    {
        var now = DateTimeOffset.UtcNow;

        if (
            _ruleCache.TryGetValue(broadcasterId, out var cached)
            && now - cached.CachedAt < RuleCacheExpiry
        )
        {
            return cached.Rules;
        }

        var rules = await LoadRulesFromDbAsync(broadcasterId, ct);
        _ruleCache[broadcasterId] = new CachedRules(rules, now);
        return rules;
    }

    private async Task<IReadOnlyList<AutoModRule>> LoadRulesFromDbAsync(
        string broadcasterId,
        CancellationToken ct
    )
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var records = await db
                .Records.Where(r =>
                    r.BroadcasterId == broadcasterId && r.RecordType == "moderation_rule"
                )
                .ToListAsync(ct);

            return records
                .Select(r =>
                {
                    try
                    {
                        return ParseRule(r.Data);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(r => r is not null)
                .Select(r => r!)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Failed to load auto-mod rules for {BroadcasterId}",
                broadcasterId
            );
            return [];
        }
    }

    private static AutoModRule ParseRule(string data)
    {
        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        return new AutoModRule
        {
            Name = root.TryGetProperty("Name", out var n)
                ? n.GetString() ?? string.Empty
                : string.Empty,
            Type = root.TryGetProperty("Type", out var t)
                ? t.GetString() ?? string.Empty
                : string.Empty,
            Action = root.TryGetProperty("Action", out var a)
                ? a.GetString() ?? "timeout"
                : "timeout",
            IsEnabled = !root.TryGetProperty("IsEnabled", out var e) || e.GetBoolean(),
            DurationSeconds =
                root.TryGetProperty("DurationSeconds", out var d)
                && d.ValueKind == JsonValueKind.Number
                    ? d.GetInt32()
                    : (int?)null,
            Reason = root.TryGetProperty("Reason", out var r) ? r.GetString() : null,
            Settings =
                root.TryGetProperty("Settings", out var s) && s.ValueKind == JsonValueKind.Object
                    ? s.EnumerateObject().ToDictionary(p => p.Name, p => (object)p.Value.Clone())
                    : new Dictionary<string, object>(),
            ExemptRoles =
                root.TryGetProperty("ExemptRoles", out var er)
                && er.ValueKind == JsonValueKind.Array
                    ? er.EnumerateArray()
                        .Select(x => x.GetString() ?? string.Empty)
                        .Where(x => x.Length > 0)
                        .ToList()
                    : [],
        };
    }

    // ─── Inner types ──────────────────────────────────────────────────────────

    private sealed class AutoModRule
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Action { get; set; } = "timeout";
        public bool IsEnabled { get; set; } = true;
        public int? DurationSeconds { get; set; }
        public string? Reason { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
        public List<string> ExemptRoles { get; set; } = [];
    }

    private sealed record CachedRules(IReadOnlyList<AutoModRule> Rules, DateTimeOffset CachedAt);

    [GeneratedRegex(@"https?://[^\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
