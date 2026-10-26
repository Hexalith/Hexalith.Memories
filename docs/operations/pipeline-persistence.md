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

## Natural-language retry history and erasure

The retry scheduler retains a bounded content-free tenant/unit history ledger before
Dapr scheduling and before queue removal. Each asynchronous scheduling attempt owns a
nonexpiring per-unit lease and a unique authority token. Acknowledgement releases only
that token's lease; uncertain submission or process loss remains an explicit
reconciliation obligation across restart. A verified older terminal scoped execution
can be retired under this lease so a replacement payload can run at the same address.
Retirement re-reads authoritative scope, input, creation time and terminal state against
the observed execution and verifies absence afterward. Dapr has no execution-conditional
purge API: cooperative registered schedulers own this lease; out-of-band Dapr mutations
cannot be made conditional by it.

Tenant deletion permanently fences retry admission, reconciles both live/dead queues
and hashes, and purges discovered history through the Dapr workflow client after
independently validating stored scope. It refuses unknown historical coverage, even
when queues are empty. Existing tenants need independent owner reconciliation of
historical workflow coverage; this implementation does not infer or attest that
coverage. The disposable integration fixture alone supplies its known complete
inventory. Legacy addresses still require the default-false scheduler-drain option.

Completion qualifies aliases by every immutable admission field, retaining distinct
or ambiguous lineage. A content-free completion digest prevents later legacy replay.
Final generated output is compared with the exact expected retry payload in the same
Redis operation as publication, after all asynchronous readiness checks.

### Owner reconciliation procedure

This is an operator procedure for an independently approved maintenance session.
The implementation and its disposable tests do not reconcile production data or
attest historical completeness. Keep inspection output private: stored workflow
inputs and queue members can contain personal content. Public evidence records
only exact identifiers, revisions, counts, digests and dispositions.

1. **Fix the scope and custody.** Record the canonical tenant identifier, requested
   unit identifiers, Redis database, Dapr app/namespace, state store, runtime
   revisions and the responsible owner. Resolve the exact keys with the producer's
   key builders. Inspect types and bounded cardinalities before reading the
   tenant's live/dead sorted sets and companion payload hashes, history-unit set,
   scheduling hash, per-unit scheduler lease, coverage and unresolved markers.
   A hash field must equal the payload's unit; the payload tenant must equal the
   requested tenant. Treat unknown/duplicate admission properties, malformed scope,
   excess capacity and foreign legacy members as unresolved. Do not delete them
   merely to make cleanup pass.
2. **Quiesce every writer.** Disable the retry worker on every replica, drain active
   ticks and in-flight Dapr submissions, and control any external workflow writers.
   Retain instance/start identity and acknowledgement evidence for that drain.
   Restart, elapsed time, an empty queue or an expired process-local timeout is
   insufficient. For erasure, retain the existing permanent tenant/unit admission
   fences. Never remove a fence to repair scheduling. The legacy scheduler-drain
   option remains false until its separate owner-verified drain is approved.
3. **Inspect authoritative workflow scope.** For each unit, compute the scoped ID
   with `GetRetryWorkflowInstanceId(tenant, unit)` and the historic concatenated ID
   with `GetLegacyRetryWorkflowInstanceId(tenant, unit)`. Query Dapr with authenticated
   `GetWorkflowStateAsync(instanceId, true, cancellationToken)`. Independently inspect the stored
   tenant/unit input, payload version when present, creation time and terminal state.
   An address alone does not establish ownership. A legacy address belonging to
   another tuple survives. Unreadable, duplicated or missing scope blocks mutation.
4. **Resolve uncertain submission authority.** Match the lease's exact token with
   the scheduling-hash field and its unit. Preserve the history-unit obligation
   before changing either. Under the verified drain, determine whether the original
   submission was acknowledged, remains running, completed, or definitely could not
   execute. Compare authoritative workflow scope and payload version with the
   admitted payload. If absence cannot exclude an in-flight submission or missing
   retained history, keep the lease/hash and report uncertainty. For a proven
   disposition, use the producer's compare-and-remove `AcknowledgeAsync` operation
   with that exact token; never clear the entire scheduling hash or delete leases
   by pattern. A stale token cannot settle a newer authority.
5. **Establish independently complete historical inventory.** The queue/hash/ledger
   union is current discovery, not proof that all older histories are represented.
   Reconcile every relevant runtime/app identity and historical ID format against
   a complete, owner-controlled workflow inventory and retention/custody records.
   Follow all inventory pages and document its bounds, errors and unavailable periods.
   History-only units and omitted legacy executions must be included. Unit IDs,
   timestamps, current-code deployment, empty queues and sampled scans cannot grant
   coverage. If retention gaps or an unavailable inventory prevent this proof, leave
   coverage unknown and keep deletion blocked.
6. **Apply only the approved exact-scope disposition.** Retain the validated unit
   union with the bounded history-retention operation. Only after independent
   completeness and ambiguous-state reconciliation may the owner record
   `owner-reconciled-v1` in that tenant's coverage key and settle its unresolved
   marker. Keep evidence binding that decision to the same tenant, backend and
   historical inventory. This marker is an attestation supplied by the owner; the
   application never infers it. Capacity failure requires bounded reconciliation
   or an owner-approved inventory repair, not dropping retained obligations.
7. **Resume or erase and verify.** For authorized erasure, use the producer's scoped
   workflow cleanup, re-reading input/scope, creation time and terminal state before
   purge and verifying absence afterward. Never delete Dapr actor-state keys
   directly. Verify physical retry payload/member absence and unrelated survivors,
   then recreate the adapters to confirm retained fences and settled authority.
   For retry recovery, re-check current admission/version before resuming workers.
   An ambiguous response or changed execution preserves the obligation and stops
   that scope. Dapr has no execution-conditional purge; cooperative scheduler leases
   do not make out-of-band Dapr mutations safe without the verified writer drain.

## Practical operator notes

- If you want restart persistence locally, keep the default named Redis volume.
- If you want isolated test runs, set a unique `MEMORIES_REDIS_VOLUME_NAME`.
- Integration tests also set a unique `MEMORIES_DAPR_APP_ID` so reminder and actor namespaces do not bleed across runs.
- A failed-unit record surviving restart is expected; clear it by successful re-ingestion, not by assuming restart will wipe it.
