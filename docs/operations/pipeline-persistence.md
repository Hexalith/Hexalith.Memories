# Pipeline Persistence & Restart Validation (Story 6.4)

This page documents the durability and restart-validation work added in Story 6.4.
The goal is simple: a controlled restart must not silently lose ingestion state or create duplicate indexed data.

## Redis durability configuration

Redis persistence is now explicit and repo-owned.

- AppHost file: `src/Hexalith.Memories.AppHost/Program.cs`
- Redis config: `deploy/redis/redis.conf`
- Mounted config path inside the container: `/redis-stack.conf`
- Mounted data path inside the container: `/data`
- Default named volume: `hexalith-memories-redis-data`
- Test override for isolation: `MEMORIES_REDIS_VOLUME_NAME`

`deploy/redis/redis.conf` enables the Redis durability settings used by the local AppHost and restart tests:

- `appendonly yes`
- `appendfsync everysec`
- `dir /data`
- `aof-use-rdb-preamble yes`
- RDB snapshots remain enabled for faster restart and extra safety

That combination keeps Redis-backed workflow history, actor state, syntactic hashes, semantic hashes,
dedup keys, and failed-unit registry entries durable across controlled restarts.

## What is durable vs. what is intentionally ephemeral

### Durable state

- Redis syntactic hashes: `{tenantId}:mu:{memoryUnitId}`
- Redis semantic/vector hashes: `{tenantId}:vec:{memoryUnitId}`
- Dedup keys: `dedup:{tenantId}:{caseId}:...`
- Dapr actor/workflow state stored through the Redis state store (`actorStateStore: true`)
- Failed-unit hashes and per-case failed-unit sorted sets introduced in Story 6.3

### Intentionally ephemeral helpers

- `GenerateEmbeddingActivity.RetryTrackingKeys`

That cache is a per-process jitter helper only. Resetting it on restart is expected and is **not** a data-loss bug.

## Automated restart scenarios

The restart-aware integration fixture lives in:

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`

It exposes `RestartTopologyAsync()` and reconnects the HTTP, Redis, and FalkorDB clients after the AppHost comes back.

The automated Story 6.4 restart coverage lives in:

- `tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs`

Covered scenarios:

1. In-flight URL ingestion resumes after restart without duplicate writes.
2. Dedup key remains singular and the memory-unit identity stays stable across restart.
3. Case in-flight counts recover correctly after restart.
4. Embedding rate-limit state does not reset unexpectedly after restart.
5. Corpus statistics remain usable after restart.
6. Failed-unit records survive restart and can be re-ingested successfully afterward.
7. Redis-backed indexed data remains queryable after a controlled restart.

Run the restart suite with:

```bash
dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --filter FullyQualifiedName~PipelinePersistenceIntegrationTests
```

## Warm restart and throughput benchmark

Story 6.4 also adds a performance-oriented benchmark in:

- `tests/Hexalith.Memories.IntegrationTests/Performance/PipelinePersistencePerformanceTests.cs`

The benchmark uses:

- warmed local images,
- `Memories__Testing__UseFakeEmbedding=true`,
- a local scripted HTTP server for deterministic payload delivery,
- a test-scoped Dapr app ID (`MEMORIES_DAPR_APP_ID`) and Redis volume (`MEMORIES_REDIS_VOLUME_NAME`) for isolation.

Run it with:

```bash
dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --filter FullyQualifiedName~PipelinePersistencePerformanceTests
```

Benchmark artifact output:

- `tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/pipeline-persistence-performance.json`

### Latest recorded measurements

From the passing Story 6.4 benchmark run in this session:

| Metric                                       |              Result |            Target | Outcome |
| -------------------------------------------- | ------------------: | ----------------: | ------- |
| Warm restart readiness                       |           `16.69 s` |         `<= 60 s` | Pass    |
| Small payload throughput (`8 KB`, 24 units)  | `1323.08 units/min` | `> 100 units/min` | Pass    |
| Large payload throughput (`256 KB`, 8 units) |  `233.98 units/min` |  `> 10 units/min` | Pass    |

The benchmark's “large payload” representative uses a deterministic `256 KB` body. That keeps the run stable in the
current local Dapr workflow harness while still measuring a payload size inside the story's `<= 1 MB` large-payload class.

## Dapr workflow retention caveat

Completed Dapr workflow history is retained in the actor state store until it is explicitly purged.
Story 6.4 documents that behavior but does **not** add a purge feature.

Operationally, that means:

- workflow completion is durable,
- restart validation can rely on persisted workflow history,
- storage growth for completed workflow state is a separate lifecycle concern.

## Practical operator notes

- If you want restart persistence locally, keep the default named Redis volume.
- If you want isolated test runs, set a unique `MEMORIES_REDIS_VOLUME_NAME`.
- Integration tests also set a unique `MEMORIES_DAPR_APP_ID` so reminder and actor namespaces do not bleed across runs.
- A failed-unit record surviving restart is expected; clear it by successful re-ingestion, not by assuming restart will wipe it.
