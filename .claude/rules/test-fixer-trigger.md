---
description: Proactively offer the test-fixer agent after production code changes that affect test coverage.
alwaysApply: true
---

## test-fixer Auto-Suggest Trigger

After completing any task that modifies production `.cs` files, check whether the test-fixer agent should be offered.

### Trigger conditions (ALL must apply)

- At least one of the following changes occurred in `Fantasy.Server/` (not `Fantasy.Test/`):
  - A new `Service` class or implementation method was added
  - An existing service method signature was changed
  - A service class or method was deleted
- The task is fully complete (no pending edits)

### How to ask

Use `AskUserQuestion` with exactly these two options immediately after finishing the task:

```
question: "test-fixer 에이전트를 실행해서 테스트를 업데이트할까요?"
options:
  - label: "실행"
    description: "test-fixer 에이전트가 변경된 코드에 맞춰 테스트를 추가·수정·삭제합니다."
  - label: "건너뜀"
    description: "테스트는 나중에 직접 처리합니다."
```

### After the answer

- "실행" → immediately invoke the `test-fixer` agent
- "건너뜀" → do nothing

### Do NOT trigger when

- Only DTOs, request/response records, or EF config files changed
- Only controller routing or attribute changes (no logic)
- The task was already a test-only change (`Fantasy.Test/` only)
- The user explicitly said they will handle tests themselves
- Convention or style fixes only
