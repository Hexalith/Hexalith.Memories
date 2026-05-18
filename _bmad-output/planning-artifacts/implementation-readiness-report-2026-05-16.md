---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
documentsIncluded:
  - type: PRD
    path: D:\Hexalith.Memories\_bmad-output\planning-artifacts\prd.md
  - type: Architecture
    path: D:\Hexalith.Memories\_bmad-output\planning-artifacts\architecture.md
  - type: Epics and Stories
    path: D:\Hexalith.Memories\_bmad-output\planning-artifacts\epics.md
  - type: UX Design
    path: D:\Hexalith.Memories\_bmad-output\planning-artifacts\ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-16
**Project:** Hexalith.Memories

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- `prd.md` (81,792 bytes, modified March 23, 2026 11:39:10)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- `architecture.md` (100,162 bytes, modified March 28, 2026 20:45:20)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- `epics.md` (158,340 bytes, modified May 13, 2026 20:43:59)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- `ux-design-specification.md` (96,474 bytes, modified May 16, 2026 09:49:48)

**Sharded Documents:**
- None found

### Issues Found

- No duplicate whole/sharded document conflicts found.
- No required document types missing.

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

FR71: Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format

FR72: System exposes readiness and liveness health checks verifying all backends

FR73: Operator can detect index/graph divergence via consistency check

FR74: Operator can repair detected index/graph inconsistencies via consistency repair operation

Total FRs: 74

### Non-Functional Requirements

NFR1: Syntactic search latency (p95) target <200ms with 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.

NFR2: Semantic search latency (p95) target <500ms with 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.

NFR3: Hybrid search latency (p95) target <1s with 10 concurrent queries/tenant and 10K memory units/tenant. Phase: MVP.

NFR4: Graph traversal latency (p95) target <2s with 10 concurrent queries/tenant, 10K memory units/tenant, and depth <=5. Phase: MVP.

NFR5: Ingestion throughput target >100 memory units/min for payloads <=10KB and >10 memory units/min for payloads <=1MB, per tenant, with single-document embedding calls. Phase: Ongoing.

NFR6: Event indexing freshness target <5s from DAPR pub/sub publication to searchable under normal conditions, with degradation documented when embedding provider is rate-limited. Phase: P1.5.

NFR7: Cold start time target: service fully operational within 60s from containers running to accepting queries, excluding image pull time. Phase: Ongoing.

NFR8: Zero cross-tenant data leakage - no search, ingestion, or graph traversal returns data from another tenant. Verification requires automated tests across search, ingest, and graph axes with malformed/empty/swapped tenant IDs, plus graph-specific edge-ID collision tests. Phase: MVP.

NFR9: Embedding provider API keys stored in secure secret management (.NET User Secrets for local dev, DAPR Secrets API for deployed), never in config files or environment variables in production. Verification: code review and CI secret scanning. Phase: Ongoing.

NFR10: All inter-service communication authenticated via DAPR API tokens. Verification: DAPR configuration validation. Phase: Ongoing.

NFR11: External access authenticated at ingress layer; no unauthenticated access to REST API endpoints. Verification: integration test with unauthenticated requests. Phase: P1.5.

NFR12: System supports linear scaling of tenants; adding a new tenant must not degrade existing tenant performance by more than 5%. Target validation at 10 tenants, each with 100K memory units, by benchmarking tenant 1 before and after adding 9 loaded tenants. Phase: Ongoing.

NFR13: Per-tenant ingestion pipeline scales independently; one tenant's batch ingestion must not block another tenant's real-time ingestion. Verification: concurrent ingestion test across 3 tenants. Phase: Ongoing.

NFR14: Redis memory footprint per memory unit is predictable and documented so operators can estimate infrastructure costs before tenant provisioning. Target: published sizing guide by vector dimension and metadata size. Phase: Ongoing.

NFR15: Architecture must not preclude backend migration from Redis to Qdrant; concrete implementation must have clear extraction points identified and avoid premature interfaces. Verification: architecture review documenting extraction points and no tight coupling to Redis-specific APIs in domain logic. Phase: Ongoing.

NFR16: Zero memory unit loss during Redis restart. Target: AOF persistence enabled and verified. Phase: MVP.

NFR17: Ingestion pipeline state survives process restarts; queued and in-progress units resume without data loss. Target: DAPR actor state persistence verified. Phase: MVP.

NFR18: Partial backend failure, where one of three backends is down, results in degraded service rather than total failure; available axes continue serving results. Verification: chaos test killing each backend individually and verifying partial results. Phase: Ongoing.

NFR19: Failed ingestion units are never silently dropped; all failures are visible via CLI status with error details and failure stage. Verification: end-to-end test with intentional failures at each pipeline stage. Phase: Ongoing.

NFR20: MCP tool responses conform to MCP protocol specification, including valid tool schemas, typed parameters, and structured error responses. Verification: MCP protocol conformance test suite. Phase: P1.5.

NFR21: DAPR pub/sub integration handles CloudEvents envelope format, and events from any DAPR-compatible publisher are processable. Verification: integration test with standard CloudEvents payloads. Phase: P1.5.

NFR22: Embedding provider integration handles rate limiting gracefully; 429 responses trigger backoff without pipeline crash or data loss. Verification: rate limit simulation test per provider. Phase: Ongoing.

