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
    - D:\Hexalith.Memories\_bmad-output\planning-artifacts\prd.md
  architecture:
    - D:\Hexalith.Memories\_bmad-output\planning-artifacts\architecture.md
  epics:
    - D:\Hexalith.Memories\_bmad-output\planning-artifacts\epics.md
  ux: []
missingDocuments:
  - UX design document
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-12
**Project:** Hexalith.Memories

## Document Discovery

### PRD Files Found

**Whole Documents:**
- `D:\Hexalith.Memories\_bmad-output\planning-artifacts\prd.md` (81,792 bytes, modified 2026-03-23 11:39:10)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- `D:\Hexalith.Memories\_bmad-output\planning-artifacts\architecture.md` (100,162 bytes, modified 2026-03-28 20:45:20)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- `D:\Hexalith.Memories\_bmad-output\planning-artifacts\epics.md` (156,875 bytes, modified 2026-05-12 19:08:55)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- None found

**Sharded Documents:**
- None found

### Issues Found

- No duplicate whole/sharded document formats found.
- Warning: UX design document not found. This may reduce assessment completeness.

## PRD Analysis

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
FR32: System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion
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
FR50: System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence
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
FR71: Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format
FR72: System exposes readiness and liveness health checks verifying all backends
FR73: Operator can detect index/graph divergence via consistency check
FR74: Operator can repair detected index/graph inconsistencies via consistency repair operation

Total FRs: 74

### Non-Functional Requirements

NFR1: Syntactic search latency (p95) <200ms at 10 concurrent queries/tenant and 10K memory units/tenant [MVP]
NFR2: Semantic search latency (p95) <500ms at 10 concurrent queries/tenant and 10K memory units/tenant [MVP]
NFR3: Hybrid search latency (p95) <1s at 10 concurrent queries/tenant and 10K memory units/tenant [MVP]
NFR4: Graph traversal latency (p95) <2s at 10 concurrent queries/tenant, 10K memory units/tenant, depth <=5 [MVP]
NFR5: Ingestion throughput >100 memory units/min for payloads <=10KB and >10 memory units/min for payloads <=1MB, per tenant, single-document embedding calls [Ongoing]
NFR6: Event indexing freshness <5s from DAPR pub/sub publication to searchable under normal conditions, with degradation documented when embedding provider is rate-limited [P1.5]
NFR7: Cold start time: service fully operational within 60s from containers running to accepting queries, excluding image pull time [Ongoing]
NFR8: Zero cross-tenant data leakage: no search, ingestion, or graph traversal returns data from another tenant; verify search, ingest, graph across malformed/empty/swapped tenant IDs and graph edge collisions [MVP]
NFR9: Embedding provider API keys stored in secure secret management (.NET User Secrets for local dev, DAPR Secrets API for deployed), never in config files or environment variables in production [Ongoing]
NFR10: All inter-service communication authenticated via DAPR API tokens [Ongoing]
NFR11: External access authenticated at ingress layer, with no unauthenticated access to REST API endpoints [P1.5]
NFR12: System supports linear scaling of tenants; adding a new tenant does not degrade existing tenant performance by more than 5%, validated at 10 tenants with 100K memory units each [Ongoing]
NFR13: Per-tenant ingestion pipeline scales independently; one tenant's batch ingestion does not block another tenant's real-time ingestion [Ongoing]
NFR14: Redis memory footprint per memory unit is predictable and documented so operators can estimate infrastructure costs before tenant provisioning [Ongoing]
NFR15: Architecture must not preclude backend migration from Redis to Qdrant; concrete implementation must identify extraction points without premature interfaces [Ongoing]
NFR16: Zero memory unit loss during Redis restart, with AOF persistence enabled and verified [MVP]
NFR17: Ingestion pipeline state survives process restarts; queued and in-progress units resume without data loss using DAPR actor state persistence [MVP]
NFR18: Partial backend failure results in degraded service, not total failure; available axes continue serving results [Ongoing]
NFR19: Failed ingestion units are never silently dropped; all failures are visible via CLI status with error details and failure stage [Ongoing]
NFR20: MCP tool responses conform to MCP protocol specification with valid tool schemas, typed parameters, and structured error responses [P1.5]
NFR21: DAPR pub/sub integration handles CloudEvents envelope format; events from any DAPR-compatible publisher are processable [P1.5]
NFR22: Embedding provider integration handles rate limiting gracefully; 429 responses trigger backoff without pipeline crash or data loss [Ongoing]
NFR23: CLI connects to the memory server via configurable endpoint and supports local dev, container, and remote ingress environments [Ongoing]
NFR24: All axis scores normalized to 0.0-1.0 before fusion: BM25 saturation normalization against corpus statistics, cosine native range, graph proximity inverse hop distance with decay [MVP]
NFR25: Fusion algorithm produces deterministic scores for same query and data; result ordering within same score tier may vary [MVP]
NFR26: Benchmark suite produces reproducible NDCG@10 results across repeated runs against the same dataset [MVP]
NFR27: Structured JSON logging with OpenTelemetry correlation IDs from DAPR trace context [Ongoing]
NFR28: Trace context propagates across all DAPR service invocation hops from CLI/MCP through server to backend [Ongoing]
NFR29: Custom metrics exported via OpenTelemetry: ingestion throughput, search latency per axis, index size per tenant, pipeline queue depth [Ongoing]
NFR30: Every CLI command includes `--help` with at least one usage example [MVP]
NFR31: README includes working quickstart that completes in <30 minutes on a clean machine with Docker installed [MVP]

