---
name: commit
description: Creates Git commits by splitting changes into logical units. Use for staging files and writing commit messages.
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git add:*), Bash(git commit:*), Bash(git log:*)
---

Create Git commits following the project's commit conventions.

## Commit Message Format

```
{type}: {Korean description}
```

**Types**:
- `feat` — new feature added
- `fix` — bug fix, missing config, or missing DI registration
- `update` — modification to existing code
- `chore` — tooling, CI/CD, dependency updates, config changes unrelated to app logic

**Description rules**:
- Written in **Korean**
- Short and imperative (단문)
- No trailing punctuation (`.`, `!` etc.)
- Avoid noun-ending style — prefer verb style

**Examples**:
```
feat: 로그인 로직 추가
fix: 세션 DI 누락 수정
update: Account 엔터티 수정
```

See `.claude/skills/commit/examples/type-guide.md` for a boundary-rule table and real scenarios from this project.

**Do NOT**:
- Add Claude as co-author
- Write descriptions in English
- Add a commit body — subject line only

## Steps

1. Check all changes with `git status` and `git diff`
2. Categorize changes into logical units:
   - New feature addition → `feat`
   - Bug / missing registration fix → `fix`
   - Modification to existing code → `update`
3. Group files by each logical unit
4. For each group:
   - Stage only the relevant files with `git add <files>`
   - Write a concise commit message following the format above
   - Execute `git commit -m "message"`
5. Verify results with `git log --oneline -n {number of commits made}`
