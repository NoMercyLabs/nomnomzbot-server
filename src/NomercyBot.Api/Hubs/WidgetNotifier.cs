// SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.AspNetCore.SignalR;
using NoMercyBot.Api.Hubs.Clients;
using NoMercyBot.Api.Hubs.Dtos;

namespace NoMercyBot.Api.Hubs;

public interface IWidgetNotifier
{
    Task SendWidgetEventAsync(
        string broadcasterId,
        string widgetId,
        WidgetEventDto dto,
        CancellationToken ct = default
    );
    Task ReloadWidgetAsync(string broadcasterId, string widgetId, CancellationToken ct = default);
    Task SendSettingsChangedAsync(
        string broadcasterId,
        string widgetId,
        WidgetSettingsDto dto,
        CancellationToken ct = default
    );
}

public class WidgetNotifier : IWidgetNotifier
{
    private readonly IHubContext<OverlayHub, IOverlayClient> _hub;

    public WidgetNotifier(IHubContext<OverlayHub, IOverlayClient> hub) => _hub = hub;

    public Task SendWidgetEventAsync(
        string broadcasterId,
        string widgetId,
        WidgetEventDto dto,
        CancellationToken ct = default
    ) => _hub.Clients.Group($"widget-{broadcasterId}-{widgetId}").WidgetEvent(dto);

    public Task ReloadWidgetAsync(
        string broadcasterId,
        string widgetId,
        CancellationToken ct = default
    ) => _hub.Clients.Group($"widget-{broadcasterId}-{widgetId}").WidgetReload();

    public Task SendSettingsChangedAsync(
        string broadcasterId,
        string widgetId,
        WidgetSettingsDto dto,
        CancellationToken ct = default
    ) => _hub.Clients.Group($"widget-{broadcasterId}-{widgetId}").WidgetSettingsChanged(dto);
}
