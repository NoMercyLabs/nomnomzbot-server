// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>Handles bits/cheer events.</summary>
public sealed class CheerEventHandler
    : TwitchAlertHandlerBase<CheerEvent>,
        IEventHandler<CheerEvent>
{
    protected override string EventTypeKey => "cheer";

    public CheerEventHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<CheerEventHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(CheerEvent e) => e.IsAnonymous ? null : e.UserId;

    protected override string? GetUserDisplayName(CheerEvent e) =>
        e.IsAnonymous ? "Anonymous" : e.UserDisplayName;

    protected override Dictionary<string, string> BuildVariables(CheerEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.IsAnonymous ? "Anonymous" : e.UserDisplayName,
            ["user.id"] = e.IsAnonymous ? string.Empty : e.UserId,
            ["bits"] = e.Bits.ToString(),
            ["message"] = e.Message,
            ["anonymous"] = e.IsAnonymous ? "true" : "false",
        };

    public Task HandleAsync(CheerEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}
