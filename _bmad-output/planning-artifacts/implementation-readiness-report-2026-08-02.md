---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
status: complete
overallReadiness: NOT READY
completedAt: 2026-08-02
assessor: Codex (BMad Implementation Readiness Workflow)
documentsIncluded:
  prd:
    - prd.md
  architecture:
    - architecture.md
  epics:
    - epics.md
  ux:
    - ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-02
**Project:** memories

## Document Discovery

### Documents Included

| Document Type | File | Size (bytes) | Modified |
| --- | --- | ---: | --- |
| PRD | `prd.md` | 88,353 | 2026-07-19 16:20:02 +0200 |
| Architecture | `architecture.md` | 121,376 | 2026-08-02 11:06:53 +0200 |
| Epics and Stories | `epics.md` | 382,434 | 2026-08-02 11:06:53 +0200 |
| UX Design | `ux-design-specification.md` | 99,240 | 2026-06-27 08:02:38 +0200 |

No sharded variants or missing required documents were found.

### Supplemental Filename Matches Excluded from the Primary Set

#### Architecture pattern

- `sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md` — 10,985 bytes — 2026-07-16 12:44:58 +0200
- `sprint-change-proposal-2026-07-28-architecture-anchor-reverification.md` — 9,327 bytes — 2026-07-28 20:14:45 +0200

#### Epic pattern

- `sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md` — 9,747 bytes — 2026-06-02 17:54:55 +0200
- `sprint-change-proposal-2026-07-06-epic-0-evidence-map.md` — 4,623 bytes — 2026-07-06 18:09:33 +0200
- `sprint-change-proposal-2026-07-06-epic17-browser-at-gap-closure.md` — 12,987 bytes — 2026-07-06 18:10:08 +0200
- `sprint-change-proposal-2026-07-06-epic-17-deferred-web-triage.md` — 4,978 bytes — 2026-07-06 18:15:52 +0200
- `sprint-change-proposal-2026-07-16-epic-0-evidence-map-maintenance.md` — 8,531 bytes — 2026-07-16 10:16:14 +0200
- `sprint-change-proposal-2026-07-16-epic-26-benchmark-closure.md` — 17,490 bytes — 2026-07-16 12:55:51 +0200
- `sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md` — 24,777 bytes — 2026-07-27 08:14:01 +0200
- `sprint-change-proposal-2026-07-28-epic-ac-code-verification.md` — 21,436 bytes — 2026-07-28 16:01:46 +0200
- `sprint-change-proposal-2026-07-28-epic-ac-verification-route-and-binding-coverage.md` — 40,125 bytes — 2026-07-28 20:18:52 +0200
- `sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md` — 28,861 bytes — 2026-08-01 11:06:00 +0200
- `sprint-change-proposal-2026-08-02-epic-23-documentation-verification.md` — 27,427 bytes — 2026-08-02 18:54:46 +0200

#### UX pattern

- `sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` — 17,841 bytes — 2026-06-27 08:08:21 +0200

These files were confirmed as supplemental matches and excluded from the primary assessment set.

## PRD Analysis

### Functional Requirements

#### Knowledge Ingestion

- **FR1:** Developer can ingest content from local files into a specified case
- **FR2:** Developer can ingest content from URLs into a specified case
- **FR3:** Developer can batch-ingest content from a directory into a specified case
- **FR4:** System can extract text from ingested content (plain text, PDF, markdown)
- **FR5:** System can generate embeddings for ingested content via a configurable embedding provider
- **FR6:** System ensures a memory unit is fully searchable across all axes after ingestion completes
- **FR7:** Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score
- **FR8:** System manages ingestion load per tenant independently
- **FR9:** System retries failed ingestion automatically with configurable limits
- **FR10:** Developer can view ingestion status per case (queued, embedding, indexed, failed counts)
- **FR11:** Developer can view failed ingestion units with error details and failure stage
- **FR12:** Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk
- **FR13:** System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes)

#### Knowledge Retrieval

- **FR14:** Developer can search memory units by syntactic matching within a tenant
- **FR15:** Developer can search memory units by semantic similarity within a tenant
- **FR16:** Developer can search memory units by graph traversal within a tenant
- **FR17:** Developer can search memory units by hybrid fusion combining all available axes
- **FR18:** Developer can control which axes are included in a search query
- **FR19:** Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode)
- **FR20:** Developer can filter search results by case
- **FR21:** Developer can filter search results by metadata field values
- **FR22:** Developer can paginate search results
- **FR23:** LLM Agent can constrain search response size by token budget
- **FR24:** System returns the origin identifier (file path, URL, or event ID) and origin type for each search result
- **FR25:** Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output

#### Memory Organization

- **FR26:** Developer can create a case within a tenant
- **FR27:** Developer can delete a case and all its memory units
- **FR28:** Developer can add members to a case
- **FR29:** Developer can remove members from a case
- **FR30:** Developer can list cases within a tenant
- **FR31:** Developer can view case status including memory unit count, last activity timestamp, and health indicators
- **FR32:** System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion
- **FR33:** System maintains case-scoped graph edges between memory units within a case
- **FR34:** Developer can search across all cases within a tenant by keyword, returning results with case attribution
- **FR35:** Developer can delete an individual memory unit from a case
- **FR36:** Developer can view recent activity within a case (ingestion events, searches, membership changes)
- **FR37:** Developer can annotate or correct a memory unit, with annotations tracked as linked memory units

#### Tenant Management

- **FR38:** Operator can create a tenant with physically separate indexes
- **FR39:** Operator can delete a tenant and all its indexes, graph data, and memory units
- **FR40:** Operator can verify tenant isolation via automated checks
- **FR41:** Operator can list tenants
- **FR42:** Operator can update tenant configuration after creation (rate limits, display name, settings)
- **FR43:** System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment
- **FR44:** System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages
- **FR45:** Operator can view current configuration of a tenant (embedding provider, rate limits, index status)

#### Causal Intelligence

- **FR46:** System can index CausationId and CorrelationId from events as typed, directional graph edges
- **FR47:** Developer can traverse causal chains from a starting node with configurable depth
- **FR48:** Developer can filter graph traversal by edge type
- **FR49:** When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier
- **FR50:** System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence
- **FR51:** Developer can promote AI-inferred edge confidence when verifying a relationship
- **FR52:** System maintains chronological ordering and timestamps on causal chain nodes

#### Developer Interfaces

- **FR53:** Developer can interact with all retrieval and ingestion capabilities via CLI
- **FR54:** Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools
- **FR55:** CLI supports multiple output formats: human-readable (default), JSON, and table
- **FR56:** CLI provides actionable error messages with recovery suggestions for common failure modes
- **FR57:** Developer can discover available actions from any system state, including empty states and error conditions
- **FR58:** MCP tools include typed parameter schemas with descriptions for LLM agent consumption

#### EventStore Integration

- **FR59:** System can auto-discover event types published to DAPR pub/sub topics
- **FR60:** System can generate dual embeddings for events (raw payload + natural language description)
- **FR61:** System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code
- **FR62:** Developer can list registered event handlers and detect handler registration mismatches

#### Trust & Transparency

