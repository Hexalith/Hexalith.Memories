---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-03-24'
revisedAt: '2026-03-25'
revisionNote: 'DAPR as first-class citizen: Workflow + Actors (D23-D25), Conversation API for LLM (D26), Dapr Agents Python sidecar (D27), Polyglot architecture (D28)'
inputDocuments:
  - '_bmad-output/planning-artifacts/prd.md'
  - '_bmad-output/planning-artifacts/product-brief-Hexalith.Memories-2026-03-22.md'
  - '_bmad-output/brainstorming/brainstorming-session-2026-03-21-1530.md'
workflowType: 'architecture'
project_name: 'Hexalith.Memories'
user_name: 'Jerome'
date: '2026-03-24'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

> **Read This First:** If you're a contributor, start with the [Gate-Blocking vs Deferrable Summary](#gate-blocking-vs-deferrable-summary) — it tells you exactly what matters for MVP and what can wait.

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
74 requirements across 10 categories. The heaviest areas are Knowledge Ingestion (13 FRs), Knowledge Retrieval (12 FRs), and Memory Organization (12 FRs) — together these form the core memory engine. Causal Intelligence (7 FRs) is architecturally distinct, requiring typed graph semantics with gap detection and confidence-tracked edges. Developer Interfaces (6 FRs) mandate four distinct API surfaces with different response shaping. Trust & Transparency (5 FRs) and Embedding Provider Management (3 FRs) are cross-cutting concerns that affect every component.

**Non-Functional Requirements:**
31 NFRs drive architecture in four critical dimensions:
- **Performance:** Tiered latency targets per axis (<200ms syntactic, <500ms semantic, <1s hybrid, <2s graph). Ingestion throughput >100 units/min. Event freshness <5s.
- **Security:** Zero cross-tenant data leakage (hard gate). Physical index isolation. DAPR API token authentication. Ingress-layer auth for external access.
- **Reliability:** Zero data loss on restart (AOF). Pipeline state survives restarts (DAPR actors). Partial backend failure → degraded service, not total failure.
- **Algorithmic Quality:** Hybrid search uses deterministic weighted reciprocal-rank fusion with bounded rank-contribution scores. Single-axis scores keep axis-specific semantics. Reproducible benchmark results.

**Top Architectural Drivers — the 5 requirements that most constrain the architecture:**

1. **NFR24-26 (Algorithmic Quality):** Fusion must be deterministic with bounded rank-contribution scores. Forces the fusion algorithm to be a **pure function** with explicit inputs — no hidden state or backend calls.

2. **NFR8 (Zero cross-tenant leakage):** Hard gate. Forces physical index isolation, tenant context validation in every query path, parameterized graph queries, and provisioning rollback. Non-negotiable.

3. **FR6 (Memory unit fully searchable across all axes after ingestion):** Forces the multi-backend consistency pattern: EventStore command acceptance for domain truth, rebuildable Redis/FalkorDB projections, and workflow retry/compensation for projection fan-out. Every ingestion must verify all three search backends have the data.

4. **NFR17 (Pipeline state survives restarts):** Forces DAPR Workflow for pipeline orchestration. Workflow state is automatically persisted via the Durable Task Framework — survives restarts, sidecar failures, and redeployments. Each activity (extract, embed, index) is individually retriable with configurable exponential backoff. No custom queue management needed.

5. **FR46-49 (Causal chain traversal with gap detection):** Forces typed, directional graph edges with explicit gap markers. Combined with DAPR's out-of-order delivery, forces retroactive gap-filling when late events arrive.

**Scale & Complexity:**

- Primary domain: API Backend / AI Infrastructure
- Complexity level: High
- Estimated architectural components: 7 published NuGet packages plus 3 non-packable service/orchestration projects, 3 backend systems (RediSearch, Redis Vector, FalkorDB), 4 interface layers (CLI, MCP, REST, DAPR), per-tenant pipeline actors. `tools/release-packages.json` is the release package source of truth.

### Technical Constraints & Dependencies

| Constraint | Impact | Rationale (First Principles) |
|---|---|---|
| .NET 10 / C# 13 | Runtime, language features, Aspire compatibility | Platform choice |
| DAPR | Load-bearing infrastructure: workflows, actors, conversation (AI), pub/sub, state, service invocation, secrets | **First-class citizen, polyglot enabler, and ecosystem decision** — alignment with Hexalith.EventStore. DAPR's value is infrastructure portability, ecosystem coherence, rich building blocks, and **language-agnostic service invocation**. Use **DAPR Workflow** (.NET) for core domain orchestrations. Use **DAPR Actors** (.NET) for per-tenant stateful singletons. Use **DAPR Conversation API** for provider-agnostic LLM communication. Use **Dapr Agents** (Python) for AI enrichment — the GA Python SDK runs as a sidecar service called via DAPR service invocation. **Polyglot principle:** when a Python/other-language library is the best fit for a major feature, create a service in that language and call it through DAPR — do not reimplement in C#. |
| .NET Aspire | Orchestration, local dev, health checks, observability defaults | Development experience and deployment model |
| Redis Stack (SSPL/RSAL) | Cannot offer as competing managed service; self-host or Redis Cloud | **Operational simplicity** — one infrastructure for three capabilities. Trade-off: capability depth (RediSearch ≠ Elasticsearch, Redis Vector ≠ Qdrant, FalkorDB ≠ Neo4j). Extraction points address this. Avoid deep dependencies on RediSearch-specific query syntax in domain logic — wrap query construction behind a builder pattern. |
| FalkorDB (AGPL) | Architectural boundary via DAPR required; extraction point for future swap. **Unvalidated at scale** — graph traversal performance at >100K nodes, depth >3 needs early benchmarking. **Memory is shared at process level** — physical database isolation does NOT provide memory isolation; memory-heavy tenants risk OOM affecting all co-located tenants. | Graph engine for causal intelligence. Decision: FalkorDB for MVP (see resolved decision below). |
| references/Hexalith.Commons (git submodule) | Error handling conventions, shared base types. **Build script must detect missing submodules** and print helpful error instead of cryptic MSBuild failures. | Ecosystem consistency |
| references/Hexalith.EventStore (git submodule) | Event types, versioning conventions, DAPR integration patterns | Zero-code integration target |
| Embedding provider APIs | External dependency for vector generation; rate limits, latency, cost. **Provider outage halts ingestion for all affected tenants.** Thundering herd risk on recovery — workflow retry policies with jitter. Shared rate limiter coordination deferred to Phase 3. | External chokepoint |
| Polyglot services via DAPR | When a Python/other-language library is the best fit for a major feature, create a service in that language and call it through DAPR service invocation. **DAPR makes the calling language invisible** — C# workflows call Python services identically to C# activities. The Dapr Agents Python SDK (GA 1.0.0) is the primary example: AI enrichment runs as a Python sidecar service rather than reimplementing agent patterns in C#. | Best-of-breed per feature; DAPR service invocation as universal glue |
| JSON-only serialization | All service communication; CloudEvents for pub/sub | Simplicity and interoperability |
| Physical tenant isolation | Separate indexes per tenant plus the Story 24.3 target of per-tenant Redis ACL users resolved through tenant-scoped backend routing. **Defense-in-depth**: even a query filter bug can't leak data because data is not accessible through the wrong tenant backend principal. FalkorDB isolation must be at database level, not label level. Prefixes, hash tags, and logical DBs are placement tools, not the primary security boundary. | Makes leakage a configuration/access-control error, not a query-filter error |
| DAPR Workflow model | Workflow state persisted via Durable Task Framework in actor state store. Workflows are event-sourced — incremental append-only history. Activities are individually retriable with `WorkflowRetryPolicy`. Compensation via try/catch in workflow orchestration. Workflows survive restarts. | Replaces custom actor-based queue management for orchestrations |
| DAPR actor state model | Virtual actor pattern. Per-tenant actors for stateful singletons (rate limiting, corpus stats). **State must be persisted before every response** — not batch-persisted on deactivation. Actor idle timeout configured per actor type via `entitiesConfig`. | Per-entity state management; complements workflows |
| DAPR pub/sub delivery | At-least-once delivery semantics. **Ingestion must be idempotent** — duplicate event detection by source identifier (event ID + aggregate ID) is required. **Message ordering is NOT guaranteed** — causal chain gap markers must be fillable retroactively when out-of-order events arrive. Graph edge updates must be idempotent. | Prevents duplicates; handles reordering |
| RediSearch/Vector index schemas | Immutable after creation. **Schema evolution must be additive-only** or use create-backfill-switch migration pattern. Index naming should support concurrent versions (`{tenant}:{model-version}:syntactic`) for future model migration. | Deployment and versioning constraint |
| Backend selection scope | Must be **per-tenant, not global** — tenant configuration must support different backends per tenant for the migration escape hatch to work. | Enables gradual migration (e.g., one tenant to Qdrant while others remain on Redis) |

### Cross-Cutting Concerns Identified

1. **Tenant Isolation (NFR8, FR38-45) [MVP-critical]:** Enforced at 4 layers — API validation, tenant-scoped backend authorization, DAPR actor scoping, and graph query isolation. Every component must be tenant-aware. Physical index separation means tenant provisioning/deletion is a multi-backend operation requiring atomicity or rollback handling. Story 24.3 ratifies per-tenant Redis ACL users plus tenant-scoped backend resolution as the target Redis security boundary; per-tenant RediSearch/vector indexes remain lifecycle resources, while prefixes/hash tags/logical DBs are placement aids only. FalkorDB isolation must be at the database level, not label/namespace level. Cypher queries must use parameterized queries to prevent injection. **Tenant deletion at scale is a potentially blocking operation — async deletion with progress tracking required; graph deletion must not block other tenants' queries (batched deletion: delete N nodes per transaction, yield between batches).**

2. **Observability (NFR27-29) [MVP-critical]:** OpenTelemetry traces must propagate across all DAPR hops (CLI → ingress → Server → MCP → backends). Structured JSON logging with DAPR trace context correlation IDs. Custom metrics per tenant (ingestion throughput, search latency per axis, index size, pipeline queue depth). Aspire dashboard for local dev.

3. **Error Propagation (FR56) [MVP-critical]:** MVP error format: error code + human-readable message + recovery suggestion. JSON structure: `{"code": "TENANT_NOT_FOUND", "message": "...", "suggestion": "Run 'memories tenant list'..."}`. Full Hexalith.Commons error envelope integration (failed component, trace context, nested errors) is Phase 1.5 when MCP needs structured error mapping. CLI must translate error codes to actionable guidance.

4. **Confidence & Provenance (FR7, FR63-65) [MVP-critical]:** Every memory unit tracks metadata origin (human-declared vs AI-inferred) with confidence scores per field. AI-inferred metadata is produced by `AiEnrichmentWorkflow` activities using the DAPR Conversation API — the `origin` field distinguishes `human` from `ai` and the `confidence` score reflects LLM certainty. Every search result carries composite + per-axis scores. Every memory unit records `ingested_by`. This metadata schema is load-bearing across ingestion, storage, and retrieval.

5. **Rate Limiting & Throttling (FR8, FR69, NFR22) [MVP-critical]:** Per-tenant `EmbeddingRateLimiterActor` (DAPR virtual actor) enforces embedding API throttle ceilings. DAPR Workflow retry policies handle 429 responses with exponential backoff per activity. Shared API keys = shared rate limits across tenants. **Pipeline resource isolation covers all stages (extraction, embedding, indexing) — CPU-intensive extraction (PDF, URL fetch) bounded per tenant via workflow concurrency control, not just embedding API calls.** Thundering herd coordination across tenants deferred to Phase 3 — per-tenant jittered retry via workflow retry policies sufficient for MVP.

6. **Backend Portability (NFR15) [MVP-critical]:** Concrete Redis implementation first. Extraction points for IMemoryIndex/IMemoryGraph identified but not prematurely abstracted. Server depends on Contracts only — Redis registered at composition root. This avoids breaking NuGet version bumps when extracting interfaces in Phase 2/3. Redis scores highest on operational simplicity (critical for solo-developer MVP) but lowest on capability depth and scalability ceiling. PostgreSQL + pgvector + Apache AGE is a viable future migration target alongside Qdrant, offering clean licensing and single-engine simplicity.

7. **Serialization & Protocol Conformance (phase-split):** JSON contract serialization and source-generated round-trip tests are MVP-critical. MCP protocol conformance (NFR20) and DAPR CloudEvents publisher compatibility for EventStore ingestion (NFR21) are Phase 1.5 fast-follow scope; the MVP architecture must keep these additions additive, not count them as MVP gate completion.

8. **Fusion Algorithm (FR17, FR19, NFR24-26) [MVP-critical]:** The architectural center of gravity — depends on available retrieval axes producing stable rankings and feeds all 4 interface layers. Story 22.4 selected a corpus-invariant, rank-based implementation: weighted reciprocal-rank fusion. Raw BM25, cosine, and graph-proximity magnitudes are not averaged in hybrid scoring; explain metadata exposes rank-contribution semantics. Three-axis retrieval remains a hypothesis, not a given — the graph axis must be architecturally optional so the system degrades gracefully to two-axis if the hypothesis fails.

   **Epic 26 calibration:** Default three-axis fusion uses RRF `k=10` and live syntactic/semantic/graph weights `0.30/0.35/0.35`; the optional NL default remains `0.20` and NL remains default-off. The lower constant restores meaningful top-10 rank decay (`rank10/rank1 = 0.55`, versus `0.871` at `k=60`). Explicit request or tenant configuration remains authoritative. Durable `StoredFusionWeights` fallback values remain unchanged for backward compatibility. Benchmark data, NDCG@10, strict-winner semantics, the 80% hard line, and exact-repeat reproducibility are governance controls and must not be altered as calibration implementation.

