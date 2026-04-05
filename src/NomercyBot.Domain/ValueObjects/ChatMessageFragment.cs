// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.ValueObjects;

public sealed record ChatMessageFragment(
    string Type,
    string Text,
    string? EmoteId = null,
    string? MentionUserId = null,
    string? CheermotePrefix = null,
    int? CheermoteBits = null);
