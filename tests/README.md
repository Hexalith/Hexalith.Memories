# Hexalith.Memories Test Suite

## Setup

1. Use the SDK pinned in `../global.json`.
2. Review `../.env.example`; local overrides live in `../.env`.
3. Start Docker Desktop or another Docker engine before running integration tests.
4. Restore and build the solution before the first full run.
5. Keep git submodules initialized when running the full workspace build.

For local service-backed runs, the default server endpoints exposed by the AppHost are:

- `http://localhost:5000/health`
- `http://localhost:5000/alive`
- `http://localhost:5000/ready`

## Running tests

### Fast local loop

```bash
./tools/test.sh --filter "Category!=Integration"
```

```powershell
./tools/test.ps1 -Filter 'Category!=Integration'
```

### Release preflight tooling

Run the release-preflight regression suite without starting the application:

```bash
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' -q
```

The complete suite uses POSIX `sh` executable fixtures for its simulated npm command; run it on a
POSIX host. This harness requires Python 3.9 or newer and Git with `--initial-branch` support (2.28+).
PowerShell 7 (`pwsh`) must be on `PATH` to execute the preflight script cases. Those cases report
an explicit prerequisite skip when PowerShell is absent; the harness regressions still run. A skip
is not evidence that preflight execution passed. Fixture repositories explicitly use `main`,
independent of the host Git default. Captured Git and PowerShell diagnostics use UTF-8 and replace
malformed bytes so output remains readable. Owned temporary directories retry transient permission
errors during cleanup at most three times (50 ms and 100 ms delays); persistent permission errors
and other cleanup failures still fail the test run.

To qualify the minimum Python runtime, also run the focused harness cases on Python 3.9:

```bash
PYTHONDONTWRITEBYTECODE=1 python3.9 -m unittest discover -s tests/tooling/release_preflight -p 'preflight_harness_test.py' -q
```

This includes module collection and evaluation of helper annotations under that interpreter.

### Integration coverage

Integration tests split into two lanes by wall-clock cost. Performance-tagged integration smoke tests are opt-in so the PR lane stays deterministic under shared CI resources.

- `Category=Integration&Category!=IntegrationSlow&Category!=Performance` — PR-fast lane (~5 min budget)
- `Category=IntegrationSlow` — nightly-only tests that restart the full Aspire topology or exercise long retry loops (individual tests >20 s)
- `Category=Performance` — opt-in latency and throughput smoke tests

```bash
./tools/test.sh --filter "Category=Integration"                            # all integration tests (~17 min)
./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance"    # PR-fast lane
./tools/test.sh --filter "Category=IntegrationSlow"                        # slow lane only
./tools/test.sh --filter "Category=Performance"                            # performance smoke tests
```

```powershell
./tools/test.ps1 -Filter 'Category=Integration'
./tools/test.ps1 -Filter 'Category=Integration&Category!=IntegrationSlow&Category!=Performance'
./tools/test.ps1 -Filter 'Category=IntegrationSlow'
./tools/test.ps1 -Filter 'Category=Performance'
```

### Full suite

```bash
dotnet test
```

```powershell
./tools/test.ps1
```

### Coverage

The required `test-unit-contract` PR/push job collects the seven-project Docker-free inventory and then
enforces unioned first-party line coverage. Run the same blocking sequence locally from the repository
root after a Release build:

```bash
bash ./tools/test.sh --filter "Category!=Integration" --configuration Release --no-build --coverage --results-directory TestResults/test-unit-contract
python3 tools/validate-coverage.py --results-directory TestResults/test-unit-contract --config tests/tooling/coverage_gate/line-coverage-gate.json
```

```powershell
./tools/test.ps1 -Filter 'Category!=Integration' -Configuration Release -NoBuild -Coverage -ResultsDirectory 'TestResults/test-unit-contract'
python tools/validate-coverage.py --results-directory TestResults/test-unit-contract --config tests/tooling/coverage_gate/line-coverage-gate.json
```

