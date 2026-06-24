---
name: db-migrate
description: Manages EF Core migrations. Supports add/update/list/remove subcommands. e.g. /db-migrate add CreateAccountTable
argument-hint: [add <MigrationName> | update | list | remove]
allowed-tools: Bash(dotnet ef migrations:*), Bash(dotnet ef database:*), AskUserQuestion
context: fork
---

Manage EF Core migrations for Fantasy.Server.

**Project**: `Fantasy.Server/Fantasy.Server.csproj`
**DbContext**: `AppDbContext` (`Global/Infrastructure/AppDbContext.cs`)

## Current migrations state

!`dotnet ef migrations list --project Fantasy.Server/Fantasy.Server.csproj 2>&1 || echo "(no migrations yet)"`

---

## Dispatch on $ARGUMENTS

Parse the first word of `$ARGUMENTS` as the subcommand.

---

### `add <MigrationName>`

1. Validate that a migration name was provided. If missing, use AskUserQuestion to ask:
   > "마이그레이션 이름을 입력해주세요. (예: CreateAccountTable)"
2. Run:
   ```bash
   dotnet ef migrations add {MigrationName} --project Fantasy.Server/Fantasy.Server.csproj
   ```
3. Report the generated files under `Fantasy.Server/Migrations/`.
4. Remind the user to review the generated `Up()` / `Down()` methods before applying.

---

### `update`

1. Show pending migrations from the list above (migrations not yet applied).
2. If there are no pending migrations, report "적용할 마이그레이션이 없습니다." and stop.
3. If there are pending migrations, run:
   ```bash
   dotnet ef database update --project Fantasy.Server/Fantasy.Server.csproj
   ```
4. Confirm success or surface any connection/schema errors.

---

### `list`

Run:
```bash
dotnet ef migrations list --project Fantasy.Server/Fantasy.Server.csproj
```
Display the output, marking applied migrations with ✓ and pending ones with ○.

---

### `remove`

1. Warn the user:
   > "마지막 마이그레이션을 삭제합니다. DB에 이미 적용된 경우 먼저 rollback이 필요합니다. 계속할까요?"
2. Use AskUserQuestion to confirm.
3. If confirmed, run:
   ```bash
   dotnet ef migrations remove --project Fantasy.Server/Fantasy.Server.csproj
   ```
4. Report which files were deleted.

---

### Unknown subcommand

If `$ARGUMENTS` is empty or doesn't match any subcommand, show:
```
사용법: /db-migrate [add <MigrationName> | update | list | remove]
```
