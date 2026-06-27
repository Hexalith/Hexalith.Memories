---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
documentsIncluded:
  - type: prd
    path: _bmad-output/planning-artifacts/prd.md
  - type: architecture
    path: _bmad-output/planning-artifacts/architecture.md
  - type: epics
    path: _bmad-output/planning-artifacts/epics.md
  - type: ux
    path: _bmad-output/planning-artifacts/ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-06-27
**Project:** memories

## Document Discovery

### PRD Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/prd.md` (86,662 bytes, modified 2026-06-27 08:02)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/architecture.md` (104,724 bytes, modified 2026-06-27 09:48)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/epics.md` (245,184 bytes, modified 2026-06-27 09:48)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md` (9,747 bytes, modified 2026-06-02 17:54)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/ux-design-specification.md` (99,240 bytes, modified 2026-06-27 08:02)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` (17,841 bytes, modified 2026-06-27 08:08)

**Sharded Documents:**
- None found

### File Selection

Primary documents selected for assessment:

- PRD: `_bmad-output/planning-artifacts/prd.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Epics & Stories: `_bmad-output/planning-artifacts/epics.md`
- UX Design: `_bmad-output/planning-artifacts/ux-design-specification.md`

Additional sprint change proposals were discovered and noted, but not selected as primary assessment documents.

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

NFR1: Syntactic search latency (p95) target is <200ms under 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.

NFR2: Semantic search latency (p95) target is <500ms under 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.

NFR3: Hybrid search latency (p95) target is <1s under 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.

NFR4: Graph traversal latency (p95) target is <2s under 10 concurrent queries/tenant, 10K memory units/tenant, and depth <=5. Phase: MVP.

NFR5: Ingestion throughput target is >100 memory units/min for payloads <=10KB and >10 memory units/min for payloads <=1MB, per tenant, using single-document embedding calls rather than batching. Phase: Ongoing.

NFR6: Event indexing freshness target is <5s from DAPR pub/sub publication to searchable under normal conditions, with degradation documented when the embedding provider is rate-limited. Phase: P1.5.

NFR7: Cold start time target is service fully operational within 60s from containers running to accepting queries, excluding image pull time. Phase: Ongoing.

NFR8: Zero cross-tenant data leakage: no search, ingestion, or graph traversal returns data from another tenant. Verification must include search, ingest, and graph across all axes with malformed/empty/swapped tenant IDs, plus graph-specific identical graph structures in tenants A and B with edge-ID collision checks. Phase: MVP.

NFR9: Embedding provider API keys must be stored in secure secret management (.NET User Secrets for local dev, DAPR Secrets API for deployed) and never in config files or environment variables in production. Verification: code review and secret scanning in CI. Phase: Ongoing.

NFR10: All inter-service communication must be authenticated via DAPR API tokens. Verification: DAPR configuration validation. Phase: Ongoing.

NFR11: External access must be authenticated at the ingress layer with no unauthenticated REST API endpoint access. Verification: integration test with unauthenticated requests. Phase: P1.5.

NFR12: System supports linear scaling of tenants, where adding a new tenant does not degrade existing tenant performance by more than 5%; validate at 10 tenants with 100K memory units each by benchmarking tenant 1 alone, adding 9 loaded tenants, re-benchmarking tenant 1, and measuring delta. Phase: Ongoing.

NFR13: Per-tenant ingestion pipelines scale independently so one tenant's batch ingestion does not block another tenant's real-time ingestion. Verification: concurrent ingestion test across 3 tenants. Phase: Ongoing.

NFR14: Redis memory footprint per memory unit is predictable and documented so operators can estimate infrastructure costs before tenant provisioning. Target: published sizing guide by vector dimension and metadata size. Phase: Ongoing.

NFR15: Architecture must not preclude backend migration from Redis to Qdrant; use concrete implementation with clear extraction points identified and no premature interfaces. Verification: architecture review proving extraction points and no Redis-specific coupling in domain logic. Phase: Ongoing.

NFR16: Zero memory unit loss during Redis restart. Target: AOF persistence enabled and verified. Phase: MVP.

NFR17: Ingestion pipeline state survives process restarts; queued and in-progress units resume without data loss. Verification: DAPR actor state persistence. Phase: MVP.

NFR18: Partial backend failure, where one of three backends is down, results in degraded service rather than total failure, with available axes continuing to serve results. Verification: chaos test killing each backend individually and verifying partial results. Phase: Ongoing.

NFR19: Failed ingestion units are never silently dropped; all failures are visible via CLI status with error details and failure stage. Verification: end-to-end test with intentional failures at each pipeline stage. Phase: Ongoing.

NFR20: MCP tool responses conform to the MCP protocol specification, including valid tool schemas, typed parameters, and structured error responses. Verification: MCP protocol conformance test suite. Phase: P1.5.

NFR21: DAPR pub/sub integration handles CloudEvents envelope format so events from any DAPR-compatible publisher are processable. Verification: integration test with standard CloudEvents payloads. Phase: P1.5.

NFR22: Embedding provider integration handles rate limiting gracefully; 429 responses trigger backoff without pipeline crash or data loss. Verification: rate limit simulation test per provider. Phase: Ongoing.

NFR23: CLI connects to the memory server via configurable endpoint and supports local dev (localhost), container (docker service name), and remote (ingress URL) environments. Verification: configuration layering test across all three environments. Phase: Ongoing.

NFR24: All axis scores are normalized to 0.0-1.0 before fusion: BM25 via saturation normalization against corpus statistics, cosine similarity by native range, and graph proximity via inverse hop distance with decay. Verification: normalization unit tests with known inputs/outputs. Phase: MVP.

NFR25: Fusion algorithm produces deterministic scores; the same query against the same data produces identical composite scores, while result ordering within the same score tier may vary. Verification: 100 repeated queries with zero score variance. Phase: MVP.

NFR26: Benchmark suite produces reproducible results; running benchmarks twice against the same dataset yields identical NDCG@10 scores. Verification: reproducibility test in CI. Phase: MVP.

NFR27: Structured JSON logging with OpenTelemetry correlation IDs from DAPR trace context. Verification: log format validation. Phase: Ongoing.

NFR28: Trace context propagates across all DAPR service invocation hops, creating an end-to-end trace from CLI/MCP through server to backend. Verification: distributed trace completeness test. Phase: Ongoing.

NFR29: Custom metrics are exported via OpenTelemetry: ingestion throughput, search latency per axis, index size per tenant, and pipeline queue depth. Target: Aspire dashboard shows all metrics during local development. Phase: Ongoing.

NFR30: Every CLI command includes `--help` with at least one usage example. Verification: CLI help completeness test parsing all commands and verifying example presence. Phase: MVP.

NFR31: README includes a working quickstart that completes in <30 minutes on a clean machine with Docker installed. Verification: timed walkthrough on clean environment. Phase: MVP.

Total NFRs: 31

### Additional Requirements

- Success gates: the MVP has three hard gates (three-axis validation passes at 80%, zero cross-tenant data leaks, and developer onboarding under 30 minutes) and three soft gates (causal chain completeness >=95%, MCP end-to-end integration works, and case model correctly scopes memory). All hard gates must pass and at least two soft gates must pass.
- Three-axis kill switch: 80% of benchmark queries must show measurably better NDCG@10 from hybrid retrieval than any single axis. If not, graph-axis investment must be re-evaluated before expanding scope.
- Benchmark scoring protocol: ground truth is defined by Jerome plus two independent reviewers before queries are written; automated scoring uses NDCG@10; disputes receive human review; inter-rater agreement must be >=80% before a benchmark is valid.
- Implementation sequencing: foundation path must precede ingestion/indexing/search writes: buildable scaffold/AppHost/ServiceDefaults, minimum build/test feedback, tenant provisioning, minimal case bootstrap, and tenant/case validation guards.
- MVP scope: proof-of-thesis MVP covers Memory Engine, Content Ingestion API, Three-Axis Search, Case/Folder Model, Tenant Isolation, CLI benchmark essentials, and Benchmark Suite. EventStore integration, MCP, and expanded CLI are committed Phase 1.5 capabilities within four weeks of thesis validation unless pulled into MVP by schedule risk.
- Compliance boundary: Memories is interpretive infrastructure. It owns accurate embeddings, causal chains, calibrated confidence, complete edge graphs, physical tenant isolation, deletion primitives, and access telemetry, while applications own legal decisions and certified audit trails.
- Evidence Packet: CLI JSON output, MCP responses, and future web UI must share one response envelope for confidence, degradation state, omitted details, source attribution, recovery guidance, tenant/case scope, and result state. Its concrete shape is owned by `Contracts.V1`.
- Confidence distinction: search confidence scores measure query-result relevance, not factual accuracy or data completeness. This caveat must appear in API docs, CLI explain output, compliance guidance, and MCP response schema docs.
- Causal data responsibility: traversal results must provide ordered nodes, timestamps, typed directional edges, edge confidence, and explicit gap markers for missing intermediate nodes.
- Edge taxonomy: MVP edge types are `caused_by`, `correlated_with`, `references`, `contains`, and `annotates`; `caused_by` and `correlated_with` must not be collapsed.
- Licensing: project license is Apache 2.0. Redis Stack SSPL/RSAL and FalkorDB AGPL implications must be documented, including managed-service constraints and the network-service boundary.
- Package architecture: `Server` depends on `Contracts` only; Redis implementation registers at composition root; `tools/release-packages.json` remains the authoritative package inventory.
- Deployment topology: .NET Aspire orchestrates Memories Server, MCP Server, Redis, FalkorDB, and DAPR sidecars. External consumers reach REST through infrastructure-managed ingress; internal services use DAPR invocation/pub-sub.
- Embedding provider constraints: MVP runtime provider is Google `text-embedding-004`; per-tenant provider/model/rate-limit configuration is required; changing provider dimensions requires full tenant reindex.
- Ingestion pipeline constraints: a per-tenant DAPR actor owns bounded queueing, throttling, progress tracking, retries, and durable state; document processing remains stateless.
- Interface parity constraints: CLI is operational superset; MCP exposes LLM-needed search/ingest/traverse/case-info; DAPR service invocation is internal programmatic API.
- CLI configuration precedence: command flags, environment variables, config file, DAPR Secrets API, .NET User Secrets, then DAPR configuration.
- Documentation requirements: README quickstart, CLI help, getting started guide, API reference, compliance enablement guide, and operator guide are required.
- Test strategy: unit tests mock DaprClient, integration tests use Aspire/DAPR infrastructure, and contract tests verify serialization and service/REST/error envelopes.

### PRD Completeness Assessment

The PRD is materially complete for readiness analysis: it defines product intent, success gates, phased scope, user journeys, 74 functional requirements, 31 non-functional requirements, and detailed domain constraints. The requirement set is strong enough for traceability validation because FR/NFR IDs are stable and most NFRs include measurable targets plus verification methods.

Initial risks to validate in later steps:

- Phase boundary pressure: several journeys describe Phase 1.5 or later capabilities, while MVP implementation readiness depends on clear epic placement.
- Cross-surface trust envelope: the Evidence Packet is a central contract and must be represented consistently in architecture and epics.
- Operational claims: zero data loss, zero cross-tenant leakage, degraded backend behavior, and benchmark reproducibility require explicit test/epic coverage.
- Scope consistency: FR71 is explicitly Phase 2, while most other FRs do not carry phase tags, so epic coverage must distinguish MVP/P1.5/Ongoing obligations.

## Epic Coverage Validation

### Epic FR Coverage Extracted

- FR1: Epic 1 - Ingest from local files
- FR2: Epic 6 - Ingest from URLs
- FR3: Epic 6 - Batch-ingest from directory
- FR4: Epic 1 - Text extraction (Kreuzberg)
- FR5: Epic 1 - Generate embeddings
- FR6: Epic 1 - Memory unit fully searchable after ingestion
- FR7: Epic 1 - Metadata with origin tracking
- FR8: Epic 6 - Per-tenant ingestion load management
- FR9: Epic 6 - Auto-retry with configurable limits
- FR10: Epic 6 - Ingestion status per case
- FR11: Epic 6 - Failed unit visibility
- FR12: Epic 6 - Re-ingestion of failed content
- FR13: Epic 1 - Partial backend write failure recovery (IngestionWorkflow saga/compensation)
- FR14: Epic 2 - Syntactic search
- FR15: Epic 2 - Semantic search
- FR16: Epic 2 - Graph search
- FR17: Epic 2 - Hybrid fusion search
- FR18: Epic 2 - Axis selection control
- FR19: Epic 2 - Per-axis score breakdown (explain)
- FR20: Epic 3 - Filter search by case
- FR21: Epic 3 - Filter search by metadata
- FR22: Epic 2 - Pagination
- FR23: Epic 10 - Token budget (MCP), including deterministic omitted-detail expansion handles
- FR24: Epic 2 - Origin identifier in results
- FR25: Epic 2 - Benchmark comparisons
- FR26: Epic 0 + Epic 3 - Minimal case bootstrap, then full case management
- FR27: Epic 3 - Delete case
- FR28: Epic 3 - Add case members
- FR29: Epic 3 - Remove case members
- FR30: Epic 3 - List cases
- FR31: Epic 3 - Case status
- FR32: Epic 3 - Single-case ownership
- FR33: Epic 3 - Case-scoped graph edges
- FR34: Epic 3 - Cross-case tenant search
- FR35: Epic 3 - Delete memory unit
- FR36: Epic 3 - Case activity
- FR37: Epic 3 - Annotations/corrections
- FR38: Epic 0 + Epic 5 - Tenant creation and isolated infrastructure provisioning
- FR39: Epic 5 - Delete tenant
- FR40: Epic 5 - Verify tenant isolation
- FR41: Epic 5 - List tenants
- FR42: Epic 5 - Update tenant config
- FR43: Epic 5 - Prevent inconsistent config changes
- FR44: Epic 0 + Epic 5 - Tenant context validation and enforcement
- FR45: Epic 5 - View tenant configuration
- FR46: Epic 1 - Index CausationId/CorrelationId as graph edges
- FR47: Epic 4 - Traverse causal chains
- FR48: Epic 4 - Filter by edge type
- FR49: Epic 4 - Gap markers for missing nodes
- FR50: Epic 4 - Edge type taxonomy
- FR51: Epic 4 - Promote AI-inferred confidence
- FR52: Epic 4 - Chronological ordering
- FR53: Epic 7 - CLI for all capabilities
- FR54: Epic 10 - MCP tools
- FR55: Epic 7 - CLI output formats
- FR56: Epic 7 - Actionable CLI errors
- FR57: Epic 7 - Discoverable actions
- FR58: Epic 10 - MCP typed schemas
- FR59: Epic 9 - Auto-discover event types
- FR60: Epic 9 - Dual embeddings for events
- FR61: Epic 9 - Auto-index CausationId/CorrelationId
- FR62: Epic 9 - Handler registration management
- FR63: Epic 2 - Composite confidence scores and Evidence Packet contract mapping
- FR64: Epic 7 - Metadata origin tracking display
- FR65: Epic 1 - `ingested_by` field
- FR66: Epic 5 - Partial results on backend failure
- FR67: Epic 7 - Search/access telemetry
- FR68: Epic 1 - Configure Google embedding provider for MVP with extensible provider/model/dimensions/rate-limit shape
- FR69: Epic 5 - Per-tenant rate limits
- FR70: Epic 5 - Track embedding model per unit
- FR71: Phase 2 placeholder - Portable case/tenant export
- FR72: Epic 8 - Health checks
- FR73: Epic 8 - Consistency check
- FR74: Epic 8 - Consistency repair

Total FRs in epics: 74

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
|---|---|---|---|
| FR1 | Developer can ingest content from local files into a specified case | Epic 1 | Covered |
| FR2 | Developer can ingest content from URLs into a specified case | Epic 6 | Covered |
| FR3 | Developer can batch-ingest content from a directory into a specified case | Epic 6 | Covered |
| FR4 | System can extract text from ingested content (plain text, PDF, markdown) | Epic 1 | Covered |
| FR5 | System can generate embeddings for ingested content via a configurable embedding provider | Epic 1 | Covered |
| FR6 | System ensures a memory unit is fully searchable across all axes after ingestion completes | Epic 1 | Covered |
| FR7 | Developer can attach metadata to ingested content, with each field tracking its origin and confidence score | Epic 1 | Covered |
| FR8 | System manages ingestion load per tenant independently | Epic 6 | Covered |
| FR9 | System retries failed ingestion automatically with configurable limits | Epic 6 | Covered |
| FR10 | Developer can view ingestion status per case | Epic 6 | Covered |
| FR11 | Developer can view failed ingestion units with error details and failure stage | Epic 6 | Covered |
| FR12 | Developer can manually trigger re-ingestion of failed or previously ingested content | Epic 6 | Covered |
| FR13 | System handles partial backend write failures with recovery behavior | Epic 1 | Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant | Epic 2 | Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant | Epic 2 | Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant | Epic 2 | Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes | Epic 2 | Covered |
| FR18 | Developer can control which axes are included in a search query | Epic 2 | Covered |
| FR19 | Developer can view per-axis score breakdown and normalization method | Epic 2 | Covered |
| FR20 | Developer can filter search results by case | Epic 3 | Covered |
| FR21 | Developer can filter search results by metadata field values | Epic 3 | Covered |
| FR22 | Developer can paginate search results | Epic 2 | Covered |
| FR23 | LLM Agent can constrain search response size by token budget | Epic 10 | Covered - P1.5 |
| FR24 | System returns origin identifier and origin type for each search result | Epic 2 | Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search | Epic 2 | Covered |
| FR26 | Developer can create a case within a tenant | Epic 0 + Epic 3 | Covered |
| FR27 | Developer can delete a case and all its memory units | Epic 3 | Covered |
| FR28 | Developer can add members to a case | Epic 3 | Covered |
| FR29 | Developer can remove members from a case | Epic 3 | Covered |
| FR30 | Developer can list cases within a tenant | Epic 3 | Covered |
| FR31 | Developer can view case status with memory count, activity, and health | Epic 3 | Covered |
| FR32 | System enforces strict single-case ownership per memory unit | Epic 3 | Covered |
| FR33 | System maintains case-scoped graph edges between memory units | Epic 3 | Covered |
| FR34 | Developer can search across all cases within a tenant with case attribution | Epic 3 | Covered |
| FR35 | Developer can delete an individual memory unit from a case | Epic 3 | Covered |
| FR36 | Developer can view recent activity within a case | Epic 3 | Covered |
| FR37 | Developer can annotate or correct a memory unit | Epic 3 | Covered |
| FR38 | Operator can create a tenant with physically separate indexes | Epic 0 + Epic 5 | Covered |
| FR39 | Operator can delete a tenant and all indexes, graph data, and memory units | Epic 5 | Covered |
| FR40 | Operator can verify tenant isolation via automated checks | Epic 5 | Covered |
| FR41 | Operator can list tenants | Epic 5 | Covered |
| FR42 | Operator can update tenant configuration after creation | Epic 5 | Covered |
| FR43 | System prevents inconsistent configuration changes without acknowledgment | Epic 5 | Covered |
| FR44 | System enforces tenant context at all access layers | Epic 0 + Epic 5 | Covered |
| FR45 | Operator can view current tenant configuration | Epic 5 | Covered |
| FR46 | System can index CausationId and CorrelationId as typed directional graph edges | Epic 1 | Covered |
| FR47 | Developer can traverse causal chains from a starting node | Epic 4 | Covered |
| FR48 | Developer can filter graph traversal by edge type | Epic 4 | Covered |
| FR49 | Traversal result includes gap marker for missing intermediate node | Epic 4 | Covered |
| FR50 | System supports required edge types with default confidence | Epic 4 | Covered |
| FR51 | Developer can promote AI-inferred edge confidence | Epic 4 | Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes | Epic 4 | Covered |
| FR53 | Developer can interact with all retrieval and ingestion capabilities via CLI | Epic 7 | Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info via MCP | Epic 10 | Covered - P1.5 |
| FR55 | CLI supports human-readable, JSON, and table output | Epic 7 | Covered |
| FR56 | CLI provides actionable errors with recovery suggestions | Epic 7 | Covered |
| FR57 | Developer can discover actions from any state, including empty/error states | Epic 7 | Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions | Epic 10 | Covered - P1.5 |
| FR59 | System can auto-discover event types from DAPR pub/sub topics | Epic 9 | Covered - P1.5 |
| FR60 | System can generate dual embeddings for events | Epic 9 | Covered - P1.5 |
| FR61 | System can auto-index CausationId/CorrelationId metadata as graph edges | Epic 9 | Covered - P1.5 |
| FR62 | Developer can list event handlers and detect mismatches | Epic 9 | Covered - P1.5 |
| FR63 | System returns composite confidence scores with per-axis breakdowns | Epic 2 | Covered |
| FR64 | System tracks metadata origin and confidence on every memory unit | Epic 7 | Covered |
| FR65 | System records `ingested_by` on every memory unit | Epic 1 | Covered |
| FR66 | System returns partial results with unavailable axes indicated | Epic 5 | Covered |
| FR67 | System logs search and access events per tenant | Epic 7 | Covered |
| FR68 | Operator can configure embedding provider and model per tenant | Epic 1 | Covered |
| FR69 | System enforces per-tenant embedding rate limit ceilings | Epic 5 | Covered |
| FR70 | System tracks embedding provider/model used for each memory unit | Epic 5 | Covered |
| FR71 | Developer can export memory units, metadata, and graph edges for case or tenant | Phase 2 placeholder | Traceable - Deferred |
| FR72 | System exposes readiness and liveness health checks verifying all backends | Epic 8 | Covered |
| FR73 | Operator can detect index/graph divergence via consistency check | Epic 8 | Covered |
| FR74 | Operator can repair detected index/graph inconsistencies | Epic 8 | Covered |

### Missing Requirements

No PRD functional requirements are missing from the epics document. FR1-FR74 all have traceability.

Important phase caveat: FR71 is traceable only to a Phase 2 placeholder and is explicitly excluded from active MVP readiness unless a later sprint change pulls export forward. FR23, FR54, FR58, and FR59-FR62 are traced to Phase 1.5 fast-follow epics rather than MVP active scope.

No FR IDs appear in the epic coverage map that are absent from the PRD.

### Coverage Statistics

- Total PRD FRs: 74
- FRs covered in epics/stories or explicit backlog placeholders: 74
- Missing FRs: 0
- Coverage percentage: 100%
- Active MVP scope caveat: 1 FR (`FR71`) is deferred to Phase 2 by explicit planning decision; 7 FRs (`FR23`, `FR54`, `FR58`, `FR59`, `FR60`, `FR61`, `FR62`) are Phase 1.5 fast-follow.

## UX Alignment Assessment

### UX Document Status

Found.

Primary UX document:

- `_bmad-output/planning-artifacts/ux-design-specification.md` (99,240 bytes, modified 2026-06-27 08:02)

Related UX governance document reviewed:

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` (17,841 bytes, modified 2026-06-27 08:08)

