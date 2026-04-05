// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class FeatureToggledEvent : DomainEventBase
{
    public required string FeatureKey { get; init; }
    public required bool Enabled { get; init; }
}
