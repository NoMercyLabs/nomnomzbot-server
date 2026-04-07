// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Infrastructure.Pipeline.Actions;

public sealed class StopAction : ICommandAction
{
    public string ActionType => "stop";

    public Task<ActionResult> ExecuteAsync(PipelineExecutionContext ctx, ActionDefinition action)
    {
        ctx.ShouldStop = true;
        return Task.FromResult(ActionResult.Success("Pipeline stopped"));
    }
}
