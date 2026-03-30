# Fantasy Server — Claude Code Guide

## Solution Overview

.NET 10 single-solution backend:

| Project | Type | Role |
|---|---|---|
| `Fantasy.Server` | ASP.NET Core Web API | Authentication, account management, game service |
| `Fantasy.Test` | xUnit Test Project | Unit tests for services |

## Tech Stack

| Package | Version | Context7 ID |
|---|---|---|
| .NET / ASP.NET Core | 10.0 | `/dotnet/docs` |
| `Microsoft.EntityFrameworkCore` | 10.0.5 | `/dotnet/docs` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.1 | `/npgsql/efcore.pg` |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 10.0.5 | `/stackexchange/stackexchange.redis` |
| `StackExchange.Redis` | 2.12.8 | `/stackexchange/stackexchange.redis` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.5 | `/dotnet/docs` |
| `System.IdentityModel.Tokens.Jwt` | 8.17.0 | `/dotnet/docs` |
| `BCrypt.Net-Next` | 4.1.0 | — (no Context7 entry) |
| `Gamism.SDK.Extensions.AspNetCore` | 0.2.8 | — (no Context7 entry) |
| `xunit.v3` | 3.2.2 | `/xunit/xunit.net` |
| `NSubstitute` | 5.3.0 | `/nsubstitute/nsubstitute` |
| `FluentAssertions` | 8.9.0 | `/fluentassertions/fluentassertions` |

## Context7 Usage

When working with any library listed above, use the Context7 MCP to fetch version-accurate official documentation before writing or modifying code.

```
# Example: EF Core Fluent API
mcp__context7__query-docs(libraryId: "/dotnet/docs", query: "EF Core migration fluent API", version: "10.0")

# Example: Npgsql EF Core setup
mcp__context7__query-docs(libraryId: "/npgsql/efcore.pg", query: "UseNpgsql configuration")

# Example: xUnit v3 test patterns
mcp__context7__query-docs(libraryId: "/xunit/xunit.net", query: "Fact Theory async test")
```

`BCrypt.Net-Next` and `Gamism.SDK.Extensions.AspNetCore` have no Context7 entry — refer to the source code directly.
