# Architecture Spine Review — Rubric, Security, and Data Integrity

## Verdict

**REVISE before handoff.** The spine is mechanically clean, materially shorter than the legacy architecture, and strong on paradigm, projection roles, migration shape, and explicit brownfield gaps. It does not yet pass the semantic gate because tenant erasure conflicts with the chosen event-source authority, case isolation is not made enforceable, and several durability/security boundaries still permit incompatible implementations.

The deterministic linter passed with zero findings.

## Critical

### C1 — Tenant erasure conflicts with immutable authoritative history

**Evidence**

- `ARCHITECTURE-SPINE.md:59-69` makes Hexalith.EventStore authoritative for deletion and requires every derived store to be rebuildable from its events.
- `ARCHITECTURE-SPINE.md:83-87` assigns tenant cleanup to lifecycle workflows but does not say what happens to authoritative event payloads, snapshots, tombstones, backups, or replay after tenant deletion.
- `prd.md:537` and `prd.md:1039` require tenant deletion to remove all memory units and map it to erasure.
- The current domain includes content-bearing events such as `AnnotationRequestedEvent`, so deleting projections alone does not necessarily remove tenant content from authoritative history.

**Why this fails the spine test**

Two deletion implementations can both follow the present rules yet diverge critically: one can retain the full event history and suppress replay with a tombstone; another can purge or crypto-shred the stream. The first may fail the stated erasure outcome, while an unscoped replay can resurrect deleted content. This is a security and data-integrity boundary, not implementation detail.

**Disposition:** **Discuss and amend before finalizing.** Decide the authoritative-store erasure model: physical stream purge, crypto-shredding, payload redaction with irreversible key destruction, or an explicitly narrower product claim. Then bind replay, backup/restore, tombstone retention, re-registration of a deleted tenant ID, and completion evidence to that choice. AD-2, AD-6, the deletion capability map, and Operational Boundaries must agree.

## High

### H1 — Case isolation is named but not enforced by any Rule

**Evidence**

- AD-5 Binds tenant/case access and says caller-supplied case membership cannot authorize access, but its Rule (`ARCHITECTURE-SPINE.md:77-81`) specifies only tenant-claim authorization and tenant-scoped credentials.
- AD-9 requires tenant scope over graph paths but not case scope (`ARCHITECTURE-SPINE.md:101-105`).
- AD-8 auto-seeds graph traversal from syntactic/semantic hits without saying those seeds and traversed nodes must remain in the selected case (`ARCHITECTURE-SPINE.md:95-99`).
- The PRD requires strict single-case ownership and case-scoped graph edges (`prd.md:1029-1031`), and G4 is a ship gate.
- “Cross-case references” is Deferred (`ARCHITECTURE-SPINE.md:259`), but the current rule does not forbid them until that decision is made.

**Impact**

Search, graph, deletion, and annotation units can independently choose incompatible case checks. In particular, a graph traversal can satisfy tenant isolation while leaking structure or results across cases.

**Disposition:** **Autofix.** Add a binding rule that authoritative case membership is resolved server-side; each unit has exactly one case; case-scoped search, graph seeds, every path node/edge, annotations, mutation, and deletion enforce it; cross-case edges/references remain forbidden until the Deferred item is resolved.

### H2 — “Same source version acknowledgement” lacks an identity and checkpoint protocol

**Evidence**

- AD-3 gates `Indexed` on all three projections acknowledging “the same source version” (`ARCHITECTURE-SPINE.md:65-69`).
- The diagram repeats the comparison but names neither the version token nor the authority that commits completion (`ARCHITECTURE-SPINE.md:195-210`).
- AD-4 requires at-least-once-safe activities, but there is no monotonicity/CAS rule for late acknowledgements (`ARCHITECTURE-SPINE.md:71-75`).
- NFR16 requires zero EventStore-committed units missing after projection loss and replay (`prd.md:1145`).

**Impact**

Independent axis implementations can call aggregate version, event ID, content revision, or workflow attempt “source version.” A delayed acknowledgement from an older attempt can incorrectly complete a newer unit, and concurrent repair/migration can regress a checkpoint.

**Disposition:** **Discuss, then amend.** Name the canonical projection identity tuple (at minimum tenant, unit/aggregate, authoritative event or stream version, schema/model generation), the durable owner of the completion record, monotonic compare-and-set semantics, stale-ack rejection, and how repair/replay advances it. Also state that degraded reads may use available axes per NFR18 but cannot promote an incompletely acknowledged revision to `Indexed`.

### H3 — The idempotency boundary is asserted but not made authoritative

**Evidence**

