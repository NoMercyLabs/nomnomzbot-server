// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Pipeline.Actions;

public sealed class DeleteMessageAction : ICommandAction
{
    private readonly IChatProvider _chat;

    public string ActionType => "delete_message";

    public DeleteMessageAction(IChatProvider chat) => _chat = chat;

    public async Task<ActionResult> ExecuteAsync(PipelineExecutionContext ctx, ActionDefinition action)
    {
        var messageId = action.GetString("message_id") ?? ctx.MessageId;
        if (string.IsNullOrEmpty(messageId))
            return ActionResult.Failure("delete_message: message_id not resolved");

        await _chat.DeleteMessageAsync(ctx.BroadcasterId, messageId, ctx.CancellationToken);
        return ActionResult.Success($"Deleted message {messageId}");
    }
}
