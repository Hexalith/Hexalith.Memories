---
title: Hexalith.Memories
status: change-controlled
created: 2026-03-22
updated: 2026-11-20
stepsCompleted: ['step-01-init', 'step-02-discovery', 'step-02b-vision', 'step-02c-executive-summary', 'step-03-success', 'step-04-journeys', 'step-05-domain', 'step-06-innovation', 'step-07-project-type', 'step-08-scoping', 'step-09-functional', 'step-10-nonfunctional', 'step-11-polish', 'step-12-complete']
inputDocuments:
  - '_bmad-output/planning-artifacts/product-brief-Hexalith.Memories-2026-03-22.md'
  - '_bmad-output/planning-artifacts/validation-report.md'
  - '_bmad-output/planning-artifacts/review-rubric.md'
  - '_bmad-output/planning-artifacts/review-adversarial-general.md'
  - '_bmad-output/planning-artifacts/review-product-brief.md'
  - '_bmad-output/planning-artifacts/review-downstream-drift.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
  - '_bmad-output/planning-artifacts/ux-designs/ux-memories-2026-09-12/DESIGN.md'
  - '_bmad-output/planning-artifacts/ux-designs/ux-memories-2026-09-12/EXPERIENCE.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-01.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-20.md'
  - '_bmad-output/planning-artifacts/prd-validation-2026-11-20/validation-report.md'
documentCounts:
  briefs: 1
  research: 0
  brainstorming: 0
  projectDocs: 3
classification:
  projectType: 'Developer Tool / API Backend'
  domain: 'AI Infrastructure / Knowledge Management'
  complexity: 'Medium-High'
  projectContext: 'Brownfield / change-controlled'
workflowType: 'prd'
---

# Product Requirements Document - Hexalith.Memories

