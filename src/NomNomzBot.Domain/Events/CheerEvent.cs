// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Published when a viewer cheers with bits.
/// </summary>
public sealed class CheerEvent : DomainEventBase
{
    public required string UserId { get; init; }
    public required string UserDisplayName { get; init; }
    public required int Bits { get; init; }
    public required string Message { get; init; }
    public required bool IsAnonymous { get; init; }
}
