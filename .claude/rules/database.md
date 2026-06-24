---
paths:
  - "Fantasy.Server/Migrations/**/*.cs"
  - "Fantasy.Server/Domain/**/Entity/Config/*.cs"
  - "Fantasy.Server/Global/Infrastructure/AppDbContext*.cs"
---

# Database Rules

- This project uses EF Core migrations as the source of truth for schema changes. Do not rewrite committed migrations unless explicitly required and safe.
- Add a new migration for schema changes instead of editing old migration history.
- Keep entity-to-table mapping in Fluent API config classes under `Domain/{Name}/Entity/Config/`.
- Use `snake_case` table names and the existing schema names such as `account`, `player`, and `game_data`.
- Keep entities free of persistence-specific DataAnnotations when the same rule can be expressed with Fluent API.
- When adding new entities, ensure all of the following stay in sync:
  - `DbSet<T>` in `AppDbContext`
  - entity config class
  - generated EF migration
- Docker init scripts are only for database bootstrap such as schema creation or grants. They do not replace EF Core migrations.
