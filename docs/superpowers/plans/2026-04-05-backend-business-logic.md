# Backend Business Logic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Flesh out all stub/placeholder implementations in the NoMercyBot backend with real business logic per design specs.

**Architecture:** Clean architecture (Domain → Application → Infrastructure → API). EventSub WebSocket is the primary chat path (receive via `channel.chat.message`, send via Helix POST `/chat/messages`). IRC is fallback-only for watch streaks. All new services register in `Infrastructure/DependencyInjection.cs`.

**Tech Stack:** .NET 9, EF Core + Npgsql, SignalR + MessagePack, Polly, EventSub WebSocket, Helix REST API, Spotify Web API, Edge TTS (Microsoft.CognitiveServices.Speech or edge-tts process)

---

## Task 1: EventSub-First Chat Infrastructure

**Files:**
- Modify: `src/NomercyBot.Application/Contracts/Twitch/ITwitchApiService.cs`
- Modify: `src/NomercyBot.Infrastructure/Services/Twitch/TwitchApiService.cs`
- Create: `src/NomercyBot.Infrastructure/Services/Twitch/HelixChatProvider.cs`
- Modify: `src/NomercyBot.Infrastructure/Services/Twitch/TwitchEventSubService.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] Add to `ITwitchApiService`: `SendMessageAsync(broadcasterId, botUserId, message)`, `SendReplyAsync(broadcasterId, botUserId, replyToId, message)`, `DeleteMessageAsync(broadcasterId, messageId)`, `UnbanUserAsync(broadcasterId, userId)`
- [ ] Implement all four in `TwitchApiService` via Helix endpoints
- [ ] Create `HelixChatProvider` implementing `IChatProvider` using `ITwitchApiService`
- [ ] Add `channel.chat.message` case to `TwitchEventSubService.HandleNotificationAsync`
- [ ] Update `BuildCondition` for `channel.chat.message` to use `user_id` = bot account ID
- [ ] Register `HelixChatProvider` as `IChatProvider` singleton in DI
- [ ] Build + commit

## Task 2: Command Execution Flow

**Files:**
- Create: `src/NomercyBot.Infrastructure/EventHandlers/ChatMessageHandler.cs`
- Modify: `src/NomercyBot.Infrastructure/BackgroundServices/ChannelManagerService.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] Create `ChatMessageHandler : IEventHandler<ChatMessageReceivedEvent>`
  - Parse `!commandname args` from message
  - Look up command in `IChannelRegistry` (fast in-memory, no DB hit)
  - Check permission level (broadcaster > mod > vip > sub > viewer)
  - Check global + user cooldowns via `ICooldownManager`
  - For `response` type: resolve template → send via `IChatProvider`
  - For `pipeline` type: invoke `IPipelineEngine.ExecuteAsync`
  - Publish `CommandExecutedEvent` or `CommandFailedEvent`
- [ ] Update `ChannelManagerService` to subscribe EventSub for each channel after join
- [ ] Register handler in DI
- [ ] Build + commit

## Task 3: PipelineEngine + Pipeline Models + Built-in Actions

