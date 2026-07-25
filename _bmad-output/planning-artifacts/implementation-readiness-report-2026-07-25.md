---
stepsCompleted: ['step-01-document-discovery', 'step-02-prd-analysis', 'step-03-epic-coverage-validation', 'step-04-ux-alignment', 'step-05-epic-quality-review', 'step-06-final-assessment']
documentsAssessed:
  prd: '_bmad-output/planning-artifacts/prd.md'
  architecture: '_bmad-output/planning-artifacts/architecture.md'
  epics: '_bmad-output/planning-artifacts/epics.md'
  stories: '_bmad-output/implementation-artifacts/*.md'
  ux: '_bmad-output/planning-artifacts/ux-design-specification.md'
projectContext: '_bmad-output/project-context.md'
status: 'complete'
readinessStatus: 'NEEDS WORK'
issuesFound: 21
requirementCounts:
  functional: 74
  nonFunctional: 31
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-25
**Project:** Hexalith.Memories

## Step 1 — Document Discovery

### Document Inventory

| Type | File | Size | Lines | Last Modified | Format |
| --- | --- | --- | --- | --- | --- |
| PRD | `prd.md` | 86 KB | 1024 | 2026-07-19 | Whole |
| Architecture | `architecture.md` | 117 KB | 1729 | 2026-07-20 | Whole |
| Epics & Stories | `epics.md` | 327 KB | 5030 | 2026-07-25 | Whole |
| UX Design | `ux-design-specification.md` | 97 KB | 1164 | 2026-06-27 | Whole |

All core documents were located in `_bmad-output/planning-artifacts/` (the configured
`planning_artifacts` location). No sharded folders exist for any document type.

### Supporting Context Discovered

- **Story specifications:** 200+ individual story files in `_bmad-output/implementation-artifacts/`
- **Product brief:** `product-brief-Hexalith.Memories-2026-03-22.md`
- **Research:** `research/architecture-audit-2026-07-04.md`,
  `research/cerebras-knowledge-base-findings-2026-07-25.md`,
  `research/technical-kreuzberg-ocr-research-2026-03-28.md`
- **Sprint change proposals:** 58 files; most recent is
  `sprint-change-proposal-2026-07-25-cerebras-knowledge-base-findings.md`
- **Prior readiness reports:** 12; latest is `implementation-readiness-report-2026-07-04-rerun.md`
- **UX design directions:** `ux-design-directions.html` (exploratory, not an authoritative spec)
- **Project context:** `_bmad-output/project-context.md` (94 rules, status `complete`).
  The `project-context.md` bridge fail-closed check passed: the canonical file contains the
  active `Tenant isolation requires attached negative evidence` rule under `### Testing Rules`.

### Issues Found

- **Duplicates:** None. Every document type exists in exactly one format.
- **Missing required documents:** None. PRD, Architecture, Epics, and UX Design Specification
  are all present.

### Observations Carried Forward (not blockers at this step)

1. `epics.md` and `sprint-change-proposal-2026-07-25-cerebras-knowledge-base-findings.md` were
   both modified today. The most recent commit describes the addition of "Phase 2 backlog
   placeholders". Placeholder epic/story entries must be scrutinized for completeness in later
   steps.
2. `ux-design-specification.md` (2026-06-27) is the oldest core artifact while the PRD
   (2026-07-19), Architecture (2026-07-20), and Epics (2026-07-25) have all changed since.
   This is a UX-drift risk to test explicitly during UX alignment analysis.

**Step 1 status:** Complete. No unresolved duplicates or missing documents. Confirmed by user.

---

## Step 2 — PRD Analysis

Source: `_bmad-output/planning-artifacts/prd.md` (1025 lines, read in full).

### Functional Requirements

#### Knowledge Ingestion

- **FR1:** Developer can ingest content from local files into a specified case
- **FR2:** Developer can ingest content from URLs into a specified case
- **FR3:** Developer can batch-ingest content from a directory into a specified case
- **FR4:** System can extract text from ingested content (plain text, PDF, markdown)
- **FR5:** System can generate embeddings for ingested content via a configurable embedding provider
- **FR6:** System ensures a memory unit is fully searchable across all axes after ingestion completes
- **FR7:** Developer can attach metadata to ingested content, with each field tracking its origin
  (human-declared vs AI-inferred) and confidence score
- **FR8:** System manages ingestion load per tenant independently
- **FR9:** System retries failed ingestion automatically with configurable limits
- **FR10:** Developer can view ingestion status per case (queued, embedding, indexed, failed counts)
- **FR11:** Developer can view failed ingestion units with error details and failure stage
- **FR12:** Developer can manually trigger re-ingestion of failed or previously ingested content,
  individually or in bulk
- **FR13:** System handles partial backend write failures with defined recovery behavior
  (rollback or retry to achieve consistency across all axes)

#### Knowledge Retrieval

- **FR14:** Developer can search memory units by syntactic matching within a tenant
- **FR15:** Developer can search memory units by semantic similarity within a tenant
- **FR16:** Developer can search memory units by graph traversal within a tenant
- **FR17:** Developer can search memory units by hybrid fusion combining all available axes
- **FR18:** Developer can control which axes are included in a search query
- **FR19:** Developer can view per-axis score breakdown for each search result, including
  normalization method applied (explain mode)
- **FR20:** Developer can filter search results by case
- **FR21:** Developer can filter search results by metadata field values
- **FR22:** Developer can paginate search results
- **FR23:** LLM Agent can constrain search response size by token budget
- **FR24:** System returns the origin identifier (file path, URL, or event ID) and origin type
  for each search result
- **FR25:** Developer can run automated benchmark comparisons of hybrid vs single-axis search
  results with scored output

#### Memory Organization

- **FR26:** Developer can create a case within a tenant
- **FR27:** Developer can delete a case and all its memory units
- **FR28:** Developer can add members to a case
- **FR29:** Developer can remove members from a case
- **FR30:** Developer can list cases within a tenant
- **FR31:** Developer can view case status including memory unit count, last activity timestamp,
  and health indicators
- **FR32:** System enforces strict single-case ownership per memory unit — reassignment requires
  deletion and re-ingestion
- **FR33:** System maintains case-scoped graph edges between memory units within a case
- **FR34:** Developer can search across all cases within a tenant by keyword, returning results
  with case attribution
- **FR35:** Developer can delete an individual memory unit from a case
- **FR36:** Developer can view recent activity within a case (ingestion events, searches,
  membership changes)
- **FR37:** Developer can annotate or correct a memory unit, with annotations tracked as linked
  memory units

#### Tenant Management

- **FR38:** Operator can create a tenant with physically separate indexes
- **FR39:** Operator can delete a tenant and all its indexes, graph data, and memory units
- **FR40:** Operator can verify tenant isolation via automated checks
- **FR41:** Operator can list tenants
- **FR42:** Operator can update tenant configuration after creation (rate limits, display name,
  settings)
- **FR43:** System prevents configuration changes that would create data inconsistency without
  explicit operator acknowledgment
- **FR44:** System enforces tenant context at all access layers, rejecting cross-tenant requests
  with clear error messages
- **FR45:** Operator can view current configuration of a tenant (embedding provider, rate limits,
  index status)

#### Causal Intelligence

- **FR46:** System can index CausationId and CorrelationId from events as typed, directional
  graph edges
- **FR47:** Developer can traverse causal chains from a starting node with configurable depth
- **FR48:** Developer can filter graph traversal by edge type
- **FR49:** When an intermediate node in a causal chain is not indexed, the traversal result
  includes a gap marker with the missing node identifier
