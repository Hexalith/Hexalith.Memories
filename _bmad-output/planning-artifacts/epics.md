---
stepsCompleted: ['step-01-validate-prerequisites', 'step-02-design-epics', 'step-03-create-stories', 'step-04-final-validation']
inputDocuments:
  - '_bmad-output/planning-artifacts/prd.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/ux-design-specification.md'
  - '_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md'
changeControlContext:
  approvedProposalGlob: '_bmad-output/planning-artifacts/sprint-change-proposal-*.md'
  note: 'The latest frontmatter inputs are not the full change-control history. Approved sprint-change proposals are discovered through the glob unless a canonical index replaces it.'
---

# Hexalith.Memories - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.Memories, decomposing the requirements from the PRD, UX Design Specification, Architecture requirements, and approved sprint change proposals into implementable stories.

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
- NFR9: Product services retrieve embedding-provider and other application runtime secrets exclusively through the DAPR Secrets API backed by OpenBao; secret values never live in application configuration or ordinary environment variables. Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or direct pod inputs outside the DAPR secret-store boundary. Verified by structural dependency tests, secret scanning, AppHost topology tests, and integration tests [Ongoing]
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

- NFR24: Hybrid fusion uses deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0; single-axis explain still documents axis-specific score semantics [MVP]
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
- Git submodules under `references/`: `references/Hexalith.Commons` (error handling, shared base types) and `references/Hexalith.EventStore` (event types, versioning conventions)
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

- UX-DR1: Define the Evidence Packet as the shared response object across CLI, MCP, and future web UI, including scope, result, sources, evidence, graph, state, omitted details, and recovery actions.
- UX-DR2: Every evidence packet must identify tenant and case scope, top source references, evidence strength, freshness status, retrieval axes used, explain summary, graph relationship summary when relevant, and the next recovery action when evidence is weak, incomplete, absent, or out of scope.
- UX-DR3: If details are omitted for compactness or token budget, the response must say what was omitted and provide deterministic expansion handles or equivalent expansion guidance.
- UX-DR4: Search must be scope-first; tenant and case context must be visible before query submission, preserved through result inspection, and treated as trust-blocking when ambiguous, unavailable, unauthorized, or inconsistent.
- UX-DR5: Implement a Trust Strip for Evidence Packet and briefing surfaces with tenant, case, confidence state, freshness state, source count, evidence health, and optional token-budget indicator.
- UX-DR6: Implement a Scope Header for search, ingestion, briefing, tenant verification, operator workflows, and compact/mobile contexts so tenant, case, permissions, and isolation state remain visible.
- UX-DR7: Search responses must begin the full trust loop in one query by including source lookup, evidence strength scoring, explain breakdown, relevant graph context, and a safe next action.
- UX-DR8: Deeper inspection must use progressive disclosure: detailed source snippets, scoring math, graph paths, token-budget behavior, backend diagnostics, and candidate details are available but secondary to the evidence summary.
- UX-DR9: Empty, weak, stale, degraded, unauthorized, and compressed states must be first-class states with a clear state title, short explanation, diagnostic clue, and recovery action.
- UX-DR10: No-result states must distinguish no match, not ingested yet, wrong case, inaccessible tenant/case, stale memory, degraded backend, graph gap, and insufficient evidence.
- UX-DR11: Implement a Recovery Action Panel or Recovery Footer for incomplete Evidence Packets, no-result states, operator warnings, and MCP structured errors, with one safest next action and optional secondary actions.
- UX-DR12: Conflicting evidence must be exposed rather than smoothed away, including competing sources, stale versus fresh memory, high lexical match with weak graph support, strong graph context with weak source confidence, and backend disagreement.
- UX-DR13: CLI UX must be keyboard-driven and developer/operator focused, with compact explain output, actionable diagnostics, tenant/case visibility, and scriptable output formats.
- UX-DR14: MCP UX must be schema-first, typed, bounded, source-attributed, confidence-aware, token-budget-aware, and structured so agents can act without parsing prose-only explanations.
- UX-DR15: All Memories web UI and UX implementation must use only Hexalith.FrontComposer and Microsoft Fluent UI Blazor V5 components for controls, navigation, forms, grids, dialogs, drawers, tabs, menus, status feedback, layout, focus behavior, and command surfaces. Raw HTML/CSS/JavaScript or third-party UI components are allowed only as explicitly justified gaps when no FrontComposer or Fluent UI V5 component/token exists, and those exceptions must be tracked by conformance tests.
- UX-DR16: Future web surfaces must provide an Evidence Cockpit lens centered on scoped search, Evidence Packets, source inspection, retrieval axes, graph context, and recovery actions.
- UX-DR17: Implement a Retrieval Axis Breakdown component or response section for explain mode and benchmark inspection, showing raw score, normalized score, fusion contribution, ranking reason, omitted/degraded axis state, and detail expansion.
- UX-DR18: Implement a Source Citation Stack for cited sources, including source type, origin identifier, freshness, snippet or summary, confidence/metadata origin, and keyboard-openable preview behavior where UI exists.
- UX-DR19: Implement a Graph Path Summary for causal and why-oriented workflows, showing relationship path, edge type, confidence, gap markers, and chronological ordering.
- UX-DR20: Implement an Agent Packet Inspector pattern for MCP debugging, including request summary, response schema, token budget, omitted fields, expansion handles, structured errors, copy controls, and accessible schema/JSON views.
- UX-DR21: Implement Case Activity Trail patterns for Marcus-style continuity, showing ingestion events, searches, membership changes, annotations, health states, source links, and briefing context.
- UX-DR22: Implement Ingestion Lifecycle Tracker patterns for pending, queued, extracting, embedding, indexing, indexed, failed, retried, and re-ingested states.
- UX-DR23: Implement Operator Health Matrix patterns for tenant verification, backend health, isolation status, ingestion health, consistency repair, degradation, and alert states.
- UX-DR24: Implement Benchmark Result Comparator patterns for three-axis validation, hybrid-vs-single-axis comparison, NDCG@10 evidence, and thesis review.
- UX-DR25: Use a consistent evidence state grammar across CLI, MCP, and web UI: confidence (`supported`, `partial`, `disputed`, `insufficient`), freshness (`current`, `aging`, `stale`, `unknown`), evidence health (`complete`, `degraded`, `missing source`, `schema mismatch`), and scope (`verified`, `inferred`, `cross-case`, `unauthorized`, `out-of-scope`).
- UX-DR26: Feedback patterns must answer what happened, what it affects, how serious it is, and what to do next; trust-critical feedback appears close to the affected Evidence Packet or object rather than only in global notifications.
- UX-DR27: Form patterns must be contract-aware and validation-first, with tenant and case scope near the top and actionable validation for tenant, case, source, permissions, and dangerous scope changes.
- UX-DR28: Search and filtering patterns must expose active filters for axis, source type, freshness, confidence, time range, metadata, graph depth, and evidence state, and must show when filters narrow scope, broaden scope, exclude axes, or affect confidence.
- UX-DR29: Navigation patterns must preserve tenant/case/search context and provide clear return paths from Evidence Packets to sources, graph paths, activity items, and agent packets.
- UX-DR30: Modal and overlay patterns must use inspection drawers or panels for source, graph, reasoning, MCP payload, export, and repair flows; destructive or scope-sensitive confirmations must name the tenant, case, object, and consequence.
- UX-DR31: Command palette patterns should expose search, ingest, inspect source, verify tenant, open graph, retry ingestion, export packet, and inspect MCP payload actions for advanced users.
- UX-DR32: Data grid patterns should support memory units, sources, ingestion jobs, case activity, tenant checks, backend health, and benchmark results with sorting, filtering, status badges, row actions, and keyboard navigation.
- UX-DR33: Responsive behavior must preserve trust fundamentals on every viewport; scope, confidence, freshness, source count, evidence health, and recovery remain reachable on mobile, tablet, desktop, and wide desktop.
- UX-DR34: Responsive breakpoint coverage must include mobile 320-767px, tablet 768-1023px, desktop 1024px+, and wide desktop 1440px+, with test viewports at minimum 360px, 768px, 1024px, and 1440px.
- UX-DR35: Accessibility must target WCAG 2.2 AA for web surfaces and preserve keyboard access, visible focus, labels, contrast, screen-reader semantics, live-region behavior for meaningful async transitions, reduced-motion support, and forced-colors/high-contrast support.
- UX-DR36: Trust states must not rely on color alone; status indicators require text labels, accessible names, and consistent state grammar.
- UX-DR37: Focus management must move into drawers, dialogs, source previews, graph detail panels, MCP inspectors, and confirmations, then return focus to the invoking control when closed.
- UX-DR38: Hover-only interactions are forbidden for trust-critical source preview, graph detail, recovery action, tooltip, and command behavior; all must be accessible by keyboard and touch.
- UX-DR39: Automated and human UX validation must cover color contrast, accessible names, form labels, ARIA validity, heading order, focusable controls, keyboard-only navigation, focus order, no-color-only state comprehension, reduced motion, and high-contrast behavior.
- UX-DR40: Accessible text, tooltips, labels, announcements, copied text, and diagnostics must not expose secrets, raw payloads, bearer tokens, tenant-sensitive diagnostics, or restricted source details.

### UX Design Requirements Coverage Map

- UX-DR1: Story 2.7 and future Story 17.1 — Evidence Packet contract and visual composition.
- UX-DR2: Stories 2.6 and 2.7 — evidence packet trust fields and explain/confidence semantics.
- UX-DR3: Stories 2.7 and 10.2 — omitted detail naming and deterministic expansion handles.
- UX-DR4: Stories 0.3, 5.4, 7.3, and 17.1 — scope-first validation and visible tenant/case context.
- UX-DR5: Story 17.1 — Trust Strip web composition.
- UX-DR6: Stories 0.3 and 17.1 — shared scope guard and Scope Header web composition.
- UX-DR7: Stories 2.6, 2.7, and 7.2 — one-query trust loop with explain and source evidence.
- UX-DR8: Stories 2.7, 10.2, 17.1, and 17.2 — progressive disclosure and expansion semantics.
- UX-DR9: Stories 2.7, 5.6, 7.3, 10.2, and 17.2 — empty, weak, stale, degraded, unauthorized, and compressed states.
- UX-DR10: Stories 2.1, 7.3, and 17.2 — no-result state distinctions and recovery.
- UX-DR11: Stories 2.7, 7.3, 10.2, and 17.2 — Recovery Action Panel/Footer behavior.
- UX-DR12: Stories 2.6, 2.7, 8.2, and 17.2 — conflicting evidence and backend discrepancy visibility.
- UX-DR13: Stories 7.1, 7.2, 7.3, and 7.4 — CLI interaction, formatting, errors, and quickstart.
- UX-DR14: Stories 10.1 and 10.2 — MCP schema-first bounded responses and structured errors.
- UX-DR15: Stories 17.1, 17.3, 17.5, and 17.6 — FrontComposer and Fluent UI Blazor V5 web implementation foundation and conformance hardening.
- UX-DR16: Story 17.1 — Evidence Cockpit future web surface.
- UX-DR17: Stories 2.6, 2.8, and 17.1 — Retrieval Axis Breakdown for explain and benchmark inspection.
- UX-DR18: Stories 2.6, 2.7, 7.2, and 17.1 — Source Citation Stack semantics and presentation.
- UX-DR19: Stories 4.1, 4.3, and 17.1 — Graph Path Summary and gap visibility.
- UX-DR20: Stories 10.1, 10.2, and 17.4 — Agent Packet Inspector and MCP response inspection.
- UX-DR21: Stories 3.2 and 17.4 — Case Activity Trail.
- UX-DR22: Stories 6.3, 6.4, and 17.4 — Ingestion Lifecycle Tracker.
- UX-DR23: Stories 5.3, 8.1, 8.2, and 17.4 — Operator Health Matrix.
- UX-DR24: Stories 2.8 and 17.4 — Benchmark Result Comparator.
- UX-DR25: Stories 2.7, 7.2, 10.2, and 17.2 — consistent evidence state grammar.
- UX-DR26: Stories 7.3, 8.1, and 17.2 — feedback that explains cause, impact, severity, and next action.
- UX-DR27: Stories 5.5, 13.4, and 17.3 — contract-aware forms and validation-first configuration UX.
- UX-DR28: Stories 2.5, 3.4, 4.2, and 17.3 — search/filtering behavior and scope/axis impact.
- UX-DR29: Story 17.3 — context-preserving web navigation and return paths.
- UX-DR30: Stories 3.5, 5.5, and 17.3 — confirmation and inspection overlay behavior.
- UX-DR31: Stories 7.1 and 17.3 — command access through CLI and future command palette.
- UX-DR32: Stories 3.2, 5.5, 8.2, and 17.3 — data grid behavior for operational records.
- UX-DR33: Story 17.5 — responsive trust fundamentals.
- UX-DR34: Story 17.5 — breakpoint and viewport validation.
- UX-DR35: Story 17.5 — WCAG 2.2 AA accessibility validation.
- UX-DR36: Stories 2.7, 7.2, 17.2, and 17.5 — status labels beyond color.
- UX-DR37: Story 17.5 — overlay and dialog focus management.
- UX-DR38: Story 17.5 — no hover-only trust-critical interactions.
- UX-DR39: Story 17.5 — automated and human accessibility/responsive validation.
- UX-DR40: Stories 7.5, 10.1, 13.2, 13.3, 14.3, and 17.5 — privacy-safe accessible text and diagnostics.

### FR Coverage Map

- FR1: Epic 1 — Ingest from local files
- FR2: Epic 6 — Ingest from URLs
- FR3: Epic 6 — Batch-ingest from directory
- FR4: Epic 1 — Text extraction (Kreuzberg)
- FR5: Epic 1 — Generate embeddings
- FR6: Epic 1 — Memory unit fully searchable after ingestion; reinforced by Epic 23 for scalable chunking and batch embedding
- FR7: Epic 1 — Metadata with origin tracking
- FR8: Epic 6 — Per-tenant ingestion load management
- FR9: Epic 6 — Auto-retry with configurable limits
- FR10: Epic 6 — Ingestion status per case
- FR11: Epic 6 — Failed unit visibility
- FR12: Epic 6 — Re-ingestion of failed content; reinforced by Epic 23 for non-URL re-ingestion correctness
- FR13: Epic 1 — Partial backend write failure recovery (IngestionWorkflow saga/compensation); reinforced by Epic 21 for ratified consistency and migration safety
- FR14: Epic 2 — Syntactic search
- FR15: Epic 2 — Semantic search
- FR16: Epic 2 — Graph search
- FR17: Epic 2 — Hybrid fusion search
- FR18: Epic 2 — Axis selection control
- FR19: Epic 2 — Per-axis score breakdown (explain)
- FR20: Epic 3 — Filter search by case
- FR21: Epic 3 — Filter search by metadata
- FR22: Epic 2 — Pagination (search concern); reinforced by Epic 22 for semantic, graph-scoped, and hybrid pagination correctness
- FR23: Epic 10 — Token budget (MCP), including deterministic omitted-detail expansion handles
- FR24: Epic 2 — Origin identifier in results
- FR25: Epic 2 — Benchmark comparisons
- FR26: Epic 0 + Epic 3 — Minimal case bootstrap, then full case management
- FR27: Epic 3 — Delete case
- FR28: Epic 3 — Add case members
- FR29: Epic 3 — Remove case members
- FR30: Epic 3 — List cases
- FR31: Epic 3 — Case status
- FR32: Epic 3 — Single-case ownership
- FR33: Epic 3 — Case-scoped graph edges
- FR34: Epic 3 — Cross-case tenant search; reinforced by Epic 22 for fusion case attribution
- FR35: Epic 3 — Delete memory unit
- FR36: Epic 3 — Case activity
- FR37: Epic 3 — Annotations/corrections
- FR38: Epic 0 + Epic 5 — Tenant creation and isolated infrastructure provisioning; reinforced by Epic 24 for physical isolation strategy
- FR39: Epic 5 — Delete tenant; reinforced by Epic 21 for deletion completeness
- FR40: Epic 5 — Verify tenant isolation; reinforced by Epic 24 for verifier scaling
- FR41: Epic 5 — List tenants
- FR42: Epic 5 — Update tenant config
- FR43: Epic 5 — Prevent inconsistent config changes
- FR44: Epic 0 + Epic 5 — Tenant context validation and enforcement; reinforced by Epic 20 for authorization and Epic 24 for physical isolation
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
- FR63: Epic 2 — Composite confidence scores and Evidence Packet contract mapping
- FR64: Epic 7 — Metadata origin tracking display
- FR65: Epic 1 — `ingested_by` field
- FR66: Epic 5 — Partial results on backend failure
- FR67: Epic 7 — Search/access telemetry; reinforced by Epic 20 for audit emission. A41 access-telemetry retention remains governed by `20.5-A41-ACCESS-TELEMETRY-RETENTION`.
- FR68: Epic 1 — Configure Google embedding provider for MVP with an extensible provider/model/dimensions/rate-limit shape. OpenAI, Mistral, Ollama, and custom runtime providers are post-MVP provider expansion work unless explicitly pulled forward by sprint change.
- FR69: Epic 5 — Per-tenant rate limits
- FR70: Epic 5 — Track embedding model per unit
- FR71: Epic 26 — Portable export reinforced through backup/restore and operational readiness; broader application-facing export remains Phase 2 unless explicitly pulled forward
- FR72: Epic 8 — Health checks
- FR73: Epic 8 — Consistency check
- FR74: Epic 8 — Consistency repair

## Selected Implementation Scope

**Selected scope as of 2026-05-17:** planning correction for implementation readiness. No product requirement reset is approved. The clean executable foundation path is:

1. Story 0.0: Project Scaffolding & Single-Command Boot (historical alias: Story 1.1)
2. Story 0.1: Tenant Provisioning Minimum Viable Workflow
3. Story 0.2: Minimal Case Bootstrap
4. Story 0.3: Tenant and Case Validation Guard
5. Story 0.4: Minimum Build/Test CI Preflight
6. Epic 1 data-writing ingestion/search stories

Minimum build/test CI is part of the executable foundation path and is tracked as Story 0.4. Semantic release, NuGet publishing, branch protection hardening, release operations, and extended CI quality hardening remain in the Engineering/Operational Readiness track.

## Implementation Readiness Boundary

**Active MVP scope:** Epic 0 through Epic 8 are the only epics in active MVP implementation readiness. Any work outside this set must be explicitly sprint-selected and must not be pulled into MVP completion accounting by accident.

**Machine-readable accounting:** `_bmad-output/implementation-artifacts/sprint-status.yaml` owns readiness metadata under `readiness_accounting`. Readiness reports and story tooling must use that metadata rather than inferring MVP readiness from story status, numeric ordering, or FR coverage alone.

- Epic 0: Foundation path, including scaffold, tenant provisioning, minimal case bootstrap, and validation guard
- Epic 1-8: MVP thesis, tenant isolation, CLI developer experience, and operations gates

**Phase 1.5 fast-follow:** Epic 9 and Epic 10. Not active MVP readiness; pulled forward only by explicit sprint change.

**Engineering/Operational Readiness Track:** Epics 11-16 and Epic 18. They remain in this file for lifecycle continuity, require explicit sprint selection before implementation, and are judged by delivery safety, release integrity, maintainer/operator outcomes, and validation evidence. They must never be counted toward MVP product readiness. Epic 18 holds the 2026-05-27 Parties downstream-consumer integration asks (MEM-1 … MEM-7); only Story 18.4 carries semantic-release sensitivity and must land before the Parties project pins the stabilised SDK.

**Future web UI** (Epic 17, FrontComposer, Fluent UI implementation) remains out of MVP unless a later approved sprint change pulls it forward.

**Post-MVP audit remediation:** Epics 20-26 reinforce already-approved requirements and production-readiness gaps. They do not reopen MVP thesis validation or expand active MVP readiness unless a story is explicitly sprint-selected as a blocker for production exposure.

**Post-MVP operational hardening:** Epics 27-31 are Operational Readiness track. Epic 27 hardens the access-telemetry lifecycle; Epic 28 adopts the owner-approved EventStore runtime identity; Epic 29 owns Aspire-local OpenBao secret topology; Epic 30 owns the container release pipeline; Epic 31 owns the deployed OpenBao platform and runtime secret-store migration. None is counted toward MVP product readiness, each requires explicit sprint selection, and each is judged by the Engineering/Operational Readiness Track acceptance rules below.

**FR71 scope interpretation:** Epic 26 covers the operational backup/restore and disaster-recovery slice of FR71. It does not pull the broader application-facing portable export feature into active MVP scope; full export remains Phase 2 unless explicitly sprint-selected.

### Pre-Implementation CI Preflight Gate (2026-05-19)

Before any Epic 1.x product-capability story writes data, Story 0.4 must be complete. Story 0.4 is the minimum build/test CI preflight: pull-request build, restore, `dotnet build` with `TreatWarningsAsErrors=true`, and Docker-free Tier-1 unit/contract test execution.

If Story 0.4 is not complete, do not start Story 1.2 onward. Either complete Story 0.4 first, or open a sprint change to defer the preflight requirement explicitly. Story 11.1 extends this foundation into the full GitHub Actions quality pipeline; it is no longer the source of the Epic 1 prerequisite.

### Story Key Policy

New story keys must use numeric `Epic.Story` format. Alphabetic suffixes are allowed only as historical aliases during migration and must not be introduced for new work unless story tooling explicitly supports them and a sprint change approves the exception.

When completed history or external traceability prevents renumbering, execution order must be declared in `_bmad-output/implementation-artifacts/sprint-status.yaml` under `story_execution_order`. Story tooling must honor `story_execution_order` before numeric key order. Do not create a story file for a story whose declared prerequisite in that execution-order list is incomplete unless a sprint change explicitly approves the exception.

**Story-key alias & status map** (reconciles `epics.md` keys with `_bmad-output/implementation-artifacts/*` file keys and reserved/optional slots):

| Current key | Historical alias / status | Notes |
|---|---|---|
| Story 0.0 | was Story 1.1 | Project scaffolding & single-command boot; renumbered into the Epic 0 foundation path. Completed story file and sprint-status may retain the `1-1-project-scaffolding-and-single-command-boot` key for traceability. |
| Epic 1 (first story 1.2) | — | Epic 1 has no Story 1.1; it begins at Story 1.2 because 1.1 became Story 0.0. |
| Story 2.7 | was Story 2.6A | Evidence Packet Contract Mapping; implementation artifacts may keep `2.6A` as an alias. |
| Story 8.3 | reserved-non-mvp | Phase 2 data export (FR71). The Epic 8 MVP sequence intentionally continues with 8.4 and 8.5. Story-status / file-scope tooling must treat `8.3` as `reserved-non-mvp`, not a missing MVP story. |
| Stories 12.7 / 12.8 | optional / conditional | S11-FB / S11-FC follow-ups; created only if their re-open trigger actually fires (do not scaffold speculatively). |

### Non-Story Implementation Artifact Policy

`development_status` is the registry for epics, stories defined in this
document, and retrospectives. A file does not become a story merely because its
name begins with an `Epic-Story`-shaped numeric prefix.

A one-shot artifact is permitted only for a bounded, zero-blast-radius
correction completed and reviewed in one workflow execution. Its canonical
trace must declare `route: 'one-shot'` and `status: 'done'` in valid frontmatter.
It remains outside `development_status`, does not lift, hold, reopen, or close an
epic, and is listed separately from registered stories when a retrospective
uses it as supporting evidence.

This convention applies prospectively from 2026-07-16 and expressly ratifies
the historical 19.5 trace that triggered it. Older `route: 'one-shot'` traces
may retain their historical metadata, but they do not establish precedent and
do not override the lifecycle of any registered story they support.

If the work needs a draft, in-progress, review, dependency, or multi-session
lifecycle, route it through a normal plan/code/review spec whose frontmatter
self-tracks that lifecycle. If the work belongs to an epic, changes epic scope
or acceptance criteria, or affects epic completion, register it as a story in
this document and `development_status` before implementation continues.

Generated story-automator, orchestration, review, and test-output files are
supporting evidence. They inherit the lifecycle of the canonical registered
story, normal spec, or one-shot trace that references them and do not receive
individual sprint-status rows.

## Epic List

### Phase: MVP — Foundation Gate

### Epic 0: Tenant and Case Safety Foundation
Before any ingestion, indexing, search, or graph story writes data, the system must have a buildable scaffold, an active tenant with physically isolated backend infrastructure, and an active case within that tenant. `TenantProvisioningWorkflow` is the sole owner of tenant index/database creation. Ingestion and indexing require an active tenant and case and fail clearly before backend writes when either is missing.
**Sequencing:** Epic 0 is the complete foundation path: Story 0.0 scaffold first, then tenant provisioning, minimal case bootstrap, and validation guard. Epic 1 starts only after Epic 0 is complete.
**FRs covered:** FR26, FR38, FR44
**NFRs covered:** NFR8

### Phase: MVP — Gate 1 (Three-Axis Validation)

### Epic 1: First Tenant-Scoped Memory Ingestion and Search
Developer can use the completed Epic 0 foundation to ingest local content and see it persisted and searchable across text, vector, and graph axes with tenant-safe provenance and typed graph edges. Implementation delivered by this epic includes `IngestionWorkflow` with saga/compensation, Contracts V1 ingestion/search contracts, Redis (RediSearch + Vector), FalkorDB graph indexing activities, Kreuzberg (NuGet, in-process), embedding provider configuration, provenance, and causal metadata indexing.
**FRs covered:** FR1, FR4, FR5, FR6, FR7, FR13, FR46, FR65, FR68

