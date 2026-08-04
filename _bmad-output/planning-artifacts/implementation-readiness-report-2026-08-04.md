---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
assessmentStatus: NOT READY
filesIncluded:
  prd:
    primary:
      - _bmad-output/planning-artifacts/prd.md
    supplementary: []
  architecture:
    primary:
      - _bmad-output/planning-artifacts/architecture.md
    supplementary:
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-architecture-anchor-reverification.md
  epics:
    primary:
      - _bmad-output/planning-artifacts/epics.md
    supplementary:
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06-epic-0-evidence-map.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06-epic-17-deferred-web-triage.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06-epic17-browser-at-gap-closure.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-epic-0-evidence-map-maintenance.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-epic-26-benchmark-closure.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-epic-ac-code-verification.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-epic-ac-verification-route-and-binding-coverage.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02-epic-23-documentation-verification.md
  ux:
    primary:
      - _bmad-output/planning-artifacts/ux-design-specification.md
    supplementary:
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-04
**Project:** memories

## Document Discovery

### PRD Files Found

**Whole Documents:**

- `prd.md` (88,353 bytes, modified 2026-07-19)

**Sharded Documents:** None.

### Architecture Files Found

**Whole Documents:**

- `architecture.md` (121,376 bytes, modified 2026-08-02)
- `sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md` (10,985 bytes, modified 2026-07-16)
- `sprint-change-proposal-2026-07-28-architecture-anchor-reverification.md` (9,327 bytes, modified 2026-07-28)

**Sharded Documents:** None.

### Epics and Stories Files Found

**Whole Documents:**

- `epics.md` (382,690 bytes, modified 2026-08-03)
- `sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md` (9,747 bytes, modified 2026-06-02)
- `sprint-change-proposal-2026-07-06-epic-0-evidence-map.md` (4,623 bytes, modified 2026-07-06)
- `sprint-change-proposal-2026-07-06-epic-17-deferred-web-triage.md` (4,978 bytes, modified 2026-07-06)
- `sprint-change-proposal-2026-07-06-epic17-browser-at-gap-closure.md` (12,987 bytes, modified 2026-07-06)
- `sprint-change-proposal-2026-07-16-epic-0-evidence-map-maintenance.md` (8,531 bytes, modified 2026-07-16)
- `sprint-change-proposal-2026-07-16-epic-26-benchmark-closure.md` (17,490 bytes, modified 2026-07-16)
- `sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md` (24,777 bytes, modified 2026-07-27)
- `sprint-change-proposal-2026-07-28-epic-ac-code-verification.md` (21,436 bytes, modified 2026-07-28)
- `sprint-change-proposal-2026-07-28-epic-ac-verification-route-and-binding-coverage.md` (40,125 bytes, modified 2026-07-28)
- `sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md` (29,405 bytes, modified 2026-08-03)
- `sprint-change-proposal-2026-08-02-epic-23-documentation-verification.md` (27,427 bytes, modified 2026-08-02)

**Sharded Documents:** None.

### UX Design Files Found

**Whole Documents:**

- `ux-design-specification.md` (99,240 bytes, modified 2026-06-27)
- `sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` (17,841 bytes, modified 2026-06-27)

**Sharded Documents:** None.

### Discovery Resolution

- No whole-versus-sharded duplicate formats were found.
- No required document type is missing.
- The canonical PRD, architecture, epics, and UX files are selected as primary assessment inputs.
- Matching sprint-change proposals are selected as supplementary amendments.

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

#### Trust and Transparency

- **FR63:** System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result
- **FR64:** System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit
- **FR65:** System records `ingested_by` (user or system identity) as a mandatory field on every memory unit
- **FR66:** When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded
- **FR67:** System logs search and access events per tenant for audit purposes

#### Embedding Provider Management

- **FR68:** Operator can configure embedding provider and model per tenant
- **FR69:** System enforces per-tenant rate limit ceilings for embedding API calls
- **FR70:** System tracks the embedding provider and model used for each memory unit's vectors

#### Data Portability and System Health

- **FR71:** Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. **Phase:** Phase 2 unless a later sprint change explicitly pulls export into MVP.
- **FR72:** System exposes readiness and liveness health checks verifying all backends
- **FR73:** Operator can detect index/graph divergence via consistency check
- **FR74:** Operator can repair detected index/graph inconsistencies via consistency repair operation

