---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
includedFiles:
  prd:
    - D:/Hexalith.Memories/_bmad-output/planning-artifacts/prd.md
  architecture:
    - D:/Hexalith.Memories/_bmad-output/planning-artifacts/architecture.md
  epics:
    - D:/Hexalith.Memories/_bmad-output/planning-artifacts/epics.md
  ux:
    - D:/Hexalith.Memories/_bmad-output/planning-artifacts/ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-17
**Project:** Hexalith.Memories

## Document Inventory

### PRD

Whole documents:
- D:/Hexalith.Memories/_bmad-output/planning-artifacts/prd.md (83,799 bytes, modified 2026-05-17 13:04:13 +02:00)

Sharded documents:
- None found

### Architecture

Whole documents:
- D:/Hexalith.Memories/_bmad-output/planning-artifacts/architecture.md (103,151 bytes, modified 2026-05-17 13:04:18 +02:00)

Sharded documents:
- None found

### Epics & Stories

Whole documents:
- D:/Hexalith.Memories/_bmad-output/planning-artifacts/epics.md (180,172 bytes, modified 2026-05-17 13:20:58 +02:00)

Sharded documents:
- None found

### UX Design

Whole documents:
- D:/Hexalith.Memories/_bmad-output/planning-artifacts/ux-design-specification.md (97,176 bytes, modified 2026-05-17 09:13:25 +02:00)

Sharded documents:
- None found

### Issues

- No duplicate whole/sharded document conflicts found.
- No required document type is missing.

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
FR71: Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. Phase: Phase 2 unless a later sprint change explicitly pulls export into MVP.
FR72: System exposes readiness and liveness health checks verifying all backends.
FR73: Operator can detect index/graph divergence via consistency check.
FR74: Operator can repair detected index/graph inconsistencies via consistency repair operation.

Total FRs: 74

### Non-Functional Requirements

