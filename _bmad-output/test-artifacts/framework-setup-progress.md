---
stepsCompleted:
    [
        "step-01-preflight",
        "step-02-select-framework",
        "step-03-scaffold-framework",
        "step-04-docs-and-scripts",
        "step-05-validate-and-summary",
    ]
lastStep: "step-05-validate-and-summary"
lastSaved: "2026-04-16"
---

# Test Framework Setup Progress

## Step 1: Preflight

- **Detected stack:** backend (.NET 10, C# 14)
- **Existing framework:** xUnit 2.9.3 + Shouldly 4.3.0 + NSubstitute 5.3.0 + coverlet
- **Test projects:** Contracts.Tests (63 tests), Server.Tests (90 tests)
- **Architecture docs:** architecture.md, prd.md, epics.md available
- **Gap identified:** No integration tests, no shared factories, no test categorization

### Preflight refresh (2026-04-16)

- **Detected stack:** backend (.NET 10 / C#, 52 `.csproj` manifests, no root `package.json`)
- **Existing browser E2E framework:** none — no `playwright.config.*`, `cypress.config.*`, or `cypress.json` found in the workspace root flow
- **Existing backend framework:** `xunit` 2.9.3 + `Microsoft.NET.Test.Sdk` 17.14.1 + `Shouldly` 4.3.0 + `NSubstitute` 5.3.0 + `coverlet.collector` 6.0.4 + `Testcontainers` 4.3.0
- **Current test architecture:** unit + integration layers already exist under `tests/`, including `Hexalith.Memories.IntegrationTests` and `Hexalith.Memories.TestHelpers`
- **Context docs reviewed:** `README.md`, `tests/README.md`, `_bmad-output/planning-artifacts/architecture.md`
- **API/auth notes:** local server endpoints are `/health`, `/alive`, and `/ready`; DAPR API token auth is architectural baseline, while ingress-layer auth is deferred to Phase 1.5
- **Preflight outcome:** pass — prerequisites are satisfied, but Create mode is re-baselining an existing backend test framework rather than scaffolding from zero

## Step 2: Framework Selection

- **Selected:** xUnit (already installed) — formalize architecture
- **Rationale:** Aligned with Hexalith.EventStore (Decision D16), solid toolchain already in place
- **Focus:** Add integration test infrastructure, shared factories, CI-aware categorization

### Selection refresh (2026-04-16)

- **Selected framework:** xUnit for backend testing; no Playwright/Cypress addition at the workspace root
- **Config check:** `test_framework=auto`, so the default backend selection logic applies
- **Reasoning:** the detected stack is backend-only (`.NET` / C#), and the repository already carries a healthy xUnit + Shouldly + NSubstitute + Testcontainers toolchain
- **Risk calculation:** adding browser E2E infrastructure to a backend-first workspace would increase flakiness and maintenance cost without materially improving confidence for the current architecture
- **Decision:** keep xUnit as the primary framework, and invest further in unit + integration depth rather than introducing browser automation prematurely

## Step 3: Scaffold Framework

### Files Created

| File                                                                                  | Purpose                                                           |
| ------------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| `tests/Directory.Build.props`                                                         | Shared test config (imports parent, adds xUnit global using)      |
| `tests/tests.runsettings`                                                             | Coverage config (cobertura), test timeout, filter docs            |
| `tests/Hexalith.Memories.TestHelpers/`                                                | Shared factory library                                            |
| `tests/Hexalith.Memories.TestHelpers/Factories/IndexInputFactory.cs`                  | IndexInput factory with overrides and realistic vector support    |
| `tests/Hexalith.Memories.TestHelpers/Factories/ExtractionInputFactory.cs`             | ExtractionInput factory with overrides                            |
| `tests/Hexalith.Memories.IntegrationTests/`                                           | Testcontainers-based integration test project                     |
| `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs`              | Shared Redis Stack container (RediSearch + Vector)                |
| `tests/Hexalith.Memories.IntegrationTests/Fixtures/FalkorDbFixture.cs`                | Shared FalkorDB container                                         |
| `tests/Hexalith.Memories.IntegrationTests/Indexing/IndexSyntacticIntegrationTests.cs` | Real Redis: index creation, HASH storage, tenant isolation        |
| `tests/Hexalith.Memories.IntegrationTests/Graph/GraphQueryBuilderIntegrationTests.cs` | Real FalkorDB: MERGE idempotency, edge creation, tenant isolation |

### Files Modified

| File                       | Change                                        |
| -------------------------- | --------------------------------------------- |
| `Directory.Packages.props` | Added Testcontainers 4.3.0                    |
| `Hexalith.Memories.slnx`   | Added TestHelpers + IntegrationTests projects |

### Build Status

- `dotnet build`: 0 warnings, 0 errors (all 9 projects)
- Unit tests: 153 pass (63 + 90)
- Integration tests: require Docker (Testcontainers) — 7 tests scaffolded

### Scaffold refresh (2026-04-16)

- **Execution mode:** `subagent` resolved from `tea_execution_mode=auto` because subagent support is available and agent-team support is not
- **Structure decision:** retained the existing multi-project `tests/` layout (`*.Tests`, `IntegrationTests`, `TestHelpers`) as the repository-idiomatic xUnit scaffold instead of forcing `tests/Unit`, `tests/Integration`, and `tests/Api` folders
- **Files created:** `global.json`, `.env.example`
- **Files updated:** `.env`
- **Existing scaffold confirmed:** xUnit project configuration, shared fixtures, override-based test factories, and example tests/helpers already satisfy the intent of Step 3
- **Gap closed:** the only missing root-level scaffold artifacts were SDK pinning and documented test environment defaults

### Test Run Commands

```bash
# Fast (unit only — no Docker required)
dotnet test --filter "Category!=Integration"

# Integration only (requires Docker)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

## Step 4: Docs and Scripts

### Docs and scripts refresh (2026-04-16)

- **Documentation updated:** `tests/README.md` now includes setup, fast-loop vs integration vs coverage commands, debug guidance, CI notes, troubleshooting, and knowledge base references
- **Scripts created:** `tools/test.ps1`, `tools/test.sh`
- **Environment guidance added:** the test guide now points contributors at `global.json`, `.env.example`, and the local `.env` overrides
- **Validation run:** `./tools/test.ps1 -Filter 'Category!=Integration'` succeeded cleanly after the project-targeting refinement, keeping the fast loop free of integration-project filter noise

## Step 5: Validate & Summarize

### Validation summary (2026-04-16)

- **Preflight:** passed — backend manifest detected, no conflicting browser framework present, architecture context available
- **Directory structure:** accepted — existing multi-project `tests/` layout satisfies the root repository’s xUnit conventions without forcing a generic `Unit/Integration/Api` reshuffle
- **Config correctness:** validated — `global.json`, `.env.example`, `.env`, `tools/test.ps1`, `tools/test.sh`, `tests/README.md`, and this progress artifact all load without reported errors
- **Fixtures/factories:** confirmed — shared fixtures and override-based factories already exist in `Hexalith.Memories.IntegrationTests` and `Hexalith.Memories.TestHelpers`
- **Docs/scripts:** present — setup, execution, CI notes, troubleshooting, and knowledge references are documented; test runner scripts exist for PowerShell and bash
- **Execution proof:** `./tools/test.ps1 -Filter 'Category!=Integration'` passed cleanly across the targeted projects (`1506` passed, `0` failed)

### Completion summary (2026-04-16)

- **Framework selected:** xUnit
- **Artifacts created:** `global.json`, `.env.example`, `tools/test.ps1`, `tools/test.sh`
- **Artifacts updated:** `.env`, `tests/README.md`, `_bmad-output/test-artifacts/framework-setup-progress.md`
- **Next steps:** run `./tools/test.ps1 -Filter 'Category=Integration'` when Docker is available; wire `tools/test.ps1` or `tools/test.sh` into CI; continue with `test-design`, `atdd`, or `ci` workflows as needed
- **Knowledge fragments applied:** `data-factories.md`, `fixture-architecture.md`, `test-quality.md`, `test-levels-framework.md`, `test-priorities-matrix.md`, `risk-governance.md`
