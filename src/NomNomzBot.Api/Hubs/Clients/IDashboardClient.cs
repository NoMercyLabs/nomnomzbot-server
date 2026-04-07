// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Api.Hubs.Dtos;

namespace NoMercyBot.Api.Hubs.Clients;

public interface IDashboardClient
{
    Task ChatMessage(DashboardChatMessageDto message);
    Task ChannelEvent(ChannelEventDto evt);
    Task PermissionChanged(PermissionChangedDto evt);
    Task MusicStateChanged(MusicStateDto state);
    Task ModAction(ModActionDto action);
    Task CommandExecuted(CommandExecutedDto evt);
    Task RewardRedeemed(RewardRedeemedDto evt);
    Task StreamStatusChanged(StreamStatusDto status);
    Task AlertTriggered(AlertDto alert);
}
