---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
inputDocuments:
  prd:
    - _bmad-output/planning-artifacts/prd.md
  architecture:
    - _bmad-output/planning-artifacts/architecture.md
  epics:
    - _bmad-output/planning-artifacts/epics.md
  ux:
    - _bmad-output/planning-artifacts/ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-02
**Project:** memories

## Document Inventory

### PRD Files Found

**Whole Documents:**

- `prd.md` (88,353 bytes, modified 2026-07-19 16:20 CEST) — selected for assessment

**Sharded Documents:** None.

### Architecture Files Found

**Whole Documents:**

- `architecture.md` (121,376 bytes, modified 2026-08-02 11:06 CEST) — selected for assessment

**Sharded Documents:** None.

**Supplemental keyword matches:**

- `sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md` (10,985 bytes, modified 2026-07-16 12:44 CEST)
- `sprint-change-proposal-2026-07-28-architecture-anchor-reverification.md` (9,327 bytes, modified 2026-07-28 20:14 CEST)

These sprint-change proposals are not duplicate canonical architecture documents.

### Epics and Stories Files Found

**Whole Documents:**

- `epics.md` (382,956 bytes, modified 2026-08-02 19:58 CEST) — selected for assessment

**Sharded Documents:** None.

**Supplemental keyword matches:**

- `sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md` (9,747 bytes, modified 2026-06-02 17:54 CEST)
- `sprint-change-proposal-2026-07-06-epic-0-evidence-map.md` (4,623 bytes, modified 2026-07-06 18:09 CEST)
- `sprint-change-proposal-2026-07-06-epic-17-deferred-web-triage.md` (4,978 bytes, modified 2026-07-06 18:15 CEST)
- `sprint-change-proposal-2026-07-06-epic17-browser-at-gap-closure.md` (12,987 bytes, modified 2026-07-06 18:10 CEST)
- `sprint-change-proposal-2026-07-16-epic-0-evidence-map-maintenance.md` (8,531 bytes, modified 2026-07-16 10:16 CEST)
- `sprint-change-proposal-2026-07-16-epic-26-benchmark-closure.md` (17,490 bytes, modified 2026-07-16 12:55 CEST)
- `sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md` (24,777 bytes, modified 2026-07-27 08:14 CEST)
- `sprint-change-proposal-2026-07-28-epic-ac-code-verification.md` (21,436 bytes, modified 2026-07-28 16:01 CEST)
- `sprint-change-proposal-2026-07-28-epic-ac-verification-route-and-binding-coverage.md` (40,125 bytes, modified 2026-07-28 20:18 CEST)
- `sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md` (29,405 bytes, modified 2026-08-02 19:57 CEST)
- `sprint-change-proposal-2026-08-02-epic-23-documentation-verification.md` (27,427 bytes, modified 2026-08-02 18:54 CEST)

These sprint-change proposals are not duplicate canonical epics-and-stories documents.

### UX Design Files Found

**Whole Documents:**

- `ux-design-specification.md` (99,240 bytes, modified 2026-06-27 08:02 CEST) — selected for assessment

**Sharded Documents:** None.

**Supplemental keyword matches:**

- `sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` (17,841 bytes, modified 2026-06-27 08:08 CEST)

This sprint-change proposal is not a duplicate canonical UX document.

### Discovery Resolution

- All four required document types are present.
- No whole-versus-sharded duplicate formats were found.
- The canonical PRD, architecture, epics, and UX documents listed in frontmatter were confirmed for assessment.
- The pre-existing `implementation-readiness-report-2026-08-02.md` was preserved; this run uses the collision-safe `implementation-readiness-report-2026-08-02-rerun.md` output.

## PRD Analysis

### Functional Requirements

FR1: Developer can ingest content from local files into a specified case.

FR2: Developer can ingest content from URLs into a specified case.

FR3: Developer can batch-ingest content from a directory into a specified case.

FR4: System can extract text from ingested content (plain text, PDF, markdown).

FR5: System can generate embeddings for ingested content via a configurable embedding provider.

FR6: System ensures a memory unit is fully searchable across all axes after ingestion completes.

FR7: Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score.

FR8: System manages ingestion load per tenant independently.

FR9: System retries failed ingestion automatically with configurable limits.

FR10: Developer can view ingestion status per case (queued, embedding, indexed, failed counts).

FR11: Developer can view failed ingestion units with error details and failure stage.

FR12: Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk.

FR13: System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes).

FR14: Developer can search memory units by syntactic matching within a tenant.

FR15: Developer can search memory units by semantic similarity within a tenant.

FR16: Developer can search memory units by graph traversal within a tenant.

FR17: Developer can search memory units by hybrid fusion combining all available axes.

FR18: Developer can control which axes are included in a search query.

FR19: Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode).

FR20: Developer can filter search results by case.

FR21: Developer can filter search results by metadata field values.

FR22: Developer can paginate search results.

FR23: LLM Agent can constrain search response size by token budget.

FR24: System returns the origin identifier (file path, URL, or event ID) and origin type for each search result.

FR25: Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output.

FR26: Developer can create a case within a tenant.

FR27: Developer can delete a case and all its memory units.

FR28: Developer can add members to a case.

FR29: Developer can remove members from a case.

FR30: Developer can list cases within a tenant.

FR31: Developer can view case status including memory unit count, last activity timestamp, and health indicators.

FR32: System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion.

FR33: System maintains case-scoped graph edges between memory units within a case.

FR34: Developer can search across all cases within a tenant by keyword, returning results with case attribution.

FR35: Developer can delete an individual memory unit from a case.

FR36: Developer can view recent activity within a case (ingestion events, searches, membership changes).

FR37: Developer can annotate or correct a memory unit, with annotations tracked as linked memory units.

FR38: Operator can create a tenant with physically separate indexes.

FR39: Operator can delete a tenant and all its indexes, graph data, and memory units.

FR40: Operator can verify tenant isolation via automated checks.

FR41: Operator can list tenants.

FR42: Operator can update tenant configuration after creation (rate limits, display name, settings).

FR43: System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment.

FR44: System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages.

FR45: Operator can view current configuration of a tenant (embedding provider, rate limits, index status).

FR46: System can index CausationId and CorrelationId from events as typed, directional graph edges.

FR47: Developer can traverse causal chains from a starting node with configurable depth.

FR48: Developer can filter graph traversal by edge type.

FR49: When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier.

FR50: System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence.

FR51: Developer can promote AI-inferred edge confidence when verifying a relationship.

FR52: System maintains chronological ordering and timestamps on causal chain nodes.

FR53: Developer can interact with all retrieval and ingestion capabilities via CLI.

FR54: Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools.

FR55: CLI supports multiple output formats: human-readable (default), JSON, and table.

FR56: CLI provides actionable error messages with recovery suggestions for common failure modes.

FR57: Developer can discover available actions from any system state, including empty states and error conditions.

FR58: MCP tools include typed parameter schemas with descriptions for LLM agent consumption.

FR59: System can auto-discover event types published to DAPR pub/sub topics.

