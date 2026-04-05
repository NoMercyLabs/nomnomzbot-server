# Feasibility Analysis: Old Bot Feature Parity via Dashboard Configuration

**Date:** 2026-04-05
**Scope:** Can the old NoMercyBot (`nomercy-bot`) behavior be recreated entirely through client-side dashboard configuration of the new NomercyBot platform?
**Verdict:** ~15% functional parity today. Read on for the full breakdown.

---

## Preliminary Notes

1. **No frontend exists.** The new bot is API-only. CORS is configured for `localhost:3000` / `localhost:5173` suggesting a separate frontend repo, but none was found. Every "dashboard config" answer below assumes a future frontend — even so, many areas have no backing API to call.

2. **`ITwitchApiService` is a stub.** Any feature that requires the bot to perform an action on Twitch (ban, timeout, shoutout, mod actions, channel point sync) logs the intent and returns `true` without making an HTTP call.

3. **Event handlers are mostly absent.** 13 of 14 EventSub subscriptions publish domain events that no handler consumes. The event fires, logs, and disappears.

---

## Feature Parity Matrix

### 1. Event Responses — `NOT SUPPORTED`

Configurable chat message / TTS / widget triggers per Twitch event (follow, sub, cheer, raid, ban).

| Layer | Status |
|-------|--------|
| Follow handler | Event published, zero handlers |
| Sub / resub / gift sub handler | Event published, zero handlers |
| Cheer handler | Event published, zero handlers |
| Raid handler | Event published, zero handlers |
| Ban / unban handler | Event not even published (logged only) |
| User-configurable "when X → do Y" rules | No DB model, no service, no API |
| Per-event chat message templates | Does not exist |
| Per-event TTS trigger | Does not exist |
| Per-event widget/overlay alert | Does not exist |

Old bot had: snarky cheer templates, multi-voice TTS per cheer, auto chat welcome on follow/raid/sub, per-event widget alerts.
New bot has: none of the above.

---

### 2. TTS — `PARTIALLY SUPPORTED`

| Layer | Status |
|-------|--------|
| Edge TTS engine (WebSocket, no API key) | ✅ Real, 15 voices |
| Azure TTS / ElevenLabs providers | ✅ Wired in DI |
| Word filters | ❌ Not implemented |
| Per-event TTS config | ❌ No event handlers call TTS |
| TTS from pipeline actions | ❌ No TTS action type in PipelineEngine |
| API to configure TTS (voice, volume, filters) | ❌ No TtsController |

The engine is production-ready. Nothing invokes it. No configuration surface exists.

---

### 3. Chat Commands — `SUPPORTED`

| Layer | Status |
|-------|--------|
| Custom command CRUD | ✅ Full REST API |
| Cooldowns (global + per-user) | ✅ |
| Permission levels (broadcaster / mod / vip / sub / viewer) | ✅ |
| Aliases | ✅ |
| Multiple responses (round-robin) | ✅ |
| Pipeline commands (multi-step JSON) | ✅ PipelineEngine with 8 actions, 2 conditions |
| Template variables (`{user}`, `{args}`, etc.) | ✅ |
| In-memory cache (no DB hit per message) | ✅ |
| Frontend UI | ❌ No frontend exists yet |

This is the most complete feature area. A frontend over the existing REST API would give full parity with the old bot's command system.

---

### 4. Timers — `PARTIALLY SUPPORTED`

| Layer | Status |
|-------|--------|
| Timer execution engine (interval, activity gate, round-robin) | ✅ `TimerService` fully implemented |
| `MinChatActivity` gating | ✅ |
| Multiple messages cycling | ✅ |
| State persistence (`LastFiredAt`, `NextMessageIndex`) | ✅ |
| REST API for timer CRUD | ❌ No `TimersController` exists |
| Frontend UI | ❌ No frontend exists yet |

The execution engine is solid. You cannot create or edit timers without direct database access.

---

### 5. Auto-Moderation — `PARTIALLY SUPPORTED`

| Layer | Status |
|-------|--------|
| Caps filter (configurable threshold) | ✅ |
| Link filter | ✅ |
| Banned phrases | ✅ |
| Emote spam filter | ❌ Not implemented |
| Per-channel rules with exempt roles | ✅ |
| REST API for rule CRUD | ✅ Full endpoints |
| Twitch enforcement (timeout / ban execution) | ❌ `ITwitchApiService` is stubbed — no actual Twitch call |
| Frontend UI | ❌ No frontend exists yet |

