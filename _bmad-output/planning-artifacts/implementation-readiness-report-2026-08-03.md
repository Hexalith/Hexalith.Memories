---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
overallReadiness: NOT_READY
completedAt: 2026-08-03
inputDocuments:
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

**Date:** 2026-08-03
**Project:** memories

## Document Inventory

### PRD

- Whole document: `prd.md` (88,353 bytes; modified 2026-07-19)
- Sharded documents: None

### Architecture

- Whole document: `architecture.md` (121,376 bytes; modified 2026-08-02)
- Sharded documents: None

### Epics and Stories

- Whole document: `epics.md` (382,690 bytes; modified 2026-08-03)
- Sharded documents: None

### UX Design

- Whole document: `ux-design-specification.md` (99,240 bytes; modified 2026-06-27)
- Sharded documents: None

### Discovery Issues

- Duplicate whole and sharded formats: None
- Missing required document types: None

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

NFR1: Syntactic search latency (p95) must be <200ms with 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR2: Semantic search latency (p95) must be <500ms with 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR3: Hybrid search latency (p95) must be <1s with 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR4: Graph traversal latency (p95) must be <2s with 10 concurrent queries per tenant, 10K memory units per tenant, and depth ≤5. **Phase:** MVP.

NFR5: Ingestion throughput must exceed 100 memory units/min for payloads ≤10KB and 10 memory units/min for payloads ≤1MB, per tenant, using single-document embedding calls rather than batching. **Phase:** Ongoing.

NFR6: Event indexing freshness must be <5s from DAPR pub/sub publication to searchable under normal conditions, with degradation documented when the embedding provider is rate-limited, per event. **Phase:** P1.5.

NFR7: Cold start time must be within 60s from containers running to accepting queries, excluding image pull time. **Phase:** Ongoing.

NFR8: Zero cross-tenant data leakage — no search, ingestion, or graph traversal returns data from another tenant. Verification is an automated suite covering search, ingest, and graph across all axes with malformed, empty, and swapped tenant IDs; graph testing creates identical structures in tenants A and B, traverses from A, and verifies that zero nodes from B appear even when edge IDs collide. **Phase:** MVP.

NFR9: Product services retrieve embedding-provider and other application runtime secrets exclusively through the DAPR Secrets API, backed by OpenBao in Aspire and deployed environments. Secret values are never stored in application configuration or ordinary environment variables. Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or direct pod inputs outside the DAPR secret-store boundary. Verification uses structural dependency tests, secret scanning, AppHost topology tests, and integration tests proving DAPR reads from OpenBao without secret disclosure. **Phase:** Ongoing.

NFR10: All inter-service communication must be authenticated via DAPR API tokens, verified through DAPR configuration validation. **Phase:** Ongoing.

NFR11: External access must be authenticated at the ingress layer, with no unauthenticated access to REST API endpoints, verified by integration tests with unauthenticated requests. **Phase:** P1.5.

NFR12: The system supports linear scaling of tenants: adding a tenant must not degrade existing-tenant performance by more than 5%. Validation uses 10 tenants with 100K memory units each by benchmarking tenant 1 alone, adding nine loaded tenants, re-benchmarking tenant 1, and measuring the delta. **Phase:** Ongoing.

NFR13: Per-tenant ingestion pipelines must scale independently so one tenant's batch ingestion does not block another tenant's real-time ingestion, verified by concurrent ingestion across three tenants. **Phase:** Ongoing.

NFR14: Redis memory footprint per memory unit must be predictable and documented so operators can estimate infrastructure costs before tenant provisioning. The target is a published sizing guide covering memory per unit by vector dimension and metadata size. **Phase:** Ongoing.

NFR15: The architecture must not preclude backend migration from Redis to Qdrant: use a concrete implementation with clear extraction points identified and no premature interfaces. Verification is an architecture review confirming documented extraction points and no tight coupling to Redis-specific APIs in domain logic. **Phase:** Ongoing.

NFR16: There must be zero memory-unit loss during Redis restart, with AOF persistence enabled and verified. **Phase:** MVP.

NFR17: Ingestion pipeline state must survive process restarts so queued and in-progress units resume without data loss, verified through DAPR actor state persistence. **Phase:** MVP.

NFR18: Partial backend failure, with one of three backends down, must result in degraded service rather than total failure; available axes continue serving results. Verification is a chaos test that kills each backend individually and confirms partial results. **Phase:** Ongoing.

NFR19: Failed ingestion units must never be silently dropped; all failures must be visible through CLI status with error details and failure stage. Verification is an end-to-end test with intentional failure at every pipeline stage. **Phase:** Ongoing.

NFR20: MCP tool responses must conform to the MCP protocol specification with valid tool schemas, typed parameters, and structured error responses, verified by an MCP protocol conformance suite. **Phase:** P1.5.

NFR21: DAPR pub/sub integration must handle CloudEvents envelope format so events from any DAPR-compatible publisher are processable, verified with standard CloudEvents payloads. **Phase:** P1.5.

NFR22: Embedding-provider integration must handle rate limiting gracefully: HTTP 429 responses trigger backoff without pipeline crash or data loss, verified by rate-limit simulation per provider. **Phase:** Ongoing.

NFR23: CLI must connect to the memory server through a configurable endpoint supporting local development (localhost), container (Docker service name), and remote (ingress URL) environments, verified by configuration-layering tests across all three environments. **Phase:** Ongoing.

NFR24: Hybrid fusion must use deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0; single-axis explain must still document axis-specific score semantics. Verification uses fusion and explain unit tests with known rankings and weights. **Phase:** MVP.

NFR25: Fusion must produce deterministic scores: the same query against the same data produces identical composite scores, while result ordering within the same score tier may vary. Verification runs 100 repeated queries with zero score variance. **Phase:** MVP.

NFR26: The benchmark suite must produce reproducible results: two runs against the same dataset yield identical NDCG@10 scores, verified in CI. **Phase:** MVP.

NFR27: Logging must be structured JSON with OpenTelemetry correlation IDs from DAPR trace context, verified through log-format validation. **Phase:** Ongoing.

NFR28: Trace context must propagate across all DAPR service-invocation hops, producing an end-to-end trace from CLI/MCP through server to backend, verified by a distributed trace completeness test. **Phase:** Ongoing.

NFR29: Custom metrics must be exported via OpenTelemetry for ingestion throughput, search latency per axis, index size per tenant, and pipeline queue depth. The Aspire dashboard must show every metric during local development. **Phase:** Ongoing.

