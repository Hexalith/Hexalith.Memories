---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
filesIncluded:
  prd:
    - _bmad-output/planning-artifacts/prd.md
  architecture:
    - _bmad-output/planning-artifacts/architecture.md
  epics:
    - _bmad-output/planning-artifacts/epics.md
  ux:
    - _bmad-output/planning-artifacts/ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-03
**Project:** memories

## Document Discovery

The canonical assessment set confirmed for this run is:

- PRD: `prd.md` (88,353 bytes; modified 2026-07-19 16:20:02 +0200)
- Architecture: `architecture.md` (121,376 bytes; modified 2026-08-02 11:06:53 +0200)
- Epics and stories: `epics.md` (382,690 bytes; modified 2026-08-03 13:09:25 +0200)
- UX design: `ux-design-specification.md` (99,240 bytes; modified 2026-06-27 08:02:38 +0200)

No sharded versions were found, so there are no whole-versus-sharded conflicts. No required document category is missing.

### Additional Filename Matches Excluded from the Canonical Set

The following whole Markdown documents matched the discovery patterns but were confirmed as excluded supplementary sprint-change proposals:

- `sprint-change-proposal-2026-05-18-epic-1-observable-proof-guard.md` (9,747 bytes; modified 2026-06-02 17:54:55 +0200)
- `sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` (17,841 bytes; modified 2026-06-27 08:08:21 +0200)
- `sprint-change-proposal-2026-07-06-epic-0-evidence-map.md` (4,623 bytes; modified 2026-07-06 18:09:33 +0200)
- `sprint-change-proposal-2026-07-06-epic-17-deferred-web-triage.md` (4,978 bytes; modified 2026-07-06 18:15:52 +0200)
- `sprint-change-proposal-2026-07-06-epic17-browser-at-gap-closure.md` (12,987 bytes; modified 2026-07-06 18:10:08 +0200)
- `sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md` (10,985 bytes; modified 2026-07-16 12:44:58 +0200)
- `sprint-change-proposal-2026-07-16-epic-0-evidence-map-maintenance.md` (8,531 bytes; modified 2026-07-16 10:16:14 +0200)
- `sprint-change-proposal-2026-07-16-epic-26-benchmark-closure.md` (17,490 bytes; modified 2026-07-16 12:55:51 +0200)
- `sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md` (24,777 bytes; modified 2026-07-27 08:14:01 +0200)
- `sprint-change-proposal-2026-07-28-architecture-anchor-reverification.md` (9,327 bytes; modified 2026-07-28 20:14:45 +0200)
- `sprint-change-proposal-2026-07-28-epic-ac-code-verification.md` (21,436 bytes; modified 2026-07-28 16:01:46 +0200)
- `sprint-change-proposal-2026-07-28-epic-ac-verification-route-and-binding-coverage.md` (40,125 bytes; modified 2026-07-28 20:18:52 +0200)
- `sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md` (29,405 bytes; modified 2026-08-03 13:09:25 +0200)
- `sprint-change-proposal-2026-08-02-epic-23-documentation-verification.md` (27,427 bytes; modified 2026-08-02 18:54:46 +0200)

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

