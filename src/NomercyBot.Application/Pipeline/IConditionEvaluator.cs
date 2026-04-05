// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Application.Pipeline;

public interface IConditionEvaluator
{
    string Type { get; }
    Task<bool> EvaluateAsync(ConditionDefinition condition, ActionContext context);
}

public interface IConditionEvaluatorRegistry
{
    IConditionEvaluator? GetEvaluator(string type);
    void Register(IConditionEvaluator evaluator);
}
