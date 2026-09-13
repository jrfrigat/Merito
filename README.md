# Merito

Семейное приложение баллов: ребенок отмечает сделанные дела, родитель подтверждает и назначает баллы,
баллы тратятся в семейном магазине наград.

- Родитель создает семью, настраивает задания, штрафы и магазин (или заполняет их примером).
- Ребенок выбирает сделанное из списка или пишет свое дело.
- Родитель подтверждает, меняет баллы, может сохранить новое дело в список, начислить или списать
  баллы с комментарием.
- Ребенок покупает награды, родитель отмечает выдачу или отменяет покупку с возвратом баллов.

Клиент - Blazor WebAssembly PWA на [Flare](https://github.com/jrfrigat/Flare) с темой
Material 3 Expressive, сервер - ASP.NET Core + PostgreSQL.

## Запуск в Docker

```bash
cp .env.example .env    # задать POSTGRES_PASSWORD
docker compose up -d --build
```

Приложение откроется на http://localhost:5080. Данные PostgreSQL хранятся в томе `merito-pgdata`,
миграции применяются при старте сервера.

## Разработка

```bash
docker compose up -d merito-db
dotnet run --project src/Merito.Server
dotnet test --solution Merito.slnx
```

Для локального запуска серверу нужна строка подключения к базе из compose (порт 5433), пароль - из
вашего `.env`:

```bash
dotnet user-secrets set ConnectionStrings:Merito "Host=localhost;Port=5433;Database=merito;Username=merito;Password=<пароль>" --project src/Merito.Server
```
