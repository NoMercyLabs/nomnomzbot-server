// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class MessageAutoModdedEvent : DomainEventBase
{
    public required string MessageId { get; init; }
    public required string UserId { get; init; }
    public required string Reason { get; init; }
}
