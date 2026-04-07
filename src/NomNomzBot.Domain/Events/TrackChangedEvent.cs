// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class TrackChangedEvent : DomainEventBase
{
    public required string TrackName { get; init; }
    public required string Artist { get; init; }
    public required string TrackUri { get; init; }
    public string? AlbumArtUrl { get; init; }
    public required int DurationMs { get; init; }
    public required string Provider { get; init; }
}
