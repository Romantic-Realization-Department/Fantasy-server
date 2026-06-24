#!/bin/bash

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.tool_name // empty')
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

[[ "$TOOL_NAME" != "Bash" ]] && exit 0

NORMALIZED=$(echo "$COMMAND" | tr -s ' ' | tr -d '\n')

DANGEROUS_KEYWORDS=(
    "rm -rf"
    "mkfs"
    "dd if="
    "shutdown"
    "reboot"
    "kill -9 -1"
    ":(){"
    "> /dev/sda"
    "chmod -R 777 /"
    "chown -R root"
)

for keyword in "${DANGEROUS_KEYWORDS[@]}"; do
    if [[ "$NORMALIZED" == *"$keyword"* ]]; then
        echo "[Hook] ✗ Blocked dangerous keyword: $keyword" >&2
        echo "Command: $COMMAND" >&2
        exit 2
    fi
done

# rm -rf /  or  rm -rf *
if [[ "$NORMALIZED" =~ rm[[:space:]]+-rf.*(/|\*) ]]; then
    echo "[Hook] ✗ Blocked root/wildcard deletion" >&2
    exit 2
fi

# curl/wget → pipe/redirect → sh/bash
if [[ "$NORMALIZED" =~ (curl|wget).*(\||>).*(sh|bash) ]]; then
    echo "[Hook] ✗ Blocked remote execution" >&2
    exit 2
fi

exit 0
