---
name: review-pr
description: Review a pull request or branch diff against a base branch and produce a Korean PR review report focused on bugs, regressions, missing tests, API or schema risks, and project convention violations. Use when asked to review a PR before merge, inspect branch changes, or give merge-readiness feedback.
allowed-tools: Bash(git diff:*), Bash(git log:*), Bash(git branch:*), Read, Glob, Grep
context: fork
---

# Review PR

Review the branch as a pull request and report only meaningful findings.

If `scripts/collect-pr-data.sh` exists and `gh` is available, run it first to collect PR metadata, comments, commit list, changed files, and diff into `.pr-tmp/{pr-number}/`. Use those artifacts as the primary review input before falling back to manual `git` and `gh` commands.

## Step 1 - Determine Scope

1. Get the current branch with `git branch --show-current`.
2. Determine the base branch:
   - If an argument is provided, for example `/review-pr develop`, use that branch as base.
   - Otherwise, use `main` when reviewing `develop`.
   - Otherwise, use `develop`.
3. Collect review material:
   - Changed files: `git diff {base}...HEAD --name-only`
   - Full diff: `git diff {base}...HEAD`
   - Commit list: `git log {base}..HEAD --oneline`
   - Diff stat: `git diff {base}...HEAD --stat`
   - If available, prefer artifacts from `.pr-tmp/{pr-number}/changed_files.txt`, `.pr-tmp/{pr-number}/diff.txt`, `.pr-tmp/{pr-number}/commits.txt`, `.pr-tmp/{pr-number}/review_comments.json`, and `.pr-tmp/{pr-number}/issue_comments.json`

## Step 2 - Load Review Context

Before commenting on conventions, read the repository guidance that applies to the changed files.

- Always read `AGENTS.md`.
- Read `.claude/rules/architecture.md`, `.claude/rules/code-style.md`, `.claude/rules/conventions.md`, `.claude/rules/domain-patterns.md`, `.claude/rules/testing.md`, and `.claude/rules/verify.md`.
- Read `.claude/rules/flows.md` when controller, service, auth, or request/response behavior changed.
- Read only the files that actually changed, plus nearby files if needed to validate behavior.

## Step 3 - Review With PR Mindset

Prioritize correctness over style. Ignore untouched code unless the change relies on it.

Look for:

- Functional bugs and behavioral regressions
- Missing null handling, validation, authorization, or transaction boundaries
- API contract changes without corresponding DTO, controller, or test updates
- EF Core or persistence risks such as missing config, migration mismatch, tracking issues, or cache invalidation gaps
- DI registration omissions
- Async misuse, swallowed exceptions, or incorrect error mapping
- Security issues such as secrets, token leakage, weak password handling, or sensitive logging
- Missing or insufficient tests for newly introduced behavior
- Maintainability issues only when they create clear follow-up risk

Do not pad the review with low-signal style nits. Report a finding only when you can explain the concrete risk.

## Step 4 - Output Format

Write the review in Korean. Present findings first, ordered by severity.

Use this structure:

```md
## PR 리뷰
### 범위
- 브랜치: {current} -> {base}
- 변경 파일: {count}개
- 커밋: {short summary}

### 주요 발견사항
1. [심각도] 제목
   - 위치: {file}:{line}
   - 문제: {what is wrong}
   - 영향: {why it matters}
   - 제안: {specific fix}

### 확인 필요
- 가정이나 누락된 정보가 있으면 작성

### 요약
- 머지 전 수정 필요: {count}건
- 권장 확인 사항: {count}건
```

Severity labels:

- `치명적`: merge blocker, production failure, security issue, data corruption risk
- `높음`: likely bug or regression that should be fixed before merge
- `보통`: real risk or missing coverage worth addressing soon

If there are no findings, say that explicitly under `주요 발견사항` and include any residual test or verification gaps in `확인 필요`.

## Step 5 - Reply To GitHub Comments

When replying to inline review comments, read `references/github-reply-formats.md` and follow it exactly.

- Always write replies in Korean.
- Always quote `comment_id` when building shell commands or `gh api` payloads.
- Use only the approved reply formats for `VALID`, `INVALID`, and `PARTIAL` cases.
- Keep the reply short and decisive. Do not add extra argument unless the format explicitly allows it.
