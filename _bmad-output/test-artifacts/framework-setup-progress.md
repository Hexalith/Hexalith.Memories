---
stepsCompleted: ['step-01-preflight', 'step-02-select-framework', 'step-03-scaffold-framework', 'step-04-docs-and-scripts']
lastStep: 'step-04-docs-and-scripts'
lastSaved: '2026-03-29'
---

# Test Framework Setup Progress

## Step 1: Preflight

- **Detected stack:** backend (.NET 10, C# 14)
- **Existing framework:** xUnit 2.9.3 + Shouldly 4.3.0 + NSubstitute 5.3.0 + coverlet
- **Test projects:** Contracts.Tests (63 tests), Server.Tests (90 tests)
- **Architecture docs:** architecture.md, prd.md, epics.md available
- **Gap identified:** No integration tests, no shared factories, no test categorization

## Step 2: Framework Selection

- **Selected:** xUnit (already installed) — formalize architecture
- **Rationale:** Aligned with Hexalith.EventStore (Decision D16), solid toolchain already in place
- **Focus:** Add integration test infrastructure, shared factories, CI-aware categorization

## Step 3: Scaffold Framework

### Files Created

| File | Purpose |
|---|---|
| `tests/Directory.Build.props` | Shared test config (imports parent, adds xUnit global using) |
| `tests/tests.runsettings` | Coverage config (cobertura), test timeout, filter docs |
| `tests/Hexalith.Memories.TestHelpers/` | Shared factory library |
| `tests/Hexalith.Memories.TestHelpers/Factories/IndexInputFactory.cs` | IndexInput factory with overrides and realistic vector support |
| `tests/Hexalith.Memories.TestHelpers/Factories/ExtractionInputFactory.cs` | ExtractionInput factory with overrides |
| `tests/Hexalith.Memories.IntegrationTests/` | Testcontainers-based integration test project |
| `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs` | Shared Redis Stack container (RediSearch + Vector) |
| `tests/Hexalith.Memories.IntegrationTests/Fixtures/FalkorDbFixture.cs` | Shared FalkorDB container |
| `tests/Hexalith.Memories.IntegrationTests/Indexing/IndexSyntacticIntegrationTests.cs` | Real Redis: index creation, HASH storage, tenant isolation |
| `tests/Hexalith.Memories.IntegrationTests/Graph/GraphQueryBuilderIntegrationTests.cs` | Real FalkorDB: MERGE idempotency, edge creation, tenant isolation |

### Files Modified

| File | Change |
|---|---|
| `Directory.Packages.props` | Added Testcontainers 4.3.0 |
| `Hexalith.Memories.slnx` | Added TestHelpers + IntegrationTests projects |

### Build Status

- `dotnet build`: 0 warnings, 0 errors (all 9 projects)
- Unit tests: 153 pass (63 + 90)
- Integration tests: require Docker (Testcontainers) — 7 tests scaffolded

### Test Run Commands

```bash
# Fast (unit only — no Docker required)
dotnet test --filter "Category!=Integration"

# Integration only (requires Docker)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```