Total NFRs: 31

### Additional Requirements

- MVP hard gates: three-axis validation passes at 80%, zero cross-tenant data leaks, and developer onboarding under 30 minutes. All hard gates must pass before shipping.
- MVP soft gates: causal chain completeness >=95%, MCP end-to-end integration works, and case model correctly scopes memory. At least 2 of 3 soft gates must pass.
- Three-axis kill switch: 80% of 5-10 benchmark queries requiring all three axes must show measurably better results from hybrid retrieval than any single axis alone, scored with NDCG@10 and >=80% inter-rater agreement.
- Phase 1 MVP validates the thesis via CLI and includes Memory Engine, Content Ingestion API, Three-Axis Search, Case/Folder Model, Tenant Isolation, benchmark-essential CLI, and Benchmark Suite.
- Phase 1.5 must ship EventStore Integration, MCP Server, and expanded CLI within 4 weeks of thesis validation; if this slips, MCP moves back into MVP.
- DAPR infrastructure is feature scaffolding for actors, state management, sidecars, service invocation, and pub/sub, not a separate story-estimated feature.
- Storage starts on Redis Stack/RediSearch/Redis Vector plus FalkorDB, with documented backend extraction points for later migration.
- Tenant isolation is physical: separate RediSearch indexes, vector indexes, FalkorDB graph data, and access-layer enforcement.
- CLI is the operational superset; MCP exposes agent-facing search, ingest, traverse, and case-info capabilities.
- Confidence scores indicate query-result relevance, not factual accuracy or data completeness; this distinction must appear in API docs, CLI explain output, and MCP schema docs.
- Gap detection in causal chains must be explicit and must not silently skip missing intermediate nodes.
- Compliance documentation must cover erasure limits, access telemetry limits, tenant deletion behavior, secret management, and license constraints.
- License constraints around Redis Stack SSPL/RSAL and FalkorDB AGPL must be documented, including managed-service implications and dependency/license boundary rationale.
- Implementation uses .NET 10/C# with Aspire AppHost orchestration, DAPR sidecars, REST ingress for external consumers, and DAPR service invocation for internal services.
- Unit tests mock DaprClient; integration tests use Aspire DistributedApplicationTestingBuilder or DAPR testcontainers; contract tests cover serialization and API envelopes.

### PRD Completeness Assessment

The PRD is broad and explicit: it contains 74 FRs, 31 NFRs, measurable MVP gates, validation protocols, interface expectations, architectural constraints, and phase boundaries. It is strong enough for traceability work. The main completeness risk is scope pressure: several journey-implied capabilities, especially diagnostics, replay, handlers, MCP, and EventStore integration, are phase-sensitive and must be checked against epics to ensure they are not accidentally promised in MVP or omitted from Phase 1.5 planning.

## Epic Coverage Validation

### Epic FR Coverage Extracted