NFR30: Every CLI command must include `--help` with at least one usage example, verified by parsing all commands and checking for an example. **Phase:** MVP.

NFR31: The README must include a working quickstart that completes in <30 minutes on a clean machine with Docker installed, verified by a timed walkthrough. **Phase:** MVP.

**Total NFRs: 31**

### Additional Requirements

- The three-axis thesis is governed by a hard kill switch: on a benchmark of 5–10 queries requiring all three axes, hybrid retrieval must outperform every single-axis alternative on at least 80% of queries. Ground truth is defined by Jerome and two independent reviewers, NDCG@10 is the automated metric, human review resolves scoring disputes, and inter-rater agreement must reach at least 80% for validity.
- The MVP go/no-go decision requires all three hard gates: three-axis validation at 80%, zero cross-tenant leaks, and onboarding from `dotnet add package` to first search result in under 30 minutes. At least two of three soft gates must also pass: causal-chain completeness ≥95%, MCP end-to-end operation, and correct case scoping.
- Implementation sequencing requires a buildable scaffold/AppHost/ServiceDefaults and a minimum build/test feedback path before ingestion or storage work; tenant provisioning, minimal case bootstrap, and fail-before-write tenant/case guards precede every ingestion, indexing, search, and graph path. Search axes are implemented independently before the fusion spike.
- Phase 1 is a seven-feature proof of thesis. EventStore integration, MCP, and expanded CLI capabilities are a committed Phase 1.5 within four weeks of thesis validation; if that commitment slips, MCP returns to MVP. Discussion threads, diffs, REST UI support, briefing, and embedding migration are Phase 2; storage tiers, deduplication, advanced knowledge features, alternate backends, UI views, ACL/redaction, geographic controls, encryption, and formal compliance evidence are Phase 3.
- Tenant isolation is physical across RediSearch, vector indexes, FalkorDB graphs, pipeline actors, and access boundaries. Tenant deletion removes that tenant's indexes, graph data, and memory units, while cross-references held by other tenants remain an application responsibility that documentation must disclose.
- Memories is interpretive infrastructure: it owns embedding, causal-structure, confidence, ordering, edge-type, and gap accuracy; applications own decisions and legal compliance; LLMs own narrative quality.
- The shared `Contracts.V1` Evidence Packet must carry tenant/case scope, result state, source/origin attribution, composite and per-axis relevance scores, token-budget omissions with deterministic expansion handles, degraded-axis disclosure, and recovery guidance consistently across CLI JSON, MCP, and future web UI surfaces.
- Search confidence represents relevance, not factual accuracy or completeness. That caveat must appear in API documentation, every CLI explain result, the compliance guide, and MCP schema documentation. Metadata confidence is separate, field-specific, and records human-declared versus AI-inferred origin.
- Every memory unit requires `ingested_by` provenance. Causal traversal requires ordered timestamped nodes, typed directional edges, edge confidence, and explicit missing-node markers. The minimum edge taxonomy is `caused_by`, `correlated_with`, `references`, `contains`, and `annotates`; inferred confidence is never automatically promoted.
- Access telemetry is infrastructure telemetry rather than a tamper-evident compliance audit trail. Compliance documentation must explain this boundary, tenant-erasure mapping, cross-reference limitations, segregation, lineage, auditor-facing posture, and include the prescribed legal disclaimer.
- The project is Apache 2.0 and the README must make the stated license-continuity commitment. Deployment documentation must disclose Redis Stack's managed-service restriction. The FalkorDB network boundary, pinning policy, and future `IMemoryGraph`/`IMemoryIndex` extraction points are licensing-risk controls.
- The current distribution contains nine published packages and three non-packable service/orchestration projects; `tools/release-packages.json` is authoritative. The Server directly declares backend clients and must not treat the compatibility-only Redis package as a transitive facade.
- Aspire owns topology. Internal service calls use DAPR, external consumers use infrastructure-managed ingress, serialization is JSON, internal calls use DAPR API tokens, external access uses ingress authentication, and tenant context is explicit and server-validated.
- Runtime application secrets must come exclusively from DAPR Secrets API backed by OpenBao. Configuration fallback must not resolve secret values; local bootstrap and unavoidable platform bootstrap inputs are narrowly excepted.
- Ingestion is a durable per-tenant actor-managed bounded pipeline with `queued`, `extracting`, `embedding`, `indexing`, `indexed`, and `failed` states, backpressure, throttling, exponential retry, persisted progress, and visible dead-letter details. The indexing stage is described as atomic across three backends.
- Google `text-embedding-004` at 768 dimensions is the MVP provider. Provider/model/dimension/API-key reference/rate limit are tenant-scoped; changing vector dimensions requires a full tenant reindex; shared provider keys remain a cross-tenant resource bottleneck despite tenant-local throttles.
- CLI is the operational superset, supports human, JSON, and table output, and uses precedence of flags, `HEXALITH_MEMORIES_*` environment variables, config files, DAPR/OpenBao secrets, then DAPR non-secret configuration. MCP exposes search, ingestion, traversal, and case information only.
- Required developer enablement includes three numbered samples, README and getting-started materials, CLI examples, generated API reference, compliance and operator guides, Docker-free unit tests, Aspire/DAPR integration tests, and serialization contract tests.

### PRD Completeness Assessment

The PRD is unusually comprehensive and highly testable: it contains 74 numbered FRs, 31 numbered NFRs, explicit success thresholds, validation methods, phased scope, failure behavior, interface mappings, trust semantics, and operational constraints. Its strongest areas are tenant isolation, retrieval-quality gates, deterministic fusion, causal-data correctness, graceful degradation, and evidence transparency.

The following clarity gaps should be resolved or explicitly accepted during traceability review:

1. Functional requirements are generally untagged by phase. The narrative and capability matrix defer several commands and surfaces, while broad requirements such as FR53 say all retrieval and ingestion capabilities are available through CLI. This can cause MVP acceptance to absorb Phase 1.5 or Phase 2 behavior accidentally.
2. The technical matrix states .NET 10 / C# 13, while the active repository baseline requires C# 14+. The PRD should identify the authoritative language version.
3. Case membership (FR28–FR29), mandatory user/system provenance (FR65), and access logging (FR67) require identity semantics, yet the service communication model says per-user identity is not in MVP and tenant-level isolation is sufficient. The source and trust boundary for member and `ingested_by` identities are under-specified.
4. The pipeline table calls the three-backend indexing write atomic, but FR13 permits rollback or retry to converge after partial failure. Because the stores do not share a transaction, the intended consistency state machine and observable completion contract need one authoritative definition.
5. External ingress authentication is P1.5 in NFR11, while the MVP CLI uses a direct HTTP/ingress adapter. The MVP authentication boundary and acceptable local-only assumptions are not explicit.
6. The PRD has no explicit accessibility or usability NFRs. This may be acceptable for the CLI-first MVP, but it leaves later web-interface quality without measurable requirements.
7. Access telemetry is explicitly not retention-compliant, but no numbered requirement establishes retention, TTL, ownership, or a time-bounded accepted-debt decision. This is a known residual rather than a closed compliance capability.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The canonical FR Coverage Map in `epics.md` contains exactly one entry for every PRD identifier from FR1 through FR74. It contains no duplicate, missing, or out-of-range FR identifiers.

| FR range | Primary epic coverage | Principal story path |
| --- | --- | --- |
| FR1, FR4-FR7, FR13, FR46, FR65, FR68 | Epic 1 | Stories 1.2-1.7 |
| FR14-FR19, FR22, FR24-FR25, FR63 | Epic 2 | Stories 2.1-2.8 |
| FR20-FR21, FR26-FR37 | Epic 3, with FR26 also in Epic 0 | Stories 0.2 and 3.1-3.6 |
| FR47-FR52 | Epic 4 | Stories 4.1-4.3 |
| FR38-FR45, FR66, FR69-FR70 | Epic 5, with FR38/FR44 also in Epic 0 | Stories 0.1, 0.3, and 5.1-5.6 |
| FR2-FR3, FR8-FR12 | Epic 6 | Stories 6.1-6.4 |
| FR53, FR55-FR57, FR64, FR67 | Epic 7 | Stories 7.1-7.5 |
| FR72-FR74 | Epic 8 | Stories 8.1-8.2 |
| FR59-FR62 | Epic 9 | Stories 9.1-9.3 |
| FR23, FR54, FR58 | Epic 10 | Stories 10.1-10.2 |
| FR71 | Epic 26 in the map; full export remains the reserved Story 8.3 Phase 2 placeholder | Story 26.2 covers backup/restore only; Story 8.3 is not registered |

Post-MVP Epics 18, 20-24, 26-29 reinforce selected FRs but do not replace the primary implementation paths above. Epic 17 covers UX design requirements rather than additional PRD FR identifiers.

### Coverage Matrix