FR60: System can generate dual embeddings for events (raw payload + natural language description).

FR61: System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code.

FR62: Developer can list registered event handlers and detect handler registration mismatches.

FR63: System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result.

FR64: System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit.

FR65: System records `ingested_by` (user or system identity) as a mandatory field on every memory unit.

FR66: When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded.

FR67: System logs search and access events per tenant for audit purposes.

FR68: Operator can configure embedding provider and model per tenant.

FR69: System enforces per-tenant rate limit ceilings for embedding API calls.

FR70: System tracks the embedding provider and model used for each memory unit's vectors.

FR71: Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. **Phase:** Phase 2 unless a later sprint change explicitly pulls export into MVP.

FR72: System exposes readiness and liveness health checks verifying all backends.

FR73: Operator can detect index/graph divergence via consistency check.

FR74: Operator can repair detected index/graph inconsistencies via consistency repair operation.

**Total FRs: 74**

### Non-Functional Requirements

NFR1: Syntactic search latency (p95) must be less than 200ms at 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR2: Semantic search latency (p95) must be less than 500ms at 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR3: Hybrid search latency (p95) must be less than 1s at 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR4: Graph traversal latency (p95) must be less than 2s at 10 concurrent queries per tenant, 10K memory units per tenant, and depth ≤5. **Phase:** MVP.

NFR5: Ingestion throughput must exceed 100 memory units/minute for payloads ≤10KB and 10 memory units/minute for payloads ≤1MB, per tenant using single-document embedding calls rather than batching. **Phase:** Ongoing.

NFR6: Event indexing freshness must be less than 5s from DAPR pub/sub publication to searchable under normal conditions, with degradation documented when the embedding provider is rate-limited, per event. **Phase:** P1.5.

NFR7: The service must become fully operational within 60s from containers running to accepting queries, excluding image-pull time. **Phase:** Ongoing.

NFR8: There must be zero cross-tenant data leakage: no search, ingestion, or graph traversal may return data from another tenant. Verification uses an automated suite covering search, ingest, and graph across all axes with malformed, empty, and swapped tenant IDs; the graph-specific test creates identical graph structures in tenants A and B, traverses from tenant A, and verifies that no tenant-B nodes appear even if edge IDs collide. **Phase:** MVP.

NFR9: Product services must retrieve embedding-provider and other application runtime secrets exclusively through the DAPR Secrets API, backed by OpenBao in Aspire and deployed environments. Secret values must never be stored in application configuration or ordinary environment variables. Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or direct pod inputs outside the DAPR secret-store boundary. Verification uses structural dependency tests, secret scanning, AppHost topology tests, and integration tests proving DAPR reads from OpenBao without secret disclosure. **Phase:** Ongoing.

NFR10: All inter-service communication must be authenticated via DAPR API tokens, verified by DAPR configuration validation. **Phase:** Ongoing.

NFR11: External access must be authenticated at the ingress layer, with no unauthenticated access to REST API endpoints, verified by integration tests using unauthenticated requests. **Phase:** P1.5.

NFR12: The system must support linear tenant scaling: adding a tenant must not degrade existing-tenant performance by more than 5%. Validate at 10 tenants with 100K memory units each by benchmarking tenant 1 alone, adding nine loaded tenants, re-benchmarking tenant 1, and measuring the delta. **Phase:** Ongoing.

NFR13: Per-tenant ingestion pipelines must scale independently so that one tenant's batch ingestion does not block another tenant's real-time ingestion, verified by concurrent ingestion across three tenants. **Phase:** Ongoing.

NFR14: Redis memory footprint per memory unit must be predictable and documented so operators can estimate infrastructure costs before tenant provisioning, through a sizing guide covering memory per unit by vector dimension and metadata size. **Phase:** Ongoing.

NFR15: The architecture must not preclude backend migration from Redis to Qdrant: use a concrete implementation with clear extraction points identified and no premature interfaces. Verify by architecture review showing documented extraction points and no tight coupling to Redis-specific APIs in domain logic. **Phase:** Ongoing.

NFR16: There must be zero memory-unit loss during Redis restart, verified with AOF persistence enabled. **Phase:** MVP.

NFR17: Ingestion-pipeline state must survive process restarts so queued and in-progress units resume without data loss, verified through DAPR actor-state persistence. **Phase:** MVP.

NFR18: Partial backend failure, with one of three backends down, must produce degraded service rather than total failure; available axes must continue serving results. Verify with chaos tests that kill each backend individually and confirm partial results. **Phase:** Ongoing.

NFR19: Failed ingestion units must never be silently dropped; all failures must be visible through CLI status with error details and failure stage. Verify end to end with intentional failures at each pipeline stage. **Phase:** Ongoing.

NFR20: MCP tool responses must conform to the MCP protocol specification, including valid tool schemas, typed parameters, and structured error responses, verified through an MCP protocol conformance suite. **Phase:** P1.5.

NFR21: DAPR pub/sub integration must handle CloudEvents envelope format so events from any DAPR-compatible publisher are processable, verified with standard CloudEvents payloads. **Phase:** P1.5.

NFR22: Embedding-provider integration must handle rate limiting gracefully so HTTP 429 responses trigger backoff without pipeline crash or data loss, verified by rate-limit simulation per provider. **Phase:** Ongoing.

NFR23: The CLI must connect to the memory server through a configurable endpoint supporting local development (`localhost`), container (`docker` service name), and remote ingress URL environments, verified through configuration-layering tests across all three. **Phase:** Ongoing.

NFR24: Hybrid fusion must use deterministic weighted reciprocal-rank fusion with per-axis rank contributions in the 0.0-1.0 range, while single-axis explain continues to document axis-specific score semantics. Verify through fusion and explain unit tests with known rankings and weights. **Phase:** MVP.

NFR25: The fusion algorithm must produce deterministic scores: the same query against the same data produces identical composite scores, although ordering within the same score tier may vary. Verify with 100 repeated queries and zero score variance. **Phase:** MVP.

NFR26: The benchmark suite must produce reproducible results: running benchmarks twice against the same dataset yields identical NDCG@10 scores, verified in CI. **Phase:** MVP.

NFR27: Logging must be structured JSON with OpenTelemetry correlation IDs from DAPR trace context, verified by log-format validation. **Phase:** Ongoing.

NFR28: Trace context must propagate across all DAPR service-invocation hops, producing an end-to-end trace from CLI or MCP through the server to the backend, verified by a distributed-trace completeness test. **Phase:** Ongoing.

NFR29: Custom metrics must be exported through OpenTelemetry for ingestion throughput, search latency per axis, index size per tenant, and pipeline queue depth; the Aspire dashboard must show all metrics during local development. **Phase:** Ongoing.

NFR30: Every CLI command must include `--help` with at least one usage example, verified by parsing all commands and checking for example presence. **Phase:** MVP.

NFR31: The README must include a working quickstart that completes in less than 30 minutes on a clean machine with Docker installed, verified through a timed walkthrough. **Phase:** MVP.

