#!/usr/bin/env bash
# Story 7.1 Task 8.1 — dev-only packaging verification script (bash counterpart).
# Not intended for CI: packaging is checked manually or in release workflows (anti-pattern #8).
set -euo pipefail

CONFIGURATION="${CONFIGURATION:-Release}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS_DIR="${ARTIFACTS_DIR:-$REPO_ROOT/artifacts}"

CLI_PROJECT="$REPO_ROOT/src/Hexalith.Memories.Cli/Hexalith.Memories.Cli.csproj"
if [[ ! -f "$CLI_PROJECT" ]]; then
    echo "CLI project not found at $CLI_PROJECT" >&2
    exit 1
fi

mkdir -p "$ARTIFACTS_DIR"

echo "[1/4] dotnet pack $CLI_PROJECT -c $CONFIGURATION -o $ARTIFACTS_DIR"
dotnet pack "$CLI_PROJECT" -c "$CONFIGURATION" -o "$ARTIFACTS_DIR"

echo "[2/4] dotnet tool install --global --add-source $ARTIFACTS_DIR Hexalith.Memories.Cli"
dotnet tool uninstall --global Hexalith.Memories.Cli >/dev/null 2>&1 || true
dotnet tool install --global --add-source "$ARTIFACTS_DIR" Hexalith.Memories.Cli

echo "[3/5] memories --version"
if ! command -v memories >/dev/null 2>&1; then
    echo "'memories' command not found on PATH after install." >&2
    echo "Check that '$HOME/.dotnet/tools' is on your PATH." >&2
    echo "See docs/dev/cli-config.md (PATH troubleshooting) for per-shell remediation." >&2
    exit 1
fi
memories --version

echo "[4/5] Story 7.2 format surface smoke (help-only, no server required)"
memories search query --help >/dev/null
memories --format json tenant list --help >/dev/null

echo "[5/5] dotnet tool uninstall --global Hexalith.Memories.Cli"
dotnet tool uninstall --global Hexalith.Memories.Cli

echo "OK — packaging pipeline verified."