FR1: Covered in Epic 1 — Ingest from local files
FR2: Covered in Epic 6 — Ingest from URLs
FR3: Covered in Epic 6 — Batch-ingest from directory
FR4: Covered in Epic 1 — Text extraction (Kreuzberg)
FR5: Covered in Epic 1 — Generate embeddings
FR6: Covered in Epic 1 — Memory unit fully searchable after ingestion
FR7: Covered in Epic 1 — Metadata with origin tracking
FR8: Covered in Epic 6 — Per-tenant ingestion load management
FR9: Covered in Epic 6 — Auto-retry with configurable limits
FR10: Covered in Epic 6 — Ingestion status per case
FR11: Covered in Epic 6 — Failed unit visibility
FR12: Covered in Epic 6 — Re-ingestion of failed content
FR13: Covered in Epic 1 — Partial backend write failure recovery (IngestionWorkflow saga/compensation)
FR14: Covered in Epic 2 — Syntactic search
FR15: Covered in Epic 2 — Semantic search
FR16: Covered in Epic 2 — Graph search
FR17: Covered in Epic 2 — Hybrid fusion search
FR18: Covered in Epic 2 — Axis selection control
FR19: Covered in Epic 2 — Per-axis score breakdown (explain)
FR20: Covered in Epic 3 — Filter search by case
FR21: Covered in Epic 3 — Filter search by metadata
FR22: Covered in Epic 2 — Pagination (search concern)
FR23: Covered in Epic 10 — Token budget (MCP)
FR24: Covered in Epic 2 — Origin identifier in results
FR25: Covered in Epic 2 — Benchmark comparisons
FR26: Covered in Epic 3 — Create case
FR27: Covered in Epic 3 — Delete case
FR28: Covered in Epic 3 — Add case members
FR29: Covered in Epic 3 — Remove case members
FR30: Covered in Epic 3 — List cases
FR31: Covered in Epic 3 — Case status
FR32: Covered in Epic 3 — Single-case ownership
FR33: Covered in Epic 3 — Case-scoped graph edges
FR34: Covered in Epic 3 — Cross-case tenant search
FR35: Covered in Epic 3 — Delete memory unit
FR36: Covered in Epic 3 — Case activity
FR37: Covered in Epic 3 — Annotations/corrections
FR38: Covered in Epic 5 — Create tenant
FR39: Covered in Epic 5 — Delete tenant
FR40: Covered in Epic 5 — Verify tenant isolation
FR41: Covered in Epic 5 — List tenants
FR42: Covered in Epic 5 — Update tenant config
FR43: Covered in Epic 5 — Prevent inconsistent config changes
FR44: Covered in Epic 5 — Tenant context enforcement
FR45: Covered in Epic 5 — View tenant configuration
FR46: Covered in Epic 1 — Index CausationId/CorrelationId as graph edges (creation during ingestion)
FR47: Covered in Epic 4 — Traverse causal chains
FR48: Covered in Epic 4 — Filter by edge type
FR49: Covered in Epic 4 — Gap markers for missing nodes
FR50: Covered in Epic 4 — Edge type taxonomy
FR51: Covered in Epic 4 — Promote AI-inferred confidence
FR52: Covered in Epic 4 — Chronological ordering
FR53: Covered in Epic 7 — CLI for all capabilities
FR54: Covered in Epic 10 — MCP tools
FR55: Covered in Epic 7 — CLI output formats
FR56: Covered in Epic 7 — Actionable CLI errors
FR57: Covered in Epic 7 — Discoverable actions
FR58: Covered in Epic 10 — MCP typed schemas
FR59: Covered in Epic 9 — Auto-discover event types
FR60: Covered in Epic 9 — Dual embeddings for events
FR61: Covered in Epic 9 — Auto-index CausationId/CorrelationId
FR62: Covered in Epic 9 — Handler registration management
FR63: Covered in Epic 2 — Composite confidence scores
FR64: Covered in Epic 7 — Metadata origin tracking display
FR65: Covered in Epic 1 — `ingested_by` field
FR66: Covered in Epic 5 — Partial results on backend failure
FR67: Covered in Epic 7 — Search/access telemetry
FR68: Covered in Epic 1 — Configure embedding provider
FR69: Covered in Epic 5 — Per-tenant rate limits
FR70: Covered in Epic 5 — Track embedding model per unit
FR71: Covered in Epic 8 — Export data
FR72: Covered in Epic 8 — Health checks
FR73: Covered in Epic 8 — Consistency check
FR74: Covered in Epic 8 — Consistency repair