No sharded UX folder was found.

### UX to PRD Alignment

The UX specification aligns strongly with the PRD's product thesis and user journeys.

- The UX "recoverable trust" model directly supports the PRD's Evidence Packet definition, confidence caveat, source attribution, token-budget behavior, degraded result handling, and tenant/case scope requirements.
- The UX journey set maps to PRD journeys: Alex onboarding and debug path, LLM agent MCP consumption, Kenji tenant verification, Marcus case briefing, and future downstream application users.
- CLI and MCP are treated as first-class UX surfaces, matching PRD FR53-FR58 and the PRD interface capability matrix.
- The UX specification's absence/recovery states align with PRD requirements for actionable CLI errors, no-result guidance, token-budget omission handling, degraded backend indicators, ingestion status, and tenant mismatch errors.
- The UX specification adds detailed future web requirements (Trust Strip, Scope Header, Source Citation Stack, Agent Packet Inspector, responsive behavior, WCAG 2.2 AA checks). These do not conflict with the PRD because the PRD already treats web UI as future work and the epics classify Epic 17 as future web UI unless a sprint change pulls it forward.

UX requirements not explicitly present as PRD FRs are still represented in epics as UX-DR1 through UX-DR40. They are mostly implementation and presentation constraints over PRD concepts rather than new product capabilities.

