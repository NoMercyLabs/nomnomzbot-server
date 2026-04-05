// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

/// <summary>
/// Configures what the bot does when a specific channel event fires
/// (e.g., a follow → send a chat message; a sub → trigger a pipeline).
/// </summary>
public class EventResponse : BaseEntity, ITenantScoped
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    /// <summary>The event type this response applies to (e.g., "channel.follow", "channel.subscribe").</summary>
    [MaxLength(100)]
    public string EventType { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;

    /// <summary>How the bot responds: "chat_message", "overlay", "pipeline", or "none".</summary>
    [MaxLength(50)]
    public string ResponseType { get; set; } = "chat_message";

    /// <summary>The chat message template to send (when ResponseType is "chat_message").</summary>
    [MaxLength(2000)]
    public string? Message { get; set; }

    /// <summary>JSON pipeline definition (when ResponseType is "pipeline").</summary>
    public string? PipelineJson { get; set; }

    /// <summary>Additional key-value metadata (e.g., overlay widget ID).</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
