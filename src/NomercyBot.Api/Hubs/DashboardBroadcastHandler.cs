// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Domain.ValueObjects;

namespace NoMercyBot.Api.Hubs;

/// <summary>
/// Listens to ChatMessageReceivedEvent and broadcasts the rich decorated
/// message to all dashboard/overlay clients subscribed to that channel group.
/// </summary>
public sealed class ChatMessageBroadcastHandler : IEventHandler<ChatMessageReceivedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public ChatMessageBroadcastHandler(IDashboardNotifier notifier)
    {
        _notifier = notifier;
    }

    public async Task HandleAsync(ChatMessageReceivedEvent evt, CancellationToken ct = default)
    {
        var dto = new DashboardChatMessageDto(
            MessageId: evt.MessageId,
            BroadcasterId: evt.BroadcasterId,
            UserId: evt.UserId,
            UserDisplayName: evt.UserDisplayName,
            UserLogin: evt.UserLogin,
            Message: evt.Message,
            Fragments: evt.Fragments.Select(MapFragment).ToList(),
            IsSubscriber: evt.IsSubscriber,
            IsVip: evt.IsVip,
            IsModerator: evt.IsModerator,
            IsBroadcaster: evt.IsBroadcaster,
            Badges: evt.Badges.Select(b => new ChatBadgeDto(b.SetId, b.Id, b.Info)).ToList(),
            Bits: evt.Bits,
            ColorHex: evt.ColorHex,
            MessageType: evt.MessageType,
            ReplyParentMessageId: evt.ReplyParentMessageId,
            ReplyParentMessageBody: evt.ReplyParentMessageBody,
            ReplyParentUserName: evt.ReplyParentUserName,
            Timestamp: DateTimeOffset.UtcNow.ToString("O"));

        await _notifier.SendChatMessageAsync(evt.BroadcasterId, dto, ct);
    }

    private static ChatFragmentDto MapFragment(ChatMessageFragment f) => new(
        Type: f.Type,
        Text: f.Text,
        Emote: f.EmoteId is not null
            ? new ChatEmoteDto(f.EmoteId, f.EmoteSetId, f.EmoteOwnerId, f.EmoteFormats)
            : null,
        Cheermote: f.CheermotePrefix is not null
            ? new ChatCheermoteDto(f.CheermotePrefix, f.CheermoteBits ?? 0, f.CheermoteTier ?? 1)
            : null,
        Mention: f.MentionUserId is not null
            ? new ChatMentionDto(f.MentionUserId, f.MentionUserLogin ?? string.Empty, f.MentionUserName ?? string.Empty)
            : null);
}
