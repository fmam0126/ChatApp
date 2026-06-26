# ChatApp

A real-time chat application built with .NET 10, featuring three client frontends (Blazor WebAssembly, Avalonia desktop, and a terminal CLI) backed by an ASP.NET Core server with SignalR for live messaging and PostgreSQL for persistence.

## Architecture

```
                  +------------------+
                  |     Traefik      |  reverse proxy / TLS termination
                  +--------+---------+
                           |
            +--------------+--------------+
            |                             |
   +--------v---------+         +--------v---------+
   |  Blazor WASM     |         |  ASP.NET Core    |
   |  (nginx)         |         |  Server          |
   |  port 80 (int)   |         |  port 8080 (int) |
   +------------------+         +--------+---------+
            |                             |
            |   SignalR + REST            |   EF Core
            |   /chathub                  |   /auth
            |   /auth/login               |   /ChatMessages
            |   /ChatMessages             |   /metrics
            |                             |
   +--------v---------+         +--------v---------+
   |  Avalonia        |         |   PostgreSQL 18  |
   |  Desktop Client  |         |                  |
   +------------------+         +------------------+
            |
   +--------v---------+
   |  CLI Client      |
   |  (Spectre)       |
   +------------------+
```

## Project Structure

```
ChatApp/
+-- ChatApp.server/            ASP.NET Core 10 server
|   +-- Controllers/           REST API endpoints
|   +-- Hubs/                  SignalR ChatHub
|   +-- Models/                EF Core entities (User, ChatMessage, ChatContext)
|   +-- DTO/                   Request/response DTOs
|   +-- Class/                 TokenService, ConnectedUsersService, ChatMetrics
|   +-- Interfaces/            IChatContext abstraction
|   +-- Program.cs             Application entry point and middleware pipeline
+-- ChatApp.Blazor/            Blazor WebAssembly client
|   +-- Pages/                 Desktop page, NotFound page
|   +-- Components/            DesktopWindow, DesktopIcon, Taskbar, ChatWindowContent
|   +-- Models/                WindowState
|   +-- Services/              WindowManager
|   +-- wwwroot/               Static assets and Win95 CSS theme
+-- ChatApp.Avalonia/          Avalonia desktop client
|   +-- Views/                 MainWindow (login and chat panels)
|   +-- ViewModels/            MainViewModel, MessageViewModel
|   +-- Models/                ChatMessage, Settings
|   +-- Classes/               AuthService, ChatClient, DevSslBypass
+-- ChatApp.CliClient/         Terminal chat client
|   +-- Classes/               ChatConsole, AuthService, ChatClient, SpectreDisplay
|   +-- Models/                ChatMessage, Settings
|   +-- Interfaces/            IChatClient
+-- ChatApp.Server.Tests/      xUnit integration and unit tests
+-- docker-compose.yml         Multi-service orchestration
+-- traefik/                   Traefik reverse proxy configuration
+-- prometheus/                Prometheus scrape configuration
+-- grafana/                   Grafana dashboards and datasources
+-- .github/workflows/         CI/CD (empty, not yet configured)
```

## Server

The server is an **ASP.NET Core 10** application providing a REST API and SignalR hub for real-time chat.

### API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/auth/login` | None | Authenticate by username. Returns a JWT. |
| GET | `/ChatMessages?count=N` | JWT | Retrieve the N most recent messages (1-200, default 50). |
| POST | `/ChatMessages` | JWT | Create a message via REST (unused in normal flow; messages go through SignalR). |

### SignalR Hub

| Hub | Path | Auth | Description |
|-----|------|------|-------------|
| ChatHub | `/chathub` | JWT | Real-time chat. Clients invoke `SendMessage(string)`. Server broadcasts `ReceiveMessage(string username, string message)`. |

The server tracks connected users in memory via `ConnectedUsersService` and broadcasts system messages on join/leave. A `JwtExpirationFilter` checks token expiry on every hub invocation and aborts expired connections.

### Authentication

- Username-only login. No password required. Any username (3-30 characters) not currently connected can join.
- JWT tokens are HMAC-SHA256 signed, valid for 60 minutes by default.
- JWTs are passed via `Authorization: Bearer` header for REST calls and via `?access_token=` query parameter for SignalR WebSocket connections.

### Data Model

- **User**: `Id` (int, PK), `Name` (string, required)
- **ChatMessage**: `Id` (int, PK), `Content` (string), `Created` (DateTime), `SenderId` (int, FK to User)

EF Core with PostgreSQL. The database is created on startup via `EnsureCreated()` (no formal migrations).

### Rate Limiting

- Global: 10 requests per second per user/host, fixed window.
- Login endpoint: 4 requests per minute (`LoginPolicy`).

### Observability

OpenTelemetry with console exporters and a Prometheus scraping endpoint at `/metrics`. Custom metrics track messages sent, system messages, connections, disconnections, and active connection count.

## Clients

All three clients share the same communication pattern: REST for authentication and message history, SignalR for real-time messaging. Each implements its own username-input login flow and message display.

### Blazor WebAssembly Client

A **Win95-themed desktop simulation** running entirely in the browser.

- The UI models a classic Windows desktop with a teal background, desktop icons, draggable windows, and a taskbar with a live clock.
- Double-click the "Chat Client" icon to open a chat window.
- Each chat window manages its own isolated SignalR connection and auth token, enabling multi-user testing in separate windows.
- Uses the `BlazorWinOld` NuGet package for Win98-styled form controls.
- Custom CSS and JavaScript for window dragging, scroll-to-bottom, and the retro aesthetic.
- Served via nginx in production (Dockerfile with aggressive WASM asset caching and SPA fallback).

