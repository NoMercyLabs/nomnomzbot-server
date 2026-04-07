// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Domain.Entities;

public class Pronoun
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = null!;

    [MaxLength(20)]
    public string Subject { get; set; } = null!;

    [MaxLength(20)]
    public string Object { get; set; } = null!;

    public bool Singular { get; set; }
}