- **FR63:** System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result
- **FR64:** System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit
- **FR65:** System records `ingested_by` (user or system identity) as a mandatory field on every memory unit
- **FR66:** When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded
- **FR67:** System logs search and access events per tenant for audit purposes

#### Embedding Provider Management

- **FR68:** Operator can configure embedding provider and model per tenant
- **FR69:** System enforces per-tenant rate limit ceilings for embedding API calls
- **FR70:** System tracks the embedding provider and model used for each memory unit's vectors

#### Data Portability & System Health

- **FR71:** Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. **Phase:** Phase 2 unless a later sprint change explicitly pulls export into MVP.
- **FR72:** System exposes readiness and liveness health checks verifying all backends
- **FR73:** Operator can detect index/graph divergence via consistency check
- **FR74:** Operator can repair detected index/graph inconsistencies via consistency repair operation

**Total FRs: 74**

### Non-Functional Requirements

*NFRs are tagged by validation phase: **[MVP]** = must verify before thesis validation, **[P1.5]** = verify when EventStore + MCP ship, **[Ongoing]** = validate as infrastructure matures.*

#### Performance

| NFR | Metric | Target | Conditions | Phase |
|---|---|---|---|---|
| **NFR1** | Syntactic search latency (p95) | <200ms | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| **NFR2** | Semantic search latency (p95) | <500ms | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| **NFR3** | Hybrid search latency (p95) | <1s | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| **NFR4** | Graph traversal latency (p95) | <2s | 10 concurrent queries/tenant, 10K memory units/tenant, depth ≤5 | MVP |
| **NFR5** | Ingestion throughput | >100 memory units/min (payloads ≤10KB), >10 memory units/min (payloads ≤1MB) | Per tenant, single-document embedding calls (not batched) | Ongoing |
| **NFR6** | Event indexing freshness | <5s from DAPR pub/sub publication to searchable under normal conditions; degradation documented when embedding provider is rate-limited | Per event | P1.5 |
| **NFR7** | Cold start time | Service fully operational within 60s | From containers running to accepting queries — excludes image pull time | Ongoing |

#### Security

| NFR | Requirement | Verification | Phase |
|---|---|---|---|
| **NFR8** | Zero cross-tenant data leakage — no search, ingestion, or graph traversal returns data from another tenant | Automated test suite: search, ingest, graph across all axes with malformed/empty/swapped tenant IDs. Graph-specific test: create identical graph structures in tenant A and B, traverse from tenant A, verify zero nodes from tenant B appear even if edge IDs collide | MVP |
| **NFR9** | Product services retrieve embedding-provider and other application runtime secrets exclusively through the DAPR Secrets API, backed by OpenBao in Aspire and deployed environments. Secret values are never stored in application configuration or ordinary environment variables. Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or direct pod inputs outside the DAPR secret-store boundary. | Structural dependency tests, secret scanning, AppHost topology tests, and integration tests proving DAPR reads from OpenBao without secret disclosure | Ongoing |
| **NFR10** | All inter-service communication authenticated via DAPR API tokens | DAPR configuration validation | Ongoing |
| **NFR11** | External access authenticated at ingress layer — no unauthenticated access to REST API endpoints | Integration test with unauthenticated requests | P1.5 |

#### Scalability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR12** | System supports linear scaling of tenants — adding a new tenant does not degrade existing tenant performance by more than 5% | Validated at 10 tenants, each with 100K memory units. Methodology: benchmark tenant 1 alone, add 9 loaded tenants, re-benchmark tenant 1, measure delta | Ongoing |
| **NFR13** | Per-tenant ingestion pipeline scales independently — one tenant's batch ingestion does not block another tenant's real-time ingestion | Concurrent ingestion test across 3 tenants | Ongoing |
| **NFR14** | Redis memory footprint per memory unit is predictable and documented — operator can estimate infrastructure costs before tenant provisioning | Published sizing guide: memory per unit by vector dimension and metadata size | Ongoing |
| **NFR15** | Architecture must not preclude backend migration (Redis → Qdrant) — concrete implementation with clear extraction points identified, no premature interfaces | Architecture review: extraction points documented, no tight coupling to Redis-specific APIs in domain logic | Ongoing |

#### Reliability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR16** | Zero memory unit loss during Redis restart | AOF persistence enabled and verified | MVP |
| **NFR17** | Ingestion pipeline state survives process restarts — queued and in-progress units resume without data loss | DAPR actor state persistence verified | MVP |
| **NFR18** | Partial backend failure (one of three backends down) results in degraded service, not total failure — available axes continue serving results | Chaos test: kill each backend individually, verify partial results returned | Ongoing |
| **NFR19** | Failed ingestion units are never silently dropped — all failures visible via CLI status with error details and failure stage | End-to-end test with intentional failures at each pipeline stage | Ongoing |

#### Integration

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR20** | MCP tool responses conform to MCP protocol specification — valid tool schemas, typed parameters, structured error responses | MCP protocol conformance test suite | P1.5 |
| **NFR21** | DAPR pub/sub integration handles CloudEvents envelope format — events from any DAPR-compatible publisher are processable | Integration test with standard CloudEvents payloads | P1.5 |
| **NFR22** | Embedding provider integration handles rate limiting gracefully — 429 responses trigger backoff without pipeline crash or data loss | Rate limit simulation test per provider | Ongoing |
| **NFR23** | CLI connects to the memory server via configurable endpoint — supports local dev (localhost), container (docker service name), and remote (ingress URL) environments | Configuration layering test across all three environments | Ongoing |

#### Algorithmic Quality

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR24** | Hybrid fusion uses deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0; single-axis explain still documents axis-specific score semantics | Fusion and explain unit tests with known rankings/weights | MVP |
| **NFR25** | Fusion algorithm produces deterministic scores — same query against same data produces identical composite scores. Result ordering within the same score tier may vary. | Determinism test: 100 repeated queries, zero score variance | MVP |
| **NFR26** | Benchmark suite produces reproducible results — running benchmarks twice against the same dataset yields identical NDCG@10 scores | Reproducibility test in CI | MVP |

#### Observability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR27** | Structured JSON logging with OpenTelemetry correlation IDs from DAPR trace context | Log format validation | Ongoing |
| **NFR28** | Trace context propagates across all DAPR service invocation hops — end-to-end trace from CLI/MCP through server to backend | Distributed trace completeness test | Ongoing |
| **NFR29** | Custom metrics exported via OpenTelemetry: ingestion throughput, search latency per axis, index size per tenant, pipeline queue depth | Aspire dashboard shows all metrics during local development | Ongoing |

#### Documentation Quality

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR30** | Every CLI command includes --help with at least one usage example | CLI help completeness test: parse all commands, verify example presence | MVP |
| **NFR31** | README includes working quickstart that completes in <30 minutes on a clean machine with Docker installed | Timed walkthrough on clean environment | MVP |

**Total NFRs: 31**

### Additional Requirements

#### Scope, gates, and sequencing

