// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Infrastructure.Configuration;

namespace NoMercyBot.Infrastructure.Services.Twitch;

/// <summary>
/// Twitch IRC-over-WebSocket chat service.
/// Connects to wss://irc-ws.chat.twitch.tv, joins/parts channels, sends messages,
/// parses incoming IRC lines, and publishes domain events to IEventBus.
///
/// Rate limiting: 20 messages / 30 s (non-verified bot default).
/// Reconnects automatically with exponential back-off capped at 64 s.
/// </summary>
public sealed class TwitchIrcService : ITwitchChatService, IHostedService
{
    private const string IrcUrl = "wss://irc-ws.chat.twitch.tv:443";

    // Rate limit: 20 msgs / 30 s for non-verified bots — keep 2 in reserve
    private const int RateLimitBurst = 18;
    private const int RateLimitWindowMs = 30_000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventBus _eventBus;
    private readonly TwitchOptions _options;
    private readonly ILogger<TwitchIrcService> _logger;

    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;

    // Channels that should be joined; re-joined after reconnect
    private readonly ConcurrentDictionary<string, byte> _joinedChannels = new(
        StringComparer.OrdinalIgnoreCase
    );

    // Send-side rate limiting
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private int _tokenCount = RateLimitBurst;
    private DateTime _windowStart = DateTime.UtcNow;

    public TwitchIrcService(
        IServiceScopeFactory scopeFactory,
        IEventBus eventBus,
        IOptions<TwitchOptions> options,
        ILogger<TwitchIrcService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _eventBus = eventBus;
        _options = options.Value;
        _logger = logger;
    }