| FR | PRD requirement | Epic/story coverage | Status |
| --- | --- | --- | --- |
| FR1 | Ingest local files into a case | Epic 1 / Story 1.6 | ✓ Covered |
| FR2 | Ingest URLs into a case | Epic 6 / Story 6.1 | ✓ Covered |
| FR3 | Batch-ingest a directory into a case | Epic 6 / Story 6.1 | ✓ Covered; phase conflict |
| FR4 | Extract plain text, PDF, and Markdown | Epic 1 / Story 1.3 | ✓ Covered |
| FR5 | Generate embeddings through a configurable provider | Epic 1 / Stories 1.4 and 1.7 | ✓ Covered |
| FR6 | Make a unit searchable across all axes after ingestion | Epic 1 / Stories 1.5 and 1.6; Epic 23 / Story 23.1 | ✓ Covered |
| FR7 | Attach metadata with origin and confidence | Epic 1 / Stories 1.2 and 1.6 | ✓ Covered |
| FR8 | Manage ingestion load independently per tenant | Epic 6 / Story 6.2 | ✓ Covered |
| FR9 | Retry failed ingestion with configurable limits | Epic 6 / Story 6.3 | ✓ Covered |
| FR10 | View per-case ingestion state counts | Epic 6 / Story 6.3 | ✓ Covered |
| FR11 | View failed units with stage and error details | Epic 6 / Story 6.3 | ✓ Covered |
| FR12 | Re-ingest failed or prior content singly or in bulk | Epic 6 / Story 6.3; Epic 23 / Story 23.4 | ✓ Covered |
| FR13 | Recover from partial backend writes | Epic 1 / Story 1.6; Epic 21 / Story 21.2 | ✓ Covered |
| FR14 | Syntactic tenant search | Epic 2 / Story 2.1 | ✓ Covered |
| FR15 | Semantic tenant search | Epic 2 / Story 2.2 | ✓ Covered |
| FR16 | Graph-based tenant search | Epic 2 / Story 2.3 | ✓ Covered |
| FR17 | Hybrid fusion search | Epic 2 / Story 2.5 | ✓ Covered |
| FR18 | Select search axes | Epic 2 / Story 2.5 | ✓ Covered |
| FR19 | Explain per-axis scores and normalization | Epic 2 / Story 2.6 | ✓ Covered |
| FR20 | Filter search by case | Epic 3 / Story 3.4 | ✓ Covered |
| FR21 | Filter search by metadata | Epic 3 / Story 3.4 | ✓ Covered |
| FR22 | Paginate search results | Epic 2 / Story 2.6; Epic 22 / Stories 22.1 and 22.3 | ✓ Covered |
| FR23 | Constrain responses by token budget | Epic 10 / Story 10.2 | ✓ Covered |
| FR24 | Return source identifier and type | Epic 2 / Stories 2.1, 2.2, and 2.6 | ✓ Covered |
| FR25 | Benchmark hybrid versus single axes | Epic 2 / Story 2.8 | ✓ Covered |
| FR26 | Create a case within a tenant | Epic 0 / Story 0.2; Epic 3 / Story 3.1 | ✓ Covered |
| FR27 | Delete a case and its units | Epic 3 / Story 3.5 | ✓ Covered |
| FR28 | Add case members | Epic 3 / Story 3.3 | ✓ Covered |
| FR29 | Remove case members | Epic 3 / Story 3.3 | ✓ Covered |
| FR30 | List tenant cases | Epic 3 / Story 3.1 | ✓ Covered |
| FR31 | View case status and health | Epic 3 / Story 3.2 | ✓ Covered |
| FR32 | Enforce strict single-case ownership | Epic 0 / Story 0.2; Epic 3 / Story 3.1 | ✓ Covered |
| FR33 | Maintain case-scoped graph edges | Epic 3 / Story 3.1 | ✓ Covered |
| FR34 | Search across tenant cases with attribution | Epic 3 / Story 3.4; Epic 22 / Story 22.4 | ✓ Covered |
| FR35 | Delete an individual memory unit | Epic 3 / Story 3.5 | ✓ Covered |
| FR36 | View recent case activity | Epic 3 / Story 3.2 | ✓ Covered |
| FR37 | Annotate or correct memory units | Epic 3 / Story 3.6 | ✓ Covered |
| FR38 | Create physically isolated tenant indexes | Epic 0 / Story 0.1; Epic 5 / Story 5.1; Epic 24 / Story 24.3 | ✓ Covered |
| FR39 | Delete tenant data across all backends | Epic 5 / Story 5.2; Epic 21 / Story 21.5 | ✓ Covered |
| FR40 | Verify tenant isolation | Epic 5 / Story 5.3; Epic 24 / Story 24.3 | ✓ Covered |
| FR41 | List tenants | Epic 5 / Story 5.5 | ✓ Covered |
| FR42 | Update tenant configuration | Epic 5 / Story 5.5 | ✓ Covered |
| FR43 | Guard inconsistent configuration changes | Epic 5 / Story 5.5 | ✓ Covered |
| FR44 | Enforce tenant context at all access layers | Epic 0 / Story 0.3; Epic 5 / Story 5.4; Epic 20 / Story 20.2 | ✓ Covered |
| FR45 | View tenant configuration | Epic 5 / Story 5.5 | ✓ Covered |
| FR46 | Index causation and correlation as typed edges | Epic 1 / Story 1.5 | ✓ Covered |
| FR47 | Traverse causal chains with depth | Epic 4 / Story 4.1 | ✓ Covered |
| FR48 | Filter traversal by edge type | Epic 4 / Story 4.2 | ✓ Covered |
| FR49 | Return explicit missing-node gap markers | Epic 4 / Story 4.3 | ✓ Covered |
| FR50 | Support the minimum edge taxonomy | Epic 4 / Story 4.2 | ✓ Covered |
| FR51 | Promote inferred-edge confidence explicitly | Epic 4 / Story 4.3 | ✓ Covered |
| FR52 | Preserve causal chronology and timestamps | Epic 4 / Stories 4.1 and 4.3 | ✓ Covered |
| FR53 | Offer all retrieval and ingestion through CLI | Epic 7 / Story 7.1 | ⚠ Partial: ACs cover MVP subset; remaining Phase 1.5 CLI surface has no complete registered story path |
| FR54 | Offer search, ingestion, traversal, and case info through MCP | Epic 10 / Story 10.1 | ✓ Covered |
| FR55 | Support human, JSON, and table CLI output | Epic 7 / Story 7.2 | ✓ Covered |
| FR56 | Give actionable CLI errors | Epic 7 / Story 7.3 | ✓ Covered |
| FR57 | Make next actions discoverable in every state | Epic 7 / Stories 7.3 and 7.4 | ✓ Covered |
| FR58 | Give MCP tools typed descriptive schemas | Epic 10 / Story 10.1 | ✓ Covered |
| FR59 | Auto-discover DAPR-published event types | Epic 9 / Story 9.1 | ✓ Covered |
| FR60 | Generate raw and natural-language event embeddings | Epic 9 / Story 9.2 | ✓ Covered |
| FR61 | Auto-index causal metadata without mappings | Epic 9 / Story 9.2 | ✓ Covered |
| FR62 | List handlers and detect registration mismatches | Epic 9 / Story 9.3; Epic 16 / Story 16.1 | ✓ Covered |
| FR63 | Return composite confidence and per-axis breakdown | Epic 2 / Stories 2.6 and 2.7 | ✓ Covered |
| FR64 | Track and display metadata origin/confidence | Epic 1 / Story 1.2; Epic 7 / Story 7.2 | ✓ Covered |
| FR65 | Require `ingested_by` provenance | Epic 1 / Stories 1.2 and 1.6 | ✓ Covered |
| FR66 | Return partial results with excluded-axis disclosure | Epic 5 / Story 5.6 | ✓ Covered |
| FR67 | Log tenant-scoped search and access events | Epic 7 / Story 7.5; Epic 20 / Story 20.5; Epic 27 | ✓ Covered; retention residual remains open |
| FR68 | Configure embedding provider/model per tenant | Epic 1 / Story 1.7; Epic 13 | ✓ Covered |
| FR69 | Enforce per-tenant embedding rate ceilings | Epic 5 / Story 5.5; Epic 6 / Story 6.2 | ✓ Covered |
| FR70 | Track provider/model used for vectors | Epic 5 / Story 5.5 | ✓ Covered |
| FR71 | Export all units, metadata, and graph edges portably | FR map points to Epic 26; Story 26.2 covers backup/restore; full Story 8.3 remains reserved and unregistered | ⚠ Partial/deferred |
| FR72 | Expose readiness/liveness for all backends | Epic 8 / Story 8.1 | ✓ Covered |
| FR73 | Detect index/graph divergence | Epic 8 / Story 8.2 | ✓ Covered |
| FR74 | Repair index/graph divergence | Epic 8 / Story 8.2 | ✓ Covered |

### Missing Requirements

No PRD FR is wholly absent from the epic-level FR Coverage Map, and no epic FR identifier falls outside FR1-FR74. Two requirements lack a complete, currently actionable story-level path:

#### Critical Partial Coverage

**FR53: Developer can interact with all retrieval and ingestion capabilities via CLI.**

- **Impact:** Story 7.1's acceptance criteria explicitly limit MVP to `ingest`, `search`, `case`, `tenant`, and benchmark groups. `explore`, `status`, `handlers`, `quickstart`, and batch-directory polish are described as Phase 1.5, but the document does not register a complete Phase 1.5 CLI story that closes the broad “all capabilities” requirement. The headline coverage therefore overstates the accepted surface.
- **Recommendation:** Split FR53 by phase or register a Phase 1.5 CLI-completion story with explicit command-by-command acceptance criteria, including traversal, handler diagnostics, status, batch-directory ingestion, and quickstart behavior.

#### High-Priority Partial Coverage

**FR71: Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format.**

- **Impact:** The FR map assigns FR71 to Epic 26, but Story 26.2 is a restore/fidelity story that presupposes export and explicitly represents the operational backup/restore slice. The full application-facing export is only a reserved Story 8.3 placeholder with an activation rule; it has no registered story file or sprint-status owner.
- **Recommendation:** Keep FR71 explicitly Phase 2 and register Story 8.3 before implementation, or narrow FR71 to the backup/restore contract that Epic 26 actually owns. Do not count the current placeholder as implementation-ready coverage.

### Coverage Observations

