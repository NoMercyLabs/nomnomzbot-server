// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class Storage : BaseEntity
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string? BroadcasterId { get; set; }

    [MaxLength(255)]
    public string Key { get; set; } = null!;

    public string? Value { get; set; }

    [MaxLength(4096)]
    public string? SecureValue { get; set; }

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel? Channel { get; set; }
}
