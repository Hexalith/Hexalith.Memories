---
stepsCompleted: ['step-01-validate-prerequisites', 'step-02-design-epics', 'step-03-create-stories', 'step-04-final-validation']
inputDocuments:
  - '_bmad-output/planning-artifacts/prd.md'
  - '_bmad-output/planning-artifacts/architecture.md'
---

# Hexalith.Memories - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.Memories, decomposing the requirements from the PRD and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

**Knowledge Ingestion (13 FRs)**

- FR1: Developer can ingest content from local files into a specified case
- FR2: Developer can ingest content from URLs into a specified case
- FR3: Developer can batch-ingest content from a directory into a specified case
- FR4: System can extract text from ingested content (plain text, PDF, markdown)
- FR5: System can generate embeddings for ingested content via a configurable embedding provider
- FR6: System ensures a memory unit is fully searchable across all axes after ingestion completes
- FR7: Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score
- FR8: System manages ingestion load per tenant independently
- FR9: System retries failed ingestion automatically with configurable limits
- FR10: Developer can view ingestion status per case (queued, embedding, indexed, failed counts)
- FR11: Developer can view failed ingestion units with error details and failure stage
- FR12: Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk
- FR13: System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes)

**Knowledge Retrieval (12 FRs)**

- FR14: Developer can search memory units by syntactic matching within a tenant
- FR15: Developer can search memory units by semantic similarity within a tenant
- FR16: Developer can search memory units by graph traversal within a tenant
- FR17: Developer can search memory units by hybrid fusion combining all available axes
- FR18: Developer can control which axes are included in a search query
- FR19: Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode)
- FR20: Developer can filter search results by case
- FR21: Developer can filter search results by metadata field values
- FR22: Developer can paginate search results
- FR23: LLM Agent can constrain search response size by token budget
- FR24: System returns the origin identifier (file path, URL, or event ID) and origin type for each search result
- FR25: Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output

**Memory Organization (12 FRs)**

- FR26: Developer can create a case within a tenant
- FR27: Developer can delete a case and all its memory units
- FR28: Developer can add members to a case
- FR29: Developer can remove members from a case
- FR30: Developer can list cases within a tenant
- FR31: Developer can view case status including memory unit count, last activity timestamp, and health indicators
- FR32: System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion
- FR33: System maintains case-scoped graph edges between memory units within a case
- FR34: Developer can search across all cases within a tenant by keyword, returning results with case attribution
- FR35: Developer can delete an individual memory unit from a case
- FR36: Developer can view recent activity within a case (ingestion events, searches, membership changes)
- FR37: Developer can annotate or correct a memory unit, with annotations tracked as linked memory units

**Tenant Management (8 FRs)**

- FR38: Operator can create a tenant with physically separate indexes
- FR39: Operator can delete a tenant and all its indexes, graph data, and memory units
- FR40: Operator can verify tenant isolation via automated checks
- FR41: Operator can list tenants
- FR42: Operator can update tenant configuration after creation (rate limits, display name, settings)
- FR43: System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment
- FR44: System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages
- FR45: Operator can view current configuration of a tenant (embedding provider, rate limits, index status)

**Causal Intelligence (7 FRs)**

- FR46: System can index CausationId and CorrelationId from events as typed, directional graph edges
- FR47: Developer can traverse causal chains from a starting node with configurable depth
- FR48: Developer can filter graph traversal by edge type
- FR49: When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier
- FR50: System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence
- FR51: Developer can promote AI-inferred edge confidence when verifying a relationship
- FR52: System maintains chronological ordering and timestamps on causal chain nodes

**Developer Interfaces (6 FRs)**

- FR53: Developer can interact with all retrieval and ingestion capabilities via CLI
- FR54: Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools
- FR55: CLI supports multiple output formats: human-readable (default), JSON, and table
- FR56: CLI provides actionable error messages with recovery suggestions for common failure modes
- FR57: Developer can discover available actions from any system state, including empty states and error conditions
- FR58: MCP tools include typed parameter schemas with descriptions for LLM agent consumption

**EventStore Integration (4 FRs)**

- FR59: System can auto-discover event types published to DAPR pub/sub topics
- FR60: System can generate dual embeddings for events (raw payload + natural language description)
- FR61: System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code
- FR62: Developer can list registered event handlers and detect handler registration mismatches

**Trust & Transparency (5 FRs)**

- FR63: System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result
- FR64: System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit
- FR65: System records `ingested_by` (user or system identity) as a mandatory field on every memory unit
- FR66: When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded
- FR67: System logs search and access events per tenant for audit purposes

**Embedding Provider Management (3 FRs)**

- FR68: Operator can configure embedding provider and model per tenant
- FR69: System enforces per-tenant rate limit ceilings for embedding API calls
- FR70: System tracks the embedding provider and model used for each memory unit's vectors

**Data Portability & System Health (4 FRs)**

- FR71: Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format
- FR72: System exposes readiness and liveness health checks verifying all backends
- FR73: Operator can detect index/graph divergence via consistency check
- FR74: Operator can repair detected index/graph inconsistencies via consistency repair operation

### NonFunctional Requirements

**Performance (NFR1-NFR7)**

- NFR1: Syntactic search latency (p95) <200ms at 10 concurrent queries/tenant, 10K units/tenant [MVP]
- NFR2: Semantic search latency (p95) <500ms at 10 concurrent queries/tenant, 10K units/tenant [MVP]
- NFR3: Hybrid search latency (p95) <1s at 10 concurrent queries/tenant, 10K units/tenant [MVP]
- NFR4: Graph traversal latency (p95) <2s at 10 concurrent queries/tenant, 10K units/tenant, depth <=5 [MVP]
- NFR5: Ingestion throughput >100 units/min (<=10KB), >10 units/min (<=1MB) per tenant [Ongoing]
- NFR6: Event indexing freshness <5s from DAPR pub/sub publication to searchable [P1.5]
- NFR7: Cold start time: service fully operational within 60s [Ongoing]

**Security (NFR8-NFR11)**

- NFR8: Zero cross-tenant data leakage — verified by automated test suite across all axes [MVP]
- NFR9: Embedding API keys stored in secure secret management — never in config files [Ongoing]
- NFR10: All inter-service communication authenticated via DAPR API tokens [Ongoing]
- NFR11: External access authenticated at ingress layer [P1.5]

**Scalability (NFR12-NFR15)**

- NFR12: Linear scaling of tenants — adding a tenant does not degrade existing performance by >5% [Ongoing]
- NFR13: Per-tenant ingestion pipeline scales independently [Ongoing]
- NFR14: Redis memory footprint per unit is predictable and documented [Ongoing]
- NFR15: Architecture must not preclude backend migration (Redis -> Qdrant) [Ongoing]

**Reliability (NFR16-NFR19)**

- NFR16: Zero memory unit loss during Redis restart (AOF persistence) [MVP]
- NFR17: Ingestion pipeline state survives process restarts (DAPR actor state) [MVP]
- NFR18: Partial backend failure results in degraded service, not total failure [Ongoing]
- NFR19: Failed ingestion units are never silently dropped [Ongoing]

**Integration (NFR20-NFR23)**

- NFR20: MCP tool responses conform to MCP protocol specification [P1.5]
- NFR21: DAPR pub/sub integration handles CloudEvents envelope format [P1.5]
- NFR22: Embedding provider integration handles rate limiting gracefully (429 backoff) [Ongoing]
- NFR23: CLI connects via configurable endpoint (localhost, docker, remote) [Ongoing]

**Algorithmic Quality (NFR24-NFR26)**

- NFR24: All axis scores normalized to 0.0-1.0 before fusion [MVP]
- NFR25: Fusion algorithm produces deterministic scores [MVP]
- NFR26: Benchmark suite produces reproducible results (identical NDCG@10) [MVP]

**Observability (NFR27-NFR29)**

- NFR27: Structured JSON logging with OpenTelemetry correlation IDs [Ongoing]
- NFR28: Trace context propagates across all DAPR service invocation hops [Ongoing]
- NFR29: Custom metrics exported via OpenTelemetry (ingestion throughput, search latency per axis, index size per tenant) [Ongoing]

**Documentation Quality (NFR30-NFR31)**

- NFR30: Every CLI command includes --help with at least one usage example [MVP]
- NFR31: README includes working quickstart that completes in <30 minutes [MVP]

### Additional Requirements

**From Architecture — Starter Template & Scaffolding:**
- Aspire Empty + Incremental Projects (D-selected approach). `dotnet new aspire` for orchestration foundation, then add projects incrementally as features are built
- Git submodules: `Hexalith.Commons` (error handling, shared base types) and `Hexalith.EventStore` (event types, versioning conventions)
- Build script must detect missing submodules and print helpful error

**From Architecture — DAPR as First-Class Citizen:**
- DAPR Workflow for multi-step orchestrations: `IngestionWorkflow`, `TenantProvisioningWorkflow`, `TenantDeletionWorkflow`, `ConsistencyVerificationWorkflow`, `AiEnrichmentWorkflow` (D23)
- DAPR Actors for per-tenant stateful singletons: `EmbeddingRateLimiterActor`, `CorpusStatisticsActor` (D24)
- DAPR Conversation API for provider-agnostic LLM communication (D26)
- Dapr Agents as Python sidecar service for AI enrichment (D27)
- Polyglot services via DAPR service invocation (D28)

**From Architecture — Technology Decisions:**
- FalkorDB for MVP with escape hatch via `IGraphQueryBuilder` (D1)
- Graph axis: dual-role — standalone traversal + optional fusion scorer (D2)
- Eventual consistency + DAPR Workflow saga/compensation (D3)
- Google embedding only in MVP; OpenAI/Mistral in Phase 1.5/2 (D4)
- Kreuzberg NuGet package for content extraction — in-process, Rust core via P/Invoke (D13)
- Versioned contract namespaces: `Contracts.V1` (D14)
- Synthetic benchmark dataset with known relationships (D11)
- Domain validation service: `IngestionValidator` (D12)

**From Architecture — Testing & CI:**
- xUnit + Shouldly + NSubstitute (aligned with EventStore) (D16)
- GitHub Actions + semantic release (D17)
- Three test layers: unit (mock DaprClient), integration (Aspire DistributedApplicationTestingBuilder), contract (serialization round-trips)