### UX to Architecture Alignment

The architecture supports the core UX needs.

- `Contracts.V1` owns the shared Evidence Packet grammar used by CLI JSON output, MCP responses, and future web UI.
- The architecture defines Evidence Packet minimum fields for `scope`, `result`, `sources`, `evidence`, `graph`, `state`, `omittedDetails`, and `recoveryActions`, matching UX packet anatomy.
- The interface philosophy explicitly separates CLI, MCP, REST, DAPR service invocation, and future web UI by capability while preserving semantic consistency.
- The architecture supports token-budget and omitted-detail behavior through MCP and Evidence Packet fields.
- Error propagation uses stable error code, human-readable message, and recovery suggestion, aligning with UX recovery-state requirements.
- Tenant validation, physical tenant isolation, and tenant/case guardrails support the UX requirement that scope errors are trust-blocking.
- Search and traversal architecture supports UX explainability through per-axis score breakdowns, graph summaries, gap markers, source attribution, and degraded-axis handling.
- The architecture now documents the Epic 17 web RCL as FrontComposer-aligned and Fluent UI Blazor V5-only, with conformance tests for any unavoidable custom markup/CSS exceptions.

### Alignment Issues

1. **Architecture phase-coverage wording is internally inconsistent.**
   The architecture says MVP is CLI-only with MCP and EventStore integration in Phase 1.5, but its Requirements Coverage section also says "FR1-70, FR72-74" are covered in MVP. That statement incorrectly includes Phase 1.5 FRs (`FR23`, `FR54`, `FR58`, `FR59`, `FR60`, `FR61`, `FR62`) when compared with PRD and epics. This does not block traceability, but it can mislead implementation readiness accounting.

