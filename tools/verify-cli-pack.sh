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

echo "[4/7] Story 7.2 format surface smoke (help-only, no server required)"
memories search query --help >/dev/null
memories --format json tenant list --help >/dev/null

# Story 7.3 Task 8.3: confirm the error-translation surface survived packaging. The call targets a
# nonexistent tenant against whatever endpoint is configured (typically unreachable in this dev
# loop), so exit code 1 (domain) or 2 (plumbing) is expected — we only check the binary didn't
# crash or silently exit 0.
echo "[5/7] Story 7.3 error-translation smoke (expect exit 1 or 2, NOT 0)"
set +e
memories search inspect --tenant nonexistent --case x --id y >/dev/null 2>&1
exit_code=$?
set -e
if [[ "$exit_code" -eq 0 ]]; then
    echo "Unexpected success (exit 0) from 'memories search inspect --tenant nonexistent'; the error-translation surface is broken." >&2
    exit 1
fi
if [[ "$exit_code" -ne 1 && "$exit_code" -ne 2 ]]; then
    echo "Unexpected exit code $exit_code from error smoke call; expected 1 (domain) or 2 (plumbing)." >&2
    exit 1
fi
echo "Error smoke exited $exit_code as expected."

# Story 7.4 Task 14.1/14.3: --help must exit 0 regardless of server state and embed the NFR30
# example block. Do NOT substitute --dry-run here — the wizard still constructs MemoriesClient at DI
# resolution time which requires a resolvable endpoint; --help bypasses the DI graph entirely.
echo "[6/7] Story 7.4 quickstart --help smoke (expect exit 0 with 'Example' in stdout)"
help_output="$(memories quickstart --help)"
if ! grep -qi 'Example' <<<"$help_output"; then
    echo "'memories quickstart --help' output missing the 'Example' keyword — NFR30 contract violated." >&2
    exit 1
fi
echo "Quickstart --help smoke passed."

echo "[7/7] dotnet tool uninstall --global Hexalith.Memories.Cli"
dotnet tool uninstall --global Hexalith.Memories.Cli

echo "OK — packaging pipeline verified."
