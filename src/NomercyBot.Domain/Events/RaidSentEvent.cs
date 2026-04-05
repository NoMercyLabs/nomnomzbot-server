// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class RaidSentEvent : DomainEventBase
{
    public required string ToUserId { get; init; }
    public required string ToDisplayName { get; init; }
}