- FR3 is implemented by Story 6.1 inside the MVP epic set, while Epic 7 repeatedly says batch-directory ingestion is Phase 1.5 polish. The implementation path exists, but its readiness phase is contradictory.
- “100% traceable” in `epics.md` is true only at the identifier-to-epic-map level. It is not equivalent to 100% active MVP scope or 100% complete story-level coverage.
- FR67 has implementation coverage for event emission, but its bounded retention/TTL lifecycle remains explicitly open under Epic 27 and the `20.5-A41-ACCESS-TELEMETRY-RETENTION` residual.

### Coverage Statistics

- Total PRD FRs: 74
- Unique FRs claimed in the epic coverage map: 74
- Fully covered by an actionable story path: 72
- Partially covered or deferred without a complete registered story path: 2
- Completely absent FRs: 0
- Extra epic FR identifiers not present in the PRD: 0
- Strict story-level coverage: 97.3% (72/74)
- Identifier-to-epic traceability: 100% (74/74)

## UX Alignment Assessment

### UX Document Status

A complete whole-document UX specification is present. It defines the experience strategy, user journeys, cross-surface Evidence Packet, trust-state grammar, reusable components, responsive behavior, accessibility expectations, interaction patterns, and validation criteria for CLI, MCP, and the future web surface. It also follows the repository's mandatory FrontComposer and Microsoft Fluent UI Blazor V5 rules.

### UX ↔ PRD Alignment

The UX specification is strongly aligned with the PRD's product intent:

- The Alex, LLM-agent, Kenji/Priya, and Marcus journeys correspond to the PRD's developer, agent, operator, and platform-integration workflows.
- The Evidence Packet directly operationalizes tenant/case scope, provenance, source citation, freshness, confidence, per-axis reasoning, graph gaps, partial/degraded states, and recovery guidance from FR19, FR23, FR24, FR44, FR54, FR58, FR63, FR64, and FR66.
- The trust strip, scope header, source stack, graph summary, and recovery actions consistently preserve the PRD's fail-closed tenant boundary and “useful under partial failure” behavior.
- The CLI-first MVP, MCP/EventStore Phase 1.5, and future web sequencing broadly match the PRD roadmap.
- Responsive layouts, keyboard operation, screen-reader behavior, reduced-motion support, forced-colors support, and WCAG 2.2 AA targets make the future web experience testable rather than aspirational.

The main PRD-side gap is traceability, not product contradiction. The PRD has no numbered usability or accessibility NFRs and does not canonically own many of the UX constraints later expressed as UX-DRs in `epics.md`. Mandatory trust-strip contents, the complete state grammar, responsive breakpoints, 44-pixel targets, keyboard/focus rules, assistive-technology coverage, and FrontComposer/Fluent conformance therefore lack direct PRD requirement identifiers.

### UX ↔ Architecture Alignment

The architecture supports the UX's central model:

- `Contracts.V1` owns a shared Evidence Packet envelope for CLI JSON, MCP, and future web composition, including scope, sources, evidence, graph context, state, omitted details, deterministic expansion handles, and recovery actions.
- The architecture explicitly defines `Hexalith.Memories.Web` as a future FrontComposer-aligned Razor component library that uses Microsoft Fluent UI Blazor V5 and prohibits a standalone design system, raw control library, or theme fork.
- Capability alignment rather than literal feature parity is an explicit architectural rule, consistent with the UX's surface-specific presentation strategy.
- Tenant authorization, partial-backend degradation, optional graph participation, provenance, and actionable errors provide the backend semantics required by the trust-oriented UX.

However, the web architecture remains a boundary statement rather than an implementation design. It does not define the web project's composition topology, render mode, state-management boundary, localization approach, component ownership, browser-side security behavior, or mapping from the UX component catalog and UX-DRs to architectural components. It also provides backend search-latency targets but no browser rendering or interaction-performance budgets.

### Alignment Issues

#### High: UX Requirements Are Not Fully Traceable to the PRD

The UX specification and epic UX-DR map contain implementation-significant requirements that are absent from the PRD's numbered requirements. This weakens change control and makes it unclear which accessibility and interaction rules are release gates.

**Recommendation:** Add a canonical PRD usability/accessibility NFR group or explicitly incorporate the UX-DR catalog by reference, then map each UX-DR to the owning story and verification level.

#### High: Future Web Architecture Is Under-Specified

The architecture selects FrontComposer, Fluent UI Blazor V5, `Contracts.V1`, and an RCL boundary, but it stops short of a buildable web design. Epic 17 can establish the shell and components, yet implementers must still infer composition, rendering, state, localization, authorization projection, and component-to-contract ownership.

**Recommendation:** Before activating Epic 17, add a web architecture section and project map covering those decisions, plus explicit UX-DR-to-component and Evidence-Packet-field-to-view-model mappings.

#### Medium: UI Performance Has No Architectural Budget

The UX defines responsive behavior and test viewport sizes, while the PRD and architecture quantify only backend/query latency. No limit exists for initial render, interaction response, layout stability, payload size, or large evidence/graph rendering.

**Recommendation:** Define measurable web performance budgets before web implementation and add representative evidence-packet and graph-size test fixtures.

#### Medium: Phase Terminology Can Be Misread

The UX uses “Phase 1/2/3” for component delivery while also describing the product's CLI-first MVP, Phase 1.5, and future web roadmap. Those labels can be mistaken for the product phases even though web delivery is future scope.

**Recommendation:** Rename the UX component phases to “Web UX Wave 1/2/3” and state the product-phase activation gate for each wave.

#### Medium: Isolation Claims Must Reflect Verified Runtime State

The UX calls for visible physical-isolation assurance. The architecture identifies per-tenant Redis ACL users and tenant-scoped backend routing as the target, while noting that full provisioning and migration remain follow-up enforcement work. A UI could otherwise present a stronger assurance than the deployed system has earned.

**Recommendation:** Bind the scope/isolation status to runtime verification evidence and distinguish `target`, `configured`, `verified`, and `degraded/unknown` states. Never infer “verified” from tenant identifiers alone.

#### Low: Cross-Surface Equivalence Needs Phase Gates

The UX appropriately aims for equivalent Evidence Packet fields across CLI, MCP, and web, but those surfaces ship in different phases and the architecture intentionally promises capability alignment rather than feature parity. Acceptance criteria should not imply simultaneous availability.

**Recommendation:** Express equivalence per capability and per active product phase, with contract compatibility tests activated only when each surface is registered.

### Warnings