**From Architecture — Build Order Aligned to Gates:**
1. `Hexalith.Memories.Contracts` (all other projects depend on it)
2. `Hexalith.Memories.Redis` (three-axis backends — Gate 1)
3. `Hexalith.Memories.Server` (ingestion pipeline, search — Gate 1)
4. `Hexalith.Memories.AppHost` (orchestration — Gate 3)
5. `Hexalith.Memories.ServiceDefaults` (health checks, telemetry — Gate 2 verification)
6. `Hexalith.Memories.Cli` (Phase 1.5/Gate 3 polish)
7-10. Client, Client.Rest, Mcp, EventStore (Phase 1.5)

**From Architecture — Gate Strategy:**
- Gate 1 → Gate 2 → Gate 3 order. Highest risk first.
- Gate 1 (Three-axis validation): R&D, unproven thesis — start first
- Gate 2 (Zero cross-tenant leaks): known engineering — design alongside Gate 1
- Gate 3 (<30 min onboarding): developer experience craft — build last
- If Gate 1 fails, Gates 2 and 3 are moot

### UX Design Requirements

No UX Design document — this project is a Developer Tool / API Backend with no UI component. Developer experience is addressed via CLI (Epic 7) and MCP (Epic 10).

### FR Coverage Map

- FR1: Epic 1 — Ingest from local files
- FR2: Epic 6 — Ingest from URLs
- FR3: Epic 6 — Batch-ingest from directory
- FR4: Epic 1 — Text extraction (Kreuzberg)
- FR5: Epic 1 — Generate embeddings
- FR6: Epic 1 — Memory unit fully searchable after ingestion
- FR7: Epic 1 — Metadata with origin tracking
- FR8: Epic 6 — Per-tenant ingestion load management
- FR9: Epic 6 — Auto-retry with configurable limits
- FR10: Epic 6 — Ingestion status per case
- FR11: Epic 6 — Failed unit visibility
- FR12: Epic 6 — Re-ingestion of failed content
- FR13: Epic 1 — Partial backend write failure recovery (IngestionWorkflow saga/compensation)
- FR14: Epic 2 — Syntactic search
- FR15: Epic 2 — Semantic search
- FR16: Epic 2 — Graph search
- FR17: Epic 2 — Hybrid fusion search
- FR18: Epic 2 — Axis selection control
- FR19: Epic 2 — Per-axis score breakdown (explain)
- FR20: Epic 3 — Filter search by case
- FR21: Epic 3 — Filter search by metadata
- FR22: Epic 2 — Pagination (search concern)
- FR23: Epic 10 — Token budget (MCP)
- FR24: Epic 2 — Origin identifier in results
- FR25: Epic 2 — Benchmark comparisons
- FR26: Epic 3 — Create case
- FR27: Epic 3 — Delete case
- FR28: Epic 3 — Add case members
- FR29: Epic 3 — Remove case members
- FR30: Epic 3 — List cases
- FR31: Epic 3 — Case status
- FR32: Epic 3 — Single-case ownership
- FR33: Epic 3 — Case-scoped graph edges
- FR34: Epic 3 — Cross-case tenant search
- FR35: Epic 3 — Delete memory unit
- FR36: Epic 3 — Case activity
- FR37: Epic 3 — Annotations/corrections
- FR38: Epic 5 — Create tenant
- FR39: Epic 5 — Delete tenant
- FR40: Epic 5 — Verify tenant isolation
- FR41: Epic 5 — List tenants
- FR42: Epic 5 — Update tenant config
- FR43: Epic 5 — Prevent inconsistent config changes
- FR44: Epic 5 — Tenant context enforcement
- FR45: Epic 5 — View tenant configuration
- FR46: Epic 1 — Index CausationId/CorrelationId as graph edges (creation during ingestion)
- FR47: Epic 4 — Traverse causal chains
- FR48: Epic 4 — Filter by edge type
- FR49: Epic 4 — Gap markers for missing nodes
- FR50: Epic 4 — Edge type taxonomy
- FR51: Epic 4 — Promote AI-inferred confidence
- FR52: Epic 4 — Chronological ordering
- FR53: Epic 7 — CLI for all capabilities
- FR54: Epic 10 — MCP tools
- FR55: Epic 7 — CLI output formats
- FR56: Epic 7 — Actionable CLI errors
- FR57: Epic 7 — Discoverable actions
- FR58: Epic 10 — MCP typed schemas
- FR59: Epic 9 — Auto-discover event types
- FR60: Epic 9 — Dual embeddings for events
- FR61: Epic 9 — Auto-index CausationId/CorrelationId
- FR62: Epic 9 — Handler registration management
- FR63: Epic 2 — Composite confidence scores
- FR64: Epic 7 — Metadata origin tracking display
- FR65: Epic 1 — `ingested_by` field
- FR66: Epic 5 — Partial results on backend failure
- FR67: Epic 7 — Search/access telemetry
- FR68: Epic 1 — Configure embedding provider
- FR69: Epic 5 — Per-tenant rate limits
- FR70: Epic 5 — Track embedding model per unit
- FR71: Epic 8 — Export data
- FR72: Epic 8 — Health checks
- FR73: Epic 8 — Consistency check
- FR74: Epic 8 — Consistency repair

## Epic List

### Phase: MVP — Gate 1 (Three-Axis Validation)

### Epic 1: Foundation, Ingestion & Graph Edge Indexing
Developer can boot the full stack with a single command, ingest content from local files, and see it persisted and searchable across all three backends — including typed graph edges created during ingestion. This epic establishes the entire infrastructure spine: Aspire AppHost, DAPR Workflows (IngestionWorkflow with saga/compensation), Contracts, Redis (RediSearch + Vector), FalkorDB, Kreuzberg (NuGet, in-process), git submodules, and the IndexGraphActivity.
**FRs covered:** FR1, FR4, FR5, FR6, FR7, FR13, FR46, FR65, FR68

### Epic 2: Three-Axis Search, Fusion & Benchmark Validation
Developer can search memory units across syntactic, semantic, and graph axes independently and as a fused hybrid query, with explainable per-axis score breakdowns and paginated results. Benchmark suite validates the three-axis thesis with automated NDCG@10 scoring against a synthetic dataset with known ground truth.
**FRs covered:** FR14, FR15, FR16, FR17, FR18, FR19, FR22, FR24, FR25, FR63

**--- Gate 1 Validation Checkpoint: if hybrid doesn't outperform single-axis on 80%+ of benchmarks, re-evaluate graph axis investment ---**

### Phase: MVP — Core Domain (post Gate 1 validation)

### Epic 3: Case Management & Memory Organization
Developer can create cases, organize memory units into cases with strict single-case ownership, manage case members, view case status and activity, search within and across cases, delete individual memory units, and annotate/correct memory units — the collaborative memory structure that teams use to organize knowledge.
**FRs covered:** FR20, FR21, FR26, FR27, FR28, FR29, FR30, FR31, FR32, FR33, FR34, FR35, FR36, FR37

### Epic 4: Causal Intelligence & Graph Traversal
Developer can traverse causal chains from any starting node with configurable depth, filter by edge type, see gap markers for missing intermediate nodes, promote AI-inferred edge confidence, and view chronologically ordered causal chain nodes — the "why did this happen?" query interface over the graph edges created during ingestion (Epic 1).
**FRs covered:** FR47, FR48, FR49, FR50, FR51, FR52

### Phase: MVP — Gate 2 (Tenant Isolation)

### Epic 5: Tenant Isolation & Multi-Tenancy
Operator can provision tenants with physically separate indexes across all three backends, delete tenants with full cleanup, verify zero cross-tenant leakage via automated checks, manage tenant configuration (rate limits, embedding providers), and enforce tenant context at all access layers. System returns partial results when backends are unavailable rather than failing completely.
**FRs covered:** FR38, FR39, FR40, FR41, FR42, FR43, FR44, FR45, FR66, FR69, FR70

### Phase: MVP — Gate 3 (Developer Experience)

### Epic 6: Ingestion Pipeline Resilience & Operations
Developer can ingest from URLs and directories, monitor pipeline status per case, view failed units with error details, re-ingest failed content, and rely on per-tenant load management with automatic retry. System survives restarts without data loss.
**FRs covered:** FR2, FR3, FR8, FR9, FR10, FR11, FR12

### Epic 7: CLI & Developer Experience
Developer can accomplish all operational tasks via a polished CLI tool with actionable error messages including recovery suggestions, multiple output formats (human-readable, JSON, table), discoverable actions from any state (including empty and error states), metadata origin tracking display, and a guided quickstart — achieving <30 min onboarding on a clean machine.
**FRs covered:** FR53, FR55, FR56, FR57, FR64, FR67

### Phase: MVP — Operations

### Epic 8: Observability & System Health
Operator can verify consistency across all three backends, detect and repair index/graph divergence, export memory units/metadata/graph edges in a portable format, and observe the system via readiness/liveness health checks, structured logging, distributed traces, and custom metrics.
**FRs covered:** FR71, FR72, FR73, FR74

### Phase 1.5 (Fast-Follow — within 4 weeks of thesis validation)

### Epic 9: EventStore Integration & Zero-Code Memory
Any event-sourced system publishing to DAPR pub/sub topics gets automatic memory integration — events auto-discovered, dual embeddings generated (raw payload + natural language description), and CausationId/CorrelationId metadata automatically indexed as graph edges without developer mapping code. Developers can list registered handlers and detect mismatches.
**FRs covered:** FR59, FR60, FR61, FR62

### Epic 10: MCP Server & LLM Agent Interface
LLM agents can search, ingest, traverse, and query case info via MCP tools with typed parameter schemas, token-budget-aware responses, and structured error handling conforming to MCP protocol specification.
**FRs covered:** FR23, FR54, FR58

### Infrastructure (Cross-Cutting)

### Epic 11: CI/CD & Automated Quality Pipeline
Every commit is automatically built, tested, and versioned via GitHub Actions. PRs get build + test checks. Releases publish NuGet packages with semantic versioning from conventional commits. Branch protection on main.
**Driven by:** Architecture Decision D17

---

## Epic 1: Foundation, Ingestion & Graph Edge Indexing

Developer can boot the full stack with a single command, ingest content from local files, and see it persisted and searchable across all three backends — including typed graph edges created during ingestion. This epic establishes the entire infrastructure spine: Aspire AppHost, DAPR Workflows (IngestionWorkflow with saga/compensation), Contracts V1, Redis (RediSearch + Vector), FalkorDB, Kreuzberg (NuGet, in-process), git submodules, and the IndexGraphActivity.

### Story 1.1: Project Scaffolding & Single-Command Boot

As a developer,
I want to run a single command (`dotnet run --project Hexalith.Memories.AppHost`) and have the entire stack boot — Memories Server with DAPR sidecar, Redis Stack, FalkorDB, and Aspire Dashboard,
So that I have a working development environment without manual container orchestration.