2. **Future web UX conformance depends on Story 17.6.**
   The approved 2026-06-24 sprint change identifies raw Razor markup/scoped CSS drift in the existing Story 17.1 RCL and adds Story 17.6 for FrontComposer/Fluent UI Blazor V5 conformance hardening. Planning artifacts now align, but web UX implementation should not continue into Stories 17.2-17.5 until Story 17.6 or equivalent conformance cleanup is completed.

3. **Some UX validation details live in epics rather than architecture.**
   Responsive breakpoints, WCAG 2.2 AA validation, no-hover-only behavior, focus management, and no-color-only state semantics are detailed in the UX specification and Epic 17, but architecture only records the higher-level web RCL boundary. This is acceptable while web UI remains future scope, but architecture should be updated if Epic 17 becomes active product readiness scope.

### Warnings

- No warning for missing UX documentation: UX documentation exists and is complete.
- Warning for scope accounting: UX/MCP/web planning is phase-split. MVP readiness should not count Epic 9, Epic 10, Epic 17, or FR71 as active MVP completion unless a later approved sprint change says so.
- Warning for future web implementation: all module UI work must follow repository UX rules and the approved FrontComposer/Fluent UI Blazor V5-only boundary. Raw controls, parallel UI primitives, legacy Fluent v4/FAST tokens, and handcrafted theme primitives must be treated as conformance issues.

