// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class ChatMessageDeletedEvent : DomainEventBase
{
    public required string MessageId { get; init; }
    public required string DeletedByUserId { get; init; }
    public required string TargetUserId { get; init; }
}
