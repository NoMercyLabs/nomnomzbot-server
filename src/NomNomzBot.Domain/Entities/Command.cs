// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class Command : SoftDeletableEntity, ITenantScoped
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(20)]
    public string Permission { get; set; } = "everyone";

    [MaxLength(20)]
    public string Type { get; set; } = "text";

    [MaxLength(2000)]
    public string? Response { get; set; }

    public List<string> Responses { get; set; } = [];

    public string? PipelineJson { get; set; }

    public bool IsEnabled { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int CooldownSeconds { get; set; }

    public bool CooldownPerUser { get; set; }

    public List<string> Aliases { get; set; } = [];

    public bool IsPlatform { get; set; }

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