### Epic 2: Three-Axis Search, Fusion & Benchmark Validation
Developer can search memory units across syntactic, semantic, and graph axes independently and as a fused hybrid query, with explainable per-axis score breakdowns, Evidence Packet contract mapping, and paginated results. Benchmark suite validates the three-axis thesis with automated NDCG@10 scoring against a synthetic dataset with known ground truth.
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
Developer can accomplish MVP thesis-validation tasks via a CLI tool with actionable error messages, multiple output formats, scope visibility, explain output, and a README quickstart path that proves the under-30-minute first-search gate. MVP CLI essentials are `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, benchmark support, and README quickstart validation. Full CLI polish (`explore`, `status`, `handlers`, `quickstart`, batch directory ingestion, richer diagnostics) remains Phase 1.5 unless explicitly pulled forward.
**FRs covered:** FR53, FR55, FR56, FR57, FR64, FR67

### Phase: MVP — Operations

### Epic 8: Observability & System Health
Operator can verify consistency across all three backends, detect and repair index/graph divergence, and observe the system via readiness/liveness health checks, structured logging, distributed traces, and custom metrics.
**FRs covered:** FR72, FR73, FR74

FR coverage is 100% traceable. MVP implementation scope is not 100% of FR1-FR74 because FR71 portable export is explicitly deferred to Phase 2 unless a later approved sprint change pulls it forward.

### Phase 1.5 (Fast-Follow — within 4 weeks of thesis validation)

### Epic 9: EventStore Integration & Zero-Code Memory
Any event-sourced system publishing to DAPR pub/sub topics gets automatic memory integration — events auto-discovered, dual embeddings generated (raw payload + natural language description), and CausationId/CorrelationId metadata automatically indexed as graph edges without developer mapping code. Developers can list registered handlers and detect mismatches.
**FRs covered:** FR59, FR60, FR61, FR62

### Epic 10: MCP Server & LLM Agent Interface
LLM agents can search, ingest, traverse, and query case info via MCP tools with typed parameter schemas, token-budget-aware responses, and structured error handling conforming to MCP protocol specification.
**FRs covered:** FR23, FR54, FR58

### Engineering/Operational Readiness Track

Epics in this track are judged by delivery safety, release integrity, maintainer/operator outcomes, and validation evidence. They are not product capability epics, but they protect implementation and release quality.

Operational-readiness stories are accepted only when they produce maintainer/operator decisions and concrete evidence such as CI check names, release run results, package inventory proof, deferred-ID resolution records, runbook updates, or explicit accepted-risk entries. Before implementing Epics 14 or 15, verify every referenced deferred ID, retrospective item, review finding, and sprint-change proposal path still exists. If a reference is stale, update the story before implementation begins.

Acceptance criteria that allow "implemented, documented, accepted, or carried forward" are allowed only in Engineering/Operational Readiness stories. MVP product capability stories must deliver working behavior or explicit testable validation, not documentation-only completion, unless a separate sprint change approves a deferral.

Operational checkpoint stories may remain umbrella tracking stories, but each checkpoint must be implemented, reviewed, and evidenced as a separately verifiable slice. A checkpoint cannot be marked complete only because the umbrella story is complete.

When a checkpoint-heavy story is authored or registered — at any status, including `backlog` — the story file must either split checkpoints into separately tracked child story files or include a checklist evidence table with owner, validation command or artifact, review status, and completion date for each checkpoint. A story created by an approved sprint change is bound at the moment that change registers it, not at the moment it is later selected; a split must not reproduce the shape it was executed to cure (amended 2026-07-28 by approved Sprint Change Proposal 2026-07-28, executing Epic 0 action item 3 after the DW 27.3-CR16 recurrence). This guard is shape-based, not story-specific: it binds any story whose acceptance criteria enumerate more than five independently verifiable gates, however those gates are grouped into criteria. A single table row covering multiple gates does not satisfy it — one row per gate is required, because a shared review state and completion date cannot record partial completion. Stories 21.9, 26.5, and 27.3 are the known instances to date.

### Epic 11: CI/CD & Automated Quality Pipeline
Minimum build/test CI is an enabling prerequisite for any greenfield or restarted implementation sequence. Semantic release, NuGet publishing, branch protection, and release-hardening behavior remain in this operational-readiness epic.
**Driven by:** Architecture Decision D17

### Phase: Post-MVP — Operations & First Release

### Epic 12: First Release & Operations Foundation
Cut the first real release of Hexalith.Memories to nuget.org, apply branch protection on `main`, operationalize the Epic 11 retrospective action items, and prove the release path end-to-end before any further feature investment. Closes the gap between "CI infrastructure built" and "release path proven against a real publish event."
**Driven by:** Epic 11 retrospective + Sprint Change Proposal 2026-04-26 (Hybrid path = Operations Epic 12 first, then Phase 2 decision)

### Epic 13: Embedding Provider Pluggability + Vector Migration
Operator can migrate the embedding pipeline from Google to a self-hosted Ollama gateway protected by Keycloak OIDC, while preserving Google as an opt-in provider and providing a Path A vector migration tool.
**Driven by:** Sprint Change Proposal 2026-04-29

### Epic 14: Deferred Work Hardening and Operational Readiness
Maintainers and operators can close high-value deferred review findings across CI correctness, release integrity, OIDC/embedding security, migration reliability, and deferred-work governance.
**Lifecycle label:** Operational Readiness / Release Hardening

### Epic 15: Carry-Forward Operational Risk Closure
Maintainers and operators can convert remaining carry-forward risks from Epic 14 into planned implementation, acceptance, or refreshed deferral decisions.
**Lifecycle label:** Operational Readiness / Release Hardening

### Epic 16: Projection Registry Cross-Check Hardening
Maintainers and operators can close the Story 9.3 projection-registry gap by comparing EventStore routing declarations with the projection bindings that tenant application code exposes at runtime.
**Lifecycle label:** Operational Readiness / EventStore Integration Hardening

### Epic 17: Future Web UX Composition & Accessibility
Future web users can inspect evidence, scope, sources, graph context, case activity, operator health, benchmark results, and MCP packets through FrontComposer/Fluent UI compositions with responsive and accessible behavior. This is deferred future web UI work and is not part of MVP unless a later sprint change explicitly pulls web UI implementation forward.
**UX-DRs covered:** UX-DR5, UX-DR6, UX-DR15, UX-DR16, UX-DR20, UX-DR21, UX-DR22, UX-DR23, UX-DR24, UX-DR26, UX-DR27, UX-DR29, UX-DR30, UX-DR31, UX-DR32, UX-DR33, UX-DR34, UX-DR35, UX-DR36, UX-DR37, UX-DR38, UX-DR39, UX-DR40

### Epic 18: Downstream Consumer Integration Contract Hardening
Maintainers can give the first external consumer of Hexalith.Memories (the `Hexalith.Parties` project) a stable, documented, and race-safe integration contract, closing seven cross-repository asks (MEM-1 … MEM-7) raised during the Parties `bmad-correct-course` intake on 2026-05-27. Three asks (MEM-1, MEM-4, MEM-7) were partly satisfied by the current `main`; the stories close only the verified residual gap.
**Lifecycle label:** Operational Readiness / Downstream Consumer Integration Hardening
**Driven by:** Sprint Change Proposal 2026-05-27 (Parties consumer integration intake)
**FRs reinforced:** FR6, FR24, FR59, FR60, FR61, FR62

### Epic 19: Deferred Register Backlog Home and Residual Hardening
Maintainers can convert active `open` and `carried-forward` deferred-work entries into explicit backlog homes, accepted-debt decisions, or trigger-bound future work without reopening completed epics.
**Lifecycle label:** Operational Readiness / Deferred Register Governance
**Driven by:** Sprint Change Proposal 2026-06-30 (Deferred Work Backlog Homes)

### Phase: Post-MVP — Audit Remediation

### Epic 20: API Security & Tenant Authorization
Authenticated, tenant-authorized server boundary; trustworthy audit identity; MCP production-key hardening; inbound rate limiting; complete audit emission.
**Lifecycle label:** Operational Readiness / Security Hardening
**Driven by:** Sprint Change Proposal 2026-07-04 (Architecture Audit Remediation) — closes A1, A2, A6, A20, A31, and A41's request-limiting/audit-emission slices; the retention residual remains carried forward
**FRs reinforced:** FR44, FR67

### Epic 21: Data Integrity, Consistency & Migration Safety
Ratified consistency model, non-diverging multi-backend writes, disjoint key namespaces, complete deletion, safe blue/green embedding migration, and migration test coverage.
**Lifecycle label:** Operational Readiness / Data Integrity
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A3, A4, A5, A16, A17, A22, A27, A28, A44, A47
**FRs reinforced:** FR13, FR39

### Epic 22: RAG Retrieval Quality & Correctness
Correct pagination, bounded graph traversal, calibrated fusion with case attribution, case-scoped path integrity, post-filter recall, and NL-axis/reranker completion.
**Lifecycle label:** Product Capability / Retrieval Quality
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A8, A9, A29, A30, A48, A49, A50
**FRs reinforced:** FR22, FR34

### Epic 23: Ingestion Pipeline Scalability & Resilience
Chunking + batch embedding, claim-check payloads, Retry-After 429 handling, working non-URL re-ingestion, single-round-trip admission, efficient directory batches, and a provider strategy.
**Lifecycle label:** Product Capability / Ingestion Scalability
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A11, A12, A13, A14, A15, A33, A34, A35, A51
**FRs reinforced:** FR6, FR12

### Epic 24: Observability & Performance Hardening
End-to-end workflow tracing, read-path caching, physical tenant isolation with a scalable verifier, unified metric naming with a committed dashboard, and hot-path write-amplification cleanup.
**Lifecycle label:** Operational Readiness / Observability & Performance
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A19, A26, A36, A46
**FRs reinforced:** FR38, FR40, FR44

### Epic 25: Architecture Factorization & Code Health
Thin composition root, centralized error/telemetry handling, shared route table, contract/persistence separation, consolidated CLI/MCP, UX-conformant evidence cockpit, and clean project topology.
**Lifecycle label:** Operational Readiness / Code Health
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A7, A21, A32, A37, A38, A39, A40, A43, A45

### Epic 26: Test, Deployment & Operational Readiness
Production deployment artifacts, backup/restore, integration-stub closure, coverage gating, and the missing operational runbook set.
**Lifecycle label:** Operational Readiness / Deploy & Test
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A23, A24, A25, A42
**FRs covered:** FR71
**FR71 scope note:** This epic covers backup/restore and disaster-recovery readiness. Broader application-facing portable export remains Phase 2 unless explicitly sprint-selected.

### Epic 27: Access Telemetry Lifecycle Hardening
Operators can configure and verify a bounded lifecycle for access telemetry through one explicitly owned write-only sink/store without weakening audit emission, tenant/privacy boundaries, or the PRD compliance boundary.
**Lifecycle label:** Operational Readiness / Security and Observability Hardening
**Driven by:** Sprint Change Proposal 2026-07-16 (Access-Telemetry Retention Implementation)
**FRs reinforced:** FR67

### Epic 28: Owner-Approved EventStore Runtime Adoption
Memories source and package modes converge on the exact EventStore runtime identity authorized by EventStore Story 1.20 while preserving the existing zero-code DAPR ingestion contract.
**Lifecycle label:** Operational Readiness / EventStore Dependency Adoption
**Driven by:** Approved direct course correction 2026-07-17
**Activation gate:** Externally gated on EventStore Story 1.20 recording `final_decision: available`, `authorize_consumer_migration: true`, a 40-hex `tested_runtime_sha`, named owner approval, and the approved package version and SHA-256 inventory.

### Epic 29: OpenBao-First Dapr Secret Management
Aspire-hosted services resolve application secrets exclusively through Dapr secret-store components backed by OpenBao. Kubernetes Secrets remain permitted only for unavoidable bootstrap credentials or direct pod inputs that Dapr cannot inject.
**Lifecycle label:** Operational Readiness / Secret Management Hardening
**Driven by:** Sprint Change Proposal 2026-07-19 — OpenBao-First Aspire Secret Management
**NFRs reinforced:** NFR9
**Scope boundary:** Epic 29 owns the Aspire/AppHost-local secret topology and provider-neutral composition. The deployed-cluster OpenBao platform and the runtime Dapr `secretstore` migration are Epic 31.

### Epic 30: CI/CD Pipeline Ownership and Alignment
Memories adopts the EventStore-shaped Hexalith CI/CD model: reusable Hexalith.Builds workflows own standard build, test, and release mechanics, while Memories retains only named module-specific verification and recovery lanes. Tenants supplies compatible coverage and consumer-validation patterns where those shared inputs fit. The existing four-image container release and partial-recovery scope remains independently owned inside this epic.
**Lifecycle label:** Operational Readiness / CI/CD Engineering
**Alignment target:** EventStore's shared-core plus companion-lane structure, with Tenants-style coverage and consumer validation where supported. Alignment must not weaken tenant-negative evidence, web E2E, integration, deployment, benchmark, package-inventory, or partial-release recovery gates.
**Driven by:** Sprint Change Proposal 2026-07-26 — DW 27.3-CR5 split approved by Administrator 2026-07-21

### Epic 31: OpenBao Secrets Platform and Runtime Secret-Store Migration
The deployed OpenBao `hexalith-keys` platform and the runtime Dapr `secretstore` migration from Kubernetes Secrets to `hashicorp.vault` are owned, hardened, documented, and security-reviewed as an independently deployable operations platform.
**Lifecycle label:** Operational Readiness / Secret Management Platform
**Driven by:** Sprint Change Proposal 2026-07-26 — DW 27.3-CR6 split approved by Administrator 2026-07-21
**NFRs reinforced:** NFR9
**Scope boundary:** Epic 31 owns the deployed-cluster platform and runtime secret-store migration. Aspire/AppHost-local topology and provider-neutral composition are Epic 29.

---

## Epic 0: Tenant and Case Safety Foundation

Before any ingestion, indexing, search, or graph story writes data, the system must have a buildable scaffold, an active tenant with physically isolated backend infrastructure, and an active case within that tenant. `TenantProvisioningWorkflow` is the sole owner of tenant index/database creation. Ingestion and indexing require an active tenant and case and fail clearly before backend writes when either is missing.

Implementation sequence note: Epic 0 is the clean foundation path. Execute Story 0.0 first, then Story 0.1, Story 0.2, and Story 0.3 before any Epic 1 data-writing ingestion/search story.

**Epic 0 Definition of Done / Readiness Gate:**

**Given** a new tenant ID and display name
**When** `TenantProvisioningWorkflow` completes
**Then** RediSearch, Redis Vector, and FalkorDB tenant infrastructure exists before any ingestion or search activity writes tenant data
**And** the tenant is marked active in the tenant registry

**Given** an active tenant
**When** a minimal case is created
**Then** the case is associated with that tenant and can be selected by ingestion workflows
**And** a case node exists in the tenant's dedicated FalkorDB database before ingestion creates `contains` edges

**Given** a missing, inactive, or mismatched tenant/case context
**When** ingestion, indexing, search, or graph traversal is requested
**Then** the operation fails before backend writes or reads that could cross tenant boundaries
**And** the response includes a structured error with a recovery suggestion

### Story 0.0: Project Scaffolding & Single-Command Boot

Historical alias: Story 1.1. Existing completed story files and sprint status may retain the `1-1-project-scaffolding-and-single-command-boot` key for traceability, but future implementation sequencing treats this as the first Epic 0 foundation story.

Implementation sequence note: Story 0.0 is the first executable story. It creates the solution scaffold, AppHost, ServiceDefaults, contracts/test structure, and composition root required by every later workflow, guard, storage, and CLI story.

As a developer,
I want to run a single command (`dotnet run --project Hexalith.Memories.AppHost`) and have the entire stack boot — Memories Server with DAPR sidecar, Redis Stack, FalkorDB, and Aspire Dashboard,
So that I have a working development environment without manual container orchestration.

**Acceptance Criteria:**

**Given** the repository is cloned with root-declared git submodules initialized under `references/` (`references/Hexalith.Commons`, `references/Hexalith.EventStore`, `references/Hexalith.AI.Tools`, `references/Hexalith.Tenants`, `references/Hexalith.FrontComposer`, `references/Hexalith.Builds`, `references/Hexalith.PolymorphicSerializations`)
**When** I run `dotnet run --project Hexalith.Memories.AppHost`
**Then** Redis Stack container starts on port 6379
**And** FalkorDB container starts on port 6380
**And** Memories Server starts with DAPR sidecar (app port 5000, DAPR HTTP 3500, DAPR gRPC 50001)
**And** Aspire Dashboard is accessible showing all services healthy
**And** nested submodules are not initialized or updated unless explicitly requested

**Given** the solution is opened for the first time
**When** I run `dotnet build`
**Then** the build succeeds with projects: Contracts, Server, Redis, AppHost, ServiceDefaults
**And** if git submodules are missing, the build prints a helpful error message instead of cryptic MSBuild failures

**Given** the AppHost is running
**When** I check the Aspire Dashboard
**Then** I see health status for Memories Server, Redis Stack, and FalkorDB
**And** OpenTelemetry traces, metrics, and structured JSON logging are configured via ServiceDefaults

### Story 0.1: Tenant Provisioning Minimum Viable Workflow

As a system operator,
I want the minimum tenant provisioning workflow to create isolated infrastructure before any data-writing story runs,
So that ingestion, indexing, search, and graph work never create tenant resources implicitly or out of sequence.

**Acceptance Criteria:**

**Given** a new tenant ID and display name
**When** `TenantProvisioningWorkflow` completes
**Then** RediSearch, Redis Vector, and FalkorDB tenant infrastructure exists
**And** the tenant is marked active only after all required backend checks pass
**And** failed provisioning rolls back any partial backend resources

**Given** any ingestion, search, graph, CLI, or MCP path receives a missing or inactive tenant
**When** the path validates scope
**Then** it fails before backend writes or reads
**And** it does not create tenant infrastructure on demand

**Ownership Boundary:** Story 0.1 is the minimum executable prerequisite proving an active tenant exists before Epic 1 data-writing work. It must use the same `TenantProvisioningWorkflow` ownership model as Story 5.1 and must not introduce a separate tenant infrastructure creation path.

**Traceability:** FR38, FR44, NFR8. Story 5.1 deepens this into the full tenant lifecycle story; this slice is the executable prerequisite before Epic 1 data-writing work.

### Story 0.2: Minimal Case Bootstrap

As a developer,
I want to create and list a minimal case inside an active tenant before ingestion begins,
So that every memory unit has a valid single-case owner from its first write.

**Acceptance Criteria:**

**Given** an active tenant
**When** I create a case with the minimum required fields
**Then** the case is stored under that tenant
**And** the case can be listed and selected by ingestion workflows
**And** the tenant's graph database contains the case node required for later `contains` edges

**Given** a missing, inactive, or cross-tenant case
**When** ingestion or search requests use that case
**Then** validation fails with a structured error and recovery suggestion before backend mutation.

**Ownership Boundary:** Story 0.2 is the minimum executable prerequisite proving an active case exists before Epic 1 data-writing work. It delivers only minimal case creation, listing, and the case-node-in-graph requirement. It must not absorb case status, activity history, member management, single-case ownership enforcement, case-scoped graph edges, cross-case search, deletion, or annotation work — those belong in Epic 3 (Stories 3.1-3.6) and Story 5.4.

**Traceability:** FR26, FR32, FR44. Story 3.1 deepens case management after the MVP foundation is executable.

### Story 0.3: Tenant and Case Validation Guard

As a developer,
I want one shared validation guard for tenant and case scope,
So that ingestion, indexing, search, graph traversal, CLI, and later MCP behavior enforce the same isolation rule.

**Acceptance Criteria:**

**Given** any operation that reads or writes memory data
**When** tenant or case context is absent, inactive, mismatched, or malformed
**Then** the shared guard rejects the request before backend access
**And** the error includes a stable code, human-readable message, and recovery suggestion

**Given** tests exercise two tenants with similarly named cases or memory units
**When** the guard validates scope
**Then** no cross-tenant read or write succeeds
**And** tenant/case identifiers remain explicit in logs and telemetry without leaking secrets.

**Traceability:** FR44, NFR8. Story 5.4 deepens this guard across all access layers after the minimum prerequisite is in place.

### Story 0.4: Minimum Build/Test CI Preflight

As a maintainer preparing the first data-writing product stories,
I want a minimum automated build and Docker-free test gate in place,
So that Epic 1 work cannot proceed on an unbuildable or untested foundation.

**Acceptance Criteria:**

**Given** a pull request is opened against `main`
**When** the minimum CI preflight runs
**Then** the repository restores and builds `Hexalith.Memories.slnx` with `TreatWarningsAsErrors=true`
**And** the build uses the SDK pinned by `global.json`
**And** package versions remain centrally managed.

**Given** Docker is not available on a contributor machine
**When** the Docker-free test command runs
**Then** Tier-1 unit and contract tests execute without a Dapr sidecar
**And** Docker-required tests are skipped by filter or isolated in a separate lane with clear guidance.

**Given** the CI preflight reports status
**When** a build or test lane fails, is cancelled, or runs zero matching tests
**Then** the lane fails visibly and exposes diagnostics sufficient for implementation handoff.

**Validation Evidence Required:** Story completion must name the build command, Docker-free test command, CI check names if available, and the evidence location. If the minimum gate already exists, Story 0.4 may cite evidence originally produced under Story 11.1 as imported historical evidence. That citation does not create a dependency on future Story 11.1 completion.

## Epic 1: First Tenant-Scoped Memory Ingestion and Search

Developer can boot the stack, provision or select an active tenant and case, ingest local content, and see it persisted and searchable across text, vector, and graph axes with tenant-safe provenance and typed graph edges. Implementation foundation delivered by this epic includes Aspire AppHost, DAPR Workflows (`TenantProvisioningWorkflow` as tenant infrastructure owner, `IngestionWorkflow` with saga/compensation), Contracts V1, Redis (RediSearch + Vector), FalkorDB, Kreuzberg (NuGet, in-process), `references/` submodule validation, and graph indexing activities.

**Implementation Readiness Amendment (2026-05-18; reaffirmed 2026-06-27):** Stories 1.2, 1.5, and 1.6 are accepted as historical broad technical or bundled infrastructure slices, but they are not valid patterns for future story creation. Any reopened, reimplemented, or analogous work must be split into independently demonstrable vertical stories with CLI/API/contract/trace/integration evidence. Internal classes, mocks, or green unit tests alone are not sufficient completion evidence.

### Story 1.2: Memory Unit Domain Model & Contracts

As a developer,
I want a well-defined domain model for memory units, graph edges, metadata fields, and ingestion types in `Contracts.V1`,
So that all services share a consistent, versioned type system with serialization guarantees.

**Historical Scope Guard:** Do not reopen Story 1.2 as a single broad contract/model implementation unit. If contract rework resumes, split it into separate numeric stories such as memory-unit contract shape, graph-edge contract shape, Evidence Packet/error contract mapping, and downstream serialization/consumer fixture proof.

**Readiness tooling guard:** Do not create a new implementation story file that reuses this broad historical story key for new work. New implementation must use newly numbered split stories with externally observable completion evidence.

**Acceptance Criteria:**

**Given** the Contracts.V1 namespace exists
**When** I inspect the memory unit model
**Then** it contains all required fields: Id (opaque stable string; callers must not rely on ULID or time-sortable semantics), TenantId, CaseId, Content, ContentHash (SHA-256), SourceUri, SourceType (enum: file, url, event, command, projection, discussion), IngestedBy, IngestedAt, LastUpdated, Status (enum: queued, extracting, embedding, indexing, indexed, failed), Metadata (Dictionary<string, MetadataField>), EmbeddingProvider, EmbeddingDimensions, Classification (optional), FailureDetails (optional)
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

**Validation Evidence Required:** Contract serialization tests and schema-compatible JSON examples must be captured with the story completion evidence.

**Observable Proof Gate:** Future rework of this story must include a contract-visible proof package: representative JSON payloads for `MemoryUnit`, `GraphEdge`, `MetadataField`, `FailureDetails`, and `ErrorResponse`; serialization round-trip results; and at least one downstream consumer fixture or API/CLI example that proves the contract is usable outside the contracts assembly.

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

**Validation Evidence Required:** Completion evidence must include extraction fixture results for text, markdown, and PDF plus externally observable trace evidence. Pure internal implementation completion is not sufficient.

**Observable Proof Gate:** Future rework of this story must show extracted content through an activity, API, CLI, trace, or integration-harness boundary using text, markdown, and PDF fixtures. Completion cannot rely only on `ContentExtractionClient` unit tests.

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
**Then** it retrieves the value through DAPR Secrets API component `secretstore`
**And** the component is backed by OpenBao in Aspire and deployed topologies
**And** tenant configuration contains only the secret name
**And** product code has no direct dependency on OpenBao, .NET User Secrets, Kubernetes Secrets, or another provider-specific secret API
**And** the secret value is never written to configuration, ordinary environment variables, logs, traces, or API responses

**Supersession note:** Epic 29 owns implementation and observable verification of this strengthened secret-provider contract. Story 1.4 remains historical completed work and is not reopened.

**Validation Evidence Required:** Completion evidence must include embedding contract output with provider/model/dimension metadata and secret-redaction verification. Pure internal implementation completion is not sufficient.

**Observable Proof Gate:** Future rework of this story must show a developer-observable embedding result with provider, model, dimensions, and redacted secret behavior through an activity, API, CLI, trace, or integration-harness boundary. Completion cannot rely only on mocked `EmbeddingClient` or rate-limiter unit tests.

### Story 1.5: Three-Backend Indexing

**Sizing note:** Story 1.5 is historical completed scope. Future reimplementation or major rework must split it into smaller vertical stories: (a) syntactic indexing on RediSearch with tenant-namespaced index and tenant-validation failure mode; (b) semantic indexing on Redis Vector with tenant-namespaced KNN index; (c) graph indexing on FalkorDB including node creation, `caused_by`/`correlated_with`/`contains` edge writes, parameterized-query enforcement via `IGraphQueryBuilder`, and the tenant lifecycle separation rule that activities never create or mutate tenant index/database state.

**Historical Scope Guard:** Do not reopen Story 1.5 as a single implementation unit. If indexing rework resumes, create separate numeric stories for the documented slices before implementation starts, keep each slice independently testable against tenant-scoped infrastructure, and require observable proof per slice (CLI-visible, contract-visible, or integration-harness output) — internal unit tests alone are not sufficient completion evidence.

**Rework Ownership Gate:** Any Epic 1 indexing rework must prove it only validates or verifies tenant infrastructure readiness before writing. It must not call `FT.CREATE`, create Redis Vector indexes, create FalkorDB tenant databases, or otherwise create/mutate tenant infrastructure lifecycle state from ingestion, indexing, search, graph, CLI, or MCP paths. Missing or incompatible tenant infrastructure is a `TENANT_NOT_PROVISIONED` or equivalent structured validation/operational inconsistency, not a trigger for on-demand resource creation.

**Readiness tooling guard:** Do not create a new implementation story file that reuses this broad historical story key for new work. New implementation must use newly numbered split stories with externally observable completion evidence.

As a developer,
I want ingested content to be indexed across RediSearch (syntactic), Redis Vector (semantic), and FalkorDB (graph) using tenant infrastructure already provisioned by `TenantProvisioningWorkflow`,
So that memory units are searchable across all three axes after ingestion.

**Acceptance Criteria:**

**Given** an active tenant provisioned by `TenantProvisioningWorkflow`
**And** a memory unit with extracted content and generated embedding
**When** `IndexSyntacticActivity` executes
**Then** the memory unit is indexed in RediSearch with tenant-namespaced index (`{tenantId}:syntactic`)
**And** the content, metadata, and source information are searchable via full-text query

**Given** an active tenant provisioned by `TenantProvisioningWorkflow`
**And** a memory unit with a generated embedding vector
**When** `IndexSemanticActivity` executes
**Then** the vector is stored in Redis Vector Search with tenant-namespaced index (`{tenantId}:semantic`)
**And** the vector is retrievable via KNN similarity search

**Given** an active tenant provisioned by `TenantProvisioningWorkflow`
**And** a memory unit with source information
**When** `IndexGraphActivity` executes
**Then** a node is created in FalkorDB in the tenant's dedicated database (physical isolation at database level)
**And** if the source contains CausationId, a `caused_by` edge is created (confidence 1.0, origin: explicit)
**And** if the source contains CorrelationId, a `correlated_with` edge is created (confidence 0.8, origin: explicit)
**And** a `contains` edge is created from the case node to the memory unit node (confidence 1.0)

**Given** the `IGraphQueryBuilder` is used for all FalkorDB queries
**When** any graph operation is performed
**Then** only parameterized Cypher queries are used — no raw Cypher string construction
**And** this is enforced structurally by the interface design

**Given** tenant infrastructure is missing or inactive
**When** any indexing activity is invoked
**Then** the activity fails before writing data with a structured `TENANT_NOT_PROVISIONED` error and recovery suggestion

**Given** indexing activities execute for an active tenant
**When** they write to RediSearch, Redis Vector, or FalkorDB
**Then** they use only tenant infrastructure created by `TenantProvisioningWorkflow`
**And** they do not create or mutate tenant index/database lifecycle state

**Validation Evidence Required:** Completion evidence must include tenant-scoped indexing proof across RediSearch, Redis Vector, and FalkorDB using contract, CLI-visible, or integration-harness output. Pure internal implementation completion is not sufficient.

**Observable Proof Gate:** Future rework of this story must show the same memory unit discoverable from all three tenant-scoped backends, or must explicitly document which backend is unavailable and why. Completion cannot rely only on activity unit tests or graph query builder tests.

### Story 1.6: Ingestion Workflow Orchestration

As a developer,
I want to ingest a local file and have it automatically processed through the full pipeline (validate → extract → embed → index across all backends → verify consistency),
So that a single API call results in a fully searchable memory unit with provenance tracking.

**Sizing note:** Story 1.6 is historical completed scope. Future reimplementation or major rework must split it into smaller vertical stories: happy-path local file ingestion orchestration; failure, compensation, and failed-unit visibility; restart recovery, idempotency, and duplicate detection hardening.

**Historical Scope Guard:** Do not reopen Story 1.6 as a single implementation unit. If orchestration work resumes, create separate numeric stories for the documented slices before implementation starts, keep each slice independently testable, and require observable API/CLI/integration proof for each slice.

**Readiness tooling guard:** Do not create a new implementation story file that reuses this broad historical story key for new work. New implementation must use newly numbered split stories with externally observable completion evidence.

**Acceptance Criteria:**

**Given** a valid file, an active tenant, and an active case
**When** `IngestionWorkflow` is started
**Then** it validates tenant and case existence before extraction, embedding, or backend writes
**And** it orchestrates: `ValidateContentActivity` → `ExtractContentActivity` → `GenerateEmbeddingActivity` → fan-out (`IndexSyntacticActivity` + `IndexSemanticActivity` + `IndexGraphActivity`) → `VerifyConsistencyActivity`
**And** the memory unit status transitions: queued → extracting → embedding → indexing → indexed

**Given** the tenant or case is missing, inactive, or mismatched
**When** `IngestionWorkflow` is started
**Then** it fails before extraction, embedding, or indexing
**And** it returns a structured error (`TENANT_NOT_FOUND`, `TENANT_NOT_ACTIVE`, `CASE_NOT_FOUND`, or `CASE_TENANT_MISMATCH`) with a recovery suggestion

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
So that MVP tenants can configure Google embedding settings consistently and the system is ready for multi-provider support in later provider expansion stories.

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
**And** the embedding provider strategy/factory shape supports addition of new concrete providers without refactoring the ingestion workflow

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

**Validation Evidence Required:** Completion evidence must include CLI or API search output showing tenant-scoped BM25 results and empty-state behavior. Pure internal implementation completion is not sufficient.

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

**Validation Evidence Required:** Completion evidence must include semantic search output showing vector result ordering, source attribution, and provider/model metadata. Pure internal implementation completion is not sufficient.

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
I want each search axis to expose a documented, deterministic 0.0-1.0 score for single-axis results and explain output,
So that per-axis relevance is interpretable on its own terms, independent of how the hybrid composite is produced.

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
**Then** outputs match expected values exactly (NFR24 single-axis explain semantics, NFR25 determinism)

**Validation Evidence Required:** Deterministic normalization tests must cover BM25 saturation, cosine pass-through, graph proximity decay, and repeated-query stability.

### Story 2.5: Fusion Algorithm & Hybrid Search

As a developer,
I want to search memory units across all available axes in a single hybrid query with configurable axis selection,
So that I get the best possible results by combining syntactic, semantic, and graph relevance signals.

**Acceptance Criteria:**

**Given** a hybrid search query with all three axes enabled
**When** the fusion algorithm executes
**Then** it calls all three search backends in parallel
**And** results are merged using the pure function `Fuse(List<ScoredResult>[], FusionWeights) → RankedResults`
**And** the composite score is a deterministic weighted reciprocal-rank fusion of per-axis result ranks — raw BM25, cosine, and graph-proximity magnitudes are not averaged into the composite (NFR24)
**And** explain output exposes each axis's rank contribution and the fusion weights applied, rather than its raw magnitude
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

**Given** hybrid search receives axis-availability information from the system degradation policy
**When** one axis is unavailable and the remaining axes return results
**Then** the fusion layer combines only the available axes
**And** the response carries degraded search metadata naming the excluded axis.

**Ownership note:** Story 2.5 owns search-layer fusion behavior over available axes. Story 5.6 owns the system-wide backend availability policy, health detection, chaos/degradation verification, and FR66/NFR18 degraded-service contract.

**Fusion supersession note (2026-07-26):** Stories 2.4 and 2.5 were originally specified against a normalize-then-weighted-average model. Story 22.4 selected corpus-invariant weighted reciprocal-rank fusion, and Epic 26 calibrated it (RRF `k=10`; live syntactic/semantic/graph weights `0.30/0.35/0.35`; optional NL weight `0.20`, default-off). The text above was reconciled to that as-built model by the approved Sprint Change Proposal 2026-07-26; the implementation in `FusionEngine` and `ExplainMetadataBuilder` was already weighted RRF and is unchanged by that reconciliation. Per-axis normalization is retained for single-axis score semantics and explain output only. Authority for fusion behavior is `architecture.md` section 8; `prd.md` NFR24-NFR26 owns the requirement.

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

### Story 2.7: Evidence Packet Contract Mapping

**Historical alias:** This story was previously tracked as `2.6A`. Existing implementation artifacts and sprint-status history may keep the old key as an alias, but new story tooling and future references should use `2.7`.

As a developer and LLM-agent integrator,
I want search and diagnostic responses to share an Evidence Packet contract,
So that CLI, MCP, and future web UI expose the same trust semantics.

**Acceptance Criteria:**

**Given** `Contracts.V1` defines or maps the Evidence Packet response shape
**When** search, explain, empty-result, degraded-result, or diagnostic responses are produced
**Then** the response can expose tenant/case scope, result summary, sources, evidence strength, confidence caveat, retrieval axes used, graph summary, state, omitted details, and recovery actions

**Given** CLI JSON search output is requested
**When** an Evidence Packet is emitted
**Then** it uses the same field semantics as MCP responses and future UI composition descriptors
**And** no surface invents a conflicting definition of confidence, degraded state, omitted details, or recovery action

**Given** a response is compressed by token budget or output density
**When** details are omitted
**Then** omitted fields are named explicitly
**And** deterministic expansion handles identify how to retrieve the omitted detail groups

**Given** Evidence Packet contract tests run
**When** complete, degraded, empty, unauthorized, and token-budget-compressed packets are serialized
**Then** JSON round-trips preserve the contract shape and required fields

### Story 2.8: Benchmark Suite & Thesis Validation

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

**Given** an active tenant provisioned by Epic 0
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
**And** the operation is orchestrated by DAPR Workflow as a durable saga with retry and compensation steps for each backend
**And** if a backend deletion step fails after retries, the workflow records the failed stage, keeps the case in `deleting` or `delete_failed` state, and exposes a retry or repair path instead of silently leaving partial state

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

Operator can provision tenants with physically separate indexes across all three backends, delete tenants with full cleanup, verify zero cross-tenant leakage via automated checks, manage tenant configuration (rate limits, embedding providers), and enforce tenant context at all access layers. System returns partial results when backends are unavailable rather than failing completely. Tenant provisioning is also consumed by Epic 0 before ingestion/indexing/search begin; the remaining Epic 5 stories deepen the full tenant lifecycle. Zero cross-tenant data leakage is a hard gate.

### Story 5.1: Tenant Provisioning Workflow

As a system operator,
I want to create a tenant with physically separate indexes across all three backends in a single command,
So that each tenant has isolated infrastructure with rollback protection if provisioning fails.

**Acceptance Criteria:**

**Given** a new tenant ID and display name
**When** `TenantProvisioningWorkflow` is started
**Then** it orchestrates: `ProvisionRediSearchActivity` → `ProvisionRedisVectorActivity` → `ProvisionFalkorDbActivity` → `VerifyTenantActivity`
**And** RediSearch creates tenant-namespaced indexes (`{tenantId}:syntactic`)
**And** Redis Vector creates tenant-namespaced indexes (`{tenantId}:semantic`)
**And** FalkorDB creates a dedicated database for the tenant (physical isolation at database level, not label level)
**And** `TenantProvisioningWorkflow` is the sole owner of RediSearch, Redis Vector, and FalkorDB tenant infrastructure creation
**And** ingestion, search, graph, CLI, and MCP paths treat missing or inactive tenant infrastructure as a validation failure rather than creating indexes on demand

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

**Ownership Boundary:** Story 5.1 is the canonical full tenant lifecycle story for provisioning semantics, rollback behavior, verification, and lifecycle ownership. If Story 0.1 has already implemented the complete workflow, Story 5.1 should verify, extend, or mark the full lifecycle criteria as satisfied rather than duplicating divergent provisioning logic.

**Rework Ownership Gate:** Any Epic 5 tenant lifecycle rework may change tenant infrastructure creation only inside `TenantProvisioningWorkflow` and its tenant provisioning, verification, rollback, or deletion activities. It must update schema definitions, provisioning/deletion activities, verification behavior, and lifecycle tests together. Feature paths such as ingestion, indexing, search, graph, CLI, and MCP remain consumers of active tenant infrastructure and must fail clearly when required infrastructure is missing or inactive.

### Story 5.2: Tenant Deletion Workflow

As a system operator,
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

As a system operator,
I want to run automated tenant isolation checks,
So that I can verify zero cross-tenant data leakage with confidence.

**Acceptance Criteria:**

**Given** two tenants (A and B) each with indexed memory units
**When** I run `tenant verify` on tenant A (FR40)
**Then** the verification report includes passing checks for search, ingestion visibility, and graph traversal isolation
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

As a system operator,
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

As a system operator,
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

**Ownership note:** Story 5.6 is the canonical owner of FR66 and NFR18 degraded-service behavior. Search stories consume the availability/degradation result; they do not define backend health policy independently.

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

Developer can accomplish MVP thesis-validation tasks via a CLI tool with actionable error messages including recovery suggestions, multiple output formats (human-readable, JSON, table), tenant/case scope visibility, metadata origin tracking display, explain output, and a README quickstart path that proves the under-30-minute first-search gate. MVP CLI essentials are `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, benchmark support, and README quickstart validation. Full CLI polish (`explore`, `status`, `handlers`, `quickstart`, batch directory ingestion, richer diagnostics) is Phase 1.5 unless explicitly pulled forward. This is the Gate 3 critical path.

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
**Then** I see the MVP command groups required for thesis validation: `ingest`, `search`, `case`, `tenant`, and benchmark commands (FR53)
**And** each group shows a brief description
**And** Phase 1.5 command groups (`explore`, `status`, `handlers`, `quickstart`, batch directory ingestion) are tracked separately and do not block MVP thesis validation unless explicitly pulled forward

**Given** the CLI needs to connect to the Memories Server
**When** I configure the endpoint
**Then** non-secret CLI configuration layering is respected (precedence high to low): command-line flags → environment variables (`HEXALITH_MEMORIES_*`) → config file (`~/.hexalith/memories.json` or project-local) → DAPR configuration (NFR23)
**And** application and provider secrets are not CLI configuration
**And** those secrets remain behind the Server's DAPR secret-store boundary
**And** credentials required directly by the CLI use the protected mechanism defined by the selected execution environment and are never persisted in the CLI config file

**Given** the CLI is configured for different environments
**When** I target localhost (local dev), docker service name (container), or remote URL (ingress)
**Then** the CLI connects successfully to each environment type

**Given** any MVP CLI command
**When** it communicates with the Memories Server
**Then** it uses the minimal direct HTTP/ingress adapter owned by the CLI for the thesis-validation command set
**And** it does not depend on the Phase 1.5 `Client.Rest` package to satisfy MVP Gate 3
**And** authentication uses the configured DAPR API token or ingress auth where that environment requires it

**Given** Phase 1.5 client package work begins
**When** `Hexalith.Memories.Client.Rest` is introduced or extracted
**Then** the MVP CLI adapter can be replaced by the reusable client without changing CLI command semantics or output contracts

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
**Then** the message says: "No results. This tenant has no memory units yet. Get started: `memories ingest <file>` to add your first document, or configure a DAPR subscription to auto-index events. Follow the README quickstart for a guided setup. If the Phase 1.5 quickstart command is installed, run `memories quickstart`." (FR57)

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
**Then** they can boot the stack, create or select a tenant and case, ingest a sample document, and complete a first successful search in <30 minutes (NFR31)
**And** the steps use only MVP CLI essentials

**Given** the `memories quickstart` command
**When** CLI Phase 1.5 polish is in scope
**Then** it provides an interactive guided setup: verify prerequisites (Docker, DAPR), boot the stack, create a tenant, create a case, ingest a sample document, run a search (FR57)
**And** each step provides clear instructions and validates success before proceeding
**And** it is not required to satisfy MVP Gate 3 unless explicitly pulled forward by a later sprint change

**Given** any CLI command
**When** I run it with `--help`
**Then** it displays: command description, available flags/options, and at least one usage example (NFR30)

**Given** the complete CLI command set
**When** help completeness is verified
**Then** every command has `--help` with at least one example (NFR30 — testable in CI)

### Story 7.5: Search & Access Telemetry

As a system operator,
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
**And** the event includes only sanitized, bounded, low-cardinality fields: timestamp, tenant ID, operation type (search/ingest/traverse), case ID, user identity or stable internal subject identifier, operation state/error code, duration bucket, result count bucket, axis selection, and query length bucket
**And** search query text, raw query parameters, metadata filter values, source payloads, secrets, and access tokens are never written to normal logs, traces, metrics, or audit telemetry
**And** if protected raw query capture is ever introduced for diagnostics, it must be opt-in, access-controlled, time-limited, separately documented, and excluded from the default audit telemetry path

---

## Epic 8: Observability & System Health

Operator can verify consistency across all three backends, detect and repair index/graph divergence, and observe the system via readiness/liveness health checks. This epic delivers the operational confidence layer. FR71 portable data export is Phase 2 and is excluded from MVP readiness unless a later sprint change explicitly pulls it forward.

### Story 8.1: Health Checks & Readiness

As a system operator,
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

As a system operator,
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
**When** I run `memories consistency inspect --tenant <tenant-id> --id <unit-id>`
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

### Story 8.4: End-to-End Telemetry Integration Tests (Tier-3 / Aspire)

As a Memories release manager,
I want end-to-end Tier-3 integration tests that verify distributed traces propagate across CLI → Server → backends AND audit events reach the deployed stack's stdout log stream,
So that I can ship releases with confidence that NFR28 and FR67 hold on real infrastructure — not just on the Tier-2 in-process approximation.

**Outcome summary:** The release manager can prove distributed trace and audit-event behavior on real infrastructure before release promotion.

**Acceptance Criteria:**

**Given** the AspireIngestionPipelineFixture is running with in-memory OTLP capture
**When** the CLI invokes a search via DI against the fixture
**Then** captured spans share a single TraceId across CLI root → HttpClient → AspNetCore → memories.search activity
**And** parent-child relationships match W3C TraceContext semantics (NFR28 authoritative gate)

