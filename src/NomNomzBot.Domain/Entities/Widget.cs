// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class Widget : SoftDeletableEntity, ITenantScoped
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string Version { get; set; } = "1.0.0";

    [MaxLength(20)]
    public string Framework { get; set; } = "vanilla";

    public bool IsEnabled { get; set; } = true;

    [MaxLength(100)]
    public string? TemplateId { get; set; }

    public List<string> EventSubscriptions { get; set; } = [];
    public Dictionary<string, object> Settings { get; set; } = new();

    public string? CustomCode { get; set; }

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
