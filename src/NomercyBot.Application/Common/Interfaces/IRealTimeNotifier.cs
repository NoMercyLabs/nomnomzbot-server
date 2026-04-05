// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Common.Interfaces;

public interface IRealTimeNotifier
{
    Task NotifyChannelAsync(string channelId, string eventName, object payload, CancellationToken ct = default);
    Task NotifyAllAsync(string eventName, object payload, CancellationToken ct = default);
}