**Acceptance Criteria:**

**Given** the repository is cloned with git submodules initialized (Hexalith.Commons, Hexalith.EventStore)
**When** I run `dotnet run --project Hexalith.Memories.AppHost`
**Then** Redis Stack container starts on port 6379
**And** FalkorDB container starts on port 6380
**And** Memories Server starts with DAPR sidecar (app port 5000, DAPR HTTP 3500, DAPR gRPC 50001)
**And** Aspire Dashboard is accessible showing all services healthy

**Given** the solution is opened for the first time
**When** I run `dotnet build`
**Then** the build succeeds with projects: Contracts, Server, Redis, AppHost, ServiceDefaults
**And** if git submodules are missing, the build prints a helpful error message instead of cryptic MSBuild failures

**Given** the AppHost is running
**When** I check the Aspire Dashboard
**Then** I see health status for Memories Server, Redis Stack, and FalkorDB
**And** OpenTelemetry traces, metrics, and structured JSON logging are configured via ServiceDefaults

### Story 1.2: Memory Unit Domain Model & Contracts

As a developer,
I want a well-defined domain model for memory units, graph edges, metadata fields, and ingestion types in `Contracts.V1`,
So that all services share a consistent, versioned type system with serialization guarantees.

**Acceptance Criteria:**

**Given** the Contracts.V1 namespace exists
**When** I inspect the memory unit model
**Then** it contains all required fields: Id (ULID), TenantId, CaseId, Content, ContentHash (SHA-256), SourceUri, SourceType (enum: file, url, event, command, projection, discussion), IngestedBy, IngestedAt, LastUpdated, Status (enum: queued, extracting, embedding, indexing, indexed, failed), Metadata (Dictionary<string, MetadataField>), EmbeddingProvider, EmbeddingDimensions, Classification (optional), FailureDetails (optional)
**And** MetadataField contains: value, origin (human/ai), confidence (0.0-1.0)

**Given** the graph edge model exists
**When** I inspect it
**Then** it contains: Id, SourceId, TargetId, EdgeType (enum: caused_by, correlated_with, references, contains, annotates), Confidence (float), Origin (enum: explicit, inferred), CreatedAt
**And** default confidence values per edge type are defined (caused_by=1.0, correlated_with=0.8, references=0.5-1.0, contains=1.0, annotates=1.0)

**Given** the error format is defined
**When** I inspect it
**Then** it contains: code (string), message (string), suggestion (string)
**And** JSON structure matches: `{"code": "TENANT_NOT_FOUND", "message": "...", "suggestion": "Run 'memories tenant list'..."}`

**Given** any Contracts.V1 type
**When** I serialize it to JSON and deserialize it back
**Then** the round-trip produces an identical object (contract tests pass)

### Story 1.3: Content Extraction via Kreuzberg

As a developer,
I want the system to extract text from ingested files (plain text, PDF, markdown) using Kreuzberg NuGet,
So that any supported file format can be processed into searchable content.

**Acceptance Criteria:**

**Given** the Kreuzberg NuGet package is installed in the Server project
**When** `ExtractContentActivity` receives a plain text file
**Then** it returns the raw text content unchanged

**Given** the Kreuzberg NuGet package is installed
**When** `ExtractContentActivity` receives a PDF file
**Then** it returns the extracted text content from the PDF

**Given** the Kreuzberg NuGet package is installed
**When** `ExtractContentActivity` receives a markdown file
**Then** it returns the text content with markdown structure preserved

**Given** `KreuzbergClient.ExtractBytesSync()` throws an exception
**When** `ExtractContentActivity` is invoked
**Then** the exception propagates for DAPR Workflow retry with exponential backoff

**Given** any supported file
**When** extraction completes
**Then** the Aspire Dashboard shows a trace span for the extraction activity with duration and status

### Story 1.4: Embedding Generation

As a developer,
I want the system to generate vector embeddings for extracted content using Google text-embedding-004,
So that memory units can be searched by semantic similarity.

**Acceptance Criteria:**

**Given** a tenant is configured with Google embedding provider (text-embedding-004, 768 dimensions)
**When** `GenerateEmbeddingActivity` receives extracted text content
**Then** it calls the Google embedding API and returns a 768-dimension vector
**And** the EmbeddingProvider and EmbeddingDimensions fields are populated on the memory unit

**Given** an `EmbeddingRateLimiterActor` exists for the tenant (actor ID = tenant ID)
**When** embedding generation is requested
**Then** the actor checks the rate budget before proceeding
**And** if the budget is exhausted, the activity waits until the rate window resets

**Given** the embedding API returns a 429 (rate limited) response
**When** the activity handles the error
**Then** DAPR Workflow retry policy triggers exponential backoff
**And** no data loss occurs

**Given** the embedding API key is configured
**When** the system accesses it
**Then** it reads from DAPR Secrets API (deployed) or .NET User Secrets (local dev)
**And** the key is never stored in config files or environment variables

### Story 1.5: Three-Backend Indexing

As a developer,
I want ingested content to be indexed across RediSearch (syntactic), Redis Vector (semantic), and FalkorDB (graph) with tenant-namespaced indexes,
So that memory units are searchable across all three axes after ingestion.

**Acceptance Criteria:**

**Given** a memory unit with extracted content and generated embedding
**When** `IndexSyntacticActivity` executes
**Then** the memory unit is indexed in RediSearch with tenant-namespaced index (`{tenantId}:syntactic`)
**And** the content, metadata, and source information are searchable via full-text query

**Given** a memory unit with a generated embedding vector
**When** `IndexSemanticActivity` executes
**Then** the vector is stored in Redis Vector Search with tenant-namespaced index (`{tenantId}:semantic`)
**And** the vector is retrievable via KNN similarity search

**Given** a memory unit with source information
**When** `IndexGraphActivity` executes
**Then** a node is created in FalkorDB in the tenant's dedicated database (physical isolation at database level)
**And** if the source contains CausationId, a `caused_by` edge is created (confidence 1.0, origin: explicit)
**And** if the source contains CorrelationId, a `correlated_with` edge is created (confidence 0.8, origin: explicit)
**And** a `contains` edge is created from the case node to the memory unit node (confidence 1.0)

**Given** the `IGraphQueryBuilder` is used for all FalkorDB queries
**When** any graph operation is performed
**Then** only parameterized Cypher queries are used — no raw Cypher string construction
**And** this is enforced structurally by the interface design

**Given** indexes are created for a tenant
**When** I inspect the index naming
**Then** the naming scheme supports concurrent versions (`{tenantId}:{model-version}:syntactic`) for future model migration

### Story 1.6: Ingestion Workflow Orchestration

As a developer,
I want to ingest a local file and have it automatically processed through the full pipeline (validate → extract → embed → index across all backends → verify consistency),
So that a single API call results in a fully searchable memory unit with provenance tracking.

**Acceptance Criteria:**

**Given** a valid file and a tenant/case context
**When** `IngestionWorkflow` is started
**Then** it orchestrates: `ValidateContentActivity` → `ExtractContentActivity` → `GenerateEmbeddingActivity` → fan-out (`IndexSyntacticActivity` + `IndexSemanticActivity` + `IndexGraphActivity`) → `VerifyConsistencyActivity`
**And** the memory unit status transitions: queued → extracting → embedding → indexing → indexed

**Given** `VerifyConsistencyActivity` runs after all indexing activities complete
**When** it queries all three backends for the memory unit
**Then** it confirms the unit exists in RediSearch, Redis Vector, and FalkorDB
**And** if any backend is missing the unit, it reports the discrepancy

**Given** `IndexSemanticActivity` fails after `IndexSyntacticActivity` succeeds
**When** the workflow retry policy is exhausted
**Then** compensation activities clean up the successfully written RediSearch entry
**And** the memory unit status is set to `failed` with FailureDetails (stage: indexing, error code, retry count)
**And** the failed unit is never silently dropped

**Given** ingestion completes successfully
**When** I inspect the memory unit
**Then** `ingested_by` contains the user or system identity (FR65)
**And** `IngestedAt` timestamp is set
**And** metadata fields each track origin (human/ai) and confidence (FR7)

**Given** the DAPR sidecar restarts during an in-progress workflow
**When** the sidecar recovers
**Then** the workflow resumes from its last persisted state (Durable Task Framework)
**And** no data loss occurs

**Given** the same content is ingested twice (duplicate detection)
**When** the second ingestion is processed
**Then** duplicate detection by source identifier prevents duplicate memory units

### Story 1.7: Embedding Provider Configuration

As a developer,
I want to configure the embedding provider and model per tenant,
So that different tenants can use different embedding providers and the system is ready for multi-provider support.

**Acceptance Criteria:**

**Given** a new tenant is being configured
**When** I set the embedding provider configuration
**Then** I can specify: provider (google), model (text-embedding-004), dimensions (768), rateLimitPerMinute (1500)
**And** the configuration is stored as part of the tenant configuration

**Given** the tenant configuration supports the provider/model/dimensions/rateLimit fields
**When** the `GenerateEmbeddingActivity` runs for a tenant
**Then** it reads the tenant's provider configuration to determine which embedding API to call
**And** it reads the tenant's rate limit to configure the `EmbeddingRateLimiterActor`

**Given** MVP supports Google only
**When** I inspect the configuration structure
**Then** the provider field accepts an enum/string that can be extended to openai, mistral, custom in future phases
**And** the `IEmbeddingProvider` pattern (concrete class, not interface) supports addition of new providers without refactoring

**Given** switching embedding providers requires full reindex
**When** a tenant's provider configuration is changed
**Then** the system warns that existing vectors are incompatible and a reindex is required
**And** the change is not silently applied without acknowledgment

---

## Epic 2: Three-Axis Search, Fusion & Benchmark Validation

Developer can search memory units across syntactic, semantic, and graph axes independently and as a fused hybrid query, with explainable per-axis score breakdowns and paginated results. Benchmark suite validates the three-axis thesis with automated NDCG@10 scoring against a synthetic dataset with known ground truth. This is the Gate 1 critical path — if hybrid doesn't outperform single-axis on 80%+ of benchmarks, the product direction must be re-evaluated.

### Story 2.1: Syntactic Search (BM25 via RediSearch)

As a developer,
I want to search memory units by text terms using BM25 ranking within a tenant,
So that I can find memory units that contain specific keywords or phrases.

**Acceptance Criteria:**