**Total NFRs: 31**

### Additional Requirements

#### Scope, sequencing, and release gates

- The MVP is a proof-of-thesis release. All three hard gates must pass: three-axis retrieval outperforms every single axis on at least 80% of valid benchmark queries, cross-tenant leaks remain at zero, and first-search onboarding completes in under 30 minutes.
- At least two of three soft gates must pass: causal-chain completeness ≥95%, end-to-end MCP integration, and correct case scoping.
- Benchmark ground truth is defined by Jerome plus two independent reviewers before queries are written; inter-rater agreement must reach at least 80%; NDCG@10 is the automated metric; disputed automated judgments receive human review.
- Foundation sequencing is mandatory before data-writing stories: scaffold/AppHost/ServiceDefaults, minimum build/test feedback, tenant provisioning, minimal case bootstrap, and tenant/case validation guards precede ingestion, indexing, search, or graph writes.
- Each search axis must work independently before the fusion spike begins. BM25, cosine, and graph-proximity normalization must each be solved and documented before fusion weighting.
- Phase 1.5—EventStore integration, MCP, and expanded CLI—must ship within four weeks of thesis validation; if that cannot happen, MCP returns to MVP scope.
- FR71 export is Phase 2 unless a later approved change explicitly advances it.

#### Architecture and deployment constraints

- The system is a .NET 10, DAPR-native service topology orchestrated by .NET Aspire; JSON is the sole serialization format.
- External clients use infrastructure-managed ingress; internal services use DAPR service invocation or pub/sub. Application code does not own ingress.
- EventStore integrations publish CloudEvents to DAPR pub/sub; the Memories Server sidecar delivers them to `/events/ingest`. Domain modules must not bypass this with direct REST pushes.
- Tenant provisioning owns physically isolated RediSearch, Redis Vector, and FalkorDB resources; minimal case bootstrap occurs only inside an active tenant; invalid or mismatched tenant/case context fails before any backend write.
- Each memory unit has strict single-case ownership. Reassignment requires deletion and re-ingestion, and graph relationships remain case-scoped.
- The ingestion pipeline is a durable, per-tenant DAPR actor with a bounded queue, throttling, ordering, progress tracking, retry with exponential backoff, and visible dead-letter state. Document work is stateless; per-document actors are prohibited.
- Indexing completion means the memory unit is searchable across every required axis. Partial writes require rollback or retry toward cross-backend consistency.
- The package release inventory is governed by `tools/release-packages.json`; package versions follow Semantic Versioning and service contracts accept backward-compatible additions only.

#### Embedding and secret-management constraints

- Google `text-embedding-004` at 768 dimensions is the MVP runtime provider; other listed providers are post-MVP unless a later sprint change advances them.
- Embedding provider, model, dimensions, secret reference, and throttle are tenant-scoped. Vector dimensions derive from provider/model.
- Because the Redis Vector index schema is fixed at creation, changing provider or vector dimensions requires a full tenant reindex and must be documented as a migration.
- Shared provider keys create a shared upstream quota even when per-tenant actor ceilings are enforced; separate tenant keys are required for full quota isolation.
- Runtime secrets are retrieved via DAPR Secrets API backed by OpenBao. Configuration fallback must not resolve sensitive values; local Aspire parameters or User Secrets may bootstrap or seed but must not become an alternate runtime provider.

#### Trust, compliance, and data-model constraints

- Memories is interpretive infrastructure: it owns correct embeddings, structured causal chains, calibrated relationship confidence, edge completeness, ordering, edge typing, and explicit gap detection; applications own decisions and legal compliance; LLMs own narrative quality.
- Search relevance confidence is not factual accuracy or completeness. That caveat must appear in API reference material, every CLI explain result, compliance guidance, and MCP response-schema documentation.
- The Evidence Packet is the common `Contracts.V1` trust envelope across CLI JSON, MCP, and future web UI. It combines confidence breakdown, source/origin attribution, omitted-detail handling, degraded-state reporting, tenant/case scope, result state, and recovery guidance.
- Metadata origin/confidence is distinct from search relevance. Every memory unit must carry mandatory `ingested_by` provenance.
- Causal traversals must return ordered timestamped nodes, typed directional edges, confidence tiers, and explicit `[MISSING: event-id]` markers rather than silently skipping missing nodes.
- `caused_by` and `correlated_with` are semantically distinct and may not be collapsed. AI-inferred edge confidence is never automatically promoted.
- Tenant deletion removes tenant-owned indexes, graph data, and memory units; cross-references held in other tenants remain the consuming application's responsibility and must be documented.
- Access telemetry is infrastructure telemetry, not a certified tamper-evident audit trail. Applications needing certified retention and integrity controls must build those controls above it.
- Compliance documentation requires a compliance-enablement guide, deletion-limitations section, legal disclaimer, and auditor-oriented security-posture section.

#### Licensing, documentation, and contributor constraints

- The product license is Apache 2.0 and the README must state a public commitment not to switch to a restrictive license.
- Redis Stack SSPL/RSAL managed-service constraints and the FalkorDB AGPL network-service boundary must be documented; FalkorDB must be version-pinned; `IMemoryGraph` and `IMemoryIndex` extraction points are licensing insurance targeted for Phase 2.
- Documentation includes a README quickstart, command examples in CLI help, a getting-started guide, generated API reference, compliance guide, and operator guide.
- The repository must build cleanly for contributors; unit tests run without Docker, integration tests require documented Docker prerequisites, and CI runs unit, integration, and contract layers.
- Samples form a numbered learning path covering quickstart, EventStore integration, and MCP-agent integration.

### PRD Completeness Assessment

The PRD is unusually comprehensive: it defines 74 sequential functional requirements, 31 sequential non-functional requirements with measurable targets and verification methods, personas and journeys, phase boundaries, success and kill criteria, domain responsibilities, interface parity, operational behavior, testing strategy, licensing constraints, and trust semantics. The requirement numbering has no gaps, and most NFRs are directly testable.

The principal PRD-readiness risk is phase attribution rather than missing capability. Except for FR71, the FR list does not tag requirements by phase, while surrounding sections split capabilities among MVP, Phase 1.5, Phase 2, and Phase 3. Examples needing authoritative phase reconciliation include batch ingestion (FR3), MCP capabilities (FR54/FR58), EventStore integration (FR59-FR62), membership and annotation capabilities (FR28-FR29/FR36-FR37), and operational CLI diagnostics. This can cause an epic to appear complete while targeting the wrong release gate.

Additional internal clarity issues should be resolved or explicitly accepted:

