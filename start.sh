#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CONFIGURATION="${CONFIGURATION:-Debug}"
FACE_URL="${FACE_URL:-http://127.0.0.1:5000}"
BRAIN_URL="${BRAIN_URL:-http://127.0.0.1:5233}"
FACE_PID=""
BRAIN_PID=""

for command in dotnet curl; do
    if ! command -v "$command" >/dev/null 2>&1; then
        echo "Required command not found: $command" >&2
        exit 1
    fi
done

if [[ "${1:-}" == "build" ]]; then
    echo "Building face, brain, and CLI..."
    dotnet build "$ROOT_DIR/face/src/face.csproj" --configuration "$CONFIGURATION"
    dotnet build "$ROOT_DIR/brain/Brain.csproj" --configuration "$CONFIGURATION"
    dotnet build "$ROOT_DIR/ears/cli/Cli.csproj" --configuration "$CONFIGURATION"
elif [[ $# -gt 0 ]]; then
    echo "Usage: $0 [build]" >&2
    exit 2
fi

cleanup() {
    local status=$?
    trap - EXIT INT TERM

    for pid in "$BRAIN_PID" "$FACE_PID"; do
        if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
            kill "$pid" 2>/dev/null || true
        fi
    done

    for pid in "$BRAIN_PID" "$FACE_PID"; do
        if [[ -n "$pid" ]]; then
            wait "$pid" 2>/dev/null || true
        fi
    done

    exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export Face__BaseUrl="$FACE_URL"
export BRAIN_URL

echo "Starting face at $FACE_URL"
(
    cd "$ROOT_DIR/face/src"
    exec dotnet "bin/$CONFIGURATION/net8.0/face.dll" --urls "$FACE_URL"
) &
FACE_PID=$!

wait_for_http() {
    local name=$1
    local url=$2
    local endpoint=$3
    local pid=$4

    for _ in {1..30}; do
        if ! kill -0 "$pid" 2>/dev/null; then
            echo "$name exited before becoming ready." >&2
            return 1
        fi
        if curl --silent --output /dev/null --max-time 2 "$url$endpoint"; then
            return 0
        fi
        sleep 1
    done

    echo "$name did not become ready at $url within 30 seconds." >&2
    return 1
}

wait_for_http "Face" "$FACE_URL" "/health" "$FACE_PID"

echo "Starting brain at $BRAIN_URL"
(
    cd "$ROOT_DIR/brain"
    exec dotnet "bin/$CONFIGURATION/net10.0/Brain.dll" --urls "$BRAIN_URL"
) &
BRAIN_PID=$!

wait_for_http "Brain" "$BRAIN_URL" "/prompt" "$BRAIN_PID"

echo "Starting interactive ears CLI (type /exit to stop)."
cd "$ROOT_DIR/ears/cli"
dotnet "bin/$CONFIGURATION/net10.0/braincli.dll"
