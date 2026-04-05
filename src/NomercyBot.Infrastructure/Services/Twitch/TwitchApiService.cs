// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;
using NoMercyBot.Infrastructure.Configuration;

namespace NoMercyBot.Infrastructure.Services.Twitch;

/// <summary>
/// Twitch Helix API client.
/// Tokens are loaded from the Service entity (Name="twitch_bot" for the shared bot account,
/// Name="twitch" for per-broadcaster tokens needed for mod actions).
/// On 401, attempts a single token refresh and retries.
/// Respects Ratelimit-Reset header on 429.
/// </summary>
public sealed class TwitchApiService : ITwitchApiService
{
    private readonly IApplicationDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly ITwitchAuthService _authService;
    private readonly HttpClient _http;
    private readonly TwitchOptions _options;
    private readonly ILogger<TwitchApiService> _logger;

    private const string HelixBase = "https://api.twitch.tv/helix";

    public TwitchApiService(
        IApplicationDbContext db,
        IEncryptionService encryption,
        ITwitchAuthService authService,
        IHttpClientFactory httpClientFactory,
        IOptions<TwitchOptions> options,
        ILogger<TwitchApiService> logger
    )
    {
        _db = db;
        _encryption = encryption;
        _authService = authService;
        _http = httpClientFactory.CreateClient("twitch-helix");
        _options = options.Value;
        _logger = logger;
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    public async Task<TwitchUserInfo?> GetUserInfoAsync(
        string userId,
        CancellationToken ct = default
    )
    {
        var tokenInfo = await GetBotTokenAsync(ct);
        if (tokenInfo is null)
            return null;

        var response = await SendHelixAsync(
            HttpMethod.Get,
            $"{HelixBase}/users?id={Uri.EscapeDataString(userId)}",
            tokenInfo.Value,
            null,
            ct
        );

        if (response is null)
            return null;

        var data = await response.Content.ReadFromJsonAsync<HelixDataResponse<HelixUser>>(
            cancellationToken: ct
        );
        var user = data?.Data?.FirstOrDefault();
        if (user is null)
            return null;

        return new TwitchUserInfo(
            user.Id,
            user.Login,
            user.DisplayName,
            user.ProfileImageUrl,
            user.BroadcasterType
        );
    }

    public async Task<TwitchStreamInfo?> GetStreamInfoAsync(
        string broadcasterId,
        CancellationToken ct = default
    )
    {
        var tokenInfo = await GetBotTokenAsync(ct);
        if (tokenInfo is null)
            return null;

        var response = await SendHelixAsync(
            HttpMethod.Get,
            $"{HelixBase}/streams?user_id={Uri.EscapeDataString(broadcasterId)}",
            tokenInfo.Value,
            null,
            ct
        );

        if (response is null)
            return null;

        var data = await response.Content.ReadFromJsonAsync<HelixDataResponse<HelixStream>>(
            cancellationToken: ct
        );
        var stream = data?.Data?.FirstOrDefault();

        // When the stream is offline, the array is empty
        return stream is not null
            ? new TwitchStreamInfo(
                stream.Id,
                stream.UserId,
                stream.GameId,
                stream.GameName,
                stream.Title,
                true,
                stream.ViewerCount
            )
            : new TwitchStreamInfo(string.Empty, broadcasterId, null, null, null, false, 0);
    }

    public async Task<bool> TimeoutUserAsync(
        string broadcasterId,
        string userId,
        int durationSeconds,
        string? reason = null,
        CancellationToken ct = default
    )
    {
        return await BanOrTimeoutAsync(broadcasterId, userId, durationSeconds, reason, ct);
    }

    public async Task<bool> BanUserAsync(
        string broadcasterId,
        string userId,
        string? reason = null,
        CancellationToken ct = default
    )
    {
        return await BanOrTimeoutAsync(broadcasterId, userId, null, reason, ct);
    }

    public async Task<bool> UnbanUserAsync(
        string broadcasterId,
        string userId,
        CancellationToken ct = default
    )
    {
        var tokenInfo = await GetModeratorTokenAsync(broadcasterId, ct);
        if (tokenInfo is null)
        {
            _logger.LogWarning(
                "No moderator token for broadcaster {BroadcasterId}, skipping unban",
                broadcasterId
            );
            return false;
        }

        var url =
            $"{HelixBase}/moderation/bans"
            + $"?broadcaster_id={Uri.EscapeDataString(broadcasterId)}"
            + $"&moderator_id={Uri.EscapeDataString(broadcasterId)}"
            + $"&user_id={Uri.EscapeDataString(userId)}";

        var response = await SendHelixAsync(HttpMethod.Delete, url, tokenInfo.Value, null, ct);
        return response is { IsSuccessStatusCode: true };
    }

    public async Task<bool> SendChatMessageAsync(
        string broadcasterId,
        string senderUserId,
        string message,
        string? replyParentMessageId = null,
        CancellationToken ct = default
    )
    {
        var tokenInfo = await GetBotTokenAsync(ct);
        if (tokenInfo is null)
        {
            _logger.LogWarning(
                "No bot token available, cannot send chat message to {BroadcasterId}",
                broadcasterId
            );
            return false;
        }

        var body = replyParentMessageId is not null
            ? (object)
                new
                {
                    broadcaster_id = broadcasterId,
                    sender_id = senderUserId,
                    message,
                    reply_parent_message_id = replyParentMessageId,
                }
            : (object)
                new
                {
                    broadcaster_id = broadcasterId,
                    sender_id = senderUserId,
                    message,
                };

        var response = await SendHelixAsync(
            HttpMethod.Post,
            $"{HelixBase}/chat/messages",
            tokenInfo.Value,
            body,
            ct
        );
        if (response is null)
            return false;

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Helix SendChatMessage failed for {BroadcasterId}: {Status} — {Error}",
                broadcasterId,
                response.StatusCode,
                err
            );
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteChatMessageAsync(
        string broadcasterId,
        string messageId,
        CancellationToken ct = default
    )
    {
        var tokenInfo = await GetModeratorTokenAsync(broadcasterId, ct);
        if (tokenInfo is null)
            return false;

        var url =
            $"{HelixBase}/moderation/chat"
            + $"?broadcaster_id={Uri.EscapeDataString(broadcasterId)}"
            + $"&moderator_id={Uri.EscapeDataString(broadcasterId)}"
            + $"&message_id={Uri.EscapeDataString(messageId)}";

        var response = await SendHelixAsync(HttpMethod.Delete, url, tokenInfo.Value, null, ct);
        return response is { IsSuccessStatusCode: true };
    }

    public async Task<bool> ShoutoutAsync(
        string broadcasterId,
        string toUserId,
        string moderatorId,
        CancellationToken ct = default
    )
    {
        var token = await GetModeratorTokenAsync(broadcasterId, ct);
        if (token is null)
        {
            _logger.LogWarning(
                "No moderator token for broadcaster {BroadcasterId}, skipping shoutout",
                broadcasterId
            );
            return false;
        }

        var url =
            $"{HelixBase}/chat/shoutouts"
            + $"?from_broadcaster_id={Uri.EscapeDataString(broadcasterId)}"
            + $"&to_broadcaster_id={Uri.EscapeDataString(toUserId)}"
            + $"&moderator_id={Uri.EscapeDataString(moderatorId)}";

        var response = await SendHelixAsync(HttpMethod.Post, url, token.Value, new { }, ct);
        return response?.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────────

    private async Task<bool> BanOrTimeoutAsync(
        string broadcasterId,
        string userId,
        int? durationSeconds,
        string? reason,
        CancellationToken ct
    )
    {
        var tokenInfo = await GetModeratorTokenAsync(broadcasterId, ct);
        if (tokenInfo is null)
        {
            _logger.LogWarning(
                "No moderator token for broadcaster {BroadcasterId}, skipping ban/timeout",
                broadcasterId
            );
            return false;
        }

        var url =
            $"{HelixBase}/moderation/bans"
            + $"?broadcaster_id={Uri.EscapeDataString(broadcasterId)}"
            + $"&moderator_id={Uri.EscapeDataString(broadcasterId)}";

        var body = durationSeconds.HasValue
            ? (object)
                new
                {
                    data = new
                    {
                        user_id = userId,
                        duration = durationSeconds.Value,
                        reason = reason ?? string.Empty,
                    },
                }
            : (object)new { data = new { user_id = userId, reason = reason ?? string.Empty } };

        var response = await SendHelixAsync(HttpMethod.Post, url, tokenInfo.Value, body, ct);
        return response is { IsSuccessStatusCode: true };
    }

    /// <summary>
    /// Sends a Helix request with bearer auth. Handles 429 backoff and 401 refresh-and-retry.
    /// </summary>
    private async Task<HttpResponseMessage?> SendHelixAsync(
        HttpMethod method,
        string url,
        (string Token, string? BroadcasterId, string ServiceName) tokenInfo,
        object? body,
        CancellationToken ct
    )
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", $"Bearer {tokenInfo.Token}");
            request.Headers.Add("Client-Id", _options.ClientId);

            if (body is not null)
                request.Content = JsonContent.Create(body);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Helix request failed: {Method} {Url}", method, url);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (
                    response.Headers.TryGetValues("Ratelimit-Reset", out var values)
                    && long.TryParse(values.First(), out var resetEpoch)
                )
                {
                    var delay =
                        DateTimeOffset.FromUnixTimeSeconds(resetEpoch) - DateTimeOffset.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogWarning("Helix rate limited, waiting {Delay:g}", delay);
                        await Task.Delay(delay, ct);
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
                continue;
            }

            if (
                response.StatusCode == HttpStatusCode.Unauthorized
                && attempt == 0
                && tokenInfo.BroadcasterId is not null
            )
            {
                _logger.LogDebug(
                    "Helix 401, refreshing token for {BroadcasterId}",
                    tokenInfo.BroadcasterId
                );
                var refreshed = await _authService.RefreshTokenAsync(
                    tokenInfo.BroadcasterId,
                    tokenInfo.ServiceName,
                    ct
                );
                if (refreshed is null)
                    return response;

                tokenInfo = tokenInfo with { Token = refreshed.AccessToken };
                continue;
            }

            return response;
        }

