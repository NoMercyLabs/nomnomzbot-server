// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class UserUnbannedEvent : DomainEventBase
{
    public required string TargetUserId { get; init; }
    public required string ModeratorUserId { get; init; }
}
