// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class DiscordServerAuthorization : BaseEntity
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(50)]
    public string GuildId { get; set; } = null!;

    [MaxLength(255)]
    public string GuildName { get; set; } = null!;

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [MaxLength(50)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
