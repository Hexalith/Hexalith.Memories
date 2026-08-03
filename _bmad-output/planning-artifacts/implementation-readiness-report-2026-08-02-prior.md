---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
status: complete
overallReadiness: NOT_READY
documentsIncluded:
  prd:
    - prd.md
  architecture:
    - architecture.md
  epics:
    - epics.md
  ux:
    - ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-02
**Project:** memories

## Document Discovery

### Documents Included

| Document Type | File | Size (bytes) | Modified |
| --- | --- | ---: | --- |
| PRD | `prd.md` | 88,353 | 2026-07-19 16:20:02 +0200 |
| Architecture | `architecture.md` | 121,376 | 2026-08-02 11:06:53 +0200 |
| Epics and Stories | `epics.md` | 382,434 | 2026-08-02 11:06:53 +0200 |
| UX Design | `ux-design-specification.md` | 99,240 | 2026-06-27 08:02:38 +0200 |

No sharded variants or missing required documents were found.

### Supplemental Filename Matches Excluded from the Primary Set

- Architecture pattern: 2 sprint-change proposals
- Epic pattern: 11 sprint-change proposals
- UX pattern: 1 sprint-change proposal

These supplemental artifacts were excluded from the primary assessment set by user confirmation on 2026-08-02.

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

FR71: Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. **Phase:** Phase 2 unless a later sprint change explicitly pulls export into MVP.

FR72: System exposes readiness and liveness health checks verifying all backends

FR73: Operator can detect index/graph divergence via consistency check

FR74: Operator can repair detected index/graph inconsistencies via consistency repair operation

**Total FRs: 74**

### Non-Functional Requirements

NFR1: Syntactic search latency (p95) must be <200ms with 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR2: Semantic search latency (p95) must be <500ms with 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR3: Hybrid search latency (p95) must be <1s with 10 concurrent queries per tenant and 10K memory units per tenant. **Phase:** MVP.

NFR4: Graph traversal latency (p95) must be <2s with 10 concurrent queries per tenant, 10K memory units per tenant, and depth ≤5. **Phase:** MVP.

NFR5: Ingestion throughput must exceed 100 memory units/minute for payloads ≤10KB and 10 memory units/minute for payloads ≤1MB, per tenant, using single-document embedding calls rather than batching. **Phase:** Ongoing.

NFR6: Event indexing freshness must be <5s from DAPR pub/sub publication to searchable under normal conditions; degradation must be documented when the embedding provider is rate-limited. **Phase:** P1.5.

NFR7: The service must be fully operational within 60s from containers running to accepting queries, excluding image pull time. **Phase:** Ongoing.

NFR8: Zero cross-tenant data leakage — no search, ingestion, or graph traversal may return data from another tenant. Verification requires an automated suite covering search, ingest, and graph across all axes with malformed, empty, and swapped tenant IDs, plus identical graph structures in tenants A and B with colliding edge IDs to prove traversal from A returns no nodes from B. **Phase:** MVP.

NFR9: Product services must retrieve embedding-provider and other application runtime secrets exclusively through the DAPR Secrets API, backed by OpenBao in Aspire and deployed environments. Secret values must never be stored in application configuration or ordinary environment variables. Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or direct pod inputs outside the DAPR secret-store boundary. Verification requires structural dependency tests, secret scanning, AppHost topology tests, and integration tests proving DAPR reads from OpenBao without secret disclosure. **Phase:** Ongoing.

NFR10: All inter-service communication must be authenticated via DAPR API tokens, verified through DAPR configuration validation. **Phase:** Ongoing.

NFR11: External access must be authenticated at the ingress layer, with no unauthenticated access to REST API endpoints, verified by integration tests with unauthenticated requests. **Phase:** P1.5.

NFR12: The system must support linear tenant scaling: adding a tenant must not degrade existing tenant performance by more than 5%. Validate with 10 tenants at 100K memory units each by benchmarking tenant 1 alone, adding nine loaded tenants, re-benchmarking tenant 1, and measuring the delta. **Phase:** Ongoing.

NFR13: Per-tenant ingestion pipelines must scale independently so one tenant's batch ingestion does not block another tenant's real-time ingestion, verified through concurrent ingestion across three tenants. **Phase:** Ongoing.

NFR14: Redis memory footprint per memory unit must be predictable and documented so operators can estimate infrastructure costs before tenant provisioning; publish a sizing guide by vector dimension and metadata size. **Phase:** Ongoing.

NFR15: Architecture must not preclude backend migration from Redis to Qdrant; use a concrete implementation with clearly documented extraction points and no premature interfaces, and verify there is no Redis-specific coupling in domain logic. **Phase:** Ongoing.

NFR16: There must be zero memory-unit loss during Redis restart, with AOF persistence enabled and verified. **Phase:** MVP.

NFR17: Ingestion pipeline state must survive process restarts so queued and in-progress units resume without data loss, verified through DAPR actor state persistence. **Phase:** MVP.