- The MVP is a proof-of-thesis release. It must validate three-axis retrieval while including cases and physical tenant isolation from day one.
- Shipping requires all three hard gates: hybrid retrieval wins on at least 80% of benchmark queries, zero cross-tenant leaks, and onboarding under 30 minutes. At least two of the three soft gates must also pass.
- Benchmark validity requires ground truth agreed by Jerome and two independent reviewers, NDCG@10 scoring, human dispute resolution, and at least 80% inter-rater agreement.
- Implementation must establish the buildable scaffold, test feedback, tenant provisioning, case bootstrap, and tenant/case validation before ingestion, indexing, search, or graph work writes data.
- Phase 1.5 is committed within four weeks of thesis validation. It includes EventStore/DAPR event integration, MCP, and expanded CLI capabilities; if it slips, MCP moves into MVP.
- Phase 2 includes collaboration, diffing, REST application-search support, briefings, embedding migration, and export unless later change control pulls items forward. Backend migration and advanced governance capabilities remain Phase 3.

#### Architecture and integration constraints

- The product is a .NET 10/DAPR/Aspire system using Redis/RediSearch/Redis Vector and FalkorDB. External clients enter through infrastructure-managed ingress; internal service communication uses DAPR; payloads are JSON.
- EventStore integration must consume CloudEvents through DAPR pub/sub at the Memories Server sidecar and must not require direct REST pushes or developer mapping code for domain event streams.
- The current release inventory is nine NuGet packages plus non-packable service/orchestration projects; `tools/release-packages.json` is authoritative.
- The per-tenant DAPR pipeline actor owns the durable bounded queue, throttling, ordering, progress, retry, and backpressure behavior. Document processing remains stateless.
- Ingestion completion means the memory unit is searchable across all axes. Partial multi-backend writes require explicit rollback or retry behavior that restores consistency.
- Google `text-embedding-004` is the MVP runtime provider. Provider/model configuration is tenant-scoped, and changing vector dimensions requires a full tenant reindex.
- Product services must obtain application runtime secrets exclusively from the DAPR Secrets API backed by OpenBao; ordinary configuration and environment-variable fallback for secret values is forbidden.
- Tenant context remains explicit in payloads and server validation. Physical indexes, vectors, graphs, and access checks are part of the isolation boundary.

#### Trust, compliance, and licensing constraints

- Memories is interpretive infrastructure: it owns correct embeddings, causal-chain structure, confidence semantics, complete edge graphs, ordering, edge types, and explicit gap detection; consuming applications own decisions and legal obligations.
- Relevance confidence is not factual-accuracy or completeness confidence. This distinction must appear in API documentation, CLI explain output, compliance documentation, and MCP schemas.
- Metadata confidence is separate from search relevance. Every metadata field records origin and confidence, and every memory unit records mandatory `ingested_by` provenance.
- CLI JSON, MCP, and future web surfaces must share the Contracts.V1 Evidence Packet for tenant/case scope, attribution, score breakdowns, degraded state, omitted details, and recovery guidance.
- Tenant deletion must remove the tenant's indexes, graph data, and memory units; references retained by other tenants remain the consuming application's responsibility and must be documented.
- Access telemetry is not a tamper-evident or retention-certified audit trail. Applications needing certified audit evidence must build it on top.
- Compliance documentation must include erasure, segregation, lineage, deletion limitations, auditor-facing security posture, and the stated legal disclaimer.
- Apache 2.0 is the intended license with a public non-relicensing commitment. Redis Stack managed-service restrictions and FalkorDB AGPL considerations must be explicit in deployment and dependency documentation, with pinned dependencies and identified extraction points.

#### Interface, documentation, and verification constraints

- CLI output must support human, JSON, and table modes. Configuration precedence is flags, environment, config file, DAPR secrets for secrets, then DAPR sidecar configuration.
- MCP is limited to agent-facing search, ingestion, traversal, and case information; operational tenant and diagnostic work belongs to the CLI.
- Required learning assets include three numbered samples, a 30-second README, a sub-30-minute getting-started guide, generated API reference, compliance guidance, and an operator guide.
- Unit tests must run without Docker; integration tests cover the complete ingestion/search/isolation/actor/consistency path; contract tests cover CloudEvents, service invocation, REST, and error serialization. CI runs all layers.

### PRD Completeness Assessment

The PRD is unusually comprehensive: it defines 74 numbered functional requirements and 31 measurable non-functional requirements, connects them to personas and journeys, supplies delivery phases, describes verification methods, and makes the product's core kill switches explicit.

The following clarity gaps should be resolved before implementation planning is treated as authoritative:

- Functional requirements are not individually phase-tagged except FR71. MVP, Phase 1.5, and later scope must currently be inferred from feature tables and surrounding prose.
- The success criteria cite cached response under 200 ms and cold response under 2 seconds, while NFR3 specifies hybrid p95 under 1 second. The authoritative latency contract needs one explicit mapping.
- The platform matrix says .NET 10 / C# 13, while the current repository baseline uses C# 14. The PRD technology statement is stale or underspecified.
- The ingestion table calls the three-backend write atomic, while FR13 and the failure sections anticipate rollback/retry after partial writes. The consistency model and observable completion boundary need precise semantics.
- Case membership requirements and mandatory user/system `ingested_by` provenance coexist with “per-user identity not in MVP.” The identity source and authorization semantics for MVP need clarification.
- Apache 2.0 is introduced as “recommended” but immediately treated as a public commitment. The licensing decision should be stated as final.
- The document states “nine published packages + three non-packable projects,” but the package table visibly identifies only the Server and AppHost as non-packable. The third project or the count needs correction.
- Availability, recovery objectives, deletion-completion timing, retention, and data-residency requirements are not quantified. Some may be intentionally post-MVP, but their scope should be explicit.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The explicit epics coverage map contains 74 unique identifiers, FR1 through FR74:

- Epic 0: FR26, FR38, FR44
- Epic 1: FR1, FR4-FR7, FR13, FR46, FR65, FR68
- Epic 2: FR14-FR19, FR22, FR24-FR25, FR63
- Epic 3: FR20-FR21, FR26-FR37
- Epic 4: FR47-FR52
- Epic 5: FR38-FR45, FR66, FR69-FR70
- Epic 6: FR2-FR3, FR8-FR12
- Epic 7: FR53, FR55-FR57, FR64, FR67
- Epic 8: FR72-FR74
- Epic 9: FR59-FR62
- Epic 10: FR23, FR54, FR58
- Epic 26 and the Phase 2 Data Export placeholder: FR71
- Epics 18, 20-24, and 27 reinforce previously mapped requirements.

### Coverage Matrix

