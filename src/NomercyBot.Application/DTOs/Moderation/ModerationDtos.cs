namespace NoMercyBot.Application.DTOs.Moderation;

public sealed record ModerationRuleListItem(
    int Id,
    string Name,
    string Type,
    bool IsEnabled,
    string Action,
    int? DurationSeconds,
    DateTime CreatedAt);

public sealed record ModerationRuleDetail(
    int Id,
    string Name,
    string Type,
    bool IsEnabled,
    string Action,
    int? DurationSeconds,
    string? Reason,
    Dictionary<string, object?> Settings,
    List<string> ExemptRoles,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ModerationActionLog(
    string Id,
    string Action,
    string ModeratorId,
    string ModeratorUsername,
    string? TargetUserId,
    string? TargetUsername,
    string? Reason,
    int? DurationSeconds,
    DateTime Timestamp);

public sealed record CreateModerationRuleRequest
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Action { get; init; }
    public int? DurationSeconds { get; init; }
    public string? Reason { get; init; }
    public Dictionary<string, object?>? Settings { get; init; }
    public List<string>? ExemptRoles { get; init; }
}

public sealed record UpdateModerationRuleRequest
{
    public string? Name { get; init; }
    public string? Action { get; init; }
    public int? DurationSeconds { get; init; }
    public string? Reason { get; init; }
    public Dictionary<string, object?>? Settings { get; init; }
    public List<string>? ExemptRoles { get; init; }
    public bool? IsEnabled { get; init; }
}

public sealed record PerformModerationActionRequest
{
    public required string Action { get; init; }
    public required string TargetUserId { get; init; }
    public string? Reason { get; init; }
    public int? DurationSeconds { get; init; }
}

public sealed record ModerationActionResult(
    bool Success,
    string? Message);
