---
name: write-pr
description: Generates a PR title suggestion and body based on the current branch, then creates a GitHub PR. Supports develop/release/feature branches.
allowed-tools: Bash(git log:*), Bash(git diff:*), Bash(git branch:*), Bash(git tag:*), Bash(git checkout:*), Bash(gh pr create:*), Bash(rm:*), Write, AskUserQuestion
context: fork
---

Generate a PR based on the current branch. Behavior differs depending on the branch.

## Steps

### Step 0. Initialize & Branch Discovery
1. Identify the current branch using `git branch --show-current`.
2. **Check for Arguments**:
  - **If an argument is provided (e.g., `/write-pr {target}`)**: Set `{Base Branch}` = `{target}` and proceed directly to **Case 3**.
  - **If no argument is provided**: Follow the **Branch-Based Behavior** below:
    - Current branch is `develop` → **Case 1**
    - Current branch matches `release/x.x.x` → **Case 2**
    - Any other branch → **Case 3** with `{Base Branch}` = `develop`

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

**Step 1. Analyze changes from `{Base Branch}`**

- Commits: `git log {Base Branch}..HEAD --oneline`
- Diff stats: `git diff {Base Branch}...HEAD --stat`
- Detailed diff: `git diff {Base Branch}...HEAD`

**Step 2. Suggest three PR titles** following the PR Title Convention below

**Step 3. Write PR body** following the PR Body Template below
- Save to `PR_BODY.md`

**Step 4. Ask the user** using AskUserQuestion with a `choices` array:
- Options: the 3 generated titles + "직접 입력" as the last option
- If the user selects "직접 입력", ask a follow-up AskUserQuestion for the custom title

**Step 6. Create PR to `{Base Branch}`**

- Use the selected title, or the custom title if the user chose "직접 입력"

```bash
gh pr create --title "{chosen title}" --body-file PR_BODY.md --base {Base Branch}
```

**Step 7. Delete PR_BODY.md**

```bash
rm PR_BODY.md
```

---

## PR Title Convention

Format: `{type}: {Korean description}`

**Types:**
- `feat` — new feature added
- `fix` — bug fix or missing configuration/DI registration
- `update` — modification to existing code
- `docs` — documentation changes
- `refactor` — refactoring without behavior change
- `test` — adding or updating tests
- `chore` — tooling, CI/CD, dependency updates, config changes unrelated to app logic

**Rules:**
- Description in Korean
- Short and imperative (단문)
- No trailing punctuation

**Examples:**
- `feat: 방 생성 API 추가`
- `fix: Key Vault 연동 방식을 AddAzureKeyVault으로 변경`
- `refactor: 로그인 로직 리팩토링`

See `.claude/skills/pr/examples/feature-to-develop.md` for a complete example (title options + filled body) of a feature → develop PR.

---

## PR Body Template

Follow this exact structure (keep the emoji headers as-is):

!.claude/skills/pr/templates/pr-body.md

**Rules:**
- Analyze commits and diffs to fill in `작업 내용` with a concise bullet list
- Keep the total body under 2500 characters
- Write in Korean
- No emojis in text content (keep the section header emojis)