- The MVP strategy requires cases and physical tenant isolation from day one and makes isolation a hard gate, but the resource-tightening fallback says cases and tenant isolation could move to fast-follow. Those positions conflict.
- The developer-tool matrix states .NET 10 / C# 13, while current repository policy uses C# 14; the PRD should identify whether this is stale text or an intentional compatibility constraint.
- The release inventory says nine published packages plus three non-packable service/orchestration projects, while the package table visibly identifies only two non-packable projects; the third project is not named there.
- Service-to-backend communication is described as “DAPR state / direct connection via DAPR sidecar,” which does not establish a single ownership boundary for direct Redis/FalkorDB clients.
- The top-level LLM-agent latency goal uses cached `<200ms` and cold `<2s`, while NFR3 sets hybrid p95 `<1s` under a stated load. These may be compatible, but the caching and measurement relationship is unspecified.
- Per-user identity is declared outside MVP while `ingested_by`, access logging, and support-safe attribution are mandatory; the permitted MVP identity source needs an explicit contract.
- NFR9 is tagged Ongoing even though the MVP requires a runtime embedding provider and explicitly mandates DAPR/OpenBao secret retrieval. The phase tag may understate a launch-critical security dependency.

Subject to those phase and boundary clarifications, the PRD is complete enough to support systematic epic traceability analysis.

## Epic Coverage Validation

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR1 | Developer can ingest content from local files into a specified case. | Epic 1 — Ingest from local files | ✓ Covered |
| FR2 | Developer can ingest content from URLs into a specified case. | Epic 6 — Ingest from URLs | ✓ Covered |
| FR3 | Developer can batch-ingest content from a directory into a specified case. | Epic 6 — Batch-ingest from directory | ✓ Covered |
| FR4 | System can extract text from ingested content (plain text, PDF, markdown). | Epic 1 — Text extraction (Kreuzberg) | ✓ Covered |
| FR5 | System can generate embeddings for ingested content via a configurable embedding provider. | Epic 1 — Generate embeddings | ✓ Covered |
| FR6 | System ensures a memory unit is fully searchable across all axes after ingestion completes. | Epic 1; reinforced by Epic 23 for scalable chunking and batch embedding | ✓ Covered |
| FR7 | Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score. | Epic 1 — Metadata with origin tracking | ✓ Covered |
| FR8 | System manages ingestion load per tenant independently. | Epic 6 — Per-tenant ingestion load management | ✓ Covered |
| FR9 | System retries failed ingestion automatically with configurable limits. | Epic 6 — Automatic retry with configurable limits | ✓ Covered |
| FR10 | Developer can view ingestion status per case (queued, embedding, indexed, failed counts). | Epic 6 — Ingestion status per case | ✓ Covered |
| FR11 | Developer can view failed ingestion units with error details and failure stage. | Epic 6 — Failed-unit visibility | ✓ Covered |
| FR12 | Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk. | Epic 6; reinforced by Epic 23 for non-URL re-ingestion correctness | ✓ Covered |
| FR13 | System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes). | Epic 1; reinforced by Epic 21 for ratified consistency and migration safety | ✓ Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant. | Epic 2 — Syntactic search | ✓ Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant. | Epic 2 — Semantic search | ✓ Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant. | Epic 2 — Graph search | ✓ Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes. | Epic 2 — Hybrid fusion search | ✓ Covered |
| FR18 | Developer can control which axes are included in a search query. | Epic 2 — Axis-selection control | ✓ Covered |
| FR19 | Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode). | Epic 2 — Per-axis score breakdown | ✓ Covered |
| FR20 | Developer can filter search results by case. | Epic 3 — Case filtering | ✓ Covered |
| FR21 | Developer can filter search results by metadata field values. | Epic 3 — Metadata filtering | ✓ Covered |
| FR22 | Developer can paginate search results. | Epic 2; reinforced by Epic 22 for semantic, graph-scoped, and hybrid pagination correctness | ✓ Covered |
| FR23 | LLM Agent can constrain search response size by token budget. | Epic 10 — MCP token budget and deterministic omitted-detail expansion handles | ✓ Covered — Phase 1.5 |
| FR24 | System returns the origin identifier (file path, URL, or event ID) and origin type for each search result. | Epic 2 — Origin identifier in results | ✓ Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output. | Epic 2 — Benchmark comparisons | ✓ Covered |
| FR26 | Developer can create a case within a tenant. | Epic 0 + Epic 3 — Minimal case bootstrap and full case management | ✓ Covered |
| FR27 | Developer can delete a case and all its memory units. | Epic 3 — Case deletion | ✓ Covered |
| FR28 | Developer can add members to a case. | Epic 3 — Add case members | ✓ Covered |
| FR29 | Developer can remove members from a case. | Epic 3 — Remove case members | ✓ Covered |
| FR30 | Developer can list cases within a tenant. | Epic 3 — List cases | ✓ Covered |
| FR31 | Developer can view case status including memory unit count, last activity timestamp, and health indicators. | Epic 3 — Case status | ✓ Covered |
| FR32 | System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion. | Epic 3 — Single-case ownership | ✓ Covered |
| FR33 | System maintains case-scoped graph edges between memory units within a case. | Epic 3 — Case-scoped graph edges | ✓ Covered |
| FR34 | Developer can search across all cases within a tenant by keyword, returning results with case attribution. | Epic 3; reinforced by Epic 22 for fusion case attribution | ✓ Covered |
| FR35 | Developer can delete an individual memory unit from a case. | Epic 3 — Memory-unit deletion | ✓ Covered |
| FR36 | Developer can view recent activity within a case (ingestion events, searches, membership changes). | Epic 3 — Case activity | ✓ Covered |
| FR37 | Developer can annotate or correct a memory unit, with annotations tracked as linked memory units. | Epic 3 — Annotations and corrections | ✓ Covered |
| FR38 | Operator can create a tenant with physically separate indexes. | Epic 0 + Epic 5; reinforced by Epic 24 for physical-isolation strategy | ✓ Covered |
| FR39 | Operator can delete a tenant and all its indexes, graph data, and memory units. | Epic 5; reinforced by Epic 21 for deletion completeness | ✓ Covered |
| FR40 | Operator can verify tenant isolation via automated checks. | Epic 5; reinforced by Epic 24 for verifier scaling | ✓ Covered |
| FR41 | Operator can list tenants. | Epic 5 — List tenants | ✓ Covered |
| FR42 | Operator can update tenant configuration after creation (rate limits, display name, settings). | Epic 5 — Update tenant configuration | ✓ Covered |
| FR43 | System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment. | Epic 5 — Prevent inconsistent configuration changes | ✓ Covered |
| FR44 | System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages. | Epic 0 + Epic 5; reinforced by Epic 20 for authorization and Epic 24 for physical isolation | ✓ Covered |
| FR45 | Operator can view current configuration of a tenant (embedding provider, rate limits, index status). | Epic 5 — View tenant configuration | ✓ Covered |
| FR46 | System can index CausationId and CorrelationId from events as typed, directional graph edges. | Epic 1 — Graph-edge creation during ingestion | ✓ Covered |
| FR47 | Developer can traverse causal chains from a starting node with configurable depth. | Epic 4 — Causal traversal | ✓ Covered |
| FR48 | Developer can filter graph traversal by edge type. | Epic 4 — Edge-type filtering | ✓ Covered |
| FR49 | When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier. | Epic 4 — Missing-node gap markers | ✓ Covered |
| FR50 | System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence. | Epic 4 — Edge taxonomy | ✓ Covered |
| FR51 | Developer can promote AI-inferred edge confidence when verifying a relationship. | Epic 4 — Confidence promotion | ✓ Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes. | Epic 4 — Chronological ordering | ✓ Covered |
| FR53 | Developer can interact with all retrieval and ingestion capabilities via CLI. | Epic 7 — CLI capability surface | ✓ Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools. | Epic 10 — MCP tools | ✓ Covered — Phase 1.5 |
| FR55 | CLI supports multiple output formats: human-readable (default), JSON, and table. | Epic 7 — CLI output formats | ✓ Covered |
| FR56 | CLI provides actionable error messages with recovery suggestions for common failure modes. | Epic 7 — Actionable CLI errors | ✓ Covered |
| FR57 | Developer can discover available actions from any system state, including empty states and error conditions. | Epic 7 — Discoverable actions | ✓ Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions for LLM agent consumption. | Epic 10 — MCP typed schemas | ✓ Covered — Phase 1.5 |
| FR59 | System can auto-discover event types published to DAPR pub/sub topics. | Epic 9 — Event auto-discovery | ✓ Covered — Phase 1.5 |
| FR60 | System can generate dual embeddings for events (raw payload + natural language description). | Epic 9 — Dual event embeddings | ✓ Covered — Phase 1.5 |
| FR61 | System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code. | Epic 9 — Automatic causal metadata indexing | ✓ Covered — Phase 1.5 |
| FR62 | Developer can list registered event handlers and detect handler registration mismatches. | Epic 9 — Handler registration management | ✓ Covered — Phase 1.5 |
| FR63 | System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result. | Epic 2 — Composite confidence and Evidence Packet mapping | ✓ Covered |
| FR64 | System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit. | Epic 7 — Metadata-origin display; the underlying model is also covered by Epic 1 | ✓ Covered |
| FR65 | System records `ingested_by` (user or system identity) as a mandatory field on every memory unit. | Epic 1 — `ingested_by` provenance | ✓ Covered |
| FR66 | When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded. | Epic 5 — Degraded partial results | ✓ Covered |
| FR67 | System logs search and access events per tenant for audit purposes. | Epic 7; reinforced by Epic 20 for audit emission and Epic 27 for lifecycle hardening | ✓ Covered — retention residual remains open |
| FR68 | Operator can configure embedding provider and model per tenant. | Epic 1 — Google MVP configuration with extensible provider/model shape; later provider expansion is post-MVP unless explicitly selected | ✓ Covered |
| FR69 | System enforces per-tenant rate limit ceilings for embedding API calls. | Epic 5 — Per-tenant rate limits | ✓ Covered |
| FR70 | System tracks the embedding provider and model used for each memory unit's vectors. | Epic 5 — Embedding-model tracking | ✓ Covered |
| FR71 | Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. | Epic 26 covers backup/restore and disaster-recovery reinforcement; the full application-facing export story remains a reserved Phase 2 placeholder | ✓ Traceable — deferred Phase 2 |
| FR72 | System exposes readiness and liveness health checks verifying all backends. | Epic 8 — Health checks | ✓ Covered |
| FR73 | Operator can detect index/graph divergence via consistency check. | Epic 8 — Consistency verification | ✓ Covered |
| FR74 | Operator can repair detected index/graph inconsistencies via consistency repair operation. | Epic 8 — Consistency repair | ✓ Covered |