        return null;
    }

    /// <summary>Get bot account token (Name="twitch_bot", no BroadcasterId).</summary>
    private async Task<(string Token, string? BroadcasterId, string ServiceName)?> GetBotTokenAsync(
        CancellationToken ct
    )
    {
        var service = await _db
            .Services.Where(s => s.Name == "twitch_bot" && s.Enabled && s.AccessToken != null)
            .OrderByDescending(s => s.TokenExpiry)
            .FirstOrDefaultAsync(ct);

        if (service?.AccessToken is null)
            return null;

        var token = _encryption.TryDecrypt(service.AccessToken);
        if (token is null)
            return null;

        return (token, service.BroadcasterId, service.Name);
    }

    /// <summary>Get moderator/broadcaster token for a specific channel (Name="twitch").</summary>
    private async Task<(
        string Token,
        string? BroadcasterId,
        string ServiceName
    )?> GetModeratorTokenAsync(string broadcasterId, CancellationToken ct)
    {
        var service = await _db.Services.FirstOrDefaultAsync(
            s =>
                s.BroadcasterId == broadcasterId
                && s.Name == "twitch"
                && s.Enabled
                && s.AccessToken != null,
            ct
        );

        if (service?.AccessToken is null)
        {
            // Fall back to bot token
            return await GetBotTokenAsync(ct);
        }

        var token = _encryption.TryDecrypt(service.AccessToken);
        if (token is null)
            return null;

        return (token, broadcasterId, service.Name);
    }

    // ─── Helix response models ────────────────────────────────────────────────────

    private sealed class HelixDataResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }
    }

    private sealed class HelixUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("login")]
        public string Login { get; set; } = null!;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = null!;

        [JsonPropertyName("profile_image_url")]
        public string? ProfileImageUrl { get; set; }

        [JsonPropertyName("broadcaster_type")]
        public string BroadcasterType { get; set; } = null!;
    }

    private sealed class HelixStream
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = null!;

        [JsonPropertyName("game_id")]
        public string? GameId { get; set; }

        [JsonPropertyName("game_name")]
        public string? GameName { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("viewer_count")]
        public int ViewerCount { get; set; }
    }
}
