// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Application.Features.Commands.Commands.CreateCommand;

public record CreateCommandRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = null!;

    [Required]
    public string Type { get; init; } = "text";

    public string Permission { get; init; } = "everyone";

    [MaxLength(2000)]
    public string? Response { get; init; }

    public List<string>? Responses { get; init; }

    public object? Pipeline { get; init; }

    [Range(0, 86400)]
    public int CooldownSeconds { get; init; }

    public bool CooldownPerUser { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }

    public List<string>? Aliases { get; init; }
}