### Missing Requirements

No PRD functional requirement is absent from the explicit epics FR Coverage Map.

- Missing PRD FR identifiers: none.
- Duplicate FR Coverage Map identifiers: none.
- FR identifiers present in the epics map but absent from the PRD: none.

FR71 is traceable but intentionally deferred: Epic 26 reinforces backup/restore and disaster-recovery behavior, while the complete application-facing portable-export capability remains a reserved Phase 2 placeholder that requires activation and a normal story before implementation. This is a scope-state caveat, not an unmapped requirement.

FR67 is mapped, but the `20.5-A41-ACCESS-TELEMETRY-RETENTION` residual remains carried forward. The mapping must not be interpreted as full closure of bounded access-telemetry retention.

### Coverage Statistics

- Total PRD FRs: 74
- Unique FRs in the explicit epics coverage map: 74
- FRs covered or explicitly deferred with an implementation home: 74
- Missing FRs: 0
- Extra epic-map FR identifiers: 0
- Overall traceability coverage: 100.0%
- Active MVP epics (0-8) map 66 of 74 FRs (89.2%); seven FRs map to Phase 1.5 (FR23, FR54, FR58-FR62), and FR71 remains deferred to Phase 2.

The 100% figure measures traceability across the complete epic portfolio. It does not mean all 74 requirements belong to active MVP scope or are implemented.

## UX Alignment Assessment

### UX Document Status

**Found and complete:** `_bmad-output/planning-artifacts/ux-design-specification.md` (1,164 lines).

The UX specification is full-horizon guidance rather than an MVP scope declaration. It explicitly sequences CLI-first MVP work, MCP/EventStore Phase 1.5 work, and future FrontComposer/Fluent UI web composition. It defines personas, journeys, a shared trust model, 40 UX design requirements, component patterns, responsive behavior, accessibility expectations, and validation guidance.

### UX-to-PRD Alignment

The UX specification is strongly aligned with the PRD's users, journeys, and trust requirements:

- Alex, the LLM Agent, Kenji, Marcus, and Priya have corresponding PRD journeys and UX flows.
- The shared Evidence Packet composes PRD requirements for source attribution (FR24), explainability and score breakdowns (FR19 and FR63), token-budget omissions (FR23), tenant/case scope (FR20, FR26, and FR44), graceful degradation (FR66), graph context and gaps (FR47-FR52), and recovery guidance (FR56-FR57).
- Ingestion lifecycle and recovery experiences align with FR10-FR13, while operator health and repair experiences align with FR72-FR74.
- The UX phase statement correctly preserves the PRD's CLI-first MVP and Phase 1.5 MCP/EventStore sequencing.

The following PRD/UX gaps need explicit disposition:

1. **Web accessibility and responsive behavior are not PRD requirements.** The UX requires WCAG 2.2 AA, keyboard completion of the trust loop, labelled regions, live-region behavior, predictable focus, reduced motion, forced-colors support, minimum responsive viewports, and compact component variants. None of the 31 PRD NFRs establishes these as product-level acceptance requirements. They are therefore vulnerable to being treated as optional design guidance when Epic 17 is activated.
2. **“First-class surface” language is not yet unambiguous.** The PRD says every feature is accessible through both MCP and CLI, while its capability matrix and phase plan deliberately reserve operational capabilities for CLI and defer MCP. The UX says CLI, MCP, and web are first-class while limiting semantic parity to the core trust loop. The PRD should define first-class as equivalent Evidence Packet semantics and task-appropriate capability, not identical feature parity.
3. **The composed first-response behavior is implicit rather than independently measurable.** The UX requires the first trust response to include or derive scope, sources, evidence strength, freshness, explain detail, relevant graph context, and recovery. The PRD defines the constituent FRs but no end-to-end requirement or latency target for composing that complete packet.

