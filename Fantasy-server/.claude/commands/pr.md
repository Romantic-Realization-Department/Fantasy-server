---
description: Generate PR title suggestions and body based on changes from develop
allowed-tools: Bash(git log:*), Bash(git diff:*), Bash(git branch:*), Bash(git tag:*), Bash(git checkout:*), Bash(gh pr create:*), Bash(rm:*), Write, AskUserQuestion
---

Generate a PR based on the current branch.

If the user provides a branch name, use that branch instead of the current branch.

---

## Step 0. Determine target branch

Ask the user:

> "특정 브랜치로 PR을 생성하시겠습니까? (없으면 Enter)"
> (예: feature/login-api, release/1.2.0)

- If user input is empty:
  - Use current branch:
    !`git branch --show-current`
- If user provides a branch name:
  - Checkout that branch:
    !`git checkout {branch_name}`
  - Use it as current branch

---

## Context

- Current branch: (determined above)

---

## Branch-Based Behavior

### Case 1: Current branch is `develop`

**Step 1. Check the current version**

- Check git tags: `git tag --sort=-v:refname | head -10`
- Check existing release branches: `git branch -a | grep release`
- Determine the latest version (e.g., `1.0.0`)

**Step 2. Analyze changes and recommend version bump**

- Commits: `git log main..HEAD --oneline`
- Diff stats: `git diff main...HEAD --stat`
- Recommend one of:
  - **Major** (x.0.0): Breaking changes
  - **Minor** (0.x.0): New features
  - **Patch** (0.0.x): Bug fixes

**Step 3. Ask user for version**

Use AskUserQuestion:
> "현재 버전: {current_version}
> 추천 버전 업: {type} → {recommended_version}
> 이유: {reason}
>
> 사용할 버전 번호를 입력해주세요."

**Step 4. Create release branch**

!`git checkout -b release/{version}`

**Step 5. Write PR body → PR_BODY.md**

**Step 6. Create PR**

!`gh pr create --title "release/{version}" --body-file PR_BODY.md --base main`

**Step 7. Cleanup**

!`rm PR_BODY.md`

---

### Case 2: Current branch is `release/x.x.x`

**Step 1. Extract version**

**Step 2. Analyze changes**

- `git log main..HEAD --oneline`
- `git diff main...HEAD --stat`

**Step 3. Write PR body**

**Step 4. Create PR**

!`gh pr create --title "release/{version}" --body-file PR_BODY.md --base main`

**Step 5. Cleanup**

!`rm PR_BODY.md`

---

### Case 3: Any other branch

**Step 1. Analyze changes from develop**

- `git log develop..HEAD --oneline`
- `git diff develop...HEAD --stat`
- `git diff develop...HEAD`

**Step 2. Suggest 3 PR titles**

**Step 3. Write PR body → PR_BODY.md**

**Step 4. Output**