| NFR | Requirement or metric | Target / verification | Conditions | Phase |
|---|---|---|---|---|
| NFR1 | Syntactic search latency (p95) | <200ms | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| NFR2 | Semantic search latency (p95) | <500ms | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| NFR3 | Hybrid search latency (p95) | <1s | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| NFR4 | Graph traversal latency (p95) | <2s | 10 concurrent queries/tenant, 10K memory units/tenant, depth ≤5 | MVP |
| NFR5 | Ingestion throughput | >100 memory units/min (payloads ≤10KB), >10 memory units/min (payloads ≤1MB) | Per tenant, single-document embedding calls (not batched) | Ongoing |
| NFR6 | Event indexing freshness | <5s from DAPR pub/sub publication to searchable under normal conditions; degradation documented when embedding provider is rate-limited | Per event | P1.5 |
| NFR7 | Cold start time | Service fully operational within 60s | From containers running to accepting queries — excludes image pull time | Ongoing |
| NFR8 | Zero cross-tenant data leakage — no search, ingestion, or graph traversal returns data from another tenant | Automated test suite: search, ingest, graph across all axes with malformed/empty/swapped tenant IDs. Graph-specific test: create identical graph structures in tenant A and B, traverse from tenant A, verify zero nodes from tenant B appear even if edge IDs collide | — | MVP |
| NFR9 | Product services retrieve embedding-provider and other application runtime secrets exclusively through the DAPR Secrets API, backed by OpenBao in Aspire and deployed environments. Secret values are never stored in application configuration or ordinary environment variables. Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or direct pod inputs outside the DAPR secret-store boundary. | Structural dependency tests, secret scanning, AppHost topology tests, and integration tests proving DAPR reads from OpenBao without secret disclosure | — | Ongoing |
| NFR10 | All inter-service communication authenticated via DAPR API tokens | DAPR configuration validation | — | Ongoing |
| NFR11 | External access authenticated at ingress layer — no unauthenticated access to REST API endpoints | Integration test with unauthenticated requests | — | P1.5 |
| NFR12 | System supports linear scaling of tenants — adding a new tenant does not degrade existing tenant performance by more than 5% | Validated at 10 tenants, each with 100K memory units. Methodology: benchmark tenant 1 alone, add 9 loaded tenants, re-benchmark tenant 1, measure delta | — | Ongoing |
| NFR13 | Per-tenant ingestion pipeline scales independently — one tenant's batch ingestion does not block another tenant's real-time ingestion | Concurrent ingestion test across 3 tenants | — | Ongoing |
| NFR14 | Redis memory footprint per memory unit is predictable and documented — operator can estimate infrastructure costs before tenant provisioning | Published sizing guide: memory per unit by vector dimension and metadata size | — | Ongoing |
| NFR15 | Architecture must not preclude backend migration (Redis → Qdrant) — concrete implementation with clear extraction points identified, no premature interfaces | Architecture review: extraction points documented, no tight coupling to Redis-specific APIs in domain logic | — | Ongoing |
| NFR16 | Zero memory unit loss during Redis restart | AOF persistence enabled and verified | — | MVP |
| NFR17 | Ingestion pipeline state survives process restarts — queued and in-progress units resume without data loss | DAPR actor state persistence verified | — | MVP |
| NFR18 | Partial backend failure (one of three backends down) results in degraded service, not total failure — available axes continue serving results | Chaos test: kill each backend individually, verify partial results returned | — | Ongoing |
| NFR19 | Failed ingestion units are never silently dropped — all failures visible via CLI status with error details and failure stage | End-to-end test with intentional failures at each pipeline stage | — | Ongoing |
| NFR20 | MCP tool responses conform to MCP protocol specification — valid tool schemas, typed parameters, structured error responses | MCP protocol conformance test suite | — | P1.5 |
| NFR21 | DAPR pub/sub integration handles CloudEvents envelope format — events from any DAPR-compatible publisher are processable | Integration test with standard CloudEvents payloads | — | P1.5 |
| NFR22 | Embedding provider integration handles rate limiting gracefully — 429 responses trigger backoff without pipeline crash or data loss | Rate limit simulation test per provider | — | Ongoing |
| NFR23 | CLI connects to the memory server via configurable endpoint — supports local dev (localhost), container (docker service name), and remote (ingress URL) environments | Configuration layering test across all three environments | — | Ongoing |
| NFR24 | Hybrid fusion uses deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0; single-axis explain still documents axis-specific score semantics | Fusion and explain unit tests with known rankings/weights | — | MVP |
| NFR25 | Fusion algorithm produces deterministic scores — same query against same data produces identical composite scores. Result ordering within the same score tier may vary. | Determinism test: 100 repeated queries, zero score variance | — | MVP |
| NFR26 | Benchmark suite produces reproducible results — running benchmarks twice against the same dataset yields identical NDCG@10 scores | Reproducibility test in CI | — | MVP |
| NFR27 | Structured JSON logging with OpenTelemetry correlation IDs from DAPR trace context | Log format validation | — | Ongoing |
| NFR28 | Trace context propagates across all DAPR service invocation hops — end-to-end trace from CLI/MCP through server to backend | Distributed trace completeness test | — | Ongoing |
| NFR29 | Custom metrics exported via OpenTelemetry: ingestion throughput, search latency per axis, index size per tenant, pipeline queue depth | Aspire dashboard shows all metrics during local development | — | Ongoing |
| NFR30 | Every CLI command includes --help with at least one usage example | CLI help completeness test: parse all commands, verify example presence | — | MVP |
| NFR31 | README includes working quickstart that completes in <30 minutes on a clean machine with Docker installed | Timed walkthrough on clean environment | — | MVP |

**Total NFRs: 31**

### Additional Requirements

AR1 — Thesis gate: Hybrid retrieval must outperform every single-axis alternative on at least 80% of the 5–10 benchmark queries, scored with NDCG@10 against predeclared ground truth. Ground truth is defined by Jerome and two independent reviewers; inter-rater agreement must be at least 80%, with human review resolving automated-score disagreements.

AR2 — MVP release gate: All three hard gates must pass—three-axis validation at 80%, zero cross-tenant data leakage, and onboarding from `dotnet add package` to first search result in under 30 minutes. At least two of the following must also pass: causal-chain completeness at or above 95%, MCP end-to-end integration, and correct case scoping.

AR3 — Phase sequencing: The buildable scaffold, AppHost, ServiceDefaults, minimum build/test feedback, tenant provisioning, minimal case bootstrap, and tenant/case validation guards must exist before ingestion, indexing, search, or graph stories write data. `TenantProvisioningWorkflow` owns physically isolated tenant infrastructure creation.

AR4 — Phase commitment: EventStore integration, the MCP server, and expanded CLI capabilities are committed within four weeks of thesis validation. If that timeline cannot be met, the MCP server moves into MVP.

AR5 — Evidence Packet contract: Composite and per-axis confidence, source/origin attribution, token-budget omitted-detail handling, graceful-degradation signaling, tenant/case scope, result state, and recovery guidance form one shared cross-surface response envelope. `Contracts.V1` owns its concrete shape; CLI JSON, MCP, and future web composition must use it consistently.

AR6 — Confidence semantics: Search confidence measures relevance, not factual accuracy, completeness, or currency. This distinction must appear in API reference documentation, every CLI explain result, the compliance guide, and MCP response-schema documentation. Metadata confidence is a separate per-field concept recording human-declared or AI-inferred origin.

