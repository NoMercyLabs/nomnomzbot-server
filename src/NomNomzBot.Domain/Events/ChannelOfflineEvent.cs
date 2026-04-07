// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Published when a channel's stream goes offline (EventSub stream.offline).
/// </summary>
public sealed class ChannelOfflineEvent : DomainEventBase
{
    public required string BroadcasterDisplayName { get; init; }
    public required TimeSpan StreamDuration { get; init; }
}
