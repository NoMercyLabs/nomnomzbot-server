// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Api.Hubs.Clients;
using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs;

public class OverlayHub : Hub<IOverlayClient>
{
    private static readonly ConcurrentDictionary<string, string> _connectionWidget = new(); // connectionId -> widgetId
    private readonly IChannelRegistry _registry;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<OverlayHub> _logger;

    public OverlayHub(
        IChannelRegistry registry,
        IApplicationDbContext db,
        ILogger<OverlayHub> logger
    )
    {
        _registry = registry;
        _db = db;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // Validate overlay token from query string
        var token = Context.GetHttpContext()?.Request.Query["token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            Context.Abort();
            return;
        }

        var channel = await _db.Channels.FirstOrDefaultAsync(c => c.OverlayToken == token);
        if (channel == null)
        {
            Context.Abort();
            return;
        }

        Context.Items["BroadcasterId"] = channel.Id;
        _logger.LogDebug("Overlay connected for channel {B}", channel.Id);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionWidget.TryRemove(Context.ConnectionId, out var widgetId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"widget-{widgetId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<JoinWidgetResponse> JoinWidget(string widgetId)
    {
        var broadcasterId = Context.Items["BroadcasterId"] as string;
        if (broadcasterId == null)
            return new JoinWidgetResponse(false, "Not authenticated", null);

        var groupName = $"widget-{broadcasterId}-{widgetId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _connectionWidget[Context.ConnectionId] = $"{broadcasterId}-{widgetId}";
        _logger.LogDebug(
            "Overlay connection {C} joined widget {W}",
            Context.ConnectionId,
            widgetId
        );
        return new JoinWidgetResponse(true, null, null);
    }

    public async Task LeaveWidget(string widgetId)
    {
        var broadcasterId = Context.Items["BroadcasterId"] as string;
        if (broadcasterId == null)
            return;
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            $"widget-{broadcasterId}-{widgetId}"
        );
        _connectionWidget.TryRemove(Context.ConnectionId, out _);
    }

    public Task WidgetReady(string widgetId)
    {
        _logger.LogDebug("Widget {W} ready on connection {C}", widgetId, Context.ConnectionId);
        return Task.CompletedTask;
    }
}
