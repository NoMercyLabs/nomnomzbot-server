// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Features.Commands.Queries.GetCommands;

public record GetCommandsQuery(string ChannelId, bool IncludeDisabled = false);
