// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class ChannelEvent : BaseEntity
{
    [MaxLength(50)]
    public string Id { get; set; } = null!;

    [MaxLength(50)]
    public string? ChannelId { get; set; }

    [MaxLength(50)]
    public string? UserId { get; set; }

    [MaxLength(50)]
    public string Type { get; set; } = null!;

    public string? Data { get; set; }

    [ForeignKey(nameof(ChannelId))]
    public virtual Channel? Channel { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