NFR23: CLI connects to the memory server via configurable endpoint and supports local dev (localhost), container (docker service name), and remote (ingress URL) environments. Verification: configuration layering test across all three environments. Phase: Ongoing.

NFR24: All axis scores normalized to 0.0-1.0 before fusion - BM25 via saturation normalization against corpus statistics, cosine similarity native range, graph proximity via inverse hop distance with decay. Verification: normalization unit tests with known inputs/outputs. Phase: MVP.

NFR25: Fusion algorithm produces deterministic scores; same query against same data produces identical composite scores, while result ordering within the same score tier may vary. Verification: 100 repeated queries with zero score variance. Phase: MVP.

NFR26: Benchmark suite produces reproducible results; running benchmarks twice against the same dataset yields identical NDCG@10 scores. Verification: reproducibility test in CI. Phase: MVP.

NFR27: Structured JSON logging with OpenTelemetry correlation IDs from DAPR trace context. Verification: log format validation. Phase: Ongoing.

NFR28: Trace context propagates across all DAPR service invocation hops, giving an end-to-end trace from CLI/MCP through server to backend. Verification: distributed trace completeness test. Phase: Ongoing.

NFR29: Custom metrics exported via OpenTelemetry: ingestion throughput, search latency per axis, index size per tenant, and pipeline queue depth. Target: Aspire dashboard shows all metrics during local development. Phase: Ongoing.

NFR30: Every CLI command includes `--help` with at least one usage example. Verification: CLI help completeness test parsing all commands and verifying example presence. Phase: MVP.

NFR31: README includes working quickstart that completes in <30 minutes on a clean machine with Docker installed. Verification: timed walkthrough on clean environment. Phase: MVP.

Total NFRs: 31

### Additional Requirements

- The product thesis requires three-axis retrieval to outperform any single axis on 80%+ of benchmark queries, scored with NDCG@10 and >=80% inter-rater agreement.
- MVP Go/No-Go requires all hard gates to pass: three-axis validation at 80%, zero cross-tenant leaks, and developer onboarding under 30 minutes; at least two of three soft gates must also pass: causal chain completeness >=95%, MCP end-to-end integration, and case model scoping.
- MVP scope is Phase 1 proof-of-thesis with seven must-have capabilities: Memory Engine, Content Ingestion API, Three-Axis Search, Case/Folder Model, Tenant Isolation, benchmark-focused CLI, and Benchmark Suite.
- Phase 1.5 is committed within 4 weeks of thesis validation and includes EventStore Integration, MCP Server, and CLI expansion. If this slips, MCP Server moves into MVP.
- DAPR infrastructure, actors, state management, and sidecar configuration are scaffolding inside features 1-5, not a separate work item.
- Fusion algorithm work must be sequenced after independent syntactic, semantic, and graph axes work; BM25 normalization, cosine handling, and graph proximity decay must be solved and documented before fusion weighting.
- Shared embedding API keys are an operational risk; per-tenant pipeline actors enforce throttle ceilings, and full isolation requires separate tenant API keys.
- Compliance boundary: Memories is interpretive infrastructure and must document tenant deletion limitations, access telemetry limitations, security posture for auditors, and a legal disclaimer.
- Confidence scores measure query-result relevance, not factual accuracy or data completeness; this distinction must appear in API docs, CLI explain output, compliance docs, and MCP schema docs.
- Memory unit provenance requires mandatory `ingested_by` for every memory unit.
- Causal traversal must return unambiguous ordered structure, typed directional edges, timestamps, edge confidence, and explicit gap markers for missing intermediate nodes.
- Minimum edge types are `caused_by`, `correlated_with`, `references`, `contains`, and `annotates`; `caused_by` and `correlated_with` must remain semantically distinct.
- Hexalith.Memories license is recommended as Apache 2.0, with a public README commitment not to change to a restrictive license.
- Dependency licensing constraints must be documented, especially Redis Stack SSPL/RSAL managed-service constraints and FalkorDB AGPL architectural boundary.
- Required packages include 8 published NuGet packages and 2 internal Aspire projects: Contracts, Client, Client.Rest, Server, Redis, CLI, MCP, EventStore, AppHost, and ServiceDefaults.
- Server must depend only on Contracts; Redis implementation is registered at composition root.
- External consumers connect via infrastructure-managed ingress; internal services communicate through DAPR service invocation; JSON is the exclusive serialization format.
- Tenant context is passed as a payload parameter and validated by the server; per-user identity is not in MVP.
- Internal DAPR errors must propagate through ingress with enough context for actionable CLI diagnostics.
- Embedding providers supported from MVP: Google `text-embedding-004`, OpenAI `text-embedding-3-small`, Mistral `mistral-embed`, plus custom.
- Switching embedding providers requires a full tenant reindex because Redis Vector Search index schema is fixed at creation.
- Ingestion uses a per-tenant pipeline actor with bounded queue, throttling, ordering, progress tracking, persisted DAPR actor state, and stages `queued`, `extracting`, `embedding`, `indexing`, `indexed`, and `failed`.
- CLI is the operational superset; MCP exposes agent-facing search, ingest, traverse, and case-info capabilities.
- CLI commands include `ingest`, `search`, `explore`, `traverse`, `case`, `tenant`, `status`, `handlers`, and `quickstart`; output formats are human-readable, JSON, and table.
- Configuration layering precedence is command-line flags, environment variables, config file, DAPR Secrets API, .NET User Secrets, then DAPR configuration.
- Required examples: `samples/01-quickstart`, `samples/02-eventstore-integration`, and `samples/03-mcp-agent`.
- Documentation set requires README, CLI help, getting started guide, API reference, compliance enablement guide, and operator guide.
- Test strategy requires unit tests with mocked `DaprClient`, integration tests with Aspire `DistributedApplicationTestingBuilder` or DAPR testcontainers, and contract serialization tests.
- Local development must run through Aspire AppHost launching Server, MCP Server, Redis, FalkorDB, and DAPR sidecars.
- Non-.NET consumers integrate through ingress REST API or DAPR service invocation; Python/TypeScript clients are future convenience layers.

