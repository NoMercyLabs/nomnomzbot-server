// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class ChatClearedEvent : DomainEventBase
{
    public required string ClearedByUserId { get; init; }
}