NFR18: Partial backend failure, with one of three backends down, must produce degraded service rather than total failure; available axes must continue serving results. Verify by killing each backend individually and confirming partial results. **Phase:** Ongoing.

NFR19: Failed ingestion units must never be silently dropped; all failures must be visible via CLI status with error details and the failure stage. Verify end-to-end with intentional failures at every pipeline stage. **Phase:** Ongoing.

NFR20: MCP tool responses must conform to the MCP protocol specification, including valid tool schemas, typed parameters, and structured error responses, verified by an MCP protocol conformance suite. **Phase:** P1.5.

NFR21: DAPR pub/sub integration must handle the CloudEvents envelope format so events from any DAPR-compatible publisher are processable, verified with standard CloudEvents payloads. **Phase:** P1.5.

NFR22: Embedding-provider integration must handle rate limiting gracefully: HTTP 429 responses trigger backoff without pipeline crash or data loss, verified per provider through rate-limit simulation. **Phase:** Ongoing.

NFR23: The CLI must connect through a configurable endpoint supporting local development (`localhost`), container environments (Docker service name), and remote environments (ingress URL), verified through configuration-layering tests across all three. **Phase:** Ongoing.

NFR24: Hybrid fusion must use deterministic weighted reciprocal-rank fusion with per-axis rank contributions in the 0.0–1.0 range; single-axis explain must still document axis-specific score semantics. Verify through fusion and explain unit tests with known rankings and weights. **Phase:** MVP.

NFR25: Fusion must produce deterministic scores: the same query against the same data produces identical composite scores, though ordering within the same score tier may vary. Verify with 100 repeated queries and zero score variance. **Phase:** MVP.

NFR26: The benchmark suite must produce reproducible results: running twice against the same dataset yields identical NDCG@10 scores, verified in CI. **Phase:** MVP.

NFR27: Logging must be structured JSON with OpenTelemetry correlation IDs from DAPR trace context, verified through log-format validation. **Phase:** Ongoing.

NFR28: Trace context must propagate across all DAPR service invocation hops, producing an end-to-end trace from CLI/MCP through the server to the backend, verified by distributed trace completeness testing. **Phase:** Ongoing.

NFR29: Custom metrics must be exported through OpenTelemetry for ingestion throughput, search latency per axis, index size per tenant, and pipeline queue depth; the Aspire dashboard must display all metrics during local development. **Phase:** Ongoing.

NFR30: Every CLI command must include `--help` with at least one usage example, verified by parsing all commands and checking for an example. **Phase:** MVP.

NFR31: The README must include a working quickstart that completes in <30 minutes on a clean machine with Docker installed, verified through a timed walkthrough. **Phase:** MVP.

**Total NFRs: 31**

### Additional Requirements

