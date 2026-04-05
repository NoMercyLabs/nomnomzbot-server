// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace NoMercyBot.Domain.Entities;

public class DeletionAuditLog
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string RequestType { get; set; } = null!;

    [MaxLength(64)]
    public string SubjectIdHash { get; set; } = null!;

    [MaxLength(20)]
    public string RequestedBy { get; set; } = null!;

    public List<string> TablesAffected { get; set; } = [];

    public int RowsDeleted { get; set; }

    public DateTime CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