### UX-to-Architecture Alignment

The architecture supports the principal UX direction:

- `Contracts.V1` owns one Evidence Packet envelope for CLI JSON, MCP responses, and future web composition, including scope, result, sources, evidence, graph context, state, omitted details, and recovery actions.
- The architecture preserves tenant and case context, origin/freshness metadata, per-axis scoring, graph gaps, degraded/partial results, token-budget expansion handles, and actionable errors across surfaces.
- Web work is correctly identified as a future FrontComposer-aligned Razor component library using Microsoft Fluent UI Blazor V5 only. The architecture repeats the UX constraints against a standalone design system, raw-control library, legacy theme fork, or unjustified custom markup/CSS.
- The future full REST application surface is phase-aligned with future web composition, while MVP and Phase 1.5 remain CLI/MCP focused.
- Durable workflows, explicit failure propagation, backend health, tenant verification, and consistency repair provide architectural support for the UX command-lifecycle and recovery patterns.

Architecture gaps affecting executable UX:

1. **No Evidence Packet composition boundary is assigned.** The architecture defines the contract, but the project structure and search data flow terminate at `SearchResult`. They do not identify the service/component that assembles scope, freshness, score explanation, graph summary, omitted details, and recovery actions into the mandatory cross-surface packet. Without an owner, CLI, MCP, and web can drift despite sharing DTOs.
2. **Trust-state vocabularies are not fully contractual.** UX defines stable values for confidence (`supported`, `partial`, `disputed`, `insufficient`), freshness (`current`, `aging`, `stale`, `unknown`), evidence health, and scope. Architecture exposes generic freshness plus a top-level state list, but does not define the shared enums, precedence rules, or mapping from backend conditions to those labels. In particular, `aging` and `unknown` freshness need explicit wire semantics.
3. **The UX acceptance strategy is not present in the architecture's test model.** Architecture requires FrontComposer/Fluent conformance tests for web exceptions, but its three-tier test plan does not allocate responsive viewport, keyboard, screen-reader, live-region, reduced-motion, forced-colors, or WCAG checks to a project or CI tier.
4. **The complete Evidence Packet has no end-to-end performance budget.** Architecture retains axis-level search targets, but no budget covers packet composition, source/freshness resolution, graph summary, answer synthesis where applicable, and serialization. This leaves the UX “first response” promise unverifiable.
5. **Future web data access is a sequencing dependency.** The architecture defers full REST pagination, facets, and drill-down for application UIs to Phase 2 and places the web RCL in Epic 17. Epic 17 must either depend on that API work or explicitly define a different host-side query boundary; the present documents do not state the dependency.

### Alignment Issues and Warnings

| Severity | Issue | Readiness Effect | Required Resolution |
| --- | --- | --- | --- |
| High | Evidence Packet assembly has a contract but no implementation owner or data-flow step. | Cross-surface semantic drift and incomplete first responses are likely. | Assign a composition service/boundary and make every surface consume it. |
| High | WCAG 2.2 AA and responsive behavior exist only in UX guidance, not PRD NFRs. | Future web stories can be accepted without the declared accessibility baseline. | Add phase-tagged, measurable accessibility/responsive NFRs or explicitly elevate UX-DR acceptance criteria to the product gate. |
| Medium | Trust-state labels and mappings are not fully defined in shared contracts. | CLI, MCP, and web may represent the same backend condition differently. | Define shared enums, mapping rules, precedence, and serialization tests. |
| Medium | No end-to-end Evidence Packet latency target exists. | The defining first-response experience cannot be performance-gated. | Add a phase/load-qualified packet-composition SLO and verification method. |
| Medium | Epic 17's dependency on the Phase 2 application API boundary is implicit. | Web implementation could begin without a stable query/drill-down integration path. | Declare the dependency or define the alternate host-side boundary. |
| Low | “First-class” is used alongside intentional capability asymmetry. | Teams may infer unsupported CLI/MCP/web feature parity. | Normalize the term to semantic parity of the trust loop plus surface-specific capabilities. |

**Step 4 conclusion:** UX intent is substantively aligned with the PRD and architecture, and the design specification is implementation-grade as full-horizon guidance. Readiness is conditional on converting the Evidence Packet composition, trust-state grammar, accessibility baseline, web dependency, and end-to-end performance promise into owned, testable requirements before the affected surface is implemented.

## Epic Quality Review

### Review Population and Structural Baseline

The portfolio contains **32 epics and 165 registered stories**. The active MVP readiness boundary is explicitly limited to Epics 0-8, comprising 47 stories; Epics 9-10 are Phase 1.5, and all later tracks are excluded from MVP accounting unless explicitly selected.

Structural strengths:

- All 165 stories contain an actor, desired outcome, and benefit (`As a` / `I want` / `So that`).
- The active MVP epics deliver developer or operator outcomes and follow a valid backward-only progression: Epic 0 establishes an executable tenant/case-safe foundation; Epics 1-8 consume only earlier outputs.
- Story 0.0 satisfies the architecture's starter/scaffold requirement with the AppHost, ServiceDefaults, solution structure, and single-command boot. Story 0.4 supplies the early greenfield build/test CI preflight before data-writing work.
- Data infrastructure is created when first needed: tenant provisioning owns tenant indexes/databases, feature paths are forbidden from creating them on demand, and domain/index structures are introduced with their consuming stories rather than through an all-entities-up-front story.
- Historical numbering gaps are intentional and documented: Story 1.1 became 0.0, Story 8.3 is reserved non-MVP, and completed 18.x/23.x keys are preserved with an explicit `story_execution_order`.

### Epic-by-Epic Compliance