`tests/tooling/coverage_gate/line-coverage-gate.json` is the single source for the **76.5%** minimum, production source
scope, required assemblies, seven required project reports, and Server/CLI/MCP composition-root evidence. Each wrapper
removes the requested repository-local results directory before starting, so repeated runs cannot union stale
attachments. The validator unions
Cobertura lines by normalized repository path and line number, deduplicates collector attachment
copies, and fails on missing, malformed, incomplete, or below-threshold evidence. CI retains the TRX
and per-project Cobertura files together in the `test-unit-contract-results` artifact for 14 days.

### Benchmark quality gate

The benchmark project requires Docker because its three end-to-end suite tests use the pinned Redis
Stack and FalkorDB Testcontainers. After a Release build, run the same whole-project selector used by
the scheduled/manual `.github/workflows/nightly.yml` job:

```bash
bash ./tools/test.sh --filter "Category=Benchmark" --configuration Release --no-build --results-directory TestResults/benchmark
```

```powershell
./tools/test.ps1 -Filter 'Category=Benchmark' -Configuration Release -NoBuild -ResultsDirectory 'TestResults/benchmark'
```

Exact `Category=Benchmark` selects `tools/test-projects.benchmark.txt` but passes no trait filter to
the test host, so all 17 tests run: 10 scorer tests, 4 untraited seeder tests, and 3 Testcontainers
suite tests. Both wrappers verify from the generated TRX that exactly 17 tests executed with none skipped,
even when the test host returns the known thesis failure. Other filter expressions retain normal `dotnet test` filter semantics. The benchmark
remains a blocking quality gate: at least 80% of queries must favor hybrid retrieval and identical
input runs must produce identical NDCG@10. Do not hide a failure with a narrower filter or retries.

Local TRX evidence is written under `TestResults/benchmark`; the quality JSON is written to
`tests/Hexalith.Memories.Benchmarks/bin/Release/net10.0/benchmark-results.json`. Nightly uploads them
separately as `nightly-benchmark-trx` and `nightly-benchmark-quality-result`, even when the test step
fails. Keep current outcomes out of this procedural guide: the governed result, production calibration,
per-query scores, reproducibility proof, and synthetic-vector caveat live in
`_bmad-output/implementation-artifacts/epic-26-benchmark-remediation-evidence-2026-07-16.md`.

### Debug / investigation

```bash
./tools/test.sh --filter "Category=Integration"
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug
```

### Headed / browser mode

Not applicable to the current root scaffold. This repository is backend-first at the workspace root, so no Playwright/Cypress harness is added here. If a browser harness is introduced later, prefer `data-testid` selectors and keep UI tests focused on user journeys only.

## Architecture overview

```text
tests/
├── Hexalith.Memories.Contracts.Tests/     # Serialization and contract round-trips
├── Hexalith.Memories.Server.Tests/        # Fast unit/service tests with mocked dependencies
├── Hexalith.Memories.IntegrationTests/    # Real infrastructure via Testcontainers + Aspire helpers
├── Hexalith.Memories.Cli.Tests/           # CLI/client behavior and formatter coverage
└── Hexalith.Memories.TestHelpers/         # Shared factories and reusable test support
```

### Fixtures, factories, and helpers

- `Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs` — shared Redis Stack lifecycle
- `Hexalith.Memories.IntegrationTests/Fixtures/FalkorDbFixture.cs` — shared FalkorDB lifecycle
- `Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` — end-to-end Aspire test orchestration
- `Hexalith.Memories.TestHelpers/Factories/IndexInputFactory.cs` — override-based indexing inputs and realistic vectors
- `Hexalith.Memories.TestHelpers/Factories/IngestionInputFactory.cs` — override-based ingestion inputs
- `Hexalith.Memories.TestHelpers/Factories/ExtractionInputFactory.cs` — override-based extraction inputs

## Best practices