NFR1: Syntactic search latency (p95) target <200ms under 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.
NFR2: Semantic search latency (p95) target <500ms under 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.
NFR3: Hybrid search latency (p95) target <1s under 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.
NFR4: Graph traversal latency (p95) target <2s under 10 concurrent queries/tenant, 10K memory units/tenant, and depth <=5. Phase: MVP.
NFR5: Ingestion throughput target >100 memory units/min for payloads <=10KB and >10 memory units/min for payloads <=1MB, per tenant, single-document embedding calls. Phase: Ongoing.
NFR6: Event indexing freshness target <5s from DAPR pub/sub publication to searchable under normal conditions; degradation documented when embedding provider is rate-limited. Phase: P1.5.
NFR7: Cold start time target service fully operational within 60s from containers running to accepting queries, excluding image pull time. Phase: Ongoing.
NFR8: Zero cross-tenant data leakage; no search, ingestion, or graph traversal returns data from another tenant. Verification: automated tests across axes with malformed/empty/swapped tenant IDs and graph-specific edge collision checks. Phase: MVP.
NFR9: Embedding provider API keys stored in secure secret management (.NET User Secrets for local dev, DAPR Secrets API for deployed); never in config files or environment variables in production. Phase: Ongoing.
NFR10: All inter-service communication authenticated via DAPR API tokens. Phase: Ongoing.
NFR11: External access authenticated at ingress layer; no unauthenticated access to REST API endpoints. Phase: P1.5.
NFR12: System supports linear scaling of tenants; adding a new tenant does not degrade existing tenant performance by more than 5%, validated at 10 tenants each with 100K memory units. Phase: Ongoing.
NFR13: Per-tenant ingestion pipeline scales independently; one tenant's batch ingestion does not block another tenant's real-time ingestion. Phase: Ongoing.
NFR14: Redis memory footprint per memory unit is predictable and documented so operators can estimate infrastructure costs before tenant provisioning. Phase: Ongoing.
NFR15: Architecture must not preclude backend migration from Redis to Qdrant; concrete implementation with clear extraction points identified and no premature interfaces. Phase: Ongoing.
NFR16: Zero memory unit loss during Redis restart; AOF persistence enabled and verified. Phase: MVP.
NFR17: Ingestion pipeline state survives process restarts; queued and in-progress units resume without data loss via DAPR actor state persistence. Phase: MVP.
NFR18: Partial backend failure results in degraded service, not total failure; available axes continue serving results. Phase: Ongoing.
NFR19: Failed ingestion units are never silently dropped; all failures visible via CLI status with error details and failure stage. Phase: Ongoing.
NFR20: MCP tool responses conform to MCP protocol specification with valid schemas, typed parameters, and structured errors. Phase: P1.5.
NFR21: DAPR pub/sub integration handles CloudEvents envelope format; events from any DAPR-compatible publisher are processable. Phase: P1.5.
NFR22: Embedding provider integration handles rate limiting gracefully; 429 responses trigger backoff without pipeline crash or data loss. Phase: Ongoing.
NFR23: CLI connects to the memory server via configurable endpoint supporting local dev, container, and remote ingress environments. Phase: Ongoing.
NFR24: All axis scores normalized to 0.0-1.0 before fusion; BM25 via saturation normalization against corpus statistics, cosine similarity native range, graph proximity via inverse hop distance with decay. Phase: MVP.
NFR25: Fusion algorithm produces deterministic scores; same query against same data produces identical composite scores, while result ordering within same score tier may vary. Phase: MVP.
NFR26: Benchmark suite produces reproducible results; running benchmarks twice against the same dataset yields identical NDCG@10 scores. Phase: MVP.
NFR27: Structured JSON logging with OpenTelemetry correlation IDs from DAPR trace context. Phase: Ongoing.
NFR28: Trace context propagates across all DAPR service invocation hops; end-to-end trace from CLI/MCP through server to backend. Phase: Ongoing.
NFR29: Custom metrics exported via OpenTelemetry: ingestion throughput, search latency per axis, index size per tenant, and pipeline queue depth. Phase: Ongoing.
NFR30: Every CLI command includes --help with at least one usage example. Phase: MVP.
NFR31: README includes working quickstart that completes in <30 minutes on a clean machine with Docker installed. Phase: MVP.

Total NFRs: 31

### Additional Requirements

