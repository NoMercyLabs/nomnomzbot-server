# NomercyBot

Multi-tenant Twitch bot platform. Self-hosted, AGPL-3.0. One instance, unlimited channels.

## Features

- **Chat Commands** — Pipeline-based with cooldowns, permissions, and chained actions
- **Moderation** — Auto-mod rules, spam detection, banned words, link protection
- **TTS** — Edge TTS (free, no key), Azure/ElevenLabs (BYOK), per-user voice settings
- **Music** — Song requests, fair-queue trust scoring, Spotify + YouTube support
- **Channel Points** — Custom reward handlers with pipeline actions
- **Overlays** — Real-time OBS widgets via SignalR WebSocket
- **Multi-Channel** — Tenant isolation, one bot process serves unlimited streamers

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 |
| API | ASP.NET Core, SignalR |
| ORM | EF Core 10 + Npgsql |
| Database | PostgreSQL 16 |
| Cache | Redis 7 |
| Auth | JWT + Twitch OAuth |
| Migrations | EF Core migrations (auto-applied at startup) |

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose v2
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for local development)
- A [Twitch Developer Console](https://dev.twitch.tv/console) application (Client ID + Secret)

## Quick Start (Docker)

```bash
git clone https://github.com/NoMercyLabs/nomercybot.git
cd nomercybot
cp .env.example .env
# Edit .env: set POSTGRES_PASSWORD, JWT_SECRET, ENCRYPTION_KEY, TWITCH_CLIENT_ID, TWITCH_CLIENT_SECRET
docker compose up -d
```

The API starts on `http://localhost:5000`. Database migrations and seed data run automatically on first boot.

Health check: `curl http://localhost:5000/health`

## Development Setup (without Docker)

```bash
# 1. Start dependencies only
docker compose up -d postgres redis

# 2. Copy and configure environment
cp .env.example .env
# Edit .env — set ConnectionStrings__DefaultConnection, Jwt__Secret, Twitch__* vars

# 3. Run the API
dotnet run --project src/NomercyBot.Api
```

The app auto-migrates and seeds on startup. No manual `dotnet ef` commands needed.

### Run Tests

```bash
dotnet test
```

## Project Structure

```
src/
  NomercyBot.Domain/          # Entities, interfaces, domain events (zero dependencies)
  NomercyBot.Application/     # Service interfaces, DTOs, FluentValidation, pipeline logic
  NomercyBot.Infrastructure/  # EF Core, Twitch/Spotify/TTS clients, background services
  NomercyBot.Api/             # Controllers, SignalR hubs, middleware, health checks
tests/
  NomercyBot.*.Tests/         # Unit and integration tests per layer
```

Dependency direction: `Api → Infrastructure → Application → Domain`

## API

- Base URL: `http://localhost:5000/api/v1`
- OpenAPI (Swagger): `http://localhost:5000/openapi/v1.json` (development only)
- SignalR hubs: `/hubs/dashboard`, `/hubs/overlay`, `/hubs/obs`, `/hubs/admin`

Authentication uses JWT Bearer tokens obtained via Twitch OAuth (`/api/v1/auth/login`).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

To add a new pipeline action:
1. Implement `ICommandAction` in `NomercyBot.Infrastructure/Pipeline/Actions/`
2. Register it as `Transient<ICommandAction>` in `DependencyInjection.cs`
3. Add a corresponding condition in `Pipeline/Conditions/` if needed

## License

[AGPL-3.0](LICENSE). You may self-host freely. Modifications to the server-side code must be made available under the same license.
