// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class Reward : SoftDeletableEntity, ITenantScoped
{
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(255)]
    public string Title { get; set; } = null!;

    [MaxLength(2000)]
    public string? Response { get; set; }

    [MaxLength(20)]
    public string Permission { get; set; } = "everyone";

    public bool IsEnabled { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? PipelineJson { get; set; }

    public bool IsPlatform { get; set; }

    /// <summary>Twitch's own reward ID (UUID). Null until synced with Twitch.</summary>
    [MaxLength(50)]
    public string? TwitchRewardId { get; set; }

    public int? Cost { get; set; }

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
