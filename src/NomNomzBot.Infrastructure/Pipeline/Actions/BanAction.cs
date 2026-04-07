// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Pipeline.Actions;

public sealed class BanAction : ICommandAction
{
    private readonly IChatProvider _chat;

    public string ActionType => "ban";

    public BanAction(IChatProvider chat) => _chat = chat;

    public async Task<ActionResult> ExecuteAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action
    )
    {
        string userId =
            action.GetString("user_id")
            ?? ctx.Variables.GetValueOrDefault("target.id")
            ?? ctx.Variables.GetValueOrDefault("user.id")
            ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
            return ActionResult.Failure("ban: user_id not resolved");

        string? reason = action.GetString("reason");
        await _chat.BanUserAsync(ctx.BroadcasterId, userId, reason, ctx.CancellationToken);
        return ActionResult.Success($"Banned {userId}");
    }
}
