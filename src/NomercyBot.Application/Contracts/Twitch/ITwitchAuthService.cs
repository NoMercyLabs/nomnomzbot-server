// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Contracts.Twitch;

public interface ITwitchAuthService
{
    Task<TokenResult?> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken ct = default
    );
    Task<TokenResult?> RefreshTokenAsync(
        string broadcasterId,
        string serviceName,
        CancellationToken ct = default
    );
    Task RefreshExpiringTokensAsync(CancellationToken ct = default);
    Task RevokeTokenAsync(string broadcasterId, string serviceName, CancellationToken ct = default);
}

public record TokenResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string[] Scopes
);