- The product thesis is gated on three-axis hybrid retrieval outperforming every single axis on at least 80% of 5–10 benchmark queries, scored with NDCG@10 against ground truth defined by Jerome and two independent reviewers; benchmark validity requires at least 80% inter-rater agreement.
- MVP release requires all three hard gates: the three-axis threshold, zero cross-tenant leakage, and onboarding in under 30 minutes. At least two of three soft gates must also pass: ≥95% causal-chain completeness, MCP end-to-end integration, and correct case scoping.
- Implementation sequencing requires a buildable scaffold/AppHost/ServiceDefaults and minimum build/test feedback before tenant provisioning, minimal active-tenant case bootstrap, and tenant/case validation guards. No ingestion, indexing, search, or graph path may write backend data before those guards exist.
- Search axes must be implemented independently before the fusion spike; BM25, cosine, and graph-proximity normalization must be solved and documented before weighting begins.
- Phase 1.5 is committed within four weeks after thesis validation. If it slips, MCP moves into MVP. EventStore integration must use DAPR pub/sub through the Memories Server sidecar and `/events/ingest`; modules must not push domain event streams directly through REST.
- The Evidence Packet is a shared `Contracts.V1` response envelope across CLI JSON, MCP, and future web UI. It combines confidence/per-axis breakdown, source attribution, token-budget omission details, degraded-axis signaling, tenant/case scope, result state, and recovery guidance.
- Confidence scores express result relevance, not factual accuracy or data completeness. Single-axis and hybrid per-axis scores have distinct semantics, and the caveat must appear in API reference, CLI explain output, compliance documentation, and MCP schema documentation.
- Causal results must be ordered, timestamped, typed, directional, and gap-aware. Missing intermediate nodes must be explicit; `caused_by` and `correlated_with` must never be collapsed. AI-inferred edge confidence may only be promoted by a user.
- Memories is interpretive infrastructure: it owns accurate embeddings, causal structure, calibrated confidence, and complete edge graphs; consuming applications own decisions, legal compliance, and user-facing representations.
- Tenant deletion must remove the tenant's indexes, graph data, and memory units, while cross-references held by other tenants remain the consuming application's responsibility. Access telemetry is not a tamper-evident certified audit trail.
- Compliance documentation must cover compliant-application patterns, infrastructure-level deletion limits, auditor-facing security posture, and an explicit legal-advice disclaimer.
- The project is committed to Apache 2.0. README deployment guidance must disclose the Redis Stack SSPL managed-service constraint. FalkorDB must be version-pinned, dependency licensing documented, and `IMemoryGraph`/`IMemoryIndex` extraction points identified as licensing insurance.
- Runtime secrets must be obtained exclusively through the DAPR Secrets API backed by OpenBao. Aspire parameters or .NET User Secrets may bootstrap local secrets but must not become an alternative product runtime provider.
- Google `text-embedding-004` is the MVP runtime embedding provider. Switching provider or vector dimensions requires a full tenant reindex. Shared provider keys share the upstream quota even though per-tenant pipeline actors enforce local ceilings.
- Ingestion uses a durable per-tenant DAPR pipeline actor with a bounded queue, throttling, ordering, progress tracking, exponential retry, dead-letter visibility, and restart persistence. A unit is `indexed` only after it is searchable across every required backend.
- External consumers use infrastructure-managed ingress; internal services use DAPR service invocation/pub-sub. JSON is the only serialization format. Internal DAPR calls use API tokens; external calls use ingress authentication; tenant context is explicit and server-validated.
- Internal errors must preserve component, error code, details, and recovery guidance across Server → DAPR → MCP/ingress → CLI; generic 502 collapsing is forbidden.
- CLI is the operational superset, with human, JSON, and table output. Configuration precedence is flags, environment variables, config file, DAPR Secrets/OpenBao, then DAPR non-secret configuration; sensitive values do not participate in fallback resolution.
- Documentation requires numbered quickstart/EventStore/MCP samples, a 30-second README demo, complete CLI help, generated API reference, compliance guide, and operator guide. Unit tests run without Docker; integration tests require Docker; CI runs unit, integration, and contract layers.
- The current package inventory is stated as nine published packages plus three non-packable service/orchestration projects, with `tools/release-packages.json` authoritative.

### PRD Completeness Assessment

The PRD is unusually detailed and largely implementation-ready at the requirements level: all 74 FRs and 31 NFRs are numbered, grouped, measurable where appropriate, and connected to journeys, phase intent, verification methods, and hard go/no-go gates. Tenant isolation, deterministic fusion, resilience, trust semantics, secrets, observability, and documentation have explicit success criteria.

The following internal ambiguities require traceability attention during epic validation:

1. The MVP philosophy says cases and multi-tenancy are required from day one, and zero cross-tenant leakage is a hard gate, but the resource-risk fallback allows cases and tenant isolation to be deferred to fast-follow.
2. Journey 2 says handler listing and event replay must be included in “MVP Feature #3 (EventStore Integration),” while the MVP feature table defines Feature #3 as Three-Axis Search and places EventStore integration in Phase 1.5.
3. The executive summary says every feature is accessible through both MCP and CLI, while the interface parity matrix intentionally limits MCP and reserves tenant management, diagnostics, and several operations for CLI only.
4. FR53 and FR54 are unphased, although the CLI and MCP capability matrices split their constituent behavior across MVP and Phase 1.5. Except for FR71, FRs generally lack explicit phase tags, leaving scope interpretation dependent on surrounding narrative.
5. The topology alternates between DAPR-mediated backend access and direct Redis/FalkorDB clients registered at the composition root. The intended infrastructure boundary needs confirmation against the architecture.
6. The stated “nine published + three non-packable” inventory is not fully reconciled by the package table, which explicitly identifies only Server and AppHost as non-packable.

These issues do not prevent requirements extraction, but they can create conflicting epic coverage and acceptance criteria if not resolved by the authoritative architecture and epic documents.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The epic document contains an explicit `FR Coverage Map` for FR1–FR74 and detailed story acceptance criteria supporting those mappings. No FR identifier beyond the PRD range appears as an additional functional requirement.

### Coverage Matrix

