// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NoMercyBot.Api.Hubs.Clients;

namespace NoMercyBot.Api.Hubs;

[Authorize(Roles = "admin")]
public class AdminHub : Hub<IAdminClient>
{
}
