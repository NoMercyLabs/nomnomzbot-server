using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Auth;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Application service for authentication: OAuth callbacks, token refresh, and logout.
/// </summary>
public interface IAuthService
{
    /// <summary>Get the Twitch OAuth authorization URL.</summary>
    string GetTwitchOAuthUrl(string? state = null);

    /// <summary>Handle the OAuth callback from Twitch and return auth tokens.</summary>
    Task<Result<AuthResultDto>> HandleTwitchCallbackAsync(OAuthCallbackDto callback, CancellationToken cancellationToken = default);

    /// <summary>Refresh an expired access token.</summary>
    Task<Result<AuthResultDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Log out a user, revoking their tokens.</summary>
    Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default);
}