**Given** a tenant with indexed memory units containing varied content
**When** I execute a syntactic search with query terms (e.g., "claim denied")
**Then** results are returned ranked by BM25 relevance score
**And** each result includes the memory unit summary, raw BM25 score, SourceUri, and SourceType (FR24)
**And** results are scoped to the specified tenant only

**Given** a syntactic search is executed
**When** results are returned
**Then** p95 latency is <200ms at 10 concurrent queries/tenant with 10K memory units (NFR1)

**Given** a search query that matches no memory units
**When** results are returned
**Then** an empty result set is returned with zero results count
**And** no error is thrown

**Given** a tenant with no indexed memory units
**When** a syntactic search is executed
**Then** an empty result set is returned with a clear indication that no memory units exist

### Story 2.2: Semantic Search (Vector via Redis Vector)

As a developer,
I want to search memory units by semantic similarity using vector embeddings within a tenant,
So that I can find memory units that are conceptually related to my query even without exact keyword matches.

**Acceptance Criteria:**

**Given** a tenant with indexed memory units and their embedding vectors
**When** I execute a semantic search with a natural language query
**Then** the query is embedded using the tenant's configured embedding provider
**And** KNN similarity search is performed against Redis Vector
**And** results are returned ranked by cosine similarity score (native 0.0-1.0 range)
**And** each result includes the memory unit summary, cosine score, SourceUri, and SourceType (FR24)

**Given** a semantic search is executed
**When** results are returned
**Then** p95 latency is <500ms at 10 concurrent queries/tenant with 10K memory units (NFR2)

**Given** a query like "payment rejection" against memory units containing "claim denied"
**When** semantic search is executed
**Then** semantically similar results appear even without keyword overlap

### Story 2.3: Graph-Scoped Search

As a developer,
I want to search memory units by first traversing the graph to find related nodes, then searching within that set,
So that I can discover content that is structurally connected to a starting point.

**Acceptance Criteria:**

**Given** a memory unit with known graph relationships (caused_by, correlated_with, references edges)
**When** I execute a graph-scoped search with a starting node ID and depth
**Then** the system performs a two-stage query: traverse first (find related node IDs via FalkorDB), then search within that set
**And** results include only memory units reachable within the specified depth

**Given** a graph-scoped search with optional `graph_scope` parameter
**When** the parameter specifies a starting node and depth
**Then** the search is constrained to the subgraph reachable from that node
**And** results still carry syntactic and/or semantic scores from the inner search

**Given** the starting node has no graph edges
**When** graph-scoped search is executed
**Then** only the starting node itself is returned (depth 0)
**And** no error is thrown

**Given** all graph queries
**When** executed against FalkorDB
**Then** only parameterized Cypher via `IGraphQueryBuilder` is used
**And** queries are scoped to the tenant's dedicated FalkorDB database

### Story 2.4: Score Normalization

As a developer,
I want all search axis scores normalized to 0.0-1.0 before fusion,
So that scores from different axes are comparable and the fusion algorithm produces meaningful composite rankings.

**Acceptance Criteria:**

**Given** a raw BM25 score from RediSearch (unbounded range)
**When** normalization is applied
**Then** the score is normalized to 0.0-1.0 using saturation normalization against corpus statistics
**And** the `CorpusStatisticsActor` per tenant provides: document count, average document length, term frequencies
**And** the normalization function is a pure function: `NormalizeBm25(rawScore, corpusStats) → float` with known inputs producing known outputs

**Given** a cosine similarity score from Redis Vector
**When** normalization is applied
**Then** the score is passed through unchanged (native 0.0-1.0 range)

**Given** a graph proximity score
**When** normalization is applied
**Then** the score is computed via inverse hop distance with decay function, producing 0.0-1.0
**And** the decay function is documented and deterministic

**Given** the `CorpusStatisticsActor` for a tenant
**When** corpus statistics are queried
**Then** methods `GetDocumentCount()`, `GetAverageDocumentLength()`, `GetTermFrequency(term)` return cached values
**And** statistics are refreshed via timer
**And** actor state is persisted before every response (not batch-persisted on deactivation)

**Given** normalization unit tests with known inputs
**When** each normalization function is executed
**Then** outputs match expected values exactly (NFR24)

### Story 2.5: Fusion Algorithm & Hybrid Search

As a developer,
I want to search memory units across all available axes in a single hybrid query with configurable axis selection,
So that I get the best possible results by combining syntactic, semantic, and graph relevance signals.

**Acceptance Criteria:**

**Given** a hybrid search query with all three axes enabled
**When** the fusion algorithm executes
**Then** it calls all three search backends in parallel
**And** results are merged using the pure function `Fuse(List<ScoredResult>[], FusionWeights) → RankedResults`
**And** the composite score is a weighted average of normalized axis scores
**And** the function has no backend calls or hidden state — all dependencies (corpus statistics, normalization parameters) are injected

**Given** the same query executed twice against the same data
**When** fusion scores are computed
**Then** identical composite scores are produced (NFR25: deterministic)
**And** result ordering within the same score tier may vary

**Given** a search query with axis selection control (FR18)
**When** I specify `axes=syntactic,semantic` (excluding graph)
**Then** only BM25 and vector search are executed
**And** the fusion algorithm operates on the available axes only
**And** the graph axis is architecturally optional — disabling it is a config change, not a rearchitecture

**Given** a hybrid search is executed
**When** results are returned
**Then** p95 latency is <1s at 10 concurrent queries/tenant with 10K memory units (NFR3)

**Given** one search backend is unavailable during hybrid search
**When** the remaining axes return results
**Then** the system returns partial results with a `degraded: true` flag indicating which axes were unavailable

### Story 2.6: Explain Mode & Confidence Scores

As a developer,
I want to see per-axis score breakdowns and composite confidence scores for each search result, with pagination support,
So that I understand why each result appeared and can debug relevance issues.

**Acceptance Criteria:**

**Given** a search query with explain mode enabled
**When** results are returned
**Then** each result includes: composite confidence score (0.0-1.0), per-axis breakdown (syntactic score, semantic score, graph score), and the normalization method applied per axis (FR19, FR63)

**Given** explain mode output
**When** I inspect the response
**Then** the caveat is included: "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness"

**Given** a search query returns more results than the page size
**When** I request paginated results (FR22)
**Then** results are returned in pages with total count, page number, and next page token
**And** pagination preserves score ordering across pages

**Given** a search result
**When** I inspect the origin information
**Then** it includes SourceUri (file path, URL, or event ID) and SourceType (FR24)

### Story 2.7: Benchmark Suite & Thesis Validation

As a developer,
I want to run automated benchmark comparisons of hybrid vs single-axis search results,
So that I can validate the three-axis thesis with reproducible, scored evidence.

**Acceptance Criteria:**

**Given** a synthetic benchmark dataset with known relationships and controlled vocabulary (D11)
**When** the dataset is loaded
**Then** it contains sufficient memory units with defined ground truth results for each benchmark query
**And** relationships (causal edges, correlations, references) are pre-defined for graph axis testing

**Given** 5-10 benchmark queries that require all three axes
**When** each query is executed in hybrid mode and in each single-axis mode
**Then** results are scored by NDCG@10 (Normalized Discounted Cumulative Gain at rank 10) against ground truth

**Given** the benchmark suite is run twice against the same dataset
**When** NDCG@10 scores are computed
**Then** identical scores are produced (NFR26: reproducible)

**Given** benchmark results for hybrid vs single-axis
**When** the comparison is evaluated
**Then** the output clearly shows: hybrid NDCG@10 score, each single-axis NDCG@10 score, and whether hybrid outperforms on each query (FR25)
**And** the 80% threshold (hybrid outperforms single-axis on 80%+ of benchmarks) is evaluated and reported

**Given** the benchmark suite
**When** it runs in CI
**Then** it completes within a reasonable time and produces a machine-readable results file
**And** results include per-query breakdown suitable for analysis

---

## Epic 3: Case Management & Memory Organization

Developer can create cases, organize memory units into cases with strict single-case ownership, manage case members, view case status and activity, search within and across cases, delete individual memory units, and annotate/correct memory units — the collaborative memory structure that teams use to organize knowledge.

### Story 3.1: Create and List Cases

As a developer,
I want to create cases within a tenant and list existing cases,
So that I can organize memory units into meaningful groups with strict ownership boundaries.

**Acceptance Criteria:**

**Given** a valid tenant context
**When** I create a case with a name and optional description
**Then** a case is created with a unique ID (ULID), tenant association, creation timestamp, and status "active"
**And** a case node is created in FalkorDB within the tenant's database
**And** the case is immediately visible in the case list

**Given** a tenant with multiple cases
**When** I list cases
**Then** all cases for the tenant are returned with ID, name, description, status, creation date, and memory unit count (FR30)

**Given** a memory unit is ingested into a case
**When** the ingestion completes
**Then** the memory unit belongs to exactly one case — no multi-case membership (FR32)
**And** a `contains` edge is created from the case node to the memory unit node in FalkorDB (FR33)

**Given** a memory unit already belongs to a case
**When** an attempt is made to assign it to a different case
**Then** the operation is rejected with error code `SINGLE_CASE_OWNERSHIP` and suggestion "Delete the unit and re-ingest into the target case"

### Story 3.2: Case Status & Activity

As a developer,
I want to view case status and recent activity,
So that I can monitor the health and usage of each case.

**Acceptance Criteria:**

**Given** a case with indexed memory units
**When** I view case status (FR31)
**Then** I see: memory unit count, last activity timestamp, and health indicators (all-backends-indexed count vs total, any failed units)

**Given** a case with recent operations
**When** I view recent activity (FR36)
**Then** I see a chronological list of events: ingestion events (unit added/failed), search queries against this case, membership changes (member added/removed)
**And** each event includes timestamp, event type, actor (user/system), and brief description

**Given** a case with no activity
**When** I view recent activity
**Then** an empty activity list is returned with the case creation event as the only entry

### Story 3.3: Case Member Management

As a developer,
I want to add and remove members to a case,
So that I can control who has access to the knowledge within each case.

**Acceptance Criteria:**

**Given** an existing case
**When** I add a member by identity (user ID or role)
**Then** the member is associated with the case
**And** a membership-changed activity event is recorded (FR36)

**Given** a case with members
**When** I remove a member by identity
**Then** the member is disassociated from the case
**And** a membership-changed activity event is recorded

**Given** a case with members
**When** I list case details
**Then** the member list is included in the response

**Given** an attempt to add a member that already exists in the case
**When** the operation is processed
**Then** it is idempotent — no error, no duplicate entry

### Story 3.4: Case-Scoped & Cross-Case Search