AR7 — Causal integrity: Causal traversal must return an ordered, timestamped sequence with typed directional edges, confidence tiers, and explicit missing-node markers. `caused_by` must remain distinct from `correlated_with`; the system never auto-promotes AI-inferred relationship confidence.

AR8 — Compliance boundary: Memories is interpretive infrastructure. It owns accurate embeddings, causal chains, calibrated confidence, and complete edge graphs, while consuming applications own decisions and legal compliance. Tenant deletion must remove that tenant's indexes, graph data, and memory units; cross-references held by other tenants remain the application's responsibility.

AR9 — Compliance documentation: Publish “Building Compliant Applications on Memories,” “Limitations of Infrastructure-Level Deletion,” and “Security Posture for Auditors” sections, including the stated legal-advice disclaimer and an accurate explanation that access telemetry is not a tamper-evident certified audit trail.

AR10 — Licensing: The project is Apache 2.0 and the README must commit not to move to a restrictive license. `LICENSE-DEPENDENCIES.md` must document the FalkorDB network boundary. The default deployment must pin FalkorDB, and the README must disclose Redis Stack SSPL constraints for managed-service offerings.

AR11 — Backend portability: Phase 2 identifies `IMemoryGraph` and `IMemoryIndex` extraction points as licensing and migration insurance. Domain logic must not tightly couple itself to Redis-specific APIs, but the project must avoid premature placeholder abstractions.

AR12 — Runtime topology: .NET Aspire AppHost orchestrates services and DAPR sidecars. External CLI, agent, and application traffic enters through infrastructure-managed ingress; internal server-to-server calls use DAPR service invocation; EventStore integration uses CloudEvents over DAPR pub/sub.

AR13 — Package inventory: `tools/release-packages.json` is authoritative for the current nine published NuGet packages. Service and orchestration projects remain non-packable as declared by the release model.

AR14 — Embedding-provider scope: Google `text-embedding-004` at 768 dimensions is the MVP runtime provider. Provider/model configuration is per tenant, while non-Google providers remain post-MVP unless explicitly advanced by a sprint change. Changing dimensions/provider requires a full tenant reindex because the Redis vector schema is fixed at index creation.

AR15 — Secret boundary: Sensitive runtime values do not use ordinary configuration fallback. Product services retrieve them through the DAPR Secrets API backed by OpenBao. Aspire secret parameters or User Secrets may only provide protected local bootstrap or one-time seeding inputs; Kubernetes Secrets are limited to documented OpenBao bootstrap or unavoidable direct pod inputs.

AR16 — Durable ingestion: One per-tenant DAPR pipeline actor owns a bounded queue, rate limiting, ordering, progress, retry, and persisted recovery. Processing stages are `queued`, `extracting`, `embedding`, `indexing`, `indexed`, and `failed`; no per-document actor fan-out is used.

AR17 — Atomic searchability: Ingestion is complete only after a unit is searchable across RediSearch, Redis Vector, and FalkorDB. Partial writes require rollback or retry to convergence, and terminal failures preserve actionable stage-specific error details.

