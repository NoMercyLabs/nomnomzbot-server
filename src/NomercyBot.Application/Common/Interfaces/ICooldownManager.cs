// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Common.Interfaces;

public interface ICooldownManager
{
    bool IsOnCooldown(string channelId, string commandName, string? userId = null);
    TimeSpan? GetRemainingCooldown(string channelId, string commandName, string? userId = null);
    void SetCooldown(
        string channelId,
        string commandName,
        TimeSpan duration,
        string? userId = null
    );
    void ClearCooldown(string channelId, string commandName, string? userId = null);
    void ClearAllCooldowns(string channelId);
}
