// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Application.Pipeline.Actions;

public class RandomResponseAction : ICommandAction
{
    private readonly IChatProvider _chat;
    public string Type => "random_response";
    public string Category => "chat";
    public string Description => "Sends a random message from a list";

    public RandomResponseAction(IChatProvider chat) => _chat = chat;

    public async Task<ActionResult> ExecuteAsync(ActionContext ctx)
    {
        if (!ctx.Parameters.TryGetValue("responses", out object? respObj))
            return ActionResult.Fail("'responses' parameter is required");

        List<string> responses;
        if (
            respObj is System.Text.Json.JsonElement je
            && je.ValueKind == System.Text.Json.JsonValueKind.Array
        )
            responses = je.EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .Where(s => s != "")
                .ToList();
        else
            responses =
                respObj?.ToString()?.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                ?? [];

        if (responses.Count == 0)
            return ActionResult.Fail("'responses' list is empty");

        string msg = VariableResolver.Resolve(
            responses[Random.Shared.Next(responses.Count)],
            ctx.Variables
        );
        await _chat.SendMessageAsync(ctx.BroadcasterId, msg, ctx.CancellationToken);
        return ActionResult.Ok(msg);
    }
}
