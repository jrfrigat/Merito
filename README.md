# Merito

**Points for chores, homework and good habits - a family app where kids earn points and spend them in
a family shop.** [Русская версия](README.ru.md)

[![CI](https://github.com/jrfrigat/Merito/actions/workflows/ci.yml/badge.svg)](https://github.com/jrfrigat/Merito/actions/workflows/ci.yml)
[![Docker Pulls](https://img.shields.io/docker/pulls/frigat/merito?logo=docker&label=Docker%20pulls)](https://hub.docker.com/r/frigat/merito)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WebAssembly%20PWA-5C2D91)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Merito (Latin for "deservedly") turns a family's rules into a simple loop: a child reports what they
did, a parent confirms it and awards points, and the child spends the points on rewards the family
agreed on - screen time, a trip, a treat. The user interface is in Russian.

## Features

**For parents**
- Create a family and fill it in minutes: tasks (daily and extra), penalties and a reward shop - or
  start from a ready example catalog with 21 tasks, 7 penalties and a screen-time shop.
- Add children as login-and-password accounts (no email needed) or invite anyone with a code - a second
  parent or a child who already has an account.
- Review what children reported: approve with the suggested points or your own, reject with a reason,
  and save a new kind of work into the task list in the same step.
- Grant or deduct points by hand - a comment is always required - or apply a penalty from the catalog.
- Hand over bought rewards or cancel a purchase with a refund.
- See every child's balance and the full history of points.

**For children**
- One tap from the home screen: "I did it" or "Shop".
- Pick a task from the list or describe something that is not in it.
- Buy rewards when the balance covers the price; see pending reviews and the own history.

**Everywhere**
- Mobile first: a bottom navigation bar on phones, a side menu on wide screens.
- Installable PWA with light and dark mode.

## Built with

- **Client:** Blazor WebAssembly PWA on [Flare](https://github.com/jrfrigat/Flare) with the
  Material 3 Expressive theme.
- **Server:** ASP.NET Core minimal APIs, ASP.NET Core Identity (bearer tokens), EF Core with
  PostgreSQL. The server also hosts the WebAssembly client, so the app and its API share one origin.
- **Tests:** xUnit v3 - domain services on SQLite in memory and the real HTTP pipeline through
  `WebApplicationFactory`, including access checks for strangers and children.

## Run with Docker

Requirements: Docker with Compose v2.

```bash
cp .env.example .env              # set POSTGRES_PASSWORD
docker network create proxy_network   # once; skip if it already exists
docker compose up -d --build
```

The stack has two containers: `merito` (the app, listening on port 8080 inside the container) and
`merito-db` (PostgreSQL 16 with the `merito-pgdata` volume). Database migrations run when the app starts.

`merito` joins the external `proxy_network` and publishes no ports: point your reverse proxy (for
example Nginx Proxy Manager on the same network) at `http://merito:8080`. To open it directly on this
machine instead, add a local `docker-compose.override.yml` (ignored by git):

```yaml
services:
  merito:
    ports:
      - "5080:8080"
```

and open http://localhost:5080. On Windows, `run-docker-compose.bat` rebuilds and restarts the stack.

The first account you register is a parent account; create the family, then add children from the
"Семья" (Family) page.

### Configuration

| Variable | Default | Purpose |
| --- | --- | --- |
| `POSTGRES_PASSWORD` | - (required) | Database password used by both containers |
| `POSTGRES_DB` | `merito` | Database name |
| `POSTGRES_USER` | `merito` | Database user |

Serve the app over HTTPS in production (the reverse proxy is the natural place): sign-in tokens travel
in request headers, and browsers install PWAs only from secure origins.

### Prebuilt image

Every version tag (`v1.2.3`) publishes the image to Docker Hub as `frigat/merito` and to GitHub
Container Registry as `ghcr.io/jrfrigat/merito`, tagged `X.Y.Z` and `latest`. To run a release
instead of building from source, replace the `build:` section of the `merito` service with:

```yaml
    image: frigat/merito:latest   # or a pinned X.Y.Z tag
```

## Development

Requirements: .NET SDK 10.0.400 or newer.

```bash
# a throwaway database for local runs
docker run -d --name merito-dev-db -e POSTGRES_USER=merito -e POSTGRES_PASSWORD=merito \
  -e POSTGRES_DB=merito -p 127.0.0.1:5433:5432 postgres:16-alpine

dotnet user-secrets set ConnectionStrings:Merito \
  "Host=localhost;Port=5433;Database=merito;Username=merito;Password=merito" --project src/Merito.Server

dotnet run --project src/Merito.Server   # http://localhost:5080
dotnet test --solution Merito.slnx
```

Schema changes go through EF Core migrations:

```bash
dotnet ef migrations add <Name> --project src/Merito.Server --output-dir Data/Migrations
```

## Project layout

```
src/
  Merito.Shared/   API contracts, enums and input limits - no dependencies
  Merito.Server/   API, EF Core, Identity, domain services (Features/*); hosts the client
  Merito.Client/   Blazor WebAssembly PWA built from Flare components
tests/
  Merito.Server.Tests/
scripts/
  make-icons.ps1   renders the PWA icons
```

Rules the code keeps:
- The server is the only source of truth about points. A balance changes only together with an
  immutable ledger entry, guarded by a concurrency token, so a balance cannot be spent twice.
- Every family endpoint checks membership and role; a child sees only their own submissions,
  purchases and history.
- The UI is assembled from Flare components and their parameters, with no custom stylesheets.

## Roadmap

- Weekly savings bonus and a "week without penalties" reward.
- Shop rules: a break between screen-time packages, rewards available only on certain days.
- An optional family setting that approves tasks from the list automatically.

## License

[MIT](LICENSE)
