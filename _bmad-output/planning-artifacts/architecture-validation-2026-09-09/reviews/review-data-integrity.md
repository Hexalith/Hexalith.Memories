# Reviewer Gate — Data Integrity, State Ownership, and Upstream Contract Drift

**Target:** `../../architecture.md` (legacy spine candidate; reviewed read-only)  
**Lens:** ad-hoc data-integrity / ownership / contract-drift reviewer  
**Date:** 2026-09-09  
**Verdict:** **FAIL.** The candidate names the right high-level pattern—EventStore commit followed by Dapr Workflow projections—but does not define a realizable MemoryUnit commit/version contract, permits an implementation that reports `indexed` after verification finds a missing projection, and contradicts the current PRD's benchmark and graph-entry contracts. It is not safe as the controlling architecture spine until the critical ownership and completion semantics are decided and made testable.

## Review basis and limits

This review traced the following paths against the current repository rather than treating the legacy document's completion claims as evidence:

- Current product contract: `../../prd.md`, especially the canonical terminology at lines 80-96, thesis protocol at 155-168, ingestion contract at 814-839, FR6/FR10/FR13 at 997-1004, FR17/FR25 at 1011-1019, tenant requirements at 1038-1045, and NFR16 at 1145-1146.
- Current change-control clarification: `../../addendum.md`, especially EventStore's three distinct contracts at lines 11-15, the shipped-vs-required benchmark at 23-37, isolation mechanism at 43-47, ingestion ownership at 49-53, and downstream-drift warning at 118-130.
- Current domain contracts and implementation under `src/`, plus focused unit/integration tests and `docs/operations/index-rebuild.md`.

This is a static evidence review. It does not claim that skipped, environment-dependent, or unexecuted integration tests passed on this machine.

## Critical findings

### DI-01 — The claimed EventStore authority for ordinary MemoryUnit creation has no authoritative mutation contract

**Disposition:** **Discuss**, then **autofix** the spine and implementation together.  
**Impact:** A file/URL MemoryUnit can exist only in projection/workflow state even though the architecture and PRD say its durable commit is an EventStore acknowledgement. After projection loss, there may be no EventStore event from which that MemoryUnit can be enumerated or reconstructed.

The candidate repeatedly makes an unqualified ownership claim:

- `architecture.md:45` says FR6 forces EventStore command acceptance for domain truth.
- `architecture.md:98` says `Case`, `MemoryUnit`, and `Tenant` domain state is sourced from EventStore events, and that Case/MemoryUnit commands must be accepted before projection fan-out.
- `architecture.md:292`, `architecture.md:582`, and `architecture.md:1598-1620` call EventStore replay the durable rebuild path while the content-ingest flow goes directly to `IngestionWorkflow`.

The current upstream contract is equally explicit: `prd.md:818-831`, FR6/FR13 at `prd.md:997-1004`, and `addendum.md:11-15` say the MemoryUnit durable commit is EventStore acknowledgement and projections acknowledge the same EventStore source version.

The executable path does not support that statement:

- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:108-142` reserves a Redis dedup identity and schedules `IngestionWorkflow`; it does not submit a MemoryUnit command.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:304-395` constructs projection inputs and writes syntactic, semantic, and graph stores directly through activities.
- The complete command inventory under `src/Hexalith.Memories.EventStore/Domain/Commands/` contains case create/delete, annotation request, MemoryUnit delete, tenant registration, and tenant lifecycle status—there is no file/URL MemoryUnit create/accept command.
- `src/Hexalith.Memories.EventStore/Domain/Aggregates/MemoryUnitAggregate.cs:16-54` handles only annotation intent and deletion intent. `MemoryUnitAggregateState.cs:10-42` can therefore fold only annotations and deletion requests, not an ordinary ingested unit's source/content identity or lifecycle.
- Neither `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs:9-65` nor `src/Hexalith.Memories.Server/Workflows/Contracts/IndexInput.cs:9-65` carries an EventStore source version.

The event-subscription path does not close the file/URL hole. `src/Hexalith.Memories.EventStore/EventIngestionService.cs:143-185` receives an already-published CloudEvent and schedules the same workflow, which is a valid product-integration path for event units, but it does not create the missing Memories-domain commit for ordinary content ingest.

