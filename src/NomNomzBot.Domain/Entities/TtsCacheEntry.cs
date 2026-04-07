// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class TtsCacheEntry : BaseEntity
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string ContentHash { get; set; } = null!;

    public byte[] AudioData { get; set; } = null!;

    public int DurationMs { get; set; }

    [MaxLength(50)]
    public string Provider { get; set; } = null!;

    [MaxLength(255)]
    public string VoiceId { get; set; } = null!;
}
