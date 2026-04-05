// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class PermissionChangedEvent : DomainEventBase
{
    public required string SubjectType { get; init; }
    public required string SubjectId { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public required int NewPermissionValue { get; init; }
}
