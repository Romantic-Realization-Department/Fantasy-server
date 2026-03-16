# Fantasy Server — Claude Code Guide

## Project Overview

A .NET 10 multi-project backend solution composed of three projects:

| Project | Type | Role |
|---|---|---|
| `Fantasy.Common` | Class Library | Shared domain models, DTOs, interfaces, infrastructure, and config |
| `Fantasy.Auth` | ASP.NET Core Web API | Authentication service (signup, login, token management) |
| `Fantasy.Game` | ASP.NET Core Web API | Game service (in development) |

Both `Fantasy.Auth` and `Fantasy.Game` reference `Fantasy.Common`.

---

## Architecture

### Domain Structure

Each domain follows this layout inside a service project:

```
Domain/{DomainName}/
  Controller/         # ASP.NET Core controllers
  Service/            # Service implementations
    Interface/        # Service interfaces
  Repository/         # Repository implementations
```

Interfaces for repositories and services are defined in `Fantasy.Common`. Implementations live in the consuming service project (`Fantasy.Auth`, `Fantasy.Game`).

### Layering Rules

- **Controllers** depend on service interfaces, never concrete services.
- **Services** depend on repository interfaces, never `AppDbContext` directly.
- **Repositories** are the only layer that touches `AppDbContext`.
- All interfaces (repository + service) are defined in `Fantasy.Common`, not in the implementing project.

### Service Pattern

Services use a single `ExecuteAsync` method:

```csharp
public interface ICreateAccountService
{
    Task ExecuteAsync(CreateAccountRequest request);
}
```

### Repository Pattern

Interface in `Fantasy.Common/Domain/{DomainName}/Repository/`. Implementation in `Fantasy.Auth/Domain/{DomainName}/Repository/`.

---

## Tech Stack

- **.NET 10** / ASP.NET Core Web API
- **PostgreSQL** via EF Core 10 + Npgsql
- **Redis** via StackExchange.Redis (used for refresh token storage)
- **JWT** — `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt`
- **BCrypt.Net-Next** — password hashing
- **ASP.NET Core Rate Limiting** — built-in fixed window limiter

---

## Key Conventions

### Naming

- Database table names: `snake_case`, schema-qualified (e.g., `"account"."account"`)
- `AccountRole` enum is stored as a string in the database.
- When using the `Account` entity inside `Fantasy.Auth`, alias it to avoid namespace collision:
  ```csharp
  using AccountEntity = Fantasy.Common.Domain.Account.Entity.Account;
  ```

### DTOs

- Use `record` types with `DataAnnotations` for validation.
- Requests go in `Dto/Request/`, responses in `Dto/Response/`.

### Entity Configuration

- Use EF Core Fluent API via `IEntityTypeConfiguration<T>` in `Fantasy.Common/Domain/{DomainName}/Entity/Config/`.
- `AppDbContext` applies all configurations automatically via `ApplyConfigurationsFromAssembly`.

### Config Extension Methods

All infrastructure registrations are implemented as `IServiceCollection` extension methods in `Fantasy.Common/Global/Config/`:

| Class | Method | Purpose |
|---|---|---|
| `DatabaseConfig` | `AddDatabase` | EF Core + PostgreSQL |
| `RedisConfig` | `AddRedis` | StackExchange.Redis + IDistributedCache |
| `JwtConfig` | `AddJwtAuthentication` | JWT Bearer auth |
| `RateLimitConfig` | `AddRateLimit` | Fixed window rate limiters |

Call these from `Program.cs` in each service project.

### JWT

`JwtConfig` provides three static members:
- `AddJwtAuthentication(services, configuration)` — registers JWT Bearer auth middleware.
- `GenerateAccessToken(account, configuration)` — issues a signed JWT with `sub`, `email`, `role`, `jti` claims.
- `GenerateRefreshToken()` — returns a cryptographically random Base64 string.

Required `appsettings.json` keys:
```json
"Jwt": {
  "SecretKey": "...",
  "Issuer": "...",
  "Audience": "...",
  "AccessTokenExpirationMinutes": "15"
}
```

### Refresh Tokens

Stored in Redis via `IRefreshTokenRedisRepository`, keyed by account `Id` (long), with a TTL.

### Rate Limiting

Two named policies defined in `RateLimitConfig`:
- `"login"` — 5 requests per minute
- `"game"` — 30 requests per second

Apply with `[EnableRateLimiting("login")]` on controllers or actions.

### Password Hashing

Always hash with `BCrypt.Net.BCrypt.HashPassword()` before persisting. Verify with `BCrypt.Net.BCrypt.Verify()`.

---

## Adding a New Domain

1. Define the entity in `Fantasy.Common/Domain/{Name}/Entity/`.
2. Add EF config in `Fantasy.Common/Domain/{Name}/Entity/Config/`.
3. Register `DbSet<T>` in `AppDbContext`.
4. Define DTOs (records) in `Fantasy.Common/Domain/{Name}/Dto/Request|Response/`.
5. Define the repository interface in `Fantasy.Common/Domain/{Name}/Repository/`.
6. Define service interface(s) in the service project's `Domain/{Name}/Service/Interface/`.
7. Implement the repository and service in the relevant service project.
8. Wire up DI in that project's `Program.cs`.
