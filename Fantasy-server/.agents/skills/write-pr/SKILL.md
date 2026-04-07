---
name: write-pr
description: Generates a PR title suggestion and body based on the current branch, then creates a GitHub PR. Supports develop/release/feature branches.
allowed-tools: Bash(git log:*), Bash(git diff:*), Bash(git branch:*), Bash(git tag:*), Bash(git checkout:*), Bash(gh pr create:*), Bash(rm:*), Write, AskUserQuestion
context: fork
---

Generate a PR based on the current branch. Behavior differs depending on the branch.

Use `references/label.md` to select 1-2 PR labels before creating the PR. Apply the selected labels when running the PR creation step.

## Steps

### Step 0. Initialize & Branch Discovery
1. Identify the current branch using `git branch --show-current`.
2. Check for arguments:
  - If an argument is provided, for example `/write-pr {target}`, set `{Base Branch}` = `{target}` and proceed directly to Case 3.
  - If no argument is provided, follow the branch-based behavior below:
    - Current branch is `develop` -> Case 1
    - Current branch matches `release/x.x.x` -> Case 2
    - Any other branch -> Case 3 with `{Base Branch}` = `develop`

---

## Branch-Based Behavior

### Case 1. Current branch is `develop`

**Step 1. Check the current version**

- Check git tags: `git tag --sort=-v:refname | head -10`
- Check existing release branches: `git branch -a | grep release`
- Determine the latest version, for example `1.0.0`

**Step 2. Analyze changes and recommend version bump**

- Commits: `git log main..HEAD --oneline`
- Diff stats: `git diff main...HEAD --stat`
- Recommend one of:
  - Major (`x.0.0`): breaking changes or incompatible API changes
  - Minor (`0.x.0`): new backward-compatible features
  - Patch (`0.0.x`): bug fixes only
- Briefly explain why you chose that level

**Step 3. Ask the user for a version number**

Use AskUserQuestion:
> Current version: {current_version}
> Recommended bump: {Major/Minor/Patch} -> {recommended_version}
> Reason: {brief reason}
>
> Enter the release version. Example: `1.0.1`

**Step 4. Create a release branch**

```bash
git checkout -b release/{version}
```

**Step 5. Write PR body**

- Analyze changes from `main`
- Follow the PR Body Template below
- Save to `PR_BODY.md`

**Step 6. Select labels**

- Follow `references/label.md`
- Select 1-2 PR-eligible labels that match the change

**Step 7. Create PR to `main`**

```bash
./scripts/create-pr.sh "release/{version}" PR_BODY.md "{label1,label2}"
```

**Step 8. Delete PR_BODY.md**

```bash
rm PR_BODY.md
```

---

### Case 2. Current branch is `release/x.x.x`

**Step 1. Extract version**

- Extract the version from the branch name, for example `release/1.2.0` -> `1.2.0`

**Step 2. Analyze changes from `main`**

- Commits: `git log main..HEAD --oneline`
- Diff stats: `git diff main...HEAD --stat`

**Step 3. Write PR body**

- Follow the PR Body Template below
- Save to `PR_BODY.md`

**Step 4. Select labels**

- Follow `references/label.md`
- Select 1-2 PR-eligible labels that match the change

**Step 5. Create PR to `main`**

```bash
./scripts/create-pr.sh "release/{version}" PR_BODY.md "{label1,label2}"
```

**Step 6. Delete PR_BODY.md**

```bash
rm PR_BODY.md
```

---

### Case 3. Any other branch

**Step 1. Analyze changes from `{Base Branch}`**

- Commits: `git log {Base Branch}..HEAD --oneline`
- Diff stats: `git diff {Base Branch}...HEAD --stat`
- Detailed diff: `git diff {Base Branch}...HEAD`

**Step 2. Suggest three PR titles**

- Follow the PR Title Convention below

**Step 3. Write PR body**

- Follow the PR Body Template below
- Save to `PR_BODY.md`

**Step 4. Ask the user**

Use AskUserQuestion with a `choices` array:
- Options: the 3 generated titles plus `직접 입력` as the last option
- If the user selects `직접 입력`, ask a follow-up AskUserQuestion for the custom title

**Step 5. Select labels**

- Follow `references/label.md`
- Select 1-2 PR-eligible labels that match the change

**Step 6. Create PR to `{Base Branch}`**

- Use the selected title, or the custom title if the user chose `직접 입력`

```bash
./scripts/create-pr.sh "{chosen title}" PR_BODY.md "{label1,label2}"
```

**Step 7. Delete PR_BODY.md**

```bash
rm PR_BODY.md
```

---

## PR Title Convention

Format: `{type}: {Korean description}`

**Types:**
- `feat`: new feature added
- `fix`: bug fix or missing configuration or DI registration
- `update`: modification to existing code
- `docs`: documentation changes
- `refactor`: refactoring without behavior change
- `test`: adding or updating tests
- `chore`: tooling, CI/CD, dependency updates, or config changes unrelated to app logic

**Rules:**
- Description in Korean
- Short and imperative
- No trailing punctuation

**Examples:**
- `feat: 계정 생성 API 추가`
- `fix: Key Vault 연동 방식을 AddAzureKeyVault로 변경`
- `refactor: 로그 처리 로직 분리`

See `examples/feature-to-develop.md` for a complete example of a feature -> develop PR.

## Labels

Follow `references/label.md` and select 1-2 labels before the PR creation step.

## PR Body Template

Follow this exact structure:

`templates/pr-body.md`

**Rules:**
- Analyze commits and diffs to fill in the work summary with concise bullet points
- Keep the total body under 2500 characters
- Write in Korean
- Do not add emojis in the body text