| FR Number | PRD Requirement | Epic and Story Coverage | Status |
| --- | --- | --- | --- |
| FR1 | Developer can ingest content from local files into a specified case | Epic 1 — Ingest from local files; Stories 1.6 | ✓ Covered |
| FR2 | Developer can ingest content from URLs into a specified case | Epic 6 — Ingest from URLs; Story 6.1 | ✓ Covered |
| FR3 | Developer can batch-ingest content from a directory into a specified case | Epic 6 — Batch-ingest from directory; Stories 6.1 and 23.6 | ✓ Covered |
| FR4 | System can extract text from ingested content (plain text, PDF, markdown) | Epic 1 — Text extraction (Kreuzberg); Story 1.3 | ✓ Covered |
| FR5 | System can generate embeddings for ingested content via a configurable embedding provider | Epic 1 — Generate embeddings; Story 1.4 | ✓ Covered |
| FR6 | System ensures a memory unit is fully searchable across all axes after ingestion completes | Epic 1 — Memory unit fully searchable after ingestion; reinforced by Epic 23 for scalable chunking and batch embedding; Stories 1.5, 1.6, and 23.1 | ✓ Covered |
| FR7 | Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score | Epic 1 — Metadata with origin tracking; Stories 1.2 and 1.6 | ✓ Covered |
| FR8 | System manages ingestion load per tenant independently | Epic 6 — Per-tenant ingestion load management; Story 6.2 | ✓ Covered |
| FR9 | System retries failed ingestion automatically with configurable limits | Epic 6 — Auto-retry with configurable limits; Story 6.3 | ✓ Covered |
| FR10 | Developer can view ingestion status per case (queued, embedding, indexed, failed counts) | Epic 6 — Ingestion status per case; Story 6.3 | ✓ Covered |
| FR11 | Developer can view failed ingestion units with error details and failure stage | Epic 6 — Failed unit visibility; Story 6.3 | ✓ Covered |
| FR12 | Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk | Epic 6 — Re-ingestion of failed content; reinforced by Epic 23 for non-URL re-ingestion correctness; Stories 6.3 and 23.4 | ✓ Covered |
| FR13 | System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes) | Epic 1 — Partial backend write failure recovery (IngestionWorkflow saga/compensation); reinforced by Epic 21 for ratified consistency and migration safety; Stories 1.6, 21.1, and 21.2 | ✓ Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant | Epic 2 — Syntactic search; Story 2.1 | ✓ Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant | Epic 2 — Semantic search; Story 2.2 | ✓ Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant | Epic 2 — Graph search; Story 2.3 | ✓ Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes | Epic 2 — Hybrid fusion search; Story 2.5 | ✓ Covered |
| FR18 | Developer can control which axes are included in a search query | Epic 2 — Axis selection control; Story 2.5 | ✓ Covered |
| FR19 | Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode) | Epic 2 — Per-axis score breakdown (explain); Story 2.6 | ✓ Covered |
| FR20 | Developer can filter search results by case | Epic 3 — Filter search by case; Story 3.4 | ✓ Covered |
| FR21 | Developer can filter search results by metadata field values | Epic 3 — Filter search by metadata; Story 3.4 | ✓ Covered |
| FR22 | Developer can paginate search results | Epic 2 — Pagination (search concern); reinforced by Epic 22 for semantic, graph-scoped, and hybrid pagination correctness; Stories 2.6, 22.1, and 22.3 | ✓ Covered |
| FR23 | LLM Agent can constrain search response size by token budget | Epic 10 — Token budget (MCP), including deterministic omitted-detail expansion handles; Story 10.2 | ✓ Covered |
| FR24 | System returns the origin identifier (file path, URL, or event ID) and origin type for each search result | Epic 2 — Origin identifier in results; Stories 2.1, 2.2, 2.6, and 2.7 | ✓ Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output | Epic 2 — Benchmark comparisons; Story 2.8 | ✓ Covered |
| FR26 | Developer can create a case within a tenant | Epic 0 + Epic 3 — Minimal case bootstrap, then full case management; Stories 0.2 and 3.1 | ✓ Covered |
| FR27 | Developer can delete a case and all its memory units | Epic 3 — Delete case; Story 3.5 | ✓ Covered |
| FR28 | Developer can add members to a case | Epic 3 — Add case members; Story 3.3 | ✓ Covered |
| FR29 | Developer can remove members from a case | Epic 3 — Remove case members; Story 3.3 | ✓ Covered |
| FR30 | Developer can list cases within a tenant | Epic 3 — List cases; Story 3.1 | ✓ Covered |
| FR31 | Developer can view case status including memory unit count, last activity timestamp, and health indicators | Epic 3 — Case status; Story 3.2 | ✓ Covered |
| FR32 | System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion | Epic 3 — Single-case ownership; Stories 0.2 and 3.1 | ✓ Covered |
| FR33 | System maintains case-scoped graph edges between memory units within a case | Epic 3 — Case-scoped graph edges; Story 3.1 | ✓ Covered |
| FR34 | Developer can search across all cases within a tenant by keyword, returning results with case attribution | Epic 3 — Cross-case tenant search; reinforced by Epic 22 for fusion case attribution; Stories 3.4 and 22.4 | ✓ Covered |
| FR35 | Developer can delete an individual memory unit from a case | Epic 3 — Delete memory unit; Story 3.5 | ✓ Covered |
| FR36 | Developer can view recent activity within a case (ingestion events, searches, membership changes) | Epic 3 — Case activity; Story 3.2 | ✓ Covered |
| FR37 | Developer can annotate or correct a memory unit, with annotations tracked as linked memory units | Epic 3 — Annotations/corrections; Story 3.6 | ✓ Covered |
| FR38 | Operator can create a tenant with physically separate indexes | Epic 0 + Epic 5 — Tenant creation and isolated infrastructure provisioning; reinforced by Epic 24 for physical isolation strategy; Stories 0.1, 5.1, and 24.3 | ✓ Covered |
| FR39 | Operator can delete a tenant and all its indexes, graph data, and memory units | Epic 5 — Delete tenant; reinforced by Epic 21 for deletion completeness; Stories 5.2 and 21.5 | ✓ Covered |
| FR40 | Operator can verify tenant isolation via automated checks | Epic 5 — Verify tenant isolation; reinforced by Epic 24 for verifier scaling; Stories 5.3 and 24.3 | ✓ Covered |
| FR41 | Operator can list tenants | Epic 5 — List tenants; Story 5.5 | ✓ Covered |
| FR42 | Operator can update tenant configuration after creation (rate limits, display name, settings) | Epic 5 — Update tenant config; Story 5.5 | ✓ Covered |
| FR43 | System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment | Epic 5 — Prevent inconsistent config changes; Story 5.5 | ✓ Covered |
| FR44 | System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages | Epic 0 + Epic 5 — Tenant context validation and enforcement; reinforced by Epic 20 for authorization and Epic 24 for physical isolation; Stories 0.3, 5.4, 20.2, and 24.3 | ✓ Covered |
| FR45 | Operator can view current configuration of a tenant (embedding provider, rate limits, index status) | Epic 5 — View tenant configuration; Story 5.5 | ✓ Covered |
| FR46 | System can index CausationId and CorrelationId from events as typed, directional graph edges | Epic 1 — Index CausationId/CorrelationId as graph edges (creation during ingestion); Stories 1.5 and 9.2 | ✓ Covered |
| FR47 | Developer can traverse causal chains from a starting node with configurable depth | Epic 4 — Traverse causal chains; Story 4.1 | ✓ Covered |
| FR48 | Developer can filter graph traversal by edge type | Epic 4 — Filter by edge type; Story 4.2 | ✓ Covered |
| FR49 | When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier | Epic 4 — Gap markers for missing nodes; Story 4.3 | ✓ Covered |
| FR50 | System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence | Epic 4 — Edge type taxonomy; Story 4.2 | ✓ Covered |
| FR51 | Developer can promote AI-inferred edge confidence when verifying a relationship | Epic 4 — Promote AI-inferred confidence; Story 4.3 | ✓ Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes | Epic 4 — Chronological ordering; Story 4.1 | ✓ Covered |
| FR53 | Developer can interact with all retrieval and ingestion capabilities via CLI | Epic 7 — CLI for all capabilities; Story 7.1 | ✓ Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools | Epic 10 — MCP tools; Story 10.1 | ✓ Covered |
| FR55 | CLI supports multiple output formats: human-readable (default), JSON, and table | Epic 7 — CLI output formats; Story 7.2 | ✓ Covered |
| FR56 | CLI provides actionable error messages with recovery suggestions for common failure modes | Epic 7 — Actionable CLI errors; Story 7.3 | ✓ Covered |
| FR57 | Developer can discover available actions from any system state, including empty states and error conditions | Epic 7 — Discoverable actions; Stories 7.3 and 7.4 | ✓ Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions for LLM agent consumption | Epic 10 — MCP typed schemas; Story 10.1 | ✓ Covered |
| FR59 | System can auto-discover event types published to DAPR pub/sub topics | Epic 9 — Auto-discover event types; Stories 9.1 and 18.8 | ✓ Covered |
| FR60 | System can generate dual embeddings for events (raw payload + natural language description) | Epic 9 — Dual embeddings for events; Story 9.2 | ✓ Covered |
| FR61 | System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code | Epic 9 — Auto-index CausationId/CorrelationId; Stories 9.2 and 18.8 | ✓ Covered |
| FR62 | Developer can list registered event handlers and detect handler registration mismatches | Epic 9 — Handler registration management; Stories 9.3 and 16.1 | ✓ Covered |
| FR63 | System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result | Epic 2 — Composite confidence scores and Evidence Packet contract mapping; Stories 2.6 and 2.7 | ✓ Covered |
| FR64 | System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit | Epic 7 — Metadata origin tracking display; Stories 1.2 and 7.2 | ✓ Covered |
| FR65 | System records `ingested_by` (user or system identity) as a mandatory field on every memory unit | Epic 1 — `ingested_by` field; Stories 1.2 and 1.6 | ✓ Covered |
| FR66 | When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded | Epic 5 — Partial results on backend failure; Story 5.6 | ✓ Covered |
| FR67 | System logs search and access events per tenant for audit purposes | Epic 7 — Search/access telemetry; reinforced by Epic 20 for audit emission. A41 access-telemetry retention remains governed by `20.5-A41-ACCESS-TELEMETRY-RETENTION`.; Stories 7.5, 20.5, and Epic 27 lifecycle stories | ✓ Covered |
| FR68 | Operator can configure embedding provider and model per tenant | Epic 1 — Configure Google embedding provider for MVP with an extensible provider/model/dimensions/rate-limit shape. OpenAI, Mistral, Ollama, and custom runtime providers are post-MVP provider expansion work unless explicitly pulled forward by sprint change.; Stories 1.7 and Epic 13 provider stories | ✓ Covered |
| FR69 | System enforces per-tenant rate limit ceilings for embedding API calls | Epic 5 — Per-tenant rate limits; Stories 5.5 and 6.2 | ✓ Covered |
| FR70 | System tracks the embedding provider and model used for each memory unit's vectors | Epic 5 — Track embedding model per unit; Story 5.5 | ✓ Covered |
| FR71 | Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. **Phase:** Phase 2 unless a later sprint change explicitly pulls export into MVP. | Epic 26 — Portable export reinforced through backup/restore and operational readiness; broader application-facing export remains Phase 2 unless explicitly pulled forward; Phase 2 Data Export placeholder; Story 26.2 covers backup/restore only | ⚠ Deferred |
| FR72 | System exposes readiness and liveness health checks verifying all backends | Epic 8 — Health checks; Story 8.1 | ✓ Covered |
| FR73 | Operator can detect index/graph divergence via consistency check | Epic 8 — Consistency check; Story 8.2 | ✓ Covered |
| FR74 | Operator can repair detected index/graph inconsistencies via consistency repair operation | Epic 8 — Consistency repair; Story 8.2 | ✓ Covered |