### PRD Completeness Assessment

The PRD is detailed and implementation-oriented. It contains a complete numbered FR set (74 items), a complete numbered NFR set (31 items), measurable product gates, validation methods, phase tags, journey coverage, package topology, interface capability mapping, operational constraints, and test strategy. The main readiness risk is not PRD absence, but alignment pressure: some journey-implied capabilities are phased differently from MVP, and the epics must preserve those phase boundaries clearly so implementation does not overbuild or accidentally defer thesis-critical gates.

## Epic Coverage Validation

### Epic FR Coverage Extracted

FR1: Covered in Epic 1 - Ingest from local files

FR2: Covered in Epic 6 - Ingest from URLs

FR3: Covered in Epic 6 - Batch-ingest from directory

FR4: Covered in Epic 1 - Text extraction (Kreuzberg)

FR5: Covered in Epic 1 - Generate embeddings

FR6: Covered in Epic 1 - Memory unit fully searchable after ingestion

FR7: Covered in Epic 1 - Metadata with origin tracking

FR8: Covered in Epic 6 - Per-tenant ingestion load management

FR9: Covered in Epic 6 - Auto-retry with configurable limits

FR10: Covered in Epic 6 - Ingestion status per case

FR11: Covered in Epic 6 - Failed unit visibility

FR12: Covered in Epic 6 - Re-ingestion of failed content

FR13: Covered in Epic 1 - Partial backend write failure recovery (IngestionWorkflow saga/compensation)

FR14: Covered in Epic 2 - Syntactic search

FR15: Covered in Epic 2 - Semantic search

FR16: Covered in Epic 2 - Graph search

FR17: Covered in Epic 2 - Hybrid fusion search

FR18: Covered in Epic 2 - Axis selection control

FR19: Covered in Epic 2 - Per-axis score breakdown (explain)

FR20: Covered in Epic 3 - Filter search by case

FR21: Covered in Epic 3 - Filter search by metadata

FR22: Covered in Epic 2 - Pagination (search concern)

FR23: Covered in Epic 10 - Token budget (MCP)

FR24: Covered in Epic 2 - Origin identifier in results

FR25: Covered in Epic 2 - Benchmark comparisons

FR26: Covered in Epic 3 - Create case

FR27: Covered in Epic 3 - Delete case

FR28: Covered in Epic 3 - Add case members

FR29: Covered in Epic 3 - Remove case members

FR30: Covered in Epic 3 - List cases

FR31: Covered in Epic 3 - Case status

FR32: Covered in Epic 3 - Single-case ownership

FR33: Covered in Epic 3 - Case-scoped graph edges

FR34: Covered in Epic 3 - Cross-case tenant search

FR35: Covered in Epic 3 - Delete memory unit

FR36: Covered in Epic 3 - Case activity

FR37: Covered in Epic 3 - Annotations/corrections

FR38: Covered in Epic 5 - Create tenant

FR39: Covered in Epic 5 - Delete tenant

FR40: Covered in Epic 5 - Verify tenant isolation

FR41: Covered in Epic 5 - List tenants

FR42: Covered in Epic 5 - Update tenant config

FR43: Covered in Epic 5 - Prevent inconsistent config changes

FR44: Covered in Epic 5 - Tenant context enforcement

FR45: Covered in Epic 5 - View tenant configuration

FR46: Covered in Epic 1 - Index CausationId/CorrelationId as graph edges

FR47: Covered in Epic 4 - Traverse causal chains

FR48: Covered in Epic 4 - Filter by edge type

FR49: Covered in Epic 4 - Gap markers for missing nodes

FR50: Covered in Epic 4 - Edge type taxonomy

FR51: Covered in Epic 4 - Promote AI-inferred confidence

FR52: Covered in Epic 4 - Chronological ordering

FR53: Covered in Epic 7 - CLI for all capabilities

FR54: Covered in Epic 10 - MCP tools

FR55: Covered in Epic 7 - CLI output formats

FR56: Covered in Epic 7 - Actionable CLI errors

FR57: Covered in Epic 7 - Discoverable actions

FR58: Covered in Epic 10 - MCP typed schemas

FR59: Covered in Epic 9 - Auto-discover event types

FR60: Covered in Epic 9 - Dual embeddings for events

FR61: Covered in Epic 9 - Auto-index CausationId/CorrelationId

FR62: Covered in Epic 9 - Handler registration management

FR63: Covered in Epic 2 - Composite confidence scores

FR64: Covered in Epic 7 - Metadata origin tracking display

