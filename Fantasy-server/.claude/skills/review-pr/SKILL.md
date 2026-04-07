---
name: review-pr
description: Collect PR review comments, critically assess each one against project conventions, auto-apply valid ones, post refutation replies for invalid ones, and prompt for partial ones. Replaces resolve-pr-comments.
compatibility: Requires git, gh (GitHub CLI), and jq
---

## Step 1 — Collect PR Data

```bash
bash .claude/skills/review-pr/scripts/collect-pr-data.sh
```

Output directory: `.pr-tmp/<PR_NUMBER>/`

Output files:
- `pr_meta.json` — PR metadata (number, title, url, base/head branch, author)
- `review_comments.json` — inline review comments (id, path, line, side, body, user, createdAt)
- `issue_comments.json` — PR-level (non-inline) comments (id, body, user, createdAt)
- `commits.txt` — commits in this PR
- `changed_files.txt` — changed file paths
- `diff.txt` — full diff

## Step 2 — Assess Each Comment

For each comment in `review_comments.json` (and `issue_comments.json` if it references code), apply the following **layered judgment criteria**:

### Judgment criteria (priority order)

1. **Project conventions** (primary): cross-reference the following rule files
   - `CLAUDE.md` — tech stack, Context7 usage
   - `.claude/rules/architecture.md` — directory structure, layering rules
   - `.claude/rules/code-style.md` — C# naming, DTO/entity patterns, async rules
   - `.claude/rules/conventions.md` — DB naming, EF Core Fluent API, DI registration, password hashing
   - `.claude/rules/domain-patterns.md` — service, repository, controller implementation patterns
   - `.claude/rules/global-patterns.md` — JWT, Redis, rate limiting infrastructure patterns
   - `.claude/rules/testing.md` — test naming, mocking with NSubstitute, FluentAssertions
2. **Language/framework best practices** (secondary): C# / ASP.NET Core / .NET 10 official guidelines
   - Apply only when no matching project rule exists

### Verdicts

- **VALID**: reviewer is correct → attempt auto code fix
- **INVALID**: reviewer is wrong with a clear refutation → skip, post refutation reply
- **PARTIAL**: intent is correct but application method or scope is ambiguous → confirm with AskUserQuestion

Always cite a specific source in the rationale (e.g. `code-style.md §Naming`, `conventions.md §Entity Configuration`).

## Step 3 — Act on Each Verdict

### VALID → Auto fix

1. Read the target file with the Read tool
2. Apply the reviewer's concern with the Edit tool
3. Run `/test` to verify the build and tests pass; fix any failures before continuing
4. Commit the change
5. Record the short commit hash for use in Step 5:
   ```bash
   git rev-parse --short HEAD
   ```

On failure: record the reason and fall back to PARTIAL.

### INVALID → Skip

Do not modify any code. Record the refutation rationale for Step 5.

### PARTIAL → Confirm with AskUserQuestion

Use the AskUserQuestion tool to ask:

```
⚠️ PARTIAL: [file:line] (reviewer)
Review: "..."
Rationale: ...
Accept? (y / n / s = skip for now)
```

- `y`: treat as VALID, attempt code fix
- `n`: treat as INVALID, skip
- `s` / other: record as PENDING

## Step 4 — Print Report

```
## review-pr Results

| # | Reviewer | File | Verdict | Rationale | Action |
|---|----------|------|---------|-----------|--------|
| 1 | alice | Foo.cs:12 | ✅ VALID | code-style.md §Naming | Auto-fixed (abc1234) |
| 2 | bob | Bar.cs:34 | ❌ INVALID | conventions.md §Entity Configuration | Skipped |
| 3 | alice | Baz.cs:56 | ⚠️ PARTIAL | - | PENDING |
```

## Step 5 — Post GitHub Replies

Post an inline reply for each `review_comments.json` entry. Always quote `comment_id` to prevent shell injection.

```bash
gh api "repos/<owner>/<repo>/pulls/<pr_number>/comments/<comment_id>/replies" \
  -f body="<reply_body>"
```

For reply body templates, read `.claude/skills/review-pr/references/github-reply-formats.md`.

## Step 6 — Cleanup

```bash
rm -rf .pr-tmp
```