### Missing Requirements

No PRD functional requirement is absent from the epics document, and no FR identifier appears in the explicit coverage map that is absent from the PRD.

FR71 is not missing, but it is not implementation-ready as an active product story. The full portable export requirement is held in an unregistered Phase 2 placeholder; Story 26.2 covers backup/restore and disaster-recovery fidelity rather than the complete application-facing case/tenant export contract. Before FR71 is selected, the activation rule requires a normal story file and sprint-status registration.

Coverage should not be confused with active MVP scope: Epics 9-10 are Phase 1.5, Epic 17 is future web work, and later remediation/operations epics require explicit sprint selection.

### Coverage Statistics

- Total PRD FRs: 74
- Unique FR identifiers in the epics coverage map: 74
- FRs captured in an epic, story, or governed deferred placeholder: 74
- Missing FRs: 0
- Extra epic-map FR identifiers not present in the PRD: 0
- Deferred placeholder requirements: 1 (FR71)
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

The canonical whole UX specification, `ux-design-specification.md`, was found and reviewed in full (1,164 lines). It is a complete, full-horizon interaction specification rather than an MVP scope declaration: CLI is the MVP reference surface, MCP/EventStore follow in Phase 1.5, and the FrontComposer/Fluent UI web surface remains future work unless explicitly pulled forward. One sprint-change proposal that matched the UX filename pattern was retained as supplemental material and excluded from the primary document set.

### UX-to-PRD Alignment

The UX specification is strongly aligned with the PRD:

- The same primary users and jobs are represented: developers, LLM agents, operators, technical leads, tenant-scoped teams, and case-scoped collaborators.
- The defining UX object—the Evidence Packet—directly composes the PRD's retrieval results, origin identifiers, confidence and per-axis explanation, tenant/case scope, freshness, graph relationships, omitted details, degraded-axis disclosure, and recovery guidance.
- The UX state grammar distinguishes complete, partial, weak, empty, stale, degraded, unauthorized, and pending states, which supports FR56-FR57 and FR63-FR67 without hiding failure or absence behind a generic no-result treatment.
- CLI, MCP, and web are correctly treated as surface-specific presentations of shared semantics rather than as feature-identical interfaces. The UX document explicitly preserves the PRD phase split.
- Ingestion lifecycle, case activity, tenant verification, graph traversal, MCP token budgeting, source inspection, and operator health journeys map to requirements and epic ownership.
- WCAG 2.2 AA, keyboard access, screen-reader semantics, reduced motion, forced-colors support, responsive layouts, and touch-target guidance add testable quality constraints for the future web surface without changing product scope.

The following UX-to-PRD clarifications are still needed before their affected work is selected:

