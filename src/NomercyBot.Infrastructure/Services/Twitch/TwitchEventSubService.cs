// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Domain.ValueObjects;
using NoMercyBot.Infrastructure.Configuration;

namespace NoMercyBot.Infrastructure.Services.Twitch;

/// <summary>
/// Manages the Twitch EventSub WebSocket connection.
///
/// Flow:
///   1. Connect to wss://eventsub.wss.twitch.tv/ws
///   2. Receive session_welcome → extract session_id
///   3. Create subscriptions via REST POST /eventsub/subscriptions
///   4. Receive notification messages → publish domain events to IEventBus
///   5. Handle session_reconnect: connect to new URL, wait for Welcome, then close old
///
/// ITwitchEventSubService methods let callers subscribe/unsubscribe at runtime.
/// Reconnects automatically with exponential back-off capped at 64 s.
/// </summary>
public sealed class TwitchEventSubService : ITwitchEventSubService, IHostedService
{
    private const string DefaultWsUrl = "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30";
    private const string HelixBase = "https://api.twitch.tv/helix";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventBus _eventBus;
    private readonly TwitchOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<TwitchEventSubService> _logger;

    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;

    private string? _sessionId;

    // Pending subscriptions: broadcasterId → list of eventType
    // Stored before the WebSocket connects, then subscribed once session_id is available
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _pendingSubscriptions = new();

    // Active subscriptions: subscriptionId → (broadcasterId, eventType)
    private readonly ConcurrentDictionary<string, (string BroadcasterId, string EventType)> _activeSubscriptions = new();