| FR | PRD Requirement | Epic and Principal Story Coverage | Status |
| --- | --- | --- | --- |
| FR1 | Ingest local files into a case | Epic 1 — Story 1.6 | ✓ Covered |
| FR2 | Ingest URLs into a case | Epic 6 — Story 6.1 | ✓ Covered |
| FR3 | Batch-ingest a directory into a case | Epic 6 — Story 6.1 | ✓ Covered |
| FR4 | Extract plain text, PDF, and markdown | Epic 1 — Story 1.3 | ✓ Covered |
| FR5 | Generate embeddings through a configurable provider | Epic 1 — Story 1.4 | ✓ Covered |
| FR6 | Make a completed unit searchable across all axes | Epic 1 — Stories 1.5/1.6; reinforced by Epic 23 | ✓ Covered |
| FR7 | Attach metadata with origin and confidence | Epic 1 — Stories 1.2/1.6 | ✓ Covered |
| FR8 | Manage ingestion load independently per tenant | Epic 6 — Story 6.2 | ✓ Covered |
| FR9 | Retry failed ingestion with configurable limits | Epic 6 — Story 6.3 | ✓ Covered |
| FR10 | View ingestion status per case | Epic 6 — Story 6.3 | ✓ Covered |
| FR11 | View failed units with error and stage | Epic 6 — Story 6.3 | ✓ Covered |
| FR12 | Re-ingest failed or prior content singly or in bulk | Epic 6 — Story 6.3; reinforced by Epic 23 — Story 23.4 | ✓ Covered |
| FR13 | Recover from partial multi-backend writes | Epic 1 — Story 1.6; reinforced by Epic 21 — Story 21.2 | ✓ Covered |
| FR14 | Syntactic search within a tenant | Epic 2 — Story 2.1 | ✓ Covered |
| FR15 | Semantic search within a tenant | Epic 2 — Story 2.2 | ✓ Covered |
| FR16 | Graph search within a tenant | Epic 2 — Story 2.3 | ✓ Covered |
| FR17 | Hybrid search across available axes | Epic 2 — Story 2.5 | ✓ Covered |
| FR18 | Select search axes | Epic 2 — Story 2.5 | ✓ Covered |
| FR19 | Explain per-axis scores and normalization | Epic 2 — Story 2.6 | ✓ Covered |
| FR20 | Filter search by case | Epic 3 — Story 3.4 | ✓ Covered |
| FR21 | Filter search by metadata | Epic 3 — Story 3.4 | ✓ Covered |
| FR22 | Paginate search results | Epic 2 — Story 2.6; reinforced by Epic 22 — Stories 22.1/22.3 | ✓ Covered |
| FR23 | Constrain agent responses by token budget | Epic 10 — Story 10.2 | ✓ Covered (P1.5) |
| FR24 | Return origin identifier and type | Epic 2 — Stories 2.1/2.2/2.6 | ✓ Covered |
| FR25 | Benchmark hybrid against single axes | Epic 2 — Story 2.8 | ✓ Covered |
| FR26 | Create a case within a tenant | Epic 0 — Story 0.2; Epic 3 — Story 3.1 | ✓ Covered |
| FR27 | Delete a case and its units | Epic 3 — Story 3.5 | ✓ Covered |
| FR28 | Add case members | Epic 3 — Story 3.3 | ✓ Covered |
| FR29 | Remove case members | Epic 3 — Story 3.3 | ✓ Covered |
| FR30 | List tenant cases | Epic 3 — Story 3.1 | ✓ Covered |
| FR31 | View case status | Epic 3 — Story 3.2 | ✓ Covered |
| FR32 | Enforce single-case ownership | Epic 0 — Story 0.2; Epic 3 — Story 3.1 | ✓ Covered |
| FR33 | Maintain case-scoped graph edges | Epic 3 — Story 3.1 | ✓ Covered |
| FR34 | Search across cases with attribution | Epic 3 — Story 3.4; reinforced by Epic 22 — Story 22.4 | ✓ Covered |
| FR35 | Delete an individual memory unit | Epic 3 — Story 3.5 | ✓ Covered |
| FR36 | View recent case activity | Epic 3 — Story 3.2 | ✓ Covered |
| FR37 | Add linked annotations/corrections | Epic 3 — Story 3.6 | ✓ Covered |
| FR38 | Create a physically isolated tenant | Epic 0 — Story 0.1; Epic 5 — Story 5.1; reinforced by Epic 24 — Story 24.3 | ✓ Covered |
| FR39 | Delete a tenant and all data | Epic 5 — Story 5.2; reinforced by Epic 21 — Story 21.5 | ✓ Covered |
| FR40 | Verify tenant isolation | Epic 5 — Story 5.3; reinforced by Epic 24 — Story 24.3 | ✓ Covered |
| FR41 | List tenants | Epic 5 — Story 5.5 | ✓ Covered |
| FR42 | Update tenant configuration | Epic 5 — Story 5.5 | ✓ Covered |
| FR43 | Guard inconsistent configuration changes | Epic 5 — Story 5.5 | ✓ Covered |
| FR44 | Enforce tenant context at every layer | Epic 0 — Story 0.3; Epic 5 — Story 5.4; reinforced by Epics 20/24 | ✓ Covered |
| FR45 | View tenant configuration | Epic 5 — Story 5.5 | ✓ Covered |
| FR46 | Index causation/correlation as typed edges | Epic 1 — Story 1.5 | ✓ Covered |
| FR47 | Traverse causal chains with depth | Epic 4 — Story 4.1 | ✓ Covered |
| FR48 | Filter traversal by edge type | Epic 4 — Story 4.2 | ✓ Covered |
| FR49 | Emit explicit graph gap markers | Epic 4 — Story 4.3 | ✓ Covered |
| FR50 | Support the required edge taxonomy | Epic 4 — Story 4.2 | ✓ Covered |
| FR51 | Promote inferred edge confidence | Epic 4 — Story 4.3 | ✓ Covered |
| FR52 | Preserve chronological causal ordering | Epic 4 — Story 4.1 | ✓ Covered |
| FR53 | Expose retrieval and ingestion through CLI | Epic 7 — Story 7.1 | ✓ Covered; phase split noted |
| FR54 | Expose search, ingestion, traversal, and case info via MCP | Epic 10 — Story 10.1 | ✓ Covered (P1.5) |
| FR55 | Support human, JSON, and table CLI output | Epic 7 — Story 7.2 | ✓ Covered |
| FR56 | Provide actionable CLI errors | Epic 7 — Story 7.3 | ✓ Covered |
| FR57 | Provide discoverable actions in every state | Epic 7 — Stories 7.3/7.4 | ✓ Covered |
| FR58 | Provide typed MCP schemas | Epic 10 — Story 10.1 | ✓ Covered (P1.5) |
| FR59 | Auto-discover DAPR-published event types | Epic 9 — Story 9.1 | ✓ Covered (P1.5) |
| FR60 | Generate dual event embeddings | Epic 9 — Story 9.2 | ✓ Covered (P1.5) |
| FR61 | Auto-index causation/correlation without mapping code | Epic 9 — Story 9.2 | ✓ Covered (P1.5) |
| FR62 | List handlers and detect mismatches | Epic 9 — Story 9.3; reinforced by Epic 16 — Story 16.1 | ✓ Covered (P1.5) |
| FR63 | Return composite confidence and axis breakdown | Epic 2 — Stories 2.6/2.7 | ✓ Covered |
| FR64 | Track metadata origin and confidence per field | Epic 1 — Stories 1.2/1.6; Epic 7 — Story 7.2 display | ✓ Covered |
| FR65 | Record mandatory `ingested_by` | Epic 1 — Stories 1.2/1.6 | ✓ Covered |
| FR66 | Return partial results and excluded axes | Epic 5 — Story 5.6 | ✓ Covered |
| FR67 | Log search/access events per tenant | Epic 7 — Story 7.5; reinforced by Epics 20 and 27 | ✓ Covered; retention residual open |
| FR68 | Configure provider and model per tenant | Epic 1 — Story 1.7 | ✓ Covered |
| FR69 | Enforce per-tenant embedding rate ceilings | Epic 5 — Story 5.5 | ✓ Covered |
| FR70 | Track embedding provider/model per unit | Epic 5 — Story 5.5 | ✓ Covered |
| FR71 | Export tenant/case data portably | Epic 26 — Story 26.2 backup/restore; reserved Story 8.3 for full export | ⚠ Traceable but full feature deferred/unregistered |
| FR72 | Expose backend-aware readiness/liveness | Epic 8 — Story 8.1 | ✓ Covered |
| FR73 | Detect index/graph divergence | Epic 8 — Story 8.2 | ✓ Covered |
| FR74 | Repair index/graph divergence | Epic 8 — Story 8.2 | ✓ Covered |

