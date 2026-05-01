#!/usr/bin/env bash
# Root test runner for local development and CI.
# Examples:
#   ./tools/test.sh
#   ./tools/test.sh --filter "Category!=Integration"
#   ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance"   # PR-fast integration lane
#   ./tools/test.sh --filter "Category=IntegrationSlow"                          # Nightly-only slow lane
#   ./tools/test.sh --coverage
set -euo pipefail

COVERAGE=false
FILTER=""
CONFIGURATION="Debug"
NO_BUILD=false
RESULTS_DIRECTORY=""

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
    --configuration)
      CONFIGURATION="${2:-}"
      if [[ -z "$CONFIGURATION" ]]; then
        echo "--configuration requires a value" >&2
        exit 1
      fi
      shift 2
      ;;
    --no-build)
      NO_BUILD=true
      shift
      ;;
    --results-directory)
      RESULTS_DIRECTORY="${2:-}"
      if [[ -z "$RESULTS_DIRECTORY" ]]; then
        echo "--results-directory requires a value" >&2
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

read_inventory() {
  local inventory_file="$1"
  local inventory_path="$REPO_ROOT/$inventory_file"

  if [[ ! -f "$inventory_path" ]]; then
    echo "Test project inventory '$inventory_file' was not found." >&2
    return 1
  fi

  grep -vE '^[[:space:]]*(#|$)' "$inventory_path"
}

# Resolve the inventory file for the current filter. Filters that don't match any case fall
# back to running against the entire solution (PROJECTS=("")).
inventory_file=""
case "$FILTER" in
  "Category!=Integration")
    inventory_file="tools/test-projects.unit-contract.txt"
    ;;
  *Category=IntegrationSlow*|*Category=Integration*)
    inventory_file="tools/test-projects.integration-fast.txt"
    ;;
  "Category=Benchmark")
    inventory_file="tools/test-projects.benchmark.txt"
    ;;
esac

PROJECTS=()
if [[ -n "$inventory_file" ]]; then
  # Capture the inventory output through command substitution so a missing file (read_inventory
  # returning 1) actually propagates to the parent shell. Process substitution `<( ... )` runs
  # in a subshell whose exit code is invisible to mapfile, which would otherwise silently fall
  # back to PROJECTS=("") and run dotnet test against the entire solution with no project filter.
  if ! inventory_contents=$(read_inventory "$inventory_file"); then
    exit 1
  fi
  if [[ -n "$inventory_contents" ]]; then
    mapfile -t PROJECTS <<<"$inventory_contents"
  fi
fi

if [[ ${#PROJECTS[@]} -eq 0 ]]; then
  PROJECTS=("")
fi

for project in "${PROJECTS[@]}"; do
  CMD=(dotnet test)
  EFFECTIVE_FILTER="$FILTER"
  if [[ "$FILTER" == "Category!=Integration" ]]; then
    EFFECTIVE_FILTER="Category!=Integration&Category!=Benchmark"
  fi

  if [[ -n "$project" ]]; then
    CMD+=("$project")
  fi

  CMD+=(--configuration "$CONFIGURATION")

  if [[ "$NO_BUILD" == true ]]; then
    CMD+=(--no-build)
  fi

  if [[ -n "$EFFECTIVE_FILTER" ]]; then
    CMD+=(--filter "$EFFECTIVE_FILTER")
  fi

  TRX_PATH=""
  if [[ -n "$RESULTS_DIRECTORY" ]]; then
    if [[ -n "$project" ]]; then
      project_name="$(basename "$project" .csproj)"
    else
      project_name="solution"
    fi

    project_results_directory="$REPO_ROOT/$RESULTS_DIRECTORY/$project_name"
    mkdir -p "$project_results_directory"
    TRX_PATH="$project_results_directory/$project_name.trx"
    CMD+=(--logger "trx;LogFileName=$project_name.trx" --results-directory "$project_results_directory")
  fi

  if [[ "$COVERAGE" == true ]]; then
    CMD+=(--collect "XPlat Code Coverage" --settings tests/tests.runsettings)
  fi

  echo "${CMD[*]}"
  "${CMD[@]}"

  if [[ -n "$TRX_PATH" ]]; then
    python3 - "$TRX_PATH" "$project" "$EFFECTIVE_FILTER" <<'PY'
import sys
import xml.etree.ElementTree as ET

trx_path, project, test_filter = sys.argv[1], sys.argv[2], sys.argv[3]
root = ET.parse(trx_path).getroot()
counters = root.find(".//{*}Counters")
executed = int(counters.attrib.get("executed", "0")) if counters is not None else 0

if executed <= 0:
    raise SystemExit(f"Test project '{project}' executed zero tests for filter '{test_filter}'.")

print(f"Executed {executed} tests for {project}")
PY
  fi
done
