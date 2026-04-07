// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

public class TtsVoice : BaseEntity
{
    [MaxLength(255)]
    public string Id { get; set; } = null!;

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(255)]
    public string DisplayName { get; set; } = null!;

    [MaxLength(10)]
    public string Locale { get; set; } = null!;

    [MaxLength(10)]
    public string Gender { get; set; } = null!;

    [MaxLength(50)]
    public string Provider { get; set; } = null!;

    public bool IsDefault { get; set; }
}
