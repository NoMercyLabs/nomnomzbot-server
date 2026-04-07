// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class RaidReceivedEvent : DomainEventBase
{
    public required string FromUserId { get; init; }
    public required string FromDisplayName { get; init; }
    public required int ViewerCount { get; init; }
}