FR65: Covered in Epic 1 - `ingested_by` field

FR66: Covered in Epic 5 - Partial results on backend failure

FR67: Covered in Epic 7 - Search/access telemetry

FR68: Covered in Epic 1 - Configure embedding provider

FR69: Covered in Epic 5 - Per-tenant rate limits

FR70: Covered in Epic 5 - Track embedding model per unit

FR71: Covered in Epic 8 - Export data

FR72: Covered in Epic 8 - Health checks

FR73: Covered in Epic 8 - Consistency check

FR74: Covered in Epic 8 - Consistency repair

Total FRs in epics: 74

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --------- | --------------- | ------------- | ------ |
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
| FR13 | System handles partial backend write failures with rollback or retry recovery behavior | Epic 1 | Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant | Epic 2 | Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant | Epic 2 | Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant | Epic 2 | Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes | Epic 2 | Covered |
| FR18 | Developer can control which axes are included in a search query | Epic 2 | Covered |
| FR19 | Developer can view per-axis score breakdown including normalization method | Epic 2 | Covered |
| FR20 | Developer can filter search results by case | Epic 3 | Covered |
| FR21 | Developer can filter search results by metadata field values | Epic 3 | Covered |
| FR22 | Developer can paginate search results | Epic 2 | Covered |
| FR23 | LLM Agent can constrain search response size by token budget | Epic 10 | Covered |
| FR24 | System returns origin identifier and origin type for each search result | Epic 2 | Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search results | Epic 2 | Covered |
| FR26 | Developer can create a case within a tenant | Epic 3 | Covered |
| FR27 | Developer can delete a case and all its memory units | Epic 3 | Covered |
| FR28 | Developer can add members to a case | Epic 3 | Covered |
| FR29 | Developer can remove members from a case | Epic 3 | Covered |
| FR30 | Developer can list cases within a tenant | Epic 3 | Covered |
| FR31 | Developer can view case status including count, activity timestamp, and health indicators | Epic 3 | Covered |
| FR32 | System enforces strict single-case ownership per memory unit | Epic 3 | Covered |
| FR33 | System maintains case-scoped graph edges between memory units within a case | Epic 3 | Covered |
| FR34 | Developer can search across all cases within a tenant with case attribution | Epic 3 | Covered |
| FR35 | Developer can delete an individual memory unit from a case | Epic 3 | Covered |
| FR36 | Developer can view recent activity within a case | Epic 3 | Covered |
| FR37 | Developer can annotate or correct a memory unit as linked memory units | Epic 3 | Covered |
| FR38 | Operator can create a tenant with physically separate indexes | Epic 5 | Covered |
| FR39 | Operator can delete a tenant and all its data | Epic 5 | Covered |
| FR40 | Operator can verify tenant isolation via automated checks | Epic 5 | Covered |
| FR41 | Operator can list tenants | Epic 5 | Covered |
| FR42 | Operator can update tenant configuration after creation | Epic 5 | Covered |
| FR43 | System prevents inconsistent configuration changes without explicit acknowledgment | Epic 5 | Covered |
| FR44 | System enforces tenant context at all access layers | Epic 5 | Covered |
| FR45 | Operator can view current tenant configuration | Epic 5 | Covered |
| FR46 | System can index CausationId and CorrelationId as typed directional graph edges | Epic 1 | Covered |
| FR47 | Developer can traverse causal chains from a starting node with configurable depth | Epic 4 | Covered |
| FR48 | Developer can filter graph traversal by edge type | Epic 4 | Covered |
| FR49 | Traversal includes gap markers for missing intermediate nodes | Epic 4 | Covered |
| FR50 | System supports required edge types with default confidence | Epic 4 | Covered |
| FR51 | Developer can promote AI-inferred edge confidence | Epic 4 | Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes | Epic 4 | Covered |
| FR53 | Developer can interact with retrieval and ingestion capabilities via CLI | Epic 7 | Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info via MCP tools | Epic 10 | Covered |
| FR55 | CLI supports human-readable, JSON, and table output formats | Epic 7 | Covered |
| FR56 | CLI provides actionable error messages with recovery suggestions | Epic 7 | Covered |
| FR57 | Developer can discover available actions from any system state | Epic 7 | Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions | Epic 10 | Covered |
| FR59 | System can auto-discover event types published to DAPR pub/sub topics | Epic 9 | Covered |
| FR60 | System can generate dual embeddings for events | Epic 9 | Covered |
| FR61 | System can automatically index CausationId/CorrelationId metadata as graph edges | Epic 9 | Covered |
| FR62 | Developer can list registered event handlers and detect mismatches | Epic 9 | Covered |
| FR63 | System returns composite confidence scores with per-axis breakdowns | Epic 2 | Covered |
| FR64 | System tracks metadata origin and confidence per field on every memory unit | Epic 7 | Covered |
| FR65 | System records `ingested_by` on every memory unit | Epic 1 | Covered |
| FR66 | System returns partial results and excluded-axis indications when backends are unavailable | Epic 5 | Covered |
| FR67 | System logs search and access events per tenant for audit purposes | Epic 7 | Covered |
| FR68 | Operator can configure embedding provider and model per tenant | Epic 1 | Covered |
| FR69 | System enforces per-tenant rate limit ceilings for embedding API calls | Epic 5 | Covered |
| FR70 | System tracks embedding provider and model used for each memory unit's vectors | Epic 5 | Covered |
| FR71 | Developer can export memory units, metadata, and graph edges for case or tenant | Epic 8 | Covered |
| FR72 | System exposes readiness and liveness health checks verifying all backends | Epic 8 | Covered |
| FR73 | Operator can detect index/graph divergence via consistency check | Epic 8 | Covered |
| FR74 | Operator can repair detected index/graph inconsistencies | Epic 8 | Covered |

