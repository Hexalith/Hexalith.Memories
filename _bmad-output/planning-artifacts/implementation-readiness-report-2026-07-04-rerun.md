---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
documentInventory:
  primary:
    prd: _bmad-output/planning-artifacts/prd.md
    architecture: _bmad-output/planning-artifacts/architecture.md
    epics: _bmad-output/planning-artifacts/epics.md
    ux: _bmad-output/planning-artifacts/ux-design-specification.md
  patternMatchesNotSelectedAsPrimary:
    epics:
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md
    ux:
      - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md
---
# Implementation Readiness Assessment Report

**Date:** 2026-07-04
**Project:** memories

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/prd.md` (86,662 bytes, modified 2026-06-27 08:02)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/architecture.md` (105,594 bytes, modified 2026-06-27 10:21)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/epics.md` (296,221 bytes, modified 2026-07-04 10:39)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md` (9,747 bytes, modified 2026-06-02 17:54; pattern match, not selected as primary)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/ux-design-specification.md` (99,240 bytes, modified 2026-06-27 08:02)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` (17,841 bytes, modified 2026-06-27 08:08; pattern match, not selected as primary)

**Sharded Documents:**
- None found

### Issues Found

- No duplicate whole/sharded document formats found.
- No required primary document category is missing.
- Two sprint change proposals matched the raw `epic`/`ux` filename patterns but were not selected as primary assessment documents.
- The default report path already contained a completed 2026-07-04 readiness assessment, so this run is recorded in `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04-rerun.md`.

## Step 2: PRD Analysis

### Functional Requirements

FR1: Developer can ingest content from local files into a specified case

FR2: Developer can ingest content from URLs into a specified case

FR3: Developer can batch-ingest content from a directory into a specified case

FR4: System can extract text from ingested content (plain text, PDF, markdown)

FR5: System can generate embeddings for ingested content via a configurable embedding provider

FR6: System ensures a memory unit is fully searchable across all axes after ingestion completes

FR7: Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score

FR8: System manages ingestion load per tenant independently

FR9: System retries failed ingestion automatically with configurable limits

FR10: Developer can view ingestion status per case (queued, embedding, indexed, failed counts)

FR11: Developer can view failed ingestion units with error details and failure stage

FR12: Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk

FR13: System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes)

FR14: Developer can search memory units by syntactic matching within a tenant

FR15: Developer can search memory units by semantic similarity within a tenant

FR16: Developer can search memory units by graph traversal within a tenant

FR17: Developer can search memory units by hybrid fusion combining all available axes

FR18: Developer can control which axes are included in a search query

FR19: Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode)

FR20: Developer can filter search results by case

FR21: Developer can filter search results by metadata field values

FR22: Developer can paginate search results

FR23: LLM Agent can constrain search response size by token budget

FR24: System returns the origin identifier (file path, URL, or event ID) and origin type for each search result

FR25: Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output

FR26: Developer can create a case within a tenant

FR27: Developer can delete a case and all its memory units

FR28: Developer can add members to a case

FR29: Developer can remove members from a case

FR30: Developer can list cases within a tenant

FR31: Developer can view case status including memory unit count, last activity timestamp, and health indicators

FR32: System enforces strict single-case ownership per memory unit - reassignment requires deletion and re-ingestion

FR33: System maintains case-scoped graph edges between memory units within a case

FR34: Developer can search across all cases within a tenant by keyword, returning results with case attribution

FR35: Developer can delete an individual memory unit from a case

FR36: Developer can view recent activity within a case (ingestion events, searches, membership changes)

FR37: Developer can annotate or correct a memory unit, with annotations tracked as linked memory units

FR38: Operator can create a tenant with physically separate indexes

FR39: Operator can delete a tenant and all its indexes, graph data, and memory units

FR40: Operator can verify tenant isolation via automated checks

FR41: Operator can list tenants

FR42: Operator can update tenant configuration after creation (rate limits, display name, settings)

FR43: System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment

FR44: System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages

FR45: Operator can view current configuration of a tenant (embedding provider, rate limits, index status)

FR46: System can index CausationId and CorrelationId from events as typed, directional graph edges

FR47: Developer can traverse causal chains from a starting node with configurable depth

FR48: Developer can filter graph traversal by edge type

FR49: When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier

