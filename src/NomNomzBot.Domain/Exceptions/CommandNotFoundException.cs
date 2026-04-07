// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Exceptions;

public class CommandNotFoundException : DomainException
{
    public CommandNotFoundException(string commandName)
        : base($"Command '{commandName}' was not found.") { }
}
