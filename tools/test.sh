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

  # Most repository text is intentionally materialized as CRLF in CI. Normalize only the
  # terminal carriage return so project paths passed to dotnet remain exact on both platforms.
  sed 's/\r$//' "$inventory_path" | grep -vE '^[[:space:]]*(#|$)'
}

# Translate the wrapper's VSTest-shaped Category expressions into Microsoft.Testing.Platform
# trait filters. SDK 10.0.400 selects MTP in global.json; VSTest --filter executes zero tests.
append_mtp_filter_args() {
  local filter="$1"
  MTP_FILTER_ARGS=()
  if [[ -z "$filter" ]]; then
    return 0
  fi

  local part
  local -a parts
  IFS='&' read -ra parts <<< "$filter"
  for part in "${parts[@]}"; do
    case "$part" in
      Category!=*)
        MTP_FILTER_ARGS+=(--filter-not-trait "Category=${part#Category!=}")
        ;;
      Category=*)
        MTP_FILTER_ARGS+=(--filter-trait "$part")
        ;;
      *)
        echo "Unsupported test filter '$part' in '$filter'; Microsoft.Testing.Platform requires Category trait expressions." >&2
        return 1
        ;;
    esac
  done
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

RESULTS_ROOT=""
if [[ -n "$RESULTS_DIRECTORY" ]]; then
  if [[ "$RESULTS_DIRECTORY" == /* || "$RESULTS_DIRECTORY" =~ ^[A-Za-z]:[\\/] || "/$RESULTS_DIRECTORY/" == *"/../"* ]]; then
    echo "--results-directory must be a repository-relative path without '..' segments." >&2
    exit 1
  fi
  RESULTS_ROOT="$REPO_ROOT/${RESULTS_DIRECTORY#./}"
  rm -rf -- "$RESULTS_ROOT"
  mkdir -p "$RESULTS_ROOT"
fi

for project in "${PROJECTS[@]}"; do
  CMD=(dotnet test)
  EFFECTIVE_FILTER="$FILTER"
  if [[ "$FILTER" == "Category!=Integration" ]]; then
    EFFECTIVE_FILTER="Category!=Integration&Category!=Benchmark"
  elif [[ "$FILTER" == "Category=Benchmark" ]]; then
    EFFECTIVE_FILTER=""
  fi

  if [[ -n "$project" ]]; then
    CMD+=("$project")
  fi

  CMD+=(--configuration "$CONFIGURATION")

  if [[ "$NO_BUILD" == true ]]; then
    CMD+=(--no-build)
  fi

  append_mtp_filter_args "$EFFECTIVE_FILTER"
  if [[ ${#MTP_FILTER_ARGS[@]} -gt 0 ]]; then
    CMD+=("${MTP_FILTER_ARGS[@]}")
  fi

  TRX_PATH=""
  EXPECTED_EXECUTED_TESTS=""
  if [[ -n "$RESULTS_DIRECTORY" ]]; then
    if [[ -n "$project" ]]; then
      project_name="$(basename "$project" .csproj)"
    else
      project_name="solution"
    fi

    project_results_directory="$RESULTS_ROOT/$project_name"
    mkdir -p "$project_results_directory"
    TRX_PATH="$project_results_directory/$project_name.trx"
    CMD+=(--results-directory "$project_results_directory" --report-xunit-trx --report-xunit-trx-filename "$project_name.trx")
    if [[ "$FILTER" == "Category=Benchmark" ]]; then
      EXPECTED_EXECUTED_TESTS="17"
    fi
  fi

  if [[ "$COVERAGE" == true ]]; then
    CMD+=(--coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml)
  fi

  echo "${CMD[*]}"
  test_exit_code=0
  "${CMD[@]}" || test_exit_code=$?

  if [[ -n "$TRX_PATH" ]]; then
    python3 - "$TRX_PATH" "$project" "$EFFECTIVE_FILTER" "$EXPECTED_EXECUTED_TESTS" <<'PY'
import sys
import xml.etree.ElementTree as ET

trx_path, project, test_filter, expected_text = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
root = ET.parse(trx_path).getroot()
counters = root.find(".//{*}Counters")
executed = int(counters.attrib.get("executed", "0")) if counters is not None else 0
not_executed = int(counters.attrib.get("notExecuted", "0")) if counters is not None else 0

if executed <= 0:
    raise SystemExit(f"Test project '{project}' executed zero tests for filter '{test_filter}'.")
if expected_text:
    expected = int(expected_text)
    if executed != expected or not_executed != 0:
        raise SystemExit(
            f"Test project '{project}' must execute exactly {expected} tests with none skipped; "
            f"TRX reported executed={executed}, notExecuted={not_executed}."
        )

print(f"Executed {executed} tests for {project}")
PY
  fi

  if [[ $test_exit_code -ne 0 ]]; then
    echo "dotnet test failed ($test_exit_code) for $project" >&2
    exit "$test_exit_code"
  fi
done