### Missing Requirements

No missing FR coverage found. The epics document claims explicit coverage for every PRD FR from FR1 through FR74.

No FR numbers were found in the epics coverage map that are absent from the PRD FR inventory. Later Epics 14 and 15 reinforce existing FRs and NFRs but do not introduce new FR identifiers.

### Coverage Statistics

- Total PRD FRs: 74
- FRs covered in epics: 74
- FRs missing from epics: 0
- FRs in epics but not in PRD: 0
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

Found: `ux-design-specification.md` (96,474 bytes, modified May 16, 2026 09:49:48).

The UX document defines a cross-surface trust model for CLI, MCP, and future web UI. Its central artifact is the Evidence Packet: a shared response object exposing scope, source, reasoning, state, and recovery. It also defines Trust Strip, Scope Header, Retrieval Axis Breakdown, Source Citation Stack, Graph Path Summary, Recovery Action Panel, Agent Packet Inspector, and Case Activity Trail patterns.

### UX to PRD Alignment

Strong alignment:

- PRD search, explain, source attribution, confidence, tenant/case scoping, degraded backend, token-budget, case activity, causal traversal, and onboarding requirements are directly reflected in the UX Evidence Packet and trust-loop model.
- PRD user journeys for Alex, Kenji, Marcus, LLM Agent, and Priya are represented in UX flows and component patterns.
- PRD trust requirements FR63-FR67 align closely with UX requirements for confidence semantics, provenance, degraded states, audit-oriented telemetry, and recovery actions.
- PRD NFR30-NFR31 align with UX onboarding and CLI help expectations.

Potential PRD/UX scope tension:

- The UX spec treats CLI, MCP, and web UI as first-class surfaces. The PRD primarily commits CLI in MVP and MCP in Phase 1.5, while broader REST/application UI experiences are growth-phase. This is acceptable if the UX is understood as a cross-phase design north star, but implementation stories must keep MVP scope clear.
- UX requires deterministic expansion handles and omitted-detail semantics for MCP. PRD requires token-budget compliance and omitted-result indication, but the precise expansion-handle behavior is not clearly captured in FR23/FR54/FR58 or Epic 10 acceptance criteria.

### UX to Architecture Alignment

Strong alignment:

- Architecture explicitly supports capability alignment rather than feature parity across CLI, MCP, REST, and DAPR, matching the UX expectation that surfaces differ by density while sharing semantics.
- Architecture defines token-budget-aware MCP responses, full node context for graph traversal, structured error format with recovery suggestion, confidence/provenance metadata, per-axis score breakdowns, tenant isolation, degraded backend behavior, and traceable DAPR workflows.
- Architecture maps Trust & Transparency to `Contracts/V1` via `MetadataField`, `ScoredResult`, and `SearchResult`, which can support the UX evidence model.
- Architecture supports scope-first safety through tenant validation, physical tenant isolation, DAPR API tokens, and planned `TenantAuthorizationMiddleware`.
- Architecture supports bad-path UX through failure details, workflow recovery, degraded results, health checks, consistency verification, and telemetry.

Alignment gaps:

- **Evidence Packet contract not explicit enough:** UX says implementation should begin with the Evidence Packet contract before web UI composition. Architecture references `SearchResult`/`ScoredResult` but does not clearly define a shared Evidence Packet contract covering scope, sources, reasoning, freshness, health, omitted details, degraded state, and recovery action. This could lead CLI, MCP, and future UI to diverge.
- **Web UI phase mismatch:** UX contains detailed Fluent UI/FrontComposer web component guidance, while architecture intentionally keeps full REST/application UI scope in Phase 2 and does not include a concrete web UI project in the core package topology. This is not a blocker if documented as future-phase design guidance.
- **MCP expansion handles under-specified:** UX asks for deterministic expansion handles for omitted details. Architecture mentions `omitted_count` in MVP and score range metadata in Phase 1.5, but not a stable expansion-handle contract.
- **Accessibility implementation ownership unclear:** UX defines WCAG 2.2 AA, keyboard support, screen reader semantics, focus management, reduced motion, and forced colors for future web surfaces. Architecture does not map these to implementation/testing ownership because web UI is not currently in MVP scope.

### Alignment Issues

1. **Stale epics statement conflicts with discovered UX artifact.** The epics document says "No UX Design document - this project is a Developer Tool / API Backend with no UI component." That is now false: `ux-design-specification.md` exists and should be referenced or summarized in epics.

2. **Shared evidence model needs contract-level ownership.** The UX document makes Evidence Packet semantics foundational, but the architecture and epics currently split pieces across `SearchResult`, `ScoredResult`, metadata, errors, health checks, and MCP stories. Without an explicit shared contract or schema map, cross-surface consistency is at risk.