- The UX is suitable as a design source, but its accessibility and responsive rules are not presently first-class PRD release requirements.
- The architecture's early `.NET 10 / C# 13` constraint is stale relative to the repository's active C# 14 baseline. The architecture also does not pin the current FrontComposer/Fluent UI V5 dependency facts needed for future web implementation.
- The Evidence Packet architecture covers the main UX grammar, but detailed UI states such as freshness categories, evidence-health categories, focus behavior, and accessible recovery announcements remain owned only by UX/epic material.

## Epic Quality Review

### Review Scope and Overall Structure

The review covered all 32 epics and all 166 registered story definitions in `epics.md`. Every registered story uses an `As a / I want / So that` structure. The document contains 561 `Given` clauses, 552 `When` clauses, and 561 `Then` clauses; most acceptance criteria are concrete and independently verifiable. Epic 0 also provides the required early project scaffold and CI preflight, and tenant-owned backend resources are created by the tenant-provisioning capability rather than by a generic “create every table” story.

The strongest product epics are Epics 1-10 and 20-24: their goals name a developer, agent, or operator outcome; dependencies normally point backward to already-delivered capabilities; and their criteria usually specify happy paths, errors, tenancy, recovery, and test evidence. The explicit separation of active MVP scope (Epics 0-8), Phase 1.5, future web, remediation, and operational work is also useful.

The corpus nevertheless mixes a greenfield implementation plan, completed implementation history, review findings, supersession records, current backlog, and external activation gates in one file. A story being historically `done` does not make its original shape a valid implementation-ready template, so historical defects are distinguished below from current blockers.

### Per-Epic Compliance Summary

| Epic | User/Operator Value | Independence and Sequence | Story Quality | Verdict |
|---:|---|---|---|---|
| 0 | Mixed safety and developer-environment value | Correct prerequisite path before Epic 1 | Cohesive safety slices, but scaffold and CI are enabling work | Concern |
| 1 | Clear ingestion/search value | Uses only Epic 0 | Stories 1.5 and 1.6 are acknowledged historical oversized bundles | Major historical issue |
| 2 | Clear search and benchmark value | Uses Epic 1 outputs | Strong, testable decomposition | Pass |
| 3 | Clear case-management value | Uses prior ingestion/search | Independent vertical capabilities | Pass |
| 4 | Clear causal-traversal value | Correctly consumes Epic 1 graph data | Strong BDD coverage | Pass |
| 5 | Clear operator multi-tenancy value | Deepens the earlier minimum safely | Strong negative and failure coverage | Pass |
| 6 | Clear ingestion-resilience value | Uses prior pipeline | Appropriate slices | Pass |
| 7 | Clear CLI value | Uses prior capabilities | Broad FR53 remains only partially closed | Major scope issue |
| 8 | Clear operator health value | Uses prior runtime capabilities | Story 8.5 is an acknowledged four-deliverable bundle | Major historical issue |
| 9 | Clear zero-code integration value | Additive after MVP | Cohesive stories | Pass |
| 10 | Clear LLM-agent value | Additive after search/graph | Cohesive stories | Pass |
| 11 | Delivery-enablement value | No forbidden future dependency after Story 0.4 correction | Epic is a technical milestone rather than a product capability | Standards violation |
| 12 | Clear release/operator outcome | Uses completed CI | Several criteria allow alternate disposition rather than one result | Major issue |
| 13 | Clear provider-migration outcome | Proper backward sequence | Stories 13.2 and 13.7 are broad but checkpointed | Concern |
| 14 | Primarily a collection of deferred technical findings | Independent only as a governance container | Not a coherent user-value epic | Standards violation |
| 15 | Primarily carry-forward risk processing | Uses prior findings | Story 15.6 bundles 15 independent findings | Critical shape violation |
| 16 | Operator registry-verification outcome | Uses Story 9.3 | Decision/disposition criteria can close without delivered behavior | Major issue |
| 17 | Clear future-web user value | Circular/non-monotonic execution gate | Story 17.7 is large; sequence is not independently executable | Critical sequence violation |
| 18 | Clear downstream-consumer value | 18.5 requires 18.6; execution map compensates | Mostly bounded contract slices | Major sequence issue |
| 19 | Deferred-register administration, not product value | Uses prior ledger | Criteria classify work instead of delivering a capability | Standards violation |
| 20 | Clear security/operator value | Independent remediation slices | Concrete negative/security criteria | Pass |
| 21 | Clear integrity/operator value | Decision-first Story 21.1 correctly precedes code | Story 21.9 is checkpoint-heavy but has a story-file evidence table | Concern |
| 22 | Clear retrieval value | Uses completed search | Stories 22.1 and 22.7 permit ambiguous or bundled outcomes | Major issue |
| 23 | Clear ingestion value | 23.1 requires later-numbered 23.9 | Execution map mitigates but does not remove forward-key dependency | Major sequence issue |
| 24 | Clear observability/operator value | Uses prior runtime | Story 24.3 decides isolation but does not own full enforcement | Major outcome gap |
| 25 | Predominantly internal refactoring/code health | Uses existing product | Technical epic; Story 25.7 is misplaced user-facing UX work | Standards violation |
| 26 | Clear deploy/recover/operator value | Uses prior system | Story 26.5 is checkpoint-heavy; FR71 is narrower than its headline | Major issue |
| 27 | Clear telemetry-lifecycle/operator value | Depends on 24 unregistered future C1 owners | Criteria and ownership are not implementation-ready | Critical blocker |
| 28 | Internal dependency adoption with audit value | External authorization is now satisfied | Technically focused but bounded | Concern |
| 29 | Clear secret-management/operator value | 29.2 correctly consumes 29.1 | Concrete and sequential | Pass |
| 30 | Primarily CI/CD engineering | Requires unavailable external workflow capabilities and non-numeric order | Current backlog cannot execute independently | Critical blocker |
| 31 | Clear security/operator value | 31.2 consumes 31.1 checkpoints | Unassigned external countersignature prevents full epic closure | Major blocker |

### Critical Violations

#### Epic 27 Has Unregistered Forward Dependencies

Story 27.4 cannot start until twenty-five C1 successor stories have passed. Only Story 27.21 is registered, it is still `backlog`, and the other twenty-four gates are explicitly held without registered story owners. Story 27.4 also names future Stories 27.7 through 27.31 as prerequisites even though those story files and registrations do not exist. This is the exact prohibited pattern of a story depending on undefined future work.

