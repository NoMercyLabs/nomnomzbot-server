// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Application.DTOs.Pipelines;

/// <summary>Full pipeline details including the deserialized node graph.</summary>
public sealed record PipelineDto(
    int Id,
    string ChannelId,
    string Name,
    string? Description,
    bool IsEnabled,
    object Graph,
    int TriggerCount,
    DateTime? LastTriggeredAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Lightweight pipeline summary for list views.</summary>
public sealed record PipelineListItemDto(
    int Id,
    string Name,
    string? Description,
    bool IsEnabled,
    int TriggerCount,
    DateTime? LastTriggeredAt,
    DateTime UpdatedAt
);

/// <summary>Request to create a new pipeline.</summary>
public sealed record CreatePipelineDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = null!;

    [MaxLength(500)]
    public string? Description { get; init; }

    public bool IsEnabled { get; init; } = true;

    public object? Graph { get; init; }
}

/// <summary>Request to update an existing pipeline.</summary>
public sealed record UpdatePipelineDto
{
    [MaxLength(200)]
    public string? Name { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }

    public bool? IsEnabled { get; init; }

    public object? Graph { get; init; }
}
