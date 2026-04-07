// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Features.Commands.Queries.GetCommands;

public record CommandListItemDto(
    int Id,
    string Name,
    string Type,
    string Permission,
    bool IsEnabled,
    bool IsPlatform,
    int CooldownSeconds,
    string? Description,
    List<string> Aliases,
    DateTime CreatedAt
);