AR18 — Interface phasing: MVP CLI scope is `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, and benchmark support. `explore`, `status`, `handlers`, `quickstart`, batch-directory ingestion, and richer diagnostics are Phase 1.5 unless explicitly advanced. MCP exposes search, ingestion, traversal, and case information, while operational tenant and diagnostic functions remain CLI concerns.

AR19 — Developer experience: Unit tests must run without Docker; integration tests require and document Docker. CI runs unit, integration, and serialization contract layers. The learning path includes numbered quickstart, EventStore-integration, and MCP-agent samples plus README, CLI help, API reference, compliance, and operator documentation.

AR20 — Graceful degradation: An unavailable memory server produces an actionable timeout/retry signal so agents do not hallucinate organizational context. Partial backend failure returns available-axis results plus an explicit degraded marker identifying excluded axes.

### PRD Completeness Assessment

The PRD is structurally strong and unusually traceable: it supplies 74 explicitly numbered FRs, 31 measurable NFRs with phases and verification methods, user journeys, scope boundaries, release gates, interface allocation, operational behavior, and detailed trust and compliance semantics. The requirements are generally testable and carry enough context for epic mapping.

Internal clarity risks to check during later alignment steps:

- The implementation matrix still names C# 13 for the .NET 10 server, while current project governance identifies C# 14; this is a version statement requiring reconciliation.
- The “absolute minimum” resource fallback permits deferring cases and tenant isolation, but the MVP strategy says both are foundational from day one and the release gate makes zero cross-tenant leakage mandatory. That fallback cannot be exercised without changing the MVP gate.
- The document says there are nine published packages plus three non-packable service/orchestration projects, while the accompanying package table and labels do not make all three non-packable projects unambiguous.
- The service-communication table describes backend access as “DAPR state / direct connection via DAPR sidecar”; direct backend connections and sidecar-mediated state access are distinct boundaries and should be stated separately.
- External CLI access exists in MVP, but unauthenticated-ingress rejection is tagged P1.5. The intended MVP external-auth posture should be made explicit.
- Several journey capabilities and interface rows are explicitly Phase 1.5 or Phase 2 despite appearing in the broader FR list. Epic coverage must preserve those phase qualifiers instead of treating every FR as an MVP obligation.

## Epic Coverage Validation

### Coverage Matrix

| FR | PRD requirement | Epic coverage | Status |
|---|---|---|---|
| FR1 | Developer can ingest content from local files into a specified case. | Epic 1 | Covered |
| FR2 | Developer can ingest content from URLs into a specified case. | Epic 6 | Covered |
| FR3 | Developer can batch-ingest content from a directory into a specified case. | Epic 6 | Covered |
| FR4 | System can extract text from ingested content (plain text, PDF, markdown). | Epic 1 | Covered |
| FR5 | System can generate embeddings for ingested content via a configurable embedding provider. | Epic 1 | Covered |
| FR6 | System ensures a memory unit is fully searchable across all axes after ingestion completes. | Epic 1; reinforced by Epic 23 | Covered |
| FR7 | Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score. | Epic 1 | Covered |
| FR8 | System manages ingestion load per tenant independently. | Epic 6 | Covered |
| FR9 | System retries failed ingestion automatically with configurable limits. | Epic 6 | Covered |
| FR10 | Developer can view ingestion status per case (queued, embedding, indexed, failed counts). | Epic 6 | Covered |
| FR11 | Developer can view failed ingestion units with error details and failure stage. | Epic 6 | Covered |
| FR12 | Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk. | Epic 6; reinforced by Epic 23 | Covered |
| FR13 | System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes). | Epic 1; reinforced by Epic 21 | Covered |
| FR14 | Developer can search memory units by syntactic matching within a tenant. | Epic 2 | Covered |
| FR15 | Developer can search memory units by semantic similarity within a tenant. | Epic 2 | Covered |
| FR16 | Developer can search memory units by graph traversal within a tenant. | Epic 2 | Covered |
| FR17 | Developer can search memory units by hybrid fusion combining all available axes. | Epic 2 | Covered |
| FR18 | Developer can control which axes are included in a search query. | Epic 2 | Covered |
| FR19 | Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode). | Epic 2 | Covered |
| FR20 | Developer can filter search results by case. | Epic 3 | Covered |
| FR21 | Developer can filter search results by metadata field values. | Epic 3 | Covered |
| FR22 | Developer can paginate search results. | Epic 2; reinforced by Epic 22 | Covered |
| FR23 | LLM Agent can constrain search response size by token budget. | Epic 10 | Covered — Phase 1.5 |
| FR24 | System returns the origin identifier (file path, URL, or event ID) and origin type for each search result. | Epic 2 | Covered |
| FR25 | Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output. | Epic 2 | Covered |
| FR26 | Developer can create a case within a tenant. | Epic 0 and Epic 3 | Covered |
| FR27 | Developer can delete a case and all its memory units. | Epic 3 | Covered |
| FR28 | Developer can add members to a case. | Epic 3 | Covered |
| FR29 | Developer can remove members from a case. | Epic 3 | Covered |
| FR30 | Developer can list cases within a tenant. | Epic 3 | Covered |
| FR31 | Developer can view case status including memory unit count, last activity timestamp, and health indicators. | Epic 3 | Covered |
| FR32 | System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion. | Epic 3 | Covered |
| FR33 | System maintains case-scoped graph edges between memory units within a case. | Epic 3 | Covered |
| FR34 | Developer can search across all cases within a tenant by keyword, returning results with case attribution. | Epic 3; reinforced by Epic 22 | Covered |
| FR35 | Developer can delete an individual memory unit from a case. | Epic 3 | Covered |
| FR36 | Developer can view recent activity within a case (ingestion events, searches, membership changes). | Epic 3 | Covered |
| FR37 | Developer can annotate or correct a memory unit, with annotations tracked as linked memory units. | Epic 3 | Covered |
| FR38 | Operator can create a tenant with physically separate indexes. | Epic 0 and Epic 5; reinforced by Epic 24 | Covered |
| FR39 | Operator can delete a tenant and all its indexes, graph data, and memory units. | Epic 5; reinforced by Epic 21 | Covered |
| FR40 | Operator can verify tenant isolation via automated checks. | Epic 5; reinforced by Epic 24 | Covered |
| FR41 | Operator can list tenants. | Epic 5 | Covered |
| FR42 | Operator can update tenant configuration after creation (rate limits, display name, settings). | Epic 5 | Covered |
| FR43 | System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment. | Epic 5 | Covered |
| FR44 | System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages. | Epic 0 and Epic 5; reinforced by Epics 20 and 24 | Covered |
| FR45 | Operator can view current configuration of a tenant (embedding provider, rate limits, index status). | Epic 5 | Covered |
| FR46 | System can index CausationId and CorrelationId from events as typed, directional graph edges. | Epic 1 | Covered |
| FR47 | Developer can traverse causal chains from a starting node with configurable depth. | Epic 4 | Covered |
| FR48 | Developer can filter graph traversal by edge type. | Epic 4 | Covered |
| FR49 | When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier. | Epic 4 | Covered |
| FR50 | System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence. | Epic 4 | Covered |
| FR51 | Developer can promote AI-inferred edge confidence when verifying a relationship. | Epic 4 | Covered |
| FR52 | System maintains chronological ordering and timestamps on causal chain nodes. | Epic 4 | Covered |
| FR53 | Developer can interact with all retrieval and ingestion capabilities via CLI. | Epic 7 | Covered |
| FR54 | Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools. | Epic 10 | Covered — Phase 1.5 |
| FR55 | CLI supports multiple output formats: human-readable (default), JSON, and table. | Epic 7 | Covered |
| FR56 | CLI provides actionable error messages with recovery suggestions for common failure modes. | Epic 7 | Covered |
| FR57 | Developer can discover available actions from any system state, including empty states and error conditions. | Epic 7 | Covered |
| FR58 | MCP tools include typed parameter schemas with descriptions for LLM agent consumption. | Epic 10 | Covered — Phase 1.5 |
| FR59 | System can auto-discover event types published to DAPR pub/sub topics. | Epic 9 | Covered — Phase 1.5 |
| FR60 | System can generate dual embeddings for events (raw payload + natural language description). | Epic 9 | Covered — Phase 1.5 |
| FR61 | System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code. | Epic 9 | Covered — Phase 1.5 |
| FR62 | Developer can list registered event handlers and detect handler registration mismatches. | Epic 9 | Covered — Phase 1.5 |
| FR63 | System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result. | Epic 2 | Covered |
| FR64 | System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit. | Epic 7 display path, with Epic 1 data capture | Covered |
| FR65 | System records `ingested_by` (user or system identity) as a mandatory field on every memory unit. | Epic 1 | Covered |
| FR66 | When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded. | Epic 5 | Covered |
| FR67 | System logs search and access events per tenant for audit purposes. | Epic 7; reinforced by Epics 20 and 27 | Covered; retention residual remains open |
| FR68 | Operator can configure embedding provider and model per tenant. | Epic 1 for MVP Google configuration; Epic 13 expands providers | Covered |
| FR69 | System enforces per-tenant rate limit ceilings for embedding API calls. | Epic 5 | Covered |
| FR70 | System tracks the embedding provider and model used for each memory unit's vectors. | Epic 5 | Covered |
| FR71 | Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. | Epic 26 operational backup/restore slice; reserved Phase 2 Story 8.3 for complete portable export | Covered — phase-qualified |
| FR72 | System exposes readiness and liveness health checks verifying all backends. | Epic 8 | Covered |
| FR73 | Operator can detect index/graph divergence via consistency check. | Epic 8 | Covered |
| FR74 | Operator can repair detected index/graph inconsistencies via consistency repair operation. | Epic 8 | Covered |

### Missing Requirements

No PRD FR is absent from the epics coverage map. The machine-checked map contains exactly one row for each FR1–FR74. No FR identifier appears in the epics document outside the PRD range.

FR71 requires scope discipline rather than gap remediation: the epics document explicitly keeps full application-facing portable export in Phase 2 while Epic 26 covers only backup/restore and disaster-recovery readiness. This is a traceable deferred implementation path, not active MVP coverage.

### Coverage Statistics

- Total PRD FRs: 74
- Unique FR coverage rows in epics: 74
- Missing PRD FRs: 0
- Duplicate FR coverage rows: 0
- Epic-only FR identifiers: 0
- Coverage percentage: 100%
- Active MVP implementation scope: not 100% of FR1–FR74, by explicit phase design

## UX Alignment Assessment

### UX Document Status

The UX design specification is present, complete, and usable as implementation guidance. It defines the shared Evidence Packet interaction grammar, personas and journeys, CLI/MCP/web phase boundaries, component behavior, responsive breakpoints, accessibility expectations, and explicit empty, weak, stale, degraded, unauthorized, compressed, and conflicting states.

Overall alignment is strong:

- The UX personas and journeys correspond directly to the PRD's developer, LLM-agent, operator, architect, and compliance use cases.
- The UX Evidence Packet maps to PRD requirements for scope, source attribution, explainability, confidence, graph context, partial-result disclosure, and recovery guidance (especially FR19, FR23-FR24, FR44, FR49, FR56-FR57, and FR63-FR66).
- Case, activity, causal-chain, tenant-health, and consistency views map to FR26-FR52 and FR72-FR74.
- CLI-first MVP, MCP/EventStore Phase 1.5, and future web composition are consistent across the UX, PRD, architecture, and epics.
- The architecture owns the shared Evidence Packet in `Contracts.V1`, including scope, result, sources, evidence, graph, state, omitted details, and recovery actions. This gives the UX a concrete cross-surface contract rather than a presentation-only concept.
- The architecture explicitly constrains the future web surface to a FrontComposer-aligned Razor component library using Microsoft Fluent UI Blazor V5 and Fluent 2 semantics, matching the UX implementation boundary.
- Architectural degraded search, graph-gap, actionable-error, tenant-isolation, and token-budget behavior supports the UX trust and recovery states.

### Alignment Issues

1. **Accessibility and responsive behavior lack PRD-level NFR identifiers.** The UX requires WCAG 2.2 AA behavior, keyboard and focus support, live-region announcements, reduced-motion and forced-color support, 44px targets, and defined responsive breakpoints. The epics trace these through UX-derived requirements and future web work, but the PRD's numbered NFR set does not make them independently traceable. Treat the UX specification as the normative source for web acceptance criteria or add corresponding PRD NFRs when the web phase becomes active.

2. **Freshness state semantics are more precise in UX than in the numbered requirements.** The UX defines a reusable fresh/stale state grammar and expected recovery behavior. The PRD discusses freshness and stale evidence narratively, while the architecture carries source freshness in the Evidence Packet, but no numbered FR/NFR defines thresholds or state-transition rules. Before implementing the web Evidence Packet, establish the authoritative threshold/configuration and contract semantics.

3. **Automatic answer and graph-context composition must remain capability- and query-sensitive.** The UX's ideal first-response flow can be read as requiring a synthesized answer and relevant graph traversal on every core search. The PRD and architecture guarantee ranked retrieval, optional graph scoring, standalone traversal, graph-scoped search, and an Evidence Packet answer or ranked-result summary. Implementations should not imply mandatory LLM synthesis or graph traversal for every MVP search unless an approved requirement promotes that behavior.

4. **Identity and permission presentation must follow the implemented phase.** The UX includes unauthorized/permission states and case-member interactions. The PRD includes case membership but does not make a full per-user authorization model part of the original MVP; the architecture records authentication and tenant-claim authorization as later operational-readiness implementation. UX acceptance criteria must distinguish tenant isolation from finer-grained user/case authorization and only expose controls backed by the active authorization model.

### Warnings

- The UX is a full-horizon specification. Command-palette actions such as Evidence Packet export must not pull FR71's complete portable case/tenant export into MVP; that capability remains phase-qualified for Phase 2, apart from Epic 26's operational backup/restore slice.
- Synthesized briefings, comparisons/diffing, and richer application UI behavior are growth or future capabilities. Preserve the UX concepts, but do not count them as MVP acceptance criteria without an approved scope change.
- The UX statement that CLI, MCP, and web are all first-class surfaces describes the product horizon, not simultaneous MVP delivery. The nearby phase table and architecture's capability-alignment rule are authoritative for delivery order.
- Architecture examples contain some historical drift (for example `.NET 10 / C# 13` in an early constraint table despite the later verified C# 14 matrix). Web implementation must follow current repository governance and the explicit FrontComposer/Fluent UI V5 boundary, not stale illustrative text.

