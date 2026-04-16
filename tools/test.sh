#!/usr/bin/env bash
# Root test runner for local development and CI.
# Examples:
#   ./tools/test.sh
#   ./tools/test.sh --filter "Category!=Integration"
#   ./tools/test.sh --coverage
set -euo pipefail

COVERAGE=false
FILTER=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --coverage)
      COVERAGE=true
      shift
      ;;
    --filter)
      FILTER="${2:-}"
      if [[ -z "$FILTER" ]]; then
        echo "--filter requires a value" >&2
        exit 1
      fi
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

PROJECTS=()
case "$FILTER" in
  "Category!=Integration")
    PROJECTS=(
      "tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj"
      "tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj"
      "tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj"
      "tests/Hexalith.Memories.Benchmarks/Hexalith.Memories.Benchmarks.csproj"
    )
    ;;
  "Category=Integration")
    PROJECTS=("tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj")
    ;;
esac

if [[ ${#PROJECTS[@]} -eq 0 ]]; then
  PROJECTS=("")
fi

for project in "${PROJECTS[@]}"; do
  CMD=(dotnet test)

  if [[ -n "$project" ]]; then
    CMD+=("$project")
  fi

  if [[ -n "$FILTER" ]]; then
    CMD+=(--filter "$FILTER")
  fi

  if [[ "$COVERAGE" == true ]]; then
    CMD+=(--collect "XPlat Code Coverage" --settings tests/tests.runsettings)
  fi

  echo "${CMD[*]}"
  "${CMD[@]}"
done
