using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Moderation;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Application service for moderation actions and auto-moderation rule management.
/// </summary>
public interface IModerationService
{
    /// <summary>Timeout a user in a channel.</summary>
    Task<Result<ModerationActionResult>> TimeoutAsync(
        string broadcasterId,
        string targetUserId,
        int durationSeconds,
        string? reason = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Ban a user from a channel.</summary>
    Task<Result<ModerationActionResult>> BanAsync(
        string broadcasterId,
        string targetUserId,
        string? reason = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Unban a user in a channel.</summary>
    Task<Result<ModerationActionResult>> UnbanAsync(
        string broadcasterId,
        string targetUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Create an auto-moderation rule.</summary>
    Task<Result<ModerationRuleDetail>> CreateRuleAsync(
        string broadcasterId,
        CreateModerationRuleRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Delete an auto-moderation rule.</summary>
    Task<Result> DeleteRuleAsync(
        string broadcasterId,
        int ruleId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Update an existing moderation rule.</summary>
    Task<Result<ModerationRuleDetail>> UpdateRuleAsync(
        string broadcasterId,
        int ruleId,
        UpdateModerationRuleRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>List all moderation rules in a channel.</summary>
    Task<Result<PagedList<ModerationRuleListItem>>> ListRulesAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get moderation action history for a channel.</summary>
    Task<Result<PagedList<ModerationActionLog>>> GetActionsAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get the auto-moderation config (link filter, caps filter, banned phrases, emote spam).</summary>
    Task<Result<AutomodConfigDto>> GetAutomodConfigAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Save the auto-moderation config, upserting the four built-in rule types.</summary>
    Task<Result<AutomodConfigDto>> SaveAutomodConfigAsync(
        string broadcasterId,
        AutomodConfigDto config,
        CancellationToken cancellationToken = default
    );

    /// <summary>List users currently banned in a channel.</summary>
    Task<Result<List<BannedUserDto>>> GetBannedUsersAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    );
}
