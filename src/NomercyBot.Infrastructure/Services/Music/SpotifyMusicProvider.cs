// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Music;

/// <summary>
/// Spotify music provider using the official Spotify Web API.
///
/// Token management:
///   - Tokens are stored per-broadcaster in the Service entity (Name = "spotify").
///   - Access tokens are refreshed automatically using the stored refresh token.
///   - Token state is persisted back to the database after each refresh.
///
/// All operations require the Spotify app to be connected (OAuth flow completed separately).
/// </summary>
public sealed class SpotifyMusicProvider : IMusicProvider
{
    private const string ProviderName = "spotify";
    private const string SpotifyApiBase = "https://api.spotify.com/v1";
    private const string SpotifyTokenUrl = "https://accounts.spotify.com/api/token";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SpotifyMusicProvider> _logger;

    public SpotifyMusicProvider(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<SpotifyMusicProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task PlayAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthorizedClientAsync(broadcasterId, cancellationToken);
        if (client is null) return;

        var response = await client.PutAsync(
            $"{SpotifyApiBase}/me/player/play",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Spotify play failed: {Status}", response.StatusCode);
    }

    public async Task PauseAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthorizedClientAsync(broadcasterId, cancellationToken);
        if (client is null) return;

        var response = await client.PutAsync(
            $"{SpotifyApiBase}/me/player/pause",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Spotify pause failed: {Status}", response.StatusCode);
    }

    public async Task SkipAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthorizedClientAsync(broadcasterId, cancellationToken);
        if (client is null) return;

        var response = await client.PostAsync(
            $"{SpotifyApiBase}/me/player/next",
            null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Spotify skip failed: {Status}", response.StatusCode);
    }

    public async Task<TrackInfo?> GetCurrentTrackAsync(string broadcasterId, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthorizedClientAsync(broadcasterId, cancellationToken);
        if (client is null) return null;

        var response = await client.GetAsync(
            $"{SpotifyApiBase}/me/player/currently-playing",
            cancellationToken);

        if (!response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseCurrentTrack(json);
    }

    public async Task<IReadOnlyList<TrackInfo>> SearchAsync(
        string broadcasterId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAuthorizedClientAsync(broadcasterId, cancellationToken);
        if (client is null) return [];

        var encoded = Uri.EscapeDataString(query);
        var response = await client.GetAsync(
            $"{SpotifyApiBase}/search?q={encoded}&type=track&limit={maxResults}",
            cancellationToken);

        if (!response.IsSuccessStatusCode) return [];

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseSearchResults(json);
    }

    public async Task<bool> AddToQueueAsync(
        string broadcasterId,
        string trackUri,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAuthorizedClientAsync(broadcasterId, cancellationToken);
        if (client is null) return false;

        var encoded = Uri.EscapeDataString(trackUri);
        var response = await client.PostAsync(
            $"{SpotifyApiBase}/me/player/queue?uri={encoded}",
            null,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    // ─── Token management ─────────────────────────────────────────────────────

    private async Task<HttpClient?> GetAuthorizedClientAsync(
        string broadcasterId,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var service = await db.Services
            .FirstOrDefaultAsync(s =>
                s.Name == ProviderName
                && s.BroadcasterId == broadcasterId
                && s.Enabled, ct);

        if (service is null)
        {
            _logger.LogDebug("No Spotify service record for broadcaster {BroadcasterId}", broadcasterId);
            return null;
        }

        // Refresh token if expired (with 60s buffer)
        if (service.TokenExpiry is not null
            && service.TokenExpiry.Value < DateTime.UtcNow.AddSeconds(60))
        {
            if (!await RefreshTokenAsync(db, service, ct))
                return null;
        }

        var client = _httpClientFactory.CreateClient("spotify");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", service.AccessToken);

        return client;
    }

    private async Task<bool> RefreshTokenAsync(
        IApplicationDbContext db,
        Domain.Entities.Service service,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(service.RefreshToken)
            || string.IsNullOrEmpty(service.ClientId)
            || string.IsNullOrEmpty(service.ClientSecret))
        {
            _logger.LogWarning("Cannot refresh Spotify token — missing refresh token or credentials");
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("spotify-auth");
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{service.ClientId}:{service.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, SpotifyTokenUrl)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", credentials) },
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = service.RefreshToken,
                }),
            };

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Spotify token refresh failed: {Status}", response.StatusCode);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            service.AccessToken = root.GetProperty("access_token").GetString();
            service.TokenExpiry = DateTime.UtcNow.AddSeconds(
                root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600);

            // Spotify may return a new refresh token
            if (root.TryGetProperty("refresh_token", out var newRefresh)
                && newRefresh.ValueKind == JsonValueKind.String)
            {
                service.RefreshToken = newRefresh.GetString();
            }

            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Spotify token for broadcaster {BroadcasterId}",
                service.BroadcasterId);
            return false;
        }
    }

    // ─── JSON parsing ─────────────────────────────────────────────────────────

    private TrackInfo? ParseCurrentTrack(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("item", out var item) || item.ValueKind == JsonValueKind.Null)
                return null;

            var name = item.GetProperty("name").GetString() ?? string.Empty;
            var uri = item.GetProperty("uri").GetString() ?? string.Empty;
            var durationMs = item.TryGetProperty("duration_ms", out var dur) ? dur.GetInt32() : 0;

            var artists = item.TryGetProperty("artists", out var artistArr) && artistArr.GetArrayLength() > 0
                ? artistArr[0].GetProperty("name").GetString() ?? string.Empty
                : string.Empty;

            var album = string.Empty;
            var albumArtUrl = (string?)null;
            if (item.TryGetProperty("album", out var albumObj))
            {
                album = albumObj.TryGetProperty("name", out var albumName)
                    ? albumName.GetString() ?? string.Empty
                    : string.Empty;

                if (albumObj.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                    albumArtUrl = images[0].TryGetProperty("url", out var imgUrl) ? imgUrl.GetString() : null;
            }

            return new TrackInfo
            {
                TrackName = name,
                Artist = artists,
                Album = album,
                TrackUri = uri,
                AlbumArtUrl = albumArtUrl,
                DurationMs = durationMs,
                Provider = ProviderName,
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse Spotify current track");
            return null;
        }
    }

    private IReadOnlyList<TrackInfo> ParseSearchResults(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tracks", out var tracks)) return [];
            if (!tracks.TryGetProperty("items", out var items)) return [];

            var results = new List<TrackInfo>();
            foreach (var item in items.EnumerateArray())
            {
                var name = item.GetProperty("name").GetString() ?? string.Empty;
                var uri = item.GetProperty("uri").GetString() ?? string.Empty;
                var durationMs = item.TryGetProperty("duration_ms", out var dur) ? dur.GetInt32() : 0;

                var artists = item.TryGetProperty("artists", out var artistArr) && artistArr.GetArrayLength() > 0
                    ? artistArr[0].GetProperty("name").GetString() ?? string.Empty
                    : string.Empty;

                var album = string.Empty;
                var albumArtUrl = (string?)null;
                if (item.TryGetProperty("album", out var albumObj))
                {
                    album = albumObj.TryGetProperty("name", out var albumName)
                        ? albumName.GetString() ?? string.Empty
                        : string.Empty;

                    if (albumObj.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                        albumArtUrl = images[0].TryGetProperty("url", out var imgUrl) ? imgUrl.GetString() : null;
                }

                results.Add(new TrackInfo
                {
                    TrackName = name,
                    Artist = artists,
                    Album = album,
                    TrackUri = uri,
                    AlbumArtUrl = albumArtUrl,
                    DurationMs = durationMs,
                    Provider = ProviderName,
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse Spotify search results");
            return [];
        }
    }
}
