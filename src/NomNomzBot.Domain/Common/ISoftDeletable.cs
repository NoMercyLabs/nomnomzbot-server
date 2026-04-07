// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Common;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
