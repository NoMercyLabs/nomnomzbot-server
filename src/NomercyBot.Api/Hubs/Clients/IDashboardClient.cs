// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Api.Hubs.Clients;

public interface IDashboardClient
{
    Task ReceiveChannelUpdate(object channelData);
    Task ReceiveChatMessage(object messageData);
    Task ReceiveChannelEvent(object eventData);
}