### Missing Requirements

No PRD FR is absent from the epic coverage map.

FR71 is not missing from planning, but its broader application-facing export feature is explicitly deferred to Phase 2 and has no registered implementation story. Epic 26 covers backup/restore and disaster-recovery readiness only. This distinction must remain visible in implementation-readiness accounting.

### Extra Epic FR Identifiers

None. The epics add architecture requirements, UX-DR1–UX-DR40, operational-remediation stories, and NFR reinforcement, but no functional requirement identifier outside FR1–FR74.

### Coverage Statistics

- Total PRD FRs: 74
- FRs mapped in epics: 74
- Missing FR mappings: 0
- Traceability coverage: 100%
- Fully active/current implementation scope: not equivalent to 100%; FR23, FR54, FR58–FR62 are Phase 1.5, and full FR71 export is Phase 2 with an unregistered placeholder

## UX Alignment Assessment

### UX Document Status

The selected master UX specification is present and complete. It explicitly describes itself as full-horizon guidance rather than an MVP scope declaration: CLI-visible and contract-visible Evidence Packet semantics bind the MVP; MCP/EventStore follow in Phase 1.5; FrontComposer/Fluent UI browser composition remains future work unless approved through a later scope change.

The UX, PRD, and architecture are strongly aligned on the central experience:

- The same four consumer perspectives recur across the artifacts: developer, LLM agent, operator, and case/team owner.
- The shared trust loop is consistently built around visible tenant/case scope, source attribution, confidence, retrieval-axis explanation, graph context, degraded behavior, omitted details, and recovery actions.
- Architecture assigns this grammar to the versioned `Contracts.V1` Evidence Packet and explicitly makes it the common envelope for CLI JSON, MCP responses, and future web composition.
- Architecture supports the future browser design with `Hexalith.Memories.Web` as a FrontComposer-aligned Razor component library using Microsoft Fluent UI Blazor V5 rather than a separate design system.
- UX accessibility expectations are specific and implementation-usable: WCAG 2.2 AA, full keyboard operation, text as well as color for state, visible focus, screen-reader semantics, reduced-motion and forced-colors support, responsive layouts, and 44-pixel touch targets.

### Alignment Issues

1. **Answer-generation ownership is not consistently bounded.** The PRD positions Memories as a structured retrieval/evidence system and assigns final narrative quality to the consuming LLM. The UX repeatedly describes Memories as automatically producing a synthesized answer, narrative, or briefing, while the architecture's Evidence Packet includes an `answer summary or ranked result summary` without specifying who produces the answer, whether it is extractive or generative, or which phase owns it. This affects contract fields, acceptance criteria, security review, latency, provenance, and degraded-mode behavior. Before implementing answer-bearing packets, the documents need one explicit responsibility rule: Memories either returns ranked evidence only, returns a deterministic/extractive summary, accepts a caller-provided answer, or invokes a phase-scoped AI service under defined provenance and failure semantics.

2. **Interface parity language conflicts with the selected architecture.** The PRD executive language and UX statement that CLI, MCP, and web are all first-class surfaces can be read as requiring the same capabilities everywhere. Architecture Decision D7 deliberately chooses capability alignment rather than feature parity, keeping operational and administrative actions CLI-only while MCP exposes the agent task subset and web remains future scope. The detailed UX mostly supports semantic consistency rather than literal command parity, but the higher-level wording should be revised so stories cannot interpret it as a requirement to expose every feature on every surface.

3. **Default search-scope behavior needs a single normative rule.** UX makes the core search experience scope-first and says a query without tenant and case clarity is not trustworthy. The PRD makes tenant mandatory but describes case as an optional filter and separately permits attributed cross-case search. Architecture supports tenant-wide relevance plus optional case affinity. These can coexist, but the artifacts do not yet state a single default for omitted case scope or the confirmation/authorization behavior for deliberate broadening. Define whether the system defaults to a selected case, explicitly labeled tenant-wide scope, or rejects ambiguity, then carry that rule into CLI, MCP, contract, and future web acceptance criteria.

4. **Freshness is a mandatory UX state without a complete computation contract.** UX requires freshness assessment in the first response and defines visible current/aging/stale/unknown states. The data model provides `LastUpdated`, and the PRD journeys mention stale evidence, but the numbered requirements and architecture do not define threshold ownership, tenant configuration, clock semantics, or how mixed-age sources roll up to packet freshness. A shared freshness policy is needed before different surfaces implement incompatible state labels.

5. **Interactive performance is qualitative at the UX layer.** The PRD supplies backend search latency targets, while UX says compact evidence should return quickly and deeper graph/source diagnostics should expand progressively. No end-to-end response budget is defined for assembling the first trustworthy Evidence Packet, including source lookup, explanation, freshness, graph summary, authorization, and degradation metadata. Add a measurable first-packet latency target and progressive-expansion/loading behavior before browser work becomes active.

### Warnings

- The UX component roadmap uses labels such as `Phase 1`, `Phase 2`, and `Phase 3` for component sequencing. Those labels are easy to confuse with the product's MVP, Phase 1.5, and later release phases even though the UX document says browser work is future scope. Rename them to component waves or explicitly map them to approved product phases.
- UX recovery actions include `export packet`, while full portable export under FR71 remains deferred and has no registered implementation story. The action must remain phase-gated or be limited to exporting the current Evidence Packet rather than tenant/case data.
- UX asks the system to automatically perform relevant graph traversal and source lookup for the first response. Architecture permits the graph axis to be optional/disabled and defines stricter latency targets for ordinary search than graph traversal. Acceptance criteria must preserve honest partial/degraded packets rather than making every query wait for every axis.
- Future web implementation is not an MVP readiness blocker because the UX document explicitly phase-gates it. When that work is approved, conformance tests must enforce the specified FrontComposer/Fluent UI Blazor V5 boundary, Fluent 2 token use, accessibility states, and justified exceptions for custom markup/CSS.

## Epic Quality Review

### Review Scope and Positive Findings

The master epic artifact defines 32 epics and 165 registered story specifications. All 74 PRD functional requirements remain traceable. The foundational product sequence (Epics 0–10) is generally organized around developer, operator, and agent outcomes, and almost every story uses a role/goal/benefit statement plus testable Given/When/Then criteria.

The greenfield safeguards are present:

