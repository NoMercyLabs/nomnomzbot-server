// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;
using NoMercyBot.Application.Features.Commands.Commands.CreateCommand;
using NoMercyBot.Application.Features.Commands.Commands.DeleteCommand;
using NoMercyBot.Application.Features.Commands.Queries.GetCommands;

namespace NoMercyBot.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/channels/{channelId}/commands")]
[Authorize]
public class CommandsController : BaseController
{
    private readonly GetCommandsQueryHandler _getCommands;
    private readonly CreateCommandHandler _createCommand;
    private readonly DeleteCommandHandler _deleteCommand;

    public CommandsController(
        GetCommandsQueryHandler getCommands,
        CreateCommandHandler createCommand,
        DeleteCommandHandler deleteCommand)
    {
        _getCommands = getCommands;
        _createCommand = createCommand;
        _deleteCommand = deleteCommand;
    }

    [HttpGet]
    public async Task<IActionResult> GetCommands(string channelId, CancellationToken ct)
    {
        var result = await _getCommands.HandleAsync(new GetCommandsQuery(channelId), ct);
        return ResultResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCommand(
        string channelId,
        [FromBody] CreateCommandRequest request,
        CancellationToken ct)
    {
        var result = await _createCommand.HandleAsync(channelId, request, ct);
        if (result.IsFailure)
            return ResultResponse(result);

        return CreatedAtAction(nameof(GetCommands), new { channelId },
            new StatusResponseDto<object> { Message = "Command created successfully." });
    }

    [HttpDelete("{commandName}")]
    public async Task<IActionResult> DeleteCommand(string channelId, string commandName, CancellationToken ct)
    {
        var result = await _deleteCommand.HandleAsync(channelId, commandName, ct);
        if (result.IsFailure)
            return result.ErrorCode switch
            {
                "NOT_FOUND" => NotFoundResponse(result.ErrorMessage),
                "FORBIDDEN" => UnauthorizedResponse(result.ErrorMessage),
                _ => InternalServerErrorResponse(result.ErrorMessage)
            };

        return NoContent();
    }
}
