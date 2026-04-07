// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class WidgetDisconnectedEvent : DomainEventBase
{
    public required string WidgetId { get; init; }
    public required string ConnectionId { get; init; }
}
