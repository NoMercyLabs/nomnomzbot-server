// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Domain.Entities;

/// <summary>
/// A channel timer that sends a message at a configured interval,
/// with optional minimum chat activity enforcement.
/// </summary>
public class Timer : SoftDeletableEntity, ITenantScoped
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>List of messages to rotate through (round-robin).</summary>
    public List<string> Messages { get; set; } = [];

    /// <summary>How often the timer fires (in minutes).</summary>
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>Minimum number of chat messages since last fire before the timer will fire again.</summary>
    public int MinChatActivity { get; set; } = 0;

    public bool IsEnabled { get; set; } = true;

    /// <summary>UTC time the timer last sent a message.</summary>
    public DateTime? LastFiredAt { get; set; }

    /// <summary>Round-robin index into Messages.</summary>
    public int NextMessageIndex { get; set; } = 0;

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;
}