9. **Multi-Backend Consistency (FR6, FR13) [MVP-critical]:** "Atomic write across all three backends" is not achievable — no distributed transaction exists across Redis + FalkorDB. **Story 21.1 ratifies the EventStore aggregate model as the consistency target for `Case`, `MemoryUnit`, and `Tenant`: domain state is sourced from Hexalith.EventStore events, while RediSearch syntactic hashes, Redis Vector entries, FalkorDB nodes/edges, case activity streams, and tenant registry/read records are rebuildable projections/read models.** DAPR Workflow saga/compensation remains the required delivery mechanism for projection fan-out and tenant infrastructure side effects, not the domain source of truth. `Case` and `MemoryUnit` commands must first append/accept EventStore events, then project to Redis/FalkorDB with idempotent activity retries and compensation for partial projection writes. `Tenant` lifecycle commands must use EventStore events for registry/status semantics; backend provisioning/deletion remains workflow-owned because those are infrastructure side effects. Story 21.2 implements the command-acceptance boundary for case, annotation, memory-unit deletion, case deletion, and tenant lifecycle mutation paths; new domain mutation paths must follow that boundary instead of treating direct Redis/FalkorDB or Dapr state writes as authoritative. `IngestionWorkflow` writes to each backend as separate activities — `IndexSyntacticActivity` (RediSearch), `IndexSemanticActivity` (Redis Vector), `IndexGraphActivity` (FalkorDB) — each with its own `WorkflowRetryPolicy` (exponential backoff). If any activity fails after retries, the workflow executes compensation activities to clean up partially-written projection state. `VerifyConsistencyActivity` runs after all writes succeed. `memories tenant verify` triggers `ConsistencyVerificationWorkflow` for operator-initiated audits. Per-memory-unit consistency inspection (`memories consistency inspect --tenant <tenant-id> --id <unit-id>`) queries all three backends for a single unit's state — essential for operator and developer debugging. This command is owned by Epic 8 operational consistency work, not by the root MVP CLI essentials list in Epic 7.

### Memory Unit Field Inventory (Draft)

| Field | Type | Required | Source | Notes |
|---|---|---|---|---|
| `Id` | string (opaque) | Yes | Generated | Workflow `InstanceId` (a GUID) or a fresh GUID via `ResolveMemoryUnitId`; opaque, **not** a ULID and **not** time-sortable. Stability semantics: `docs/dev/memory-unit-id-stability.md` |
| `TenantId` | string | Yes | Caller | Physical index routing key |
| `CaseId` | string | Yes | Caller | Strict ownership — one unit, one case |
| `Content` | string | Yes | Extracted | Raw text content (extracted from source) |
| `ContentHash` | string | Yes | Computed | SHA-256 of content — enables dedup detection (Growth-phase) |
| `SourceUri` | string | Yes | Caller | File path, URL, or event ID |
| `SourceType` | enum | Yes | Caller | `file`, `url`, `event`, `command`, `projection`, `discussion` |
| `IngestedBy` | string | Yes | Auth context | User or system identity (FR65) |
| `IngestedAt` | DateTimeOffset | Yes | Generated | Ingestion timestamp |
| `LastUpdated` | DateTimeOffset | Yes | Generated | Last modification — enables freshness awareness |
| `Status` | enum | Yes | Pipeline | `queued`, `extracting`, `embedding`, `indexing`, `indexed`, `failed` |
| `Metadata` | Dictionary<string, MetadataField> | No | Caller + AI | Each field: `value`, `origin` (human/ai), `confidence` (0.0-1.0) |
| `EmbeddingProvider` | string | Yes (post-embedding) | Config | Provider + model (e.g., `google:text-embedding-004`) |
| `EmbeddingDimensions` | int | Yes (post-embedding) | Derived | Vector dimensions |
| `Classification` | string | No | Caller | Optional data classification — schema-present for Phase 4 redaction |
| `FailureDetails` | object | No | Pipeline | Stage, error code, retry count — populated only in `failed` status |

**Graph Edge Model:**

| Field | Type | Notes |
|---|---|---|
| `Id` | string | Edge identifier |
| `SourceId` | string | Source memory unit ID |
| `TargetId` | string | Target memory unit ID |
| `EdgeType` | enum | `caused_by`, `correlated_with`, `references`, `contains`, `annotates` |
| `Confidence` | float | Default by type: caused_by=1.0, correlated_with=0.8, references=0.5-1.0, contains=1.0, annotates=1.0 |
| `Origin` | enum | `explicit` (from event metadata, user declaration) or `inferred` (AI-discovered) |
| `CreatedAt` | DateTimeOffset | Edge creation timestamp |

**Edge type classification:**
- **Structural edges** (organizational): `contains`, `annotates` — express ownership and correction relationships
- **Semantic edges** (meaning/causal): `caused_by`, `correlated_with`, `references` — express content relationships queryable via graph traversal

### Interface Philosophy