| Epic | Stories | User/Operator Value | Independence and Story Quality Verdict |
| --- | ---: | --- | --- |
| 0 | 5 | Yes — safe boot, tenant, case, and preflight foundation | Pass; valid starter-template exception and complete executable foundation. |
| 1 | 6 | Yes — first tenant-scoped ingest/search | Pass for current history; Stories 1.5 and 1.6 were oversized bundled slices, and the document correctly forbids reopening that shape. |
| 2 | 8 | Yes — independent and hybrid retrieval with benchmark proof | Pass; consumes Epic 1 only. |
| 3 | 6 | Yes — case organization and lifecycle | Pass; Epic 0 supplies the minimum case capability so no later epic is required. |
| 4 | 3 | Yes — causal traversal | Pass; consumes graph edges from Epic 1. |
| 5 | 6 | Yes — tenant lifecycle and isolation | Pass; deepens the earlier Epic 0 vertical slice without creating a parallel ownership path. |
| 6 | 4 | Yes — resilient ingestion operations | Pass; depends only on the earlier ingestion foundation. |
| 7 | 5 | Yes — CLI developer experience | Pass; integrates already-delivered capabilities. |
| 8 | 4 | Yes — operator health and consistency | Concern: Story 8.5 combines four independently reviewable deliverables; its historical scope guard prevents recurrence. |
| 9 | 3 | Yes — zero-code EventStore integration | Pass for Phase 1.5 sequencing. |
| 10 | 2 | Yes — MCP agent interface | Pass for Phase 1.5 sequencing. |
| 11 | 2 | Indirect — CI/release mechanics | Major strict-standard deviation: primarily a technical milestone epic, although explicitly excluded into the operational track. |
| 12 | 6 | Yes — first release and operability | Pass as an operator/maintainer outcome; several criteria use abbreviated BDD triads. |
| 13 | 7 | Yes — provider migration | Pass with sizing caution around the multi-checkpoint OIDC and integration stories. |
| 14 | 5 | Indirect — deferred-work hardening | Major strict-standard deviation: umbrella technical remediation rather than an independently usable outcome. |
| 15 | 6 | Indirect — risk/governance closure | Major strict-standard deviation: completion can mean implementation, acceptance, or renewed deferral across multiple unrelated risks. |
| 16 | 1 | Indirect — registry cross-check design | Major: a design-only technical story is the entire epic; the user-visible operational outcome is not independently delivered by the epic definition. |
| 17 | 7 | Yes — accessible evidence inspection | Major historical ordering deviation: Stories 17.2-17.5 require later-numbered Story 17.6; the execution-order registry mitigates scheduling but not the forward-reference structure. |
| 18 | 8 | Indirect — consumer contract hardening | Major historical ordering deviation: Story 18.5 consumes Story 18.6. The explicit order and completed status preserve traceability but do not meet normal numeric independence rules. |
| 19 | 4 | Indirect — deferred-register governance | Major strict-standard deviation: administrative backlog disposition rather than standalone product/operator behavior. |
| 20 | 6 | Yes — authenticated, tenant-safe API operations | Pass as security/operator value. |
| 21 | 10 | Yes — data integrity and safe migration | Major sizing concern in Story 21.9; its completed story file contains the required per-checkpoint evidence table, mitigating execution ambiguity. |
| 22 | 7 | Yes — retrieval correctness | Pass; cohesive user-facing remediation outcomes. |
| 23 | 9 | Yes — scalable/resilient ingestion | Major historical ordering deviation: Story 23.1 depends on later-numbered Story 23.9. The authoritative execution order was followed and both are done. |
| 24 | 5 | Yes — observable and performant operations | Pass as operator value. |
| 25 | 8 | Mostly indirect — architecture/code health | Major strict-standard deviation: most stories are internal refactoring milestones; only the Evidence Cockpit slice directly exposes a user outcome. |
| 26 | 8 | Yes — deployable, recoverable operations | Concern: Story 26.5 is an umbrella runbook story, but its completed story file contains the required checkpoint evidence table. |
| 27 | 4 | Yes — bounded access-telemetry lifecycle | **Critical failure:** Story 27.3 contains non-dischargeable criteria and Story 27.4 requires withdrawn, unregistered Stories 27.5/27.6. |
| 28 | 1 | Indirect — dependency identity adoption | Major strict-standard deviation: technical dependency adoption; now externally unblocked but still backlog and missing an explicit AC heading. |
| 29 | 2 | Yes — secure provider-neutral local secret topology | Pass conditionally; Story 29.2 depends backward on 29.1, which is done. |
| 30 | 5 | Indirect — CI/CD alignment and publication | Major readiness failure: technical milestone epic; Stories 30.3 and 30.4 are activation-blocked on a future Hexalith.Builds capability/evidence contract. |
| 31 | 2 | Yes — deployable OpenBao platform and runtime migration | Pass conditionally; Story 31.2 has a clear earlier-story checkpoint gate and is recorded ready for development. |

### Dependency Analysis

The active MVP sequence contains no forward dependency. Its principal dependencies are explicit and valid: Epic 0 before data writing, Epic 1 before search fusion and causal traversal, and Story 0.4 before Story 1.2 onward.

Strict forward-dependency violations or blockers in the wider portfolio are:

1. **Epic 27 has a missing dependency owner.** Story 27.4 cannot start until actual Story 27.5 and 27.6 files are approved, registered, and done, but those stories were withdrawn. The 25 C1.1-C1.25 production gates are held in a proposal annex and have no registered owner or sprint state. This is not a schedulable dependency chain.
2. **Story 27.3 preserves acceptance criteria it explicitly cannot discharge.** AC1-AC5 describe the transferred C1 scope while the story's binding text says those criteria cannot be completed by Story 27.3. A story cannot reach an unambiguous done state while retaining mandatory-looking, non-owned criteria.
3. **Epic 27 consumes later Epic 30 output.** Story 30.3 owns the four OCI archives consumed by Story 27.3's deployment-verification lane while Epic 30 remains backlog. This is an explicit later-epic dependency and violates epic independence even though existing historical archive tooling may permit partial verification.
4. **Epics 17, 18, and 23 preserve later-numbered prerequisites.** The repository's machine-readable execution order makes these executable, and all affected stories are done, but the structure remains a documented exception rather than best-practice compliance.
5. **Epic 30 is not currently independently startable end to end.** Story 30.3 requires an owner-approved Hexalith.Builds revision supporting multi-container publication identity, and Story 30.4 requires shared evidence sufficient for partial-release recovery. These are honest fail-closed gates, but the epic is not implementation-ready until the external capability is available and pinned.

### Story Sizing and Completeness

- Stories 1.5, 1.6, and 8.5 explicitly acknowledge that they bundle multiple independently reviewable slices. They are historical, and their scope guards require future rework to be split; no current corrective renumbering is needed.
- Stories 21.9 and 26.5 are checkpoint-heavy, but their story files contain owner/evidence/review/completion tables as required by the portfolio policy.
- Story 27.3 remains an epic-sized, high-churn implementation record: its story file is 1,590 lines, mixes transferred and active criteria, carries multiple checkpoint lanes, and retains stale/superseded ownership narratives. The evidence table does not cure the absence of owners for C1.1-C1.25 or the contradictory completion contract.
- No relational-database “create every table up front” violation was found. Redis/FalkorDB resource creation is deliberately centralized in tenant provisioning and introduced before first use.

### Acceptance Criteria Quality

Quantitative structure:

- 164 of 165 stories have an explicit `Acceptance Criteria` heading. Story 28.1 has seven complete Given/When/Then scenarios but omits the heading.
- 164 of 165 stories use at least one Given/When/Then scenario. Story 27.3 instead uses eight long numbered criteria.
- Nine stories contain at least one abbreviated Given/Then scenario without a matching When: 12.1-12.6, 17.6, 26.6, and 26.7. These remain testable but are inconsistent with the declared BDD standard.
- Twenty-three remediation stories use `When this story completes` as the event. This is implementation-centric rather than behavioral and often packs several independently verifiable results into one `Then` (Stories 18.1-18.3, 21.1, 21.4, 21.10, 22.7, 23.9, 24.3-24.4, 25.1-25.8, and 26.1-26.5).

