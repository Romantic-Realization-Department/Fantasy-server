---
name: review-plan
description: Sends a plan file to Codex CLI for non-interactive review. Saves results to .claude/reviews/{feature-name}-codex-review.md and prints a summary.
argument-hint: [plan-file-path]
allowed-tools: Bash(.claude/skills/review-plan/review-plan.sh:*)
context: fork
---

# Review Plan with Codex CLI

Send an implementation plan to Codex for a structured non-interactive review.

## Step 1 — Run the script

```bash
bash .claude/skills/review-plan/review-plan.sh $ARGUMENTS
```

The script handles all steps:
1. Resolves the plan file (`$ARGUMENTS` or `.claude/current_plan.md`)
2. Derives a kebab-case feature name from the first `# ` heading (falls back to filename stem)
3. Creates `.claude/reviews/` if needed
4. Runs `codex exec` with the review prompt
5. Prints the review output and a summary footer

## Step 2 — Handle errors

- Exit code non-zero → show the error output and stop with: "Codex 실행에 실패했습니다. 위 오류를 확인해주세요."
