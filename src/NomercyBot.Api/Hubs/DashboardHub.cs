// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NoMercyBot.Api.Hubs.Clients;

namespace NoMercyBot.Api.Hubs;

[Authorize]
public class DashboardHub : Hub<IDashboardClient>
{
    public async Task JoinChannelGroup(string channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");
    }

    public async Task LeaveChannelGroup(string channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");
    }
}
