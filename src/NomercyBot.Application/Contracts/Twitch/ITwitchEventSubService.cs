// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Contracts.Twitch;

public interface ITwitchEventSubService
{
    Task SubscribeAsync(string broadcasterId, string eventType, CancellationToken ct = default);
    Task UnsubscribeAsync(string subscriptionId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetActiveSubscriptionsAsync(
        string broadcasterId,
        CancellationToken ct = default
    );
}