The planning artifacts also disagree about predecessor state: `epics.md` says Story 27.3 cannot enter review until five Story 27.2 gaps are closed and C0 is re-reviewed, while the Story 27.3 implementation artifact records C0 as reopened and re-closed. That contradiction prevents a reliable selection decision.

**Remediation:** Do not select Story 27.4. Reconcile C0 status in the canonical planning document, create one bounded registered story per remaining C1 gate with owner and executable producer, and place those stories before a much smaller close-out story. A mere execution-order override is insufficient because the missing stories have no implementable definitions.

#### Epic 17 Contains a Circular Execution Contract

The execution map puts Story 17.6 first as the conformance preflight and Story 17.7 second. Story 17.6, however, requires the existing RCL from Story 17.1 and includes an acceptance criterion conditioned on Stories 17.2-17.5 being implemented. Story 17.7 then depends on Story 17.6. A clean implementation cannot satisfy that declared order:

`17.6 preflight → 17.7 → 17.2-17.5`, while `17.6` itself consumes `17.1` and `17.2-17.5`.

All rows are historical `done`, but this remains a critical defect in the reusable plan and explains why `story_execution_order` cannot be treated as a substitute for independent story design.

**Remediation:** Rewrite the sequence as a small initial conformance harness, component stories, and a final conformance/browser gate. Move “Stories 17.2 through 17.5 are implemented” out of the preflight and into the final validation story.

#### Epic 30 Is Not Independently Implementable

Stories 30.3 and 30.4 are explicitly blocked on future owner-approved Hexalith.Builds capabilities that the document says do not currently exist. Story 30.1 also relies on exact-source CI while `story_execution_order` places later-numbered Story 30.2 first to provide it. The epic therefore depends on external future work and a non-monotonic internal sequence.

**Remediation:** Register the required Hexalith.Builds work with an owner and accepted contract before selecting Epic 30, or define a locally implementable adapter slice. Renumber or replace the 30.2-before-30.1 workaround so each story consumes only earlier story output.

#### Technical/Process Epics Violate the User-Value Standard

Epics 14, 15, 19, and 25 are primarily containers for deferred findings, risk disposition, register hygiene, or internal factorization. Their work can be valuable, but as written they are technical/process milestones rather than independently valuable user outcomes. Epic 11 and Epic 30 have the same issue at the CI/CD level, though they do provide maintainer safety. Story 25.7 is a user-facing UX correction embedded inside an otherwise internal refactoring epic.

**Remediation:** Track pure engineering work as enabling work outside the product-epic hierarchy, or regroup it beneath explicit operator/developer outcomes. Move Story 25.7 under the web UX outcome. Do not use “hardening,” “code health,” or “deferred closure” as the sole epic value proposition.

### Major Issues

#### Oversized or Bundled Stories

The document itself acknowledges that Stories 1.5, 1.6, and 8.5 contain multiple independently reviewable slices and must not be reused as templates. Additional problematic bundles are:

- Story 15.6: fifteen patch findings across AppHost, health, DAPR templates, CI/build behavior, documentation, and regression coverage.
- Story 17.7: runnable host creation, route fixtures, Playwright/axe, media/layout validation, manual screen-reader evidence, artifact sanitization, and gap-ledger closure.
- Story 21.9: staging, cutover, rollback, lock ownership, TTL/heartbeat, abort, and failure validation; the separate story file mitigates this with a checkpoint table.
- Story 26.5: six independent operational runbooks; its story file contains checkpoint tables, but the planning-story outcome remains too broad.
- Story 27.3: C0 plus C2/C3/C4, a deployment lane, adapter-unit contract, manifest qualification, and extensive governance reconciliation. Criteria 6-8 are separate delivery slices.
- Story 27.4: multi-writer durability, expiry/purge, privacy/authority evidence, monitoring, incident/recovery/decommission documentation, publication verification, and A41 governance closure.

**Remediation:** Split each active or reopened bundle into vertical stories with one demonstrable outcome. Keep checkpoint tables as evidence tracking, not as a substitute for story sizing.

#### Non-Monotonic Story Dependencies

The `story_execution_order` mechanism records several exceptions but does not remove the dependency defect:

- Story 18.5 consumes Story 18.6.
- Story 23.1 consumes the batch API from Story 23.9.
- Story 30.1 effectively consumes the exact-source CI contract from Story 30.2.
- Epic 17's override is internally circular, as described above.

**Remediation:** Renumber unfinished work or introduce new correctly ordered keys. Preserve old numbers only as aliases in historical records.

#### Acceptance Criteria Permit Divergent Completion Outcomes

Several criteria do not identify one expected product outcome:

- Story 16.1 can resolve, accept, or carry forward the registry gap.
- Story 22.1 can either implement semantic pagination or reject non-zero offsets, despite the pagination requirement.
- Story 23.4 can either make non-URL re-ingestion work or reject it, despite its stated user outcome and FR12.
- Story 25.8 allows boundaries to be “fixed, hosted, or documented as intentional.”
- Story 26.3 allows empty integration stubs to be implemented or explicitly skipped.
- Several Stories 12, 14, 15, and 19 can close by implementation, documentation, acceptance, or a refreshed deferral.

Disposition work is legitimate governance, but it is not implementation acceptance. Product and remediation stories need one behavior; a separate decision story may select that behavior first.

**Remediation:** Split decision/disposition from implementation, then give the implementation story a single observable outcome. Do not count an accepted deferral or skip as delivery of the capability named by the story.

#### Physical-Isolation Enforcement Has No Complete Owning Story

Story 24.3 is titled “Physical Tenant Isolation & Verifier Scaling,” but its acceptance criteria only ratify a strategy, improve the verifier, and update architecture. The architecture says per-tenant ACL provisioning, tenant-scoped connection migration, and data migration remain follow-up enforcement work, yet no complete enforcement story is registered. This leaves the epic outcome and NFR8 assurance stronger than the owned implementation scope.

**Remediation:** Register separate decision, enforcement/migration, and runtime-negative-evidence stories. Treat physical isolation as `target` rather than `verified` until the enforcement story passes.

#### Epic 31 Has an Unowned Completion Dependency

Story 31.1 cannot reach `done` until independent security countersignature checkpoints C4b, C5b, and C7 are discharged, but the sprint record says the owner is unassigned. Story 31.2 may proceed after a subset of Story 31.1 checkpoints, so implementation can continue, but the epic cannot close predictably.

**Remediation:** Assign the independent reviewer and schedule the countersignature evidence, or explicitly mark Epic 31 blocked outside implementation readiness until that authority is available.