- AD-4 says activities are safe under at-least-once execution (`ARCHITECTURE-SPINE.md:71-75`).
- AD-7 permits a direct Redis `SET NX`+TTL reservation (`ARCHITECTURE-SPINE.md:89-93`) but does not name the stable idempotency key, completion/commit transition, TTL ownership, or failure posture.
- Current `RedisPreflightDedupStore` explicitly fails open and says workflow-level dedup is authoritative, while the spine never records that second authority.

**Impact**

A transient reservation can expire during a long workflow, disappear during Redis loss, or fail open. Without an EventStore/workflow-level durable uniqueness rule, duplicate domain events or duplicate projection side effects remain compatible with the spine.

**Disposition:** **Discuss, then amend.** Make the authoritative idempotency owner and key explicit. Treat Redis reservation as admission optimization only; bind durable duplicate suppression to EventStore command/message identity and require every projection write to be an idempotent upsert for the source-version tuple. Specify reservation release/expiry and retry behavior.

### H4 — The ports-and-adapters rule is not enforceable and incompletely reconciles brownfield code

**Evidence**

- The dependency diagram depicts adapters as a distinct inward-dependent layer (`ARCHITECTURE-SPINE.md:36-46`).
- AD-7 allows direct provider clients only inside “approved Redis and FalkorDB adapters” but never identifies an approval boundary (`ARCHITECTURE-SPINE.md:89-93`).
- Structural Seed says Server contains both application services and adapters (`ARCHITECTURE-SPINE.md:176-187`).
- Brownfield Server directly references `NFalkorDB`, `NRedisStack`, and `StackExchange.Redis` (`src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj:56-58`); `SearchEndpoints` receives `IConnectionMultiplexer` and executes `NFalkorDB` calls directly (`src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs:56-67`, `1191-1208`). This AD-7 violation is absent from Current Alignment Gaps.
- `Hexalith.Memories.Redis` is intentionally compatibility-only, so the project name cannot silently serve as the adapter boundary.

**Impact**

Any new class can self-identify as an adapter. Separately built endpoint, search, import, consistency, and lifecycle units can continue exposing provider types while claiming compliance.

**Disposition:** **Autofix after choosing the existing-code-compatible seam.** Define the actual enforceable boundary—named namespaces/folders plus architecture guards, or dedicated internal adapter assemblies—and require endpoints, workflows, domain services, and public contracts to depend only on ports. Add the current endpoint/service leakage to Current Alignment Gaps. Do not imply the compatibility-only Redis package owns adapters unless that is a separately adopted migration.

### H5 — Inter-service authentication conflates Dapr channel tokens with workload and tenant authorization

**Evidence**

- Structural Seed says service calls use “Dapr application identity/token boundaries” (`ARCHITECTURE-SPINE.md:213`); Operational Security calls this an “application-token boundary” (`ARCHITECTURE-SPINE.md:220`).
- AD-5 does not define how end-user/tenant authority survives MCP-to-Server or other service invocation.
- The committed Dapr configuration has a stronger, deny-by-default app-id/namespace/trust-domain access-control policy (`deploy/kubernetes/base/dapr/config.yaml:24-36`), while the MCP documentation says the validated bearer is forwarded unchanged.

**Impact**

One implementation can treat possession of a Dapr API token as sufficient tenant authorization; another can require Dapr workload policy plus the original user bearer. Both fit the current wording, but only the latter preserves NFR8/NFR10/NFR11 across hops.

**Disposition:** **Autofix.** Distinguish three layers: external OIDC/JWT identity and tenant claims; Dapr mTLS/access-control workload authorization with deny-by-default app-id/namespace/trust-domain policies; and local app-to-sidecar/sidecar-to-app API tokens. Require propagation or verifiable delegation of user/tenant authority across product service hops; Dapr channel authentication never substitutes for tenant authorization.

### H6 — Performance, fairness, and backpressure are an owned dimension left silent

**Evidence**

- Frontmatter claims all NFR1-NFR36 (`ARCHITECTURE-SPINE.md:11-15`).
- The PRD fixes latency, throughput, freshness, tenant-scaling, independent ingestion, rate-limit, and cold-start outcomes (`prd.md:1111-1121`, `1132-1139`, `1156`, `1195`).
- The spine provides graph query bounds and independent Server/MCP scaling, but no invariant or Deferred item for tenant-partitioned admission, queue/concurrency ownership, provider throttling, durable backoff, or capacity envelopes.

**Impact**

Separate ingestion, batch, embedding, and migration units can choose global queues/semaphores or tenant-partitioned controls incompatibly. A single tenant or provider recovery herd can starve others while every unit still follows the spine.

