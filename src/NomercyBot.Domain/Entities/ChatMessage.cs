// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NoMercyBot.Domain.Common;
using NoMercyBot.Domain.ValueObjects;

namespace NoMercyBot.Domain.Entities;

public class ChatMessage : SoftDeletableEntity
{
    [MaxLength(255)]
    public string Id { get; set; } = null!;

    [MaxLength(50)]
    public string BroadcasterId { get; set; } = null!;

    [MaxLength(50)]
    public string UserId { get; set; } = null!;

    [MaxLength(255)]
    public string Username { get; set; } = null!;

    [MaxLength(255)]
    public string DisplayName { get; set; } = null!;

    [MaxLength(20)]
    public string UserType { get; set; } = null!;

    [MaxLength(7)]
    public string? ColorHex { get; set; }

    public string Message { get; set; } = null!;

    public List<ChatMessageFragment> Fragments { get; set; } = [];
    public List<ChatBadge> Badges { get; set; } = [];

    [MaxLength(50)]
    public string MessageType { get; set; } = "text";

    public bool IsCommand { get; set; }
    public bool IsCheer { get; set; }
    public int? BitsAmount { get; set; }
    public bool IsHighlighted { get; set; }

    [MaxLength(255)]
    public string? ReplyToMessageId { get; set; }

    [MaxLength(50)]
    public string? StreamId { get; set; }

    [ForeignKey(nameof(BroadcasterId))]
    public virtual Channel Channel { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(StreamId))]
    public virtual Stream? Stream { get; set; }
}