- Prefer the lowest useful test level: unit first, integration for backend boundaries, end-to-end only when the user journey truly requires it.
- Keep tests deterministic: no hard waits, no conditional control flow, no hidden assertions.
- Keep tests isolated and self-cleaning so they run safely in parallel.
- Use override-based factories to make intent explicit and prevent data collisions.
- Mark integration scenarios with `[Trait("Category", "Integration")]`.
- Use Shouldly for assertions and NSubstitute for mocking.
- Prefer fixture composition over inheritance.
- Keep UI-selector guidance dormant until a browser harness exists; if one is added later, prefer `data-testid`.

## Shared CI regression fixtures

```bash
python3 -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py'
```

Run `npm ci` first to install the tracked tooling graph; these stdlib unittest fixtures use the same
pinned `js-yaml` parser as the commitlint fixtures. They read the actual CI caller, reject wrong
workflow pins/platforms/project lists and bypassed mandatory gates, execute all aggregate result
combinations, and execute the source-catalog command against missing, empty, failed and skipped
reports. `test-unit-contract` runs this suite as a blocking step.

`shared-ci / build-and-test` delegates the Release build and only `Contracts.Tests` to Builds
`domain-ci.yml@81c88b34d09d34260b92d7f0928dbbc1c7446baa`, explicitly using
`microsoft-testing-platform`. Shared results use `blocking-test-results` (seven days).
The local `source-debug` job builds with `UseHexalithProjectReferences=true` and requires at least
three source-selected owner catalog tests with no failures, errors, skips or not-run tests; its
XML is uploaded as `source-catalog-verification` after failure as well as success. The unchanged
required `build` status depends on both lanes and succeeds only when both succeed.

The complete seven-project Coverlet collection, **76.5%** gate and package-only validation stay in
`test-unit-contract`, retaining `test-unit-contract-results` for 14 days. Contracts appears in both
lanes until coverage collector/report equivalence is proved. Shared consumer/coverage gates remain
disabled because Memories owns these specialized checks; no collectors change. Web E2E, tenant
negative evidence, scope/readiness, fast integration, deployment, SQL, commitlint and nightly
benchmark owners remain blocking. For rollback see the
[release runbook](../docs/dev/release-runbook.md#shared-ci-adoption-and-rollback).

## CI integration notes

- Use `tools/test.ps1` on PowerShell/Windows runners.
- Use `tools/test.sh` on bash/Linux runners.
- Coverage collection uses coverlet.MTP (`--coverlet`, Cobertura) via the wrappers. The include/exclude
  filters match `tests/tests.runsettings`.
- Integration targets require Docker availability for Testcontainers.
- Root PR/push automation lives in `.github/workflows/ci.yml`; scheduled/manual integration and
  benchmark automation lives in `.github/workflows/nightly.yml`.

## Troubleshooting

- **Docker-backed tests fail immediately**: confirm Docker Desktop or your container runtime is running before executing `Category=Integration` targets.
- **Coverage output is missing**: use the coverage command or `tools/test.ps1 -Coverage` / `tools/test.sh --coverage`, which emit `coverage.cobertura.xml` under the results directory.
- **Raw filtered `dotnet test` runs execute zero tests**: `global.json` selects Microsoft.Testing.Platform, which ignores VSTest `--filter`. Use the wrappers (`tools/test.sh` / `tools/test.ps1`) or MTP `--filter-trait` / `--filter-not-trait`.
- **Endpoint-dependent tests fail locally**: review `../.env` and confirm `BASE_URL` / `API_URL` still point at the expected local server endpoint.

## Knowledge base references

- `_bmad/tea/testarch/knowledge/data-factories.md`
- `_bmad/tea/testarch/knowledge/fixture-architecture.md`
- `_bmad/tea/testarch/knowledge/test-quality.md`
- `_bmad/tea/testarch/knowledge/test-levels-framework.md`
- `_bmad/tea/testarch/knowledge/test-priorities-matrix.md`
- `_bmad/tea/testarch/knowledge/risk-governance.md`