FR50: System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` - each with default confidence

FR51: Developer can promote AI-inferred edge confidence when verifying a relationship

FR52: System maintains chronological ordering and timestamps on causal chain nodes

FR53: Developer can interact with all retrieval and ingestion capabilities via CLI

FR54: Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools

FR55: CLI supports multiple output formats: human-readable (default), JSON, and table

FR56: CLI provides actionable error messages with recovery suggestions for common failure modes

FR57: Developer can discover available actions from any system state, including empty states and error conditions

FR58: MCP tools include typed parameter schemas with descriptions for LLM agent consumption

FR59: System can auto-discover event types published to DAPR pub/sub topics

FR60: System can generate dual embeddings for events (raw payload + natural language description)

FR61: System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code

FR62: Developer can list registered event handlers and detect handler registration mismatches

FR63: System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result

FR64: System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit

FR65: System records `ingested_by` (user or system identity) as a mandatory field on every memory unit

FR66: When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded

FR67: System logs search and access events per tenant for audit purposes

FR68: Operator can configure embedding provider and model per tenant

FR69: System enforces per-tenant rate limit ceilings for embedding API calls

FR70: System tracks the embedding provider and model used for each memory unit's vectors

FR71: Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. Phase: Phase 2 unless a later sprint change explicitly pulls export into MVP.

FR72: System exposes readiness and liveness health checks verifying all backends

FR73: Operator can detect index/graph divergence via consistency check

FR74: Operator can repair detected index/graph inconsistencies via consistency repair operation

Total FRs: 74

### Non-Functional Requirements

NFR1: Syntactic search latency p95 is less than 200ms at 10 concurrent queries per tenant and 10K memory units per tenant. Phase: MVP.

NFR2: Semantic search latency p95 is less than 500ms at 10 concurrent queries per tenant and 10K memory units per tenant. Phase: MVP.

NFR3: Hybrid search latency p95 is less than 1s at 10 concurrent queries per tenant and 10K memory units per tenant. Phase: MVP.

NFR4: Graph traversal latency p95 is less than 2s at 10 concurrent queries per tenant, 10K memory units per tenant, and depth <= 5. Phase: MVP.

NFR5: Ingestion throughput is greater than 100 memory units/min for payloads <= 10KB and greater than 10 memory units/min for payloads <= 1MB, per tenant, using single-document embedding calls rather than batched calls. Phase: Ongoing.

NFR6: Event indexing freshness is less than 5s from DAPR pub/sub publication to searchable under normal conditions, with degradation documented when the embedding provider is rate-limited. Phase: P1.5.

NFR7: Cold start time has the service fully operational within 60s from containers running to accepting queries, excluding image pull time. Phase: Ongoing.

NFR8: Zero cross-tenant data leakage: no search, ingestion, or graph traversal returns data from another tenant. Verification requires automated tests across all axes with malformed, empty, and swapped tenant IDs, plus graph-specific edge-collision checks. Phase: MVP.

NFR9: Embedding provider API keys are stored in secure secret management (.NET User Secrets for local dev, DAPR Secrets API for deployed) and never in config files or environment variables in production. Verification: code review plus secret scanning in CI. Phase: Ongoing.

NFR10: All inter-service communication is authenticated via DAPR API tokens. Verification: DAPR configuration validation. Phase: Ongoing.

NFR11: External access is authenticated at the ingress layer with no unauthenticated access to REST API endpoints. Verification: integration test with unauthenticated requests. Phase: P1.5.

NFR12: System supports linear scaling of tenants; adding a new tenant does not degrade existing tenant performance by more than 5%. Target validation: 10 tenants, each with 100K memory units, measuring tenant 1 alone and after 9 loaded tenants are added. Phase: Ongoing.

NFR13: Per-tenant ingestion pipeline scales independently so one tenant's batch ingestion does not block another tenant's real-time ingestion. Verification: concurrent ingestion test across 3 tenants. Phase: Ongoing.

NFR14: Redis memory footprint per memory unit is predictable and documented so operators can estimate infrastructure costs before tenant provisioning. Target: published sizing guide by vector dimension and metadata size. Phase: Ongoing.

NFR15: Architecture must not preclude backend migration from Redis to Qdrant; the concrete implementation must identify clear extraction points without premature interfaces. Verification: architecture review for extraction points and absence of Redis-specific tight coupling in domain logic. Phase: Ongoing.

NFR16: Zero memory unit loss during Redis restart. Target: AOF persistence enabled and verified. Phase: MVP.

NFR17: Ingestion pipeline state survives process restarts, with queued and in-progress units resuming without data loss. Target: DAPR actor state persistence verified. Phase: MVP.

NFR18: Partial backend failure of one of the three backends results in degraded service, not total failure, and available axes continue serving results. Verification: chaos test killing each backend individually and verifying partial results. Phase: Ongoing.

NFR19: Failed ingestion units are never silently dropped; all failures are visible via CLI status with error details and failure stage. Verification: end-to-end tests with intentional failures at each pipeline stage. Phase: Ongoing.

NFR20: MCP tool responses conform to the MCP protocol specification, including valid tool schemas, typed parameters, and structured error responses. Verification: MCP protocol conformance test suite. Phase: P1.5.

NFR21: DAPR pub/sub integration handles CloudEvents envelope format and can process events from any DAPR-compatible publisher. Verification: integration test with standard CloudEvents payloads. Phase: P1.5.

NFR22: Embedding provider integration handles rate limiting gracefully; 429 responses trigger backoff without pipeline crash or data loss. Verification: rate limit simulation test per provider. Phase: Ongoing.

NFR23: CLI connects to the memory server via configurable endpoint supporting local dev, container, and remote ingress URL environments. Verification: configuration layering test across all three environments. Phase: Ongoing.

NFR24: All axis scores are normalized to 0.0-1.0 before fusion: BM25 via saturation normalization against corpus statistics, cosine similarity native range, and graph proximity via inverse hop distance with decay. Verification: normalization unit tests with known inputs and outputs. Phase: MVP.

NFR25: Fusion algorithm produces deterministic scores: the same query against the same data produces identical composite scores, while result ordering within the same score tier may vary. Verification: 100 repeated queries with zero score variance. Phase: MVP.

NFR26: Benchmark suite produces reproducible results: running benchmarks twice against the same dataset yields identical NDCG@10 scores. Verification: reproducibility test in CI. Phase: MVP.

NFR27: Structured JSON logging uses OpenTelemetry correlation IDs from DAPR trace context. Verification: log format validation. Phase: Ongoing.

NFR28: Trace context propagates across all DAPR service invocation hops from CLI/MCP through server to backend. Verification: distributed trace completeness test. Phase: Ongoing.

NFR29: Custom metrics are exported via OpenTelemetry, including ingestion throughput, search latency per axis, index size per tenant, and pipeline queue depth. Target: Aspire dashboard shows all metrics during local development. Phase: Ongoing.

NFR30: Every CLI command includes `--help` with at least one usage example. Verification: CLI help completeness test parses all commands and verifies example presence. Phase: MVP.

NFR31: README includes a working quickstart that completes in less than 30 minutes on a clean machine with Docker installed. Verification: timed walkthrough on a clean environment. Phase: MVP.

Total NFRs: 31

### Additional Requirements

- The MVP strategy is proof of thesis: validate three-axis retrieval before broad integration surfaces, while carrying cases and multi-tenancy from day one.
- Implementation must establish scaffold, AppHost, ServiceDefaults, minimum build/test feedback, tenant provisioning, case bootstrap, and tenant/case validation before ingestion, indexing, search, or graph stories write data.
- Tenant provisioning owns physically isolated infrastructure creation; ingestion and indexing must fail before backend writes when tenant or case context is missing or mismatched.
- MVP hard gates are three-axis validation at 80%, zero cross-tenant data leaks, and developer onboarding under 30 minutes.
- At least 2 of 3 MVP soft gates must pass: causal chain completeness >= 95%, MCP end-to-end integration works, and case model correctly scopes memory.
- If the 80% three-axis benchmark threshold fails, graph-axis investment must be re-evaluated before scope expansion.
- EventStore integration is a fast-follow commitment within 4 weeks of thesis validation; if that slips, MCP moves back into MVP.
- The Memories Server is the sidecar-managed event subscriber for Hexalith modules; modules publish CloudEvents to DAPR pub/sub, and the server maps events to tenant/case memory.
- Memories is framed as interpretive infrastructure with clear responsibility boundaries among storage, interpretation, and application layers.
- Tenant deletion enables erasure of tenant indexes, graph data, and memory units, but cross-references in other tenants remain an application responsibility.
- Access telemetry is infrastructure telemetry, not a tamper-evident audit trail.
- Confidence scores measure query-result relevance, not factual accuracy or data completeness.
- Evidence Packet semantics unify confidence, source attribution, token-budget handling, degradation signaling, tenant/case scope, result state, and recovery guidance across CLI JSON, MCP, and future web UI.
- Causal chain traversal must return structured, ordered, directional, timestamped, gap-aware results; missing intermediate nodes must be explicit rather than silently skipped.
- Event graph edge taxonomy must distinguish `caused_by` from `correlated_with`.
- The project license is Apache 2.0; dependency-license risk for Redis Stack and FalkorDB must be documented.
- Redis Vector Search schema is fixed at creation, so embedding provider changes require full tenant reindex.
- Shared embedding API keys create shared provider ceilings; per-tenant throttles do not eliminate the shared-key bottleneck.
- Per-tenant pipeline actor owns throttling, queueing, ordering, progress tracking, retry, and durable pipeline state.
- CLI is the operational superset; MCP exposes LLM-agent-facing search, ingest, traverse, and case-info capabilities.
- External consumers connect through infrastructure ingress; internal services communicate through DAPR.
- Errors must preserve Hexalith.Commons envelope context across server, MCP, ingress, and CLI surfaces.
- Release package scope is controlled by `tools/release-packages.json`.
- Local development is orchestrated through .NET Aspire AppHost with DAPR sidecars, Redis, FalkorDB, Server, and MCP Server.

### PRD Completeness Assessment

The PRD is materially complete for readiness validation. It includes explicit FR and NFR numbering, validation phases, MVP gates, user journeys, package topology, interface parity, operational constraints, compliance boundaries, licensing risks, and test strategy. The strongest traceability anchors are FR1-FR74, NFR1-NFR31, the MVP Go/No-Go gates, and the Evidence Packet definition.

The main caution is scope density: the PRD deliberately mixes MVP, Phase 1.5, Phase 2, Phase 3, and ongoing requirements in one artifact. Coverage validation must therefore distinguish active MVP readiness from fast-follow, growth, and future vision coverage rather than treating every requirement as a same-phase implementation obligation.

## Step 3: Epic Coverage Validation

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --------- | --------------- | ------------- | ------ |
| FR1 | Developer can ingest content from local files into a specified case | Epic 1 - Ingest from local files | Covered |
| FR2 | Developer can ingest content from URLs into a specified case | Epic 6 - Ingest from URLs | Covered |
| FR3 | Developer can batch-ingest content from a directory into a specified case | Epic 6 - Batch-ingest from directory | Covered |
| FR4 | System can extract text from ingested content (plain text, PDF, markdown) | Epic 1 - Text extraction with Kreuzberg | Covered |
| FR5 | System can generate embeddings for ingested content via a configurable embedding provider | Epic 1 - Generate embeddings | Covered |
| FR6 | System ensures a memory unit is fully searchable across all axes after ingestion completes | Epic 1; reinforced by Epic 23 for scalable chunking and batch embedding | Covered |
| FR7 | Developer can attach metadata to ingested content, with each field tracking its origin and confidence score | Epic 1 - Metadata with origin tracking | Covered |
| FR8 | System manages ingestion load per tenant independently | Epic 6 - Per-tenant ingestion load management | Covered |
| FR9 | System retries failed ingestion automatically with configurable limits | Epic 6 - Auto-retry with configurable limits | Covered |
| FR10 | Developer can view ingestion status per case | Epic 6 - Ingestion status per case | Covered |
| FR11 | Developer can view failed ingestion units with error details and failure stage | Epic 6 - Failed unit visibility | Covered |
| FR12 | Developer can manually trigger re-ingestion of failed or previously ingested content | Epic 6; reinforced by Epic 23 for non-URL re-ingestion correctness | Covered |
| FR13 | System handles partial backend write failures with defined recovery behavior | Epic 1; reinforced by Epic 21 for ratified consistency and migration safety | Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant | Epic 2 - Syntactic search | Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant | Epic 2 - Semantic search | Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant | Epic 2 - Graph search | Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes | Epic 2 - Hybrid fusion search | Covered |
| FR18 | Developer can control which axes are included in a search query | Epic 2 - Axis selection control | Covered |
| FR19 | Developer can view per-axis score breakdown for each search result | Epic 2 - Explain mode | Covered |
| FR20 | Developer can filter search results by case | Epic 3 - Filter search by case | Covered |
| FR21 | Developer can filter search results by metadata field values | Epic 3 - Filter search by metadata | Covered |
| FR22 | Developer can paginate search results | Epic 2; reinforced by Epic 22 for semantic, graph-scoped, and hybrid pagination correctness | Covered |
| FR23 | LLM Agent can constrain search response size by token budget | Epic 10 - MCP token budget with deterministic omitted-detail expansion handles | Covered |
| FR24 | System returns origin identifier and origin type for each search result | Epic 2 - Origin identifier in results | Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search results | Epic 2 - Benchmark comparisons | Covered |
| FR26 | Developer can create a case within a tenant | Epic 0 and Epic 3 - Minimal case bootstrap, then full case management | Covered |
| FR27 | Developer can delete a case and all its memory units | Epic 3 - Delete case | Covered |
| FR28 | Developer can add members to a case | Epic 3 - Add case members | Covered |
| FR29 | Developer can remove members from a case | Epic 3 - Remove case members | Covered |
| FR30 | Developer can list cases within a tenant | Epic 3 - List cases | Covered |
| FR31 | Developer can view case status | Epic 3 - Case status | Covered |
| FR32 | System enforces strict single-case ownership per memory unit | Epic 3 - Single-case ownership | Covered |
| FR33 | System maintains case-scoped graph edges between memory units within a case | Epic 3 - Case-scoped graph edges | Covered |
| FR34 | Developer can search across all cases within a tenant with case attribution | Epic 3; reinforced by Epic 22 for fusion case attribution | Covered |
| FR35 | Developer can delete an individual memory unit from a case | Epic 3 - Delete memory unit | Covered |
| FR36 | Developer can view recent activity within a case | Epic 3 - Case activity | Covered |
| FR37 | Developer can annotate or correct a memory unit | Epic 3 - Annotations and corrections | Covered |
| FR38 | Operator can create a tenant with physically separate indexes | Epic 0 and Epic 5; reinforced by Epic 24 for physical isolation strategy | Covered |
| FR39 | Operator can delete a tenant and all its indexes, graph data, and memory units | Epic 5; reinforced by Epic 21 for deletion completeness | Covered |
| FR40 | Operator can verify tenant isolation via automated checks | Epic 5; reinforced by Epic 24 for verifier scaling | Covered |
| FR41 | Operator can list tenants | Epic 5 - List tenants | Covered |
| FR42 | Operator can update tenant configuration after creation | Epic 5 - Update tenant configuration | Covered |
| FR43 | System prevents configuration changes that would create data inconsistency without explicit acknowledgment | Epic 5 - Prevent inconsistent configuration changes | Covered |
| FR44 | System enforces tenant context at all access layers | Epic 0 and Epic 5; reinforced by Epic 20 authorization and Epic 24 physical isolation | Covered |
| FR45 | Operator can view current tenant configuration | Epic 5 - View tenant configuration | Covered |
| FR46 | System can index CausationId and CorrelationId as typed directional graph edges | Epic 1 - Event graph edge creation during ingestion | Covered |
| FR47 | Developer can traverse causal chains from a starting node | Epic 4 - Causal chain traversal | Covered |
| FR48 | Developer can filter graph traversal by edge type | Epic 4 - Edge type filtering | Covered |
| FR49 | Traversal result includes a gap marker for missing intermediate nodes | Epic 4 - Gap markers | Covered |
| FR50 | System supports required edge types and default confidence values | Epic 4 - Edge type taxonomy | Covered |
| FR51 | Developer can promote AI-inferred edge confidence | Epic 4 - Confidence promotion | Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes | Epic 4 - Chronological ordering | Covered |
| FR53 | Developer can interact with all retrieval and ingestion capabilities via CLI | Epic 7 - CLI capabilities | Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info via MCP tools | Epic 10 - MCP tools | Covered |
| FR55 | CLI supports human-readable, JSON, and table output formats | Epic 7 - CLI output formats | Covered |
| FR56 | CLI provides actionable error messages with recovery suggestions | Epic 7 - Actionable CLI errors | Covered |
| FR57 | Developer can discover available actions from any system state | Epic 7 - Discoverable actions | Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions | Epic 10 - MCP typed schemas | Covered |
| FR59 | System can auto-discover event types published to DAPR pub/sub topics | Epic 9 - Event auto-discovery | Covered |
| FR60 | System can generate dual embeddings for events | Epic 9 - Dual embeddings | Covered |
| FR61 | System can automatically index CausationId/CorrelationId without mapping code | Epic 9 - Auto-index causal metadata | Covered |
| FR62 | Developer can list registered event handlers and detect handler mismatches | Epic 9 - Handler registration management | Covered |
| FR63 | System returns composite confidence scores with per-axis breakdowns | Epic 2 - Composite confidence and Evidence Packet mapping | Covered |
| FR64 | System tracks metadata origin and confidence per field | Epic 7 - Metadata origin tracking display | Covered |
| FR65 | System records `ingested_by` as a mandatory memory-unit field | Epic 1 - `ingested_by` field | Covered |
| FR66 | System returns partial results with excluded axes when search backends are unavailable | Epic 5 - Partial results on backend failure | Covered |
| FR67 | System logs search and access events per tenant for audit purposes | Epic 7; reinforced by Epic 20 for audit completeness | Covered |
| FR68 | Operator can configure embedding provider and model per tenant | Epic 1 - Google MVP config with extensible provider shape; provider expansion is post-MVP unless pulled forward | Covered |
| FR69 | System enforces per-tenant rate limit ceilings for embedding API calls | Epic 5 - Per-tenant rate limits | Covered |
| FR70 | System tracks embedding provider and model used for each memory unit | Epic 5 - Track embedding model per unit | Covered |
| FR71 | Developer can export all memory units, metadata, and graph edges for a case or tenant | Epic 26 - backup/restore and operational readiness slice; broader portable export remains Phase 2 unless pulled forward | Covered with scope caveat |
| FR72 | System exposes readiness and liveness health checks verifying all backends | Epic 8 - Health checks | Covered |
| FR73 | Operator can detect index/graph divergence via consistency check | Epic 8 - Consistency check | Covered |
| FR74 | Operator can repair detected index/graph inconsistencies | Epic 8 - Consistency repair | Covered |

### Missing Requirements

No PRD FRs are missing from the epics coverage map. The epics document explicitly names FR1 through FR74 and the extracted unique FR set contains no FR IDs outside the PRD range.

### Coverage Statistics

- Total PRD FRs: 74
- FRs covered in epics: 74
- Coverage percentage: 100%
- FRs in epics but not in PRD: 0

### Coverage Notes

- The epics document explicitly warns that FR coverage is 100% traceable, but MVP implementation scope is not 100% of FR1-FR74.
- Machine-readable readiness metadata under `_bmad-output/implementation-artifacts/sprint-status.yaml` is authoritative for readiness accounting; coverage alone must not be used to infer MVP implementation readiness.
- FR71 requires careful handling: Epic 26 covers an operational backup/restore and disaster-recovery slice, while broader application-facing portable export remains Phase 2 unless explicitly sprint-selected.

## Step 4: UX Alignment Assessment

### UX Document Status

Found.

Primary UX document:
- `_bmad-output/planning-artifacts/ux-design-specification.md` (99,240 bytes, modified 2026-06-27 08:02)

Relevant approved UX change-control document:
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` (17,841 bytes, modified 2026-06-27 08:08)