| Severity | Alignment issue | Implementation risk | Required clarification |
| --- | --- | --- | --- |
| High for answer-generation work; non-blocking for retrieval-only MVP | The UX core loop permits a "synthesized answer, or both," while PRD FR14-FR25 specify search and retrieval results rather than an owned answer-generation capability. | Teams could independently introduce an LLM answer path, with inconsistent citation, abstention, tenant-scope, and failure semantics. | Either declare synthesized answers outside current scope and constrain `result` to ranked-result summaries, or add a phased requirement with owner, input/output contract, citation and abstention rules, authorization behavior, and acceptance criteria. |
| Medium | The PRD requires numeric composite scores and per-axis breakdowns; the UX prefers human-readable evidence-strength states and cautions rather than raw percentages as the primary signal. | CLI, MCP, and web could map the same score to different trust states, especially because hybrid RRF contributions and single-axis scores have different semantics. | Define one contract-owned mapping from axis/composite score semantics to Evidence Packet strength/state labels, including thresholds, unavailable axes, stale data, and degraded results. Numeric detail should remain inspectable. |
| Medium for future web | UX actions include verify, retry, repair, inspect, request permission, expand budget, and export. Their availability spans MVP, Phase 1.5, future web, and deferred FR71 work. | A composed UI could expose unavailable, unauthorized, or semantically ambiguous actions; "export packet" may be confused with portable tenant/case export. | Add capability, authorization, phase, confirmation, and disabled-state metadata to recovery-action descriptors. Distinguish Evidence Packet export from FR71 portable data export. |
| Low for current MVP; medium for future web | The UX specifies responsive behavior but no measurable UI interaction or rendering budgets. PRD latency NFRs measure backend/search behavior. | A technically correct Evidence Cockpit, graph view, or large data grid could still feel slow or become inaccessible under load. | Before Epic 17 implementation, define targets for initial/updated render, interaction response, live-status latency, large-result virtualization, and graph expansion, with representative device and dataset bounds. |

### UX-to-Architecture Alignment

Architecture and UX agree on the main implementation invariants:

- `Contracts.V1` owns a shared Evidence Packet grammar used by CLI JSON, MCP responses, and future web composition.
- The architecture's packet fields cover scope, result, sources, evidence, graph context, state, omitted details, and recovery actions.
- The future `Hexalith.Memories.Web` surface is explicitly constrained to Hexalith.FrontComposer composition primitives and Microsoft Fluent UI Blazor V5, with custom markup/CSS limited to justified gaps and guarded by conformance tests.
- Tenant/case scope is carried as a trust boundary, while authorization and physical isolation remain enforced server-side rather than entrusted to presentation logic.
- Partial-backend behavior, workflow lifecycle state, graph gap markers, source provenance, freshness, token-budget omission, and deterministic expansion handles support the UX recovery and inspection patterns.
- The phased interface topology matches the UX: CLI-first, MCP fast-follow, and web/RCL future work.

Architecture support remains incomplete in these areas:

| Severity | Architecture gap | Implementation risk | Required architecture work |
| --- | --- | --- | --- |
| High planning/architecture drift; non-blocking for CLI MVP | `Hexalith.Memories.Web` is named in Interface Philosophy but is absent from the complete project tree, build order, dependency diagram, service boundary table, and test topology. The implementation registry nevertheless marks Epic 17 and its stories `done`. | The completed web RCL has no canonical architectural placement, package boundary, dependency map, or test topology, so later extension or release claims may follow implementation history instead of architecture. | Reconcile the architecture to the implemented Web RCL and tests: define package/publication status, dependencies on Contracts/FrontComposer/Fluent UI V5, allowed server/client boundaries, and host/composition ownership. |
| High for answer-generation work | The architecture allows `result` to contain an answer summary and describes AI enrichment/tool-calling, but no component owns search-result synthesis. | Implementers may reuse the ingestion-enrichment agent for an ungoverned retrieval-time answer path or omit synthesis despite UX expectations. | Resolve the same scope decision as above, then model the owning service/workflow, Evidence Packet production path, timeout/degradation behavior, and trust controls if synthesis is retained. |
| Medium for Epic 17 | FrontComposer/Fluent constraints are explicit, but render mode, contract-to-descriptor mapping, state/command lifecycle, streaming or polling behavior, navigation/scope persistence, and server/client trust boundaries are not architected. | UX components may become tightly coupled to transport or duplicate command/evidence state across surfaces. | Add a web composition view of the architecture before Epic 17 selection, including typed descriptor ownership, Fluxor-compatible state expectations, command lifecycle, render-mode constraints, and transport adapters. |
| Medium | The architecture has backend latency budgets and general conformance tests but no web performance, responsive, accessibility, or browser-validation architecture. | UX quality constraints may remain prose-only and regress without enforceable evidence. | Map WCAG 2.2 AA and responsive requirements to test projects and CI lanes; define supported browsers/viewports, automated and manual evidence, keyboard/screen-reader checks, forced-colors/reduced-motion checks, and performance budgets. |

### Alignment Warnings

- The UX document exists and is substantive; there is no missing-UX warning.
- The unresolved web architecture items do **not** block the current CLI-first MVP or Phase 1.5 MCP work. Because `sprint-status.yaml` marks Epic 17 and its stories `done` while the UX, architecture, and epics planning prose still call the surface future work, this is now post-implementation planning drift. Reconcile it before reopening, extending, publishing, or making release-readiness claims for the web surface.
- Answer synthesis is the only cross-document ambiguity that can affect the shared Evidence Packet contract before web implementation. It should be resolved before any story claims a generated narrative or answer rather than a ranked-result summary.
- Evidence-state thresholds and recovery-action capability metadata should be contract-level decisions so every surface presents the same trust semantics.

## Epic Quality Review

### Review Scope and Baseline

The review covered all 32 numbered epics (Epic 0 through Epic 31), all 165 numbered story headings, the six unkeyed Phase 2 placeholders embedded after Story 8.5, and the authoritative `story_execution_order` overrides in `sprint-status.yaml`.

- All 165 numbered stories contain an `As a/As an`, `I want`, and `So that` narrative.
- 164 stories contain at least one Given/When/Then scenario. Story 27.3 instead uses eight numbered criteria.
- 47 stories contain only one Given scenario; this is not automatically defective, but many of the post-MVP remediation stories compress several behaviors and failure modes into that one scenario.
- PRD traceability is complete at the FR identifier level: 74/74 FRs are mapped, with FR71 governed as deferred application-facing export work.
- Epic 0 through Epic 8 are the declared active MVP readiness boundary; later epics are fast-follow, future UI, remediation, or operational tracks and require explicit selection.

### 🔴 Critical Violations

#### C1. Epic 17 has a circular execution contract

`story_execution_order.epic-17` requires Story 17.6 and Story 17.7 to execute before Stories 17.2-17.5. Story 17.6, however, contains an acceptance criterion beginning "Given Stories 17.2 through 17.5 are implemented." A clean execution cannot satisfy both rules: the conformance gate must precede work whose completed implementation it requires.

**Remediation:** Split Story 17.6 into an earlier conformance-foundation story and a later conformance-audit story, or rewrite its acceptance criteria so the preflight establishes reusable rules without requiring future stories. Preserve completed history through aliases, but make the canonical dependency graph acyclic.

#### C2. Epic 27 close-out depends on withdrawn, unregistered future stories

Story 27.4 requires Stories 27.5 and 27.6 to exist, be registered, and be `done`, but those stories were explicitly withdrawn. All 25 C1 gates are held without a registered story owner. `sprint-status.yaml` lists Story 27.4 after Story 27.3 while acknowledging that the missing successors must be created later. This violates story independence and makes Epic 27 impossible to complete from the registered backlog.

