// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Api.Hubs.Clients;

public interface IOverlayClient
{
    Task ReceiveAlert(object alertData);
    Task ReceiveEvent(object eventData);
    Task ReceiveMusicUpdate(object musicData);
}
