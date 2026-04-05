// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Application.Pipeline.Actions;

public class SendMessageAction : ICommandAction
{
    private readonly IChatProvider _chat;
    public string Type => "send_message";
    public string Category => "chat";
    public string Description => "Sends a message to chat";

    public SendMessageAction(IChatProvider chat) => _chat = chat;

    public async Task<ActionResult> ExecuteAsync(ActionContext ctx)
    {
        IReadOnlyDictionary<string, string> resolved = VariableResolver.ResolveAll(ctx.Parameters, ctx.Variables);
        if (!resolved.TryGetValue("message", out string? message) || string.IsNullOrWhiteSpace(message))
            return ActionResult.Fail("'message' parameter is required");

        await _chat.SendMessageAsync(ctx.BroadcasterId, message, ctx.CancellationToken);
        return ActionResult.Ok(message);
    }
}
