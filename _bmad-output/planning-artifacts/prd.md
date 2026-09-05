---
title: Hexalith.Memories
status: draft
created: 2026-03-22
updated: 2026-09-05
stepsCompleted: ['step-01-init', 'step-02-discovery', 'step-02b-vision', 'step-02c-executive-summary', 'step-03-success', 'step-04-journeys', 'step-05-domain', 'step-06-innovation', 'step-07-project-type', 'step-08-scoping', 'step-09-functional', 'step-10-nonfunctional', 'step-11-polish', 'step-12-complete']
inputDocuments:
  - '_bmad-output/planning-artifacts/product-brief-Hexalith.Memories-2026-03-22.md'
  - '_bmad-output/planning-artifacts/review-rubric.md'
  - '_bmad-output/planning-artifacts/review-adversarial-general.md'
  - '_bmad-output/planning-artifacts/review-product-brief.md'
  - '_bmad-output/planning-artifacts/review-downstream-drift.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md'
documentCounts:
  briefs: 1
  research: 0
  brainstorming: 0
  projectDocs: 0
classification:
  projectType: 'Developer Tool / API Backend'
  domain: 'AI Infrastructure / Knowledge Management'
  complexity: 'Medium-High'
  projectContext: 'Brownfield / change-controlled'
workflowType: 'prd'
---

# Product Requirements Document - Hexalith.Memories

**Author:** Jerome
**Date:** 2026-03-22
**Updated:** 2026-09-05 — validation Update: dual ship-contract split, unapplied August 2026 change control, EventStore contract split. Mechanism/topology detail lives in `addendum.md`. Active work breakdown lives in `epics.md` and `sprint-status.yaml`, not in the original 22–32 story estimate.

## 0. Document Purpose

This PRD is the product-outcome contract for Hexalith.Memories: thesis, ship gates, capabilities (FR1–FR74), and cross-cutting quality (NFR1–NFR35). It is written for the maintainer, downstream UX/architecture/story owners, and reviewers of change control.

It is a **brownfield / change-controlled** document. Implementation, epics, and architecture have been running since March 2026. This file does not re-estimate the backlog. Where a later approved sprint-change proposal required a PRD amendment, that amendment belongs here; SDK pins, package counts, fusion weights, and host topology belong in architecture or `addendum.md`.

Glossary-anchored nouns are used in FRs, journeys, and success metrics. Assumptions are tagged `[ASSUMPTION]` and indexed. Open tensions are in Open Questions, not smoothed into coexistence.

## Executive Summary

Your LLM agent forgets everything between sessions. Your team's knowledge is scattered across cloud drives, chat logs, and event stores. When someone asks "why did this happen?" — no search tool can answer. The relationships between documents, events, and decisions are invisible.

Hexalith.Memories is an open-source relational memory server that answers "why did this happen?" and "how are these connected?" — questions every team asks and no existing tool can answer. It organizes knowledge in team-scoped case containers, then searches across content, meaning, and connections in a single query. An LLM agent asks: *"What led to the API redesign?"* — and gets back a sourced narrative walking the causal chain from the original incident, through the team discussion, to the architecture decision record. Not just documents — the *story* of how they connect.

The system combines three retrieval axes — syntactic search (BM25), semantic search (vector embeddings), and graph traversal — into a unified hybrid query. This three-axis approach is the core thesis: if hybrid retrieval doesn't outperform BM25+semantic on 80%+ of the benchmark protocol in Measurable Outcomes, execute the named kill-switch actions. Weighted RRF is the fusion decision (NFR24); numeric weights live in architecture.

**Phase 1 (thesis) onboarding:** under 30 minutes from a clean machine with Docker to first CLI search result on file/URL ingest (NFR31). **Phase 1.5 (launch) onboarding:** under 30 minutes from `dotnet add package` plus DAPR subscription to first search on auto-indexed events. Those are two clocks; they are not interchangeable.

For developers already on Hexalith.EventStore, Phase 1.5 integration follows documented conventions: add the package, subscribe to DAPR topics, and events are indexed with causal chains (CausationId/CorrelationId as graph edges) and dual embeddings (payload + natural language description). Schema evolution requires handler registration; that is not "zero configuration." Non-EventStore DAPR publishers are a later adapter path, not the EventStore-equivalent beachhead. `[ASSUMPTION: generic Marten/Wolverine/Axon zero-code remains an experiment until a named Phase 1.5/2 spike passes the DAPR-generic kill switch.]`

Teams organize knowledge in case/folder memory containers where documents, discussions, and events accumulate into shared, searchable knowledge. Every memory unit tracks whether its metadata was set by a human or inferred by AI, with a confidence score. Tenant isolation is an MVP hard gate (NFR8): tenant-scoped backend principals plus tenant-scoped indexes, with zero cross-tenant leaks. The isolation *mechanism* (ACL users, resolvers, index names) is architecture-owned; this PRD owns the outcome.

The system runs on DAPR, starts on Redis (RediSearch + Vector Search + FalkorDB), with architecture designed to support backend portability (concrete implementation first, extraction points identified for future migration). Topology, Aspire, OpenBao, and package inventory are recorded in `addendum.md` and architecture — they are not additional product surfaces.

CLI is the operational superset. MCP is the agent subset (search, ingest, traverse, case-info) and ships as Phase 1.5. Surfaces are capability-aligned, not 100% feature-parity. `[NON-GOAL for MVP]: MCP, EventStore CloudEvent auto-index, application-facing REST search UI, briefings, discussions, and memory diffing.`

### What Makes This Special

**Phase 1 proves hybrid retrieval plus non-retrofittable isolation.** The sequenced bet — queryable causality from EventStore conventions — is Phase 1.5, not the thesis gate. Event-sourced systems already capture *why* things happen; Memories makes CausationId/CorrelationId queryable once the EventStore product integration ships: *"What happened because of this deployment?"* walks the graph. Happy path is subscription plus conventions, not zero mapping and zero configuration.

EventStore CloudEvent auto-index is the first *product* proof point for causal intelligence. Separately, Hexalith.EventStore is already the **domain source of truth** for Case / MemoryUnit / Tenant writes (current MVP consistency contract). Package/runtime pins for EventStore bits are architecture-owned (Epic 28). Those three "EventStore" meanings must not be collapsed.

Two additional differentiators compound later:

- **Team-scoped collaborative memory** — case/folder containers in MVP; threaded discussions, memory diffing, and onboarding briefings are Phase 2. Do not market them as unique until they ship.
- **Integrated system over duct tape** — three-axis retrieval, case-scoped graph, tenant isolation, confidence tracking, and async ingestion work together. Hybrid-search and GraphRAG incumbents (Elasticsearch/OpenSearch hybrid, Azure AI Search, Weaviate, Vespa, Microsoft GraphRAG, LlamaIndex PropertyGraphIndex, Neo4j LLM graphs) are the comparison set, not only Mem0/Zep/LangChain.

## Project Classification

- **Project Type:** Developer Tool / API Backend (NuGet packages + DAPR service + CLI + MCP server)
- **Domain:** AI Infrastructure / Knowledge Management
- **Complexity:** Medium-High — driven by three-axis query fusion, DAPR workflow ingestion, multi-tenancy with tenant-scoped isolation, and EventStore domain + product integration
- **Project Context:** Brownfield / change-controlled (greenfield thesis recorded March 2026; implementation and epics are the living work breakdown)
- **License:** Apache 2.0 (decision, not a recommendation). Public README commitment: the project will not switch to a restrictive license.

## Glossary

Downstream workflows and readers must use these terms exactly.

- **Memory unit** — The stored unit of knowledge (document chunk, event, annotation) owned by exactly one **Case**.
- **Case** — Team-scoped container of memory units and case-scoped graph edges. Not an authorization principal.
- **Tenant** — Isolation boundary. Access is authorized by tenant claims on an authenticated principal.
- **Axis** — A retrieval method: syntactic (BM25), semantic (embedding), graph (traversal/proximity). `nl` is an optional extra semantic score on the natural-language-description embedding, not a fourth marketing axis.
- **Hybrid / three-axis** — Fusion of *available* axes for a query via weighted reciprocal-rank fusion (RRF). Missing axes degrade; they do not fail the query (FR66).
- **Evidence Packet** — Cross-surface trust envelope (confidence breakdown, origin, omitted-detail handling, degradation). Concrete shape is architecture/`Contracts.V1`-owned.
- **Relevance confidence** — Composite/RRF score of query-result relevance. Not factual accuracy.
- **Metadata confidence** — Per-field origin (`human-declared` vs `ai-inferred`) score on a memory unit.
- **Edge confidence** — Default or promoted strength of a typed graph edge (`caused_by`, `correlated_with`, `references`, `contains`, `annotates`).
- **Access telemetry** — Per-tenant search/access logs (FR67, NFR34). Not a tamper-evident audit trail.
- **Member** — Tenant-scoped case membership metadata (FR28–FR29). Does not grant authorization in the current phase.
- **EventStore (domain truth)** — Hexalith.EventStore as durable commit for Case / MemoryUnit / Tenant aggregates. Current MVP consistency contract.
- **EventStore (product integration)** — CloudEvent auto-index, dual embedding, CausationId/CorrelationId edges without mapping code (FR59–FR62). Phase 1.5.
- **EventStore (runtime pin)** — Which EventStore packages/SHA the repo consumes. Architecture/Epic 28; not a product FR.

## Non-Goals (Explicit)

- `[NON-GOAL for MVP]` MCP server and token-budget agent tools (FR23, FR54, FR58) — Phase 1.5 launch gate.
- `[NON-GOAL for MVP]` EventStore CloudEvent auto-index / handler diagnostics (FR59–FR62) — Phase 1.5 launch gate. Domain-truth EventStore writes remain in MVP.
- `[NON-GOAL for MVP]` Application-facing REST search UI (Priya / Journey 8) — Phase 2. Internal/CLI HTTP transport may exist in MVP.
- `[NON-GOAL for MVP]` Discussions, memory diffing, onboarding briefing, extraction-phrase templates — Phase 2.
- `[NON-GOAL for MVP]` Per-unit ACLs, geo pinning, encryption-at-rest-per-tenant as a compliance product, tamper-evident audit trail.
- `[NON-GOAL for MVP]` Cloud-drive, git, image, and video ingest. `[ASSUMPTION: those destinations stay deferred until an owner names a phase; they are not silently in the roadmap.]`
- `[NON-GOAL for MVP]` Personal-only / per-user memory SKU. This is shared case memory.
- Resource pressure may not drop tenant isolation, case bootstrap, or NFR8 without an approved MVP rebaseline.

## Success Criteria

### User Success

