[Back to README](../README.md)

## Running the System

Use this runbook to execute the backend stack (WebApi + PostgreSQL + MongoDB + Redis) consistently across development, production-like runs, and integration tests. **Why** the stack is shaped this way (Mongo history, JWT, dev seed) is documented in [Technical decisions](./technical-decisions.md).

### Prerequisites

1. Install Docker and Docker Compose.
2. Install .NET SDK 8.0 (required for local test execution).
3. Ensure ports `8080`, `5432`, `27017`, and `6379` are available locally.

### 1) Development Environment (Docker Compose)

1. Create your development env file from the example:
   - PowerShell: `Copy-Item backend/.env.example backend/.env.development`
2. Update local values/secrets in `backend/.env.development`.
3. Start the stack:
   - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml up -d --build`
4. Verify service health:
   - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml ps`
   - Expected result: `ambev.developerevaluation.webapi` is `healthy`.
5. Open the API in your browser:
   - Swagger UI: `http://localhost:8080/swagger` (or `http://localhost:8080/swagger/index.html`)
   - Health check: `http://localhost:8080/health`
   - API routes are under `/api/...` (example: `http://localhost:8080/api/sales`)
   - Note: `http://localhost:8080/` may return 404 because there is no mapped homepage route.
   - If Swagger does not load, confirm the WebApi container is running with `ASPNETCORE_ENVIRONMENT=Development` (the dev compose stack should).
6. **Development bootstrap user and JWT (Docker / Development only)** — see [Development bootstrap user](./technical-decisions.md#development-bootstrap-user) in *Technical decisions* for rationale and file locations.  
   On first startup in **Development**, the WebApi seeds a single user if one with the seed e-mail does not exist yet. The e-mail and password are defined in code as constants on the class **`DevelopmentAuthSeed`** in the backend Common project:
   - Source: [`backend/src/Ambev.DeveloperEvaluation.Common/Security/DevelopmentAuthSeed.cs`](../backend/src/Ambev.DeveloperEvaluation.Common/Security/DevelopmentAuthSeed.cs) (`Ambev.DeveloperEvaluation.Common.Security.DevelopmentAuthSeed`)
   - Default values: **`DevelopmentAuthSeed.Email`** = `dev@local.test`, **`DevelopmentAuthSeed.Password`** = `DevSeed_P@ssw0rd!`  
   Treat these as **local/dev credentials only**. They are not intended for production; the seed runs only when `ASPNETCORE_ENVIRONMENT=Development` (as in this Compose stack).
7. **Calling protected routes and Swagger** — see [API security and Swagger](./technical-decisions.md#api-security-and-swagger) in *Technical decisions*.  
   - Almost all HTTP API endpoints require a **JWT** (`Authorization: Bearer <token>`). The public exception is **`POST /api/Auth`** (login).  
   - In Swagger UI, use **`POST /api/Auth`** with the seed e-mail/password to obtain a token, then click **Authorize**, choose the Bearer scheme, and enter `Bearer <your_token>` (or only the token, depending on Swagger UI version).  
   - Endpoints that require auth show a lock icon; unlocked routes are those marked anonymous (login).
8. Stop and remove containers:
   - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml down`

### Authentication and authorization (summary)

| Topic | Behavior |
| --- | --- |
| Public route | `POST /api/Auth` (no JWT required). |
| Other routes | Require a valid JWT (global fallback authorization policy). |
| Dev seed user | Created automatically in Development using `DevelopmentAuthSeed` constants (see step 6 under Docker Compose). |
| Changing dev credentials | Update `DevelopmentAuthSeed` and restart the WebApi so the seed logic can align with your local workflow (or create additional users via `POST /api/users` while authenticated as the seed user). |

### 2) Production-like Execution

Use this flow for local production-like validation with Compose (not a full production deployment guide).

1. Provide environment values through a dedicated file (for example `backend/.env.production`) and keep secrets outside source control.
2. Start the stack in detached mode:
   - `docker compose --env-file backend/.env.production -f backend/docker-compose.yml -f backend/docker-compose.override.yml up -d --build`
3. Verify service status and health:
   - `docker compose --env-file backend/.env.production -f backend/docker-compose.yml -f backend/docker-compose.override.yml ps`
4. Inspect runtime logs when needed:
   - `docker compose --env-file backend/.env.production -f backend/docker-compose.yml -f backend/docker-compose.override.yml logs ambev.developerevaluation.webapi`
5. Stop the stack:
   - `docker compose --env-file backend/.env.production -f backend/docker-compose.yml -f backend/docker-compose.override.yml down`

### 3) Running Integration Tests

1. Start the stack:
   - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml up -d --build`
2. Wait until WebApi is healthy:
   - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml ps`
   - Expected result: `ambev.developerevaluation.webapi` is `healthy`.
3. Run integration tests:
   - `dotnet test backend/tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj`  
   The integration harness logs in with the same **Development** seed user (`DevelopmentAuthSeed.Email` / `DevelopmentAuthSeed.Password`) and sends `Authorization: Bearer` on subsequent requests, so the stack must have completed migrations and seed at least once while healthy.
4. Stop and clean containers:
   - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml down`

### Troubleshooting

- If WebApi does not become `healthy`, check logs first:
  - `docker compose --env-file backend/.env.development -f backend/docker-compose.yml -f backend/docker-compose.override.yml logs ambev.developerevaluation.webapi`
- If integration tests fail due to startup race conditions, rerun after confirming all services are healthy in `ps`.
- If the browser shows 404 on `/` but the container is healthy, use `/swagger` or `/health` instead (root is not mapped).
- If API calls return **401 Unauthorized**, obtain a JWT via `POST /api/Auth` with the Development seed credentials (see `DevelopmentAuthSeed`) and send `Authorization: Bearer <token>` on protected routes.
- `docker-compose.yml` is parameterized and should not contain hardcoded secrets.
- ASP.NET Core environment variables (for example `ConnectionStrings__DefaultConnection`, `Mongo__ConnectionString`) override `appsettings.json`.
