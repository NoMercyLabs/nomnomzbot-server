// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.AspNetCore.SignalR;
using NoMercyBot.Api.Hubs.Clients;

namespace NoMercyBot.Api.Hubs;

public class OverlayHub : Hub<IOverlayClient>
{
    public async Task JoinOverlay(string overlayToken)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"overlay:{overlayToken}");
    }
}