| Persona | Success Criterion | Measurement | Target |
|---|---|---|---|
| **Alex (Developer)** | Onboards without hand-holding (thesis) | Time from README/AppHost quickstart to first CLI search on file/URL ingest | <30 minutes — Phase 1 hard gate (NFR31) |
| **Alex (Developer)** | EventStore happy path (launch) | Time from `dotnet add package` + DAPR subscription to first search on auto-indexed events | <30 minutes — Phase 1.5 hard gate |
| **Alex (Developer)** | Ships AI features using Memories | Projects integrating Hexalith.Memories client | Tracked via NuGet dependency graph |
| **Alex (Developer)** | Trusts the system enough to ship | Deploys an application using Memories to production | Within 60 days of first use |
| **LLM Agent** | Gets better answers than single-axis retrieval | Retrieval relevance score (NDCG@10) on benchmark queries | Three-axis outperforms single-axis on 80%+ of benchmarks |
| **LLM Agent** | Respects token budget | Response size stays within caller-specified limits | 100% compliance on budget-constrained queries |
| **LLM Agent** | Low latency | Search-to-response time at 10 concurrent queries/tenant | NFR1–NFR3 (no separate cached/cold budget; NFR7 is process boot, not cache warmth). **Phase:** 1.5 for agent surface |
| **Marcus (Team Lead)** | Instant case context | New member asks "brief me on this case" and gets accurate, sourced answer | Phase 2 — narrative only until briefing ships; not a Phase 1 success metric |
| **Marcus (Team Lead)** | Knowledge is visible | Cases with active memory (>10 units, accessed within 30 days) | Growing month-over-month |
| **Kenji (Operator)** | Friction-free operations | Tenant provisioning time | Single CLI command, <5 min |
| **Kenji (Operator)** | No surprises | Cross-tenant data leaks | Zero — verified by automated security suite |

### Business Success

| Metric | 3-Month (Aspirational) | 3-Month (Concern Threshold) | 12-Month (Aspirational) | 12-Month (Concern Threshold) |
|---|---|---|---|---|
| GitHub stars | 100+ | <30 | 1,000+ | <200 |
| NuGet downloads | 500+ | <100 | 5,000+ | <500 |
| External contributors | 3+ | 0 | 10+ | <3 |
| Community engagement | >20 issues, >10 discussions | <5 issues | Self-sustaining: external PRs, community-answered questions | No external PRs |
| EventStore integration users | 5+ projects | 0 | 50+ projects | <5 |
| MCP directory listing | Listed in at least 1 directory | Not listed | Referenced in LLM agent tutorials | Still unlisted — **clock starts at Phase 1.5 MCP ship, not thesis day** |

**Concern thresholds** trigger a retrospective on positioning, documentation, or developer experience — not necessarily a pivot, but a mandatory "why" investigation.

**Sustainability signals (12-month "this is working" test):**
- **Community contributions:** External PRs beyond typo fixes — feature PRs, new embedding providers, backend implementations
- **Company adoption:** At least 2 organizations that have engaged with the project (issues, PRs, discussions) AND confirmed production usage

Both signals must be present. Community without production usage means it's interesting but not trusted. Production usage without community means it's useful but fragile.

### Technical Success

Detailed performance targets, verification methods, and phase tags are defined in the **Non-Functional Requirements** section (NFR1–NFR35). Key hard gates: search latency NFR1–NFR3, zero cross-tenant leaks (NFR8), zero data loss on restart (NFR16).

### Measurable Outcomes

**The Three-Axis Kill Switch (thesis gate):**
Hybrid retrieval must beat the named controls on a frozen corpus. 80% is the hard line, not a stretch goal.

**Scoring protocol:**
- **Population:** A representative mix of developer/agent tasks on the Phase 1 corpus (files/URLs/cases). Do **not** filter the suite to "queries that require all three axes." Thesis-stress queries may exist as a diagnostic slice, not the only slice. `[ASSUMPTION: N remains 5–10 topics until Epic 26 (or successor) expands N; the protocol is honest about statistical weakness at that N.]`
- **Controls:** Primary comparison is hybrid vs BM25+semantic (the realistic alternative). Single-axis runs are diagnostics.
- **Ground truth:** Graded labels collected *after* queries exist. Jerome + 2 independent reviewers. Inter-rater agreement ≥80% (name the statistic in the benchmark README).
- **Automated scoring:** NDCG@10. "Measurably better" requires a pre-registered minimum ΔNDCG@10 in the benchmark README (architecture may own the number; the PRD requires that it exist).
- **Dispute resolution:** Human review where automated score and reviewer judgment diverge.
- **Fallback:** If independent reviewers are unavailable, automated scoring may still run but **does not** satisfy the thesis hard gate — it is a documented downgrade of gate confidence, not a substitute.

**If the threshold is not met, the kill switch names sunk-cost actions:** stop fusion R&D as default hybrid; remove graph from default `axes=hybrid`; freeze FalkorDB for causal traversal or cut it from general search; change README positioning *before* Phase 1.5 MCP/EventStore product expansion; re-scope and re-estimate. "Reposition and keep building the same system" is not a pass.

**Causal Chain Completeness:**
For 95%+ of EventStore events with known CausationId/CorrelationId chains, graph traversal returns the complete causal path. Validated by automated tests against known event chains. **Phase 1.5 launch gate**, not a Phase 1 thesis gate.

**Phase 1 thesis go/no-go (CLI proof of hybrid + isolation):**

| Gate Type | Criterion | Requirement |
|---|---|---|
| **Hard gate** | Hybrid vs BM25+semantic passes the 80% NDCG@10 protocol above | Must pass |
| **Hard gate** | Zero cross-tenant data leaks (NFR8) | Must pass |
| **Hard gate** | Phase 1 onboarding <30 minutes (NFR31: README/AppHost → first CLI search) | Must pass |
| Soft gate | Case ownership/isolation tests (FR32/FR33/NFR8): a memory unit is searchable only in its case and tenant | Must pass — this replaces the adjective "case model correctly scopes memory" |
| Soft gate | Fusion explain is deterministic (NFR24–NFR26) | Must pass |

All 3 hard gates must pass. Both soft gates must pass. Phase 1 does **not** require MCP, EventStore CloudEvent auto-index, or causal-chain completeness.

**Phase 1.5 launch go/no-go (agent + EventStore product integration):**

| Gate Type | Criterion | Requirement |
|---|---|---|
| **Hard gate** | MCP end-to-end: agent task on held-out queries within token budget (FR23/FR54/FR58) | Must pass |
| **Hard gate** | EventStore product integration: `dotnet add package` + subscription → first event search <30 minutes | Must pass |
| **Hard gate** | Causal chain completeness ≥95% on known CausationId/CorrelationId chains | Must pass |

