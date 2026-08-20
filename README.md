# ProductManager

Full-stack product management application built for the Carl Zeiss fullstack assessment: a REST API (ASP.NET Core, Clean Architecture) and an Angular + Angular Material frontend.

- [`ProductManager.WebAPI`](#productmanager-api) and the rest of the `.NET` solution — the backend API (this section).
- [`client-app`](./client-app/README.md) — the Angular frontend (see [Frontend](#frontend) below for a quick start).

> **Want the fastest path to a running app?** See [Run with Docker](#run-with-docker) — one command starts SQL Server, the API (with migrations + seeding applied automatically), and the Angular frontend.

# ProductManager API

REST API for managing products, built with ASP.NET Core and Clean Architecture for the Carl Zeiss fullstack assessment.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server Express (or SQL Server LocalDB included with Visual Studio)
- [Node.js 20+](https://nodejs.org/) and npm (only needed to run the [Angular frontend](#frontend))

> Alternatively, install only [Docker Desktop](https://www.docker.com/products/docker-desktop/) and skip straight to [Run with Docker](#run-with-docker) — no .NET/Node/SQL Server installation required.

## Run locally

1. Restore dependencies and tools:

```bash
dotnet restore
dotnet tool restore
```

2. Update the connection string in `ProductManager.WebAPI/appsettings.json` if needed (currently configured for `localhost\SQLEXPRESS`).

3. Configure the JWT signing secret. `appsettings.json` intentionally ships with an **empty** `JwtSettings:SecretKey` — the app fails fast at startup if it isn't set, so a real secret never gets committed to source control. For local (non-Docker) development, store it with [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (saved outside the repo, under your user profile):

```bash
dotnet user-secrets init --project ProductManager.WebAPI
dotnet user-secrets set "JwtSettings:SecretKey" "<a-long-random-string-at-least-32-chars>" --project ProductManager.WebAPI
```

   PowerShell one-liner to generate a strong random value:

   ```powershell
   [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
   ```

   (When running via [Docker Compose](#run-with-docker) instead, the secret comes from the `JWT_SECRET_KEY` value in your `.env` file — see below — so User Secrets aren't needed in that flow.)

4. Apply migrations (optional — the app applies them automatically on startup):

```bash
dotnet ef database update --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI
```

5. Run the API:

```bash
dotnet run --project ProductManager.WebAPI
```

6. Access the API:

**Swagger UI (Interactive API Documentation):**
- HTTPS: `https://localhost:7228/`
- HTTP: `http://localhost:5270/`

The Swagger UI provides an interactive interface to explore and test all API endpoints. A demo login (`demo` / `Demo@1234`) is seeded automatically the first time `SeedAsync` runs against an empty database, so you can call `POST /api/Auth/login` right away instead of registering a new user first (see [Authentication](#authentication)).

**Alternative test methods:**
- OpenAPI JSON spec: `https://localhost:7228/swagger/v1/swagger.json`
- Sample HTTP requests: `ProductManager.WebAPI/ProductManager.WebAPI.http`

## Run with Docker

The whole stack — **SQL Server + API + Angular frontend** — can be started with a single command using the provided `Dockerfile`s and `docker-compose.yml`. This is the quickest way to run the app locally and doesn't require .NET, Node, or SQL Server installed on your machine — only [Docker Desktop](https://www.docker.com/products/docker-desktop/).

### 1. Configure environment variables

Copy the example env file and adjust the values (at minimum, change the passwords):

```bash
cp .env.example .env
```

| Variable | Purpose | Default |
|----------|---------|---------|
| `DB_SA_PASSWORD` | SQL Server `sa` password (must meet SQL Server's complexity rules) | `Str0ng!Passw0rd#2026` |
| `JWT_SECRET_KEY` | Secret key used to sign JWT tokens (32+ chars) | `Str0ng!Passw0rd#2026SuperSecretKeyForHS256Docker` |
| `ASPNETCORE_ENVIRONMENT` | `Development` keeps Swagger UI enabled | `Development` |
| `SQL_PORT` / `API_PORT` / `CLIENT_PORT` | Host ports, change if already in use | `1433` / `8080` / `4200` |

### 2. Build and start everything

```bash
docker compose up --build
```

This will, in order:

1. Start **SQL Server 2022** in a container with a persisted volume (`sqlserver_data`), and wait until it reports healthy.
2. Build and start the **API** container. On startup, the API automatically **applies pending EF Core migrations and seeds the 5 sample products plus one demo login** (`DatabaseSeeder.SeedAsync`, called from `Program.cs`) — no manual migration/seeding/registration step is required.
3. Build and start the **Angular frontend**, served by nginx. nginx proxies any `/api/*` request to the API container (same pattern as the dev `proxy.conf.json`), so the frontend and API share an origin and no CORS configuration is required in the browser.

### 3. Access the app

- **Frontend:** `http://localhost:4200` — log in with the seeded demo account:
  - **Username:** `demo`
  - **Password:** `Demo@1234`

  (Seeded once, on first run against an empty database, by `DatabaseSeeder`. The credentials are also printed to the API container logs — `docker compose logs api` — the first time they're created.)
- **API + Swagger UI:** `http://localhost:8080/` (only when `ASPNETCORE_ENVIRONMENT=Development`)
- **SQL Server:** `localhost,1433` (e.g. via SSMS or Azure Data Studio, using `sa` / `DB_SA_PASSWORD`)

### Useful commands

```bash
# Run in the background
docker compose up --build -d

# Tail logs for a single service
docker compose logs -f api

# Stop everything (containers only, keeps the SQL Server volume/data)
docker compose down

# Stop everything and wipe the database volume (fresh reseed on next `up`)
docker compose down -v

# Rebuild a single service after code changes
docker compose up --build api
```

### Applying migrations / re-seeding manually (optional)

Migrations and seeding run automatically every time the `api` container starts, so this is rarely needed. If you want to run `dotnet ef` commands against the containerized database from your host machine (e.g. after adding a new migration), point the connection string at the exposed SQL Server port:

```bash
dotnet ef database update --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI --connection "Server=localhost,1433;Database=ProductManagerDb;User Id=sa;Password=<DB_SA_PASSWORD>;TrustServerCertificate=true;Encrypt=true"
```

### Files involved

| File | Purpose |
|------|---------|
| `ProductManager.WebAPI/Dockerfile` | Multi-stage build (`dotnet publish`) → ASP.NET Core runtime image for the API |
| `client-app/Dockerfile` | Multi-stage build (`npm ci` + `ng build`) → nginx serving the compiled Angular app |
| `client-app/nginx.conf` | Serves the SPA and reverse-proxies `/api/*` to the `api` container |
| `docker-compose.yml` | Orchestrates `sqlserver`, `api`, and `client` with volumes, env vars, healthchecks |
| `.env.example` | Template for secrets/ports consumed by `docker-compose.yml` (copy to `.env`) |

## API endpoints

### Authentication

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login and receive JWT token |

### Products (Requires Authentication)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/products` | List all products |
| POST | `/api/products` | Create a product |
| GET | `/api/products/{id}` | Get product by ID |
| PUT | `/api/products/{id}` | Update a product |
| DELETE | `/api/products/{id}` | Delete a product |
| POST | `/api/products/{id}/decrement-stock/{quantity}` | Decrease stock |
| POST | `/api/products/{id}/add-to-stock/{quantity}` | Increase stock |
| GET | `/api/products/search?name={name}` | Search by name (partial match) |
| GET | `/api/products/stock-level?min={min}&max={max}` | Filter by stock range |

## Authentication

The API uses **JWT (JSON Web Tokens)** for authentication. All product endpoints require a valid token.

> **Fastest path:** a demo login (`demo` / `Demo@1234`) is seeded automatically on first run — see [Run with Docker](#3-access-the-app) or [Run locally](#run-locally) — so you can `POST /api/auth/login` (or just use the Angular login page) immediately without registering anything.

### Quick Start

1. **Register a new user:**

```json
POST /api/auth/register
Content-Type: application/json

{
  "username": "testuser",
  "email": "test@example.com",
  "password": "password123"
}
```

Response:
```json
{
  "token": "eyJhbGciOi...",
  "username": "testuser",
  "email": "test@example.com"
}
```

2. **Login (if already registered):**

```json
POST /api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "password123"
}
```

3. **Use the token in subsequent requests:**

```http
GET /api/products
Authorization: Bearer eyJhbGciOi...
```

### Testing with Swagger UI

1. Open the Swagger UI (`https://localhost:7228/`)
2. Expand `POST /api/Auth/register` (or `/api/Auth/login`), click **Try it out**, fill in the request body, and click **Execute**
3. Copy the `token` value from the response body
4. Click the **Authorize** button (top-right, lock icon)
5. Paste the token into the **Value** field — do **not** prefix it with `Bearer `, Swagger adds that automatically
6. Click **Authorize**, then **Close**
7. All subsequent requests made from Swagger UI (including the padlocked `/api/products` endpoints) will automatically include the `Authorization: Bearer <token>` header

### Testing with Postman/Thunder Client

1. Register or login to get a token
2. Copy the token from the response
3. Add to request header: `Authorization: Bearer <your-token>`

### JWT Configuration

JWT settings live under `JwtSettings` in `appsettings.json`:

```json
"JwtSettings": {
  "SecretKey": "",
  "Issuer": "ProductManagerAPI",
  "Audience": "ProductManagerClient",
  "ExpiryMinutes": "60"
}
```

`SecretKey` is **intentionally left empty in source control**. `Program.cs` validates it at startup and throws immediately with a descriptive error if it's missing, rather than letting the app boot with no/weak signing key:

```
Unhandled exception. System.InvalidOperationException: JwtSettings:SecretKey is not configured.
Set it via .NET User Secrets for local development (dotnet user-secrets set "JwtSettings:SecretKey" "<value>")
or via the JwtSettings__SecretKey environment variable in Docker/production. See README.md.
```

Supply the real value out-of-band, matching your environment:

| Environment | Where the secret comes from |
|---|---|
| Local `dotnet run` | [.NET User Secrets](#run-locally) (`dotnet user-secrets set ...`) — stored under your user profile, never in the repo |
| Docker Compose | `JWT_SECRET_KEY` in your `.env` file → passed through as the `JwtSettings__SecretKey` env var (double underscore = nested config key) |
| Production hosting (VM/cloud) | An environment variable or your platform's secret store (e.g. Docker/Kubernetes secrets, Azure Key Vault, AWS Secrets Manager) injected as `JwtSettings__SecretKey` — never written to disk in a config file |

**Security notes:**
- Use a cryptographically random value of at least 32 bytes (256 bits) for HS256 — see the generator snippet in [Run locally](#run-locally).
- Rotate the secret periodically and immediately if it's ever exposed; rotating invalidates all previously issued tokens.
- Never commit a real secret to `appsettings.json`, `appsettings.*.json`, or `.env` — only `.env.example`/`appsettings.Development.json.example` (with placeholder values) belong in Git.

## CORS

The API uses a **whitelist-based CORS policy** — only origins explicitly listed in configuration can call the API from a browser. There is no `AllowAnyOrigin()` fallback, so the policy fails closed (denies all cross-origin requests) if no origins are configured.

Configure allowed origins in `appsettings.json` / `appsettings.Development.json`:

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:4200",
    "http://localhost:5173"
  ]
}
```

- **Allowed methods:** `GET`, `POST`, `PUT`, `DELETE`
- **Allowed headers:** `Content-Type`, `Authorization`
- **Credentials:** allowed (required for the `Authorization: Bearer <token>` header to be sent cross-origin)

`appsettings.Development.json` ships with common local frontend dev server ports pre-configured (React/CRA, Angular, Vite). For production, replace the list with your actual frontend domain(s), e.g. `"https://app.yourdomain.com"`.

**Security Note:** Never use `AllowAnyOrigin()` together with `AllowCredentials()` — the CORS spec forbids it, and enabling it would expose the API to cross-site credential leakage. Always whitelist specific origins.

## Architecture

```
ProductManger.Domain          → Entities, repository interfaces
ProductManager.Application    → CQRS handlers, validators, DTOs
ProductManager.Infrastructure → EF Core, repositories, ID generator, seeding
ProductManager.Presentation   → API controllers
ProductManager.WebAPI         → Host, middleware, configuration
```

## Frontend

The [`client-app`](./client-app) folder contains the Angular frontend (latest Angular, standalone components, Angular Material UI). It provides:

- A user-friendly **Login** page (Material card, reactive form validation, error handling) required before the rest of the app is accessible. There's no self-service registration screen by design (see [Authentication](#authentication)) — instead, `DatabaseSeeder` seeds one demo login (`demo` / `Demo@1234`) on first run so the app works immediately out of the box; additional users can be created via `POST /api/auth/register`.
- Route guards that redirect unauthenticated users to `/login` and keep authenticated users out of `/login`.
- An HTTP interceptor that attaches the JWT to every API request and signs the user out automatically on a `401`.
- A **Products** dashboard: table of all products with search-by-name, filter-by-stock-range, create/edit dialog, add-to-stock/decrement-stock dialogs, and a delete confirmation dialog.

### Quick start

```bash
# Terminal 1 — backend (from the repository root)
dotnet run --project ProductManager.WebAPI

# Terminal 2 — frontend
cd client-app
npm install
npm start
```

Then open `http://localhost:4200`. The Angular dev server proxies `/api/*` requests to the backend (`proxy.conf.json`), so no CORS setup is needed for local development — just make sure the backend is running first. See [`client-app/README.md`](./client-app/README.md) for more details on the frontend's structure and configuration.

> **Note:** The Angular CLI's production build/dev-server pipeline (`ng build` / `ng serve`) uses a native `esbuild` binary. On some Windows machines, antivirus/endpoint-security software (e.g. Bitdefender's Advanced Threat Defense) may block a freshly-downloaded, unsigned `esbuild.exe` from executing the first time, causing a `spawn EPERM` error. If you hit this, add an exclusion for the `client-app` folder (or `node_modules\@esbuild`) in your antivirus settings, or approve the security prompt if one appears, then retry `npm start`.
>
> Prefer to skip Node/npm entirely? Use [Docker](#run-with-docker) instead — it builds and serves the frontend in a container.

## Entity Framework Core Migrations

Migrations live in `ProductManager.Infrastructure/Migrations`. The `Microsoft.EntityFrameworkCore.Design` package and `dotnet-ef` tool (restored via `dotnet tool restore`) are required to run these commands. All commands are run from the repository root and target the `Infrastructure` project, using `WebAPI` as the startup project (for configuration/connection string).

**Add a new migration** (after changing an entity or configuration):

```bash
dotnet ef migrations add <MigrationName> --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI
```

**Apply pending migrations to the database:**

```bash
dotnet ef database update --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI
```

> The app also applies pending migrations automatically on startup, so this step is optional for local development.

**Remove the last (not-yet-applied) migration:**

```bash
dotnet ef migrations remove --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI
```

**List all migrations:**

```bash
dotnet ef migrations list --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI
```

**Roll back the database to a specific migration** (use `0` to revert everything):

```bash
dotnet ef database update <MigrationName> --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI
```

**Generate a SQL script for a migration** (useful for reviewing changes or deploying without `dotnet ef` on the target machine):

```bash
dotnet ef migrations script --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI --output migration.sql
```

**Drop the database entirely** (useful when resetting local dev data):

```bash
dotnet ef database drop --project ProductManager.Infrastructure --startup-project ProductManager.WebAPI
```

## Tests

The solution ships with a comprehensive automated test suite covering every layer of the Clean Architecture, from domain rules up to full HTTP request/response round-trips.

### Test projects

| Project | Type | What it covers |
|---------|------|-----------------|
| `ProductManager.Application.Tests` | Unit | Domain entities (`Product`, `User`), all MediatR command/query handlers (Products + Auth), all FluentValidation validators, and the `ValidationBehavior` pipeline |
| `ProductManager.Infrastructure.Tests` | Unit (EF Core InMemory), plus one opt-in real-SQL-Server suite | `ProductRepository`, `AuthRepository`, `ProductIdGenerator` (sequential 6-digit ID allocation + exhaustion), `PasswordHasher` (BCrypt), `JwtTokenGenerator` (claims/expiry/issuer), `DatabaseSeeder`. Also `ProductIdGeneratorConcurrencyTests` — see [below](#concurrency-test-against-a-real-sql-server) — which proves the Serializable-transaction/`UPDLOCK, ROWLOCK` locking path is safe under real concurrent access |
| `ProductManager.Presentation.Tests` | Unit | `ProductsController` and `AuthController` action methods, using a mocked `ISender` to assert the correct MediatR request is dispatched and the correct `IActionResult` (200/201/204/etc.) is returned |
| `ProductManager.WebAPI.Tests` | Unit | `ExceptionHandlingMiddleware` — verifies every exception type (`NotFoundException`, `ValidationException`, `InvalidOperationException`, `ArgumentException`, unhandled) maps to the correct HTTP status code and JSON error body |
| `ProductManager.WebAPI.Integration.Tests` | Integration (`WebApplicationFactory` + EF Core InMemory) | Full HTTP pipeline: JWT registration/login flow, protected endpoints returning 401 without/with an invalid token, complete Products CRUD lifecycle, stock management, search, stock-level filtering, and validation/not-found error responses |

Each test class in the integration suite spins up its own isolated in-memory database (a fresh `WebApplicationFactory` with a unique database name per test), so tests can run in parallel without interfering with each other.

### Running the tests

Run the entire suite:

```bash
dotnet test
```

Run a single test project:

```bash
dotnet test ProductManager.Application.Tests
dotnet test ProductManager.Infrastructure.Tests
dotnet test ProductManager.Presentation.Tests
dotnet test ProductManager.WebAPI.Tests
dotnet test ProductManager.WebAPI.Integration.Tests
```

Run with detailed output:

```bash
dotnet test --logger "console;verbosity=normal"
```

Run with code coverage (uses the built-in `coverlet.collector`):

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Concurrency test against a real SQL Server

`ProductIdGenerator` branches on `Database.IsRelational()`: against a real relational engine it
opens a `Serializable` transaction (via EF Core's execution strategy, so it survives transient
failure retries) and claims the next ID with a raw `SELECT ... WITH (UPDLOCK, ROWLOCK)` against
the `ProductIdSequences` counter row. EF Core's InMemory provider — used by every other test in
the suite, including the rest of `ProductIdGeneratorTests` — reports `IsRelational() == false`,
so it never exercises that locking path at all.

`ProductIdGeneratorConcurrencyTests` (in `ProductManager.Infrastructure.Tests`) closes that gap:
it fires 50 concurrent `GenerateNextIdAsync` calls — each with its own `AppDbContext`, mirroring
independent concurrent HTTP requests — against a real SQL Server database and asserts all 50 IDs
are distinct, in the valid 6-digit range, and that the counter lands exactly on `start + 50` with
no lost updates.

This test needs a real, reachable SQL Server, so it resolves a connection in this order and
**skips itself automatically** (rather than failing) if none is available:

1. `SQL_TEST_CONNECTION_STRING` environment variable, if set — used as-is, no fallback.
2. The docker-compose `sqlserver` service on `localhost,1433` (same sa credentials as `.env.example`):
   ```bash
   docker compose up -d sqlserver
   ```
3. SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) — usually already available on a Windows dev
   machine with Visual Studio/SQL Server tooling installed, and much faster to start than Docker.

Each run creates a uniquely-named throwaway database (`ProductManagerId_ConcurrencyTests_<guid>`),
applies migrations to it, and drops it again afterwards, so the test is fully isolated and
repeatable. Run it on its own with:

```bash
dotnet test ProductManager.Infrastructure.Tests --filter "FullyQualifiedName~ProductIdGeneratorConcurrencyTests"
```

### What's exercised

- **Happy paths** — successful create/read/update/delete, register/login, add/decrement stock, search, stock-level filtering
- **Validation failures** — empty/too-long names, non-positive prices, negative stock, invalid emails, short passwords/usernames, invalid ID ranges, invalid stock-level ranges
- **Not-found scenarios** — operating on a product ID that doesn't exist (404 across get/update/delete/stock endpoints)
- **Business rule violations** — decrementing more stock than is available (400 `InvalidOperationException`)
- **Authentication/authorization** — duplicate email/username on register, wrong password on login, missing/invalid JWT token on protected endpoints (401)
- **Infrastructure behavior** — case-insensitive search/email lookups, sequential/exhausted ID generation, BCrypt hash round-tripping, JWT claim/issuer/audience/expiry correctness, idempotent database seeding
- **Concurrency safety** — 50 parallel `ProductIdGenerator` calls against a real SQL Server never produce a duplicate ID (see [Concurrency test against a real SQL Server](#concurrency-test-against-a-real-sql-server))

## Features

- **JWT Authentication** — Secure token-based authentication with user registration and login
- **CORS Whitelist** — Configuration-driven, fail-closed cross-origin policy
- **Clean Architecture** — Domain-driven design with clear separation of concerns
- **CQRS + MediatR** — Command/Query separation with pipeline behaviors
- **Global Error Handling** — Centralized exception middleware with structured error responses
- **FluentValidation** — Comprehensive input validation with detailed error messages
- **Password Hashing** — Secure password storage using BCrypt
- **Swagger UI** — Interactive API documentation available in Development mode
- **EF Core Migrations** — Code-first database with automatic migration on startup
- **Auto-seeding** — Sample products automatically created on first run
- **Comprehensive Test Suite** — 195 unit and integration tests covering domain, application, infrastructure, presentation, and full HTTP request/response flows, including a real-SQL-Server concurrency test proving ID generation is safe under concurrent access
- **Angular Frontend** — Login page with Angular Material, route guards, JWT interceptor, and a full Products management dashboard (see [Frontend](#frontend))
- **Docker Compose** — One command spins up SQL Server, the API (auto-migrated/seeded), and the Angular frontend (see [Run with Docker](#run-with-docker))

## Notes

- **Authentication:** All product endpoints require a valid JWT token in the `Authorization: Bearer <token>` header
- **User Storage:** User credentials are stored securely with BCrypt password hashing
- **Token Expiry:** JWT tokens expire after 60 minutes (configurable in `appsettings.json`)
- Product IDs are auto-generated as unique 6-digit numbers (100,000–999,999)
- ID generation uses a database sequence with row-level locking for multi-instance safety
- The database is seeded with 5 sample products on first startup
- Swagger UI is only enabled in Development environment for security
- **Swagger + Microsoft.OpenApi v2:** Swashbuckle.AspNetCore 10.x uses `Microsoft.OpenApi` 2.x, which moved its types from the `Microsoft.OpenApi.Models` namespace straight into `Microsoft.OpenApi`, and changed `AddSecurityRequirement` to take a `document =>` delegate returning an `OpenApiSecurityRequirement` keyed by `OpenApiSecuritySchemeReference`. The JWT "Authorize" button in Swagger UI is configured accordingly in `Program.cs` and has been verified to work end-to-end.
