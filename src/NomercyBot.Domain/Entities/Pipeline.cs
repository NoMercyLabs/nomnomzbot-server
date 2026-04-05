// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

/// <summary>
/// A named pipeline created via the visual node builder in the frontend.
/// Stores a JSON graph that the pipeline engine executes.
/// </summary>
public class Pipeline : BaseEntity, ITenantScoped
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>JSON representation of the pipeline node graph.</summary>
    public string GraphJson { get; set; } = "{}";

    public int TriggerCount { get; set; }

    public DateTime? LastTriggeredAt { get; set; }

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
