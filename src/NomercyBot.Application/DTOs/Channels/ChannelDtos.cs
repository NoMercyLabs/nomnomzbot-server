namespace NoMercyBot.Application.DTOs.Channels;

/// <summary>Full channel detail for dashboard views.</summary>
public sealed record ChannelDto(
    string Id,
    string Name,
    string DisplayName,
    string? ProfileImageUrl,
    bool IsLive,
    bool IsOnboarded,
    string? Title,
    string? GameName,
    int? ViewerCount,
    DateTime? BotJoinedAt,
    string SubscriptionTier,
    DateTime CreatedAt
);

/// <summary>Lightweight channel info for lists and dropdowns.</summary>
public sealed record ChannelSummaryDto(
    string Id,
    string Name,
    string DisplayName,
    string? ProfileImageUrl,
    bool IsLive,
    string Role,
    int? ViewerCount
);

/// <summary>Lightweight channel info returned when looking up by overlay token.</summary>
public sealed record ChannelOverlayInfo(string BroadcasterId, string DisplayName);

/// <summary>Request to update channel settings.</summary>
public sealed record UpdateChannelSettingsDto
{
    public string? DisplayName { get; init; }
    public string? SubscriptionTier { get; init; }
    public string? Prefix { get; init; }
    public string? Locale { get; init; }
    public bool? AutoJoin { get; init; }
}

/// <summary>Request to create/onboard a new channel.</summary>
public sealed record CreateChannelRequest
{
    public required string BroadcasterId { get; init; }
    public string? DisplayName { get; init; }
}
