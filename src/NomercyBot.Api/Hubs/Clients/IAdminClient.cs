// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Api.Hubs.Clients;

public interface IAdminClient
{
    Task ReceiveSystemStatus(object statusData);
    Task ReceiveLog(object logEntry);
    Task ReceiveChannelRegistryUpdate(object registryData);
}
