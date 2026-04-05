// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Infrastructure.Pipeline.Conditions;

/// <summary>
/// Condition: pass with a given probability.
/// Usage: { "type": "random", "chance": 0.5 }  (50% chance)
///        { "type": "random", "percent": 25 }   (25% chance)
/// </summary>
public sealed class RandomCondition : ICommandCondition
{
    public string ConditionType => "random";

    public bool Evaluate(PipelineExecutionContext ctx, ConditionDefinition condition)
    {
        double threshold = 0.5;

        if (condition.Parameters is not null)
        {
            if (
                condition.Parameters.TryGetValue("chance", out var chance)
                && chance.ValueKind == System.Text.Json.JsonValueKind.Number
            )
            {
                threshold = chance.GetDouble();
            }
            else if (
                condition.Parameters.TryGetValue("percent", out var pct)
                && pct.ValueKind == System.Text.Json.JsonValueKind.Number
            )
            {
                threshold = pct.GetDouble() / 100.0;
            }
        }

        return Random.Shared.NextDouble() < threshold;
    }
}
