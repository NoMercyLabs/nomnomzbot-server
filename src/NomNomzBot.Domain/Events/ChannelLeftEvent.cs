// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

using NoMercyBot.Domain.Common;

public sealed record ChannelLeftEvent(string ChannelId, string ChannelName) : DomainEvent;
