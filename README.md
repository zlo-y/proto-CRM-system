[![English](https://img.shields.io/badge/Language-Русский-blue.svg)](README.md)
[![Russian](https://img.shields.io/badge/Language-English-red.svg)](README.en.md)

# Manager API

Backend для системы управления проектами, задачами и сотрудниками. REST API на ASP.NET Core с JWT-аутентификацией, ролевой моделью доступа и хранением файлов проектов.

## Содержание

- [Технологии](#технологии)
- [Архитектура](#архитектура)
- [Возможности](#возможности)
- [Безопасность](#безопасность)
- [Быстрый старт](#быстрый-старт-docker)
- [Локальный запуск без Docker](#локальный-запуск-без-docker)
- [Переменные окружения](#переменные-окружения)
- [Структура проекта](#структура-проекта)
- [API](#api)
- [Роли доступа](#роли-доступа)

## Технологии

| Категория | Технологии |
|---|---|
| Платформа | .NET 8, ASP.NET Core Web API |
| База данных | PostgreSQL, Entity Framework Core |
| Аутентификация | ASP.NET Core Identity, JWT (httpOnly cookies) |
| Логирование | Serilog (консоль + файл) |
| Почта | MailKit / MimeKit (SMTP) |
| Инфраструктура | Docker, docker-compose |
| Паттерны | Repository, Unit of Work, DTO-маппинг, Middleware pipeline |

## Архитектура

Проект построен по слоистой архитектуре (Layered Architecture) с чётким разделением ответственности:

```
Manager.WebAPI          → контроллеры, middleware, конфигурация DI, аутентификация
Manager.BusinessLogic    → сервисы, DTO, интерфейсы, кастомные исключения, маппинги
Manager.DataAccess       → сущности EF Core, репозитории, DbContext, миграции
```

Ключевые паттерны:

- **Repository + Unit of Work** — абстракция над EF Core, единая точка управления транзакциями.
- **Кастомные исключения + `ExceptionMiddleware`** — доменные ошибки (`NotFoundException`, `ForbiddenException`, `ValidationAppException` и т.д.) централизованно превращаются в корректные HTTP-статусы и единый формат ответа `ApiResponse<T>`.
- **DTO-слой** — сущности EF Core никогда не отдаются наружу напрямую, только через явные DTO и extension-методы маппинга.

## Возможности

- Регистрация и авторизация пользователей (email + пароль), выдача JWT в httpOnly cookie
- Восстановление пароля через email со ссылкой сброса
- CRUD проектов: фильтрация по датам и приоритету, сортировка, пагинация
- Прикрепление документов к проекту (с валидацией размера и типа файла)
- Управление сотрудниками проекта (добавление/удаление участников)
- CRUD задач проекта: назначение исполнителя, смена статуса (`ToDo` → `InProgress` → `Done`)
- Ролевая модель доступа: `Admin` / `Employee`, с проверкой прав на уровне менеджера проекта
- Health checks (`/health/live`, `/health/ready`) для мониторинга и оркестрации

## Безопасность

- JWT-токен передаётся через `httpOnly` + `Secure` cookie (недоступен из JS, защита от XSS)
- Rate limiting: отдельная строгая политика для `/auth/*` эндпоинтов (защита от брутфорса), общий лимитер для остальных запросов
- Lockout аккаунта после серии неудачных попыток входа (ASP.NET Core Identity)
- CORS с явным allowlist источников + дополнительная проверка `Origin`-заголовка на mutating-запросах
- Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`, `Permissions-Policy`)
- Защита от path traversal при удалении загруженных файлов
- Секреты (JWT-ключ, пароли БД, SMTP) вынесены из репозитория — см. [Переменные окружения](#переменные-окружения)

## Быстрый старт (Docker)

Самый простой способ поднять проект целиком — API + PostgreSQL — одной командой.

**Требования:** Docker, Docker Compose.

1. Склонируй репозиторий:
   ```bash
   git clone https://github.com/<zlo-y>/<proto-CRM-system>.git
   cd <repo-name>
   ```

2. Создай `.env` на основе примера:
   ```bash
   cp .env.example .env
   ```
   Заполни своими значениями (см. [Переменные окружения](#переменные-окружения)).

3. Создай `Manager.WebAPI/appsettings.json` на основе примера:
   ```bash
   cp Manager.WebAPI/appsettings.Example.json Manager.WebAPI/appsettings.json
   ```
   Заполни `Jwt:Key` случайной строкой (см. ниже, как сгенерировать) и остальные значения под себя.

   Сгенерировать надёжный ключ:
   ```bash
   openssl rand -base64 48
   ```

4. Запусти:
   ```bash
   docker compose up --build
   ```

5. API будет доступен на `http://localhost:8080`. Миграции применяются автоматически при старте контейнера.

6. Проверить, что всё работает:
   ```bash
   curl http://localhost:8080/health/live
   ```

## Локальный запуск без Docker

**Требования:** .NET 8 SDK, PostgreSQL (локально или в контейнере).

1. Подними PostgreSQL любым удобным способом, например:
   ```bash
   docker run -d --name manager_pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=ManagerDB -p 5432:5432 postgres:15-alpine
   ```

2. Настрой `Manager.WebAPI/appsettings.json` (на основе `appsettings.Example.json`), указав актуальную `ConnectionStrings:DefaultConnection`.

3. Примени миграции:
   ```bash
   dotnet ef database update --project Manager.DataAccess --startup-project Manager.WebAPI
   ```

4. Запусти проект:
   ```bash
   dotnet run --project Manager.WebAPI
   ```

5. В режиме разработки доступен Swagger UI: `http://localhost:5000/swagger` (порт зависит от `launchSettings.json`).

## Переменные окружения

### `.env` (используется docker-compose)

| Переменная | Описание |
|---|---|
| `DB_USER` | Имя пользователя PostgreSQL |
| `DB_PASSWORD` | Пароль PostgreSQL |
| `DB_NAME` | Имя базы данных |
| `DB_PORT` | Порт, на который PostgreSQL пробрасывается наружу контейнера |

### `appsettings.json` (основная конфигурация API)

| Ключ | Описание |
|---|---|
| `ConnectionStrings:DefaultConnection` | Строка подключения к PostgreSQL |
| `Jwt:Key` | Секретный ключ подписи JWT (сгенерируй случайный, минимум 32 символа) |
| `Jwt:Issuer` / `Jwt:Audience` | Издатель и аудитория токена |
| `Jwt:ExpiryInDays` | Срок жизни токена в днях |
| `Cors:AllowedOrigins` | Список разрешённых origin'ов фронтенда |
| `FileSettings:MaxFileSize` | Максимальный размер загружаемого файла (в байтах) |
| `FileSettings:AllowedExtensions` | Разрешённые расширения файлов через запятую |
| `FileSettings:UploadsRootPath` | Корневая директория для хранения загруженных файлов |
| `EmailSettings:*` | Настройки SMTP-сервера для отправки писем сброса пароля |
| `Frontend:BaseUrl` | Базовый URL фронтенда (используется в ссылке сброса пароля) |

> ⚠️ **Никогда не коммить `appsettings.json` и `.env` с реальными значениями.** Оба файла добавлены в `.gitignore`; в репозитории хранятся только `appsettings.Example.json` и `.env.example`.

## Структура проекта

```
├── Manager.WebAPI/            # Контроллеры, middleware, DI, точка входа
│   ├── Controllers/
│   ├── Middlewares/
│   ├── Extensions/
│   └── Program.cs
├── Manager.BusinessLogic/     # Сервисы, DTO, интерфейсы, исключения
│   ├── Services/
│   ├── Interfaces/
│   ├── DTOs/
│   ├── Mappings/
│   └── Exceptions/
├── Manager.DataAccess/        # EF Core сущности, репозитории, DbContext
│   ├── Entities/
│   ├── Repositories/
│   ├── Interfaces/
│   └── AppDbContext.cs
├── docker-compose.yml
├── Dockerfile
└── README.md
```

## API

Полная спецификация доступна через Swagger UI в режиме разработки (`/swagger`). Основные группы эндпоинтов:

| Группа | Base route | Описание |
|---|---|---|
| Auth | `/api/v1/auth` | Регистрация, логин, сброс пароля, логаут |
| Projects | `/api/v1/projects` | CRUD проектов, управление участниками |
| Tasks | `/api/v1/tasks` | CRUD задач, назначение исполнителя, смена статуса |
| Employees | `/api/v1/employees` | Список сотрудников, редактирование, удаление (Admin) |
| Health | `/health/live`, `/health/ready` | Проверка живости и готовности сервиса |

Все ответы API оборачиваются в единый формат:
```json
{
  "success": true,
  "message": "Список проектов успешно получен.",
  "data": { }
}
```

## Роли доступа

| Роль | Права |
|---|---|
| `Employee` | Базовая роль по умолчанию при регистрации. Доступ к проектам/задачам, где сотрудник участвует, а также управление проектами, которыми он руководит |
| `Admin` | Полный доступ ко всем проектам, задачам и сотрудникам, включая редактирование и удаление |

---

Проект создан для личного использования или же небольшой , но доверенной группе лиц , служит для демонстрации работы с различными паттернами (не говорить про оверинжиниринг , я и так про него знаю)
