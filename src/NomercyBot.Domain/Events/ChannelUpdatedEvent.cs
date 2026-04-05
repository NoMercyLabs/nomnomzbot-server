// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class ChannelUpdatedEvent : DomainEventBase
{
    public required string BroadcasterDisplayName { get; init; }
    public required string NewTitle { get; init; }
    public required string NewGameName { get; init; }
}
