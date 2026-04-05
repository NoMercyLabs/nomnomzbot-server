# NomercyBot

Open-source multi-channel Twitch bot platform. Self-hosted, multi-tenant, built for streamers who want full control.

## Features

- **Chat Commands** - Custom commands with pipeline actions, cooldowns, permissions
- **Moderation** - Auto-mod rules, banned words, link protection, timeout/ban management
- **TTS** - Text-to-speech with Edge TTS (free), Azure/Google/ElevenLabs (BYOK)
- **Music** - Song requests with fair queue, trust scoring, 14 provider support
- **Rewards** - Channel point reward management with pipeline handlers
- **Widgets** - Customizable OBS overlays with real-time SignalR updates
- **Multi-Channel** - One instance serves unlimited channels with tenant isolation

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10, ASP.NET Core, EF Core |
| Database | PostgreSQL 17 |
| Cache | Redis 7 |
| Real-time | SignalR |
| Frontend | Expo (React Native) - web, iOS, Android |

## Quick Start

```bash
git clone https://github.com/NoMercyLabs/nomercybot.git
cd nomercybot
cp .env.example .env
docker compose up -d postgres redis
dotnet run --project src/NomercyBot.Api
```

## Architecture

Clean Architecture with strict dependency direction:

```
API -> Infrastructure -> Application -> Domain
```

- **Domain** - Entities, interfaces, events (zero dependencies)
- **Application** - Service interfaces, DTOs, validation
- **Infrastructure** - EF Core, external APIs, background services
- **API** - Controllers, SignalR hubs, middleware

## License

AGPL-3.0. Self-hosted instances have no restrictions.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