### UX to PRD Alignment

The UX specification aligns strongly with the PRD's product thesis and user journeys. The PRD defines the product around why-oriented questions, source-backed causal answers, tenant/case scope, CLI and MCP surfaces, token-budget behavior, confidence caveats, degraded backend behavior, and future application experiences. The UX specification turns those into a cross-surface Evidence Packet model with trust fundamentals: scope, source, reasoning, state, and recovery.

Specific alignment points:
- PRD trust requirements FR19, FR23, FR24, FR63, FR66, and FR67 align with the UX Evidence Packet, Trust Strip, omitted-detail, source, degradation, and recovery patterns.
- PRD CLI and MCP requirements FR53-FR58 align with UX guidance that CLI is keyboard-driven and MCP is schema-first, bounded, source-attributed, confidence-aware, and structured.
- PRD case and causal-intelligence requirements FR26-FR37 and FR46-FR52 align with UX Case Activity Trail, Source Citation Stack, Graph Path Summary, and evidence inspection patterns.
- PRD no-result, empty-state, onboarding, and debug journeys align with UX recovery-state grammar for no match, pending ingestion, wrong scope, inaccessible data, stale memory, degraded backend, graph gaps, and insufficient evidence.
- The approved 2026-06-24 UX change strengthens alignment with repository rules by making FrontComposer and Fluent UI Blazor V5-only web composition mandatory.