Total FRs in epics: 74

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR1 | Developer can ingest content from local files into a specified case | Epic 1 — Ingest from local files | Covered |
| FR2 | Developer can ingest content from URLs into a specified case | Epic 6 — Ingest from URLs | Covered |
| FR3 | Developer can batch-ingest content from a directory into a specified case | Epic 6 — Batch-ingest from directory | Covered |
| FR4 | System can extract text from ingested content (plain text, PDF, markdown) | Epic 1 — Text extraction (Kreuzberg) | Covered |
| FR5 | System can generate embeddings for ingested content via a configurable embedding provider | Epic 1 — Generate embeddings | Covered |
| FR6 | System ensures a memory unit is fully searchable across all axes after ingestion completes | Epic 1 — Memory unit fully searchable after ingestion | Covered |
| FR7 | Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score | Epic 1 — Metadata with origin tracking | Covered |
| FR8 | System manages ingestion load per tenant independently | Epic 6 — Per-tenant ingestion load management | Covered |
| FR9 | System retries failed ingestion automatically with configurable limits | Epic 6 — Auto-retry with configurable limits | Covered |
| FR10 | Developer can view ingestion status per case (queued, embedding, indexed, failed counts) | Epic 6 — Ingestion status per case | Covered |
| FR11 | Developer can view failed ingestion units with error details and failure stage | Epic 6 — Failed unit visibility | Covered |
| FR12 | Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk | Epic 6 — Re-ingestion of failed content | Covered |
| FR13 | System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes) | Epic 1 — Partial backend write failure recovery | Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant | Epic 2 — Syntactic search | Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant | Epic 2 — Semantic search | Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant | Epic 2 — Graph search | Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes | Epic 2 — Hybrid fusion search | Covered |
| FR18 | Developer can control which axes are included in a search query | Epic 2 — Axis selection control | Covered |
| FR19 | Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode) | Epic 2 — Per-axis score breakdown | Covered |
| FR20 | Developer can filter search results by case | Epic 3 — Filter search by case | Covered |
| FR21 | Developer can filter search results by metadata field values | Epic 3 — Filter search by metadata | Covered |
| FR22 | Developer can paginate search results | Epic 2 — Pagination | Covered |
| FR23 | LLM Agent can constrain search response size by token budget | Epic 10 — Token budget | Covered |
| FR24 | System returns the origin identifier (file path, URL, or event ID) and origin type for each search result | Epic 2 — Origin identifier in results | Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output | Epic 2 — Benchmark comparisons | Covered |
| FR26 | Developer can create a case within a tenant | Epic 3 — Create case | Covered |
| FR27 | Developer can delete a case and all its memory units | Epic 3 — Delete case | Covered |
| FR28 | Developer can add members to a case | Epic 3 — Add case members | Covered |
| FR29 | Developer can remove members from a case | Epic 3 — Remove case members | Covered |
| FR30 | Developer can list cases within a tenant | Epic 3 — List cases | Covered |
| FR31 | Developer can view case status including memory unit count, last activity timestamp, and health indicators | Epic 3 — Case status | Covered |
| FR32 | System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion | Epic 3 — Single-case ownership | Covered |
| FR33 | System maintains case-scoped graph edges between memory units within a case | Epic 3 — Case-scoped graph edges | Covered |
| FR34 | Developer can search across all cases within a tenant by keyword, returning results with case attribution | Epic 3 — Cross-case tenant search | Covered |
| FR35 | Developer can delete an individual memory unit from a case | Epic 3 — Delete memory unit | Covered |
| FR36 | Developer can view recent activity within a case (ingestion events, searches, membership changes) | Epic 3 — Case activity | Covered |
| FR37 | Developer can annotate or correct a memory unit, with annotations tracked as linked memory units | Epic 3 — Annotations/corrections | Covered |
| FR38 | Operator can create a tenant with physically separate indexes | Epic 5 — Create tenant | Covered |
| FR39 | Operator can delete a tenant and all its indexes, graph data, and memory units | Epic 5 — Delete tenant | Covered |
| FR40 | Operator can verify tenant isolation via automated checks | Epic 5 — Verify tenant isolation | Covered |
| FR41 | Operator can list tenants | Epic 5 — List tenants | Covered |
| FR42 | Operator can update tenant configuration after creation (rate limits, display name, settings) | Epic 5 — Update tenant config | Covered |
| FR43 | System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment | Epic 5 — Prevent inconsistent config changes | Covered |
| FR44 | System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages | Epic 5 — Tenant context enforcement | Covered |
| FR45 | Operator can view current configuration of a tenant (embedding provider, rate limits, index status) | Epic 5 — View tenant configuration | Covered |
| FR46 | System can index CausationId and CorrelationId from events as typed, directional graph edges | Epic 1 — Index CausationId/CorrelationId as graph edges | Covered |
| FR47 | Developer can traverse causal chains from a starting node with configurable depth | Epic 4 — Traverse causal chains | Covered |
| FR48 | Developer can filter graph traversal by edge type | Epic 4 — Filter by edge type | Covered |
| FR49 | When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier | Epic 4 — Gap markers for missing nodes | Covered |
| FR50 | System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence | Epic 4 — Edge type taxonomy | Covered |
| FR51 | Developer can promote AI-inferred edge confidence when verifying a relationship | Epic 4 — Promote AI-inferred confidence | Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes | Epic 4 — Chronological ordering | Covered |
| FR53 | Developer can interact with all retrieval and ingestion capabilities via CLI | Epic 7 — CLI for all capabilities | Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools | Epic 10 — MCP tools | Covered |
| FR55 | CLI supports multiple output formats: human-readable (default), JSON, and table | Epic 7 — CLI output formats | Covered |
| FR56 | CLI provides actionable error messages with recovery suggestions for common failure modes | Epic 7 — Actionable CLI errors | Covered |
| FR57 | Developer can discover available actions from any system state, including empty states and error conditions | Epic 7 — Discoverable actions | Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions for LLM agent consumption | Epic 10 — MCP typed schemas | Covered |
| FR59 | System can auto-discover event types published to DAPR pub/sub topics | Epic 9 — Auto-discover event types | Covered |
| FR60 | System can generate dual embeddings for events (raw payload + natural language description) | Epic 9 — Dual embeddings for events | Covered |
| FR61 | System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code | Epic 9 — Auto-index CausationId/CorrelationId | Covered |
| FR62 | Developer can list registered event handlers and detect handler registration mismatches | Epic 9 — Handler registration management | Covered |
| FR63 | System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result | Epic 2 — Composite confidence scores | Covered |
| FR64 | System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit | Epic 7 — Metadata origin tracking display | Covered |
| FR65 | System records `ingested_by` (user or system identity) as a mandatory field on every memory unit | Epic 1 — `ingested_by` field | Covered |
| FR66 | When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded | Epic 5 — Partial results on backend failure | Covered |
| FR67 | System logs search and access events per tenant for audit purposes | Epic 7 — Search/access telemetry | Covered |
| FR68 | Operator can configure embedding provider and model per tenant | Epic 1 — Configure embedding provider | Covered |
| FR69 | System enforces per-tenant rate limit ceilings for embedding API calls | Epic 5 — Per-tenant rate limits | Covered |
| FR70 | System tracks the embedding provider and model used for each memory unit's vectors | Epic 5 — Track embedding model per unit | Covered |
| FR71 | Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format | Epic 8 — Export data | Covered |
| FR72 | System exposes readiness and liveness health checks verifying all backends | Epic 8 — Health checks | Covered |
| FR73 | Operator can detect index/graph divergence via consistency check | Epic 8 — Consistency check | Covered |
| FR74 | Operator can repair detected index/graph inconsistencies via consistency repair operation | Epic 8 — Consistency repair | Covered |