**Disposition:** **Discuss and add one compact AD.** Bind tenant-partitioned admission/concurrency, bounded queues, durable provider `retry-after` waits rather than short in-memory retry loops, and explicit global-versus-tenant quota ownership. Keep numeric tuning in configuration/PRD evidence. Add capacity and single-tenant noisy-neighbor behavior to the operational envelope or Deferred with a pre-production revisit condition.

## Medium

### M1 — Access-telemetry retention is too abstract for NFR34

AD-14 says “independently owned and lifecycle-bound” but does not bind the Platform Operations owner, tenant-erasure mapping, configured TTL source, purge progress, or accepted-debt path required by `prd.md:1188`. Those are exactly the seams that writers, lifecycle actors, adapters, and tenant deletion can implement incompatibly.

**Disposition:** **Autofix.** Add these contract-level obligations without copying the detailed ADR mechanics; reference the accepted access-telemetry ADR as seed/evidence.

### M2 — “OpenBao HA mode” overstates the current failure domain

Operational Deployment says production operates OpenBao in HA mode (`ARCHITECTURE-SPINE.md:223`). The pinned values do use three Raft voters, but all are on one Kubernetes node and explicitly provide no node-level availability (`deploy/openbao/values.yaml:158-164`).

**Disposition:** **Autofix.** Say “three-voter process/pod failover within one node; no node/site HA claim” and include node-level secret-service availability under the existing HA Deferred item or a dedicated revisit condition.

### M3 — Structural Seed omits architecture-significant current projects

The seed omits `Hexalith.Memories.Redis`, `Hexalith.Memories.Telemetry`, `Hexalith.Memories.AccessTelemetry.Contracts`, `Hexalith.Memories.AccessTelemetry`, and `Hexalith.Memories.AccessTelemetry.Clock`, even though AD-14, the capability map, AppHost references, and the release inventory depend on them.

**Disposition:** **Autofix.** Add compact grouped entries and accurately label Redis as compatibility-only. This also makes the access-telemetry ownership boundary visible.

### M4 — A declared companion does not exist

Frontmatter names `docs/requirements-to-implementation-traceability.md` (`ARCHITECTURE-SPINE.md:25-27`), but that path is absent. The repository currently contains `_bmad-output/test-artifacts/traceability/traceability-matrix.md`.

**Disposition:** **Autofix.** Point to the intended real companion or remove the entry.

## Low

### L1 — “Opaque ordinal string” can imply an undocumented ordering contract

The convention calls `MemoryUnitId` an “opaque ordinal string” (`ARCHITECTURE-SPINE.md:148`) while AD-11 forbids inferred meaning and AD-8 uses ordinal comparison only as a tie-break. “Ordinal” can be read as sequence semantics rather than `StringComparison.Ordinal`.

**Disposition:** **Autofix.** Say “opaque stable string; compare with ordinal string comparison only where a deterministic tie-break is required.”

## Rubric Summary

| Criterion | Result | Note |
| --- | --- | --- |
| Short and direct | Pass | Fifteen compact ADs, two useful diagrams, and concise seed/gap/deferred sections. |
| Real divergence points fixed | Fail | Erasure, case authorization, projection checkpointing, idempotency, adapter ownership, and fairness remain divergent. |
| Rules enforce Binds/Prevents | Partial | Most do; AD-3, AD-5, and AD-7 are not enforceable enough for their stated prevention. |
| Deferred items safe | Partial | Cross-case references are deferred without an explicit prohibition; HA deferral understates current OpenBao failure-domain limits. |
| Technology current/pinned | Pass | Exact repository pins are recorded and the mechanical linter accepts them. |
| Brownfield grounded | Partial | Current gaps are valuable but omit widespread provider leakage and the Redis compatibility-only topology. |
| PRD capability coverage | Partial | Core functional flows are mapped; case isolation, erasure, performance/scaling, and NFR34 are not fully governed. |
| Operational envelope complete | Partial | Deployment, reliability, observability, security, and evolvability appear; capacity/fairness, erasure, and precise service identity remain incomplete. |

## Strengths to Preserve

- The named paradigm is clear and the event-truth/projection split is easy for builders to follow.
- AD-4’s workflow/activity/actor split is concise and genuinely convergence-producing.
- AD-8 and AD-12 capture deterministic fusion and safe staged migrations without recreating the whole implementation.
- Current Alignment Gaps makes target-versus-reality explicit rather than disguising brownfield debt.
- Deferred items have useful revisit conditions, and the stack is pinned rather than aspirational.

