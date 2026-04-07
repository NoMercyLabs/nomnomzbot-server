// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Application.Pipeline.Actions;

public class SetVariableAction : ICommandAction
{
    public string Type => "set_variable";
    public string Category => "control";
    public string Description => "Sets a pipeline variable";

    public Task<ActionResult> ExecuteAsync(ActionContext ctx)
    {
        IReadOnlyDictionary<string, string> resolved = VariableResolver.ResolveAll(ctx.Parameters, ctx.Variables);
        if (!resolved.TryGetValue("name", out string? name) || string.IsNullOrWhiteSpace(name))
            return Task.FromResult(ActionResult.Fail("'name' parameter is required"));

        resolved.TryGetValue("value", out string? value);
        Dictionary<string, string> vars = new() { { name, value ?? string.Empty } };
        return Task.FromResult(ActionResult.Ok(vars: vars));
    }
}
