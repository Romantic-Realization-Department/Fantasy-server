---
paths:
  - "**/appsettings*.json"
  - "**/compose*.yaml"
  - "**/*.csproj"
---

# Configuration Rules

- Do not commit real secrets. Keep database, Redis, and JWT secrets in environment variables or deploy-time configuration.
- `appsettings.Development.json` may contain local defaults only. Shared or production settings must come from environment variables, compose files, or secret storage.
- Keep connection string keys consistent with the application code:
  - `ConnectionStrings:Database`
  - `ConnectionStrings:Redis`
  - `Jwt:SecretKey`
- When Docker compose files are changed, keep service names and connection strings aligned with `Fantasy.Server` runtime settings.
- Prefer adding new configuration keys explicitly in all relevant `appsettings*.json` files when the app expects them at startup.
