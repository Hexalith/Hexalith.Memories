---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
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
