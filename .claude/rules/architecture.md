---
description: Overall architecture, directory structure, and layering rules. Always applied.
globs:
alwaysApply: true
---

## Directory Structure

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
│   │   │   └── Interface/
│   │   └── Service/
│   │       └── Interface/
└── Global/
    ├── Config/             # Infrastructure registrations (DB, Redis, JWT, RateLimit)
    ├── Constant/
    ├── Controller/         # Global endpoints (e.g., health check)
    ├── Infrastructure/     # AppDbContext
    └── Security/
        ├── Config/
        ├── Filter/
        ├── Jwt/
        └── Provider/
```

## Layering Rules

- **Controllers** depend on service interfaces only — never concrete services.
- **Services** depend on repository interfaces only — never `AppDbContext` directly.
- **Repositories** are the only layer that touches `AppDbContext`.
- Interfaces (repository + service) are co-located within their domain folder.

## Adding a New Domain

1. Define entity in `Domain/{Name}/Entity/`
2. Add EF Fluent config in `Domain/{Name}/Entity/Config/`
3. Register `DbSet<T>` in `AppDbContext`
4. Define DTOs (records) in `Domain/{Name}/Dto/Request/` and `Dto/Response/`
5. Define repository interface in `Domain/{Name}/Repository/Interface/`
6. Define service interface(s) in `Domain/{Name}/Service/Interface/`
7. Implement repository and service
8. Create `Domain/{Name}/Config/{Name}ServiceConfig.cs` with DI extension method
9. Call the extension method from `Program.cs`