**Remediation:** Register properly sized successor stories with real evidence producers and explicit ownership before keeping Story 27.4 as selectable, or move Story 27.4 to a governed placeholder and keep it out of the executable story sequence until its predecessors exist.

#### C3. Story 27.3 is not a completable story contract

Story 27.3 bundles manifest qualification, adapter unit-contract qualification, and a deployment-verification lane. Its eight criteria span approximately 1,400 words; Criteria 1-5 explicitly define evidence that Story 27.3 "cannot discharge," and Criterion 6 is a large multi-stage process with an admitted manifest/runtime substitution gap. The story is also marked `in-progress` while its C0 review-readiness gate is blocked on predecessor gaps. Acceptance criteria that the story cannot satisfy cannot define completion.

**Remediation:** Remove held C1 definitions from Story 27.3 acceptance criteria, retain them in a non-story gate registry, and split C0, C2, C3, and C4 into independently owned/completable slices. Every retained story criterion must be within that story's mutation authority and have a re-runnable producer.

#### C4. Several records are technical/process initiatives presented as product epics

The strict create-epics standard rejects technical milestones that do not deliver standalone user value. The clearest violations are Epic 11 (CI/CD pipeline), Epic 14 (deferred-work hardening), Epic 15 (carry-forward risk closure), Epic 19 (deferred-register triage), Epic 25 (architecture factorization explicitly "without changing product behavior"), Epic 28 (dependency identity adoption), and Epic 30 (CI/CD ownership/alignment). Epic 12 mixes a real release outcome with retrospective process enforcement and is borderline.

**Remediation:** Track pure engineering/process work as enablers, risks, release objectives, or operational initiatives outside the product-epic sequence. Where an epic is retained, rewrite it around a concrete beneficiary-observable outcome and ensure each story produces independently consumable value or evidence.

### 🟠 Major Issues

#### M1. Numeric story order is not dependency order

The registry has to override numeric order in four epics:

- Epic 17: 17.6 → 17.7 → 17.2 → 17.3 → 17.4 → 17.5.
- Epic 18: 18.6 → 18.5 because the lookup endpoint consumes the stability contract.
- Epic 23: 23.9 → 23.1 because chunking consumes the provider batch API.
- Epic 30: 30.2 → 30.1 because guarded release dispatch requires the shared CI contract.

The overrides preserve historical keys, but they violate the workflow rule that later-numbered stories must not be prerequisites for earlier-numbered stories and make naive story selection unsafe.

**Remediation:** Use explicit predecessor metadata in every affected story file and make tooling reject numeric selection that ignores it. For any new or reopened work, assign keys in executable order; do not create another override.

#### M2. Multiple stories are acknowledged or observable bundles rather than independent slices

The document itself marks Stories 1.2, 1.5, 1.6, and 8.5 as historical broad/bundled stories that must not be reopened as single units. Additional oversized or multi-outcome stories include:

- Story 17.4: five independent inspection lenses.
- Story 17.5: responsive layout plus automated and human accessibility validation across multiple modalities.
- Story 17.7: runnable specimen, Playwright/axe, media/layout validation, manual assistive-technology evidence, and artifact redaction.
- Story 21.9: staging migration, atomic cutover, rollback, marker locking/heartbeat, and abort behavior in one criterion.
- Story 22.7: NL-axis activation, weight tuning, highlighting, and reranker seam.
- Story 26.5: six separately useful runbooks.
- Story 27.4: deployment evidence, operations documentation, and A41 governance close-out.

**Remediation:** Split these into vertical stories whose result can be demonstrated and accepted independently. Use a parent initiative/checklist only for aggregation, not as the unit of implementation.

#### M3. Twenty-six stories use completion as the acceptance-criterion trigger

The phrase `When this/the story completes` appears in Stories 15.1, 15.3, 16.1, 18.1-18.3, 21.1, 21.4, 21.10, 22.7, 23.9, 24.3-24.4, 25.1-25.8, and 26.1-26.5. This is circular: completion cannot be both the trigger and the outcome being tested.

**Remediation:** Replace each with an observable operation or event—request, migration run, deployment render, test execution, failure injection, release attempt, or operator inspection—and state independently verifiable results and error behavior.

#### M4. Acceptance criteria are compressed or non-measurable in several stories

- Story 2.8 requires completion in a "reasonable time" without a bound.
- Story 14.1 permits test selection "as appropriate."
- Story 18.5 exposes the lookup through MCP/CLI "as appropriate."
- Story 26.1 asks for deployment manifests with "real config" without enumerating the production profile, validation command, or pass threshold.
- Story 27.3 is the only numbered story without Given/When/Then criteria and mixes owned and explicitly unowned gates.
- Story 28.1 has detailed BDD scenarios but omits the standard Acceptance Criteria heading.

The 47 one-scenario stories are concentrated in Epics 20-26 and 30. Several of those scenarios cover multiple independent success, failure, rollback, security, and evidence behaviors, which makes partial completion difficult to detect.

**Remediation:** Add measurable bounds and named evidence, separate success/failure/rollback cases, and avoid optional wording unless the condition selecting an alternative is itself testable.

#### M5. Six Phase 2 requirements are embedded as unkeyed pseudo-stories

Data Export, Recency-Aware Ranking, Ingestion Distillation, Context-Prepended Chunk Embedding, Reranker Activation, and Scope Bundles appear after Story 8.5 with user-story prose but without numbered story ownership. Only Data Export has full acceptance criteria. Their placement also makes mechanical parsing treat them as part of Story 8.5.

**Remediation:** Move them to a dedicated governed backlog section that cannot be parsed as story content. On activation, create normal numbered stories and sprint-status records before implementation, as the document's own activation rules require.

#### M6. FR71/backup semantics remain ambiguous

Story 26.2 assumes an export format and implements restore/fidelity evidence, while the primary epics map still holds application-facing portable export (FR71) as an unregistered Phase 2 placeholder. The implementation registry marks Story 26.2 done, but the planning documents do not clearly distinguish the operational backup export contract it consumed from the broader case/tenant portable export feature.

**Remediation:** Name the operational backup artifact and schema separately, define whether it is a supported product contract, and state exactly which FR71 clauses remain deferred.

#### M7. Some backlog stories are intentionally not selectable yet

Stories 30.3 and 30.4 depend on capabilities from a future owner-approved Hexalith.Builds revision. Story 31.2 is `ready-for-dev` but cannot enter implementation until specific Story 31.1 checkpoints complete. These gates are explicit and fail-closed, but the status vocabulary can mislead selectors that do not evaluate activation conditions.

**Remediation:** Use a `blocked`/`gated` readiness state distinct from `backlog` or `ready-for-dev`, with machine-readable predecessor and external-capability conditions.

### 🟡 Minor Concerns

