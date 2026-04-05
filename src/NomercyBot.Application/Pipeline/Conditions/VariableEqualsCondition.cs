// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Application.Pipeline.Conditions;

public class VariableEqualsCondition : IConditionEvaluator
{
    public string Type => "variable_equals";

    public Task<bool> EvaluateAsync(ConditionDefinition condition, ActionContext ctx)
    {
        if (condition.Variable == null)
            return Task.FromResult(false);
        ctx.Variables.TryGetValue(condition.Variable, out var actual);
        var result = condition.Operator switch
        {
            "not_equals" => actual != condition.Value,
            "contains" => actual?.Contains(
                condition.Value ?? "",
                StringComparison.OrdinalIgnoreCase
            ) ?? false,
            "starts_with" => actual?.StartsWith(
                condition.Value ?? "",
                StringComparison.OrdinalIgnoreCase
            ) ?? false,
            "is_empty" => string.IsNullOrEmpty(actual),
            "is_not_empty" => !string.IsNullOrEmpty(actual),
            _ => string.Equals(actual, condition.Value, StringComparison.OrdinalIgnoreCase),
        };
        return Task.FromResult(result);
    }
}
