// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Pipeline.Actions;

public sealed class SendReplyAction : ICommandAction
{
    private readonly IChatProvider _chat;
    private readonly ITemplateResolver _resolver;

    public string ActionType => "send_reply";

    public SendReplyAction(IChatProvider chat, ITemplateResolver resolver)
    {
        _chat = chat;
        _resolver = resolver;
    }

    public async Task<ActionResult> ExecuteAsync(PipelineExecutionContext ctx, ActionDefinition action)
    {
        var template = action.GetString("message") ?? action.GetString("text") ?? string.Empty;
        if (string.IsNullOrEmpty(template))
            return ActionResult.Failure("send_reply requires a 'message' parameter");

        var resolved = await _resolver.ResolveAsync(template, ctx.Variables, ctx.BroadcasterId, ctx.CancellationToken);
        await _chat.SendReplyAsync(ctx.BroadcasterId, ctx.MessageId, resolved, ctx.CancellationToken);
        return ActionResult.Success(resolved);
    }
}
