// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Infrastructure.Pipeline.Actions;

public sealed class WaitAction : ICommandAction
{
    public string ActionType => "wait";

    public async Task<ActionResult> ExecuteAsync(PipelineExecutionContext ctx, ActionDefinition action)
    {
        var ms = action.GetInt("milliseconds", 0);
        var seconds = action.GetInt("seconds", 0);
        var totalMs = ms + seconds * 1000;

        if (totalMs <= 0) return ActionResult.Success();
        if (totalMs > 30_000) totalMs = 30_000; // cap at 30s per step

        await Task.Delay(totalMs, ctx.CancellationToken);
        return ActionResult.Success();
    }
}