**Files:**
- Create: `src/NomercyBot.Infrastructure/Pipeline/PipelineDefinition.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/PipelineContext.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/PipelineEngine.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Actions/ICommandAction.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Actions/SendMessageAction.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Actions/SendReplyAction.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Actions/TimeoutAction.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Actions/BanAction.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Actions/WaitAction.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Actions/SetVariableAction.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Actions/StopAction.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Conditions/ICommandCondition.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Conditions/UserRoleCondition.cs`
- Create: `src/NomercyBot.Infrastructure/Pipeline/Conditions/RandomCondition.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] Define `PipelineDefinition`, `PipelineStepDefinition`, `ConditionDefinition`, `ActionDefinition` records
- [ ] Define `PipelineContext` with mutable `Variables` dictionary
- [ ] Implement `PipelineEngine`: per-channel max 5 concurrent, 5-min timeout, sequential step execution, variable resolution, `ShouldStop` check
- [ ] Implement `ICommandAction` interface: `string ActionType`, `Task<ActionResult> ExecuteAsync(ActionContext ctx)`
- [ ] Implement 7 built-in actions
- [ ] Implement `ICommandCondition` interface + `UserRoleCondition`, `RandomCondition`
- [ ] Register all as keyed services, register `IPipelineEngine` as singleton
- [ ] Build + commit

## Task 4: TemplateResolver (90+ variables)

**Files:**
- Create: `src/NomercyBot.Infrastructure/Services/General/TemplateResolver.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] Implement `ITemplateResolver` with lazy async resolution of all variable groups:
  - `{user}`, `{user.id}`, `{user.name}`, `{user.color}`, `{user.role}`, `{user.followAge}`, `{user.accountAge}`, `{user.messageCount}`, `{user.pronouns}`
  - `{target}`, `{target.id}`, `{target.name}`, `{target.followAge}`
  - `{args}`, `{args.0}`, `{args.1}`, `{args.count}`
  - `{channel}`, `{channel.display}`, `{channel.id}`
  - `{streamer}`, `{botname}`
  - `{stream.title}`, `{stream.game}`, `{stream.uptime}`, `{stream.viewers}`, `{stream.isLive}`, `{stream.startedAt}`
  - `{time}`, `{time.utc}`, `{date}`
  - `{random.user}`, `{random.number.N}`, `{random.pick.a.b.c}`
  - `{count}` (post-counter action), `{previousTrack}`, `{currentTrack}`, `{currentTrack.artist}`, `{currentTrack.album}`
- [ ] Register as `ITemplateResolver` singleton
- [ ] Build + commit

## Task 5: TimerService

**Files:**
- Create: `src/NomercyBot.Application/Services/ITimerService.cs`
- Create: `src/NomercyBot.Infrastructure/BackgroundServices/TimerService.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] Define `ITimerService`: `AddTimer(channelId, timerId, config)`, `RemoveTimer`, `GetTimers`
- [ ] `TimerService : BackgroundService` loops every 60s, fires due timers per channel
- [ ] Timer config: interval (seconds), enabled, chatLines (min messages between fires), pipeline JSON
- [ ] On fire: invoke `IPipelineEngine` or send static message via `IChatProvider`
- [ ] Store timer state in `ChannelContext` (in-memory, persisted to DB on change)
- [ ] Register as hosted service + `ITimerService` singleton
- [ ] Build + commit

## Task 6: FairQueue + SpotifyMusicProvider + MusicService

**Files:**
- Create: `src/NomercyBot.Infrastructure/Services/General/FairQueue.cs`
- Create: `src/NomercyBot.Infrastructure/Services/Music/SpotifyMusicProvider.cs`
- Create: `src/NomercyBot.Infrastructure/Services/Music/MusicService.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] Implement `FairQueue<T> : IFairQueue<T>` per spec algorithm (rank = user's current count in queue, insert after all entries with rank ≤ this rank, recalculate on dequeue)
- [ ] Implement `SpotifyMusicProvider : IMusicProvider` via Spotify Web API:
  - `GetCurrentTrackAsync` → GET `/v1/me/player/currently-playing`
  - `PlayAsync`/`PauseAsync` → PUT `/v1/me/player/play|pause`
  - `SkipAsync` → POST `/v1/me/player/next`
  - `AddToQueueAsync` → POST `/v1/me/player/queue`
  - `SearchAsync` → GET `/v1/search`
  - Token loaded from `Service(Name="spotify", BroadcasterId=broadcasterId)`
- [ ] Implement `MusicService : IMusicService` that delegates to provider + manages in-memory queue per channel using `FairQueue<SongRequest>`
- [ ] Register `FairQueue<SongRequest>` (transient), `SpotifyMusicProvider` (scoped), `MusicService` (scoped)
- [ ] Build + commit

## Task 7: EdgeTtsProvider + TtsService

**Files:**
- Create: `src/NomercyBot.Infrastructure/Services/Tts/EdgeTtsProvider.cs`
- Create: `src/NomercyBot.Infrastructure/Services/Tts/TtsService.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] Implement `EdgeTtsProvider : ITtsProvider` using Microsoft Edge TTS via HTTP (free, no key):
  - POST to `https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1`
  - SSML payload with voice + text
  - Return `TtsSynthesisResult` with audio bytes and SHA256 content hash
  - `GetVoicesAsync` returns ~300 voices from the Edge TTS voices list endpoint
- [ ] Implement `TtsService : ITtsService` that selects provider based on voice prefix (`edge:`, `azure:`, `google:`) and delegates
- [ ] Register `EdgeTtsProvider` as `ITtsProvider` keyed service, `TtsService` as `ITtsService`
- [ ] Build + commit

## Task 8: AutoModerationHandler

**Files:**
- Create: `src/NomercyBot.Infrastructure/EventHandlers/AutoModerationHandler.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] `AutoModerationHandler : IEventHandler<ChatMessageReceivedEvent>`
  - Skip moderators, VIPs, broadcasters
  - Load rules for channel from `IModerationService` (cached 5 min in ChannelContext)
  - Rule types: `caps_filter` (% caps > threshold), `link_filter` (URL regex), `banned_phrase` (contains match)
  - On rule match: execute action (timeout/ban/delete) via `IChatProvider`
  - Log action via `IModerationService.RecordActionAsync`