3. **Future web UI guidance must remain phase-safe.** UX guidance for FrontComposer/Fluent UI is valuable, but implementation readiness depends on keeping MVP/Phase 1.5 stories focused on CLI/MCP/contract semantics unless a sprint change explicitly pulls web UI forward.

### Warnings

- **Warning:** Update the epics "UX Design Requirements" section to acknowledge the UX spec and state whether it is MVP guidance, Phase 1.5 guidance, or future web UI guidance.
- **Warning:** Add or identify an architecture artifact that maps Evidence Packet fields to concrete contracts for CLI JSON, MCP schema, and future web UI composition.
- **Warning:** If Agent Packet Inspector or web UI work enters scope, add accessibility acceptance criteria and tests rather than leaving them as design-only guidance.
- **Warning:** If MCP token-budget compression must support follow-up expansion, Epic 10 should specify expansion handles, omitted fields, and retry/expand behavior explicitly.

## Epic Quality Review

### Overall Quality Summary

The epics and stories are unusually detailed and mostly testable. Most stories use clear persona/value framing, Given/When/Then acceptance criteria, error-path coverage, and measurable NFR hooks. However, the sequence has several implementation-readiness defects. The most serious problem is that early epics depend on tenant and case infrastructure that is not created until later epics, while tenant isolation is a hard PRD gate and should not be retrofitted.

### Critical Violations

#### Critical 1: Tenant and case forward dependency breaks Epic 1 independence

**Evidence:**

- Epic 1 promises a developer can ingest content from local files into a case and see it searchable.
- Story 1.5 creates tenant-namespaced RediSearch/Vector indexes and a FalkorDB node in the tenant's dedicated database.
- Story 1.5 also creates a `contains` edge from the case node to the memory unit node.
- Story 1.6 starts `IngestionWorkflow` with a valid tenant/case context.
- Tenant provisioning is not until Epic 5.
- Case creation is not until Epic 3.

**Why this violates best practices:**

Epic 1 cannot stand alone unless a tenant and case already exist. That is a forbidden forward dependency on Epic 3 and Epic 5. It also risks building ingestion against placeholder/default tenant behavior that later tenant isolation has to unwind.

**Recommendation:**

Move a minimal tenant bootstrap and case bootstrap into Epic 1, or move Tenant Provisioning and Case Creation before ingestion/indexing stories. The cleanest sequence is: minimal tenant infrastructure -> minimal case creation -> local file ingestion -> indexing/search. If the project wants tenant isolation from day one, Epic 5's provisioning foundations should be part of the first implementation slice, not Gate 2.

#### Critical 2: Tenant isolation hard gate is sequenced too late

**Evidence:**

- PRD says tenant isolation is physical and a hard MVP Go/No-Go gate.
- Project context says tenant isolation is physical, not merely filtered.
- Epic 5, which creates tenant provisioning, tenant deletion, tenant isolation verification, and tenant context enforcement, comes after Epics 1-4.
- Epics 1-4 already use tenant IDs, tenant-scoped indexes, tenant databases, and case-scoped graph edges.

**Why this violates best practices:**

Tenant isolation is a foundational invariant, not a later feature. If implemented after ingestion/search/case/graph stories, those earlier stories either cannot be completed honestly or must be reworked when physical isolation lands.

**Recommendation:**

Restructure the MVP sequence so minimal tenant provisioning and tenant context enforcement precede any ingestion/search/case/graph implementation. Keep richer tenant operations (delete, list, update config, verify reporting) later if needed, but physical index/database creation and context validation must be first-class early dependencies.

#### Critical 3: Duplicate index ownership between Epic 1 and Epic 5

**Evidence:**

- Story 1.5 indexes memory units into tenant-namespaced RediSearch and Redis Vector indexes and a tenant-dedicated FalkorDB database.
- Story 5.1 provisions RediSearch, Redis Vector, and FalkorDB tenant infrastructure.

**Why this violates best practices:**

The stories do not clearly define whether indexes are created on first ingestion or during tenant provisioning. This creates competing ownership for index/database lifecycle and makes rollback/retry behavior ambiguous.

**Recommendation:**

Declare one owner for tenant infrastructure lifecycle. Prefer TenantProvisioningWorkflow as the sole creator of tenant indexes/databases; ingestion should require an active tenant and fail clearly if tenant infrastructure is missing.

### Major Issues

#### Major 1: Case management depends on tenant management that appears later

**Evidence:**

- Story 3.1 starts with "Given a valid tenant context".
- Tenant creation and listing are in Epic 5.

**Impact:**

Epic 3 cannot be independently completed after Epics 1-2 unless a tenant exists by another mechanism. This also weakens traceability for tenant-scoped case APIs.

**Recommendation:**

Either move minimal tenant creation before Epic 3 or make Story 3.1 explicitly depend on an Epic 1 tenant bootstrap story.

#### Major 2: Epic 8 includes FR71 export despite architecture deferring FR71 to Phase 2

**Evidence:**

- Epic coverage maps FR71 to Epic 8.
- Architecture validation says FR71 export is deferred to Phase 2.
- Epic 8 is listed under MVP Operations.

**Impact:**

Implementation scope is contradictory: one planning artifact says export is MVP, another says Phase 2. This can cause wasted work or false readiness claims.

**Recommendation:**

