// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Pipeline.Actions;

public sealed class SendMessageAction : ICommandAction
{
    private readonly IChatProvider _chat;
    private readonly ITemplateResolver _resolver;

    public string ActionType => "send_message";

    public SendMessageAction(IChatProvider chat, ITemplateResolver resolver)
    {
        _chat = chat;
        _resolver = resolver;
    }

    public async Task<ActionResult> ExecuteAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action
    )
    {
        string template = action.GetString("message") ?? action.GetString("text") ?? string.Empty;
        if (string.IsNullOrEmpty(template))
            return ActionResult.Failure("send_message requires a 'message' parameter");

        string resolved = await _resolver.ResolveAsync(
            template,
            ctx.Variables,
            ctx.BroadcasterId,
            ctx.CancellationToken
        );
        await _chat.SendMessageAsync(ctx.BroadcasterId, resolved, ctx.CancellationToken);
        return ActionResult.Success(resolved);
    }
}
