namespace NoMercyBot.Application.DTOs.Users;

/// <summary>Full user information.</summary>
public sealed record UserDto(
    string Id,
    string Username,
    string DisplayName,
    string? ProfileImageUrl,
    string? Email,
    DateTime CreatedAt,
    DateTime LastLoginAt
);

/// <summary>User profile information for display.</summary>
public sealed record UserProfileDto(
    string Id,
    string Username,
    string DisplayName,
    string? ProfileImageUrl,
    string? Email,
    string? Pronoun,
    DateTime CreatedAt,
    DateTime LastLoginAt
);

/// <summary>Lightweight user info for search results and dropdowns.</summary>
public sealed record UserSearchResult(
    string Id,
    string Username,
    string DisplayName,
    string? ProfileImageUrl
);

/// <summary>Request to update a user profile.</summary>
public sealed record UpdateUserProfileRequest
{
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
}

/// <summary>GDPR data summary for a user.</summary>
public sealed record UserStatsDto(
    int MessageCount,
    double WatchHours,
    int ChannelsCount,
    int CommandsUsed,
    DateTime? FirstSeen,
    DateTime? LastActive,
    bool ExportAvailable
);