## Epic Quality Review

### Review Scope

Reviewed `_bmad-output/planning-artifacts/epics.md` against create-epics-and-stories quality expectations:

- Epics should deliver user/operator/maintainer value, not only technical milestones.
- Epic sequencing must avoid forward dependencies.
- Stories should be independently completable, appropriately sized, and testable.
- Acceptance criteria should use specific Given/When/Then outcomes and include error paths.
- Greenfield setup must include starter-template/project setup and early build/test feedback.

### Overall Quality Result

The active MVP spine is usable: Epic 0 establishes the greenfield foundation, Epic 1-2 prove ingestion/search and the three-axis thesis, Epic 3-8 cover domain, tenant, CLI, and operational confidence, and Epic 9-10 are clearly marked Phase 1.5. Most stories include concrete Given/When/Then acceptance criteria with measurable validation.

The main quality risk is historical technical slicing. Several completed or historical stories are explicitly marked as too broad or too technical and guarded with "do not reopen as a single unit" notes. Those guards are necessary and should be treated as binding. Future implementation should use vertical, observable slices rather than repeating the historical contract/indexing/orchestration mega-slice pattern.

### Critical Violations

No active MVP epic has an unrecoverable critical violation that makes the full implementation plan unusable.

No forward dependency was found where an active MVP epic can only function by requiring a later active product epic. The Epic 0 -> Epic 1 -> Epic 2 -> Epic 3+ sequence is mostly valid.

