# Customer Support API

ASP.NET Core 8 Web API for a customer-support desk: accounts, customers, tickets, assignment, status history, and messages.

The API is organized as a layered backend (domain, application, infrastructure, host). Identity lives in its own SQL database. Logout revocation uses Redis when it is configured.

## Features

- JWT login, signup, refresh, and logout
- Access-token blacklist on logout (Redis, or in-memory if Redis is not set)
- Roles: **Admin**, **Agent**, **Customer**
- Customer records linked to Identity users on signup
- Tickets with priority, assignment, status workflow, messages, and status history
- Customer-scoped ticket access (customers only see their own tickets)
- User, role, and user-role management
- EF Core migrations applied on startup
- Swagger UI in Development
- Docker Compose: API + SQL Server + Redis

## Tech stack

| Area | Choice |
|---|---|
| Runtime | .NET 8 |
| API | ASP.NET Core Web API |
| Auth | ASP.NET Core Identity + JWT Bearer |
| ORM | Entity Framework Core |
| Database | SQL Server (two databases) |
| Cache | Redis (`IDistributedCache`) |
| Validation | FluentValidation |
| Docs | Swashbuckle / Swagger |

## Architecture

```
CustomerSupport/                  HTTP host, controllers, Swagger, Program.cs
Application/                      Application services and validators
Application.Contrast/             Service contracts, roles, policies
Application.Dto/                  Request and response models
Domain.Customer/                  Customer and ticket entities and enums
Customer.DomainServiceContract/   Repository contracts
Infrastructure.Customer.Persistance/   Customer DB, repositories, queries, migrations
Infrastructure.Security.Identity/      Identity DB, JWT, blacklist, seeding
Customer.Bootstrap/               Customer DI and migrations
Security.Bootstrap/               Identity, JWT, and authorization DI
Applicatio.Freamwork/             Shared OperationResult and paging helpers
```

Two SQL databases are used:

| Database | Purpose |
|---|---|
| `CustomerSupport` | Customers, tickets, messages, status history |
| `CustomerSupportSecurity` | Identity users, roles, refresh tokens |

## Prerequisites

**Docker (recommended)**

- Docker Desktop

**Local run**

- .NET 8 SDK
- SQL Server (LocalDB, Express, or full)
- Redis optional — without it, logout blacklist uses an in-memory cache

## Quick start with Docker

From the repository root:

```bash
docker compose up --build
```

| Service | URL / port |
|---|---|
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |
| SQL Server | `localhost,1434` (SA password in `docker-compose.yml`) |
| Redis | `localhost:6379` |

On first start the API waits for SQL Server, applies migrations, and seeds roles plus an admin user.

Stop with `Ctrl+C`, or `docker compose down`. Add `-v` only if you want to wipe the SQL and Redis volumes.

## Run locally

1. Create the two databases (or let startup create them via migrations) and set connection strings in `CustomerSupport/appsettings.json`.
2. From the repository root:

```bash
dotnet restore
dotnet run --project CustomerSupport
```

| Profile | URL |
|---|---|
| HTTP | http://localhost:5125 |
| HTTPS | https://localhost:7102 |
| Swagger | http://localhost:5125/swagger |

## Seeded admin

Startup creates **Admin**, **Agent**, and **Customer** roles, then an admin from `Seed:Admin` in `CustomerSupport/appsettings.json`.

This repository currently seeds:

| Field | Value |
|---|---|
| UserName | `amirhassan` |
| Email | `hs@customersupport.local` |
| Password | `amir1234` |

If those keys are missing, the fallback is `admin` / `Admin1234`.

Passwords must be at least 8 characters and include a lowercase letter.

These credentials are for local development only. Change them before any shared or production environment.

## Authentication

Public endpoints (no token):

```http
POST /api/Authentication/signup
POST /api/Authentication/login
POST /api/Authentication/refresh
POST /api/Authentication/logout
GET  /health
```

Login accepts username or email:

```json
{
  "userName": "amirhassan",
  "password": "amir1234"
}
```

Response:

```json
{
  "item": {
    "accessToken": "...",
    "refreshToken": "..."
  }
}
```

Send the access token as `Authorization: Bearer <token>`.

| Token | Lifetime |
|---|---|
| Access | 60 minutes (`jwt:DurationInMinutes`) |
| Refresh | 7 days |

Signup creates an Identity user with the **Customer** role and links or creates a customer record (match by `customerId` or email).

Logout revokes the refresh token and blacklists the current access token (`jti`) until it expires.

## Roles and access

| Policy | Roles | Typical use |
|---|---|---|
| `AdminOnly` | Admin | Users, roles, customer create/update/delete |
| `Staff` | Admin, Agent | Customer read, ticket queue, assign, status |
| `TicketAccess` | Admin, Agent, Customer | Open and follow tickets |

Customers can only read and message tickets that belong to their linked `customerId`. Staff can search the queue, assign agents, and change status.

## Ticket workflow

Statuses: `Open` → `Assigned` → `InProgress` → `WaitingForCustomer` → `Resolved` → `Closed`

Priorities: `Low`, `Medium`, `High`, `Critical`

Customer types: `Individual`, `Company`  
Customer statuses: `Active`, `Inactive`

## API map

| Area | Base route | Notes |
|---|---|---|
| Auth | `/api/Authentication` | login, signup, refresh, logout |
| Customers | `/api/Customer` | CRUD, search, status |
| Tickets | `/api/Ticket` | CRUD, assign, status, messages, search |
| Users | `/api/UserManagement` | `GET me`, admin search/update/delete |
| Roles | `/api/RoleManagement` | Admin only |
| User roles | `/api/UserRoleManagement` | assign / remove |
| Health | `/health` | process + cache ping |

Use Swagger for request bodies, enums, and `Authorize`.

## Configuration

`CustomerSupport/appsettings.json`:

| Key | Purpose |
|---|---|
| `ConnectionStrings:CustomerSupport` | Customer database |
| `ConnectionStrings:Security` | Identity database |
| `ConnectionStrings:Redis` | Empty = in-memory cache |
| `jwt:SecretKey` | HMAC key (32+ characters) |
| `jwt:Issuer` / `jwt:Audience` | Token validation |
| `jwt:DurationInMinutes` | Access-token lifetime |
| `Seed:Admin:*` | First admin user |
| `DisableHttpsRedirection` | Set by Docker Compose |

Compose overrides connection strings and JWT settings for the container network. Do not commit production secrets. Prefer environment variables or a secret store.

## License

No license file is included. Treat the repository as private unless you add one.
