namespace NoMercyBot.Application.DTOs.Rewards;

public sealed record RewardListItem(
    string Id,
    string Title,
    int Cost,
    bool IsEnabled,
    string? BackgroundColor,
    string? ImageUrl,
    DateTime CreatedAt
);

public sealed record RewardDetail(
    string Id,
    string Title,
    string? Prompt,
    int Cost,
    bool IsEnabled,
    bool IsUserInputRequired,
    bool IsPaused,
    string? BackgroundColor,
    string? ImageUrl,
    int? MaxPerStream,
    int? MaxPerUserPerStream,
    int? GlobalCooldownSeconds,
    string? ActionType,
    Dictionary<string, object?>? ActionSettings,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record CreateRewardRequest
{
    public required string Title { get; init; }
    public required int Cost { get; init; }
    public string? Prompt { get; init; }
    public bool IsUserInputRequired { get; init; }
    public string? BackgroundColor { get; init; }
    public int? MaxPerStream { get; init; }
    public int? MaxPerUserPerStream { get; init; }
    public int? GlobalCooldownSeconds { get; init; }
    public string? ActionType { get; init; }
    public Dictionary<string, object?>? ActionSettings { get; init; }
}

public sealed record UpdateRewardRequest
{
    public string? Title { get; init; }
    public int? Cost { get; init; }
    public string? Prompt { get; init; }
    public bool? IsUserInputRequired { get; init; }
    public bool? IsEnabled { get; init; }
    public bool? IsPaused { get; init; }
    public string? BackgroundColor { get; init; }
    public int? MaxPerStream { get; init; }
    public int? MaxPerUserPerStream { get; init; }
    public int? GlobalCooldownSeconds { get; init; }
    public string? ActionType { get; init; }
    public Dictionary<string, object?>? ActionSettings { get; init; }
}
