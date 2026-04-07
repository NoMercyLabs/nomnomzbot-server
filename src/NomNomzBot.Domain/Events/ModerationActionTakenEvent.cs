// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

using NoMercyBot.Domain.Common;

public sealed record ModerationActionTakenEvent(
    string ChannelId,
    string ModeratorId,
    string TargetUserId,
    string ActionType,
    string? Reason
) : DomainEvent;
