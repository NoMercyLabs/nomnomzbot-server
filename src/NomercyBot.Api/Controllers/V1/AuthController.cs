// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Auth;
using NoMercyBot.Application.Features.Auth.Queries.GetCurrentUser;
using NoMercyBot.Application.Services;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Tags("Auth")]
public class AuthController : BaseController
{
    private readonly GetCurrentUserQueryHandler _getCurrentUser;
    private readonly IAuthService _authService;

    public AuthController(
        GetCurrentUserQueryHandler getCurrentUser,
        IAuthService authService)
    {
        _getCurrentUser = getCurrentUser;
        _authService = authService;
    }

    /// <summary>Get the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<StatusResponseDto<CurrentUserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        Result<CurrentUserDto> result = await _getCurrentUser.HandleAsync(ct);
        return ResultResponse(result);
    }

    /// <summary>
    /// Start the Twitch OAuth flow. Redirects the browser to Twitch's authorization page.
    /// Pass <c>redirect_uri</c> for mobile deep-link callbacks (e.g. <c>nomercybot://callback</c>).
    /// </summary>
    [HttpGet("twitch")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public IActionResult StartTwitchOAuth([FromQuery] string? redirect_uri)
    {
        // Encode the mobile redirect URI into the OAuth state parameter so we can
        // retrieve it when Twitch redirects back to our callback endpoint.
        string? state = null;
        if (!string.IsNullOrWhiteSpace(redirect_uri))
        {
            var payload = JsonSerializer.Serialize(new { redirect_uri });
            state = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        }

        string authUrl = _authService.GetTwitchOAuthUrl(state);
        return Redirect(authUrl);
    }

    /// <summary>
    /// Handle the OAuth callback from Twitch. Exchanges the authorization code for
    /// platform tokens. If a mobile <c>redirect_uri</c> was passed in the original
    /// request, the browser is redirected to the app's deep link with tokens in the
    /// query string. Otherwise returns a JSON response (for web clients).
    /// </summary>
    [HttpGet("twitch/callback")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> HandleTwitchCallback(
        [FromQuery] string code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        // Try to extract the mobile redirect URI from the state parameter.
        string? mobileRedirectUri = null;
        string? rawState = state;
        if (!string.IsNullOrWhiteSpace(state))
        {
            try
            {
                byte[] decoded = Convert.FromBase64String(state);
                using var doc = JsonDocument.Parse(decoded);
                if (doc.RootElement.TryGetProperty("redirect_uri", out var uriElement))
                {
                    mobileRedirectUri = uriElement.GetString();
                }
            }
            catch
            {
                // State isn't our encoded payload — pass through as-is.
            }
        }

        Result<AuthResultDto> result = await _authService.HandleTwitchCallbackAsync(
            new OAuthCallbackDto { Code = code, State = rawState }, ct);

        if (result.IsFailure)
        {
            if (!string.IsNullOrWhiteSpace(mobileRedirectUri))
            {
                return Redirect(
                    $"{mobileRedirectUri}?error=auth_failed&error_description={Uri.EscapeDataString(result.ErrorMessage ?? "Authentication failed")}");
            }

            return ResultResponse(result);
        }

        AuthResultDto auth = result.Value;
        int expiresIn = (int)(auth.ExpiresAt - DateTime.UtcNow).TotalSeconds;

        // Mobile: redirect back to the app's deep link with tokens.
        if (!string.IsNullOrWhiteSpace(mobileRedirectUri))
        {
            var qs = new StringBuilder(mobileRedirectUri);
            qs.Append(mobileRedirectUri.Contains('?') ? '&' : '?');
            qs.Append("access_token=").Append(Uri.EscapeDataString(auth.AccessToken));
            qs.Append("&refresh_token=").Append(Uri.EscapeDataString(auth.RefreshToken));
            qs.Append("&expires_in=").Append(expiresIn);

            return Redirect(qs.ToString());
        }

        // Web: return JSON.
        return Ok(new StatusResponseDto<object>
        {
            Data = new
            {
                accessToken = auth.AccessToken,
                refreshToken = auth.RefreshToken,
                expiresIn,
                user = auth.User,
            },
        });
    }

    /// <summary>
    /// Exchange an OAuth authorization code for platform tokens (mobile / SPA flow).
    /// The client handles the Twitch redirect directly and sends the code + redirect_uri
    /// to this endpoint for server-side token exchange.
    /// </summary>
    [HttpPost("twitch/callback")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ExchangeCode(
        [FromBody] OAuthCallbackDto body,
        CancellationToken ct)
    {
        Result<AuthResultDto> result = await _authService.HandleTwitchCallbackAsync(body, ct);

        if (result.IsFailure)
            return ResultResponse(result);

        AuthResultDto auth = result.Value;
        int expiresIn = (int)(auth.ExpiresAt - DateTime.UtcNow).TotalSeconds;

        return Ok(new StatusResponseDto<object>
        {
            Data = new
            {
                accessToken = auth.AccessToken,
                refreshToken = auth.RefreshToken,
                expiresIn,
                user = auth.User,
            },
        });
    }

    /// <summary>Refresh an expired access token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        Result<AuthResultDto> result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);

        if (result.IsFailure)
            return ResultResponse(result);

        AuthResultDto auth = result.Value;
        int expiresIn = (int)(auth.ExpiresAt - DateTime.UtcNow).TotalSeconds;

        return Ok(new StatusResponseDto<object>
        {
            Data = new
            {
                accessToken = auth.AccessToken,
                refreshToken = auth.RefreshToken,
                expiresIn,
                user = auth.User,
            },
        });
    }

    /// <summary>Log out the current user, revoking their Twitch tokens.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return UnauthenticatedResponse();

        Result result = await _authService.LogoutAsync(userId, ct);
        return ResultResponse(result);
    }
}
