# Merito

Family points app: a child reports done chores and homework, a parent confirms them and awards points,
and the child spends the points on rewards in a family shop. Mobile-first Blazor WebAssembly PWA
(Material 3 Expressive, Russian UI) with an ASP.NET Core API. Built on .NET 10.

This image is the whole application: the API plus the PWA served from the same origin. It needs a
PostgreSQL database that you provide; migrations run on start.

## Run with Docker Compose

```yaml
services:
  merito-db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: merito
      POSTGRES_USER: merito
      POSTGRES_PASSWORD: a-strong-password
    volumes:
      - merito-pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U merito -d merito"]
      interval: 5s
      retries: 20

  merito:
    image: frigat/merito:latest
    restart: unless-stopped
    depends_on:
      merito-db:
        condition: service_healthy
    environment:
      ConnectionStrings__Merito: "Host=merito-db;Port=5432;Database=merito;Username=merito;Password=a-strong-password"
    ports:
      - "8080:8080"

volumes:
  merito-pgdata:
```

Open http://localhost:8080 and register the first parent account. In production put the app behind a
reverse proxy with HTTPS: sign-in tokens travel in request headers, and browsers install PWAs only from
secure origins.

## Tags

- `latest` - the most recent release.
- `X.Y.Z` - a pinned version (recommended for production).

## Configuration

| Variable | Purpose |
| --- | --- |
| `ConnectionStrings__Merito` | PostgreSQL connection string (required) |
| `ASPNETCORE_HTTP_PORTS` | Port inside the container, `8080` by default |

## Links

- Source, documentation and issues: https://github.com/jrfrigat/Merito