**Given** a search + ingest + traverse + case-access run against the fixture
**When** the Server container's stdout JSON log stream is captured
**Then** exactly one AccessTelemetryEvent per operation is emitted with schemaVersion=1 and EventId in 7500-7599 (FR67 authoritative gate)
**And** health-endpoint probes emit zero AccessTelemetryEvent entries

**Given** the captured memories.search activity and its audit event
**When** both are cross-referenced
**Then** the audit event's traceId + spanId match the activity's ids

**Given** the test-side in-memory OTLP capture is absent
**When** the Server is run without the 8.4 trigger
**Then** no in-memory exporter is registered and Story 7.5's OpenTelemetryRegistrationTests pass unchanged

**Given** the GitHub Actions workflow matrix
**When** a PR is opened
**Then** 8.4's tests run only on the Docker-provisioned merge-queue lane (Tier-2 variants gate per-PR; Tier-3 gates release promotion)

**Source:** Follow-up for Story 7.5 Tasks 11.3 + 11.4 (deferred in Rev 1.3/1.4 on Docker availability). Depends on `AspireIngestionPipelineFixture` (Epic 6) and the `OpenTelemetry.Exporter.InMemory` package (already in `Directory.Packages.props`).

### Story 8.5: Redis OTEL Instrumentation

**Sizing note:** Story 8.5 covers four distinct deliverables that can be implemented and reviewed independently: (a) production Redis/FalkorDB instrumentation registration with keyed `IConnectionMultiplexer` resolution; (b) Tier-3 end-to-end trace assertion replacing the previous soft-skip helper; (c) Tier-2 registration tests asserting tracer subscription without Docker or Aspire; (d) telemetry documentation update in `docs/dev/telemetry.md`. If future implementation work resumes, prefer landing each deliverable as a separately reviewable slice with explicit completion evidence rather than a single bundled change.

**Historical Scope Guard:** Do not reopen Story 8.5 as a single implementation unit. If trace-instrumentation work resumes, the four slices above must be independently testable and each must close with the corresponding proof: instrumentation activity present in trace, Tier-3 hard assertion green, Tier-2 registration tests green, and updated documentation merged.

As a Memories release manager,
I want Redis client calls (RediSearch, Redis Vector, and FalkorDB) to emit OpenTelemetry spans inside the same distributed trace as the originating request,
So that operators can attribute search and traversal latency to the correct backend, and Story 8.4's Redis-span check is a hard assertion rather than a soft skip.

**Outcome summary:** Operators can attribute Redis-backed search and traversal latency inside the same distributed trace as the originating request.

**Acceptance Criteria:**

**Given** the Memories Server is running via Aspire or `dotnet run`
**When** a search, ingest, traverse, or case-access request touches Redis-backed infrastructure
**Then** at least one activity with `Source.Name == "OpenTelemetry.Instrumentation.StackExchangeRedis"` is emitted for backend Redis calls
**And** the activity shares the `TraceId` of the originating ASP.NET Core request
**And** instrumentation covers both keyed `IConnectionMultiplexer` connections: `redis` for RediSearch and Redis Vector, and `falkordb` for graph operations.

**Given** the Tier-3 telemetry fixture runs `CliSearch_EndToEnd_SingleTraceIdAcrossAllHops`
**When** the end-to-end trace is captured
**Then** at least one Redis-source activity appears in the trace
**And** its parent chain reaches the CLI root activity
**And** the retired `Ac2RedisSkipReviewBy` helper, its tests, and the `telemetry.redis.instrumentation.skipped` warning path no longer exist.

**Given** Tier-2 telemetry registration tests run without Docker or Aspire
**When** the tracer provider is built through the shared service-defaults path
**Then** the Redis instrumentation source is subscribed
**And** missing keyed Redis multiplexers fail eagerly with a clear `IConnectionMultiplexer` key error.

**Given** `docs/dev/telemetry.md`
**When** an operator or developer reviews the end-to-end trace verification guidance
**Then** Redis spans are documented in the signal inventory
**And** the previous Story 8.4 AC #2 deferral language is replaced with the shipped hard-assertion behavior.

## Phase 2 Backlog Placeholders

### Data Export (FR71 / Non-MVP Gate)

As a developer,
I want to export all memory units, metadata, and graph edges for a case or tenant,
So that I can back up knowledge, migrate data, or analyze it externally.

**Phase Note:** FR71 export is deferred to Phase 2 for readiness accounting and is not part of Epic 8's MVP Operations story sequence. If export exists as completed historical work, it remains non-blocking and must not be counted as MVP readiness unless a later sprint change explicitly pulls export forward. Story key `8.3` is reserved for this Phase 2 export history; the detailed MVP Epic 8 sequence intentionally continues with `8.4` and `8.5`. Story-status and story-file-scope tooling must treat `8.3` as `reserved-non-mvp` or explicitly map it as non-MVP historical work so it is not reported as a missing MVP story.

**Activation rule:** When FR71 becomes active, create a normal Story 8.3 story file and sprint-status entry before implementation. Until then, this placeholder remains `reserved-non-mvp` and must not be counted as a missing MVP story or as active MVP readiness.

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

### Recency-Aware Ranking (Age Decay)

As a developer,
I want an optional deterministic recency prior as a fusion input, tunable per query and tenant,
So that when relevance is otherwise equal, newer memory wins.

**Phase Note:** Sourced from Sprint Change Proposal 2026-07-25 (Cerebras knowledge-base findings intake, finding D1; see `research/cerebras-knowledge-base-findings-2026-07-25.md`). Complements the existing >90-day staleness confidence flag, which is informational only and does not affect ranking.

**Activation rule:** When activated, create a normal story file and sprint-status entry before implementation. Fusion must stay deterministic and pure, the recency prior must be explainable in `--explain` output, and the benchmark NDCG suite must be re-validated against the PRD hard line (≥ 7/8 hybrid wins) before any default change ships.

### Ingestion Distillation & Normalized Embedding

As a developer,
I want an optional LLM distillation activity in `IngestionWorkflow` that normalizes noisy conversational or long-form content into a consistent searchable form (one-line question, short summary, resolution, referenced systems) embedded alongside full-text indexing,
So that semantic recall improves on content whose raw text embeds poorly.

**Phase Note:** Sourced from Sprint Change Proposal 2026-07-25 (finding D2). Cerebras reports significant accuracy gains from embedding a normalized distilled form instead of raw transcripts; Memories already applies this pattern to events via the NL-description dual embedding (Epic 9) — this placeholder extends it to document and discussion ingestion.

**Activation rule:** When activated, create a normal story file and sprint-status entry before implementation. Distillation runs in workflow activities only (replay-safe orchestration), raw content remains fully indexed, and distilled fields carry `ai-inferred` origin with confidence scores.

### Context-Prepended Chunk Embedding & Low-Signal Gating

As a developer,
I want chunk embeddings prepended with parent-document or thread context and a deterministic low-signal gate (IDF and length thresholds) applied before embedding rows are created,
So that tangent content is findable on its own while filler never pollutes the vector index.

**Phase Note:** Sourced from Sprint Change Proposal 2026-07-25 (finding D3). Cites Anthropic Contextual Retrieval and the Cerebras bursting gate (IDF ≥ 4.0 on at least one token, ≥ 200 combined characters). Prerequisite thinking for Phase 2 discussion threading.

**Activation rule:** When activated, create a normal story file and sprint-status entry before implementation. Gating thresholds must be deterministic, configurable per tenant, and covered by ingestion tests including the rejected-below-threshold path.

### Reranker Activation & Context Re-Expansion

As a developer,
I want a small-model reranker implemented behind the existing `IResultFuser` seam (fused top-N → scored → top-K) and neighbor re-expansion of winning chunks into the Evidence Packet,
So that results are ranked against the actual question and never lose the surrounding context that chunking split apart.

**Phase Note:** Sourced from Sprint Change Proposal 2026-07-25 (finding D4). The `IResultFuser` reranker seam was delivered by Story 22.7; this placeholder covers its first implementation plus Evidence Packet neighbor re-expansion.

**Activation rule:** When activated, create a normal story file and sprint-status entry before implementation. The reranker must be optional and degradable — a reranker outage falls back to deterministic fusion order with the degraded flag set (FR66) — and token-budget rules (FR23) still bound re-expanded responses. Benchmark NDCG scoring must cover the reranked path.

### Scope Bundles & Default Scope

As a team member or agent,
I want named, non-exclusive scope bundles that reference cases and sources without duplicating them, and a default scope stored per user or agent identity,
So that search is relevant by default without weakening strict case ownership.

**Phase Note:** Sourced from Sprint Change Proposal 2026-07-25 (finding D5), mirroring the Cerebras "projects" pattern (the same source may belong to many projects; onboarding sets a default project that scopes queries automatically). Additive over the strict single-ownership case model and Story 3.4 cross-case search.

**Activation rule:** When activated, create a normal story file and sprint-status entry before implementation. Physical tenant isolation is unchanged, cross-tenant bundles are forbidden, and activation requires attached cross-tenant negative evidence per the standing tenant-isolation testing rule.


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

**Scope Clarification (2026-06-24):** For Hexalith module integration, modules publish CloudEvents to the configured DAPR pub/sub topic and set stable `source` prefixes so `SourceToTenantMap` can route them. A consumer needing multiple independent topics runs separate Memories deployments today; multi-topic routing remains a future refinement.

The Memories Server DAPR sidecar is the event-subscription owner. Other Hexalith modules should not call Memories REST ingestion directly for domain event streams; they publish to DAPR pub/sub and let the Memories sidecar deliver to `/events/ingest`.

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

As a developer building LLM agents,
I want to constrain response sizes by token budget and ensure authenticated access,
So that memory responses fit within context windows and access is properly secured.

**Acceptance Criteria:**

**Given** a `search_memory` call with `token_budget=2000` (FR23)
**When** results exceed the token budget
**Then** results are truncated by relevance rank — highest-scoring results included first
**And** the response includes `omitted_count` indicating how many results or fields were omitted
**And** the response includes deterministic expansion handles for omitted detail groups
**And** a follow-up expansion request can retrieve omitted details without changing the original result identity, tenant scope, case scope, or ranking context
**And** omitted fields are named explicitly so agents can decide whether to expand, refine, request ingestion, or escalate
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

**Lifecycle label:** Operational Readiness / Release Hardening.

Every commit is automatically built, tested, and versioned via GitHub Actions. PRs get build + test checks. Releases publish NuGet packages with semantic versioning from conventional commits. Branch protection on main. This is cross-cutting infrastructure that enables the open-source contributor journey.

### Story 11.1: GitHub Actions Build & Test Pipeline

As a contributor,
I want every PR to be automatically built and tested,
So that I can trust the codebase quality and get fast feedback on my changes.

**Sequencing note:** The minimum Epic 1 preflight is owned by Story 0.4. Story 11.1 extends that foundation into the full GitHub Actions quality pipeline, including stable check names, integration-fast behavior, diagnostic artifacts, branch-protection documentation, and contributor/maintainer CI parity.

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

**Validation Evidence Required:** Story completion must name the CI check evidence path for build, unit/contract tests, and integration-fast behavior.

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
**Then** all packages listed in `tools/release-packages.json` are built and published
**And** the current published package set is Contracts, Client.Rest, Redis, Cli, Mcp, Aspire, ServiceDefaults, EventStore, and Telemetry
**And** Server, AppHost, and Web are explicitly non-packable
**And** all packages share the same version number

**Given** the release completes
**When** I check NuGet
**Then** all published packages are available with correct version, descriptions, and dependencies

**Validation Evidence Required:** Story completion must include release dry-run or publish evidence tied to the authoritative package inventory in `tools/release-packages.json`.

**Given** CONTRIBUTING.md
**When** a new contributor reads it
**Then** it covers: conventional commit format, PR process, how to run tests (unit without Docker, integration with Docker), branch naming conventions, code review expectations
**And** it is clear enough for a first-time contributor to submit a valid PR

---

## Epic 12: First Release & Operations Foundation

**Lifecycle label:** Operational Readiness / Release Hardening.

Cut the first real release of Hexalith.Memories to nuget.org, apply branch protection on `main`, operationalize the Epic 11 retrospective action items, and prove the release path end-to-end before any further feature investment. This epic closes the gap between "CI infrastructure built" and "release path proven against a real publish event," and operationalizes the six systemic patterns surfaced in the Epic 11 retrospective so they become enforced rather than aspirational.

**Driven by:** Epic 11 retrospective (`_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md`) + Sprint Change Proposal 2026-04-26 (`sprint-change-proposal-2026-04-26.md`) Option C — Hybrid: Operations Epic 12 first, then Phase 2 decision after first release lands.

**Out of scope:** Phase 2 feature work (per-tenant LLM config, tokenizer-accurate budgets, projection registry, MCP trace-hop assertion, etc.). Phase 2 is a separate decision deferred until Epic 12 outcomes are known.

### Story 12.1: First Release Path Validation

As a maintainer,
I want the first real release to nuget.org to run end-to-end with the Epic 11 infrastructure,
So that the release path is proven against a real publish event before any further feature investment.

**Acceptance Criteria:**

**Given** Epic 11 retrospective action **A1** (branch protection on `main` per `docs/dev/branch-protection.md`) has been applied in the GitHub repository settings by a maintainer,
**And** the first CI workflow run on `main` has exposed the `build`, `test-unit-contract`, and `integration-fast` check names,
**When** the maintainer selects those three checks as required + requires at least one approval + blocks direct push,
**Then** branch protection on `main` is enforcing the Epic 11 contract end-to-end.

**Given** Epic 11 retrospective action **A2** (`NUGET_API_KEY` repository secret) has been added in the GitHub repository settings by a maintainer with a scoped nuget.org API key,
**When** the secret is present and `release.yml` references it,
**Then** the release workflow's `npx semantic-release` step has the credential it needs to publish.

**Given** A1 and A2 are both applied,
**When** a deliberate `feat:` or `fix:` commit lands on `main` (via PR + approval, not direct push),
**Then** `release.yml` triggers,
**And** semantic-release determines the next version from conventional commits,
**And** `pack-release.ps1` produces 7 packages with consistent versions and metadata,
**And** `validate-release-packages.ps1` passes,
**And** `publish-nuget.ps1` pushes the packages to `https://api.nuget.org/v3/index.json`,
**And** semantic-release creates the `v${version}` tag + GitHub Release with auto-generated release notes,
**And** the `chore(release)` commit lands without re-triggering the release workflow.

**Given** the publish step completes,
**When** the maintainer inspects nuget.org,
**Then** all 7 packages (`Hexalith.Memories.Contracts`, `…Client.Rest`, `…Redis`, `…Cli`, `…Mcp`, `…EventStore`, `…Telemetry`) are listed at the released version with correct descriptions, READMEs, license metadata, and cross-package dependency versions equal to the released version.

**Given** the first release succeeds,
**Then** a release runbook is captured at `docs/dev/release-runbook.md` (closes Epic 11 retrospective deliverable D2) documenting the end-to-end first-release sequence so the second release is repeatable by any maintainer.

### Story 12.2: Forbidden Default Tolerances Checklist (A3)

As a code reviewer,
I want a written checklist of "tolerant defaults that hide failure" patterns to scan for,
So that the four tolerance-idiom silent-failure patterns surfaced in Epic 11 (and equivalent shapes) are caught at review time rather than after a green-but-broken pipeline reaches production.

**Acceptance Criteria:**

**Given** `CONTRIBUTING.md` has a Code Review section,
**When** a contributor reads it before reviewing infrastructure / scripts / workflow YAML changes,
**Then** the section names — at minimum — these tolerance-idiom patterns to flag:

- Process-substitution / pipeline exit-code swallowing (`mapfile -t X < <(cmd)` losing `cmd`'s `exit 1`)
- `actions/upload-artifact` `if-no-files-found: ignore` masking failed pack/build steps
- `dotnet nuget push --skip-duplicate` masking partial-publish without idempotency precondition
- Per-row / per-iteration zero-count silently passing aggregate verifiers
- Empty `catch { }` blocks in PowerShell / C# that swallow exceptions
- `|| true` and equivalent shell idioms that discard non-zero exit codes
- Default-empty-array fallbacks (`PROJECTS=("")`) that flip "no inventory" into "match everything"

**Given** the checklist is published,
**Then** it cross-references the Epic 11 retrospective Pattern 3 + the `feedback_tolerance_idioms.md` memory so future agents loading the memory know where the canonical guidance lives.

**Given** the checklist exists,
**When** a future PR introduces one of the listed patterns,
**Then** a reviewer can point at the checklist as the basis for requesting a change instead of relitigating the rationale.

### Story 12.3: Story-File-Scope Enforcement (A4)

As a sprint discipline owner,
I want diffs to be checked against the originating story's `File Scope` declaration,
So that the D5-shape file-scope leak from Epic 11 (runtime `.cs` changes shipped under a CI/release story) cannot recur silently.

**Acceptance Criteria:**

**Given** every story file in `_bmad-output/implementation-artifacts/` has a `File Scope` section listing the file globs it is allowed to touch,
**When** a developer prepares a commit that references a story (via branch name, conventional commit footer, or explicit annotation),
**Then** an automated check (pre-commit hook OR CI check OR both) compares the staged diff against the story's `File Scope`,
**And** fails loudly when files outside the declared scope are touched.

**Given** legitimate cross-story stabilization sometimes requires touching files the story didn't anticipate,
**When** a developer needs an explicit override,
**Then** they can include a `Scope-Override:` line in the commit message naming the affected file(s) + a short rationale,
**And** the check passes when the override covers the out-of-scope files,
**And** the override is visible in `git log` for retrospective audit.

**Given** the check is in place,
**When** Epic 11's D5-shape scenario recurs (a CI/release story touching `src/**/*.cs`),
**Then** the check fires at PR / commit time, not at adversarial review time.

**Given** the check exists,
**Then** `CONTRIBUTING.md` documents how to declare `File Scope` correctly and how to use `Scope-Override:` legitimately.

### Story 12.4: Baseline Failures Sweep (A5)

As a quality owner,
I want every red test that the new `test-unit-contract` and `integration-fast` lanes will encounter against existing code to be either fixed or formally accepted with a re-open trigger,
So that the "baseline failures hiding under script-only execution" pattern from Epic 11 stops accumulating silently.

**Acceptance Criteria:**

**Given** the new CI lanes are running against `main`,
**When** the quality owner replays the lanes against recent stories' completion states (Epic 8.x, 9.x, 10.x history),
**Then** every additional pre-existing red test (beyond S11-FA `EmbeddingInputContentKindTests` already tracked) is identified and documented.

**Given** the sweep produces a list,
**When** each baseline failure is triaged,
**Then** each is either:

- (a) **fixed** in this story, with the fix anchored to the story that introduced the regression in `git log`, OR
- (b) **formally accepted** as an `S11-FX` style entry in `deferred-work.md` with: the test name, the story that introduced the regression, the rationale for accepting it, and an explicit re-open trigger, OR
- (c) **filtered** in the appropriate test-runner script with an inline comment pointing at the deferred-work entry.

**Given** the sweep completes,
**Then** `tools/test-release.ps1` baseline-filter list is the canonical source of all currently-accepted baselines,
**And** `CiTestInventoryTests` asserts that every entry in the filter has a corresponding deferred-work entry (or fails),
**And** zero baseline failures are unaccounted-for.

### Story 12.5: Partial-Publish Alerting (S11-FD)

As a release operations owner,
I want the half-published-then-network-failure scenario in `tools/publish-nuget.ps1` to produce an audible signal,
So that the `--skip-duplicate` self-healing model can be retained without it becoming an undetected silent-failure path.

**Acceptance Criteria:**

**Given** `tools/publish-nuget.ps1` is in the middle of pushing N packages,
**When** any individual `dotnet nuget push` fails with a non-`--skip-duplicate`-eligible error (network failure, auth failure, validation failure),
**Then** the script captures the failed package + error,
**And** completes the remaining package pushes (if a partial-success state is recoverable),
**And** emits a structured failure summary listing exactly which packages were pushed vs. failed.

**Given** the script runs in CI under `release.yml`,
**When** the failure summary indicates a partial publish,
**Then** an alert is raised — choose one based on Hexalith.Memories operations posture:

- (a) GitHub Issue auto-created in this repository with the failure summary, OR
- (b) Slack / equivalent webhook notification, OR
- (c) Failed workflow with explicit "PARTIAL PUBLISH — manual reconciliation required" annotation visible from the GitHub Actions UI.

**Given** the alert fires,
**Then** the alert text references `tools/publish-nuget.ps1` operator runbook section and lists the recovery procedure (re-run the workflow; `--skip-duplicate` self-heals to fully published).

### Story 12.6: EmbeddingInputContentKind Baseline Resolution (S11-FA)

As a quality owner,
I want the single tracked baseline filter currently in `tools/test-release.ps1` (`EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag`) to be resolved,
So that the baseline filter list returns to zero and any future addition to it is a deliberate, traceable event rather than a quiet drift.

**Acceptance Criteria:**

**Given** `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` is currently filtered out of `tools/test-release.ps1`,
**When** the test failure mode is investigated,
**Then** the root cause is documented (metric-tag contract drift? race condition? environment dependency? actual regression?).

**Given** the root cause is known,
**When** the team triages it,
**Then** one of the following happens:

- (a) The test is **fixed** and the filter entry is removed, OR
- (b) The test contract is **renegotiated** (the test was wrong; update the test or the contract; remove filter), OR
- (c) The behavior is **formally accepted** as an architectural choice and the test is removed (with rationale in code + commit + ADR if architecturally significant), OR
- (d) The test is **explicitly skipped** with a `[Trait("KnownFailure")]` + tracked deferred-work entry (interim only — not the desired end state).

**Given** resolution is achieved,
**Then** `tools/test-release.ps1`'s tracked-baseline list returns to zero entries,
**And** `CiTestInventoryTests` (per Story 12.4 AC) asserts that zero is the expected size when no `S11-FX` deferred entries are open.

### Optional follow-up stories (not included in initial scaffold; create only if their re-open trigger fires)

- **Story 12.7 — S11-FB compile-time symbol verification for `tools/integration-fast-required-surfaces.txt`** — re-open trigger: a surface drift slips past the runtime verifier, OR `IntegrationTests` compile dependencies become trivial enough to make the typed approach cheap.
- **Story 12.8 — S11-FC `release.yml` stale-tag preflight** — re-open trigger: a stale-tag collision actually bites on a real release attempt.

---

## Decision Point: Beyond Epic 12

**Status as of 2026-04-26:** Epic 12 is the operations-and-first-release epic selected by Sprint Change Proposal 2026-04-26 (Option C — Hybrid). Whatever comes after Epic 12 is **deliberately undecided** and depends on what the first release actually reveals.

The post-Epic-12 direction will be informed by:

1. **First real release outcome** — did `release.yml` execute end-to-end? What broke? What did the deferred-work backlog look right about / wrong about?
2. **Initial first-contributor friction** (if any) — does `CONTRIBUTING.md` survive contact with a real first-time contributor?
3. **Re-open triggers in `_bmad-output/implementation-artifacts/deferred-work.md`** — production observation may convert "Phase 2 candidate" entries into concrete next-epic story candidates.

### What NOT to do without an explicit Sprint Change Proposal

- **Do not add Epic 13+ here speculatively.** Any next-epic decision (continue Operations? pivot to Phase 2 features? declare project complete?) requires an explicit directional decision recorded in a new Sprint Change Proposal.
- **Do not assume the deferred-work backlog is the Phase 2 backlog.** It contains a mix of "fix when triggered," "Phase 2 candidate," and "operational follow-up" items. Phase 2 scoping (if pursued) requires explicit triage, not bulk import.
- **Do not promote optional stories (12.7 / 12.8) into the active scope without their re-open trigger having actually fired.** Adding them speculatively contradicts Epic 11 retrospective Action A4 (story-file-scope discipline).

> **Update 2026-04-29:** the Decision Point above was resolved by Sprint Change Proposal 2026-04-29 (`_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md`). Epic 13 — Embedding Provider Pluggability + Vector Migration — has been formally accepted as the next epic, driven by the operator's decision to migrate the embedding pipeline from Google Generative Language API to a self-hosted Ollama gateway protected by Keycloak. Epic 12 work continues in parallel; Epic 13 is independent of Epic 12 outcomes.

---

## Epic 13: Embedding Provider Pluggability + Vector Migration

Extend the existing `IEmbeddingProvider`-shaped abstraction (originally built into Stories 1.4 / 1.7 for exactly this kind of growth) so the embedding pipeline can target a self-hosted Ollama gateway protected by Keycloak OIDC client_credentials, retain Google as an opt-in cloud provider, and migrate existing tenants' Redis Vector Search indexes from 768-dimension Google vectors to 2560-dimension Ollama vectors. This epic delivers cost / sovereignty / latency control for the embedding workload (operator's primary motivation) and proves the multi-provider extensibility that PRD §"Embedding Provider Configuration" promised but Story 1.7 deferred.

**Driven by:** Sprint Change Proposal 2026-04-29 (`_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md`). Trigger: operational architecture review on 2026-04-29 after Epics 1–6 closed; operator has a self-hosted Ollama instance reachable at `https://llm.tache.ai` serving `qwen3-embedding:4b` (2560-dim) and a Keycloak realm `tache` ready to mint service-account access tokens for the `memories-embedding` confidential client.

**In scope:**

- Multi-provider validation in `EmbeddingProviderDefaults` (accepts `google` and `ollama`).
- Ollama-native HTTP request / response shape in `EmbeddingClient` (`POST {BaseUrl}/api/embed` with `{model, input}` payload, parses `embeddings[0]`).
- OIDC client_credentials token acquisition + cache + refresh + 401-retry — new `OidcTokenProvider` consumed by `EmbeddingClient` when `AuthMode = oidc-client-credentials`.
- `TenantEmbeddingConfig` non-breaking additive fields: `BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, `OidcScope`. Existing Google tenants continue to work without re-provisioning.
- `TenantConfigurationActor` surfaces and persists the new fields; state migration is non-destructive.
- Vector migration tool that drops `{tenantId}:semantic` indexes and replays ingestion at the new dimension count, with dry-run + per-tenant progress reporting.
- Integration test fixtures + Aspire wiring updated to cover both providers.
- Operator-facing deployment guide at `docs/operations/embedding-providers.md` documenting the gateway contract (Ollama-native HTTP API + Bearer JWT + JWKS validation + audience claim) with anonymized example config and a complete `TenantEmbeddingConfig` field table per provider option (Google api-key, Ollama OIDC, Ollama local-no-auth).

**Out of scope:**

- Path B vector migration (concurrent versioned `{tenantId}:google-768:semantic` + `{tenantId}:ollama-2560:semantic` indexes for live coexistence). Path A — drop-and-reindex — is the default, given current data volume is "test-data" per repo. Path B is documented as a per-tenant operator option but not built.
- Replacing the DAPR Conversation API path (LLM chat for `GenerateNaturalLanguageDescriptionActivity`). That stays unchanged — the `embedding` and `chat` axes are decoupled in the architecture.
- cert-manager installation. Wildcard TLS `*.tache.ai` (already provisioned as `tache-ai-tls`) remains operator-managed.
- Multi-node / GPU-sharing for Ollama.
- AC amendments to Stories 1.4 / 1.7 / 5.1 / 5.5 are documented in the Sprint Change Proposal but executed at the **architecture / PRD edit layer** — those stories are already `done` and we do NOT re-open them. Their AC text in `epics.md` is updated in lockstep with this epic landing (handled by the sprint-change-proposal acceptance pass), and Epic 13's stories are the carrier for the actual code change.

**Cross-cutting expectations:**

- **Wire compatibility:** `TenantEmbeddingConfig` extensions are additive only. Existing serialized state (DAPR actor state, HTTP request/response bodies) deserializes cleanly with new fields defaulted to null.
- **Secrets discipline:** the OIDC `client_secret` lives in DAPR Secrets store under `apiSecretKeyName`, never in config or env. Issued Bearer JWTs are never persisted, never surfaced via APIs, never logged at Info+.
- **Default for new tenants:** flips from Google to Ollama once Epic 13 is shipped end-to-end (Story 13.4 lands the new default; Story 13.6 migrates existing tenants).

### Story 13.1: Extend EmbeddingProviderDefaults to Accept Ollama

As a backend developer,
I want `EmbeddingProviderDefaults` to recognize `ollama` as a valid provider name with sensible defaults for `qwen3-embedding:4b`,
So that downstream code (provisioning, validation, embedding-client dispatch) can dispatch to the Ollama path without any caller having to special-case provider strings.

**Acceptance Criteria:**

**Given** a `TenantEmbeddingConfig` with `Provider = "ollama"`,
**When** `EmbeddingProviderDefaults.Validate(config)` is called,
**Then** validation succeeds (no exception) when the config also carries a non-empty `Model`, `Dimensions > 0`, `RateLimitPerMinute > 0`, and a valid `ApiSecretKeyName`.
**And** this story does not require transport/authentication fields that are introduced by later tenant-configuration work; those fields are validated by the story that adds them.

**Given** the existing Google-default factory continues to work,
**When** `EmbeddingProviderDefaults.Google()` is called,
**Then** it returns the same record shape it has today (provider=google, model=gemini-embedding-001, dimensions=768, rateLimitPerMinute=1500, apiSecretKeyName=google-embedding-api-key, reindexRequired=false) — no observable change for existing callers.

**Given** an operator wants out-of-the-box Ollama defaults,
**When** they call a new `EmbeddingProviderDefaults.Ollama()` factory method,
**Then** it returns a `TenantEmbeddingConfig` populated with `Provider = "ollama"`, `Model = "qwen3-embedding:4b"`, `Dimensions = 2560`, a tenant-appropriate `RateLimitPerMinute` default (self-hosted has no provider quota; emit a sensible local default like 6000 — operator can override per tenant), and an `ApiSecretKeyName` placeholder (`memories-embedding-client-secret`) that operators wire to the DAPR Secrets store.

**Given** a `TenantEmbeddingConfig` with an unsupported provider name (e.g., `"openai"`, `"cohere"`),
**When** `Validate` is called,
**Then** it throws `ArgumentException` whose message lists exactly the supported provider names — currently `"google"` and `"ollama"` — so future-proofing is honest about MVP scope without claiming non-existent support.

**Given** the existing `Google_ShouldReturnCorrectDefaults` and `Validate_*` tests in `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`,
**When** the tests run after the change,
**Then** every existing test continues to pass without modification (no regressions to Google flow).

**Given** a new test class section covers Ollama,
**When** the suite runs,
**Then** it contains at minimum: `Ollama_ShouldReturnCorrectDefaults`, `Validate_OllamaProvider_ShouldNotThrow`, `Validate_OllamaWithEmptyModel_ShouldThrow`, `Validate_UnsupportedProvider_ErrorMessageListsSupportedProviders`.

### Story 13.2: Implement OidcTokenProvider

As a backend developer,
I want a thread-safe in-process OIDC token provider that performs `client_credentials` grants against Keycloak, caches the access token until 30 s before expiry, and invalidates + refreshes once on 401,
So that the embedding client can attach `Authorization: Bearer <jwt>` to every Ollama request without flooding Keycloak, leaking tokens, or breaking on routine expiry.

**Implementation Checkpoints:**

- Checkpoint A - token acquisition and cache core: first fetch, response parsing, cache keying, cache hit, refresh-before-expiry, typed acquisition exception, and no negative caching.
- Checkpoint B - invalidation and concurrency: 401 invalidation, forced refresh, per-key concurrency collapse, and deterministic cancellation behavior.
- Checkpoint C - transport, DI, and redaction hardening: singleton registration with typed `HttpClient`, retry/timeout policy, correlation ID propagation, and proof that `client_secret` and `access_token` never appear in logs or test snapshots.

This story may remain one tracked story for Epic 13 sequencing, but implementation and review must close each checkpoint independently before the story is accepted.

**Acceptance Criteria:**

**Given** a fresh `OidcTokenProvider` instance,
**When** `GetAccessTokenAsync(tokenEndpoint, clientId, clientSecret, scope?)` is called for the first time,
**Then** the provider POSTs `grant_type=client_credentials&client_id=...&client_secret=...&scope=...` (URL-encoded form body) to `tokenEndpoint`,
**And** parses the JSON response (`access_token`, `expires_in`, `token_type`),
**And** returns `access_token`,
**And** stores `(token, expiresAt = now + expires_in - 30s)` in an in-memory cache keyed by `(tokenEndpoint, clientId)`.

**Given** a cached entry exists and `now < expiresAt`,
**When** `GetAccessTokenAsync` is called again for the same `(tokenEndpoint, clientId)`,
**Then** the cached token is returned without any HTTP call (cache hit).

**Given** a cached entry exists and `now >= expiresAt`,
**When** `GetAccessTokenAsync` is called,
**Then** the entry is invalidated, a new token is fetched, and the new entry replaces the old one.

**Given** the embedding client receives a `401 Unauthorized` from Ollama,
**When** the client calls `OidcTokenProvider.InvalidateAndRefreshAsync(tokenEndpoint, clientId, clientSecret, scope?)`,
**Then** the cached entry is forcibly evicted,
**And** exactly one token-fetch request is issued,
**And** the returned token is cached.

**Given** two concurrent callers both observe a cache miss for the same `(tokenEndpoint, clientId)` at the same instant,
**When** both call `GetAccessTokenAsync`,
**Then** exactly one HTTP request is made (per-key concurrency guard via `SemaphoreSlim` or equivalent),
**And** both callers receive the same token.

**Given** the token endpoint returns a non-2xx response,
**When** `GetAccessTokenAsync` is called,
**Then** an `OidcTokenAcquisitionException` (typed) is thrown carrying the HTTP status, the response body (truncated to ≤ 1 KiB to avoid log floods), the token endpoint, the client ID, and a correlation ID,
**And** the cache is **not** populated (no negative caching).

**Given** the provider is registered in DI as a singleton with a typed `HttpClient`,
**When** `Program.cs` (or `ServiceCollectionExtensions`) wires it,
**Then** the typed `HttpClient` carries a Polly retry policy (3 attempts, exponential backoff, retry only on 5xx + transient network errors),
**And** the timeout is ≤ 10 s (Keycloak token endpoints are sub-second on a healthy stack; longer suggests a problem and we want fail-fast).

**Given** the `client_secret` and the issued `access_token`,
**When** the provider logs at any level,
**Then** **neither** value appears in the log output. Tests assert this with a Sink-based logger inspector.

**Given** unit-test coverage,
**When** `OidcTokenProviderTests` runs,
**Then** it covers cache-hit, cache-miss-fetch, refresh-before-expiry, 401-invalidate-and-retry, concurrent-callers-single-fetch, non-2xx-throws, secret-and-token-never-logged.

### Story 13.3: Extend EmbeddingClient to Support Ollama

As a backend developer,
I want `EmbeddingClient` to dispatch to the Ollama-native HTTP API when the tenant's provider is `ollama`, with `Authorization: Bearer <jwt>` injected from `IOidcTokenProvider`,
So that the existing `GenerateEmbeddingActivity` workflow lands tenant-aware embeddings against the new gateway with no caller-side changes.

**Acceptance Criteria:**

**Given** a tenant configured with `Provider = "ollama"`, `Model = "qwen3-embedding:4b"`, `Dimensions = 2560`, `BaseUrl = "https://llm.tache.ai"`, `AuthMode = "oidc-client-credentials"`, `OidcTokenEndpoint`, `OidcClientId`, `OidcScope`, `ApiSecretKeyName`,
**When** `EmbeddingClient.GenerateEmbeddingAsync(text, tenantId, ct)` is called,
**Then** it POSTs `{ "model": "qwen3-embedding:4b", "input": "<text>" }` (Ollama-native shape) to `{BaseUrl}/api/embed`,
**And** attaches `Authorization: Bearer <jwt>` from `IOidcTokenProvider.GetAccessTokenAsync(...)` (using the tenant's OIDC config + the resolved `client_secret`),
**And** parses the response as `{ "embeddings": [[...]] }` and returns `embeddings[0]` as `float[]`,
**And** asserts the returned vector length matches `config.Dimensions` (otherwise throws `EmbeddingApiException` with a clear "expected N got M" message).

**Given** the existing Google flow,
**When** the tenant is configured with `Provider = "google"`,
**Then** `EmbeddingClient` continues to use the existing Google path (URL build, `x-goog-api-key`, response shape) without modification — verified by the existing `EmbeddingClientTests` Google scenarios continuing to pass unchanged.

**Given** Ollama returns 401,
**When** `EmbeddingClient` receives the response,
**Then** it calls `IOidcTokenProvider.InvalidateAndRefreshAsync(...)` exactly once,
**And** retries the request once with the fresh token,
**And** if the second attempt also returns 401, throws `EmbeddingApiException` carrying status + truncated body + correlation ID without leaking the bearer or the secret.

**Given** the dispatcher logic,
**When** `Provider` is anything other than `"google"` or `"ollama"`,
**Then** `EmbeddingClient` throws `NotSupportedException` with a message listing the supported providers — defense in depth alongside `EmbeddingProviderDefaults.Validate`.

**Given** unit-test coverage,
**When** `EmbeddingClientTests` runs,
**Then** new Ollama-flow tests cover: request shape (URL + verb + body + bearer header injection), success response parsing, dimension-mismatch throws, 401-invalidate-and-retry succeeds, 401-twice throws, bearer never logged, request body never logs the full input text at Info+ (Debug-only with size cap).

### Story 13.4: Extend TenantEmbeddingConfig with Additive OIDC Fields

As a backend developer,
I want `TenantEmbeddingConfig` extended with non-breaking optional fields (`BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, `OidcScope`),
So that Ollama tenants can carry the OIDC config they need while existing Google tenants continue to deserialize without re-provisioning.

