# Merito

**Баллы за дела, уроки и хорошие привычки - семейное приложение, где дети зарабатывают баллы и тратят
их в семейном магазине.** [English version](README.md)

[![CI](https://github.com/jrfrigat/Merito/actions/workflows/ci.yml/badge.svg)](https://github.com/jrfrigat/Merito/actions/workflows/ci.yml)
[![Docker Pulls](https://img.shields.io/docker/pulls/frigat/merito?logo=docker&label=Docker%20pulls)](https://hub.docker.com/r/frigat/merito)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WebAssembly%20PWA-5C2D91)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Merito (лат. "по заслугам") превращает семейные правила в простой цикл: ребенок отмечает, что сделал,
родитель подтверждает и начисляет баллы, а ребенок тратит их на награды, о которых договорилась
семья, - экранное время, поездку, угощение.

## Возможности

**Для родителей**
- Создать семью и за пару минут заполнить ее: задания (ежедневные и дополнительные), штрафы и магазин
  наград - или начать с готового примера: 21 задание, 7 штрафов и магазин экранного времени.
- Добавить детей как аккаунты с логином и паролем (почта не нужна) или пригласить по коду - второго
  родителя или ребенка, у которого уже есть аккаунт.
- Проверять отметки детей: засчитать с баллами по списку или своими, отклонить с причиной и тут же
  сохранить новое дело в список заданий.
- Начислить или списать баллы вручную - только с комментарием - или назначить штраф из списка.
- Выдать купленную награду или отменить покупку с возвратом баллов.
- Видеть баланс каждого ребенка и всю историю баллов.

**Для детей**
- Одно нажатие с главного экрана: "Я сделал" или "Магазин".
- Выбрать задание из списка или написать дело, которого в нем нет.
- Покупать награды, когда хватает баллов; видеть, что ждет проверки, и свою историю.

**Везде**
- Сначала телефон: нижняя панель навигации на телефоне, боковое меню на широком экране.
- PWA, которое ставится на домашний экран, со светлой и темной темой.

## Технологии

- **Клиент:** Blazor WebAssembly PWA на [Flare](https://github.com/jrfrigat/Flare) с темой
  Material 3 Expressive.
- **Сервер:** ASP.NET Core minimal API, ASP.NET Core Identity (bearer-токены), EF Core и PostgreSQL.
  Сервер сам отдает WebAssembly-клиент, поэтому приложение и API живут на одном адресе.
- **Тесты:** xUnit v3 - доменные сервисы на SQLite в памяти и настоящий HTTP-конвейер через
  `WebApplicationFactory`, включая проверки доступа для чужих пользователей и детей.

## Запуск в Docker

Нужен Docker с Compose v2.

```bash
cp .env.example .env              # задать POSTGRES_PASSWORD
docker network create proxy_network   # один раз; пропустить, если сеть уже есть
docker compose up -d --build
```

В стеке два контейнера: `merito` (приложение, внутри контейнера слушает порт 8080) и `merito-db`
(PostgreSQL 16 с томом `merito-pgdata`). Миграции базы применяются при старте приложения.

`merito` подключается к внешней сети `proxy_network` и не публикует порты: направьте на
`http://merito:8080` ваш обратный прокси (например, Nginx Proxy Manager в той же сети). Чтобы открыть
приложение прямо на этой машине, создайте локальный `docker-compose.override.yml` (он в `.gitignore`):

```yaml
services:
  merito:
    ports:
      - "5080:8080"
```

и откройте http://localhost:5080. В Windows стек пересобирает и перезапускает `run-docker-compose.bat`.

Первый зарегистрированный аккаунт - родительский: создайте семью, затем добавьте детей на странице
"Семья".

### Настройки

| Переменная | По умолчанию | Назначение |
| --- | --- | --- |
| `POSTGRES_PASSWORD` | - (обязательно) | Пароль базы для обоих контейнеров |
| `POSTGRES_DB` | `merito` | Имя базы |
| `POSTGRES_USER` | `merito` | Пользователь базы |

В продакшене отдавайте приложение по HTTPS (удобнее всего на обратном прокси): токены входа ходят в
заголовках запросов, а браузеры устанавливают PWA только с защищенных адресов.

### Готовый образ

Каждый тег версии (`v1.2.3`) публикует образ в Docker Hub как `frigat/merito` и в GitHub Container
Registry как `ghcr.io/jrfrigat/merito` с тегами `X.Y.Z` и `latest`. Чтобы запустить релиз, а не
собирать из исходников, замените у сервиса `merito` раздел `build:` на:

```yaml
    image: frigat/merito:latest   # или закрепленный тег X.Y.Z
```

## Разработка

Нужен .NET SDK 10.0.400 или новее.

```bash
# одноразовая база для локального запуска
docker run -d --name merito-dev-db -e POSTGRES_USER=merito -e POSTGRES_PASSWORD=merito \
  -e POSTGRES_DB=merito -p 127.0.0.1:5433:5432 postgres:16-alpine

dotnet user-secrets set ConnectionStrings:Merito \
  "Host=localhost;Port=5433;Database=merito;Username=merito;Password=merito" --project src/Merito.Server

dotnet run --project src/Merito.Server   # http://localhost:5080
dotnet test --solution Merito.slnx
```

Схема базы меняется миграциями EF Core:

```bash
dotnet ef migrations add <Name> --project src/Merito.Server --output-dir Data/Migrations
```

## Структура

```
src/
  Merito.Shared/   контракты API, перечисления и ограничения ввода - без зависимостей
  Merito.Server/   API, EF Core, Identity, доменные сервисы (Features/*); отдает клиент
  Merito.Client/   Blazor WebAssembly PWA из компонентов Flare
tests/
  Merito.Server.Tests/
scripts/
  make-icons.ps1   рисует иконки PWA
```

Правила, которые держит код:
- Сервер - единственный источник правды о баллах. Баланс меняется только вместе с неизменяемой записью
  журнала и под токеном конкурентности, поэтому одни и те же баллы нельзя потратить дважды.
- Каждый эндпоинт семьи проверяет членство и роль; ребенок видит только свои отметки, покупки и историю.
- Интерфейс собран из компонентов Flare и их параметров, без собственных таблиц стилей.

## Планы

- Недельный бонус за накопление и награда за "неделю без штрафов".
- Правила магазина: перерыв между пакетами экранного времени, награды только в определенные дни.
- Настройка семьи, при которой задания из списка засчитываются автоматически.

## Лицензия

[MIT](LICENSE)
