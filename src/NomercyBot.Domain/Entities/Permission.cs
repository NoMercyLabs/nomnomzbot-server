// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class Permission : SoftDeletableEntity, ITenantScoped
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(10)]
    public string SubjectType { get; set; } = null!;

    [MaxLength(50)]
    public string SubjectId { get; set; } = null!;

    [MaxLength(20)]
    public string ResourceType { get; set; } = null!;

    [MaxLength(255)]
    public string? ResourceId { get; set; }

    [MaxLength(5)]
    public string PermissionValue { get; set; } = null!;

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