**Acceptance Criteria:**

**Given** the existing `TenantEmbeddingConfig` with `Provider`, `Model`, `Dimensions`, `RateLimitPerMinute`, `ApiSecretKeyName`, `ReindexRequired`,
**When** the new optional fields are added,
**Then** the record exposes additionally: `string? BaseUrl`, `string AuthMode = "api-key"`, `string? OidcTokenEndpoint`, `string? OidcClientId`, `string? OidcScope`.

**Given** historical serialized JSON payloads (existing tenant state) without the new fields,
**When** they are deserialized via `MemoriesJsonContext.Options`,
**Then** they deserialize successfully with the new fields defaulted (`null` for nullables, `"api-key"` for `AuthMode`).

**Given** `MemoriesJsonContext.cs` is the AOT serializer context,
**When** the config record is updated,
**Then** the source-generator-friendly attributes / JsonSerializable registrations remain valid and the project builds 0W/0E with `<EnableTrimming>true</EnableTrimming>` if applicable.

**Given** `EmbeddingProviderDefaults.Validate(config)`,
**When** `AuthMode = "oidc-client-credentials"`,
**Then** validation requires `BaseUrl`, `OidcTokenEndpoint`, `OidcClientId` to be non-empty (throws `ArgumentException` listing the missing field name when violated). `OidcScope` is optional.

**Given** `Validate(config)` with `Provider = "ollama"`,
**When** `AuthMode = "api-key"`,
**Then** validation requires `BaseUrl` to be non-empty (Ollama always needs a target URL — the `api-key` mode for Ollama is the documented "local-no-auth or upstream-API-key" path).

**Given** `ApiSecretKeyName` semantics,
**When** `AuthMode = "oidc-client-credentials"`,
**Then** it holds the DAPR Secrets store key for the OIDC `client_secret` value (not the API key) — documented in the XML doc comment on the property.

**Given** the existing `EmbeddingProviderDefaults.GetBreakingChangeFields(currentConfig, proposedConfig)`,
**When** `Provider`, `Model`, or `Dimensions` change,
**Then** the existing return list still surfaces those three (no behavior change). When `BaseUrl` changes between Ollama instances, `BaseUrl` is also added to the breaking-changes list (operator-controlled migration).

### Story 13.5: Surface New Fields via TenantConfigurationActor

As a backend developer,
I want `TenantConfigurationActor.GetEmbeddingConfigAsync()` (and the corresponding write paths) to surface and persist the new OIDC fields,
So that Ollama tenants can be provisioned, listed, and configured end-to-end through the existing actor surface without state loss.

**Acceptance Criteria:**

**Given** an existing tenant whose persisted actor state predates the new fields,
**When** the actor activates and reads its state,
**Then** deserialization succeeds with `BaseUrl=null`, `AuthMode="api-key"`, `OidcTokenEndpoint=null`, `OidcClientId=null`, `OidcScope=null` defaulted (non-destructive migration).

**Given** a new tenant is provisioned with an Ollama config,
**When** `TenantConfigurationActor.SetEmbeddingConfigAsync(config)` is called,
**Then** the new fields are persisted into actor state, round-trip cleanly across actor reactivations, and are returned by `GetEmbeddingConfigAsync()`.

