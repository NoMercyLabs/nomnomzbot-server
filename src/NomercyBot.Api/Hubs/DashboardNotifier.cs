// SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.AspNetCore.SignalR;
using NoMercyBot.Api.Hubs.Clients;
using NoMercyBot.Api.Hubs.Dtos;

namespace NoMercyBot.Api.Hubs;

public interface IDashboardNotifier
{
    Task NotifyChannelAsync(string broadcasterId, string method, object data, CancellationToken ct = default);
    Task SendChatMessageAsync(string broadcasterId, DashboardChatMessageDto dto, CancellationToken ct = default);
    Task SendStreamStatusAsync(string broadcasterId, StreamStatusDto dto, CancellationToken ct = default);
    Task SendCommandExecutedAsync(string broadcasterId, CommandExecutedDto dto, CancellationToken ct = default);
    Task SendAlertAsync(string broadcasterId, AlertDto dto, CancellationToken ct = default);
}

public class DashboardNotifier : IDashboardNotifier
{
    private readonly IHubContext<DashboardHub, IDashboardClient> _hub;

    public DashboardNotifier(IHubContext<DashboardHub, IDashboardClient> hub) => _hub = hub;

    public Task NotifyChannelAsync(string broadcasterId, string method, object data, CancellationToken ct = default)
        => _hub.Clients.Group($"channel-{broadcasterId}").ChannelEvent(
            new ChannelEventDto(method, broadcasterId, null, null, data, DateTimeOffset.UtcNow.ToString("O")));

    public Task SendChatMessageAsync(string broadcasterId, DashboardChatMessageDto dto, CancellationToken ct = default)
        => _hub.Clients.Group($"channel-{broadcasterId}").ChatMessage(dto);

    public Task SendStreamStatusAsync(string broadcasterId, StreamStatusDto dto, CancellationToken ct = default)
        => _hub.Clients.Group($"channel-{broadcasterId}").StreamStatusChanged(dto);

    public Task SendCommandExecutedAsync(string broadcasterId, CommandExecutedDto dto, CancellationToken ct = default)
        => _hub.Clients.Group($"channel-{broadcasterId}").CommandExecuted(dto);

    public Task SendAlertAsync(string broadcasterId, AlertDto dto, CancellationToken ct = default)
        => _hub.Clients.Group($"channel-{broadcasterId}").AlertTriggered(dto);
}