Rules are evaluated correctly on every message. When a rule fires, the action is recorded in the DB and reported as successful, but the user is never actually timed out or banned on Twitch.

---

### 6. Channel Points — `NOT SUPPORTED`

| Layer | Status |
|-------|--------|
| Reward CRUD (local DB) | ✅ |
| Sync rewards with Twitch | ❌ Returns `503 SERVICE_UNAVAILABLE` |
| Redemption event captured | ✅ `RewardRedeemedEvent` published |
| Fulfillment logic on redemption | ❌ No handler — event fires into the void |
| Redemption status update (fulfilled / rejected) | ❌ Not subscribed |
| Reward lifecycle events (add / update / remove) | ❌ Not subscribed |
| API to configure fulfillment actions | ❌ Does not exist |

Rewards exist in the local database. They never sync to Twitch. Redemptions are captured but nothing happens.

---

### 7. Music / Song Requests — `PARTIALLY SUPPORTED`

| Layer | Status |
|-------|--------|
| Spotify API (play, pause, skip, search, queue, volume) | ✅ Fully implemented |
| Token refresh / OAuth | ✅ |
| YouTube Music | ❌ Stubbed |
| Fair queue with trust-level enforcement | ✅ Implemented |
| `!songrequest` / `!skip` commands wired to `MusicService` | ❌ No command handler calls `MusicService` |
| REST API for music configuration | ❌ No `MusicController` |
| Frontend UI | ❌ No frontend exists yet |

Spotify is a working library. There is no command glue, no chat integration, and no way to enable song requests per channel from any UI.

---

### 8. Overlays / Widgets — `PARTIALLY SUPPORTED`

| Layer | Status |
|-------|--------|
| SignalR infrastructure (`OverlayHub`, `DashboardHub`) | ✅ Real, authenticated |
| Chat messages broadcast to overlays | ✅ Via `ChatMessageBroadcastHandler` |
| Follow / sub / cheer / raid alert overlays | ❌ No handlers to broadcast these events |
| Widget CRUD (config in DB) | ✅ REST API exists |
| OBS relay hub | ✅ `OBSRelayHub` exists |
| Stream status tracking in dashboard | ✅ `DashboardHub` tracks live/title/game |
| Widget visual templates / alert CSS | ❌ No frontend template system |

Overlays can display chat. Alert overlays for monetization and engagement events require event handlers that don't exist.

---

### 9. Shoutouts — `NOT SUPPORTED`

| Layer | Status |
|-------|--------|
| Auto-shoutout on raid | ❌ `RaidEvent` has no handler |
| Manual shoutout via API | ❌ `TwitchApiService.ShoutoutAsync()` logs only, no HTTP call |
| `channel.shoutout.create/receive` subscription | Subscribed, not published as domain event |
| Shoutout queue with cooldown (old bot feature) | ❌ Not implemented |

The `ShoutoutSentEvent` domain event and `ITwitchApiService.ShoutoutAsync()` signature exist as hollow scaffolding.

---

### 10. Watch Streaks — `NOT SUPPORTED`

| Layer | Status |
|-------|--------|
| IRC `USERNOTICE viewermilestone` parsing | ❌ Not in `TwitchIrcService` |
| `WatchStreak` entity | ❌ No DB model |
| Watch streak TTS | ❌ |
| Watch streak widget event | ❌ |
| Any mention in codebase | ❌ Zero |

Completely absent. Not started.

---

### 11. Polls / Predictions / Hype Trains — `NOT SUPPORTED`

| Layer | Status |
|-------|--------|
| EventSub subscriptions | ❌ None of these 9 event types subscribed |
| DB entities | ❌ No `Poll`, `Prediction`, or `HypeTrain` entities |
| REST API endpoints | ❌ None |
| Chat results posting (old bot posted poll results) | ❌ |

Three full feature categories with zero presence in the codebase.

---

### 12. Multi-Channel Management — `SUPPORTED`