### Minor Concerns

- Ten stories have unequal `Given`/`When`/`Then` block counts: Stories 12.1-12.6, 17.6, 26.6, 26.7, and 27.3. Most are shorthand formatting issues, but Story 27.3 has no standard block-form BDD criteria at all; criteria 7 and 8 are declarative compound requirements.
- Architecture explicitly selects `dotnet new aspire` as the scaffold baseline. Story 0.0 provides the right boot and project outcomes but never states that the selected starter must be used or records the template/version command.
- Story numbering includes historical aliases, a reserved 8.3 slot, and several execution-order overrides. The policy explains them, but the cognitive cost is high and automated consumers must merge two documents to determine actual order.
- `sprint-status.yaml` declares `last_updated: 2026-08-02` while containing corrections dated 2026-08-03. The metadata should match the newest authoritative mutation.

### Best-Practices Checklist Result

| Check | Result |
|---|---|
| Epics deliver user/operator value | Partial — most do; Epics 14, 15, 19, and 25 are clear technical/process containers, with Epics 11 and 30 also technically framed |
| Epics avoid future dependencies | Fail — Epics 27 and 30 have current unresolved future/external gates |
| Stories are appropriately sized | Partial — most are bounded; the listed historical and active bundles are not |
| Stories avoid forward dependencies | Fail — 17.x is circular; 18.5/18.6, 23.1/23.9, 27.x, and 30.1/30.2 require overrides or undefined future work |
| Storage structures are created when first needed | Pass — tenant provisioning owns lifecycle resources for a user-visible isolation capability; no generic all-schema-upfront story was found |
| Acceptance criteria are clear and testable | Partial — generally strong, but disjunctive completion and Story 27.3 weaken determinism |
| Traceability is maintained | Partial — FR identifier mapping is complete, but FR53/FR71 and physical-isolation enforcement overstate complete story ownership |
| Starter and early CI needs are covered | Partial — Story 0.0 and Story 0.4 exist; Story 0.0 omits the architecture-selected `dotnet new aspire` command/version |

### Epic Quality Verdict

The active historical MVP story set is well specified and mostly exemplary at the acceptance-criterion level, but the combined corpus is not uniformly implementation-ready. Current selection must be gated particularly around Epic 27, Epic 30, Epic 31, full physical-isolation enforcement, FR53, and FR71. The plan should not be used as a simple numeric story queue until the critical dependency and ownership defects above are corrected.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY for unrestricted implementation.**

The foundation is strong: all four expected planning documents exist without duplication, all 74 FR identifiers map to epics, 72 FRs have complete actionable story paths, the Evidence Packet provides a coherent cross-surface contract, and most of the 166 stories have specific BDD criteria. That strength does not overcome the current blockers. The remaining plan contains unregistered future dependencies, unresolved external activation gates, incomplete story ownership for security-critical enforcement, and requirements whose claimed coverage is broader than the executable stories.

This verdict does not mean every remaining story is blocked. A separately selected story with satisfied prerequisites may proceed. It means the artifact set cannot safely be treated as an implementation-ready queue as a whole.

### Critical Issues Requiring Immediate Action

1. **Epic 27 is structurally blocked.** Story 27.4 requires twenty-five C1 successors; only Story 27.21 is registered and remains `backlog`, while twenty-four gates have no registered owner. Canonical C0 status also conflicts between planning and implementation artifacts.
2. **Epic 30 depends on unavailable external capabilities.** Stories 30.3 and 30.4 require an owner-approved Hexalith.Builds revision that the plan says does not yet exist, and Story 30.1 relies on later-numbered Story 30.2 output.
3. **Physical tenant isolation is not fully owned.** Story 24.3 ratifies a strategy and verifier but does not implement per-tenant Redis ACL provisioning, tenant-scoped connection migration, or data migration. NFR8 and UX isolation claims must not be presented as verified until enforcement and attached negative evidence pass.
4. **FR53 and FR71 are not fully implementation-ready.** FR53 lacks a complete Phase 1.5 CLI-completion story; FR71 maps to backup/restore while full portable export remains an unregistered reserved placeholder.
5. **Epic 31 cannot close predictably.** Independent security countersignature checkpoints have no assigned owner.
6. **The reusable Epic 17 sequence is circular.** Story 17.6 is declared the preflight while consuming Story 17.1 and criteria from Stories 17.2-17.5; it must be rewritten before this plan is used for analogous or reopened web work.

### Recommended Next Steps

1. **Declare the next implementation boundary.** Select one phase/epic explicitly. Keep Story 27.4 and externally gated Epic 30 stories unselectable until their prerequisites are registered and satisfied.
2. **Repair requirement ownership.** Phase-tag the FRs; split or phase-narrow FR53; either register the full FR71 export story or narrow FR71 to backup/restore; define identity/authentication semantics and the multi-store consistency state machine; update the language baseline to C# 14+; add numbered accessibility/usability requirements.
3. **Rebuild the blocked dependency chains.** Register one bounded, producer-backed owner story for every remaining Epic 27 C1 gate; reconcile Story 27.3/C0 status; assign Epic 31's independent reviewer; register the required Hexalith.Builds capability before selecting Epic 30.
4. **Register physical-isolation enforcement.** Separate decision, ACL provisioning and connection/data migration, and deployment-shaped negative verification into owned stories. Require the repository baseline's attached cross-tenant negative evidence for every scope-sensitive change.
5. **Normalize story order and size.** Correct the Epic 17 cycle and the 18.5/18.6, 23.1/23.9, and 30.1/30.2 inversions. Split active checkpoint-heavy stories, especially 27.3 and 27.4, into independently demonstrable outcomes.
6. **Complete web traceability before product-route work.** Add the web composition/rendering/state/localization/security architecture, map UX-DRs to PRD requirements and story owners, and establish measurable browser performance budgets.
7. **Re-run readiness validation.** Require zero unregistered prerequisites, one deterministic outcome per selected story, assigned evidence owners, and 100% complete story-level coverage for the selected phase before changing the verdict.

### Final Note

This assessment identified **28 actionable findings across four categories**—requirements clarity, functional coverage, UX/architecture alignment, and epic/story quality—plus three supporting warnings. The issues are concentrated rather than pervasive: correcting the blocked dependency chains, security ownership, and overclaimed requirement coverage should convert the current artifact set into a reliable implementation plan without discarding its substantial existing detail.

**Assessment date:** 2026-08-03  
**Assessor:** Codex, using the BMad Implementation Readiness workflow
