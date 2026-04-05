// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Application.Pipeline.Actions;

public class DelayAction : ICommandAction
{
    public string Type => "delay";
    public string Category => "control";
    public string Description => "Waits for a specified number of seconds (max 30)";

    public async Task<ActionResult> ExecuteAsync(ActionContext ctx)
    {
        if (
            !ctx.Parameters.TryGetValue("seconds", out var secObj)
            || !double.TryParse(secObj?.ToString(), out var secs)
        )
            return ActionResult.Fail("'seconds' parameter is required");

        secs = Math.Clamp(secs, 0.1, 30.0);
        await Task.Delay(TimeSpan.FromSeconds(secs), ctx.CancellationToken);
        return ActionResult.Ok();
    }
}