## Epic Quality Review

### Review Scope and Structural Results

The review covered all 32 epics (Epic 0 through Epic 31), 166 registered story headings, their acceptance criteria, explicit sequencing notes, and the `story_execution_order` overrides in sprint status. Phase 2 placeholders that have activation rules but no registered story are not counted among the 166 stories.

- All 166 registered stories contain an `As a` / `I want` / `So that` statement.
- 165 of 166 stories have an explicit `Acceptance Criteria` heading.
- 165 of 166 stories use at least one Given/When/Then scenario.
- 48 stories contain only one Given/When/Then scenario. A single scenario is not inherently defective, but several of these stories combine multiple independently failing behaviors and therefore lack adequate negative or recovery coverage.
- All FRs retain epic traceability; the quality defects below concern backlog shape, sequencing, sizing, and testability rather than missing FR mappings.

### Epic-by-Epic Compliance Summary

| Epic(s) | User-value and independence assessment | Story-quality assessment | Verdict |
|---|---|---|---|
| 0 | Establishes a demonstrable developer/operator foundation and tenant/case safety before data writes. | Story 0.0 satisfies the architecture's Aspire-empty starter/scaffolding requirement; Stories 0.1-0.4 are ordered and testable. | Pass |
| 1 | Delivers the first tenant-scoped ingest/search outcome using only Epic 0. | Stories 1.2, 1.5, and 1.6 are acknowledged in the source as historical broad technical/bundled slices. | Major historical debt |
| 2-4 | Each delivers a coherent developer-visible retrieval, organization, or causal-intelligence outcome and depends only on prior capability. | Well-formed, multi-scenario BDD criteria; no forward dependency found. | Pass |
| 5 | Delivers a coherent operator tenant lifecycle. | Story 5.1 overlaps Story 0.1, but the ownership boundary explicitly makes 0.1 the minimum slice and 5.1 the canonical extension, preventing a second provisioning path. | Pass with overlap guard |
| 6-7 | Deliver resilient ingestion and an actionable CLI/quickstart outcome. | Sized and sequenced adequately; negative and recovery paths are represented. | Pass |
| 8 | Delivers operator health, repair, and telemetry confidence. | Story 8.5 explicitly bundles four independently reviewable deliverables and says it must not be reopened as one unit. Story 8.3 is correctly reserved/non-MVP rather than masquerading as active coverage. | Major historical sizing issue |
| 9-10 | Deliver Phase 1.5 EventStore and MCP consumer outcomes. | Coherent story ordering, typed criteria, and no forward dependency. | Pass |
| 11 | Enables contributor/release flow but is organized primarily as a CI/CD technical milestone. | Testable, but should be framed as an independently usable contributor/release outcome rather than pipeline construction. | Major value-framing issue |
| 12 | Delivers a proven first-release and operational path using Epic 11. | Criteria are concrete; no forward dependency. | Pass |
| 13 | Delivers operator choice, sovereignty, and vector migration. | Story 13.7 combines integration tests, Aspire fixtures, and an operator deployment guide in one review unit. | Major sizing issue |
| 14-15 | Organized around deferred-finding closure and carry-forward risk rather than a cohesive product/operator capability. | Several sweep/hardening stories aggregate unrelated changes by provenance. | Critical epic-shape violation |
| 16 | Delivers an operator-visible projection-registration mismatch check and depends on prior EventStore capability. | Despite the word `Design`, Story 16.1 explicitly designs and implements a testable cross-check. | Pass with title concern |
| 17 | Delivers a coherent future web inspection/accessibility outcome and is correctly excluded from MVP. | Strong UX-derived criteria and explicit conformance boundary. The special execution order is documented, although Story 17.1 is absent from the override list. | Pass with minor tracking concern |
| 18 | Delivers a stable downstream-consumer integration contract. | Story 18.6 is placed before dependent Story 18.5 and the override records that order. | Pass |
| 19 | A governance container for classifying deferred-register entries, not an independently consumable capability. | Stories are triage/residual sweeps organized by backlog provenance. | Critical epic-shape violation |
| 20 | Delivers authenticated, tenant-authorized, rate-limited access. | Coherent security slices with explicit negative evidence. | Pass |
| 21 | Delivers integrity and migration safety. | Story 21.2 spans multiple mutation families; several one-scenario stories carry multiple failure modes in compound `Then` clauses. | Major sizing/AC issue |
| 22 | Delivers retrieval correctness. | Story 22.7 bundles NL-axis activation, weight tuning, highlighting, and a reranker seam—four separable outcomes. | Major sizing issue |
| 23 | Delivers ingestion scalability and resilience. | Story 23.9 is deliberately placed before Story 23.1 in both the document and sprint execution order; no forward dependency remains. | Pass |
| 24 | Delivers operator observability/performance outcomes. | Story 24.5 bundles four unrelated hot-path fixes; Story 24.2 combines caching and pagination/concurrency. | Major sizing issue |
| 25 | Explicitly delivers architecture refactoring “without changing product behavior,” making it a technical milestone epic under the governing standard. | Multiple stories bundle cross-cutting refactors; 25.4 and 25.8 each combine separable boundaries. | Critical epic-shape violation |
| 26 | Delivers deployment, recovery, tests, and runbooks to operators. | User value is clear, but Stories 26.1 and 26.5 are broad multi-artifact units. | Major sizing issue |
| 27 | Has a valid operator lifecycle outcome, but cannot complete independently on the registered backlog. | Story 27.4 requires Stories 27.7-27.31 and all 25 C1 gates; only Story 27.21 is registered, leaving 24 required gates unowned. Story 27.3 is an oversized qualification packet with numbered prose rather than Given/When/Then criteria. | Critical dependency/readiness failure |
| 28 | Preserves zero-code EventStore integration, but is framed mainly as dependency/runtime adoption. | External authorization is now recorded as satisfied; Story 28.1 remains backlog and lacks an explicit AC heading despite having seven BDD scenarios. | Major value-framing; minor formatting |
| 29 | Delivers OpenBao-backed, provider-neutral secret consumption. | Story 29.2 depends only on preceding Story 29.1; the boundary is explicit. | Pass |
| 30 | Organized primarily around CI/CD ownership and pipeline mechanics. | Execution order correctly places 30.2 before 30.1 and later stories, but Stories 30.3 and 30.4 are externally activation-gated on future Hexalith.Builds capability. | Major value/readiness issue |
| 31 | Delivers a reviewable deployed secrets platform and runtime migration. | Story 31.2 depends on defined prior checkpoints in 31.1; status and activation gate are explicit. Independent countersignature work remains an external completion condition for 31.1. | Conditional pass |

