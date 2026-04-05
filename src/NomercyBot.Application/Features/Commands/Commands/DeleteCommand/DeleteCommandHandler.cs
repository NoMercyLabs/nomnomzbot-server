// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Application.Features.Commands.Commands.DeleteCommand;

public class DeleteCommandHandler
{
    private readonly IApplicationDbContext _db;

    public DeleteCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> HandleAsync(
        string channelId,
        string commandName,
        CancellationToken ct = default
    )
    {
        Command? command = await _db.Commands.FirstOrDefaultAsync(
            c => c.BroadcasterId == channelId && c.Name == commandName,
            ct
        );

        if (command is null)
            return Errors.NotFound<object>("Command", commandName);

        if (command.IsPlatform)
            return Result.Failure("Platform commands cannot be deleted.", "FORBIDDEN");

        _db.Commands.Remove(command);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