| Layer | Status |
|-------|--------|
| Channel CRUD | ✅ Full REST API |
| Bot join / leave per channel | ✅ |
| EventSub subscriptions per channel at join | ✅ `ChannelManagerService` |
| Per-channel tenant isolation | ✅ All entities scoped by `BroadcasterId` |
| Feature flags per channel | ✅ `FeaturesController` |
| In-memory per-channel context | ✅ `ChannelRegistry` |
| Frontend UI | ❌ No frontend exists yet |

The architecture is solid. Fully usable once a frontend exists.

---

### 13. User Management / Mod Actions from Dashboard — `PARTIALLY SUPPORTED`

| Layer | Status |
|-------|--------|
| User search + profiles | ✅ |
| Trust score model | ✅ |
| GDPR data export / erasure | ✅ |
| Mod action log / history | ✅ |
| Dashboard timeout / ban / unban (DB recording) | ✅ |
| **Actual Twitch execution** of timeout / ban | ❌ `ITwitchApiService` is stubbed |
| Frontend UI | ❌ No frontend exists yet |

Actions are stored and audited. Nobody is actually banned on Twitch.

---

### 14. Stream Lifecycle — `NOT SUPPORTED`

| Layer | Status |
|-------|--------|
| `stream.online` / `stream.offline` subscribed | ✅ |
| Domain events published | ✅ `ChannelOnlineEvent`, `ChannelOfflineEvent` |
| Handler that creates stream DB record | ❌ |
| `IsLive` flag updated | ❌ |
| Timer / shoutout queue reset on go-live | ❌ |
| User-configurable online / offline triggers | ❌ No API, no model |

Events fire. Nothing listens.

---

## Summary

| Feature Area | Classification |
|---|---|
| Chat Commands | **SUPPORTED** |
| Multi-Channel Management | **SUPPORTED** |
| Auto-Moderation | **PARTIALLY SUPPORTED** |
| Timers | **PARTIALLY SUPPORTED** |
| TTS | **PARTIALLY SUPPORTED** |
| Music / Song Requests | **PARTIALLY SUPPORTED** |
| Overlays / Widgets | **PARTIALLY SUPPORTED** |
| User Management / Mod Actions | **PARTIALLY SUPPORTED** |
| Event Responses (follow/sub/cheer/raid) | **NOT SUPPORTED** |
| Channel Points | **NOT SUPPORTED** |
| Shoutouts | **NOT SUPPORTED** |
| Stream Lifecycle | **NOT SUPPORTED** |
| Polls / Predictions / Hype Trains | **NOT SUPPORTED** |
| Watch Streaks | **NOT SUPPORTED** |

---

## Overall Feature Parity Estimate

**~15% of old bot functionality can be recreated today via dashboard configuration.**

That 15% is almost entirely the command system and multi-channel management — both of which are architecturally complete but still lack a frontend.

### Why the number is so low

| Root cause | Features affected |
|---|---|
| **No frontend** | Every feature requires raw REST calls |
| **Event handlers absent** | Follow, sub, cheer, raid, stream lifecycle, channel points |
| **`ITwitchApiService` is a stub** | Bans, timeouts, shoutouts, mod actions, auto-mod enforcement |
| **No event→response mapping** | No "when X, do Y" system exists (no DB model, no API) |
| **Missing subscriptions** | Polls, predictions, hype trains, ad break, mod lifecycle |
| **No chat→service glue** | TTS and Spotify are working libraries disconnected from everything |
| **Completely absent** | Watch streaks, polls, predictions, hype trains |

### What needs to be built to reach 80%+ parity

1. **Unblock `ITwitchApiService`** — implement real HTTP calls for timeout, ban, shoutout, mod actions
2. **Event response system** — DB model + API for "event type → action list" (chat message, TTS, widget alert)
3. **Event handlers** — implement `IEventHandler<T>` for follow, sub, cheer, raid, stream online/offline
4. **Timer CRUD API** — add `TimersController` (the engine is ready)
5. **TTS pipeline action** — add a `tts` action type to `PipelineEngine`
6. **Music command glue** — wire `!sr` / `!skip` / `!queue` commands to `MusicService`
7. **EventSub subscriptions** — add polls, predictions, hype train, ad break, mod lifecycle
8. **Watch streak IRC parsing** — add `USERNOTICE viewermilestone` handling to `TwitchIrcService`
9. **Frontend** — any UI at all
