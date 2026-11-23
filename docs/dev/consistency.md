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
- Orphans in a projection backend (semantic or graph) without the current
  repair-source syntactic record.

**What verification does NOT detect:**

- Content-hash drift (unit present in all three backends but with different
  content).
- Edge integrity (dangling graph edges to non-existent nodes).
- Cross-tenant registry mismatches.
- Per-tenant configuration drift.

## Endpoint summary

| Endpoint | Path | Predicate | Typical consumer | Success status |
| --- | --- | --- | --- | --- |
| Verify | `POST /api/v1/tenants/{tenantId}/consistency/verify` | Accepts optional `batchSize` in [10, 5000] | `memories consistency verify` CLI; operator scripts | `202 Accepted` with `workflowInstanceId` |
| Verify status | `GET /api/v1/tenants/{tenantId}/consistency/verify/{instanceId}` | Instance id must start with `verify-consistency-{tenantId}-` | CLI `--wait`; operator polling | `200 OK` with `ConsistencyWorkflowState` |
| Inspect | `GET /api/v1/tenants/{tenantId}/consistency/inspect/{memoryUnitId}` | Memory unit id is an opaque, non-blank identifier; it is not ULID-only | `memories consistency inspect` CLI | `200 OK` with `ConsistencyInspectionResult` |
| Repair | `POST /api/v1/tenants/{tenantId}/consistency/repair` | Accepts optional `batchSize` + `includeUnrepairable` | `memories consistency repair --yes` CLI | `202 Accepted` with `workflowInstanceId` |
| Repair status | `GET /api/v1/tenants/{tenantId}/consistency/repair/{instanceId}` | Instance id must start with `repair-consistency-{tenantId}-` | CLI `--wait`; operator polling | `200 OK` with `ConsistencyWorkflowState` |

For inspect, pass the exact non-blank `MemoryUnitId` returned by ingest or the
source-URI lookup. Do not trim, case-fold, parse, or otherwise reformat it before
the first request. `MemoryUnitId` is opaque, so callers must not infer ULID/GUID
syntax or ordering from it. The existing route still carries one URL-escaped path
segment; opacity does not promise slash-bearing or unrestricted URL grammar. See
the authoritative [MemoryUnitId stability contract](./memory-unit-id-stability.md)
for identifier lifetime and resolution guidance.

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

## Current repair source and durable write model

Story 21.2 routes case, annotation, memory-unit deletion, and case deletion
mutations through a durable EventStore command boundary before projection fan-out.
The EventStore command/event stream is the authoritative write record for these
domain mutations. Redis case hashes, RediSearch syntactic memory-unit hashes,
Redis Vector entries, FalkorDB nodes/edges, case activity streams, and tenant
registry/read records are projections/read models that may be replayed, rebuilt,
or retried after a projection failure.
Case activity storage is bounded operational state: the Redis stream retains a
configured maximum number of recent events, while failed-count and last-activity
status reads use the companion summary hash instead of scanning the stream.
Cases that predate the summary hash backfill it from the activity stream on the
first missing-summary read.

The syntactic hash `{tenantId}:mu:{memoryUnitId}` remains the operational input
for the existing consistency repair workflow because that workflow repairs
memory-unit search/vector/graph projections from the data shape available today.
It is no longer the target source of truth for new write-side domain decisions.
When a post-21.2 projection workflow fails after EventStore command acceptance,
operators should retry or rebuild the failed projection from the durable command
or event history instead of treating partial Redis/FalkorDB state as permanent
truth.

When syntactic is absent in the current repair implementation:

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

A completed verification returns two lists. `Discrepancies` contains units with
a repair recommendation. A unit that also has an informational note carries it
in the same discrepancy record. `Notes` contains observations about units whose
three primary backends agree, such as a queued natural-language embedding. These
units count toward `ConsistentCount`.

Each list retains up to 10,000 records independently. `TruncatedAt` is set if
either list dropped records. The JSON result identifies which list was cut short.