### Critical Violations

1. **Epic 27 has an impossible registered dependency chain.** Story 27.4 cannot start until Stories 27.7 through 27.31 exist, are registered, are done, and all C1.1-C1.25 gates pass on one profile. The planning and sprint artifacts state that only Story 27.21/C1.15 is registered and the other 24 gates are held, unregistered, and unowned. This is a direct forward-dependency and completeness failure. The epic is correctly marked in progress, but it is not implementation-ready as specified.

2. **Epics 14, 15, 19, and 25 violate the user-value epic rule.** They are organized as deferred-work sweeps, risk/governance containers, or internal refactoring. Epic 25 is explicit that it changes no product behavior. Necessary engineering work can remain, but it should be attached to a measurable contributor, operator, security, reliability, or consumer outcome and split into independently demonstrable slices.

### Major Issues

1. **Oversized or bundled stories:** Stories 1.2, 1.5, 1.6, 8.5, 13.7, 21.2, 22.7, 24.2, 24.5, 25.2, 25.4, 25.8, 26.1, 26.5, and 27.3 contain multiple independently implementable or reviewable outcomes. Several are already acknowledged as historical anti-patterns; that acknowledgement does not make them suitable templates for new or reopened work.

2. **Thin negative-path coverage in compressed remediation stories:** 48 stories have only one Given/When/Then block. High-risk examples such as 21.2, 22.7, 24.2, 24.5, and 25.1-25.8 put multiple behavior changes into a single compound success scenario. When any such work is reopened, add separate failure, compatibility, rollback, and tenant-negative scenarios as applicable.