### Avalonia Desktop Client

A **cross-platform desktop application** using the Classic Windows theme.

- Single-window design: toggles between a login panel and a chat panel via visibility bindings.
- MVVM pattern with `MainViewModel` as the central state holder.
- Messages are displayed in a ListBox with per-user deterministic color assignment.
- Supports Enter-to-send and auto-scroll to newest message.
- Uses `DevSslBypass` in debug builds for local development with self-signed certificates.
- No dependency injection container -- services are instantiated directly.

### CLI Client

A **terminal-based chat client** using Spectre.Console and bespoke cursor control.

- Split-screen TUI: scrollable message area, fixed instruction line, bottom-anchored input line.
- Inline editing with arrow keys, Home/End, Backspace/Delete support.
- Welcome screen displayed via Spectre.Console's `FigletText`.
- Username prompt with validation, followed by JWT login and SignalR connection.
- Sends messages on Enter, exits on `/exit`.
- User colors are deterministically assigned from an 8-color palette via username hash.

## Infrastructure

### Docker Compose

Seven services orchestrated on a shared `chatapp-network`:

| Service | Image | Purpose |
|---------|-------|---------|
| `traefik` | `traefik:v3` | Reverse proxy, TLS termination, routing |
| `chatapp-blazor` | Built from `ChatApp.Blazor/Dockerfile` | Blazor WASM served via nginx |
| `chatapp-server` | Built from `ChatApp.server/Dockerfile` | ASP.NET Core API and SignalR hub |
| `postgres` | `postgres:18` | PostgreSQL database |
| `pgadmin` | `dpage/pgadmin4` | Database admin UI (port 5454) |
| `prometheus` | `prom/prometheus:latest` | Metrics collection |
| `grafana` | `grafana/grafana:latest` | Metrics dashboards (port 3000) |

Traefik routes requests based on path prefixes. The API server routes (`/auth`, `/ChatMessages`, `/chathub`, `/chatHub`, `/metrics`) take priority over the Blazor SPA catch-all.

### Monitoring

- **Prometheus** scrapes `/metrics` from the server every 15 seconds.
- **Grafana** (default credentials admin/admin) includes a pre-provisioned ChatApp dashboard with panels for active connections, message rate, request duration histograms, error rates, and top endpoints.

### Environment Variables

Configuration is managed through `.env` (local, not committed to a public repo) and `.env.example` (template). Key variables:

| Variable | Description |
|----------|-------------|
| `POSTGRES_DB` | Database name |
| `POSTGRES_USER` | Database user |
| `POSTGRES_PASSWORD` | Database password |
| `PGADMIN_DEFAULT_EMAIL` | pgAdmin login email |
| `PGADMIN_DEFAULT_PASSWORD` | pgAdmin login password |
| `DISABLE_HTTPS_REDIRECTION` | Set to `true` when behind Traefik |
| `ASPNETCORE_URLS` | Server listen address |
| `GRAFANA_ADMIN_USER` | Grafana admin username |
| `GRAFANA_ADMIN_PASSWORD` | Grafana admin password |

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker and Docker Compose
- PostgreSQL 18 (if running outside Docker)

### Running with Docker Compose

```bash
# Copy and configure environment
cp .env.example .env
# Edit .env with your preferred credentials

# Start all services
docker compose up -d
```

This starts the full stack: Traefik, server, Blazor client, PostgreSQL, pgAdmin, Prometheus, and Grafana.

### Running Locally (Development)

```bash
# Start the server
cd ChatApp.server
dotnet run

# In another terminal, start the Blazor client
cd ChatApp.Blazor
dotnet run

# Or start the Avalonia desktop client
cd ChatApp.Avalonia
dotnet run

# Or start the CLI client
cd ChatApp.CliClient
dotnet run
```

### Running Tests

```bash
dotnet test
```

## Testing

The test project (`ChatApp.Server.Tests`) uses **xUnit** with approximately 37 tests across six test files:

| Test Class | Type | What It Covers |
|------------|------|----------------|
| `AuthControllerTests` | Integration | Login validation, duplicate usernames, idempotency |
| `AuthControllerRateLimitTests` | Integration | Rate limit enforcement and reset behavior |
| `ChatMessagesControllerTests` | Integration | Authorized and unauthorized message access, message creation |
| `ChatHubTests` | Unit (Moq) | Connection lifecycle, message persistence, duplicate rejection |
| `ConnectedUsersServiceTests` | Unit | Concurrent user tracking, add/remove/get operations |
| `TokenServiceTests` | Unit | JWT generation, claim correctness, expiration |

Integration tests use `WebApplicationFactory<T>` with **SQLite in-memory** replacing PostgreSQL and configurable rate limiting (high limits for functional tests, production limits for rate limit tests).

## Configuration

Server configuration is in `ChatApp.server/appsettings.json`:

- **JwtSettings**: Secret key, issuer, audience, expiration (60 minutes).
- **ConnectionStrings.DefaultConnection**: PostgreSQL connection string.
- **Rate Limiting**: Global fixed window (10 req/s) and LoginPolicy (4 req/min).

Client configuration is in each client's `appsettings.json`, specifying the server URL (defaults to `https://localhost`).