Decide whether FR71 is MVP or Phase 2. If Phase 2, remove it from MVP Epic 8 and mark it deferred in the epic coverage map. If MVP, update architecture readiness to stop listing FR71 as deferred.

#### Major 3: CLI phase is inconsistent across artifacts

**Evidence:**

- PRD MVP includes benchmark-essential CLI commands.
- Epics place Epic 7 under MVP Gate 3.
- Epics package inventory labels `Hexalith.Memories.Cli` as Phase 1.5/Gate 3 polish.
- Architecture service boundary labels CLI as Phase 1.5, while also relying on CLI for gate validation and onboarding.

**Impact:**

The implementation team may delay CLI work even though MVP validation requires CLI onboarding, benchmark commands, explain output, and recovery guidance.

**Recommendation:**

Split CLI into two explicit scopes: MVP CLI essentials (ingest, search --explain, case create/delete, tenant create/delete/verify, benchmark) and Phase 1.5 CLI expansion (explore, status, handlers, quickstart, batch directory ingestion if not MVP). Align PRD, architecture, and epics.

#### Major 4: Some stories are too large to be independently completed

**Examples:**

- Story 1.5: Three-Backend Indexing includes RediSearch, Redis Vector, FalkorDB node/edge creation, parameterized graph query enforcement, and versioned index naming.
- Story 1.6: Ingestion Workflow Orchestration includes validation, extraction, embedding, parallel indexing, consistency verification, compensation, provenance, workflow restart recovery, and duplicate detection.
- Story 13.6: Vector Migration Tool includes dry-run, live migration, tenant config updates, index drop/recreate, replay ingestion, progress reporting, interruption/resume, rollback toggle, and documentation.
- Story 13.7: Integration Tests, Aspire Fixtures & Operator Deployment Guide combines test fixtures, end-to-end integration tests, operator guide, Keycloak recipe, DAPR secret layout, and developer-doc cross references.

**Impact:**

These are likely multi-story slices. Large stories raise implementation risk and make "done" harder to verify.

**Recommendation:**

Split large stories along independently testable boundaries: backend-specific indexing, workflow orchestration core, compensation/idempotency, consistency verification, migration dry-run, live migration, resume/rollback, and documentation.

#### Major 5: Operations/release epics are mixed into product epic sequence

**Evidence:**

- Epics 11, 12, 14, and 15 are CI/CD, release hardening, deferred-work governance, and carry-forward risk closure.
- They have valid maintainer/operator value, but they are not product capability epics in the same sense as Epics 1-10.

**Impact:**

Readiness reporting can blur product scope with engineering-enablement scope. This makes MVP readiness harder to interpret.

**Recommendation:**

Keep these epics, but label them as Engineering Enablement / Operational Readiness epics outside the product capability sequence. Do not count them as product feature completeness unless the readiness question explicitly includes release governance.

#### Major 6: Epic 10 does not fully capture UX-required MCP expansion behavior

**Evidence:**

- UX requires deterministic expansion handles and omitted-detail inspection.
- Story 10.2 covers token-budget truncation and `omitted_count`, but not stable expansion handles or follow-up expansion semantics.

**Impact:**

MCP may satisfy token-budget compliance while still failing the UX requirement that omitted details remain recoverable and inspectable.

**Recommendation:**

Add acceptance criteria for omitted fields, expansion handles, retry/expand behavior, and schema stability.

### Minor Concerns

- The epics "UX Design Requirements" section is stale and says no UX document exists. Update it.
- Story 2.7 uses "reasonable time" for benchmark CI duration. Replace with a concrete threshold.
- Story 5.2 says batched deletion uses "N nodes per activity invocation"; define N or make it configurable with a default.
- Story 13.1 says "a sensible local default like 6000"; replace with an explicit default and rationale.
- Story 14.2 says release bot identity is "pinned enough"; define the required evidence.
- Story 14.3 says refresh requests collapse "where practical"; either require bounded collapse or explicitly document why not.
- Several stories reference future extensibility without always separating present acceptance from future-proofing. Keep future hooks as design notes unless they are required for story completion.

### Best Practices Compliance Checklist

| Epic | User Value | Independent | Story Size | No Forward Dependencies | Clear ACs | Traceability |
| ---- | ---------- | ----------- | ---------- | ----------------------- | --------- | ------------ |
| Epic 1 | Partial | Fails | Mixed | Fails | Strong | Strong |
| Epic 2 | Pass | Pass if Epic 1 fixed | Mixed | Pass | Strong | Strong |
| Epic 3 | Pass | Fails until tenant bootstrap exists | Pass | Fails | Strong | Strong |
| Epic 4 | Pass | Pass after graph/indexing | Pass | Pass | Strong | Strong |
| Epic 5 | Pass | Should move earlier | Pass | Pass | Strong | Strong |
| Epic 6 | Pass | Pass after ingestion foundation | Pass | Pass | Strong | Strong |
| Epic 7 | Pass | Phase ambiguity | Pass | Mixed | Strong | Strong |
| Epic 8 | Pass | Mixed due FR71 phase conflict | Mixed | Mixed | Strong | Strong |
| Epic 9 | Pass | Pass as Phase 1.5 | Pass | Pass | Strong | Strong |
| Epic 10 | Pass | Pass as Phase 1.5 | Pass | Pass | Needs expansion ACs | Strong |
| Epic 11 | Maintainer value | Pass | Pass | Pass | Strong | Indirect |
| Epic 12 | Maintainer value | Mixed external dependencies | Mixed | Pass | Mixed | Indirect |
| Epic 13 | Operator value | Pass after tenant config base | Mixed | Mostly pass | Strong | Growth-change driven |
| Epic 14 | Maintainer/operator value | Pass | Pass | Pass | Mixed wording | Reinforcement only |
| Epic 15 | Maintainer/operator value | Pass | Pass | Pass | Mixed wording | Reinforcement only |

