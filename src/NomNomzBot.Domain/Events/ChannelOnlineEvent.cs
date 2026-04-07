// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Published when a channel's stream goes online (EventSub stream.online).
/// </summary>
public sealed class ChannelOnlineEvent : DomainEventBase
{
    public required string BroadcasterDisplayName { get; init; }
    public required string StreamTitle { get; init; }
    public required string GameName { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
}