| List | Retained count | Full count | Truncation warning from `ConsistencyVerificationWorkflow` |
| --- | --- | --- | --- |
| `Discrepancies` | Length of `discrepancies` | `totalDiscrepancyCount` | 8204 `DiscrepancyListTruncated` |
| `Notes` | `noteCount` (length of `notes`) | `totalNoteCount` | 8210 `NotesListTruncated` |

When a full count exceeds its retained count, use the workflow's information-level
event 8201 (`DiscrepancyDetected`) to recover individual findings from retained
logs for the verification run. Match `TenantId` and `WorkflowInstanceId` to the
scheduling receipt, then filter `EntryKind` for `Discrepancy` or `Note`. Each
event includes the memory-unit ID.
The `Recommendation` field carries the repair action, the note kind, or
`InformationalNote` for a free-text-only observation. The CLI table shows
retained counts in its `discrepancies` and `notes` columns.

When reviewing logs across an upgrade, include older 8201 events that lack
`EntryKind`. A repair action in `Recommendation` identifies a discrepancy. A
note kind or `InformationalNote` identifies a note. Older events also lack
`WorkflowInstanceId`. If runs overlap for a tenant, attributing those events
to an individual run requires correlation from the log sink.

## Replay-safety startup warning

Critical event 9173 comes from `WorkflowReplaySafetyHostedService` during
ingestion startup. On a query failure the gate fails open and the host proceeds
with startup. Keep intake quiesced and pause the rollout until direct Dapr
workflow-state evidence confirms the in-flight ingestion work has drained.
Use the structured `Reason` field to select the next check.

| Reason | Where it appears | Operator action |
| --- | --- | --- |
| `sidecar-query-timeout` | Current hosts | Check Dapr sidecar health and latency for workflow-state reads or instance enumeration. Retry the drain check after reads respond within the ten-second per-query timeout. |
| `sidecar-query-exception` | Current hosts | Inspect the attached exception. Check connectivity to the Redis in-flight registry and Dapr workflow state, then repeat the failed read. |
| `workflow-name-reflection-null` | Older hosts using SDK reflection | Inspect the active instance in Dapr to identify its workflow. Confirm ingestion instances have drained before continuing. |
| `metadata-field-missing` | Older hosts using SDK reflection | Check the deployed Dapr SDK version against the server build. Restore a supported pairing or upgrade to the registry-based host, and verify drain independently. |

Follow the rollout stop and resume criteria in the [upgrade runbook](../operations/upgrade-migration.md).

## CLI walkthrough

Recommended operator flow:

```shell
# 1. Run the read-only audit once and save the completed result
memories consistency verify --tenant acme --wait --format json > verify.json

# 2. Review repair recommendations from that run
jq '.data.discrepancies[] | {memoryUnitId, recommendation}' verify.json

# 3. Per-unit diagnosis (optional)
memories consistency inspect --tenant acme --id wf-file-instance-7

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
(which covers search, ingest, traverse, case-access, delete, tenant-lifecycle,
tenant-config, case-member, and annotation operations). Adding them to the
enricher would be a silent privacy regression — a regression guard lives in
`ConsistencyEndpointTests`.

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
| 8210 | Warning | `NotesListTruncated` (verification workflow) |
| 8210 | Info | `SemanticReIndexStarted` (semantic indexer) |
| 8211 | Info | `GraphReMergeCompleted` |
| 8220 | Warning | `RedisScanFailed` |
| 8221 | Warning | `GraphEnumerationFailed` |
| 8222 | Info | `RepairNoOp` (re-verify reports consistent) |
| 8223 | Error | `RepairActionFailed` |

## See also

- [memory-unit-id-stability.md](./memory-unit-id-stability.md) — authoritative
  opaque identifier, lifetime, and source-URI resolution contract.
- [health-checks.md](./health-checks.md) — `/ready`, `/alive`, `/health` probe
  contract.
- [telemetry.md](./telemetry.md) — access telemetry, rolling counters,
  telemetry summary endpoint.
- [export.md](./export.md) — case and tenant data export (Story 8.3). Running
  `memories consistency verify` before a compliance-critical export is
  recommended.
- [experimental-apis.md](./experimental-apis.md) — suppression protocol for
  HXL001-tagged client surfaces.
