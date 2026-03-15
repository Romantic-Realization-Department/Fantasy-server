---
description: Create Git commits by splitting changes into logical units
allowed-tools: Bash
---

Create Git commits following the project's commit conventions.

## Commit Message Format

```
{type}: {Korean description}
```

**Types**:
- `feat` — new feature added
- `fix` — bug fix, missing config, or missing DI registration
- `modify` — modification to existing code

**Description rules**:
- Written in **Korean**
- Short and imperative (단문)
- No trailing punctuation (`.`, `!` etc.)
- Avoid noun-ending style — prefer verb style

**Examples**:
```
feat: 방 생성 API 추가
fix: 세션 DI 누락 수정
modify: Room 엔터티 수정
```

**Do NOT**:
- Add Claude as co-author
- Write descriptions in English
- Add a commit body — subject line only

## Steps

1. Check all changes with `git status` and `git diff`
2. Categorize changes into logical units:
   - New feature addition → `feat`
   - Bug / missing registration fix → `fix`
   - Modification to existing code → `modify`
3. Group files by each logical unit
4. For each group:
   - Stage only the relevant files with `git add <files>`
   - Write a concise commit message following the format above
   - Execute `git commit -m "message"`
5. Verify results with `git log --oneline -n {number of commits made}`