As a developer,
I want to filter search results by case and metadata, and search across all cases within a tenant,
So that I can find knowledge both within a specific context and across the entire tenant.

**Acceptance Criteria:**

**Given** a tenant with multiple cases containing memory units
**When** I execute a search with a case filter (FR20)
**Then** results are returned only from the specified case
**And** all search axes (syntactic, semantic, graph, hybrid) respect the case filter

**Given** memory units with metadata fields (e.g., source_type, priority, category)
**When** I execute a search with metadata filters (FR21)
**Then** results are filtered to only memory units where the specified metadata field values match
**And** metadata filters combine with case filters (AND logic)

**Given** a tenant with multiple cases
**When** I execute a cross-case search (FR34)
**Then** results are returned from all cases within the tenant
**And** each result includes case attribution (case ID and case name)
**And** results are ranked by relevance regardless of case boundaries

**Given** a case filter specifying a case that does not exist
**When** the search is executed
**Then** an error is returned with code `CASE_NOT_FOUND` and suggestion to list available cases

### Story 3.5: Memory Unit Deletion & Case Deletion

As a developer,
I want to delete individual memory units and entire cases,
So that I can manage knowledge lifecycle and clean up outdated content.

**Acceptance Criteria:**

**Given** a memory unit in a case
**When** I delete the memory unit (FR35)
**Then** it is removed from RediSearch (syntactic index entry)
**And** it is removed from Redis Vector (semantic vector)
**And** it is removed from FalkorDB (node and all connected edges)
**And** a deletion activity event is recorded in the case activity log

**Given** a case with memory units
**When** I delete the case (FR27)
**Then** all memory units in the case are deleted from all three backends
**And** the case node and all case-scoped edges are removed from FalkorDB
**And** the case is removed from the case list
**And** the operation completes atomically — partial deletion is not acceptable (use DAPR Workflow for orchestration)

**Given** a case deletion is in progress
**When** a new ingestion request targets the case
**Then** the ingestion is rejected with error code `CASE_DELETING` and a suggestion to wait

**Given** a memory unit has graph edges connecting it to other memory units
**When** the memory unit is deleted
**Then** all edges to and from that memory unit are also deleted
**And** the connected memory units are not affected

### Story 3.6: Annotations & Corrections

As a developer,
I want to annotate or correct a memory unit, with annotations tracked as linked memory units,
So that human knowledge and corrections are preserved alongside the original content.

**Acceptance Criteria:**

**Given** an existing memory unit
**When** I create an annotation with text content and metadata
**Then** a new memory unit is created with the annotation content
**And** an `annotates` edge (confidence 1.0, origin: explicit) is created from the annotation to the original memory unit in FalkorDB (FR37)
**And** the annotation memory unit has its own embeddings and is independently searchable

**Given** a correction annotation
**When** I create it with type "correction"
**Then** the metadata field `annotation_type` is set to "correction" with origin "human" and confidence 1.0
**And** the original memory unit is not modified — corrections are additive, not destructive

**Given** a memory unit with annotations
**When** I search and find the original memory unit
**Then** the result includes an `annotations_count` field
**And** annotations can be retrieved by traversing the `annotates` edges from the result

**Given** the original memory unit is deleted
**When** the deletion is processed
**Then** the annotation memory units are also deleted (cascade via `annotates` edges)

---

## Epic 4: Causal Intelligence & Graph Traversal

Developer can traverse causal chains from any starting node with configurable depth, filter by edge type, see gap markers for missing intermediate nodes, promote AI-inferred edge confidence, and view chronologically ordered causal chain nodes — the "why did this happen?" query interface over the graph edges created during ingestion (Epic 1).

### Story 4.1: Causal Chain Traversal

As a developer,
I want to traverse causal chains from a starting memory unit with configurable depth,
So that I can understand how events, documents, and decisions are causally connected.

**Acceptance Criteria:**

**Given** a memory unit with known causal relationships (caused_by, correlated_with, references edges)
**When** I execute a traversal from that node with depth=3
**Then** the system returns all reachable memory units within 3 hops
**And** results are ordered chronologically by timestamp on each node (FR52)
**And** each result includes: memory unit summary, edge metadata (type, confidence, origin, direction), and timestamps establishing chronological order

**Given** a traversal response
**When** I inspect the structure
**Then** it provides full node context (memory unit summary + edge metadata), not just IDs
**And** the response enables single-call causal chain composition without a second search round-trip

**Given** a traversal with depth=0
**When** executed
**Then** only the starting node is returned with its direct edge metadata

**Given** a traversal is executed
**When** results are returned
**Then** p95 latency is <2s at 10 concurrent queries/tenant with 10K memory units and depth <=5 (NFR4)

**Given** all traversal queries
**When** executed against FalkorDB
**Then** only parameterized Cypher via `IGraphQueryBuilder` is used
**And** queries are scoped to the tenant's dedicated FalkorDB database

### Story 4.2: Edge Type Filtering & Taxonomy

As a developer,
I want to filter graph traversals by edge type,
So that I can focus on specific relationship categories (e.g., only causal links, or only references).

**Acceptance Criteria:**

**Given** a memory unit with multiple edge types connecting to other units
**When** I execute a traversal with edge type filter `caused_by` (FR48)
**Then** only edges of type `caused_by` are followed during traversal
**And** other edge types are ignored even if they exist

**Given** the full edge type taxonomy (FR50)
**When** I inspect available edge types
**Then** the system supports: `caused_by` (default confidence 1.0), `correlated_with` (0.8), `references` (0.5-1.0), `contains` (1.0), `annotates` (1.0)
**And** each edge type is classified as structural (contains, annotates) or semantic (caused_by, correlated_with, references)

**Given** a traversal with multiple edge type filters (e.g., `caused_by,correlated_with`)
**When** executed
**Then** edges matching any of the specified types are followed (OR logic)

**Given** a traversal with no edge type filter specified
**When** executed
**Then** all semantic edge types are followed by default (caused_by, correlated_with, references)
**And** structural edges (contains, annotates) are excluded from default traversal to avoid noise

**Given** the distinction between `caused_by` and `correlated_with`
**When** edges are created and queried
**Then** CausationId produces `caused_by` edges (direct causal link)
**And** CorrelationId produces `correlated_with` edges (same correlation context, not necessarily causal)
**And** these are never collapsed — every event in a correlation group does NOT appear to cause every other event

### Story 4.3: Gap Detection & Confidence Promotion

As a developer,
I want to see gap markers when intermediate nodes in a causal chain are missing, and promote AI-inferred edge confidence when I verify a relationship,
So that I can trust the completeness of causal chains and contribute human verification to improve data quality.

**Acceptance Criteria:**

**Given** a causal chain where A's CausationId points to B, and B's points to C, but B is not indexed
**When** traversal is executed from A
**Then** the chain includes a gap marker: `A → [MISSING: event-id-B] → C` (FR49)
**And** the missing node identifier is included so the gap is traceable
**And** the system never silently skips missing nodes

**Given** a causal chain with multiple gaps
**When** traversal is executed
**Then** all gaps are flagged individually with their specific missing node identifiers
**And** the chain structure remains intact around the gaps

**Given** an edge with AI-inferred confidence (e.g., references edge at 0.5)
**When** a developer promotes the confidence to 1.0 (FR51)
**Then** the edge confidence is updated to the promoted value
**And** the edge origin remains unchanged (still `inferred`) but a new field `verified_by` records the promoting identity
**And** the system never auto-promotes — only explicit human action changes confidence

**Given** an edge with explicit origin (e.g., caused_by from CausationId)
**When** a developer attempts to change the confidence
**Then** the operation succeeds (human override is allowed)
**And** the original confidence is preserved in an audit field for traceability

**Given** late-arriving events that fill a previously detected gap
**When** the missing node is ingested
**Then** the gap marker is retroactively resolved
**And** the causal chain becomes complete without manual intervention

---

## Epic 5: Tenant Isolation & Multi-Tenancy

Operator can provision tenants with physically separate indexes across all three backends, delete tenants with full cleanup, verify zero cross-tenant leakage via automated checks, manage tenant configuration (rate limits, embedding providers), and enforce tenant context at all access layers. System returns partial results when backends are unavailable rather than failing completely. This is the Gate 2 critical path — zero cross-tenant data leakage is a hard gate.

### Story 5.1: Tenant Provisioning Workflow

As an operator,
I want to create a tenant with physically separate indexes across all three backends in a single command,
So that each tenant has isolated infrastructure with rollback protection if provisioning fails.

**Acceptance Criteria:**

**Given** a new tenant ID and display name
**When** `TenantProvisioningWorkflow` is started
**Then** it orchestrates: `ProvisionRediSearchActivity` → `ProvisionRedisVectorActivity` → `ProvisionFalkorDbActivity` → `VerifyTenantActivity`
**And** RediSearch creates tenant-namespaced indexes (`{tenantId}:syntactic`)
**And** Redis Vector creates tenant-namespaced indexes (`{tenantId}:semantic`)
**And** FalkorDB creates a dedicated database for the tenant (physical isolation at database level, not label level)

**Given** `ProvisionFalkorDbActivity` fails after RediSearch and Redis Vector indexes are created
**When** the workflow handles the failure
**Then** compensation activities delete the successfully created RediSearch and Redis Vector indexes (saga rollback)
**And** the tenant is not left in a partially provisioned state
**And** the error is reported with details of what failed and what was rolled back

**Given** `VerifyTenantActivity` runs after all backends are provisioned
**When** verification completes
**Then** it confirms: all three backend indexes exist, are empty, and are accessible
**And** the tenant is marked as active in the tenant registry

