---
paths:
  - "**/*.cs"
  - "**/appsettings*.json"
  - "**/compose*.yaml"
---

# Security Rules

- Never hardcode secrets such as JWT secret keys, database passwords, or Redis credentials in source files.
- Passwords must be hashed with the project's approved hashing approach before persistence.
- Do not log passwords, refresh tokens, access tokens, or raw authorization headers.
- Authenticated endpoints must be explicit about authorization requirements.
- When changing Docker or config files, ensure production-sensitive defaults are not committed as real credentials.
