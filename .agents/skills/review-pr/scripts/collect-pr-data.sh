#!/bin/bash
set -euo pipefail

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh is required." >&2
  exit 1
fi

PR_NUMBER=$(gh pr view --json number -q .number 2>/dev/null || true)
if [ -z "${PR_NUMBER:-}" ]; then
  echo "ERROR: No open PR found for current branch." >&2
  exit 1
fi

REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
BASE=$(gh pr view "$PR_NUMBER" --json baseRefName -q .baseRefName)

OUT_DIR=".pr-tmp/$PR_NUMBER"
mkdir -p "$OUT_DIR"

git fetch origin "$BASE" --quiet || true

gh pr view "$PR_NUMBER" --json number,title,url,baseRefName,headRefName,author \
  > "$OUT_DIR/pr_meta.json"

gh api "repos/$REPO/pulls/$PR_NUMBER/comments" \
  --jq '[.[] | {id, path, line, side, body, user: .user.login, createdAt: .created_at}]' \
  > "$OUT_DIR/review_comments.json"

gh api "repos/$REPO/issues/$PR_NUMBER/comments" \
  --jq '[.[] | {id, body, user: .user.login, createdAt: .created_at}]' \
  > "$OUT_DIR/issue_comments.json"

git log "origin/$BASE..HEAD" --pretty=format:"%H %h %s" > "$OUT_DIR/commits.txt"
git diff "origin/$BASE...HEAD" --name-only > "$OUT_DIR/changed_files.txt"
git diff "origin/$BASE...HEAD" > "$OUT_DIR/diff.txt"

echo "PR #$PR_NUMBER | Repo: $REPO | Base: $BASE | Output: $OUT_DIR"
echo "Review comments: $(gh api --method GET "repos/$REPO/pulls/$PR_NUMBER/comments" --jq 'length'), Issue comments: $(gh api --method GET "repos/$REPO/issues/$PR_NUMBER/comments" --jq 'length'), Changed files: $(wc -l < "$OUT_DIR/changed_files.txt" | tr -d ' ')"