**Updated 2026-11-20.** Phase 1 remains **no-go**. This revision reconciles the release tests, operator journeys and delivery register with the November audit, the current architecture and the McpCli migration inventory. [Release decision and evidence needed to reopen](#release-decision-record)

**Author:** Jerome
**Date:** 2026-03-22

## 0. Document Purpose

This PRD is the product-outcome contract for Hexalith.Memories: thesis, ship gates, capabilities (FR1–FR75), and cross-cutting quality (NFR1–NFR37). It is written for the maintainer, downstream UX/architecture/story owners, and reviewers of change control.

It is a **brownfield / change-controlled** document. Implementation, epics, and architecture have been running since March 2026. This file does not re-estimate the backlog. Where a later approved sprint-change proposal required a PRD amendment, that amendment belongs here; SDK pins, package counts, fusion weights, and host topology belong in architecture or `addendum.md`.

Glossary-anchored nouns are used in FRs, journeys, and success metrics. Assumptions are tagged `[ASSUMPTION]` and indexed. Open tensions are in Open Questions, not smoothed into coexistence.

## Executive Summary

**The product this PRD ships first (Phase 1, thesis):** Hexalith.Memories is an open-source, DAPR-native memory server for teams and their LLM agents. A developer ingests files and URLs into team-scoped **cases** inside an isolated **tenant**, then runs one search that fuses syntactic (BM25), semantic (embedding), and graph retrieval and explains every score. Tenant isolation is a hard gate, not a feature flag.

The three-axis fusion is the core thesis and it is falsifiable: if hybrid retrieval does not beat BM25+semantic on 80% of topics under the thesis-gate protocol in Measurable Outcomes, the named kill-switch actions execute. Weighted RRF is the fusion decision (NFR24); numeric weights live in architecture. In Phase 1 the graph axis runs on the edges file/URL ingest can create — `contains`, `references`, `annotates`, and `caused_by`/`correlated_with` only where ingested metadata already carries CausationId/CorrelationId (FR46). Phase 1 does not claim causal completeness.

**Phase 1 (thesis) onboarding:** under 30 minutes from a clean machine with Docker to first CLI search result on file/URL ingest (NFR31). **Phase 1.5 (launch) onboarding:** under 30 minutes from `dotnet add package Hexalith.Memories.EventStore` plus DAPR subscription to first search on auto-indexed events. Those are two clocks; they are not interchangeable.

**Phase 1.5 abstract (launch, not thesis):** for developers on Hexalith.EventStore, the sequenced bet is queryable causality. Add the package, subscribe to DAPR topics, and events are indexed with causal chains (CausationId/CorrelationId as graph edges) and dual embeddings (payload + natural-language description). An agent asks *"What led to the API redesign?"* and gets a sourced, gap-aware causal chain rather than a pile of documents. That answer is what the launch gates L1–L3 test; it is not what the thesis gate tests. Schema evolution requires handler registration; that is not "zero configuration." Non-EventStore DAPR publishers are a later adapter path, not the EventStore-equivalent beachhead. `[ASSUMPTION: generic Marten/Wolverine/Axon zero-code remains an experiment until a named Phase 1.5/2 spike passes the DAPR-generic kill switch.]`

Teams organize knowledge in case/folder memory containers where documents and, from Phase 1.5, events accumulate into shared, searchable knowledge (discussions are Phase 2). Every memory unit tracks whether its metadata was set by a human or inferred by AI, with a confidence score. Tenant isolation is an MVP hard gate (NFR8): tenant-scoped backend principals plus tenant-scoped indexes, with zero cross-tenant leaks. The isolation *mechanism* (ACL users, resolvers, index names) is architecture-owned; this PRD owns the outcome.

The system runs on DAPR, starts on Redis (RediSearch + Vector Search + FalkorDB), with architecture designed to support backend portability (concrete implementation first, extraction points identified for future migration). Topology, Aspire, OpenBao, and package inventory are recorded in `addendum.md` and architecture — they are not additional product surfaces.

CLI is the operational superset (target state; today MCP exposes ingest while the CLI `ingest` group is a placeholder). MCP is the agent subset (search, ingest, traverse, case-info) and ships as Phase 1.5. Surfaces are capability-aligned, not 100% feature-parity. `[NON-GOAL for MVP]: MCP, EventStore CloudEvent auto-index, application-facing REST search UI, briefings, discussions, and memory diffing.`

**Release decision, 2026-11-20. Phase 1 remains no-go.** The project missed the October 31 prerequisite checkpoint. Two independent G1 reviewers still need to be named. The December 1 decision is unreachable on the recorded evidence. January 1 was the derived Phase 1.5 date and has no qualifying launch verdict. The Administrator owns the single permitted date reset once reviewer availability, corpus preparation, the target CLI delivery plan and engineering capacity support a new date. [Approved audit follow-up](sprint-change-proposal-2026-11-20.md)

See the [release decision record](#release-decision-record) for the status of each gate and the consequences of a failed or missing run.

### What Makes This Special

**Phase 1 proves hybrid retrieval plus non-retrofittable isolation.** The graph mechanics — typed edges, traversal, gap markers, chronological ordering (FR46–FR52) — ship in Phase 1 because they are the graph axis; what Phase 1 cannot promise is that the graph *contains* causal edges for a folder of files. The sequenced bet — queryable causality from EventStore conventions — is Phase 1.5, not the thesis gate. Event-sourced systems already capture *why* things happen; Memories makes CausationId/CorrelationId queryable once the EventStore product integration ships: *"What happened because of this deployment?"* walks the graph. Happy path is subscription plus conventions, not zero mapping and zero configuration.

Phase 1.5 tests whether EventStore CloudEvents make causality searchable. For domain writes, FR13 requires an EventStore acknowledgement before projection. Cases, tenants, and memory-unit annotations and deletions use EventStore today. File/URL ingestion and CloudEvent ingestion still need that durable commit at intake (Stories 34.1 and 34.2). Epic 28 tracks the separate dependency on EventStore packages and runtime.

Two additional differentiators compound later:

- **Team-scoped collaborative memory** — case/folder containers in MVP; threaded discussions, memory diffing, and onboarding briefings are Phase 2. Do not market them as unique until they ship.
- **Integrated system over duct tape** — three-axis retrieval, case-scoped graph, tenant isolation, confidence tracking, and async ingestion work together. Hybrid-search and GraphRAG incumbents (Elasticsearch/OpenSearch hybrid, Azure AI Search, Weaviate, Vespa, Microsoft GraphRAG, LlamaIndex PropertyGraphIndex, Neo4j LLM graphs) are the comparison set, not only Mem0/Zep/LangChain.

## Project Classification

- **Project Type:** Developer Tool / API Backend (NuGet packages + DAPR service; target Hexalith-owned CLI and MCP access through `Hexalith.McpCli`). The existing Memories CLI and MCP server are obsolete migration compatibility until their operation inventory and parity gates pass.
- **Domain:** AI Infrastructure / Knowledge Management
- **Complexity:** Medium-High — driven by three-axis query fusion, DAPR workflow ingestion, multi-tenancy with tenant-scoped isolation, and EventStore domain + product integration
- **Project Context:** Brownfield / change-controlled (greenfield thesis recorded March 2026; implementation and epics are the living work breakdown)
- **License:** MIT (decision, Jerome, 2026-09-08 — supersedes the March 2026 Apache 2.0 decision; matches `LICENSE`, file headers, and `PackageLicenseExpression`). Public README commitment: the project will not switch to a restrictive license.

## Glossary

Downstream workflows and readers must use these terms exactly.

- **Memory unit** — The stored unit of knowledge (document chunk, event, annotation) owned by exactly one **Case**.
- **Case** — Team-scoped container of memory units and case-scoped graph edges. Not an authorization principal.
- **Tenant** — Isolation boundary. A user’s authenticated tenant claims authorize ordinary product access. A trusted workload needs an explicit grant for the active tenant. Operator-authorized lifecycle work is scoped to one tenant and its enumerated operation under AD-5.
- **Axis** — A retrieval method: syntactic (BM25), semantic (embedding), graph (traversal/proximity). `nl` is an optional extra semantic score on the natural-language-description embedding, not a fourth marketing axis.
- **Hybrid / three-axis** — Fusion of *available* axes for a query via weighted reciprocal-rank fusion (RRF). Missing axes degrade; they do not fail the query (FR66).
- **Evidence Packet** — Cross-surface trust envelope (confidence breakdown, origin, omitted-detail handling, degradation). Its versioned state vocabulary is `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, and `pendingExpansion`; current mappers need not emit every target state yet. Concrete shape is architecture/`Contracts.V1`-owned.
- **Relevance confidence** — Composite/RRF score of query-result relevance. Not factual accuracy.
- **Metadata confidence** — Per-field origin (`human-declared` vs `ai-inferred`) score on a memory unit.
- **Edge confidence** — Default or promoted strength of a typed graph edge (`caused_by`, `correlated_with`, `references`, `contains`, `annotates`).
- **Access telemetry** — Per-tenant search/access logs (FR67, NFR34). Not a tamper-evident audit trail. The word "audit" is not used for this capability anywhere in this PRD.
- **Member** — Tenant-scoped case membership metadata (FR28–FR29). Does not grant authorization in the current phase. Its Phase 1 outcome is attribution: members appear in case listings and the case activity feed (FR36), and membership is the seed for per-unit ACLs in Phase 3.
- **Ingestion state** — Exactly one of `pending`, `extracting`, `embedding`, `projecting`, `indexed`, `failed` (Async Ingestion Pipeline). Retry, dead-letter, and repair are detail fields, not states. The V1 JSON wire values remain exactly lowercase `queued`, `extracting`, `embedding`, `indexing`, `indexed`, `failed`; product labels map `pending`≙`queued` and `projecting`≙`indexing`. They remain unchanged until a versioned breaking contract.
- **Thesis gate / diagnostic run** — The *thesis gate* is G1 under the protocol in Measurable Outcomes (N ≥ 50, BM25+semantic control, ΔNDCG@10 ≥ 0.02, κ ≥ 0.6). A *diagnostic run* is any benchmark execution that does not meet that protocol (including the shipped N=8 suite); it may inform, it cannot pass the gate.
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

| Persona | Success Criterion | Measurement | Target | Phase |
|---|---|---|---|---|
| **Alex (Developer)** | Onboards without hand-holding (thesis) | Time from README/AppHost quickstart to first CLI search on file/URL ingest | <30 minutes — gate G3 (NFR31) | 1 |
| **Alex (Developer)** | EventStore happy path (launch) | Launch stopwatch (Measurable Outcomes, L2) | <30 minutes — gate L2 | 1.5 |
| **Alex (Developer)** | Ships AI features using Memories | Projects referencing `Hexalith.Memories.Client.Rest` or `Hexalith.Memories.EventStore` | nuget.org download counts plus self-reported adopters (no public dependency graph for private projects) | 1.5+ |
| **Alex (Developer)** | Trusts the system enough to ship | Deploys an application using Memories to production | Within 60 days of first use | 1.5+ |
| **Developer / agent query** | Hybrid beats the realistic alternative | Thesis-gate protocol: hybrid vs BM25+semantic, NDCG@10 per topic | ≥80% of topics win by ΔNDCG@10 ≥ 0.02 — gate G1 | 1 |
| **LLM Agent** | Respects token budget | Response size stays within caller-specified limits | 100% compliance on budget-constrained queries — gate L1 | 1.5 |
| **LLM Agent** | Low latency | Search-to-response time at 10 concurrent queries/tenant | NFR1–NFR3 (no separate cached/cold budget; NFR7 is process boot, not cache warmth) | 1 (CLI), 1.5 (agent surface) |
| **Marcus (Team Lead)** | Instant case context | New member asks "brief me on this case" and gets accurate, sourced answer | Narrative only until briefing ships; not a Phase 1 or 1.5 metric | 2 |
| **Marcus (Team Lead)** | Knowledge is visible | Cases with active memory (>10 units, accessed within 30 days) | Growing month-over-month once external teams adopt | 2 (post-launch) |
| **Kenji (Operator)** | Friction-free operations | Tenant provisioning time | Single CLI command, <5 min | 1 |
| **Kenji (Operator)** | No surprises | Cross-tenant data leaks | Zero — verified by automated isolation suite — gate G2 | 1 |

### Business Success

These are **signals, not gates**. Sources: GitHub repository insights (stars, contributors, issues, discussions); nuget.org package statistics (downloads); self-reported adopters for "EventStore integration users" (there is no public dependency counter for private projects); manual directory check for MCP listing.

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

Detailed performance targets, verification methods, and phase tags are defined in the **Non-Functional Requirements** section (NFR1–NFR37). Key hard gates: search latency NFR1–NFR3, zero cross-tenant leaks (NFR8), no loss of EventStore-committed units across a Redis restart (NFR16).

### Measurable Outcomes

**The Three-Axis Kill Switch (thesis gate):**
Hybrid retrieval must beat BM25+semantic on a frozen, labelled corpus under the protocol below. 80% of topics is the hard line, not a stretch goal. The numbers in this protocol are the contract; any benchmark documentation restates them, it does not own them. Handoff: the shipped `ThesisValidation_HybridOutperforms80Percent` test must be renamed to a diagnostic; a passing run of it is not G1 evidence.

**Thesis-gate protocol (not yet run at this specification):**
- **Population:** N ≥ 50 topics. A representative mix of developer/agent tasks on the Phase 1 corpus (files/URLs/cases with real, not synthetic, content). Do **not** filter the suite to "queries that require all three axes." Thesis-stress queries are a pre-registered diagnostic slice of **≤ 20% of N**, labelled as such before scoring. The frozen corpus records edge counts by type and by source (explicit / metadata-carried / AI-inferred); if explicit + metadata-carried edges are **< 30%** of non-`contains` edges, the run is recorded as testing *similarity-graph fusion* and the Executive Summary thesis sentence must say so. `[ASSUMPTION: 20% and 30% set in this Update; the PRD owns them and may revise them before the label freeze, never after.]`
- **Embeddings:** vectors for the frozen corpus and topics are computed once, cached, and hashed together with the corpus; every scoring run reads the cache. This is what makes NFR26 achievable on real embeddings whose provider is not bit-stable across model updates.
- **Control:** The unit of the 80% is a topic. A topic counts as a hybrid win when hybrid NDCG@10 exceeds the **BM25+semantic (two-axis RRF)** control by at least the pre-registered **ΔNDCG@10 ≥ 0.02**. Two aggregate guards also apply: mean ΔNDCG@10 over all N ≥ 0.00, and no single topic regresses by more than 0.10. A run that clears 80% and fails either guard fails G1. Single-axis runs are diagnostics only. The two-axis control is not implemented in the shipped suite; it is a prerequisite of G1 (FR25, status partial).
- **Graph seeding (decision 2026-09-08):** G1 runs hybrid exactly as a developer gets it from `search query` — the current public search grammar has no start-node input. Therefore FR17 hybrid seeds the graph axis from the union of the top-5 syntactic and top-5 semantic candidates and traverses from them (depth ≤ 2). The shipped `HybridSearchService` skips the graph axis when no start node is supplied — that is a gap the G1 date depends on (FR17, status partial), not a protocol allowance. Explicit graph starts belong to `traverse` / `traverse_relations`, not an invented search `--from` flag.
- **Ground truth.** Jerome and two independent reviewers each grade every pooled topic–unit pair on the same 0–3 relevance scale. Each reviewer pair must reach unweighted Cohen’s κ ≥ 0.6 on the complete set of raw grades. The median of the three grades is the final judgment. Freeze the pool, raw grades, final judgments and scorer version before scoring either ranking. [Grading and pooling procedure](addendum.md#g1-judgments)
- **Automated scoring.** For each topic compute NDCG@10 with gain `2^grade − 1` and discount `1/log2(rank + 1)`. All N registered topics enter the win fraction and both aggregate guards. A topic with zero ideal gain scores zero for each arm. A missing judgment in either top ten prevents a qualifying run.
- **Dispute resolution:** After scoring, labels are immutable for that run. A reviewer may annotate a disagreement; if the annotations justify relabelling, the run is discarded, the label set is re-frozen, and a *new* run is recorded with the reason. A run's pass/fail is never changed by editing labels after the fact.
- **Fallback:** If independent reviewers are unavailable, automated scoring may still run but **does not** satisfy the thesis hard gate — it is a documented downgrade of gate confidence, not a substitute.

**Evidence to date (diagnostic, not the gate):** The Epic 2 / Epic 26 benchmark (`tests/Hexalith.Memories.Benchmarks`, last run 2026-07-16) reports 8/8 hybrid wins. It does not satisfy the protocol above: N=8, every query is tagged as requiring all three axes, the corpus and labels are synthetic, the control is the best *single* axis rather than BM25+semantic, and the win rule is any positive ΔNDCG@10. It is a reproducibility and regression check (NFR26), and the PRD records it as such.

**If the threshold is not met, the kill switch executes these actions, in order, with no "or":** (1) stop fusion R&D as default hybrid; (2) remove graph from the default `--axis hybrid` — hybrid becomes BM25+semantic, and graph stays available only as an explicit axis and through `traverse` (FR47); (3) FalkorDB is retained for explicit traversal and is cut from general search scoring; (4) README and executive summary reposition to "BM25+semantic search with an explicit graph" *before* any other decision; (5) the Phase 1.5 launch gates L1–L3 are **not evaluated** — the launch decision date is cancelled and a sprint-change proposal must re-scope the product before a new date is set; (6) re-scope and re-estimate. "Reposition and keep building the same system" is not a pass.

**Causal Chain Completeness:**
Freeze at least 20 labelled chains. Count each distinct eligible event once across all chains. At least 95% must return every expected path within the event’s case, including the labelled IDs, edge types and directions in order. Incomplete and unavailable traversals count as failures. [Fixture, eligibility and scoring](addendum.md#l3-event-denominator)

**Phase 1 thesis go/no-go (CLI proof of hybrid + isolation):**

| # | Criterion | Requirement |
|---|---|---|
| G1 | Hybrid vs BM25+semantic passes the thesis-gate protocol above (N ≥ 50, ΔNDCG@10 ≥ 0.02, κ ≥ 0.6) | Must pass |
| G2 | Zero cross-tenant data leaks under NFR8. An explicit conflict between authenticated and requested scope must be rejected before any restricted output or product side effect. | Must pass |
| G3 | Starting from the NFR31 clean machine, follow the README manual path through AppHost, create a tenant and case, ingest a file or URL, and obtain the first correct search result through enrolled `Hexalith.McpCli` operations in <30 minutes. | Must pass. Story 35.7 records the cold run. |
| G4 | Case ownership/isolation tests (FR32–FR34/NFR8): case-scoped queries never escape their case; tenant-wide hybrid fixtures span cases with mandatory attribution while every graph seed, node, edge, and path remains case-local | Must pass |
| G5 | On native REST and the enrolled Phase 1 McpCli CLI JSON search path, the same fixed inputs produce identical scores, order and canonical Evidence Packet bytes under NFR24–NFR26. Repeat 100 times. Compare the shared offline serializer fixture as a component check. End-to-end MCP parity belongs to L1. | Must pass |
| G6 | MVP contract closure: every MVP-active requirement not marked **Verified against its current wording** — including not-started, partial, hardening, implemented-without-recorded-evidence, and re-verification rows — and every architecture-critical active-foundation gap has a named current re-runnable evidence path. Genuinely phase-inactive surfaces are explicitly outside MVP scope and earn no gate credit. A risk acceptance records accountability only; no product/architecture exception substitutes for evidence. Every such gap has an owner and a tracking entry in `sprint-status.yaml`; during an approved tracker freeze, AD-20 requires the architecture ledger to record the tracker obligation, freeze reference, and review date, and suspends only the tracker conjunct. | Must pass |

All six gates are hard gates; there is no soft tier. Phase 1 does **not** require MCP, EventStore CloudEvent auto-index, or causal-chain completeness.

**Phase 1.5 launch go/no-go (agent + EventStore product integration):**

| # | Criterion | Requirement |
|---|---|---|
| L1 | On N ≥ 10 separately frozen topics held out from G1 and all tuning, every topic produces at least one budgeted MCP response and every response fits its requested budget. On at least 80% of all registered topics, the agent cites a unit returned in that interaction with a frozen L1 relevance grade ≥2 for that topic. Verify end-to-end MCP parity with the NFR25 reference vectors. | Must pass. Freeze the independent L1 labels and the reference agent configuration before the run. [Evidence procedure](addendum.md#l1-holdout) |
| L2 | EventStore product integration: launch stopwatch below completes in <30 minutes on a clean machine as defined in NFR31 | Must pass |
| L3 | At least 95% of distinct eligible events in the fixture of ≥20 frozen chains return all their labelled paths under the protocol above. | Must pass |

**Launch stopwatch (L2).** Use the NFR31 clean profile. Before timing, stage only the pinned source checkout and the provider key. That revision must contain the unchanged EventStore starter service and its sample AppHost. Begin with empty package and container-image caches, no installed project tools, a stopped stack and no configured Memories package, subscription or app secret. Start the clock immediately before step 1. Count all restores, tool installs, configuration, image pulls and startup in the elapsed time. [Starting-state manifest](addendum.md#l2-starting-state)

1. Run `dotnet add package Hexalith.Memories.EventStore` in the pinned starter service.
2. Add the DAPR pub/sub subscription (topic + `/events/ingest` route) per the getting-started guide.
3. Provide the embedding-provider secret through the DAPR Secrets API (OpenBao dev instance in the sample AppHost).
4. Boot the sample stack (`samples/02-eventstore-integration/`, `dotnet run --project <sample AppHost>`; until that sample exists the stopwatch cannot be run and L2 is not evaluable).
5. Invoke the starter service to commit a new domain event and record its EventStore event ID. Run the sample’s documented search. Stop the clock when the response attributes a returned unit to that exact event.

#### Release decision record

Disposition as of 2026-11-20.

| Decision | State | Owner | Decision date | If it fails |
|---|---|---|---|---|
| Thesis increment (Epics 0–8) | The tracker records Epics 0–8 as done. Target CLI delivery, default graph seeding (FR17) and the two-axis benchmark control (FR25) remain open. | Jerome | Closed in tracking on 2026-07-16 | Gate qualification still requires the runs below. |
| Gate prerequisites | The checkpoint required current evidence or a registered, independently verified owner story for each prerequisite. The Administrator stewards the corpus. Two independent external reviewers remain unnamed, blocking label freeze and the qualifying G1 run. Epic 32 target operations, CLI accessibility qualification (NFR37) and the final verdict still lack qualifying evidence. | Jerome for planning and evidence, Administrator for corpus and reviewers, Architecture for its evidence ledger | **2026-10-31 — missed** | The December 1 decision is unreachable on the recorded evidence. A registered story counts only toward planning. G6 requires current, re-runnable evidence. |
| Thesis gate G1–G6 | G1 has no qualifying run. G2 needs current tenant-principal isolation evidence. G3 awaits enrolled target operations and a timed cold run. G4 and G5 need evidence against their current wording. G6 remains open for MVP requirements and architecture-critical gaps without current evidence. | Jerome owns the verdict. Administrator owns the date reset. | **2026-12-01 — unreachable.** No replacement approved. | A measured G1 failure triggers the six fusion kill-switch actions above. A G2–G6 failure blocks release and launch pending remediation or approved rescoping. Without a run there is no verdict or launch. One sprint-change reset is permitted with a recorded reason. A second miss counts as failure. |
| Phase 1.5 launch L1–L3 | Epics 9–10 delivered preview code. L1–L3 remain unevaluated. L2 lacks the sample and a full-stack EventStore-originated publish proof (DW-713). The resource-start test remains skipped with its historical SDK rationale, although both repositories now select the same SDK. | Jerome | **2027-01-01 — previously derived.** Any revised launch date requires a sprint change. | Without a qualifying verdict, the MCP and EventStore packages remain preview and the launch stays on hold. |

**Planning evidence.** The McpCli owner completed Stories 4.4 and 4.5 and approved inventory dispositions. Story 35.7 still needs a delivered CLI path. Epic 34 has 60 reviewed story definitions. The specification for the V1 wire-name register and build guard (34.33) precedes the additive URL retry token and durable ingestion acceptance (34.1). The guard and acceptance still require implementation evidence. [Story 34.33 specification](../specs/spec-34-33-v1-wire-name-register/SPEC.md) Story 34.58 awaits a per-tenant storage budget. The successor sprint tracker remains on hold. AD-14 phasing and FR75/NFR37/G6 binding are ratified. G6 still requires evidence for the implementation. [Current readiness](implementation-readiness.md) · [Epic definitions](epics.md)

A Phase 1.5 launch failure is a no-go, not a slip. It does not pull MCP into the thesis MVP and does not re-open isolation or fusion as optional.

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Proof of Thesis — validate three-axis retrieval before launching integration surfaces. Ship the smallest thing that proves hybrid retrieval outperforms BM25+semantic under the thesis-gate protocol, with cases and multi-tenancy from day one (architectural decisions that can't be retrofitted).

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
| 6 | `Hexalith.McpCli` target CLI for the approved Phase 1 operation inventory; the **CLI surface** table records legacy compatibility verbs and required output semantics | Thesis validation tooling; shared-surface qualification required |
| 7 | Benchmark Suite (thesis-gate protocol: N ≥ 50, BM25+semantic control, ΔNDCG@10 ≥ 0.02) | Thesis validation (G1) |

**Phase 1 graph inventory (decision, 2026-09-08):** the graph *mechanics* — typed edge taxonomy, traversal with depth and edge-type filters, gap markers, chronological ordering, confidence promotion (FR46–FR52) — are MVP and shipped by Epics 1 and 4. The graph *population* in Phase 1 is what file/URL ingest can create: `contains` (case membership), `references` (explicit link or AI-inferred similarity), `annotates` (FR37), and `caused_by`/`correlated_with` only when ingested metadata already carries CausationId/CorrelationId. Automatic causal population from an event stream is Phase 1.5 (FR59–FR62). Hybrid fuses *available* axes; the PRD does not claim causal completeness on a folder tree, and the thesis-gate corpus must reflect the Phase 1 population, not a synthetic causal graph.

**Note:** DAPR infrastructure is scaffolding built as part of features 1–5, not a separate work item. README ships with MVP as the NFR31 vehicle. A help entry backed by `NotImplementedCommand`, or target grammar absent from the command tree, is not coverage — see the CLI Specification for exact delivery status.

### Phase 1.5 — Fast-Follow (launch decision date = thesis-gate/release decision date + 1 month; if either date moves, the other is re-derived by sprint change)

| # | Feature | Validates |
|---|---|---|
| 1 | EventStore product integration package `Hexalith.Memories.EventStore` (DAPR pub/sub through the Memories Server sidecar, auto-discovery, dual embedding, causal chains) | Launch gates L2 + L3 |
| 2 | Search, ingest, traverse, and case-info with token-budget awareness through `Hexalith.McpCli`; `Hexalith.Memories.Mcp` is the obsolete compatibility server pending approved parity | Launch gate L1, re-baseline on shared-surface readiness |
| 3 | Developers can diagnose EventStore handler registration and routing. Any mapping to McpCli requires separate approval. The owner deferred the legacy diagnostics (L13/L14) and withdrew the interactive exploration stub (S04) from migration scope. | Phase 1.5 EventStore diagnostics |

The Memories Server sidecar receives Hexalith.EventStore CloudEvents through DAPR pub/sub at `/events/ingest`. For Phase 1.5, one subscribed topic serves one tenant. The operator maps the subscribing component, topic and publisher authenticated through the deployment’s publishing scope to exactly one candidate tenant. Admission requires an allowlisted publisher, an explicit grant for that tenant and its authoritative `Active` state. An unknown or ambiguous channel, or an envelope that contradicts the authenticated route, is rejected before any write. The publisher-supplied `source` identifies the event for provenance and duplicate detection. Changing it cannot select another tenant. **Implementation gap, recorded 2026-10-08.** `TenantEventRoutingOptions.SourceToTenantMap` still uses source prefixes. Story 34.10 owns authenticated channel routing. The Phase 1.5 multi-tenant target uses per-tenant EventStore topics. Modules publish domain events through this subscription.

**Hard commitment:** Phase 1.5 remains the launch path for MCP and EventStore *product* integration. Its go/no-go is dated in the Release decision record; a failed gate is a no-go for those surfaces, not a slip. They are **not** pulled into the thesis MVP and isolation/fusion are **not** reopened.

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
- Thesis-only increment is not the marketed EventStore/MCP product. Mitigation: keep exec summary, README, and samples honest about phase; a failed Phase 1.5 gate is a no-go for those surfaces, not a reason to accordion MCP into the thesis MVP.
- Independent reviewer availability for benchmark scoring. Mitigation: automated scoring may run but does not satisfy the thesis hard gate without independent labels.

**Resource Risks:**
- Resource pressure may defer phase-qualified interfaces and diagnostics, but it may **not** defer tenant isolation, tenant/case validation, or the zero-leakage release gate without an approved MVP rebaseline. Engine, scoped search, minimum case bootstrap, tenant provisioning, and their fail-closed guards remain inseparable MVP foundations.

**Operational Risks:**
- Shared embedding API key exhaustion — one tenant's batch ingestion starves others' real-time ingestion. Mitigation: per-tenant embedding throttle (rate-limiter actor) plus inbound request quotas (authenticated tenant). For full provider isolation, tenants use separate API keys. Document shared-key limitation in operator guide.

## User Journeys

Durations and counts in the journeys are illustrative. Gate decisions use the G/L criteria and their recorded runs. `memories` commands and the four named MCP tools refer to the compatibility clients. Enrolled `Hexalith.McpCli` operations are the target. Use the tenant and case IDs returned by the service in place of the placeholders. The CLI specification distinguishes working commands, stubs and native routes.

### Journey 1: Alex — "Zero to First Search" (Phase 1.5 launch path)

> **Phase:** 1.5 EventStore product integration. Not the Phase 1 thesis success path — that is Journey 9.

Alex has been building a claims processing platform on Hexalith.EventStore for eight months. The system processes 50,000 events per day across six aggregate types. Last sprint, the product owner asked: "Can the AI assistant explain why a claim was denied?" Alex spent three days duct-taping Qdrant and LangChain together. It worked for the demo. Then the security review asked about tenant isolation, and the whole thing collapsed.

**Opening Scene:** Alex finds Hexalith.Memories linked from the EventStore documentation. In this launch scenario the sample guide demonstrates how to publish an event and find it in search. Alex thinks "that can't be right" and opens the getting started guide.

**Rising Action:** `dotnet add package Hexalith.Memories.EventStore` — familiar. DAPR subscription config — two lines in `appsettings.json`. Alex enters the embedding key and runs the sample AppHost pinned in the L2 manifest. The host starts the instrumented claims service alongside Memories and its dependencies. Alex invokes the instrumented claims service to commit a test event. Its EventStore publication is delivered through the DAPR subscription. This is the launch stopwatch (L2), steps 1–5.

**Climax:** Alex searches for “claim denied” under the authorized claims tenant. The compatibility example is `memories search query --tenant <tenantId> --query "claim denied"`. The result attributes the denial to the published event and its CausationId chain. In this launch scenario it takes 14 minutes. L2 still requires the timed sample run.

**Trust Deepening:** Alex repeats the search with explain enabled. The syntactic and semantic scores show the rank each match earned on its own axis. A graph edge connects the denial to the original submission. The response exposes the weights used in the composite, so Alex can tell why the claim ranked highly.

**Resolution:** By end of day, Alex commits the integration code. The duct-tape Qdrant solution gets deleted. Alex goes home on time.

**Capabilities revealed:** EventStore auto-integration, CLI search with --explain, causal chain traversal, <30 min onboarding, debug-first DX.

---

### Journey 2: Alex — "Something's Wrong" (Debug Path)

Two weeks after shipping, Alex gets a Slack message: "The AI assistant says there's no information about the Henderson claim, but I filed it yesterday."

**Opening Scene:** Alex opens a terminal. `memories search query --tenant <claims-tenant-id> --case <claims-case-id> --query "Henderson"` returns zero results. Not good.

**Rising Action:** Alex broadens the query in the same authorized scope and repeats it with `--explain`. The compatibility command `memories status telemetry --tenant <tenantId>` shows tenant-wide activity and queue depth. For the affected case Alex reads `GET /api/v1/tenants/{tenantId}/cases/{caseId}/status` and `GET /api/v1/tenants/{tenantId}/cases/{caseId}/failed-units`. A zero-match search alone cannot identify a failed ingestion.

Alex runs `memories handlers list` and `memories handlers mismatches --tenant <claims-tenant-id>`, then checks the DAPR subscription logs. The ClaimSubmitted event for Henderson was published but the Memories handler threw a serialization error — a new field added last sprint broke the auto-discovery mapping. The error message names the unknown event type and these implemented diagnostic commands.

**Climax:** Alex registers the V2 handler, triggers a replay of the missed events, and within seconds the Henderson search returns the full claim with causal chain intact.

**Resolution:** Alex adds a monitoring alert on handler registration mismatches. The debug-first DX — clear error messages, `--explain`, telemetry, handler listing, and mismatch diagnostics — turned a potential hours-long investigation into a 15-minute fix.

**Capabilities revealed:** CLI diagnostics (status, explain, handlers list), clear error messages, event replay, handler registration, debug-first developer experience.

> **Scope note:** The working handler diagnostics belong to the Phase 1.5 EventStore investigation. Event replay is a Phase 1.5 capability. Compatibility inventory rows L13/L14 are deferred for McpCli. The current CLI registers `status telemetry` only.

---

### Journey 3: Alex — "Wiring Up the AI Assistant" (MCP Integration, Phase 1.5)

Alex has the memory server running and CLI working. Now the product owner wants the team's AI assistant to use it.

**Opening Scene:** Alex opens the compatibility MCP documentation and finds four typed tools, `search_memory`, `ingest_content`, `traverse_relations`, and `get_case_info`. The following walkthrough describes those schemas. The approved McpCli inventory defers M01–M04, and Phase 1.5 acceptance requires its own qualifying surface.

**Rising Action:** Alex adds the MCP tool definitions to the AI assistant configuration. First test: "What happened with claim 4821?" The assistant calls `search_memory(tenantId="<claims-tenant-id>", query="claim 4821", case="<claims-case-id>", axes="Hybrid")` and returns results with source attribution. It works — but the response is too long, blowing past the context window.

**Climax:** Alex adds `tokenBudget=2000` to the call. The assistant calls again — same query, but the response is now concise: top-ranked results, truncated by relevance, with a note "8 additional results omitted." The assistant composes a focused answer. Alex tests three more queries, each producing grounded, sourced responses.

**Resolution:** The product owner asks "why was claim 4821 denied?" and the assistant walks the causal chain: SubmitClaim → FraudCheckTriggered → FraudScoreExceeded → ClaimDenied. Sourced, attributed, traceable. The AI feature ships to the team by end of week.

**Capabilities revealed:** MCP tool definitions, token-budget-aware responses, multi-axis search control, source attribution, assistant configuration workflow.

---

### Journey 4: Marcus — "Brief the New Person" (Phase 2)

> **Phase:** 2 (briefing, annotations-as-onboarding). Case membership metadata (FR28–FR29) may exist earlier; it does not grant authorization and does not make this journey MVP.

Marcus leads a team of seven working across three active cases. Sarah, a senior developer, left last month. Her replacement, Tomás, starts Monday. Marcus has spent every previous onboarding doing four hours of tribal knowledge transfer, walking through Confluence pages that are six months out of date.

**Opening Scene:** Friday afternoon, Marcus adds Tomás as a member of three cases in the Engineering tenant. For Project Alpha he calls `PUT /api/v1/tenants/{tenantId}/cases/{caseId}/members/{memberId}` with Engineering’s tenant ID, Project Alpha’s case ID and Tomás’s member ID. He repeats it for Project Beta and incident response. The membership records attribution. Tomás already has authenticated access to Engineering.

**Rising Action:** Monday morning, Tomás opens the AI assistant and types: "Brief me on project-alpha." The assistant calls `search_memory(tenantId="<engineering-tenant-id>", query="project overview and recent activity", case="<project-alpha-case-id>")` and composes a narrative: the project started eight months ago as a payment processing rewrite, hit a critical incident in February when the gateway provider changed their API, pivoted to a dual-provider architecture, and is currently in testing. Key decisions, who made them, and why — all sourced from events, documents, and team discussions in the case memory.

**Climax:** Tomás asks: "What led to the dual-provider decision?" The assistant walks the causal chain: GatewayTimeoutEvent → IncidentDeclared → ArchitectureReviewDiscussion → DualProviderProposal → ApprovedByMarcus. Tomás understands not just *what* the architecture is, but *why* it exists. In 20 minutes, not four hours.

**Recovery beat:** Tomás notices the briefing says "approved by Marcus on February 12" but the PR was actually merged on February 14. He clicks "show sources" — the approval event is dated February 12, but the implementation PR landed two days later. The briefing was accurate about the *decision*, just not the *implementation*. Tomás flags the discrepancy, and the memory unit gets an annotation clarifying the timeline. The system is self-correcting.

**Resolution:** Marcus checks in after lunch. Tomás is already reviewing PRs with full context. Marcus didn't spend a single minute on knowledge transfer. He realizes he has his Friday afternoon back for the first time in a year.

**Capabilities revealed:** Case member management, case briefing via MCP, causal chain narrative, source attribution with verification, memory correction annotations, knowledge health visibility.

---

### Journey 5: Kenji — "New Tenant, No Drama" (MVP)

Kenji manages the DAPR infrastructure for three business units. The compliance team just approved a fourth business unit for the AI platform, and they need their own isolated memory space by Thursday.

**Opening Scene:** Kenji requests provisioning for the Compliance Unit. AD-23 requires the platform to issue the tenant ID. The current native route accepts a caller-supplied ID. Kenji provisions through `POST /api/v1/tenants`, with status at `GET /api/v1/tenants/{tenantId}/provision-status/{instanceId}`. The legacy CLI exposes `tenant list` only. Kenji retains the authorized tenant ID and provisioning receipt for subsequent calls. NFR8 requires tenant-scoped enforcement in both data stores.

**Rising Action:** Kenji calls `POST /api/v1/tenants/{tenantId}/verify` and reads each diagnostic for indexes, syntactic and semantic isolation, and graph isolation. A check against an empty tenant can pass even when isolation is broken because there are no records to expose. For NFR8, Kenji needs populated fixtures and independent principals whose cross-tenant reads and writes are denied on each backend. `memories consistency verify --tenant <tenantId>` answers a different question about projection divergence.

**Failure beat:** A new intern runs `memories search query --tenant <operations-tenant-id> --query "test"` using a bearer authorized only for `<compliance-tenant-id>`. The server rejects the explicit mismatch before retrieval. The CLI names the authenticated tenant and returns no restricted data. Kenji can see the denied operation and outcome in authorized access telemetry.

**Resolution:** Kenji's Thursday deadline was met in under 10 minutes. The monitoring dashboard shows all four tenants healthy, isolated, with clear resource consumption per tenant.

**Capabilities revealed:** Native tenant provisioning, isolation verification (NFR8), boundary violation errors, access telemetry.

---

### Journey 6: Kenji — "Time to Scale" (Phase 3)

Six months after the compliance tenant launch, bu-compliance has grown to 2 million memory units. Redis memory is climbing past the comfort zone.

**Opening Scene:** Kenji runs `memories backend assess --tenant <compliance-tenant-id>` and sees the recommendation: "Consider Qdrant migration for tenants exceeding 1M units. Current memory usage: 12.4GB, projected 30-day growth: 2.1GB."

**Rising Action:** `memories backend migrate --tenant <compliance-tenant-id> --target qdrant --dry-run` shows the migration plan: estimated time, data volume, steps, and rollback procedure. No surprises.

**Climax:** Kenji executes the migration during a maintenance window. Migration completes with zero downtime — queries are served from Redis until the Qdrant index is ready, then traffic switches. The compliance team's AI assistant doesn't notice anything changed.

**Resolution:** Kenji has a clear, repeatable scaling playbook. The next tenant that hits the threshold gets the same treatment.

**Capabilities revealed:** Backend assessment tooling, migration dry-run, Redis → Qdrant migration path, zero-downtime backend swap.

> **Scope note:** This journey maps to Phase 3 (IMemoryIndex Qdrant implementation). Not an MVP deliverable.

---

### Journey 7: LLM Agent — Technical Integration Path

This journey describes the four working compatibility MCP tool schemas. Inventory rows M01–M04 are deferred for McpCli. Until enrolled equivalents exist, this walkthrough supplies behavior to preserve and the target end-to-end gate remains outstanding.

**Integration Setup:**
1. Application registers MCP tool definitions: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`
2. Agent receives the current tool schemas: `search_memory` requires `tenantId` and `query`, with optional `case`, `axes` (`Syntactic`/`Semantic`/`Nl`/`Hybrid`), `tokenBudget`, and `explain`; graph-detail work uses `traverse_relations`

**Query Cycle:**
1. User prompt arrives requiring organizational context
2. Agent calls `search_memory(tenantId="<engineering-tenant-id>", query="what led to the API redesign?", case="<project-alpha-case-id>", tokenBudget=2000, axes="Hybrid")`
3. Memory server executes hybrid search, fuses available axes under FR17, and truncates to the token budget
4. For graph detail, the agent calls `traverse_relations(tenantId="<engineering-tenant-id>", from="<result-id>", caseId="<project-alpha-case-id>")`
5. Responses include ranked memory units, source attribution, relevance confidence, typed causal links, and explicit gaps/degradation
6. Agent composes a response grounded in sourced memory, citing specific documents and events

**Edge Cases and Graceful Degradation:**
- **Token budget exceeded:** Server truncates results by relevance rank, includes "X additional results omitted" count, names omitted detail groups, and provides deterministic expansion handles
- **No results:** Response classifies true absence versus empty/wrong scope, incomplete or delayed ingestion, filters, stale evidence, authorization refusal, and backend degradation, then suggests only safe available actions
- **Ambiguous case:** Server returns case disambiguation options
- **Stale context:** The Evidence Packet freshness state (NFR33: `current` / `aging` / `stale` / `unknown`) marks the unit; the agent caveats accordingly. Relevance confidence never encodes age.
- **Memory server unreachable:** Agent receives timeout error with retry-after header. Agent should fall back to informing the user that organizational memory is temporarily unavailable rather than hallucinating context
- **Redis degraded (partial results):** Response includes `"degraded": true` flag and which axes were unavailable, so the agent can caveat its answer: "Based on text and semantic search only — graph traversal temporarily unavailable"

**Success criteria:** Agent produces responses that are sourced, attributed, within token budget, and causally grounded when graph data is available. On degradation, agent transparently communicates limitations rather than silently producing lower-quality answers.

**Capabilities revealed:** MCP tool definitions, token-budget-aware responses, multi-axis search control, source attribution, confidence scoring, graceful degradation signaling.

---

### Journey 8: Priya — "I Need to Understand This Case" (Phase 2 downstream application)

Priya is a claims adjuster at an insurance company. She handles 40 cases per week. She's never heard of Hexalith.Memories — she uses a web application that Alex's team built on top of it.

**Opening Scene:** Priya stares at claim #7293 in her queue and feels her stomach tighten. Complex escalation, three contractor assessments, a coverage dispute, and the previous adjuster left the company with no handover notes. She has a call with the claimant in 10 minutes.

**Rising Action:** She types into the application's search bar: "What happened with claim 7293?" The response is a chronological narrative: initial claim filed January 12, first assessment January 18 (contractor found $12K damage), coverage dispute raised January 25 (policy exclusion for pre-existing conditions), second assessment February 3 (independent contractor confirmed $9.5K new damage), escalation to senior adjuster February 10, senior adjuster approved partial coverage February 15.

Priya asks: "Why was partial coverage approved instead of full?" The causal chain returns: the independent assessment distinguished $9.5K new damage from $2.5K pre-existing damage, the policy exclusion applied only to pre-existing, and the senior adjuster's approval note referenced the independent assessment as the deciding factor.

**Verification beat:** Before the call, Priya clicks "show sources." Each claim in the narrative links to the actual document or event: the contractor's assessment PDF, the policy exclusion clause, the senior adjuster's approval with timestamp and signature. Relevance confidence shows 0.95 for the causal chain — the UI labels it "relevance, not verified fact" (Glossary). Priya reads the approval note herself — it matches the narrative. She's not trusting the AI blindly, and 0.95 is not why she relaxes; she trusts it because she can verify every link.

**Climax:** Priya calls the claimant with complete context. She can explain exactly what was covered, why, and cite the specific evidence. The claimant asks a follow-up about the contractor selection — Priya checks the sources in real time and has the answer in seconds.

**Resolution:** The call takes 8 minutes instead of 30. Priya moves to the next case. Her stomach unknots.

**Capabilities revealed:** REST API consumption (via Alex's app), chronological narrative composition, causal chain explanation, source attribution with verification links, confidence scoring for end users, cross-document relationship traversal.

---

### Journey 9: Alex — "The First Case" (Empty State)

> Journey 9 describes NFR31. The named native routes and compatibility commands give Alex a working manual path today. G3 separately requires a clean-machine run using enrolled McpCli operations in under 30 minutes. L03 search is deferred and L06, the legacy guided wizard, was withdrawn from migration.

Alex has the stack running. Redis is up, FalkorDB is up, the DAPR service is healthy. But there's nothing in it yet.

**Opening Scene:** Alex searches for “water damage” using the issued ID of the Pilot tenant and finds no matches. A query alone cannot establish that the tenant is empty. The required hint lets Alex check the selected case and filters, try broader terms, or inspect a known unit. For a confirmed empty scope it points to the file or URL ingestion route. The current compatibility hint still advertises the `ingest` stub. Alex can follow the native steps below.

**Rising Action:** Alex uses the authorized tenant ID issued by the platform at the AD-23 target. On the native path, `POST /api/v1/tenants/{tenantId}/cases` creates “Claims Pilot.” Alex keeps the case ID from the response and uses it in the ingestion request. “Claims Pilot” is the display name. The AD-23 target issues an opaque case ULID.

Alex submits a sample claims file through `POST /api/v1/ingest` or a source URL through `POST /api/v1/ingest/url`, then follows the receipt at `GET /api/v1/ingest/{instanceId}`. Case status shows the indexed count. For the separate 47-document batch example, `POST /api/v1/ingest/directory` takes a directory on the server, and `GET /api/v1/ingest/batches/{batchId}` reports progress.

**Climax:** Alex reruns `memories search query --tenant <tenantId> --case <caseId> --query "water damage"` using the saved IDs. The compatibility CLI finds the sample and shows its source. In G3 the corresponding enrolled search must return the correct file or URL. The Phase 1.5 event-publish clock belongs to Journey 1.

**Resolution:** Alex shares the README quickstart experience in the team channel. Two other developers set up their own cases by end of day.

**Capabilities revealed:** An actionable empty result, authorized case creation, file or URL ingestion, progress by receipt, and a first search with source attribution. The README’s warm-stack wizard remains a compatibility walkthrough. Enrolled setup and search must support the manual target without it.

---

### Journey 10: The Contributor — "From Bug Report to First PR"

Dani is a .NET developer at a fintech startup. They adopted Hexalith.Memories three months ago for their transaction monitoring system. It's been working well, but Dani hit a rough edge: the CLI's `memories search query --explain` output doesn't show which embedding model was used for the semantic match, making it hard to debug relevance issues when testing different providers.

**Opening Scene:** Dani opens a GitHub issue: "Feature request: show embedding model name in --explain output." They include a concrete use case and a mock of what the output should look like.

**Rising Action:** Jerome labels the issue `good-first-issue` and points Dani to the evidence contract, its server mapping, and the target presenter that consumes it. Dani builds the repository and follows an existing result through its tests. They propose the explanatory field on the shared response and add a fixture that checks how the active surface presents it. New Memories functionality follows the approved McpCli enrollment path.

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

Hexalith.Memories provides **interpretive infrastructure**. It stores source evidence and derives embeddings, causal links and relevance scores. The consuming application decides how to act on those interpretations.

**Three-tier responsibility model:**

| Layer | Responsible for | Example |
|---|---|---|
| **Storage** | Data durability, isolation, encryption at rest | EventStore truth plus Redis/FalkorDB projections |
| **Interpretation (Memories)** | Accurate embeddings, correct causal chains, documented score semantics, complete traversal of *indexed* edges with explicit gap markers for missing nodes (FR49) | Relevance confidence is not factual reliability; see Glossary |
| **Application** | Decisions based on interpretations, compliance, user-facing representations, legal obligations | Denying a claim based on a causal chain, GDPR compliance |

Memories provides the primitives that *enable* compliance:

- **Tenant deletion.** An authorized operator requests deletion through `DELETE /api/v1/tenants/{tenantId}`. FR39 permits completion only after product projections are purged, EventStore-held tenant content is irreversibly inaccessible with verification, and a durable access-telemetry erasure handoff is recorded. Replay, restart, and restore cannot resurrect the content; the deleted tenant ID cannot be reused. Retained opaque access telemetry follows NFR34's TTL rather than being presented as content or legal-audit evidence. **Limitation:** Cross-references to that tenant's data in *other* tenants' memory units are the application's responsibility to handle. The compliance guide must document this explicitly.
- **Tenant isolation** (tenant-scoped backend principals and indexes, NFR8) ensures no cross-tenant data leakage, a prerequisite for downstream compliance. It is a shared-cluster isolation tier, not separate processes or volumes per tenant (see `addendum.md`).
- **Access telemetry** of queries, ingestion, and mutations is provided as infrastructure telemetry. This is *not* a tamper-evident audit trail — it does not guarantee append-only storage, integrity verification, or retention compliance. Applications requiring certified audit trails must implement their own on top of this telemetry.

**Compliance enablement documentation** must include:
- "Building Compliant Applications on Memories" guide showing how tenant delete maps to erasure, access telemetry maps to access records, case isolation maps to data segregation, memory unit metadata tracks data lineage
- "Limitations of Infrastructure-Level Deletion" section covering cross-reference scenarios
- **Legal disclaimer:** "This guide provides architectural patterns, not legal advice. Consult qualified legal counsel for your specific regulatory requirements."
- "Security Posture for Auditors" section providing architecture documentation, security design rationale, and dependency analysis to support enterprise audits. Hexalith.Memories is open-source software — SOC 2, ISO 27001, and similar certifications apply to organizations operating services, not to software artifacts. Deploying organizations include Memories in *their* audit scope.

### AI Reliability and Trust Boundaries

**Confidence Score Semantics:**
A native single-axis search returns the measure for that axis. For each contributing axis, hybrid search reports a score calculated from the unit’s rank. Rank one scores 1.0. The result also includes a weighted composite. These values measure relevance to the query.

| Axis | Native single-axis score | Public hybrid per-axis score |
|---|---|---|
| Syntactic | RediSearch BM25 relevance. The native score can exceed 1.0. | Rank on the syntactic list, normalized to its top rank. |
| Semantic | Cosine similarity computed as `clamp(1 − distance, 0, 1)`. | Rank on the semantic list, normalized to its top rank. |
| NL (Phase 1.5, event units only) | Cosine similarity of the natural-language description embedding. | Rank on the NL list when requested and configured (FR60). |
| Graph | Proximity `1 / (1 + hop distance)` in a graph-scoped search. | Rank on the graph list, normalized to its top rank. |
| Hybrid composite (**relevance confidence**) | Applies to fused results only. | Sum of weighted raw RRF terms divided by the maximum for the axes that returned at least one result. Range 0.0–1.0. |

For V1, rank `r` has internal term `raw(r) = 1 / (10 + r)`. The public hybrid per-axis score is unweighted, `raw(r) / raw(1)`. A rank-one hit exposes `1.0` even with a weight of `0.30`. Its raw term is `1/11`. A rank-two hit exposes `11/12`. A unit missing from a participating axis has no score for that axis and contributes zero to the numerator. Explain reports weights separately and preserves the difference between an excluded, unavailable and empty axis (AD-9/AD-22).

**Evidence Packet (cross-surface trust envelope):** The trust primitives defined across these requirements — composite confidence with per-axis breakdown (FR63), source/origin attribution (FR24), token-budget-aware omitted-detail handling (FR23), and graceful-degradation signaling (FR66), together with tenant/case scope, result state, freshness impact, and recovery guidance — are composed into a single shared response object referred to as the **Evidence Packet**. CLI JSON output, MCP tool responses, and future web UI composition use the same Evidence Packet contract. No surface defines confidence, degradation, omitted details, or recovery actions differently. Its versioned states are `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, and `pendingExpansion`; surfaces must not fabricate states that current mappers do not yet emit. Every evidence-bearing surface preserves the distinction between an unavailable axis, an excluded axis, and an available axis with no hits. Its concrete shape is owned by `Contracts.V1` (see Architecture) and its presentation semantics are elaborated in the UX Design Specification.

**Critical distinction: confidence scores measure query-result relevance, NOT factual accuracy or data completeness.** A score of 0.95 means the result is highly relevant to the query — it does not mean the underlying data is complete, correct, or current. This distinction must appear in:
- API reference documentation
- CLI `--explain` output (every explain result includes this caveat)
- Compliance enablement guide
- MCP tool response schema documentation

**Metadata confidence** is separate from search relevance: each metadata field on a memory unit tracks its origin (`human-declared` vs `ai-inferred`) and confidence (0.0–1.0). This distinguishes "the user tagged this as 'payment-related'" from "the embedding model inferred this is about payments."

**Memory unit provenance:** Every memory unit carries the mandatory MVP field `ingested_by`. For an authenticated external actor, capture the `(issuer, subject)` pair, normalize it once at authentication and compare the resulting identity ordinally on every surface. Equal subjects from different issuers identify different actors. External request metadata cannot override that identity. Trusted internal ingestion records the canonical `system:*` principal from the authenticated workload allowlist after validating the explicit grant and active tenant. Case membership records attribution only. AD-5 governs tenant and operator authority.

**Structured Causal Data:**
Memories is responsible for delivering **unambiguous causal chain structure**, not raw results that the LLM must interpret. When `traverse_relations` returns a causal chain, it provides:

- Ordered sequence of nodes (events/documents/discussions) with explicit direction
- Timestamps on each node establishing chronological order
- Typed, directional edges with confidence tiers
- Edge confidence reflecting relationship strength
- **Gap detection:** If a causal chain has missing intermediate nodes (e.g., A's CausationId points to B, B's points to C, but B isn't indexed), the chain must flag the gap explicitly: `A → [MISSING: event-id-B] → C`. Never silently skip missing nodes. This is a data accuracy responsibility of the Interpretation layer.

**Edge Type Taxonomy (MVP; the "Source" column says which phase can populate each type):**

| Edge Type | Source | Default Confidence | Semantics |
|---|---|---|---|
| `caused_by` | Explicit CausationId — Phase 1: only when ingested metadata carries it (FR46); Phase 1.5: from the EventStore event stream (FR61) | 1.0 | Direct causal link: Event B was directly caused by Event A |
| `correlated_with` | CorrelationId — same phase rule as `caused_by` | 0.8 | Same correlation context: Events B, C, D all occurred in the same workflow as Event A, but did not necessarily cause each other |
| `references` | Explicit link or AI-inferred content similarity — Phase 1 | 0.5–1.0 | Document A references or relates to Document B. 1.0 for explicit links, 0.5 for AI-inferred |
| `contains` | Case/folder structure — Phase 1 | 1.0 | Structural: case contains memory unit |
| `annotates` | User correction or commentary (FR37) — Phase 1 | 1.0 | Memory unit B is an annotation/correction on memory unit A |

The distinction between `caused_by` and `correlated_with` is critical. Collapsing CorrelationId into causation makes every event in a correlation group appear to cause every other event — exactly the misrepresentation the structured data model exists to prevent.

Users can promote AI-inferred edge confidence (e.g., from 0.5 to 1.0) when they verify a relationship. The system never auto-promotes.

**Confidence calibration (Phase 2):** Periodic review of *edge* confidence tiers against reviewer judgments of relationship correctness — separate from relevance confidence. Do not read 0.8 relevance as "~80% factual accuracy."

**Responsibility boundary:** Memories owns data accuracy (correct ordering, complete chains, accurate edge types, gap detection). The LLM owns narrative quality (prose composition, summarization). If the structured data has wrong ordering, missing links, or silent gaps, that's a Memories bug. If the prose misrepresents correct structured data, that's an LLM problem.

### Open-Source Licensing

**Hexalith.Memories license: MIT** (decision, Jerome, 2026-09-08). The March 2026 PRD recorded Apache 2.0; the repository and every published package shipped MIT, and the owner confirmed MIT as the contract. No relicensing work is owed. MIT is permissive and signals long-term trust for enterprise adoption. The README must include a public commitment: *"Hexalith.Memories is committed to the MIT license. We will not change to a restrictive license."* This preempts BSL-switch concerns that have eroded trust in other AI infrastructure projects.

**Dependency chain licensing:**

| Dependency | License | Risk Level | Implication |
|---|---|---|---|
| DAPR | Apache 2.0 | None | Permissive, fully compatible |
| Redis (core) | BSD 3-Clause | None | Permissive, fully compatible |
| RediSearch / Redis Stack | SSPL / RSAL | **Medium** | Users must self-host or use Redis Cloud. **Cannot offer Hexalith.Memories as a competing managed service on Redis Stack.** This constraint must be documented in the README deployment section, not buried in licensing files. |
| FalkorDB | AGPL-3.0 | **Medium** | Memories connects to FalkorDB as an external service through an approved server-side graph adapter. Network separation does not by itself settle licensing obligations; deployment and distribution guidance must disclose the dependency and direct operators to qualified legal review. |

**Licensing de-risk strategy:**

1. **LICENSE-DEPENDENCIES.md** — Document the direct adapter/network boundary between Memories and FalkorDB without claiming that topology resolves AGPL obligations. Give enterprise legal teams concrete deployment and distribution facts to evaluate.
2. **FalkorDB version pinning** — Pin to a specific AGPL-licensed image version in the AppHost resource definition and the production deployment artifacts (Epic 26). If FalkorDB relicenses, users can stay on the pinned version while alternatives are built. Version pinning is the cheapest first defense against relicensing risk.
3. **IMemoryGraph AND IMemoryIndex extraction points identified in Phase 2** — Not premature abstraction, but licensing insurance. If FalkorDB's AGPL becomes an enterprise blocker, extracting the interface enables swapping to Neo4j (GPL with commercial license) or graph-on-Redis. If Redis Stack goes proprietary, IMemoryIndex enables migration to Dragonfly/KeyDB (BSD) or Qdrant. Low extraction cost, high insurance value. Recovery time with pre-identified extraction points: 2–4 weeks. Without: 2–4 months.
4. **SSPL constraint in README deployment section** — "Offering Hexalith.Memories as a hosted/managed service requires compliance with Redis Stack's SSPL terms. Self-hosted deployments are unaffected."

## Innovation & Novel Patterns

### Detected Innovation Areas

**1. Three-Axis Retrieval Fusion (Core Innovation — Phase 1 tests the fusion, Phase 1.5 supplies the causal graph)**
The Phase 1 bet is documented deterministic RRF across syntactic, semantic, and a case-scoped graph, with `--explain`, beating BM25+semantic under the thesis-gate protocol. Hybrid BM25+vector is widely available (Elasticsearch, Azure AI Search, Weaviate, Vespa). The novelty this PRD *markets* — the EventStore/DAPR causal graph inside that fusion — arrives with Phase 1.5 population; it is not "no system fuses three axes," and it is not what G1 proves.

**2. Event Memory via DAPR Pub/Sub (Platform Innovation)**
EventStore happy path: subscribe, follow conventions, index dual embeddings and CausationId/CorrelationId as graph edges. Schema changes require handler registration. Generic DAPR publishers (Marten, Wolverine, Axon) are an experiment — if integration needs custom code beyond subscription config, keep EventStore conventions as the beachhead.

**3. Causal Intelligence as a Query Interface (Domain Innovation — Phase 1.5 outcome)**
Event-sourced systems already capture *why* things happen — but that causal data is locked inside infrastructure, queryable only by developers who know the event store schema. Once Phase 1.5 populates the graph from the event stream, Memories makes causal chains queryable via natural language: "What led to this decision?" walks the CausationId graph and returns structured, ordered, gap-aware results (the FR46–FR52 mechanics shipped in Phase 1). This transforms event sourcing from a persistence pattern into a knowledge management pattern. Gate: L3.

**4. Interpretation and application decisions**
Memories preserves source attribution and labels its interpretations, including retrieval relevance and inferred connections. Applications own decisions based on that evidence. Reviewers can check whether users can trace a conclusion to its source and see where its causal chain has a gap.

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

The differentiation hypothesis is that one Dapr/EventStore integration can return a ranked answer with the causal chain and its gaps. G1 tests retrieval quality against BM25+semantic. L2 measures the effort to reach the first event-backed result and L3 measures known-chain completeness. Competitive durability and the cost of reproducing this integration remain unmeasured.

**Addressable market:** EventStore users are the beachhead. Generic DAPR pub/sub (Marten, Wolverine, Axon) is an experiment with a kill switch (custom code beyond subscription config). It is not the brief's standalone case+three-axis expansion path and not an EventStore-equivalent zero-code promise.

### Validation Approach

| Innovation | Validation Method | Kill Switch |
|---|---|---|
| Three-axis fusion | Thesis-gate protocol per Measurable Outcomes (G1: N ≥ 50, hybrid vs BM25+semantic, ΔNDCG@10 ≥ 0.02, κ ≥ 0.6) — not yet run; the 2026-07-16 N=8 run is diagnostic | Execute named kill-switch actions if 80% misses |
| EventStore product integration | Launch stopwatch L2: `dotnet add package Hexalith.Memories.EventStore` + subscription + secret + boot + first event search | A run taking 30 minutes or longer fails L2 |
| Causal intelligence | L3 requires complete paths for ≥95% of the fixture’s distinct eligible events | No launch |
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

Hexalith.Memories combines a developer tool and API backend distributed as NuGet packages. .NET Aspire composes the Dapr-native service. Native REST clients use infrastructure-managed ingress. The target CLI and MCP surfaces use enrolled operations in Hexalith.McpCli. The repository reuses Hexalith.Commons and Hexalith.EventStore through root-declared submodules under `references/`.

### Technical Architecture Considerations

The September 27 course correction approved `Hexalith.McpCli` as the shared CLI and MCP surface for eligible operations. The legacy Memories CLI and MCP server remain available for compatibility until replacement or withdrawal satisfies the approved search, ingest, traversal, tenant isolation, identity, output and accessibility criteria.

**Language & Platform Matrix:**

| Aspect | MVP | Future |
|---|---|---|
| Server runtime | .NET 11 / C# 14. SDK pin is `global.json` (repository configuration), not this PRD. | .NET plus optional architecture-owned polyglot sidecars (see addendum: Dapr Agents) |
| Client libraries | Native ingress REST and the `Hexalith.Memories.Client.Rest` adapter serve existing consumers. The legacy CLI uses HTTP. | Reusable Dapr and language-specific clients are separately planned. |
| CLI and MCP | `dotnet tool install -g Hexalith.Memories.Cli` installs the **compatibility** tool. The legacy MCP server is a separate compatibility package. | The `hexalith` CLI and `Hexalith.McpCli` MCP server expose enrolled operations. Inventory v1.1.0 defers L03 search and M01–M04, and withdraws L06 quickstart. Eligibility, installation and enrollment are distinct. |
| Cross-language access | Via ingress REST API (any HTTP client) or DAPR service invocation (any DAPR SDK) | Dedicated language-specific client packages |
| IDE tooling | None | VS/Rider templates, analyzers (deferred) |

**Package Distribution:**

`tools/release-packages.json` defines the published package IDs and separately lists non-packable projects. The two tables follow those manifest entries.

**Published packages** (IDs must match `tools/release-packages.json`):

| Package | Purpose |
|---|---|
| `Hexalith.Memories.Contracts` | Domain types, memory unit model, envelopes |
| `Hexalith.Memories.Client.Rest` | Typed HTTP client for external consumers via ingress REST |
| `Hexalith.Memories.Redis` | Compatibility-only Redis/FalkorDB API retained for existing package consumers |
| `Hexalith.Memories.Cli` | Obsolete compatibility CLI; target is the `Hexalith.McpCli` tool |
| `Hexalith.Memories.Mcp` | Obsolete compatibility MCP server; target is `Hexalith.McpCli` |
| `Hexalith.Memories.Aspire` | Reusable Aspire resource-model integration |
| `Hexalith.Memories.EventStore` | EventStore product-integration package (Phase 1.5 surface) |
| `Hexalith.Memories.Telemetry` | Shared telemetry constants and collectors |
| `Hexalith.Memories.ServiceDefaults` | Shared packaged service defaults |
| `Hexalith.Memories.EventStoreExtensions` | Registers Memories-owned local internal intent adapters with the EventStore SDK |

**Non-packable projects**

| Project | Purpose |
|---|---|
| `Hexalith.Memories.Server` | DAPR service, workflows/actors, REST controllers |
| `Hexalith.Memories.AppHost` | Local development and test orchestration with .NET Aspire |
| `Hexalith.Memories.DomainService` | Independently authenticated EventStore SDK host for tenant, case and memory-unit commands and replay |
| `Hexalith.Memories.Web` | Razor component library used by the runnable `tests/Hexalith.Memories.Web.SpecimenHost`. NFR32/NFR35 activate when the Phase 2 product web surface ships |
| `Hexalith.Memories.AccessTelemetry` | Access-telemetry host component |
| `Hexalith.Memories.AccessTelemetry.Clock` | Access-telemetry clock service |
| `Hexalith.Memories.AccessTelemetry.Contracts` | Access-telemetry contracts |

The [release manifest](../../tools/release-packages.json) records each project path. The [route inventory](../../docs/operations/route-surface.md) identifies the separate Server and DomainService endpoints.

Mechanism detail (dependency graph, Redis compatibility facade, composition root) is in `addendum.md` and architecture.

**Deployment Topology:**

Native REST clients use infrastructure-managed ingress. Internal services use the Dapr mesh. The compatibility paths below describe existing consumers. New CLI and MCP operations follow the approved McpCli/Gateway enrollment contract.

- **Compatibility MCP client** → ingress → compatibility MCP host → Dapr → Memories Server under the validated delegated bearer
- **Compatibility CLI and direct REST clients** → infrastructure ingress → Memories Server under the validated bearer
- **Shared McpCli target** → Gateway → enrolled module operation under workload authority. Admission of a user-scoped operation requires independent tenant and user authorization on that transport.
- **Memories Server** → approved internal Redis/FalkorDB adapters → backend data-plane clients using Aspire-injected keyed connections; DAPR state remains the portable coordination path
- **EventStore events** → DAPR pub/sub → Memories Server

The compatibility MCP host is a separate Dapr application that invokes Memories Server. The shared McpCli Gateway uses its own workload identity and forwards no user credentials. Memories Server exposes REST controllers and internal DAPR endpoints in the same ASP.NET Core host.

**Service Communication Model:**

| Layer | Communication |
|---|---|
| Internal (Server ↔ compatibility MCP host) | DAPR service invocation with validated delegated bearer authority |
| Internal (DAPR state) | Sidecar state API only |
| Internal (Redis/FalkorDB search/graph) | Approved infrastructure-boundary clients with Aspire-injected keyed connections — not via the DAPR state API as a generic proxy. Product projects do not construct infrastructure endpoints or clients. |
| Internal (EventStore domain truth) | EventStore acknowledgement is the durable commit for Case / MemoryUnit / Tenant |
| Internal (EventStore product integration) | CloudEvents via DAPR pub/sub (Phase 1.5) |
| External → internal | Direct clients and compatibility CLI/MCP use their bearer paths. Enrolled shared McpCli operations reach module handlers through the workload-authenticated Gateway. Native REST is active in MVP. The application search UI belongs to Phase 2. |
| Serialization | JSON exclusively |
| Authentication | Native REST and compatibility CLI/MCP preserve validated bearer tenant and subject authority. The shared Gateway authenticates as a workload. Ordinary internal calls require protected DAPR channels, a deny-by-default app allowlist, an explicit tenant grant and an active tenant (NFR10). Anonymous access is limited to enumerated health and infrastructure routes. |
| Tenant context | Resolve authorized scope on the server. Reject an explicit tenant mismatch before any restricted output or product side effect. Request fields, workload identity, case membership and channel credentials alone never authorize an operation. |
| Rate limiting | In scope: embedding-provider throttle (FR69), inbound request quotas by authenticated tenant, plus ingress. Not "deferred to infrastructure" as the only control. |
| Identity / provenance | External attribution uses the authenticated issuer and subject. A granted internal workload uses its canonical `system:*` identity. Case membership records attribution only. |

**Error Handling Model:**

Errors follow the Hexalith.Commons shared error handling conventions (via `references/Hexalith.Commons`). Error propagation chain across all hops:

| Hop | Error Format | Includes |
|---|---|---|
| Memories Server → DAPR | Hexalith.Commons error envelope | Error code, failed component (actor, index, graph), details |
| MCP Server → LLM Agent | MCP error response | Hexalith error code mapped to MCP format, failed service identifier |
| Ingress → CLI / third-party | HTTP status + Hexalith.Commons JSON envelope | Error code, failed component, recovery suggestion |
| CLI → terminal | Human-readable message + error code; JSON in `--format json` | Actionable guidance ("Is the service running? Check the local stack with `dotnet run --project src/Hexalith.Memories.AppHost`") |

Internal DAPR errors must propagate through ingress with enough context for CLI to display actionable diagnostics — never swallow into generic 502.

**Versioning Strategy:**

Aligned with Hexalith.EventStore conventions (via `references/Hexalith.EventStore`):
- NuGet packages: Semantic versioning
- `Contracts.V1.MemoriesRoutes` defines versioned native API paths under `/api/v1`. Dapr service invocation uses the configured, unversioned Server app ID (default `memories`) and the same versioned method paths. V1 evolves through backward-compatible additions. [Route inventory](../../docs/operations/route-surface.md)
- Breaking changes: New message types, deprecation cycle matching EventStore patterns

**Health Check & Observability:**

| Aspect | Requirement |
|---|---|
| Readiness/liveness | Liveness reports process viability. Readiness requires valid authentication configuration, the DAPR control boundary, and EventStore command availability; a failed query backend degrades the affected capability while at least one selected safe axis remains usable (FR66/FR72), rather than making the whole server unready. |
| Tracing | Aspire ServiceDefaults configures OpenTelemetry export; trace context propagates across DAPR calls and through ingress |
| Logging | Aspire ServiceDefaults configures structured JSON logging with OpenTelemetry; correlation IDs from DAPR trace context |
| Metrics | Aspire dashboard surfaces DAPR + custom metrics (ingestion throughput, search latency per axis, index size per tenant) via OpenTelemetry export |
| Dashboard | Aspire dashboard provides local dev observability out of the box — no separate management API needed |
| Projection consistency | `memories consistency verify --tenant <tenantId>` checks index/graph divergence. Tenant-isolation diagnostics use `POST /api/v1/tenants/{tenantId}/verify`. |

### Embedding Provider Configuration

MVP supports Google embedding generation at runtime. Configuration is per-tenant and deliberately shaped for provider expansion — different tenants can carry provider/model/rate-limit configuration, but non-Google runtime providers are post-MVP unless a later sprint change explicitly pulls them forward.

**Supported Providers (MVP):**

| Provider | Model (default) | Dimensions | Rate Limit (default) |
|---|---|---|---|
| Google | `gemini-embedding-001` | 768 | 1500 req/min |

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

**Observable ingestion states (the one vocabulary; Glossary "Ingestion state"):** `pending` → `extracting` → `embedding` → `projecting` → `indexed`, with `failed` as the terminal failure. Retry attempts, dead-letter, and repair are *detail on* `projecting`/`failed` (attempt count, last error, `repaired_at`), not additional states. `indexed` is emitted only after all three projections — search, vector, graph — acknowledge the same current authoritative revision under the active schema generation and embedding configuration. Stale or incompatible acknowledgements cannot complete a newer revision. In MVP "required projections" means all three; query-time degradation never promotes an incomplete revision.

**Pipeline Stages:**

| State | What happens | Owner |
|---|---|---|
| `pending` | Content accepted; EventStore commit durable | Workflow |
| `extracting` | Text extraction from content (PDF, URL, file) | Workflow activity |
| `embedding` | Call embedding provider API, get vector | Throttled by per-tenant rate-limiter actor |
| `projecting` | Write RediSearch, Redis Vector, FalkorDB as rebuildable projections; retries with backoff stay in this state | Workflow + compensation |
| `indexed` | All three projections acknowledge the current authoritative revision under the active schema and embedding configuration | Terminal success |
| `failed` | Terminal failure retains the error, stage and attempt count. | Operator reads the native case failed-units endpoint. CLI failure visibility remains an NFR19 obligation. |

**Failure handling:**
- Failed units retry with exponential backoff (configurable max retries) while remaining `projecting` (or the failing stage)
- After max retries, units move to `failed` with error details preserved
- Read current case counts at `GET /api/v1/tenants/{tenantId}/cases/{caseId}/status`. `memories status telemetry --tenant <tenantId>` gives tenant-level metrics and queue depth.
- Read failure details at `GET /api/v1/tenants/{tenantId}/cases/{caseId}/failed-units`. FR10, FR11 and NFR19 require the enrolled CLI to show per-state case counts and each failed unit’s stage and safe error. Both CLI reads await delivery.
- FR13: no silent two-of-three searchable unit. Retry, repair, and EventStore replay converge through the same completion contract; a unit is not `indexed` until all three projections acknowledge the current revision and configuration, and query degradation cannot promote it.
- **File/URL freshness (NFR36):** under normal conditions a ≤10 KB file unit reaches `indexed` within 60 seconds of `pending`; degradation is reported, not silent.

**Runtime split:**
- **IngestionWorkflow:** stages, retry, compensation, Durable Task persistence (NFR17).
- **EmbeddingRateLimiterActor / CorpusStatisticsActor:** singleton/budget actors only.

### Interface Capability Parity Matrix

The target CLI serves developers and operators. The Phase 1.5 MCP scope covers search, ingestion, traversal and case information. Each target operation requires enrollment. The following table records compatibility commands and native access available to operators.

CLI names below belong to the legacy tree. A stub or missing verb links to its current native alternative. FR53 and the approved migration inventory govern target coverage.

| Capability | Compatibility CLI or current alternative | Compatibility MCP (Phase 1.5 behavior) | Service capability |
|---|---|---|---|
| Search (syntactic, semantic, graph, hybrid) | `memories search query` | `search_memory` | `SearchAsync` |
| Search with explain | `memories search query --explain` | `search_memory` (explain field) | `SearchAsync` (explain option) |
| Result / unit inspection | `memories search inspect`, `memories search lookup` | -- | -- |
| Content ingestion | CLI stub. Use `POST /api/v1/ingest` or `POST /api/v1/ingest/url`. | `ingest_content` (file content only) | `IngestAsync` |
| Graph traversal | CLI stub. Use `GET /api/v1/tenants/{tenantId}/traverse`. | `traverse_relations` | `TraverseAsync` |
| Case management (create, delete, members, activity) | CLI stub. Use the native tenant-scoped cases routes. | `get_case_info` (case information only) | `CaseAsync` methods |
| Tenant management | `memories tenant list` works. Native tenant routes handle create and delete. | -- | `TenantAsync` methods |
| Tenant isolation verification | Native `POST /api/v1/tenants/{tenantId}/verify`. | -- | -- |
| Ingestion status & failed units | Native case `/status` and `/failed-units` reads. `memories status telemetry --tenant <tenantId>` gives tenant-wide metrics. | -- | -- |
| Index/graph consistency | `memories consistency verify/inspect/repair` (current repair mutates after confirmation) | -- | -- |
| Basic portable application export (FR71) | `memories export case/tenant` | -- | -- |
| Interactive exploration | `memories explore` is a stub. Use the working search, inspect and lookup commands for individual queries. | -- | -- |
| Handler management | `memories handlers` | -- | -- |
| Guided quickstart | `memories quickstart` works in the compatibility walkthrough. Inventory L06 withdraws it from McpCli migration. | -- | -- |
| Batch directory ingestion | CLI stub. Use `POST /api/v1/ingest/directory` with a server-side source directory. | -- | -- |

**Design rationale:** MCP exposes agent work. CLI exposes ops. DAPR service invocation is the internal programmatic API. FR53 is satisfied per active phase; a help entry backed by `NotImplementedCommand` is not coverage.

### CLI Specification

**Compatibility distribution.** `dotnet tool install -g Hexalith.Memories.Cli` installs the legacy `memories` tool. New CLI and MCP delivery belongs to `Hexalith.McpCli` through enrolled Memories operations. The owner inventory defers L03 search and M01–M04 and withdraws L06 quickstart. These rows are excluded from approved target coverage. G3 still requires a working enrolled search. The compatibility packages remain available until their retirement criteria are met. [Migration inventory](../../references/Hexalith.McpCli/_bmad-output/implementation-artifacts/memories-migration-inventory-v1.1.0.md) · [Owner approval](../../references/Hexalith.McpCli/_bmad-output/implementation-artifacts/evidence/story-5-10/approval.json)

**Compatibility CLI global options** include `--endpoint`, `--token`, `--format`, `--verbose`, and `--telemetry`. A tenant-scoped command takes `--tenant <tenantId>` for the requested tenant. The authenticated claims authorize access (NFR11). There is no ambient tenant selection.

**Compatibility CLI commands and output semantics.** The table records the phase and delivery status of the legacy command tree. FR53 requires delivery through enrolled McpCli operations.

| Command | Phase | Compatibility delivery status (`RootCommandFactory`, reviewed 2026-11-20) | Acceptance output |
|---|---|---|---|
| `memories search query --tenant --query [--case] [--axis] [--max-results] [--explain]` | 1 | Shipped (Story 7.2) | Hybrid explain distinguishes unweighted normalized axis ranks and the weights used in the composite. See NFR24 for the numeric acceptance fixture. |
| `memories search inspect` / `search lookup` | 1 | Shipped (7.2 / 18.5) | Single-unit Evidence Packet / keyed lookup |
| `memories traverse --tenant --from [--depth] [--edge-type]` | 1 | **Not started** — `NotImplementedCommand` stub | Ordered nodes with typed edges, timestamps, `[MISSING: id]` gap markers (FR47–FR49, FR52) |
| `memories ingest <file\|url\|dir> --tenant --case` | 1 | Compatibility stub. Use the native file, URL or directory route in Journey 9. | Progress, unit count, per-unit ingestion state (FR1–FR3, FR10) |
| `memories case create/delete/list` | 1 | Compatibility stub. Create/list at `/api/v1/tenants/{tenantId}/cases`. Delete through the native single-case route. | Case ID, unit count, next-step hint (FR26–FR27, FR30) |
| `memories case add-member/remove-member/activity` | 1 | **Not started** — stub | Member list (metadata only, no authorization); activity feed attributing ingest/search/membership events (FR28–FR29, FR36) |
| `memories tenant create/delete/verify` | 1 | Absent. Use native tenant lifecycle routes. `POST /api/v1/tenants/{tenantId}/verify` runs isolation diagnostics. | Provisioning receipt and per-check diagnostic results (FR38–FR40). NFR8 requires separate populated, dual-principal evidence. |
| `memories tenant list` | 1 | Shipped (7.1) | Tenant table |
| `memories status telemetry --tenant` | 1 | Shipped (7.5) | Tenant index health and size, recent search and ingestion activity, queue depth. |
| `memories status --case/--failed` | 1 | Absent. Current per-case counts and failed-unit details are native case reads (see Failure handling). | Counts by ingestion state and failures with stage and error (FR10–FR11). |
| `memories consistency verify/inspect/repair --tenant` | 1 | Verify, inspect and mutating repair shipped (8.2) | Reports divergence. Confirmation or `--yes` schedules Redis-input repair. FR74 still requires an authoritative preview and fenced apply (Story 34.30) |
| `memories quickstart` | 1 | Shipped compatibility wizard (7.4). Withdrawn from McpCli migration (L06). | Checks prerequisites and health, provisions the sample tenant, ingests a sample and searches for it. |
| `memories config show` | 1 | Shipped (7.1) | Effective configuration with sources |
| `memories export case/tenant --tenant` | 2 | Basic application export shipped early (8.3) | Streams portable JSON for the selected case or tenant. FR71 custody safeguards remain outstanding |
| `memories handlers list/mismatches` | 1.5 | Shipped (9.3) | Registered handlers; mismatch diagnostics (FR62) |
| `memories explore` | 1.5 | Not started — stub | Interactive exploration |

**Shipped without a CLI verb (REST `/api/v1` + `Hexalith.Memories.Client.Rest` are the Phase 1 surface; no CLI verb is planned for Phase 1):** FR12 re-ingest (`POST …/memory-units/{id}/re-ingest`, `POST …/failed-units/re-ingest`), FR21–FR22 metadata filter and pagination (`GET /api/v1/search` parameters), FR35 delete unit (`DELETE …/memory-units/{id}`), FR37 annotate (`POST …/memory-units/{id}/annotations`), FR42/FR45 tenant configuration (`PATCH /api/v1/tenants/{id}`, `GET/PUT …/embedding-config`), FR51 promote edge confidence (`PATCH /api/v1/tenants/{id}/edges/confidence`). Route constants are `Contracts.V1.MemoriesRoutes`. A later CLI verb for these operations requires selection under FR53.

FR43 uses the native `PUT …/embedding-config` endpoint in Phase 1. Its conflict response warns that existing vectors require a full reindex. An acknowledged `forceReindex=true` records `ReindexRequired`. Story 34.31 still owes the rebuild and verified cutover. A Phase 1 CLI verb is outside the approved inventory. [Native guard](../../src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs)

Epic 32 owns delivery of the approved Phase 1 operations through McpCli. Story 35.7 can qualify G3 once an operator can complete the manual journey through first search.

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

Sensitive values are not resolved through configuration fallback. Product services retrieve them through DAPR secret-store components. Aspire secret parameters or .NET User Secrets may supply protected local bootstrap or one-time seeding inputs, but product services must not read them as an alternative runtime secret provider. Kubernetes Secrets are permitted only where required for OpenBao bootstrap material. Direct Redis/FalkorDB credential injection is an alignment gap, not an approved second path (NFR9, architecture AD-15).

### Developer Experience & Documentation

**In-Repo Examples (`samples/` folder):**

| Example | Maps to | Demonstrates | Status (2026-09-08) |
|---|---|---|---|
| `samples/01-quickstart/` | Journey 9 (Phase 1 thesis path) | Boot AppHost and ingest and search through the CLI | Sample pending. The README documents the compatibility walkthrough. Story 35.7 owns the timed target-CLI run. |
| `samples/02-eventstore-integration/` | Journey 1 (Phase 1.5) | Pinned EventStore starter service and sample AppHost. L2 times package addition, subscription setup and search for the event emitted by that service | Not started — required before L2 can be timed |
| `samples/03-mcp-agent/` | Journey 3 (Phase 1.5) | MCP server launched by Aspire, pinned reference agent configuration and L1 evidence procedure | Not started — required before L1 can be run |

Numbered naming signals the learning path and mirrors user journey progression. No `samples/` folder exists in the repository yet; the Phase 1.5 samples are launch prerequisites, not documentation polish.

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
Current path: .NET Aspire AppHost with DAPR sidecars. Local: `dotnet run --project src/Hexalith.Memories.AppHost`. This is orchestration, not an extra product surface — see `addendum.md`.

**Cross-Language Future Path:**
Non-.NET external consumers can integrate today via ingress REST API (JSON payloads). Non-.NET internal services can integrate via DAPR service invocation (any DAPR SDK). Dedicated Python/TypeScript client packages are a future convenience layer.

## Functional Requirements

FR1–FR75 are the product-horizon inventory, not a claim that every FR is active thesis-MVP scope. Every FR carries a phase from the register and a delivery status from the status register below; the two are independent (an FR can be MVP and not started, or Phase 2 and shipped early).

**Canonical phase register** (August 2026 change control; confirmed 2026-09-08 — this register is the only increment source of truth; FR46–FR52 stay MVP as shipped graph mechanics, see MVP Feature Set):

- **MVP (thesis + foundation):** FR1–FR22, FR24–FR52, FR55–FR57, FR63–FR70, FR72–FR75, plus approved Phase 1 McpCli capabilities (FR53).
- **Phase 1.5:** FR23, FR54, FR58–FR62, and the Phase 1.5 CLI capabilities of FR53. Target mappings for EventStore diagnostics require separate approval.
- **Phase 2:** FR71. Story 8.3 delivered the basic case and tenant application export early. The custody and import safeguards remain open. Epic 26 owns operational backup and recovery.

A capability completed before its planned phase is recorded as completed non-MVP and does not silently change thesis-MVP acceptance.

**Delivery status register (updated 2026-11-20).** The sprint tracker records story state. The register below records delivery against the current FR wording. Release gates require the evidence specified in Measurable Outcomes.

| Status | FRs | Notes |
|---|---|---|
| **Implemented and product-active** | FR4–FR5, FR7, FR9, FR12, FR14–FR16, FR18, FR20–FR22, FR24, FR31–FR33, FR35, FR37, FR41–FR42, FR45–FR46, FR48–FR52, FR55, FR63–FR64, FR67–FR70, FR73 | Implementation exists under the current wording and the capability belongs to an active product phase. This is delivery, not thesis-gate qualification. |
| **Implemented preview; inactive pending Phase 1.5 gates** | FR23, FR54, FR58–FR62 | Code/artifacts exist, but L1–L3 qualification, publication, and announcement have not occurred. Tracker completion does not activate the product surface. |
| **Partial (protocol prerequisite missing)** | FR17, FR25 | FR17: shipped hybrid skips the graph axis without an explicit start node; the auto-seeding rule (Measurable Outcomes) is not implemented. FR25: shipped benchmark compares against best single axis; the BM25+semantic control does not exist. Both gate G1. |
| **Partial (architecture contract incomplete)** | FR6, FR13, FR34, FR39, FR65, FR75 | Missing or incomplete: current-revision all-axis checkpoint/replay (FR6/FR13), tenant-wide case-partitioned graph merge (FR34), end-to-end erasure plus CLI (FR39), server-derived external/system provenance (FR65), and durable authoritative duplicate suppression (FR75). |
| **Partial explanation and CLI recovery** | FR19, FR56–FR57 | FR19 exposes ranks and weights, but the current explain text calls the public hybrid score weighted. FR56 and FR57 have recovery hints, but empty-state guidance still points to the compatibility ingest stub. The enrolled CLI must demonstrate that its suggested actions work. [Explain metadata](../../src/Hexalith.Memories.Server/Search/ExplainMetadataBuilder.cs) · [Compatibility search hints](../../src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs) |
| **Partial operator operations** | FR43, FR74 | FR43 has a native configuration warning and acknowledgement guard. Story 34.31 owes an authoritative rebuild, catch-up, verified tenant cutover and retirement. FR74 has a confirmed mutating Redis-input repair. Story 34.30 owes an authoritative revision-bound dry run, a tenant-fenced apply, deletion provenance in access telemetry and rejection of unsupported edges. [Repair story](epics.md#story-3430-repair-only-from-authoritative-revisions) · [Rebuild story](epics.md#story-3431-activate-a-phase-1-embedding-rebuild-epoch) |
| **Shipped against previous wording; re-verification owed** | FR8, FR66, FR72 | Existing capabilities predate the strengthened fairness, capability-aware degradation, and readiness contracts. |
| **Shipped, hardening in progress** | FR44 | Epics 0/5/20 delivered isolation controls. The November audit found a shared Redis connection in the search path. Stories 34.11 and 35.6 must demonstrate tenant-scoped access, cross-tenant denial and denial after revocation. Story 34.53 covers FalkorDB separately. |
| **Partial (server shipped, Phase 1 CLI operation unavailable)** | FR1–FR3, FR10–FR11, FR26–FR30, FR36, FR38, FR40, FR47, FR53 | Server and compatibility implementations exist. Epic 32 still needs to deliver the required shared CLI operations. DW-732 closed the FR2/FR36 signed-URL display defect on November 20. Logs and case activity redact sensitive values, including prior activity on read, while exact fetch and tenant/case deduplication are preserved. The 159 focused server tests and two live integration cases cover that boundary. AD-4 durable workflow-history protection and the target CLI operations remain open. [DW-732 evidence](../implementation-artifacts/deferred-work.md#dw-732-keep-ingestion-source-credentials-out-of-logs-and-case-activity) FR38/FR40 need backend isolation and revocation evidence. FR39 is tracked above under the incomplete erasure contract. |
| **Partial, basic export shipped early (non-MVP)** | FR71 | Story 8.3 delivered streamed application JSON. Story 34.26 owes verifiable origin, payload integrity and staging cleanup. Story 34.57 binds release to the tenant lifecycle. Story 34.25 must refuse same-population imports from an erased origin. Epic 26 covers operational backup and recovery. |
| **Not started** | — | Every FR has at least some implementation. |

### Knowledge Ingestion

- **FR1:** Developer can ingest content from local files into a specified case
- **FR2:** Developers can ingest a URL into a specified case. Fetch and scoped duplicate detection use the original source. On success, duplicate delivery and failure, sensitive URL path and query values must be absent from structured logs, rendered logs and the case activity described in FR36.
- **FR3:** Developer can batch-ingest content from a directory into a specified case
- **FR4:** System can extract text from ingested content (plain text, PDF, markdown)
- **FR5:** System can generate embeddings for ingested content via a configurable embedding provider
- **FR6:** System marks a memory unit `indexed` (searchable as complete on all three axes) only after search, vector, and graph projections each acknowledge the current authoritative EventStore revision under the active schema generation and embedding configuration; stale or incompatible acknowledgements cannot complete a newer revision
- **FR7:** Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and metadata confidence score
- **FR8:** System applies the measurable bounded per-tenant admission and concurrency contract in NFR13 so one tenant's batch, repair, recovery, or migration work cannot starve another tenant's interactive ingestion or query work
- **FR9:** System retries failed ingestion automatically with configurable limits
- **FR10:** Developer can view ingestion status per case as counts per ingestion state (`pending`, `extracting`, `embedding`, `projecting`, `indexed`, `failed`)
- **FR11:** Developer can view failed ingestion units with error details and failure stage
- **FR12:** Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk
- **FR13:** Partial projection failure never yields a silently complete two-of-three unit. EventStore acknowledgement is the durable commit; retry, repair, and replay converge through the same current-revision completion contract until `indexed` or actionable `failed`. Query-time degradation cannot promote an incomplete revision. No distributed transaction is claimed, and rollback of the EventStore commit is not the recovery model.
- **FR75:** Retried V1 commands with the same tenant-, case-, and operation-scoped idempotency token, and retried CloudEvents with the same tenant, case, exact validated source, and event ID, produce one durable domain mutation. Each authoritative source-version/schema-generation/embedding-configuration tuple may produce one idempotent projection outcome, so a legitimate reprojection under a new epoch is not suppressed; duplicate delivery never creates another memory unit. **Phase:** MVP. **Status:** partial (authoritative durable suppression is incomplete)

### Knowledge Retrieval

- **FR14:** Developer can search memory units by syntactic matching within a tenant
- **FR15:** Developer can search memory units by semantic similarity within a tenant
- **FR16:** Developer can search memory units by graph traversal within a tenant
- **FR17:** Developer can search memory units by hybrid fusion combining all available axes; when no graph start node is supplied, the graph axis is auto-seeded from the top syntactic and semantic candidates (Measurable Outcomes › Graph seeding). For tenant-wide search, seeds and traversal are partitioned by each result's authoritative case and never cross case boundaries. Hybrid never silently degrades to two axes on a populated graph. **Status:** partial (auto-seeding and tenant-wide case merge not implemented)
- **FR18:** Developer can control which axes are included in a search query
- **FR19:** Developers can inspect the score semantics of a native single-axis result or the unweighted normalized rank contributions and composite weights of a hybrid result (NFR24).
- **FR20:** Developer can filter search results by case
- **FR21:** Developer can filter search results by metadata field values
- **FR22:** Developer can paginate search results
- **FR23:** LLM Agent can constrain search response size by token budget. **Phase:** 1.5
- **FR24:** System returns the origin identifier (file path, URL, or event ID) and origin type for each search result
- **FR25:** Developer can run automated benchmark comparisons of hybrid vs the BM25+semantic control (with single-axis diagnostics) and get per-topic NDCG@10 output in the thesis-gate protocol's terms. **Status:** partial (two-axis control not implemented)

### Memory Organization

- **FR26:** Developer can create a case within a tenant
- **FR27:** Developer can delete a case and all its memory units
- **FR28:** Developer can add members to a case. Membership is attribution metadata (listings, activity feed); it does not authorize access in the current phase
- **FR29:** Developer can remove members from a case
- **FR30:** Developer can list cases within a tenant
- **FR31:** Developer can view case status including memory unit count, last activity timestamp, and the ingestion state of the most recent unit (one of the Glossary ingestion states)
- **FR32:** System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion
- **FR33:** System maintains case-scoped graph edges and paths between memory units within a case; cross-case edges and traversal remain forbidden through Phase 1.5
- **FR34:** Developer can search across all cases within a tenant, returning independently ranked results with mandatory case attribution while every graph contribution remains case-local
- **FR35:** Developer can delete an individual memory unit from a case
- **FR36:** Developers can view recent ingestion, search and membership activity within a case. Both stored activity and its API representation must protect sensitive URL values under FR2.
- **FR37:** Developer can annotate or correct a memory unit, with annotations tracked as linked memory units

### Tenant Management

- **FR38:** Operator can create a tenant with tenant-scoped backend principals and tenant-scoped indexes (isolation *outcome* is NFR8; mechanism is architecture-owned)
- **FR39:** Operator can complete verified tenant erasure: purge every product projection, make EventStore-held tenant content irreversibly inaccessible, and record the access-telemetry erasure handoff. Replay, restart, and restore cannot resurrect erased content; unreadable restored payloads are quarantined, and the deleted tenant ID cannot be reused
- **FR40:** Operator can verify tenant isolation via automated checks
- **FR41:** Operator can list tenants
- **FR42:** Operator can update tenant configuration after creation (rate limits, display name, settings)
- **FR43:** Before accepting an embedding-provider or index-schema change that requires reindexing, the Phase 1 native API must tell the operator that existing vectors require rebuilding and require explicit acknowledgement. The acknowledged rebuild may degrade service for that tenant. It must catch up to authoritative changes, verify parity and activate the new configuration atomically for the tenant. Any later CLI exposure must relay the warning and acknowledgement.
- **FR44:** System derives or verifies tenant and case authority server-side on every external and internal path. An explicit tenant conflict is rejected before retrieval or mutation with a clear error and zero restricted output. Request fields, app identity, case membership and channel credentials alone confer no authority.
- **FR45:** Operator can view current configuration of a tenant (embedding provider, rate limits, index status)

### Graph & Causal Intelligence

These are the graph *mechanics* (MVP, Epics 1 and 4). What populates `caused_by`/`correlated_with` is phase-dependent: ingested metadata in Phase 1, the EventStore event stream in Phase 1.5 (FR59–FR62).

- **FR46:** System can index CausationId and CorrelationId carried by ingested content metadata as typed, directional graph edges
- **FR47:** Developer can traverse the graph from a starting node with configurable depth, including causal chains where causal edges exist
- **FR48:** Developer can filter graph traversal by edge type
- **FR49:** When an intermediate node in a causal chain is not indexed, the traversal result includes a gap marker with the missing node identifier
- **FR50:** System supports edge types: `caused_by`, `correlated_with`, `references`, `contains`, `annotates` — each with default confidence
- **FR51:** Developer can promote AI-inferred edge confidence when verifying a relationship
- **FR52:** System maintains chronological ordering and timestamps on causal chain nodes

### Developer Interfaces

- **FR53:** Developers can ingest and retrieve memory through working, enrolled `Hexalith.McpCli` operations for the approved Phase 1 scope. Phase 1.5 adds its approved CLI capabilities. Operations that require user authority must pass the NFR10 transport-specific admission checks before enrollment. The compatibility table records the expected output and recovery behavior. **Status:** partial pending target delivery.
- **FR54:** Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools. **Phase:** 1.5
- **FR55:** CLI supports multiple output formats: human-readable (default), JSON, and table
- **FR56:** CLI provides actionable error messages with recovery suggestions for, at minimum: server unreachable (name the boot command); authentication or tenant-claim mismatch (name the authenticated tenant without disclosing restricted evidence); unknown tenant or case id; true empty tenant or case (name an implemented next action); incomplete/delayed ingestion; active filters; stale evidence; unit `failed` (name the stage and error); backend degraded (name the unavailable/excluded axes); provider rate-limited (name the retry window). A generic "no results" message is insufficient, and unavailable target commands are never presented as completed actions. The CLI exit-code map (`CliExitCodes`) is the machine-readable side of this list
- **FR57:** Developer can discover available *implemented* actions from empty states and error conditions (empty-state copy + `--help` examples). Does not require a universal command catalog of unbuilt verbs.
- **FR58:** MCP tools include typed parameter schemas with descriptions for LLM agent consumption. **Phase:** 1.5

### EventStore Integration

- **FR59:** System can auto-discover event types published to DAPR pub/sub topics. **Phase:** 1.5. Happy path is conventions + subscription; schema evolution requires handler registration.
- **FR60:** System can generate dual embeddings for events (raw payload + natural language description). **Phase:** 1.5
- **FR61:** System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code on the EventStore happy path. **Phase:** 1.5
- **FR62:** Developer can list registered event handlers and detect handler registration mismatches. **Phase:** 1.5

### Trust & Transparency

- **FR63:** A hybrid result exposes relevance confidence in 0.0–1.0 and unweighted normalized per-axis rank contributions. Native single-axis results identify their own score measure (FR19/NFR24).
- **FR64:** System tracks metadata origin (human-declared vs AI-inferred) and metadata confidence per metadata field on every memory unit
- **FR65:** Every memory unit records `ingested_by`. External attribution binds to the authenticated issuer and subject, normalized once at ingress and compared ordinally thereafter. The same subject at different issuers yields distinct actors. Trusted internal attribution uses the canonical `system:*` principal for the authenticated allowlisted workload after its active-tenant grant check. Caller-supplied provenance cannot override either identity.
- **FR66:** When selected search axes degrade, the system returns a safe partial result if at least one selected axis can respond and fails only when none can. The Evidence Packet distinguishes unavailable, excluded, and available-with-no-hits axes and reports freshness impact plus recovery guidance; empty results distinguish true absence, empty/wrong scope, incomplete or delayed ingestion, filters, stale evidence, authorization refusal, and backend degradation. Unauthorized responses contain no restricted evidence, and no surface promotes an incomplete ingestion revision
- **FR67:** System records search and access events per tenant as access telemetry (Glossary) — infrastructure telemetry that applications may build access records on; not a tamper-evident audit trail. Telemetry delivery failure never changes domain truth or rolls back an accepted product mutation

### Embedding Provider Management

- **FR68:** Operator can configure embedding provider and model per tenant
- **FR69:** System enforces per-tenant rate limit ceilings for embedding API calls
- **FR70:** System tracks the embedding provider and model used for each memory unit's vectors

### Data Portability & System Health

- **FR71:** A developer can export all memory units, metadata and graph edges for a case or tenant in a portable format. Story 8.3 delivered basic streamed JSON ahead of Phase 2. The application capability belongs to Phase 2. Its deployed paths require verifiable origin and payload integrity, controlled custody transfer, cleanup of source-held copies and refusal of same-population imports from an erased origin. These safeguards also protect the active FR39/NFR16 erasure boundary under AD-16/AD-21. Epic 26 owns operational backup and recovery.
- **FR72:** System exposes liveness for process viability and readiness for authentication configuration, the DAPR control boundary, and EventStore command availability. Search-backend outages report capability degradation while safe selected axes remain usable rather than making the entire server unready
- **FR73:** Operator can detect index/graph divergence via consistency check
- **FR74:** Operator can repair detected index/graph inconsistencies via a dry-run-then-apply consistency repair that cannot cross tenants, cannot silently delete units without provenance in access telemetry, and cannot invent edges the current authoritative EventStore revision does not support

## Non-Functional Requirements

*NFRs are tagged by validation phase: **[MVP]** = thesis/foundation, **[P1.5]** = EventStore product integration + MCP, **[Ongoing]** = as infrastructure matures, **[Future web]** = Epic 17+.*

**NFR delivery status (updated 2026-11-20):** *verified* = an automated suite or recorded run exercises the current wording; *implemented* = the mechanism exists but the stated verification has no recorded run; *not started* = neither. Verification of a Phase 1.5 component does not activate its product surface; only L1–L3 do that. The current architecture spine's gap ledger corrects claims that predate the strengthened wording. The NFR delivery-status register is refreshed only during PRD Updates.

| Status | NFRs |
|---|---|
| Verified | NFR11, NFR17, NFR20, NFR26–NFR28, NFR30 |
| Verified against the *previous* wording; re-verification owed | NFR8–NFR10 (principal-driven isolation, data-plane secret path, and layered internal authorization), NFR13 (mixed-workload fairness), NFR18 (capability-aware failure boundary), NFR21 (negative envelope case), NFR22 (durable provider retry timing), NFR24–NFR25 (canonical cross-surface fusion/explain semantics) |
| Partial mechanism and re-verification owed | NFR16 — the controlled Redis restart covered the previous wording. Authoritative replay and erased-tenant non-resurrection remain unverified. |
| Partial mechanism and re-verification owed | NFR19 — native ingestion status and case failed-unit endpoints exist. The enrolled CLI and intentional-failure evidence across all stages are outstanding. [V1 route constants](../../src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs) |
| Partial mechanism and re-verification owed | NFR31 — the compatibility walkthrough exists. Target operations and the Story 35.7 cold run remain outstanding. |
| Implemented, verification run not recorded in PRD evidence | NFR1–NFR5, NFR7, NFR12, NFR14, NFR23, NFR29, NFR33–NFR34. DW-735 tracks measurement of the NFR12 100K-unit workload. Epic 27 closed with local retention evidence. Production lifecycle writes remain disabled. The operator’s log platform controls retention. The lack of a Memories-enforced bound is accepted debt. The Production qualification campaign is parked. [November 1 decision](sprint-change-proposal-2026-11-01.md) |
| Not started | NFR6 (event freshness measurement), NFR15 (extraction-point review), NFR32 and NFR35 (future web, Epic 17 delivered components but gates activate with the web surface), NFR36 (new 2026-09-08), NFR37 (active CLI accessibility contract; current-contract evidence absent) |

`[ASSUMPTION: the "implemented" row reflects the absence of recorded evidence in planning artifacts, not proof of absence in CI; the architecture owner should move rows up when a run is linked.]`

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
| **NFR8** | Zero cross-tenant data leakage. Reject an explicit tenant conflict before any product data read, mutation or restricted output. Tenant A’s credentials cannot operate on tenant B even when index names or graph IDs overlap. | As A, search, ingest and traverse using B’s tenant ID or resource names. Require a denial with no restricted results or product side effects. Verify that malformed and explicitly empty tenant IDs are rejected before product work. Where an operation permits omitted scope, verify it derives only the authenticated tenant. Repeat with identical graph structures in A and B and assert zero B nodes. Re-run when Epic 24 closes. | MVP |
| **NFR9** | Product services retrieve embedding-provider, application, and data-plane credentials exclusively through the DAPR Secrets API backed by OpenBao. Secret values never appear in application configuration, ordinary environment variables, workflow history, logs, or public contracts. Deployments may supply only documented minimum OpenBao bootstrap material outside that boundary; direct Redis/FalkorDB credential injection is an alignment gap, not an approved second path. | Structural dependency tests, secret scanning, AppHost/deployment topology tests, and integration tests proving DAPR reads from OpenBao without secret disclosure | Ongoing |
| **NFR10** | Native REST and compatibility CLI/MCP preserve validated bearer tenant and subject authority. The shared Gateway calls with workload authority and forwards no user credentials. Before enrolling an operation that requires a user, demonstrate authenticated tenant and user authorization on its selected transport. Refuse when that proof is absent. Ordinary trusted internal calls also require protected DAPR channels, an allowlisted app ID, an explicit tenant grant and authoritative `Active` state. | Exercise delegated requests on the bearer paths and workload requests on the Gateway. Verify authorized and denied user-scoped operations, absent user proof, unknown workloads, missing grants and inactive tenants. | Ongoing |
| **NFR11** | External product REST/CLI ingress is authenticated for the active MVP HTTP surface. Health probes and required DAPR infrastructure routes are the only deliberate anonymous exceptions and are named and tested. Additional identity-provider hardening may remain operational-readiness work; unauthenticated product ingress is not a Phase 1.5 allowance. | Integration test with unauthenticated product requests plus named anonymous exceptions | MVP |

### Scalability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR12** | Adding nine loaded tenants does not degrade an existing tenant's p95 query latency or ingestion throughput by more than 5% under the NFR13 reference mixed-load profile | Benchmark tenant 1 alone at 100K units, then repeat with 9 additional 100K-unit tenants running the NFR13 mix; compare p95 latency and throughput | Ongoing |
| **NFR13** | Over ten measured minutes, A runs ten concurrent queries over a fixed mix of syntactic, semantic and hybrid search. For each query type, mixed-load p95 must meet NFR1–NFR3 and be ≤1.10 times its matched solo p95. B offers 120 ingests/min of ≤10 KB each and must complete >100/min on average (NFR5). C starts with 300 distinct units queued for repair and must complete ≥250 during the same window. Every eligible non-empty tenant queue receives an admission within 5 seconds after capacity is available. Default pending caps are 1,000 units/tenant and 10,000 globally. Reject the first excess item within 1 second with retry guidance, retaining all accepted work. | Reproducible three-tenant reference run and separate cap-plus-one tests covering interactive, batch, repair, recovery and migration. [Load-test procedure](addendum.md#nfr13-load-window) | Ongoing |
| **NFR14** | Redis memory footprint per memory unit is predictable and documented — operator can estimate infrastructure costs before tenant provisioning | Published sizing guide: memory per unit by vector dimension and metadata size | Ongoing |
| **NFR15** | Architecture must not preclude backend migration (Redis → Qdrant) — concrete implementation with clear extraction points identified, no premature interfaces | Architecture review: extraction points documented, no tight coupling to Redis-specific APIs in domain logic | Ongoing |

### Reliability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR16** | No loss of EventStore-committed memory units across Redis restart or projection loss: every non-erased unit that reached `pending` or later becomes searchable again under the current schema and embedding configuration, either because projections survived or because authoritative EventStore replay rebuilt all axes. AOF is an optimisation, not the durability contract. Replay, restart, export restore, and backup restore must never resurrect content from a tenant whose erasure completed under FR39. | Commit N units, restart Redis, then separately destroy projections and replay EventStore truth through the FR6/FR13 completion contract. **AOF-intact:** all N `indexed` within 5 minutes for 10K units/tenant. **Rebuild:** progress visible via `status`, completion at no worse than NFR5 throughput, zero non-erased units missing. Restore an erased-tenant fixture and prove zero content rehydrates. The current architecture spine records the authoritative replay mechanism as missing. | MVP |
| **NFR17** | Ingestion pipeline state survives process restarts — pending and in-progress units resume without data loss | DAPR Workflow / Durable Task history verified | MVP |
| **NFR18** | Partial backend failure returns a safe result when at least one selected axis can respond and fails only when none can. The Evidence Packet distinguishes unavailable, excluded, and available-with-no-hits axes and includes freshness impact plus recovery guidance; incomplete ingestion revisions remain incomplete. | Chaos test each backend alone and in combinations; verify safe partial responses, exact Evidence Packet meanings, and the no-safe-axis failure boundary | Ongoing |
| **NFR19** | The tenant-authorized enrolled CLI lists each failed ingestion unit with its failure stage and safe error details | Induce a failure at each pipeline stage through retry exhaustion. From the enrolled CLI, check that each unit appears in its tenant and case with the expected stage and safe error details | Ongoing |

### Integration

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR20** | MCP tool responses conform to MCP protocol specification — valid tool schemas, typed parameters, structured error responses | MCP protocol conformance test suite | P1.5 |
| **NFR21** | Admit EventStore-convention CloudEvents using the authenticated delivery channel. Match the subscribing component, topic and scoped publisher to one candidate tenant, then require the allowlisted publisher, explicit tenant grant and an active tenant. Read event attributes, including causation and correlation, only inside that authorized scope. Other publishers’ convention-compatible fields may be accepted through the same admission checks. Generic integration without custom code remains an experiment. | Deliver a conforming event on its authorized channel. Change only its envelope `source` and verify it cannot select another tenant. Reject unknown or ambiguous routes, contradictory envelope tenants, ungranted or unknown publishers and inactive tenants before mutation. Record a nonconforming-envelope test. | P1.5 |
| **NFR22** | Embedding provider rate limits do not crash the pipeline or lose work: the system honors provider `Retry-After` through durable workflow timers and bounded queues, never retries before the stated instant, and resumes eligible work within 5 seconds after the window when capacity is available | Rate-limit simulation per provider across restart, queue saturation, and concurrent tenants; assert retry timing and no accepted-work loss | Ongoing |
| **NFR23** | CLI connects to the memory server via configurable endpoint — supports local dev (localhost), container (docker service name), and remote (ingress URL) environments | Configuration layering test across all three environments | Ongoing |

### Algorithmic Quality

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR24** | Hybrid uses deterministic weighted RRF. Public hybrid per-axis values are unweighted `raw(r)/raw(1)` on 0.0–1.0. Native single-axis scores keep their own meaning. All evidence-bearing surfaces preserve the same contributions and null/empty meanings. | With a non-unit axis weight, assert rank one yields internal `1/11` and public `1.0`, rank two yields public `11/12`, and only the composite changes when relative weights change on a fixture with different axis ranks. Compare each active presenter. | MVP |
| **NFR25** | The same query, data, configuration and active axes produce identical scores and order across runs and surfaces. Ties resolve by ordinal memory-unit ID. | G5 compares native REST with enrolled McpCli CLI JSON on fixed golden vectors and 100 repeated queries, including original Evidence Packet bytes. The shared offline serializer fixture checks the contract. L1 adds end-to-end MCP parity for the Phase 1.5 surface. | MVP on active surfaces, MCP at Phase 1.5 |
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
| **NFR31** | A developer following the README on a clean machine can start AppHost, create a tenant and case, ingest a file or URL, and obtain the first correct search result through enrolled McpCli operations in <30 minutes. The clean machine for G3 and L2 is a fresh Windows 11, macOS or Ubuntu LTS user profile with only Docker and the SDK pinned in `global.json` preinstalled. Assume broadband access. Obtain the embedding-provider key before timing starts and enter it at the prompt. Count Aspire installation and image pulls in the elapsed time. For L2, stage the pinned starter checkout and use the time-zero state in the launch stopwatch. | Story 35.7 records the date, OS, machine, tool versions, raw steps and elapsed time. Record missing operations or a missed threshold as a blocker. Compatibility runs are logged separately. The target run is pending. [Walkthrough log](../../docs/dev/quickstart-walkthrough-log.md) | MVP |

### Future web, freshness, and telemetry

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR32** | When a web capability is activated, it meets WCAG 2.2 AA and supports the complete trust workflow by keyboard. Focus remains visible and unobscured. State is not communicated by color alone, and recovery and status changes are announced accessibly. Light and dark themes, forced colors, and reduced motion preserve meaning. Genuinely tabular or graph content has an equivalent ordered representation. Author-sized pointer targets meet WCAG 2.2 SC 2.5.8. Activation requires a dated route/state evidence matrix covering viewport, 200% text resize, 400% zoom/320-CSS-pixel reflow, focus-not-obscured, theme, forced colors, reduced motion, input mode, supported browser/assistive technology (including NVDA on supported Edge/Chrome), keyboard start/end focus, artifact, tester/date, defect or waiver owner, and release disposition. Automated component/axe checks do not replace manual browser/AT evidence. | Dated UX/browser/AT evidence matrix by activated route and representative state | Future web (Epic 17+) |
| **NFR33** | Evidence Packet freshness semantics: authoritative `current`, `aging`, `stale`, and `unknown` thresholds, transitions, disclosure, and recovery actions, versioned in the Evidence Packet contract and activated per delivery surface. | Contract + surface tests | Ongoing / per surface |
| **NFR34** | Access telemetry has an explicit Platform Operations owner, configured TTL, observable purge progress, tenant-erasure mapping, bounded recovery, sanitization, and dated accepted debt for unsupported retention profiles. Qualification/configuration fails closed before Production activation. After admission, access-telemetry delivery remains non-blocking for accepted product writes within the approved telemetry-failure bound. The system surfaces degradation when delivery exceeds that bound. It never changes domain truth or claims a tamper-evident audit trail. | Production-admission tests, runtime outage/degradation tests, erasure/TTL runbook evidence, and payload-sanitization checks | Ongoing |
| **NFR35** | When a web capability is activated, on representative Evidence Packet and graph fixtures the surface targets an initial usable trust packet within 2.5 seconds, p95 local interaction response within 200 ms, cumulative layout shift no greater than 0.1, and initial route payload no greater than 256 KiB. Architecture review may revise these budgets before activation but must replace them with explicit measured values rather than removing the gate. | Measured lab evidence | Future web (Epic 17+) |

### File/URL ingest freshness

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR36** | File/URL ingest freshness: under normal admitted load with all three projections healthy and the provider not rate-limiting, a ≤10 KB unit moves from `pending` to `indexed` within 60 seconds and a ≤1 MB unit within 5 minutes. When throttled or queued by NFR13/NFR22 controls, the current state and delay are visible; freshness degradation is never silent. `[ASSUMPTION: the 60 s / 5 min budgets were derived from NFR5 throughput; architecture may tighten them, not remove them.]` | Timed p95 test over 100 units plus a concurrent noisy-neighbor batch/repair tenant proving interactive fairness | MVP |

### Active CLI accessibility

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR37** | CLI output uses the stable reading order scope → result → sources → reasoning/state → recovery, with text labels for every state, axis, score meaning, omission, progress stage, and recovery. Human output is bounded and wrappable; wide tables have a complete linear alternative; redirected output is deterministic and does not depend on terminal-control sequences. Progress emits durable stage/failure lines with last update and delay reason; cancellation and timeout are explicit; duplicate delivery never prints a second created unit. Users can complete prompts, confirmations, help interactions, and error-recovery actions by keyboard alone. Secrets and restricted identifiers never enter copied output, accessible names, diagnostics, or suggestions. Human, table, JSON, stderr, and exit-code forms preserve the same semantics. | Automated narrow-terminal, redirected-output, no-color, duplication, timeout/cancellation, secret-sanitization, and semantic-parity tests; keyboard-only manual walkthrough across the Phase 1 command tree | MVP |

## Open Questions

1. **Date reset open.** On November 20 the audit established that the missed October 31 checkpoint makes the previously approved December 1 gate unreachable. The Administrator may use the single sprint-change reset once reviewer availability, corpus preparation, the target CLI delivery plan and engineering capacity support a new date. January 1 remains the previous derived launch date. A replacement requires an explicit decision.
2. `[PHASE-BLOCKER / G1]` Administrator stewards the Phase 1 corpus and must name two external human reviewers, verify that neither curated the corpus nor implemented the evaluated behavior, and confirm their availability before freezing labels. Revisit when both reviewers are available and before proposing a new gate date.
3. Compact scope, source, relevance caveat, freshness/degradation, omission, and recovery remain visible on every evidence-bearing surface. Should detailed ranks, weights, matched terms, and graph diagnostics remain opt-in (`--explain`, FR19) or become default at web activation? Owner: UX. Revisit: Epic 17 activation SCP.
4. **Closed 2026-09-08.** Licence is MIT (Jerome). The PRD's Apache 2.0 text was replaced; the repository already complies. Remaining action: publish the MIT no-relicense sentence in the README (handoff, `addendum.md`).
5. FR32 stays absolute (single-case ownership) for MVP and Phase 1.5; brief R3 (lightweight cross-case references) may be restored only as a Phase 2 FR via sprint change. Owner: Jerome. Revisit: Phase 2 planning.
6. Closed 2026-09-08: ingest-from-anywhere (cloud/git/image/video) is an explicit deferral — see Non-Goals. Re-open only by sprint change.
7. **Closed 2026-09-12.** The optional Python `ai-agent` sidecar remains architecture-only and deferred until a selected product feature requires it; no current FR/NFR promotes it.
8. **Closed 2026-09-12.** Preserve the exact lowercase V1 wire values `queued`/`extracting`/`embedding`/`indexing`/`indexed`/`failed` until a versioned break; the Glossary maps product labels without changing the wire contract.
9. **Closed 2026-09-12.** AD-14 is phase-qualified and ratified by Jerome: MVP FR43 remains a tenant-scoped, explicitly acknowledged degraded rebuild that never claims zero downtime; Phase 2 owns non-disruptive embedding/schema migration; Phase 3 owns backend replacement. No MVP implementation is retroactively claimed to meet the Phase 2/3 contract. Recorded in the architecture spine's `ratifications` frontmatter and AD-14.
10. **Closed 2026-09-12.** The architecture spine now binds FR1–FR75 / NFR1–NFR37 / G1–G6. FR75's epoch-aware durable-idempotency contract was already adopted in AD-3 and AD-4 and is now traced through their `Binds:` lines and a capability-map row; NFR37 gained normative text in AD-12 plus an Active CLI output contract convention; G6 gained AD-20 and a five-column gap ledger carrying disposition and criticality. Note: the spine has no FR→AD trace table — traceability is per-AD `Binds:` lines plus the capability map.

11. **Story 34.58 storage allowance, 2026-11-21.** The approved accounting unit is the logical payload byte retained in the tenant content store. Count each live tenant-owned stored payload once, including source or fetched bytes, extracted text, retained chunk text and retained vectors. Distinct retained representations each consume their own bytes. A retry that writes the same payload identity consumes no additional allowance. Freed records release their bytes. Backend overhead and operational headroom are measured separately for sizing. The Administrator and capacity owner will declare one numeric limit for the supported deployment profile after validating tenant count, workload and usable storage. The current sample establishes the accounting behavior for the measured writes. Story 34.58 registration and runtime enforcement remain pending. [Storage measurements](../../docs/operations/capacity-planning.md#tenant-content-store-measurement--21-november-2026)

## Assumptions Index

- `[ASSUMPTION]` Generic Marten/Wolverine/Axon zero-code remains an experiment until a named spike passes the DAPR-generic kill switch. (§ Executive Summary)
- `[ASSUMPTION]` Cloud-drive, git, image, and video ingest stay deferred until an owner names a phase. (§ Non-Goals)
- `[CHECKPOINT MISSED 2026-10-31]` The prerequisite review closed without the independent reviewers required for G1. See the release decision record and Open Question 2.
- `[DATE RESET OPEN 2026-11-20]` December 1 was the ratified gate date and January 1 its derived launch date. The Administrator owns the single reset under Open Question 1.
- `[ASSUMPTION]` Thesis-stress slice ≤ 20% of N; explicit + metadata-carried edges ≥ 30% of non-`contains` edges for the run to count as a three-axis (not similarity-graph) test. (§ Measurable Outcomes › Thesis-gate protocol)
- `[ASSUMPTION]` NFR36 file/URL freshness budgets (60 s for ≤10 KB, 5 min for ≤1 MB) are derived from NFR5 throughput; architecture may tighten, not remove. (§ NFR36)
- `[ASSUMPTION]` The NFR "implemented" status row reflects absence of recorded evidence in planning artifacts, not proof of absence in CI. (§ Non-Functional Requirements › NFR delivery status)
- `[ASSUMPTION]` NFR33 is Evidence Packet freshness (rerun SCP) and NFR35 is future-web interaction performance (remediation-batch SCP) — the two August 2026 patch sets used the same NFR33 id for different requirements. (§ NFR33 / NFR35; index-only by design — the collision is history, not a live requirement)

- `[ASSUMPTION]` The initial NFR13 reference profile uses ten measured minutes, a per-type mixed/solo p95 ratio ≤1.10, >100 completed small ingests/min, ≥250 repaired units, a 5-second admission bound and 1,000/10,000 pending caps. Revisit these budgets through change control after load-test calibration.

Resolved in this Update (no longer assumptions): benchmark N (now a protocol number, G1); Phase 1 graph population (now a decision in MVP Feature Set).
