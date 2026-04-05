using NoMercyBot.Application.DTOs.Users;

namespace NoMercyBot.Application.DTOs.Auth;

/// <summary>Authentication result containing tokens and user info.</summary>
public sealed record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

/// <summary>OAuth callback data received from the Twitch redirect.</summary>
public sealed record OAuthCallbackDto
{
    public required string Code { get; init; }
    public string? State { get; init; }
}

/// <summary>Token refresh request.</summary>
public sealed record RefreshTokenRequest(string RefreshToken);
