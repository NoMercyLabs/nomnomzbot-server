using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Commands;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Application service for managing custom chat commands.
/// </summary>
public interface ICommandService
{
    /// <summary>Create a new command in a channel.</summary>
    Task<Result<CommandDto>> CreateAsync(string broadcasterId, CreateCommandDto request, CancellationToken cancellationToken = default);

    /// <summary>Update an existing command.</summary>
    Task<Result<CommandDto>> UpdateAsync(string broadcasterId, string commandName, UpdateCommandDto request, CancellationToken cancellationToken = default);

    /// <summary>Delete a command by name.</summary>
    Task<Result> DeleteAsync(string broadcasterId, string commandName, CancellationToken cancellationToken = default);

    /// <summary>Get a single command by name.</summary>
    Task<Result<CommandDto>> GetAsync(string broadcasterId, string commandName, CancellationToken cancellationToken = default);

    /// <summary>List all commands in a channel with pagination.</summary>
    Task<Result<PagedList<CommandListItem>>> ListAsync(string broadcasterId, PaginationParams pagination, CancellationToken cancellationToken = default);

    /// <summary>Execute a command and return its response text.</summary>
    Task<Result<string>> ExecuteAsync(string broadcasterId, string commandName, string userId, string? input = null, CancellationToken cancellationToken = default);
}
