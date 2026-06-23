#!/bin/bash
# Bridges the Unity MCP relay into the host /tmp so Claude Code can reach it.
# Unity runs in a Flatpak sandbox with an isolated /tmp; this script creates
# symlinks for any unity-mcp-* sockets before starting the relay --mcp process.

set -e

UNITY_PID=$(pgrep -f "Editor/Unity " | head -1)
if [ -z "$UNITY_PID" ]; then
    echo '{"error":"Unity editor not running"}' >&2
    exit 1
fi

# Symlink every unity-mcp-* socket from inside the sandbox into the host /tmp.
# When Unity runs without a Flatpak sandbox, /proc/$PID/root/ resolves to /,
# making source and destination identical — skip the symlink in that case.
for socket in /proc/$UNITY_PID/root/tmp/unity-mcp-*; do
    [ -e "$socket" ] || continue
    name=$(basename "$socket")
    host_path="/tmp/$name"
    real_socket=$(realpath "$socket" 2>/dev/null)
    real_host=$(realpath "$host_path" 2>/dev/null)
    if [ "$real_socket" != "$real_host" ] && [ "$(readlink "$host_path" 2>/dev/null)" != "$socket" ]; then
        ln -sf "$socket" "$host_path"
    fi
done

PROJECT_PATH="$(cd "$(dirname "$0")/../Code/AlienOgKo.Unity" && pwd)"
RELAY=$(find "$PROJECT_PATH/Library/PackageCache" -name "relay_linux" -path "*/RelayApp~/*" 2>/dev/null | head -1)
RELAY="${RELAY:-/home/fehaar/.unity/relay/relay_linux}"

exec "$RELAY" --mcp --project-path "$PROJECT_PATH" "$@"
