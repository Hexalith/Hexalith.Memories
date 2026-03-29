# Hexalith.Memories Test Suite

## Architecture

```
tests/
├── Hexalith.Memories.Contracts.Tests/     # Serialization round-trips for V1 contracts
├── Hexalith.Memories.Server.Tests/        # Unit tests (mocked dependencies)
│   ├── Activities/Ingestion/              # Extract, Embedding activities
│   ├── Activities/Indexing/               # Syntactic, Semantic, Graph activities
│   ├── Actors/                            # RateLimiter logic
│   ├── Graph/                             # GraphQueryBuilder (injection prevention)
│   └── Ingestion/                         # Client tests
├── Hexalith.Memories.IntegrationTests/    # Real infrastructure via Testcontainers
│   ├── Fixtures/                          # Shared containers (Redis Stack, FalkorDB)
│   ├── Indexing/                          # Activity integration tests
│   └── Graph/                             # FalkorDB query verification
└── Hexalith.Memories.TestHelpers/         # Shared factories (not a test runner)
    └── Factories/                         # IndexInputFactory, ExtractionInputFactory
```

## Running Tests

### Unit Tests (fast, no Docker)

```bash
dotnet test --filter "Category!=Integration"
```

### Integration Tests (requires Docker)

```bash
dotnet test --filter "Category=Integration"
```

### All Tests

```bash
dotnet test
```

### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --settings tests/tests.runsettings
```

### Single Project

```bash
dotnet test tests/Hexalith.Memories.Server.Tests
```

## Test Categorization

- **Unit tests**: No `[Trait]` attribute needed (default)
- **Integration tests**: Mark with `[Trait("Category", "Integration")]`
- **Collection fixtures**: Use `[Collection("RedisStack")]` or `[Collection("FalkorDB")]` to share container lifecycle

## Factories

Shared test data factories in `Hexalith.Memories.TestHelpers`:

```csharp
// Defaults — parallel-safe with auto-incrementing IDs
IndexInput input = IndexInputFactory.Create();

// Override specific fields to show test intent
IndexInput input = IndexInputFactory.Create(
    tenantId: "my-tenant",
    causationId: "mu-cause-001");

// Realistic 768-dim vector for dimension-sensitive tests
float[] vector = IndexInputFactory.CreateRealisticVector(768);
```

## Integration Test Infrastructure

Uses [Testcontainers for .NET](https://dotnet.testcontainers.org/) to spin up real backends:

- **RedisStackFixture**: `redis/redis-stack:latest` — RediSearch + Vector Search
- **FalkorDbFixture**: `falkordb/falkordb:latest` — graph database

Containers start once per `[Collection]` (not per test) and use random ports to avoid conflicts.

## Conventions

- **Assertions**: Shouldly (`.ShouldBe()`, `.ShouldContain()`, `Should.ThrowAsync<T>()`)
- **Mocking**: NSubstitute (`Substitute.For<T>()`, `.Received()`, `.Returns()`)
- **Naming**: `MethodName_Scenario_ExpectedBehavior` or `Scenario_ShouldExpectedBehavior`
- **No test inheritance**: Prefer composition via collection fixtures
- **Exceptions propagate**: Activities don't catch — test exception propagation explicitly
