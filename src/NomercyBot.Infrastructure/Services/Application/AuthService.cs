// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.Contracts.Twitch;
using NoMercyBot.Application.DTOs.Auth;
using NoMercyBot.Application.DTOs.Users;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;
using NoMercyBot.Infrastructure.Configuration;

namespace NoMercyBot.Infrastructure.Services.Application;

/// <summary>
/// Handles Twitch OAuth authentication:
/// 1. Redirect to Twitch authorization URL
/// 2. Exchange code for tokens
/// 3. Upsert user record
/// 4. Issue a platform JWT
/// 5. Store Twitch tokens encrypted in Service table
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly ITwitchAuthService _twitchAuth;
    private readonly IJwtTokenService _jwt;
    private readonly IEncryptionService _encryption;
    private readonly HttpClient _http;
    private readonly TwitchOptions _options;
    private readonly ILogger<AuthService> _logger;

    private static readonly string[] RequiredScopes =
    [
        "user:read:email",
        "channel:read:subscriptions",
        "channel:manage:redemptions",
        "moderator:read:chatters",
        "moderator:manage:banned_users",
        "chat:read",
        "chat:edit",
    ];

    public AuthService(
        IApplicationDbContext db,
        ITwitchAuthService twitchAuth,
        IJwtTokenService jwt,
        IEncryptionService encryption,
        IHttpClientFactory httpClientFactory,
        IOptions<TwitchOptions> options,
        ILogger<AuthService> logger)
    {
        _db = db;
        _twitchAuth = twitchAuth;
        _jwt = jwt;
        _encryption = encryption;
        _http = httpClientFactory.CreateClient("twitch-helix");
        _options = options.Value;
        _logger = logger;
    }

    public string GetTwitchOAuthUrl(string? state = null)
    {
        var scopes = Uri.EscapeDataString(string.Join(" ", RequiredScopes));
        var redirectUri = Uri.EscapeDataString(_options.RedirectUri);
        var clientId = Uri.EscapeDataString(_options.ClientId);
        var stateParam = state is not null ? $"&state={Uri.EscapeDataString(state)}" : string.Empty;

        return $"https://id.twitch.tv/oauth2/authorize" +
               $"?client_id={clientId}" +
               $"&redirect_uri={redirectUri}" +
               $"&response_type=code" +
               $"&scope={scopes}" +
               $"&force_verify=true" +
               stateParam;
    }

    public async Task<Result<AuthResultDto>> HandleTwitchCallbackAsync(
        OAuthCallbackDto callback,
        CancellationToken cancellationToken = default)
    {
        // Exchange code for tokens
        var tokens = await _twitchAuth.ExchangeCodeAsync(callback.Code, _options.RedirectUri, cancellationToken);
        if (tokens is null)
            return Result.Failure<AuthResultDto>("Failed to exchange authorization code.", "TOKEN_EXCHANGE_FAILED");

        // Fetch Twitch user info using the fresh access token (returns authenticated user, no id query)
        var twitchUser = await GetUserFromTokenAsync(tokens.AccessToken, cancellationToken);
        if (twitchUser is null)
            return Result.Failure<AuthResultDto>("Failed to fetch Twitch user info.", "USER_FETCH_FAILED");

        // Upsert user
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == twitchUser.Id, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = twitchUser.Id,
                Username = twitchUser.Login,
                DisplayName = twitchUser.DisplayName,
                ProfileImageUrl = twitchUser.ProfileImageUrl,
                BroadcasterType = twitchUser.BroadcasterType,
                Enabled = true,
            };
            _db.Users.Add(user);
        }
        else
        {
            user.Username = twitchUser.Login;
            user.DisplayName = twitchUser.DisplayName;
            user.ProfileImageUrl = twitchUser.ProfileImageUrl;
        }

        // Store Twitch tokens in Service table
        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.BroadcasterId == twitchUser.Id && s.Name == "twitch", cancellationToken);

        if (service is null)
        {
            service = new Domain.Entities.Service
            {
                Name = "twitch",
                BroadcasterId = twitchUser.Id,
                UserId = twitchUser.Id,
                Enabled = true,
            };
            _db.Services.Add(service);
        }

        service.AccessToken = _encryption.Encrypt(tokens.AccessToken);
        service.RefreshToken = _encryption.Encrypt(tokens.RefreshToken);
        service.TokenExpiry = tokens.ExpiresAt;
        service.Scopes = tokens.Scopes;

        await _db.SaveChangesAsync(cancellationToken);

        // Issue platform JWT
        var platformJwt = _jwt.GenerateToken(twitchUser.Id, twitchUser.Login, ["user"]);
        var refreshJwt = _jwt.GenerateToken(twitchUser.Id, twitchUser.Login, ["refresh"]);

        var userDto = new UserDto(
            twitchUser.Id,
            twitchUser.Login,
            twitchUser.DisplayName,
            twitchUser.ProfileImageUrl,
            null,
            user.CreatedAt,
            user.UpdatedAt);

        _logger.LogInformation("User {UserId} authenticated via Twitch OAuth", twitchUser.Id);

        return Result.Success(new AuthResultDto(
            platformJwt,
            refreshJwt,
            DateTime.UtcNow.AddHours(1),
            userDto));
    }

    public async Task<Result<AuthResultDto>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var principal = _jwt.ValidateToken(refreshToken);
        if (principal is null)
            return Result.Failure<AuthResultDto>("Invalid or expired refresh token.", "INVALID_TOKEN");

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Result.Failure<AuthResultDto>("Token missing user ID.", "INVALID_TOKEN");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result.Failure<AuthResultDto>("User not found.", "NOT_FOUND");

        var newJwt = _jwt.GenerateToken(user.Id, user.Username, ["user"]);
        var newRefresh = _jwt.GenerateToken(user.Id, user.Username, ["refresh"]);

        var userDto = new UserDto(
            user.Id,
            user.Username,
            user.DisplayName,
            user.ProfileImageUrl,
            null,
            user.CreatedAt,
            user.UpdatedAt);

        return Result.Success(new AuthResultDto(newJwt, newRefresh, DateTime.UtcNow.AddHours(1), userDto));
    }

    public async Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _twitchAuth.RevokeTokenAsync(userId, "twitch", cancellationToken);
            _logger.LogInformation("User {UserId} logged out", userId);
            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return Result.Failure("Logout failed.", "LOGOUT_FAILED");
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls GET /users (no id param) using the user's own access token to get their profile.
    /// This is the correct flow after OAuth code exchange.
    /// </summary>
    private async Task<TwitchUserInfo?> GetUserFromTokenAsync(string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("Client-Id", _options.ClientId);

        try
        {
            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadFromJsonAsync<HelixDataResponse<HelixUser>>(cancellationToken: ct);
            var user = data?.Data?.FirstOrDefault();
            if (user is null) return null;

            return new TwitchUserInfo(user.Id, user.Login, user.DisplayName, user.ProfileImageUrl, user.BroadcasterType);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch current Twitch user from token");
            return null;
        }
    }

    private sealed class HelixDataResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }
    }

    private sealed class HelixUser
    {
        [JsonPropertyName("id")] public string Id { get; set; } = null!;
        [JsonPropertyName("login")] public string Login { get; set; } = null!;
        [JsonPropertyName("display_name")] public string DisplayName { get; set; } = null!;
        [JsonPropertyName("profile_image_url")] public string? ProfileImageUrl { get; set; }
        [JsonPropertyName("broadcaster_type")] public string BroadcasterType { get; set; } = "";
    }
}
