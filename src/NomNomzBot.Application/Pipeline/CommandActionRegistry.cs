// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Concurrent;

namespace NoMercyBot.Application.Pipeline;

public class CommandActionRegistry : ICommandActionRegistry
{
    private readonly ConcurrentDictionary<string, ICommandAction> _actions = new(
        StringComparer.OrdinalIgnoreCase
    );

    public ICommandAction? GetAction(string type) => _actions.GetValueOrDefault(type);

    public IReadOnlyDictionary<string, ICommandAction> GetAll() => _actions;

    public void Register(ICommandAction action) => _actions[action.Type] = action;
}
