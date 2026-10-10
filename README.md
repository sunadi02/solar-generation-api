# Solar Generation Data API

A REST API for the Sri Lanka Sustainable Energy Authority that exposes real-time solar generation data for rooftop installations across the national grid hierarchy (Province, District, Grid Substation, Solar Installation, Generation Reading).

Coursework project for **NB6007CEM Web API Development**.

## Live deployment

| | URL |
|---|---|
| API base | https://solar-gen.runasp.net/api/v1 |
| Swagger / OpenAPI UI | https://solar-gen.runasp.net/swagger |
| OpenAPI document | https://solar-gen.runasp.net/swagger/v1/swagger.json |

The service runs on a free ASP.NET Core host with a hosted SQL Server database. The first request after a quiet period can take a few seconds while the application wakes up.

## Quick start (for markers)

1. Open https://solar-gen.runasp.net/swagger
2. Call `POST /api/v1/tokens/user` with a demo user (below) and copy the `accessToken` from the response.
3. Click **Authorize**, paste the token (without the word `Bearer`) and confirm.
4. Try `GET /api/v1/provinces`, `GET /api/v1/solar-installations` or `GET /api/v1/readings`.

### Demo accounts (seeded)

| Username | Password | Role | Can see | Scopes |
|---|---|---|---|---|
| `admin` | `Admin@123` | Admin | everything | `readings:read`, `admin:write` |
| `national` | `National@123` | National | everything | `readings:read` |
| `western` | `Western@123` | Provincial | Western province only | `readings:read` |
| `colombo` | `Colombo@123` | District | Colombo district only | `readings:read` |

### Demo meter (device token)

Metering devices authenticate with `POST /api/v1/tokens/device`:

```json
{ "meterIdentifier": "MTR-000001", "deviceSecret": "secret-MTR-000001" }
```

Every seeded meter follows the pattern `MTR-0000NN` with secret `secret-MTR-0000NN`. A device token has the single scope `installation:write` and can only submit readings for its own installation.

These credentials are demonstration data and exist only to make the API testable.

## Seed data

On first start the application applies migrations and seeds: 9 provinces, 25 districts, 30 grid substations, 240 installations (3 to 15 kW), 80,880 half-hourly readings covering seven days, and the four demo users. The seeder is idempotent, so restarts never duplicate data.

## Endpoints

All paths are under `/api/v1`.

| Method and path | Purpose | Scope |
|---|---|---|
| `POST /tokens/user`, `POST /tokens/device` | Exchange credentials for a JWT | none |
| `GET /provinces`, `/provinces/{id}`, `/provinces/{id}/districts` | Read the hierarchy | `readings:read` |
| `GET /districts/{id}`, `/districts/{id}/grid-substations`, `/grid-substations/{id}` | Read the hierarchy | `readings:read` |
| `GET /districts/{id}/generation-summary` | District aggregate | `readings:read` |
| `GET /solar-installations` | Filtered, sorted, paged list | `readings:read` |
| `GET /solar-installations/{id}` | One installation (ETag, 304) | `readings:read` |
| `POST /solar-installations` | Register an installation | `admin:write` |
| `PUT`, `PATCH`, `DELETE /solar-installations/{id}` | Replace, amend, remove (`If-Match` required) | `admin:write` |
| `GET /solar-installations/{id}/readings` | Reading history (`from`, `to`, `sort`, paging) | `readings:read` |
| `GET /solar-installations/{id}/readings/{readingId}` | One reading | `readings:read` |
| `POST /solar-installations/{id}/readings` | Submit one reading | `installation:write` |
| `GET /solar-installations/{id}/last-reading` | Latest reading | `readings:read` |
| `GET /solar-installations/{id}/overview` | Installation, location and latest reading | `readings:read` |
| `GET /readings` | Readings across installations in the caller's territory | `readings:read` |

## Design notes

- **Richardson Level 2.** Resources have their own URIs, methods keep their HTTP meaning, and status codes are precise (201 with `Location`, 204, 304, 400, 401, 403, 404, 405, 406, 409, 412, 415, 428). Collection responses carry paging links.
- **Collections** take `page` and `page_size` (default 20, maximum 100) and return `{ total, page, pageSize, items, links }`. Sorting accepts only whitelisted fields.
- **Jurisdiction scoping.** A provincial or district user only sees data inside their territory. The filter is applied inside the database query. A missing resource returns `404`, and an existing one outside the caller's territory returns `403 OUT_OF_JURISDICTION`.
- **Conditional requests.** Single installations return an `ETag`. `PUT`, `PATCH` and `DELETE` need a matching `If-Match` header (`428` if missing, `412` if stale).
- **Errors** share one body: `{ "code": "...", "message": "...", "details": ... }`.
- **Security.** JWT bearer tokens (HMAC-SHA256, one hour), BCrypt hashes for passwords and device secrets, scope-based authorization policies, parameterised queries through Entity Framework Core.

## Run locally

Requirements: .NET 8 SDK and SQL Server (or SQL Server Express).

```bash
git clone https://github.com/sunadi02/solar-generation-api.git
cd solar-generation-api
```

Set `ConnectionStrings:DefaultConnection` and `Jwt:Key` (at least 32 characters) in `appsettings.Development.json` or through environment variables, for example:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=SolarGenerationDb;Trusted_Connection=True;TrustServerCertificate=True;"
export Jwt__Key="replace-with-a-long-random-development-key-0123456789"
dotnet run
```

The application creates the schema and seed data on start. Swagger is served at `/swagger`.

## Project structure

```
Controllers/   HTTP endpoints
Services/      TokenService, JurisdictionService, paging links
Data/          DbContext, seeder, UTC date converter
Models/        Entities
Dtos/          Request and response contracts
Errors/        Shared error contract
Security/      Claim helpers
Migrations/    EF Core migrations (generated)
```

## Configuration and secrets

`appsettings.json` holds development placeholders only. The deployed service overrides the connection string and `Jwt:Key` with a server-side `appsettings.Production.json` that is excluded from Git.

## Testing

Endpoints were exercised manually through Swagger UI and PowerShell, covering success, validation errors, missing tokens, out-of-territory access, duplicate readings and stale ETags. There is no automated test suite.