**Given** the listing surface (Story 5.5's tenant configuration listing endpoint, server-side),
**When** the configuration is serialized for the public response,
**Then** `apiSecretKeyName` is exposed (name only, never value) and `oidcTokenEndpoint`, `oidcClientId`, `oidcScope`, `baseUrl`, `authMode`, `provider`, `model`, `dimensions` are exposed as plaintext config metadata, while the `client_secret` value resolved via DAPR is **never** exposed.

**Given** unit-test coverage,
**When** `TenantConfigurationActorTests` (or equivalent) runs,
**Then** new tests cover: actor state migration from old to new shape, Ollama-config round-trip, listing-surface masks the secret value but exposes the secret-name reference, GetBreakingChangeFields correctly flags BaseUrl changes for Ollama tenants.

### Story 13.6: Vector Migration Tool

As a system operator,
I want a console tool (or extension to `Hexalith.Memories.Cli`) that drops `{tenantId}:semantic` indexes and replays ingestion at the new dimension count, with a dry-run mode that lists affected tenants and content counts,
So that I can migrate existing Google tenants to Ollama without ad-hoc Redis CLI operations.

**Implementation Checkpoints:**

- Checkpoint A - dry-run and preflight: affected tenant discovery, current/target dimension reporting, content counts, backend reachability checks, and zero state mutation.
- Checkpoint B - live migration execution: tenant configuration mutation, semantic index drop/recreate, replay/re-embedding, per-batch progress, and final summary.
- Checkpoint C - interruption, resume, and rollback safety: idempotent resume by per-unit `EmbeddingProvider:Model`, partial-state detection, rollback behavior for retained versioned indexes, and failed-unit reporting.
- Checkpoint D - operator evidence: runbook command sequence, expected output, abort/resume semantics, and validation evidence from a controlled migration run or equivalent test fixture.

This story may remain one tracked story for Epic 13 sequencing, but implementation and review must close each checkpoint independently before the story is accepted.

**Acceptance Criteria:**

**Given** the migration tool,
**When** the operator invokes it in `--dry-run` mode against the live Redis instance,
**Then** the tool lists every affected tenant (those whose `Provider != target_provider`), the content unit count per tenant, and the current vs. target index dimension — without modifying any state.

**Given** the operator invokes the tool in `--live` mode for a specific tenant,
**When** the migration runs,
**Then** the tool: (a) updates the tenant's `TenantEmbeddingConfig` to the new provider/model/dimensions, (b) drops `{tenantId}:semantic`, (c) re-creates the index with the new dimension count, (d) replays ingestion (re-embeds every content unit) emitting per-batch progress (count + percent), (e) emits a final summary (tenant ID, units processed, units failed, elapsed time).

**Given** the migration is interrupted (Ctrl-C or process kill),
**When** the operator restarts the tool against the same tenant,
**Then** the tool detects partial state (some content units re-embedded, some not) and resumes from the unprocessed batch — does NOT re-embed already-migrated units (idempotent on the per-unit `EmbeddingProvider:Model` field check).

**Given** the rollback toggle,
**When** the operator passes `--rollback` for a tenant whose Path-B versioned indexes were retained,
**Then** the tool re-installs the previous-version index as the active `{tenantId}:semantic`. Path-B coexistence is **not** the default — this is documented as the operator-opt-in safety net.

**Given** documentation,
**When** the tool ships,
**Then** `docs/operations/embedding-providers.md` carries the migration runbook entry with the exact command sequence, expected output, and abort/resume semantics before the migration tool is considered complete.
**And** later integration/deployment-guide work may expand or validate the same runbook, but does not own the minimum operator documentation needed to ship this tool.

### Story 13.7: Integration Tests, Aspire Fixtures & Operator Deployment Guide

As a developer and operator,
I want the Aspire test fixtures + integration tests to exercise both provider paths (Google + Ollama) and a written deployment guide that documents the gateway contract end-to-end,
So that a new operator can stand up the Ollama gateway, wire Keycloak, configure a tenant, and verify the result against a documented expectation.

**Acceptance Criteria:**

**Given** the existing Aspire test fixtures,
**When** the test suite runs,
**Then** the embedding integration suite is parameterized over `provider in {google, ollama}` and both branches go green against either the existing Google fake or a newly-added Ollama-compatible HTTP fake (gated behind an env-flag for Tier-3 and using a stub for Tier-2). The Tier-2 stub returns deterministic 2560-dim vectors so consistency-verification tests can assert dimension-correctness.

**Given** the new `OllamaEmbeddingEndToEnd` integration test,
**When** it runs against an Aspire-hosted Ollama-compatible HTTP fake + Keycloak fake (or Wiremock-with-OIDC-stub),
**Then** it exercises: provisioning an Ollama tenant, ingesting one content unit, verifying the persisted embedding has 2560 dimensions, verifying the stored `EmbeddingProvider` field is `ollama:qwen3-embedding:4b`, verifying that hybrid search returns the unit.

**Given** the operator-facing deployment guide at `docs/operations/embedding-providers.md`,
**When** an operator reads it,
**Then** it documents:
- The gateway contract: Ollama-native HTTP API (`POST /api/embed` with `{model, input}` → `{embeddings: [[...]]}`), Bearer JWT with audience claim, JWKS validation expectations.
- A generic anonymized Envoy + Ollama stack example with placeholders (`{ISSUER}`, `{AUDIENCE}`, `{JWKS_URL}`, `{HOSTNAME}`).
- The complete `TenantEmbeddingConfig` field table per provider option: Google api-key, Ollama OIDC, Ollama local-no-auth.
- The Story 13.6 migration runbook entry, preserving the command sequence, expected output, and abort/resume semantics already required by Story 13.6.
- The Keycloak client setup recipe (realm, client ID, audience mapper, service-accounts-enabled, access-token-lifespan, scopes).
- The DAPR Secrets store entry layout (`memories-embedding-client-secret` per tenant or shared, operator's choice).

**Given** the existing `docs/dev/embedding-providers.md` (or equivalent dev-facing notes),
**When** Story 13.7 lands,
**Then** the developer-facing documentation cross-references the new operator guide and notes the dual-mode (api-key vs. oidc-client-credentials) decision matrix.

---

## Epic 14: Deferred Work Hardening and Operational Readiness

**Lifecycle label:** Operational Readiness / Release Hardening.

Developer and operator can close the highest-value deferred review findings without reopening completed epics, improving CI correctness, release integrity, OIDC/embedding security, migration reliability, and deferred-work governance.

**Preflight required:** Before implementation starts, verify every referenced deferred ID, retrospective item, review finding, and sprint-change proposal path still exists. If a reference is stale, update the story before implementation begins.

**FRs reinforced:** FR43, FR56, FR57, FR67, FR68, FR69, FR70, FR72, FR73, FR74
**NFRs reinforced:** NFR8, NFR9, NFR10, NFR11, NFR17, NFR18, NFR19, NFR22, NFR27, NFR28, NFR30, NFR31

### Story 14.1: CI Story-Scope Enforcement Hardening

As a maintainer,
I want story-scope validation and CI diff discovery to fail loudly and parse story keys consistently,
So that future feature work cannot bypass file-scope enforcement through shallow fetches, malformed story keys, or ambiguous branch metadata.

**Acceptance Criteria:**

**Given** the CI story-scope job fetches the comparison base,
**When** the fetch fails because of auth, network, repository rename, or unavailable refs,
**Then** the workflow fails loudly with a diagnostic that names the failed fetch operation
**And** it does not continue into a degraded `git diff-tree -r HEAD` fallback caused by `|| true`.

**Given** the workflow runs on a push to `main`,
**When** the calculated diff is empty or `origin/main` resolves to the same commit as `HEAD`,
**Then** the story-scope check fails with a direct-push/empty-diff diagnostic
**And** it does not silently pass file-scope validation.

**Given** branch metadata or explicit `--story-key` input contains more than one story key,
**When** `tools/check-story-file-scope.py` parses it,
**Then** validation rejects the input consistently with trailer multi-key rejection
**And** reports all detected conflicting keys.

**Given** `git interpret-trailers` is unavailable,
**When** the story-scope validator needs trailer parsing,
**Then** it raises a clean validation error with an actionable installation/path message
**And** no raw `FileNotFoundError` stack trace is emitted.

**Given** the story-scope validator parses story files,
**When** boundary cases are exercised for `STORY_KEY_PATTERN`, code fences, backtick paths, allow-list termination, and diagnostics,
**Then** focused tests cover those cases using Shouldly/xUnit or the existing Python test harness as appropriate
**And** all existing story-scope tests remain green.

**Given** Story 14.1 closes deferred work,
**When** the story is marked done,
**Then** deferred IDs 12.4-RV1 through 12.4-RV5, 12.4-RV7 through 12.4-RV18, and any implemented related 12.3 parser findings are removed from `deferred-work.md` or marked resolved with validation evidence.

### Story 14.2: Release Pipeline Audit Hardening

As a release maintainer,
I want release workflow and package validation guardrails strengthened,
So that package publication, stale tags, release evidence, and package inventory drift are caught before they can create ambiguous release states.

**Acceptance Criteria:**

**Given** release workflow hardening is applied,
**When** `.github/workflows/release.yml` is reviewed,
**Then** action pinning, stale-tag handling, and partial-publish signal behavior are explicitly decided and either implemented or documented with a new defer-by date.

**Given** package validation runs,
**When** `tools/validate-release-packages.ps1` scans `src/**/*.csproj`,
**Then** every packable and non-packable project is accounted for in release package inventory
**And** direct operator version inputs with build metadata fail or normalize with a clear message.

**Given** release evidence is collected,
**When** `docs/dev/release-runbook.md` is updated,
**Then** package evidence includes checksum or equivalent audit evidence for newly validated packages
**And** the release bot identity is pinned enough for future forensic review.

**Given** `tools/release-packages.json` is edited,
**When** validation runs,
**Then** schema validation or a schema reference catches misspelled package fields before publish-time scripts run.

**Given** CI inventory tests guard release lanes,
**When** workflow text is parsed,
**Then** tests avoid broad substring matching where a structural or narrower assertion is feasible.

### Story 14.3: OIDC and Embedding Security Hardening

As a system operator,
I want OIDC token acquisition and embedding-client error handling hardened,
So that cancellation, credential rotation, malformed URLs, token refresh storms, and transport errors do not leak secrets or produce avoidable outages.

**Acceptance Criteria:**

**Given** several callers wait on the same OIDC token acquisition,
**When** the caller that started the fetch cancels,
**Then** remaining waiters are not forced to refire the HTTP token request solely because the leader cancelled.

**Given** OIDC and embedding clients are registered in DI,
**When** their HttpClient lifetime is inspected,
**Then** the implementation follows the chosen `IHttpClientFactory` or typed-client pattern without singleton-captured stale handlers.

**Given** provider URLs and OIDC token endpoints are validated,
**When** a URL contains userinfo such as `https://user:pw@host`,
**Then** validation rejects it for both `OidcTokenProvider` and `EmbeddingProviderDefaults`.

**Given** several callers force-refresh the same token concurrently,
**When** invalidation occurs,
**Then** refresh requests collapse where practical or are explicitly bounded and covered by tests.

**Given** OIDC or embedding transport fails because of network, timeout, or IO errors,
**When** the caller receives the failure,
**Then** it is wrapped in the typed exception expected by the higher-level retry and classification code
**And** secret values and bearer tokens are not present in exception text or logs.

**Given** an Ollama tenant's DAPR secret has rotated,
**When** the first request returns 401 or 403 and retry is attempted,
**Then** stale `client_secret` cache state is evicted symmetrically with the Google API-key path.

**Given** redaction handles sensitive values,
**When** values overlap, are short, or appear in upstream error payloads,
**Then** redaction is length-aware, longest-value-first, and tested with realistic OIDC and embedding failure text.

### Story 14.4: Migration and Integration Test Hardening

As a maintainer and operator,
I want migration and Aspire integration tests hardened,
So that provider migration evidence remains stable under CI pressure and malformed fake-server input cannot weaken coverage silently.

**Acceptance Criteria:**

**Given** migration service expected-failure paths are refactored,
**When** options or tenant migration results are invalid,
**Then** business failures use `ValueOrError<T>` where appropriate or retain exceptions with a documented, focused reason.

**Given** migration redaction is expanded,
**When** AWS access keys, raw JWTs, HTTP Basic auth, and approved secret-value shapes appear in captured payloads,
**Then** the redactor masks them without masking benign secret-name references unless the story explicitly chooses stricter behavior.

**Given** the Ollama integration test waits for Redis state,
**When** a bounded targeted alternative exists,
**Then** it no longer uses Redis `KEYS` polling in the 3-minute loop.

**Given** Aspire fixture DAPR config files are created in temp directories,
**When** the fixture disposes or initialization fails,
**Then** generated config files and parent temp directories are cleaned up.

**Given** the Ollama OIDC fake server rejects malformed token requests,
**When** tests run,
**Then** dedicated theory cases cover missing content type, missing grant type, missing client ID, missing client secret, duplicate form values, and malformed body branches.

**Given** provider integration assertions depend on expected embedding call counts,
**When** the raw + natural-language embedding path is asserted,
**Then** magic numeric thresholds are replaced with named constants or clearer assertions.

### Story 14.5: Deferred Register Governance and Sprint-Status Hygiene

As a maintainer,
I want deferred-work entries and sprint-status history to stay auditable,
So that future planning can distinguish open risk, resolved risk, accepted risk, and stale historical noise without manual archaeology.

**Acceptance Criteria:**

**Given** `deferred-work.md` remains the canonical deferred register,
**When** new or migrated entries are written,
**Then** each entry has a minimal consistent structure for ID, status, source story, target artifact, and re-open trigger.

**Given** Epic 14 stories resolve deferred items,
**When** each story completes,
**Then** its targeted deferred entries are updated as `resolved`, `accepted`, or `carried-forward` with validation evidence or rationale.

**Given** `sprint-status.yaml` records history,
**When** future status updates are appended,
**Then** guidance avoids unbounded one-line history comments and prefers concise dated notes.

**Given** tests or scripts parse deferred-work entries,
**When** the register structure changes,
**Then** those tests or scripts are updated to parse the new structure without broad author-controlled substring heuristics.

**Given** this governance story touches planning and tracking files,
**When** it is implemented,
**Then** it avoids submodule pointer changes and follows root-declared `references/` submodule discipline.

## Epic 15: Carry-Forward Operational Risk Closure

**Lifecycle label:** Operational Readiness / Release Hardening.

Maintainers and operators can convert the remaining high-value carry-forward risks from Epic 14 into planned implementation, acceptance, or refreshed deferral decisions without reopening completed epics.

**Preflight required:** Before implementation starts, verify every referenced deferred ID, retrospective item, review finding, and sprint-change proposal path still exists. If a reference is stale, update the story before implementation begins.

**FRs reinforced:** FR43, FR56, FR57, FR67, FR68, FR69, FR70, FR72, FR73, FR74
**NFRs reinforced:** NFR8, NFR9, NFR10, NFR11, NFR17, NFR18, NFR19, NFR22, NFR27, NFR28, NFR30, NFR31

### Story 15.1: Release Edge-Case Preflight Hardening

As a release maintainer,
I want stale-tag and skip-CI edge cases handled before release execution,
So that releases do not fail late, silently skip, or leave ambiguous audit evidence.

**Acceptance Criteria:**

**Given** stale release tags can collide with `tagFormat: "v${version}"`,
**When** release preflight behavior is reassessed,
**Then** deferred ID `S11-FC` is resolved with a concrete preflight, accepted with refreshed rationale, or carried forward with a new defer-by date and trigger.

**Given** release workflow skip logic reads commit messages,
**When** a PR merge or squash body contains `[skip ci]` as quoted text,
**Then** deferred ID `12.1-RV3` is resolved by documentation, tests, or workflow guardrails, or explicitly accepted with rationale.

**Given** release tooling depends on Node package restore,
**When** release package validation is reviewed,
**Then** deferred ID `12.1-RV4` is resolved by confirming `package-lock.json` tracking and fresh-clone behavior, or carried forward with a concrete owner and trigger.

**Given** release-hardening decisions change workflow, runbook, or tooling behavior,
**When** the story completes,
**Then** focused validation covers the changed behavior and `deferred-work.md` records `resolved`, `accepted`, or `carried-forward` evidence for every targeted ID.

### Story 15.2: Provider Model Dimension Registry

As a system operator,
I want provider, model, and vector-dimension validation to use a centralized registry,
So that invalid or cross-pollinated embedding configurations fail before tenant state or indexes drift.

**Acceptance Criteria:**

**Given** provider/model/dimension combinations are validated,
**When** `EmbeddingProviderDefaults.Validate(...)` receives Google, Ollama, or future provider input,
**Then** validation is driven by one provider-to-model registry that owns allowed models, dimensions, and provider-specific limits.

**Given** dimension values can be unbounded today,
**When** a proposed config uses `Dimensions = int.MaxValue` or another out-of-policy dimension,
**Then** deferred ID `13.1-RV6` is resolved by a shared upper bound and tests that fail fast at config time.

**Given** cross-provider model names can accidentally validate,
**When** configurations mix provider and model families, such as Google with `qwen3-embedding:4b` or Ollama with `gemini-embedding-001`,
**Then** deferred ID `13.1-RV11` is resolved with negative tests and no special-case downstream parser assumptions.

**Given** casing and persistence can affect comparisons,
**When** provider/model values round-trip through tenant configuration,
**Then** deferred IDs such as `13.1-RV10` and `13.3-RV8` are either resolved by documented normalization/equality semantics or accepted with rationale.

**Given** this story touches tenant configuration validation,
**When** it completes,
**Then** contract/server tests cover success, invalid-provider, invalid-model, invalid-dimension, and cross-provider negative paths, and `deferred-work.md` is updated for all targeted IDs.

### Story 15.3: Live Migration Coordination Policy

As a system operator,
I want live embedding-vector migration to coordinate with concurrent ingestion,
So that a tenant cannot finish migration with mixed provider/model vector state.

**Acceptance Criteria:**

**Given** migration currently updates tenant config before enumerating syntactic units,
**When** ingestion starts or resumes during migration,
**Then** deferred ID `13.6-RV1` is resolved by a defined coordination policy such as tenant migration lock, ingestion pause/drain, migration-aware ingestion routing, or a deliberately accepted operational constraint.

**Given** the migration service exposes operator-visible failures,
**When** expected business failures are represented,
**Then** deferred ID `13.6-RV3` is resolved with `ValueOrError<T>` or equivalent project-approved result semantics, or accepted with a specific architectural rationale.

**Given** migration coordination changes runtime or operator behavior,
**When** tests run,
**Then** coverage proves no new old-provider vectors are written after the migration cutover point, or the accepted policy is enforced and documented.

**Given** operator guidance is part of the safety contract,
**When** the story completes,
**Then** `docs/operations/embedding-providers.md` or the migration runbook documents the coordination policy, abort/resume expectations, and any ingestion downtime requirement.

### Story 15.4: Token Endpoint Transport Policy

As a security-conscious operator,
I want OIDC token endpoint transport rules to distinguish local development from production,
So that production token acquisition cannot silently use insecure transport.

**Acceptance Criteria:**

**Given** local Keycloak and fake-server tests may use `http://localhost`,
**When** token endpoint validation sees loopback HTTP endpoints,
**Then** the local/development path remains explicitly supported and covered by tests.

**Given** production token endpoints carry client credentials,
**When** a non-loopback `http://` token endpoint is configured outside an explicitly allowed local/test context,
**Then** deferred ID `13.2-RV4` is resolved by rejecting it with a sanitized, actionable error.

**Given** provider base URLs and OIDC token endpoints are operator-facing configuration,
**When** transport policy is documented,
**Then** docs name the allowed schemes, local exceptions, production expectations, and secret-redaction guarantees.

**Given** validation errors can include endpoint text,
**When** invalid transport is rejected,
**Then** tests assert no embedded credentials or token-like values leak in errors, logs, or snapshots.

### Story 15.5: Deferred Register Triage Sweep

As a maintainer,
I want a bounded sweep of remaining deferred entries,
So that historical noise, consciously accepted risks, and true backlog candidates are separated before the next implementation epic.

**Acceptance Criteria:**

**Given** `deferred-work.md` still contains historical prose entries,
**When** this story runs,
**Then** entries selected for active planning are migrated to the Story 14.5 structured schema without bulk-rewriting unrelated history.

**Given** some remaining items are quality hardening rather than architectural decisions,
**When** the sweep selects candidates,
**Then** items such as `12.4-RV20`, `12.6-RV5`, and `Story-9.3-ProjectionRegistryCrossCheck` are either promoted into named future stories, accepted with rationale, or carried forward with refreshed triggers.

**Given** the Epic 14 retrospective named carry-forward risks,
**When** the sweep completes,
**Then** the retrospective's remaining-risk list is reconciled with the current deferred register, including the already-resolved `13.7-RV4` repository-root helper cleanup.

**Given** the backlog should remain actionable,
**When** this story closes,
**Then** it proposes no more than five follow-up stories, each with explicit deferred IDs, target artifacts, and validation expectations.

### Story 15.6: Scaffolding Hardening Sweep

**Implementation Checkpoints:**

- Checkpoint A — AppHost boot orchestration: `OnResourceReady` rewrite for the DAPR sidecar start-event, per-invocation temp directory for component YAML generation, and concurrent-AppHost-run isolation for `statestore.yaml` and `pubsub.yaml`.
- Checkpoint B — Submodule guard expansion: `Directory.Build.props` `CheckSubmodules` MSBuild target validates every `.gitmodules` entry under `references/` (`references/Hexalith.Commons`, `references/Hexalith.EventStore`, `references/Hexalith.AI.Tools`, `references/Hexalith.Tenants`, `references/Hexalith.FrontComposer`, `references/Hexalith.Builds`, `references/Hexalith.PolymorphicSerializations`).
- Checkpoint C — Health-check and DAPR-template hardening: `ServiceDefaults.AddDefaultHealthChecks` returns 503 from `/ready` when Redis is unreachable, `memories-server` waits for `secretstore` and `llm` components, and the production DAPR templates (`statestore.yaml`, `secretstore.yaml`, `conversation-llm.yaml`) ship with correct env-var interpolation, volume mounts, and Conversation API metadata keys.
- Checkpoint D — Story 1.1 Scope-Override and regression coverage: AppPort=5000 spec receives the Scope-Override block recording the Aspire-Testing port-randomization decision, Completion Notes amended, and targeted regression coverage exercises the expanded submodule guard, the ready-tagged Redis health check, and the component-file rewrite ordering invariant.

This story may remain one tracked story for Epic 15 sequencing, but implementation and review must close each checkpoint independently before the story is accepted. Per the Engineering/Operational Readiness Track preamble, individual checkpoints that cannot be applied may be deferred or accepted with rationale recorded in `deferred-work.md`.

As a maintainer,
I want the 15 patch findings from the 2026-05-16 fresh re-review of Story 1.1's scaffolding to be triaged and applied with proper file scope,
So that the AppHost boot orchestration, ServiceDefaults health/telemetry surface, and DAPR component templates are hardened without retroactively flipping the released Story 1.1 to `in-progress`.

**Acceptance Criteria:**

**Given** the 15 patch findings recorded in `_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md` under `### Review Findings (Re-Review 2026-05-16)`,
**When** this story runs,
**Then** each finding is either applied, downgraded to defer with rationale captured in `deferred-work.md`, or explicitly dismissed in this story's Review Findings section.
**And** no finding is silently dropped.

**Given** the AppHost generates DAPR component YAML at runtime,
**When** the implementation lands,
**Then** the sidecar start-event awaits the `OnResourceReady` rewrite, not just the Redis PING,
**And** concurrent AppHost runs use a per-invocation temp directory so two `dotnet run` invocations cannot corrupt each other's `statestore.yaml` or `pubsub.yaml`.

**Given** `Directory.Build.props` is the build gate for missing submodules,
**When** the implementation lands,
**Then** the `CheckSubmodules` MSBuild target validates every entry in `.gitmodules`: `references/Hexalith.Commons`, `references/Hexalith.EventStore`, `references/Hexalith.AI.Tools`, `references/Hexalith.Tenants`, `references/Hexalith.FrontComposer`, `references/Hexalith.Builds`, and `references/Hexalith.PolymorphicSerializations`.

**Given** `ServiceDefaults.AddDefaultHealthChecks` is the canonical health-check entry point,
**When** the implementation lands,
**Then** `/ready` returns 503 when Redis is unreachable,
**And** the AppHost `memories-server` resource waits for the `secretstore` and `llm` DAPR components in addition to `redis` and `falkordb`.

**Given** the production `deploy/dapr/components/statestore.yaml`, `secretstore.yaml`, and `conversation-llm.yaml` templates ship to Kubernetes deployments,
**When** the implementation lands,
**Then** the statestore template uses env-var interpolation for `redisPassword`, the secretstore template uses an absolute path with a documented volume mount, and the conversation component uses the DAPR Conversation API's documented `cacheTTL` metadata key.

**Supersession note:** D31 and Epic 29 supersede Story 15.6 only for the `secretstore.yaml` provider decision. A secret-store template used by an Aspire or deployed runtime must use an OpenBao-backed DAPR component rather than `secretstores.local.file` or `secretstores.kubernetes`. Kubernetes Secrets may supply only documented bootstrap tokens/CA material or unavoidable direct pod inputs. The statestore and conversation-component requirements remain unchanged, and Story 15.6 remains historical completed work.

**Given** Story 1.1's spec calls for `AppPort=5000` in `WithDaprSidecar()` but the current code intentionally omits it for Aspire-Testing port randomization,
**When** this story runs,
**Then** Story 1.1's spec receives a Scope-Override block recording the testability decision and amends the Completion Notes accordingly.
**And** no code change to AppPort handling is required by this story.

**Given** safety regressions are easy to introduce in boot orchestration,
**When** the implementation lands,
**Then** targeted regression coverage exercises the expanded submodule guard, the ready-tagged Redis health check, and the component-file rewrite ordering invariant.

## Epic 16: Projection Registry Cross-Check Hardening

**Lifecycle label:** Operational Readiness / EventStore Integration Hardening.

Maintainers and operators can close the Story 9.3 projection-registry gap by comparing EventStore routing declarations with the projection bindings that tenant application code actually exposes at runtime.

**Preflight required:** Before implementation starts, verify `Story-9.3-ProjectionRegistryCrossCheck` still exists in `_bmad-output/implementation-artifacts/deferred-work.md`, confirm Story 15.5 still carries it forward, and inspect the current `HandlerMismatchDetector`, `HandlerRegistryService`, handler CLI/REST clients, and EventStore projection discovery APIs before designing the patch.

**FRs reinforced:** FR62
**NFRs reinforced:** NFR8, NFR17, NFR19, NFR27, NFR31

### Story 16.1: Projection Registry Cross-Check Design

As a system operator,
I want handler mismatch detection to compare routing declarations with runtime-bound projection bindings,
So that events can no longer look "handled" from routing configuration while silently lacking a projection consumer.

**Acceptance Criteria:**

**Given** handler mismatch detection currently treats `EventStoreIntegration:Routing:SourceToTenantMap` as the registration source of truth,
**When** this story designs and implements the projection cross-check,
**Then** the implementation defines an explicit repository-owned projection binding contract that can represent tenant, source prefix or aggregate, projection type/name, and supported event/aggregate patterns without mutating the `Hexalith.EventStore` submodule.

**Given** EventStore client discovery already exposes projection metadata through `DiscoveryResult.Projections`,
**When** the implementation chooses a projection registry shape,
**Then** it reuses existing EventStore discovery concepts where compatible or records a clear rationale for a Memories-owned adapter, and it does not add a broad new dependency or reflection scanner without tests proving the need.

**Given** a tenant has a `SourceToTenantMap` entry but the runtime projection registry has no matching projection binding,
**When** `HandlerMismatchDetector.DetectAsync` runs,
**Then** the report includes an actionable warning for the configured-but-unbound projection path without regressing existing `UnhandledEventType`, `StaleHandler`, or `VersionMismatch` behavior.

**Given** a tenant has both routing and matching projection bindings,
**When** observed event types match the configured aggregate/source prefix,
**Then** mismatch detection remains healthy and does not emit the new projection-binding warning.

**Given** this story may extend the experimental HXL002 API shape,
**When** it changes `HandlerMismatchCategory`, `HandlerMismatchReport`, `HandlerRegistration`, CLI formatting, or REST client behavior,
**Then** the change is additive, serialized through `MemoriesJsonContext`, covered by contract/CLI/server tests, and preserves existing JSON property names and CLI filtering semantics.

**Given** projection registry data may be absent in deployments that have not opted into the new contract,
**When** the registry has no bindings,
**Then** the failure posture is explicit: either report projection bindings as unknown/disabled without false warnings, or emit warnings only when the operator has configured the registry as authoritative.

**Given** the deferred work entry is the source of this story,
**When** the story completes,
**Then** `_bmad-output/implementation-artifacts/deferred-work.md` marks `Story-9.3-ProjectionRegistryCrossCheck` as `resolved`, `accepted`, or `carried-forward` with evidence or rationale, and focused validation covers the selected disposition.

## Epic 17: Future Web UX Composition & Accessibility

**Lifecycle label:** Future Web UI / UX Accessibility.

Future web users can inspect evidence, scope, sources, graph context, case activity, operator health, benchmark results, and MCP packets through FrontComposer/Fluent UI compositions with responsive and accessible behavior.

**Scope note:** This epic records story coverage for UX Design Specification requirements that are explicitly future web UI work. It is not part of MVP readiness unless a later approved sprint change pulls web UI implementation forward.

**UX implementation boundary:** Epic 17 web work is FrontComposer-first and Fluent UI Blazor V5-only. Components must consume FrontComposer shell/composition primitives and Fluent UI Blazor V5 primitives before creating Memories-specific wrappers. Raw semantic markup and scoped CSS may be used only for unavoidable container/layout gaps with no component equivalent, and must not recreate Fluent theme primitives, controls, status treatments, typography ramps, color roles, or spacing systems. Any exception requires an explicit conformance-test allowlist entry and removal condition.

**Readiness note:** Story 17.6 is the current conformance gate for the host-less web RCL. It does not close product-route browser, axe, forced-colors, reduced-motion, zoom/reflow, touch, or manual screen-reader validation gaps. Story 17.7 is the scheduled browser/assistive-technology gap-closure story for those fail-closed dimensions.

**Execution gate:** Any future Epic 17 implementation or reopened Story 17.2-17.5 work must verify Story 17.6 completion evidence first and must reuse its conformance tests. If `story_execution_order` is present in sprint status, tooling must treat 17.6 as the preflight regardless of numeric suffix.

**Query-pipeline note (2026-07-25):** When web question-answering is pulled forward, adopt a planner → executor → synthesis pipeline over the Evidence Packet (a lightweight planning pass selects axes/tools; the executor fans out in parallel and normalizes results into the Evidence Packet; synthesis answers with citations and caveats), per the Cerebras knowledge-base findings (`research/cerebras-knowledge-base-findings-2026-07-25.md`, finding D6).

**UX-DRs covered:** UX-DR5, UX-DR6, UX-DR15, UX-DR16, UX-DR20, UX-DR21, UX-DR22, UX-DR23, UX-DR24, UX-DR26, UX-DR27, UX-DR29, UX-DR30, UX-DR31, UX-DR32, UX-DR33, UX-DR34, UX-DR35, UX-DR36, UX-DR37, UX-DR38, UX-DR39, UX-DR40

### Story 17.1: Evidence Cockpit and Trust Components

As a developer or team lead,
I want a FrontComposer/Fluent UI Evidence Cockpit with Evidence Packet, Trust Strip, Scope Header, source, axis, and graph summaries,
So that I can verify answers, sources, retrieval reasons, scope, and graph context in one inspectable web workflow.

**Acceptance Criteria:**

**Given** the web Evidence Cockpit is opened for a tenant and case
**When** a search or briefing response is displayed
**Then** the page composes the shared Evidence Packet contract without inventing new confidence, state, omitted-detail, source, graph, or recovery semantics
**And** tenant and case scope are visible before the query or briefing content.

**Given** an Evidence Packet is displayed
**When** the Trust Strip renders
**Then** it shows tenant, case, confidence state, freshness state, source count, evidence health, and token-budget indicator when applicable
**And** each state has a text label and accessible name, not color alone.

**Given** a result has sources, axis scores, or graph context
**When** the user expands evidence details
**Then** Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary components expose source type, origin identifier, freshness, score normalization, ranking reason, edge type, confidence, gap markers, and chronological ordering as available from the contract.

**Given** the UI uses FrontComposer and Fluent UI Blazor
**When** controls, panels, tabs, grids, or menus are needed
**Then** existing primitives are used before custom controls
**And** custom Memories components remain contract-aware and tenant-aware.

### Story 17.2: Recovery and Feedback State Grammar

As a developer or operator,
I want weak, empty, stale, degraded, unauthorized, compressed, and conflicting evidence states to show clear recovery guidance,
So that I can decide the next safe action without leaving the current workflow.

**Acceptance Criteria:**

**Given** an Evidence Packet is empty, weak, stale, degraded, unauthorized, compressed, or disputed
**When** the state is displayed
**Then** the UI shows a clear state title, explanation, diagnostic clue, severity, affected capability, and one safest recovery action
**And** optional secondary actions are available without hiding the primary recovery path.

**Given** no-result or low-evidence states occur
**When** the Recovery Action Panel renders
**Then** it distinguishes no match, not ingested yet, wrong case, inaccessible tenant/case, stale memory, degraded backend, graph gap, and insufficient evidence where the response data allows.

**Given** sources, freshness, scores, graph context, or backend health disagree
**When** evidence is presented
**Then** the conflict is visible using the shared evidence state grammar rather than converted into a confident-looking answer.

**Given** feedback appears in the web UI
**When** users inspect it with keyboard or assistive technology
**Then** status labels are readable, focusable recovery actions are reachable, and color is never the only signal.

### Story 17.3: Contract-Aware Web Interaction Patterns

As a developer or operator,
I want forms, filters, navigation, confirmations, command access, overlays, and data grids to preserve tenant scope and evidence context,
So that web interactions remain safe, predictable, and efficient for repeated work.

**Acceptance Criteria:**

**Given** a form changes tenant, case, ingestion, source filter, graph, token budget, repair, or benchmark configuration
**When** the user submits it
**Then** validation is contract-aware, tenant and case scope appear near the top, and dangerous or inconsistent changes require explicit acknowledgement.

**Given** search or filtering controls are displayed
**When** filters are changed
**Then** active filters for axis, source type, freshness, confidence, time range, metadata, graph depth, and evidence state remain inspectable
**And** the UI indicates when filters narrow scope, broaden scope, exclude axes, or affect confidence.

**Given** the user navigates from an Evidence Packet to a source, graph path, activity item, operator check, or MCP packet
**When** navigation completes
**Then** tenant/case/search context is preserved and a clear return path remains available.

**Given** an action is destructive, scope-expanding, repair-oriented, or diagnostic-exporting
**When** confirmation is required
**Then** the dialog or panel names the tenant, case, object, consequence, and recovery or undo expectation before allowing the action.

**Given** advanced users need fast access
**When** the command palette or command surface is opened
**Then** search, ingest, inspect source, verify tenant, open graph, retry ingestion, export packet, and inspect MCP payload actions are discoverable with accessible labels.

**Given** memory units, sources, ingestion jobs, case activity, tenant checks, backend health, or benchmark results are listed
**When** data grids render
**Then** they support sorting, filtering, status badges, row actions, and keyboard navigation without hiding trust-critical fields.

### Story 17.4: Role-Specific Web Inspection Lenses

As a developer, operator, team lead, or LLM-agent integrator,
I want dedicated inspection lenses for case activity, ingestion lifecycle, operator health, benchmark results, and MCP packets,
So that each audience can inspect the same evidence model at the right density.

**Acceptance Criteria:**

**Given** a case has ingestion, search, membership, annotation, health, or source-link activity
**When** the Case Activity Trail renders
**Then** activity is chronological, source-linked where possible, status-labelled, and scoped to the selected tenant and case.

**Given** ingestion jobs are queued, extracting, embedding, indexing, indexed, failed, retried, or re-ingested
**When** the Ingestion Lifecycle Tracker renders
**Then** each unit shows its stage, outcome, retry state, failure details when present, and recovery action when safe.

**Given** tenant verification, backend health, consistency repair, degradation, or ingestion health is inspected
**When** the Operator Health Matrix renders
**Then** it shows per-check status, affected capabilities, evidence, and next action without exposing secrets or restricted diagnostics.

**Given** benchmark validation has run
**When** the Benchmark Result Comparator renders
**Then** it shows hybrid-vs-single-axis NDCG@10 results, the 80% thesis threshold status, per-query breakdowns, and links to reproducible evidence.

**Given** MCP requests or responses are inspected
**When** the Agent Packet Inspector renders
**Then** it shows request summary, response schema, token budget, omitted fields, expansion handles, structured errors, copy controls, and readable schema/JSON views.

### Story 17.5: Responsive and Accessible Web Validation

As a user of the future web surface,
I want trust-critical workflows to remain usable across screen sizes, keyboard, screen reader, reduced-motion, and forced-colors contexts,
So that evidence inspection is accessible and reliable rather than visual polish only.

**Acceptance Criteria:**

**Given** the web UI is tested at 360px, 768px, 1024px, and 1440px
**When** Evidence Cockpit, Evidence Packet, Source Citation Stack, Retrieval Axis Breakdown, Recovery Action Panel, Case Activity Trail, Agent Packet Inspector, and Operator Console surfaces are rendered
**Then** scope, confidence, freshness, source count, evidence health, and recovery remain reachable
**And** trust-critical content does not require horizontal scrolling.

**Given** automated accessibility checks run
**When** the surfaces are validated
**Then** checks cover color contrast, accessible names, form labels, ARIA validity, heading order, and focusable controls.

**Given** human accessibility checks run
**When** keyboard-only navigation, focus order, no-color-only state comprehension, reduced motion, high contrast, and at least one screen reader pass are tested
**Then** critical trust workflows remain usable and defects are tracked before release.

**Given** overlays, dialogs, drawers, source previews, graph detail panels, MCP inspectors, and confirmations are opened
**When** focus moves
**Then** focus enters the overlay predictably and returns to the invoking control when closed.

**Given** source preview, graph detail, recovery action, tooltip, command, or status behavior is trust-critical
**When** the UI is used by keyboard or touch
**Then** no behavior depends on hover-only interaction.

**Given** accessible labels, tooltips, announcements, copied text, diagnostics, or error payloads are emitted
**When** they contain tenant or source context
**Then** secrets, raw payloads, bearer tokens, tenant-sensitive diagnostics, and restricted source details are not exposed.

### Story 17.6: FrontComposer and Fluent UI Blazor V5 Conformance Hardening

As a maintainer of the Memories web UX,
I want all Memories web components to use only FrontComposer and Fluent UI Blazor V5 components and tokens,
So that Epic 17 cannot drift into a parallel design system or raw HTML/CSS implementation.

**Acceptance Criteria:**

**Given** the existing `Hexalith.Memories.Web` RCL from Story 17.1
**When** conformance is audited
**Then** every `.razor` and `.razor.css` file is classified as FrontComposer component usage, Fluent UI Blazor V5 component usage, unavoidable semantic/container markup, or a violation requiring remediation.

**Given** a FrontComposer or Fluent UI Blazor V5 component exists for a control, status indicator, message, badge, stack/layout, grid/list, dialog/drawer, menu, tooltip, input, command surface, tab, or data display
**When** the Memories web component renders that function
**Then** it uses the component rather than raw HTML or a custom UI primitive.

**Given** hand-authored CSS remains
**When** it is reviewed
**Then** it contains only layout the design system does not own, uses Fluent 2 tokens where tokens are needed, and does not define theme primitives, direct typography ramps, direct foreground roles, legacy Fluent v4/FAST tokens, or one-off status color systems.

**Given** an exception is unavoidable
**When** it remains in source
**Then** a conformance allowlist names the file, selector or markup pattern, reason, missing FrontComposer/Fluent primitive, owner story, and removal condition.

**Given** Stories 17.2 through 17.5 are implemented
**When** their code is reviewed
**Then** they reuse the same conformance tests and cannot add new raw UI/CSS exceptions without an explicit allowlist entry.

**Given** the Fluent UI Blazor package version is checked
**When** component APIs are selected
**Then** implementation follows the centrally pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` and the aligned `Hexalith.FrontComposer` submodule; incompatible MCP documentation examples are not copied blindly.

**Given** focused validation runs
**Then** `dotnet test tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj`, the new conformance tests, and `git diff --check` pass.

**Target artifacts:**

- `src/Hexalith.Memories.Web/**/*.razor`
- `src/Hexalith.Memories.Web/**/*.razor.css`
- `tests/Hexalith.Memories.Web.Tests/**`
- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md`
- `_bmad-output/implementation-artifacts/17-2-recovery-and-feedback-state-grammar.md`
- `_bmad-output/implementation-artifacts/17-3-contract-aware-web-interaction-patterns.md`
- `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`
- `_bmad-output/implementation-artifacts/17-5-responsive-and-accessible-web-validation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Out of scope:**

- Broad FrontComposer framework redesign
- Fluent UI package upgrade beyond the current pinned V5 prerelease
- New Evidence Packet semantics
- Backend, CLI, MCP, storage, ingestion, search, or tenant-isolation behavior
- Recursive submodule initialization or casual submodule changes

### Story 17.7: Runnable Web Specimen and Browser/AT Accessibility Gap Closure

As a user of the future web surface,
I want Epic 17 trust workflows to run in a browser-backed specimen with automated and manual accessibility evidence,
So that the fail-closed browser and assistive-technology gaps in `Epic17ValidationInventory.Gaps` are closed by evidence rather than waived by component-specimen coverage.

**Acceptance Criteria:**

**Given** Story 17.6 conformance evidence is complete
**When** the browser validation host is created
**Then** a minimal runnable Memories web specimen app exposes existing Epic 17 RCL components through stable fixture routes for Evidence Cockpit, Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, Graph Path Summary, Recovery Action Panel, Evidence Grid, Command Surface, Interaction Form, Filter Summary, Case Activity Trail, Ingestion Lifecycle Tracker, Operator Health Matrix, Benchmark Result Comparator, Agent Packet Inspector, and Lens Shell
**And** the specimen uses existing contract fixtures and does not introduce new Evidence Packet semantics, backend dependencies, product workflows, public APIs, package versions, or FrontComposer framework changes.

**Given** Playwright validation runs against the specimen
**When** each route is scanned
**Then** smoke checks and `@axe-core/playwright` scans fail on zero target nodes, record selector/route/fixture metadata, and cover accessible names, ARIA validity, heading order, focusable controls, color contrast where supported, and WCAG 2.2 AA tags where the local axe/tooling stack supports them.

**Given** browser media and layout validation runs
**When** the specimen is tested at the Epic 17 viewport set and required media conditions
**Then** forced-colors/high-contrast, reduced-motion, zoom/reflow, and 44x44px touch-target checks produce bounded evidence for trust-critical fields and controls, with any unsupported browser/tooling dimension kept fail-closed in the evidence matrix rather than treated as passed.

**Given** manual accessibility validation is performed
**When** at least one screen-reader pass is completed for a trust workflow
**Then** the evidence names the workflow script, viewport, browser, operating system, screen reader or checklist method, tester/date, pass/fail result, defects, severity, owner, waiver state, and release disposition; preferred initial pass is NVDA with Edge or Chrome on Windows unless unavailable.

**Given** browser, axe, screenshot, trace, copied-text, and manual evidence artifacts are produced
**When** they are archived or summarized
**Then** they are bounded, relative-path-safe, sanitized for secrets, bearer tokens, raw payload fragments, tenant-sensitive diagnostics, local absolute paths, provider internals, stack traces, and restricted source details, and the redaction scan result is included in the evidence summary.

**Given** the story completes
**When** `Epic17ValidationInventory.Gaps` is reviewed
**Then** the prior Playwright/axe, color-contrast, forced-colors, reduced-motion, zoom/reflow, touch-target, screen-reader, and live keyboard focus-trap/focus-return gaps are either resolved with evidence rows or remain fail-closed with explicit owner, severity, waiver state, and release disposition.

**Target artifacts:**

- `tests/Hexalith.Memories.Web.SpecimenHost/**` or the equivalent test-only runnable specimen host path selected during implementation
- `tests/Hexalith.Memories.Web.E2E/**` or the equivalent Playwright project path selected during implementation
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17InventoryTests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary-17-7-browser-at-gap-closure.md`
- `_bmad-output/implementation-artifacts/17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Out of scope:**

- Production web application launch or product-route commitment beyond a test/specimen host
- New Evidence Packet semantics, backend calls, ingestion/search behavior, MCP behavior, CLI behavior, storage behavior, or tenant-isolation policy changes
- Broad FrontComposer framework redesign
- Fluent UI package upgrade beyond the current pinned V5 prerelease
- Recursive submodule initialization or casual submodule changes


---

## Epic 18: Downstream Consumer Integration Contract Hardening

**Lifecycle label:** Operational Readiness / Downstream Consumer Integration Hardening.

Maintainers can give the first external consumer of Hexalith.Memories (the `Hexalith.Parties` project) a stable, documented, and race-safe integration contract, closing seven cross-repository asks (MEM-1 … MEM-7) raised during the Parties `bmad-correct-course` intake on 2026-05-27.

**Origin:** Parties consumer correct-course intake, 2026-05-27 (origins tagged 7-7, 9-3, 9-6 chunk A / passes 2/3/5). Each story carries its `(MEM-n)` origin and a **Parties-side follow-up** so the cross-repo linkage stays auditable. Three asks (MEM-1, MEM-4, MEM-7) were partly satisfied by the current `main`; the stories below close only the verified residual gap.

**Preflight required:** Before implementation starts, re-verify the state each story cites (the codebase moves): `Projects.Hexalith_Memories_Server` / `Projects.Hexalith_Memories_Mcp` resolution in `src/Hexalith.Memories.AppHost/Program.cs`; the `AddServerEventStoreIntegration` → `AddMemoriesEventStoreIntegration` wiring signature; the `[Experimental("HXL001")]` marker on `MemoriesClient.IngestAsync`; the `DedupKeyBuilder` / `CheckIdempotencyActivity` dedup path; the absence of a source-URI lookup endpoint; the `ResolveMemoryUnitId` logic in `IngestionWorkflow.cs`; and Architecture Decision D9. Update the story if any cited anchor has moved. Per the Engineering/Operational Readiness Track preamble, individual stories may be implemented, accepted, or carried forward with rationale recorded in `deferred-work.md`.

**FRs reinforced:** FR6, FR24, FR59, FR60, FR61, FR62.
**NFRs reinforced:** tenant isolation (NFR8), idempotent at-least-once event handling, deployment/observability configurability.

**Release-timing note:** Story 18.4 is the only story with semantic-release sensitivity — it changes the public `Hexalith.Memories.Client.Rest` contract and must land as an additive `feat` (new optional idempotency token / new overload; experimental-marker removal is non-breaking) and be cut before the Parties project pins the stabilised SDK. The other six stories are documentation, drift-guard tests, or additive endpoints with no breaking-change risk.

**Sequencing correction:** Stories 18.5 and 18.6 must not be treated as mutually dependent. Story 18.6 is the independent `MemoryUnitId` and source-URI dedup-record stability contract and is listed before Story 18.5 for execution order. Story 18.5 is the lookup endpoint that consumes that contract. Preserve the existing numeric keys and completed story history for traceability.

### Story 18.1: AppHost Project-Resolution Guard and Public-Surface Stability Contract

**Origin:** MEM-1 (Parties passes 7-7 known-unrelated, 9-3).

As a maintainer of a downstream Aspire AppHost,
I want a compile-time guarantee that `Projects.Hexalith_Memories_Server` and `Projects.Hexalith_Memories_Mcp` resolve and that their public project/type names stay stable,
So that a clean clone with root-declared `references/` submodules initialised builds the full `.slnx` without submodule-drift surprises.

**Acceptance Criteria:**

**Given** the AppHost already references `Projects.Hexalith_Memories_Server` and `Projects.Hexalith_Memories_Mcp`,
**When** a dedicated AppHost-resolution test runs,
**Then** it asserts those project symbols resolve at compile time as a buildable test, not an integration/Docker test, and does not depend on a running sidecar.

**Given** the Parties intake reported `AddHexalithEventStore` redis-parameter drift,
**When** the EventStore wiring surface is reviewed,
**Then** the story confirms the current public wiring is `AddServerEventStoreIntegration(IConfiguration)` → `AddMemoriesEventStoreIntegration(IConfiguration, Action<EventStoreIntegrationBuilder>?)` with no redis parameter, and records that the reported drift was a stale submodule pin rather than a current API.

**Given** external AppHosts depend on stable project and assembly names,
**When** this story completes,
**Then** the project name, assembly name, and root namespace of `Hexalith.Memories.Server` and `Hexalith.Memories.Mcp` are recorded as a stability contract under `docs/dev`, and any future rename is flagged as requiring a breaking-change note.

**Parties-side follow-up:** Parties adds its own AppHost compile assertion that `Projects.Hexalith_Memories_Server` resolves.

### Story 18.2: Deployment Configuration Contract Publication

**Origin:** MEM-2 (Parties pass 9-3).

As an operator deploying Memories into a downstream Kubernetes overlay,
I want the canonical environment, port, and OTLP configuration surface published,
So that placeholder-shaped env literals in consumer kustomizations can be replaced with real, documented values without first running aspirate.

**Acceptance Criteria:**

**Given** there is no aspirate manifest tooling in the repo today,
**When** this story completes,
**Then** `docs/operations` documents the canonical deploy config contract: the OTLP exporter endpoint variable (`OTEL_EXPORTER_OTLP_ENDPOINT`) and its enable/disable semantics, the Dapr sidecar HTTP/gRPC ports the Server and MCP expect (3500/50001 and 3600/50101 in the AppHost defaults), and the required runtime env (`PUBSUB_REDIS_HOST`, `PUBSUB_REDIS_PASSWORD`, `MEMORIES_EVENTSTORE_TOPIC`, and connection-string keys).

**Given** the documentation must not drift from code,
**When** the contract is published,
**Then** the documented variable names are cross-checked against `ServiceDefaults`, `AppHost/Program.cs`, and `appsettings*.json`, and a test or doc-lint guards the variable-name list against silent rename.

**Given** full aspirate emission is a larger, separable effort,
**When** this story is scoped,
**Then** aspirate manifest generation is explicitly deferred to a future story and recorded as such; this story delivers the documented contract only.

**Given** Hexalith modules publish events through DAPR pub/sub,
**When** the deployment contract is published,
**Then** it documents the shared pub/sub component name (`pubsub`), the required `MEMORIES_EVENTSTORE_TOPIC`, the source-prefix routing map (`EventStoreIntegration:Routing:SourceToTenantMap`), and the Memories Server sidecar ports used for subscription discovery and internal delivery.

**Parties-side follow-up:** Parties replaces the placeholder env literals in `deploy/k8s/memories/kustomization.yaml` using the published contract.

### Story 18.3: Invocable Route and Operation Surface Publication

**Origin:** MEM-3 (Parties pass 9-3).

As an operator authoring a Dapr access-control policy for Memories,
I want the invocable HTTP route and pub/sub operation surface published,
So that `accesscontrol.memories.yaml` can be verified against real operation paths instead of an unverified `/process` placeholder.

**Acceptance Criteria:**

**Given** the Parties ACL references an operation path `/process` that does not exist on the Memories surface,
**When** the route surface is published,
**Then** the documentation enumerates the real invocable surface — the `/api/*` REST routes and the Dapr pub/sub subscription endpoint (`[HttpPost("ingest")]` → `/events/ingest`, topic from `MEMORIES_EVENTSTORE_TOPIC`) — and explicitly states that no `/process` operation exists.

**Given** an external ACL must be machine-verifiable,
**When** this story completes,
**Then** the route surface is published in a form an ACL can be checked against (an OpenAPI document or a maintained route-surface doc under `docs/dev` or `docs/operations`), covering method, path, and Dapr operation semantics.

**Given** the surface can drift as endpoints are added,
**When** the surface is published,
**Then** a test or generation step keeps the published surface in sync with the actual mapped endpoints, or a documented review trigger requires updating it whenever routes change.

**Given** the Memories Server sidecar manages event delivery,
**When** the route surface is published,
**Then** it includes the DAPR subscription discovery contract (`/dapr/subscribe`) and the pub/sub delivery route (`POST /events/ingest`), and it states that domain modules publish CloudEvents to DAPR rather than invoking Memories REST ingestion for event streams.

**Parties-side follow-up:** Parties corrects the `/process` operation path in `accesscontrol.memories.yaml` and adds an end-to-end ACL assertion against the published surface.

### Story 18.4: Stable Ingest Contract with Explicit Idempotency Token and Atomic Dedup

**Origin:** MEM-4 (Parties pass 9-6 chunk A / 3rd pass). **Release-timing sensitive — additive `feat`.**

As a downstream service indexing memories from near-simultaneous projection events,
I want a non-experimental ingest path that accepts an explicit idempotency token and resolves concurrent same-source ingests atomically,
So that two near-simultaneous ingests of the same party/source cannot race into duplicate or partially-written memory units, and consumers can drop the `HXL001` suppression.

**Acceptance Criteria:**

**Given** `MemoriesClient.IngestAsync` is currently `[Experimental("HXL001")]` (Story 7.4),
**When** this story stabilises the ingest path,
**Then** a non-experimental ingest entry point exists, the change is additive (new overload or experimental-marker removal — no breaking signature change), serialized through the existing JSON context, and covered by contract/client tests, so consumers can ingest without `#pragma warning disable HXL001`.

**Given** the only dedup key today is `dedup:{tenantId}:{caseId}:{SHA256(sourceUri)}` derived server-side,
**When** the ingest contract is extended,
**Then** the request carries an optional explicit idempotency token that, when supplied, participates in dedup alongside `sourceUri`, and the contract documents token precedence and the natural-key fallback when the token is absent.

**Given** the current idempotency check in `CheckIdempotencyActivity` is check-then-act and can race under concurrency,
**When** two ingests with the same dedup key arrive near-simultaneously,
**Then** dedup resolution is atomic (for example a Redis `SET … NX` reservation) so exactly one ingest wins and the other observes the existing `MemoryUnitId`, proven by a concurrent-ingest test.

**Given** ingestion runs on at-least-once, unordered Dapr pub/sub,
**When** a duplicate or out-of-order ingest is received,
**Then** behavior remains idempotent and returns the same `MemoryUnitId` without creating a second unit, consistent with the project idempotency rules.

**Parties-side follow-up:** Parties drops the `HXL001` suppression in `PartyMemoryIndexingService` and passes the idempotency token.

### Story 18.6: MemoryUnitId Stability Contract

**Origin:** MEM-6 (Parties pass 9-6 / 5th pass).

As a downstream service maintaining a per-party mapping keyed by `MemoryUnitId`,
I want the stability semantics of `MemoryUnitId` documented and guaranteed,
So that the mapping cannot accumulate ghost ids and exceed the Dapr state-store value-size limit after a Memories restart or contract change.

**Acceptance Criteria:**

**Given** `MemoryUnitId` is currently the workflow `InstanceId` or a new GUID (`ResolveMemoryUnitId` in `IngestionWorkflow.cs`) and is not derived from `sourceUri`,
**When** the contract is documented,
**Then** the stability guarantee is stated precisely: for a given `(tenantId, caseId, sourceUri)`, re-ingestion returns the same `MemoryUnitId` for as long as the dedup record persists, and the guarantee's dependency on the dedup record's TTL/retention is explicit.

**Given** the Parties intake labelled this "decision D1" on the Parties side,
**When** the contract is written,
**Then** it clarifies this is unrelated to the Memories Architecture Decision D1 (FalkorDB for MVP), to avoid cross-repo confusion.

**Given** loss of the dedup record (eviction, TTL expiry, or contract change) would re-mint an id,
**When** the contract is published,
**Then** it documents that failure mode
**And** it tells consumers to retain `(tenantId, caseId, sourceUri)` as the durable source identity for long-lived correlation
**And** it states that consumers should resolve the current `MemoryUnitId` through the source-URI keyed lookup when that endpoint is available, or dedup by `sourceUri` when it is not.

**Parties-side follow-up:** Parties revisits cap / TTL / dedup-by-`SourceUri` in `PartyMemoryUnitMappingStore` against the documented guarantee.

### Story 18.5: Source-URI-Keyed Memory-Unit Lookup Endpoint

**Origin:** MEM-5 (Parties pass 9-6 chunk A).

As a downstream service resolving a graph start node from a source URI,
I want an exact source-URI-keyed lookup that returns the canonical `MemoryUnitId`,
So that graph mode no longer silently degrades to local mode when the canonical match falls outside a free-text search's top hits.

**Acceptance Criteria:**

**Given** there is no keyed lookup today and free-text/syntactic search may not surface the canonical unit in its top results,
**When** a consumer needs the unit for a known source URI,
**Then** a tenant- and case-scoped endpoint resolves a source URI to its canonical `MemoryUnitId` by exact key, returning a structured not-found result when no unit exists rather than a best-effort search hit.

**Given** the Story 18.6 stability contract states the lifetime and failure modes of the source-URI dedup record `dedup:{tenantId}:{caseId}:{SHA256(sourceUri)}`,
**When** the lookup is implemented or reworked,
**Then** it reuses that existing mapping as the authoritative index where possible rather than introducing a parallel store
**And** if the stability contract is missing or stale, Story 18.6 must be completed or refreshed before the endpoint is accepted.

**Given** the endpoint is part of the public client contract,
**When** it is added,
**Then** it is exposed through `MemoriesClient` and the MCP/CLI surface as appropriate, tenant-isolated, additive to the JSON contract, and covered by success, not-found, and cross-tenant-rejection tests.

**Parties-side follow-up:** Parties switches `MemoriesPartySearchService.ResolveGraphStartNodeIdAsync` from the free-text URN search to the keyed lookup.

### Story 18.7: MemoriesClient Mockability Stability Contract

**Origin:** MEM-7 (Parties pass 9-6 / 2nd pass).

As a downstream test author,
I want the supported mocking seam for `MemoriesClient` documented and guaranteed stable,
So that consumer test fixtures (for example `ProbingMemoriesClient`) do not break if the SDK evolves.

**Acceptance Criteria:**

**Given** Architecture Decision D9 deliberately keeps `MemoriesClient` a concrete class with no interface ("avoid abstraction tax; extract when a second implementation arrives"),
**When** this story addresses the mockability ask,
**Then** it reaffirms D9 and documents the supported mock seam as the `HttpClient` / `IHttpClientFactory` boundary with a worked example, rather than introducing an `IMemoriesClient` interface.

**Given** the consumer fixture currently subclasses the concrete client and relies on `virtual` members,
**When** the contract is published,
**Then** it records a stability guarantee that `MemoriesClient` remains non-sealed with `virtual` public members so subclass-based fixtures keep compiling, and notes that sealing the class or removing `virtual` would be a breaking change requiring the D9 escape hatch (extract `IMemoriesClient`) and a sprint change.

**Given** the recommended seam should be demonstrably real,
**When** the doc lands,
**Then** the `HttpClient`-boundary mocking approach is backed by an example test in the repo so the documented seam is proven, not asserted.

**Parties-side follow-up:** Parties keeps `ProbingMemoriesClient` (now contract-guaranteed) or migrates to the documented `HttpClient`-boundary seam at its discretion.

### Story 18.8: Cross-Module Dapr Event Intake Contract and Verification

**Origin:** Sprint Change Proposal 2026-06-24.

As an operator integrating Hexalith modules with Memories,
I want the Memories Server DAPR sidecar to be the documented and tested subscriber for module CloudEvents,
So that Tenants, Parties, and future Hexalith modules can publish events without direct REST coupling or per-module ingestion code.

**Acceptance Criteria:**

**Given** a downstream Hexalith module publishes a CloudEvent to the configured DAPR `pubsub` component and `MEMORIES_EVENTSTORE_TOPIC`,
**When** the Memories Server sidecar discovers subscriptions,
**Then** `/dapr/subscribe` exposes `pubsubname=pubsub`, the configured topic, and route `/events/ingest`.

**Given** two module source prefixes, for example `hexalith/tenants` and `hexalith/parties`,
**When** events are published on the shared topic,
**Then** `SourceToTenantMap` routes each source prefix to the configured tenant without direct REST ingestion calls.

**Given** an operator authors DAPR access-control policy,
**When** they inspect the published operation surface,
**Then** the documented allowed operation is `POST /events/ingest` through pub/sub delivery and the docs explicitly state that `/process` is not part of the Memories event-ingest surface.

**Given** the same CloudEvent is delivered more than once by DAPR,
**When** the event reaches Memories,
**Then** existing preflight and workflow idempotency produce one memory unit and duplicate deliveries do not create additional units.

**Given** a module publishes to an unknown source prefix,
**When** the event reaches Memories,
**Then** the endpoint returns the existing non-retry drop outcome and handler mismatch/unknown-source diagnostics identify the missing route.

**Given** the current one-topic-per-deployment limitation,
**When** docs are updated,
**Then** they explain the supported shared-topic pattern and the separate-deployment workaround for independent topics; multi-topic routing remains deferred.

**Given** this story completes,
**When** focused validation runs,
**Then** tests or documented smoke evidence prove sidecar subscription discovery, source-prefix routing for at least two synthetic Hexalith modules, and duplicate-safe delivery.

**Target artifacts:**

- `docs/dev/eventstore-integration.md`
- `docs/operations/*` deployment or route-surface docs
- `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs` if consumer AppHost guidance needs stronger defaults
- `tests/Hexalith.Memories.*` focused tests for subscription discovery/routing where practical
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Out of scope:**

- Multi-topic routing in a single Memories deployment
- Direct REST-based module event ingestion
- Mutating `references/Hexalith.EventStore`, `references/Hexalith.Tenants`, or other submodules
- New persistence mechanisms outside the existing Memories ingestion workflow

## Epic 19: Deferred Register Backlog Home and Residual Hardening

**Lifecycle label:** Operational Readiness / Deferred Register Governance.

Maintainers can convert active `open` and `carried-forward` entries from `deferred-work.md` and recent retrospective action items into explicit backlog homes, accepted-debt decisions, or trigger-bound future work without reopening completed epics.

**Driven by:** Sprint Change Proposal 2026-06-30 (Deferred Work Backlog Homes).

**Preflight required:** Before implementing any story, re-read `deferred-work.md`, `sprint-status.yaml` action items, and the relevant retrospective that produced each residual. Do not bulk-rewrite historical deferred prose; only migrate active entries that need planning signal.

### Story 19.1: Deferred Register Active-Entry Classification Sweep

As a maintainer,
I want every active `open` or `carried-forward` deferred-work entry to have a current disposition,
So that completed epics do not hide unscheduled operational or consumer-risk work.

**Acceptance Criteria:**

**Given** `deferred-work.md` contains structured entries with `Status: open` or `Status: carried-forward`,
**When** the sweep runs,
**Then** every active structured entry is classified as one of: scheduled story, accepted debt with rationale, carried-forward with explicit trigger and owner, or resolved with evidence.

**Given** Epic 18 retrospective Action Item 4 names parked carry-forwards,
**When** the sweep runs,
**Then** `MEM-2-ASPIRATE`, `MEM-3-OPENAPI`, the real-Redis race evidence, the Dapr-sidecar pub/sub smoke evidence, and the Story 18.4 token-anchoring edge each receive a story id or accepted-debt entry with a re-open trigger.

**Given** active entries from completed Epics 15 and 18 may still be valid,
**When** the sweep updates planning artifacts,
**Then** it references the completed source story but does not reopen the completed epic or alter completed story history.

**Given** `sprint-status.yaml` has retrospective action items,
**When** the sweep completes,
**Then** related action items are updated only when their acceptance condition is actually met.

### Story 19.2: Downstream Contract Artifact Generation Decisions

As a downstream integration maintainer,
I want explicit decisions for generated deployment and route artifacts,
So that consumers know whether to rely on maintained docs, generated manifests, or generated OpenAPI/Swagger output.

**Acceptance Criteria:**

**Given** `MEM-2-ASPIRATE` is carried forward without a story id,
**When** this story runs,
**Then** aspirate or equivalent manifest emission is either scheduled for implementation, explicitly accepted as not needed, or deferred with an owner, trigger, and target artifact.

**Given** `MEM-3-OPENAPI` is carried forward without a story id,
**When** this story runs,
**Then** OpenAPI/Swagger generation is either scheduled for implementation, explicitly accepted as not needed, or deferred with an owner, trigger, and target artifact.

**Given** maintained docs already exist for deployment configuration and route surface,
**When** generated artifacts remain deferred,
**Then** the rationale states why the maintained-doc plus drift-guard tests remain sufficient for current consumers.

### Story 19.3: Release Preflight and Baseline Evidence Residual Sweep

As a release maintainer,
I want release-preflight and baseline-evidence carry-forwards reviewed as one release-quality backlog decision,
So that low-value hardening stays trigger-bound and high-value release risks get implementation stories.

**Acceptance Criteria:**

**Given** `12.4-RV20` requests optional strict literal per-SHA replay evidence,
**When** release evidence needs are reviewed,
**Then** the team either creates a strict replay evidence story or records why ancestry-based proof remains sufficient until a release post-mortem or quality story reopens it.

**Given** `15.1-RV1` through `15.1-RV16` are carried forward from release-preflight review,
**When** the sweep runs,
**Then** each entry is grouped into implement-now, accept-until-trigger, or future release-hardening story buckets.

**Given** release tooling changes can affect package publication,
**When** any implement-now item is selected,
**Then** focused validation covers the changed script, workflow, and inventory-test behavior.

### Story 19.4: Provider Registry and Migration Residual Sweep

As a system operator,
I want provider-registry and migration-marker residual risks reviewed against current code,
So that embedding-provider expansion and live migration do not inherit stale assumptions.

**Acceptance Criteria:**

**Given** `15.2-RV1` through `15.2-RV9` are marked `open`,
**When** the sweep runs,
**Then** each item is either resolved, accepted with rationale, or assigned to a concrete provider-registry follow-up story.

**Given** migration-marker deferred entries from Story 15.3 include concurrency, stale-marker, TTL, and operator-documentation risks,
**When** the sweep runs,
**Then** the team identifies which risks remain trigger-bound and which need a migration-hardening story before the next provider migration investment.

**Given** provider/model casing and registry dispatch appear in both provider and migration paths,
**When** follow-up work is scheduled,
**Then** tests cover both write-time validation and read/runtime comparison paths where practical.

---

## Phase: Post-MVP — Audit Remediation (2026-07-04)

Epics 20-26 are added by Sprint Change Proposal 2026-07-04 (Architecture Audit Remediation), driven by the audit evidence file `research/architecture-audit-2026-07-04.md` (findings A1-A51). They are remediation epics: each story closes one or more audit findings and must preserve the strengths the audit recorded (health-check depth, contract serialization sweep, Testcontainers/Aspire end-state fixtures, ingestion compensation skeleton, disciplined secrets handling) rather than regress them. No completed epic is reopened. Two stories are decision-first (21.1 consistency model, 24.3 physical isolation) and gate their epic's implementation until the architecture decision is ratified.

**Audit-anchor and AC-claim preflight (2026-07-04; broadened 2026-07-28; bound at authoring and registration 2026-07-28):** Before any story is authored, registered, selected, created, or implemented—regardless of epic number, and at any status, including `backlog`—re-verify against the current repository both the code anchors and implementation-state assumptions that story cites and every verifiable claim in the epic, PRD, architecture, or audit text it inherits: quantitative counts, existence and absence assertions, behavioral descriptions, and file, symbol, or line locations. Epic acceptance text is planning intent recorded at a point in time and is advisory until re-derived; where code and planning text disagree, the code wins. Story files must record the re-verification date, moved or renamed anchors, how the implementation adapts, and per claim a re-runnable command with a `confirmed`, `corrected`, or `unverifiable` verdict, as specified by `_bmad/custom/epic-ac-verification.md`. A corrected claim must also correct this file or carry a dated correction note here, because a story that fixes only its own text leaves the planning artifact wrong for the next reader; a correction that changes scope, epic intent, or a ratified decision is escalated for a human decision instead of absorbed. Story 25.3's "60 server literals", Story 25.5's "no `Client.Rest` reference", and Story 25.6's "double authorization" are the recorded exemplars of claims that were false against the code. A story created by an approved sprint change is bound at the moment that change registers it, not at the moment it is later selected.

**A41 access-telemetry retention residual (2026-07-16):** Epic 20 and Story 20.5 close only A41's request-limiting and audit-emission slices. `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains `carried-forward`, and its retrospective action remains `open`, until either bounded retention/TTL is implemented and validated or an explicit accepted-debt disposition records a named approver/owner, scope, rationale, risk/consequence, compensating controls, and a time-bounded review/expiry date or measurable reopen trigger. No artifact may claim A41 fully closed before that gate is met. This guard does not reopen completed Epic 20 or schedule implementation by itself.

**Cross-tenant negative-evidence carry-forward (2026-07-06; broadened 2026-07-16):** Any future scope-sensitive story, spec, refactor, fix, review patch, sprint correction, or implementation change—regardless of epic number—must keep cross-tenant negative validation evidence attached to the change instead of treating it as historical proof. Scope-sensitive includes tenant/case route grouping or versioning; endpoint filters or middleware; auth or claim normalization; tenant status guards; MCP tool executors or client calls; evidence-packet scope metadata or restrictive web rendering; tenant verifier logic or tenant markers; key/index/graph routing, actor IDs, storage selectors, or query builders; search/graph/case attribution; export/import or backup/restore; and any refactor that moves those paths. The story/spec and completion or review record must list the impacted surfaces, cite Story 20.2 denial-before-dependency and Story 24.3 verifier fail-closed/tenant-marker evidence when applicable (or link a newer canonical replacement), and record focused negative test names, command, and result. If proof cannot run, record an explicit accepted blocker with owner, consequence, and reopen trigger. A scope-sensitive change cannot close on happy-path, broad-suite, build-only, or refactor-green evidence alone.

## Epic 20: API Security & Tenant Authorization
Operator and downstream consumers get an authenticated, tenant-authorized server boundary: every endpoint requires an authenticated principal, tenant access is verified against principal claims (not caller-supplied parameters), the audit identity is trustworthy, MCP cannot run on a development signing key in production, inbound load is bounded per tenant, and audit coverage spans all mutating operations.
**Lifecycle label:** Operational Readiness / Security Hardening
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A1, A2, A6, A20, A31, and A41's request-limiting/audit-emission slices; the retention residual remains carried forward
**FRs reinforced:** FR44, FR67 · **NFRs reinforced:** NFR8, NFR11

### Story 20.1: Server Authentication Foundation

As an operator,
I want every server endpoint to require an authenticated principal,
So that no network caller can read, mutate, or destroy tenant data anonymously.

**Acceptance Criteria:**

**Given** JWT/OIDC bearer authentication is registered in ServiceDefaults with a fallback `RequireAuthenticatedUser` authorization policy,
**When** any `/api/**` endpoint is called without a valid bearer token,
**Then** the request is rejected with 401 and only health and Dapr subscription routes remain `AllowAnonymous`.

**Given** the deferral comment at `Server/Program.cs:3122` (Story-9.3-MemoriesServerAuthN),
**When** this story is complete,
**Then** the deferred-work entry is resolved with evidence and the comment is removed or updated. Closes A1.

### Story 20.2: Tenant Authorization Filter & Principal-Derived Audit Identity

As an operator,
I want tenant access enforced from authenticated principal claims and the audit user derived from the principal,
So that cross-tenant access is impossible and the FR67 audit trail is non-forgeable.

**Acceptance Criteria:**

**Given** a claims-based tenant-membership endpoint filter applied to the `/api/tenants/{tenantId}/**` route group,
**When** an authenticated principal requests a tenant it is not a member of,
**Then** the request is denied with a clear cross-tenant error, verified by negative cross-tenant tests across all axes.

**Given** audit events currently read `x-user-id` (`Program.cs:3245-3261`),
**When** audit events are emitted after this story,
**Then** the user identity comes from the authenticated principal and the spoofable header is ignored. Closes A2.

### Story 20.3: Tenant-Scope Workflow & Batch Status Endpoints

As an operator,
I want workflow and batch status endpoints scoped to the caller's tenant,
So that a leaked or guessed instance id cannot expose another tenant's document content.

**Acceptance Criteria:**

**Given** `GET /api/ingest/{instanceId}` and `GET /api/ingest/batches/{batchId}` (`Program.cs:488-492,740-807`),
**When** a status is requested,
**Then** the endpoint verifies the stored state's tenant against the authorized tenant and returns a projected status DTO, never the raw `WorkflowState`. Closes A6.

### Story 20.4: MCP Production Signing-Key Hardening

As an operator,
I want the MCP server to refuse a development symmetric signing key in production,
So that the corpus cannot be reached with a static shared secret.

**Acceptance Criteria:**

**Given** `Mcp/Authentication/ValidateMcpAuthenticationOptions.cs`,
**When** an HS256 `SigningKey` is configured under `IHostEnvironment.IsProduction()`,
**Then** startup fails with a clear message and `RequireHttpsMetadata` is enforced on the Authority branch. Closes A20.

### Story 20.5: Inbound Rate Limiting, Quotas & Audit Completeness

As an operator,
I want per-tenant inbound rate limiting and complete audit emission,
So that one tenant cannot saturate the service and every mutating operation is audited.

**Acceptance Criteria:**

**Given** ASP.NET `AddRateLimiter` partitioned by authenticated tenant,
**When** a tenant exceeds its ceiling,
**Then** requests are throttled with a structured error and telemetry.

**Given** `AccessTelemetryLog` currently omits lifecycle events,
**When** tenant create/delete/status/embedding-config, case-member add/remove, annotation, and deletion operations run,
**Then** each emits an audit event. This closes A41's request-limiting and audit-emission slices; `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward.

### Story 20.6: RediSearch Query-Injection Hardening

As a developer,
I want one shared, complete RediSearch escaper on all axes,
So that user input cannot break query syntax or cause query-shaped denial of service.

**Acceptance Criteria:**

**Given** the two divergent escapers (`SyntacticSearchService.cs:302`, `SemanticSearchService.cs:293`),
**When** user-controlled text (including `subject`) flows into a query,
**Then** a single shared escaper covering the full dialect-2 special set is applied and adversarial inputs return safe empty/typed results rather than 503. Closes A31.

## Epic 21: Data Integrity, Consistency & Migration Safety
Maintainers get a persistence layer whose multi-backend writes cannot silently diverge, whose key namespaces do not collide, whose deletions are complete, and whose embedding-vector migration cannot strand a tenant. This epic ratifies the consistency model, then closes the divergence, namespace, deletion, routing, dedup, registry, and migration-safety gaps.
**Lifecycle label:** Operational Readiness / Data Integrity
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A3, A4, A5, A16, A17, A22, A27, A28, A44, A47
**FRs reinforced:** FR13, FR39 · **NFRs reinforced:** NFR16, NFR17, NFR18, NFR19

### Story 21.1: Consistency Model Decision (Decision-First)

As a solution architect,
I want a ratified consistency model for `Case`, `MemoryUnit`, and `Tenant`,
So that multi-backend writes stop diverging without a rebuild path.

**Acceptance Criteria:**

**Given** the current direct triple-writes (`CaseService.cs:64-112,646-694`) contradict architecture decision D3 (workflow saga/compensation),
**When** this story completes,
**Then** the team ratifies either event-sourced aggregates with the three backends as rebuildable projections, or workflow-wrapped compensated multi-writes, and updates `architecture.md` D3.

**Given** this is decision-first,
**When** the decision is pending,
**Then** no production code in Epic 21 dependent on the model begins. Frames A3.

### Story 21.2: Transactional Multi-Backend Mutation

As a maintainer,
I want case/memory-unit mutations to be atomic or compensated,
So that a partial backend failure cannot leave permanent cross-store divergence (FR13).

**Acceptance Criteria:**

**Given** the ratified model from 21.1,
**When** a case, annotation, or memory-unit mutation writes to Redis, FalkorDB, and the activity stream,
**Then** either all writes commit or compensation restores consistency, mirroring `TenantDeletionWorkflow`, with workflow/compensation tests. Closes A3.

### Story 21.3: Natural-Language Vector Namespace Separation

As a maintainer,
I want NL vectors on a disjoint key namespace,
So that consistency verification, repair, and raw KNN search stop being corrupted by nested prefixes.

**Acceptance Criteria:**

**Given** `SemanticKeyPrefixSuffix = ":vec:"` and `NaturalLanguageSemanticKeyPrefixSuffix = ":vec:nl:"` (`IndexSchemaDefinitions.cs:46,52`),
**When** NL hashes are stored and a tenant is verified/repaired,
**Then** NL keys live under a disjoint prefix, the raw index is rebuilt with a non-overlapping prefix, existing data is migrated, and a regression test enumerating a tenant with NL hashes shows zero phantom discrepancies and no repair-workflow crash. Closes A4.

### Story 21.4: Key-Schema Single Source of Truth

As a maintainer,
I want all Redis key/index names built through `IndexSchemaDefinitions`,
So that a schema rename cannot silently orphan search, consistency, or migration.

**Acceptance Criteria:**

**Given** ≥12 hand-interpolated `:mu:`/`:vec:` sites bypass the declared single source of truth,
**When** this story completes,
**Then** `Build{Syntactic,Semantic,NlSemantic}Key` helpers exist, all sites use them, and a CI grep guard fails on raw `:mu:`/`:vec:` literals. Closes A44.

### Story 21.5: Deletion Completeness

As an operator,
I want case and tenant deletion to remove every associated key,
So that a re-created case/tenant cannot inherit stale routing or a write-blocking marker.

**Acceptance Criteria:**

**Given** `DeleteCaseAsync` never touches the aggregate-case-map/router cache and `DeleteTenantDataKeysActivity.cs:42-43` deletes only `case:*`/`dedup:*`,
**When** a case or tenant is deleted,
**Then** the aggregate-case-map entry is `HDEL`ed with cache invalidation, and tenant deletion also sweeps `eventstore:*`, `embedding-migration:*`, and a defensive `mu:*`/`vec:*`, verified by end-state tests. Closes A16, A17.

### Story 21.6: Event Routing for Unknown/Unavailable Tenants

As a maintainer,
I want events for unknown or unavailable tenants to be retried or dead-lettered,
So that rollout ordering or transient tenant states cannot silently blackhole traffic.

**Acceptance Criteria:**

**Given** `EventIngestionController.cs:96-99` returns HTTP 200 for `TenantNotFound`/`TenantDeleting`(incl. `Unavailable`),
**When** such an event arrives,
**Then** the handler returns 500 (retry) or routes to a dead-letter topic, with duplicate/late-event safety preserved. Closes A27.

### Story 21.7: Dedup Race & Duplicate-Instance Handling

As a maintainer,
I want the dedup save to be race-safe and duplicate workflow instances handled,
So that concurrent ingests cannot create permanent duplicate memory units or poison-redelivery loops.

**Acceptance Criteria:**

**Given** the check-then-save TOCTOU window and unhandled duplicate-instance scheduling (`DaprEventIngestionWorkflowScheduler.cs:33-35`),
**When** two ingests of the same `(tenant,case,sourceUri)` race, or a duplicate instance is scheduled,
**Then** `SaveDedupKeyActivity` uses `When.NotExists` and compensates the loser, and duplicate-instance scheduling returns `Duplicate()`. Closes A28.

### Story 21.8: Tenant Registry CAS & Rollback Integrity

As a maintainer,
I want tenant status updates and registry rollback to be race-safe,
So that a deletion claim cannot be clobbered and a failed add cannot leave an invisible tenant.

**Acceptance Criteria:**

**Given** `UpdateTenantStatusAsync` is get-then-save without ETag while siblings use CAS (`TenantRegistryService.cs:150-170`),
**When** concurrent status updates occur,
**Then** ETag CAS with retry is used and entry+index are saved transactionally so rollback cannot orphan a tenant. Closes A47.

### Story 21.9: Blue/Green Embedding Migration

As an operator,
I want embedding-vector migration to be non-destructive with a real rollback and a locked marker,
So that a mid-run failure cannot strand a tenant with broken search and blocked writes.

**Acceptance Criteria:**

**Given** live migration currently drops indexes before generating vectors with a stub rollback (`EmbeddingVectorMigrationService.cs:224-321`),
**When** a migration runs and fails partway,
**Then** new vectors are written under a staging prefix/index, cutover is atomic, the previous index is retained for real rollback, and the marker uses `SET NX` ownership + TTL/heartbeat with an `--abort` path. Closes A5.

### Story 21.10: Migration Subsystem Test Coverage

As a test architect,
I want the migration subsystem covered by unit and real-vector integration tests,
So that the riskiest operation is validated before it touches live tenant data.

**Acceptance Criteria:**

**Given** `Migration/` (26 files) has one test file and the console tool has zero references,
**When** this story completes,
**Then** store/marker/generator unit tests and a 768→1024-dim real-vector integration migration exist, asserting `FT.INFO` dimension, rewritten keys, marker end-state, and the rollback-unavailable/`--abort` paths. Closes A22.

## Epic 22: RAG Retrieval Quality & Correctness
Developers and agents get retrieval that paginates correctly, bounds graph work, fuses axes on calibrated scores, carries case attribution, respects case scope on every path node, does not lose recall to post-filters, and exposes the built-but-stranded NL axis and reranking seams.
**Lifecycle label:** Product Capability / Retrieval Quality
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A8, A9, A29, A30, A48, A49, A50
**FRs reinforced:** FR22, FR34 · **NFRs reinforced:** NFR4, NFR24, NFR25

### Story 22.1: Semantic-Axis Pagination

As a developer,
I want `axis=semantic` to honor `Offset`,
So that paginating semantic search returns subsequent pages (FR22) instead of the same page forever.

**Acceptance Criteria:**

**Given** `SemanticSearchService.cs:64-81` ignores `query.Offset`,
**When** a semantic search is requested with a non-zero offset,
**Then** it fetches `offset+maxResults` neighbors and skips after enrichment, or rejects non-zero offsets with a documented error, with a pagination test. Closes A8.

### Story 22.2: Bounded, Cancellable Graph Traversal

As a developer,
I want graph traversals bounded and server-side cancellable,
So that a dense graph cannot exhaust FalkorDB CPU after the client gives up (NFR4).

**Acceptance Criteria:**

**Given** undirected `[*0..depth]` traversals with no `LIMIT` and a client-only `Task.WaitAsync` guard (`GraphQueryBuilder.cs:330,382`),
**When** a traversal runs,
**Then** it passes the server-side `timeout`, applies a `LIMIT`, and restricts `BuildTraverseFromNode` to semantic edge types. Closes A9.

### Story 22.3: Graph-Scoped & Hybrid Pagination Correctness

As a developer,
I want scoped and hybrid searches to paginate honestly,
So that clients can page results and deep results are reachable or explicitly capped.

**Acceptance Criteria:**

**Given** Mode-2 scans the whole inner set with growing OFFSET and hybrid caps at rank 100 with a fabricated `TotalCount` (`GraphScopedSearch.cs:206-242`, `HybridSearchService.cs:263-344`),
**When** scoped/hybrid searches paginate,
**Then** scope is pushed into the query (`INKEYS`/TAG pre-filter), `TotalCount` reflects real totals, and deep-pagination beyond the cap returns an explicit error. Closes A29.

### Story 22.4: Fusion Case Attribution, Score Calibration & Pinned Scorer

As a developer,
I want hybrid fusion to carry case attribution and fuse calibrated scores on a pinned scorer,
So that hybrid results are not silently degraded versus single-axis (FR34, NFR24, NFR25).

**Acceptance Criteria:**

**Given** `FusionEngine` drops `CaseId`, the scorer is unpinned (`SyntacticSearchService.cs:85-89`), and axes have differently-shaped score distributions,
**When** a hybrid search runs,
**Then** `CaseId` is carried through fusion, `SCORER BM25` is pinned, and fusion uses a scale-free method (RRF or per-axis min-max), with deterministic-score tests. Closes A30.

### Story 22.5: Case-Scoped Traversal Path Integrity

As a developer,
I want case-scoped traversal to constrain every path node to the case,
So that in-case results are not reachable only via other cases and hop scores do not leak cross-case structure.

**Acceptance Criteria:**

**Given** `BuildTraverseFromNode` constrains only the terminal node (`GraphQueryBuilder.cs:329-330`) while `BuildTraverseWithEdges` constrains all path nodes,
**When** a case-scoped graph search runs,
**Then** the all-path-nodes case predicate is applied, verified by a cross-case negative test. Closes A48.

### Story 22.6: Post-Filter Recall

As a developer,
I want metadata/source-type filters not to shrink results below available matches,
So that a filtered semantic search does not return zero while matches exist beyond top-K.

**Acceptance Criteria:**

**Given** filters are applied post-KNN over exactly `maxResults` neighbors (`SemanticSearchService.cs:136-138,263-266`),
**When** a filtered semantic/graph-scoped search runs,
**Then** the query over-fetches when a post-filter is present or applies the filter as a KNN pre-filter, with a recall test. Closes A49.

### Story 22.7: Retrieval Feature Completion

As a developer,
I want the built-but-stranded NL axis, weight tuning, highlighting, and a reranker seam,
So that half-built retrieval features are usable.

**Acceptance Criteria:**

**Given** `axis=nl` is unwired (`NaturalLanguageSemanticSearchService.cs:28-30`), fusion weights are hardcoded (`Program.cs:2541`), snippets are naive 200-char prefixes, and there is no reranker seam,
**When** this story completes,
**Then** `axis=nl` is wired into hybrid, fusion weights are tunable per query/tenant, RediSearch highlighting is used, and an `IResultFuser` reranker seam exists. Closes A50.

## Epic 23: Ingestion Pipeline Scalability & Resilience
Developers get ingestion that chunks documents, keeps workflow history small, survives provider rate limits, can actually re-ingest failed non-URL content, admits work without an actor bottleneck, batches directories efficiently, and isolates provider specifics behind a strategy.
**Lifecycle label:** Product Capability / Ingestion Scalability
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A11, A12, A13, A14, A15, A33, A34, A35, A51
**FRs reinforced:** FR6, FR12 · **NFRs reinforced:** NFR5, NFR22

**Execution note:** Story 23.9 must execute before Story 23.1 because content chunking depends on the provider batch API. The numeric keys are preserved to avoid unnecessary backlog churn; sprint-status `story_execution_order.epic-23` is authoritative for story selection.

### Story 23.9: EmbeddingClient Provider Strategy

As a maintainer,
I want provider specifics behind an `IEmbeddingProvider` strategy with a batch API,
So that adding a provider or chunking does not touch transport/auth/format at once.

**Acceptance Criteria:**

**Given** `EmbeddingClient.cs` (733 lines) mixes six responsibilities with hard-coded provider dispatch and a single-text API,
**When** this story completes,
**Then** an `IEmbeddingProvider` strategy (BuildRequest/ParseResponse/Authenticate) with a shared transport/auth-retry decorator and `GenerateBatchAsync` exists, provider knowledge is out of the workflow, and provider tests cover both providers. Closes A51.

### Story 23.1: Content Chunking & Batch Embedding

As a developer,
I want documents chunked and embedded in batches,
So that long documents embed reliably and retrieval granularity supports RAG relevance.

**Acceptance Criteria:**

**Given** one embedding is generated per whole (≤1MB) document with no token handling (`IngestionWorkflow.cs:156-159`),
**When** a document is ingested,
**Then** a token-aware splitter produces N vectors per unit under `{t}:vec:{id}:{seq}` and the provider batch API from Story 23.9 is used, with chunk-boundary and truncation tests. Closes A12.

### Story 23.2: Claim-Check Workflow Payloads

As a maintainer,
I want large content/vectors kept out of workflow history,
So that history size and replay cost stay bounded (NFR5).

**Acceptance Criteria:**

**Given** content and vectors are serialized into workflow history 6-8× per document (`IngestionWorkflow.cs:29-292`),
**When** an ingestion runs,
**Then** the producing activity persists the blob and passes `{id, hash}` between activities, with slimmed per-activity input records. Closes A11.

### Story 23.3: Retry-After-Aware 429 Orchestration

As a developer,
I want provider 429s handled by a durable Retry-After timer,
So that a transient rate limit does not become a permanent failed unit (NFR22).

**Acceptance Criteria:**

**Given** the generic retry budget (~16s) is shorter than the closed rate-limit window (≥90s) (`ActivityRetryPolicy.cs:12-21`, `RateLimiterLogic.cs:88-93`),
**When** the embedding provider returns 429,
**Then** the workflow performs a durable `CreateTimer(retryAfter)` before retrying and the window-open math is corrected, with a rate-limit-recovery integration test. Closes A13.

### Story 23.4: Non-URL Re-Ingestion

As an operator,
I want re-ingestion of failed non-URL units to work or fail clearly,
So that FR12 retries do not silently loop back to failed.

**Acceptance Criteria:**

**Given** `ReIngestionCoordinator.cs:139-155` rebuilds input with `ContentBytes = null`, rejected for non-URL sources,
**When** an operator re-ingests a failed File/Event unit,
**Then** a persisted content pointer is used, or the operation is rejected with an actionable error rather than scheduled to fail. Closes A14.

### Story 23.5: Rate-Limiter Admission Simplification

As a maintainer,
I want embedding admission control to cost one round trip,
So that the limiter is not the throughput ceiling (NFR5).

**Acceptance Criteria:**

**Given** three serialized actor round trips per embedding call (`GenerateEmbeddingActivity.cs:72-104`),
**When** an embedding is admitted,
**Then** a single `TryConsume(ceiling)` method or a Redis Lua token bucket is used and tenant config is cached, with a concurrency test. Closes A15.

### Story 23.6: Directory-Batch Scalability

As a developer,
I want directory batches scheduled efficiently with an extension allowlist,
So that large batches do not stall on O(n²) state writes or waste budget on unsupported files.

**Acceptance Criteria:**

**Given** per-file full-batch state rewrites and denylist-only filtering (`DirectoryIngestionService.cs:186-260`),
**When** a directory of N files is ingested,
**Then** batch state is checkpointed (not rewritten per file), scheduling is bounded-parallel, and `SupportedExtensions` is applied as an allowlist. Closes A33.

### Story 23.7: Index-Provisioning Ownership

As a maintainer,
I want index existence verified once per tenant, not per document,
So that ingestion does not block threads or spam warnings.

**Acceptance Criteria:**

**Given** each indexed document attempts `FT.CREATE` with exception-as-control-flow and `Thread.Sleep` (`IndexSyntacticActivity.cs:55-66,205`),
**When** documents are indexed,
**Then** index verification is memoized per tenant, index family, and expected schema per process, `Thread.Sleep` is replaced with `Task.Delay`, and the per-ingest warning is removed
**And** the readiness verifier inspects existing infrastructure only and never issues `FT.CREATE` or initializes a tenant graph/database.

**Given** an active tenant is missing a required index/database or has incompatible schema,
**When** an ingestion, indexing, search, graph, CLI, or MCP path checks readiness,
**Then** it fails before data access with `TENANT_NOT_PROVISIONED`, schema-mismatch, or equivalent structured evidence
**And** it does not create or repair tenant lifecycle resources on demand.

**Given** Epic 1 or Epic 5 rework is reviewed,
**When** ownership evidence is collected,
**Then** `TenantProvisioningWorkflow` remains the sole owner of tenant infrastructure creation
**And** focused source/architecture guards prove feature paths do not contain create-if-missing behavior. Closes A34 and preserves the Epic 0 ownership invariant.

### Story 23.8: Workflow Config Determinism

As a maintainer,
I want workflow orchestration to read config from its input, not mutable statics,
So that a config change mid-flight cannot break replay determinism.

**Acceptance Criteria:**

**Given** the orchestrator reads process-global statics (`IngestionWorkflow.cs:41,261`),
**When** an in-flight instance replays after a config change,
**Then** retry-policy/NL options are captured into the workflow input at scheduling time and no mutable static is read in orchestrator code, with a replay-determinism test. Closes A35.

## Epic 24: Observability & Performance Hardening
Operators can trace ingestion end-to-end, the read path stops paying avoidable round trips, tenant isolation moves toward physical enforcement with a scalable verifier, metrics land in one naming family with a committed dashboard, and hot-path write amplification is removed.
**Lifecycle label:** Operational Readiness / Observability & Performance
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A19, A26, A36, A46
**NFRs reinforced:** NFR8, NFR12, NFR28

### Story 24.1: Trace Propagation Across the Workflow Boundary

As an operator,
I want traces to follow an ingest request through workflow activities,
So that the async pipeline is observable (NFR28).

**Acceptance Criteria:**

**Given** no activity/workflow creates or links spans and no durabletask source is registered (`ServiceDefaults/Extensions.cs:88-101`),
**When** an ingest request runs,
**Then** `traceparent` is serialized into the workflow input, activities emit linked spans via a base class, and `Microsoft.DurableTask` is added as a trace source, verified by an end-to-end trace test. Closes A19.

### Story 24.2: Read-Path Caching & Tenant-List Bounding

As a developer,
I want tenant status/config/stats cached and the tenant list bounded,
So that search does not pay 4-6 auxiliary round trips and the dashboard does not stampede actors.

**Acceptance Criteria:**

**Given** no caching of tenant status/embedding config/corpus stats and an unbounded `GET /api/tenants` fan-out (`Program.cs:996-1008`),
**When** searches and tenant-list refreshes run,
**Then** a short-TTL cache invalidated on writes fronts those reads and the tenant list is paged with bounded concurrency. Closes A26.

### Story 24.3: Physical Tenant Isolation & Verifier Scaling (Decision-First)

As a solution architect,
I want a ratified physical tenant-isolation strategy and a scalable verifier,
So that isolation is enforced structurally (NFR8), not just detected pairwise.

**Acceptance Criteria:**

**Given** isolation is prefix-only on a shared Redis and the verifier is O(tenants²) deep-pagination (`TenantIsolationVerifier.cs:195-556`),
**When** this story completes,
**Then** the team ratifies a physical strategy (per-tenant Redis ACL user, or hash-tag/DB separation), the verifier uses cursor/aggregate checks, the runtime self-test is deleted, and `architecture.md` is updated. Frames A36; decision-first before enforcement implementation.

### Story 24.4: Metric Naming & Committed Dashboards

As an operator,
I want one metric-naming family and a committed dashboard,
So that emitted metrics are actually consumable.

**Acceptance Criteria:**

**Given** dot- and snake_case instruments coexist in `MemoriesMeter` and no dashboard exists in the repo,
**When** this story completes,
**Then** instruments use one naming family and at least one Grafana/Aspire dashboard is committed alongside `MetricTagKeyPolicy`. Closes A19 (metrics portion).

### Story 24.5: Hot-Path Write-Amplification Cleanup

As a maintainer,
I want read paths and background loops to stop over-writing state,
So that latency and memory stay bounded under load.

**Acceptance Criteria:**

**Given** `CorpusStatisticsActor` writes state on every read, activity streams are unbounded, the replay gate scans all instances per 5s, and the NL retry queue removes by JSON identity,
**When** these paths run,
**Then** reads return cached values, streams use `XADD MAXLEN` + a counter, the replay gate uses an app-owned in-flight set, and the NL queue is id-keyed. Closes A46.

## Epic 25: Architecture Factorization & Code Health
Maintainers get a thin composition root, centralized error/telemetry handling, a shared route table, a separated contract/persistence boundary, a consolidated CLI/MCP, a UX-conformant evidence cockpit, and a clean project topology — without changing product behavior.
**Lifecycle label:** Operational Readiness / Code Health
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A7, A21, A32, A37, A38, A39, A40, A43, A45
**NFRs reinforced:** NFR15

### Story 25.1: Program.cs Decomposition

As a maintainer,
I want endpoints extracted into per-resource classes,
So that the 3,836-line composition root becomes testable and merge-safe.

**Acceptance Criteria:**

**Given** 43 of 46 endpoints are inline lambdas (`Program.cs`),
**When** this story completes,
**Then** endpoints live in `{Ingestion,TenantLifecycle,Cases,Search,Graph,Consistency,Export}Endpoints` classes on route groups, the composition root is ≤ ~150 lines, and no product behavior changes (existing integration suite green). Closes A7.

### Story 25.2: Error & Telemetry Centralization

As a maintainer,
I want error envelopes, tenant validation, and telemetry scopes centralized,
So that duplicated idioms stop drifting and unhandled exceptions still return the envelope.

**Acceptance Criteria:**

**Given** 118 inline `ErrorResponse` constructions, a 10× `DAPR_UNAVAILABLE` clone, two tenant-validation idioms, and no `IExceptionHandler`,
**When** this story completes,
**Then** an `ErrorResults` factory, tenant-id/tenant-active endpoint filters, an `EndpointTelemetryFilter`, and one `IExceptionHandler` exist and are used. Closes A32.

### Story 25.3: Shared Route Table & Client Consolidation

As a maintainer,
I want routes defined once and the REST client de-duplicated,
So that a route rename cannot silently break consumers.

**Acceptance Criteria:**

**Given** routes are duplicated as 60 server + 23 client literals and `MemoriesClient` (1,307 lines) repeats a decode block 22×,
**When** this story completes,
**Then** a `MemoriesRoutes` table in Contracts is consumed by server and client, a single generic `SendAsync<T>` backs client methods, and `TraverseAsync` parameter order is corrected while `Experimental`. Closes A21.

### Story 25.4: Contract/Persistence Separation & Route Versioning

As a maintainer,
I want contracts free of backend names, versioned routes, and persistence DTOs split out,
So that a vector-store swap (NFR15) or a V2 does not break URLs or stored state.

**Acceptance Criteria:**

**Given** contracts leak Redis/FalkorDB names (`TenantIndexSizes.cs:17-20`), routes are unversioned, and `MemoriesJsonContext` is reused for persistence,
**When** this story completes,
**Then** contracts are axis-named, routes carry `/api/v1/`, and persistence DTOs are split out of the public Contracts package, preserving wire-shape compatibility for existing consumers. Closes A37.

### Story 25.5: CLI Consolidation

As a maintainer,
I want the CLI on `Client.Rest` with a generic formatter,
So that the CLI stops re-implementing HTTP and formatter ceremony.

**Acceptance Criteria:**

**Given** the CLI hand-rolls HTTP (no `Client.Rest` reference) and ships 14 clone JSON formatters,
**When** this story completes,
**Then** CLI commands consume `MemoriesClient` and a generic `JsonEnvelopeFormatter<T>` replaces the clones, with output-format and exit-code tests preserved. Closes A38.

### Story 25.6: MCP Tool Executor

As a maintainer,
I want MCP tools to share a validate/authorize/catch executor,
So that a new tool cannot silently lose tenant scoping.

**Acceptance Criteria:**

**Given** four tools repeat a 60-line skeleton with a redundant double authorization,
**When** this story completes,
**Then** an `McpToolExecutor.RunAsync(...)` owns validation, single-source tenant authorization, and error mapping, with tool-contract tests preserved. Closes A39.

### Story 25.7: Evidence Cockpit UX Conformance

As a future web user,
I want the evidence cockpit to follow FrontComposer/Fluent V5 rules and be localized,
So that the flagship trust surface conforms to the mandated UX rules.

**Acceptance Criteria:**

**Given** `MemoriesEvidenceCockpit.razor` uses raw `<h2>/<h3>` sibling sections, hardcoded English, and a hand-built evidence packet,
**When** this story completes,
**Then** sibling sections use `FluentAccordion`/`FluentLabel`, strings route through `EvidenceResourceKeys`, and a shared `EvidencePacketMapper.Unavailable(...)` is consumed, with bUnit conformance tests. Closes A40.

### Story 25.8: Dead-Code & Topology Cleanup

As a maintainer,
I want dead code removed and project boundaries resolved,
So that the topology matches its stated intent.

**Acceptance Criteria:**

**Given** an unregistered `RedisPreflightDedupStore` twin, dead `SupportedExtensions`/`:previous`/verifier self-test, and unclear `ServiceDefaults→Contracts`/`Web`/`Aspire`/`Redis` boundaries,
**When** this story completes,
**Then** dead code is deleted and each project boundary is either fixed, hosted, or documented as intentional. Closes A43, A45.

## Epic 26: Test, Deployment & Operational Readiness
Operators can deploy to production, back up and restore data, and rely on a coverage gate and real failure-mode tests; the empty integration stubs are closed and the operational runbook set is complete.
**Lifecycle label:** Operational Readiness / Deploy & Test
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A23, A24, A25, A42
**NFRs reinforced:** NFR7, NFR14, NFR16, NFR17

### Story 26.1: Production Deployment Artifacts

As an operator,
I want container images and deployment manifests with real config,
So that the system can be deployed to production from this repo.

**Acceptance Criteria:**

**Given** no Dockerfile/K8s/Helm/compose exists and release publishes NuGet only,
**When** this story completes,
**Then** SDK container publishing is enabled per Hexalith convention and a K8s overlay/Helm with resource limits and real Dapr component values (no echo LLM, no empty passwords) is committed and validated. Closes A24.

### Story 26.2: Backup & Restore

As an operator,
I want a restore counterpart to export plus a fidelity test,
So that data loss is recoverable by procedure (NFR16).

**Acceptance Criteria:**

**Given** export exists but there is no import/restore route,
**When** this story completes,
**Then** an import/restore endpoint consumes the export format, an integration test proves export→import fidelity (every Redis hash and FalkorDB edge), and backup/restore + DR runbooks exist. Closes A25 (feature portion).

### Story 26.3: Integration Stub Closure

As a test architect,
I want the empty integration stubs implemented or explicitly skipped,
So that failure-mode coverage is real, not apparent.

**Acceptance Criteria:**

**Given** 28 of 29 `[RunnableSkippedFact]` methods have empty `_ = _fixture;` bodies,
**When** this story completes,
**Then** retry, rate-limit, and degradation scenarios assert state-store end-state (or are marked with an explicit `Skip=` reason), and none silently pass without asserting. Closes A23.

### Story 26.4: Coverage Gating & Benchmark Lane

As a test architect,
I want a coverage gate and a benchmark CI lane,
So that regressions in the remediation epics are caught.

**Acceptance Criteria:**

**Given** CI never collects coverage and excludes `Program.cs`, and the NDCG benchmarks run in no lane,
**When** this story completes,
**Then** coverage collection + a threshold gate exist in CI and the benchmarks run in a nightly lane. Closes A42.

### Story 26.5: Operational Runbook Set

As an operator,
I want the missing operational runbooks,
So that production incidents and lifecycle operations have documented procedures.

**Acceptance Criteria:**

**Given** `docs/operations/` lacks capacity planning, incident response, index-rebuild, tenant onboarding/offboarding, upgrade/migration, and monitoring/alerting-threshold runbooks,
**When** this story completes,
**Then** each runbook exists under `docs/operations/` and is cross-linked from the deployment/failure-recovery docs. Closes A25 (docs portion).

### Story 26.6: Zot Release Contract Alignment

As a release maintainer,
I want container publication to use the shared Hexalith Zot credential and repository conventions only when an actual release is published,
So that ordinary `main` pushes do not fail on unused credentials and real releases publish discoverable images.

**Acceptance Criteria:**

**Given** semantic-release evaluates a commit that does not produce a release,
**When** the Release workflow runs,
**Then** it completes without attempting registry authentication or container pushes.

**Given** semantic-release reaches the container publish command,
**When** the publisher authenticates,
**Then** it consumes `HEXALITH_ZOT_REGISTRY`, `HEXALITH_ZOT_USERNAME`, and `HEXALITH_ZOT_API_KEY`,
**And** missing publish credentials fail at the publish boundary with an actionable, secret-safe message.

**Given** the default Hexalith Zot registry convention,
**When** Server and MCP images are built, rendered, verified, or published,
**Then** their targets are `registry.hexalith.com/memories:<version>` and `registry.hexalith.com/memories-mcp:<version>`,
**And** build-only validation remains credential-free,
**And** immutable-tag digest reconciliation and aggregate publication evidence remain intact.

**Given** release workflow and publisher fixtures run,
**Then** they reject early unconditional login, legacy secret names, and legacy nested repository names.

### Story 26.7: Restart-Recovery Reliability Gate

**Status:** done

As a reliability maintainer,
I want restart tests to expose the first terminal failure and prove replay-safe counter/workflow recovery,
So that NFR17 regressions are actionable and cannot be hidden by timeouts or flaky green runs.

**Acceptance Criteria:**

**Given** a persistence test expects workflow completion,
**When** the workflow reaches an unexpected terminal state,
**Then** the waiter fails immediately with safe status, topology-log, scripted-request, and counter-state diagnostics.

**Given** a deterministic counter transition is delayed or redelivered after later transitions,
**When** the case counter actor applies it,
**Then** the transition is idempotent across restart and persisted-state evolution remains backward compatible.

**Given** URL ingestion is in flight,
**When** the Aspire topology restarts,
**Then** the workflow reaches `Completed`,
**And** the pending counter survives restart,
**And** every counter bucket drains to zero at completion.

**Given** the correction is verified,
**Then** focused repetition and the full slow integration lane pass,
**And** evidence is not obtained by raising the timeout, suppressing terminal `Failed`, removing the restart, or weakening zero-loss assertions.

### Review Findings

- [x] [Review][Patch] Replace the Dictionary-order pseudo-LRU so refreshing a workflow actually prevents eviction and delayed replay double-counting [src/Hexalith.Memories.Server/Actors/CaseIngestionCounterLogic.cs:46]
- [x] [Review][Patch] Add persisted-state serialization compatibility coverage for legacy counter state and the new replay watermark [tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterLogicTests.cs:92]
- [x] [Review][Patch] Make the restart gate deterministically fail if nested HTTP resilience retries are reintroduced [tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs:175]

### Story 26.8: Benchmark Quality Calibration

**Status:** done

As a product-quality owner,
I want production hybrid-fusion defaults calibrated against the governed benchmark,
So that Epic 26 closes by meeting the product thesis without weakening its evidence gates.

**Acceptance Criteria:**

**Given** the fixed eight-query corpus, ground truth, top-10 NDCG scorer, strict `hybrid > best active single axis` rule, 80% threshold, and Redis/Falkor execution,
**When** production weighted-RRF calibration is applied and the complete Release benchmark runs,
**Then** all 17 tests pass with none skipped,
**And** at least 7 of 8 queries are strict hybrid wins,
**And** the approved calibration target is 8 of 8 wins with no per-query regression.

**Given** two independent complete benchmark runs,
**When** their per-query results are compared,
**Then** NDCG@10 metrics and win outcomes are identical.

**Given** explicit weights, persisted legacy weights, missing or empty axes, ties, attribution, score bounds, and NL default-off behavior,
**When** focused regression tests run,
**Then** all established compatibility and determinism contracts remain green.

**Given** verified green evidence,
**When** Epic 26 records are reconciled,
**Then** historical 6/8 evidence remains intact,
**And** Story 26.8, the benchmark action, the alignment action, and Epic 26 are marked done.

## Epic 27: Access Telemetry Lifecycle Hardening

Operators can configure and verify a bounded lifecycle for access telemetry through one explicitly owned write-only sink/store without weakening audit emission, tenant/privacy boundaries, or the PRD compliance boundary.

**Lifecycle label:** Operational Readiness / Security and Observability Hardening.

**Driven by:** Sprint Change Proposal 2026-07-16 (Access-Telemetry Retention Implementation), the approved Sprint Change Proposal 2026-07-19/20 (Story 27.3 C1 Production Adapter and Deployment Profile), the approved Sprint Change Proposal 2026-07-20 (Story 27.3 On-Premises PostgreSQL 18.4 Profile), the approved Sprint Change Proposal 2026-07-28 (blocked C1 gate split to Story 27.5 and C3/C4 ratification), and the approved Sprint Change Proposal 2026-07-30 (runtime secret-store substitution ratification and all-C1 ownership transfer).

**Sequencing gate:** Story 27.1 is decision-first. Stories 27.2 and 27.3 must not implement or claim a sink/store before its ownership, topology, failure, retention, purge, and validation contract is ratified.

**Qualification and close-out split:** Story 27.3 owns exact Production-adapter qualification and the immutable deployment profile. Story 27.4 consumes only that approved profile and owns deployment-shaped lifecycle evidence, operations documentation, and A41 close-out. Adapter rejection is a complete Story 27.3 outcome only when it preserves fail-closed writes and routes a new correct-course decision; it never closes A41.

**Amended 2026-07-28 by approved Sprint Change Proposal 2026-07-28.** Story 27.5 owns the thirteen C1 capability gates (C1.1-C1.12 and C1.14) for which no operator-executable producer can exist while the `PG-ONPREM-1` lifecycle environment is disabled, and owns authoring each gate's producer against the running target. Story 27.3 retains profile identity capture, capacity admission, declared-fault durability, backup/restore, both separated approvals, and the three non-C1 lanes (C2, C3, C4). Production lifecycle writes remain disabled until the gates of both stories pass: Story 27.3 reaching `done` does not enable them. Story 27.4's predecessor gate is met only when Stories 27.3 and 27.5 are both `done` at the same profile hash.

**Amended 2026-07-30 by approved Sprint Change Proposal 2026-07-30.** Story 27.5 now owns all twenty-five C1 child gates, C1.1-C1.25. The twelve gates Story 27.3 retained on 2026-07-28 — C1.13 and C1.15-C1.25 — transfer with their identifiers and evidence definitions unchanged. Story 27.3 retains C0 and the independent C2/C3/C4 lanes; its C1 umbrella closes only as an administrative scope-transfer record and is not a `passed` C1 result. AC1-AC5 remain unchanged as the shared fail-closed Production-profile contract, but their proof and completion ownership belongs to Story 27.5. Production lifecycle writes remain disabled while any Story 27.5 C1 gate is unproven. Story 27.3 reaching `done` neither enables writes nor advances Story 27.4, and A41 remains open and outside Story 27.3's mutation authority.

### Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)

As an architect and operator,
I want one ratified access-telemetry lifecycle contract,
So that implementation has an owned, deployable, and testable target.

**Acceptance Criteria:**

**Given** access telemetry currently reaches JSON console and optional OTLP export without a repository-owned bounded lifecycle,
**When** the decision evaluates external OTLP storage, a dedicated write-only store, and any file/volume alternative,
**Then** it selects one design and records ownership, topology, multi-replica write behavior, durability boundary, retention default/range, expiry/purge semantics, clock source, failure/backpressure policy, recovery, observability, privacy/tenant boundary, capacity assumptions, and rollback.

**Given** the production Server has two replicas and a read-only root filesystem,
**When** the decision is ratified,
**Then** no local-file approach is accepted without durable shared or per-replica storage, concurrency-safe rotation, pod-rescheduling behavior, and executable purge evidence; no unspecified external default is treated as a policy.

**Given** the PRD calls this infrastructure telemetry,
**When** the contract states its assurance boundary,
**Then** it does not claim tamper evidence, append-only integrity, legal compliance, or certified audit retention.

### Story 27.2: Bounded Retention/TTL and Purge Implementation

As an operator,
I want the ratified access-telemetry sink/store to enforce a bounded lifecycle,
So that emitted access records do not grow without limit.

**Acceptance Criteria:**

**Given** Story 27.1's ratified contract,
**When** Server and deployment configuration are applied,
**Then** access events enter the selected write-only sink/store with the documented bounded duration and expiry/purge behavior, while existing required audit emission remains continuous.

**Given** valid, invalid, missing, minimum, and maximum lifecycle settings,
**When** the host starts in Development and Production,
**Then** configuration validation follows the ratified fail-closed/degraded policy and never silently falls back to unbounded retention.

**Given** two Server writers, restart/rescheduling, backpressure, and temporary sink/store failure,
**When** access events are emitted,
**Then** behavior matches the ratified delivery and recovery contract and low-cardinality health/metrics expose loss or degradation without secrets, raw content, or unbounded tenant labels.

**Given** two authorized tenant contexts and rejected/unknown scope,
**When** records are written, expired, purged, and inspected through any supported operational seam,
**Then** tenant/privacy boundaries fail closed and focused cross-tenant negative tests name the affected storage, routing, and evidence surfaces.

### Story 27.3: Production Adapter Manifest, Unit, and Deployment-Lane Qualification

As a Platform Operations and security review pair,
I want the Production state-store adapter's manifests, adapter code contract, and deployment-verification lane qualified on repository-executable evidence,
So that the running-target C1 capability qualification in Stories 27.5 and 27.6 starts from a reviewed, statically-bound, and lane-verified adapter.

**Scope superseded 2026-07-31 by approved Sprint Change Proposal 2026-07-31** (`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md`, DW 27.3-CR35). All twenty-five C1 gates now belong to Stories 27.5 (C1.15-C1.25) and 27.6 (C1.1-C1.14). Story 27.3 retains C0 and the independent C2/C3/C4 lanes only. The prior title and statement — "Production Adapter and Deployment Profile" / "one immutable Production state-store profile qualified against every C1 gate" — described pre-transfer scope and no longer bind. Story 27.3 reaching `done` advances no C1 gate, enables no Production lifecycle write, and does not advance Story 27.4. A41 remains open and outside this story's mutation authority.

**Acceptance Criteria:**

1. The exact `PG-ONPREM-1` runtime, component, PostgreSQL 18.4 backend, Dapr control plane, application images, component/config manifests, actor/Scheduler identities, configuration epoch, profile hash, node/storage capacity, and operating cost are captured from the running on-premises target. *Transferred 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR31, CR34). Every gate this criterion governs is owned by Story 27.5 (C1.15-C1.25) or Story 27.6 (C1.1-C1.14). The criterion text is retained unchanged as the governing definition those stories inherit; it states no obligation Story 27.3 can discharge, and Story 27.3 reaching `done` satisfies none of it. AC5's fail-closed rule continues to bind both successor stories.*
2. Capacity is proven for the 1-hour, configured 24-hour, and 7-day horizons: every operand is normalized to integer bytes/counts, the arithmetic is checked, and the computed result is admitted against the approved 70/80/90% threshold table without skip. Narrowed 2026-07-28 by approved Sprint Change Proposal 2026-07-28: CRUD, strong reads, ETags, rollback-atomic multi-key transactions, TTL, actor reactivation, Placement/Scheduler/reminder recovery, request bounds, two-writer 500 events/s throughput, 150,000-record purge catch-up, isolation, encryption, and cohort-attributable physical reclamation transfer to Story 27.6 as gates C1.1-C1.12 and C1.14, keeping their gate identifiers. Amended 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR31, CR34): the destination is Story 27.6, not Story 27.5, and configured capacity (C1.13) joins them there - its running-target binding is retained and the passing unit lane is a precondition, not discharge, withdrawing the 2026-07-30 ruling that C1.13 needs no activation gate. No gate is dropped, weakened, or made discharge-able by a unit lane. *Transferred 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR31, CR34). Every gate this criterion governs is owned by Story 27.5 (C1.15-C1.25) or Story 27.6 (C1.1-C1.14). The criterion text is retained unchanged as the governing definition those stories inherit; it states no obligation Story 27.3 can discharge, and Story 27.3 reaching `done` satisfies none of it. AC5's fail-closed rule continues to bind both successor stories.*
3. Forced loss and replacement of the PostgreSQL container/process proves zero loss of every acknowledged record while the single node and retained local volume remain healthy. Node, local-volume, control-plane, and site loss are explicitly outside profile; backup/restore evidence and the resulting nonzero RPO/RTO are published without an HA claim or overstatement. *Transferred 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR31, CR34). Every gate this criterion governs is owned by Story 27.5 (C1.15-C1.25) or Story 27.6 (C1.1-C1.14). The criterion text is retained unchanged as the governing definition those stories inherit; it states no obligation Story 27.3 can discharge, and Story 27.3 reaching `done` satisfies none of it. AC5's fail-closed rule continues to bind both successor stories.*
4. Hexalith Platform Operations approves node/storage capacity, operating cost, operation, bounded fault, backup/restore, upgrade, rollback, and reclamation evidence and explicitly acknowledges the absence of node/disk/site HA; a separate security reviewer approves identity, secrets, TLS, network, authorization, encryption, privacy, and evidence integrity. *Transferred 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR31, CR34). Every gate this criterion governs is owned by Story 27.5 (C1.15-C1.25) or Story 27.6 (C1.1-C1.14). The criterion text is retained unchanged as the governing definition those stories inherit; it states no obligation Story 27.3 can discharge, and Story 27.3 reaching `done` satisfies none of it. AC5's fail-closed rule continues to bind both successor stories.*
5. Any missing digest, placeholder, profile drift, failed probe, missing approval, or unreserved capacity keeps Production writes disabled, Story 27.3 `in-progress`, Story 27.4 `backlog`, and A41 open. Extended 2026-07-28 by approved Sprint Change Proposal 2026-07-28: the same fail-closed rule binds the thirteen gates transferred to Story 27.5. Production writes remain disabled while any of them is unproven, and Story 27.3 reaching `done` neither enables them nor advances Story 27.4. *Transferred 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR31, CR34). Every gate this criterion governs is owned by Story 27.5 (C1.15-C1.25) or Story 27.6 (C1.1-C1.14). The criterion text is retained unchanged as the governing definition those stories inherit; it states no obligation Story 27.3 can discharge, and Story 27.3 reaching `done` satisfies none of it. AC5's fail-closed rule continues to bind both successor stories.*
6. The kind-based production-deployment-verification lane renders and applies the production manifests verbatim to a disposable cluster from the four release OCI archives. Because that disposable cluster has no OpenBao runtime, the lane refuses to run against any non-disposable context, then enumerates the applied Dapr secret-store Components from the cluster and substitutes the live `spec.type` of every Component whose rendered type is `secretstores.hashicorp.vault` with `secretstores.kubernetes` before the health stages. The substituted set is discovered by type, never by a fixed set of Component names, so an added vault-typed Component cannot be silently left unpatched. Zero vault-typed Components is a passing observation, not a failure: it is the Story 31.2 end state in which the production manifests apply unmodified, and the lane must remain able to reach it. A failed Component enumeration is distinguished from that end state and fails the lane. The lane records the substitution and the per-Component observed post-patch types read back from the cluster in `secret-store-substitution.json`, writes that disclosure before raising any substitution failure, produces verification evidence, validates that evidence, and uploads the evidence artifact; any failed render, verbatim apply, disposable-context assertion, Component enumeration, patch, post-patch readback, health stage, evidence production, evidence validation, or evidence upload fails the lane rather than being skipped. The resulting health proof is for a cluster that differs from the rendered Production manifests: every health-stage observation occurs after the live substitution, so no health stage exercises any vault-typed secret store and the lane does not prove the Production OpenBao/vault secret-resolution path. Added 2026-07-27 by approved Sprint Change Proposal 2026-07-27 (DW 27.3-CR15); amended 2026-07-30 by approved Sprint Change Proposal 2026-07-30 to disclose the deliberate runtime deviation without weakening the desired Production secret-store contract; amended 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR32) to state the dynamic-enumeration contract the shipped lane implements, restore evidence production and upload to the fail list, and harmonize this copy with the Story 27.3 copy. This criterion is independent of AC1-AC5: it neither requires nor unblocks C1, and passing it advances no C1 gate and enables no Production lifecycle write. AC5 remains unchanged.
7. The `Hexalith.Memories.AccessTelemetry.Tests` unit lane proves the `state.postgresql/v2` access-telemetry adapter contract — transactional record-plus-expiry-index write and delete, ETag and `FirstWrite` semantics, `Conflict`/`StaleIndex`/`VerificationFailed`/`AlreadyAbsent` classification, ordering parity, and bucket-identity matching — against in-process fakes, executed from a fresh Release build under the story's Checkpoint Execution Contract. Added 2026-07-28 by approved Sprint Change Proposal 2026-07-28, ratifying checkpoint C3. This criterion is independent of AC1-AC5: it proves the adapter code contract, never the running target; passing it advances no C1 gate, enables no Production lifecycle write, and does not satisfy C1.11, whose cross-tenant denial must be observed against the running profile.
8. The `ProductionDeploymentArtifactsTests` lane statically binds the reviewed `PG-ONPREM-1` manifests — `connectionString` resolving only through `secretKeyRef`, `actorStateStore: "true"`, `skipVerify`/`tlsServerName`, the ordered first-match `pg_hba` rules, least-privilege init-SQL grants, the RBAC secret-reader Roles, the deny-default lifecycle ACL, and the connection-pool arithmetic against `max_connections` including `maxSurge`/`maxUnavailable` — executed under its own class selector from a fresh Release build under the story's Checkpoint Execution Contract. Added 2026-07-28 by approved Sprint Change Proposal 2026-07-28, ratifying checkpoint C4. This criterion is independent of AC1-AC5: it proves the manifests say what they must, never that the running deployment behaves as they say; passing it advances no C1 gate and enables no Production lifecycle write.

### Story 27.4: Retention Verification, Operations Runbook, and A41 Close-Out

As a security reviewer,
I want executable lifecycle evidence and one coordinated close-out against the approved Production profile,
So that A41 closes only after the policy works in the deployment shape.

**Predecessor Gate:**

- Story 27.3 is `done` with C0 and C2-C4 complete against the immutable `PG-ONPREM-1` profile and with all otherwise-governed remediation and ledger obligations closed. Its administratively closed C1 umbrella is not qualification evidence and does not advance this story by itself.
- Story 27.5 is `done`: all twenty-five C1 child gates (C1.1-C1.25) are `passed` on their required evidence from the exact running `PG-ONPREM-1` target at the same profile hash. Added 2026-07-28 by approved Sprint Change Proposal 2026-07-28 and extended to all C1 gates by approved Sprint Change Proposal 2026-07-30.
- The live profile hash at Story 27.4 start exactly matches Story 27.3. A mismatch returns ownership to Story 27.3 and keeps writes disabled. A mismatch between Stories 27.3, 27.5 and 27.4 has the same effect.

**Acceptance Criteria:**

**Given** the approved immutable profile, a short test retention window, and a production-shaped deployment,
**When** old and new access events cross the expiry boundary across at least two Server writers and controlled workload, sidecar, actor, Placement, Scheduler, and backend-fault recovery,
**Then** focused evidence proves exact acknowledgement, durable recovery, expired-record unavailability and purge, newer-record preservation, required audit emission continuity, and tenant/privacy denial before dependencies.

**Given** the ratified Production duration and exact adapter profile,
**When** operators deploy, monitor, change, fail over, recover, or roll back the policy,
**Then** telemetry, deployment configuration, capacity, monitoring, incident, recovery, adapter-reclamation, and decommission documentation identifies the owner, configuration, defaults, storage impact, purge verification, alarms, rollback, RPO/RTO limits, and assurance boundary.

**Given** C2-C6, terminal validation, and publish verification all pass against the unchanged approved profile,
**When** A41 is closed,
**Then** `20.5-A41-ACCESS-TELEMETRY-RETENTION` is reconciled from `carried-forward`, the matching sprint action is closed, architecture and all A41 summaries cite the same canonical evidence, and Epic 20/Story 20.5 remain historical `done` records rather than being reopened.

**Transferred scope:** Former Story 27.3 Tasks 2-8 move here without gate reduction: multi-writer/replacement/durability proof; expiry, purge, newer-record preservation, and cohort-attributable reclamation; failure/privacy/authority/health/metrics/alerts; runbook and exact-adapter appendix; residual reconciliation; evidence-backed A41 mutation; and terminal governed validation. A41 remains `carried-forward` and its sprint action remains `open` until every checkpoint and publish verification passes.

### Story 27.5: Running PG-ONPREM-1 Capability Qualification

As a Platform Operations and security review pair,
I want the eleven C1 identity, declared-fault durability, and approval gates for the exact running `PG-ONPREM-1` target proven on their authored evidence,
So that the Production adapter's behavior is qualified rather than asserted.

**Origin:** split out of Story 27.3 on 2026-07-28 by approved Sprint Change Proposal 2026-07-28, executing the Administrator decision of 2026-07-27 (Story 27.3 code review, eighth-invocation review). The thirteen gates keep their existing identifiers — C1.1-C1.12 and C1.14 — so every prior citation in the Story 27.3 ledger, the deferred register, and the committed evidence packets stays resolvable.

**Amended 2026-07-30 by approved Sprint Change Proposal 2026-07-30.** C1.13 and C1.15-C1.25 transfer from Story 27.3 with their identifiers, owners, and evidence definitions unchanged. Together with the 2026-07-28 transfer of C1.1-C1.12 and C1.14, Story 27.5 owns all C1.1-C1.25. Story 27.3's closed umbrella is a scope-transfer record, not qualification evidence. **Superseded in part 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR31, CR34):** C1.1-C1.14, including C1.13, move to new Story 27.6. Story 27.5 retains C1.15-C1.25. No gate is dropped, weakened, or renumbered.

**Activation gate:** this story must not be set `ready-for-dev` until the `PG-ONPREM-1` lifecycle Deployments are scaled above zero with the production flag and profile hash set and the clock authorities pointed at real endpoints. Until then no operator-executable producer can exist for any of its gates and the story stays `backlog`. This is the reopen trigger recorded in Story 27.3's C1 gate table on 2026-07-27.

**Predecessor gate:** the approved `PG-ONPREM-1` planning record and ADR pin `profile_sha256 dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`, and Story 27.3's C4 lane statically guards the reviewed manifests. Story 27.5 qualifies that exact profile; it does not rely on Story 27.3 owning C1.15-C1.18. A hash mismatch at start returns profile-definition ownership to Story 27.3 and keeps every Story 27.5 gate `not complete`.

**Acceptance Criteria:**

**Given** the eleven C1 child gates retained by this story and the exact immutable profile definition,
**When** the running target becomes available,
**Then** each gate's own operator-executed command is authored and recorded in its checkpoint row before any completion state changes, and no row is discharged by a shared or unrelated command.


**Given** the exact running target and the approved threshold, fault, recovery, and approval contract,
**When** the remaining C1 gates are evaluated,
**Then** the profile identity and epoch, configured-capacity admission, declared-fault acknowledged-record durability, out-of-profile boundaries, successful backup/restore with published nonzero RPO/RTO, node/storage cost and capacity, non-HA acknowledgement, and separate Platform Operations and security approvals each pass on their own row's evidence without substitution or skip.

**Given** any gate that is unproven, skipped, stale, or discharged by a non-running-target artifact,
**When** the story is evaluated,
**Then** Production lifecycle writes remain disabled, Story 27.4 remains `backlog`, A41 remains open, and rejection of `PG-ONPREM-1` routes a new correct-course decision rather than a weakened gate or a substituted profile.

**Implementation evidence:** one checkpoint row per gate, each carrying its accountable owner, its own evidence command or artifact or accepted activation blocker, a consequence and reopen trigger, a review state, and a completion state — the `story-scope-guard.md:32-34` condition. A shared umbrella state never discharges a child gate. Amended 2026-07-31: registered below as eleven rows for C1.15-C1.25.

**Boundary:** Story 27.5 owns proof and completion of C1.15-C1.25 only (amended 2026-07-31 by approved Sprint Change Proposal 2026-07-31; C1.1-C1.14 belong to Story 27.6). Story 27.3 retains C0 and the independent C2/C3/C4 lanes plus its otherwise-governed remediation and ledger obligations. No repository path transfers in this record-only correction: existing tools and evidence packets are read/verify inputs, and Story 27.5 declares future producer paths when `create-story` authors its implementation artifact. Story 27.5 owns no runbook, A41 mutation, product-code, or manifest-authoring outcome. It never mutates `20.5-A41-ACCESS-TELEMETRY-RETENTION`.

#### Historical Context Classification — Story 27.5

| Reference | Classification | Permitted influence on Story 27.5 |
| :-------- | :------------- | :-------------------------------- |
| Story 27.3 whole-story shape, including its original 25-gate C1 bundle | `anti-template` | Transfer only the exact ratified C1 gate identifiers, owners, evidence definitions, and fail-closed rules. Do not copy Story 27.3's tasks, non-C1 checkpoints, File List, ledger breadth, review history, or status shape. |
| Approved 2026-07-28 C1 split proposal | `historical-reference-only` | Authority and provenance for the activation gate; not a template for another partial split. |
| Approved 2026-07-30 proposal | `historical-reference-only` | Authority and provenance for all-C1 ownership and the preserved fail-closed consequences; never evidence that a gate passed. |
| Current ADR 27.1 `PG-ONPREM-1` profile and current C1 evidence definitions | `current-narrow-pattern` | Re-verified exact profile identity, thresholds, faults, and the one-row-per-gate evidence contract only; whole-story shapes remain excluded. |
| Approved 2026-07-31 proposal | `historical-reference-only` | Authority for the 11/14 registration split and the C1.13 running-target rebinding; never evidence that a gate passed. |

#### Slice Proof — Story 27.5

Story 27.5 has one outcome: accept or reject the exact running `PG-ONPREM-1` profile on its authored-evidence gates — identity and epoch (C1.15-C1.18), declared-fault durability and recovery boundaries (C1.19-C1.22), and the approval record (C1.23-C1.25). It remains one explicitly approved checkpoint story because all eleven appear in one table, each with an accountable owner, its own evidence artifact, review state, completion state, consequence, and reopen trigger. No shared umbrella state discharges a gate. Story 27.5 owns no C0, C2, C3, C4, runbook, A41 mutation, product-code, or manifest-authoring outcome, and no C1.1-C1.14 gate.

**Boundary disclosure (2026-07-31).** The split between Stories 27.5 and 27.6 is by **record shape**, not delivery independence: Story 27.5's eleven gates carry an authored evidence artifact, while Story 27.6's fourteen carry only an accepted activation blocker. Both stories unblock at the same moment — when the `PG-ONPREM-1` lifecycle Deployments scale above zero. Only C1.20 and C1.24, both published statements, are authorable before then. This boundary is recorded as guard-satisfying registration under `story-scope-guard.md:41-43`, not claimed as two independently deliverable outcomes.

#### C1 Checkpoint Table — Story 27.5

Eleven rows, transferred verbatim on 2026-07-31 from `sprint-change-proposal-2026-07-30.md` Annex A with identifiers, accountable owners, evidence definitions, consequences, and reopen triggers unchanged. Every row registers as `pending | not complete | —`. Transfer completes no gate.

| Gate | AC | Accountable owner | Required evidence observation, command, artifact, or accepted blocker | Consequence and reopen trigger | Review state | Completion state | Completion date |
| :--- | :-- | :---------------- | :--------------------------------------------------------------- | :----------------------------- | :----------- | :--------------- | :-------------- |
| C1.15 Runtime and control-plane identity | AC1 | Deployment adapter owner | Packet observation: Dapr runtime version, sidecar image digest, Scheduler connections, actor types, enabled features and alpha opt-in, captured from the running deployment rather than package pins. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.16 Component and backend identity | AC1 | Deployment adapter owner | Packet observation: component type, API version, capabilities, backend identity, and PostgreSQL 18.4 version. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.17 Image, manifest and epoch identity | AC1 | Deployment adapter owner | Packet observation: application image digests, component/config manifest identity, configuration epoch, and component manifest/profile hash with its coverage statement. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.18 Node/storage capacity and operating cost | AC1 | Hexalith Platform Operations | Packet observation: node/storage capacity, host-filesystem headroom for the non-reserving local PVC, and operating cost. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.19 Declared-fault zero acknowledged-record loss | AC3 | Deployment adapter owner | Packet observation: PostgreSQL pod/process forcibly lost and its StatefulSet pod replaced while node and retained local volume remain healthy; every Dapr-acknowledged record remains present. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.20 Out-of-profile statement | AC3 | Hexalith Platform Operations | Packet observation: node, local-volume, control-plane, and site loss explicitly published as outside profile. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.21 Backup destination and successful restore | AC3 | Hexalith Platform Operations | Packet observation: named backup destination and successful restore result. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.22 Published RPO/RTO without HA claim | AC3 | Hexalith Platform Operations | Packet observation: resulting nonzero RPO and RTO for out-of-profile failures, with no node/disk/site HA claim. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.23 Platform Operations approval | AC4 | Hexalith Platform Operations | Packet observation: separate approval of node/storage capacity, operating cost, operation, bounded fault, backup/restore, upgrade, rollback, and reclamation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact approval, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.24 Non-HA acknowledgement | AC4 | Hexalith Platform Operations | Packet observation: explicit acknowledgement of absent node, disk, and site HA. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact acknowledgement, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.25 Security reviewer approval | AC4 | Independent security reviewer | Packet observation: separate approval of identity, secrets, TLS, network, authorization, encryption, privacy, and evidence integrity. **Accepted blocker:** no independent security approver is currently assigned. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when an independent approver is assigned and the exact approval, transition, and reviewer confirmation exist. | pending | not complete | — |

### Story 27.6: Running PG-ONPREM-1 Data-Path and Capability Gate Proof

As a Platform Operations and security review pair,
I want the fourteen C1 data-path, throughput, isolation, and capacity gates proven against the exact running `PG-ONPREM-1` target,
So that the Production adapter's runtime behavior is qualified rather than asserted.

**Origin:** split out of Story 27.5 on 2026-07-31 by approved Sprint Change Proposal 2026-07-31, executing Administrator decisions `DW 27.3-CR31` and `DW 27.3-CR34`. The fourteen gates keep their existing identifiers — C1.1-C1.14 — so every prior citation in the Story 27.3 ledger, the deferred register, and the committed evidence packets stays resolvable. No gate is dropped, weakened, renumbered, or made discharge-able by a unit lane.

**Activation gate:** unchanged and inherited from Story 27.5 — this story must not be set `ready-for-dev` until the `PG-ONPREM-1` lifecycle Deployments are scaled above zero with the production flag and profile hash set and the clock authorities pointed at real endpoints. Until then no operator-executable producer can exist for any of its gates and the story stays `backlog`. Production writes remain disabled, Story 27.4 remains `backlog`, and A41 remains open while any gate is unproven.

**Predecessor gate:** the approved `PG-ONPREM-1` planning record and ADR pin `profile_sha256 dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`, and Story 27.3's C4 lane statically guards the reviewed manifests. Story 27.6 qualifies that exact profile. A hash mismatch at start returns profile-definition ownership to Story 27.3.

**Acceptance Criteria:**

**Given** the fourteen C1 child gates retained by this story and the exact immutable profile definition,
**When** the running target becomes available,
**Then** each gate's own operator-executed command is authored and recorded in its checkpoint row before any completion state changes, and no row is discharged by a shared or unrelated command.

**Given** the running `PG-ONPREM-1` target at the approved profile hash,
**When** the capability probe runs,
**Then** CRUD, strong reads, ETags, rollback-atomic multi-key transactions with a fault injected on a later operation and no partial record or expiry-index commit, effective TTL, actor reactivation, and Placement/Scheduler/reminder recovery after control-plane disruption all pass without skip.

**Given** the ADR two-writer workload at 500 events/s during purge,
**When** the 30-minute steady-state window and the 10-minute 150,000-due-record purge backlog run,
**Then** request bounds hold, acknowledged loss is zero, p99 transaction latency stays below the configured 3-second Dapr client timeout, p95 regression against the same-profile no-purge baseline stays at or below 10%, and the backlog drains within five minutes with oldest-due age below 15 minutes.

**Given** two authorized tenant contexts against the running profile,
**When** isolation and encryption are observed,
**Then** physical cross-tenant denial is proven with focused negative evidence naming the affected surfaces, per the project-context tenant-isolation rule, and TLS `verify-full` plus the at-rest encryption posture are recorded. The existing `DeleteAndVerifyAsync_EntryCarryingAnotherTenantMarker_IsDeniedAndLeavesTheRecordPurgeable` unit test does not satisfy this criterion: both its records resolve to one state key, so it exercises envelope-hash mismatch rather than tenant isolation.

**Given** a purge cohort,
**When** reclamation is measured,
**Then** the collector and its bound are named and physical space reclamation is attributed to that cohort.

**Given** any gate that is unproven, skipped, stale, or discharged by a non-running-target artifact,
**When** the story is evaluated,
**Then** Production lifecycle writes remain disabled, Story 27.4 remains `backlog`, A41 remains open, and rejection of `PG-ONPREM-1` routes a new correction rather than a silent completion.

**Implementation evidence:** one checkpoint row per gate, each carrying its accountable owner, its own evidence command or artifact or accepted activation blocker, a consequence and reopen trigger, a review state, and a completion state — the `story-scope-guard.md:32-34` condition. A shared umbrella state never discharges a child gate.

**Boundary:** Story 27.6 owns proof and completion of C1.1-C1.14 only. Story 27.5 retains C1.15-C1.25. Story 27.3 retains C0 and the independent C2/C3/C4 lanes. Story 27.6 owns no runbook, A41 mutation, product-code, or manifest-authoring outcome. It never mutates `20.5-A41-ACCESS-TELEMETRY-RETENTION`.

#### Historical Context Classification — Story 27.6

| Reference | Classification | Permitted influence on Story 27.6 |
| :-------- | :------------- | :-------------------------------- |
| Story 27.3 whole-story shape, including its original 25-gate C1 bundle | `anti-template` | Transfer only the exact ratified C1 gate identifiers, owners, evidence definitions, and fail-closed rules. Do not copy Story 27.3's tasks, non-C1 checkpoints, File List, ledger breadth, review history, or status shape. |
| Approved 2026-07-28 C1 split proposal | `historical-reference-only` | Authority and provenance for the activation gate; not a template for another partial split. |
| Approved 2026-07-30 proposal | `historical-reference-only` | Authority and provenance for all-C1 ownership and the preserved fail-closed consequences; never evidence that a gate passed. |
| Current ADR 27.1 `PG-ONPREM-1` profile and current C1 evidence definitions | `current-narrow-pattern` | Re-verified exact profile identity, thresholds, faults, and the one-row-per-gate evidence contract only; whole-story shapes remain excluded. |
| Approved 2026-07-31 proposal | `historical-reference-only` | Authority for the 11/14 registration split and the C1.13 running-target rebinding; never evidence that a gate passed. |
| Story 27.5's own 25-gate bundle as registered on 2026-07-30 | `anti-template` | Transfer only the C1.1-C1.14 identifiers and evidence definitions. Do not copy its slice proof, table breadth, or acceptance-criteria density. |

#### Slice Proof — Story 27.6

Story 27.6 has one outcome: accept or reject the exact running `PG-ONPREM-1` profile on its data-path, throughput, isolation, capacity, and reclamation gates (C1.1-C1.14). It remains one explicitly approved checkpoint story because all fourteen appear in one table, each with an accountable owner, its own accepted activation blocker, review state, completion state, consequence, and reopen trigger — the sanctioned form for proof that cannot yet run. No shared umbrella state discharges a gate. Story 27.6 owns no C0, C2, C3, C4, runbook, A41 mutation, product-code, or manifest-authoring outcome, and no C1.15-C1.25 gate.

**Boundary disclosure (2026-07-31).** The split between Stories 27.5 and 27.6 is by **record shape**, not delivery independence: Story 27.5's eleven gates carry an authored evidence artifact, while Story 27.6's fourteen carry only an accepted activation blocker. Both stories unblock at the same moment — when the `PG-ONPREM-1` lifecycle Deployments scale above zero. Only C1.20 and C1.24, both published statements, are authorable before then. This boundary is recorded as guard-satisfying registration under `story-scope-guard.md:41-43`, not claimed as two independently deliverable outcomes.

#### C1 Checkpoint Table — Story 27.6

Fourteen rows, transferred verbatim on 2026-07-31 from `sprint-change-proposal-2026-07-30.md` Annex A with identifiers, accountable owners, accepted activation blockers, consequences, and reopen triggers unchanged, except C1.13, which is rewritten per `DW 27.3-CR34`. Every row registers as `pending | not complete | —`. Transfer completes no gate.

| Gate | AC | Accountable owner | Required evidence observation, command, artifact, or accepted blocker | Consequence and reopen trigger | Review state | Completion state | Completion date |
| :--- | :-- | :---------------- | :--------------------------------------------------------------- | :----------------------------- | :----------- | :--------------- | :-------------- |
| C1.1 CRUD | AC2 | Deployment adapter owner | **Accepted activation blocker:** required running-target create/read/update/delete round trip on `state.postgresql/v2`; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.2 Strong reads | AC2 | Deployment adapter owner | **Accepted activation blocker:** required post-write strong-consistency read observation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.3 ETags | AC2 | Deployment adapter owner | **Accepted activation blocker:** required ETag match/mismatch and `FirstWrite` insert-semantics observation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.4 Rollback-atomic multi-key transactions | AC2 | Deployment adapter owner | **Accepted activation blocker:** required later-operation fault injection with no partial record or expiry-index commit; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.5 TTL | AC2 | Deployment adapter owner | **Accepted activation blocker:** required effective TTL-expiry observation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.6 Actor reactivation | AC2 | Deployment adapter owner | **Accepted activation blocker:** required actor-state survival across deactivation/reactivation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.7 Placement / Scheduler / reminder recovery | AC2 | Deployment adapter owner | **Accepted activation blocker:** required Placement and Scheduler reconnection and reminder firing after control-plane disruption; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.8 Request bounds | AC2 | Deployment adapter owner | **Accepted activation blocker:** required request size/count bound enforcement; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.9 Two-writer 500 events/s throughput | AC2 | Deployment adapter owner + Hexalith Platform Operations | **Accepted activation blocker:** required `--workload-profile adr-27.1-two-writer-500eps --steady-state-minutes 30` observation: ADR mix, zero acknowledged loss, p99 below 3s, and no more than 10% p95 regression; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.10 150,000-record purge catch-up | AC2 | Deployment adapter owner + Hexalith Platform Operations | **Accepted activation blocker:** required `--purge-backlog-records 150000` observation: ten-minute backlog drains within five minutes and oldest-due age stays below fifteen minutes; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.11 Isolation | AC2 | Deployment adapter owner + independent security reviewer | **Accepted activation blocker:** required physical cross-tenant denial on the running profile; the one-key envelope-hash unit test is insufficient and the command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.12 Encryption | AC2 | Deployment adapter owner + independent security reviewer | **Accepted activation blocker:** required TLS `verify-full` and at-rest-encryption posture observation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.13 Capacity | AC2 | Deployment adapter owner + Hexalith Platform Operations | **Accepted activation blocker:** required configured-capacity admission against the exact running target over the 1h / 24h / 7d horizons against the approved 70/80/90% threshold table (its byte values, and the rule that exactly 80% is critical rather than an admissible peak, live in `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` under `### Production-Shaped Execution Contract`); the command is not authorable until activation. **Precondition, not discharge:** `AdapterProfileTests.test_capacity_inputs_fail_closed` and `AdapterProfileTests.test_capacity_result_is_admitted_against_profile_thresholds` must pass (`PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_adapter_profile.py'`), but passing them advances no gate — `epics.md` AC2 and the running-target Given/When/Then bind configured-capacity admission to "the exact running target", and `story-scope-guard.md:68-69` forbids closing on internal-only proof. Amended 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR34), withdrawing the 2026-07-30 ruling that C1.13 "needs no activation gate". | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens, this row's running-target command is authored, and reviewer confirmation exists. | pending | not complete | — |
| C1.14 Cohort-attributable physical reclamation | AC2 | Deployment adapter owner + Hexalith Platform Operations | **Accepted activation blocker:** required named collector/bound and cohort-attributable physical-space reclamation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |

## Epic 28: Owner-Approved EventStore Runtime Adoption

Memories source and package modes converge on the exact EventStore runtime identity authorized by
EventStore Story 1.20 while preserving the existing zero-code DAPR ingestion contract.

**Lifecycle label:** Operational Readiness / EventStore Dependency Adoption.

**Activation gate:** Epic 28 remains backlog and Story 28.1 has no implementation file until
EventStore Story 1.20 durably records `final_decision: available`,
`authorize_consumer_migration: true`, a 40-hex `tested_runtime_sha`, named owner approval, and the
approved package version and SHA-256 inventory. A current tag, repository HEAD, or unapproved package
version is insufficient.

### Story 28.1: Adopt Owner-Approved EventStore Runtime Identity

**Status:** backlog. **Owner:** Memories Maintainer + EventStore Maintainer.

As a Memories maintainer,
I want source and package modes aligned to the owner-approved EventStore runtime identity,
So that zero-code event ingestion is tested against one auditable dependency contract.

**Given** EventStore Story 1.20 remains blocked, non-authorizing, incomplete, or lacks any required
source/package/approval identity,
**When** Memories backlog selection runs,
**Then** this story remains `backlog`, no EventStore or Builds gitlink is changed, and no existing
ingestion, projection, or deployment topology is redesigned.

**Given** Story 1.20 authorizes migration and names the approved EventStore source SHA,
**When** Debug/source mode is adopted,
**Then** `references/Hexalith.EventStore` gitlink and checkout both equal that SHA, the EventStore
submodule is not edited, and only Memories-root-declared submodules are initialized.

**Given** Story 1.20 names the approved 14-package version and hashes,
**When** Release/package mode restores from an isolated cache,
**Then** `Hexalith.EventStore.Client`, `Hexalith.EventStore.Aspire`, and every resolved
`Hexalith.EventStore*` asset use that exact version, fetched package bytes match the approved hashes,
no EventStore project reference enters the Release asset graph, and the selected `Hexalith.Builds`
gitlink already exposes that version.

**Given** Memories' existing EventStore integration,
**When** dependency adoption is implemented,
**Then** it preserves `AddMemoriesServerServices()` → `AddServerEventStoreIntegration()` →
`AddMemoriesEventStoreIntegration()`, `UseCloudEvents()`, `MapControllers()`,
`MapSubscribeHandler()`, `/events/ingest`, the `pubsub` component, and
`MEMORIES_EVENTSTORE_TOPIC` without introducing direct REST ingestion for domain event streams.

**Given** source and package identities are aligned,
**When** validation runs,
**Then** Debug/source and Release/package builds pass, exact Client/Aspire assets are proven, focused
EventStore/Server contract tests pass, and a real DAPR publish proves a persisted and searchable
memory result while duplicate replay is ignored.

**Given** adoption exposes a behavioral incompatibility,
**When** it cannot be resolved without changing the zero-code ingestion contract or topology,
**Then** this story fails closed and routes that behavior change to a separately approved
compatibility story rather than expanding silently.

## Epic 29: OpenBao-First Dapr Secret Management

Aspire-hosted services resolve application secrets exclusively through Dapr secret-store components
backed by OpenBao. Kubernetes Secrets remain permitted only for unavoidable bootstrap credentials or
direct pod inputs that Dapr cannot inject.

**Lifecycle label:** Operational Readiness / Secret Management Hardening.

**Driven by:** Sprint Change Proposal 2026-07-19 — OpenBao-First Aspire Secret Management.

**Sequencing gate:** Story 29.1 establishes the AppHost OpenBao topology. Story 29.2 consumes that topology
and must not claim provider-neutral composition or Dapr access verification before Story 29.1's resource,
bootstrap, isolation, and readiness contract is executable.

### Story 29.1: OpenBao-Backed AppHost Secret Topology

As a developer and operator,
I want the Aspire AppHost to provision and initialize OpenBao-backed Dapr secret stores,
So that local and deployed application code use the same provider-neutral secret-access boundary.

**Acceptance Criteria:**

**Given** the Aspire AppHost starts the Memories topology,
**When** secret infrastructure is composed,
**Then** AppHost adds a pinned, health-checked OpenBao resource with a safe development profile that cannot silently become a production deployment
**And** `secretstore` and `access-telemetry-secrets` use `secretstores.hashicorp.vault`
**And** the stores use separate least-privilege policies and secret prefixes.

**Given** a service consumes an application secret,
**When** its Dapr sidecar starts,
**Then** it waits for OpenBao initialization and receives only its required Dapr component
**And** application secret payloads are not stored in local-file or Kubernetes secret-store components.

**Given** OpenBao requires bootstrap or one-time seeding material,
**When** AppHost supplies it,
**Then** local bootstrap uses Aspire secret parameters or protected temporary files
**And** Kubernetes Secrets are allowed only for required deployed bootstrap tokens and CA certificates or direct pod inputs Dapr cannot provide
**And** secrets never appear in source control, configuration, logs, diagnostics, or Aspire model output.

**Given** the OpenBao-backed topology is running,
**When** integration verification executes,
**Then** successful Dapr secret reads, cross-prefix denial, health, and restart recovery are proven without disclosing secret values.

### Story 29.2: Provider-Neutral Aspire Composition and Secret Verification

As an Aspire integration consumer,
I want the reusable Memories Aspire APIs to accept externally provisioned Dapr secret-store resources,
So that consumers can use OpenBao without product code depending on OpenBao.

**Acceptance Criteria:**

**Given** a consumer composes Memories through `Hexalith.Memories.Aspire`,
**When** it supplies a Dapr secret-store resource,
**Then** reusable Aspire extensions do not hard-code `secretstores.local.file`
**And** Server, access-telemetry lifecycle, and clock sidecars reference their required secret-store components.

**Given** embedding, lifecycle bootstrap, or clock code requires a secret,
**When** it resolves the value,
**Then** it uses `DaprClient.GetSecretAsync`
**And** product projects contain no OpenBao SDK, HTTP client, endpoint, or provider credentials.

**Given** standalone Dapr templates, tests, and operations documentation are reviewed,
**When** Story 29.2 completes,
**Then** they follow the OpenBao-first rule and document every remaining Kubernetes Secret exception
**And** automated topology and integration tests prove both Dapr secret components resolve values from OpenBao without exposing secret values.

---

## Epic 30: CI/CD Pipeline Ownership and Alignment

Memories adopts the EventStore-shaped Hexalith CI/CD model: reusable Hexalith.Builds workflows own standard build, test, and release mechanics, while Memories retains only named module-specific verification and recovery lanes. Tenants supplies compatible coverage and consumer-validation patterns where those shared inputs fit. The existing four-image container release and partial-recovery scope remains independently owned inside this epic.

**Lifecycle label:** Operational Readiness / CI/CD Engineering.

**Alignment target:** EventStore's shared-core plus companion-lane structure, with Tenants-style coverage and consumer validation where supported. Alignment must not weaken tenant-negative evidence, web E2E, integration, deployment, benchmark, package-inventory, or partial-release recovery gates.

**Driven by:** Sprint Change Proposal 2026-07-26, executing DW 27.3-CR5 (split approved by the Administrator on 2026-07-21 during Story 27.3 code review).

**Scope origin:** The pipeline artifacts already exist in the repository and were ledgered as an external CI/CD lane while bundled into Story 27.3's single C1 adapter-qualification slice. This epic gives them an owner, not a new implementation.

**Scope boundary (restated at epic level 2026-07-27; carried from the pre-split Story 30.1).** Epic 30 owns the release and publish lane only. Production deployment rendering, verification, and evidence tooling (`tools/render-production-deployment.ps1`, `tools/verify-production-deployment.ps1`, `tools/validate-production-deployment-evidence.ps1`) and the `production-deployment-verification` CI job remain owned by Story 27.3, which declares them under acceptance criterion AC6 and checkpoint C2. Story 30.3 owns `tools/publish-containers.ps1`, which produces the four archives that lane consumes, and must not regress it.

### Story 30.1: Guarded Release Dispatch and Shared Caller Adoption

**Status:** backlog. **Owner:** Memories Maintainer.

**Split note (2026-07-27).** Created by approved Sprint Change Proposal 2026-07-27 executing DW 27.3-CR16. The pre-split Story 30.1 carried seven Given/When/Then blocks and eight checkpoints with no owner, evidence command, review state or completion state, reproducing the anti-template shape its own split was executed to cure. Its scope is now Stories 30.1, 30.3, 30.4 and 30.5; no scope was added or dropped. The pre-split activation gate moved to Stories 30.3, 30.4 and 30.5, which are the stories that actually require multi-container Hexalith.Builds support. **Story 30.1 has no external activation gate.**

As a maintainer,
I want release publication to be reachable only from a guarded dispatch against a shared, pinned caller,
So that no release can be published from an unverified source, an unprotected environment, or an unpinned shared workflow.

**Acceptance Criteria:**

**Given** an operator intentionally dispatches a release from `main`,
**When** the release caller starts,
**Then** an unprotected preflight proves the dispatch SHA is the current `main` tip with successful exact-source push CI
**And** the release job uses a protected `production` environment, `cancel-in-progress: false`, and `domain-release.yml` pinned to the same approved 40-character Hexalith.Builds SHA passed as `builds-execution-sha`
**And** ordinary pushes to `main` never publish a release.

**Given** exact-source CI already tested the release candidate,
**When** the reusable release job is invoked,
**Then** `test-projects` remains empty to avoid duplicate release compute
**And** `expected-package-count` is fixed at `9`
**And** any failed source, package-count, environment, destination-absence, or Builds-identity proof stops before publication.

**Given** the shared publication preflight reads `packages[].id`,
**When** Memories adopts the shared release,
**Then** `tools/release-packages.json`, its schema, validators, pack scripts, recovery tooling, and fixtures migrate atomically from `packageId` to `id`
**And** the canonical inventory remains exactly the existing nine package IDs.

**Implementation evidence:** The story file must carry a checkpoint table in which every row has an accountable owner, an exact evidence command or artifact, a review state, and a completion state. Required rows: guarded-dispatch preflight rejection of a non-tip SHA; protected-environment and pinned-Builds-SHA configuration; no-publish-on-push proof; and the atomic `packageId` to `id` migration with the nine-ID inventory unchanged.

**Owned paths:** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`. Story 27.3 retains the declared cross-story edit it made under the 2026-07-26 Administrator decision.

### Story 30.2: Shared CI Core and Module-Specific Verification Lanes

**Status:** backlog. **Owner:** Memories Maintainer + Hexalith.Builds Maintainer.

As a maintainer,
I want Memories CI aligned to the shared Hexalith.Builds contract,
So that standard checks remain consistent across modules without losing Memories-specific evidence.

**Acceptance Criteria:**

**Given** pull requests and pushes to `main`,
**When** `ci.yml` runs,
**Then** its standard restore, Release build, warnings-as-errors, and compatible per-project test work is delegated to `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main`
**And** test projects and platform selection are explicit rather than inferred.

**Given** Memories has verification that the reusable workflow does not model,
**When** the pipeline is reorganized,
**Then** story-file scope, tenant-negative evidence, tooling fixtures, release-package topology, web E2E, fast integration, and production-deployment verification remain named local jobs or companion workflows
**And** nightly slow integration and benchmark lanes remain intact.

**Given** consumer validation, coverage, or package validation cannot use an existing shared input without weakening evidence,
**When** alignment is implemented,
**Then** the missing reusable capability is added to Hexalith.Builds or retained locally with a documented exception
**And** shared workflow logic is not copied into Memories.

**Given** commit and pull-request title validation,
**When** a pull-request title is opened, synchronized, reopened, or edited, or a commit reaches `main`,
**Then** commitlint runs with the pull-request title supplied explicitly and enforces the repository Conventional Commit contract.

**Given** the aligned pipeline is proposed for required-check adoption,
**When** old and new lanes are compared,
**Then** every existing required gate has equivalent or stronger executable evidence, stable check names are documented for branch protection, TRX and coverage evidence remain downloadable, and duplicate work is removed only after equivalence is proven.

**Implementation evidence:** The story file must contain a lane-by-lane migration table naming the old owner, new owner, trigger, required-check name, validation command or artifact, and rollback path.

### Story 30.3: Four-Image Publication Contract

**Status:** backlog. **Owner:** Memories Maintainer.

**Split note (2026-07-27).** Created by approved Sprint Change Proposal 2026-07-27 executing DW 27.3-CR16, from the pre-split Story 30.1.

**Activation gate:** Story 30.3 must not enter implementation until an owner-approved Hexalith.Builds revision supports a frozen multi-container publication identity and repeated per-container verification without phase collisions. The current single `container_repository` identity and single-use container phase do not satisfy the four-image contract.

As a maintainer,
I want all four release images published and verified under one declared mapping set,
So that every shipped container has a provable identity, platform set, and health contract.

**Acceptance Criteria:**

**Given** the approved multi-container Hexalith.Builds contract,
**When** semantic-release publishes version `${nextRelease.version}`,
**Then** the caller supplies exactly these mappings:

- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj|memories`
- `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj|memories-mcp`
- `src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj|memories-access-telemetry`
- `src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj|memories-access-telemetry-clock`

**And** every image is verified against its declared platforms and workload-appropriate health contract
**And** Memories-specific production-deployment asset generation remains in the caller rather than being copied into Hexalith.Builds.

**Given** registry push authorization,
**When** the pipeline authenticates,
**Then** the authorization mode in use is recorded, and any registry-side limitation that blocks an authenticated push is captured as a named blocker with owner, consequence, and reopen trigger rather than worked around silently.

**Known risk (carried, not yet re-verified):** a prior investigation recorded that the zot registry rejected challenge-response push authentication - Docker and skopeo pushes returned 401 while a preemptive single-connection preflight probe succeeded - and suspected a registry-side replica/state issue requiring server-side diagnosis. Re-verify this against the current registry before treating the publish path as green; if it reproduces, record it as an accepted blocker per the acceptance criterion above.

**Implementation evidence:** The story file must carry a checkpoint table in which every row has an accountable owner, an exact evidence command or artifact, a review state, and a completion state. Required rows: the four declared mappings; per-image platform verification; per-image health verification; and the recorded registry authorization mode with any named blocker.

**Owned paths:** `tools/publish-containers.ps1`, `tools/verify-container-registry.ps1`, the four `tests/tooling/publish_containers/*` suites, and the four-image expansion of `docs/dev/release-runbook.md`.

**Downstream obligation:** `tools/publish-containers.ps1` produces the four `.tar.gz` archives consumed by the `production-deployment-verification` lane that Story 27.3 AC6 and checkpoint C2 declare. Story 30.3 must not regress that lane.

### Story 30.4: Partial-Release Recovery

**Status:** backlog. **Owner:** Memories Maintainer.

**Split note (2026-07-27).** Created by approved Sprint Change Proposal 2026-07-27 executing DW 27.3-CR16, from the pre-split Story 30.1.

**Activation gate:** Story 30.4 must not enter implementation until the approved Hexalith.Builds revision emits evidence sufficient for partial-release recovery.

As a maintainer,
I want a publish run that fails after some images are pushed to recover deterministically,
So that a partial release is completed exactly once without overwriting or retagging what already shipped.

**Acceptance Criteria:**

**Given** a publish run that fails after some images are pushed,
**When** recovery is authorized,
**Then** `.github/workflows/recover-partial-release.yml` consumes immutable release evidence, proves the exact source/version/inventory, skips already-published members, and publishes only the missing members
**And** recovery never overwrites, retags, or silently treats an ambiguous destination response as success.

**Implementation evidence:** The story file must carry a checkpoint table in which every row has an accountable owner, an exact evidence command or artifact, a review state, and a completion state. Required rows: an exercised partial-failure scenario; the source/version/inventory proof; the skip-already-published result; and a negative proof that an ambiguous destination response fails rather than passes.

**Owned paths:** `.github/workflows/recover-partial-release.yml`, `tools/complete-partial-release.ps1`.

### Story 30.5: Release Cutover Parity and Rollback

**Status:** backlog. **Owner:** Memories Maintainer.

**Split note (2026-07-27).** Created by approved Sprint Change Proposal 2026-07-27 executing DW 27.3-CR16, from the pre-split Story 30.1.

**Activation gate:** Story 30.5 must not enter implementation until Stories 30.1, 30.3 and 30.4 are `done`, because parity is proven against their completed lanes.

As a maintainer,
I want the migration from the existing custom publisher proven at parity before the old path is removed,
So that cutover cannot lose a capability and rollback cannot alter anything already published.

**Acceptance Criteria:**

**Given** the existing automatic release and custom publisher,
**When** migration is cut over,
**Then** a dry run and controlled release rehearsal prove nine-package, four-image, GitHub Release asset, registry authorization, failure, and recovery parity
**And** the old path is removed only after parity succeeds
**And** rollback restores the prior caller without changing a published version or mutable tag.

**Implementation evidence:** The story file must carry a checkpoint table in which every row has an accountable owner, an exact evidence command or artifact, a review state, and a completion state. Required rows: the dry-run result; the controlled rehearsal result; the lane-by-lane parity comparison; the old-path removal, gated on parity; and a rollback proof showing no published version or mutable tag changed.

**Owned paths:** the cutover and rollback sections of `docs/dev/release-runbook.md`.

---

## Epic 31: OpenBao Secrets Platform and Runtime Secret-Store Migration

The deployed OpenBao `hexalith-keys` platform and the runtime Dapr `secretstore` migration from Kubernetes Secrets to `hashicorp.vault` are owned, hardened, documented, and security-reviewed as an independently deployable operations platform.

**Lifecycle label:** Operational Readiness / Secret Management Platform.

**Driven by:** Sprint Change Proposal 2026-07-26, executing DW 27.3-CR6 (split approved by the Administrator on 2026-07-21 during Story 27.3 code review).

**Scope boundary:** Epic 29 owns the Aspire/AppHost-local OpenBao topology and provider-neutral composition. Epic 31 owns the deployed-cluster platform and the runtime secret-store migration. Neither epic's stories may be closed on the other's evidence.

**Scope origin:** The platform is already deployed and operational — the 2026-07-20 live probe resolved the access-telemetry store through it. This epic formalizes ownership, hardening, and documentation of a running platform; it does not stand one up.

### Story 31.1: OpenBao Platform Hardening and Documentation

**Status:** review (dev-story 2026-07-28: post-review continuation complete, checkpoint C3 discharged by a smoke-test re-run under the CA-only projection; `done` gated on the Platform Operations `helm diff` and on checkpoints C4/C5, which the approved 2026-07-28 sprint change keeps `not complete`). **Owner:** Memories Maintainer + security reviewer.

**Split note (2026-07-27).** Created by approved Sprint Change Proposal 2026-07-27 executing DW 27.3-CR16. The pre-split Story 31.1 bundled platform hardening and the runtime secret-store migration - two independently deployable outcomes - with no checkpoint table. Its scope is now Stories 31.1 and 31.2; no scope was added or dropped.

**Scope boundary:** Epic 29 owns the Aspire/AppHost-local OpenBao topology and provider-neutral composition. Story 31.1 owns the deployed-cluster platform only. Neither story may be closed on the other's evidence.

As an operator and security reviewer,
I want the deployed OpenBao platform documented at its exact configuration and its limitations recorded as accepted,
So that the secret-management boundary is reviewable on its own evidence before anything is migrated onto it.

**Acceptance Criteria:**

**Given** the deployed OpenBao `hexalith-keys` platform,
**When** its topology is reviewed,
**Then** `deploy/openbao/values.yaml`, `namespace.yaml`, `service-account-hardening.yaml`, and `smoke-test.yaml` are documented in `docs/operations/openbao.md` with their exact deployed configuration
**And** the smoke test is runnable with a named command and recorded result.

**Given** the deployed availability profile as measured - OpenBao Raft voters co-located on a single Kubernetes node, so the node is the whole failure domain regardless of voter count,
**When** the security reviewer evaluates it,
**Then** the static file-based OpenBao seal - the unseal key held in a Kubernetes Secret beside the data - and the namespace-wide port 8200 ingress are surfaced explicitly as accepted limitations of that single-node-hosted profile, each with owner, consequence, compensating controls, and a reopen trigger
**And** neither limitation is described as hardened or production-HA
**And** the documented voter count and HA mode match the running platform rather than the tracked manifest.

**Given** platform documentation and evidence,
**When** either is produced,
**Then** no unseal key, recovery key, root or operator token, or other secret value appears in `docs/operations/openbao.md`, any evidence artifact, or any test snapshot (NFR9, project-context "Never expose secrets").

**Implementation evidence:** The story file must carry a checkpoint table in which every row has an accountable owner, an exact evidence command or artifact, a review state, and a completion state. Required rows: the four documented topology files at their deployed configuration; the executed smoke test with its command and result; each accepted limitation of the single-node-hosted profile with owner, consequence, compensating controls and reopen trigger; and the security reviewer's recorded evaluation.

**Owned paths:** `deploy/openbao/values.yaml`, `namespace.yaml`, `service-account-hardening.yaml`, `smoke-test.yaml`, `docs/operations/openbao.md`, and — ratified by the approved Sprint Change Proposal 2026-07-28 (Story 31.1 scope ratifications), which is this clause's authority — `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` (new) and `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` (deliberate update of `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`).

### Story 31.2: Runtime Dapr Secret-Store Migration to `hashicorp.vault`

**Status:** ready-for-dev (create-story 2026-07-28). **Owner:** Memories Maintainer + security reviewer.

**Split note (2026-07-27).** Created by approved Sprint Change Proposal 2026-07-27 executing DW 27.3-CR16, from the pre-split Story 31.1.

**Activation gate:** Story 31.2 must not enter implementation until Story 31.1 is `done`, so that the migration is evaluated against a documented platform whose accepted limitations are already on record. **Gate reading ratified 2026-07-28 by the Administrator, during Story 31.1's second-pass code review:** "enter implementation" means a `dev-story` execution, not story preparation. Story 31.2 may sit at `ready-for-dev` while Story 31.1 is `review`; it must not be developed until Story 31.1 is `done`. This reading is recorded because `sprint-status.yaml` and this file had drifted apart on it.

**Scope boundary:** Epic 29 owns the Aspire/AppHost-local topology. Story 31.2 owns the deployed-cluster runtime secret-store component only. Neither story may be closed on the other's evidence.

As an operator and security reviewer,
I want the runtime Dapr secret store migrated from Kubernetes Secrets to `hashicorp.vault`,
So that runtime secret resolution crosses one reviewed boundary and every remaining Kubernetes Secret is justified.

**Acceptance Criteria:**

**Given** the runtime `secretstore` component,
**When** the migration completes,
**Then** `deploy/kubernetes/base/dapr/secretstore.yaml` uses `hashicorp.vault` with the `eventstore` and `memories` scopes
**And** every remaining Kubernetes Secret is documented as an unavoidable OpenBao bootstrap credential or a direct pod input outside the DAPR secret-store boundary (NFR9).

**Given** secret-resolution behavior,
**When** structural and integration tests run,
**Then** no product project contains an OpenBao SDK, HTTP client, endpoint, or provider credential, and secret values are never exposed in logs, telemetry, CLI output, or test snapshots (NFR9, project-context "Never expose secrets").

**Implementation evidence:** The story file must carry a checkpoint table in which every row has an accountable owner, an exact evidence command or artifact, a review state, and a completion state. Required rows: the migrated component with both scopes proven by a live scoped read; a per-Secret justification inventory for every remaining Kubernetes Secret; the structural no-SDK proof; and a negative proof that a secret value cannot reach logs, telemetry, CLI output, or a snapshot.

**Owned paths:** `deploy/kubernetes/base/dapr/secretstore.yaml`.

**Retained by Story 27.3:** the access-telemetry-specific secret components and the `PG-ONPREM-1` secret backing remain in Story 27.3 adapter scope and are not migrated by this story.

