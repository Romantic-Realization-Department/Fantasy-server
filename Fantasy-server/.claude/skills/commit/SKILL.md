---
name: commit
description: Creates Git commits by splitting changes into logical units. Use for staging files and writing commit messages.
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git add:*), Bash(git commit:*), Bash(git log:*), Bash(git branch:*), Bash(git switch:*)
---

Create Git commits following the project's commit conventions.

## Argument

`$ARGUMENTS` - optional GitHub issue number (e.g. `/commit 42`)

- If provided, add `#42` as the commit body (blank line after subject, then the reference).
- If omitted, commit without any issue reference.

## Commit Message Format

Subject line only (no issue):
```
{type}: {Korean description}
```

With issue number:
```
{type}: {Korean description}

#{issue}
```

**Types**:
- `feat` - new feature added
- `fix` - bug fix, missing config, or missing DI registration
- `update` - modification to existing code
- `chore` - tooling, CI/CD, dependency updates, config changes unrelated to app logic

**Description rules**:
- Written in **Korean**
- Short and imperative
- No trailing punctuation (`.`, `!` etc.)
- Avoid noun-ending style; prefer verb style

**Examples**:
```
feat: 로그인 로직 추가
```
```
fix: 인증 DI 누락 수정

#12
```

See `.claude/skills/commit/examples/type-guide.md` for a boundary-rule table and real scenarios from this project.

**Do NOT**:
- Add Claude as co-author
- Write descriptions in English
- Commit directly on `develop`

## Branch Rule

Always check the current branch before staging or committing.

- When creating a branch from `develop`, always use the format `{type}/{content}`
- Do not use `{type}-{content}` or omit the slash between type and content
- If the current branch is **not** `develop`, proceed with the normal commit flow.
- If the current branch **is** `develop`:
  - Review the changes first with `git status` and `git diff`
  - Infer the dominant logical unit and create a new working branch before any `git add` or `git commit`
  - Use a short branch name based on the work, such as `feat/player-resource-seed`, `fix/player-stage-config`, `update/player-domain-cleanup`, or `chore/deploy-pipeline`
  - Run `git switch -c <branch-name>`
  - Confirm the branch changed successfully, then continue the commit flow on that new branch

Do not keep working on `develop` after detecting staged or unstaged changes there.

## Steps

1. Check the current branch with `git branch --show-current`
2. Check all changes with `git status` and `git diff`
3. If the current branch is `develop`, create and switch to a new branch that matches the dominant logical unit before staging anything
4. Categorize changes into logical units:
   - New feature addition - `feat`
   - Bug / missing registration fix - `fix`
   - Modification to existing code - `update`
5. Group files by each logical unit
6. For each group:
   - Stage only the relevant files with `git add <files>`
   - Write a concise commit message following the format above
   - If `$ARGUMENTS` is provided: `git commit -m "{subject}" -m "#{issue}"`
   - If `$ARGUMENTS` is omitted: `git commit -m "{subject}"`
7. Verify results with `git log --oneline -n {number of commits made}`
