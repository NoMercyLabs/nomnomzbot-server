// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>Handles incoming raid events.</summary>
public sealed class RaidEventHandler : TwitchAlertHandlerBase<RaidEvent>, IEventHandler<RaidEvent>
{
    protected override string EventTypeKey => "raid";

    public RaidEventHandler(IServiceScopeFactory s, IPipelineEngine p, ILogger<RaidEventHandler> l)
        : base(s, p, l) { }

    protected override string? GetUserId(RaidEvent e) => e.FromUserId;

    protected override string? GetUserDisplayName(RaidEvent e) => e.FromDisplayName;

    protected override Dictionary<string, string> BuildVariables(RaidEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.FromDisplayName,
            ["user.id"] = e.FromUserId,
            ["user.name"] = e.FromLogin,
            ["viewers"] = e.ViewerCount.ToString(),
        };

    public Task HandleAsync(RaidEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}
