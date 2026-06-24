---
description: Global infrastructure patterns (JWT, Security, Redis, RateLimit). Applied when working on Global/** files.
globs: ["Fantasy.Server/Global/**"]
alwaysApply: false
---

## Global Config Extension Methods

All infrastructure registrations are `IServiceCollection` extension methods in `Global/Config/`:

| Class | Method | Purpose |
|---|---|---|
| `DatabaseConfig` | `AddDatabase` | EF Core + PostgreSQL |
| `RedisConfig` | `AddRedis` | StackExchange.Redis + IDistributedCache |
| `JwtConfig` | `AddJwtAuthentication` | JWT Bearer auth middleware |
| `RateLimitConfig` | `AddRateLimit` | Fixed window rate limiters |

## JWT

Token generation is handled by `IJwtProvider` / `JwtProvider` in `Global/Security/Jwt/`:

- `GenerateAccessToken(account)` — signed JWT with `sub`, `email`, `role`, `jti`, `iat` claims.
- `GenerateRefreshToken()` — cryptographically random Base64 string.

`JwtConfig.AddJwtAuthentication` registers Bearer middleware only (not token generation).

Required `appsettings.json` keys:

```json
"Jwt": {
  "SecretKey": "...",
  "Issuer": "...",
  "Audience": "...",
  "AccessTokenExpirationMinutes": "15"
}
```

## Security Services

Registered via `SecurityServiceConfig.AddSecurityServices()` in `Global/Security/Config/`:

- `IJwtProvider` / `JwtProvider` — token generation
- `ICurrentUserProvider` / `CurrentUserProvider` — extracts current user from JWT claims
- `JwtAuthenticationFilter` — validates JWT from Authorization header

## Refresh Tokens

Stored in Redis via `IRefreshTokenRedisRepository`, keyed by account `Id` (long), with TTL.

## Rate Limiting

Two named policies in `RateLimitConfig`:

- `"login"` — 5 requests per minute
- `"game"` — 30 requests per second

Apply with `[EnableRateLimiting("login")]` on controllers or actions.