**Given** a tenant is successfully provisioned
**When** I inspect the provisioning time
**Then** it completes in <5 minutes (single CLI command, per Kenji's journey)

### Story 5.2: Tenant Deletion Workflow

As an operator,
I want to delete a tenant and all its data across all backends,
So that I can fulfill erasure requirements and reclaim resources.

**Acceptance Criteria:**

**Given** a tenant with memory units, cases, and graph data
**When** `TenantDeletionWorkflow` is started
**Then** it orchestrates: `DeleteRediSearchActivity` → `DeleteRedisVectorActivity` → `DeleteFalkorDbActivity`
**And** all RediSearch indexes for the tenant are dropped
**And** all Redis Vector indexes for the tenant are dropped
**And** the FalkorDB database for the tenant is deleted

**Given** a large tenant with many graph nodes
**When** `DeleteFalkorDbActivity` executes
**Then** deletion is batched (N nodes per activity invocation, yield between batches)
**And** batched deletion does not block other tenants' graph queries

**Given** a deletion is in progress
**When** a search or ingestion request targets the deleting tenant
**Then** the request is rejected with error code `TENANT_DELETING`

**Given** the tenant deletion completes
**When** I list tenants
**Then** the deleted tenant no longer appears
**And** any search across all axes returns zero results for the deleted tenant ID

### Story 5.3: Tenant Isolation Verification

As an operator,
I want to run automated tenant isolation checks,
So that I can verify zero cross-tenant data leakage with confidence.

**Acceptance Criteria:**

**Given** two tenants (A and B) each with indexed memory units
**When** I run `tenant verify` on tenant A (FR40)
**Then** automated checks confirm:
**And** search from tenant A context returns zero results from tenant B across all axes (syntactic, semantic, graph)
**And** ingestion into tenant A is not visible from tenant B's context
**And** graph traversal from tenant A returns zero nodes from tenant B

**Given** identical graph structures created in tenant A and tenant B
**When** traversal is executed from tenant A
**Then** zero nodes from tenant B appear even if edge IDs collide (NFR8)

**Given** malformed, empty, or swapped tenant IDs
**When** used in search, ingestion, or graph traversal requests
**Then** the system rejects them with clear error messages — never falls through to a default tenant or cross-tenant access

**Given** a verification run completes
**When** results are returned
**Then** they include per-check pass/fail status with details for any failures

### Story 5.4: Tenant Context Enforcement

As an operator,
I want tenant context enforced at all access layers,
So that cross-tenant requests are structurally impossible, not just policy-prohibited.

**Acceptance Criteria:**

**Given** a request with a tenant ID in the payload
**When** the Memories Server processes it
**Then** the tenant ID is validated against the tenant registry before any operation
**And** unknown tenant IDs are rejected with error code `TENANT_NOT_FOUND` and suggestion to list tenants (FR44)

**Given** a request authenticated as tenant A attempting to access tenant B
**When** the server processes it
**Then** it is rejected with error code `TENANT_MISMATCH` and clear error message (FR44)

**Given** inter-service communication between Memories Server components
**When** any call is made
**Then** DAPR API token authentication is required (NFR10)
**And** unauthenticated requests are rejected

**Given** all FalkorDB graph queries
**When** executed
**Then** they are scoped to the tenant's dedicated database
**And** parameterized Cypher via `IGraphQueryBuilder` prevents query injection that could access other databases

### Story 5.5: Tenant Configuration & Listing

As an operator,
I want to list tenants, view and update their configuration,
So that I can manage the multi-tenant environment effectively.

**Acceptance Criteria:**

**Given** a multi-tenant deployment
**When** I list tenants (FR41)
**Then** all tenants are returned with: ID, display name, status (active/provisioning/deleting), creation date, memory unit count, index sizes

**Given** an existing tenant
**When** I view its configuration (FR45)
**Then** I see: embedding provider, model, dimensions, rate limit ceiling, index status per backend, last activity timestamp

**Given** an existing tenant
**When** I update configuration — rate limits, display name (FR42)
**Then** the changes are applied immediately for non-breaking changes
**And** the update is recorded in the tenant's audit trail

**Given** a configuration change that would create data inconsistency (e.g., changing embedding dimensions without reindex)
**When** the change is attempted (FR43)
**Then** the system rejects it with a clear explanation of the inconsistency
**And** the operator must explicitly acknowledge the risk to proceed

**Given** per-tenant rate limit ceilings (FR69)
**When** the `EmbeddingRateLimiterActor` enforces limits
**Then** it uses the tenant's configured `rateLimitPerMinute` value

**Given** a memory unit is ingested
**When** it is indexed
**Then** the embedding provider and model used are recorded on the memory unit (FR70)
**And** this enables future auditing of which vectors used which model

### Story 5.6: Graceful Degradation on Backend Failure

As a developer,
I want the system to return partial results when a backend is unavailable,
So that I get the best possible answer even during infrastructure issues.

**Acceptance Criteria:**

**Given** Redis Vector is unavailable but RediSearch and FalkorDB are healthy
**When** a hybrid search is executed
**Then** results are returned from syntactic and graph axes only
**And** the response includes a `degraded: true` flag and indicates semantic axis was excluded (FR66)

**Given** FalkorDB is unavailable but Redis Stack is healthy
**When** a hybrid search is executed
**Then** results are returned from syntactic and semantic axes only
**And** graph traversal requests return an error indicating the graph backend is unavailable

**Given** all backends are unavailable
**When** any operation is attempted
**Then** a clear error is returned indicating total service unavailability with recovery suggestion

**Given** a backend recovers after being unavailable
**When** subsequent requests are made
**Then** the system automatically resumes using all available axes
**And** no manual intervention is required to restore full functionality

**Given** partial backend failure during ingestion
**When** the `IngestionWorkflow` encounters a backend outage
**Then** DAPR Workflow retry policies handle the retry with exponential backoff
**And** the workflow does not fail permanently until max retries are exhausted

---

## Epic 6: Ingestion Pipeline Resilience & Operations

Developer can ingest from URLs and directories, monitor pipeline status per case, view failed units with error details, re-ingest failed content, and rely on per-tenant load management with automatic retry. System survives restarts without data loss. This is production-grade ingestion that builds on the basic pipeline from Epic 1.

### Story 6.1: URL & Directory Ingestion

As a developer,
I want to ingest content from URLs and batch-ingest entire directories,
So that I can populate case memory from web resources and local file collections efficiently.

**Acceptance Criteria:**

**Given** a valid URL pointing to a web page or document
**When** I ingest from that URL into a case (FR2)
**Then** the system fetches the URL content, passes it through `ExtractContentActivity` (Kreuzberg), and processes it through the full `IngestionWorkflow`
**And** the memory unit's SourceUri is set to the URL and SourceType is `url`

**Given** a URL that returns a 404 or is unreachable
**When** ingestion is attempted
**Then** the `ExtractContentActivity` fails with a clear error
**And** the memory unit moves to `failed` status with FailureDetails including the HTTP status or network error

**Given** a directory containing multiple files (mixed types: PDF, markdown, text)
**When** I batch-ingest the directory into a case (FR3)
**Then** each file is enqueued as a separate `IngestionWorkflow` instance
**And** progress is visible: total files discovered, currently processing, completed, failed
**And** the batch does not block other tenants' ingestion

**Given** a directory containing unsupported file types
**When** batch ingestion processes them
**Then** unsupported files are reported as skipped with the reason
**And** supported files continue processing normally

### Story 6.2: Per-Tenant Load Management & Rate Limiting

As a developer,
I want ingestion load managed independently per tenant with enforced rate limits,
So that one tenant's batch ingestion doesn't starve another's real-time ingestion.

**Acceptance Criteria:**

**Given** two tenants ingesting content simultaneously
**When** tenant A starts a large batch ingestion
**Then** tenant B's real-time ingestion is not blocked or degraded (FR8, NFR13)
**And** each tenant's `EmbeddingRateLimiterActor` enforces its own rate ceiling independently

**Given** a tenant's embedding API returns 429 (rate limited)
**When** the `GenerateEmbeddingActivity` handles the response
**Then** DAPR Workflow retry policy triggers exponential backoff with jitter (NFR22)
**And** no data loss occurs — the workflow retries the activity, not the entire pipeline
**And** the rate limiter actor adjusts its budget to prevent immediate re-exhaustion

**Given** pipeline resource isolation
**When** CPU-intensive extraction (PDF, large URL fetch) is running for tenant A
**Then** extraction for tenant B is not blocked
**And** workflow concurrency control bounds per-tenant extraction activity concurrency

**Given** shared embedding API keys across tenants
**When** rate limits are enforced
**Then** per-tenant throttle ceilings are enforced by the `EmbeddingRateLimiterActor`
**And** the actual provider API ceiling is documented as the shared bottleneck

### Story 6.3: Retry, Failure Visibility & Re-Ingestion

As a developer,
I want automatic retry with configurable limits, full visibility into failures, and the ability to re-ingest failed content,
So that transient errors are handled automatically and persistent failures are diagnosable and recoverable.

**Acceptance Criteria:**

**Given** a transient failure at any pipeline stage (extraction, embedding, indexing)
**When** the activity fails
**Then** DAPR Workflow retry policy retries with exponential backoff (FR9)
**And** retry count and configuration (max retries, backoff interval) are configurable per activity type

**Given** a case with ingestion activity
**When** I view ingestion status per case (FR10)
**Then** I see counts: indexed, embedding (retrying), queued, failed
**And** the status reflects real-time pipeline state

**Given** failed ingestion units exist
**When** I view failed units (FR11)
**Then** each failed unit shows: memory unit ID, source URI, failure stage (extracting/embedding/indexing), error code, error message, retry count, last retry timestamp

**Given** a failed or previously ingested memory unit
**When** I trigger re-ingestion individually or in bulk (FR12)
**Then** the `IngestionWorkflow` is restarted for the specified units
**And** re-ingestion is idempotent — duplicate detection by source identifier prevents duplicate memory units

**Given** max retries are exhausted
**When** the unit moves to `failed` status
**Then** it is never silently dropped (NFR19)
**And** it remains visible in the failed units list until manually re-ingested or deleted

### Story 6.4: Pipeline State Persistence & Zero Data Loss

As a developer,
I want the ingestion pipeline to survive process restarts without data loss,
So that I can trust the system's reliability in production.

**Acceptance Criteria:**

**Given** an in-progress `IngestionWorkflow` with activities at various stages
**When** the Memories Server process restarts
**Then** all workflows resume from their last persisted state via DAPR Workflow (Durable Task Framework) (NFR17)
**And** no queued or in-progress memory units are lost
**And** activities that were mid-execution are retried (not duplicated)

**Given** Redis is restarted
**When** it comes back up
**Then** all indexed memory units are intact via AOF persistence (NFR16)
**And** zero memory units are lost

**Given** the DAPR sidecar restarts
**When** it recovers
**Then** workflow state is automatically replayed from persisted history
**And** actor state (rate limiter budgets, corpus statistics) is restored from Redis state store
**And** pending workflows continue without manual intervention

**Given** the full stack is started from cold
**When** all containers and services boot
**Then** the service is fully operational within 60 seconds (NFR7) — excludes image pull time
**And** the Aspire Dashboard shows all services healthy

**Given** sustained ingestion workload
**When** throughput is measured
**Then** the system achieves >100 memory units/min for payloads <=10KB per tenant (NFR5)
**And** >10 memory units/min for payloads <=1MB per tenant

---

## Epic 7: CLI & Developer Experience

Developer can accomplish all operational tasks via a polished CLI tool with actionable error messages including recovery suggestions, multiple output formats (human-readable, JSON, table), discoverable actions from any state (including empty and error states), metadata origin tracking display, and a guided quickstart — achieving <30 min onboarding on a clean machine. This is the Gate 3 critical path.

### Story 7.1: CLI Foundation & Command Structure

As a developer,
I want to install a CLI tool and interact with all retrieval, ingestion, and management capabilities through a consistent command structure,
So that I have a single tool for all Memories operations across any environment.

**Acceptance Criteria:**

**Given** the CLI is published as a .NET global tool
**When** I run `dotnet tool install -g Hexalith.Memories.Cli`
**Then** the `memories` command is available globally

**Given** the CLI is installed
**When** I run `memories`
**Then** I see top-level command groups: `ingest`, `search`, `traverse`, `case`, `tenant`, `status`, `explore`, `handlers`, `quickstart` (FR53)
**And** each group shows a brief description

**Given** the CLI needs to connect to the Memories Server
**When** I configure the endpoint
**Then** configuration layering is respected (precedence high to low): command-line flags → environment variables (`HEXALITH_MEMORIES_*`) → config file (`~/.hexalith/memories.json` or project-local) → DAPR Secrets API → .NET User Secrets → DAPR configuration (NFR23)

**Given** the CLI is configured for different environments
**When** I target localhost (local dev), docker service name (container), or remote URL (ingress)
**Then** the CLI connects successfully to each environment type

**Given** any CLI command
**When** it communicates with the Memories Server
**Then** it uses the REST API via infrastructure ingress (Client.Rest package)
**And** authentication uses the configured DAPR API token or ingress auth

### Story 7.2: Output Formats & Explain Display

As a developer,
I want search results and command output in multiple formats with detailed explain information,
So that I can use the CLI interactively, in scripts, and for debugging relevance issues.

**Acceptance Criteria:**

**Given** any CLI command that produces output
**When** I run it with no format flag
**Then** output is human-readable with clear formatting (FR55 — default)

**Given** any CLI command that produces output
**When** I run it with `--format json`
**Then** output is valid JSON suitable for scripting, pipeline integration, and LLM consumption (FR55)

**Given** any CLI command that produces output
**When** I run it with `--format table`
**Then** output is structured as a human-readable table with aligned columns (FR55)

**Given** a search result with metadata
**When** I inspect the output
**Then** metadata fields display their origin (human-declared vs AI-inferred) and confidence score (FR64)
**And** the distinction is visually clear in human-readable format

**Given** a search with `--explain` flag
**When** results are displayed
**Then** each result shows: composite confidence score, per-axis scores (syntactic, semantic, graph), normalization method per axis, and the caveat "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness"

### Story 7.3: Actionable Error Messages & Discoverable Actions

As a developer,
I want error messages that tell me what went wrong and how to fix it, and helpful guidance at every state,
So that I never feel stuck or confused when using the system.

**Acceptance Criteria:**

**Given** the Memories Server is not running
**When** I run any CLI command
**Then** the error message says: "Cannot connect to Memories Server at {endpoint}. Is the service running? Try: `dotnet run --project Hexalith.Memories.AppHost`" (FR56)

**Given** a search returns zero results in an empty tenant
**When** the response is displayed
**Then** the message says: "No results. This tenant has no memory units yet. Get started: `memories ingest <file>` to add your first document, or configure a DAPR subscription to auto-index events. Run `memories quickstart` for a guided setup." (FR57)

**Given** a tenant-related error (not found, mismatch)
**When** the error is displayed
**Then** it includes error code, human-readable message, and recovery suggestion (e.g., "Run `memories tenant list` to see available tenants")

**Given** any error condition
**When** displayed in `--format json` mode
**Then** the JSON structure is: `{"code": "ERROR_CODE", "message": "...", "suggestion": "..."}`

**Given** any system state (healthy, degraded, empty, error)
**When** the developer interacts with the CLI
**Then** discoverable next actions are suggested contextually (FR57)
**And** the developer is never presented with a dead-end response

### Story 7.4: Quickstart & Documentation

As a developer,
I want a guided quickstart and comprehensive help,
So that I can go from zero to first search result in under 30 minutes.

**Acceptance Criteria:**

**Given** a developer with Docker installed on a clean machine
**When** they follow the README quickstart
**Then** they complete `dotnet add package` through to first successful search result in <30 minutes (NFR31)

**Given** the `memories quickstart` command
**When** executed
**Then** it provides an interactive guided setup: verify prerequisites (Docker, DAPR), boot the stack, create a tenant, create a case, ingest a sample document, run a search (FR57)
**And** each step provides clear instructions and validates success before proceeding

**Given** any CLI command
**When** I run it with `--help`
**Then** it displays: command description, available flags/options, and at least one usage example (NFR30)

**Given** the complete CLI command set
**When** help completeness is verified
**Then** every command has `--help` with at least one example (NFR30 — testable in CI)

### Story 7.5: Search & Access Telemetry

As an operator,
I want structured logging, distributed traces, and custom metrics for the entire system,
So that I can monitor, debug, and audit Memories in production.

**Acceptance Criteria:**

**Given** any operation in the Memories Server
**When** it produces a log entry
**Then** the log is structured JSON with OpenTelemetry correlation IDs from DAPR trace context (NFR27)
**And** log entries include tenant ID, operation type, and relevant identifiers

**Given** a CLI request through ingress to the Memories Server to a backend
**When** the full chain executes
**Then** trace context propagates across all DAPR service invocation hops (NFR28)
**And** the Aspire Dashboard shows the complete distributed trace end-to-end

**Given** the system is running under load
**When** I inspect the Aspire Dashboard
**Then** custom metrics are visible: ingestion throughput per tenant, search latency per axis per tenant, index size per tenant, pipeline queue depth (NFR29)

**Given** a search or access operation occurs
**When** it completes
**Then** a telemetry event is logged per tenant for audit purposes (FR67)
**And** the event includes: timestamp, tenant ID, operation type (search/ingest/traverse), case ID, user identity, query parameters (for search)

---

## Epic 8: Observability & System Health

Operator can verify consistency across all three backends, detect and repair index/graph divergence, export memory units/metadata/graph edges in a portable format, and observe the system via readiness/liveness health checks. This epic delivers the operational confidence layer.

### Story 8.1: Health Checks & Readiness

As an operator,
I want readiness and liveness health checks that verify all backends,
So that I can detect infrastructure issues before they impact users and integrate with orchestrator health probes.

**Acceptance Criteria:**

**Given** the Memories Server is running with Aspire ServiceDefaults
**When** the readiness health check is called
**Then** it verifies connectivity and responsiveness of all three backends: RediSearch, Redis Vector, FalkorDB (FR72)
**And** each backend check reports independently (healthy/unhealthy with details)

**Given** all backends are healthy
**When** the readiness probe returns
**Then** status is `Healthy` with per-backend status details

**Given** one backend is unhealthy (e.g., FalkorDB down)
**When** the readiness probe returns
**Then** status is `Degraded` with the unhealthy backend identified
**And** the response indicates which capabilities are affected (e.g., "Graph traversal unavailable")

**Given** the liveness probe
**When** called
**Then** it checks the Memories Server process health and DAPR sidecar connectivity
**And** does not perform deep backend checks (liveness should be fast)

**Given** a Kubernetes or container orchestrator deployment
**When** health checks are configured
**Then** the readiness endpoint is available at a standard path
**And** the liveness endpoint is available at a standard path
**And** both integrate with Aspire ServiceDefaults health check wiring

### Story 8.2: Consistency Verification & Repair

As an operator,
I want to detect and repair inconsistencies across the three backends,
So that I can ensure data integrity and resolve divergence caused by partial failures.

**Acceptance Criteria:**

**Given** a tenant with indexed memory units
**When** `ConsistencyVerificationWorkflow` is started (FR73)
**Then** it queries all three backends for each memory unit
**And** reports discrepancies: units present in RediSearch but missing from Redis Vector, orphaned graph edges in FalkorDB without corresponding index entries, units in vector store without syntactic index entries

**Given** an operator runs consistency check via CLI
**When** the verification completes
**Then** results show: total units checked, consistent count, inconsistent count, and per-unit discrepancy details
**And** each discrepancy identifies: memory unit ID, which backends have the unit, which backends are missing it

**Given** a per-unit consistency inspection
**When** I run `memories inspect --id <unit-id>`
**Then** the system queries all three backends for that specific unit's state
**And** reports: present/absent per backend, index entry details, vector presence, graph node and edges

**Given** detected inconsistencies
**When** an operator runs consistency repair (FR74)
**Then** the system attempts to restore consistency: re-index missing entries from the authoritative source, remove orphaned entries
**And** each repair action is logged with before/after state
**And** unrepairable inconsistencies (e.g., source data lost) are flagged for manual intervention

**Given** consistency verification on a large tenant
**When** the workflow runs
**Then** it processes units in batches to avoid overwhelming any backend
**And** progress is visible via workflow status

### Story 8.3: Data Export

As a developer,
I want to export all memory units, metadata, and graph edges for a case or tenant,
So that I can back up knowledge, migrate data, or analyze it externally.

**Acceptance Criteria:**

**Given** a case with memory units and graph edges
**When** I export the case (FR71)
**Then** the export produces a portable JSON file containing: all memory units with full metadata, all graph edges (type, confidence, origin, source/target), case metadata (name, members, creation date)

**Given** a tenant with multiple cases
**When** I export the entire tenant (FR71)
**Then** the export includes all cases and their memory units, graph edges, and tenant configuration
**And** the export preserves the case structure and relationships

**Given** an export file
**When** I inspect its format
**Then** it is valid JSON with a documented schema
**And** memory unit IDs, edge IDs, and case IDs are preserved for potential re-import

**Given** a large case or tenant
**When** export is executed
**Then** it streams output progressively (not buffered entirely in memory)
**And** progress is indicated (units exported / total)

**Given** an export in progress
**When** new ingestion occurs simultaneously
**Then** the export captures a consistent snapshot — units added during export are either all included or all excluded (snapshot isolation)

---

## Epic 9: EventStore Integration & Zero-Code Memory

Any event-sourced system publishing to DAPR pub/sub topics gets automatic memory integration — events auto-discovered, dual embeddings generated (raw payload + natural language description), and CausationId/CorrelationId metadata automatically indexed as graph edges without developer mapping code. This is the Phase 1.5 platform innovation — validating the "zero-code" promise.

### Story 9.1: Event Auto-Discovery & DAPR Pub/Sub Subscription

As a developer,
I want events published to DAPR pub/sub topics to be automatically discovered and ingested into memory,
So that I can get memory integration for my event-sourced system without writing mapping code.

**Acceptance Criteria:**

**Given** a DAPR pub/sub topic with CloudEvents-compliant messages
**When** events are published to the topic
**Then** the Memories Server auto-discovers event types from the `type` field of the CloudEvents envelope (FR59)
**And** CloudEvents metadata (source, type, subject, time, id) is extracted and preserved as memory unit metadata (NFR21)

**Given** the system receives a CloudEvents message
**When** the envelope is parsed
**Then** the CloudEvents `id` field is used as the source identifier for deduplication
**And** the CloudEvents `subject` field (aggregate ID) is used for grouping

**Given** the same event is delivered twice (at-least-once DAPR guarantee)
**When** the second delivery is processed
**Then** duplicate detection by event ID prevents duplicate memory units
**And** the duplicate is silently discarded (idempotent)

**Given** events from multiple event-sourced aggregates
**When** they arrive on the same pub/sub topic
**Then** each is processed as an independent memory unit
**And** the aggregate ID (CloudEvents subject) is indexed as metadata for filtering

**Given** indexing freshness requirements
**When** an event is published to DAPR pub/sub
**Then** it is searchable within <5 seconds of publication (NFR6)

### Story 9.2: Dual Embedding & Causal Chain Indexing

As a developer,
I want events to receive dual embeddings and automatic causal chain graph edges,
So that events are searchable both by technical payload and business meaning, with causal relationships preserved automatically.

**Acceptance Criteria:**

**Given** an event with a structured JSON payload
**When** dual embeddings are generated (FR60)
**Then** embedding 1 is generated from the raw JSON payload (technical search)
**And** embedding 2 is generated from a natural language description produced via DAPR Conversation API (LLM) (business meaning search)
**And** both vectors are stored in Redis Vector with distinct keys

**Given** an event with `CausationId` in its metadata
**When** auto-indexing processes the event (FR61)
**Then** a `caused_by` edge (confidence 1.0, origin: explicit) is created from this event's node to the node identified by CausationId
**And** no developer mapping code is required

**Given** an event with `CorrelationId` in its metadata
**When** auto-indexing processes the event (FR61)
**Then** a `correlated_with` edge (confidence 0.8, origin: explicit) is created connecting this event to other events sharing the same CorrelationId
**And** every event in a correlation group does NOT create edges to every other event — only to the correlation root (the event whose ID equals the CorrelationId)

**Given** events arriving out of order (event B arrives before event A, but B's CausationId points to A)
**When** event B is processed
**Then** a gap marker is created for the missing node A
**And** when event A arrives later, the gap is retroactively resolved and the `caused_by` edge is completed

**Given** dual embedding generation fails for one embedding (e.g., LLM unavailable)
**When** the failure is handled
**Then** the raw payload embedding is still indexed (degraded but functional)
**And** the failed natural language embedding is queued for retry

### Story 9.3: Handler Registration & Mismatch Detection

As a developer,
I want to list registered event handlers and detect mismatches,
So that I can verify my event-sourced system is fully integrated and catch configuration drift.

**Acceptance Criteria:**

**Given** the Memories Server has active DAPR pub/sub subscriptions
**When** I list registered event handlers (FR62)
**Then** I see: topic name, event type pattern, subscription status (active/paused), events processed count, last event timestamp

**Given** an event type is published to a topic but no handler is registered
**When** mismatch detection runs (FR62)
**Then** it reports: "Event type 'ClaimSubmittedV2' seen on topic 'claims' but not in registered handlers"
**And** the suggestion includes how to register a handler or update the subscription

**Given** a handler is registered for an event type that hasn't been seen
**When** mismatch detection runs
**Then** it reports: "Handler registered for 'ClaimApprovedV3' but no events received — verify the event source is publishing to the expected topic"

**Given** handler registration mismatches
**When** displayed via CLI
**Then** mismatches are categorized: unhandled events (new types seen), stale handlers (registered but no events), version mismatches (e.g., V2 registered but V3 arriving)
**And** each mismatch includes a severity level and actionable suggestion

---

## Epic 10: MCP Server & LLM Agent Interface

LLM agents can search, ingest, traverse, and query case info via MCP tools with typed parameter schemas, token-budget-aware responses, and structured error handling conforming to MCP protocol specification. The MCP Server runs as a separate DAPR service with its own sidecar, communicating with the Memories Server via DAPR service invocation. This is the Phase 1.5 LLM integration surface.

### Story 10.1: MCP Server & Tool Registration

As a developer,
I want an MCP server that exposes memory capabilities as typed tools for LLM agents,
So that AI assistants can search, ingest, traverse, and query case information programmatically.

**Acceptance Criteria:**

**Given** the MCP Server is deployed as a DAPR service (app-id: `memories-mcp`)
**When** it starts and registers with the Aspire AppHost
**Then** it has its own DAPR sidecar and communicates with Memories Server via DAPR service invocation
**And** the Aspire Dashboard shows the MCP Server as a healthy service

**Given** an LLM agent connects to the MCP Server
**When** it queries available tools
**Then** the following tools are registered: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info` (FR54)
**And** each tool has typed parameter schemas with descriptions suitable for LLM consumption (FR58)

**Given** the `search_memory` tool schema
**When** inspected by an LLM agent
**Then** it includes typed parameters: `query` (string, required), `case` (string, optional), `axes` (enum: syntactic/semantic/graph/hybrid, default: hybrid), `token_budget` (integer, optional), `explain` (boolean, optional)
**And** each parameter has a description explaining its purpose

**Given** the `traverse_relations` tool schema
**When** inspected
**Then** it includes: `from` (string, required — memory unit ID), `depth` (integer, default: 3), `edge_type` (string[], optional), `graph_scope` (object, optional)

**Given** any MCP tool request and response
**When** validated against the MCP protocol specification
**Then** they conform fully: valid tool schemas, typed parameters, structured error responses (NFR20)

**Given** a tool call that results in an error
**When** the MCP Server returns the error
**Then** it maps the Hexalith error code to MCP format with the failed service identifier
**And** the error is structured for LLM interpretation (not raw stack traces)

### Story 10.2: Token-Budget Responses & Authentication

As an LLM agent developer,
I want to constrain response sizes by token budget and ensure authenticated access,
So that memory responses fit within context windows and access is properly secured.

**Acceptance Criteria:**

**Given** a `search_memory` call with `token_budget=2000` (FR23)
**When** results exceed the token budget
**Then** results are truncated by relevance rank — highest-scoring results included first
**And** the response includes `omitted_count` indicating how many results were omitted
**And** the total response stays within the specified token budget

**Given** a `traverse_relations` call with `token_budget` set
**When** the causal chain response exceeds the budget
**Then** the response is truncated while preserving chain structure integrity
**And** truncation occurs at leaf nodes first, preserving the primary causal path

**Given** a `search_memory` call without `token_budget`
**When** results are returned
**Then** all results are returned with no truncation (default behavior)

**Given** an external LLM agent connecting to the MCP Server
**When** the request passes through the ingress layer
**Then** authentication is required at the ingress layer (NFR11)
**And** unauthenticated requests are rejected with an appropriate MCP error response

**Given** the MCP Server receives an authenticated request
**When** it forwards to the Memories Server via DAPR service invocation
**Then** DAPR API token authentication secures the internal communication
**And** the tenant context from the authenticated request is passed through and validated by the Memories Server

**Given** a search result from the MCP Server
**When** a backend is unavailable
**Then** the response includes `degraded: true` and lists which axes were excluded
**And** the LLM agent can caveat its answer accordingly (e.g., "Based on text and semantic search only — graph traversal temporarily unavailable")

---

## Epic 11: CI/CD & Automated Quality Pipeline

Every commit is automatically built, tested, and versioned via GitHub Actions. PRs get build + test checks. Releases publish NuGet packages with semantic versioning from conventional commits. Branch protection on main. This is cross-cutting infrastructure that enables the open-source contributor journey.

### Story 11.1: GitHub Actions Build & Test Pipeline

As a contributor,
I want every PR to be automatically built and tested,
So that I can trust the codebase quality and get fast feedback on my changes.

**Acceptance Criteria:**

**Given** a pull request is opened against `main`
**When** the CI pipeline triggers
**Then** all projects in the solution are built successfully
**And** unit tests are executed and must pass (mock DaprClient — no sidecar required)
**And** contract tests are executed (serialization round-trips for all Contracts.V1 types)
**And** the pipeline reports per-project build status and test results

**Given** the CI pipeline
**When** integration tests are configured
**Then** they run on CI runners with Docker support
**And** integration tests use Aspire `DistributedApplicationTestingBuilder` or DAPR testcontainers
**And** integration tests verify: end-to-end ingestion, search across all axes, tenant isolation

**Given** branch protection on `main`
**When** a PR is submitted
**Then** it requires: CI pipeline pass, at least one approval review
**And** direct pushes to `main` are blocked

**Given** a contributor clones the repository
**When** they run `dotnet build` and `dotnet test` (unit tests only)
**Then** both succeed without Docker installed
**And** integration tests are skipped with a clear message: "Requires Docker — see CONTRIBUTING.md"

### Story 11.2: Semantic Release & NuGet Publishing

As a maintainer,
I want automated semantic versioning from conventional commits and NuGet publishing on release,
So that releases are predictable, traceable, and publishing is zero-friction.

**Acceptance Criteria:**

**Given** commits follow conventional commit conventions (feat:, fix:, breaking change:)
**When** a release is triggered
**Then** semantic-release determines the next version based on commit history
**And** the version follows semver: major (breaking), minor (feat), patch (fix)

**Given** a version tag is pushed (e.g., `v1.2.0`)
**When** the release pipeline triggers
**Then** all 8 published NuGet packages are built and published: Contracts, Client, Client.Rest, Server, Redis, Cli, Mcp, EventStore
**And** the 2 internal Aspire projects (AppHost, ServiceDefaults) are NOT published
**And** all packages share the same version number

**Given** the release completes
**When** I check NuGet
**Then** all published packages are available with correct version, descriptions, and dependencies

**Given** CONTRIBUTING.md
**When** a new contributor reads it
**Then** it covers: conventional commit format, PR process, how to run tests (unit without Docker, integration with Docker), branch naming conventions, code review expectations
**And** it is clear enough for a first-time contributor to submit a valid PR
