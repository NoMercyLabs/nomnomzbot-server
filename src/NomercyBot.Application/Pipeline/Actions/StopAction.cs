// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Application.Pipeline.Actions;

public class StopAction : ICommandAction
{
    public string Type => "stop";
    public string Category => "control";
    public string Description => "Stops pipeline execution";

    public Task<ActionResult> ExecuteAsync(ActionContext ctx)
        => Task.FromResult(ActionResult.Stop());
}
