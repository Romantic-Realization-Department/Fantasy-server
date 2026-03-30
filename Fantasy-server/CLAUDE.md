# Fantasy Server — Claude Code Guide

## Project Overview

A .NET 10 single-project backend solution:

| Project | Type | Role |
|---|---|---|
| `Fantasy.Server` | ASP.NET Core Web API | Authentication, account management, and game service |
| `Fantasy.Test` | xUnit Test Project | Unit tests for services |

---

## Architecture

### Directory Structure

```
Fantasy.Server/
├── Domain/
│   ├── {DomainName}/
│   │   ├── Config/         # DI registration extension methods
│   │   ├── Controller/     # ASP.NET Core controllers
│   │   ├── Dto/
│   │   │   ├── Request/
│   │   │   └── Response/
│   │   ├── Entity/
│   │   │   └── Config/     # EF Core Fluent API configurations
│   │   ├── Repository/
│   │   │   └── Interface/  # Repository interfaces
│   │   └── Service/
│   │       └── Interface/  # Service interfaces
└── Global/
    ├── Config/             # Infrastructure registrations (DB, Redis, JWT, RateLimit)
    ├── Constant/           # Global constants
    ├── Controller/         # Global endpoints (e.g., health check)
    ├── Infrastructure/     # AppDbContext
    └── Security/           # JWT provider, auth filter, current user provider
        ├── Config/
        ├── Filter/
        ├── Jwt/
        └── Provider/
```

### Layering Rules

- **Controllers** depend on service interfaces, never concrete services.
- **Services** depend on repository interfaces, never `AppDbContext` directly.
- **Repositories** are the only layer that touches `AppDbContext`.
- Interfaces (repository + service) are co-located within their domain, not in a separate project.

### Service Pattern

Services use a single `ExecuteAsync` method:

```csharp
public interface ICreateAccountService
{
    Task ExecuteAsync(CreateAccountRequest request);
}
```

### Repository Pattern

Interface in `Domain/{DomainName}/Repository/Interface/`. Implementation in `Domain/{DomainName}/Repository/`.

### Domain DI Registration

Each domain has a `Config/` class with an extension method that registers its services:

```csharp
// Domain/Account/Config/AccountServiceConfig.cs
public static class AccountServiceConfig
{
    public static IServiceCollection AddAccountServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICreateAccountService, CreateAccountService>();
        return services;
    }
}
```

Call all domain configs from `Program.cs`.

---

## Tech Stack

- **.NET 10** / ASP.NET Core Web API
- **PostgreSQL** via EF Core 10 + Npgsql
- **Redis** via StackExchange.Redis (used for refresh token storage)
- **JWT** — `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt`
- **BCrypt.Net-Next** — password hashing
- **ASP.NET Core Rate Limiting** — built-in fixed window limiter
- **Gamism.SDK.Extensions.AspNetCore** — Swagger, structured logging, response wrapping

---

## Key Conventions

### Naming

- Database table names: `snake_case`, schema-qualified (e.g., `"account"."account"`)
- `AccountRole` enum is stored as a string in the database.

### DTOs

- Use `record` types with `DataAnnotations` for validation.
- Requests go in `Dto/Request/`, responses in `Dto/Response/`.

### Entity Configuration

- Use EF Core Fluent API via `IEntityTypeConfiguration<T>` in `Domain/{DomainName}/Entity/Config/`.
- `AppDbContext` applies all configurations automatically via `ApplyConfigurationsFromAssembly`.

### Global Config Extension Methods

All infrastructure registrations are `IServiceCollection` extension methods in `Global/Config/`:

| Class | Method | Purpose |
|---|---|---|
| `DatabaseConfig` | `AddDatabase` | EF Core + PostgreSQL |
| `RedisConfig` | `AddRedis` | StackExchange.Redis + IDistributedCache |
| `JwtConfig` | `AddJwtAuthentication` | JWT Bearer auth middleware |
| `RateLimitConfig` | `AddRateLimit` | Fixed window rate limiters |

### JWT

Token generation is handled by `IJwtProvider` / `JwtProvider` in `Global/Security/Jwt/`:
- `GenerateAccessToken(account)` — issues a signed JWT with `sub`, `email`, `role`, `jti`, `iat` claims.
- `GenerateRefreshToken()` — returns a cryptographically random Base64 string.

`JwtConfig.AddJwtAuthentication` registers JWT Bearer auth middleware only.

Required `appsettings.json` keys:
```json
"Jwt": {
  "SecretKey": "...",
  "Issuer": "...",
  "Audience": "...",
  "AccessTokenExpirationMinutes": "15"
}
```

### Security Services

Registered via `SecurityServiceConfig.AddSecurityServices()` in `Global/Security/Config/`:
- `IJwtProvider` / `JwtProvider` — token generation
- `ICurrentUserProvider` / `CurrentUserProvider` — extracts current user from JWT claims
- `JwtAuthenticationFilter` — validates JWT from Authorization header

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

1. Define the entity in `Domain/{Name}/Entity/`.
2. Add EF config in `Domain/{Name}/Entity/Config/`.
3. Register `DbSet<T>` in `AppDbContext`.
4. Define DTOs (records) in `Domain/{Name}/Dto/Request|Response/`.
5. Define the repository interface in `Domain/{Name}/Repository/Interface/`.
6. Define service interface(s) in `Domain/{Name}/Service/Interface/`.
7. Implement the repository and service in `Domain/{Name}/Repository/` and `Domain/{Name}/Service/`.
8. Create `Domain/{Name}/Config/{Name}ServiceConfig.cs` with a DI extension method.
9. Call the extension method from `Program.cs`.