### Missing Requirements

No missing FR coverage found. No FRs appear in the epics coverage map that are absent from the PRD.

### Coverage Statistics

- Total PRD FRs: 74
- FRs covered in epics: 74
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

Not found. Searches for whole and sharded UX documentation under `D:\Hexalith.Memories\_bmad-output\planning-artifacts` returned no `*ux*.md` file and no `*ux*/index.md` shard.

### Alignment Issues

- No current MVP UI-specific UX artifact is required by the discovered planning set. The PRD classifies the product as a Developer Tool / API Backend, and the current implementation surfaces are CLI, MCP, REST ingress, and DAPR service invocation.
- Architecture aligns with that MVP surface: CLI is the reference/superset interface; MCP is scoped to LLM agent needs; MVP REST is minimal ingress for CLI connectivity; full REST/application UI support is deferred.
- The epics document explicitly states: "No UX Design document — this project is a Developer Tool / API Backend with no UI component. Developer experience is addressed via CLI (Epic 7) and MCP (Epic 10)."

### Warnings

- Future UI is implied outside the current MVP: PRD mentions Phase 2 "REST API for application search UIs" and Phase 3 "Memory Explorer UI, Timeline View"; Journey 8 describes an end user using a web application built on top of Memories.
- Before any REST application UI, Memory Explorer UI, timeline view, or admin-facing web experience is implemented, a UX artifact should be created or the scope should explicitly defer UI design to the consuming application.
- Current developer experience still has UX obligations through CLI/MCP: empty states, progress feedback, actionable errors, token-budget behavior, and explainability must remain covered by Epic 7 and Epic 10 acceptance criteria.

