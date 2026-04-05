// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Concurrent;
namespace NoMercyBot.Application.Pipeline;

public class ConditionEvaluatorRegistry : IConditionEvaluatorRegistry
{
    private readonly ConcurrentDictionary<string, IConditionEvaluator> _evaluators = new(StringComparer.OrdinalIgnoreCase);

    public IConditionEvaluator? GetEvaluator(string type) => _evaluators.GetValueOrDefault(type);
    public void Register(IConditionEvaluator evaluator) => _evaluators[evaluator.Type] = evaluator;
}