**Total FRs: 74**

### Non-Functional Requirements

NFR validation phases are **MVP** (before thesis validation), **P1.5** (when EventStore and MCP ship), and **Ongoing** (as infrastructure matures).

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

- All three MVP hard gates must pass: hybrid retrieval beats every single axis on at least 80% of the benchmark set, cross-tenant leakage is zero, and onboarding completes in under 30 minutes. At least two of the three soft gates—causal-chain completeness of at least 95%, MCP end-to-end operation, and correct case scoping—must also pass.
- Implementation must establish the scaffold, build/test feedback, tenant provisioning, minimal case bootstrap, and tenant/case guards before any ingestion, indexing, search, or graph path writes data. Search axes must work independently before the fusion spike begins.
- Phase 1.5 is committed within four weeks of thesis validation; if that timeline cannot be met, MCP moves back into the MVP.
- Memories is interpretive infrastructure: it owns embedding, causal-chain, confidence, ordering, edge-type, and gap-detection accuracy, while consuming applications own decisions, legal obligations, and user-facing use.
- Tenant deletion must remove that tenant's indexes, graph data, and memory units; cross-references held by other tenants remain an application responsibility and must be disclosed.
- Access telemetry is not a tamper-evident audit trail. Compliance documentation must include the compliant-application guide, deletion-limitations section, legal disclaimer, and auditor security-posture section.
- The Evidence Packet must carry confidence breakdowns, origin attribution, omitted-detail handling, degradation state, tenant/case scope, result state, and recovery guidance consistently across CLI JSON, MCP, and future web UI surfaces.
- Confidence scores describe query relevance, not factual accuracy or completeness; this distinction must appear in API documentation, CLI explain output, compliance guidance, and MCP schema documentation.
- Causal traversal must distinguish `caused_by` from `correlated_with`, preserve direction and chronology, expose gaps, and never auto-promote AI-inferred relationship confidence.
- The project is Apache 2.0 and the README must include the stated commitment not to switch to a restrictive license. Redis Stack SSPL/RSAL and FalkorDB AGPL implications, version pinning, architectural boundaries, and hosted-service limitations must be documented.
- Internal services use DAPR invocation/pub-sub; external consumers use infrastructure-managed ingress. JSON is the sole serialization format, DAPR API tokens protect internal calls, ingress authenticates external calls, and tenant context is carried in payloads and validated by the server.
- Runtime secrets must come exclusively through DAPR Secrets API backed by OpenBao. Configuration precedence applies only to non-sensitive values; documented bootstrap exceptions are narrow.
- Google `text-embedding-004` is the MVP runtime provider. Changing a tenant's provider/model where vector dimensions differ requires a full tenant reindex because Redis Vector index schema is fixed at creation.
- Ingestion uses a durable per-tenant DAPR pipeline actor with bounded queuing, throttling, retries, progress tracking, and durable failed states; document processing remains stateless.
- Nine NuGet packages are governed by `tools/release-packages.json`; service/orchestration projects remain non-packable. Package changes must respect the documented dependency direction and backend extraction points.
- Required developer assets include the numbered quickstart, EventStore, and MCP samples; README, CLI help, getting-started, API, compliance, and operator documentation; and unit, integration, and contract-test layers.

### PRD Completeness Assessment

The PRD is structurally strong and unusually testable: it provides 74 explicitly numbered FRs, 31 measurable NFRs with verification phases, quantitative release gates, scoped journeys, interface parity, operational constraints, and defined trust/compliance boundaries.

Initial clarity issues remain:

- The reduced-resource fallback says cases and tenant isolation may be deferred, while the MVP strategy says both exist from day one and the go/no-go gate requires zero cross-tenant leakage and correct case scoping. The fallback cannot satisfy the stated MVP gates as written.
- Journey 2 says handler listing and replay must be included in “MVP Feature #3 (EventStore Integration),” but the MVP table's Feature #3 is Three-Axis Search and EventStore integration is assigned to Phase 1.5.
- The platform matrix specifies C# 13, while current repository policy specifies C# 14 for the .NET 10 codebase.
- The package-distribution narrative says “9 published NuGet packages + 3 non-packable service/orchestration projects,” but its accompanying inventory visibly identifies nine package entries and two explicitly non-packable projects (`Server` and `AppHost`). The missing third non-packable project or the count needs clarification.
- The Service Communication table describes Server-to-Redis/FalkorDB communication as “DAPR state / direct connection via DAPR sidecar,” which conflates two materially different integration paths and should be made explicit.

