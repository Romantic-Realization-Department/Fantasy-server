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

# git HEAD 대신 실제 .cs 파일 내용 해시 사용 → 편집마다 올바르게 감지
CURRENT_HASH=$(find . -name "*.cs" -not -path "*/obj/*" | sort | xargs md5sum 2>/dev/null | md5sum | cut -d' ' -f1)

LAST_HASH=""
if [[ -f "$CACHE_FILE" ]]; then
    LAST_HASH=$(cat "$CACHE_FILE")
fi

if [[ "$CURRENT_HASH" != "$LAST_HASH" ]]; then
    echo "[Hook] Running dotnet build..." >&2
    if dotnet build --no-restore; then
        echo "$CURRENT_HASH" > "$CACHE_FILE"
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
