// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class EventSubscription : BaseEntity, ITenantScoped
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(50)]
    public string Provider { get; set; } = null!;

    [MaxLength(100)]
    public string EventType { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    [MaxLength(50)]
    public string? Version { get; set; }

    [MaxLength(255)]
    public string? SubscriptionId { get; set; }

    [MaxLength(255)]
    public string? SessionId { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new();
    public string[] Condition { get; set; } = [];

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
