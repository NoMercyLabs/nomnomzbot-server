// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/integrations")]
[Authorize]
[Tags("Integrations")]
public class IntegrationsController : BaseController
{
    private readonly IApplicationDbContext _db;
    private readonly IConfiguration _config;

    public IntegrationsController(IApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    public record IntegrationDto(
        string Id,
        string Name,
        string Category,
        string Description,
        bool Connected,
        string? ConnectedAs,
        string? OAuthUrl,
        string? LastSync
    );

    public record IntegrationsResponse(List<IntegrationDto> Integrations);

    // ── Known integrations metadata ───────────────────────────────────────────

    private static readonly Dictionary<
        string,
        (string Name, string Category, string Description)
    > _meta = new()
    {
        ["spotify"] = ("Spotify", "Music", "Now playing overlays and song request commands"),
        ["discord"] = ("Discord", "Social", "Cross-post alerts and notifications to Discord"),
        ["youtube"] = ("YouTube", "Video", "YouTube live stream management and stats"),
        ["obs"] = ("OBS", "Streaming", "Scene switching, sources, and OBS remote control"),
        ["twitch"] = ("Twitch", "Platform", "Primary Twitch account — always connected"),
    };

    // ── List integrations ─────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType<StatusResponseDto<IntegrationsResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListIntegrations(string channelId, CancellationToken ct)
    {
        // Load all Service records for this channel in one query
        List<string> connectedServiceNames = await _db
            .Services.Where(s => s.BroadcasterId == channelId && s.Enabled && s.AccessToken != null)
            .Select(s => s.Name.ToLower())
            .ToListAsync(ct);

        bool discordConnected = await _db.DiscordServerAuthorizations.AnyAsync(
            d => d.BroadcasterId == channelId,
            ct
        );

        if (discordConnected && !connectedServiceNames.Contains("discord"))
            connectedServiceNames.Add("discord");

        // Twitch is always connected when the channel exists
        bool twitchConnected = await _db.Channels.AnyAsync(c => c.Id == channelId, ct);

        var result = _meta
            .Select(kvp =>
            {
                string id = kvp.Key;
                (string Name, string Category, string Description) = kvp.Value;

                bool isConnected = id switch
                {
                    "twitch" => twitchConnected,
                    _ => connectedServiceNames.Contains(id),
                };

                // OBS does not use OAuth — it requires the OBS WebSocket plugin locally
                string? oauthUrl = id switch
                {
                    "obs" => null,
                    "twitch" => null,
                    _ => BuildOAuthUrl(id, channelId),
                };

                return new IntegrationDto(
                    id,
                    Name,
                    Category,
                    Description,
                    isConnected,
                    null,
                    oauthUrl,
                    null
                );
            })
            .ToList();

        return Ok(
            new StatusResponseDto<IntegrationsResponse> { Data = new IntegrationsResponse(result) }
        );
    }

    // ── Disconnect integration ────────────────────────────────────────────────

    [HttpDelete("{integrationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Disconnect(
        string channelId,
        string integrationId,
        CancellationToken ct
    )
    {
        string id = integrationId.ToLower();

        if (id == "twitch")
            return BadRequestResponse("Cannot disconnect the primary Twitch account.");

        if (id == "discord")
        {
            var discordAuth = await _db.DiscordServerAuthorizations.FirstOrDefaultAsync(
                d => d.BroadcasterId == channelId,
                ct
            );
            if (discordAuth is not null)
            {
                _db.DiscordServerAuthorizations.Remove(discordAuth);
                await _db.SaveChangesAsync(ct);
            }
            return NoContent();
        }

        var service = await _db.Services.FirstOrDefaultAsync(
            s => s.BroadcasterId == channelId && s.Name.ToLower() == id,
            ct
        );

        if (service is null)
            return NotFoundResponse($"Integration '{integrationId}' is not connected.");

        _db.Services.Remove(service);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Get OAuth connect URL ─────────────────────────────────────────────────

    [HttpGet("{integrationId}/connect")]
    [ProducesResponseType<StatusResponseDto<object>>(StatusCodes.Status200OK)]
    public IActionResult GetConnectUrl(string channelId, string integrationId)
    {
        string id = integrationId.ToLower();
        string? url = BuildOAuthUrl(id, channelId);

        if (url is null)
        {
            return BadRequestResponse(
                id == "obs"
                    ? "OBS uses WebSocket — install the OBS WebSocket plugin and configure it in your OBS settings."
                    : $"OAuth is not available for '{integrationId}'."
            );
        }

        return Ok(new StatusResponseDto<object> { Data = new { oauthUrl = url } });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string? BuildOAuthUrl(string integrationId, string channelId)
    {
        string baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        string apiBase = $"{baseUrl}/api/v1";

        return integrationId switch
        {
            "spotify" => $"{apiBase}/channels/{channelId}/integrations/spotify/callback/start",
            "discord" => $"{apiBase}/channels/{channelId}/integrations/discord/callback/start",
            "youtube" => $"{apiBase}/channels/{channelId}/integrations/youtube/callback/start",
            _ => null,
        };
    }
}