**Required resolution:** Choose and record one authoritative model with no mixed wording:

1. Add a concrete MemoryUnit acceptance event/command before every creation path (file, URL, directory, re-ingest, annotation, import as applicable), define the payload retained for deterministic rebuilding, and carry its aggregate/source version through every projection; or
2. Narrow the EventStore claim to the mutation subsets it actually owns and explicitly designate workflow/source-artifact state as the authority for ordinary ingestion, with durability, retention, and rebuild guarantees that satisfy NFR16.

The spine must include the command/event name, aggregate identity, accepted payload/reference, version token, scheduling handoff, and replay enumerator. “EventStore is source of truth” is not enforceable without them.

### DI-02 — `indexed` is not defined as an enforced all-three, same-version gate

**Disposition:** **Discuss** (contract), then **autofix** the spine and code.  
**Impact:** A two-of-three unit can be returned as terminal `indexed`, directly violating FR6/FR13 and making status unsafe as an integrity signal.

The current PRD says `indexed` is emitted only when all three projections acknowledge the same EventStore version (`prd.md:820-838`; FR6/FR13 at `prd.md:997-1004`). The candidate only says that every ingest “must verify” all three (`architecture.md:45`) and lists `VerifyConsistencyActivity` after writes (`architecture.md:98`, `architecture.md:1615-1620`). It never gives the verification result a normative transition rule, defines the version receipt stored by each projection, or says what happens when verification returns false after activities reported success.

That omission has already admitted a concrete divergence:

- `src/Hexalith.Memories.Server/Activities/Indexing/ConsistencyResult.cs:10-24` contains presence booleans only; no projection version is compared.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:433-465` turns a missing syntactic/semantic/graph record into a warning and `ConsistencyNote`.
- The same workflow then unconditionally transitions to `MemoryUnitStatus.Indexed`, sets custom status `indexed`, and returns success at `IngestionWorkflow.cs:608-623`.
- The indexing inputs have no common EventStore version (`IndexInput.cs:9-65`; `SemanticChunkIndexInput.cs:12-48`).

**Required resolution:** Define a typed projection receipt such as `(memoryUnitId, sourceVersion, projectionKind, acknowledged)` and require all mandatory receipts to match the committed version before the sole transition to `indexed`. A false/mismatched receipt must remain `projecting` while retryable or become `failed` with durable details after the retry budget. Add a test in which each activity returns successfully but post-write verification reports one backend absent or stale; `indexed` must be impossible.

## High findings

### DI-03 — “Rebuildable from EventStore” contradicts the shipped verifier and repair authority

**Disposition:** **Discuss**; do not defer the contradiction under the existing D10 open item.  
**Impact:** Operators may treat projection repair as EventStore replay, but it cannot find a unit absent from all projections, cannot recreate the syntactic source, and may delete surviving projections as orphans.

The candidate calls Redis/FalkorDB projections rebuildable and EventStore replay the durable path (`architecture.md:98`, `architecture.md:292`, `architecture.md:582`), while D10 merely says to “accept degradation” and design concurrent index naming (`architecture.md:296-301`, `architecture.md:589`). That deferral does not answer the data-loss/rebuild ownership question.

Current behavior demonstrates why the missing detail is load-bearing:

- `src/Hexalith.Memories.Server/Activities/Indexing/EnumerateMemoryUnitIdsActivity.cs:21-40` enumerates only the union of the three derived backends. An EventStore-committed unit absent from all three cannot enter a verification/repair plan.
- `src/Hexalith.Memories.Server/Consistency/RepairPlanCalculator.cs:15-44` explicitly calls the syntactic Redis hash authoritative; with syntactic absent, surviving semantic/graph records are scheduled for deletion, and all-three-absent is unrepairable.
- `src/Hexalith.Memories.Server/Activities/Indexing/RepairUnitActivity.cs:108-185` returns unrepairable when no projection remains and uses that projection-derived recommendation.
- `docs/operations/index-rebuild.md:19-22` admits the current repair source is syntactic Redis, not EventStore. Its limitations at lines 56-76 state that semantic re-index is unsupported, missing syntactic cannot be rebuilt, replay normally hits dedup, and NL-only orphans are not enumerated.
- NFR16 nevertheless requires zero missing EventStore-committed units after Redis restart and a rebuild path (`prd.md:1145-1146`).

**Required resolution:** Specify two distinct operations and their authorities: (a) projection-to-projection consistency repair, with its intentionally narrow safe actions, and (b) authoritative rebuild, which enumerates committed MemoryUnits from EventStore/source-artifact history, bypasses ingestion dedup safely, re-derives vectors, restores all required projection versions, and proves zero missing. If authoritative rebuild does not exist, state that plainly and mark NFR16 unsatisfied rather than calling current repair a replay path.

### DI-04 — The legacy benchmark/graph decisions can falsely certify the thesis against the superseding contract

**Disposition:** **Autofix** from the current PRD; no architectural discretion remains for these protocol values.  
**Impact:** A reviewer following the candidate can treat the N=8 synthetic, explicit-start-node, best-single-axis run as Gate 1 evidence even though the current product contract explicitly calls it diagnostic only.

The candidate preserves the old governance: strict positive win against the best single axis, the 80% line, synthetic benchmark data, and graph-as-optional scorer (`architecture.md:94-96`, `architecture.md:170-198`, `architecture.md:372-377`, `architecture.md:548`, `architecture.md:590`). Its requirements trace still calls FR17/FR25 ready (`architecture.md:1652-1666`).

The current PRD supersedes each of those assumptions:

- `prd.md:155-168` requires at least 50 representative real topics, BM25+semantic two-axis RRF control, per-topic delta at least 0.02, aggregate guards, frozen independently graded labels, and automatic graph seeding from top-5 syntactic plus top-5 semantic candidates at depth no greater than 2.
- `prd.md:979-985` marks FR17 and FR25 partial.
- `addendum.md:23-37` explicitly classifies the shipped 8-query suite as diagnostic.
- `tests/Hexalith.Memories.Benchmarks/BenchmarkSuiteTests.cs:36-39` admits synthetic precomputed vectors; lines 135-187 use an explicit `GraphStartNodeId` and compare hybrid to the best single active axis; lines 204-215 pass at 80% wins.
- `tests/Hexalith.Memories.Benchmarks/Data/ground-truth.json:1-156` contains only BQ-01 through BQ-08, each with an explicit graph start and all three required axes.
- `src/Hexalith.Memories.Server/Search/HybridSearchService.cs:168-186` skips graph when no start node is supplied, exactly the gap recorded by the PRD.

The spine must state that the shipped suite is an NFR26 regression/diagnostic only, identify the missing two-axis control and auto-seeding implementation, and forbid a G1 pass until the current protocol is exercised.

### DI-05 — Aggregate ownership is only partially enforced for Case and Tenant mutations

**Disposition:** **Discuss** the aggregate boundary, then **autofix** the mutation matrix.  
**Impact:** Two developers can reasonably put semantically equivalent Case/Tenant state in different authorities, defeating replay and allowing EventStore state to disagree with the live read model.

There is good partial alignment: Case create, annotation, MemoryUnit delete, and Case delete call `IMemoriesCommandStore.AcceptAsync` before scheduling a projection (`CaseService.cs:84-99`, `CaseService.cs:167-197`, `CaseService.cs:639-679`, `CaseService.cs:693-727`). Tenant registration and lifecycle status similarly accept a command before Dapr-state projection (`TenantRegistryService.cs:139-165`, `TenantRegistryService.cs:252-279`, `TenantRegistryService.cs:334-361`). The production DI default is the EventStore-backed command store; the in-memory store is an explicit test fallback.

But the candidate's blanket `Case`/`Tenant` authority claim (`architecture.md:98`) does not define the boundary for other shipped mutations:

- Case membership is written/deleted directly in Redis at `src/Hexalith.Memories.Server/Cases/CaseService.cs:470-563`; no Case command/event records it.
- Tenant display name is part of folded aggregate state (`src/Hexalith.Memories.EventStore/Domain/States/MemoriesTenantAggregateState.cs:17-36`) but is updated directly in Dapr state at `TenantRegistryService.cs:442-517`; no domain command exists for the update.
- Final tenant removal deletes the Dapr registry record directly (`TenantRegistryService.cs:530-590`). The EventStore aggregate has no `Deleted` status or deletion-completed event (`src/Hexalith.Memories.Contracts/V1/TenantStatus.cs:12-27`; `MemoriesTenantAggregate.cs:13-39`), so replay cannot express the same terminal absence as the live registry.
- Each accept-then-schedule path is two separate calls (for example `CaseService.cs:89-99`). The candidate does not specify recovery for a process failure after EventStore acceptance but before workflow scheduling; deterministic workflow IDs reduce duplicate scheduling but do not provide a durable handoff.

**Required resolution:** Add a mutation-ownership table for every shipped Case/MemoryUnit/Tenant operation: command/event, aggregate, authority, projection, idempotency key, terminal state, and recovery when scheduling fails. Explicitly decide whether membership and display name are domain state or disposable read-model/config state. Define tenant deletion as an EventStore tombstone/status transition (including history-retention policy) or narrow the source-of-truth claim.

### DI-06 — Tenant isolation is tested at the data-shaping layer, but the selected principal boundary and deletion lifecycle are incomplete

**Disposition:** **Defer to a dated/open item only if it names the exact enforcement and deletion work; otherwise discuss now.**  
**Impact:** The candidate can be read as an implemented security boundary although tenant data currently shares backend connections, and a deleted/recreated tenant can inherit stale reservation state.

The candidate correctly says that prefixes/logical DBs are not the primary boundary, selects per-tenant Redis ACL users plus tenant-scoped resolution, and admits that target is not implemented (`architecture.md:71`, `architecture.md:284`, `architecture.md:603`, `architecture.md:607-617`). The current PRD also labels FR44 hardening in progress and requires NFR8 evidence to be rerun (`prd.md:983-986`, `prd.md:1127`). This honesty is useful, but a “complete” spine cannot leave the enforcement/deletion consequences implicit.

Current deletion removes indices, graph data, broad tenant key families, EventStore mapping/read-model state, then the registry (`TenantDeletionWorkflow.cs:95-205`; `DeleteTenantDataKeysActivity.cs:50-79`). It does not provision or revoke a tenant-specific Redis/Falkor principal because that boundary is not present. It also deletes `dedup:{tenant}:*` but not REST preflight keys in the `ingest-reserve:dedup:{tenant}:*` namespace (`DeleteTenantDataKeysActivity.cs:59-69`; namespace definition at `IngestDedupReservation.cs:36-48`). Those keys expire, but can return an obsolete workflow instance if the same tenant ID is recreated before TTL.

Positive evidence should be preserved: `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs:75-158` uses colliding graph structures/edge IDs and authenticated traversals, while lines 207-260 include planted cross-tenant and malformed-marker negative controls. That validates important content-isolation behavior, not the unimplemented principal boundary.

**Required resolution:** Bind FR38/FR39/NFR8 to explicit creation, use, rotation, revocation, and absence verification for every tenant principal and index/database. Include in-flight workflow fencing, transient reservation/source-artifact/state namespaces, EventStore history/tombstone handling, and behavior when a tenant ID is recreated. Until then, label prefix/database tests as defense-in-depth evidence rather than proof of the selected principal isolation mechanism.

## Medium findings

### DI-07 — Idempotency identity and ordering are stale and internally inconsistent

**Disposition:** **Autofix** the descriptive parts; **discuss** the required identity for each ingress path.  
**Impact:** Contributors can implement incompatible duplicate identities or perform an observable side effect before the purported first idempotency check.

`architecture.md:74`, `architecture.md:188`, and `architecture.md:1250` say duplicate event identity is “event ID + aggregate ID,” attribute it to the rejected Pipeline Actor, and require `CheckIdempotencyActivity` as the first activity. Current code uses:

- REST permanent keys: tenant + case + source URI, augmented by an explicit token (`DedupKeyBuilder.cs:14-34`).
- Event ingestion: tenant + routed case + CloudEvent ID (`EventStoreDedupKey.cs:11-20`; `EventIngestionService.cs:143-180`). Aggregate type is not part of the key.
- REST race prevention: a separate Redis `SET NX` reservation with fail-open semantics (`IngestDedupReservation.cs:36-121`), covered by a repeated two-contender real-Redis test at `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestDedupReservationIntegrationTests.cs:18-46`.
- Workflow order: the first I/O activity updates the case counter at `IngestionWorkflow.cs:52-76`; only then does `CheckIdempotencyActivity` run at lines 81-86. Permanent dedup is written after indexing/verification at lines 467-520.

The mechanisms are defensible, but the spine needs a path-specific identity table, collision/replay behavior, reservation TTL/ownership cleanup, and the reason fail-open remains safe. Replace the Pipeline Actor fossil and avoid claiming the dedup check is literally first unless all earlier effects are replay/duplicate-neutral.

### DI-08 — The spine publishes the storage enum as the product ingestion vocabulary

**Disposition:** **Autofix.**  
**Impact:** API, CLI, metrics, and workflow status can drift between `queued/indexing` and the canonical `pending/projecting` terms.

The candidate's field inventory uses `queued`, `extracting`, `embedding`, `indexing`, `indexed`, `failed` (`architecture.md:100-119`) and its workflow example uses the implementation names. The current PRD declares one operator vocabulary—`pending`, `extracting`, `embedding`, `projecting`, `indexed`, `failed`—and explicitly defines the compatibility mapping `pending` ≙ `Queued`, `projecting` ≙ `Indexing` (`prd.md:80-94`, `prd.md:820-831`, FR10 at `prd.md:1001`).

Current contracts confirm why the mapping must be architectural: `MemoryUnitStatus.cs:5-15` still exposes `Queued` and `Indexing`; `IngestionWorkflow.cs:40-41`, 120-175, 270-273, and 608-610 uses those statuses/custom strings; `IngestionWorkflowStatusMapper.cs:27-77` exposes Dapr runtime status separately and only populates MemoryUnit status from completed output.

Record one translation boundary for contracts, CLI/UI, workflow custom status, counters, telemetry, and persisted values. Do not introduce a third vocabulary or silently rename the V1 enum.

## Low findings

### DI-09 — Integrity tests prove important primitives but not the end-to-end invariants claimed by the spine

**Disposition:** **Defer** as explicit verification work with owners and gates; do not count current unit/component tests as proof.  

Existing tests give useful primitive evidence: real Redis `SET NX` race behavior, projection workflow retry/compensation branches, Dapr-state ETag/CAS behavior, and adversarial tenant marker/traversal checks. Missing load-bearing proofs include:

- File/URL request → EventStore MemoryUnit commit → all three matching projection versions.
- Accepted EventStore command followed by injected scheduling failure, restart, and automatic projection recovery.
- Verification sees one absent/stale projection and cannot yield `indexed`.
- Redis projection loss followed by authoritative enumeration/rebuild of an all-three-absent committed unit.
- Tenant deletion/recreation removes every principal, index/database, Dapr key, dedup/reservation, source-artifact, and route/mapping namespace without touching another tenant.
- The actual G1 no-start-node graph seeding and BM25+semantic control on the frozen ≥50-topic labelled corpus.

## Ratified strengths worth carrying into the replacement spine

- The design correctly rejects a distributed transaction across Redis/FalkorDB and assigns stage orchestration, retries, and compensation to Dapr Workflow (`architecture.md:98`; `addendum.md:49-53`).
- Case create/delete, annotation intent, MemoryUnit deletion intent, tenant registration, and lifecycle status already demonstrate command-before-projection seams.
- Projection fan-out is explicit and compensation is tested; the issue is the terminal verification rule, not the use of a workflow saga itself.
- Dedup relies on Redis-native `SET NX` where atomicity is load-bearing (`architecture.md:679-709`) and has a real-Redis concurrency test.
- The tenant verifier distinguishes structural graph evidence from collision-shaped content evidence (`architecture.md:607-617`), and current integration tests include negative controls.

## Gate disposition

Do **not** accept this legacy document as the architecture spine and do **not** hide DI-01 through DI-03 under Deferred. First resolve the MemoryUnit authority/version/rebuild contract and the terminal `indexed` rule. Then mechanically reconcile the benchmark/graph protocol, mutation ownership matrix, tenant principal/deletion lifecycle, idempotency identities, and status translation with the 2026-09-08 PRD/addendum. Re-run this lens only after the replacement spine names executable invariants and corresponding evidence gates.