## Epic Coverage Validation

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
|---|---|---|---|
| FR1 | Developer can ingest content from local files into a specified case | Epic 1 | ✓ Covered |
| FR2 | Developer can ingest content from URLs into a specified case | Epic 6 | ✓ Covered |
| FR3 | Developer can batch-ingest content from a directory into a specified case | Epic 6 | ✓ Covered |
| FR4 | System can extract text from ingested content (plain text, PDF, markdown) | Epic 1 | ✓ Covered |
| FR5 | System can generate embeddings for ingested content via a configurable embedding provider | Epic 1 | ✓ Covered |
| FR6 | System ensures a memory unit is fully searchable across all axes after ingestion completes | Epic 1; reinforced by Epic 23 | ✓ Covered |
| FR7 | Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score | Epic 1 | ✓ Covered |
| FR8 | System manages ingestion load per tenant independently | Epic 6 | ✓ Covered |
| FR9 | System retries failed ingestion automatically with configurable limits | Epic 6 | ✓ Covered |
| FR10 | Developer can view ingestion status per case (queued, embedding, indexed, failed counts) | Epic 6 | ✓ Covered |
| FR11 | Developer can view failed ingestion units with error details and failure stage | Epic 6 | ✓ Covered |
| FR12 | Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk | Epic 6; reinforced by Epic 23 | ✓ Covered |
| FR13 | System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes) | Epic 1; reinforced by Epic 21 | ✓ Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant | Epic 2 | ✓ Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant | Epic 2 | ✓ Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant | Epic 2 | ✓ Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes | Epic 2 | ✓ Covered |
| FR18 | Developer can control which axes are included in a search query | Epic 2 | ✓ Covered |
| FR19 | Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode) | Epic 2 | ✓ Covered |
| FR20 | Developer can filter search results by case | Epic 3 | ✓ Covered |
| FR21 | Developer can filter search results by metadata field values | Epic 3 | ✓ Covered |
| FR22 | Developer can paginate search results | Epic 2; reinforced by Epic 22 | ✓ Covered |
| FR23 | LLM Agent can constrain search response size by token budget | Epic 10 (Phase 1.5) | ✓ Covered |
| FR24 | System returns the origin identifier (file path, URL, or event ID) and origin type for each search result | Epic 2 | ✓ Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output | Epic 2 | ✓ Covered |
| FR26 | Developer can create a case within a tenant | Epic 0 and Epic 3 | ✓ Covered |
| FR27 | Developer can delete a case and all its memory units | Epic 3 | ✓ Covered |
| FR28 | Developer can add members to a case | Epic 3 | ✓ Covered |
| FR29 | Developer can remove members from a case | Epic 3 | ✓ Covered |
| FR30 | Developer can list cases within a tenant | Epic 3 | ✓ Covered |
| FR31 | Developer can view case status including memory unit count, last activity timestamp, and health indicators | Epic 3 | ✓ Covered |
| FR32 | System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion | Epic 3 | ✓ Covered |
| FR33 | System maintains case-scoped graph edges between memory units within a case | Epic 3 | ✓ Covered |
| FR34 | Developer can search across all cases within a tenant by keyword, returning results with case attribution | Epic 3; reinforced by Epic 22 | ✓ Covered |
| FR35 | Developer can delete an individual memory unit from a case | Epic 3 | ✓ Covered |
| FR36 | Developer can view recent activity within a case (ingestion events, searches, membership changes) | Epic 3 | ✓ Covered |
| FR37 | Developer can annotate or correct a memory unit, with annotations tracked as linked memory units | Epic 3 | ✓ Covered |
| FR38 | Operator can create a tenant with physically separate indexes | Epic 0 and Epic 5; reinforced by Epic 24 | ✓ Covered |
| FR39 | Operator can delete a tenant and all its indexes, graph data, and memory units | Epic 5; reinforced by Epic 21 | ✓ Covered |
| FR40 | Operator can verify tenant isolation via automated checks | Epic 5; reinforced by Epic 24 | ✓ Covered |
| FR41 | Operator can list tenants | Epic 5 | ✓ Covered |
| FR42 | Operator can update tenant configuration after creation (rate limits, display name, settings) | Epic 5 | ✓ Covered |
| FR43 | System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment | Epic 5 | ✓ Covered |
| FR44 | System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages | Epic 0 and Epic 5; reinforced by Epics 20 and 24 | ✓ Covered |
| FR45 | Operator can view current configuration of a tenant (embedding provider, rate limits, index status) | Epic 5 | ✓ Covered |
| FR46 | System can index CausationId and CorrelationId from events as typed, directional graph edges | Epic 1 | ✓ Covered |
| FR47 | Developer can traverse causal chains from a starting node with configurable depth | Epic 4 | ✓ Covered |
| FR48 | Developer can filter graph traversal by edge type | Epic 4 | ✓ Covered |
| FR49 | When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier | Epic 4 | ✓ Covered |
| FR50 | System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence | Epic 4 | ✓ Covered |
| FR51 | Developer can promote AI-inferred edge confidence when verifying a relationship | Epic 4 | ✓ Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes | Epic 4 | ✓ Covered |
| FR53 | Developer can interact with all retrieval and ingestion capabilities via CLI | Epic 7, split between MVP essentials and Phase 1.5 polish | ✓ Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools | Epic 10 (Phase 1.5) | ✓ Covered |
| FR55 | CLI supports multiple output formats: human-readable (default), JSON, and table | Epic 7 | ✓ Covered |
| FR56 | CLI provides actionable error messages with recovery suggestions for common failure modes | Epic 7 | ✓ Covered |
| FR57 | Developer can discover available actions from any system state, including empty states and error conditions | Epic 7 | ✓ Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions for LLM agent consumption | Epic 10 (Phase 1.5) | ✓ Covered |
| FR59 | System can auto-discover event types published to DAPR pub/sub topics | Epic 9 (Phase 1.5) | ✓ Covered |
| FR60 | System can generate dual embeddings for events (raw payload + natural language description) | Epic 9 (Phase 1.5) | ✓ Covered |
| FR61 | System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code | Epic 9 (Phase 1.5) | ✓ Covered |
| FR62 | Developer can list registered event handlers and detect handler registration mismatches | Epic 9 (Phase 1.5) | ✓ Covered |
| FR63 | System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result | Epic 2 | ✓ Covered |
| FR64 | System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit | Epic 7 | ✓ Covered |
| FR65 | System records `ingested_by` (user or system identity) as a mandatory field on every memory unit | Epic 1 | ✓ Covered |
| FR66 | When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded | Epic 5 | ✓ Covered |
| FR67 | System logs search and access events per tenant for audit purposes | Epic 7; reinforced by Epics 20 and 27 | ✓ Covered; retention residual remains open |
| FR68 | Operator can configure embedding provider and model per tenant | Epic 1; post-MVP providers require explicit expansion work | ✓ Covered |
| FR69 | System enforces per-tenant rate limit ceilings for embedding API calls | Epic 5 | ✓ Covered |
| FR70 | System tracks the embedding provider and model used for each memory unit's vectors | Epic 5 | ✓ Covered |
| FR71 | Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format; Phase 2 unless explicitly pulled forward | Epic 26 covers operational backup/restore; full application-facing export remains a Phase 2 placeholder | ✓ Covered by an explicit deferred implementation path |
| FR72 | System exposes readiness and liveness health checks verifying all backends | Epic 8 | ✓ Covered |
| FR73 | Operator can detect index/graph divergence via consistency check | Epic 8 | ✓ Covered |
| FR74 | Operator can repair detected index/graph inconsistencies via consistency repair operation | Epic 8 | ✓ Covered |