No critical PRD-to-UX mismatch was found.

### UX to Architecture Alignment

The architecture supports the UX direction materially:
- `Contracts.V1` owns the shared Evidence Packet grammar used by CLI JSON output, MCP tool responses, and future web UI composition.
- Architecture minimum Evidence Packet fields cover scope, result, sources, evidence, graph, state, omitted details, and recovery actions, matching the UX Evidence Packet anatomy.
- Architecture explicitly phase-splits surfaces: MVP CLI essentials, Phase 1.5 MCP/EventStore, Phase 2 full REST, and future Epic 17 web RCL.
- Architecture states that `Hexalith.Memories.Web` is a FrontComposer-aligned Razor component library over `Contracts.V1` Evidence Packet semantics, using FrontComposer and Microsoft Fluent UI Blazor V5 only.
- Graph traversal response shape returns full node context and is token-budget-aware via MCP, supporting UX Graph Path Summary and Agent Packet Inspector needs.
- FR/NFR traceability in architecture maps all 74 FRs and all 31 NFRs, with active MVP, Phase 1.5, and deferred status called out.

No critical UX-to-architecture support gap was found.

### Alignment Issues

1. Full-horizon UX must not be treated as MVP scope. The UX document covers CLI, MCP, and future web UI. Architecture correctly phase-splits these, but implementation planning must keep MVP acceptance limited to CLI-visible and contract-visible Evidence Packet semantics unless a sprint change pulls web work forward.

