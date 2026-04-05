// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Application.DTOs.EventResponses;

/// <summary>An event response configuration.</summary>
public sealed record EventResponseDto(
    int Id,
    string EventType,
    bool IsEnabled,
    string ResponseType,
    string? Message,
    string? PipelineJson,
    Dictionary<string, string> Metadata,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Lightweight event response summary.</summary>
public sealed record EventResponseListItem(
    int Id,
    string EventType,
    bool IsEnabled,
    string ResponseType,
    DateTime UpdatedAt
);

/// <summary>Request to update an event response configuration.</summary>
public sealed record UpdateEventResponseDto
{
    public bool? IsEnabled { get; init; }

    [RegularExpression("^(chat_message|overlay|pipeline|none)$")]
    public string? ResponseType { get; init; }

    [MaxLength(2000)]
    public string? Message { get; init; }

    public string? PipelineJson { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }
}