## Second Pass — Revised Spine

### Verdict

**FAIL.** The revision fully resolves projection checkpoint identity, inter-service security, capacity/fairness, the missing companion, the OpenBao failure-domain wording, and structural-seed omissions. It materially improves the other findings, but four high-severity convergence blockers remain and two regressions were introduced.

The deterministic linter again passed with zero findings.

### Prior-finding resolution

| Prior finding | Result | Second-pass assessment |
| --- | --- | --- |
| C1 tenant erasure versus EventStore truth | **Partially resolved** | AD-16 now chooses EventStore crypto-shredding, content-free tombstones, replay/non-reuse denial, and restore quarantine. Its access-telemetry purge clause conflicts with the accepted telemetry lifecycle; see SP-H3. |
| H1 case isolation | **Partially resolved** | AD-7 now establishes authoritative single-case ownership and path checks, but over-constrains tenant-wide cross-case discovery required by FR34; see SP-H1. |
| H2 source-version checkpoints | **Resolved** | AD-3 names the full tuple, a single coordinator, monotonic ETag/CAS, stale-ack rejection, completion state, and repair/replay reuse. AD-10 separates degraded reads from ingestion completion. |
| H3 idempotency authority | **Partially resolved** | AD-4 correctly demotes Redis preflight to fail-open admission optimization and makes EventStore/workflow dedup durable, but its CloudEvent identity is under-scoped; see SP-H2. |
| H4 adapter boundary | **Partially resolved** | AD-8 names adapter assemblies/namespaces, architecture-test enforcement, and the compatibility-only Redis role; Current Alignment Gaps records existing leakage. Its direct-Redis exception was broadened into unenforceable categories; see SP-H4. |
| H5 inter-service identity | **Resolved** | AD-5 and Operational Security distinguish bearer authority, Dapr mTLS/access-control workload identity, app tokens, system principals, and tenant grants. |
| H6 fairness/backpressure | **Resolved** | AD-18 binds tenant/global quota ownership, partitioned admission, bounded durable queues, workflow timers, priority, and evidence while leaving numeric budgets in the PRD. |
| M1 access-telemetry ownership | **Resolved except SP-H3 conflict** | AD-17 now binds owner, TTL, purge progress, erasure mapping, recovery, debt, activation, and post-admission behavior. |
| M2-M4 and L1 | **Resolved** | OpenBao failure scope is accurate, significant projects and the Redis compatibility role are present, the companion exists, and opaque-ID wording is unambiguous. |

### Unresolved blockers

#### SP-H1 — Case isolation now conflicts with tenant-wide cross-case search

AD-7 requires “the selected case on every seed [and] result” (`ARCHITECTURE-SPINE.md:92-96`), while the PRD requires tenant-wide search across all cases with case attribution (FR34, `prd.md:1031`). AD-9 also auto-seeds one graph traversal from the union of top hits (`ARCHITECTURE-SPINE.md:104-108`) without defining behavior when those hits belong to different cases.

This is not the Deferred cross-case-reference question: tenant-wide result discovery is already required, while graph edges/paths remain case-local. As written, one search implementation can reject a tenant-wide request for lacking a selected case; another can seed a graph across cases.

**Required fix:** Distinguish authoritative case ownership from query scope. A case-scoped query filters every candidate/result/path to its requested case. A tenant-wide query may return independently ranked units from multiple cases with mandatory attribution, but graph seeding/traversal is partitioned by each seed's authoritative case and never crosses case boundaries. Mutations and deletion verify the target unit's authoritative owner case. Keep cross-case edges/references forbidden.

#### SP-H2 — CloudEvent deduplication omits the publisher source

AD-4 namespaces CloudEvent `id` only by tenant and case (`ARCHITECTURE-SPINE.md:74-78`). CloudEvents identity is scoped by `source` plus `id`; multiple routed publishers can legally reuse an `id`. Omitting `source` permits a false duplicate to suppress a distinct event and therefore creates data loss. The current integration also routes by source prefix, confirming source is semantically material.

**Required fix:** Define EventStore ingestion identity as at least `(tenantId, caseId, CloudEvent source, CloudEvent id)` using canonical byte/string semantics. If V1 idempotency tokens are accepted by more than one command intent, include the command/operation scope as well. Preserve the rule that Redis reservations are advisory and projection upserts use AD-3's tuple.

#### SP-H3 — AD-16 contradicts the accepted access-telemetry retention contract