Phase 1.5 slip **delays those surfaces**. It does not pull MCP into the thesis MVP and does not re-open isolation or fusion as optional.

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Proof of Thesis — validate three-axis retrieval before building integration surfaces. Ship the smallest thing that proves hybrid retrieval outperforms single-axis, with cases and multi-tenancy from day one (architectural decisions that can't be retrofitted).

**Resource Requirements:** Brownfield / change-controlled. Story counts and remaining work live in `epics.md` and `sprint-status.yaml`. The March 2026 "solo developer, 22–32 stories" figure is historical context, not the active work-breakdown.

**Implementation Sequencing:** Establish the complete foundation path before any ingestion, indexing, search, or graph story writes data: buildable scaffold/AppHost/ServiceDefaults first, minimum build/test feedback second, then tenant provisioning, minimal case bootstrap, and tenant/case validation guards. `TenantProvisioningWorkflow` owns tenant infrastructure creation (tenant-scoped backend principals + tenant-scoped indexes), minimal case bootstrap happens inside an active tenant, and ingestion/indexing fail before backend writes if tenant or case context is missing or mismatched. After that foundation exists, each search axis is independently available; hybrid fusion is **weighted reciprocal-rank fusion** (NFR24). Numeric RRF `k` and default axis weights are architecture-owned. The March 2026 magnitude-blend spike (BM25 normalization + cosine + graph proximity weighting) is a rejected alternative.

### MVP Feature Set (Phase 1 — "Proof of Thesis")

**Core User Journeys Supported:**
- Journey 9 (Alex — The First Case) — **Phase 1 thesis success path:** empty state, file/URL ingest, first hybrid search
- Journey 5 (Kenji — New Tenant) — Phase 1: provisioning and isolation verification
- Journey 1 (Alex — Zero to First Search) — **Phase 1.5 launch path:** EventStore conventions, not thesis MVP
- Journey 2 (Alex — Debug) — Phase 1.5 (handlers/replay) with Phase 1 `--explain` already in thesis CLI
- Journey 3 (Alex — MCP) — Phase 1.5

**Must-Have Capabilities:**

| # | Feature | Validates |
|---|---|---|
| 1 | Memory Engine (Redis: RediSearch + Vector + FalkorDB) | Available-axis foundation |
| 2 | Content Ingestion (file/URL, metadata, confidence tracking, DAPR Workflow) | Pipeline, dual-origin metadata, EventStore domain commit |
| 3 | Hybrid Search (syntactic, semantic, graph — independently available, then RRF) | Core hypothesis |
| 4 | Case/Folder Model (create/delete, strict ownership, case-scoped graph) | Collaborative memory structure |
| 5 | Tenant Isolation (tenant-scoped principals + indexes, NFR8) | Zero-leak hard gate |
| 6 | CLI — thesis essentials: `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, `status` (FR10–FR11) | Thesis validation tooling |
| 7 | Benchmark Suite (representative mix, NDCG@10 vs BM25+semantic) | Thesis validation |

Phase 1 graph inventory `[ASSUMPTION]`: file ingest creates `contains` (case membership) and optional explicit `references`. Typed causal edges `caused_by` / `correlated_with` populate from EventStore product integration (Phase 1.5) or explicit annotation. Hybrid fuses *available* axes; do not claim causal three-axis on a folder tree.

**Note:** DAPR infrastructure is scaffolding built as part of features 1–5, not a separate work item. README ships with MVP as the NFR31 vehicle. A help entry backed by `NotImplementedCommand` is not coverage.

### Phase 1.5 — Fast-Follow (committed: within 4 weeks of thesis validation)

| # | Feature | Validates |
|---|---|---|
| 1 | EventStore product integration (DAPR pub/sub through the Memories Server sidecar, auto-discovery, dual embedding, causal chains) | Phase 1.5 launch onboarding + causal completeness |
| 2 | MCP Server (search, ingest, traverse, case-info with token-budget awareness) | LLM agent integration |
| 3 | CLI expansion: `explore`, `handlers`, `quickstart`, remaining FR53 slices | Full developer experience |

The Memories Server is the sidecar-managed event subscriber. Hexalith modules publish CloudEvents to the configured DAPR pub/sub topic; the server sidecar delivers them to `/events/ingest`, where source-prefix routing maps events to tenant/case memory. Modules should not bypass this path with direct REST pushes for domain event streams.

**Hard commitment:** Phase 1.5 remains the launch path for MCP and EventStore *product* integration. If the timeline slips, those surfaces delay — they are **not** pulled into the thesis MVP and isolation/fusion are **not** reopened.

### Phase 2 (Growth)

- Discussion threading within cases
- Memory diffing ("what changed since X?")
- REST API for application search UIs
- Extraction phrase templates
- Onboarding briefing ("brief me on this case")
- Embedding versioning and model migration (<5% relevance degradation, zero downtime)

### Phase 3 (Vision)

- Hot/cold memory tiers (Redis → blob storage)
- Content-addressed deduplication
- Entity resolution, access pattern learning, knowledge decay detection
- IMemoryIndex Qdrant implementation (validated migration path)
- Memory Explorer UI, Timeline View
- Per-unit ACLs, LLM context redaction, geographic pinning
- Encryption at rest per tenant, compliance evidence, audit trails

**Full vision (2–3 years):** Hexalith.Memories becomes the standard knowledge layer for event-sourced applications.

### Risk Mitigation Strategy

**Technical Risks:**
- Fusion quality is the primary R&D risk. Mitigation: axes independently available, then RRF (NFR24). If graph adds no value to general search, execute the kill-switch actions in Measurable Outcomes — do not keep default hybrid unchanged.
- Magnitude-blend / three-normalization fusion is a rejected alternative; do not re-open it as a spike.

**Market Risks:**
- Thesis-only increment is not the marketed EventStore/MCP product. Mitigation: keep exec summary, README, and samples honest about phase; do not accordion MCP into thesis MVP if Phase 1.5 slips.
- Independent reviewer availability for benchmark scoring. Mitigation: automated scoring may run but does not satisfy the thesis hard gate without independent labels.

**Resource Risks:**
- Resource pressure may defer phase-qualified interfaces and diagnostics, but it may **not** defer tenant isolation, tenant/case validation, or the zero-leakage release gate without an approved MVP rebaseline. Engine, scoped search, minimum case bootstrap, tenant provisioning, and their fail-closed guards remain inseparable MVP foundations.

**Operational Risks:**
- Shared embedding API key exhaustion — one tenant's batch ingestion starves others' real-time ingestion. Mitigation: per-tenant embedding throttle (rate-limiter actor) plus inbound request quotas (authenticated tenant). For full provider isolation, tenants use separate API keys. Document shared-key limitation in operator guide.

## User Journeys

### Journey 1: Alex — "Zero to First Search" (Phase 1.5 launch path)

> **Phase:** 1.5 EventStore product integration. Not the Phase 1 thesis success path — that is Journey 9.

Alex has been building a claims processing platform on Hexalith.EventStore for eight months. The system processes 50,000 events per day across six aggregate types. Last sprint, the product owner asked: "Can the AI assistant explain why a claim was denied?" Alex spent three days duct-taping Qdrant and LangChain together. It worked for the demo. Then the security review asked about tenant isolation, and the whole thing collapsed.

**Opening Scene:** Alex finds Hexalith.Memories linked from the EventStore documentation. The README shows a 30-second demo: three commands, events appear searchable. Alex thinks "that can't be right" and opens the getting started guide.

**Rising Action:** `dotnet add package Hexalith.Memories.Client` — familiar. DAPR subscription config — two lines in `appsettings.json`. `docker compose up` for Redis + FalkorDB — the stack boots in under a minute. Alex publishes a test event to the DAPR topic.

**Climax:** `memories search "claim denied"`. Results come back. Actual results. With the CausationId chain showing which command triggered the denial. It's been 14 minutes. Alex stares at the terminal. The three-day duct-tape project just became a 14-minute setup. *That can't be right* — but it is.

**Trust Deepening:** Alex runs `memories search "claim denied" --explain`. The output breaks down the result: syntactic match on "denied" (BM25 score 0.82), semantic match on "claim rejection" (cosine 0.91), and a graph edge from the DenyClaim command through to the original SubmitClaim event. Three axes, one query. Alex understands *why* each result appeared, not just *that* it appeared.

**Resolution:** By end of day, Alex commits the integration code. The duct-tape Qdrant solution gets deleted. Alex goes home on time.

**Capabilities revealed:** EventStore auto-integration, CLI search with --explain, causal chain traversal, <30 min onboarding, debug-first DX.

---

### Journey 2: Alex — "Something's Wrong" (Debug Path)

Two weeks after shipping, Alex gets a Slack message: "The AI assistant says there's no information about the Henderson claim, but I filed it yesterday."

**Opening Scene:** Alex opens a terminal. `memories search "Henderson" --case claims-q1` returns zero results. Not good.

**Rising Action:** `memories status --case claims-q1` shows 12,847 memory units, last ingested 3 hours ago. The Henderson claim was filed 18 hours ago — it should be there. `memories search "Henderson" --case claims-q1 --explain` shows: "No syntactic or semantic matches. No graph nodes matching 'Henderson'." The event was never ingested.

Alex checks the DAPR subscription logs. The ClaimSubmitted event for Henderson was published but the Memories handler threw a serialization error — a new field added last sprint broke the auto-discovery mapping. The error message in the CLI is clear: `Event type 'ClaimSubmittedV2' not found in registered handlers. Run 'memories handlers --list' to see registered types.`

**Climax:** Alex registers the V2 handler, triggers a replay of the missed events, and within seconds `memories search "Henderson"` returns the full claim with causal chain intact.

**Resolution:** Alex adds a monitoring alert on handler registration mismatches. The debug-first DX — clear error messages, `--explain`, `status`, `handlers --list` — turned a potential hours-long investigation into a 15-minute fix.

**Capabilities revealed:** CLI diagnostics (status, explain, handlers list), clear error messages, event replay, handler registration, debug-first developer experience.

> **Scope note:** `memories handlers --list` and event replay are Phase 1.5 EventStore product-integration capabilities (FR59–FR62, Epic 9), not MVP Feature #3 (hybrid search).

---

### Journey 3: Alex — "Wiring Up the AI Assistant" (MCP Integration, Phase 1.5)

Alex has the memory server running and CLI working. Now the product owner wants the team's AI assistant to use it.

**Opening Scene:** Alex opens the MCP tool documentation. Four tools: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`. Each has typed parameters with descriptions.

**Rising Action:** Alex adds the MCP tool definitions to the AI assistant configuration. First test: "What happened with claim 4821?" The assistant calls `search_memory(query="claim 4821", case="claims-q1", axes="hybrid")` and returns results with source attribution. It works — but the response is too long, blowing past the context window.

**Climax:** Alex adds `token_budget=2000` to the tool configuration. The assistant calls again — same query, but the response is now concise: top-ranked results, truncated by relevance, with a note "8 additional results omitted." The assistant composes a focused answer. Alex tests three more queries, each producing grounded, sourced responses.

**Resolution:** The product owner asks "why was claim 4821 denied?" and the assistant walks the causal chain: SubmitClaim → FraudCheckTriggered → FraudScoreExceeded → ClaimDenied. Sourced, attributed, traceable. The AI feature ships to the team by end of week.

**Capabilities revealed:** MCP tool definitions, token-budget-aware responses, multi-axis search control, source attribution, assistant configuration workflow.

---

### Journey 4: Marcus — "Brief the New Person" (Phase 2)

> **Phase:** 2 (briefing, annotations-as-onboarding). Case membership metadata (FR28–FR29) may exist earlier; it does not grant authorization and does not make this journey MVP.

Marcus leads a team of seven working across three active cases. Sarah, a senior developer, left last month. Her replacement, Tomás, starts Monday. Marcus has spent every previous onboarding doing four hours of tribal knowledge transfer, walking through Confluence pages that are six months out of date.

**Opening Scene:** Friday afternoon, Marcus creates Tomás's access to the three cases: `memories case add-member --case project-alpha --user tomas`. He does the same for project-beta and the incident-response case.

**Rising Action:** Monday morning, Tomás opens the AI assistant and types: "Brief me on project-alpha." The assistant calls `search_memory(query="project overview and recent activity", case="project-alpha")` and composes a narrative: the project started eight months ago as a payment processing rewrite, hit a critical incident in February when the gateway provider changed their API, pivoted to a dual-provider architecture, and is currently in testing. Key decisions, who made them, and why — all sourced from events, documents, and team discussions in the case memory.

**Climax:** Tomás asks: "What led to the dual-provider decision?" The assistant walks the causal chain: GatewayTimeoutEvent → IncidentDeclared → ArchitectureReviewDiscussion → DualProviderProposal → ApprovedByMarcus. Tomás understands not just *what* the architecture is, but *why* it exists. In 20 minutes, not four hours.

**Recovery beat:** Tomás notices the briefing says "approved by Marcus on February 12" but the PR was actually merged on February 14. He clicks "show sources" — the approval event is dated February 12, but the implementation PR landed two days later. The briefing was accurate about the *decision*, just not the *implementation*. Tomás flags the discrepancy, and the memory unit gets an annotation clarifying the timeline. The system is self-correcting.

**Resolution:** Marcus checks in after lunch. Tomás is already reviewing PRs with full context. Marcus didn't spend a single minute on knowledge transfer. He realizes he has his Friday afternoon back for the first time in a year.

**Capabilities revealed:** Case member management, case briefing via MCP, causal chain narrative, source attribution with verification, memory correction annotations, knowledge health visibility.

---

### Journey 5: Kenji — "New Tenant, No Drama" (MVP)

Kenji manages the DAPR infrastructure for three business units. The compliance team just approved a fourth business unit for the AI platform, and they need their own isolated memory space by Thursday.

**Opening Scene:** Kenji opens the CLI: `memories tenant create --id bu-compliance --display-name "Compliance Unit"`. The command provisions tenant-scoped Redis/FalkorDB resources (indexes plus backend principals). Isolation *outcome* is NFR8; the security boundary is tenant-scoped principals, not index names alone. It takes 8 seconds.

**Rising Action:** Kenji runs the tenant isolation verification: `memories tenant verify --id bu-compliance`. Automated checks confirm: search from bu-compliance context returns zero results from other tenants, ingestion into bu-compliance is not visible from other tenant contexts. All green.

**Failure beat:** Next month, a new intern accidentally runs `memories search "test" --tenant bu-operations` from the bu-compliance service context. The CLI returns a tenant-mismatch error naming the authenticated tenant. The isolation holds. Kenji sees the rejected request in access telemetry (not a tamper-evident audit trail) with who, when, and what was attempted.

**Resolution:** Kenji's Thursday deadline was met in under 10 minutes. The monitoring dashboard shows all four tenants healthy, isolated, with clear resource consumption per tenant.

**Capabilities revealed:** CLI tenant provisioning, isolation verification (NFR8), boundary violation errors, access telemetry.

---

### Journey 6: Kenji — "Time to Scale" (Growth / Phase 3)

Six months after the compliance tenant launch, bu-compliance has grown to 2 million memory units. Redis memory is climbing past the comfort zone.

**Opening Scene:** Kenji runs `memories backend assess --tenant bu-compliance` and sees the recommendation: "Consider Qdrant migration for tenants exceeding 1M units. Current memory usage: 12.4GB, projected 30-day growth: 2.1GB."

**Rising Action:** `memories backend migrate --tenant bu-compliance --target qdrant --dry-run` shows the migration plan: estimated time, data volume, steps, and rollback procedure. No surprises.

**Climax:** Kenji executes the migration during a maintenance window. Migration completes with zero downtime — queries are served from Redis until the Qdrant index is ready, then traffic switches. The compliance team's AI assistant doesn't notice anything changed.

**Resolution:** Kenji has a clear, repeatable scaling playbook. The next tenant that hits the threshold gets the same treatment.

**Capabilities revealed:** Backend assessment tooling, migration dry-run, Redis → Qdrant migration path, zero-downtime backend swap.

> **Scope note:** This journey maps to Phase 3 (IMemoryIndex Qdrant implementation). Not an MVP deliverable.

---

### Journey 7: LLM Agent — Technical Integration Path

This journey maps the system interaction pattern rather than a human narrative.

**Integration Setup:**
1. Application registers MCP tool definitions: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`
2. Agent receives tool schemas with typed parameters including `case`, `query`, `token_budget`, `axes` (syntactic/semantic/graph/hybrid)

**Query Cycle:**
1. User prompt arrives requiring organizational context
2. Agent calls `search_memory(query="what led to the API redesign?", case="project-alpha", token_budget=2000, axes="hybrid")`
3. Memory server executes three-axis search, fuses results, truncates to token budget
4. Response includes: ranked memory units, source attribution (document/event/discussion), confidence scores, causal chain links
5. Agent composes response grounded in sourced memory, citing specific documents and events

**Edge Cases and Graceful Degradation:**
- **Token budget exceeded:** Server truncates results by relevance rank, includes "X additional results omitted" count, names omitted detail groups, and provides deterministic expansion handles
- **No results:** Response includes suggested alternative queries and case status
- **Ambiguous case:** Server returns case disambiguation options
- **Stale context:** Confidence scores flag memory units not updated in >90 days
- **Memory server unreachable:** Agent receives timeout error with retry-after header. Agent should fall back to informing the user that organizational memory is temporarily unavailable rather than hallucinating context
- **Redis degraded (partial results):** Response includes `"degraded": true` flag and which axes were unavailable, so the agent can caveat its answer: "Based on text and semantic search only — graph traversal temporarily unavailable"

**Success criteria:** Agent produces responses that are sourced, attributed, within token budget, and causally grounded when graph data is available. On degradation, agent transparently communicates limitations rather than silently producing lower-quality answers.

**Capabilities revealed:** MCP tool definitions, token-budget-aware responses, multi-axis search control, source attribution, confidence scoring, graceful degradation signaling.

---

### Journey 8: Priya — "I Need to Understand This Case"

Priya is a claims adjuster at an insurance company. She handles 40 cases per week. She's never heard of Hexalith.Memories — she uses a web application that Alex's team built on top of it.

**Opening Scene:** Priya stares at claim #7293 in her queue and feels her stomach tighten. Complex escalation, three contractor assessments, a coverage dispute, and the previous adjuster left the company with no handover notes. She has a call with the claimant in 10 minutes.

**Rising Action:** She types into the application's search bar: "What happened with claim 7293?" The response is a chronological narrative: initial claim filed January 12, first assessment January 18 (contractor found $12K damage), coverage dispute raised January 25 (policy exclusion for pre-existing conditions), second assessment February 3 (independent contractor confirmed $9.5K new damage), escalation to senior adjuster February 10, senior adjuster approved partial coverage February 15.

Priya asks: "Why was partial coverage approved instead of full?" The causal chain returns: the independent assessment distinguished $9.5K new damage from $2.5K pre-existing damage, the policy exclusion applied only to pre-existing, and the senior adjuster's approval note referenced the independent assessment as the deciding factor.

**Verification beat:** Before the call, Priya clicks "show sources." Each claim in the narrative links to the actual document or event: the contractor's assessment PDF, the policy exclusion clause, the senior adjuster's approval with timestamp and signature. Confidence scores show 0.95 for the causal chain. Priya reads the approval note herself — it matches the narrative. She's not trusting the AI blindly; she's trusting it because she can verify every link.

**Climax:** Priya calls the claimant with complete context. She can explain exactly what was covered, why, and cite the specific evidence. The claimant asks a follow-up about the contractor selection — Priya checks the sources in real time and has the answer in seconds.

**Resolution:** The call takes 8 minutes instead of 30. Priya moves to the next case. Her stomach unknots.

**Capabilities revealed:** REST API consumption (via Alex's app), chronological narrative composition, causal chain explanation, source attribution with verification links, confidence scoring for end users, cross-document relationship traversal.

---

### Journey 9: Alex — "The First Case" (Empty State)

Alex has the stack running. Redis is up, FalkorDB is up, the DAPR service is healthy. But there's nothing in it yet.

**Opening Scene:** `memories search "anything"` returns: `No results. This tenant has no memory units yet. Get started: 'memories ingest <file>' to add your first document, or configure a DAPR subscription to auto-index events. Follow the README quickstart for a guided setup. If the Phase 1.5 quickstart command is installed, run 'memories quickstart'.`

**Rising Action:** Alex creates the first case: `memories case create --id claims-pilot --display-name "Claims Pilot"`. The CLI responds: `Case 'claims-pilot' created. 0 memory units. Start building knowledge: ingest documents, subscribe to event topics, or add files from a directory.` Not an error, not a blank screen — a clear next step.

Alex runs `memories ingest ./sample-claims/ --case claims-pilot`. The CLI shows a progress indicator: 47 documents ingested, 47 memory units created, embedding in progress. Then: `Done. 47 memory units indexed. Try: 'memories search "claim" --case claims-pilot'`

**Climax:** First search on real data: `memories search "water damage" --case claims-pilot`. Three results, ranked by hybrid (RRF) score. The system works. The empty state is gone, and Alex never felt lost getting here. Event publish to a DAPR topic is Journey 1 / Phase 1.5, not this climax.

**Resolution:** Alex shares the README quickstart experience in the team channel. Two other developers set up their own cases by end of day.

**Capabilities revealed:** Helpful empty state messages, README quickstart, optional interactive quickstart polish, batch ingestion from directory, clear progress feedback, case creation, first-search experience.

---

### Journey 10: The Contributor — "From Bug Report to First PR"

Dani is a .NET developer at a fintech startup. They adopted Hexalith.Memories three months ago for their transaction monitoring system. It's been working well, but Dani hit a rough edge: the CLI's `memories search --explain` output doesn't show which embedding model was used for the semantic match, making it hard to debug relevance issues when testing different providers.

**Opening Scene:** Dani opens a GitHub issue: "Feature request: show embedding model name in --explain output." They include a concrete use case and a mock of what the output should look like.

**Rising Action:** Jerome responds within a day, labels it `good-first-issue`, and points to the relevant code path: `Hexalith.Memories.Cli/Commands/SearchCommand.cs` and the `ExplainResult` model. Dani clones the repo, runs `dotnet build` — it builds on first try. They run the existing tests — all green. The project structure matches what the README describes. Dani finds the `ExplainResult` class, adds the `EmbeddingModel` property, updates the CLI formatter, and writes a test.

**Climax:** Dani opens a PR. The CI passes. Jerome reviews within 48 hours, suggests one naming change, and approves. The PR is merged into the next release. Dani's name is in the contributors list.

**Resolution:** Dani's startup now has the feature they needed. More importantly, they trust the project — it builds cleanly, tests pass, the maintainer is responsive, and contributions are welcomed. Over the next six months, Dani submits three more PRs, including an implementation of a new embedding provider for their preferred model.

**Capabilities revealed:** Clean build experience, responsive maintainer engagement, good-first-issue labeling, CI pipeline, contributor-friendly project structure, clear code organization.

> **Scope note:** Contributor journey capabilities are project infrastructure requirements (CI, build, code organization, issue management), not product features — tracked separately from the MVP feature table.

---

### Journey Requirements Summary

| Journey | Key Capabilities Revealed |
|---|---|
| **Alex — EventStore path (J1, Phase 1.5)** | EventStore conventions, CLI search + explain, causal traversal, <30 min event onboarding |
| **Alex — Debug Path (J2, Phase 1.5)** | CLI diagnostics (status, explain, handlers list), error messages, event replay |
| **Alex — MCP Integration (J3, Phase 1.5)** | MCP tool definitions, token-budget responses, assistant configuration, source attribution |
| **Marcus — Onboarding (J4, Phase 2)** | Case member metadata, briefing via MCP, source verification, memory corrections |
| **Kenji — MVP Operations (J5, Phase 1)** | Tenant provisioning, isolation verification, boundary violation errors, access telemetry |
| **Kenji — Growth Operations (J6, Phase 3)** | Backend assessment, migration dry-run, Redis → Qdrant swap |
| **LLM Agent — Integration (J7, Phase 1.5)** | MCP tools, token-budget, multi-axis control, confidence scoring, degradation signaling |
| **Priya — End User (J8, Phase 2)** | Application REST API, narrative composition, source verification links, relevance scores |
| **Alex — Empty State (J9, Phase 1 thesis path)** | Helpful empty messages, README quickstart, ingest, case creation, first hybrid search |
| **Contributor — First PR (J10)** | Build experience, CI, code organization, maintainer responsiveness *(infrastructure)* |

**Coverage check:**
- Primary Phase 1 success path: Journey 9 (Alex first case / CLI)
- Primary Phase 1 operations: Journey 5 (Kenji provision + verify)
- Primary Phase 1.5 EventStore path: Journey 1 (Alex)
- Primary Phase 1.5 MCP: Journey 3 + Journey 7
- Primary Phase 1.5 debug: Journey 2
- Phase 2 narrative: Journey 4 (Marcus), Journey 8 (Priya)
- Phase 3: Journey 6
- Ecosystem / community: Journey 10 (Contributor) — not product scope

## Domain-Specific Requirements

### Compliance Boundary

Hexalith.Memories is **interpretive infrastructure** — it occupies a middle ground between raw storage and application logic. It doesn't make decisions, but it *does* make interpretations (embeddings, causal chains, confidence scores). This framing is more honest and defensible than "just infrastructure."

**Three-tier responsibility model:**

| Layer | Responsible for | Example |
|---|---|---|
| **Storage** | Data durability, isolation, encryption at rest | Redis, FalkorDB |
| **Interpretation (Memories)** | Accurate embeddings, correct causal chains, documented score semantics, complete edge graphs | Relevance confidence is not factual reliability; see Glossary |
| **Application** | Decisions based on interpretations, compliance, user-facing representations, legal obligations | Denying a claim based on a causal chain, GDPR compliance |

Memories provides the primitives that *enable* compliance:

- **Tenant deletion** (`memories tenant delete`) removes all indexes, graph data, and memory units for that tenant — enabling applications to fulfill erasure requests. **Limitation:** Cross-references to that tenant's data in *other* tenants' memory units are the application's responsibility to handle. The compliance guide must document this explicitly.
- **Physical tenant isolation** ensures no cross-tenant data leakage, a prerequisite for downstream compliance
- **Access telemetry** of queries, ingestion, and mutations is provided as infrastructure telemetry. This is *not* a tamper-evident audit trail — it does not guarantee append-only storage, integrity verification, or retention compliance. Applications requiring certified audit trails must implement their own on top of this telemetry.

**Compliance enablement documentation** must include:
- "Building Compliant Applications on Memories" guide showing how tenant delete maps to erasure, access telemetry maps to access records, case isolation maps to data segregation, memory unit metadata tracks data lineage
- "Limitations of Infrastructure-Level Deletion" section covering cross-reference scenarios
- **Legal disclaimer:** "This guide provides architectural patterns, not legal advice. Consult qualified legal counsel for your specific regulatory requirements."
- "Security Posture for Auditors" section providing architecture documentation, security design rationale, and dependency analysis to support enterprise audits. Hexalith.Memories is open-source software — SOC 2, ISO 27001, and similar certifications apply to organizations operating services, not to software artifacts. Deploying organizations include Memories in *their* audit scope.

### AI Reliability and Trust Boundaries

**Confidence Score Semantics:**
Confidence scores must have clear, documented meaning. Each memory unit's search result score is a composite with per-axis breakdowns:

| Component | What it measures | Range | Normalization |
|---|---|---|---|
| Syntactic score | BM25 relevance to query terms for single-axis search; rank contribution for hybrid search | 0.0–1.0 | Single-axis explain uses BM25 saturation; hybrid explain exposes weighted reciprocal-rank contribution so raw BM25 magnitude is not fused directly. |
| Semantic score | Cosine similarity for single-axis search; rank contribution for hybrid search | 0.0–1.0 | Single-axis explain uses cosine clamp; hybrid explain exposes weighted reciprocal-rank contribution. |
| NL score | Natural-language-description vector similarity for single-axis `axis=nl`; rank contribution for hybrid search when `nl` is enabled | 0.0–1.0 | Single-axis explain uses cosine clamp with syntactic-hash attribution backfill; hybrid explain exposes weighted reciprocal-rank contribution. |
| Graph score | Proximity in the relationship graph (hop distance, edge weight) | 0.0–1.0 | Inverse hop distance with decay function |
| Composite score | Weighted reciprocal-rank fusion of available axes | 0.0–1.0 | Weighted RRF over available axis rankings, normalized against the best possible rank contribution |

Single-axis scores keep their axis-specific meaning. Hybrid per-axis scores are rank-contribution scores, not raw BM25, cosine, or graph-proximity magnitudes. The fusion weights and algorithm are documented and deterministic. `--explain` exposes the score semantics and fusion weights applied.

**Evidence Packet (cross-surface trust envelope):** The trust primitives defined across these requirements — composite confidence with per-axis breakdown (FR63), source/origin attribution (FR24), token-budget-aware omitted-detail handling (FR23), and graceful-degradation signaling (FR66), together with tenant/case scope, result state, and recovery guidance — are composed into a single shared response object referred to as the **Evidence Packet**. The Evidence Packet is the cross-surface envelope used identically by CLI JSON output, MCP tool responses, and future web UI composition, so no interface invents a conflicting definition of confidence, degraded state, omitted details, or recovery action. Its concrete shape is owned by `Contracts.V1` (see Architecture) and its presentation semantics are elaborated in the UX Design Specification.

**Critical distinction: confidence scores measure query-result relevance, NOT factual accuracy or data completeness.** A score of 0.95 means the result is highly relevant to the query — it does not mean the underlying data is complete, correct, or current. This distinction must appear in:
- API reference documentation
- CLI `--explain` output (every explain result includes this caveat)
- Compliance enablement guide
- MCP tool response schema documentation

**Metadata confidence** is separate from search relevance: each metadata field on a memory unit tracks its origin (`human-declared` vs `ai-inferred`) and confidence (0.0–1.0). This distinguishes "the user tagged this as 'payment-related'" from "the embedding model inferred this is about payments."

**Memory unit provenance:** Every memory unit tracks `ingested_by` as a mandatory MVP field. At authenticated external ingress, provenance binds to the normalized `sub` principal; caller-supplied provenance is rejected or ignored. Trusted internal adapters may use only allowlisted `system:*` identities through an explicitly authenticated service boundary. Display metadata never overrides the authenticated principal. Tenant claims authorize access. Case membership is tenant-scoped domain metadata and does not grant authorization in the current phase.

**Structured Causal Data:**
Memories is responsible for delivering **unambiguous causal chain structure**, not raw results that the LLM must interpret. When `traverse_relations` returns a causal chain, it provides:

- Ordered sequence of nodes (events/documents/discussions) with explicit direction
- Timestamps on each node establishing chronological order
- Typed, directional edges with confidence tiers
- Edge confidence reflecting relationship strength
- **Gap detection:** If a causal chain has missing intermediate nodes (e.g., A's CausationId points to B, B's points to C, but B isn't indexed), the chain must flag the gap explicitly: `A → [MISSING: event-id-B] → C`. Never silently skip missing nodes. This is a data accuracy responsibility of the Interpretation layer.

**Edge Type Taxonomy (MVP minimum):**

| Edge Type | Source | Default Confidence | Semantics |
|---|---|---|---|
| `caused_by` | Explicit CausationId from EventStore | 1.0 | Direct causal link: Event B was directly caused by Event A |
| `correlated_with` | CorrelationId from EventStore | 0.8 | Same correlation context: Events B, C, D all occurred in the same workflow as Event A, but did not necessarily cause each other |
| `references` | Explicit link or AI-inferred content similarity | 0.5–1.0 | Document A references or relates to Document B. 1.0 for explicit links, 0.5 for AI-inferred |
| `contains` | Case/folder structure | 1.0 | Structural: case contains memory unit |
| `annotates` | User correction or commentary | 1.0 | Memory unit B is an annotation/correction on memory unit A |

The distinction between `caused_by` and `correlated_with` is critical. Collapsing CorrelationId into causation makes every event in a correlation group appear to cause every other event — exactly the misrepresentation the structured data model exists to prevent.

Users can promote AI-inferred edge confidence (e.g., from 0.5 to 1.0) when they verify a relationship. The system never auto-promotes.

**Confidence calibration (Growth phase):** Periodic review of *edge* confidence tiers against reviewer judgments of relationship correctness — separate from relevance confidence. Do not read 0.8 relevance as "~80% factual accuracy."

**Responsibility boundary:** Memories owns data accuracy (correct ordering, complete chains, accurate edge types, gap detection). The LLM owns narrative quality (prose composition, summarization). If the structured data has wrong ordering, missing links, or silent gaps, that's a Memories bug. If the prose misrepresents correct structured data, that's an LLM problem.

### Open-Source Licensing

**Hexalith.Memories license: Apache 2.0** (decision)

Apache 2.0 signals long-term trust for enterprise adoption. The README must include a public commitment: *"Hexalith.Memories is committed to the Apache 2.0 license. We will not change to a restrictive license."* This preempts BSL-switch concerns that have eroded trust in other AI infrastructure projects.

**Dependency chain licensing:**

| Dependency | License | Risk Level | Implication |
|---|---|---|---|
| DAPR | Apache 2.0 | None | Permissive, fully compatible |
| Redis (core) | BSD 3-Clause | None | Permissive, fully compatible |
| RediSearch / Redis Stack | SSPL / RSAL | **Medium** | Users must self-host or use Redis Cloud. **Cannot offer Hexalith.Memories as a competing managed service on Redis Stack.** This constraint must be documented in the README deployment section, not buried in licensing files. |
| FalkorDB | AGPL-3.0 | **Medium** | Memories connects to FalkorDB as an external service via DAPR state management abstraction — the DAPR sidecar is the client, not application code. This architectural boundary means application code is not subject to AGPL copyleft. However, enterprise legal teams will flag AGPL dependencies regardless. |

**Licensing de-risk strategy:**

1. **LICENSE-DEPENDENCIES.md** — Document the architectural boundary between Memories and FalkorDB explicitly. State that Memories communicates with FalkorDB over the network as an external service, not via direct embedding. Give enterprise legal teams something concrete to evaluate.
2. **FalkorDB version pinning** — Pin to a specific AGPL-licensed version in the default docker-compose.yml. If FalkorDB relicenses, users can stay on the pinned version while alternatives are built. Version pinning is the cheapest first defense against relicensing risk.
3. **IMemoryGraph AND IMemoryIndex extraction points identified in Phase 2** — Not premature abstraction, but licensing insurance. If FalkorDB's AGPL becomes an enterprise blocker, extracting the interface enables swapping to Neo4j (GPL with commercial license) or graph-on-Redis. If Redis Stack goes proprietary, IMemoryIndex enables migration to Dragonfly/KeyDB (BSD) or Qdrant. Low extraction cost, high insurance value. Recovery time with pre-identified extraction points: 2–4 weeks. Without: 2–4 months.
4. **SSPL constraint in README deployment section** — "Offering Hexalith.Memories as a hosted/managed service requires compliance with Redis Stack's SSPL terms. Self-hosted deployments are unaffected."

## Innovation & Novel Patterns

### Detected Innovation Areas

**1. Three-Axis Retrieval Fusion (Core Innovation)**
The product bet is documented deterministic RRF across syntactic, semantic, and graph on a DAPR/EventStore causal graph, with `--explain`. Hybrid BM25+vector is widely available (Elasticsearch, Azure AI Search, Weaviate, Vespa). Novelty is the EventStore/DAPR causal graph in that fusion, not "no system fuses three axes."

**2. Event Memory via DAPR Pub/Sub (Platform Innovation)**
EventStore happy path: subscribe, follow conventions, index dual embeddings and CausationId/CorrelationId as graph edges. Schema changes require handler registration. Generic DAPR publishers (Marten, Wolverine, Axon) are an experiment — if integration needs custom code beyond subscription config, keep EventStore conventions as the beachhead.

**3. Causal Intelligence as a Query Interface (Domain Innovation)**
Event-sourced systems already capture *why* things happen — but that causal data is locked inside infrastructure, queryable only by developers who know the event store schema. Memories makes causal chains queryable via natural language: "What led to this decision?" walks the CausationId graph and returns structured, ordered, gap-aware results. This transforms event sourcing from a persistence pattern into a knowledge management pattern.

**4. Interpretive Infrastructure (Positioning Innovation)**
The three-tier responsibility model — Storage → Interpretation → Application — is a novel positioning for AI infrastructure. Memories is not "just a database" (it interprets content) and not "an AI application" (it doesn't make decisions). This framing creates a defensible product category and a clear responsibility boundary.

### Market Context & Competitive Landscape

| Competitor | Axes | Team Memory | Causal Intelligence | Notes |
|---|---|---|---|---|
| Mem0 / Zep / LangChain Memory | Semantic (± metadata) | Per-user agent memory | No | Different job: chat memory, not case-scoped team knowledge |
| Elasticsearch / OpenSearch / Azure AI Search | Syntactic + semantic hybrid | Multi-tenancy varies | No CausationId graph | Real baseline for BM25+vector |
| Weaviate / Vespa | Hybrid search, some graph | Multi-tenancy varies | Not EventStore causality | Must be in the comparison set |
| Microsoft GraphRAG / LlamaIndex PropertyGraph / Neo4j LLM graphs | Graph + LLM | Project-scoped | RAG graphs, not DAPR CausationId | Closest "why" competitors |
| Qdrant + custom glue | Semantic + custom | Custom | Custom | Integration-depth argument, not uniqueness |
| Notion / Confluence | Full-text | Yes | No | Knowledge-base rivals the brief named; collaboration features here are Phase 2 |
| **Hexalith.Memories** | Syntactic + semantic + graph (RRF of available axes) | Case model (MVP); discussions/briefings Phase 2 | EventStore CausationId (Phase 1.5 product integration) | DAPR-native; EventStore conventions are the beachhead |

Falsifiable claim: Hexalith.Memories ships documented deterministic RRF across syntactic, semantic, and graph *on a DAPR/EventStore causal graph* — not "no competitor offers team memory." Replicating the integrated Hexalith path is still costly; that is not a surveyed uniqueness proof.

**Addressable market:** EventStore users are the beachhead. Generic DAPR pub/sub (Marten, Wolverine, Axon) is an experiment with a kill switch (custom code beyond subscription config). It is not the brief's standalone case+three-axis expansion path and not an EventStore-equivalent zero-code promise.

### Validation Approach

| Innovation | Validation Method | Kill Switch |
|---|---|---|
| Three-axis fusion | Benchmark suite per Measurable Outcomes (hybrid vs BM25+semantic, NDCG@10) | Execute named kill-switch actions if 80% misses |
| EventStore product integration | Timed Phase 1.5 onboarding: `dotnet add package` + subscription to first event search | If onboarding exceeds 30 minutes, the conventions promise is broken |
| Causal intelligence | Causal chain completeness test: 95%+ of known CausationId chains fully traversable | Phase 1.5 launch gate |
| DAPR-generic pattern | Test with non-EventStore event source (e.g., Marten publishing to DAPR) | If integration requires custom code beyond DAPR subscription config, the pattern isn't generic — keep EventStore conventions as the beachhead |

### Risk Mitigation

**If three-axis fusion doesn't validate (graph axis adds no value):**
Execute the Measurable Outcomes kill-switch actions. Do not treat "reposition as team memory with intelligent search while keeping default hybrid and FalkorDB" as a pass.

**If EventStore conventions require handler registration on schema change:**
That is expected. Document it. Do not claim zero mapping / zero configuration in README or exec summary.

**If causal chains are incomplete:**
Missing nodes are already handled by gap detection (`[MISSING: event-id]`). If completeness falls below 95%, investigate: is it an ingestion latency issue (events not yet indexed) or a structural issue (CausationId metadata not propagated)? Latency is fixable; structural gaps require working with the event source framework.

## Developer Tool / API Backend Specific Requirements

### Project-Type Overview

Hexalith.Memories is a hybrid Developer Tool + API Backend delivered as NuGet packages with a DAPR-native service architecture orchestrated by .NET Aspire. Internal services communicate via DAPR service invocation; external consumers (CLI, LLM agents, third-party apps) connect through a REST API behind infrastructure-managed ingress. The system reuses shared infrastructure from the Hexalith ecosystem via root-declared git submodules under `references/` (`references/Hexalith.Commons` for error handling, `references/Hexalith.EventStore` for versioning conventions).

### Technical Architecture Considerations

**Language & Platform Matrix:**

| Aspect | MVP | Future |
|---|---|---|
| Server runtime | .NET 10 / C# 14. SDK pin is `global.json` (repository configuration), not this PRD. | .NET plus optional architecture-owned polyglot sidecars (see addendum: Dapr Agents) |
| Client libraries | MVP CLI uses a minimal direct HTTP/ingress adapter inside the CLI; reusable `.NET` client packages are not MVP blockers | `.NET` (`Client` for DAPR consumers, `Client.Rest` for external consumers), Python, TypeScript clients targeting ingress REST API |
| CLI | .NET global tool (`dotnet tool install -g Hexalith.Memories.Cli`) | Same |
| Cross-language access | Via ingress REST API (any HTTP client) or DAPR service invocation (any DAPR SDK) | Dedicated language-specific client packages |
| IDE tooling | None | VS/Rider templates, analyzers (deferred) |

**Package Distribution:**

`tools/release-packages.json` is the sole release inventory of published package IDs. A separate non-packable-host table names service/orchestration projects; those rows are not part of the published-package count. Do not restate a magic "9+3" in this PRD when the JSON and host list diverge — update the tables, not a slogan.

**Published packages** (IDs must match `tools/release-packages.json`):

| Package | Purpose |
|---|---|
| `Hexalith.Memories.Contracts` | Domain types, memory unit model, envelopes |
| `Hexalith.Memories.Client.Rest` | Typed HTTP client for external consumers via ingress REST |
| `Hexalith.Memories.Redis` | Compatibility-only Redis/FalkorDB API retained for existing package consumers |
| `Hexalith.Memories.Cli` | CLI tool (dotnet global tool) |
| `Hexalith.Memories.Mcp` | MCP server |
| `Hexalith.Memories.Aspire` | Reusable Aspire resource-model integration |
| `Hexalith.Memories.EventStore` | EventStore product-integration package (Phase 1.5 surface) |
| `Hexalith.Memories.Telemetry` | Shared telemetry constants and collectors |
| `Hexalith.Memories.ServiceDefaults` | Shared packaged service defaults |

**Non-packable hosts** (not in the published count):

| Host | Purpose |
|---|---|
| `Hexalith.Memories.Server` | DAPR service, workflows/actors, REST controllers |
| `Hexalith.Memories.AppHost` | .NET Aspire orchestration |

Mechanism detail (dependency graph, Redis compatibility facade, composition root) is in `addendum.md` and architecture.

**Deployment Topology:**

External consumers connect through infrastructure-managed ingress (YARP, nginx, cloud API gateway — not application code). Internal services communicate via DAPR mesh.

- **LLM Agent** → ingress → MCP Server (DAPR sidecar) → DAPR → Memories Server
- **CLI / Third-party apps** → ingress → Memories Server (REST controllers)
- **Memories Server** → DAPR → Redis/FalkorDB
- **EventStore events** → DAPR pub/sub → Memories Server

MCP Server is a DAPR service with its own sidecar, communicating with Memories Server via DAPR service invocation. Memories Server exposes REST controllers (for ingress routing) alongside DAPR endpoints (for internal consumers), both in the same ASP.NET Core host.

**Service Communication Model:**

| Layer | Communication |
|---|---|
| Internal (Server ↔ MCP Server) | DAPR service invocation |
| Internal (DAPR state) | Sidecar state API only |
| Internal (Redis/FalkorDB search/graph) | Approved infrastructure-boundary clients with Aspire-injected keyed connections — not via the DAPR state API as a generic proxy. Product projects do not construct infrastructure endpoints or clients. |
| Internal (EventStore domain truth) | EventStore acknowledgement is the durable commit for Case / MemoryUnit / Tenant |
| Internal (EventStore product integration) | CloudEvents via DAPR pub/sub (Phase 1.5) |
| External → Internal (CLI, LLM agents, third-party) | REST API via infrastructure ingress. CLI HTTP in MVP is transport, not the Phase 2 application search UI. |
| Serialization | JSON exclusively |
| Authentication | DAPR API token (internal); NFR11 authenticated product ingress (external). Anonymous: enumerated health/DAPR infrastructure routes only. |
| Tenant context | Tenant claims on the authenticated principal; also validated in payloads |
| Rate limiting | In scope: embedding-provider throttle (FR69), inbound request quotas by authenticated tenant, plus ingress. Not "deferred to infrastructure" as the only control. |
| Identity / provenance | Tenant claims authorize. Case membership is metadata. Provenance binds to `sub` / allowlisted `system:*`. |

**Error Handling Model:**

Errors follow the Hexalith.Commons shared error handling conventions (via `references/Hexalith.Commons`). Error propagation chain across all hops:

| Hop | Error Format | Includes |
|---|---|---|
| Memories Server → DAPR | Hexalith.Commons error envelope | Error code, failed component (actor, index, graph), details |
| MCP Server → LLM Agent | MCP error response | Hexalith error code mapped to MCP format, failed service identifier |
| Ingress → CLI / third-party | HTTP status + Hexalith.Commons JSON envelope | Error code, failed component, recovery suggestion |
| CLI → terminal | Human-readable message + error code; JSON in `--format json` | Actionable guidance ("Is the service running? Check `dotnet run --project AppHost`") |

Internal DAPR errors must propagate through ingress with enough context for CLI to display actionable diagnostics — never swallow into generic 502.

**Versioning Strategy:**

Aligned with Hexalith.EventStore conventions (via `references/Hexalith.EventStore`):
- NuGet packages: Semantic versioning
- Service contract: Backward-compatible additions only (no versioned endpoints — DAPR app-id is unversioned)
- Breaking changes: New message types, deprecation cycle matching EventStore patterns

**Health Check & Observability:**

| Aspect | Requirement |
|---|---|
| Readiness/liveness | Aspire ServiceDefaults wires standard .NET health checks; must verify all three backends (RediSearch, Redis Vector, FalkorDB) |
| Tracing | Aspire ServiceDefaults configures OpenTelemetry export; trace context propagates across DAPR calls and through ingress |
| Logging | Aspire ServiceDefaults configures structured JSON logging with OpenTelemetry; correlation IDs from DAPR trace context |
| Metrics | Aspire dashboard surfaces DAPR + custom metrics (ingestion throughput, search latency per axis, index size per tenant) via OpenTelemetry export |
| Dashboard | Aspire dashboard provides local dev observability out of the box — no separate management API needed |
| Consistency check | `memories tenant verify` detects index/graph divergence (orphaned graph edges, missing index entries across RediSearch + Vector + FalkorDB) |

### Embedding Provider Configuration

MVP supports Google embedding generation at runtime. Configuration is per-tenant and deliberately shaped for provider expansion — different tenants can carry provider/model/rate-limit configuration, but non-Google runtime providers are post-MVP unless a later sprint change explicitly pulls them forward.

**Supported Providers (MVP):**

| Provider | Model (default) | Dimensions | Rate Limit (default) |
|---|---|---|---|
| Google | `text-embedding-004` | 768 | 1500 req/min |

**Post-MVP provider expansion candidates:**

| Provider | Model (default) | Dimensions | Notes |
|---|---|---|---|
| OpenAI | `text-embedding-3-small` | 1536 | Deferred provider implementation |
| Mistral | `mistral-embed` | 1024 | Deferred provider implementation |
| Ollama | `qwen3-embedding:4b` | 2560 | Covered by Epic 13 provider migration work |

**Configuration per tenant:**

| Field | Purpose | Source |
|---|---|---|
| `provider` | MVP: google. Post-MVP: openai / mistral / ollama / custom via provider expansion stories | Tenant config |
| `model` | Specific model ID | Tenant config |
| `dimensions` | Vector dimensions (determines Redis Vector index schema) | Derived from provider/model |
| `apiKey` | Provider API key reference | DAPR Secrets API backed by OpenBao |
| `rateLimitPerMinute` | Throttle ceiling for embedding calls (rate-limiter actor) | Tenant config |

**Critical constraints:**
- Redis Vector Search index schema is fixed at creation — **switching embedding providers requires full reindex of that tenant's data**. This is a migration operation, not a configuration change. Must be documented in operator guide.
- Shared API keys mean shared rate limits across tenants. For rate limit isolation, tenants should use separate API keys. The embedding rate-limiter actor enforces per-tenant throttle ceilings, but the actual provider API ceiling is the shared bottleneck.

### Async Ingestion Pipeline

Ingestion is a **DAPR Workflow** (`IngestionWorkflow`): extract, embed, and project to search/vector/graph as compensable activities. A per-tenant actor, if present, owns **embedding rate-limit budget only** — not the document queue or stage orchestration.

**Consistency (replaces "atomic write across three backends"):** EventStore acknowledgement is the durable source-of-truth commit. Search/vector/graph writes are idempotent rebuildable projections coordinated by the durable workflow. No distributed transaction is claimed.

**Observable state machine:** `pending`, `projecting`, `indexed`, `partially failed/retrying`, `failed/dead-lettered`, `repaired`. `indexed` is emitted only after every required active projection acknowledges the same source version.

**Pipeline Stages:**

| Stage | What happens | Owner |
|---|---|---|
| `pending` | Content accepted; EventStore commit durable | Workflow |
| `extracting` | Text extraction from content (PDF, URL, file) | Workflow activity |
| `embedding` | Call embedding provider API, get vector | Throttled by per-tenant rate-limiter actor |
| `projecting` | Write RediSearch, Redis Vector, FalkorDB as rebuildable projections | Workflow + compensation |
| `indexed` | All required projections ack the same source version | Terminal success |
| `failed` / `dead-lettered` | Error at any stage, max retries exceeded | Visible via CLI; not silently dropped |

**Failure handling:**
- Failed units retry with exponential backoff (configurable max retries)
- After max retries, units move to `failed`/`dead-lettered` with error details preserved
- `memories status --case X` shows counts per observable state
- `memories status --failed` shows failed units with error details and stage
- FR13: no silent two-of-three searchable unit. A unit is not `indexed` (not searchable as complete) until all required projections ack, or it stays `partially failed/retrying` / `failed`.

**Runtime split:**
- **IngestionWorkflow:** stages, retry, compensation, Durable Task persistence (NFR17).
- **EmbeddingRateLimiterActor / CorpusStatisticsActor:** singleton/budget actors only.

### Interface Capability Parity Matrix

Not all capabilities map to all interfaces. **Capability alignment, not feature parity.** CLI is the operational superset. MCP exposes what LLM agents need (search, ingest, traverse, case info). Tenant management, diagnostics, and interactive features are CLI-only.

Implementation is split by phase: Phase 1 CLI essentials are `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, `status`, and benchmark support; Phase 1.5 expands with `explore`, `handlers`, `quickstart`, remaining FR53 slices, and EventStore diagnostics.

| Capability | CLI | MCP | DAPR Service Invocation |
|---|---|---|---|
| Search (syntactic, semantic, graph, hybrid) | `memories search` | `search_memory` | `SearchAsync` |
| Search with explain | `memories search --explain` | `search_memory` (explain field) | `SearchAsync` (explain option) |
| Content ingestion | `memories ingest` | `ingest_content` | `IngestAsync` |
| Graph traversal | `memories traverse` | `traverse_relations` | `TraverseAsync` |
| Case management (create, delete, members) | `memories case` | `get_case_info` | `CaseAsync` methods |
| Tenant management | `memories tenant` | -- | `TenantAsync` methods |
| Tenant isolation verification | `memories tenant verify` | -- | -- |
| Ingestion status & failed units | `memories status` | -- | -- |
| Interactive exploration | `memories explore` | -- | -- |
| Handler management | `memories handlers` | -- | -- |
| Guided quickstart | README quickstart in MVP; `memories quickstart` in Phase 1.5 | -- | -- |
| Batch directory ingestion | `memories ingest <dir>` | -- | -- |

**Design rationale:** MCP exposes agent work. CLI exposes ops. DAPR service invocation is the internal programmatic API. FR53 is satisfied per active phase; a help entry backed by `NotImplementedCommand` is not coverage.

### CLI Specification

**Distribution:** .NET global tool (`dotnet tool install -g Hexalith.Memories.Cli`)

**MVP command scope:** `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, `status`, and benchmark support.

**Phase 1.5 expansion scope:** `explore`, `handlers`, `quickstart`, remaining FR53 slices, EventStore diagnostics.

**Command Structure:**

| Command Group | Commands |
|---|---|
| `memories ingest` | `<file>`, `<url>`, `<directory>`, `--case` |
| `memories search` | `<query>`, `--case`, `--explain`, `--axes`, `--format` |
| `memories explore` | `--case`, `--from`, `--depth` |
| `memories traverse` | `--from`, `--depth`, `--edge-type` |
| `memories case` | `create`, `delete`, `add-member`, `activity`, `list` |
| `memories tenant` | `create`, `delete`, `verify`, `switch`, `list` |
| `memories status` | `--case`, `--tenant`, `--failed`, `--ingestion` |
| `memories handlers` | `--list`, `--register` |
| `memories quickstart` | Guided interactive setup (Phase 1.5 unless explicitly pulled forward) |

**Output Formats:**

| Format | Flag | Use Case |
|---|---|---|
| Human-readable (default) | none | Interactive terminal use |
| JSON | `--format json` | Scripting, pipeline integration, LLM consumption |
| Table | `--format table` | Structured human-readable |

**Configuration Layering (precedence high to low):**

1. Command-line flags
2. Environment variables (`HEXALITH_MEMORIES_*`)
3. Config file (`~/.hexalith/memories.json` or project-local)
4. DAPR Secrets API backed by OpenBao for embedding, LLM, and application runtime secrets
5. DAPR configuration for sidecar discovery, app-id, and non-secret component settings

Sensitive values are not resolved through configuration fallback. Product services retrieve them through DAPR secret-store components. Aspire secret parameters or .NET User Secrets may supply protected local bootstrap or one-time seeding inputs, but product services must not read them as an alternative runtime secret provider. Kubernetes Secrets are permitted only where required for OpenBao bootstrap material or direct pod inputs that DAPR cannot provide.

### Developer Experience & Documentation

**In-Repo Examples (`samples/` folder):**

| Example | Maps to | Demonstrates |
|---|---|---|
| `samples/01-quickstart/` | Journey 9 (Phase 1 thesis path) | `dotnet run --project AppHost` boots full stack, ingest + search via CLI |
| `samples/02-eventstore-integration/` | Journey 1 (Phase 1.5) | Aspire AppHost with EventStore + Memories, DAPR subscription wired |
| `samples/03-mcp-agent/` | Journey 3 (Phase 1.5) | MCP server launched by Aspire, agent configuration |

Numbered naming signals the learning path and mirrors user journey progression.

**Documentation Strategy:**

| Artifact | Scope |
|---|---|
| README | 30-second demo, getting started guide, architecture overview |
| CLI `--help` | Built-in documentation with examples per command |
| Getting started guide | Phase 1: AppHost/README to first CLI search in <30 min. Phase 1.5: `dotnet add package` + subscription to first event search in <30 min. |
| API reference | Auto-generated from `Contracts` XML docs |
| Compliance enablement guide | Building compliant apps on Memories |
| Operator guide | Tenant management, embedding provider migration (reindex), scaling |

No dedicated migration guide — the getting started guide covers the path from duct-tape solutions naturally.

### Test Infrastructure Strategy

| Test Layer | Approach | What It Validates |
|---|---|---|
| **Unit tests** | Mock `DaprClient` — no sidecar required | Business logic, domain model, fusion algorithm, score normalization |
| **Integration tests** | Aspire `DistributedApplicationTestingBuilder` or DAPR testcontainers | End-to-end ingestion workflow, search across available axes, tenant isolation, workflow durability, index/graph consistency |
| **Contract tests** | Serialization round-trip tests | CloudEvent payloads, service invocation contracts, REST API contracts, error envelopes |

Contributors can run unit tests without Docker. Integration tests require Docker (documented in CONTRIBUTING.md). CI runs all layers.

### Implementation Considerations

**Git Submodule Dependencies:**
- `references/Hexalith.Commons` — Error handling, shared utilities, base types
- `references/Hexalith.EventStore` — Event types, versioning conventions, DAPR integration patterns

**DAPR + Aspire Orchestration:**
Current path: .NET Aspire AppHost with DAPR sidecars. Local: `dotnet run --project Hexalith.Memories.AppHost`. This is orchestration, not an extra product surface — see `addendum.md`.

**Cross-Language Future Path:**
Non-.NET external consumers can integrate today via ingress REST API (JSON payloads). Non-.NET internal services can integrate via DAPR service invocation (any DAPR SDK). Dedicated Python/TypeScript client packages are a future convenience layer.

## Functional Requirements

FR1–FR74 are the product-horizon inventory, not a claim that every FR is active thesis-MVP scope.

**Canonical phase register** (August 2026 change control):

- **MVP (thesis + foundation):** FR1–FR22, FR24–FR52, FR55–FR57, FR63–FR70, FR72–FR74, plus the already-delivered portion of FR53.
- **Phase 1.5:** FR23, FR54, FR58–FR62, and remaining FR53 command slices.
- **Phase 2:** FR71 (completed early as non-MVP Story 8.3; Epic 26 covers operational backup/restore only — do not reschedule application export as new Phase 2 work).

A capability completed before its planned phase is recorded as completed non-MVP and does not silently change thesis-MVP acceptance.

### Knowledge Ingestion

- **FR1:** Developer can ingest content from local files into a specified case
- **FR2:** Developer can ingest content from URLs into a specified case
- **FR3:** Developer can batch-ingest content from a directory into a specified case
- **FR4:** System can extract text from ingested content (plain text, PDF, markdown)
- **FR5:** System can generate embeddings for ingested content via a configurable embedding provider
- **FR6:** System ensures a memory unit is `indexed` (searchable across all *required active* projections/axes) only after every required projection acknowledges the same EventStore source version
- **FR7:** Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and metadata confidence score
- **FR8:** System manages ingestion load per tenant independently
- **FR9:** System retries failed ingestion automatically with configurable limits
- **FR10:** Developer can view ingestion status per case (pending, projecting, indexed, failed counts)
- **FR11:** Developer can view failed ingestion units with error details and failure stage
- **FR12:** Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk
- **FR13:** Partial projection failure never yields a silently searchable two-of-three unit. EventStore acknowledgement is the durable commit; search/vector/graph writes retry/compensate until `indexed` or `failed`/`dead-lettered`. No distributed transaction. Rollback of the EventStore commit is not the recovery model.

### Knowledge Retrieval

- **FR14:** Developer can search memory units by syntactic matching within a tenant
- **FR15:** Developer can search memory units by semantic similarity within a tenant
- **FR16:** Developer can search memory units by graph traversal within a tenant
- **FR17:** Developer can search memory units by hybrid fusion combining all available axes
- **FR18:** Developer can control which axes are included in a search query
- **FR19:** Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode)
- **FR20:** Developer can filter search results by case
- **FR21:** Developer can filter search results by metadata field values
- **FR22:** Developer can paginate search results
- **FR23:** LLM Agent can constrain search response size by token budget. **Phase:** 1.5
- **FR24:** System returns the origin identifier (file path, URL, or event ID) and origin type for each search result
- **FR25:** Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output

### Memory Organization

- **FR26:** Developer can create a case within a tenant
- **FR27:** Developer can delete a case and all its memory units
- **FR28:** Developer can add members to a case
- **FR29:** Developer can remove members from a case
- **FR30:** Developer can list cases within a tenant
- **FR31:** Developer can view case status including memory unit count, last activity timestamp, and latest ingestion state (`indexed` / `projecting` / `failed`)
- **FR32:** System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion
- **FR33:** System maintains case-scoped graph edges between memory units within a case
- **FR34:** Developer can search across all cases within a tenant by keyword, returning results with case attribution
- **FR35:** Developer can delete an individual memory unit from a case
- **FR36:** Developer can view recent activity within a case (ingestion events, searches, membership changes)
- **FR37:** Developer can annotate or correct a memory unit, with annotations tracked as linked memory units

### Tenant Management

- **FR38:** Operator can create a tenant with tenant-scoped backend principals and tenant-scoped indexes (isolation *outcome* is NFR8; mechanism is architecture-owned)
- **FR39:** Operator can delete a tenant and all its indexes, graph data, and memory units
- **FR40:** Operator can verify tenant isolation via automated checks
- **FR41:** Operator can list tenants
- **FR42:** Operator can update tenant configuration after creation (rate limits, display name, settings)
- **FR43:** System refuses embedding-provider or index-schema changes that require reindex unless the operator passes an explicit acknowledgment flag; the CLI states that existing vectors will be rebuilt
- **FR44:** System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages
- **FR45:** Operator can view current configuration of a tenant (embedding provider, rate limits, index status)

### Causal Intelligence

- **FR46:** System can index CausationId and CorrelationId from events as typed, directional graph edges
- **FR47:** Developer can traverse causal chains from a starting node with configurable depth
- **FR48:** Developer can filter graph traversal by edge type
- **FR49:** When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier
- **FR50:** System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence
- **FR51:** Developer can promote AI-inferred edge confidence when verifying a relationship
- **FR52:** System maintains chronological ordering and timestamps on causal chain nodes

### Developer Interfaces

- **FR53:** Developer can interact with retrieval and ingestion capabilities via CLI **per the active phase register**. Current real commands count; `NotImplementedCommand` does not. **Phase:** split (MVP vs 1.5 slices)
- **FR54:** Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools. **Phase:** 1.5
- **FR55:** CLI supports multiple output formats: human-readable (default), JSON, and table
- **FR56:** CLI provides actionable error messages with recovery suggestions for common failure modes
- **FR57:** Developer can discover available *implemented* actions from empty states and error conditions (empty-state copy + `--help` examples). Does not require a universal command catalog of unbuilt verbs.
- **FR58:** MCP tools include typed parameter schemas with descriptions for LLM agent consumption. **Phase:** 1.5

### EventStore Integration

- **FR59:** System can auto-discover event types published to DAPR pub/sub topics. **Phase:** 1.5. Happy path is conventions + subscription; schema evolution requires handler registration.
- **FR60:** System can generate dual embeddings for events (raw payload + natural language description). **Phase:** 1.5
- **FR61:** System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code on the EventStore happy path. **Phase:** 1.5
- **FR62:** Developer can list registered event handlers and detect handler registration mismatches. **Phase:** 1.5

### Trust & Transparency

- **FR63:** System returns composite confidence scores (0.0-1.0) with per-axis breakdowns for each search result
- **FR64:** System tracks metadata origin (human-declared vs AI-inferred) and confidence per metadata field on every memory unit
- **FR65:** System records `ingested_by` (user or system identity) as a mandatory field on every memory unit
- **FR66:** When one or more search backends are unavailable, system returns partial results with an indication of which axes were excluded
- **FR67:** System logs search and access events per tenant for audit purposes

### Embedding Provider Management

- **FR68:** Operator can configure embedding provider and model per tenant
- **FR69:** System enforces per-tenant rate limit ceilings for embedding API calls
- **FR70:** System tracks the embedding provider and model used for each memory unit's vectors

### Data Portability & System Health

- **FR71:** Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. **Phase:** Phase 2. Completed early as non-MVP (Story 8.3). Epic 26 covers operational backup/restore only — do not reschedule application-facing export as new Phase 2 work.
- **FR72:** System exposes readiness and liveness health checks verifying all backends
- **FR73:** Operator can detect index/graph divergence via consistency check
- **FR74:** Operator can repair detected index/graph inconsistencies via a dry-run-then-apply consistency repair that cannot cross tenants, cannot silently delete units without provenance in access telemetry, and cannot invent edges the EventStore source version does not support

## Non-Functional Requirements

*NFRs are tagged by validation phase: **[MVP]** = thesis/foundation, **[P1.5]** = EventStore product integration + MCP, **[Ongoing]** = as infrastructure matures, **[Future web]** = Epic 17+.*

### Performance

| NFR | Metric | Target | Conditions | Phase |
|---|---|---|---|---|
| **NFR1** | Syntactic search latency (p95) | <200ms | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| **NFR2** | Semantic search latency (p95) | <500ms | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| **NFR3** | Hybrid search latency (p95) | <1s | 10 concurrent queries/tenant, 10K memory units/tenant | MVP |
| **NFR4** | Graph traversal latency (p95) | <2s | 10 concurrent queries/tenant, 10K memory units/tenant, depth ≤5 | MVP |
| **NFR5** | Ingestion throughput | >100 memory units/min (payloads ≤10KB), >10 memory units/min (payloads ≤1MB) | Per tenant, single-document embedding calls (not batched) | Ongoing |
| **NFR6** | Event indexing freshness | <5s from DAPR pub/sub publication to searchable under normal conditions; degradation documented when embedding provider is rate-limited | Per event | P1.5 |
| **NFR7** | Cold start time | Service fully operational within 60s | From containers running to accepting queries — excludes image pull time | Ongoing |

### Security

| NFR | Requirement | Verification | Phase |
|---|---|---|---|
| **NFR8** | Zero cross-tenant data leakage — no search, ingestion, or graph traversal returns data from another tenant | Automated test suite: search, ingest, graph across all axes with malformed/empty/swapped tenant IDs. Graph-specific test: create identical graph structures in tenant A and B, traverse from tenant A, verify zero nodes from tenant B appear even if edge IDs collide | MVP |
| **NFR9** | Product services retrieve embedding-provider and other application runtime secrets exclusively through the DAPR Secrets API, backed by OpenBao in Aspire and deployed environments. Secret values are never stored in application configuration or ordinary environment variables. Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or direct pod inputs outside the DAPR secret-store boundary. | Structural dependency tests, secret scanning, AppHost topology tests, and integration tests proving DAPR reads from OpenBao without secret disclosure | Ongoing |
| **NFR10** | All inter-service communication authenticated via DAPR API tokens | DAPR configuration validation | Ongoing |
| **NFR11** | External product REST/CLI ingress is authenticated for the active MVP HTTP surface. Health probes and required DAPR infrastructure routes are the only deliberate anonymous exceptions and are named and tested. Additional identity-provider hardening may remain operational-readiness work; unauthenticated product ingress is not a Phase 1.5 allowance. | Integration test with unauthenticated product requests plus named anonymous exceptions | MVP |

### Scalability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR12** | System supports linear scaling of tenants — adding a new tenant does not degrade existing tenant performance by more than 5% | Validated at 10 tenants, each with 100K memory units. Methodology: benchmark tenant 1 alone, add 9 loaded tenants, re-benchmark tenant 1, measure delta | Ongoing |
| **NFR13** | Per-tenant ingestion pipeline scales independently — one tenant's batch ingestion does not block another tenant's real-time ingestion | Concurrent ingestion test across 3 tenants | Ongoing |
| **NFR14** | Redis memory footprint per memory unit is predictable and documented — operator can estimate infrastructure costs before tenant provisioning | Published sizing guide: memory per unit by vector dimension and metadata size | Ongoing |
| **NFR15** | Architecture must not preclude backend migration (Redis → Qdrant) — concrete implementation with clear extraction points identified, no premature interfaces | Architecture review: extraction points documented, no tight coupling to Redis-specific APIs in domain logic | Ongoing |

### Reliability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR16** | Zero memory unit loss during Redis restart | AOF persistence enabled and verified | MVP |
| **NFR17** | Ingestion pipeline state survives process restarts — pending and in-progress units resume without data loss | DAPR Workflow / Durable Task history verified | MVP |
| **NFR18** | Partial backend failure (one of three backends down) results in degraded service, not total failure — available axes continue serving results | Chaos test: kill each backend individually, verify partial results returned | Ongoing |
| **NFR19** | Failed ingestion units are never silently dropped — all failures visible via CLI status with error details and failure stage | End-to-end test with intentional failures at each pipeline stage | Ongoing |

### Integration

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR20** | MCP tool responses conform to MCP protocol specification — valid tool schemas, typed parameters, structured error responses | MCP protocol conformance test suite | P1.5 |
| **NFR21** | DAPR pub/sub integration handles CloudEvents envelope format — events from any DAPR-compatible publisher are processable | Integration test with standard CloudEvents payloads | P1.5 |
| **NFR22** | Embedding provider integration handles rate limiting gracefully — 429 responses trigger backoff without pipeline crash or data loss | Rate limit simulation test per provider | Ongoing |
| **NFR23** | CLI connects to the memory server via configurable endpoint — supports local dev (localhost), container (docker service name), and remote (ingress URL) environments | Configuration layering test across all three environments | Ongoing |

### Algorithmic Quality

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR24** | Hybrid fusion uses deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0; single-axis explain still documents axis-specific score semantics | Fusion and explain unit tests with known rankings/weights | MVP |
| **NFR25** | Fusion algorithm produces deterministic scores — same query against same data produces identical composite scores. Result ordering within the same score tier may vary. | Determinism test: 100 repeated queries, zero score variance | MVP |
| **NFR26** | Benchmark suite produces reproducible results — running benchmarks twice against the same dataset yields identical NDCG@10 scores | Reproducibility test in CI | MVP |

### Observability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR27** | Structured JSON logging with OpenTelemetry correlation IDs from DAPR trace context | Log format validation | Ongoing |
| **NFR28** | Trace context propagates across all DAPR service invocation hops — end-to-end trace from CLI/MCP through server to backend | Distributed trace completeness test | Ongoing |
| **NFR29** | Custom metrics exported via OpenTelemetry: ingestion throughput, search latency per axis, index size per tenant, pipeline queue depth | Aspire dashboard shows all metrics during local development | Ongoing |

### Documentation Quality

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR30** | Every CLI command includes --help with at least one usage example | CLI help completeness test: parse all commands, verify example presence | MVP |
| **NFR31** | README includes working Phase 1 quickstart that completes in <30 minutes on a clean machine with Docker installed (AppHost → first CLI search). Phase 1.5 EventStore 30-minute clock is a separate launch gate. | Timed walkthrough on clean environment | MVP |

### Future web, freshness, and telemetry

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR32** | When a web capability is activated, it meets WCAG 2.2 AA; supports the complete trust workflow by keyboard; provides visible focus; never communicates state by color alone; announces recovery/status changes accessibly; and is verified with the UX-defined responsive viewports plus the Epic 17 browser/assistive-technology evidence matrix (including NVDA on supported Edge/Chrome). | UX/browser evidence matrix | Future web (Epic 17+) |
| **NFR33** | Evidence Packet freshness semantics: authoritative `current`, `aging`, `stale`, and `unknown` thresholds, transitions, disclosure, and recovery actions, versioned in the Evidence Packet contract and activated per delivery surface. | Contract + surface tests | Ongoing / per surface |
| **NFR34** | Access telemetry has an explicit Platform Operations owner, configured TTL, observable purge progress, tenant-erasure mapping, bounded recovery behavior, and a dated accepted-debt decision for any unsupported retention profile. It remains infrastructure telemetry, not a tamper-evident compliance audit trail. Store choice is architecture-owned (may leave Redis). Epic 27 C1 evidence governs Production qualification. | Ops runbook + tests | Ongoing |
| **NFR35** | When a web capability is activated, on representative Evidence Packet and graph fixtures the surface targets an initial usable trust packet within 2.5 seconds, p95 local interaction response within 200 ms, cumulative layout shift no greater than 0.1, and initial route payload no greater than 256 KiB. Architecture review may revise these budgets before activation but must replace them with explicit measured values rather than removing the gate. | Measured lab evidence | Future web (Epic 17+) |

## Open Questions

1. `[NOTE FOR PM]` Expand benchmark N beyond 5–10 topics (Epic 26 follow-up) — owner and date?
2. `[NOTE FOR PM]` Pre-register ΔNDCG@10 in the benchmark README — architecture owns the number; PRD requires it exists.
3. Does `--explain` stay opt-in (FR19) while UX-DR7 wants compact trust fields on every search? Pick one before Epic 17.
4. Confirm Apache 2.0 no-relicense README sentence with Jerome if it has not been published yet.
5. Restore brief R3 (lightweight cross-case references) as Phase 2 or keep FR32 absolute?
6. Ingest-from-anywhere (cloud/git/image/video): name a phase owner or keep as explicit deferral.
7. Optional Python `ai-agent` sidecar (architecture D27): product constraint or architecture-only? See addendum.

## Assumptions Index

- `[ASSUMPTION]` Generic Marten/Wolverine/Axon zero-code remains an experiment until a named spike passes the DAPR-generic kill switch. (§ Executive Summary)
- `[ASSUMPTION]` Cloud-drive, git, image, and video ingest stay deferred until an owner names a phase. (§ Non-Goals)
- `[ASSUMPTION]` N remains 5–10 topics until Epic 26 (or successor) expands N. (§ Measurable Outcomes)
- `[ASSUMPTION]` Phase 1 graph axis may run on `contains` / case-scoped edges; `caused_by`/`correlated_with` populate from EventStore P1.5 or explicit annotation. (§ MVP Feature Set)
- `[ASSUMPTION]` NFR33 is Evidence Packet freshness (rerun SCP) and NFR35 is future-web interaction performance (remediation-batch SCP) — the two August 2026 patch sets used the same NFR33 id for different requirements.