### Major Issues

1. **Historical technical stories remain in the plan and must not become precedent.**
   Examples: Story 1.2 "Memory Unit Domain Model & Contracts", Story 1.5 "Three-Backend Indexing", and Story 1.6 "Ingestion Workflow Orchestration" are technical slices or bundled infrastructure slices rather than small user-value stories. The epics document acknowledges this with observable proof gates and "do not reopen as a single implementation unit" guidance. That mitigation is good, but the risk remains if future work copies these shapes.

   Recommendation: Keep the historical scope guards. Any reopened Story 1.2/1.5/1.6 work must be split into independently demonstrable vertical stories with CLI/API/contract/integration evidence.

2. **Story 0.4 references future operational evidence from Story 11.1.**
   Story 0.4 says it may be satisfied by migrated evidence from Story 11.1 if the minimum gate already exists. Because Story 0.4 is part of the Epic 0 prerequisite path and Story 11.1 is later operational-readiness scope, this can read as a forward dependency.

   Recommendation: Reword Story 0.4 so it owns the minimum CI preflight directly. If evidence from Story 11.1 is reused, record it as imported historical evidence, not as a dependency on future Story 11.1 completion.

3. **Graceful degradation ownership is split across Story 2.5 and Story 5.6.**
   Story 2.5 includes degraded hybrid search behavior when one backend is unavailable, while FR66 is mapped to Epic 5 / Story 5.6. This creates duplicate ownership or a hidden dependency between the search epic and the tenant/isolation epic.

   Recommendation: Define a narrow Story 2.5 behavior as search-layer capability detection and a full Story 5.6 behavior as system-wide degradation policy, or move the degraded-backend AC entirely to Story 5.6 and make Story 2.5 depend only on injectable unavailable-axis inputs.