### Missing Requirements

No PRD FR is absent from the canonical epic coverage map. No epic coverage-map FR identifier is absent from the PRD.

Coverage does not imply active-MVP scope: FR23, FR54, and FR58-FR62 are explicitly Phase 1.5; FR71's full application-facing export remains Phase 2; and several later epics reinforce requirements without reopening MVP completion accounting.

### Coverage Statistics

- Total PRD FRs: 74
- Unique FRs in the epic coverage map: 74
- FRs covered in epics or an explicit deferred implementation path: 74
- Missing FRs: 0
- Extra FR identifiers in the epic coverage map: 0
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

**Status: Present and implementation-relevant.**

The canonical `ux-design-specification.md` is a full-horizon UX specification with an explicit phase boundary: CLI-visible and contract-visible Evidence Packet semantics bind MVP; MCP/EventStore follow in Phase 1.5; FrontComposer/Fluent UI Blazor browser composition remains future work unless separately activated. The approved `sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` supplements it by making FrontComposer composition and Microsoft Fluent UI Blazor V5 the exclusive future-web component boundary and requiring conformance evidence for justified markup or styling gaps.

The PRD, UX, and architecture are strongly aligned on the central experience:

- retrieval is tenant-scoped and case-aware;
- the Evidence Packet is the shared trust envelope for CLI JSON, MCP, and future web composition;
- source, confidence, per-axis reasoning, freshness, graph context, omitted details, degraded state, and recovery guidance are first-class concepts;
- empty, weak, stale, degraded, and unauthorized outcomes are designed states rather than opaque failures;
- phase boundaries keep the UX specification from silently pulling MCP or browser UI into MVP;
- architecture provides `Contracts.V1`, the CLI/MCP/web presentation boundaries, tenant authorization, source and graph metadata, and the FrontComposer/Fluent UI Blazor V5 composition rule needed to support the specified experience.