2. Existing web RCL conformance is a known cleanup item, not a new discovery. The approved 2026-06-24 sprint-change proposal notes that Story 17.1 created useful web RCL work but also raw Razor markup and scoped CSS that need FrontComposer/Fluent UI V5 conformance hardening. Story 17.6 is the planned remediation gate before extending Stories 17.2-17.5.

3. End-user application and full REST experiences remain future/deferred. PRD journeys like Priya's downstream application flow are supported directionally, but architecture places full REST drill-down and application UI support outside MVP. Readiness assessment should not count those as MVP blockers unless they are sprint-selected.

### Warnings

- Treat UX-DR and Epic 17 web work as future web scope unless explicitly pulled into MVP by an approved sprint change.
- Before any web implementation, enforce the FrontComposer/Fluent UI Blazor V5 boundary and read the Hexalith UX instructions. Raw controls, standalone design-system primitives, legacy Fluent v4/FAST tokens, and theme-recreating CSS are not acceptable when FrontComposer or Fluent UI V5 equivalents exist.
- Browser, axe, forced-colors, reduced-motion, zoom/reflow, touch, and manual screen-reader validation are planned under future web validation; they are not proven by CLI/MCP contract readiness alone.
- Evidence Packet contract semantics are the keystone. Any drift between CLI JSON, MCP responses, and future web UI would create a UX/architecture misalignment even if individual surfaces work locally.

