// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Application.Pipeline;

public interface ICommandAction
{
    string Type { get; }
    string Category { get; }
    string Description { get; }
    Task<ActionResult> ExecuteAsync(ActionContext context);
}

public interface ICommandActionRegistry
{
    ICommandAction? GetAction(string type);
    IReadOnlyDictionary<string, ICommandAction> GetAll();
    void Register(ICommandAction action);
}