- Story 0.0 is the first executable scaffold/single-command-boot story and implements the architecture's Aspire-empty/incremental-project intent.
- Story 0.4 places a minimum build and Docker-free test gate before Epic 1 data-writing work.
- Tenant indexes/databases and case structures are created when tenant/case capability is first needed, not as a speculative all-entities-up-front data-model story.
- Epic 0 makes tenant and case prerequisites executable before ingestion and search, avoiding the former forward dependency on later tenant-management work.

The following defects remain under strict create-epics-and-stories standards.

### 🔴 Critical Violations

1. **Epic 27 has no registered path to completion.** Story 27.4 requires Stories 27.5 and 27.6 to exist, be registered, and be `done`, but the same master document says both stories were withdrawn and that all 25 C1 gates currently have no story owner. This is an explicit dependency on absent future work. Epic 27 and Story 27.4 are therefore not implementation-ready. Remediation: author producer-complete successor story files, split the 25 gates into independently owned/verifiable slices, approve and register them, then place them before Story 27.4 in the canonical execution order.

2. **Story 27.3 is not independently completable against its own acceptance criteria.** AC1–AC5 are retained in the story while repeatedly stating that Story 27.3 cannot discharge them. Its reopened C0 is also blocked on five predecessor gaps (`DW 27.3-CR42` through `CR46`) assigned to the Story 27.2 lifecycle-checkpoint owner even though Story 27.2 is recorded `done` and no new registered remediation story owns those gaps. A story cannot reach a meaningful done state when part of its acceptance contract is explicitly out of authority. Remediation: move held C1 definitions out of Story 27.3's acceptance criteria, register an owned predecessor for the five C0 gaps, and leave Story 27.3 with only C0 and independently executable C2/C3/C4 criteria.

3. **Canonical story numbering is not a safe execution order.** The separate sprint-status override correctly records actual prerequisites, but the master epic sequence still contains forward-number dependencies: Stories 17.2–17.5 require Story 17.6; Story 18.5 requires Story 18.6; Story 23.1 requires Story 23.9; and Epic 30 requires Story 30.2 to execute before Story 30.1. This violates the rule that later-numbered stories must not be prerequisites for earlier-numbered stories and makes tools/readers dependent on a second ordering source. Completed historical keys may remain aliases, but active/backlog work should be reordered or renumbered so the primary story sequence is executable without an override.

### 🟠 Major Issues

1. **Several epics are remediation buckets rather than cohesive user-value increments.** Epics 14, 15, and 19 group unrelated deferred-work/governance sweeps, and Epic 25 explicitly groups architecture refactoring and code-health work with no product behavior change. Maintainer value is real, but these are technical/process milestones under the strict epic standard. Replace future catch-all successors with outcome-focused epics such as release integrity, embedding security, migration safety, or consumer contract reliability, each independently demonstrable.

2. **Oversized story shapes are common.** The document itself acknowledges Stories 1.2, 1.5, and 1.6 as historical broad/bundled slices that must not be reused as templates. In addition, 21 stories contain more than five separate Given blocks and therefore require a split or a per-gate checkpoint/evidence table under the artifact's own checkpoint-heavy-story rule: 1.5, 1.6, 5.5, 7.1, 8.5, 10.1, 10.2, 13.1, 13.2, 13.4, 14.1, 14.3, 14.4, 15.6, 16.1, 17.3, 17.5, 17.6, 17.7, 18.8, and 28.1. Story 28.1 is a current backlog example with seven independent Given/When/Then blocks and no implementation story file, despite the guard applying at registration even to backlog work. Before any such unfinished/reopened story is selected, split independent outcomes or attach the required producer-complete checkpoint table.

3. **Story 22.1 permits an outcome that does not satisfy FR22.** Its acceptance criterion allows semantic pagination either to fetch/skip correctly or to reject non-zero offsets. FR22 requires pagination, so rejection cannot close the story. Remove the rejection alternative and require working, bounded pagination.

4. **Story 22.4 retains an obsolete fusion choice.** It permits either RRF or per-axis min-max fusion, while the architecture and later calibration pin deterministic weighted RRF (`k=10` and governed weights). Reconcile the historical story text or add a dated supersession note directly to Story 22.4 so the current master does not present min-max fusion as an acceptable implementation.

5. **Release acceptance criteria contain stale and currently invalid contracts.** Story 12.1 still requires seven packages and a `chore(release)` commit; the current release inventory is nine packages and repository commit policy forbids the `chore` type. Story 11.2 describes a tag-push release trigger, whereas the current architecture selects guarded operator dispatch from the exact green `main` source. Preserve history in retrospectives, but mark these criteria superseded in the master epic artifact so they cannot guide new work.

6. **Story 27.3 is an epic-sized, non-BDD specification.** It is the only story with numbered acceptance criteria and no Given/When/Then blocks; AC6 alone combines render/apply, context refusal, runtime component discovery/substitution, evidence production/validation/upload, health behavior, and disclosure of a known fidelity gap. Even after removing non-owned AC1–AC5, the remaining adapter, manifest, unit-contract, and deployment-lane outcomes should be split into independently reviewable stories or checkpoints with a single owner and producer per gate.