- Epic summaries are duplicated in an Epic List and again in detailed sections; the two copies can drift, as demonstrated by future/completed web status divergence.
- Historical aliases and reserved gaps (Story 0.0/1.1, Story 2.7/2.6A, reserved Story 8.3) are documented but add tooling and selection risk.
- Story 28.1 should add the standard Acceptance Criteria heading even though its scenarios are otherwise specific and testable.
- The canonical UX, architecture, and epics prose label Epic 17 as future work, while the implementation registry marks Epic 17 and all its stories `done`. Phase labels must describe current state or explicitly distinguish original planned phase from completion status.

### Starter, Data-Timing, and Brownfield Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Starter/template story | Pass | Architecture selects the empty Aspire starter plus incremental projects; Story 0.0 owns scaffolding, dependency/config setup, submodule validation, and single-command boot. |
| Early CI for a greenfield/restarted sequence | Pass | Story 0.4 is a hard prerequisite for Epic 1 data-writing work and provides build plus Docker-free Tier-1 checks. |
| Entity/storage creation timing | Pass with sizing caveat | Tenant infrastructure is provisioned when first needed in Story 0.1; ingestion/indexing do not create it on demand. No separate "create every table" story exists. Stories 1.2 and 1.5 are nevertheless acknowledged broad contract/index bundles. |
| Brownfield integration handling | Pass | EventStore, FrontComposer, downstream consumer, shared CI, OpenBao, and runtime-adoption work name integration boundaries, compatibility constraints, and activation gates. |

### Best-Practices Compliance Summary

| Criterion | Result | Assessment |
| --- | --- | --- |
| Epics deliver user/beneficiary value | Partial | Product and operator epics generally do; seven pure technical/process epics do not meet the strict standard. |
| Epics can function using only prior outputs | Fail | Epic 17 is circular; Epic 27 requires withdrawn future stories; several epics need order overrides. |
| Stories are appropriately sized | Fail | Historical bundles are acknowledged and additional multi-outcome stories remain. |
| No forward dependencies | Fail | Epic 17, Epic 27, and numeric order overrides in Epics 18, 23, and 30 violate the rule. |
| Storage created when first needed | Pass | Tenant infrastructure has one early lifecycle owner; no wholesale database bootstrap epic exists. |
| Clear, testable, complete acceptance criteria | Partial/Fail | Most stories use BDD, but completion-trigger, vague-bound, one-scenario bundles, and Story 27.3 defects remain. |
| FR traceability maintained | Pass with scope caveat | 74/74 FRs are traceable; FR71 remains deferred beyond operational backup/restore. |
| Greenfield setup and CI foundations | Pass | Stories 0.0 and 0.4 satisfy the architecture starter and early quality-gate requirements. |

### Epic Quality Verdict

The active MVP epic sequence is substantially better structured than the later lifecycle backlog and is protected by explicit scope and CI gates. However, the complete epics specification does **not** pass strict quality validation. Epic 17's circular contract and Epic 27's ownerless future dependencies are blocking structural defects; Story 27.3 is not completable as written; and technical/process initiatives, bundled stories, order overrides, and circular completion criteria require correction before the affected work is treated as implementation-ready.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY** for implementation from the complete canonical backlog as written.

The narrower active MVP boundary (Epics 0-8) is substantially stronger and has complete FR traceability, an explicit starter story, early CI, tenant-first storage ownership, and measurable product gates. It can be treated as **conditionally usable** only where selected work does not depend on the unresolved cross-document contracts below. This narrower conclusion does not make the complete planning set ready.

### Assessment Summary

| Area | Positive evidence | Readiness concern |
| --- | --- | --- |
| Artifact set | Canonical PRD, architecture, epics, and UX documents all exist as whole documents; no shard/whole conflicts were found. | Planning prose and the implementation registry disagree about whether Epic 17 is future or complete. |
| Requirements | 74 FRs and 31 NFRs were extracted; 74/74 FR identifiers are traceable to epics/stories or a governed deferred placeholder. | Eight PRD clarity gaps remain, including phase attribution, latency, platform version, consistency, identity, package count, licensing, and unquantified operational objectives. |
| UX and architecture | Shared Evidence Packet, scope-first trust, degradation, recovery, FrontComposer, and Fluent UI V5 are strongly aligned. | Answer synthesis has no owned requirement/architecture path; evidence-state thresholds, recovery-action capability metadata, web architecture, accessibility evidence, and UI performance budgets are incomplete or stale. |
| Epics and stories | All 165 numbered stories have user narratives; 164 use BDD scenarios; the active MVP sequence has clear user/operator outcomes. | Four critical and seven major quality issue groups remain, including circular/forward dependencies, ownerless gates, non-completable Story 27.3, technical epics, bundled stories, and weak completion-trigger criteria. |

This report records **31 issue groups across four categories**: eight PRD clarity gaps, eight UX/architecture alignment gaps, eleven critical/major epic-quality violations, and four minor structural concerns. Some findings overlap by design where the same unresolved contract affects more than one artifact.

### Critical Issues Requiring Immediate Action

1. **Break the Epic 17 dependency cycle.** Story 17.6 cannot both precede Stories 17.2-17.5 and require them to be implemented. Reconcile the dependency model and the stale future-vs-done planning state.
2. **Restore executable ownership in Epic 27.** Register properly authored successor stories for C1.1-C1.25 or remove Story 27.4 from the executable backlog. No implementation-ready story may depend on withdrawn placeholders.
3. **Rewrite or split Story 27.3.** Its completion criteria must contain only work it owns and can discharge; held C1 definitions belong in a gate registry, not its acceptance criteria.
4. **Resolve the shared Evidence Packet contract.** Decide whether synthesized answers are in scope. Define canonical evidence-strength/state mapping and capability/authorization metadata for recovery actions before any new surface extends the contract.
5. **Reconcile canonical planning facts.** Phase-tag requirements, make latency and consistency semantics authoritative, update .NET/C# facts, clarify MVP identity, correct package counts, finalize licensing wording, and distinguish operational backup artifacts from FR71 portable export.

### Recommended Next Steps

1. Run a focused course correction for Epic 17 and Epic 27 that produces an acyclic, fully owned dependency graph and updates both `epics.md` and `sprint-status.yaml` together.
2. Update the PRD with per-requirement phase metadata and resolve the eight recorded completeness gaps; then validate the PRD again.
3. Update architecture with the implemented Web RCL/project/test topology and either add or explicitly reject retrieval-time synthesis. Preserve FrontComposer/Fluent UI V5 and tenant-isolation invariants.
4. Refactor future selectable work: split bundled stories, convert technical/process epics to governed enablers or outcome-based initiatives, and replace all `When this story completes` criteria with observable triggers and measurable evidence.
5. Promote the six Phase 2 pseudo-stories into a separate governed backlog section; create numbered story/status records only when activated.
6. Re-run implementation readiness after the corrected documents are synchronized. Do not infer readiness from 100% FR coverage alone.

### Final Note

The artifacts are unusually detailed and the active MVP traceability is strong, but detail does not compensate for an impossible dependency graph or acceptance criteria outside a story's authority. Address the critical issues before selecting affected implementation work. Proceeding as-is is reasonable only for an explicitly bounded story whose prerequisites, phase, ownership, and contract are already unambiguous.

**Assessment date:** 2026-08-02  
**Assessor:** Codex, executing the BMad Implementation Readiness workflow