3. **Technical-milestone framing remains in Epics 11, 28, and 30.** These epics do protect real contributor/consumer/release outcomes, but titles and goals lead with mechanisms—CI/CD, runtime adoption, and pipeline ownership. Reframe their goals and acceptance gates around the actor-visible outcome so completion cannot be claimed merely because infrastructure exists.

4. **Known external activation blockers:** Stories 30.3 and 30.4 cannot enter implementation until owner-approved Hexalith.Builds capabilities exist. Story 31.1 also carries independent platform-review/countersignature conditions. These are honestly statused and therefore are not hidden forward dependencies, but they must remain excluded from an “implementation ready now” queue.

### Minor Concerns

- Story 28.1 has seven Given/When/Then scenarios but no `Acceptance Criteria` heading.
- Story 27.3's current criteria are numbered 6-8 rather than BDD scenarios, and AC6 is too large to be independently verified or reviewed as one criterion.
- The Epic 17 `story_execution_order` override omits Story 17.1. Clarify whether the list is partial-by-design or add the story so tools cannot interpret the override as the complete sequence.
- Numerous criteria retain point-in-time file and line anchors. The epic document's mandatory preflight correctly requires re-verification; story creation must continue to record corrected anchors rather than treating planning-time line numbers as authority.

### Confirmed Best-Practice Strengths

