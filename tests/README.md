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
dotnet test --filter "Category!=Integration"
```

```powershell
./tools/test.ps1 -Filter 'Category!=Integration'
```

### Integration coverage

Integration tests split into two lanes by wall-clock cost. Performance-tagged integration smoke tests are opt-in so the PR lane stays deterministic under shared CI resources.

- `Category=Integration&Category!=IntegrationSlow&Category!=Performance` — PR-fast lane (~5 min budget)
- `Category=IntegrationSlow` — nightly-only tests that restart the full Aspire topology or exercise long retry loops (individual tests >20 s)
- `Category=Performance` — opt-in latency and throughput smoke tests

```bash
dotnet test --filter "Category=Integration"                               # all integration tests (~17 min)
dotnet test --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance"    # PR-fast lane
dotnet test --filter "Category=IntegrationSlow"                           # slow lane only
dotnet test --filter "Category=Performance"                               # performance smoke tests
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

```bash
dotnet test --collect:"XPlat Code Coverage" --settings tests/tests.runsettings
```

```powershell
./tools/test.ps1 -Coverage
```

### Debug / investigation

```bash
dotnet test tests/Hexalith.Memories.Server.Tests --logger "console;verbosity=detailed"
dotnet test --filter "Category=Integration" --logger "console;verbosity=detailed"
dotnet test --blame-hang --blame-hang-timeout 5m
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

## CI integration notes

- Use `tools/test.ps1` on PowerShell/Windows runners.
- Use `tools/test.sh` on bash/Linux runners.
- Coverage collection relies on `XPlat Code Coverage` plus `tests/tests.runsettings`.
- Integration targets require Docker availability for Testcontainers.
- The root repository currently exposes script entry points rather than a dedicated root CI workflow; these commands are the intended automation surface.

## Troubleshooting

- **Docker-backed tests fail immediately**: confirm Docker Desktop or your container runtime is running before executing `Category=Integration` targets.
- **Coverage output is missing**: use the coverage command or `tools/test.ps1 -Coverage` / `tools/test.sh --coverage`, which add `tests/tests.runsettings` automatically.
- **Raw filtered `dotnet test` runs show a warning**: if you run `dotnet test --filter ...` against the whole solution, dedicated integration-only projects can emit a “no test matches the given testcase filter” warning. The wrapper scripts avoid that by targeting the relevant projects directly.
- **Endpoint-dependent tests fail locally**: review `../.env` and confirm `BASE_URL` / `API_URL` still point at the expected local server endpoint.

## Knowledge base references

- `_bmad/tea/testarch/knowledge/data-factories.md`
- `_bmad/tea/testarch/knowledge/fixture-architecture.md`
- `_bmad/tea/testarch/knowledge/test-quality.md`
- `_bmad/tea/testarch/knowledge/test-levels-framework.md`
- `_bmad/tea/testarch/knowledge/test-priorities-matrix.md`
- `_bmad/tea/testarch/knowledge/risk-governance.md`
