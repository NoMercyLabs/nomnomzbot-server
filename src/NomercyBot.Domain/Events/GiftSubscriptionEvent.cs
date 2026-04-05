// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class GiftSubscriptionEvent : DomainEventBase
{
    public required string GifterUserId { get; init; }
    public required string GifterDisplayName { get; init; }

    /// <summary>"1000", "2000", or "3000"</summary>
    public required string Tier { get; init; }

    public required int GiftCount { get; init; }
    public required bool IsAnonymous { get; init; }
    public required IReadOnlyList<GiftRecipient> Recipients { get; init; }
}

public sealed record GiftRecipient(string UserId, string DisplayName);
