// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class User : BaseEntity
{
    [MaxLength(50)]
    public string Id { get; set; } = null!;

    [MaxLength(255)]
    public string Username { get; set; } = null!;

    [MaxLength(255)]
    public string DisplayName { get; set; } = null!;

    [MaxLength(255)]
    public string? NickName { get; set; }

    [MaxLength(50)]
    public string? Timezone { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(2048)]
    public string? ProfileImageUrl { get; set; }

    [MaxLength(2048)]
    public string? OfflineImageUrl { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string BroadcasterType { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public bool IsAdmin { get; set; }

    public Pronoun? Pronoun { get; set; }
    public bool PronounManualOverride { get; set; }

    public virtual Channel? Channel { get; set; }
}
