# Consistency verification & repair — operator reference

Operator-facing reference for the Memories Server's consistency audit and repair
workflow. Describes what the workflow detects, what it mutates, the safety model,
and the CLI / REST entry points.

Shipped in Story 8.2.

## Purpose

Memory units are written across three backends (RediSearch for full-text search,
Redis Vector for semantic embeddings, FalkorDB for the graph). Partial failures
during ingestion can leave a unit present in some backends but missing from others.
Consistency verification detects this divergence across all existing memory units
for a tenant; consistency repair attempts to restore the unit to a fully consistent
state.

**What verification detects:**

- Presence divergence across the three backends (`(syntactic, semantic, graph)` →
  anything other than `(true, true, true)`).
- Orphans in a non-authoritative backend (semantic or graph) without the
  authoritative syntactic record.

**What verification does NOT detect:**

- Content-hash drift (unit present in all three backends but with different
  content).
- Edge integrity (dangling graph edges to non-existent nodes).
- Cross-tenant registry mismatches.
- Per-tenant configuration drift.

## Endpoint summary

| Endpoint | Path | Predicate | Typical consumer | Success status |
| --- | --- | --- | --- | --- |
| Verify | `POST /api/tenants/{tenantId}/consistency/verify` | Accepts optional `batchSize` in [10, 5000] | `memories consistency verify` CLI; operator scripts | `202 Accepted` with `workflowInstanceId` |
| Verify status | `GET /api/tenants/{tenantId}/consistency/verify/{instanceId}` | Instance id must start with `verify-consistency-{tenantId}-` | CLI `--wait`; operator polling | `200 OK` with `ConsistencyWorkflowState` |
| Inspect | `GET /api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}` | Memory unit id must be a 26-char Crockford-base32 ULID | `memories consistency inspect` CLI | `200 OK` with `ConsistencyInspectionResult` |
| Repair | `POST /api/tenants/{tenantId}/consistency/repair` | Accepts optional `batchSize` + `includeUnrepairable` | `memories consistency repair --yes` CLI | `202 Accepted` with `workflowInstanceId` |
| Repair status | `GET /api/tenants/{tenantId}/consistency/repair/{instanceId}` | Instance id must start with `repair-consistency-{tenantId}-` | CLI `--wait`; operator polling | `200 OK` with `ConsistencyWorkflowState` |

Typical latency: probe time per unit is ~1–2 ms per backend. A tenant with N units
takes ~3·N·2 ms = ~6N ms total (bounded by the batch fan-out). Expected wall-clock
duration table:

| Tenant size | Expected verify duration |
| ----------- | ------------------------ |
| 100 units | 1–2 s |
| 1,000 units | 5–10 s |
| 10,000 units | ~60 s |
| 50,000 units | ~5 min (soft cap — see Enumeration cap below) |

Repair latency is dominated by the worst-case per-unit action: deleting an orphan
is fast (~5 ms), re-merging a graph node is moderate (~20 ms per unit), re-indexing
a semantic entry requires an embedding API call (~100–500 ms per unit — subject
to the embedding rate limiter).

## Workflow lifecycle

```
ScheduleNewWorkflowAsync
        │
        ▼
   ┌─────────┐   Completed → ConsistencyVerificationResult / ConsistencyRepairResult
   │ Running │ ──┐
   └─────────┘   │
        │       │
        │       ▼
        │   ┌──────────┐
        │   │ Failed   │ (WorkflowTaskFailedException — see DAPR retry policy)
        │   └──────────┘
        │
        ▼
   ┌────────────┐
   │ Terminated │ (external cancellation — rare)
   └────────────┘
```

**Polling cadence recommendation:** start at 5-second intervals for the first
minute, then exponential backoff to 60 seconds. The `memories consistency verify
--wait` / `repair --wait` commands use a fixed 5-second interval with a 30-minute
timeout.

## Repair plan table

`RepairPlanCalculator.Calculate(syntactic, semantic, graph)` is the single source
of truth mapping a presence triple to the corrective recommendation. Every code
path (verify workflow, repair workflow, inspection service) calls the same
function:

| Syntactic | Semantic | Graph | Recommendation |
| :-------: | :------: | :---: | --- |
| T | T | T | `NoOp` |
| T | F | T | `ReIndexSemantic` |
| T | T | F | `ReIndexGraph` |
| T | F | F | `ReIndexSemanticAndGraph` |
| F | T | T | `RemoveOrphanedSemanticAndGraph` |
| F | T | F | `RemoveOrphanedSemantic` |
| F | F | T | `RemoveOrphanedGraph` |
| F | F | F | `Unrepairable` |

## Safety model

- **Verification is read-only.** No backend write happens during a verify
  workflow.
- **Repair re-verifies before acting.** Each unit's fresh presence is re-probed
  via `ConsistencyInspectionService` immediately before `RepairUnitActivity`
  dispatches its action. If the re-verify disagrees with the snapshot (e.g. the
  unit is now fully consistent), the repair activity is a no-op for that unit.
  This prevents destructive writes based on a stale verification snapshot (a
  transient Redis failure mid-verification could otherwise misclassify a unit as
  an orphan and delete its semantic+graph entries).
- **Repair convergence ceiling is 3 passes.** If discrepancies remain after
  three verify-then-repair loops, the remaining units are flagged
  `Unrepairable` with failure reason "Repair loop did not converge after 3
  passes". Operators should investigate backend health before scheduling
  additional repair runs.
- **No auto-repair.** Repair MUST be an explicit operator action; verification
  emits recommendations but never writes.

## Authoritative source

The syntactic hash `{tenantId}:mu:{memoryUnitId}` is the authoritative source of
truth because it stores the full content + metadata required to rebuild the
non-authoritative backends. When syntactic is absent:

- Non-authoritative backends holding the unit → orphans — delete them.
- Nothing anywhere → classified `Unrepairable` (content is lost — the operator
  must re-ingest from the original source).

Semantic re-indexing re-generates the embedding via the same `EmbeddingClient` +
rate limiter actor path that ingestion uses — the embedding is NOT stored on the
syntactic hash, only the provider and model identifiers are.

> **Phase-C note:** the embedding regeneration path (`SemanticIndexer.
> ReIndexFromSyntacticAsync`) currently throws `NotSupportedException` pending
> the rate-limiter-actor wiring in a follow-up story. Orphan removal and
> graph-node re-merge are fully supported in Phase C; `ReIndexSemantic` /
> `ReIndexSemanticAndGraph` surface as repair failures (`Succeeded=false`) until
> the follow-up lands. Track via the Story 8.2 Dev Agent Record.

## Enumeration cap

`EnumerateMemoryUnitIdsActivity` enforces a soft cap of **50,000 units per
verification run** (`EnumerateMemoryUnitIdsInput.MaxUnits`). When exceeded:

- The returned `EnumerateMemoryUnitIdsResult.Truncated` flag is set.
- The parent `ConsistencyVerificationResult.EnumerationTruncated` is `true`.
- Structured log `EventId 8204 EnumerationTruncated` captures the full union
  count and the cap applied.

Operators auditing tenants with more than 50,000 units should shard the audit —
e.g. sweep a random subset, or run multiple verify passes tagged by ingestion
time ranges.

## Discrepancy truncation

`ConsistencyVerificationResult.Discrepancies` is truncated to at most **10,000
entries**. The DAPR workflow state store budgets ~1 MB per instance; emitting
the full list for a tenant with many inconsistent units would exceed that
budget.

- `TotalDiscrepancyCount` is the un-truncated count (not limited).
- `TruncatedAt` is a non-null timestamp when truncation occurs.
- Each discrepancy logged individually via structured `EventId 8201
  DiscrepancyDetected` — operators can consume the full log stream to recover
  the truncated tail.

## CLI walkthrough

Recommended operator flow:

```shell
# 1. Audit — read-only
memories consistency verify --tenant acme --wait

# 2. Review the plan (optional — the verify result contains the Recommendation per unit)
memories consistency verify --tenant acme --format json > verify.json

# 3. Per-unit diagnosis (optional)
memories consistency inspect --tenant acme --id 01HM5Q9WXGK6T8Q4Z5Y6V7W8X9

# 4. Repair (MUTATING — requires --yes)
memories consistency repair --tenant acme --yes --wait
```

**Safety flags:**

- Repair always requires `--yes` (no interactive TTY prompt). Scripts and CI
  must pass `--yes` explicitly.
- `--include-unrepairable` includes `RepairActionRecord` entries for units
  flagged Unrepairable (for audit trails); the action itself remains a no-op.
- `--batch-size` [10, 5000] — default 500. Larger batches complete faster but
  put more load on FalkorDB and the embedding provider.

## Relation to `/ready` (Story 8.1)

Consistency verification is NOT called from health checks. `/ready` answers
**"is the backend reachable?"** (instance-scoped, <1 s, tenant-unaware).
Consistency verification answers **"is the data consistent?"** (per-tenant, can
take minutes, mutating on repair).

See [health-checks.md](./health-checks.md) for the `/ready` contract.

## Access telemetry (Story 7.5)

Consistency endpoints are **not** in the `AccessTelemetryEvent` audited scope
(which covers search / ingest / traverse / case-access — the four audited
operation types). Adding them to the enricher would be a silent privacy
regression — a regression guard lives in `ConsistencyEndpointTests`.

See [telemetry.md](./telemetry.md) for the audit-event contract.

## Out of scope

The following are explicitly NOT delivered by Story 8.2:

- Cross-tenant consistency checks / tenant-registry reconciliation.
- Content-hash drift detection (requires a hash field on the semantic entry
  too).
- Edge integrity verification (dangling graph edges).
- Automatic / scheduled verification (operator-triggered on demand only).
- Background repair (auto-trigger on verification discovery).
- Dry-run mode for repair (the verify workflow's `Discrepancies[].Recommendation`
  is the plan).
- UI dashboard.
- MCP agent-facing repair tool (agents must not trigger destructive operations).

## EventId bank (8200–8299)

| EventId | Level | Name |
| --- | --- | --- |
| 8200 | Info | `ConsistencyInspection` |
| 8201 | Info | `DiscrepancyDetected` |
| 8202 | Info | `RepairActionApplied` |
| 8203 | Warning | `UnrepairableDiscrepancy` |
| 8204 | Warning | `EnumerationTruncated` / `DiscrepancyListTruncated` |
| 8205 | Info | `VerificationCompleted` |
| 8206 | Info | `RepairCompleted` |
| 8207 | Debug | `RepairPassStarted` |
| 8210 | Info | `SemanticReIndexStarted` |
| 8211 | Info | `GraphReMergeCompleted` |
| 8220 | Warning | `RedisScanFailed` |
| 8221 | Warning | `GraphEnumerationFailed` |
| 8222 | Info | `RepairNoOp` (re-verify reports consistent) |
| 8223 | Error | `RepairActionFailed` |

## See also

- [health-checks.md](./health-checks.md) — `/ready`, `/alive`, `/health` probe
  contract.
- [telemetry.md](./telemetry.md) — access telemetry, rolling counters,
  telemetry summary endpoint.
- [export.md](./export.md) — case and tenant data export (Story 8.3). Running
  `memories consistency verify` before a compliance-critical export is
  recommended.
- [experimental-apis.md](./experimental-apis.md) — suppression protocol for
  HXL001-tagged client surfaces.
