# NomNomzBot — Server

Multi-tenant Twitch bot platform. Self-hosted, AGPL-3.0. One instance, unlimited channels.

> **This is a submodule of [nomnomzbot](https://github.com/NoMercyLabs/nomnomzbot). Clone the parent repo with `--recursive` for the full platform.**

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
git clone --recursive git@github.com:NoMercyLabs/nomnomzbot.git
cd nomnomzbot/nomnomzbot-server
cp .env.example .env
# Edit .env: set POSTGRES_PASSWORD, JWT_SECRET, ENCRYPTION_KEY, TWITCH_CLIENT_ID, TWITCH_CLIENT_SECRET
docker compose up -d
```

The API starts on `http://localhost:5080`. Database migrations and seed data run automatically on first boot.

Health check: `curl http://localhost:5080/health`

## Development Setup (without Docker)

```bash
# 1. Start dependencies only
docker compose up -d postgres redis

# 2. Copy and configure environment
cp .env.example .env
# Edit .env — set ConnectionStrings__DefaultConnection, Jwt__Secret, Twitch__* vars

# 3. Run the API
dotnet run --project src/NomNomzBot.Api
```

The app auto-migrates and seeds on startup. No manual `dotnet ef` commands needed.

### Run Tests

```bash
dotnet test
```

## Project Structure

```
src/
  NomNomzBot.Domain/          # Entities, interfaces, domain events (zero dependencies)
  NomNomzBot.Application/     # Service interfaces, DTOs, FluentValidation, pipeline logic
  NomNomzBot.Infrastructure/  # EF Core, Twitch/Spotify/TTS clients, background services
  NomNomzBot.Api/             # Controllers, SignalR hubs, middleware, health checks
tests/
  NomNomzBot.*.Tests/         # Unit and integration tests per layer
```

Dependency direction: `Api → Infrastructure → Application → Domain`

## API

- Base URL: `http://localhost:5080/api/v1`
- Interactive API docs (Scalar): `http://localhost:5080/scalar`
- SignalR hubs: `/hubs/dashboard`, `/hubs/overlay`, `/hubs/obs`, `/hubs/admin`

Authentication uses JWT Bearer tokens obtained via Twitch OAuth (`/api/v1/auth/login`).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

To add a new pipeline action:
1. Implement `ICommandAction` in `NomNomzBot.Infrastructure/Pipeline/Actions/`
2. Register it as `Transient<ICommandAction>` in `DependencyInjection.cs`
3. Add a corresponding condition in `Pipeline/Conditions/` if needed

## License

[AGPL-3.0](LICENSE). You may self-host freely. Modifications to the server-side code must be made available under the same license.