- The architecture's Aspire empty-starter decision is represented by the first executable scaffolding story (Story 0.0, historical alias 1.1), including solution boot, dependencies, configuration, health, and build verification.
- Indexes and state structures are introduced when tenant lifecycle or feature stories first need them; no “create every table/model up front” story was found.
- Apart from the explicit Epic 27 gap, cross-epic dependencies flow backward to already delivered capabilities. Non-numeric orders in Epics 18, 23, 29, 30, and 31 are explicitly captured in sprint status.
- Tenant-sensitive work consistently carries fail-closed scope and cross-tenant negative-evidence requirements.
- Phase 2 placeholders have activation rules and are not counted as active MVP implementation stories.

### Required Remediation Before Claiming Full Backlog Readiness

1. Register bounded, owned stories for each remaining Epic 27 C1 gate—or revise the close-out contract through an approved course correction so Story 27.4 has a finite, fully registered predecessor set.
2. Split Story 27.3's AC6-AC8 qualification work into independently executable/reviewable story slices with normal BDD criteria and explicit evidence owners.
3. Do not select externally gated Stories 30.3/30.4 or blocked Epic 31 work until their recorded activation evidence exists.
4. For any reopened oversized historical story, create vertical replacement slices rather than reusing the broad story definition.
5. Reframe future technical/governance epics around measurable actor outcomes and keep backlog-provenance sweeps in a ledger, not as product epics.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY for an unrestricted full-backlog implementation run.**

The core product specification is strong: all four canonical planning documents exist, the PRD contains 74 FRs and 31 NFRs, every FR maps exactly once into the epic coverage inventory, architecture and UX are substantially aligned, and the core/MVP story sequence has strong tenant-safety and verification discipline.

The full backlog is nevertheless not implementation-ready because active Epic 27 has a required but incomplete predecessor graph: Story 27.4 requires 25 C1 successor gates, while only Story 27.21/C1.15 is registered and the other 24 gates have no registered story or owner. Several other stories are intentionally blocked by external activation gates and therefore cannot be treated as ready-now work.

This decision does not mean all implementation must stop. A specifically selected story may proceed when its own predecessors, phase, activation gate, and evidence requirements are satisfied. It means the artifacts cannot support an autonomous “implement the backlog” instruction without first resolving the named gaps.

### Critical Issues Requiring Immediate Action

1. **Repair Epic 27's dependency graph.** Register bounded story definitions, owners, evidence producers, and sprint states for the remaining 24 C1 gates, or approve a revised close-out contract with a finite registered predecessor set. Until then, Story 27.4 and the A41 close-out remain blocked.

2. **Do not reuse technical/catch-all epics as implementation templates.** Epics 14, 15, 19, and 25 are organized around deferred-work provenance, governance, or refactoring rather than independently consumable actor outcomes. Preserve their completed history, but create new vertical stories for any reopened work.

3. **Keep externally gated backlog out of ready queues.** Stories 30.3 and 30.4 require owner-approved Hexalith.Builds capabilities. Epic 31 also retains explicit platform-review/countersignature conditions. Selection automation must fail closed until the recorded evidence exists.

### Recommended Next Steps

1. Run an approved course correction for Epic 27 that either registers one bounded story per remaining C1 gate or changes the successor model without weakening the fail-closed evidence contract.
2. Split Story 27.3's qualification work into independently executable BDD slices; give each slice one outcome, accountable owner, exact evidence producer, failure behavior, and review gate.
3. Reconcile the six PRD clarity risks: C# 13 versus current C# 14, the forbidden “absolute minimum” tenant/case deferral, package-count wording, direct-backend versus DAPR-state access, MVP external-auth posture, and phase qualifiers in the broad FR list.
4. Promote the UX-only accessibility, responsiveness, and freshness semantics to explicit phase-scoped requirements before future web implementation; keep export, synthesis, diffing, and mandatory graph composition out of MVP unless scope is formally changed.
5. Correct Story 28.1's missing acceptance-criteria heading and clarify whether Epic 17's partial execution-order list intentionally excludes Story 17.1.
6. When reopening any broad historical story, replace it with vertical slices and add separate compatibility, rollback, error, and cross-tenant negative scenarios where relevant.

### Final Note

This assessment identified **24 findings across three categories**: 6 PRD clarity risks, 8 UX alignment/scope cautions, and 10 epic-quality/readiness findings. The decisive blocker is narrow and explicit—Epic 27's unregistered predecessor set—while the remainder are scope, traceability, sizing, or future-selection controls.

**Assessment date:** 2026-08-03  
**Assessor:** Codex, using the BMad Implementation Readiness workflow