AD-16 says tenant deletion completes only after “access-telemetry mappings are purged” (`ARCHITECTURE-SPINE.md:146-150`). The accepted access-telemetry ADR explicitly says tenant deletion does not erase retained telemetry early and that opaque markers remain until normal bounded expiry (`docs/dev/adr-27.1-001-access-telemetry-lifecycle.md:686-691`); accelerated retroactive purge requires a separate accepted operation (`:242-249`). AD-17 otherwise preserves that lifecycle.

The term “mappings” is undefined, so independently built deletion and lifecycle units can interpret it as early record deletion, removal of a lookup authority, or merely evidence linkage. The first contradicts the ADR; the second can make retained records ungovernable.

**Required fix:** Align AD-16 with the accepted lifecycle: tenant deletion records the erasure mapping and removes product data/projections, while retained opaque access telemetry follows its bounded TTL/purge contract and is not a legal-erasure claim. An early purge is permitted only through a separately adopted accelerated-purge operation. State whether tenant deletion waits for any telemetry step and name that step precisely.

#### SP-H4 — Direct-Redis exceptions are broader than the adapter invariant can police

AD-8 now permits direct Redis atomic primitives for the broad categories “reservation/idempotency, leases/fences, registries, and migration CAS/transactions when Dapr cannot express the operation” (`ARCHITECTURE-SPINE.md:98-102`). The prior source decision allowed a specifically named preflight reservation exception while moving aggregate-case mapping and observed-event registries to Dapr state. “When Dapr cannot express” has no decision owner or evidence threshold, and “registries” can cover most current Redis state.

Two units can now choose Dapr state or a new direct Redis registry incompatibly while both claim AD-8 compliance. This weakens the ports-and-adapters and portability rules and risks Redis becoming domain truth again.

**Required fix:** Replace category-wide permission with a finite exception registry naming each port/operation, its required atomic semantic, failure posture, key/tenant scope, and test evidence. New exceptions require an architecture decision; absence from the registry means Dapr/portable port. Preserve the accepted direct preflight reservation exception and the already-adopted Dapr-state mappings unless explicitly superseded.

### Non-blocking regression

#### SP-M1 — Tied-rank progression remains ambiguous

AD-9 says tied raw scores share rank but does not state whether the next rank uses competition ranking (`1,1,3`) or dense ranking (`1,1,2`). Those choices produce different RRF contributions and can make independently built axes diverge despite deterministic tie-breaking.

**Required fix:** Name the ranking method and specify how null/NaN/duplicate axis results are handled before ranking. This is a compact autofix.

## Third Pass — Targeted Blocker Verification

### Verdict

**PASS.** All four second-pass high blockers and the non-blocking RRF ambiguity are resolved without a regression in the affected rules. The deterministic linter passes with zero findings.

### Verification

| Target | Result | Evidence |
| --- | --- | --- |
| Tenant-wide search with case-local graphs | **Resolved** | AD-7 now distinguishes case-scoped filtering from tenant-wide discovery, requires case attribution, partitions graph seeding/traversal by each seed's authoritative case, and forbids cross-case paths (`ARCHITECTURE-SPINE.md:92-96`). AD-11 independently enforces tenant and case scope over every graph node and edge (`:116-120`). |
| CloudEvent identity includes source and id | **Resolved** | AD-4 scopes durable identity by tenant, case, exact validated CloudEvent `source`, and `id`, with ordinal comparison and no post-validation normalization; V1 tokens also include command/operation scope (`:74-78`). |
| Telemetry erasure follows retained TTL | **Resolved** | AD-16 records a durable erasure handoff but explicitly does not wait for or force early deletion of retained opaque telemetry; AD-17 owns bounded TTL/purge, and accelerated purge requires a separate adopted operation (`:146-156`). This now matches the accepted access-telemetry ADR. |
| Direct Redis registry is finite | **Resolved** | AD-8 makes the finite registry the only coordination-state bypass (`:98-102`). The registry contains only `IPreflightDedupStore.TryReserveAsync` / `ReleaseAsync`, including primitive, failure posture, scope, and mandatory evidence; all other registries/leases/fences/mappings/migration coordination default to Dapr state unless a later AD adds a row (`:186-194`). |
| RRF competition ranking and preprocessing | **Resolved** | AD-9 drops blank IDs and non-finite scores, deduplicates exact IDs in provider order, fixes competition ranks as `1,1,3`, distinguishes null from empty axes, fixes denominator behavior, and specifies final ordinal tie-breaking (`:104-108`). Current implementation divergence is explicitly recorded as an alignment gap (`:301`). |

### Remaining gate result

No unresolved critical or high finding remains from this review lens. The spine is ready to proceed through the rest of the configured reviewer gate.