    public TwitchEventSubService(
        IServiceScopeFactory scopeFactory,
        IEventBus eventBus,
        IOptions<TwitchOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TwitchEventSubService> logger)
    {
        _scopeFactory = scopeFactory;
        _eventBus = eventBus;
        _options = options.Value;
        _http = httpClientFactory.CreateClient("twitch-eventsub");
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
            try { await _receiveLoop; }
            catch (OperationCanceledException) { }
        }
    }

    // ─── ITwitchEventSubService ───────────────────────────────────────────────────

    public async Task SubscribeAsync(string broadcasterId, string eventType, CancellationToken ct = default)
    {
        if (_sessionId is null)
        {
            // Queue for when the session is ready
            _pendingSubscriptions
                .GetOrAdd(broadcasterId, _ => new ConcurrentBag<string>())
                .Add(eventType);

            _logger.LogDebug("EventSub: Queued subscription {EventType} for {BroadcasterId} (no session yet)",
                eventType, broadcasterId);
            return;
        }

        await CreateSubscriptionAsync(broadcasterId, eventType, _sessionId, ct);
    }

    public async Task UnsubscribeAsync(string subscriptionId, CancellationToken ct = default)
    {
        var token = await GetBotTokenAsync(ct);
        if (token is null) return;

        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{HelixBase}/eventsub/subscriptions?id={Uri.EscapeDataString(subscriptionId)}");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Client-Id", _options.ClientId);

        var resp = await _http.SendAsync(request, ct);

        if (resp.IsSuccessStatusCode)
        {
            _activeSubscriptions.TryRemove(subscriptionId, out _);
            _logger.LogInformation("EventSub: Unsubscribed {SubscriptionId}", subscriptionId);
        }
        else
        {
            _logger.LogWarning("EventSub: Failed to unsubscribe {SubscriptionId}: {Status}",
                subscriptionId, resp.StatusCode);
        }
    }

    public async Task<IReadOnlyList<string>> GetActiveSubscriptionsAsync(string broadcasterId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return _activeSubscriptions
            .Where(kvp => kvp.Value.BroadcasterId == broadcasterId)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    // ─── Connection loop ──────────────────────────────────────────────────────────

    private async Task RunWithReconnectAsync(CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(1);
        var connectUrl = DefaultWsUrl;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                connectUrl = await ConnectAndReceiveAsync(connectUrl, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventSub connection dropped, reconnecting in {Delay:g}", delay);
                connectUrl = DefaultWsUrl; // reset to default on error
            }

            if (ct.IsCancellationRequested) break;

            _sessionId = null;
            await Task.Delay(delay, ct);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 64));
        }
    }

    /// <summary>
    /// Returns the next URL to connect to (either default or reconnect URL).
    /// </summary>
    private async Task<string> ConnectAndReceiveAsync(string url, CancellationToken ct)
    {
        _ws?.Dispose();
        _ws = new ClientWebSocket();

        _logger.LogInformation("EventSub: Connecting to {Url}", url);
        await _ws.ConnectAsync(new Uri(url), ct);
        _logger.LogInformation("EventSub: Connected");

        var buffer = new byte[16384];
        var sb = new StringBuilder();

        while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            sb.Clear();
            WebSocketReceiveResult result;

            do
            {
                result = await _ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return DefaultWsUrl;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            var reconnectUrl = await HandleEventSubMessageAsync(sb.ToString(), ct);
            if (reconnectUrl is not null)
                return reconnectUrl;
        }

        return DefaultWsUrl;
    }

    private async Task<string?> HandleEventSubMessageAsync(string json, CancellationToken ct)
    {
        EventSubEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EventSubEnvelope>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EventSub: Failed to parse message");
            return null;
        }

        if (envelope is null) return null;

        switch (envelope.Metadata?.MessageType)
        {
            case "session_welcome":
                _sessionId = envelope.Payload?.Session?.Id;
                _logger.LogInformation("EventSub: Session established {SessionId}", _sessionId);

                if (_sessionId is not null)
                    await SubscribePendingAsync(_sessionId, ct);
                break;

            case "session_keepalive":
                // No-op — confirms connection is alive
                break;

            case "notification":
                await HandleNotificationAsync(envelope, ct);
                break;

            case "session_reconnect":
                var reconnectUrl = envelope.Payload?.Session?.ReconnectUrl;
                _logger.LogWarning("EventSub: Server requested reconnect to {Url}", reconnectUrl);
                return reconnectUrl ?? DefaultWsUrl;

            case "revocation":
                var subId = envelope.Payload?.Subscription?.Id;
                if (subId is not null)
                {
                    _activeSubscriptions.TryRemove(subId, out _);
                    _logger.LogWarning("EventSub: Subscription {SubId} revoked", subId);
                }
                break;
        }

        return null;
    }

    // ─── Notification dispatch ────────────────────────────────────────────────────

    private async Task HandleNotificationAsync(EventSubEnvelope envelope, CancellationToken ct)
    {
        var subscriptionType = envelope.Payload?.Subscription?.Type;
        var eventData = envelope.Payload?.Event;
        var broadcasterId = eventData?.GetProp("broadcaster_user_id")
                         ?? eventData?.GetProp("to_broadcaster_user_id")
                         ?? string.Empty;

        _logger.LogDebug("EventSub notification: {Type} for {BroadcasterId}", subscriptionType, broadcasterId);

        switch (subscriptionType)
        {
            case "channel.follow":
                await _eventBus.PublishAsync(new FollowEvent
                {
                    BroadcasterId = broadcasterId,
                    UserId = eventData?.GetProp("user_id") ?? string.Empty,
                    UserLogin = eventData?.GetProp("user_login") ?? string.Empty,
                    UserDisplayName = eventData?.GetProp("user_name") ?? string.Empty,
                    FollowedAt = DateTimeOffset.TryParse(eventData?.GetProp("followed_at"), out var fa)
                        ? fa : DateTimeOffset.UtcNow,
                }, ct);
                break;

            case "channel.subscribe":
                await _eventBus.PublishAsync(new NewSubscriptionEvent
                {
                    BroadcasterId = broadcasterId,
                    UserId = eventData?.GetProp("user_id") ?? string.Empty,
                    UserDisplayName = eventData?.GetProp("user_name") ?? string.Empty,
                    Tier = eventData?.GetProp("tier") ?? "1000",
                }, ct);
                break;

            case "channel.subscription.gift":
                int.TryParse(eventData?.GetProp("total"), out var giftCount);
                var isAnon = eventData?.GetProp("is_anonymous") == "true";

                await _eventBus.PublishAsync(new GiftSubscriptionEvent
                {
                    BroadcasterId = broadcasterId,
                    GifterUserId = eventData?.GetProp("user_id") ?? string.Empty,
                    GifterDisplayName = eventData?.GetProp("user_name") ?? string.Empty,
                    Tier = eventData?.GetProp("tier") ?? "1000",
                    GiftCount = Math.Max(giftCount, 1),
                    IsAnonymous = isAnon,
                    Recipients = [],
                }, ct);
                break;

            case "channel.cheer":
                int.TryParse(eventData?.GetProp("bits"), out var bits);
                await _eventBus.PublishAsync(new CheerEvent
                {
                    BroadcasterId = broadcasterId,
                    UserId = eventData?.GetProp("user_id") ?? string.Empty,
                    UserDisplayName = eventData?.GetProp("user_name") ?? string.Empty,
                    Bits = bits,
                    Message = eventData?.GetProp("message") ?? string.Empty,
                    IsAnonymous = eventData?.GetProp("is_anonymous") == "true",
                }, ct);
                break;

            case "channel.raid":
                int.TryParse(eventData?.GetProp("viewers"), out var viewers);
                await _eventBus.PublishAsync(new RaidEvent
                {
                    BroadcasterId = broadcasterId,
                    FromUserId = eventData?.GetProp("from_broadcaster_user_id") ?? string.Empty,
                    FromDisplayName = eventData?.GetProp("from_broadcaster_user_name") ?? string.Empty,
                    FromLogin = eventData?.GetProp("from_broadcaster_user_login") ?? string.Empty,
                    ViewerCount = viewers,
                }, ct);
                break;

            case "channel.ban":
                // ModerationActionTakenEvent is a DomainEvent record (not IDomainEvent); log only
                _logger.LogInformation(
                    "EventSub ban/timeout: channel={BroadcasterId} target={Target} mod={Mod} reason={Reason}",
                    broadcasterId,
                    eventData?.GetProp("user_login"),
                    eventData?.GetProp("moderator_user_login"),
                    eventData?.GetProp("reason"));
                break;

            case "channel.channel_points_custom_reward_redemption.add":
                var reward = envelope.Payload?.Event?.TryGetProperty("reward", out var rewardProp) == true
                    ? rewardProp : (JsonElement?)null;

                await _eventBus.PublishAsync(new RewardRedeemedEvent
                {
                    BroadcasterId = broadcasterId,
                    RedemptionId = eventData?.GetProp("id") ?? string.Empty,
                    RewardId = reward?.GetProp("id") ?? string.Empty,
                    RewardTitle = reward?.GetProp("title") ?? string.Empty,
                    Cost = reward?.TryGetProperty("cost", out var costProp) == true
                        ? costProp.GetInt32() : 0,
                    UserId = eventData?.GetProp("user_id") ?? string.Empty,
                    UserDisplayName = eventData?.GetProp("user_name") ?? string.Empty,
                    UserInput = eventData?.GetProp("user_input"),
                }, ct);
                break;

            case "stream.online":
                var streamTitle = eventData?.GetProp("title") ?? string.Empty;
                var gameName = eventData?.GetProp("category_name") ?? string.Empty;
                DateTimeOffset.TryParse(eventData?.GetProp("started_at"), out var startedAt);

                await _eventBus.PublishAsync(new ChannelOnlineEvent
                {
                    BroadcasterId = broadcasterId,
                    BroadcasterDisplayName = eventData?.GetProp("broadcaster_user_name") ?? broadcasterId,
                    StreamTitle = streamTitle,
                    GameName = gameName,
                    StartedAt = startedAt == default ? DateTimeOffset.UtcNow : startedAt,
                }, ct);
                break;

            case "stream.offline":
                await _eventBus.PublishAsync(new ChannelOfflineEvent
                {
                    BroadcasterId = broadcasterId,
                    BroadcasterDisplayName = eventData?.GetProp("broadcaster_user_name") ?? broadcasterId,
                    StreamDuration = TimeSpan.Zero, // Duration requires tracking stream start externally
                }, ct);
                break;

            case "channel.chat.message":
                if (eventData.HasValue)
                    await HandleChatMessageAsync(eventData.Value, broadcasterId, ct);
                break;
        }
    }

    // ─── channel.chat.message parsing ────────────────────────────────────────────

    private async Task HandleChatMessageAsync(JsonElement eventData, string broadcasterId, CancellationToken ct)
    {
        var messageId = eventData.GetProp("message_id") ?? Guid.NewGuid().ToString();
        var userId = eventData.GetProp("chatter_user_id") ?? string.Empty;
        var userLogin = eventData.GetProp("chatter_user_login") ?? string.Empty;
        var userDisplayName = eventData.GetProp("chatter_user_name") ?? userLogin;
        var colorHex = eventData.GetProp("color");
        var messageType = eventData.GetProp("message_type") ?? "text";

        // Parse message object
        string rawText = string.Empty;
        List<ChatMessageFragment> fragments = [];

        if (eventData.TryGetProperty("message", out var messageObj))
        {
            rawText = messageObj.GetProp("text") ?? string.Empty;

            if (messageObj.TryGetProperty("fragments", out var fragmentsArr)
                && fragmentsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var frag in fragmentsArr.EnumerateArray())
                {
                    fragments.Add(ParseFragment(frag));
                }
            }
        }

        // Parse badges
        List<ChatBadge> badges = [];
        if (eventData.TryGetProperty("badges", out var badgesArr)
            && badgesArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var badge in badgesArr.EnumerateArray())
            {
                var setId = badge.GetProp("set_id") ?? string.Empty;
                var badgeId = badge.GetProp("id") ?? string.Empty;
                var info = badge.GetProp("info");
                badges.Add(new ChatBadge(setId, badgeId, info));
            }
        }

        // Parse cheer bits
        int bits = 0;
        if (eventData.TryGetProperty("cheer", out var cheerObj)
            && cheerObj.TryGetProperty("bits", out var bitsEl))
        {
            bits = bitsEl.GetInt32();
        }

        // Parse reply thread
        string? replyParentId = null;
        string? replyParentBody = null;
        string? replyParentUserName = null;

        if (eventData.TryGetProperty("reply", out var replyObj))
        {
            replyParentId = replyObj.GetProp("parent_message_id");
            replyParentBody = replyObj.GetProp("parent_message_body");
            replyParentUserName = replyObj.GetProp("parent_user_name");
        }

        // Derive roles from badges
        bool isBroadcaster = badges.Any(b => b.SetId == "broadcaster");
        bool isModerator = isBroadcaster || badges.Any(b => b.SetId == "moderator");
        bool isVip = badges.Any(b => b.SetId == "vip");
        bool isSubscriber = badges.Any(b => b.SetId is "subscriber" or "founder");

        await _eventBus.PublishAsync(new ChatMessageReceivedEvent
        {
            MessageId = messageId,
            BroadcasterId = broadcasterId,
            UserId = userId,
            UserLogin = userLogin,
            UserDisplayName = userDisplayName,
            Message = rawText,
            Fragments = fragments,
            ColorHex = colorHex,
            MessageType = messageType,
            Badges = badges,
            Bits = bits,
            IsSubscriber = isSubscriber,
            IsVip = isVip,
            IsModerator = isModerator,
            IsBroadcaster = isBroadcaster,
            ReplyParentMessageId = replyParentId,
            ReplyParentMessageBody = replyParentBody,
            ReplyParentUserName = replyParentUserName,
        }, ct);
    }

    private static ChatMessageFragment ParseFragment(JsonElement frag)
    {
        var type = frag.GetProp("type") ?? "text";
        var text = frag.GetProp("text") ?? string.Empty;

        switch (type)
        {
            case "emote":
            {
                if (!frag.TryGetProperty("emote", out var emoteObj))
                    return new ChatMessageFragment { Type = type, Text = text };

                var formats = Array.Empty<string>();
                if (emoteObj.TryGetProperty("format", out var fmtArr)
                    && fmtArr.ValueKind == JsonValueKind.Array)
                {
                    formats = fmtArr.EnumerateArray()
                        .Select(e => e.GetString() ?? string.Empty)
                        .ToArray();
                }

                return new ChatMessageFragment
                {
                    Type = type,
                    Text = text,
                    EmoteId = emoteObj.GetProp("id"),
                    EmoteSetId = emoteObj.GetProp("emote_set_id"),
                    EmoteOwnerId = emoteObj.GetProp("owner_id"),
                    EmoteFormats = formats,
                };
            }

            case "cheermote":
            {
                if (!frag.TryGetProperty("cheermote", out var cheerObj))
                    return new ChatMessageFragment { Type = type, Text = text };

                int bits = 0;
                if (cheerObj.TryGetProperty("bits", out var bitsEl)) bits = bitsEl.GetInt32();

                int tier = 1;
                if (cheerObj.TryGetProperty("tier", out var tierEl)) tier = tierEl.GetInt32();

                return new ChatMessageFragment
                {
                    Type = type,
                    Text = text,
                    CheermotePrefix = cheerObj.GetProp("prefix"),
                    CheermoteBits = bits,
                    CheermoteTier = tier,
                };
            }

            case "mention":
            {
                if (!frag.TryGetProperty("mention", out var mentionObj))
                    return new ChatMessageFragment { Type = type, Text = text };

                return new ChatMessageFragment
                {
                    Type = type,
                    Text = text,
                    MentionUserId = mentionObj.GetProp("user_id"),
                    MentionUserLogin = mentionObj.GetProp("user_login"),
                    MentionUserName = mentionObj.GetProp("user_name"),
                };
            }

            default:
                return new ChatMessageFragment { Type = type, Text = text };
        }
    }

    // ─── Subscription management ──────────────────────────────────────────────────

    private async Task SubscribePendingAsync(string sessionId, CancellationToken ct)
    {
        foreach (var (broadcasterId, eventTypes) in _pendingSubscriptions)
        {
            foreach (var eventType in eventTypes)
            {
                try
                {
                    await CreateSubscriptionAsync(broadcasterId, eventType, sessionId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EventSub: Failed to create pending subscription {EventType} for {BroadcasterId}",
                        eventType, broadcasterId);
                }
            }
        }

        _pendingSubscriptions.Clear();
    }

    private async Task CreateSubscriptionAsync(string broadcasterId, string eventType, string sessionId, CancellationToken ct)
    {
        var token = await GetBotTokenAsync(ct);
        if (token is null)
        {
            _logger.LogWarning("EventSub: No token available, cannot subscribe {EventType}", eventType);
            return;
        }

        var condition = BuildCondition(broadcasterId, eventType);
        var version = GetSubscriptionVersion(eventType);

        var body = new
        {
            type = eventType,
            version,
            condition,
            transport = new { method = "websocket", session_id = sessionId },
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{HelixBase}/eventsub/subscriptions");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Client-Id", _options.ClientId);
        request.Content = JsonContent.Create(body);

        var resp = await _http.SendAsync(request, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("EventSub: Failed to subscribe {EventType} for {BroadcasterId}: {Status} — {Error}",
                eventType, broadcasterId, resp.StatusCode, err);
            return;
        }

        var result = await resp.Content.ReadFromJsonAsync<HelixDataResponse<EventSubSubscription>>(
            cancellationToken: ct);
        var sub = result?.Data?.FirstOrDefault();

        if (sub is not null)
        {
            _activeSubscriptions[sub.Id] = (broadcasterId, eventType);
            _logger.LogInformation("EventSub: Subscribed {EventType} for {BroadcasterId} (id={SubId})",
                eventType, broadcasterId, sub.Id);
        }
    }

    private static Dictionary<string, string> BuildCondition(string broadcasterId, string eventType)
    {
        return eventType switch
        {
            "channel.follow" => new()
            {
                ["broadcaster_user_id"] = broadcasterId,
                ["moderator_user_id"] = broadcasterId,
            },
            "channel.raid" => new()
            {
                ["to_broadcaster_user_id"] = broadcasterId,
            },
            "channel.chat.message" or "channel.chat.notification" => new()
            {
                ["broadcaster_user_id"] = broadcasterId,
                ["user_id"] = broadcasterId,
            },
            "channel.shoutout.create" or "channel.shoutout.receive" => new()
            {
                ["broadcaster_user_id"] = broadcasterId,
                ["moderator_user_id"] = broadcasterId,
            },
            _ => new() { ["broadcaster_user_id"] = broadcasterId },
        };
    }

    private static string GetSubscriptionVersion(string eventType) =>
        eventType switch
        {
            "channel.follow" => "2",
            "channel.update" => "2",
            _ => "1",
        };

    // ─── Token access ─────────────────────────────────────────────────────────────

    private async Task<string?> GetBotTokenAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var service = await db.Services
            .Where(s => s.Name == "twitch_bot" && s.Enabled && s.AccessToken != null)
            .OrderByDescending(s => s.TokenExpiry)
            .FirstOrDefaultAsync(ct);

        if (service?.AccessToken is null) return null;
        return encryption.TryDecrypt(service.AccessToken);
    }

    // ─── JSON helpers ─────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class EventSubEnvelope
    {
        [JsonPropertyName("metadata")] public EventSubMetadata? Metadata { get; set; }
        [JsonPropertyName("payload")] public EventSubPayload? Payload { get; set; }
    }

    private sealed class EventSubMetadata
    {
        [JsonPropertyName("message_id")] public string? MessageId { get; set; }
        [JsonPropertyName("message_type")] public string? MessageType { get; set; }
        [JsonPropertyName("message_timestamp")] public string? MessageTimestamp { get; set; }
    }

    private sealed class EventSubPayload
    {
        [JsonPropertyName("session")] public EventSubSession? Session { get; set; }
        [JsonPropertyName("subscription")] public EventSubSubscription? Subscription { get; set; }
        [JsonPropertyName("event")] public JsonElement? Event { get; set; }
    }

    private sealed class EventSubSession
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("reconnect_url")] public string? ReconnectUrl { get; set; }
    }

    private sealed class EventSubSubscription
    {
        [JsonPropertyName("id")] public string Id { get; set; } = null!;
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    private sealed class HelixDataResponse<T>
    {
        [JsonPropertyName("data")] public List<T>? Data { get; set; }
    }
}

file static class JsonElementExtensions
{
    /// <summary>
    /// Safe property accessor for JsonElement. Use as: <c>element?.GetProp("key")</c>
    /// where <c>element</c> is <c>JsonElement?</c>.
    /// </summary>
    public static string? GetProp(this JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
}