## Step 5: Epic Quality Review

### Review Scope

- Epic headings reviewed: 27 (Epic 0 through Epic 26)
- Story headings reviewed: 147
- Lifecycle labels present: 27
- Readiness-accounting validation: passed via `python3 _bmad/scripts/validate_readiness_accounting.py`

### Best-Practices Compliance Summary

| Area | Assessment |
| --- | --- |
| Epic user value | Strong for active MVP product epics 0-8, Phase 1.5 product epics 9-10, future UI Epic 17, and remediation product-quality epics 22-23. Weaker by design for operational, governance, code-health, release, and remediation epics. |
| Epic independence | Acceptable for active MVP when Epic 0 is treated as an explicit foundation gate. Later epics mostly depend only on earlier outputs or declared decision gates. |
| Story sizing | Good for many active product stories. Known broad historical exceptions remain Stories 1.2, 1.5, and 1.6; checkpoint-heavy stories require extra evidence controls. |
| Forward dependencies | Previously risky non-numeric dependencies are now declared in `story_execution_order` for Epic 17, Epic 18, and Epic 23, and the validator passes. |
| Acceptance criteria | Most stories use testable Given/When/Then criteria. Operational and remediation stories often use evidence/decision criteria rather than user-facing behavior. |
| Resource creation timing | Strong planning invariant: tenant infrastructure is owned by `TenantProvisioningWorkflow`; ingestion/indexing must not create tenant resources on demand. Later audit-remediation Story 23.7 shows this invariant still needs implementation hardening. |
| Starter template/setup | Covered by Architecture and Story 0.0, with Story 0.4 adding the minimum build/test CI preflight. |
| Traceability | Strong. FR coverage is complete and phase/accounting metadata now separates MVP, P1.5, operational, future UI, and remediation tracks. |