- **FR50:** System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`,
  `annotates` — each with default confidence
- **FR51:** Developer can promote AI-inferred edge confidence when verifying a relationship
- **FR52:** System maintains chronological ordering and timestamps on causal chain nodes

#### Developer Interfaces

- **FR53:** Developer can interact with all retrieval and ingestion capabilities via CLI
- **FR54:** Developer can interact with search, ingestion, traversal, and case-info capabilities
  via MCP tools
- **FR55:** CLI supports multiple output formats: human-readable (default), JSON, and table
- **FR56:** CLI provides actionable error messages with recovery suggestions for common failure
  modes
- **FR57:** Developer can discover available actions from any system state, including empty states
  and error conditions
- **FR58:** MCP tools include typed parameter schemas with descriptions for LLM agent consumption

#### EventStore Integration

- **FR59:** System can auto-discover event types published to DAPR pub/sub topics
- **FR60:** System can generate dual embeddings for events (raw payload + natural language
  description)
- **FR61:** System can automatically index CausationId/CorrelationId metadata as graph edges
  without developer mapping code
- **FR62:** Developer can list registered event handlers and detect handler registration
  mismatches

#### Trust & Transparency

- **FR63:** System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each
  search result
- **FR64:** System tracks metadata origin (human-declared vs AI-inferred) and confidence per
  metadata field on every memory unit
- **FR65:** System records `ingested_by` (user or system identity) as a mandatory field on every
  memory unit
- **FR66:** When one or more search backends are unavailable, system returns partial results with
  an indication of which axes were excluded
- **FR67:** System logs search and access events per tenant for audit purposes

#### Embedding Provider Management

- **FR68:** Operator can configure embedding provider and model per tenant
- **FR69:** System enforces per-tenant rate limit ceilings for embedding API calls
- **FR70:** System tracks the embedding provider and model used for each memory unit's vectors

#### Data Portability & System Health

- **FR71:** Developer can export all memory units, metadata, and graph edges for a case or tenant
  in a portable format. **Phase:** Phase 2 unless a later sprint change explicitly pulls export
  into MVP.
- **FR72:** System exposes readiness and liveness health checks verifying all backends
- **FR73:** Operator can detect index/graph divergence via consistency check
- **FR74:** Operator can repair detected index/graph inconsistencies via consistency repair
  operation

**Total FRs: 74** (FR1–FR74, contiguous — no gaps, no duplicate identifiers)

### Non-Functional Requirements

NFRs are tagged by validation phase in the PRD: **[MVP]**, **[P1.5]**, **[Ongoing]**.

#### Performance

- **NFR1:** Syntactic search latency (p95) <200ms — 10 concurrent queries/tenant, 10K memory
  units/tenant. *Phase: MVP*
- **NFR2:** Semantic search latency (p95) <500ms — 10 concurrent queries/tenant, 10K memory
  units/tenant. *Phase: MVP*
- **NFR3:** Hybrid search latency (p95) <1s — 10 concurrent queries/tenant, 10K memory
  units/tenant. *Phase: MVP*
- **NFR4:** Graph traversal latency (p95) <2s — 10 concurrent queries/tenant, 10K memory
  units/tenant, depth ≤5. *Phase: MVP*
- **NFR5:** Ingestion throughput >100 memory units/min (payloads ≤10KB), >10 memory units/min
  (payloads ≤1MB) — per tenant, single-document embedding calls (not batched). *Phase: Ongoing*
- **NFR6:** Event indexing freshness <5s from DAPR pub/sub publication to searchable under normal
  conditions; degradation documented when embedding provider is rate-limited. *Phase: P1.5*
- **NFR7:** Cold start time — service fully operational within 60s from containers running to
  accepting queries (excludes image pull time). *Phase: Ongoing*

#### Security

- **NFR8:** Zero cross-tenant data leakage — no search, ingestion, or graph traversal returns data
  from another tenant. Verified by automated test suite across all axes with malformed/empty/swapped
  tenant IDs, plus a graph-specific test creating identical graph structures in tenants A and B and
  verifying zero nodes from B appear when traversing from A even if edge IDs collide. *Phase: MVP*
- **NFR9:** Product services retrieve embedding-provider and other application runtime secrets
  exclusively through the DAPR Secrets API, backed by OpenBao in Aspire and deployed environments.
  Secret values are never stored in application configuration or ordinary environment variables.
  Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or
  direct pod inputs outside the DAPR secret-store boundary. Verified by structural dependency tests,
  secret scanning, AppHost topology tests, and integration tests. *Phase: Ongoing*
- **NFR10:** All inter-service communication authenticated via DAPR API tokens. *Phase: Ongoing*
- **NFR11:** External access authenticated at ingress layer — no unauthenticated access to REST API
  endpoints. *Phase: P1.5*

#### Scalability

- **NFR12:** System supports linear scaling of tenants — adding a new tenant does not degrade
  existing tenant performance by more than 5%. Validated at 10 tenants, each with 100K memory
  units. *Phase: Ongoing*
- **NFR13:** Per-tenant ingestion pipeline scales independently — one tenant's batch ingestion does
  not block another tenant's real-time ingestion. *Phase: Ongoing*
- **NFR14:** Redis memory footprint per memory unit is predictable and documented — operator can
  estimate infrastructure costs before tenant provisioning. *Phase: Ongoing*
- **NFR15:** Architecture must not preclude backend migration (Redis → Qdrant) — concrete
  implementation with clear extraction points identified, no premature interfaces. *Phase: Ongoing*

#### Reliability

- **NFR16:** Zero memory unit loss during Redis restart — AOF persistence enabled and verified.
  *Phase: MVP*
- **NFR17:** Ingestion pipeline state survives process restarts — queued and in-progress units
  resume without data loss. DAPR actor state persistence verified. *Phase: MVP*
- **NFR18:** Partial backend failure (one of three backends down) results in degraded service, not
  total failure — available axes continue serving results. *Phase: Ongoing*
- **NFR19:** Failed ingestion units are never silently dropped — all failures visible via CLI status
  with error details and failure stage. *Phase: Ongoing*

#### Integration

- **NFR20:** MCP tool responses conform to MCP protocol specification — valid tool schemas, typed
  parameters, structured error responses. *Phase: P1.5*
- **NFR21:** DAPR pub/sub integration handles CloudEvents envelope format — events from any
  DAPR-compatible publisher are processable. *Phase: P1.5*
- **NFR22:** Embedding provider integration handles rate limiting gracefully — 429 responses trigger
  backoff without pipeline crash or data loss. *Phase: Ongoing*
- **NFR23:** CLI connects to the memory server via configurable endpoint — supports local dev
  (localhost), container (docker service name), and remote (ingress URL) environments.
  *Phase: Ongoing*

#### Algorithmic Quality

- **NFR24:** Hybrid fusion uses deterministic weighted reciprocal-rank fusion with per-axis rank
  contributions in 0.0-1.0; single-axis explain still documents axis-specific score semantics.
  *Phase: MVP*
- **NFR25:** Fusion algorithm produces deterministic scores — same query against same data produces
  identical composite scores. Result ordering within the same score tier may vary. *Phase: MVP*
- **NFR26:** Benchmark suite produces reproducible results — running benchmarks twice against the
  same dataset yields identical NDCG@10 scores. *Phase: MVP*

#### Observability

- **NFR27:** Structured JSON logging with OpenTelemetry correlation IDs from DAPR trace context.
  *Phase: Ongoing*
- **NFR28:** Trace context propagates across all DAPR service invocation hops — end-to-end trace
  from CLI/MCP through server to backend. *Phase: Ongoing*
- **NFR29:** Custom metrics exported via OpenTelemetry: ingestion throughput, search latency per
  axis, index size per tenant, pipeline queue depth. *Phase: Ongoing*

#### Documentation Quality

- **NFR30:** Every CLI command includes `--help` with at least one usage example. *Phase: MVP*
- **NFR31:** README includes working quickstart that completes in <30 minutes on a clean machine
  with Docker installed. *Phase: MVP*

**Total NFRs: 31** (NFR1–NFR31, contiguous — no gaps, no duplicate identifiers)

### Additional Requirements

Requirements and constraints that carry implementation obligations but are **not** expressed as
numbered FR/NFR identifiers. These are the highest traceability risk, because epics cannot be
mapped to them by ID.

#### Gates and kill switches

- **Three-Axis Kill Switch:** 80% of 5–10 benchmark queries must show measurably better hybrid
  results than any single axis. Ground truth defined by Jerome + 2 independent reviewers *before*
  queries are written; NDCG@10 automated scoring; human dispute resolution; inter-rater agreement
  ≥80% as a validity gate.
- **MVP Go/No-Go Gate:** 3 hard gates (three-axis validation at 80%; zero cross-tenant leaks;
  onboarding <30 min) must all pass; at least 2 of 3 soft gates (causal chain completeness ≥95%;
  MCP end-to-end integration; case model correctly scopes memory) must pass.
- **Causal Chain Completeness:** ≥95% of EventStore events with known CausationId/CorrelationId
  chains return the complete causal path, validated by automated tests.
- **Phase 1.5 hard commitment:** ships within 4 weeks of thesis validation, or MCP Server moves
  back into MVP.

#### Cross-surface contract obligations

- **Evidence Packet:** a named cross-surface trust envelope composing FR63 (composite confidence
  with per-axis breakdown), FR24 (source/origin attribution), FR23 (token-budget omitted-detail
  handling), FR66 (graceful-degradation signaling), plus tenant/case scope, result state, and
  recovery guidance. Shape owned by `Contracts.V1`; used identically by CLI JSON, MCP responses,
  and future web UI. **It has no FR/NFR identifier of its own.**
- **Confidence-score caveat placement:** the "confidence measures query-result relevance, NOT
  factual accuracy or completeness" distinction *must* appear in API reference docs, CLI `--explain`
  output (every explain result), the compliance enablement guide, and MCP tool response schema
  documentation.
- **Edge Type Taxonomy (MVP minimum):** `caused_by` (1.0), `correlated_with` (0.8), `references`
  (0.5–1.0), `contains` (1.0), `annotates` (1.0). `caused_by` must never collapse into
  `correlated_with`.
- **Score semantics table:** single-axis scores keep axis-specific meaning (BM25 saturation, cosine
  clamp, inverse-hop decay); hybrid per-axis scores are weighted reciprocal-rank contributions, not
  raw magnitudes. `--explain` must expose fusion weights applied.
- **Error propagation chain:** four hops (Server→DAPR, MCP→Agent, Ingress→CLI, CLI→terminal), each
  with a defined format; internal DAPR errors must never collapse into a generic 502.
- **Graceful-degradation signaling:** `"degraded": true` plus which axes were unavailable.
- **Token-budget overflow behavior:** truncate by relevance rank, include "X additional results
  omitted" count, name omitted detail groups, provide deterministic expansion handles.

#### Documentation deliverables (obligations without FR IDs)

- `LICENSE-DEPENDENCIES.md` documenting the FalkorDB AGPL architectural boundary
- SSPL constraint stated in the README deployment section
- Apache 2.0 non-relicensing public commitment in the README
- "Building Compliant Applications on Memories" guide
- "Limitations of Infrastructure-Level Deletion" section (cross-tenant reference caveat)
- "Security Posture for Auditors" section
- Legal disclaimer ("architectural patterns, not legal advice")
- Operator guide covering tenant management, embedding-provider migration (full reindex), scaling
- Redis sizing guide (NFR14's published artifact)
- `samples/01-quickstart/`, `samples/02-eventstore-integration/`, `samples/03-mcp-agent/`
- CONTRIBUTING.md documenting the Docker requirement for integration tests

#### Technical constraints and architectural commitments

- **Redis Vector index schema is fixed at creation** — switching embedding providers requires a full
  tenant reindex (migration operation, not configuration).
- **FalkorDB version pinning** in the default docker-compose.yml as relicensing insurance.
- **`IMemoryIndex` / `IMemoryGraph` extraction points identified in Phase 2** — licensing insurance,
  not premature abstraction.
- **Package inventory:** 9 published NuGet packages + 3 non-packable service/orchestration projects;
  `tools/release-packages.json` is the authoritative source of truth.
- **`Server` must not depend on the compatibility-only `Hexalith.Memories.Redis` package**; backend
  implementations registered at the composition root.
- **No standalone server deployment** — Aspire AppHost orchestrates all services with DAPR sidecars.
- **Configuration layering precedence:** CLI flags → env vars (`HEXALITH_MEMORIES_*`) → config file
  → DAPR Secrets (OpenBao) → DAPR configuration. Sensitive values never resolved via configuration
  fallback.
- **Per-tenant pipeline actor** owns throttling, ordering, progress; **no per-document actors**.
- **Pipeline stage model:** `queued` → `extracting` → `embedding` → `indexing` → `indexed`, with
  `failed` as a visible dead-letter state.
- **Test layering:** unit (mock `DaprClient`, no sidecar), integration (Aspire
  `DistributedApplicationTestingBuilder` or DAPR testcontainers), contract (serialization
  round-trip). Contributors can run unit tests without Docker.
- **Interface Capability Parity Matrix** — an explicit non-goal statement that MCP does *not* expose
  tenant management, isolation verification, status, explore, or handlers.
- **Submodule dependencies:** `references/Hexalith.Commons`, `references/Hexalith.EventStore`.

#### Scope-reduction fallbacks (contingency requirements)

- **Resource-risk floor:** if resources tighten, MVP reduces to Engine + Search + CLI
  (ingest/search) + Benchmarks — ~13–18 stories; cases and tenant isolation deferred.
- **Fusion fallback:** if the graph axis adds no value, fall back to two-axis retrieval with graph
  reserved for causal traversal only, and reposition the product narrative.
- **Zero-code fallback:** narrow the claim to "zero-code for EventStore, minimal-code for others"
  with a documented thin adapter pattern.

### PRD Completeness Assessment

**Overall: the PRD is unusually strong on structure and unusually weak on phase-traceability.**

**Strengths**

1. **Requirement identifiers are clean.** FR1–FR74 and NFR1–NFR31 are contiguous with no gaps,
   no duplicates, and no re-used numbers. This is rare and makes coverage validation tractable.
2. **NFRs are testable.** Every NFR carries a metric, a target, explicit conditions, and a named
   verification method. NFR8 in particular specifies the *adversarial* test design (swapped tenant
   IDs, colliding edge IDs), not just the assertion.
3. **Falsifiability is designed in.** The three-axis kill switch, the go/no-go gate table, and the
   per-innovation validation table define what failure looks like before implementation starts.
4. **Journeys are traceable to capabilities.** The Journey Requirements Summary plus the coverage
   check make it possible to test journey→FR mapping rather than guess at it.
5. **Boundaries are stated, not implied.** The three-tier responsibility model, the confidence-vs-
   accuracy distinction, and the Interface Capability Parity Matrix each state what the product
   explicitly does *not* do.

**Gaps and risks to test in later steps**

1. **CRITICAL — 73 of 74 FRs carry no phase tag.** Every NFR is tagged MVP/P1.5/Ongoing, but only
   FR71 has a phase. MVP vs Phase 1.5 vs Phase 2 boundaries for the other 73 FRs must be inferred
   from prose in the scoping section. This is the single largest traceability hazard in the
   document: epic coverage cannot be validated against a phase the PRD never assigned.
2. **CRITICAL — the Evidence Packet has no requirement ID.** It is defined as a mandatory
   cross-surface contract binding four FRs together, owned by `Contracts.V1`, consumed identically
   by CLI/MCP/Web. A contract this load-bearing with no FR number cannot be coverage-checked by ID.
3. **HIGH — internal cross-reference defect.** The Journey 2 scope note requires
   `memories handlers --list` and event replay to be "explicitly included in MVP Feature #3
   (EventStore Integration)". But in the MVP Feature Set table, **Feature #3 is Three-Axis Search**;
   EventStore Integration is **Phase 1.5 Feature #1**. Either the note points at the wrong feature
   or it silently pulls Phase 1.5 work into the MVP. The PRD's own scope note says this journey
   "overpromises" if unresolved.
4. **HIGH — declared MVP scope vs actual scope.** The PRD estimates **22–32 stories across 7
   features** for a solo developer. `epics.md` is 5030 lines. If the delivered epic set is an order
   of magnitude larger, either the PRD scope section is stale or the epics have absorbed
   Phase 1.5/2/3 work without the PRD being updated. To be verified in Step 3.
5. **MEDIUM — FR/CLI phase conflict.** FR10 and FR11 (ingestion status, failed units) are untagged
   FRs, but the CLI Specification places `memories status` in the **Phase 1.5** expansion scope, and
   NFR19 (failures never silently dropped, "visible via CLI status") is tagged *Ongoing*. The
   delivery phase for status visibility is genuinely ambiguous across three sections.
6. **MEDIUM — platform version drift.** The PRD states server runtime **".NET 10 / C# 13"**, while
   `project-context.md` states **.NET 10 / C# 14**. Minor, but the PRD is the stale side.
7. **MEDIUM — untagged FRs that read as post-MVP.** FR34 (cross-case search), FR36 (case activity),
   FR37 (annotations/corrections) map to Journey 4 and to Phase 2 themes (discussion threading,
   memory diffing) yet carry no phase marker — so they are indistinguishable from MVP requirements
   during coverage validation.
8. **LOW — documentation obligations are unnumbered.** Eleven distinct documentation deliverables
   (compliance guide, auditor posture, LICENSE-DEPENDENCIES, sizing guide, three samples, etc.)
   carry real acceptance obligations but no IDs, so they are easy to drop silently from epics.
9. **LOW — `--explain` caveat is a four-place obligation.** The confidence-vs-accuracy caveat must
   appear in four separate surfaces. Partial implementation would satisfy no single FR while
   violating the stated requirement.

**Assessment for coverage validation:** the FR/NFR identifier set is clean enough to drive a
rigorous traceability matrix in Step 3. The phase-tag gap means coverage validation must check
**both** that each FR is covered by an epic **and** that the epic's phase matches the phase the PRD
narrative implies — because the PRD will not supply that answer by ID.

---

## Step 3 — Epic Coverage Validation

Source: `_bmad-output/planning-artifacts/epics.md` (5030 lines, 30 epic bodies, 159 stories) plus
`_bmad-output/implementation-artifacts/sprint-status.yaml` (the readiness-accounting authority named
by `epics.md`).

`epics.md` carries an explicit **FR Coverage Map** (lines 317–392) claiming all 74 FRs. That claim
was not taken at face value: every FR was traced from the map, to the epic-level `FRs covered`
declaration, to an actual story acceptance criterion.

### Coverage Matrix

Legend — **Anchored**: an acceptance criterion carries the inline `(FRn)` tag.
**Untagged**: the behavior is specified by a story, but no AC carries the FR ID.

| FR | PRD Requirement (abbreviated) | Epic Coverage | Story Anchor | Status |
| --- | --- | --- | --- | --- |
| FR1 | Ingest from local files | Epic 1 | Story 1.3 / 1.6 | ⚠ Untagged |
| FR2 | Ingest from URLs | Epic 6 | Story 6.1 | ✓ Anchored |
| FR3 | Batch-ingest from directory | Epic 6 | Story 6.1 | ✓ Anchored |
| FR4 | Extract text (txt, PDF, markdown) | Epic 1 | Story 1.3 | ⚠ Untagged |
| FR5 | Generate embeddings | Epic 1 | Story 1.4 | ⚠ Untagged |
| FR6 | Searchable across all axes after ingestion | Epic 1 (+23) | Story 1.5 / 1.6 | ⚠ Untagged |
| FR7 | Metadata with origin + confidence | Epic 1 | Story 1.6 | ✓ Anchored |
| FR8 | Per-tenant ingestion load management | Epic 6 | Story 6.2 | ✓ Anchored |
| FR9 | Auto-retry with configurable limits | Epic 6 | Story 6.3 | ✓ Anchored |
| FR10 | Ingestion status per case | Epic 6 | Story 6.3 | ✓ Anchored |
| FR11 | Failed-unit visibility + stage | Epic 6 | Story 6.3 | ✓ Anchored |
| FR12 | Manual re-ingestion | Epic 6 (+23) | Story 6.3, 23.4 | ✓ Anchored |
| FR13 | Partial backend write recovery | Epic 1 (+21) | Story 21.2 | ✓ Anchored |
| FR14 | Syntactic search | Epic 2 | Story 2.1 | ⚠ Untagged |
| FR15 | Semantic search | Epic 2 | Story 2.2 | ⚠ Untagged |
| FR16 | Graph search | Epic 2 | Story 2.3 | ⚠ Untagged |
| FR17 | Hybrid fusion search | Epic 2 | Story 2.5 | ⚠ Untagged |
| FR18 | Axis selection control | Epic 2 | Story 2.5 | ✓ Anchored |
| FR19 | Per-axis score breakdown (explain) | Epic 2 | Story 2.6 | ✓ Anchored |
| FR20 | Filter search by case | Epic 3 | Story 3.4 | ✓ Anchored |
| FR21 | Filter search by metadata | Epic 3 | Story 3.4 | ✓ Anchored |
| FR22 | Paginate search results | Epic 2 (+22) | Story 2.6, 22.1 | ✓ Anchored |
| FR23 | Token-budget constraint | Epic 10 | Story 10.2 | ✓ Anchored |
| FR24 | Origin identifier + type per result | Epic 2 | Story 2.1, 2.2, 2.6 | ✓ Anchored |
| FR25 | Benchmark hybrid vs single-axis | Epic 2 | Story 2.8 | ✓ Anchored |
| FR26 | Create a case | Epic 0 + 3 | Story 0.2 (3.1 delivers) | ✓ Anchored |
| FR27 | Delete case + units | Epic 3 | Story 3.5 | ✓ Anchored |
| FR28 | Add case members | Epic 3 | Story 3.3 | ⚠ Untagged |
| FR29 | Remove case members | Epic 3 | Story 3.3 | ⚠ Untagged |
| FR30 | List cases | Epic 3 | Story 3.1 | ✓ Anchored |
| FR31 | Case status | Epic 3 | Story 3.2 | ✓ Anchored |
| FR32 | Single-case ownership | Epic 3 | Story 0.2, 3.1 | ✓ Anchored |
| FR33 | Case-scoped graph edges | Epic 3 | Story 3.1 | ✓ Anchored |
| FR34 | Cross-case tenant search | Epic 3 (+22) | Story 3.4, 22.4 | ✓ Anchored |
| FR35 | Delete a memory unit | Epic 3 | Story 3.5 | ✓ Anchored |
| FR36 | Case activity | Epic 3 | Story 3.2, 3.3 | ✓ Anchored |
| FR37 | Annotations / corrections | Epic 3 | Story 3.6 | ✓ Anchored |
| FR38 | Create tenant, separate indexes | Epic 0 + 5 (+24) | Story 0.1 (5.1 delivers) | ✓ Anchored |
| FR39 | Delete tenant + all data | Epic 5 (+21) | Story 5.2 | ⚠ Untagged |
| FR40 | Verify tenant isolation | Epic 5 (+24) | Story 5.3 | ✓ Anchored |
| FR41 | List tenants | Epic 5 | Story 5.5 | ✓ Anchored |
| FR42 | Update tenant config | Epic 5 | Story 5.5 | ✓ Anchored |
| FR43 | Prevent inconsistent config change | Epic 5 | Story 5.5 | ✓ Anchored |
| FR44 | Enforce tenant context all layers | Epic 0 + 5 (+20, 24) | Story 0.1, 0.2, 0.3, 5.4 | ✓ Anchored |
| FR45 | View tenant configuration | Epic 5 | Story 5.5 | ✓ Anchored |
| FR46 | Index Causation/CorrelationId as edges | Epic 1 | Story 1.5 / 1.6 | ⚠ Untagged |
| FR47 | Traverse causal chains, configurable depth | Epic 4 | Story 4.1 | ⚠ Untagged |
| FR48 | Filter traversal by edge type | Epic 4 | Story 4.2 | ✓ Anchored |
| FR49 | Gap markers for missing nodes | Epic 4 | Story 4.3 | ✓ Anchored |
| FR50 | Edge type taxonomy + confidence | Epic 4 | Story 4.2 | ✓ Anchored |
| FR51 | Promote AI-inferred confidence | Epic 4 | Story 4.3 | ✓ Anchored |
| FR52 | Chronological ordering on chains | Epic 4 | Story 4.1 | ✓ Anchored |
| FR53 | CLI for all capabilities | Epic 7 | Story 7.1 | ✓ Anchored |
| FR54 | MCP tools | Epic 10 | Story 10.1 | ✓ Anchored |
| FR55 | CLI output formats | Epic 7 | Story 7.2 | ✓ Anchored |
| FR56 | Actionable CLI errors | Epic 7 | Story 7.3 | ✓ Anchored |
| FR57 | Discoverable actions from any state | Epic 7 | Story 7.3, 7.4 | ✓ Anchored |
| FR58 | MCP typed parameter schemas | Epic 10 | Story 10.1 | ✓ Anchored |
| FR59 | Auto-discover event types | Epic 9 | Story 9.1 | ✓ Anchored |
| FR60 | Dual embeddings for events | Epic 9 | Story 9.2 | ✓ Anchored |
| FR61 | Auto-index causal metadata | Epic 9 | Story 9.2 | ✓ Anchored |
| FR62 | List handlers, detect mismatches | Epic 9 | Story 9.3 | ✓ Anchored |
| FR63 | Composite confidence + per-axis breakdown | Epic 2 | Story 2.6 (+2.7) | ✓ Anchored |
| FR64 | Metadata origin tracking display | Epic 7 | Story 7.2 | ✓ Anchored |
| FR65 | `ingested_by` mandatory field | Epic 1 | Story 1.6 | ✓ Anchored |
| FR66 | Partial results on backend failure | Epic 5 | Story 2.5, 5.6 | ✓ Anchored |
| FR67 | Search/access audit logging | Epic 7 (+20) | Story 7.5, 8.4, 20.2 | ✓ Anchored |
| FR68 | Configure embedding provider per tenant | Epic 1 | Story 1.7 | ⚠ Untagged |
| FR69 | Per-tenant embedding rate limits | Epic 5 | Story 5.5 | ✓ Anchored |
| FR70 | Track embedding model per unit | Epic 5 | Story 5.5 | ✓ Anchored |
| FR71 | Portable export (case/tenant) | Epic 26 (partial) | Phase 2 placeholder, Story 8.3 reserved | ◑ Deferred by design |
| FR72 | Readiness/liveness health checks | Epic 8 | Story 8.1 | ✓ Anchored |
| FR73 | Detect index/graph divergence | Epic 8 | Story 8.2 | ✓ Anchored |
| FR74 | Repair index/graph inconsistencies | Epic 8 | Story 8.2 | ✓ Anchored |

### Missing Requirements

**No functional requirement is missing.** Every FR1–FR74 is delivered by at least one story, and
FR71 is deferred by an explicit, governed decision rather than by omission. There is no orphan
FR — the epics inventory reproduces FR1–FR74 exactly, with no requirement numbered above FR74 and
no epic-invented requirement absent from the PRD.

The defects found are traceability-quality and document-consistency defects, not coverage holes.

#### Critical findings

**C1 — Epic 2 specifies a fusion algorithm the PRD no longer mandates.**
The PRD (updated 2026-07-19) NFR24 requires *"deterministic weighted reciprocal-rank fusion with
per-axis rank contributions"*, and its Confidence Score Semantics table states that hybrid per-axis
scores are *"rank-contribution scores, not raw BM25, cosine, or graph-proximity magnitudes."*
Epic 2 still encodes the superseded model:
- Story 2.4 (*Score Normalization*): "I want all search axis scores normalized to 0.0-1.0 **before
  fusion**" — and its final AC cites **(NFR24)** for behavior NFR24 no longer describes.
- Story 2.5 (*Fusion Algorithm & Hybrid Search*): "the composite score is a **weighted average of
  normalized axis scores**."
- The `epics.md` Requirements Inventory copy of NFR24 reads "All axis scores normalized to 0.0-1.0
  before fusion" — the pre-RRF text.

Only two places in the entire epics document mention RRF: Story 22.3 (line 4324), which permits
*"RRF **or** per-axis min-max"* — still ambiguous — and Epic 26 (line 4794), which assumes
*"production weighted-RRF calibration"*. A developer reading `epics.md` for the core product thesis
gets a direct contradiction on the single most important algorithm in the product.

**C2 — Two epics exist in the document but in no index or readiness classification.**
Epic 28 (*Owner-Approved EventStore Runtime Adoption*) and Epic 29 (*OpenBao-First Dapr Secret
Management*) have full bodies and stories (lines 4909–5030) but are **absent from the Epic List**,
which ends at Epic 27.

Worse, `sprint-status.yaml` — which `epics.md` line 411 declares the authority that "readiness
reports and story tooling **must** use rather than inferring MVP readiness from story status,
numeric ordering, or FR coverage alone" — omits **epic-27 and epic-28** from both
`readiness_accounting.epic_metadata` and `excluded_unless_sprint_selected` (it jumps from epic-26 to
epic-29). With `default_story_inherits_epic_metadata: true`, stories in Epics 27 and 28 inherit no
track, no phase, and no `mvpReadiness` value.

This is not hypothetical: **Epic 27 is currently `in-progress`** (Story 27.3 in-progress, 27.4
backlog). Live work is running outside the readiness-accounting system that governs it.

**C3 — `epics.md` NFR9 is two policy generations stale.**
The PRD requires secrets be retrieved *"exclusively through the DAPR Secrets API, backed by
OpenBao"*, with Kubernetes Secrets restricted to documented bootstrap material. The epics inventory
still reads *"Embedding API keys stored in secure secret management — never in config files."*
That predates both the OpenBao decision and Epic 29, which exists specifically to implement it. Any
story written against the epics inventory would under-specify NFR9.

#### High findings

**H1 — 14 FRs have no inline acceptance-criterion anchor (19% of the requirement set).**
FR1, FR4, FR5, FR6, FR14, FR15, FR16, FR17, FR28, FR29, FR39, FR46, FR47, FR68 are claimed in the FR
Coverage Map and delivered by clearly-titled stories, but no acceptance criterion carries their FR
ID. The behavior is present; the machine-checkable link is not.

This matters disproportionately because the untagged set includes **FR14–FR17 — the three retrieval
axes and the hybrid fusion that constitute the entire product thesis** — plus FR46/FR47, the two
halves of the causal-intelligence differentiator. The requirements most central to the MVP go/no-go
gate are precisely the ones that cannot be verified by ID-based traceability tooling.

**H2 — The `Implementation Readiness Boundary` does not classify four of its own epics.**
The boundary section (lines 407–425) enumerates Epic 0, Epics 1–8, Epics 9–10, Epics 11–16 and 18,
Epic 17, and Epics 20–26. **Epics 19, 27, 28, and 29 are never classified.** Epic 19 is at least
covered by `sprint-status.yaml`; Epics 27 and 28 are covered by neither (see C2).

#### Medium findings

**M1 — Several NFR restatements in the epics inventory silently drop verification detail.**
Beyond NFR9 and NFR24: NFR8 loses the graph-collision test design (identical structures in tenants
A and B with colliding edge IDs); NFR5 loses "single-document embedding calls (not batched)"; NFR6
loses the rate-limited degradation clause; NFR12 loses its benchmark methodology; NFR25 loses
"result ordering within the same score tier may vary"; NFR29 loses "pipeline queue depth". A story
written from the epics inventory would under-test each of these.

**M2 — The Phase 2 Backlog Placeholders block is positioned inside the MVP sequence.**
It sits between Epic 8 and Epic 9 (lines 2179–2265), holding FR71 export plus five new
Cerebras-sourced placeholders added today. Each carries a correct Phase Note and Activation rule, so
governance is sound — but the placement puts non-MVP content in the middle of the MVP-to-Phase-1.5
reading path.

**M3 — FR26 and FR38 are anchored only at their Epic 0 minimal-bootstrap stories.**
The tags sit on Story 0.2 and Story 0.1 (minimal case bootstrap, tenant provisioning), while the
full capability is delivered by Story 3.1 and Story 5.1 — which carry no FR26/FR38 tag. ID-based
tooling would report these FRs as satisfied by the foundation stub alone.

### Coverage Statistics

| Metric | Value |
| --- | --- |
| Total PRD FRs | 74 |
| FRs claimed in the epics FR Coverage Map | 74 (100%) |
| FRs substantively delivered by at least one story | 74 (100%) |
| **FRs anchored to an acceptance criterion by ID** | **59 (79.7%)** |
| FRs delivered but untagged | 14 (18.9%) |
| FRs deferred by explicit governed decision (FR71) | 1 (1.4%) |
| FRs present in epics but absent from the PRD | 0 |
| FRs present in the PRD but absent from epics | 0 |
| Total epic bodies | 30 (Epic 0–29) |
| Epics listed in the Epic List | 28 (Epic 0–27) |
| Epics classified by the Implementation Readiness Boundary | 26 (19, 27, 28, 29 unclassified) |
| Epics classified in `sprint-status.yaml` readiness accounting | 28 (27, 28 unclassified) |
| Total stories | 159 |
| Stories `done` / `in-progress` / `backlog` | 199 / 3 / 4 status rows in sprint-status |

**Verdict for this step:** FR coverage is **complete and genuinely traceable in substance**. The
epics document is the strongest artifact in the set for breadth — nothing was forgotten. What it
lacks is *machine-verifiable* traceability on 19% of requirements (including the entire three-axis
thesis), and it carries a live contradiction against the PRD on the fusion algorithm plus two epics
that no governance index acknowledges.

---

## Step 4 — UX Alignment Assessment

Sources: `ux-design-specification.md` (1164 lines, `workflow_completed: true`, 14 steps),
cross-checked against `prd.md`, `architecture.md`, and `epics.md`.

### UX Document Status

**FOUND — and complete.** The specification covers design-system foundation, core experience,
emotional response, visual foundation, design-direction decision, Evidence Packet invariants, five
user journey flows, component strategy, consistency patterns, and responsive/accessibility strategy.
A supporting `ux-design-directions.html` documents the eight explored directions.

The UX spec is **not** a stub and does not weaken readiness on substance. Its problems are
traceability and currency, not content.

### What Aligns (verified, not assumed)

1. **Evidence Packet is coherent across all three documents.** The UX spec's 8-part packet anatomy
   (scope strip → result summary → evidence summary → source list → reasoning trace → confidence/
   freshness/health → graph context → recovery footer) maps cleanly onto the architecture's 8
   minimum `Contracts.V1` fields (`scope`, `result`, `sources`, `evidence`, `graph`, `state`,
   `omittedDetails`, `recoveryActions`), which in turn compose the PRD's FR23/FR24/FR63/FR66. The
   only divergence is naming: the UX spec's "reasoning trace" is folded into the architecture's
   `evidence` field (retrieval axes used, per-axis score summary). Semantically covered.

2. **The FrontComposer + Fluent UI Blazor V5 mandate is consistent four ways** — UX spec
   (lines 338–342), architecture (line 144, `Hexalith.Memories.Web` as a FrontComposer-aligned RCL
   guarded by conformance tests), epics UX-DR15, and `project-context.md`. All four also agree on
   the same exception rule (custom markup only for justified gaps, conformance-tested) and on
   banning legacy Fluent v4/FAST tokens.

3. **Responsive and accessibility targets match exactly.** UX spec breakpoints (mobile 320–767,
   tablet 768–1023, desktop 1024+, wide 1440+) and test viewports (360/768/1024/1440) are reproduced
   verbatim in UX-DR34; WCAG 2.2 AA in UX-DR35; the automated-plus-human validation split in UX-DR39.

4. **MVP scope discipline is explicit and correct.** UX spec line 582 limits MVP UX acceptance to
   "CLI-visible and contract-visible evidence semantics," making Fluent/FrontComposer compositions,
   browser layouts, and visual accessibility checks binding **only** when web UI enters an approved
   implementation phase. This matches the Epic 17 deferral and the Implementation Readiness Boundary
   precisely. **This is the single best piece of scope discipline in the artifact set** — it is what
   stops deferred web UI from contaminating MVP readiness accounting.

5. **Architecture supports every UX requirement checked.** No architectural gap was found for the UX
   model: the Evidence Packet is owned by `Contracts.V1`, the web surface is a defined RCL, and the
   trust grammar has a contract home. There is no UX requirement left unsupported by architecture.

6. **The UX-DR coverage map has zero dangling references.** All 38 distinct story references across
   UX-DR1–UX-DR40 resolve to real story headings in `epics.md`.

### Alignment Issues

**U1 (HIGH) — The 40 UX-DR identifiers exist only in `epics.md`; the UX spec has no IDs at all.**
The UX Design Specification contains **zero** occurrences of `UX-DR`, and it never cites a single
`FR` or `NFR` identifier. The 40 UX-DRs are an epics-side derivation from UX prose.

Consequences:
- UX→epic traceability is **one-directional and unverifiable at the source**. There is no way to
  confirm that UX-DR*n* faithfully represents the spec, or that the spec has not since diverged.
- Nothing detects UX-spec drift invalidating a UX-DR. Contrast this with FR/NFR, which carry IDs in
  *both* the PRD and the epics inventory, so drift is at least detectable by comparison.
- The UX spec is the only one of the four core documents with no requirement-ID scheme whatsoever.

**U2 (MEDIUM) — The UX spec is the stalest core artifact, confirmed by git history.**
Its last *content* change was **2026-06-24** (`feat: Enforce FrontComposer and Fluent UI Blazor V5
usage`). The later touches were structural only — 2026-06-27 (submodule layout refactor) and
2026-07-16 (line-ending policy). Meanwhile the PRD moved 2026-07-19, architecture 2026-07-20, and
epics 2026-07-25. The Step 1 drift hypothesis is **confirmed**: the UX spec has not absorbed a month
of downstream requirement change.

**U3 (MEDIUM) — Retrieval Axis Breakdown is specified three different ways.**
- UX spec (line 855): "Axis rows, **normalized score**, contribution, unavailable/degraded marker,
  explanation text."
- epics UX-DR17 **adds a field the UX spec never states**: "showing **raw score**, normalized score,
  fusion contribution, ranking reason, omitted/degraded axis state, and detail expansion."
- PRD + architecture: hybrid per-axis values are **rank contributions**; architecture line 94 states
  outright that "raw BM25, cosine, and graph-proximity magnitudes are **not averaged** in hybrid
  scoring."

So UX-DR17 directs the UI to render a hybrid "raw score" that the current fusion model deliberately
does not produce as a fused quantity. This is not a flat contradiction — raw scores remain
meaningful for *single-axis* explain — but the UX-DR does not distinguish the two modes, and the PRD
requires that distinction to be visible. Whoever implements Story 17.1 from UX-DR17 alone will build
the wrong explain surface for hybrid results.

**U4 (MEDIUM) — The NL retrieval axis has no UX representation.**
The PRD score-semantics table defines an NL score row (`axis=nl`), architecture defines its
calibration (line 96: NL default weight `0.20`, default-off), and epics Story 22.7 wires `axis=nl`
into hybrid. The UX spec mentions the NL axis **zero times**; its axis vocabulary is three-axis
throughout. Neither the Evidence Packet's "retrieval axes used" field nor the Retrieval Axis
Breakdown component has a defined rendering for NL.

Urgency is genuinely low — NL is default-off and web UI is deferred — but it is an unclosed gap, and
it is exactly the kind of gap that surfaces late because the UX spec looks complete.

**U5 (LOW) — Journey coverage asymmetry between PRD and UX spec.**
The PRD defines 10 journeys; the UX spec defines 5 flows (Alex zero-to-first-packet, Alex weak/empty
recovery, LLM Agent MCP consumption, Kenji tenant verification/degraded recovery, Marcus case
briefing). The omissions are mostly defensible:
- Journey 6 (Kenji scale) is Phase 3 — correctly out of scope.
- Journey 10 (contributor) is project infrastructure, not product UX — correctly out of scope.
- Journey 8 (Priya, end user via REST) is explicitly delegated downstream (UX spec line 610:
  "Priya: downstream Case Briefing patterns surfaced through applications built on top of
  Memories"). Defensible — but it means the PRD's Priya "show sources" verification-link experience,
  which the PRD uses to justify the trust model, has **no owning specification** in any artifact.

### Warnings

- **No architectural gap.** Unlike the FR and fusion findings from Step 3, UX alignment surfaced no
  case where architecture fails to support a UX need. Architecture is the healthiest document in the
  set on this axis.
- **UX is not a readiness blocker for MVP**, because the UX spec itself scopes MVP acceptance to
  contract-visible semantics only. The UX findings above become blocking **only** when Epic 17 web
  UI is pulled into an approved implementation phase — at which point U1, U3, and U4 must all be
  closed first, since Story 17.1 would otherwise be implemented against an unverifiable, partly
  contradictory, and NL-blind requirement set.
- **Recommended sequencing:** refresh the UX spec against the post-2026-06-24 PRD/architecture
  changes (fusion → weighted RRF, NL axis, OpenBao-era secret handling in any diagnostic surface)
  and give it native `UX-DR` identifiers **before** any Epic 17 story is sprint-selected.

---

## Step 5 — Epic Quality Review

Standards applied: `create-epics-and-stories` best practices — user value over technical milestones,
epic independence, no forward dependencies, story sizing, AC quality, and greenfield setup
sequencing. Scope: 30 epic bodies, 159 stories.

### Material Context Discovered in This Step

`sprint-status.yaml` shows **Epics 0 through 26 are all `done`.** Only four stories remain open
across the entire backlog:

| Story | Epic | Status | Note |
| --- | --- | --- | --- |
| 27.3 Production Adapter and Deployment Profile | 27 | **in-progress** | Only actively-worked story |
| 27.4 Retention Verification, Runbook, A41 Close-Out | 27 | backlog | Gated behind 27.3 |
| 28.1 Adopt Owner-Approved EventStore Runtime Identity | 28 | backlog | Blocked on **external** EventStore Story 1.20 |
| 29.2 Provider-Neutral Aspire Composition and Secret Verification | 29 | backlog | Gated behind 29.1 (done) |

This reframes the assessment. This is not a pre-Phase-4 greenfield check — it is a late-stage
project whose remaining question is whether the last four stories are implementable and whether the
document set still coheres well enough to sustain them and any reopened work.

It also sharpens finding **C2**: the only two epics with active or pending internal work — Epic 27
and Epic 28 — are precisely the two epics absent from `readiness_accounting`. That is no longer a
cosmetic indexing gap.

### Epic Structure Validation

#### User Value Focus

**Product-capability epics pass cleanly.** Epics 1–10, 13, 17, 18, and 20–24 and 27 are framed as
user or operator outcomes ("Developer can search memory units across…", "Operator can provision
tenants with…"), not as technical milestones. Epic titles name capabilities, not layers. There is no
"Setup Database" or "Create Models" epic.

**Epic 0 is a foundation epic done correctly.** It could have been a technical-scaffolding epic; it
is instead framed as a safety prerequisite ("Before any ingestion, indexing, search, or graph story
writes data…") and carries real FR coverage (FR26, FR38, FR44) plus NFR8. It delivers a usable
outcome — a provisioned tenant and an active case — rather than inert plumbing.

**Ten epics are technical/operational rather than user-value:** 11, 12, 14, 15, 16, 19, 25, 26, 28,
29. Under a strict reading these are the "technical epics are wrong" violation.

However, this is a **governed and disclosed deviation, not an accident.** `epics.md` lines 543–553
establish an explicit *Engineering/Operational Readiness Track* which states these "are not product
capability epics, but they protect implementation and release quality," and imposes stricter
acceptance rules than the product track: operational stories are accepted only when they produce
"maintainer/operator decisions and concrete evidence such as CI check names, release run results,
package inventory proof, deferred-ID resolution records, runbook updates, or explicit accepted-risk
entries," and documentation-only completion is forbidden for MVP product stories.

That is a legitimate governance answer. **The defect is that the exemption does not reach every epic
that needs it** — see 🟠 Q3 below.

#### Epic Independence

**No epic requires a later epic to function.** Dependencies flow strictly backward:
Epic 1 → Epic 0; Epics 2/3/4 → Epic 1; Epic 5 → Epic 0; Epic 8 → Epic 6 fixture; Epics 20–26 →
Epics 1–10. Gate ordering (Gate 1 three-axis → Gate 2 isolation → Gate 3 DX) matches the
architecture's risk-first sequencing.

#### Forward Dependency Analysis

**Zero forward dependencies found across 159 stories.** This was tested by scanning every story body
for dependency language ("depends on Story", "prerequisite", "blocked by", "requires Story",
"after Story").

Every cross-epic reference found points *backward* or is a deliberate forward-*compatibility*
constraint that does not block completion. Two examples of the pattern, both correct:

- **Story 0.1** — "must use the same `TenantProvisioningWorkflow` ownership model as Story 5.1 and
  must not introduce a separate tenant infrastructure creation path… Story 5.1 deepens this into the
  full tenant lifecycle story." Story 0.1 is completable alone; the reference constrains its design
  so Story 5.1 can extend rather than replace it.
- **Story 0.2** — carries an explicit non-absorption list: it "must not absorb case status, activity
  history, member management, single-case ownership enforcement, case-scoped graph edges, cross-case
  search, deletion, or annotation work — those belong in Epic 3 (Stories 3.1-3.6) and Story 5.4."

This is textbook thin-slice-then-deepen sequencing, and it is executed more cleanly here than in most
epic sets. **This is the strongest structural quality in the artifact set.**

A related strength: explicit **ownership boundaries** prevent scope collisions between concurrent
stories — e.g. Story 2.5's note that it "owns search-layer fusion behavior over available axes"
while "Story 5.6 owns the system-wide backend availability policy, health detection, chaos/
degradation verification, and FR66/NFR18 degraded-service contract."

### Special Implementation Checks

| Check | Result |
| --- | --- |
| Architecture specifies a starter template? | Yes — "Aspire Empty + Incremental Projects (D-selected), `dotnet new aspire`" |
| Epic 1 Story 1 is project setup? | ✓ **Pass** — Story 0.0 "Project Scaffolding & Single-Command Boot" is the first story in the foundation path (historically Story 1.1, renumbered) |
| Greenfield: initial setup story | ✓ Story 0.0 |
| Greenfield: dev environment configuration | ✓ AppHost single-command boot |
| Greenfield: CI/CD early | ✓ Story 0.4 minimum build/test CI preflight is a **hard gate** — no Epic 1.x story may write data before it completes |
| Entities/indexes created when needed, not upfront | ✓ Tenant indexes are created by `TenantProvisioningWorkflow` at provisioning time; three-backend indexing lands in Story 1.5 when first required — not a big-bang schema story |

All special checks pass.

### Story Quality Assessment

**158 of 159 stories use Given/When/Then.** Acceptance criteria in Epics 0–19 are specific,
testable, and consistently cover error paths alongside happy paths — Story 3.5 covers deletion
cascade, in-flight deletion rejection (`CASE_DELETING`), and edge cleanup; Story 3.1 covers the
`SINGLE_CASE_OWNERSHIP` rejection with a recovery suggestion; Story 4.3 covers retroactive gap
resolution on late-arriving events. Error codes are named, not described vaguely.

### Findings by Severity

#### 🔴 Critical

No *new* critical violations were found in this step. The two criticals from Step 3 — **C1** (Epic 2
Stories 2.4/2.5 specify a superseded fusion model that `architecture.md` line 94 explicitly
contradicts) and **C2** (Epics 27/28 absent from readiness accounting while holding all active work)
— are also epic-quality defects and remain open.

#### 🟠 Major

**Q1 — Acceptance-criteria density collapses ~4× in the audit-remediation block.**
Measured Given/When/Then blocks per story:

| Block | Epics | Stories | Avg ACs/story |
| --- | --- | --- | --- |
| MVP + P1.5 + operational | 0–19 | 88 | **~4.6** |
| Audit remediation | **20–25** | **45** | **~1.1** |
| Deploy/telemetry/secrets | 26–29 | 15 | ~2.8 |

Epics 22, 24, and 25 average exactly **1.0 AC per story**. These single ACs bundle multiple
independent deliverables. Examples:
- **Story 21.9** packs six deliverables into one `Then`: staging prefix/index, atomic cutover,
  retained previous index for rollback, `SET NX` marker ownership, TTL/heartbeat, and an `--abort`
  path.
- **Story 26.5** packs six distinct runbooks (capacity planning, incident response, index rebuild,
  tenant onboarding/offboarding, upgrade/migration, monitoring thresholds) into one `Then`.

Combined with source-citation phrasing (`EmbeddingVectorMigrationService.cs:224-321`) and
"Closes A5" tags, these 45 stories read as **audit-finding closure tickets rather than
independently verifiable user stories.** Partial completion cannot be detected: a story is either
"closed" or not, with no criterion-level granularity.

Mitigating: all 45 are already `done`, and the `A`-number gives real traceability to the source
audit. The live risk is confined to reopened work and to anyone using these as templates.

**Q2 — Story 27.3 is checkpoint-heavy, unguarded, and in-progress right now.**
It is the **only** story in the document with no Given/When/Then (it uses five numbered ACs). Format
alone would be cosmetic — its criteria are unusually precise, and AC5 is a well-designed fail-closed
guard that names exact status transitions ("keeps Production writes disabled, Story 27.3
`in-progress`, Story 27.4 `backlog`, and A41 open").

The real problem is **AC2**, which bundles roughly eighteen distinct verification gates into a single
criterion: "CRUD, strong reads, ETags, rollback-atomic multi-key transactions, TTL, actor
reactivation, Placement/Scheduler/reminder recovery, request bounds, two-writer 500 events/s
throughput, 150,000-record purge catch-up, isolation, encryption, capacity, and cohort-attributable
physical reclamation all pass without skip."

`epics.md` line 553 anticipates exactly this shape and requires that such stories "either split
checkpoints into separately tracked child story files or include a checklist evidence table with
owner, validation command or artifact, review status, and completion date for each checkpoint" —
but the guard names only Stories **21.9 and 26.5**, both of which are already done. It does not name
27.3, the one story the guard would actually protect today.

**Q3 — The Engineering/Operational Readiness Track exemption does not cover every technical epic.**
The governed exemption is defined for "Epics 11-16 and Epic 18" (plus Epic 17 web and Epics 20–26
audit remediation). It therefore does **not** reach:
- **Epic 19** (Deferred Register Backlog Home) — governance-about-governance: work whose product is
  backlog bookkeeping. It carries a lifecycle label but no boundary classification.
- **Epic 28** (EventStore Runtime Adoption) and **Epic 29** (OpenBao Secret Management) — pure
  technical epics that appear in no boundary enumeration at all, and (for 28) in no readiness
  accounting either.

These three are the technical epics least covered by the very governance that legitimizes technical
epics in this project.

#### 🟡 Minor

**Q4 — Epic 0 has the lowest AC density of the MVP block** (2.4/story). Appropriate for deliberate
thin slices, but it means Story 0.3 (Tenant and Case Validation Guard) carries the full fail-closed
burden for every downstream data-writing story on comparatively few criteria.

**Q5 — Intentional gaps in numeric story sequence.** Story 8.3 is `reserved-non-mvp`, and Stories
12.7/12.8 are conditional (created only if their reopen trigger fires). This is documented in the
story-key alias table and honored by `story_overrides`, but any tooling that infers completeness
from numeric contiguity will misreport. The document anticipates this explicitly, so the risk is
tooling-side, not specification-side.

**Q6 — Two AC styles now coexist.** Epics 0–19 use narrative BDD; Epics 20–27 use compressed
single-`Given` audit-closure phrasing; Story 27.3 uses numbered ACs. No style is wrong, but a
contributor has no stated rule for which to use on new work.

### Best-Practices Compliance Summary

| Criterion | Result |
| --- | --- |
| Epics deliver user value | ⚠ Partial — 20/30 yes; 10 technical, governed but incompletely (Q3) |
| Epics function independently | ✓ Pass |
| No forward dependencies | ✓ **Pass — zero found across 159 stories** |
| Stories appropriately sized | ⚠ Partial — Epics 20–25 bundle deliverables (Q1); Story 27.3 (Q2) |
| Entities/indexes created when needed | ✓ Pass |
| Clear acceptance criteria | ⚠ Partial — excellent in Epics 0–19, thin in 20–25 |
| Traceability to FRs maintained | ⚠ Partial — 100% substantive, 79.7% ID-anchored (Step 3 H1) |
| Starter template / greenfield sequencing | ✓ Pass |

### Remediation Guidance

1. **Before Story 27.3 closes:** add a checklist evidence table to its story file — owner,
   validation command/artifact, review status, completion date — one row per AC2 gate. This applies
   the existing line-553 guard to the story that currently needs it, and requires no new policy.
2. **Extend the line-553 checkpoint guard** to name Story 27.3 (and any future story whose AC
   enumerates more than ~5 verification gates), rather than listing specific historical stories.
3. **Reconcile Epic 2 with architecture line 94** (C1): update Stories 2.4/2.5 and the epics NFR24
   inventory entry to weighted RRF, or add an explicit superseded-by note pointing at Story 22.4.
   Leaving the MVP thesis epic describing an algorithm the architecture forbids is the highest-value
   fix in this report.
4. **Add `epic-27` and `epic-28` to `readiness_accounting`** (C2), and classify Epics 19, 27, 28, 29
   in the Implementation Readiness Boundary (H2/Q3).
5. **Do not retrofit Epics 20–25 ACs.** They are done and audit-traceable; rewriting them would cost
   more than it returns. Instead, state a standing AC-granularity rule for *new* stories so the
   pattern does not propagate.

---

## Summary and Recommendations

**Assessor:** Product Manager review (implementation-readiness workflow)
**Date:** 2026-07-25
**Artifacts assessed:** `prd.md` (2026-07-19), `architecture.md` (2026-07-20), `epics.md`
(2026-07-25), `ux-design-specification.md` (content frozen 2026-06-24),
`sprint-status.yaml`, `project-context.md`

### Overall Readiness Status

# ⚠️ NEEDS WORK — but narrowly, and not where a readiness check usually finds it

**Nothing is missing.** All 74 FRs are covered, all 31 NFRs are present, all 40 UX-DRs map to real
stories, there are zero forward dependencies across 159 stories, and every greenfield sequencing
check passes. On the dimensions this workflow exists to test — coverage, traceability, sequencing —
this is a strong artifact set. Coverage is not the problem.

**The problem is document coherence at the tail end of a long project.** Epics 0–26 are `done`;
only four stories remain open. Across 30 epics and roughly four months of sprint changes, three
artifacts have drifted out of agreement with each other, and two epics have fallen outside the
governance that is supposed to classify them. None of this blocks the four remaining stories from a
coverage standpoint — but one drift sits on the core product thesis, and one governance gap sits
directly under the only story being worked on today.

The status is **NEEDS WORK** rather than **READY** because of C1 and C2 specifically. It is not
**NOT READY**: there is no missing requirement, no broken dependency, and no unimplementable story.

### Critical Issues Requiring Immediate Action

**C1 — `epics.md` is the last document still specifying a superseded fusion algorithm.**
The PRD (NFR24, updated 2026-07-19) and `architecture.md` (line 94) both mandate **deterministic
weighted reciprocal-rank fusion**, and architecture states outright that "raw BM25, cosine, and
graph-proximity magnitudes are **not averaged** in hybrid scoring." Epic 2 still says the opposite:
- Story 2.4: "all search axis scores normalized to 0.0-1.0 **before fusion**" — citing **(NFR24)**
  for behavior NFR24 no longer describes.
- Story 2.5: "the composite score is a **weighted average of normalized axis scores**."
- The `epics.md` NFR24 inventory entry carries the pre-RRF text verbatim.

RRF appears only twice in 5030 lines, once ambiguously ("RRF **or** per-axis min-max", Story 22.3).
**This is the MVP thesis epic contradicting the architecture on the single most important algorithm
in the product.** Anyone reopening Epic 2 work, or reading Epic 2 as the specification of record,
will build against a model the architecture forbids.

**C2 — The only two epics with live work are the two absent from readiness accounting.**
Epics 28 and 29 have full bodies and stories but do not appear in the **Epic List** (which ends at
Epic 27). More seriously, `sprint-status.yaml` — which `epics.md` line 411 designates the authority
that tooling "**must** use rather than inferring MVP readiness from story status, numeric ordering,
or FR coverage alone" — omits **epic-27 and epic-28** from both `epic_metadata` and
`excluded_unless_sprint_selected`. With `default_story_inherits_epic_metadata: true`, their stories
inherit no track, no phase, and no `mvpReadiness`.

Epic 27 is **in-progress right now** (Story 27.3), and Epic 28 is the externally-gated EventStore
adoption. All remaining internal work is running outside the accounting system that governs it.

**C3 — `epics.md` NFR9 predates two approved policy decisions.**
It reads "Embedding API keys stored in secure secret management — never in config files," while the
PRD requires secrets be retrieved "exclusively through the DAPR Secrets API, backed by OpenBao,"
with Kubernetes Secrets restricted to documented bootstrap material. Epic 29 exists specifically to
implement the newer policy — and Story 29.2 is one of the four open stories. A story written from
the epics inventory would under-specify its own NFR.

### High-Priority Issues

**H1 — 14 FRs (19%) have no acceptance-criterion ID anchor**, including **FR14–FR17 — the three
retrieval axes and hybrid fusion**, plus FR46/FR47 (causal intelligence). Coverage is real; the
machine-checkable link is not. The requirements most central to the MVP go/no-go gate are precisely
the ones ID-based tooling cannot verify. Full list: FR1, FR4, FR5, FR6, FR14, FR15, FR16, FR17,
FR28, FR29, FR39, FR46, FR47, FR68.

**H2 — The Implementation Readiness Boundary classifies 26 of its 30 epics.** Epics 19, 27, 28, and
29 are never classified by the section whose job is classification.

**U1 — The UX spec has no requirement identifiers at all.** All 40 UX-DRs exist only in `epics.md`;
the UX spec never uses `UX-DR`, and never cites a single FR or NFR. UX traceability is
one-directional and unverifiable at the source — the only core document with no ID scheme.

**Q1 — Acceptance-criteria density collapses ~4× in Epics 20–25** (45 stories at ~1.1 ACs/story
versus ~4.6 in Epics 0–19; Epics 22/24/25 at exactly 1.0), with single criteria bundling six or more
deliverables. These read as audit-closure tickets rather than independently verifiable stories.

**Q2 — Story 27.3 is checkpoint-heavy and unguarded, while in-progress.** Its AC2 bundles ~18
verification gates into one criterion. `epics.md` line 553 anticipates exactly this shape but names
only Stories 21.9 and 26.5 — both already done.

### Medium and Lower Findings

| ID | Finding |
| --- | --- |
| M1 | Epics NFR restatements silently drop verification detail (NFR8 graph-collision design, NFR5 batching clause, NFR6 rate-limit degradation, NFR12 methodology, NFR25 tie-order caveat, NFR29 queue depth) |
| M2 | Phase 2 Backlog Placeholders sit between Epic 8 and Epic 9, inside the MVP reading path (governance itself is sound — every placeholder has a Phase Note and Activation rule) |
| M3 | FR26/FR38 are ID-anchored only at their Epic 0 thin slices, not at the Story 3.1/5.1 full capability |
| U2 | UX spec content frozen 2026-06-24 — confirmed by git; later commits were structural only |
| U3 | Retrieval Axis Breakdown specified three ways; UX-DR17 adds a hybrid "raw score" architecture says is not produced |
| U4 | The NL retrieval axis exists in PRD, architecture, and epics — and appears zero times in the UX spec |
| U5 | PRD Journey 8 (Priya) verification UX is delegated downstream and owned by no specification |
| Q3 | The Engineering/Operational Readiness Track exemption does not reach Epics 19, 28, 29 |
| Q4–Q6 | Epic 0 AC density; intentional numeric story gaps (8.3, 12.7, 12.8); three coexisting AC styles with no stated rule for new work |
| P1 | PRD states server runtime "C# 13"; `project-context.md` states C# 14 |

### Recommended Next Steps

**Before Story 27.3 closes (highest value, lowest cost):**

1. **Add a checklist evidence table to the Story 27.3 file** — owner, validation command or
   artifact, review status, completion date — one row per AC2 verification gate. This applies the
   existing line-553 guard to the story that needs it today; it requires no new policy.
2. **Register `epic-27` and `epic-28` in `sprint-status.yaml` `readiness_accounting`**
   (`epic_metadata` + `excluded_unless_sprint_selected`). Without this, the only active work in the
   project is invisible to the tooling that governs readiness.

**Before any Epic 2 or fusion work is reopened:**

3. **Reconcile Epic 2 with architecture line 94.** Either rewrite Stories 2.4/2.5 and the epics
   NFR24 inventory entry to weighted RRF, or add an explicit "superseded by Story 22.4" note in
   both. Leaving the thesis epic describing an algorithm the architecture forbids is the single
   highest-value correction in this report.
4. **Refresh the epics NFR inventory against the PRD** — NFR9 (OpenBao), NFR24 (RRF), and the six
   NFRs in M1 that lost verification detail. Consider replacing the transcribed copy with a pointer
   to the PRD to stop the drift recurring.

**Before Epic 17 web UI is sprint-selected:**

5. **Give the UX spec native `UX-DR` identifiers** and refresh it against post-2026-06-24 changes
   (weighted RRF explain semantics, the NL axis, OpenBao-era diagnostics). U1, U3, and U4 must all
   close first, or Story 17.1 will be built against an unverifiable and partly contradictory
   requirement set.

**Governance hygiene (no deadline pressure):**

6. **Classify Epics 19, 27, 28, 29 in the Implementation Readiness Boundary** and add Epics 28/29 to
   the Epic List.
7. **Add FR-ID tags to the 14 unanchored FRs' acceptance criteria** — prioritize FR14–FR17 and
   FR46/FR47, which carry the MVP thesis and the causal-intelligence differentiator.
8. **State a standing AC-granularity rule for new stories** rather than retrofitting Epics 20–25.
   Those 45 stories are done and audit-traceable; rewriting them costs more than it returns. The
   goal is stopping the pattern from propagating.

### What This Assessment Did Not Find

Stated explicitly, because their absence is itself a finding:

- No missing functional requirement. No orphan requirement invented by the epics.
- No forward dependency, in any story, in any epic.
- No circular epic dependency, and no epic requiring a later epic to function.
- No architectural gap under any UX requirement.
- No unimplementable or unsized story blocking the four remaining items.
- No failure of the greenfield sequencing checks (starter template, setup story, early CI gate,
  create-entities-when-needed).

### Final Note

This assessment identified **21 issues across 5 categories** (3 critical, 5 high, 11 medium, 2 low),
spanning requirement traceability, document currency, governance classification, UX identifier
hygiene, and acceptance-criteria granularity.

The honest headline is that **this artifact set is stronger than most at the things that usually
fail, and weaker at the thing that usually goes unchecked**: keeping four large documents in
agreement with one another after months of approved sprint changes. Coverage discipline here is
genuinely excellent. Cross-document consistency is what has slipped.

Two fixes — the Epic 2 fusion reconciliation (C1) and the Epic 27/28 accounting registration (C2) —
resolve the only findings that touch live work or the core thesis. Both are edits to existing
documents, not new analysis. Everything else can be scheduled.

These findings can be used to improve the artifacts, or you may choose to proceed as-is with the
four remaining stories — noting that Story 27.3, the one story in flight, is the one this report
recommends touching first.