4. **Story 8.2 introduces `memories inspect --id` without clear CLI ownership.**
   Story 8.2 acceptance criteria require a `memories inspect --id <unit-id>` command. The MVP CLI story lists `ingest`, `search`, `case`, `tenant`, and benchmark commands, with richer diagnostics in Phase 1.5. `inspect` is not clearly introduced in Epic 7, so Story 8.2 may require an unowned CLI surface.

   Recommendation: Either add `inspect` explicitly to Epic 7 MVP CLI scope, define it as an API/server-only operation for Story 8.2, or mark CLI `inspect` as Phase 1.5 and adjust Story 8.2 MVP criteria.

5. **Architecture and epics disagree on MVP phase accounting.**
   Architecture's Requirements Coverage section says FR1-70 are MVP-covered, but epics classify MCP/EventStore FRs as Phase 1.5 and FR71 as Phase 2. This can cause readiness dashboards to overstate active MVP scope.

   Recommendation: Update architecture coverage language to match epics: active MVP is Epic 0-8, Phase 1.5 is Epic 9-10, FR71 is Phase 2, Epic 17 is future web UI, and operational tracks require explicit sprint selection.

### Minor Concerns

1. **FR71 is traceable but not shaped as a normal numeric implementation story.**
   The Phase 2 export placeholder states Story 8.3 is reserved-non-MVP, but the heading is "Data Export (FR71 / Non-MVP Gate)" rather than a normal Story 8.3 structure.

   Recommendation: When export becomes active, create a properly numbered story file with normal story status, file scope, acceptance criteria, and phase ownership.

2. **Large operational hardening stories use checkpoint language to manage scope.**
   Stories 13.2, 13.6, 14.1, 15.6 and similar operational stories are broad. They are mitigated by checkpoints, but the checkpoints are effectively sub-stories.

   Recommendation: For future execution, implement and review each checkpoint as a separately verifiable slice even if the tracking story remains one umbrella item.

