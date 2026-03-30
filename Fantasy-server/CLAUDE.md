# Fantasy Server — Claude Code Guide

## Solution Overview

.NET 10 single-solution backend:

| Project | Type | Role |
|---|---|---|
| `Fantasy.Server` | ASP.NET Core Web API | Authentication, account management, game service |
| `Fantasy.Test` | xUnit Test Project | Unit tests for services |

## Tech Stack

- **.NET 10** / ASP.NET Core Web API
- **PostgreSQL** via EF Core 10 + Npgsql
- **Redis** via StackExchange.Redis (refresh token storage)
- **JWT** — `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt`
- **BCrypt.Net-Next** — password hashing
- **ASP.NET Core Rate Limiting** — built-in fixed window limiter
- **Gamism.SDK.Extensions.AspNetCore** — Swagger, structured logging, response wrapping
