---
name: pr
description: Generates a PR title suggestion and body based on the current branch, then creates a GitHub PR. Supports develop/release/feature branches.
allowed-tools: Bash(git log:*), Bash(git diff:*), Bash(git branch:*), Bash(git tag:*), Bash(git checkout:*), Bash(gh pr create:*), Bash(rm:*), Write, AskUserQuestion
context: fork
---

Generate a PR based on the current branch. Behavior differs depending on the branch.

## Steps

### Step 0. Initialize & Branch Discovery
1. Identify the current branch using `git branch --show-current`.
2. **Check for Arguments**:
  - **If an argument is provided (e.g., `/pr {target}`)**: Set `{target}` as the **Base Branch** and proceed directly to **Case 3**.
  - **If no argument is provided**: Follow the **Branch-Based Behavior** below.

---

## Branch-Based Behavior (Default)

### Case 1: Current branch is `develop`

**Step 1. Check the current version**

- Check git tags: `git tag --sort=-v:refname | head -10`
- Check existing release branches: `git branch -a | grep release`
- Determine the latest version (e.g., `1.0.0`)

**Step 2. Analyze changes and recommend version bump**

- Commits: `git log main..HEAD --oneline`
- Diff stats: `git diff main...HEAD --stat`
- Recommend one of:
  - **Major** (x.0.0): Breaking changes, incompatible API changes
  - **Minor** (0.x.0): New backward-compatible features
  - **Patch** (0.0.x): Bug fixes only
- Briefly explain why you chose that level

**Step 3. Ask the user for a version number**

Use AskUserQuestion:
> "현재 버전: {current_version}
> 추천 버전 업: {Major/Minor/Patch} → {recommended_version}
> 이유: {brief reason}
>
> 사용할 버전 번호를 입력해주세요. (예: 1.0.1)"

**Step 4. Create a release branch**

```bash
git checkout -b release/{version}
```

**Step 5. Write PR body** following the PR Body Template below
- Analyze changes from `main` branch
- Save to `PR_BODY.md`

**Step 6. Create PR to `main`**

```bash
gh pr create --title "release/{version}" --body-file PR_BODY.md --base main
```

**Step 7. Delete PR_BODY.md**

```bash
rm PR_BODY.md
```

---

### Case 2: Current branch is `release/x.x.x`

**Step 1. Extract version** from branch name (e.g., `release/1.2.0` → `1.2.0`)

**Step 2. Analyze changes from `main`**

- Commits: `git log main..HEAD --oneline`
- Diff stats: `git diff main...HEAD --stat`

**Step 3. Write PR body** following the PR Body Template below
- Save to `PR_BODY.md`

**Step 4. Create PR to `main`**

```bash
gh pr create --title "release/{version}" --body-file PR_BODY.md --base main
```

**Step 5. Delete PR_BODY.md**

```bash
rm PR_BODY.md
```

---

### Case 3: Any other branch

**Step 1. Analyze changes from `develop`**

- Commits: `git log develop..HEAD --oneline`
- Diff stats: `git diff develop...HEAD --stat`
- Detailed diff: `git diff develop...HEAD`

**Step 2. Suggest three PR titles** following the PR Title Convention below

**Step 3. Write PR body** following the PR Body Template below
- Save to `PR_BODY.md`

**Step 4. Output** in this format:
```
## 추천 PR 제목

1. [title1]
2. [title2]
3. [title3]

## PR 본문 (PR_BODY.md에 저장됨)

[full body preview]
```

**Step 5. Ask the user** using AskUserQuestion:
> "어떤 제목을 사용할까요? (1 / 2 / 3 또는 직접 입력)"

**Step 6. Create PR to `develop`**

- If the user answered 1, 2, or 3, use the corresponding suggested title
- If the user typed a custom title, use it as-is

```bash
gh pr create --title "{chosen title}" --body-file PR_BODY.md --base develop
```

**Step 7. Delete PR_BODY.md**

```bash
rm PR_BODY.md
```

---

## PR Title Convention

Format: `{type}: {Korean description}`

**Types:**
- `feature` — new feature added
- `fix` — bug fix or missing configuration/DI registration
- `update` — modification to existing code
- `refactor` — refactoring without behavior change

**Rules:**
- Description in Korean
- Short and imperative (단문)
- No trailing punctuation

**Examples:**
- `feature: 방 생성 API 추가`
- `fix: Key Vault 연동 방식을 AddAzureKeyVault으로 변경`
- `refactor: 로그인 로직 리팩토링`

---

## PR Body Template

Follow this exact structure (keep the emoji headers as-is):

```
## 📚작업 내용

- {change item 1}
- {change item 2}

## ◀️참고 사항

{additional notes, context, before/after comparisons if relevant. Write "." if nothing to add.}

## ✅체크리스트

> `[ ]`안에 x를 작성하면 체크박스를 체크할 수 있습니다.

- [x] 현재 의도하고자 하는 기능이 정상적으로 작동하나요?
- [x] 변경한 기능이 다른 기능을 깨뜨리지 않나요?


> *추후 필요한 체크리스트는 업데이트 될 예정입니다.*
```

**Rules:**
- Analyze commits and diffs to fill in `작업 내용` with a concise bullet list
- Keep the total body under 2500 characters
- Write in Korean
- No emojis in text content (keep the section header emojis)