### Alignment Issues

1. **Core-search explanation is mandatory in UX but opt-in in PRD interface examples.** The UX requires the first core response to automatically contain source lookup, evidence strength, retrieval-axis explanation, freshness, and relevant graph context. The PRD repeatedly exposes this behavior as `search --explain` or an `explain` option, while its Evidence Packet paragraph composes the same trust primitives into the shared response envelope. Architecture defines the envelope but does not settle which fields are mandatory for every search versus populated only in explain mode. Before implementation, define a single baseline rule: either every core search returns the compact trust fields and `--explain` only expands detail, or revise the UX promise to make explanation opt-in.

2. **The Evidence Packet scope shape is underspecified for tenant-wide cross-case search.** PRD FR34 requires tenant-wide search with case attribution, UX defines `cross-case` as a valid scope state, and architecture defines cross-case ranking/grouping. However, the architecture's minimum Evidence Packet `scope` is expressed as tenant ID plus case ID, which implies one case. The contract needs an explicit tenant-wide/cross-case representation, nullable or plural case selection semantics, and per-result case attribution so implementations do not overload a single `caseId` inconsistently.

3. **The UX absence-state grammar is finer-grained than the architectural state contract.** UX requires users and agents to distinguish no match, insufficient evidence, pending ingestion, wrong case, inaccessible scope, stale memory, degraded backend, graph gap, and token-budget truncation. Architecture lists the coarse Evidence Packet states `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, and `pending expansion`; recovery actions alone do not guarantee machine-stable diagnosis. Add a structured reason/status code taxonomy beneath the coarse state, with deterministic mappings to recovery actions and equivalent CLI, MCP, and future-web semantics.

### Warnings

- **Isolation presentation must not outrun enforcement.** UX requires users to see whether tenant isolation is physically enforced. Architecture includes an `isolation status`, but also records the Redis physical-isolation target—per-tenant ACL users plus tenant-scoped backend resolution—as follow-up enforcement work. Define truthful status values and acceptance evidence so a prefix/database placement scheme cannot be displayed as physically verified before the target controls pass.
- **Future-web quality attributes are specified behaviorally but not budgeted quantitatively.** UX provides responsive breakpoints, keyboard/focus behavior, reduced-motion and forced-color requirements, and WCAG 2.2 AA intent. Architecture supports the component boundary, but no browser performance budgets or measurable interaction/render targets are defined. This is not an MVP blocker because browser composition is deferred, but those budgets and an automated accessibility/browser verification host should be added when Epic 17 is activated.

## Epic Quality Review

### Review Scope and Compliance Summary

The canonical epic specification contains 32 epics (Epic 0 through Epic 31) and 166 registered story definitions. Every registered story states a developer, operator, maintainer, reviewer, architect, test, release, platform, or user outcome and includes acceptance criteria. FR traceability remains complete at 74/74.

| Quality Dimension | Result | Evidence |
|---|---|---|
| Epic outcome focus | 29 pass; 3 cohesion defects | Epics 14, 15, and 25 are catch-all technical/risk groupings rather than one cohesive beneficiary outcome. |
| Epic/story dependency order | 31 epics pass; Epic 27 fails | Story 27.4 requires unregistered future successors and cannot be completed from the current backlog. |
| Story persona and value statement | 166/166 present | All registered stories use an explicit beneficiary and desired outcome. |
| Acceptance-criteria structure | 165 use normal multiline BDD; Story 27.3 is nonstandard | Story 27.3 uses numbered, inline lower-case `when`/`then` clauses and an exceptionally dense AC6. |
| Starter/scaffolding requirement | Pass | Story 0.0 establishes the Aspire solution and single-command boot; Story 0.4 supplies the early build/test preflight. |
| Data/resource creation timing | Pass | Tenant backend resources are created by the first tenant-provisioning slice; later stories add only the indexes/contracts/resources their capability needs. No speculative all-entity/table story was found. |
| FR traceability | Pass | 74/74 FRs map to epics or an explicit deferred implementation path. |

Numeric-key exceptions were checked rather than treated as hidden forward dependencies. Story 18.6 is physically ordered before its consumer Story 18.5; Story 23.9 is physically ordered before Story 23.1; both exceptions are explicitly governed by `story_execution_order`. Story 12.7 and Story 12.8 are conditional future reopen slots, not prerequisites of Story 12.6. The only unresolved forward dependency is in Epic 27.

### 🔴 Critical Violations

1. **Epic 27 has no complete implementable dependency chain.** Story 27.4's predecessor gate requires Story 27.7 through Story 27.31 files to exist, be registered, and be done, and requires all 25 C1 child gates to pass. The same epic states that only Story 27.21 is registered, it is still `backlog`, and the other 24 C1 gates are held and unowned. This violates story independence and makes Story 27.4—and therefore Epic 27 completion—depend on future work that is not in the backlog. **Remediation:** keep Story 27.4 non-ready; create and approve one independently executable story per remaining C1 gate (or a standards-compliant split with real producers and checkpoint ownership), register their explicit order, and only then reassess Story 27.4 readiness.

### 🟠 Major Issues

1. **Story 27.3 is an active oversized multi-deliverable story.** Its current scope combines predecessor checkpoint C0, deployment-lane qualification C2, adapter unit-contract qualification C3, and manifest/static qualification C4. AC6 alone is a long compound contract spanning build, render, apply, cluster refusal, component enumeration, runtime substitution, readback, health, evidence validation, and upload, with an admitted interval in which consumer pods can crash-loop. The acceptance section also starts at criteria 6-8 and uses a nonstandard inline BDD form, making the binding contract difficult to execute and review independently. **Remediation:** split C0/C2/C3/C4 into separately tracked stories or provide one current, producer-complete checkpoint table per independently verifiable gate; rewrite each criterion as bounded Given/When/Then scenarios with one observable result and explicit failure paths.

2. **Epics 14 and 15 are risk-register catch-alls rather than cohesive value epics.** Epic 14 combines CI diff parsing, release publication, OIDC/embedding security, migration tests, and deferred-register governance. Epic 15 combines release edge cases, provider dimensions, migration concurrency, token transport, backlog triage, and AppHost/DAPR scaffolding. “Close deferred findings” is an administrative grouping, not one standalone user/operator capability. **Remediation:** retain historical records, but route any reopened work into outcome-focused epics such as release integrity, embedding security, migration safety, or planning governance; do not extend either catch-all.

3. **Epic 25 is a horizontal technical-refactor epic.** `Architecture Factorization & Code Health` groups `Program.cs` decomposition, error/telemetry centralization, route/client consolidation, contract/persistence separation, CLI/MCP consolidation, UX conformance, and topology cleanup. These stories do not compose into one independently consumable user or operator result. **Remediation:** preserve completed history, but place future refactors in the smallest beneficiary-facing capability or operational-quality epic whose observable behavior they protect.

4. **Four historical stories are explicitly broader than the current sizing standard.** The epic document itself identifies Stories 1.2, 1.5, 1.6, and 8.5 as broad technical or bundled slices and prohibits reopening them as single units. This guard prevents them from becoming an active implementation blocker, but the definitions remain noncompliant examples. **Remediation:** enforce the existing historical-scope guards and create new numeric, vertical stories for any reimplementation.

### 🟡 Minor Concerns

1. **Several criteria retain subjective or selectable test oracles.** Examples include Story 2.8's “reasonable time,” Story 7.2's “clear formatting” and “visually clear,” Story 10.2's “appropriate MCP error response,” Story 13.1's “tenant-appropriate”/“sensible” rate limit such as 6000, Story 18.5's exposure through MCP/CLI “as appropriate,” and Story 30.3's “workload-appropriate health contract.” Replace these with exact thresholds, stable error codes/schema, required surfaces, or named health probes before any affected story is reopened.

2. **Historical ordering exceptions increase tooling risk.** Story 0.0 retains a Story 1.1 alias, Story 18.6 precedes Story 18.5, and Story 23.9 precedes Story 23.1. The explicit alias map and `story_execution_order` make the present ordering safe, but future story creation and reporting must continue to honor those records rather than infer order numerically.

### Quality Gate Recommendation

The active MVP epic sequence (Epics 0-8) is coherent, ordered, traceable, and protected by explicit historical-scope guards. The full backlog is **not independently implementation-ready** because Epic 27 contains a critical unresolved forward dependency and Story 27.3 remains an oversized active execution unit. The operational/future backlog should not receive a blanket ready status until those issues are corrected; readiness should remain phase- and story-specific.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY for blanket implementation of the full backlog.**

This is a phase-aware result, not a rejection of the existing MVP foundation. The active MVP sequence (Epics 0-8) is coherent, 100% FR-traceable, and structurally ready. The full planning set cannot be declared ready because Epic 27 has no complete registered dependency chain, Story 27.3 is still an oversized active execution unit, and the shared Evidence Packet contract leaves three cross-surface behaviors open to implementer interpretation.

### Critical Issues Requiring Immediate Action

1. **Repair Epic 27's dependency graph.** Story 27.4 requires 25 C1 successor gates, but only Story 27.21 is registered and it remains backlog; 24 gates have no registered owner. Do not mark Story 27.4 ready or Epic 27 implementable until every required predecessor has a real story, producer, owner, acceptance contract, and execution order.
2. **Decompose or checkpoint Story 27.3.** Separate C0, C2, C3, and C4 into independently executable and reviewable units, or provide a producer-complete checkpoint table that satisfies the repository's own story-shape guard. Replace compound AC6 with bounded scenarios.
3. **Ratify the Evidence Packet semantics before retrieval-surface work continues.** Decide whether compact trust evidence is mandatory on every search or only under `--explain`; define tenant-wide/cross-case scope representation; and add stable diagnostic reason codes beneath the coarse result state.

### Recommended Next Steps

1. Run a focused course correction for Epic 27 that registers the missing gate owners without recreating an umbrella story and updates `story_execution_order` atomically.
2. Amend `Contracts.V1`, architecture, PRD interface examples, UX state grammar, and relevant epic acceptance criteria together for the three Evidence Packet decisions; add serialization and cross-surface contract tests.
3. Correct the five PRD clarity defects: reduced-resource fallback versus mandatory tenant/case gates, Journey 2's phase/feature reference, C# 13 versus C# 14, the non-packable project count, and the DAPR-versus-direct backend communication wording.
4. Preserve Epics 14, 15, and 25 and broad Stories 1.2, 1.5, 1.6, and 8.5 as history only. Any reopened work should receive new, vertical, beneficiary-facing stories.
5. Replace subjective acceptance language with exact thresholds, schemas, error codes, required surfaces, and named health probes before the affected stories are reopened.
6. When future web work is activated, add measurable browser performance budgets, truthful isolation-status acceptance semantics, and automated browser/accessibility evidence alongside the existing responsive and WCAG requirements.

### Final Note

This assessment identified 17 findings across three categories: five PRD clarity issues, five UX/architecture alignment issues or warnings, and seven epic/story quality issues. The active MVP planning spine is sound, but the full backlog must remain phase- and story-specific until the critical Epic 27 and Evidence Packet issues are resolved.

**Assessment date:** 2026-08-04  
**Assessor:** Codex, using the BMAD Implementation Readiness workflow  
**Requested by:** Administrator