## Epic Quality Review

### Summary

The MVP and Phase 1.5 epics are generally traceable and implementable: they carry user/operator outcomes, Given/When/Then acceptance criteria, explicit FR references, and a reasonable gate order. However, the later operational epics introduce structural defects: out-of-order epics, forward story dependencies, and inconsistent heading levels. These are planning-readiness issues, not FR coverage gaps.

### Critical Violations

1. Epic 15 appears before Epic 14 while depending on Epic 14.
   - Evidence: Epic 15 states it closes "remaining high-value carry-forward risks from Epic 14", but Epic 14 is declared after Epic 15 in the document.
   - Impact: This creates a forward dependency and breaks the sequential epic model. A team could start Epic 15 before the source risks in Epic 14 are fully defined or closed.
   - Recommendation: Move Epic 14 before Epic 15, or remove Epic 15 from the active implementation sequence until Epic 14 has completed and produced its carry-forward list.

2. Story 13.1 depends on fields introduced in Story 13.4.
   - Evidence: Story 13.1 acceptance criteria require "Ollama-mode-required additive fields per Story 13.4" while also saying Story 13.4 lands the additive fields and tightens the AC.
   - Impact: Story 13.1 is not independently completable as written; it requires future story output.
   - Recommendation: Move additive contract fields into Story 13.1, reorder Story 13.4 before validation, or rewrite Story 13.1 to validate only the fields available at that point.

### Major Issues

1. Story 13.6 delegates part of its completion to future Story 13.7.
   - Evidence: Story 13.6 says the migration tool ships with documentation, but the `docs/operations/embedding-providers.md` runbook entry is carried by Story 13.7.
   - Impact: Story 13.6 is not independently shippable if operator documentation is part of the safety contract for live vector migration.
   - Recommendation: Put the migration runbook AC in Story 13.6, make Story 13.7 a previous prerequisite, or split Story 13.6 into tool-only and documented-operator-release stories.

2. Epic 13 uses `## Epic 13` while other epics use `### Epic N`.
   - Evidence: Epic 1-12, 14, and 15 use `### Epic`; Epic 13 uses `## Epic 13`.
   - Impact: Tools or agents scanning for `### Epic` headings miss Epic 13, which already happened during structural extraction.
   - Recommendation: Normalize Epic 13 heading level to match the rest of the file.

3. Epic 8 story ordering is inconsistent.
   - Evidence: Story 8.4 and Story 8.5 appear before Story 8.3.
   - Impact: This is not necessarily a dependency defect, but it creates sequencing ambiguity and weakens implementation handoff clarity.
   - Recommendation: Reorder stories numerically or explicitly document why 8.3 is intentionally later.

4. Operational hardening epics are more technical/process-oriented than product-capability-oriented.
   - Evidence: Epic 11 "CI/CD & Automated Quality Pipeline", Epic 12 "First Release & Operations Foundation", Epic 14 "Deferred Work Hardening and Operational Readiness", and Epic 15 "Carry-Forward Operational Risk Closure" focus on release mechanics, CI, deferred registers, and governance.
   - Impact: These can be valid if maintainers/operators are explicit personas, but they deviate from the product-capability epic shape and should not be mixed with user-facing feature epics without clear lifecycle labeling.
   - Recommendation: Keep them in an "Operational Readiness / Release Hardening" section with explicit maintainer/operator value, or move them to implementation/governance backlog artifacts separate from product epics.

### Minor Concerns

