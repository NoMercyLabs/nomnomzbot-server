// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Exceptions;

public class InsufficientPermissionException : DomainException
{
    public InsufficientPermissionException(string action)
        : base($"Insufficient permission to perform: {action}") { }
}