    // ─── IHostedService ───────────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveLoop = Task.Run(() => RunWithReconnectAsync(_cts.Token), _cts.Token);
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        if (_receiveLoop is not null)
        {
            try
            {
                await _receiveLoop;
            }
            catch (OperationCanceledException) { }
        }
    }

    // ─── ITwitchChatService ───────────────────────────────────────────────────────

    public async Task SendMessageAsync(
        string channelId,
        string message,
        CancellationToken ct = default
    )
    {
        await SendRawWithRateLimitAsync($"PRIVMSG #{channelId.ToLowerInvariant()} :{message}", ct);
    }

    public async Task SendReplyAsync(
        string channelId,
        string replyToMessageId,
        string message,
        CancellationToken ct = default
    )
    {
        await SendRawWithRateLimitAsync(
            $"@reply-parent-msg-id={replyToMessageId} PRIVMSG #{channelId.ToLowerInvariant()} :{message}",
            ct
        );
    }

    public async Task JoinChannelAsync(string channelName, CancellationToken ct = default)
    {
        var name = channelName.TrimStart('#').ToLowerInvariant();
        _joinedChannels.TryAdd(name, 0);
        await SendRawAsync($"JOIN #{name}", ct);
        _logger.LogInformation("IRC: Joined #{ChannelName}", name);
    }

    public async Task LeaveChannelAsync(string channelName, CancellationToken ct = default)
    {
        var name = channelName.TrimStart('#').ToLowerInvariant();
        _joinedChannels.TryRemove(name, out _);
        await SendRawAsync($"PART #{name}", ct);
        _logger.LogInformation("IRC: Left #{ChannelName}", name);
    }

    // ─── Connection loop ──────────────────────────────────────────────────────────

    private async Task RunWithReconnectAsync(CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(1);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndReceiveAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IRC connection dropped, reconnecting in {Delay:g}", delay);
            }

            if (ct.IsCancellationRequested)
                break;

            await Task.Delay(delay, ct);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 64));
        }
    }

    private async Task ConnectAndReceiveAsync(CancellationToken ct)
    {
        _ws?.Dispose();
        _ws = new ClientWebSocket();

        _logger.LogInformation("IRC: Connecting to {Url}", IrcUrl);
        await _ws.ConnectAsync(new Uri(IrcUrl), ct);
        _logger.LogInformation("IRC: Connected");

        await SendRawAsync("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership", ct);

        var token = await GetBotTokenAsync(ct);
        await SendRawAsync($"PASS oauth:{token}", ct);
        await SendRawAsync($"NICK {_options.BotUsername}", ct);

        // Re-join all tracked channels after reconnect
        foreach (var channel in _joinedChannels.Keys)
            await SendRawAsync($"JOIN #{channel}", ct);

        var buffer = new byte[4096];
        var sb = new StringBuilder();

        while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            sb.Clear();
            WebSocketReceiveResult result;

            do
            {
                result = await _ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            foreach (var line in sb.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries))
                await HandleIrcLineAsync(line.TrimEnd('\r'), ct);
        }
    }

    // ─── IRC dispatch ─────────────────────────────────────────────────────────────

    private async Task HandleIrcLineAsync(string line, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(line))
            return;

        if (line.StartsWith("PING"))
        {
            var pong = line.Length > 5 ? line[5..] : ":tmi.twitch.tv";
            await SendRawAsync($"PONG {pong}", ct);
            return;
        }

        var (tags, _, command, parameters) = ParseIrcLine(line);

        switch (command)
        {
            case "PRIVMSG":
                await HandlePrivMsgAsync(tags, parameters, ct);
                break;

            case "USERNOTICE":
                await HandleUserNoticeAsync(tags, parameters, ct);
                break;

            case "CLEARCHAT":
                HandleClearChat(tags, parameters);
                break;

            case "CLEARMSG":
                tags.TryGetValue("target-msg-id", out var targetMsgId);
                tags.TryGetValue("login", out var deletedUserLogin);
                await _eventBus.PublishAsync(
                    new ChatMessageDeletedEvent
                    {
                        BroadcasterId = parameters.Count > 0 ? parameters[0].TrimStart('#') : null,
                        MessageId = targetMsgId ?? string.Empty,
                        DeletedByUserId = string.Empty, // IRC does not expose the moderator's ID
                        TargetUserId = deletedUserLogin ?? string.Empty,
                    },
                    ct
                );
                break;

            case "RECONNECT":
                _logger.LogWarning("IRC: Server requested RECONNECT");
                _ws?.Abort();
                break;

            case "001":
                _logger.LogInformation("IRC: Authenticated as {Username}", _options.BotUsername);
                break;
        }
    }

    private async Task HandlePrivMsgAsync(
        Dictionary<string, string> tags,
        List<string> parameters,
        CancellationToken ct
    )
    {
        if (parameters.Count < 2)
            return;

        var channel = parameters[0].TrimStart('#');
        var messageText = parameters[1];

        tags.TryGetValue("user-id", out var userId);
        tags.TryGetValue("display-name", out var displayName);
        tags.TryGetValue("login", out var login);
        tags.TryGetValue("id", out var messageId);
        tags.TryGetValue("badges", out var badgesRaw);
        tags.TryGetValue("subscriber", out var subRaw);
        tags.TryGetValue("mod", out var modRaw);
        tags.TryGetValue("vip", out var vipRaw);
        tags.TryGetValue("bits", out var bitsRaw);
        tags.TryGetValue("reply-parent-msg-id", out var replyParentId);

        var badgeDict = ParseBadges(badgesRaw);
        var badgeList = badgeDict
            .Select(kv => new NoMercyBot.Domain.ValueObjects.ChatBadge(kv.Key, kv.Value))
            .ToList();
        int.TryParse(bitsRaw, out var bits);

        var isBroadcaster = badgeDict.ContainsKey("broadcaster");

        await _eventBus.PublishAsync(
            new ChatMessageReceivedEvent
            {
                BroadcasterId = channel,
                MessageId = messageId ?? string.Empty,
                UserId = userId ?? string.Empty,
                UserDisplayName = displayName ?? login ?? string.Empty,
                UserLogin = login ?? string.Empty,
                Message = messageText,
                Fragments =
                [
                    new NoMercyBot.Domain.ValueObjects.ChatMessageFragment
                    {
                        Type = "text",
                        Text = messageText,
                    },
                ],
                IsSubscriber = subRaw == "1" || badgeDict.ContainsKey("subscriber"),
                IsVip = vipRaw == "1" || badgeDict.ContainsKey("vip"),
                IsModerator = modRaw == "1" || badgeDict.ContainsKey("moderator"),
                IsBroadcaster = isBroadcaster,
                Badges = badgeList,
                Bits = bits,
                ReplyParentMessageId = replyParentId,
            },
            ct
        );
    }

    private async Task HandleUserNoticeAsync(
        Dictionary<string, string> tags,
        List<string> parameters,
        CancellationToken ct
    )
    {
        tags.TryGetValue("msg-id", out var msgId);
        tags.TryGetValue("user-id", out var userId);
        tags.TryGetValue("login", out var login);
        tags.TryGetValue("display-name", out var displayName);
        tags.TryGetValue("msg-param-sub-plan", out var tier);

        var channel = parameters.Count > 0 ? parameters[0].TrimStart('#') : string.Empty;

        switch (msgId)
        {
            case "sub":
            case "resub":
                await _eventBus.PublishAsync(
                    new NewSubscriptionEvent
                    {
                        BroadcasterId = channel,
                        UserId = userId ?? string.Empty,
                        UserDisplayName = displayName ?? login ?? string.Empty,
                        Tier = tier ?? "1000",
                    },
                    ct
                );
                break;

            case "subgift":
            case "submysterygift":
                tags.TryGetValue("msg-param-mass-gift-count", out var giftCountRaw);
                int.TryParse(giftCountRaw, out var giftCount);
                var isAnon = login == "ananonymousgifter";

                await _eventBus.PublishAsync(
                    new GiftSubscriptionEvent
                    {
                        BroadcasterId = channel,
                        GifterUserId = userId ?? string.Empty,
                        GifterDisplayName = displayName ?? login ?? string.Empty,
                        Tier = tier ?? "1000",
                        GiftCount = Math.Max(giftCount, 1),
                        IsAnonymous = isAnon,
                        Recipients = [],
                    },
                    ct
                );
                break;

            case "raid":
                tags.TryGetValue("msg-param-viewerCount", out var viewerCountRaw);
                int.TryParse(viewerCountRaw, out var viewerCount);

                await _eventBus.PublishAsync(
                    new RaidEvent
                    {
                        BroadcasterId = channel,
                        FromUserId = userId ?? string.Empty,
                        FromDisplayName = displayName ?? login ?? string.Empty,
                        FromLogin = login ?? string.Empty,
                        ViewerCount = viewerCount,
                    },
                    ct
                );
                break;
        }
    }

    private void HandleClearChat(Dictionary<string, string> tags, List<string> parameters)
    {
        var channel = parameters.Count > 0 ? parameters[0].TrimStart('#') : string.Empty;
        var target = parameters.Count > 1 ? parameters[1] : null;
        tags.TryGetValue("ban-duration", out var duration);

        if (target is not null)
            _logger.LogDebug(
                "IRC CLEARCHAT #{Channel}: {User} (duration={Duration})",
                channel,
                target,
                duration ?? "perm"
            );
        else
            _logger.LogDebug("IRC CLEARCHAT #{Channel}: full chat cleared", channel);
    }

    // ─── Send helpers ─────────────────────────────────────────────────────────────

    private async Task SendRawWithRateLimitAsync(string line, CancellationToken ct)
    {
        await _sendLock.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            if ((now - _windowStart).TotalMilliseconds >= RateLimitWindowMs)
            {
                _tokenCount = RateLimitBurst;
                _windowStart = now;
            }

            if (_tokenCount <= 0)
            {
                var waitMs = (int)(RateLimitWindowMs - (now - _windowStart).TotalMilliseconds) + 50;
                if (waitMs > 0)
                {
                    _logger.LogDebug("IRC rate limit reached, pausing {WaitMs}ms", waitMs);
                    await Task.Delay(waitMs, ct);
                    _tokenCount = RateLimitBurst;
                    _windowStart = DateTime.UtcNow;
                }
            }

            _tokenCount--;
            await SendRawAsync(line, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task SendRawAsync(string line, CancellationToken ct)
    {
        if (_ws is not { State: WebSocketState.Open })
            return;

        var bytes = Encoding.UTF8.GetBytes(line + "\r\n");
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    // ─── Token access ─────────────────────────────────────────────────────────────

    private async Task<string> GetBotTokenAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var service = await db
            .Services.Where(s => s.Name == "twitch_bot" && s.Enabled && s.AccessToken != null)
            .OrderByDescending(s => s.TokenExpiry)
            .FirstOrDefaultAsync(ct);

        if (service?.AccessToken is null)
        {
            _logger.LogWarning("IRC: No bot token found — connecting anonymously");
            return "SCHMOOPIIE";
        }

        return encryption.TryDecrypt(service.AccessToken) ?? "SCHMOOPIIE";
    }

    // ─── IRC parser ───────────────────────────────────────────────────────────────

    private static (
        Dictionary<string, string> Tags,
        string Prefix,
        string Command,
        List<string> Parameters
    ) ParseIrcLine(string line)
    {
        var tags = new Dictionary<string, string>(StringComparer.Ordinal);
        var prefix = string.Empty;
        var pos = 0;

        // @tags
        if (line.Length > 0 && line[0] == '@')
        {
            var end = line.IndexOf(' ', 1);
            if (end < 0)
                return (tags, prefix, line, []);

            foreach (var tag in line[1..end].Split(';'))
            {
                var eq = tag.IndexOf('=');
                if (eq >= 0)
                    tags[tag[..eq]] = UnescapeTagValue(tag[(eq + 1)..]);
                else
                    tags[tag] = string.Empty;
            }

            pos = end + 1;
        }

        while (pos < line.Length && line[pos] == ' ')
            pos++;

        // :prefix
        if (pos < line.Length && line[pos] == ':')
        {
            var end = line.IndexOf(' ', pos + 1);
            prefix = end >= 0 ? line[(pos + 1)..end] : line[(pos + 1)..];
            pos = end >= 0 ? end + 1 : line.Length;
        }

        while (pos < line.Length && line[pos] == ' ')
            pos++;

        // command
        var cmdEnd = line.IndexOf(' ', pos);
        var command = cmdEnd >= 0 ? line[pos..cmdEnd] : line[pos..];
        pos = cmdEnd >= 0 ? cmdEnd + 1 : line.Length;

        // parameters
        var parameters = new List<string>();
        while (pos < line.Length)
        {
            while (pos < line.Length && line[pos] == ' ')
                pos++;
            if (pos >= line.Length)
                break;

            if (line[pos] == ':')
            {
                parameters.Add(line[(pos + 1)..]);
                break;
            }

            var end = line.IndexOf(' ', pos);
            if (end < 0)
            {
                parameters.Add(line[pos..]);
                break;
            }
            parameters.Add(line[pos..end]);
            pos = end + 1;
        }

        return (tags, prefix, command, parameters);
    }

    private static IReadOnlyDictionary<string, string> ParseBadges(string? raw)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(raw))
            return result;

        foreach (var badge in raw.Split(','))
        {
            var slash = badge.IndexOf('/');
            if (slash >= 0)
                result[badge[..slash]] = badge[(slash + 1)..];
        }

        return result;
    }

    private static string UnescapeTagValue(string value) =>
        value
            .Replace("\\:", ";")
            .Replace("\\s", " ")
            .Replace("\\\\", "\\")
            .Replace("\\r", "\r")
            .Replace("\\n", "\n");
}