- Three-axis retrieval is a hard thesis gate: 80% of benchmark queries requiring all three axes must outperform any single axis alone using NDCG@10 and reviewer-defined ground truth.
- MVP hard gates are three-axis validation at 80%, zero cross-tenant data leaks, and developer onboarding under 30 minutes.
- MVP soft gates require at least 2 of 3: causal chain completeness >=95%, MCP end-to-end integration works, and case model correctly scopes memory.
- Implementation sequencing must establish a buildable scaffold/AppHost/ServiceDefaults, minimum build/test feedback, tenant provisioning, minimal case bootstrap, and tenant/case validation before ingestion/indexing/search/graph stories write data.
- TenantProvisioningWorkflow owns physically isolated tenant infrastructure creation.
- Ingestion and indexing must fail before backend writes if tenant or case context is missing or mismatched.
- Search axes must be built independently before fusion; fusion is a dedicated R&D spike.
- Phase 1.5 is committed within 4 weeks of thesis validation; if it slips, MCP moves back into MVP.
- MVP CLI scope is `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, and benchmark support.
- Phase 1.5 CLI scope expands to `explore`, `status`, `handlers`, `quickstart`, batch directory ingestion, and richer diagnostics.
- CLI configuration precedence is command-line flags, environment variables, config file, DAPR Secrets API, .NET User Secrets, then DAPR configuration.
- Test strategy requires unit tests with mocked DaprClient, integration tests using Aspire DistributedApplicationTestingBuilder or DAPR testcontainers, and contract serialization tests.
- Git submodule dependencies include Hexalith.Commons and Hexalith.EventStore.
- .NET Aspire AppHost orchestrates all services with DAPR sidecars; local development runs via AppHost and production uses Aspire manifest export.
- Non-.NET consumers can integrate through ingress REST API or DAPR service invocation; Python/TypeScript clients are future convenience layers.

### PRD Completeness Assessment

The PRD is strong for readiness analysis: it contains 74 explicit functional requirements, 31 explicit non-functional requirements, measurable success gates, phased scope, interface expectations, test strategy, and implementation sequencing. The main planning risk is not absence of requirements, but scope density: several requirements marked Phase 1.5 or Ongoing still appear in the full functional/non-functional inventory and must be cleanly mapped to epic/story phase boundaries during coverage validation.

## Epic Coverage Validation

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR1 | Developer can ingest content from local files into a specified case. | Epic 1 - Ingest from local files | Covered |
| FR2 | Developer can ingest content from URLs into a specified case. | Epic 6 - Ingest from URLs | Covered |
| FR3 | Developer can batch-ingest content from a directory into a specified case. | Epic 6 - Batch-ingest from directory | Covered |
| FR4 | System can extract text from ingested content (plain text, PDF, markdown). | Epic 1 - Text extraction (Kreuzberg) | Covered |
| FR5 | System can generate embeddings for ingested content via a configurable embedding provider. | Epic 1 - Generate embeddings | Covered |
| FR6 | System ensures a memory unit is fully searchable across all axes after ingestion completes. | Epic 1 - Memory unit fully searchable after ingestion | Covered |
| FR7 | Developer can attach metadata to ingested content, with each field tracking its origin and confidence score. | Epic 1 - Metadata with origin tracking | Covered |
| FR8 | System manages ingestion load per tenant independently. | Epic 6 - Per-tenant ingestion load management | Covered |
| FR9 | System retries failed ingestion automatically with configurable limits. | Epic 6 - Auto-retry with configurable limits | Covered |
| FR10 | Developer can view ingestion status per case. | Epic 6 - Ingestion status per case | Covered |
| FR11 | Developer can view failed ingestion units with error details and failure stage. | Epic 6 - Failed unit visibility | Covered |
| FR12 | Developer can manually trigger re-ingestion of failed or previously ingested content. | Epic 6 - Re-ingestion of failed content | Covered |
| FR13 | System handles partial backend write failures with defined recovery behavior. | Epic 1 - IngestionWorkflow saga/compensation | Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant. | Epic 2 - Syntactic search | Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant. | Epic 2 - Semantic search | Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant. | Epic 2 - Graph search | Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes. | Epic 2 - Hybrid fusion search | Covered |
| FR18 | Developer can control which axes are included in a search query. | Epic 2 - Axis selection control | Covered |
| FR19 | Developer can view per-axis score breakdown for each search result. | Epic 2 - Per-axis score breakdown (explain) | Covered |
| FR20 | Developer can filter search results by case. | Epic 3 - Filter search by case | Covered |
| FR21 | Developer can filter search results by metadata field values. | Epic 3 - Filter search by metadata | Covered |
| FR22 | Developer can paginate search results. | Epic 2 - Pagination | Covered |
| FR23 | LLM Agent can constrain search response size by token budget. | Epic 10 - MCP token budget and omitted-detail handles | Covered |
| FR24 | System returns the origin identifier and origin type for each search result. | Epic 2 - Origin identifier in results | Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search results. | Epic 2 - Benchmark comparisons | Covered |
| FR26 | Developer can create a case within a tenant. | Epic 0 + Epic 3 - Minimal case bootstrap and full case management | Covered |
| FR27 | Developer can delete a case and all its memory units. | Epic 3 - Delete case | Covered |
| FR28 | Developer can add members to a case. | Epic 3 - Add case members | Covered |
| FR29 | Developer can remove members from a case. | Epic 3 - Remove case members | Covered |
| FR30 | Developer can list cases within a tenant. | Epic 3 - List cases | Covered |
| FR31 | Developer can view case status. | Epic 3 - Case status | Covered |
| FR32 | System enforces strict single-case ownership per memory unit. | Epic 3 - Single-case ownership | Covered |
| FR33 | System maintains case-scoped graph edges between memory units within a case. | Epic 3 - Case-scoped graph edges | Covered |
| FR34 | Developer can search across all cases within a tenant by keyword. | Epic 3 - Cross-case tenant search | Covered |
| FR35 | Developer can delete an individual memory unit from a case. | Epic 3 - Delete memory unit | Covered |
| FR36 | Developer can view recent activity within a case. | Epic 3 - Case activity | Covered |
| FR37 | Developer can annotate or correct a memory unit. | Epic 3 - Annotations/corrections | Covered |
| FR38 | Operator can create a tenant with physically separate indexes. | Epic 0 + Epic 5 - Tenant creation and isolated infrastructure provisioning | Covered |
| FR39 | Operator can delete a tenant and all its indexes, graph data, and memory units. | Epic 5 - Delete tenant | Covered |
| FR40 | Operator can verify tenant isolation via automated checks. | Epic 5 - Verify tenant isolation | Covered |
| FR41 | Operator can list tenants. | Epic 5 - List tenants | Covered |
| FR42 | Operator can update tenant configuration after creation. | Epic 5 - Update tenant config | Covered |
| FR43 | System prevents configuration changes that would create data inconsistency. | Epic 5 - Prevent inconsistent config changes | Covered |
| FR44 | System enforces tenant context at all access layers. | Epic 0 + Epic 5 - Tenant context validation and enforcement | Covered |
| FR45 | Operator can view current configuration of a tenant. | Epic 5 - View tenant configuration | Covered |
| FR46 | System can index CausationId and CorrelationId as graph edges. | Epic 1 - Index CausationId/CorrelationId during ingestion | Covered |
| FR47 | Developer can traverse causal chains from a starting node. | Epic 4 - Traverse causal chains | Covered |
| FR48 | Developer can filter graph traversal by edge type. | Epic 4 - Filter by edge type | Covered |
| FR49 | Traversal result includes a gap marker when an intermediate node is not indexed. | Epic 4 - Gap markers for missing nodes | Covered |
| FR50 | System supports the default edge type taxonomy. | Epic 4 - Edge type taxonomy | Covered |
| FR51 | Developer can promote AI-inferred edge confidence. | Epic 4 - Promote AI-inferred confidence | Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes. | Epic 4 - Chronological ordering | Covered |
| FR53 | Developer can interact with retrieval and ingestion capabilities via CLI. | Epic 7 - CLI for all capabilities | Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info via MCP tools. | Epic 10 - MCP tools | Covered |
| FR55 | CLI supports human-readable, JSON, and table output formats. | Epic 7 - CLI output formats | Covered |
| FR56 | CLI provides actionable error messages with recovery suggestions. | Epic 7 - Actionable CLI errors | Covered |
| FR57 | Developer can discover available actions from any system state. | Epic 7 - Discoverable actions | Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions. | Epic 10 - MCP typed schemas | Covered |
| FR59 | System can auto-discover event types published to DAPR pub/sub topics. | Epic 9 - Auto-discover event types | Covered |
| FR60 | System can generate dual embeddings for events. | Epic 9 - Dual embeddings for events | Covered |
| FR61 | System can automatically index CausationId/CorrelationId metadata. | Epic 9 - Auto-index CausationId/CorrelationId | Covered |
| FR62 | Developer can list registered event handlers and detect mismatches. | Epic 9 - Handler registration management | Covered |
| FR63 | System returns composite confidence scores with per-axis breakdowns. | Epic 2 - Composite confidence scores and Evidence Packet mapping | Covered |
| FR64 | System tracks metadata origin and confidence per metadata field. | Epic 7 - Metadata origin tracking display | Covered |
| FR65 | System records `ingested_by` as a mandatory field. | Epic 1 - `ingested_by` field | Covered |
| FR66 | System returns partial results when one or more backends are unavailable. | Epic 5 - Partial results on backend failure | Covered |
| FR67 | System logs search and access events per tenant for audit. | Epic 7 - Search/access telemetry | Covered |
| FR68 | Operator can configure embedding provider and model per tenant. | Epic 1 - Configure embedding provider | Covered |
| FR69 | System enforces per-tenant rate limit ceilings for embedding API calls. | Epic 5 - Per-tenant rate limits | Covered |
| FR70 | System tracks the embedding provider and model used for each memory unit's vectors. | Epic 5 - Track embedding model per unit | Covered |
| FR71 | Developer can export memory units, metadata, and graph edges for a case or tenant. | Deferred to Phase 2 - Portable case/tenant export | Covered as explicit deferral |
| FR72 | System exposes readiness and liveness health checks verifying all backends. | Epic 8 - Health checks | Covered |
| FR73 | Operator can detect index/graph divergence via consistency check. | Epic 8 - Consistency check | Covered |
| FR74 | Operator can repair detected index/graph inconsistencies. | Epic 8 - Consistency repair | Covered |

### Missing Requirements

No PRD FRs are missing from the epics coverage map. FR71 is not in MVP implementation scope, but it is explicitly tracked as a Phase 2 deferral rather than silently omitted.

### Coverage Statistics

- Total PRD FRs: 74
- FRs covered in epics coverage map: 74
- FRs in epics but not in PRD: 0
- Missing FRs: 0
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

Found: D:/Hexalith.Memories/_bmad-output/planning-artifacts/ux-design-specification.md.

The UX document is complete and explicitly positions Hexalith.Memories as a trust, evidence, and recovery experience rather than a simple search interface. Its main UX objects are the Evidence Packet, visible tenant/case scope, source attribution, retrieval-axis explanation, confidence/freshness/state labels, omitted-detail expansion, and recovery actions.

### UX to PRD Alignment

Aligned areas:

- PRD journeys require explainable search, causal context, empty-state recovery, handler/status diagnostics, and under-30-minute onboarding; the UX spec directly designs for those moments through the trust loop, evidence packets, recovery states, and onboarding proof path.
- PRD FR19, FR23, FR24, FR56, FR57, FR63, FR64, FR66, and FR67 are strongly reflected in the UX spec's evidence, token budget, structured error, telemetry, and degraded-state guidance.
- PRD tenant and case safety requirements are reflected in UX as visible scope, wrong-scope warning/refusal behavior, and scope-first search.
- PRD MCP requirements are reflected as schema-first, bounded, attributed, token-aware agent packets with deterministic omitted-detail expansion handles.

Potential UX-to-PRD scope tension:

- The UX spec treats CLI, MCP, and web UI as first-class surfaces over the full product horizon, while the PRD and epics define MVP as CLI-first, Phase 1.5 as MCP/EventStore, and web/FrontComposer work as future-phase unless explicitly pulled forward. This is acceptable because the UX document states it is full-horizon guidance, but implementation stories must continue to separate MVP, Phase 1.5, and future web scope.

### UX to Architecture Alignment

Aligned areas:

- Architecture defines `Contracts.V1` as the owner of the shared Evidence Packet grammar for CLI JSON output, MCP tool responses, and future web UI composition.
- Architecture supports UX trust fundamentals with `scope`, `sources`, `evidence`, `graph`, `state`, `omittedDetails`, and `recoveryActions` fields.
- Architecture supports tenant/case UX safety through physical tenant isolation, tenant context validation, per-tenant actor IDs, tenant provisioning workflows, and graph query isolation.
- Architecture supports recovery-state UX through DAPR Workflow retry/compensation, failed ingestion visibility, consistency verification, health checks, and degraded backend behavior.
- Architecture supports MCP UX through typed tool schemas, token-budget-aware responses, omitted fields, deterministic expansion handles, and structured errors.
- Architecture supports CLI UX through actionable error format, `search --explain`, tenant/case commands, `tenant verify`, benchmark support, and later `status`, `handlers`, `quickstart`, and richer diagnostics.

Alignment issues:

- No blocking UX/architecture misalignment found.
- The architecture has enough contract and service support for the MVP/Phase 1.5 trust model, but future web/FrontComposer UX remains intentionally non-implemented. If a later sprint pulls web UI forward, new stories must add concrete FrontComposer/Fluent UI implementation scope and accessibility validation rather than assuming the architecture already schedules that work.

### Warnings

- Preserve scope boundaries during implementation. UX references web UI, FrontComposer, Fluent UI components, responsive layouts, and accessibility patterns; these are design-ready but not MVP implementation-ready unless explicitly selected.
- Evidence Packet contracts are load-bearing for UX alignment. Weakening or deferring them would undermine CLI, MCP, and future web consistency.
- Recovery-state semantics should be implemented as structured contract fields, not prose-only CLI/MCP messages, or the LLM-agent UX will become brittle.

## Epic Quality Review

### Overall Quality Finding

The epics document is unusually strong on traceability, scope boundaries, lifecycle labels, and acceptance criteria structure. It explicitly separates MVP (Epics 0-8), Phase 1.5 (Epics 9-10), and operational readiness (Epics 11-15). Most stories use Given/When/Then criteria and the document calls out known scope exceptions.

The main quality concern is vertical slicing. Several early Epic 1 stories are technical implementation slices rather than independently valuable user stories. This is partly mitigated by the safety-critical foundation nature of the project, but it still raises implementation-readiness risk because technical slices can be marked done before a developer-visible behavior exists.

### Critical Violations

None found that block traceability or make implementation impossible.

No forward dependency was found where Epic N requires Epic N+1 to function. The document explicitly defines Epic 0 as the foundation before Epic 1 data-writing stories, then progresses through ingestion/search, tenant isolation, developer experience, and operations. Phase 1.5 and operational epics are labeled separately.

### Major Issues

1. Epic 1 contains technical stories that are not independently user-valuable.

Examples:
- Story 1.2: Memory Unit Domain Model & Contracts.
- Story 1.3: Content Extraction via Kreuzberg.
- Story 1.4: Embedding Generation.
- Story 1.5: Three-Backend Indexing.

Impact: These stories are testable, but they are component milestones. A developer cannot complete the core user journey until orchestration and CLI/API behavior connect the pieces. This creates a risk of "green technical stories, unproven user workflow."

Recommendation: For new or restarted work, slice these around thin vertical behavior where practical. For example, "ingest a local text file into an active case and persist extracted content," then expand to PDF/markdown, embedding, syntactic index, semantic index, and graph index with observable results. If technical stories are retained, each should require a contract/API/CLI-visible proof, not only internal classes.

2. Story 1.6 is too large by its own admission.

Evidence: The story includes a sizing note: "Future reimplementation or major rework must split it into smaller vertical stories: happy-path local file ingestion orchestration; failure, compensation, and failed-unit visibility; restart recovery, idempotency, and duplicate detection hardening."

Impact: This confirms an epic-sized orchestration story. It spans validation, workflow orchestration, consistency verification, compensation, provenance, restart recovery, and duplicate detection.

Recommendation: Treat Story 1.6 as historical scope only. Any future rework should enforce the documented split before implementation begins.

### Minor Concerns

1. Story numbering contains historical gaps and aliases.

Examples: Story 0.0 has historical alias Story 1.1; Epic 1 starts at Story 1.2; Epic 8 skips Story 8.3.

Impact: The document explains this, so it is not a readiness blocker. It can still confuse automation, branch naming, and story-file lookup.

Recommendation: Keep the current alias policy visible in story files and sprint status. Do not introduce new alphabetic or non-sequential keys without explicit tooling support.

2. Tenant provisioning appears in both Epic 0 and Epic 5, but current boundary notes resolve the implementation ambiguity.

Evidence: Story 0.1 now states it is the minimum executable prerequisite proving an active tenant exists before Epic 1 data-writing work, and must use the same `TenantProvisioningWorkflow` ownership model as Story 5.1. Story 5.1 now states it is the canonical full tenant lifecycle story and should verify, extend, or mark criteria satisfied if Story 0.1 already implemented the complete workflow.

Impact: This is no longer a major readiness issue. It remains a minor coordination concern because implementers must preserve those boundaries during story execution.

Recommendation: Keep the ownership notes visible in implementation handoff and story files. Do not introduce a second tenant infrastructure creation path.

3. Operational epics intentionally allow accepted-risk and carry-forward outcomes.

Examples: Epics 14-15 include acceptance criteria that resolve items as implemented, accepted, or carried forward.

Impact: This would be unacceptable for MVP product capability stories, but the document explicitly restricts this pattern to Engineering/Operational Readiness stories. Not blocking, but reviewers must keep enforcing that boundary.

Recommendation: Maintain the document's rule that MVP product stories must deliver working behavior or explicit validation, not documentation-only closure.

### Epic Compliance Checklist

| Epic | User/Operator Value | Independent Sequencing | Story Sizing | Acceptance Criteria | Finding |
| --- | --- | --- | --- | --- | --- |
| Epic 0 | Developer/operator foundation value | Valid as prerequisite foundation | Mostly acceptable | Testable BDD | Accept with foundation exception |
| Epic 1 | Developer ingestion/search value | Depends only on Epic 0 | Mixed; several technical slices | Testable | Major slicing concern |
| Epic 2 | Developer search/thesis value | Depends on Epic 1 indexed data | Acceptable | Testable | Good |
| Epic 3 | Case organization value | Depends on tenant/case foundation and search paths | Acceptable | Testable | Good |
| Epic 4 | Causal traversal value | Depends on graph edges from Epic 1 | Acceptable | Testable | Good |
| Epic 5 | Operator tenant safety value | Builds on Epic 0 tenant foundation | Acceptable with boundary notes | Testable | Good with coordination watch |
| Epic 6 | Developer ingestion operations value | Builds on Epic 1 pipeline | Acceptable | Testable | Good |
| Epic 7 | Developer CLI value | Builds on core server capabilities | Acceptable | Testable | Good |
| Epic 8 | Operator health/consistency value | Builds on backends and workflows | Acceptable | Testable | Good |
| Epic 9 | Developer zero-code EventStore value | Phase 1.5 after MVP | Acceptable | Testable | Good |
| Epic 10 | LLM agent MCP value | Phase 1.5 after MVP | Acceptable | Testable | Good |
| Epics 11-15 | Maintainer/operator readiness value | Separate operational track | Mixed but lifecycle-labeled | Mostly testable, evidence-based | Accept only with explicit sprint selection |

### Dependency Analysis

- No forward dependency found from MVP Epic N to a later MVP Epic N+1 as a condition for the earlier epic to function.
- Epic 4 intentionally depends on graph edges created during Epic 1; this is a backward dependency and acceptable.
- Epic 6 intentionally builds on the basic ingestion pipeline from Epic 1; this is a backward dependency and acceptable.
- Epic 7 depends on core server capabilities for CLI workflows; this is acceptable because CLI is Gate 3 after Gate 1/Gate 2 functionality.
- Phase 1.5 Epics 9-10 are clearly separated and should not be pulled into MVP without an explicit sprint change.

### Special Implementation Checks

- Starter template requirement: satisfied. Architecture specifies Aspire Empty plus incremental projects, and Story 0.0 is the first executable scaffold/AppHost/ServiceDefaults story.
- Greenfield readiness: mostly satisfied. The epics include initial project setup, minimum build/test expectations, tenant provisioning, case bootstrap, and validation guards before ingestion writes data.
- Database/entity creation timing: acceptable with a caveat. The plan avoids creating all data structures as a generic upfront database milestone, but tenant infrastructure is intentionally provisioned before data-writing stories to satisfy physical isolation. Case and memory structures are introduced where first needed.
- CI/CD timing: minimum build/test CI is called out as an early prerequisite, while semantic release and full release hardening remain in operational readiness. This split is acceptable, but implementation handoff must not postpone the minimum build/test gate behind product stories.

### Quality Recommendations

- Before implementation resumes, mark the executable scope explicitly: MVP Epics 0-8 only, with Epic 9-10 queued for Phase 1.5 and Epics 11-15 requiring separate sprint selection.
- Preserve the existing Story 0.1 and Story 5.1 tenant-provisioning ownership notes during implementation handoff.
- Do not use Story 1.6 as a pattern for future story size; split any rework as already documented.
- Require each technical-leaning story to produce observable behavior, contract tests, or a narrow end-to-end proof so "done" cannot mean internal code only.
- Keep the operational-readiness "accepted/carried-forward" criteria out of product capability stories.

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK.

The planning artifact set is complete enough for serious implementation planning: PRD, architecture, epics/stories, and UX documents all exist; all 74 PRD functional requirements are traceable in the epics coverage map; UX and architecture align around the shared Evidence Packet trust model; and no missing required document type or duplicate artifact conflict was found.

However, the implementation plan is not cleanly ready to execute as-is because story quality needs tightening before developers rely on it. The main risk is not requirement coverage. The main risk is that technical/component stories can be completed without proving user-visible behavior, especially in Epic 1.

### Critical Issues Requiring Immediate Action

No critical blockers were found.

### Major Issues Requiring Action

1. Epic 1 contains technical stories that are not independently user-valuable.

   Stories 1.2 through 1.5 are mostly contracts, extraction, embedding, and indexing slices. They are testable, but they do not independently deliver a developer-visible outcome unless connected to an end-to-end proof.

2. Story 1.6 is too large for future rework.

   The epics document already admits this and gives a split recommendation. Treat that warning as binding for any future reimplementation or major change.

### Warnings and Minor Issues

- UX is full-horizon and includes future web/FrontComposer/Fluent UI guidance. MVP implementation must remain CLI-first unless a sprint change pulls web UI forward.
- Story numbering has historical aliases and gaps. This is documented, but automation and story-file lookup need care.
- Tenant provisioning is split between Story 0.1 and Story 5.1, but the current epics document includes explicit ownership boundaries. Preserve those boundaries during implementation.
- Operational-readiness epics allow accepted-risk and carry-forward outcomes. This is acceptable only because the document explicitly confines that pattern to operational stories.

### Recommended Next Steps

1. Add an observable proof requirement to technical Epic 1 stories, such as contract tests, CLI/API-visible behavior, or a narrow end-to-end validation artifact.
2. Pre-split Story 1.6 if any future implementation work touches that area.
3. Freeze the executable implementation scope as MVP Epics 0-8, with Epics 9-10 as Phase 1.5 and Epics 11-15 requiring explicit sprint selection.
4. Keep FR71 as an explicit Phase 2 deferral and do not let it appear as missing MVP scope during implementation review.
5. Protect the Evidence Packet contract early because it is the alignment point between PRD, UX, architecture, CLI, MCP, and future web surfaces.
6. Preserve the Story 0.1/Story 5.1 tenant provisioning ownership boundary so the minimum viable path and full lifecycle work do not diverge.

### Final Note

This assessment identified 0 critical blockers, 2 major issues, and 4 warnings/minor concerns across document discovery, PRD extraction, FR coverage, UX alignment, and epic/story quality. Address the major issues before proceeding to new implementation work. After those fixes, the artifact set should be ready to drive MVP implementation with strong traceability.

**Assessor:** Codex using bmad-check-implementation-readiness
**Completed:** 2026-05-17
