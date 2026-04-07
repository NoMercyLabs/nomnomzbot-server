// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class ChannelModerator : SoftDeletableEntity, ITenantScoped
{
    [MaxLength(50)]
    public string ChannelId { get; set; } = null!;

    [MaxLength(50)]
    public string UserId { get; set; } = null!;

    [MaxLength(20)]
    public string Role { get; set; } = "moderator";

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? GrantedBy { get; set; }

    string ITenantScoped.BroadcasterId
    {
        get => ChannelId;
        set => ChannelId = value;
    }

    [ForeignKey(nameof(ChannelId))]
    public virtual Channel Channel { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
