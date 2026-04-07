// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Exceptions;

public class CooldownActiveException : DomainException
{
    public TimeSpan RemainingCooldown { get; }

    public CooldownActiveException(string commandName, TimeSpan remaining)
        : base(
            $"Command '{commandName}' is on cooldown for {remaining.TotalSeconds:F0} more seconds."
        )
    {
        RemainingCooldown = remaining;
    }
}