- [ ] Register handler
- [ ] Build + commit

## Task 9: TrustScoreCalculator + TrustService

**Files:**
- Create: `src/NomercyBot.Infrastructure/Services/Trust/TrustScoreCalculator.cs`
- Create: `src/NomercyBot.Infrastructure/Services/Trust/TrustService.cs`
- Create: `src/NomercyBot.Application/Services/ITrustService.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] Implement `TrustScoreCalculator` with exact Bamo's algorithm (exponential decay, followage penalty, reputation boost, violation penalties)
- [ ] Implement `TrustService : ITrustService` that gathers `TrustContext` from DB + cache, calls calculator, caches result 5 min in ChannelContext
- [ ] `GetTrustTier(score)` → Untrusted/Low/Standard/Trusted enum
- [ ] Register `TrustService` as scoped `ITrustService`
- [ ] Build + commit

## Task 10: SignalR Hub Real Broadcasting

**Files:**
- Create: `src/NomercyBot.Infrastructure/EventHandlers/DashboardBroadcastHandler.cs`
- Modify: `src/NomercyBot.Api/Hubs/DashboardNotifier.cs`
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`

- [ ] `DashboardBroadcastHandler` handles: `ChatMessageReceivedEvent`, `ChannelOnlineEvent`, `ChannelOfflineEvent`, `RewardRedeemedEvent`, `CommandExecutedEvent`, `TrackChangedEvent`
- [ ] On each event: calls `IRealTimeNotifier` which uses `IHubContext<DashboardHub, IDashboardClient>`
- [ ] Implement real methods on `DashboardNotifier` for each event type
- [ ] Register handler + `DashboardNotifier` properly
- [ ] Build + commit

## Task 11: Polly Resilience + Background Service Improvements

**Files:**
- Modify: `src/NomercyBot.Infrastructure/DependencyInjection.cs`
- Modify: `src/NomercyBot.Infrastructure/BackgroundServices/TokenRefreshService.cs`
- Modify: `src/NomercyBot.Infrastructure/BackgroundServices/ChannelManagerService.cs`

- [ ] Add Polly `Microsoft.Extensions.Http.Resilience` to `twitch-helix` client: retry 3x with exponential backoff, circuit breaker after 5 failures/30s
- [ ] Add Polly retry to `twitch-auth` and `spotify` clients
- [ ] Implement `TokenRefreshService` properly: check tokens expiring within 5 min, refresh them proactively
- [ ] Improve `ChannelManagerService`: subscribe EventSub events after joining channels, populate `IChannelRegistry`
- [ ] Build, fix errors, commit, push
