// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Commands;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Services.Application;

public class CommandService : ICommandService
{
    private readonly IApplicationDbContext _db;

    public CommandService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CommandDto>> CreateAsync(
        string broadcasterId,
        CreateCommandDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.ToLowerInvariant();

        var exists = await _db.Commands
            .AnyAsync(c => c.BroadcasterId == broadcasterId && c.Name == normalizedName, cancellationToken);

        if (exists)
            return Errors.AlreadyExists("command", request.Name).ToTyped<CommandDto>();

        var command = new Command
        {
            BroadcasterId = broadcasterId,
            Name = normalizedName,
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
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(command));
    }

    public async Task<Result<CommandDto>> UpdateAsync(
        string broadcasterId,
        string commandName,
        UpdateCommandDto request,
        CancellationToken cancellationToken = default)
    {
        var command = await _db.Commands
            .FirstOrDefaultAsync(c => c.BroadcasterId == broadcasterId && c.Name == commandName, cancellationToken);

        if (command is null)
            return Errors.NotFound<CommandDto>("Command", commandName);

        if (request.Type is not null) command.Type = request.Type;
        if (request.Permission is not null) command.Permission = request.Permission;
        if (request.Response is not null) command.Response = request.Response;
        if (request.Responses is not null) command.Responses = request.Responses;
        if (request.CooldownSeconds.HasValue) command.CooldownSeconds = request.CooldownSeconds.Value;
        if (request.CooldownPerUser.HasValue) command.CooldownPerUser = request.CooldownPerUser.Value;
        if (request.Description is not null) command.Description = request.Description;
        if (request.Aliases is not null) command.Aliases = request.Aliases;
        if (request.IsEnabled.HasValue) command.IsEnabled = request.IsEnabled.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(command));
    }

    public async Task<Result> DeleteAsync(
        string broadcasterId,
        string commandName,
        CancellationToken cancellationToken = default)
    {
        var command = await _db.Commands
            .FirstOrDefaultAsync(c => c.BroadcasterId == broadcasterId && c.Name == commandName, cancellationToken);

        if (command is null)
            return Result.Failure($"Command '{commandName}' was not found.", "NOT_FOUND");

        _db.Commands.Remove(command);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CommandDto>> GetAsync(
        string broadcasterId,
        string commandName,
        CancellationToken cancellationToken = default)
    {
        var command = await _db.Commands
            .FirstOrDefaultAsync(c => c.BroadcasterId == broadcasterId && c.Name == commandName, cancellationToken);

        if (command is null)
            return Errors.NotFound<CommandDto>("Command", commandName);

        return Result.Success(ToDto(command));
    }

    public async Task<Result<PagedList<CommandListItem>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Commands.Where(c => c.BroadcasterId == broadcasterId);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(c => new CommandListItem(
                c.Id,
                c.Name,
                c.Type,
                c.Permission,
                c.IsEnabled,
                c.CooldownSeconds,
                c.Description,
                c.Aliases,
                0,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedList<CommandListItem>(items, total, pagination.Page, pagination.PageSize));
    }

    public async Task<Result<string>> ExecuteAsync(
        string broadcasterId,
        string commandName,
        string userId,
        string? input = null,
        CancellationToken cancellationToken = default)
    {
        var command = await _db.Commands
            .FirstOrDefaultAsync(c => c.BroadcasterId == broadcasterId
                && c.Name == commandName
                && c.IsEnabled, cancellationToken);

        if (command is null)
            return Errors.NotFound<string>("Command", commandName);

        var response = command.Response
            ?? (command.Responses.Count > 0 ? command.Responses[0] : null);

        return Result.Success(response ?? string.Empty);
    }

    private static CommandDto ToDto(Command c) => new(
        c.Id,
        c.Name,
        c.Type,
        c.Permission,
        c.IsEnabled,
        c.Response,
        c.Responses,
        c.PipelineJson is not null
            ? System.Text.Json.JsonSerializer.Deserialize<object>(c.PipelineJson)
            : null,
        c.CooldownSeconds,
        c.CooldownPerUser,
        c.Description,
        c.Aliases,
        0,
        c.CreatedAt,
        c.UpdatedAt);
}
