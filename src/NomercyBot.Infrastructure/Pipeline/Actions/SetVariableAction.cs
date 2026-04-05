// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Infrastructure.Pipeline.Actions;

public sealed class SetVariableAction : ICommandAction
{
    public string ActionType => "set_variable";

    public Task<ActionResult> ExecuteAsync(PipelineExecutionContext ctx, ActionDefinition action)
    {
        string? name = action.GetString("name");
        string value = action.GetString("value") ?? string.Empty;

        if (string.IsNullOrEmpty(name))
            return Task.FromResult(ActionResult.Failure("set_variable requires 'name'"));

        ctx.Variables[name] = value;
        return Task.FromResult(ActionResult.Success($"{name}={value}"));
    }
}