- Several later-story ACs allow outcomes like "resolved, accepted, or carried forward." These are auditable, but they can become process exits instead of concrete implementation outcomes unless each story defines what evidence is sufficient.
- Some story titles are implementation-component-centric, especially Story 1.2 "Memory Unit Domain Model & Contracts", Story 2.4 "Score Normalization", and Story 8.5 "Redis OTEL Instrumentation". Their ACs are mostly valid, but titles could better express the user/operator outcome.
- Epic 1 includes unavoidable greenfield setup work. This is acceptable because the architecture specifies Aspire starter scaffolding and the story includes single-command boot value, but its early stories should stay tightly scoped to first usable stack behavior.

### Best Practices Compliance Checklist

| Area | Result | Notes |
| --- | --- | --- |
| Epic user value | Partial pass | MVP epics are outcome-oriented; operational epics need clearer maintainer/operator framing. |
| Epic independence | Needs remediation | Epic 15 depends on later-declared Epic 14. |
| Story independence | Needs remediation | Story 13.1 and Story 13.6 contain forward dependencies. |
| Story sizing | Mostly pass | Stories are generally bounded; later governance stories are process-heavy but scoped. |
| Acceptance criteria | Pass with concerns | Most ACs use Given/When/Then and are testable; "accepted/carried-forward" exits need evidence discipline. |
| Database/entity timing | Pass | No evidence of all tables/indexes being created upfront outside the stories that need them. |
| Starter template requirement | Pass | Story 1.1 covers project scaffolding, submodules, AppHost boot, build, and dashboard/health basics. |
| Traceability to FRs | Pass | FR coverage map covers all 74 PRD FRs. |

### Actionable Recommendations

1. Reorder or split Epic 14 and Epic 15 so no epic depends on a later epic.
2. Normalize Epic 13 heading to `### Epic 13: Embedding Provider Pluggability + Vector Migration`.
3. Remove forward dependencies from Story 13.1 and Story 13.6 by moving required fields/docs into the story itself or reordering prerequisite stories.
4. Reorder Epic 8 stories numerically or explain the intended non-numeric sequence.
5. Mark operational epics as "Operational Readiness" and ensure each has a maintainer/operator outcome, not only a technical/process milestone.

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK

The planning set is strong on requirements coverage and MVP traceability, but it is not clean enough to call fully implementation-ready. The primary blockers are structural: out-of-order epics and forward dependencies in later work. These issues can confuse implementation sequencing and make some stories impossible to complete independently as written.

### Critical Issues Requiring Immediate Action

1. Epic 15 depends on Epic 14 but appears before it.
2. Story 13.1 depends on fields introduced later in Story 13.4.

### Additional Issues Requiring Attention

- Story 13.6 delegates required operator documentation to later Story 13.7.
- Epic 13 heading level is inconsistent and can be missed by tooling or agents.
- Epic 8 story numbering is out of order.
- Operational hardening epics need clearer lifecycle labeling and maintainer/operator value framing.
- No UX document exists. This is acceptable for current CLI/MCP/API MVP scope, but future UI work needs a UX artifact before implementation.

### Recommended Next Steps

1. Fix the epic sequence: place Epic 14 before Epic 15 or remove Epic 15 from active implementation scope until Epic 14 produces the carry-forward list.
2. Rewrite Story 13.1 so it has no dependency on future Story 13.4, or reorder Story 13.4 before Story 13.1.
3. Move Story 13.6 migration-runbook requirements into Story 13.6, or make documentation an explicit prerequisite before the migration tool can ship.
4. Normalize Epic 13 to the same heading level as other epics and reorder Epic 8 stories or document the non-numeric sequence.
5. Label Epics 11, 12, 14, and 15 as Operational Readiness / Release Hardening work, or move them to implementation governance artifacts separate from product capability epics.
6. Keep the current FR coverage map: it is complete and should be preserved while making structural edits.

### Final Note

This assessment identified 10 issues across 4 categories: document/UX warnings, critical sequencing violations, major story-quality issues, and minor planning hygiene concerns. The core MVP requirements are covered: 74 of 74 PRD FRs are mapped to epics. Address the critical and major planning issues before proceeding with broad implementation handoff.

**Assessor:** Codex using `bmad-check-implementation-readiness`
**Assessment Date:** 2026-05-12
