#!/bin/bash
# .claude/hooks/postToolUse.sh

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.tool_name // empty')
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')

if [[ "$TOOL_NAME" != "Edit" && "$TOOL_NAME" != "Write" ]]; then
    exit 0
fi

if [[ "$FILE_PATH" != *.cs ]]; then
    exit 0
fi

echo "[Hook] C# file modified: $FILE_PATH" >&2

dotnet format --no-restore 2>/dev/null || echo "[Hook] format failed (ignored)" >&2

CACHE_FILE=".claude/.last_build_hash"

# 캐시 파일보다 새로운 .cs 파일이 있는지 타임스탬프로 비교 (전체 내용 해시보다 빠름)
if [[ ! -f "$CACHE_FILE" ]] || find . -name "*.cs" -not -path "*/obj/*" -newer "$CACHE_FILE" 2>/dev/null | grep -q .; then
    echo "[Hook] Running dotnet build..." >&2
    if dotnet build --no-restore; then
        touch "$CACHE_FILE"
    else
        echo "[Hook] Build failed" >&2
        exit 2
    fi
else
    echo "[Hook] Skip build (no source changes)" >&2
fi

echo "[Hook] Running tests..." >&2
if dotnet test --no-build --verbosity minimal; then
    echo "[Hook] Tests passed" >&2
else
    echo "[Hook] Tests failed" >&2
    exit 2
fi

exit 0
