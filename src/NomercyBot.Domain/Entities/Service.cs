// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class Service : BaseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(50)]
    public string Name { get; set; } = null!;

    public bool Enabled { get; set; } = true;

    [MaxLength(50)]
    public string? BroadcasterId { get; set; }

    [MaxLength(512)]
    public string? ClientId { get; set; }

    [MaxLength(512)]
    public string? ClientSecret { get; set; }

    [MaxLength(255)]
    public string? UserName { get; set; }

    [MaxLength(50)]
    public string? UserId { get; set; }

    public string[] Scopes { get; set; } = [];

    [MaxLength(2048)]
    public string? AccessToken { get; set; }

    [MaxLength(2048)]
    public string? RefreshToken { get; set; }

    public DateTime? TokenExpiry { get; set; }

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel? Channel { get; set; }
}
