// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Api.Hubs.Dtos;
namespace NoMercyBot.Api.Hubs.Clients;

public interface IOverlayClient
{
    Task WidgetEvent(WidgetEventDto evt);
    Task WidgetReload();
    Task WidgetSettingsChanged(WidgetSettingsDto settings);
}