**Capability alignment, not feature parity.** The four interfaces serve different consumers:
- **CLI** — reference implementation. MVP CLI essentials are `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, benchmark support, and the README quickstart path used to validate NFR31. Phase 1.5 expands CLI polish with `explore`, `status`, `handlers`, `quickstart`, batch directory ingestion, and richer diagnostics.
- **MCP** — LLM agent surface, search/ingest/traverse/case-info only, token-budget-aware. Token-budget truncation includes `omitted_count`, explicit omitted fields, and deterministic expansion handles; score range metadata for omitted results (min/max) is Phase 1.5.
- **REST** (via ingress) — MVP: minimal ingress routing for CLI connectivity. Full REST API (pagination, facets, drill-down for application UIs) is Phase 2.
- **DAPR service invocation** — internal programmatic API
- **Web UI / RCL (Epic 17)** — future web composition surface. `Hexalith.Memories.Web` is a FrontComposer-aligned Razor component library over `Contracts.V1` Evidence Packet semantics. It uses FrontComposer shell/composition primitives and Microsoft Fluent UI Blazor V5 only; it must not become a standalone design system, raw HTML control library, or CSS theme fork. Custom markup/CSS is allowed only for explicitly justified semantic/container gaps and is guarded by conformance tests.

**Decision rule:** A capability goes to MCP if an LLM agent needs it to complete a user-facing task (search, ingest, traverse, case info). A capability stays CLI-only if it is operational, diagnostic, or administrative (tenant management, tenant verification, status, handlers, explore, quickstart). MVP CLI essentials are thesis-validation scope; full CLI polish is Phase 1.5. DAPR service invocation mirrors MCP scope for internal programmatic access.

### Evidence Packet Contract

`Contracts.V1` owns the shared Evidence Packet grammar used by CLI JSON output, MCP tool responses, and future web UI composition.

Minimum fields:
- `scope`: tenant ID, case ID, scope status, isolation status
- `result`: answer summary or ranked result summary
- `sources`: source references, origin identifiers, source type, freshness
- `evidence`: evidence strength, confidence caveat, retrieval axes used, per-axis score summary
- `graph`: graph relationship summary and gap markers when applicable
- `state`: complete, partial, weak, empty, stale, degraded, unauthorized, pending expansion
- `omittedDetails`: omitted count, omitted field names, deterministic expansion handles
- `recoveryActions`: next safe action plus optional secondary actions

`SearchResult` and `ScoredResult` remain lower-level retrieval contracts. Evidence Packet is the cross-surface response envelope that composes retrieval output, scope, state, omitted details, and recovery guidance.

**Graph traversal response shape:** `traverse_relations` returns full node context (memory unit summary + edge metadata), not just IDs. This enables single-call causal chain composition without a second search round-trip. The response is token-budget-aware when called via MCP.

### Architectural Dependencies & Failure Propagation

| Component | Depends On | Failure Impact | Mitigation |
|---|---|---|---|
| Fusion Algorithm | All 3 backends producing results + normalization correctness | Incorrect result ordering for all interfaces | Graph axis architecturally optional; score distribution monitoring per tenant |
| DAPR Sidecar | Network, DAPR runtime | Total service loss (workflows, actors, pub/sub, secrets all fail) | Kubernetes liveness probe; workflow + actor state survives in Redis; sidecar auto-restart |
| DAPR Workflow Engine | DAPR sidecar + actor state store | All orchestrations halt (ingestion, provisioning, deletion) | Workflow state is durable — automatically resumes on sidecar recovery. Pending workflows replay from persisted history. |
| Tenant Provisioning Workflow | RediSearch + Vector + FalkorDB all succeeding | Partially provisioned tenant if one backend fails mid-creation | Workflow saga pattern: compensation activities delete successfully created indexes on failure |
| Embedding Pipeline | External provider API availability | Ingestion halts for all tenants on affected provider | `WorkflowRetryPolicy` with exponential backoff per activity; `EmbeddingRateLimiterActor` per tenant; durable timers survive restarts |
| DAPR Actor State | Redis memory for actor + workflow state | Memory exhaustion affects all tenants | Actors are lightweight singletons (rate limiter, corpus stats); workflow history is incremental. Monitor Redis memory via Aspire dashboard. |
| Tenant Deletion Workflow | FalkorDB graph deletion performance | Large tenant deletion blocks other tenants' graph queries | Workflow orchestrates batched deletion activities (N nodes per activity invocation, yield between activities) |
| Redis Vector (HNSW) | Index integrity under churn | **Silent failure**: relevance degrades without errors | MVP: benchmark suite. Growth: periodic recall benchmarks |
| Fusion Scoring | Corpus statistics per tenant | **Silent failure**: score distribution skew produces subtly wrong rankings | MVP: benchmark suite. Growth: per-tenant score distribution monitoring |

### Silent Failure Modes

These failures produce no errors — the system returns results, but quality degrades. In MVP, the benchmark suite serves as the detection mechanism. Proactive monitoring is a Growth-phase investment.

| Failure Mode | Component | Why It's Silent | Detection Strategy |
|---|---|---|---|
| HNSW index degradation | Redis Vector | Results returned but relevance degrades — no errors, just worse answers | MVP: benchmark suite. Growth: periodic recall benchmarks against known query-result pairs |
| Fusion score distribution skew | Fusion Algorithm | System works but rankings subtly wrong when one axis dominates | MVP: benchmark suite. Growth: per-tenant score distribution monitoring (variance, mean per axis) |
| Duplicate event ingestion | Pipeline Actor | DAPR at-least-once delivery creates duplicate memory units — no errors, just duplicates | MVP: idempotency check on event ID + aggregate ID before ingestion |

### Phase Compatibility Requirement

The MVP architecture (Phase 1: CLI-only, no MCP, no EventStore integration) must accommodate Phase 1.5 additions as **additive, not transformative**. MCP Server and EventStore Integration must be pluggable into the existing architecture without rearchitecting the memory engine, search pipeline, or tenant model. The composition root (Aspire AppHost) is the extension point — new services register alongside existing ones.

### Graph Axis Architecture Decision

Graph axis has **dual roles**:
1. **Standalone traversal** (always available) — `traverse_relations` endpoint, causal chain queries, explicit graph walking. This is the highest-value use case and does not depend on fusion.
2. **Optional fusion scorer** (validated by benchmarks, disableable) — contributes graph proximity scores to hybrid search. Enabled by default for thesis validation. If benchmarks show no value, disabled in configuration — graph remains traversal-only. The kill switch is a config change, not a rearchitecture.
3. **Graph-scoped search** — server-side two-stage query: traverse first (find related node IDs), then search within that set. MCP tool supports optional `graph_scope` parameter. Provides integrated experience without requiring graph scores in fusion.

### FalkorDB Decision (Resolved)

**Decision: FalkorDB for MVP.** Rationale:
- MVP benchmarks include graph-scoped search, which benefits from native Cypher
- The `IGraphQueryBuilder` already provides the abstraction boundary for injection prevention — this same boundary serves as the extraction point for future backend swap
- Redis-native graph modeling would require building a traversal engine that FalkorDB provides out of the box
- The AGPL licensing risk is mitigated by the DAPR architectural boundary (network communication, not library embedding) and documented in LICENSE-DEPENDENCIES.md

**Documented escape hatch:** If FalkorDB licensing becomes a blocker, the `IGraphQueryBuilder` interface and the graph edge data model enable migration to Neo4j (GPL + commercial), Apache AGE (Apache 2.0), or Redis-native implementation. Extraction cost: 2-4 weeks with the existing boundary.

### Security Architecture

**MVP-critical components:**
- **`IGraphQueryBuilder`** — Structural Cypher injection prevention. Only accepts parameterized queries. No raw Cypher string construction in any code path. Makes injection structurally difficult, not just policy-prohibited.
- **DAPR Secrets scoping** — Configure DAPR secret scopes so only Memories Server app-id can access embedding keys. MCP Server does not have direct secret access. Documented in operator guide.

**Implemented remediation components:**
- **Server JWT bearer authentication** — Story 20.1 added the Server fallback `RequireAuthenticatedUser` policy for `/api/**`; only health probes and Dapr infrastructure routes are explicitly anonymous.
- **`TenantAuthorizationMiddleware` and endpoint filters** — Story 20.2 maps authenticated identity to authorized tenant sets. Tenant IDs from route, query, or body are never trusted until validated against principal claims. Story 20.3 applies the same stored-tenant check to ingestion workflow and batch status endpoints.
- **Inbound request limiting** — Story 20.5 added ASP.NET Core inbound request quotas partitioned by authenticated tenant context, separate from the embedding-provider throttling actor.

**Trust Boundary:**
The Memories Server is a **trusted component** with access to all tenant embedding API keys via DAPR Secrets. MVP: acceptable to cache per actor lifetime. Growth: periodic re-read from secrets for rotation support.

**Growth-phase security:**
- Memory unit optional `classification` field — schema-present in MVP, not enforced. Enables Phase 4 LLM context redaction without schema migration.
- Access telemetry lifecycle — current behavior is JSON-console emission plus optional OTLP export, with no repository-owned bounded lifecycle. [ADR 27.1-001](../../docs/dev/adr-27.1-001-access-telemetry-lifecycle.md) accepts a container-service neutral, Dapr-only access-telemetry lifecycle service: typed-state sanitization and non-blocking buffering; Dapr service invocation; a fixed-ID Dapr actor with durable state/reminders; Dapr state, configuration, and secrets components behind a fail-closed behavioral capability gate; continuously signed independent-UTC attestations with a one-second bound; millisecond logical expiry plus actor-driven purge; dynamic writer/key-rotation barriers; separate write/service/clock/inspection/adapter authorities; and component-specific physical-reclamation evidence collected outside the application API. Memories has no Redis, Kubernetes, backend-SDK, or orchestrator-API dependency for this lifecycle. The ratified all-nine-operation envelope is 250 events/s cluster-wide, up to 151,200,000 records and 144.20 GiB of canonical payload at the 7-day maximum; the selected adapter must reserve measured physical amplification, durability, index, and reclamation workspace before rollout. The approved `PG-ONPREM-1` planning record defines a dedicated PostgreSQL 18.4 Dapr v2 profile on the current single-node on-premises cluster: its in-profile zero-loss fault is PostgreSQL pod/process replacement with the node and retained local volume healthy; node, volume, control-plane, and site loss remain outside profile with no HA claim and require approved backup/restore RPO/RTO. Story 27.2 owns portable runtime implementation. Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification. Exact running-target C1 qualification is held without a registered story owner until compliant successor files and real per-gate producers exist. Story 27.4 owns deployment-shaped lifecycle verification, the operations runbook, and A41 close-out, and it remains blocked until all twenty-five C1 gates pass under those later-compliant registrations. **Ownership corrected 2026-08-01 by approved Sprint Change Proposal 2026-08-01.** `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains open until implementation and Production-shaped evidence pass or an explicit accepted-debt disposition satisfies the recorded closure gate. This is bounded infrastructure telemetry, not tamper-evident, append-only, legally compliant, or certified audit retention.
- Explain output as decorator on search results (not separate query path) — ensures Phase 4 ACL filtering applies equally to results and explain metadata.

### Testability Architecture

| Requirement | Design Implication |
|---|---|
| Fusion algorithm testable without backends | Pure function: `Fuse(List<ScoredResult>[], FusionWeights) → RankedResults` using weighted reciprocal-rank fusion. No backend calls. |
| Score semantics independently testable | Single-axis explain tests cover BM25/cosine/graph semantics; hybrid tests cover rank contributions, tie handling, and weight application. |
| Corpus statistics injectable | `ICorpusStatisticsProvider` remains available for single-axis explanation/legacy statistics paths; hybrid RRF does not require corpus statistics. |
| Workflow activities testable independently | Each activity is a standalone class with DI. Test activities directly without workflow engine. Mock external dependencies (Kreuzberg, embedding API, Redis, FalkorDB). |
| Workflow orchestration testable | Use DAPR Workflow test framework — verify activity sequencing, compensation paths, and retry behavior. |
| Actor logic testable without DAPR | Extract business logic into plain service classes; actor is a thin host that delegates. Test the service, not the actor infrastructure. |
| Consistency compensation testable | `ITenantInfrastructureResolver` doubles as fault injection point — test returns failing backend connection. Workflow compensation paths verified via activity mocks. |
| Idempotency testable | Ingest same event twice, assert single memory unit. |
| Tenant isolation testable on single instance | Logical separation (different index names, different FalkorDB databases) on same infrastructure. |

### Deployment Topology Baseline (MVP)

| Container | Purpose | Baseline Memory | Ports |
|---|---|---|---|
| Memories Server + DAPR sidecar | Core service + workflow engine + actor runtime | ~256MB + sidecar overhead | 5000 (app), 3500 (DAPR HTTP), 50001 (DAPR gRPC) |
| Redis Stack | RediSearch + Vector Search + DAPR state (workflows + actors) | ~512MB (empty, grows with data) | 6379 |
| FalkorDB | Graph database | ~256MB (empty, grows with data) | 6380 |
| Aspire Dashboard | Local dev observability | ~128MB | 18888 (dashboard), 18889 (OTLP) |

**DAPR building blocks required (MVP):**
- **Workflow:** DAPR Workflow (Durable Task Framework) — uses the actor state store. Orchestrates ingestion pipeline, tenant provisioning/deletion, consistency verification, and AI inference loops.
- **Actors:** Virtual actors for per-tenant singletons — `EmbeddingRateLimiterActor`, `CorpusStatisticsActor`. Configured via `AddActors()` + `MapActorsHandlers()`.
- **Conversation API:** DAPR AI (`Dapr.AI`) — provider-agnostic LLM abstraction for AI-powered enrichment (metadata extraction, content classification, causal relationship inference). Component YAML per provider. PII scrubbing, response caching, tool calling built-in. Alpha status — suppress `DAPR_CONVERSATION` warning.
- **State store:** Redis with `actorStateStore: "true"` (shared by workflows + actors)
- **Pub/sub:** Redis Streams (event ingestion — Phase 1.5, but component configured from start)
- **Secrets:** DAPR Secrets API in Aspire and deployed environments, backed by OpenBao through `secretstores.hashicorp.vault`. Separate `secretstore` and `access-telemetry-secrets` components isolate runtime and access-telemetry prefixes. Product code depends only on DAPR and never on OpenBao directly.
- **Service invocation:** Internal programmatic API — C# ↔ Python (AI Agent), Phase 1.5: MCP → Server

**Polyglot services (via DAPR service invocation):**

| Service | Language | DAPR App ID | Purpose | Called By |
|---|---|---|---|---|
| Memories Server | C# (.NET 10) | `memories` | Core domain: ingestion, search, tenants, fusion | CLI, MCP, REST / Dapr callers |
| AI Agent Service | Python | `ai-agent` | Dapr Agents: AI enrichment, metadata extraction, causal inference | `CallAiAgentActivity` in `AiEnrichmentWorkflow` via DAPR service invocation |
| MCP Server (Phase 1.5) | C# (.NET 10) | `memories-mcp` | LLM tool surface | External LLM agents |

**Single command:** `dotnet run --project Hexalith.Memories.AppHost` boots all containers (including Python AI Agent), sidecars, and dashboard.

### Resolved Design Questions

**1. Cross-Case Search Ranking:**
Default: pure relevance ranking (tenant-wide discovery mode). Optional `case_affinity` parameter boosts results from specified case by configurable factor. Response always includes case-level grouping metadata (case ID, result count per case) regardless of ranking mode.

**2. Operational Sizing Model:**
Architectural deliverable for operator guide. Sizing formula: `(dimensions × 4 bytes + 2.5KB metadata + 1.2KB graph) × 1.4 Redis overhead`. Reference configurations published with the operator guide; specific byte counts validated against actual implementation, not committed as architectural constants.

**3. Tenant-to-Instance Mapping:**
`ITenantInfrastructureResolver` interface — a **planned extension point**, not a free abstraction. Single implementation in MVP (all tenants → same instance). Returns per-tenant connection details for all three backends. Multi-instance is configuration, not code change. Migration tooling deferred until needed. Document clearly so contributors understand why it exists (future multi-instance scaling).

**4. Story 24.3 Physical Tenant Isolation Decision:**
Redis physical isolation target is per-tenant ACL users combined with tenant-scoped backend resolution. RediSearch syntactic indexes, raw Redis Vector indexes, natural-language Redis Vector indexes, and FalkorDB graph databases remain tenant-scoped lifecycle resources created and deleted by tenant workflows. Key prefixes, Redis hash tags, and logical Redis databases are placement and routing tools only; they are not the primary security boundary. Story 24.3 ratifies this direction and updates verifier evidence to structural/cursor checks, while full ACL user provisioning, tenant-scoped connection migration, and data migration remain follow-up enforcement work.

### PRD Deviations

Where the architecture overrides or clarifies PRD language, documented here for implementer clarity.

| PRD Statement | Architecture Position | Rationale |
|---|---|---|
| "Atomic write across all three backends" (pipeline stage) | EventStore aggregate source of truth + rebuildable projections + workflow compensation for projection delivery | No distributed transaction across Redis + FalkorDB. FR6 intent preserved: unit fully searchable after ingestion completes, with EventStore replay as the durable rebuild path. |
| "All major [embedding] providers supported from MVP" | Google runtime embedding provider only in MVP. OpenAI/Mistral are post-MVP provider expansion candidates; Ollama is covered by Epic 13 provider migration work. | Solo developer scope. The provider configuration shape and embedding provider pattern preserve extensibility without making every provider an MVP blocker. |
| REST API in Server (deployment topology) vs REST API Phase 2 (scope) | MVP REST is minimal ingress routing for CLI. Full REST API (pagination, facets) is Phase 2. | PRD contradicts itself; architecture clarifies. |

### Open Decision: Index Rebuild Strategy

When embedding model changes (e.g., `text-embedding-004` → `text-embedding-005`), all tenant vectors must be regenerated. Without concurrent index versions, this is a multi-day degradation event.

- **MVP approach:** Accept degradation during model migration. Document the limitation. Design index naming scheme to support concurrent versions later (`{tenant}:{model-version}:syntactic`).
- **Growth approach:** Build concurrent index support — old index serves queries, new index rebuilds in background, atomic swap when complete.

### Gate-Blocking vs Deferrable Summary

| Decision | Blocks Gate | Phase |
|---|---|---|
| Fusion algorithm (pure function, normalization) | Gate 1 (three-axis validation) | MVP |
| EventStore source of truth + projection compensation | Gate 1 | MVP |
| Per-tenant Redis ACL users plus tenant-scoped backend resolution | Gate 2 (zero cross-tenant leaks) | MVP target; full enforcement follows Story 24.3 |
| Physical FalkorDB isolation at database level | Gate 2 (zero cross-tenant leaks) | MVP |
| `IGraphQueryBuilder` (injection prevention) | Gate 2 | MVP |
| Provisioning rollback | Gate 2 | MVP |
| Docker Compose single-command boot | Gate 3 (<30 min onboarding) | MVP |
| Actionable CLI error messages | Gate 3 | MVP |
| `TenantAuthorizationMiddleware` | Not gate-blocking | Phase 1.5 |
| Full Hexalith.Commons error envelopes | Not gate-blocking | Phase 1.5 |
| Cross-case ranking | Not gate-blocking | Phase 1.5 |
| Operational sizing guide | Not gate-blocking | Phase 1.5 |
| Silent failure monitoring | Not gate-blocking | Phase 2 |
| Concurrent index versions | Not gate-blocking | Phase 2 |
| Thundering herd coordination | Not gate-blocking | Phase 3 |
| Memory unit `classification` enforcement | Not gate-blocking | Phase 4 |

### Architectural Components Summary

**MVP-critical — safety interfaces (must be interfaces from day one):**

| Component | Why Interface | Purpose |
|---|---|---|
| `IGraphQueryBuilder` | **Safety** — structurally prevents Cypher injection | Parameterized-only query construction. No raw Cypher anywhere. |

**MVP-critical — DAPR Workflows (multi-step orchestrations):**

| Workflow | Purpose | Activities |
|---|---|---|
| `IngestionWorkflow` | Orchestrates full ingestion pipeline with retry/compensation | `ValidateContentActivity` → `ExtractContentActivity` → `GenerateEmbeddingActivity` → `IndexSyntacticActivity` + `IndexSemanticActivity` + `IndexGraphActivity` (fan-out) → `VerifyConsistencyActivity` |
| `TenantProvisioningWorkflow` | Sole owner of tenant index/database creation across all backends with saga rollback | `ProvisionRediSearchActivity` → `ProvisionRedisVectorActivity` → `ProvisionFalkorDbActivity` → `VerifyTenantActivity`. On failure: compensation activities delete created indexes. Ingestion, search, graph, CLI, and MCP paths validate active tenant infrastructure and never create tenant indexes on demand. |
| `TenantDeletionWorkflow` | Batched async deletion across backends | `DeleteRediSearchActivity` → `DeleteRedisVectorActivity` → `DeleteFalkorDbActivity` (batched, N nodes per activity invocation) |
| `ConsistencyVerificationWorkflow` | On-demand or scheduled consistency check | Queries all 3 backends per memory unit, reports discrepancies |
| `AiEnrichmentWorkflow` | AI-powered ingestion enrichment (Phase 1.5, optional in MVP) | `CallAiAgentActivity` — invokes the Python Dapr Agents service via DAPR service invocation. The Python service runs a `DurableAgent` with tools for metadata extraction, content classification, and causal relationship inference. Runs as child workflow from `IngestionWorkflow` when AI enrichment is enabled. |

**MVP-critical — DAPR Actors (per-tenant stateful singletons):**

| Actor | Purpose | State |
|---|---|---|
| `EmbeddingRateLimiterActor` | Per-tenant embedding API rate limiting. Actor ID = tenant ID. Checks/decrements rate budget before `GenerateEmbeddingActivity` proceeds. | Rate window start, remaining budget, ceiling config |
| `CorpusStatisticsActor` | Per-tenant corpus stats caching for single-axis BM25 explanation and legacy statistics consumers. Hybrid RRF does not depend on this actor. Methods: `GetDocumentCount()`, `GetAverageDocumentLength()`, `GetTermFrequency(term)`. Refreshed via timer. | Cached stats, last refresh timestamp |

**MVP-critical — concrete classes (extract to interface when second implementation arrives):**

| Component | Purpose | Extract When |
|---|---|---|
| `TenantInfrastructureResolver` | Maps tenant ID → backend connection details (single impl: all tenants → same instance) | Multi-instance scaling needed |
| `CorpusStatisticsProvider` | Facade that delegates to `CorpusStatisticsActor` via `IActorProxyFactory` | Second backend (Qdrant) needs different stats source |
| Fusion function | `Fuse(scoredResults[], weights) → rankedResults` — pure function, no backend coupling | Remains a function, not an interface |
| Graph-scoped search mode | Server-side two-stage query: traverse → search within result set | N/A — query mode, not abstraction |

**Growth-phase:**

| Component | Purpose | Phase |
|---|---|---|
| `TenantAuthorizationMiddleware` | Auth context → tenant validation | Implemented in Epic 20; keep as a required ingress guard |
| Per-tenant score distribution monitor | Detect fusion score skew | Phase 2 |
| Concurrent index versions | Model migration without degradation | Phase 2 |
| `IndexRebuildWorkflow` | Long-running workflow for model migration with concurrent index versions | Phase 2 |
| Shared embedding rate limiter | Cross-tenant thundering herd prevention (actor coordination) | Phase 3 |
| Memory unit `classification` enforcement | Enable LLM context redaction | Phase 4 |
| Dedicated audit telemetry store | Tamper-resistant access records | Phase 3 |

### Benchmark Query Gap

Gate 1 (three-axis validation) is untestable without benchmark queries with defined ground truth. This is the highest-priority deliverable for the architecture decisions phase:

**Required:** 3-5 example benchmark queries that require all three axes, with expected ground truth results. Example pattern:
- *"Find documents about payment processing referenced in discussions following the March deployment failure"* — requires syntactic ('payment processing'), semantic (meaning-similar content), and graph (temporal/causal edges from 'March deployment failure')

**Benchmark validation:** After the first 1000 memory units are ingested, validate: (1) the sizing formula against actual Redis memory (flag if >20% off), (2) benchmark queries against ground truth, (3) HNSW recall against known query-result pairs.

### Gate Implementation Strategy

The three MVP gates are **architecturally independent** — they can be designed and built with awareness of this parallelism:

| Gate | Risk Level | Nature | Strategy |
|---|---|---|---|
| Gate 1: Three-axis validation | **Highest** — R&D, unproven thesis | Research | Start first. Fusion algorithm is the critical path. |
| Gate 2: Zero cross-tenant leaks | Medium — known engineering | Engineering | Design alongside Gate 1; test suite can run as soon as indexes exist. |
| Gate 3: <30 min onboarding | Low — developer experience craft | Craft | Build last. CLI polish and Docker Compose happen after the engine works. |

For a solo developer: build in **Gate 1 → Gate 2 → Gate 3** order, investing the most time in the highest-risk gate. If Gate 1 fails, Gates 2 and 3 are moot.

### Decision Registry

Decisions made during context analysis, extracted for quick reference:

| # | Decision | Rationale | Section |
|---|---|---|---|
| D1 | FalkorDB for MVP (with escape hatch) | Native Cypher for graph-scoped search; IGraphQueryBuilder provides extraction boundary | FalkorDB Decision |
| D2 | Graph axis: dual-role (traversal + optional fusion scorer) | Preserves graph value regardless of benchmark outcome; kill switch is config change | Graph Axis Architecture |
| D3 | EventStore aggregate source of truth + rebuildable projections + DAPR Workflow projection compensation | No distributed transaction across Redis + FalkorDB; Hexalith domain state belongs in EventStore while workflow activities with retry/compensation preserve PRD intent for projection and infrastructure side effects | Cross-Cutting Concern #9 |
| D4 | Google embedding only in MVP | Solo developer scope; IEmbeddingProvider abstraction makes additions trivial | PRD Deviations |
| D5 | MVP REST is minimal (CLI routing only) | Full REST API is Phase 2; PRD self-contradicted | PRD Deviations |
| D6 | Error format: code + message + suggestion (not full Hexalith.Commons envelope) | Simpler for MVP; full envelope in Phase 1.5 for MCP | Cross-Cutting Concern #3 |
| D7 | Capability alignment, not feature parity across interfaces | CLI is reference; others expose consumer-specific subsets | Interface Philosophy |
| D8 | TenantAuthorizationMiddleware no longer deferred | Epic 20 implemented Server auth and tenant-claim authorization; preserve the guard on every tenant-scoped ingress path | Security Architecture |
| D9 | Safety interfaces (IGraphQueryBuilder) are interfaces; extensibility points are concrete classes | Avoids abstraction tax; extract when second implementation arrives | Architectural Components |
| D10 | Index rebuild: accept degradation in MVP, design naming for concurrent versions | Solo developer; no production tenants during thesis validation | Open Decision |
| D29 | Redis physical isolation target is per-tenant ACL users plus tenant-scoped backend resolution | Prefix-only naming is insufficient as a security boundary; verifier evidence must prove target tenant storage and metadata without pairwise deep scans | Resolved Design Questions |

## Starter Template Evaluation

### Primary Technology Domain

API Backend / AI Infrastructure on .NET 10 with DAPR + Aspire orchestration. No single starter template covers this combination — the project scaffolding is composed from Aspire templates plus individual project creation.

### Starter Options Considered

| Option | What It Provides | Fit |
|---|---|---|
| `dotnet new aspire-starter` | AppHost + ServiceDefaults + Blazor frontend + API service | Overly opinionated — includes Blazor frontend not needed. Would require removing scaffolded UI projects. |
| `dotnet new aspire` (empty) | AppHost + ServiceDefaults only | **Best fit** — minimal scaffolding, add only what's needed. Clean composition root. |
| DAPR quickstart templates | Sample DAPR apps with pub/sub, state, actors | Useful for reference but not project scaffolding. Too demo-oriented. |
| Custom multi-project `dotnet new` template | Full 10-project solution structure | Overkill for a one-time scaffold. Build sequentially. |

### Selected Approach: Aspire Empty + Incremental Projects

**Rationale:** The project has a specific 10-package structure defined in the PRD. No starter captures this. Use `dotnet new aspire` for the orchestration foundation, then add projects incrementally as features are built. This matches the gate-blocking strategy: build what Gate 1 needs first, add remaining projects as gates require them.

**Initialization Sequence:**

```bash
# 1. Create solution with Aspire orchestration
dotnet new aspire -n Hexalith.Memories --output .

# 2. Add DAPR hosting integration to AppHost
cd Hexalith.Memories.AppHost
dotnet add package CommunityToolkit.Aspire.Hosting.Dapr

# 3. Create core projects (Gate 1 critical path)
dotnet new classlib -n Hexalith.Memories.Contracts
dotnet new webapi -n Hexalith.Memories.Server
dotnet new classlib -n Hexalith.Memories.Redis

# 4. Add to solution
dotnet sln add Hexalith.Memories.Contracts
dotnet sln add Hexalith.Memories.Server
dotnet sln add Hexalith.Memories.Redis
```

**MVP CLI project (added before thesis validation for benchmark and onboarding gates):**
```bash
dotnet new console -n Hexalith.Memories.Cli
```

The MVP CLI owns a small direct HTTP/ingress adapter for the thesis-validation command set. That adapter is intentionally local to the CLI so Gate 3 does not depend on the Phase 1.5 `Hexalith.Memories.Client.Rest` package.

**Phase 1.5 projects (added after thesis validation):**
```bash
dotnet new classlib -n Hexalith.Memories.Client
dotnet new classlib -n Hexalith.Memories.Client.Rest
dotnet new webapi -n Hexalith.Memories.Mcp
dotnet new classlib -n Hexalith.Memories.EventStore
```

### Current Verified Versions (March 2026)

| Package | Version | Purpose |
|---|---|---|
| .NET SDK | 10.0 (LTS) | Runtime — C# 14 |
| Aspire | 13.1.3 | Orchestration, dashboard, health checks |
| `CommunityToolkit.Aspire.Hosting.Dapr` | 9.7.0 | DAPR sidecar integration for Aspire |
| `Dapr.Client` | 1.17.6 | DAPR client SDK (service invocation, state, pub/sub) |
| `Dapr.Workflow` | 1.17.6 | DAPR Workflow SDK (Durable Task Framework orchestrations) |
| `Dapr.Actors` | 1.17.6 | DAPR virtual actor framework |
| `Dapr.Actors.AspNetCore` | 1.17.6 | DAPR actor hosting integration (`AddActors()`, `MapActorsHandlers()`) |
| `Dapr.AspNetCore` | 1.17.6 | DAPR ASP.NET Core integration (`AddDaprClient()`) |
| `Dapr.AI` | 1.17.6 | DAPR Conversation API — LLM abstraction (`DaprConversationClient`) [alpha, `[Experimental]`] |
| `Dapr.AI.Microsoft.Extensions` | 1.17.6 | `IChatClient` bridge to Microsoft.Extensions.AI (`DaprChatClient`) |
| `NRedisStack` | 1.3.0 | Redis Stack client (RediSearch + Vector Search) |
| `StackExchange.Redis` | 2.12.4 | Core Redis connectivity |
| `NFalkorDB` | 1.0.0 | FalkorDB graph client for .NET |

### Architectural Decisions Provided by Scaffolding

**From Aspire (AppHost + ServiceDefaults):**
- OpenTelemetry configuration (traces, metrics, logs)
- Health check wiring (readiness/liveness)
- Service discovery and endpoint registration
- Dashboard for local dev observability
- Container orchestration model

**From DAPR Community Toolkit:**
- `.WithDaprSidecar()` sidecar attachment pattern with explicit `AppPort` (required for workflows + actors)
- DAPR component configuration in Aspire resources
- Sidecar lifecycle management

**From DAPR Workflow + Actors SDKs:**
- `AddDaprWorkflow()` — workflow and activity registration
- `AddActors()` + `MapActorsHandlers()` — actor registration and endpoint mapping
- `DaprWorkflowClient` / `IDaprWorkflowClient` — workflow management API (schedule, status, cancel)
- `IActorProxyFactory` — actor proxy creation for service-to-actor calls

**Not provided by scaffolding (must be built):**
- Multi-project solution structure (7 published packages plus 3 non-packable service/orchestration projects)
- Workflow definitions (ingestion, tenant provisioning/deletion, consistency verification)
- Activity implementations (extraction, embedding, indexing, verification)
- Actor implementations (rate limiter, corpus statistics)
- Redis/FalkorDB resource registration in AppHost
- Tenant isolation logic
- Fusion algorithm
- CLI tool

### Build Order Aligned to Gates

| Order | Project | Needed For |
|---|---|---|
| 1 | `Hexalith.Memories.Contracts` | All other projects depend on it |
| 2 | `Hexalith.Memories.Redis` | Three-axis backends (Gate 1) |
| 3 | `Hexalith.Memories.Server` | Ingestion pipeline, search engine (Gate 1) |
| 4 | `Hexalith.Memories.AppHost` | Orchestration, Docker Compose equiv (Gate 3) |
| 5 | `Hexalith.Memories.ServiceDefaults` | Health checks, telemetry (Gate 2 verification) |
| 6 | `Hexalith.Memories.Cli` | MVP CLI essentials: `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, benchmark support |
| — | *Thesis validation checkpoint* | — |
| 7 | `Hexalith.Memories.Cli` expansion | Phase 1.5 CLI polish: `explore`, `status`, `handlers`, `quickstart`, batch directory ingestion, richer diagnostics |
| 8 | `Hexalith.Memories.Client` | Internal consumers (Phase 1.5) |
| 9 | `Hexalith.Memories.Client.Rest` | External consumers (Phase 1.5) |
| 10 | `Hexalith.Memories.Mcp` | LLM agent interface (Phase 1.5) |
| 11 | `Hexalith.Memories.EventStore` | Zero-code integration (Phase 1.5) |

**Note:** Project initialization is the first implementation story. Git submodules under `references/` (`references/Hexalith.Commons`, `references/Hexalith.EventStore`) must be configured in the same story.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
Captured in Decision Registry D1-D31. D1-D10 from context analysis, D11-D17 from architectural decision making, D18-D22 from EventStore alignment, D23-D25 from DAPR Workflow/Actor adoption, D26-D28 from DAPR AI/Conversation API and polyglot architecture, D29-D30 from operational readiness, and D31 from OpenBao-first secret provisioning.

**Deferred Decisions (Post-MVP):**
- Full REST API design (Phase 2)
- Embedding model migration tooling (Phase 2)
- Per-unit ACL model (Phase 4)
- Multi-region deployment (Phase 4)

### Data Architecture

| # | Decision | Choice | Rationale | Affects |
|---|---|---|---|---|
| D11 | Benchmark test data | Synthetic dataset with known relationships and controlled vocabulary | Deterministic, reproducible, perfect ground truth for automated NDCG@10 scoring. Real-world validation deferred to Phase 1.5. | Benchmark suite, Gate 1 validation |
| D12 | Input validation boundary | Domain validation service (`IngestionValidator`) | All entry points (REST, DAPR, MCP) share same validation. Single source of truth. Plain C# class, no infrastructure dependency. | Server, all ingestion paths |
| D13 | Content extraction | Kreuzberg NuGet package (in-process, Rust core via P/Invoke) | 91+ format support, native .NET integration (zero dependencies), eliminates JVM container, built-in OCR/chunking/embeddings for future RAG phases. Trade-off: extraction runs in-process (no container isolation), acceptable for MVP payloads ≤1MB (NFR5). | `Directory.Packages.props` (+1 package), `ContentExtractionClient`, no AppHost container, no health check needed |
| D14 | Contract evolution | Versioned namespaces (`Contracts.V1`, `Contracts.V2`) | Clean separation, consumers upgrade deliberately. MVP ships V1 only — no overhead until first breaking change. | All packages depending on Contracts, DAPR payloads, CloudEvents |

### API & Communication

| # | Decision | Choice | Rationale | Affects |
|---|---|---|---|---|
| D15 | DAPR actor identity | Type-scoped: `{actorType}-{tenantId}` | Supports multiple actor types per tenant. Clear in logs and monitoring. | All actors, observability |

### Infrastructure & Deployment

| # | Decision | Choice | Rationale | Affects |
|---|---|---|---|---|
| D16 | Test framework | xUnit v3 + Shouldly + NSubstitute | Aspire `DistributedApplicationTestingBuilder` aligned. Readable assertions. | All test projects |
| D17 | CI/CD pipeline | Hexalith.Builds reusable CI core plus module-specific verification lanes and intentional guarded release | Pull requests and pushes to `main` use `domain-ci.yml@main` for compatible standard build/test work. Memories-specific tenant evidence, tooling, web E2E, integration, deployment, benchmark, and recovery lanes remain local and explicit. Publication is operator-dispatched from the exact current green `main` source, enters a protected environment, and invokes `domain-release.yml` pinned to an approved immutable Hexalith.Builds SHA. | Shared workflow callers, module-specific workflow lanes, commit conventions, branch protection, package/container publication, recovery, CONTRIBUTING.md |

### Updated Deployment Topology

| Container | Purpose | Baseline Memory | Ports |
|---|---|---|---|
| Memories Server + DAPR sidecar | C# core service + workflow engine + actor runtime | ~256MB + sidecar | 5000, 3500, 50001 |
| AI Agent Service + DAPR sidecar | Python Dapr Agents: AI enrichment, NLP, causal inference | ~256MB + sidecar | 5010, 3510, 50011 |
| Redis Stack | RediSearch + Vector Search + DAPR state (workflows + actors) | ~512MB | 6379 |
| FalkorDB | Graph database | ~256MB | 6380 |
| Aspire Dashboard | Local dev observability | ~128MB | 18888, 18889 |

### Complete Decision Registry

| # | Decision | Rationale | Phase |
|---|---|---|---|
| D1 | FalkorDB for MVP (with escape hatch) | Native Cypher; IGraphQueryBuilder extraction boundary | MVP |
| D2 | Graph axis: dual-role (traversal + optional scorer) | Kill switch is config change | MVP |
| D3 | EventStore aggregate source of truth + rebuildable projections + DAPR Workflow projection compensation | EventStore replay is the durable rebuild path; workflow activities with retry/rollback handle projection and infrastructure side effects where no distributed transaction exists | MVP |
| D4 | Google embedding only in MVP | Solo developer scope | MVP |
| D5 | MVP REST is minimal (CLI routing only) | Full REST API Phase 2 | MVP |
| D6 | Error format: code + message + suggestion | Full envelope Phase 1.5 | MVP |
| D7 | Capability alignment, not feature parity | CLI is reference | MVP |
| D8 | TenantAuthorizationMiddleware implemented | Server auth and tenant-claim authorization landed in Epic 20 | Operational readiness |
| D9 | Safety interfaces vs concrete classes | Avoid abstraction tax | MVP |
| D10 | Index rebuild: accept degradation | Design naming for concurrent versions | MVP |
| D11 | Synthetic benchmark dataset | Deterministic ground truth | MVP |
| D12 | Domain validation service | Consistent across entry points | MVP |
| D13 | Kreuzberg NuGet for content extraction | Native .NET, no JVM, RAG-ready, 91+ formats | MVP |
| D14 | Versioned contract namespaces | Clean consumer upgrade path | MVP |
| D15 | Type-scoped actor identity | Future-safe naming | MVP |
| D16 | xUnit + Shouldly + NSubstitute (aligned with EventStore) | Ecosystem consistency | MVP |
| D17 | Reusable GitHub Actions CI + guarded semantic release | Consistent evidence, intentional publication, recoverable multi-artifact release | Engineering/Operational Readiness |
| D23 | DAPR Workflow for multi-step orchestrations | Durable Task Framework: built-in retry, compensation, state persistence, restart survival | MVP |
| D24 | DAPR Actors for per-tenant stateful singletons | Virtual actor pattern: `EmbeddingRateLimiterActor`, `CorpusStatisticsActor`. Actor ID = tenant ID. | MVP |
| D25 | Workflow-Actor separation of concerns | Workflows orchestrate processes (sequencing, retry, compensation). Actors manage per-entity state (rate limits, cached stats). Activities do I/O. | MVP |
| D26 | DAPR Conversation API for LLM communication | Provider-agnostic LLM abstraction via `Dapr.AI`. Swap providers (OpenAI, Anthropic, Google, Mistral) by changing component YAML only. Alpha status accepted — suppress `DAPR_CONVERSATION` warning. | MVP (enrichment optional), Phase 1.5 (full AI features) |
| D27 | Dapr Agents as Python sidecar service | Dapr Agents SDK is Python-only (GA 1.0.0). Run as a polyglot sidecar service (`ai-agent`) called by C# workflows via DAPR service invocation. Python owns AI enrichment, NLP, causal inference. C# owns core domain. | MVP (optional enrichment), Phase 1.5 (full AI features) |
| D28 | Polyglot services via DAPR service invocation | When a Python/other-language library is the best fit, create a service in that language. DAPR service invocation makes the calling language invisible. Aspire AppHost orchestrates all services regardless of language. | MVP |
| D29 | Redis physical isolation target | Per-tenant ACL users plus tenant-scoped backend resolution; prefixes/hash tags/logical DBs are placement aids, not the security boundary | Operational readiness |
| D30 | No infrastructure dependency in product code | Product projects (`Server`, `Cli`, `Mcp`, `Web`, `Client.Rest`) reach infrastructure only via Dapr building blocks or Aspire-injected connections/config; direct infra clients and endpoint construction live only in boundary projects (`AppHost`, `Aspire`, `ServiceDefaults`, `Redis`, `EventStore`). Sanctioned exceptions are enumerated below. See ADR-IDA-001. | Operational readiness |
| D31 | OpenBao-first DAPR secret provider | Application runtime secrets are resolved through DAPR secret-store components backed by OpenBao. Local-file and Kubernetes secret stores are not application-secret providers. Aspire secret parameters or protected files may bootstrap and seed local OpenBao. Kubernetes Secrets are permitted only for required OpenBao tokens/CA material or direct pod inputs that DAPR cannot inject; every exception must be documented and tested. | Operational readiness |

#### D30 — No infrastructure dependency in product code (sanctioned exceptions)

**Invariant.** Product code must not hardcode infrastructure endpoints/hosts/ports or construct
infrastructure clients directly. Infrastructure is reached only through Dapr building blocks (workflows,
actors, state, pub/sub, secrets, service invocation, Conversation API) or Aspire (connection/endpoint
discovery, orchestration, component generation). Direct infrastructure clients and connection lifecycle
live only in the boundary projects.

**Sanctioned exceptions** (audited 2026-07-17, spec-infrastructure-dependency-abstraction):

1. **Search/vector/graph direct clients** — direct `NRedisStack`/`NFalkorDB` usage is allowed only inside
   the `Redis`/`EventStore` boundary projects and consumes **Aspire-injected** keyed connections
   (`"redis"`/`"falkordb"`); no hardcoded endpoints.
2. **Dapr-platform env contracts** — `DAPR_API_TOKEN`, `DAPR_API_TOKEN_MODE`, `APP_API_TOKEN`,
   `DAPR_HTTP_PORT`/`DAPR_HTTP_ENDPOINT`, and the Dapr subscription-discovery topic env var
   (`EnvironmentTopicAttribute`) are owned and injected by the Dapr runtime / AppHost / K8s and are read
   directly by design.
3. **CLI minimal direct-HTTP adapter** — the CLI reaches the Server over HTTP (not Dapr service
   invocation); its endpoint defaults are config-sourced (env-overridable), never fixed pins.

**Keyed connection construction** (F5) lives in `ServiceDefaults.AddKeyedRedisConnections`; product code
only consumes the keyed `IConnectionMultiplexer` services. Embedding provider default endpoints (F1/F2)
and the CLI endpoint/OTLP defaults (F3/F4) are config-sourced with overridable literal fallbacks.

#### D31 — OpenBao-first DAPR secret provider

**Invariant.** Product services retrieve application secrets exclusively through DAPR Secrets API. They
do not use an OpenBao SDK, construct an OpenBao endpoint, read Kubernetes Secrets, or resolve application
secrets directly from .NET User Secrets.

**Component boundaries.**

| DAPR component | OpenBao prefix | Consumers |
|---|---|---|
| `secretstore` | `secret/hexalith/memories/runtime` | Memories Server and components resolving embedding or LLM secrets |
| `access-telemetry-secrets` | `secret/hexalith/memories/access-telemetry` | Memories Server, access-telemetry lifecycle, and clock |

Each component uses a distinct read-only policy. Cross-prefix reads fail closed.

**Aspire topology.** The AppHost owns the OpenBao resource, health and initialization sequencing, DAPR
component generation, protected bootstrap inputs, and secret seeding. Consumers wait for initialization
and reference only their required DAPR components. A development-mode OpenBao profile must be explicit
and must not silently publish as a production topology.

**Bootstrap exception.** The DAPR component must authenticate before it can read OpenBao. Protected Aspire
parameters or temporary credential files are allowed locally. In Kubernetes, narrowly scoped Secrets may
hold only required OpenBao bootstrap tokens and CA certificates. Direct pod inputs may remain Kubernetes
Secrets only where DAPR cannot supply them; migrating those inputs requires a separately approved Agent
Injector or CSI design.

**Security evidence.** Verification must prove successful DAPR reads, cross-prefix denial, restart
recovery, absence of provider-specific product dependencies, and secret-safe logs and diagnostics.

#### ADR-IDA-001 — EventStore store substrate: Dapr state vs direct Redis

**Status:** Accepted (2026-07-17). **Context:** Three EventStore KV stores used direct Redis. The invariant
(D30) prefers the Dapr state building block wherever Redis-native atomicity is not load-bearing.

**Decision.** Split the three stores by whether Redis-native atomicity is load-bearing:

- **Migrated to the Dapr state store (`statestore`):** `DaprAggregateCaseMappingStore` (aggregate→case KV
  map + set-if-not-exists creation lock) and `DaprObservedEventTypeStore` (observed-event-type
  index + per-aggregate counters). Redis hash/sorted-set/Lua primitives are re-expressed via Dapr state
  **ETag optimistic concurrency** (bounded-retry compare-and-set) plus `ttlInSeconds` metadata; the
  observed-type time-window query that Redis served with `ZRANGEBYSCORE` is now performed **in-memory** on
  read. Idempotency and at-least-once/late/out-of-order safety are preserved.
- **Kept on direct Redis:** `RedisPreflightDedupStore` — its `SET NX` atomic reserve + TTL + fail-OPEN is a
  load-bearing check-and-set the Dapr state API cannot express portably.

**Consequences / trade-offs.** The migrated stores lose cross-key atomicity (two non-atomic state writes)
and the exact cardinality-cap atomicity (a small CAS race window may admit a few entries over the 1024 cap
— the cap is defence-in-depth, not exact); TTL-refresh-on-write adds CAS contention on the aggregates
index under high ingestion. These were accepted with full knowledge (architect decision, 2026-07-17) in
exchange for building-block alignment. Real ETag-CAS/TTL behavior requires Dapr-sidecar integration
verification (Tier-2); unit coverage uses an in-memory ETag-CAS state fake.

### Cross-Component Dependencies

```
Contracts.V1 ← Server ← AppHost
                  ↑         ↑
                Redis    DAPR sidecar (workflows + actors + state + pub/sub + secrets)
                  ↑
              FalkorDB

Server Workflows → Activities → {Kreuzberg (in-process), Embedding API, Redis, FalkorDB}
Server Workflows → CallAiAgentActivity → DAPR service invocation → Python AI Agent Service
Python AI Agent → DAPR Conversation API → LLM providers
Server Actors → Redis (DAPR actor state store)
Server Search → CorpusStatisticsActor (via IActorProxyFactory)
Minimal API endpoint → DaprWorkflowClient (schedule, status, cancel workflows)
Minimal API endpoint → IActorProxyFactory (rate limiter queries)

CI: commit → build → test → semantic-release → NuGet publish
```

## Implementation Patterns & Consistency Rules

### Source of Truth

All patterns align with **Hexalith.EventStore** conventions. When in doubt, check the [EventStore CLAUDE.md](https://github.com/Hexalith/Hexalith.EventStore/blob/main/CLAUDE.md) for the canonical reference.

### Code Style (from `.gitattributes` and `.editorconfig`)

The root `.gitattributes` is authoritative for Git normalization: text is stored
with LF in the index and materialized with CRLF in working trees by default.
Shell/Bash, Python, YAML, `Dockerfile`, `*.dockerfile`, and `.gitattributes`
remain LF; `.editorconfig` mirrors these editor-facing conventions.

| Rule | Convention | Example |
|---|---|---|
| Namespaces | File-scoped | `namespace Hexalith.Memories.Server.Ingestion;` |
| Braces | Allman style (new line) | `if (x)\n{` |
| Private fields | `_camelCase` prefix | `private readonly string _tenantId;` |
| Interfaces | `I` prefix | `IGraphQueryBuilder` |
| Async methods | `Async` suffix | `SearchAsync()`, `IngestAsync()` |
| Indentation | 4 spaces | — |
| Line endings | Git index LF; CRLF working tree by default; LF for Unix/tooling exceptions | Root `.gitattributes` and aligned `.editorconfig` |
| Encoding | UTF-8 | — |
| Nullable | Enabled globally | `<Nullable>enable</Nullable>` |
| Implicit usings | Enabled globally | `<ImplicitUsings>enable</ImplicitUsings>` |
| Warnings as errors | Enabled | `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` |

### Naming Patterns

**JSON serialization:** `camelCase` via `System.Text.Json` with `JsonNamingPolicy.CamelCase`

**Redis index fields:** `camelCase` — matches JSON output
```
FT.CREATE {tenant}:memories:idx ON HASH PREFIX 1 {tenant}:mem:
  SCHEMA tenantId TAG caseId TAG content TEXT contentHash TAG
```

**DAPR topics:** dot-separated lowercase
```
hexalith.memories.events.memory-unit-ingested
hexalith.memories.events.memory-unit-deleted
hexalith.memories.commands.ingest-content
```

**DAPR actor IDs:** `{actorType}-{tenantId}`
```
ingestion-pipeline-bu-compliance
```

**Namespace structure:** Feature-based within projects
```
Hexalith.Memories.Server.Ingestion
Hexalith.Memories.Server.Search
Hexalith.Memories.Server.Tenants
Hexalith.Memories.Server.Graph
Hexalith.Memories.Contracts.V1
```

### Structure Patterns

**Solution format:** `.slnx` only — never `.sln`

**Project layout:**
```
src/
  Hexalith.Memories.Contracts/
  Hexalith.Memories.Server/
  Hexalith.Memories.Redis/
  Hexalith.Memories.Client/
  Hexalith.Memories.Client.Rest/
  Hexalith.Memories.Cli/
  Hexalith.Memories.Mcp/
  Hexalith.Memories.EventStore/
  Hexalith.Memories.AppHost/
  Hexalith.Memories.ServiceDefaults/

tests/
  Hexalith.Memories.Contracts.Tests/    # Tier 1
  Hexalith.Memories.Server.Tests/       # Tier 2 (DAPR slim)
  Hexalith.Memories.Redis.Tests/        # Tier 2 (Redis + FalkorDB)
  Hexalith.Memories.IntegrationTests/   # Tier 3 (Aspire e2e)

samples/
  Hexalith.Memories.Sample/
```

**Package management:** Centralized via `Directory.Packages.props`

### Error Handling Pattern

Three layers, matching EventStore:

1. **Input validation:** FluentValidation via MediatR pipeline
2. **Domain logic:** Result pattern (`DomainResult`) — never throw for business rules
3. **Infrastructure:** Exceptions only for truly exceptional conditions (Redis down, Kreuzberg extraction failure)

**Error response format (MVP):**
```json
{"code": "TENANT_NOT_FOUND", "message": "Tenant 'bu-compliance' does not exist.", "suggestion": "Run 'memories tenant list' to see available tenants."}
```

### DAPR Workflow Patterns

**Workflow definition:**
```csharp
namespace Hexalith.Memories.Server.Workflows;

public class IngestionWorkflow : Workflow<IngestionInput, IngestionResult>
{
    public override async Task<IngestionResult> RunAsync(
        WorkflowContext context, IngestionInput input)
    {
        var logger = context.CreateReplaySafeLogger<IngestionWorkflow>();
        var retryOptions = new WorkflowTaskOptions(
            RetryPolicy: new WorkflowRetryPolicy(
                maxNumberOfAttempts: 5,
                firstRetryInterval: TimeSpan.FromSeconds(2),
                backoffCoefficient: 2.0,
                maxRetryInterval: TimeSpan.FromMinutes(5)));

        // Sequential: validate → extract → embed
        // Fan-out: index all 3 backends in parallel
        // Verify consistency
        // On failure: compensation activities
    }
}
```

**Activity definition:** Activities are standalone DI-enabled classes. Each activity does one thing (single responsibility). Activities call services (Kreuzberg in-process, embedding API, Redis, FalkorDB) — workflows never call services directly.
```csharp
namespace Hexalith.Memories.Server.Activities.Ingestion;

public class ExtractContentActivity : WorkflowActivity<ExtractionInput, ExtractionResult>
{
    private readonly ContentExtractionClient _client;

    public ExtractContentActivity(ContentExtractionClient client) => _client = client;

    public override async Task<ExtractionResult> RunAsync(
        WorkflowActivityContext context, ExtractionInput input)
    {
        // Activities CAN do I/O, use DI services, throw on failure
    }
}
```

**Workflow registration in Program.cs:**
```csharp
builder.Services.AddDaprWorkflow(options =>
{
    // Workflows
    options.RegisterWorkflow<IngestionWorkflow>();
    options.RegisterWorkflow<TenantProvisioningWorkflow>();
    options.RegisterWorkflow<TenantDeletionWorkflow>();
    options.RegisterWorkflow<ConsistencyVerificationWorkflow>();

    // Activities — Ingestion
    options.RegisterActivity<CheckIdempotencyActivity>();
    options.RegisterActivity<ValidateContentActivity>();
    options.RegisterActivity<ExtractContentActivity>();
    options.RegisterActivity<GenerateEmbeddingActivity>();

    // Activities — Indexing
    options.RegisterActivity<IndexSyntacticActivity>();
    options.RegisterActivity<IndexSemanticActivity>();
    options.RegisterActivity<IndexGraphActivity>();
    options.RegisterActivity<VerifyConsistencyActivity>();

    // Activities — Tenants
    options.RegisterActivity<ProvisionRediSearchActivity>();
    options.RegisterActivity<ProvisionRedisVectorActivity>();
    options.RegisterActivity<ProvisionFalkorDbActivity>();
    options.RegisterActivity<DeleteRediSearchActivity>();
    options.RegisterActivity<DeleteRedisVectorActivity>();
    options.RegisterActivity<DeleteFalkorDbActivity>();
    options.RegisterActivity<VerifyTenantActivity>();
});
```

**Workflow management via controllers:**
```csharp
// Schedule a workflow
string instanceId = await _workflowClient.ScheduleNewWorkflowAsync(
    nameof(IngestionWorkflow), input: ingestionInput);

// Check workflow status
WorkflowState? state = await _workflowClient.GetWorkflowStateAsync(instanceId);
```

**Fan-out/fan-in pattern for parallel indexing:**
```csharp
// Inside IngestionWorkflow — index all 3 backends in parallel
var syntacticTask = context.CallActivityAsync<IndexResult>(
    nameof(IndexSyntacticActivity), indexInput, retryOptions);
var semanticTask = context.CallActivityAsync<IndexResult>(
    nameof(IndexSemanticActivity), indexInput, retryOptions);
var graphTask = context.CallActivityAsync<IndexResult>(
    nameof(IndexGraphActivity), indexInput, retryOptions);

await Task.WhenAll(syntacticTask, semanticTask, graphTask);
```

**Saga compensation pattern:**
```csharp
// Inside TenantProvisioningWorkflow
try
{
    await context.CallActivityAsync(nameof(ProvisionRediSearchActivity), input, retryOptions);
    await context.CallActivityAsync(nameof(ProvisionRedisVectorActivity), input, retryOptions);
    await context.CallActivityAsync(nameof(ProvisionFalkorDbActivity), input, retryOptions);
    await context.CallActivityAsync(nameof(VerifyTenantActivity), input);
}
catch (WorkflowTaskFailedException)
{
    // Compensation: delete any successfully created indexes
    await context.CallActivityAsync(nameof(DeleteRediSearchActivity), input);
    await context.CallActivityAsync(nameof(DeleteRedisVectorActivity), input);
    await context.CallActivityAsync(nameof(DeleteFalkorDbActivity), input);
    throw;
}
```

### DAPR Actor Patterns

**Actor interface definition:**
```csharp
namespace Hexalith.Memories.Server.Actors;

public interface IEmbeddingRateLimiterActor : IActor
{
    Task<bool> TryConsumeAsync(int tokenCount);
    Task ResetAsync();
    Task<RateLimitState> GetStateAsync();
}
```

**Actor implementation:**
```csharp
namespace Hexalith.Memories.Server.Actors;

internal class EmbeddingRateLimiterActor : Actor, IEmbeddingRateLimiterActor
{
    public EmbeddingRateLimiterActor(ActorHost host) : base(host) { }

    public async Task<bool> TryConsumeAsync(int tokenCount)
    {
        var state = await StateManager.GetOrAddStateAsync("rateState",
            new RateLimitState { Remaining = 1000, WindowStart = DateTime.UtcNow });

        // Reset window if expired, decrement, persist before returning
        await StateManager.SetStateAsync("rateState", state);
        return state.Remaining >= 0;
    }
}
```

**Actor registration in Program.cs:**
```csharp
builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<EmbeddingRateLimiterActor>();
    options.Actors.RegisterActor<CorpusStatisticsActor>();

    options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
    options.ActorScanInterval = TimeSpan.FromSeconds(30);
    options.ReentrancyConfig = new() { Enabled = false };
});

// In middleware pipeline:
app.MapActorsHandlers();
```

**Actor proxy usage (from activities or services):**
```csharp
// In GenerateEmbeddingActivity — check rate limiter before calling embedding API
var rateLimiter = _actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(
    new ActorId(tenantId), nameof(EmbeddingRateLimiterActor));

bool allowed = await rateLimiter.TryConsumeAsync(tokenCount);
if (!allowed) throw new RateLimitExceededException(tenantId);
```

**CorpusStatisticsActor with timer refresh:**
```csharp
internal class CorpusStatisticsActor : Actor, ICorpusStatisticsActor
{
    protected override async Task OnActivateAsync()
    {
        // Register timer to refresh stats periodically
        await RegisterTimerAsync(
            "RefreshStats", nameof(RefreshStatsCallback),
            null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
}
```

### DAPR Conversation API Patterns (AI / LLM)

**Registration:**
```csharp
using Dapr.AI.Conversation.Extensions;

// In AddMemoriesServer() extension method:
services.AddDaprAiConversation();

// Suppress alpha warning in .csproj:
// <NoWarn>$(NoWarn);DAPR_CONVERSATION</NoWarn>
```

**Component YAML (deploy/dapr/components/conversation-llm.yaml):**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: llm
spec:
  type: conversation.openai     # Swap provider by changing type + key
  metadata:
  - name: key
    secretKeyRef:
      name: llm-secret
      key: api-key
  - name: model
    value: gpt-4-turbo
  - name: responseCacheTTL
    value: 10m
auth:
  secretStore: secretstore
```

**Usage in activities (AI enrichment):**
```csharp
namespace Hexalith.Memories.Server.Activities.AiEnrichment;

public class ExtractMetadataActivity : WorkflowActivity<MetadataExtractionInput, MetadataExtractionResult>
{
    private readonly DaprConversationClient _conversationClient;

    public ExtractMetadataActivity(DaprConversationClient conversationClient)
        => _conversationClient = conversationClient;

    public override async Task<MetadataExtractionResult> RunAsync(
        WorkflowActivityContext context, MetadataExtractionInput input)
    {
        var messages = new ConversationInput([
            new SystemMessage { Content = [new MessageContent(
                "Extract structured metadata from the following content. " +
                "Return JSON with fields: title, summary, tags, entities, dates.")] },
            new UserMessage { Content = [new MessageContent(input.Content)] }
        ]);

        var options = new ConversationOptions("llm")  // DAPR component name
        {
            Temperature = 0.1,
            ResponseFormat = new ResponseFormat("json_object")
        };

        var response = await _conversationClient.ConverseAsync([messages], options);
        // Parse structured JSON response into MetadataExtractionResult
    }
}
```

**Provider swap without code change:** Change `conversation.openai` → `conversation.anthropic` or `conversation.googleai` in YAML. Application code references the component name (`"llm"`), not the provider.

**Tool calling pattern (for agentic workflows):**
```csharp
var options = new ConversationOptions("llm")
{
    Tools = [new ToolFunction("search_memories") {
        Description = "Search the memory store for relevant content",
        Parameters = new Dictionary<string, object?> {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object> {
                ["query"] = new Dictionary<string, string> {
                    ["type"] = "string",
                    ["description"] = "Search query"
                }
            }
        }
    }],
    ToolChoice = ToolChoice.Auto
};
```

**Agent loop pattern (manual composition — Dapr Agents is Python-only):**
```csharp
// IngestionWorkflow can call AiEnrichmentWorkflow as a child workflow
var enrichment = await context.CallChildWorkflowAsync<EnrichmentResult>(
    nameof(AiEnrichmentWorkflow),
    input: new EnrichmentInput(content, tenantId),
    options: new ChildWorkflowTaskOptions(
        InstanceId: $"enrich-{context.InstanceId}",
        RetryPolicy: llmRetryPolicy));
```

**Microsoft.Extensions.AI bridge (IChatClient):**
```csharp
// For code that wants the standard IChatClient interface:
services.AddDaprChatClient("llm");  // bridges DaprConversationClient → IChatClient
```

### Polyglot Service Invocation Patterns

**Calling Python AI Agent service from C# activity:**
```csharp
namespace Hexalith.Memories.Server.Activities.AiEnrichment;

public class CallAiAgentActivity : WorkflowActivity<EnrichmentInput, EnrichmentResult>
{
    private readonly DaprClient _daprClient;

    public CallAiAgentActivity(DaprClient daprClient) => _daprClient = daprClient;

    public override async Task<EnrichmentResult> RunAsync(
        WorkflowActivityContext context, EnrichmentInput input)
    {
        // DAPR service invocation — language-agnostic
        return await _daprClient.InvokeMethodAsync<EnrichmentInput, EnrichmentResult>(
            appId: "ai-agent",           // Python service DAPR app ID
            methodName: "enrich",         // Endpoint on Python service
            data: input);
    }
}
```

**Python AI Agent service (services/ai-agent/app.py):**
```python
from dapr_agents import DurableAgent, tool
from dapr_agents.workflow.runners import AgentRunner
from pydantic import BaseModel

class EnrichmentInput(BaseModel):
    content: str
    tenant_id: str
    source_type: str

@tool
def extract_metadata(content: str) -> dict:
    """Extract structured metadata from content."""
    # LLM-powered via Dapr Conversation API (configured in agent)
    ...

@tool
def classify_content(content: str) -> str:
    """Classify content type and domain."""
    ...

@tool
def infer_causal_relations(content: str, existing_edges: list) -> list:
    """Detect causal relationships for graph edges."""
    ...

enrichment_agent = DurableAgent(
    name="enrichment-agent",
    role="Memory Enrichment Specialist",
    instructions=[
        "Extract metadata, classify content, and infer causal relationships.",
        "Return structured JSON matching the Contracts.V1 schema."
    ],
    tools=[extract_metadata, classify_content, infer_causal_relations],
    llm=DaprChatClient(component_name="llm"),
)

runner = AgentRunner()
runner.serve(enrichment_agent, port=5010)
```

**Aspire AppHost registration (polyglot):**
```csharp
// In Hexalith.Memories.AppHost/Program.cs

// Python AI Agent service — runs as container with DAPR sidecar
var aiAgent = builder.AddContainer("ai-agent", "hexalith-memories-ai-agent")
    .WithDockerfile("../../services/ai-agent")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "ai-agent",
        AppPort = 5010,
        DaprHttpPort = 3510,
        DaprGrpcPort = 50011,
        ResourcesDirectory = "./components"
    })
    .WithReference(stateStore)
    .WithReference(secretStore);

// C# Memories Server — references AI Agent via DAPR service invocation
var server = builder.AddProject<Projects.Hexalith_Memories_Server>("memories")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "memories",
        AppPort = 5000,
        DaprHttpPort = 3500,
        DaprGrpcPort = 50001,
        ResourcesDirectory = "./components"
    })
    .WithReference(stateStore)
    .WithReference(redis)
    .WithReference(falkordb);
```

**Contract sharing:** Python Pydantic models (`services/ai-agent/models/schemas.py`) mirror C# `Contracts.V1` types. JSON serialization with `camelCase` ensures interoperability. Breaking changes to Contracts must update both C# and Python models.

### Communication Patterns

**CloudEvents via DAPR pub/sub:** Standard CloudEvents 1.0 envelope with `hexalith.memories.events.*` type prefix.

**DAPR actor state:** Per-tenant actor state via `StateManager`. State persisted before every response — never rely on deactivation persistence.

**DAPR Workflow state:** Automatically persisted via Durable Task Framework. Incremental append-only history in actor state store. No manual state management needed.

**DAPR Conversation API:** Provider-agnostic LLM calls via `DaprConversationClient.ConverseAsync()`. No streaming (alpha limitation). Response caching and PII scrubbing available at sidecar level.

### Process Patterns

**Async:** `Task<T>` with `Async` suffix, `CancellationToken` as last parameter on every async method.

**DI registration:** Extension method per project (`services.AddMemoriesServer()`, `services.AddMemoriesRedis()`). DAPR Workflow and Actor registration in `Program.cs` via `AddDaprWorkflow()` and `AddActors()` — called from the extension method.

**Idempotency:** `CheckIdempotencyActivity` checks source identifier (event ID + aggregate ID) as first activity in `IngestionWorkflow`. Duplicate → workflow returns early without error.

### Test Patterns

**Framework:** xUnit v3 (`xunit.v3` 3.2.2), Shouldly 4.3.0, NSubstitute 5.3.0, coverlet.collector

**Three-tier structure:**
- **Tier 1:** Unit tests (no external deps) — run on every PR
- **Tier 2:** Integration tests (requires DAPR slim init + Redis/FalkorDB)
- **Tier 3:** Aspire e2e (requires full DAPR init + Docker)

**Evidence claim boundary:** A tier identifies infrastructure depth, not every service boundary
traversed. Evidence must name both the tier and the exercised system boundary.

- Package/source build evidence proves compile and dependency-graph compatibility only.
- Memories Aspire evidence may claim the concrete resources observed, including Redis Stack,
  FalkorDB, OpenBao, and Memories Dapr sidecars.
- Direct `POST /events/ingest` or Dapr publish to the Memories `pubsub` subscription proves the
  Memories event-intake contract; it does not prove an EventStore producer or gateway.
- EventStore-to-Memories full-stack evidence requires an AppHost-provisioned `eventstore` resource,
  Story 1.20-aligned source/package identity, an EventStore-originating event, persisted/searchable
  Redis and FalkorDB outcomes, duplicate replay proof, and tenant-isolation negative evidence.

**Test naming:** `{ClassName}Tests.cs` with descriptive methods. Shouldly assertions.
```csharp
result.CompositeScore.ShouldBeInRange(0.0f, 1.0f);
results.ShouldNotBeEmpty();
```

### Enforcement Guidelines

**All AI agents MUST:**
1. Follow `.gitattributes` for line-ending normalization and `.editorconfig` for editor-facing code style
2. Use `Directory.Packages.props` for all NuGet versions — never in `.csproj`
3. Use `.slnx` solution format — never create `.sln`
4. Add `CancellationToken` as last parameter to every async method
5. Use Result pattern for domain logic, FluentValidation for input validation, exceptions only for infrastructure
6. Persist actor state before every response via `StateManager`
7. Use `camelCase` in JSON, `camelCase` for Redis fields, dot-separated for DAPR topics
8. Place tests in mirrored project structure under `tests/`
9. Register DI services via extension methods per project
10. Use DAPR Workflow for all multi-step orchestrations — never hand-roll state machines or queues
11. Use DAPR Actors for per-tenant stateful singletons — never use static/global state for per-tenant concerns
12. Activities do I/O; workflows orchestrate — workflows must never call external services directly
13. Use `WorkflowRetryPolicy` for activity retry — never implement custom retry loops in workflows
14. Use `context.CreateReplaySafeLogger<T>()` in workflows — regular logging replays produce duplicate log entries
15. Use DAPR Conversation API (`DaprConversationClient`) for all LLM communication — never call LLM provider APIs directly
16. Reference LLM providers by DAPR component name (`"llm"`), not by provider-specific identifiers — enables provider swap via YAML
17. When a Python/other-language library is the best fit, create a DAPR sidecar service — never reimplement in C# what exists as a mature library elsewhere
18. Python Pydantic models must mirror C# `Contracts.V1` types with `camelCase` JSON — breaking changes update both
19. Call polyglot services via `DaprClient.InvokeMethodAsync()` — never via raw HTTP

### Additional Decisions (EventStore Alignment)

| # | Decision | Choice | Source |
|---|---|---|---|
| D18 | Error handling | Result pattern + FluentValidation + infrastructure exceptions | EventStore |
| D19 | Async pattern | `Task<T>` with `Async` suffix | EventStore |
| D20 | CancellationToken | Propagated through all async layers | EventStore |
| D21 | DI registration | Extension methods per project | EventStore |
| D22 | Code style | `.slnx`, file-scoped namespaces, Allman braces, `_camelCase`, nullable, warnings-as-errors, `Directory.Packages.props` | EventStore |

## Project Structure & Boundaries

### Complete Project Directory Structure

```
Hexalith.Memories/
├── .editorconfig
├── .gitignore
├── .gitmodules                                # Root-declared submodules under references/
├── .releaserc.json                            # semantic-release config
├── commitlint.config.js                       # Conventional Commits enforcement
├── Directory.Build.props                      # Shared MSBuild properties
├── Directory.Packages.props                   # Centralized NuGet versions
├── global.json                                # SDK 10.0 pinning
├── nuget.config
├── Hexalith.Memories.slnx                     # Solution (XML format only)
├── LICENSE                                    # Apache 2.0
├── LICENSE-DEPENDENCIES.md                    # FalkorDB AGPL, Redis Stack SSPL boundaries
├── README.md
├── CONTRIBUTING.md                            # Commit conventions, submodule setup, test tiers
├── CLAUDE.md                                  # AI agent instructions
├── AGENTS.md                                  # Multi-agent instructions
│
├── .github/
│   └── workflows/
│       ├── ci.yml                             # Build + Tier 1+2 tests on PR
│       ├── release.yml                        # semantic-release + NuGet publish on main
│       └── integration.yml                    # Tier 3 Aspire e2e (optional/nightly)
│
├── src/
│   ├── Hexalith.Memories.Contracts/
│   │   ├── V1/
│   │   │   ├── MemoryUnit.cs
│   │   │   ├── GraphEdge.cs
│   │   │   ├── Case.cs
│   │   │   ├── Tenant.cs
│   │   │   ├── SearchQuery.cs
│   │   │   ├── SearchResult.cs
│   │   │   ├── ScoredResult.cs
│   │   │   ├── FusionWeights.cs
│   │   │   ├── IngestionStatus.cs
│   │   │   ├── MetadataField.cs
│   │   │   ├── EdgeType.cs
│   │   │   ├── SourceType.cs
│   │   │   ├── ErrorResponse.cs
│   │   │   ├── Commands/
│   │   │   │   ├── IngestContentCommand.cs
│   │   │   │   ├── CreateCaseCommand.cs
│   │   │   │   ├── DeleteCaseCommand.cs
│   │   │   │   ├── CreateTenantCommand.cs
│   │   │   │   └── DeleteTenantCommand.cs
│   │   │   ├── Events/
│   │   │   │   ├── MemoryUnitIngested.cs
│   │   │   │   ├── MemoryUnitDeleted.cs
│   │   │   │   ├── CaseCreated.cs
│   │   │   │   ├── TenantCreated.cs
│   │   │   │   └── TenantDeleted.cs
│   │   │   └── Results/
│   │   │       └── DomainResult.cs
│   │   └── Hexalith.Memories.Contracts.csproj
│   │
│   ├── Hexalith.Memories.Server/
│   │   ├── Workflows/
│   │   │   ├── IngestionWorkflow.cs                # Multi-step ingestion orchestration
│   │   │   ├── AiEnrichmentWorkflow.cs             # LLM-powered enrichment (child workflow, DAPR Conversation API)
│   │   │   ├── TenantProvisioningWorkflow.cs       # Multi-backend setup with saga rollback
│   │   │   ├── TenantDeletionWorkflow.cs           # Batched async deletion across backends
│   │   │   └── ConsistencyVerificationWorkflow.cs  # On-demand/scheduled consistency check
│   │   ├── Activities/
│   │   │   ├── Ingestion/
│   │   │   │   ├── ValidateContentActivity.cs
│   │   │   ��   ├── ExtractContentActivity.cs       # Calls Kreuzberg (in-process)
│   │   │   │   ├── GenerateEmbeddingActivity.cs    # Calls embedding API (checks rate limiter actor)
│   │   │   │   └── CheckIdempotencyActivity.cs     # Duplicate detection
│   │   │   ├── AiEnrichment/
│   │   │   │   └── CallAiAgentActivity.cs          # Invokes Python Dapr Agents service via DAPR service invocation
│   │   │   ├── Indexing/
│   │   │   │   ├── IndexSyntacticActivity.cs       # RediSearch write
│   │   │   │   ├── IndexSemanticActivity.cs        # Redis Vector write
│   │   │   │   ├── IndexGraphActivity.cs           # FalkorDB write
│   │   │   │   └── VerifyConsistencyActivity.cs    # Check all 3 backends
│   │   │   └── Tenants/
│   │   │       ├── ProvisionRediSearchActivity.cs
│   │   │       ├── ProvisionRedisVectorActivity.cs
│   │   │       ├── ProvisionFalkorDbActivity.cs
│   │   │       ├── DeleteRediSearchActivity.cs
│   │   │       ├── DeleteRedisVectorActivity.cs
│   │   │       ├── DeleteFalkorDbActivity.cs
│   │   │       └── VerifyTenantActivity.cs
│   │   ├── Actors/
│   │   │   ├── EmbeddingRateLimiterActor.cs        # Per-tenant rate limiting (actor ID = tenant ID)
│   │   │   ├── IEmbeddingRateLimiterActor.cs       # Actor interface
│   │   │   ├── CorpusStatisticsActor.cs            # Per-tenant corpus stats caching with timer refresh
│   │   │   └── ICorpusStatisticsActor.cs           # Actor interface
│   │   ├── Ingestion/
│   │   │   ├── IngestionValidator.cs
│   │   │   ├── ContentExtractionClient.cs
│   │   │   └── EmbeddingClient.cs
│   │   ├── AI/
│   │   │   ├── ConversationClientFactory.cs        # Configures DaprConversationClient per tenant/provider
│   │   │   └── AiAgentServiceClient.cs             # Typed client for Python Dapr Agents service invocation
│   │   ├── Search/
│   │   │   ├── FusionAlgorithm.cs
│   │   │   ├── Bm25Normalizer.cs
│   │   │   ├── GraphScorer.cs
│   │   │   ├── GraphScopedSearch.cs
│   │   │   ├── CorpusStatisticsProvider.cs         # Delegates to CorpusStatisticsActor via IActorProxyFactory
│   │   │   └── SearchService.cs
│   │   ├── Tenants/
│   │   │   ├── TenantProvisioningService.cs        # Triggers TenantProvisioningWorkflow
│   │   │   ├── TenantIsolationVerifier.cs
│   │   │   ├── TenantInfrastructureResolver.cs
│   │   │   └── TenantDeletionService.cs            # Triggers TenantDeletionWorkflow
│   │   ├── Graph/
│   │   │   ├── GraphQueryBuilder.cs                # IGraphQueryBuilder (safety interface)
│   │   │   ├── GraphTraversalService.cs
│   │   │   └── GapFiller.cs
│   │   ├── Cases/
│   │   │   ├── CaseService.cs
│   │   │   └── CaseValidator.cs
│   │   ├── Endpoints/                            # REST surface is minimal-API (app.MapGet/MapPost/... in Program.cs); handler bodies extracted here
│   │   │   └── MemoryUnitLookupEndpoint.cs        # (no Controllers/ folder; cross-module event intake uses EventIngestionController in the EventStore submodule)
│   │   ├── Extensions/
│   │   │   └── MemoriesServerServiceCollectionExtensions.cs
│   │   ├── Program.cs
│   │   └── Hexalith.Memories.Server.csproj
│   │
│   ├── Hexalith.Memories.Redis/
│   │   ├── Syntactic/
│   │   │   ├── RediSearchIndexManager.cs
│   │   │   └── RediSearchQueryExecutor.cs
│   │   ├── Semantic/
│   │   │   ├── RedisVectorIndexManager.cs
│   │   │   └── RedisVectorQueryExecutor.cs
│   │   ├── Graph/
│   │   │   ├── FalkorDbConnectionManager.cs
│   │   │   └── FalkorDbQueryExecutor.cs
│   │   ├── Extensions/
│   │   │   └── MemoriesRedisServiceCollectionExtensions.cs
│   │   └── Hexalith.Memories.Redis.csproj
│   │
│   ├── Hexalith.Memories.Client/              # Phase 1.5
│   ├── Hexalith.Memories.Client.Rest/         # Phase 1.5
│   ├── Hexalith.Memories.Cli/                 # MVP essentials; Phase 1.5 polish expansion
│   ├── Hexalith.Memories.Mcp/                 # Phase 1.5
│   ├── Hexalith.Memories.EventStore/          # Phase 1.5
│   │
│   ├── Hexalith.Memories.AppHost/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Hexalith.Memories.AppHost.csproj
│   │
│   └── Hexalith.Memories.ServiceDefaults/
│       ├── Extensions.cs
│       └── Hexalith.Memories.ServiceDefaults.csproj
│
├── tests/
│   ├── Directory.Build.props
│   ├── Hexalith.Memories.Contracts.Tests/     # Tier 1
│   │   └── V1/
│   │       ├── MemoryUnitTests.cs
│   │       ├── GraphEdgeTests.cs
│   │       └── SerializationTests.cs
│   ├── Hexalith.Memories.Server.Tests/        # Tier 1 + Tier 2
│   │   ├── Workflows/
│   │   │   ├── IngestionWorkflowTests.cs       # Verify activity sequencing, compensation
│   │   │   ├── TenantProvisioningWorkflowTests.cs
│   │   │   └── TenantDeletionWorkflowTests.cs
│   │   ├── Activities/
│   │   │   ├── ExtractContentActivityTests.cs  # Test activities independently with mocked deps
│   │   │   ├── GenerateEmbeddingActivityTests.cs
│   │   │   ├── IndexSyntacticActivityTests.cs
│   │   │   └── VerifyConsistencyActivityTests.cs
│   │   ├── Actors/
│   │   │   ├── EmbeddingRateLimiterActorTests.cs
│   │   │   └── CorpusStatisticsActorTests.cs
│   │   ├── Search/
│   │   │   ├── FusionAlgorithmTests.cs
│   │   │   ├── Bm25NormalizerTests.cs
│   │   │   └── GraphScorerTests.cs
│   │   ├── Ingestion/
│   │   │   └── IngestionValidatorTests.cs
│   │   └── Tenants/
│   │       └── TenantProvisioningServiceTests.cs
│   ├── Hexalith.Memories.Redis.Tests/         # Tier 2
│   │   ├── Syntactic/
│   │   │   └── RediSearchQueryExecutorTests.cs
│   │   ├── Semantic/
│   │   │   └── RedisVectorQueryExecutorTests.cs
│   │   ├── Graph/
│   │   │   └── FalkorDbQueryExecutorTests.cs
│   │   └── TenantIsolationTests.cs            # Gate 2 test suite
│   ├── Hexalith.Memories.IntegrationTests/    # Tier 3
│   │   ├── BenchmarkSuiteTests.cs             # Gate 1: NDCG@10
│   │   ├── IngestionPipelineTests.cs
│   │   └── ConsistencyCompensationTests.cs
│   └── Hexalith.Memories.Benchmarks/
│       ├── Data/
│       │   ├── synthetic-corpus.json
│       │   └── ground-truth.json
│       └── BenchmarkQuerySet.cs
│
├── services/
│   └── ai-agent/                                    # Python Dapr Agents service (polyglot)
│       ├── pyproject.toml                           # dapr-agents>=1.0.0, pydantic, fastapi
│       ├── Dockerfile
│       ├── app.py                                   # FastAPI + Dapr Agents entry point
│       ├── agents/
│       │   ├── __init__.py
│       │   ├── enrichment_agent.py                  # DurableAgent: metadata extraction + classification
│       │   ├── causal_agent.py                      # DurableAgent: causal relationship inference
│       │   └── tools/
│       │       ├── __init__.py
│       │       ├── metadata_tools.py                # @tool: structured metadata extraction
│       │       ├── classification_tools.py          # @tool: content classification
│       │       └── causal_tools.py                  # @tool: causal relation detection
│       ├── models/
│       │   ├── __init__.py
│       │   └── schemas.py                           # Pydantic models matching C# Contracts.V1
│       └── tests/
│           ├── test_enrichment_agent.py
│           └── test_causal_agent.py
│
├── samples/
│   └── Hexalith.Memories.Sample/
│
├── deploy/
│   └── dapr/
│       ├── components/
│       │   ├── statestore.yaml               # Redis with actorStateStore: "true" (workflows + actors)
│       │   ├── pubsub.yaml
│       │   ├── secrets.yaml
│       │   └── conversation-llm.yaml        # DAPR Conversation component (LLM provider config)
│       └── config.yaml
│
└── docs/
    ├── operator-guide.md
    └── compliance-guide.md
```

### Architectural Boundaries

**API Boundaries:**

| Boundary | Entry Point | Validation | Auth (MVP) | Auth (Phase 1.5) |
|---|---|---|---|---|
| REST (external) | minimal-API endpoints in `Program.cs` (`app.MapGet/MapPost/...`) | `IngestionValidator` + FluentValidation | JWT bearer fallback policy plus tenant authorization | `TenantAuthorizationMiddleware` and endpoint filters |
| DAPR service invocation (internal) | DAPR endpoint mapping | Same validators | DAPR API token | DAPR API token |
| MCP (Phase 1.5) | `Mcp/` project | Delegates to Server via Client | — | MCP-level auth |

**Service Boundaries:**

| Service | Language | Owns | Communicates With | Protocol |
|---|---|---|---|---|
| Memories Server | C# | Domain logic, workflows, actors, search, tenants | Redis, FalkorDB, Embedding API, AI Agent Service | DAPR workflows/actors/state/service-invocation, HTTP |
| AI Agent Service | Python | AI enrichment agents, NLP tools, causal inference | LLM providers (via DAPR Conversation API), Memories Server (callbacks) | DAPR service invocation, DAPR Conversation API |
| MCP Server (Phase 1.5) | C# | MCP tool definitions, token-budget shaping | Memories Server | DAPR service invocation |
| CLI (MVP essentials; Phase 1.5 expansion) | C# | Terminal UX, output formatting, benchmark/onboarding command surface | Memories Server | MVP: minimal direct HTTP/ingress adapter inside CLI. Phase 1.5: reusable `Client.Rest`. |

**Data Boundaries:**

| Backend | Owns | Tenant Isolation | Access Pattern |
|---|---|---|---|
| RediSearch | Syntactic index (BM25) | `{tenantId}:memories:idx` per tenant; Story 24.3 target adds per-tenant Redis ACL users via tenant-scoped backend resolution | `NRedisStack` via `RediSearchQueryExecutor` |
| Redis Vector | Raw + natural-language semantic indexes (HNSW) | `{tenantId}:memories:vec` and `{tenantId}:memories:vec:nl` per tenant; prefixes/hash tags/logical DBs are placement tools, not the primary security boundary | `NRedisStack` via `RedisVectorQueryExecutor` |
| FalkorDB | Graph (edges, traversal) | Separate database per tenant | `NFalkorDB` via `FalkorDbQueryExecutor` through `IGraphQueryBuilder` |
| Redis State | DAPR workflow state + actor state | Shared instance, workflow instance IDs + actor IDs scoped by tenant | DAPR SDK (workflows + actors) |

### FR Category to Structure Mapping

| FR Category | Primary Location | Key Files |
|---|---|---|
| Knowledge Ingestion (FR1-13) | `Server/Workflows/` + `Server/Activities/Ingestion/` + `Server/Ingestion/` | `IngestionWorkflow.cs`, `ValidateContentActivity.cs`, `ExtractContentActivity.cs`, `GenerateEmbeddingActivity.cs`, `CheckIdempotencyActivity.cs`, `IngestionValidator.cs`, `ContentExtractionClient.cs`, `EmbeddingClient.cs` |
| Knowledge Retrieval (FR14-25) | `Server/Search/` | `FusionAlgorithm.cs`, `SearchService.cs`, `Bm25Normalizer.cs`, `GraphScorer.cs`, `GraphScopedSearch.cs` |
| Memory Organization (FR26-37) | `Server/Cases/` | `CaseService.cs`, `CaseValidator.cs` |
| Tenant Management (FR38-45) | `Server/Workflows/` + `Server/Activities/Tenants/` + `Server/Tenants/` | `TenantProvisioningWorkflow.cs`, `TenantDeletionWorkflow.cs`, `ProvisionRediSearchActivity.cs`, `ProvisionRedisVectorActivity.cs`, `ProvisionFalkorDbActivity.cs`, `TenantProvisioningService.cs`, `TenantIsolationVerifier.cs`, `TenantDeletionService.cs` |
| Causal Intelligence (FR46-52) | `Server/Graph/` | `GraphTraversalService.cs`, `GraphQueryBuilder.cs`, `GapFiller.cs` |
| Developer Interfaces (FR53-58) | `Server/Program.cs` + `Server/Endpoints/`, `Cli/`, `Mcp/`, `EventStore/EventIngestionController.cs` | Minimal API endpoints + MVP CLI essentials, EventStore Dapr event controller, MCP and full CLI expansion in Phase 1.5 |
| EventStore Integration (FR59-62) | `EventStore/` | Phase 1.5 |
| Trust & Transparency (FR63-67) | `Contracts/V1/` | `MetadataField.cs`, `ScoredResult.cs`, `SearchResult.cs`, Evidence Packet contracts (`EvidencePacket`, scope, source, state, omitted details, recovery actions) |
| Embedding Provider (FR68-70) | `Server/Ingestion/` | `EmbeddingClient.cs`, tenant config |
| Data Portability (FR71) | `Server/Tenants/` | Phase 2 `TenantExportService.cs` |
| System Health & Consistency (FR72-74) | `Server/Tenants/`, health checks, workflows | `TenantIsolationVerifier.cs`, readiness/liveness health checks, `ConsistencyVerificationWorkflow.cs`, repair activities |

### Data Flow

```
Event ingest: Hexalith module → DAPR pub/sub component → Memories Server DAPR sidecar → POST /events/ingest → EventIngestionService → DaprWorkflowClient.ScheduleNewWorkflowAsync(IngestionWorkflow)

Content ingest: CLI/MCP/REST → minimal API endpoint → DaprWorkflowClient.ScheduleNewWorkflowAsync(IngestionWorkflow)
  IngestionWorkflow orchestration:
    1. CheckIdempotencyActivity (duplicate detection)
    2. ValidateContentActivity (domain validation)
    3. ExtractContentActivity → Kreuzberg (in-process, P/Invoke)
    4. EmbeddingRateLimiterActor.TryConsumeAsync() (per-tenant gate)
    5. GenerateEmbeddingActivity → Google Embedding API (HTTP)
    6. [Optional] AiEnrichmentWorkflow (child workflow):
       CallAiAgentActivity → DAPR service invocation → Python AI Agent Service
       Python DurableAgent orchestrates:
         a. Metadata extraction (LLM via DAPR Conversation API)
         b. Content classification (LLM + NLP tools)
         c. Causal relationship inference (LLM + graph tools)
       Returns enrichment results to C# workflow
    7. Fan-out: IndexSyntacticActivity + IndexSemanticActivity + IndexGraphActivity
       (parallel writes to RediSearch, Redis Vector, FalkorDB)
       Each with WorkflowRetryPolicy (exponential backoff)
       On failure: compensation activities clean up partial writes
    8. VerifyConsistencyActivity (all 3 backends)
  Workflow state persisted at each step — survives restarts.

Search: CLI/MCP → minimal API endpoint → SearchService
  → RediSearchQueryExecutor (syntactic) + RedisVectorQueryExecutor (semantic)
  + FalkorDbQueryExecutor (graph, optional)
  → CorpusStatisticsActor (per-tenant BM25 stats)
  → FusionAlgorithm (pure function) → SearchResult

Traverse: CLI/MCP → minimal API endpoint → GraphTraversalService
  → FalkorDbQueryExecutor (via IGraphQueryBuilder)
  → Ordered nodes + edges + gap markers

Tenant Ops: minimal API endpoint → DaprWorkflowClient.ScheduleNewWorkflowAsync(...)
  TenantProvisioningWorkflow: sole tenant infrastructure lifecycle owner; provision 3 backends sequentially, rollback on failure
  TenantDeletionWorkflow: batched deletion across 3 backends
  ConsistencyVerificationWorkflow: audit all memory units across backends
```

## Architecture Validation Results

### Coherence Validation

**Decision Compatibility:** All 31 decisions validated. No version conflicts across .NET 10 + Aspire 13.1.3 + DAPR 1.17.6 (Client + Workflow + Actors + AspNetCore) + NRedisStack 1.3.0 + NFalkorDB 1.0.0 + MediatR 14.0.0 + FluentValidation 12.1.1.

**Pattern Consistency:** camelCase JSON / camelCase Redis / dot-separated DAPR / PascalCase C# — consistent. EventStore alignment verified across code style, test conventions, error handling, and DI patterns.

**Structure Alignment:** Project structure supports all decisions. Feature-based namespaces map to FR categories. Three-tier test structure mirrors EventStore. Composition root enables Phase 1.5 additive design.

**Validation layer clarification:** FluentValidation (MediatR pipeline) validates command *structure* — required fields, format, range. `IngestionValidator` validates domain *rules* — tenant exists, case exists, content type supported. Two complementary layers, no overlap.

### Requirements Coverage

**Functional Requirements: 74/74 architecturally traceable. Active MVP scope is phase-filtered.**

| Status | FRs | Note |
|---|---|---|
| Active MVP readiness | FR1-FR22, FR24-FR53, FR55-FR57, FR63-FR70, FR72-FR74 | Covered by Epic 0-Epic 8 MVP scope |
| Phase 1.5 fast-follow | FR23, FR54, FR58-FR62 | MCP and EventStore integration scope; architecturally additive, not MVP-counted |
| Deferred (Phase 2) | FR71 (export) | Portable export remains non-MVP unless an approved sprint change pulls it forward |

**Non-Functional Requirements: 31/31 architecturally traceable. Active MVP scope is phase-filtered.**

| Status | NFRs | Note |
|---|---|---|
| Active MVP readiness | NFR1-NFR4, NFR8, NFR16-NFR17, NFR24-NFR26, NFR30-NFR31 | MVP gate and thesis-validation NFRs |
| Phase 1.5 fast-follow | NFR6, NFR11, NFR20-NFR21 | Event freshness, ingress auth, MCP protocol conformance, CloudEvents publisher compatibility |
| Ongoing / operational hardening | NFR5, NFR7, NFR9-NFR10, NFR12-NFR15, NFR18-NFR19, NFR22-NFR23, NFR27-NFR29 | Required but not all MVP gate-blocking; implemented through MVP and operational-readiness tracks as selected |

### Resolved Gaps

| Gap | Resolution |
|---|---|
| FR71 (export) missing | Deferred to Phase 2 |
| No Testing utilities project | `Hexalith.Memories.Testing` added from start |
| Dual validation layers unclear | Clarified: FluentValidation = structure, IngestionValidator = domain rules |

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context analyzed (74 FRs, 31 NFRs, 5 top drivers)
- [x] Scale and complexity assessed (High)
- [x] 15 technical constraints with first-principles rationale
- [x] 9 cross-cutting concerns, phase-tagged
- [x] Memory unit + graph edge data model defined

**Architectural Decisions**
- [x] 31 decisions (D1-D31) with versions and rationale
- [x] Technology stack verified via web search
- [x] Gate-blocking vs deferrable summary
- [x] Decision registry for quick reference
- [x] PRD deviations documented

**Implementation Patterns**
- [x] Naming conventions (JSON, Redis, DAPR, C#)
- [x] Structure patterns (feature-based, mirrored tests, three-tier)
- [x] Communication patterns (CloudEvents, actor state, pub/sub)
- [x] Process patterns (Result, async, DI, cancellation)
- [x] 19 enforcement rules with examples and anti-patterns (including DAPR Workflow/Actor/AI/Polyglot rules)
- [x] EventStore alignment verified

**Project Structure**
- [x] Complete directory structure (~65 files)
- [x] Component boundaries (API, service, data)
- [x] Integration points mapped
- [x] FR-to-structure mapping (10 categories)
- [x] Data flow documented (ingest, search, traverse)

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** High

**Key Strengths:**
- Gate-blocking summary provides daily implementation compass
- 31 decisions with explicit rationale prevent re-debates
- EventStore alignment ensures ecosystem consistency
- Testability architecture enables TDD from day one
- Phase-tagging prevents over-engineering MVP
- Data model field inventory prevents implementation churn
- Deployment topology guides infrastructure provisioning

**Areas for Future Enhancement:**
- Benchmark query set (3-5 queries with ground truth) — create before Gate 1 testing
- Sizing formula validation against actual Redis memory after first 1000 units
- FalkorDB performance benchmarking at >100K nodes
- Concurrent index version support (Phase 2)

### Updated Structure Additions

```
src/
  Hexalith.Memories.Testing/
    ├── Builders/
    │   ├── MemoryUnitBuilder.cs
    │   ├── GraphEdgeBuilder.cs
    │   └── TenantBuilder.cs
    ├── Fakes/
    │   ├── FakeCorpusStatisticsProvider.cs
    │   └── FakeTenantInfrastructureResolver.cs
    ├── Extensions/
    │   └── MemoriesTestingServiceCollectionExtensions.cs
    └── Hexalith.Memories.Testing.csproj

tests/
  Hexalith.Memories.Testing.Tests/         # Tier 1
```

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all 31 architectural decisions (D1-D31) exactly as documented
- Use implementation patterns consistently — `.editorconfig` and EventStore CLAUDE.md are sources of truth
- Respect project structure and boundaries — FR-to-structure mapping defines where code lives
- Use DAPR Workflow for all multi-step orchestrations — never hand-roll state machines or queues
- Use DAPR Actors for per-tenant stateful singletons — never use static/global state
- Use DAPR service invocation for polyglot calls — Python AI Agent service for enrichment
- When a best-of-breed library exists in Python, create a DAPR sidecar service — don't reimplement
- Gate-blocking table defines implementation priority
- Refer to this document for all architectural questions

**First Implementation Steps:**
1. Project scaffold: `dotnet new aspire`, create Contracts + Server + Redis + Testing + AppHost + ServiceDefaults
2. Configure git submodules under `references/` (`references/Hexalith.Commons`, `references/Hexalith.EventStore`)
3. Set up `Directory.Packages.props` (including `Dapr.Workflow`, `Dapr.Actors`, `Dapr.Actors.AspNetCore` 1.17.6), `.editorconfig`, `global.json`, `.releaserc.json`
4. Configure DAPR statestore component with `actorStateStore: "true"` for workflow + actor state
5. Minimum build/test CI preflight: `.github/workflows/ci.yml` with restore, build, and Docker-free unit/contract tests before Epic 1 data-writing work; release automation remains later operational-readiness scope.
6. Register workflows and actors in `Program.cs` via `AddDaprWorkflow()` + `AddActors()` + `MapActorsHandlers()`
7. Begin Gate 1 critical path: `FusionAlgorithm.cs` + `Bm25Normalizer.cs` + unit tests
8. Implement `IngestionWorkflow` + activities for end-to-end ingestion pipeline
