[![English](https://img.shields.io/badge/Language-Русский-blue.svg)](README.md)
[![Russian](https://img.shields.io/badge/Language-English-red.svg)](README.en.md)

# Manager API

Backend for a project, task, and employee management system. A REST API built on ASP.NET Core with JWT authentication, role-based access control, and project file storage.

## Table of Contents

- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Features](#features)
- [Security](#security)
- [Quick Start (Docker)](#quick-start-docker)
- [Local Setup Without Docker](#local-setup-without-docker)
- [Environment Variables](#environment-variables)
- [Project Structure](#project-structure)
- [API](#api)
- [Access Roles](#access-roles)

## Tech Stack

| Category | Technologies |
|---|---|
| Platform | .NET 8, ASP.NET Core Web API |
| Database | PostgreSQL, Entity Framework Core |
| Authentication | ASP.NET Core Identity, JWT (httpOnly cookies) |
| Logging | Serilog (console + file) |
| Email | MailKit / MimeKit (SMTP) |
| Infrastructure | Docker, docker-compose |
| Patterns | Repository, Unit of Work, DTO mapping, middleware pipeline |

## Architecture

The project follows a layered architecture with clear separation of concerns:

```
Manager.WebAPI          → controllers, middleware, DI configuration, authentication
Manager.BusinessLogic    → services, DTOs, interfaces, custom exceptions, mappings
Manager.DataAccess       → EF Core entities, repositories, DbContext, migrations
```

Key patterns:

- **Repository + Unit of Work** — abstraction over EF Core, a single point for transaction management.
- **Custom exceptions + `ExceptionMiddleware`** — domain errors (`NotFoundException`, `ForbiddenException`, `ValidationAppException`, etc.) are centrally converted into proper HTTP status codes and a unified `ApiResponse<T>` response format.
- **DTO layer** — EF Core entities are never exposed directly; only through explicit DTOs and mapping extension methods.

## Features

- User registration and login (email + password), JWT issued via httpOnly cookie
- Password recovery via email with a reset link
- Project CRUD: filtering by date range and priority, sorting, pagination
- Attaching documents to a project (with file size and type validation)
- Managing project team members (add/remove employees)
- Project task CRUD: executor assignment, status transitions (`ToDo` → `InProgress` → `Done`)
- Role-based access control: `Admin` / `Employee`, with permission checks at the project manager level
- Health checks (`/health/live`, `/health/ready`) for monitoring and orchestration

## Security

- JWT is delivered via an `httpOnly` + `Secure` cookie (inaccessible from JS, mitigates XSS)
- Rate limiting: a strict dedicated policy for `/auth/*` endpoints (brute-force protection), a general limiter for the rest of the API
- Account lockout after repeated failed login attempts (ASP.NET Core Identity)
- CORS with an explicit origin allowlist, plus an additional `Origin` header check on mutating requests
- Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`, `Permissions-Policy`)
- Path traversal protection when deleting uploaded files
- Secrets (JWT key, database passwords, SMTP credentials) are kept out of the repository — see [Environment Variables](#environment-variables)

## Quick Start (Docker)

The simplest way to run the whole stack — API + PostgreSQL — with a single command.

**Requirements:** Docker, Docker Compose.

1. Clone the repository:
   ```bash
   git clone https://github.com/<your-username>/<repo-name>.git
   cd <repo-name>
   ```

2. Create `.env` from the example:
   ```bash
   cp .env.example .env
   ```
   Fill in your own values (see [Environment Variables](#environment-variables)).

3. Create `Manager.WebAPI/appsettings.json` from the example:
   ```bash
   cp Manager.WebAPI/appsettings.Example.json Manager.WebAPI/appsettings.json
   ```
   Fill in `Jwt:Key` with a random string (see below on how to generate one) and the rest of the values as needed.

   Generate a strong key:
   ```bash
   openssl rand -base64 48
   ```

4. Start it up:
   ```bash
   docker compose up --build
   ```

5. The API will be available at `http://localhost:8080`. Migrations are applied automatically on container startup.

6. Verify it's running:
   ```bash
   curl http://localhost:8080/health/live
   ```

## Local Setup Without Docker

**Requirements:** .NET 8 SDK, PostgreSQL (local or containerized).

1. Spin up PostgreSQL any way you like, for example:
   ```bash
   docker run -d --name manager_pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=ManagerDB -p 5432:5432 postgres:15-alpine
   ```

2. Configure `Manager.WebAPI/appsettings.json` (based on `appsettings.Example.json`), setting an actual `ConnectionStrings:DefaultConnection`.

3. Apply migrations:
   ```bash
   dotnet ef database update --project Manager.DataAccess --startup-project Manager.WebAPI
   ```

4. Run the project:
   ```bash
   dotnet run --project Manager.WebAPI
   ```

5. In development mode, Swagger UI is available at: `http://localhost:5000/swagger` (port depends on `launchSettings.json`).

## Environment Variables

### `.env` (used by docker-compose)

| Variable | Description |
|---|---|
| `DB_USER` | PostgreSQL username |
| `DB_PASSWORD` | PostgreSQL password |
| `DB_NAME` | Database name |
| `DB_PORT` | Host port that PostgreSQL is exposed on |

### `appsettings.json` (main API configuration)

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Jwt:Key` | JWT signing secret (generate a random string, at least 32 characters) |
| `Jwt:Issuer` / `Jwt:Audience` | Token issuer and audience |
| `Jwt:ExpiryInDays` | Token lifetime in days |
| `Cors:AllowedOrigins` | List of allowed frontend origins |
| `FileSettings:MaxFileSize` | Maximum upload size (in bytes) |
| `FileSettings:AllowedExtensions` | Allowed file extensions, comma-separated |
| `FileSettings:UploadsRootPath` | Root directory for storing uploaded files |
| `EmailSettings:*` | SMTP settings for sending password reset emails |
| `Frontend:BaseUrl` | Frontend base URL (used in the password reset link) |

> ⚠️ **Never commit `appsettings.json` or `.env` with real values.** Both files are listed in `.gitignore`; only `appsettings.Example.json` and `.env.example` are tracked in the repository.

## Project Structure

```
├── Manager.WebAPI/            # Controllers, middleware, DI, entry point
│   ├── Controllers/
│   ├── Middlewares/
│   ├── Extensions/
│   └── Program.cs
├── Manager.BusinessLogic/     # Services, DTOs, interfaces, exceptions
│   ├── Services/
│   ├── Interfaces/
│   ├── DTOs/
│   ├── Mappings/
│   └── Exceptions/
├── Manager.DataAccess/        # EF Core entities, repositories, DbContext
│   ├── Entities/
│   ├── Repositories/
│   ├── Interfaces/
│   └── AppDbContext.cs
├── docker-compose.yml
├── Dockerfile
└── README.md
```

## API

Full specification is available via Swagger UI in development mode (`/swagger`). Main endpoint groups:

| Group | Base route | Description |
|---|---|---|
| Auth | `/api/v1/auth` | Registration, login, password reset, logout |
| Projects | `/api/v1/projects` | Project CRUD, team member management |
| Tasks | `/api/v1/tasks` | Task CRUD, executor assignment, status transitions |
| Employees | `/api/v1/employees` | Employee listing, editing, deletion (Admin) |
| Health | `/health/live`, `/health/ready` | Liveness and readiness checks |

All API responses are wrapped in a unified format:
```json
{
  "success": true,
  "message": "Project list retrieved successfully.",
  "data": { }
}
```

## Access Roles

| Role | Permissions |
|---|---|
| `Employee` | Default role assigned on registration. Access to projects/tasks the employee is part of, plus management rights over projects they manage |
| `Admin` | Full access to all projects, tasks, and employees, including editing and deletion |

---

Built for personal use or a small, trusted group of users, and serves as a demonstration of working with various architectural patterns.
