// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Service for checking whether a specific feature is enabled for a channel.
/// </summary>
public interface IFeatureGateService
{
    Task<bool> IsEnabledAsync(
        string broadcasterId,
        string featureKey,
        CancellationToken cancellationToken = default
    );
}
