// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class PlaybackStateChangedEvent : DomainEventBase
{
    public required bool IsPlaying { get; init; }
    public string? TrackName { get; init; }
}
