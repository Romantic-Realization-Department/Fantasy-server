---
name: code-review
description: Run a structured checklist over changed files against project conventions (architecture, code style, EF Core, DI, testing). Produces a ✓/⚠/✗ report in Korean.
allowed-tools: Bash(git diff:*), Bash(git log:*), Bash(git branch:*), Read, Glob, Grep
context: fork
---

# Code Review

Review changed files against project conventions and produce a Korean report.

## Step 1 — Determine Scope

1. Get current branch: `git branch --show-current`
2. Determine base branch:
   - If an argument is provided (e.g., `/code-review develop`) → use that branch as base
   - Otherwise → use `main` as base
3. List changed files: `git diff {base}...HEAD --name-only`
4. Get detailed diff: `git diff {base}...HEAD`
5. Get commit list: `git log {base}..HEAD --oneline`

## Step 2 — Read Changed Files

- Read each changed `.cs` file with the Read tool.
- Read non-`.cs` files (`.json`, `.md`, `.csproj`) only if relevant to the checklist.

## Step 3 — Apply Checklist

Review only files that were actually changed. Skip categories with no relevant changes.

---

### [ARCH] Architecture & Layering

- [ ] Controllers depend only on service interfaces — no concrete service classes
- [ ] Services depend only on repository interfaces — no direct `AppDbContext` access
- [ ] Only repositories access `AppDbContext`
- [ ] New domain follows `Domain/{Name}/` structure (Config / Controller / Dto / Entity / Repository / Service)
- [ ] New domain DI extension method is called from `Program.cs`

### [STYLE] C# Code Style

- [ ] `var` used only when type is obvious from the right-hand side
- [ ] Private fields: `_camelCase`; everything else: `PascalCase`
- [ ] Dependencies injected via constructor into `private readonly` fields
- [ ] Constructors contain no logic — assignments only
- [ ] Single-expression methods use expression-body (`=>`)
- [ ] No XML doc comments (`///`) unless explicitly requested
- [ ] No `#region` blocks

### [DTO] DTO Pattern

- [ ] `record` types with positional parameters
- [ ] DataAnnotations applied directly on parameters (`[Required]`, `[MaxLength]`, etc.)
- [ ] Requests in `Dto/Request/`, Responses in `Dto/Response/`

### [ENTITY] Entity Pattern

- [ ] All setters are `private set`
- [ ] Static factory method `Create(...)` used instead of public constructor
- [ ] Timestamps use `DateTime.UtcNow`
- [ ] No DataAnnotations on entities — EF config via Fluent API only
- [ ] EF Fluent config implements `IEntityTypeConfiguration<T>` in `Entity/Config/`
- [ ] `DbSet<T>` registered in `AppDbContext`
- [ ] Table/column names: `snake_case`, schema-qualified (`"schema"."table"`)
- [ ] Enums use `HasConversion<string>()`

### [SERVICE] Service Pattern

- [ ] One use case = one class + one interface
- [ ] Interface exposes exactly one `ExecuteAsync` method
- [ ] Business exceptions use Gamism.SDK types (`ConflictException`, `NotFoundException`, etc.)
- [ ] No empty catch blocks; no bare re-throw without added context

### [REPO] Repository Pattern

- [ ] Read-only queries use `AsNoTracking()`
- [ ] `SaveAsync` checks Detached state before calling `AddAsync`

### [CONTROLLER] Controller Pattern

- [ ] Return type is `CommonApiResponse` (Gamism.SDK)
- [ ] Only service interfaces injected — no concrete classes
- [ ] Rate limiting applied with `[EnableRateLimiting("login"|"game")]` where needed
- [ ] Authenticated endpoints annotated with `[Authorize]`

### [ASYNC] Async Pattern

- [ ] All I/O methods are `async Task` / `async Task<T>`
- [ ] No `.Result` or `.Wait()` calls
- [ ] No unintentional fire-and-forget — every async call is awaited

### [SECURITY] Security

- [ ] No plain-text passwords — `BCrypt.Net.BCrypt.HashPassword` required
- [ ] No hardcoded secrets (API keys, passwords, connection strings)
- [ ] No sensitive data (passwords, tokens) written to logs

### [REDIS] Redis Cache Pattern (only when Redis code changed)

- [ ] Read flow: check Redis first → on miss query DB → SET cache → return
- [ ] Write flow: update DB → DEL cache key (invalidate)

### [DI] DI Registration

- [ ] Domain services registered via `{Name}ServiceConfig.cs` extension method
- [ ] Extension method called from `Program.cs`

### [TEST] Tests (only when Fantasy.Test files changed)

- [ ] Test class name: `{ServiceName}Test`
- [ ] Test method name: `{MethodName}_{Scenario}_{ExpectedResult}`
- [ ] No direct `AppDbContext` mocks — mock repository interfaces instead
- [ ] `NSubstitute` used for mocking
- [ ] Arrange / Act / Assert structure with blank line separators

---

## Step 4 — Output Report

Write the report in Korean using the following format:

```
## 코드 리뷰 리포트

### 변경 범위
- 브랜치: {current} ← {base}
- 변경 파일 수: N개
- 커밋: {commit summary}

---

### [ARCH] 아키텍처 · 레이어링
✓ ...
⚠ ...
✗ ...

(repeat for each category that has changes; skip empty categories)

---

### 종합 결과
| 등급 | 건수 |
|------|------|
| ✓ 통과 | N |
| ⚠ 경고 | N |
| ✗ 오류 | N |

총 N개 항목 검토 — 오류 N건, 경고 N건
```

**Symbol meanings:**
- ✓ — convention followed
- ⚠ — recommendation (optional fix)
- ✗ — convention violation (fix required)

For each ✗ item, include the file name, approximate line, and suggested fix.
Skip categories with no relevant changes.
