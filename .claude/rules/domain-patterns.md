---
description: Domain layer patterns (Service, Repository, Controller). Applied when working on Domain/** files.
globs: ["Fantasy.Server/Domain/**"]
alwaysApply: false
---

## Patterns

- **Service**: single interface + single `ExecuteAsync` method per use case.
- **Repository**: interface in `Repository/Interface/`, impl in `Repository/`. Only repositories touch `AppDbContext`. Use `AsNoTracking()` for reads.
- **Controller**: inject service interfaces only. Apply `[EnableRateLimiting("policy-name")]` where needed.

## Gamism.SDK

Controllers can return DTOs directly — `ApiResponseWrapperFilter` auto-wraps them into `CommonApiResponse<T>`.

| Return | HTTP | Result |
|---|---|---|
| Plain object | 200 | `CommonApiResponse.Success("OK", value)` |
| `void` / `null` | 204 | Empty |
| `CommonApiResponse` | `code` value | Passed through as-is |

**Error handling — always `throw`, never `return`:**

```csharp
// ❌ breaks CommonApiResponse format
return NotFound();

// ✅
throw new NotFoundException("Player not found.");
```

Exception classes: `BadRequestException` (400), `UnauthorizedException` (401), `ForbiddenException` (403), `NotFoundException` (404), `ConflictException` (409). Use `ExpectedException(HttpStatusCode, message)` for anything else.