### 🟡 Minor Concerns

- Story 28.1 has seven Given/When/Then blocks but omits the `Acceptance Criteria` heading. Story 17.6 also has one focused-validation block with Given/Then but no When. Normalize both before execution/reopening.
- Historical aliases, reserved gaps, and nonnumeric execution order (missing Story 1.1, reserved Story 8.3, 18.6 before 18.5, and 23.9 before 23.1) are documented but add avoidable tooling and onboarding complexity.
- Story 31.1 is described as `review` inside `epics.md` while sprint status records `in-progress`. Keep lifecycle status authoritative in one location or synchronize the duplicate text.
- Numerous criteria cite mutable source line numbers and historical counts. The artifact's mandatory re-verification rule mitigates this, but active story files must record corrected anchors before implementation.

### Epic Quality Recommendation

Do not treat Epic 27 as ready for continued implementation or close-out until its missing owned successor path is registered. Other active work may proceed only within its independent scope and established predecessor gates. Before selecting backlog Epics 28 or 30, resolve Story 28.1's checkpoint-heavy registration defect and normalize Epic 30's canonical order so Story 30.2 precedes Story 30.1.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The core product requirements are well developed: all four required master artifacts exist, all 74 FRs and 31 NFRs were extracted, FR-to-epic traceability is 100%, and the architecture provides strong implementation boundaries. That strength does not overcome the current execution defect: active Epic 27 has no registered, owned path through its C1 gates to Story 27.4, and in-progress Story 27.3 includes acceptance criteria it explicitly cannot satisfy. The planning set is therefore not safe to treat as implementation-ready as a whole.

This status is scoped. Completed MVP/product capability history and independent work in other active epics are not invalidated. Independent work may continue only where its own prerequisites, ownership, and acceptance contract are complete; Epic 27 close-out and any work that relies on its production lifecycle claims must remain blocked.

### Critical Issues Requiring Immediate Action

1. Register real owners and producer-complete story files for Epic 27 C1.1–C1.25. Stories 27.5 and 27.6 are currently withdrawn, yet Story 27.4 requires them to be complete.
2. Rewrite Story 27.3 so every acceptance criterion is within its authority. Move held C1 criteria out of the story and register an explicit remediation owner for C0 gaps `DW 27.3-CR42` through `CR46` rather than depending on a completed Story 27.2.
3. Make the canonical story order executable without later-number prerequisites, especially for active/backlog Epic 30. Story 30.2 must precede Story 30.1 in the primary plan, not only in a sprint-status override.
4. Resolve the shared product-contract ambiguities before implementing or expanding answer-bearing Evidence Packets: answer-generation ownership, default case/tenant-wide search scope, freshness calculation, cross-surface capability semantics, and end-to-end first-packet latency.

### Recommended Next Steps

1. Run a focused course correction for Epic 27. Produce a small dependency map, split C1 gates by evidence domain and accountable owner, create the actual story files with per-gate producer commands/artifacts, approve their registration, and update `epics.md` plus `sprint-status.yaml` atomically.
2. Reduce Story 27.3 to independently completable C0/C2/C3/C4 slices. Convert its remaining numbered umbrella criteria into BDD acceptance criteria or producer-complete checkpoint rows, and do not resume Story 27.4 until the repaired predecessor chain is green.
3. Amend the PRD/architecture/UX contract in one decision record: Memories' responsibility for summaries versus LLM narratives; the omitted-case default and deliberate scope-broadening behavior; freshness thresholds/clock/roll-up; capability alignment rather than literal surface parity; and a measurable first Evidence Packet latency budget.
4. Normalize unfinished backlog structure. Give Story 28.1 the required checkpoint-heavy story file or split it; reorder Epic 30 so dependency order matches identifiers/document order; keep historical aliases only as traceability metadata.
5. Reconcile stale master acceptance criteria. Mark Story 12.1's seven-package and `chore(release)` requirements superseded, align Story 11.2 with guarded release dispatch, require actual pagination in Story 22.1, and pin Story 22.4 to the selected weighted-RRF architecture.
6. Re-run implementation readiness after the planning corrections. The minimum passing evidence is: no acceptance criterion without an owner, no registered story dependent on absent future work, no active forward-number dependency, and one authoritative rule for each UX/product contract listed above.

### Final Note

This assessment identified 29 actionable findings across four categories: PRD consistency, FR scope/coverage, UX alignment, and epic/story quality. The highest-risk findings are concentrated in Epic 27 rather than spread across the product foundation. Address the critical ownership and dependency defects before treating the plan as implementation-ready; the remaining findings can then be prioritized as contract reconciliation and planning-hygiene work.

**Assessment date:** 2026-08-02  
**Assessor:** Codex, using the BMad Implementation Readiness workflow