3. **Some acceptance criteria allow "implemented, documented, accepted, or carried forward."**
   The Engineering/Operational Readiness Track explicitly permits this pattern, but it would be unacceptable in product capability stories.

   Recommendation: Keep this allowance confined to operational-readiness stories. Product MVP stories must deliver working behavior or an explicitly approved sprint-change deferral.

4. **Completed web UX Story 17.1 is not a safe pattern for future web stories.**
   The approved 2026-06-24 sprint change identifies raw markup/CSS drift and adds Story 17.6. This is already planned, but it is a quality concern until conformance hardening lands.

   Recommendation: Treat Story 17.6 as a prerequisite before extending Stories 17.2-17.5 implementation.

### Best Practices Compliance Summary

| Area | Assessment |
|---|---|
| Epic user value | Mostly compliant for active MVP. Operational tracks have maintainer/operator value but are not product capability epics. |
| Epic independence | Mostly compliant. Epic 0 foundation is an intentional prerequisite; later MVP epics use earlier outputs. |
| Forward dependencies | No fatal forward dependency. Story 0.4 / 11.1 wording and Story 8.2 CLI ownership need cleanup. |
| Story sizing | Mixed. Current and future stories are generally specific; historical Story 1.2/1.5/1.6 and several operational checkpoint stories are large. |
| Acceptance criteria | Strong overall. Most stories use testable Given/When/Then criteria with error paths. |
| Starter template / greenfield setup | Compliant. Architecture selects Aspire Empty + incremental projects; Story 0.0 covers scaffold and single-command boot. |
| Early CI feedback | Present through Story 0.4, but wording should avoid relying on later Story 11.1. |
| Traceability | Strong. FR1-FR74 are traced; phase caveats are explicit in epics. |

### Remediation Recommendations

1. Update architecture phase accounting to match epics and PRD.
2. Reword Story 0.4 to remove any apparent dependency on future Story 11.1 completion.
3. Clarify degraded-backend ownership between Story 2.5 and Story 5.6.
4. Decide whether `memories inspect --id` is MVP CLI scope, API-only scope, or Phase 1.5 CLI scope.
5. Keep historical technical Story 1.2/1.5/1.6 guards binding and split any reopened work before implementation starts.
6. Make Story 17.6 the guardrail before additional Epic 17 web implementation.

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK

The project is not blocked by missing core planning artifacts: PRD, Architecture, Epics/Stories, and UX specification all exist. PRD coverage is strong: all 74 functional requirements are traced to epics, stories, or explicit backlog placeholders.

The readiness risk is planning precision, not missing requirements. The artifacts need cleanup before they should be used as an unrestricted implementation source of truth, especially around MVP/P1.5/Phase 2 scope accounting, story ownership boundaries, and historical technical story patterns.

### Critical Issues Requiring Immediate Action

No critical requirement-coverage blocker was found. No PRD FR is missing from epics.

Immediate action is still required on these major issues before proceeding broadly:

1. Correct architecture phase accounting so it matches PRD and epics: active MVP is Epic 0-8, Phase 1.5 is Epic 9-10, FR71 is Phase 2, Epic 17 is future web UI, and operational tracks require explicit sprint selection.
2. Reword Story 0.4 so minimum CI preflight is owned directly by Story 0.4, not apparently dependent on later Story 11.1.
3. Resolve degraded-backend ownership between Story 2.5 and Story 5.6.
4. Decide the ownership and phase for `memories inspect --id` introduced by Story 8.2.
5. Keep historical Story 1.2, 1.5, and 1.6 technical-slice guards binding; split any reopened work into vertical, observable stories before implementation.
6. Complete or enforce Story 17.6 before extending future web UX work beyond Story 17.1.

### Recommended Next Steps

1. Patch `architecture.md` to align its Requirements Coverage section with the epics phase model and the readiness report caveats.
2. Patch `epics.md` for Story 0.4, Story 2.5/5.6 degradation ownership, and Story 8.2 `inspect` ownership.
3. Add a short "MVP Scope Accounting" note near the top of `epics.md` that lists active MVP, Phase 1.5, Phase 2, future web UI, and operational-readiness tracks.
4. Convert the Story 1.2/1.5/1.6 historical warnings into a reusable "no broad technical slices" policy for future story creation.
5. Treat Story 17.6 as the web UX conformance gate before additional Epic 17 development.
6. When FR71 becomes active, create a normal numeric story file instead of relying on the reserved placeholder.

### Final Note

This assessment identified 9 quality/readiness issues: 5 major and 4 minor, across phase accounting, story dependency/ownership, story sizing, and UX governance.

The artifacts are close enough to support targeted implementation when the relevant story is clean and in active scope. They are not yet clean enough for broad Phase 4 execution without the remediation above.

**Assessment Date:** 2026-06-27
**Assessor:** Codex using `bmad-check-implementation-readiness`