Most criteria are highly specific and name errors, negative paths, test lanes, or observable state. The dominant weakness is not vagueness but over-compression: large compound `Then` clauses make partial completion difficult to represent and encourage technical task bundles instead of vertical behavior.

### Best-Practice Violations by Severity

#### Critical Violations

1. **Unowned mandatory work blocks Epic 27.** C1.1-C1.25 have no registered story owner; Story 27.4 depends on nonexistent registered successors. Register compliant, independently scoped successor stories with real evidence producers before treating Epic 27 as implementable.
2. **Story 27.3 has no clean completion contract.** Remove transferred AC1-AC5 from its binding acceptance set (preserving them only in historical notes), retain only C0/C2/C3/C4 behavior it owns, and reconcile the story file, `epics.md`, deferred ledger, and sprint status to one current truth.
3. **Epic 27 depends forward on backlog Epic 30 output.** Either move the archive producer prerequisite into an earlier independently deliverable story, consume a versioned already-available publication contract, or resequence the epics so verification never waits on a later epic.

#### Major Issues

1. **Technical-milestone epics remain in the same epic portfolio.** Epics 11, 14-16, 19, 25, 28, and 30 are principally CI, design, refactoring, dependency, or governance milestones. The operational-track disclaimer prevents MVP miscounting but does not satisfy strict user-value epic standards. Move them to an engineering roadmap or rewrite each around a standalone maintainer/operator capability with independently demonstrable value.
2. **Historical numeric forward dependencies exist in Epics 17, 18, and 23.** Preserve aliases for traceability, but new/reopened work should receive correctly ordered numeric stories rather than relying on `story_execution_order` to normalize forward references.
3. **Epic 30 is externally activation-blocked.** Do not select Stories 30.3/30.4 until the required shared-workflow revision and evidence schema exist and are pinned.
4. **Several stories are too broad for a single implementation/review unit.** Continue enforcing the existing split-on-reopen rules for Stories 1.5, 1.6, and 8.5; split Story 27.3 now; and use separately tracked vertical slices rather than umbrella completion for future checkpoint-heavy work.
5. **Twenty-three technical remediation stories use completion-as-trigger acceptance criteria.** Rewrite affected backlog/reopened criteria around a concrete system state and operator/developer action, with one independently verifiable outcome per scenario.

#### Minor Concerns

1. Add an explicit `Acceptance Criteria` heading to Story 28.1.
2. Convert Story 27.3's surviving owned criteria to Given/When/Then form after rescoping.
3. Normalize the nine abbreviated BDD scenarios by adding the missing When clause.
4. Keep the story-key alias map and sprint execution order synchronized until all historical exceptions are permanently closed; no unexplained numbering gap was found in the current artifact.

### Step 5 Conclusion

The **active MVP backlog passes** the epic-value, backward-dependency, starter, and data-creation-timing checks, with only guarded historical sizing debt. The **complete lifecycle portfolio does not pass strict implementation-readiness standards** because Epic 27 contains unowned mandatory work, a non-dischargeable story contract, and a dependency on later backlog output. Epic 30 also remains externally activation-blocked. These defects must be resolved before the affected operational-readiness work can be called ready for implementation.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY — complete lifecycle portfolio**

**Qualified result:** the active MVP artifact set (Epics 0-8) is structurally ready: its requirements are traceable, its epics deliver developer/operator value, its dependencies run backward through an executable foundation, and its historical broad stories have explicit split-on-reopen guards. The portfolio-level failure is caused by current operational-readiness work, not by missing MVP FR coverage.

The overall status remains **NOT READY** because Epic 27 has mandatory production gates with no registered story owner, Story 27.4 depends on withdrawn successor stories, Story 27.3 retains criteria it explicitly cannot discharge, and part of its verification chain depends on later backlog Epic 30. A plan with no schedulable owner for mandatory work cannot be implementation-ready.

### Critical Issues Requiring Immediate Action

1. **Restore an owned, schedulable Epic 27 path.** Author compliant Story 27.5/27.6 replacements (or a newly approved set of smaller stories), register them in `epics.md` and `sprint-status.yaml`, and give every C1.1-C1.25 gate one accountable owner, one real rerunnable evidence producer, and one completion state. Until this occurs, keep Production lifecycle writes disabled, Story 27.4 in backlog, and A41 open.
2. **Make Story 27.3's completion contract internally consistent.** Its binding story definition must contain only the C0/C2/C3/C4 scope it owns. Move AC1-AC5 and all transferred C1 material to a clearly non-binding historical/transfer annex, then reconcile the story file, epic definition, deferred ledger, and sprint registry so one status vocabulary and one current ownership map remain.
3. **Eliminate the Epic 27 → Epic 30 forward dependency.** Assign the required archive producer to an earlier independently deliverable prerequisite, consume an already-versioned and available publication contract, or formally resequence the operational epics. Do not rely on future backlog work to complete an in-progress earlier epic.

### Recommended Next Steps

1. Run a focused course correction for Epic 27 covering story registration, C1 ownership, Story 27.3 rescoping, Story 27.4 prerequisites, and the Epic 30 archive boundary in one reconciled change set.
2. Add an authoritative phase matrix to the PRD for all 74 FRs and 31 NFRs. Resolve the day-one tenant/case-isolation conflict, align .NET/C# versions, name the third non-packable project, clarify direct backend-client ownership, define MVP identity provenance, and retag NFR9 according to its launch dependency.
3. Amend the architecture with an owned Evidence Packet composer and data-flow step. Define the shared confidence/freshness/evidence/scope enums and mapping precedence, plus an end-to-end packet latency SLO and contract/serialization tests.
4. Before activating Epic 17, elevate WCAG 2.2 AA, responsive viewports, keyboard/focus, screen-reader/live-region, reduced-motion, forced-colors, and touch-target expectations into phase-tagged product NFRs or an explicit acceptance gate. Declare the web RCL's dependency on the Phase 2 application API or define its alternate host-side query boundary.
5. When technical or checkpoint-heavy backlog work is reopened, replace completion-trigger criteria with behavioral scenarios, split independently verifiable outcomes into separate stories, and keep technical roadmap items outside the product epic hierarchy unless they produce a standalone maintainer/operator capability.
6. Re-run implementation readiness after the Epic 27 correction and artifact reconciliation. Do not infer readiness from 100% FR traceability alone; verify scope activation, owner assignment, and executable dependency order.

### Final Note

This assessment recorded **27 findings and scope caveats across four categories**: requirements clarity, functional traceability/scope, UX-architecture alignment, and epic/story quality. Three are critical portfolio blockers. The documents are unusually comprehensive and achieve 100% FR traceability, but completeness of description does not compensate for unowned mandatory work or an impossible dependency chain.

**Assessment date:** 2026-08-02  
**Assessor:** Codex, executing the BMad Implementation Readiness workflow  
**Evidence set:** `prd.md`, `architecture.md`, `epics.md`, `ux-design-specification.md`, relevant readiness metadata in `sprint-status.yaml`, and checkpoint-heavy story records referenced by the epic policy.
