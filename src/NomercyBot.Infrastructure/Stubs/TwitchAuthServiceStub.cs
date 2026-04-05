// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Contracts.Twitch;

namespace NoMercyBot.Infrastructure.Stubs;

public class TwitchAuthServiceStub : ITwitchAuthService
{
    private readonly ILogger<TwitchAuthServiceStub> _logger;

    public TwitchAuthServiceStub(ILogger<TwitchAuthServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<TokenResult?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] ExchangeCode");
        return Task.FromResult<TokenResult?>(null);
    }

    public Task<TokenResult?> RefreshTokenAsync(string broadcasterId, string serviceName, CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] RefreshToken for {BroadcasterId}/{Service}", broadcasterId, serviceName);
        return Task.FromResult<TokenResult?>(null);
    }

    public Task RefreshExpiringTokensAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] RefreshExpiringTokens");
        return Task.CompletedTask;
    }

    public Task RevokeTokenAsync(string broadcasterId, string serviceName, CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] RevokeToken for {BroadcasterId}/{Service}", broadcasterId, serviceName);
        return Task.CompletedTask;
    }
}
