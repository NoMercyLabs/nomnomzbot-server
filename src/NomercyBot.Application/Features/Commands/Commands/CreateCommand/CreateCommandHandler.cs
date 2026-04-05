// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Application.Features.Commands.Commands.CreateCommand;

public class CreateCommandHandler
{
    private readonly IApplicationDbContext _db;

    public CreateCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> HandleAsync(
        string channelId,
        CreateCommandRequest request,
        CancellationToken ct = default
    )
    {
        bool exists = await _db.Commands.AnyAsync(
            c => c.BroadcasterId == channelId && c.Name == request.Name,
            ct
        );

        if (exists)
            return Errors.AlreadyExists("command", request.Name).ToTyped<int>();

        Command command = new()
        {
            BroadcasterId = channelId,
            Name = request.Name.ToLowerInvariant(),
            Type = request.Type,
            Permission = request.Permission,
            Response = request.Response,
            Responses = request.Responses ?? [],
            CooldownSeconds = request.CooldownSeconds,
            CooldownPerUser = request.CooldownPerUser,
            Description = request.Description,
            Aliases = request.Aliases ?? [],
            IsEnabled = true,
        };

        _db.Commands.Add(command);
        await _db.SaveChangesAsync(ct);

        return Result.Success(command.Id);
    }
}
