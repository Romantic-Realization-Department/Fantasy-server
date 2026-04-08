#!/usr/bin/env bash
# review-plan.sh — Send a plan file to Codex CLI for non-interactive review.
# Usage: review-plan.sh [plan-file-path]
#
# Defaults to .claude/current_plan.md when no argument is given.
# Output saved to .claude/reviews/{feature-name}-codex-review.md

set -euo pipefail

# ── Step 1: Determine plan file ──────────────────────────────────────────────
PLAN_FILE="${1:-.claude/current_plan.md}"

if [[ ! -f "$PLAN_FILE" ]]; then
  if [[ "${1:-}" == "" ]]; then
    echo "활성 플랜이 없습니다. \`/plan-deep-dive\`를 먼저 실행하거나, \`/review-plan <파일경로>\`로 경로를 직접 지정해주세요." >&2
  else
    echo "파일을 찾을 수 없습니다: $PLAN_FILE" >&2
  fi
  exit 1
fi

# ── Step 2: Derive feature name ──────────────────────────────────────────────
# Read the first '# ' heading from the plan file.
HEADING=$(grep -m1 '^# ' "$PLAN_FILE" | sed 's/^# //' || true)

if [[ -n "$HEADING" ]]; then
  # Convert to kebab-case: lowercase, collapse whitespace/special chars to '-'
  FEATURE_NAME=$(echo "$HEADING" \
    | tr '[:upper:]' '[:lower:]' \
    | sed 's/[^a-z0-9가-힣]/-/g' \
    | sed 's/-\+/-/g' \
    | sed 's/^-//;s/-$//')
else
  # Fall back to input filename stem
  BASENAME=$(basename "$PLAN_FILE" .md)
  FEATURE_NAME="${BASENAME%-plan}"
  FEATURE_NAME="${FEATURE_NAME:-current}"
fi

# ── Step 3: Prepare output path ──────────────────────────────────────────────
mkdir -p .claude/reviews
OUTPUT_FILE=".claude/reviews/${FEATURE_NAME}-codex-review.md"

echo "플랜  : $PLAN_FILE"
echo "출력  : $OUTPUT_FILE"
echo ""

# ── Step 4: Run Codex review ─────────────────────────────────────────────────
codex exec \
  --ephemeral \
  --full-auto \
  -s read-only \
  -o "$OUTPUT_FILE" \
  "다음은 구현 플랜입니다. 아래 4가지 관점에서 한국어로 상세히 리뷰해주세요.

1. 실현 가능성 — 기술 스택, 복잡도, 의존성 관점에서 실현 가능한가?
2. 누락된 단계 — 빠진 구현 단계나 고려사항이 있는가?
3. 위험 요소 — 버그, 성능, 보안, 설계 문제가 있는가?
4. 개선 제안 — 더 나은 접근법이나 최적화 방법이 있는가?

각 항목은 구체적으로 작성해주세요. 관련된 파일명, 레이어, 수정 방향을 포함하면 좋습니다." \
  < "$PLAN_FILE"

# ── Step 5: Print results ─────────────────────────────────────────────────────
echo ""
cat "$OUTPUT_FILE"
echo ""
echo "리뷰 완료 → $OUTPUT_FILE"
echo "입력 플랜  → $PLAN_FILE"
