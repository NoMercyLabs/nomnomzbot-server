// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class UserTimedOutEvent : DomainEventBase
{
    public required string TargetUserId { get; init; }
    public required string TargetDisplayName { get; init; }
    public required string ModeratorUserId { get; init; }
    public required int DurationSeconds { get; init; }
    public string? Reason { get; init; }
}