### Special Implementation Checks

- Starter template requirement: Pass. Architecture specifies Aspire starter/scaffold direction, and Story 1.1 covers initial project setup and dev environment.
- Greenfield indicators: Pass. Initial setup, development environment, package/project structure, CI/CD, and release governance are present.
- Database/entity timing: Fails for tenant infrastructure ownership. Index/database creation appears in both ingestion/indexing and tenant provisioning stories; ownership must be resolved.
- Brownfield integration indicators: Pass for Hexalith submodules and EventStore alignment, though submodule policy should remain explicit.

### Quality Recommendations

1. Re-sequence tenant provisioning and minimal case bootstrap ahead of ingestion/search.
2. Resolve whether FR71 export is MVP or Phase 2.
3. Align CLI phase labels across PRD, architecture, and epics.
4. Add an explicit Evidence Packet contract/story or contract mapping.
5. Split oversized implementation stories before development.
6. Separate product capability epics from operational/release governance epics in reporting.
7. Update stale UX section and add MCP expansion-handle acceptance criteria.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY for Phase 4 implementation.**

The artifact set is strong, but the implementation plan has critical sequencing defects. PRD, UX, architecture, and epics are complete enough to reason about the product. The blocker is not missing requirements; it is that the current epic order asks early stories to use tenants, cases, tenant-scoped indexes, and tenant-dedicated graph databases before the stories that create and enforce those foundations.

### Readiness Strengths

- All required planning documents were found: PRD, Architecture, Epics/Stories, and UX.
- PRD contains 74 functional requirements and 31 non-functional requirements.
- Epics claim explicit coverage for all 74 PRD FRs.
- Acceptance criteria are generally testable and mostly use Given/When/Then.
- Architecture provides strong technical direction for DAPR Workflow, actors, tenant isolation, hybrid retrieval, telemetry, and backend boundaries.
- UX adds a coherent trust model that can improve CLI, MCP, and future UI consistency.

### Critical Issues Requiring Immediate Action

1. **Fix tenant/case sequencing.** Epic 1 depends on tenant and case infrastructure from later epics. Minimal tenant provisioning, tenant context validation, and case bootstrap must exist before ingestion/indexing/search stories can be implementation-ready.

2. **Move tenant isolation earlier.** Physical tenant isolation is a hard MVP gate and must be foundational. It cannot sit after ingestion, search, case management, and graph traversal without forcing rework or weakening the invariant.

3. **Resolve tenant index/database ownership.** Story 1.5 and Story 5.1 both imply ownership over tenant index/database creation. Pick one owner; preferably TenantProvisioningWorkflow owns infrastructure creation, and ingestion requires an active tenant.

4. **Resolve FR71 export phase conflict.** Epics place FR71 in MVP Epic 8, while architecture says FR71 is Phase 2. Choose one and update all artifacts.

5. **Resolve CLI phase conflict.** PRD and epics need CLI for MVP validation, while architecture/package notes label CLI as Phase 1.5 in places. Split MVP CLI essentials from Phase 1.5 CLI expansion and align all documents.

6. **Add explicit Evidence Packet contract mapping.** UX depends on a shared evidence model, but architecture and epics do not clearly assign contract ownership for scope, sources, reasoning, freshness, degraded state, omitted details, and recovery actions.

### Recommended Next Steps

1. Create a small sprint-change proposal focused only on sequencing and scope reconciliation.

2. Reorder the first implementation slice to: minimal tenant provisioning -> minimal case creation -> local file ingestion -> backend indexing -> first scoped search.

3. Update `epics.md` so Epic 1 no longer has hidden forward dependencies on Epic 3 and Epic 5.

4. Update architecture and epics to make tenant infrastructure lifecycle ownership unambiguous.

5. Reconcile FR71 and CLI phase labels across PRD, architecture, and epics.

6. Add a contract/story for Evidence Packet semantics, or explicitly map the UX Evidence Packet fields to `SearchResult`, `ScoredResult`, MCP response schema, CLI JSON output, and future UI descriptors.

7. Split oversized stories before assigning them for implementation, especially Stories 1.5, 1.6, 13.6, and 13.7.

8. Update the stale UX section in `epics.md` to acknowledge `ux-design-specification.md` and state its phase relevance.

### Final Note

This assessment identified **20 issues across 4 categories**: critical sequencing violations, major scope/alignment problems, UX/architecture contract gaps, and minor acceptance-criteria wording concerns. Address the critical issues before proceeding to implementation. After the sequencing and phase conflicts are corrected, the project should be close to ready because the requirements inventory, architecture depth, and acceptance-criteria coverage are already strong.

**Assessor:** Codex using `bmad-check-implementation-readiness`
**Completed:** 2026-05-16