### Critical Violations

No unresolved critical epic-quality violation remains in the active MVP path after the 2026-07-04 cleanup. The prior high-risk sequencing issue is materially addressed by:

- `readiness_accounting` in `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `story_execution_order` for Epic 17, Epic 18, and Epic 23
- Passing `_bmad/scripts/validate_readiness_accounting.py`

### Major Issues

1. **Mixed product and non-product epic tracks require constant enforcement.**
   Epics 11-16, 19-21, and 24-26 are operational, governance, remediation, deployment, test, security, data-integrity, observability, and code-health tracks. Under strict create-epics-and-stories standards, these are not normal product-value epics. They are acceptable only because the plan lifecycle-labels them and `readiness_accounting` excludes them from MVP product readiness unless sprint-selected.

   Recommendation: keep `readiness_accounting` validation in the normal planning/status workflow and fail any report/tooling that infers MVP readiness from FR coverage or story status alone.

2. **Broad historical stories must remain closed or be split before rework.**
   Stories 1.2, 1.5, and 1.6 are explicitly accepted as historical broad technical/bundled slices but are not valid future implementation units. Their own guards require split stories if contract, indexing, or ingestion-orchestration work reopens.

   Recommendation: block any new story file that reuses these broad historical keys for implementation. Require new numeric split stories with observable API/CLI/contract/trace/integration proof.

3. **Checkpoint-heavy stories need evidence-table or child-story enforcement.**
   Stories such as 13.2, 13.6, 15.6, 21.9, and 26.5 contain multiple independently reviewable checkpoints. The epics document says checkpoint-heavy stories may remain umbrella tracking stories only if each checkpoint is separately implemented, reviewed, and evidenced.

   Recommendation: before selecting any checkpoint-heavy backlog story, either split checkpoints into child stories or add a checklist evidence table with owner, validation command/artifact, review status, and completion date.

4. **Decision-first gates must remain hard gates, not advisory prose.**
   Story 21.1 gates Epic 21 consistency implementation; Story 24.3 gates physical tenant-isolation enforcement work. These are valid planning patterns only if implementation selection refuses dependent work before the decision is ratified.

   Recommendation: add `story_execution_order` entries if future tooling cannot enforce the decision-first language from story text alone.

5. **Resource-ownership invariant has a known implementation drift remediation.**
   The planning model correctly says tenant infrastructure must be provisioned by workflow and not created by ingestion/indexing activities. Audit-remediation Story 23.7 exists because current implementation still has per-document index provisioning behavior to clean up.

   Recommendation: do not treat the resource-ownership invariant as fully implemented for post-audit readiness until Story 23.7 or an equivalent remediation is complete.

### Minor Concerns

1. **Historical duplicate story-key artifact can confuse naive scanners.**
   `_bmad-output/implementation-artifacts/2-7-benchmark-suite-and-thesis-validation.md` remains as a historical artifact while current Story 2.7 is Evidence Packet Contract Mapping and Benchmark Suite is Story 2.8. The current story files explain the alias, and sprint status uses current keys, but simple filename-prefix scanners will still see duplicate `2-7`.

   Recommendation: keep alias handling explicit in validation tooling, or move historical artifacts to a clearly ignored archive path if future tools keep tripping over it.

2. **Story-status ordering and execution ordering differ in Epic 23.**
   `development_status` lists Story 23.1 before Story 23.9 because numeric story keys are preserved, while `story_execution_order.epic-23` correctly requires 23.9 before 23.1.

   Recommendation: story-selection tools must always read `story_execution_order` before numeric order.

3. **Future web validation remains phase-scoped.**
   Epic 17 is marked done, but the UX specification itself says browser/accessibility validation becomes binding when web UI work enters an approved implementation phase. This is acceptable only because future web is excluded from MVP readiness.

   Recommendation: avoid counting Epic 17 completion as proof of MVP CLI/MCP readiness, and avoid counting CLI/MCP contract readiness as proof of browser accessibility.

### Epic Independence Notes

- Epic 0 intentionally breaks the usual "first epic is product capability" pattern by serving as a tenant/case safety foundation. This is acceptable because it delivers operator/developer-observable safety outcomes and prevents unsafe data-writing stories.
- Epic 1 depends on Epic 0 by design. That dependency is explicit and complete in sprint status.
- Epic 2 through Epic 8 form a reasonable MVP progression: ingestion/search foundation, three-axis validation, case model, causal traversal, tenant isolation, ingestion operations, CLI developer experience, and health/consistency.
- Epic 9 and Epic 10 are Phase 1.5 fast-follow and should not be used to block MVP thesis validation unless explicitly pulled forward.
- Epics 20-26 are audit-remediation backlog, not active MVP product scope.

### Epic Quality Recommendations

1. Keep `python3 _bmad/scripts/validate_readiness_accounting.py` in the normal readiness/status validation lane.
2. Treat `story_execution_order` as mandatory for all non-numeric story sequences.
3. Require split stories or evidence tables before executing checkpoint-heavy backlog work.
4. Preserve the strict rule that broad historical stories cannot be reopened as single implementation units.
5. Keep operational/remediation/code-health epics lifecycle-labeled and excluded from product readiness unless a sprint change explicitly selects them.
6. Add or preserve validation that ignores or explicitly aliases historical story artifacts such as the old `2-7` benchmark file.

## Summary and Recommendations

### Overall Readiness Status

READY for scoped implementation with guardrails.

The planning artifacts are complete enough to support controlled story execution. Required primary documents exist, PRD functional coverage is complete across epics, UX and architecture are aligned around the Evidence Packet and FrontComposer/Fluent UI boundary, and the highest-risk sequencing metadata now validates successfully.

This is not a blanket approval for broad automated implementation. Any story-selection or sprint-status tooling must consume `readiness_accounting` and `story_execution_order`; otherwise the plan can still be misread because product, operational, future-web, and audit-remediation work all coexist in one epic stream.

### Critical Issues Requiring Immediate Action

No critical issue blocks scoped story execution from the planning-artifact side.

Immediate guardrails before implementation selection:

1. Run `python3 _bmad/scripts/validate_readiness_accounting.py` before selecting work from sprint status.
2. Honor `story_execution_order` before numeric story order, especially for Epic 17, Epic 18, and Epic 23.
3. Do not reopen broad historical Stories 1.2, 1.5, or 1.6 as single implementation units.
4. For checkpoint-heavy backlog stories, split the work or require per-checkpoint evidence before marking complete.
5. Treat Epics 20-26 as audit-remediation backlog, not active MVP product scope.

### Recommended Next Steps

1. Add the readiness-accounting validator to the normal readiness/status workflow so it cannot be skipped by future agents or scripts.
2. Ensure story-selection tooling reads `story_execution_order` and refuses dependent stories whose declared prerequisites are incomplete.
3. Before selecting any Epic 21 or Epic 24 implementation story, confirm decision-first gates 21.1 and 24.3 are ratified where applicable.
4. Before selecting Story 23.1, confirm Story 23.9 is selected or complete first.
5. If future web work resumes, require Story 17.6 conformance evidence before extending or reopening Stories 17.2-17.5.
6. Decide whether the historical `2-7-benchmark-suite-and-thesis-validation.md` artifact should remain with explicit alias handling or move to an ignored archive location.

### Finding Count

This assessment identified 18 findings, cautions, or guardrails across 7 categories:

- Document inventory cautions: 1
- PRD completeness/scope cautions: 1
- FR coverage caveats: 1
- UX alignment issues: 3
- UX implementation warnings: 4
- Major epic-quality issues: 5
- Minor epic-quality concerns: 3

Critical blockers: 0.

### Final Note

The artifacts are now materially ready for disciplined implementation. The key condition is discipline: keep readiness metadata authoritative, enforce non-numeric story order, and preserve the separation between product readiness and operational/remediation backlog work.

**Assessment completed:** 2026-07-04
**Assessor:** BMAD Implementation Readiness workflow, executed by Codex
