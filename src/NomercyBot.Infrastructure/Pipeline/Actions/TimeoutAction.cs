// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Pipeline.Actions;

public sealed class TimeoutAction : ICommandAction
{
    private readonly IChatProvider _chat;

    public string ActionType => "timeout";

    public TimeoutAction(IChatProvider chat) => _chat = chat;

    public async Task<ActionResult> ExecuteAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action
    )
    {
        var userId =
            action.GetString("user_id")
            ?? ctx.Variables.GetValueOrDefault("target.id")
            ?? ctx.Variables.GetValueOrDefault("user.id")
            ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
            return ActionResult.Failure("timeout: user_id not resolved");

        var duration = action.GetInt("duration", 60);
        var reason = action.GetString("reason");

        await _chat.TimeoutUserAsync(
            ctx.BroadcasterId,
            userId,
            duration,
            reason,
            ctx.CancellationToken
        );
        return ActionResult.Success($"Timed out {userId} for {duration}s");
    }
}
