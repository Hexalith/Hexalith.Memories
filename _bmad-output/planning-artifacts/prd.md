---
title: Hexalith.Memories
status: change-controlled
created: 2026-03-22
updated: 2026-09-12
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

**Author:** Jerome
**Date:** 2026-03-22
**Updated:** 2026-09-12 — incorporated product-observable changes from the final architecture and UX spines: current-revision projection completion, durable idempotency (FR75), internal tenant authorization, case-partitioned tenant-wide graph search, capability-aware degradation/readiness, verified tenant erasure, telemetry failure posture, workload fairness, active-CLI accessibility (NFR37), Evidence Packet/no-result semantics, and exact CLI delivery evidence. Architecture reconciliation remains partial until AD-14 is phased and the spine binds FR75/NFR37. Prior (2026-09-08): thesis/launch gates, status registers, ingestion vocabulary, MIT licence, and confirmed release dates. Mechanism/topology detail lives in `addendum.md`; presentation/composition detail lives in the UX spines. Active work breakdown lives in `epics.md` and `sprint-status.yaml`.

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

**Current release posture:** no-go. Retaining 2026-12-01 as the Phase 1 decision date for the expanded G1–G6 gate is an explicit assumption. Required evidence is absent, no successor stories own the missing work, and architecture must still resolve AD-14 phasing and extend its binding to FR75 and NFR37. The Release decision record and Open Questions 1, 2, 9, and 10 are authoritative.

### What Makes This Special

**Phase 1 proves hybrid retrieval plus non-retrofittable isolation.** The graph mechanics — typed edges, traversal, gap markers, chronological ordering (FR46–FR52) — ship in Phase 1 because they are the graph axis; what Phase 1 cannot promise is that the graph *contains* causal edges for a folder of files. The sequenced bet — queryable causality from EventStore conventions — is Phase 1.5, not the thesis gate. Event-sourced systems already capture *why* things happen; Memories makes CausationId/CorrelationId queryable once the EventStore product integration ships: *"What happened because of this deployment?"* walks the graph. Happy path is subscription plus conventions, not zero mapping and zero configuration.

EventStore CloudEvent auto-index is the first *product* proof point for causal intelligence. Separately, Hexalith.EventStore is already the **domain source of truth** for Case / MemoryUnit / Tenant writes (current MVP consistency contract). Package/runtime pins for EventStore bits are architecture-owned (Epic 28). Those three "EventStore" meanings must not be collapsed.

Two additional differentiators compound later:

- **Team-scoped collaborative memory** — case/folder containers in MVP; threaded discussions, memory diffing, and onboarding briefings are Phase 2. Do not market them as unique until they ship.
- **Integrated system over duct tape** — three-axis retrieval, case-scoped graph, tenant isolation, confidence tracking, and async ingestion work together. Hybrid-search and GraphRAG incumbents (Elasticsearch/OpenSearch hybrid, Azure AI Search, Weaviate, Vespa, Microsoft GraphRAG, LlamaIndex PropertyGraphIndex, Neo4j LLM graphs) are the comparison set, not only Mem0/Zep/LangChain.

## Project Classification

- **Project Type:** Developer Tool / API Backend (NuGet packages + DAPR service + CLI + MCP server)
- **Domain:** AI Infrastructure / Knowledge Management
- **Complexity:** Medium-High — driven by three-axis query fusion, DAPR workflow ingestion, multi-tenancy with tenant-scoped isolation, and EventStore domain + product integration
- **Project Context:** Brownfield / change-controlled (greenfield thesis recorded March 2026; implementation and epics are the living work breakdown)
- **License:** MIT (decision, Jerome, 2026-09-08 — supersedes the March 2026 Apache 2.0 decision; matches `LICENSE`, file headers, and `PackageLicenseExpression`). Public README commitment: the project will not switch to a restrictive license.

## Glossary

Downstream workflows and readers must use these terms exactly.

- **Memory unit** — The stored unit of knowledge (document chunk, event, annotation) owned by exactly one **Case**.
- **Case** — Team-scoped container of memory units and case-scoped graph edges. Not an authorization principal.
- **Tenant** — Isolation boundary. Access is authorized by tenant claims on an authenticated principal.
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
- **Ground truth:** Graded labels collected *after* queries exist and **frozen before any scoring run**, by Jerome plus 2 independent reviewers. Agreement statistic is **Cohen's κ ≥ 0.6** (pairwise, reported per reviewer pair). Below that, relabel and re-freeze before the first scoring run.
- **Automated scoring:** NDCG@10 per topic, reproducible under NFR26.
- **Dispute resolution:** After scoring, labels are immutable for that run. A reviewer may annotate a disagreement; if the annotations justify relabelling, the run is discarded, the label set is re-frozen, and a *new* run is recorded with the reason. A run's pass/fail is never changed by editing labels after the fact.
- **Fallback:** If independent reviewers are unavailable, automated scoring may still run but **does not** satisfy the thesis hard gate — it is a documented downgrade of gate confidence, not a substitute.

**Evidence to date (diagnostic, not the gate):** The Epic 2 / Epic 26 benchmark (`tests/Hexalith.Memories.Benchmarks`, last run 2026-07-16) reports 8/8 hybrid wins. It does not satisfy the protocol above: N=8, every query is tagged as requiring all three axes, the corpus and labels are synthetic, the control is the best *single* axis rather than BM25+semantic, and the win rule is any positive ΔNDCG@10. It is a reproducibility and regression check (NFR26), and the PRD records it as such.

**If the threshold is not met, the kill switch executes these actions, in order, with no "or":** (1) stop fusion R&D as default hybrid; (2) remove graph from the default `--axis hybrid` — hybrid becomes BM25+semantic, and graph stays available only as an explicit axis and through `traverse` (FR47); (3) FalkorDB is retained for explicit traversal and is cut from general search scoring; (4) README and executive summary reposition to "BM25+semantic search with an explicit graph" *before* any other decision; (5) the Phase 1.5 launch gates L1–L3 are **not evaluated** — the launch decision date is cancelled and a sprint-change proposal must re-scope the product before a new date is set; (6) re-scope and re-estimate. "Reposition and keep building the same system" is not a pass.

**Causal Chain Completeness:**
For 95%+ of EventStore events with known CausationId/CorrelationId chains, graph traversal returns the complete causal path. Validated by automated tests against a named set of known event chains. **Fixture of record:** the Epic 9 event-ingestion integration fixture (`tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs` and the graph gap/edge-type fixtures under `tests/Hexalith.Memories.IntegrationTests/Graph/`), extended with a labelled chain set of at least 20 chains before L3 is evaluated. The `samples/02-eventstore-integration/` walkthrough named in Developer Experience is a Phase 1.5 deliverable that does not exist yet and is not the L3 fixture. **Phase 1.5 launch gate**, not a Phase 1 thesis gate.

**Phase 1 thesis go/no-go (CLI proof of hybrid + isolation):**

| # | Criterion | Requirement |
|---|---|---|
| G1 | Hybrid vs BM25+semantic passes the thesis-gate protocol above (N ≥ 50, ΔNDCG@10 ≥ 0.02, κ ≥ 0.6) | Must pass |
| G2 | Zero cross-tenant data leaks (NFR8) | Must pass |
| G3 | Phase 1 onboarding <30 minutes (NFR31: README/AppHost → first CLI search) | Must pass |
| G4 | Case ownership/isolation tests (FR32–FR34/NFR8): case-scoped queries never escape their case; tenant-wide hybrid fixtures span cases with mandatory attribution while every graph seed, node, edge, and path remains case-local | Must pass |
| G5 | Fusion explain is deterministic (NFR24–NFR26) | Must pass |
| G6 | MVP contract closure: every MVP requirement not marked **Verified against its current wording** — including not-started, partial, hardening, implemented-without-recorded-evidence, and re-verification rows — has current evidence or a phase-specific exception approved by product and architecture. Every architecture-critical active-foundation gap follows the same rule; every gap/exception has an owner and tracking entry in `sprint-status.yaml` | Must pass |

All six gates are hard gates; there is no soft tier. Phase 1 does **not** require MCP, EventStore CloudEvent auto-index, or causal-chain completeness.

**Phase 1.5 launch go/no-go (agent + EventStore product integration):**

| # | Criterion | Requirement |
|---|---|---|
| L1 | MCP end-to-end on a held-out query set (≥10 topics not used in G1 labelling): 100% of responses within token budget (FR23/FR54/FR58) **and** ≥ 8/10 topics where the agent's answer cites at least one memory unit graded relevant for that topic (scorer: the frozen G1 labels; agent: the reference configuration in `samples/03-mcp-agent/` once it exists) | Must pass |
| L2 | EventStore product integration: launch stopwatch below completes in <30 minutes on a clean machine as defined in NFR31 | Must pass |
| L3 | Causal chain completeness ≥95% on the named known-chain fixture | Must pass |

**Launch stopwatch (L2), timed as one numbered script, clock starts at step 1:**
1. `dotnet add package Hexalith.Memories.EventStore` in an existing EventStore-based service.
2. Add the DAPR pub/sub subscription (topic + `/events/ingest` route) per the getting-started guide.
3. Provide the embedding-provider secret through the DAPR Secrets API (OpenBao dev instance in the sample AppHost).
4. Boot the sample stack (`samples/02-eventstore-integration/`, `dotnet run --project <sample AppHost>`; until that sample exists the stopwatch cannot be run and L2 is not evaluable).
5. Publish one domain event; run `memories search query --tenant <t> --query "<term>"`; a result attributed to that event stops the clock.

**Release decision record (2026-09-08):**

| Decision | State | Owner | Decision date | If it fails |
|---|---|---|---|---|
| Thesis increment (Epics 0–8) | Epics 0–8 are `done` in `sprint-status.yaml` (2026-07-16), **but the Phase 1 CLI surface is incomplete**: `ingest`, `traverse`, and `case` are top-level placeholders; required tenant/status operations are absent; the two-axis control (FR25) and graph auto-seeding (FR17) do not exist. The increment is *closed in tracking*, not *complete against this PRD* | Jerome | — | n/a — delivery is not validation |
| Gate prerequisites (missing CLI operations, two-axis control, graph seeding, N ≥ 50 labelled corpus, and every G6 contract gap/exception) | **No owning successor stories exist** in `epics.md` / `sprint-status.yaml` as of this Update; G6 includes every MVP row without current-wording verification, not only architecture-led gaps. Architecture must also ratify the AD-14 phase exception and extend its binding through FR75/NFR37 | Jerome (sprint planning) for stories/evidence; Architecture for binding/exception | Every prerequisite has current evidence or a named story/owner or approved exception in `sprint-status.yaml` by 2026-10-31 `[DERIVED]`, or the gate date below is declared unreachable and reset by sprint change with the reason recorded | Missing evidence, unowned work, or an unratified exception makes G6 fail; the "not run" outcome below applies |
| Thesis gate G1–G6 | G2: suite exists (Epics 5/20); re-run against the restated NFR8 owed after Epic 24. G3: procedure exists (Story 7.4 walkthrough), **no timed run recorded** (`docs/dev/quickstart-walkthrough-log.md` is empty) and blocked on missing CLI operations (NFR31). G4/G5: automated suites need current wording. G1 **not yet run**. G6 **cannot pass** while architecture-critical gaps remain unowned or exceptions unratified | Jerome | **2026-12-01** (rule confirmed by Jerome, 2026-09-08: thesis-gate/release decision = launch date − 1 month) `[ASSUMPTION: the window to 2026-12-01 for reviewers, corpus freeze, ≥50 labelled topics, implementation, re-verification, and governance closure is a schedule assumption]` | **G1 fail:** execute fusion kill-switch actions (1)–(6). **G2–G6 fail:** no release and no Phase 1.5 launch; remediate or re-scope through sprint change. **Not run by the date:** no verdict/no launch; one reset is permitted by sprint change, and a second miss is treated as a fail |
| Phase 1.5 launch L1–L3 | Epics 9–10 code delivered; gates not evaluated; L2 path has recorded gaps (deferred-work DW-713: no full-stack EventStore-originated publish proof; DW-728: `eventstore` resource cannot start under SDK 10.0.400-only environments); `samples/` does not exist | Jerome | **2027-01-01** (confirmed by Jerome, 2026-09-08) | **No launch.** MCP and EventStore packages stay marked preview and are not announced; the date is not moved without a sprint-change proposal |

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
| 6 | CLI — every row tagged Phase 1 in the **CLI surface** table (CLI Specification); that table, not this row, is the verb list | Thesis validation tooling |
| 7 | Benchmark Suite (thesis-gate protocol: N ≥ 50, BM25+semantic control, ΔNDCG@10 ≥ 0.02) | Thesis validation (G1) |

**Phase 1 graph inventory (decision, 2026-09-08):** the graph *mechanics* — typed edge taxonomy, traversal with depth and edge-type filters, gap markers, chronological ordering, confidence promotion (FR46–FR52) — are MVP and shipped by Epics 1 and 4. The graph *population* in Phase 1 is what file/URL ingest can create: `contains` (case membership), `references` (explicit link or AI-inferred similarity), `annotates` (FR37), and `caused_by`/`correlated_with` only when ingested metadata already carries CausationId/CorrelationId. Automatic causal population from an event stream is Phase 1.5 (FR59–FR62). Hybrid fuses *available* axes; the PRD does not claim causal completeness on a folder tree, and the thesis-gate corpus must reflect the Phase 1 population, not a synthetic causal graph.

**Note:** DAPR infrastructure is scaffolding built as part of features 1–5, not a separate work item. README ships with MVP as the NFR31 vehicle. A help entry backed by `NotImplementedCommand`, or target grammar absent from the command tree, is not coverage — see the CLI Specification for exact delivery status.

### Phase 1.5 — Fast-Follow (launch decision date = thesis-gate/release decision date + 1 month; if either date moves, the other is re-derived by sprint change)

| # | Feature | Validates |
|---|---|---|
| 1 | EventStore product integration package `Hexalith.Memories.EventStore` (DAPR pub/sub through the Memories Server sidecar, auto-discovery, dual embedding, causal chains) | Launch gates L2 + L3 |
| 2 | MCP Server `Hexalith.Memories.Mcp` (search, ingest, traverse, case-info with token-budget awareness) | Launch gate L1 |
| 3 | CLI expansion: `explore` and EventStore diagnostics (the Phase 1.5 rows of the CLI surface table; `handlers list/mismatches` already shipped in Story 9.3) | Full developer experience |

The Memories Server is the sidecar-managed event subscriber. Hexalith modules publish CloudEvents to the configured DAPR pub/sub topic; the server sidecar delivers them to `/events/ingest`, where source-prefix routing maps events to tenant/case memory. Modules should not bypass this path with direct REST pushes for domain event streams.

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

Durations and counts inside journeys ("14 minutes", "47 documents", "8 seconds") are illustrative narrative, not measurements; gates cite NFR and G/L numbers only. All CLI examples use the one shipped grammar: `memories search query --tenant <t> --query "<q>" [--case <c>] [--axis <a>] [--explain]`.

### Journey 1: Alex — "Zero to First Search" (Phase 1.5 launch path)

> **Phase:** 1.5 EventStore product integration. Not the Phase 1 thesis success path — that is Journey 9.

Alex has been building a claims processing platform on Hexalith.EventStore for eight months. The system processes 50,000 events per day across six aggregate types. Last sprint, the product owner asked: "Can the AI assistant explain why a claim was denied?" Alex spent three days duct-taping Qdrant and LangChain together. It worked for the demo. Then the security review asked about tenant isolation, and the whole thing collapsed.

**Opening Scene:** Alex finds Hexalith.Memories linked from the EventStore documentation. The README shows a 30-second demo: three commands, events appear searchable. Alex thinks "that can't be right" and opens the getting started guide.

**Rising Action:** `dotnet add package Hexalith.Memories.EventStore` — familiar. DAPR subscription config — two lines in `appsettings.json`. The embedding key goes into the sample AppHost's OpenBao dev store, and `dotnet run --project Hexalith.Memories.AppHost` boots Redis + FalkorDB + the server in under a minute. Alex publishes a test event to the DAPR topic. This is the launch stopwatch (L2), steps 1–5.

**Climax:** `memories search query --tenant claims --query "claim denied"`. Results come back. Actual results. With the CausationId chain showing which command triggered the denial. It's been 14 minutes — inside the 30-minute L2 clock. Alex stares at the terminal. The three-day duct-tape project just became a 14-minute setup. *That can't be right* — but it is.

**Trust Deepening:** Alex runs the same search with `--explain`. The output breaks down the result: a syntactic rank contribution for "denied", a semantic rank contribution for "claim rejection", and a graph edge from the DenyClaim command through to the original SubmitClaim event. Three axes, one query, RRF contributions rather than raw magnitudes. Alex understands *why* each result appeared, not just *that* it appeared.

**Resolution:** By end of day, Alex commits the integration code. The duct-tape Qdrant solution gets deleted. Alex goes home on time.

**Capabilities revealed:** EventStore auto-integration, CLI search with --explain, causal chain traversal, <30 min onboarding, debug-first DX.

---

### Journey 2: Alex — "Something's Wrong" (Debug Path)

Two weeks after shipping, Alex gets a Slack message: "The AI assistant says there's no information about the Henderson claim, but I filed it yesterday."

**Opening Scene:** Alex opens a terminal. `memories search query --tenant claims --case claims-q1 --query "Henderson"` returns zero results. Not good.

**Rising Action:** The empty result classifies the likely causes instead of fabricating per-case status. Alex runs the shipped `memories status telemetry --tenant claims`, then repeats the search with `--explain`; target `status --case/--failed` diagnostics are not currently registered. The output shows that no selected axis found Henderson and names ingestion/handler investigation as the next safe action.

Alex runs `memories handlers list` and `memories handlers mismatches --tenant claims`, then checks the DAPR subscription logs. The ClaimSubmitted event for Henderson was published but the Memories handler threw a serialization error — a new field added last sprint broke the auto-discovery mapping. The error message names the unknown event type and these implemented diagnostic commands.

**Climax:** Alex registers the V2 handler, triggers a replay of the missed events, and within seconds the Henderson search returns the full claim with causal chain intact.

**Resolution:** Alex adds a monitoring alert on handler registration mismatches. The debug-first DX — clear error messages, `--explain`, telemetry, handler listing, and mismatch diagnostics — turned a potential hours-long investigation into a 15-minute fix.

**Capabilities revealed:** CLI diagnostics (status, explain, handlers list), clear error messages, event replay, handler registration, debug-first developer experience.

> **Scope note:** `memories handlers list`, `memories handlers mismatches`, and event replay are Phase 1.5 EventStore product-integration capabilities (FR59–FR62, Epic 9), not MVP Feature #3 (hybrid search). Target case/failed status remains absent and is not presented as shipped.

---

### Journey 3: Alex — "Wiring Up the AI Assistant" (MCP Integration, Phase 1.5)

Alex has the memory server running and CLI working. Now the product owner wants the team's AI assistant to use it.

**Opening Scene:** Alex opens the MCP tool documentation. Four tools: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`. Each has typed parameters with descriptions.

**Rising Action:** Alex adds the MCP tool definitions to the AI assistant configuration. First test: "What happened with claim 4821?" The assistant calls `search_memory(tenantId="claims", query="claim 4821", case="claims-q1", axes="Hybrid")` and returns results with source attribution. It works — but the response is too long, blowing past the context window.

**Climax:** Alex adds `tokenBudget=2000` to the call. The assistant calls again — same query, but the response is now concise: top-ranked results, truncated by relevance, with a note "8 additional results omitted." The assistant composes a focused answer. Alex tests three more queries, each producing grounded, sourced responses.

**Resolution:** The product owner asks "why was claim 4821 denied?" and the assistant walks the causal chain: SubmitClaim → FraudCheckTriggered → FraudScoreExceeded → ClaimDenied. Sourced, attributed, traceable. The AI feature ships to the team by end of week.

**Capabilities revealed:** MCP tool definitions, token-budget-aware responses, multi-axis search control, source attribution, assistant configuration workflow.

---

### Journey 4: Marcus — "Brief the New Person" (Phase 2)

> **Phase:** 2 (briefing, annotations-as-onboarding). Case membership metadata (FR28–FR29) may exist earlier; it does not grant authorization and does not make this journey MVP.

Marcus leads a team of seven working across three active cases. Sarah, a senior developer, left last month. Her replacement, Tomás, starts Monday. Marcus has spent every previous onboarding doing four hours of tribal knowledge transfer, walking through Confluence pages that are six months out of date.

**Opening Scene:** Friday afternoon, Marcus creates Tomás's access to the three cases: `memories case add-member --tenant acme --case project-alpha --user tomas` (membership is attribution metadata; Tomás's *access* comes from his tenant claims). He does the same for project-beta and the incident-response case.

**Rising Action:** Monday morning, Tomás opens the AI assistant and types: "Brief me on project-alpha." The assistant calls `search_memory(tenantId="engineering", query="project overview and recent activity", case="project-alpha")` and composes a narrative: the project started eight months ago as a payment processing rewrite, hit a critical incident in February when the gateway provider changed their API, pivoted to a dual-provider architecture, and is currently in testing. Key decisions, who made them, and why — all sourced from events, documents, and team discussions in the case memory.

**Climax:** Tomás asks: "What led to the dual-provider decision?" The assistant walks the causal chain: GatewayTimeoutEvent → IncidentDeclared → ArchitectureReviewDiscussion → DualProviderProposal → ApprovedByMarcus. Tomás understands not just *what* the architecture is, but *why* it exists. In 20 minutes, not four hours.

**Recovery beat:** Tomás notices the briefing says "approved by Marcus on February 12" but the PR was actually merged on February 14. He clicks "show sources" — the approval event is dated February 12, but the implementation PR landed two days later. The briefing was accurate about the *decision*, just not the *implementation*. Tomás flags the discrepancy, and the memory unit gets an annotation clarifying the timeline. The system is self-correcting.

**Resolution:** Marcus checks in after lunch. Tomás is already reviewing PRs with full context. Marcus didn't spend a single minute on knowledge transfer. He realizes he has his Friday afternoon back for the first time in a year.

**Capabilities revealed:** Case member management, case briefing via MCP, causal chain narrative, source attribution with verification, memory correction annotations, knowledge health visibility.

---

### Journey 5: Kenji — "New Tenant, No Drama" (MVP)

Kenji manages the DAPR infrastructure for three business units. The compliance team just approved a fourth business unit for the AI platform, and they need their own isolated memory space by Thursday.

**Opening Scene:** Kenji opens the CLI: `memories tenant create --id bu-compliance --display-name "Compliance Unit"`. The command provisions tenant-scoped Redis/FalkorDB resources (indexes plus backend principals). Isolation *outcome* is NFR8; the security boundary is tenant-scoped principals, not index names alone. It takes 8 seconds.

**Rising Action:** Kenji runs the tenant isolation verification: `memories tenant verify --id bu-compliance`. Automated checks confirm: search from bu-compliance context returns zero results from other tenants, ingestion into bu-compliance is not visible from other tenant contexts. All green.

**Failure beat:** Next month, a new intern accidentally runs `memories search query --tenant bu-operations --query "test"` with a token whose tenant claim is bu-compliance. `--tenant` is an MVP search flag that names the *requested* tenant; authorization comes from the token's tenant claims (NFR11). The CLI returns a tenant-mismatch error naming the authenticated tenant. The isolation holds. Kenji sees the rejected request in access telemetry (not a tamper-evident audit trail) with who, when, and what was attempted.

**Resolution:** Kenji's Thursday deadline was met in under 10 minutes. The monitoring dashboard shows all four tenants healthy, isolated, with clear resource consumption per tenant.

**Capabilities revealed:** CLI tenant provisioning, isolation verification (NFR8), boundary violation errors, access telemetry.

---

### Journey 6: Kenji — "Time to Scale" (Phase 3)

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
2. Agent receives the current tool schemas: `search_memory` requires `tenantId` and `query`, with optional `case`, `axes` (`Syntactic`/`Semantic`/`Nl`/`Hybrid`), `tokenBudget`, and `explain`; graph-detail work uses `traverse_relations`

**Query Cycle:**
1. User prompt arrives requiring organizational context
2. Agent calls `search_memory(tenantId="engineering", query="what led to the API redesign?", case="project-alpha", tokenBudget=2000, axes="Hybrid")`
3. Memory server executes hybrid search, fuses available axes under FR17, and truncates to the token budget
4. For graph detail, the agent calls `traverse_relations(tenantId="engineering", from="<result-id>", caseId="project-alpha")`
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

> **Target-state note:** This is the NFR31 Phase 1 manual path. Today `tenant create` is absent and the `case`/`ingest` groups are placeholders, so current CLI output must report unavailability and exit safely instead of printing the success copy below.

Alex has the stack running. Redis is up, FalkorDB is up, the DAPR service is healthy. But there's nothing in it yet.

**Opening Scene:** `memories search query --tenant pilot --query "anything"` returns: `No results. This tenant has no memory units yet. Get started: 'memories ingest <file>' to add your first document, or run 'memories quickstart' for a guided setup. Follow the README quickstart for the manual path.` (Event auto-indexing is not offered in this hint until Phase 1.5 ships.)

**Rising Action:** Alex creates the first case: `memories case create --id claims-pilot --display-name "Claims Pilot"`. The CLI responds: `Case 'claims-pilot' created. 0 memory units. Start building knowledge: 'memories ingest <file|url|directory> --tenant pilot --case claims-pilot'.` Not an error, not a blank screen — a clear next step.

Alex runs `memories ingest ./sample-claims/ --tenant pilot --case claims-pilot`. The CLI shows a progress indicator: 47 documents ingested, 47 memory units created, embedding in progress. Then: `Done. 47 memory units indexed. Try: 'memories search query --tenant pilot --case claims-pilot --query "claim"'`

**Climax:** First search on real data: `memories search query --tenant pilot --case claims-pilot --query "water damage"`. Three results, ranked by hybrid (RRF) score. The system works. The empty state is gone, and Alex never felt lost getting here. Event publish to a DAPR topic is Journey 1 / Phase 1.5, not this climax.

**Resolution:** Alex shares the README quickstart experience in the team channel. Two other developers set up their own cases by end of day.

**Capabilities revealed:** Helpful empty state messages, README quickstart, optional interactive quickstart polish, batch ingestion from directory, clear progress feedback, case creation, first-search experience.

---

### Journey 10: The Contributor — "From Bug Report to First PR"

Dani is a .NET developer at a fintech startup. They adopted Hexalith.Memories three months ago for their transaction monitoring system. It's been working well, but Dani hit a rough edge: the CLI's `memories search query --explain` output doesn't show which embedding model was used for the semantic match, making it hard to debug relevance issues when testing different providers.

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
| **Storage** | Data durability, isolation, encryption at rest | EventStore truth plus Redis/FalkorDB projections |
| **Interpretation (Memories)** | Accurate embeddings, correct causal chains, documented score semantics, complete traversal of *indexed* edges with explicit gap markers for missing nodes (FR49) | Relevance confidence is not factual reliability; see Glossary |
| **Application** | Decisions based on interpretations, compliance, user-facing representations, legal obligations | Denying a claim based on a causal chain, GDPR compliance |

Memories provides the primitives that *enable* compliance:

- **Tenant deletion** (`memories tenant delete`) completes only after product projections are purged, EventStore-held tenant content is irreversibly inaccessible with verification, and a durable access-telemetry erasure handoff is recorded. Replay, restart, and restore cannot resurrect the content; the deleted tenant ID cannot be reused. Retained opaque access telemetry follows NFR34's TTL rather than being presented as content or legal-audit evidence. **Limitation:** Cross-references to that tenant's data in *other* tenants' memory units are the application's responsibility to handle. The compliance guide must document this explicitly.
- **Tenant isolation** (tenant-scoped backend principals and indexes, NFR8) ensures no cross-tenant data leakage, a prerequisite for downstream compliance. It is a shared-cluster isolation tier, not separate processes or volumes per tenant (see `addendum.md`).
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
| NL score (**Phase 1.5**, event units only) | Natural-language-description vector similarity for single-axis `axis=nl`; rank contribution for hybrid search when `nl` is enabled | 0.0–1.0 | Single-axis explain uses cosine clamp with syntactic-hash attribution backfill; hybrid explain exposes weighted reciprocal-rank contribution. Not a Phase 1 retrieval axis: file/URL units have no NL description embedding (FR60). |
| Graph score | Proximity in the relationship graph (hop distance, edge weight) for single-axis search; rank contribution for hybrid search | 0.0–1.0 | Single-axis explain uses inverse hop distance with decay; hybrid explain exposes weighted reciprocal-rank contribution. Proximity magnitude is never fused directly. |
| Composite score (**Relevance confidence**) | Weighted reciprocal-rank fusion of available axes | 0.0–1.0 | Weighted RRF over available axis rankings, normalized against the best possible rank contribution |

Single-axis scores keep their axis-specific meaning. Hybrid per-axis scores are rank-contribution scores, not raw BM25, cosine, or graph-proximity magnitudes. The fusion weights and algorithm are documented and deterministic. `--explain` exposes the score semantics and fusion weights applied.

**Evidence Packet (cross-surface trust envelope):** The trust primitives defined across these requirements — composite confidence with per-axis breakdown (FR63), source/origin attribution (FR24), token-budget-aware omitted-detail handling (FR23), and graceful-degradation signaling (FR66), together with tenant/case scope, result state, freshness impact, and recovery guidance — are composed into a single shared response object referred to as the **Evidence Packet**. CLI JSON output, MCP tool responses, and future web UI composition use the same Evidence Packet contract. No surface defines confidence, degradation, omitted details, or recovery actions differently. Its versioned states are `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, and `pendingExpansion`; surfaces must not fabricate states that current mappers do not yet emit. Every evidence-bearing surface preserves the distinction between an unavailable axis, an excluded axis, and an available axis with no hits. Its concrete shape is owned by `Contracts.V1` (see Architecture) and its presentation semantics are elaborated in the UX Design Specification.

**Critical distinction: confidence scores measure query-result relevance, NOT factual accuracy or data completeness.** A score of 0.95 means the result is highly relevant to the query — it does not mean the underlying data is complete, correct, or current. This distinction must appear in:
- API reference documentation
- CLI `--explain` output (every explain result includes this caveat)
- Compliance enablement guide
- MCP tool response schema documentation

**Metadata confidence** is separate from search relevance: each metadata field on a memory unit tracks its origin (`human-declared` vs `ai-inferred`) and confidence (0.0–1.0). This distinguishes "the user tagged this as 'payment-related'" from "the embedding model inferred this is about payments."

**Memory unit provenance:** Every memory unit tracks `ingested_by` as a mandatory MVP field. At authenticated external ingress, provenance binds to the normalized `sub` principal; caller-supplied provenance is rejected or ignored. Trusted internal adapters may use only the canonical `system:*` identity derived from an authenticated app-ID allowlist and an explicit tenant grant. App identity, channel credentials, and display metadata never authorize access or override the authenticated principal. Tenant claims authorize access. Case membership is tenant-scoped domain metadata and does not grant authorization in the current phase.

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
| Three-axis fusion | Thesis-gate protocol per Measurable Outcomes (G1: N ≥ 50, hybrid vs BM25+semantic, ΔNDCG@10 ≥ 0.02, κ ≥ 0.6) — not yet run; the 2026-07-16 N=8 run is diagnostic | Execute named kill-switch actions if 80% misses |
| EventStore product integration | Launch stopwatch L2: `dotnet add package Hexalith.Memories.EventStore` + subscription + secret + boot + first event search | If the stopwatch exceeds 30 minutes, the conventions promise is broken — no launch |
| Causal intelligence | L3: 95%+ of the named known-chain fixture fully traversable | No launch |
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
| `Hexalith.Memories.Web` | Epic 17 conformance specimen (runnable web shell for accessibility/telemetry conformance); **not an activated product surface** — NFR32/NFR35 activate when a web surface ships (Phase 2) |
| `Hexalith.Memories.AccessTelemetry` | Access-telemetry host component |
| `Hexalith.Memories.AccessTelemetry.Clock` | Clock abstraction for access telemetry |
| `Hexalith.Memories.AccessTelemetry.Contracts` | Access-telemetry contracts |

The six rows mirror `nonPackableProjects` in `tools/release-packages.json`; that file is the source of truth if the two disagree.

Mechanism detail (dependency graph, Redis compatibility facade, composition root) is in `addendum.md` and architecture.

**Deployment Topology:**

External consumers connect through infrastructure-managed ingress (YARP, nginx, cloud API gateway — not application code). Internal services communicate via DAPR mesh.

- **LLM Agent** → ingress → MCP Server (DAPR sidecar) → DAPR → Memories Server
- **CLI / Third-party apps** → ingress → Memories Server (REST controllers)
- **Memories Server** → approved internal Redis/FalkorDB adapters → backend data-plane clients using Aspire-injected keyed connections; DAPR state remains the portable coordination path
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
| Authentication | External/delegated bearer authority is preserved across hops. Internal calls additionally require deny-by-default DAPR workload authorization and protected channels; authenticated app IDs map through one operator-owned allowlist to canonical `system:*` principals with explicit tenant grants (NFR10). Anonymous: enumerated health/DAPR infrastructure routes only. |
| Tenant context | Derived or verified server-side from authenticated authority. Payload tenant/case fields, app identity, and channel credentials cannot authorize by themselves. |
| Rate limiting | In scope: embedding-provider throttle (FR69), inbound request quotas by authenticated tenant, plus ingress. Not "deferred to infrastructure" as the only control. |
| Identity / provenance | Tenant claims authorize. Case membership is metadata. Provenance binds to normalized `sub` or the allowlist-derived `system:*` principal with an explicit tenant grant. |

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
| Readiness/liveness | Liveness reports process viability. Readiness requires valid authentication configuration, the DAPR control boundary, and EventStore command availability; a failed query backend degrades the affected capability while at least one selected safe axis remains usable (FR66/FR72), rather than making the whole server unready. |
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

**Observable ingestion states (the one vocabulary; Glossary "Ingestion state"):** `pending` → `extracting` → `embedding` → `projecting` → `indexed`, with `failed` as the terminal failure. Retry attempts, dead-letter, and repair are *detail on* `projecting`/`failed` (attempt count, last error, `repaired_at`), not additional states. `indexed` is emitted only after all three projections — search, vector, graph — acknowledge the same current authoritative revision under the active schema generation and embedding configuration. Stale or incompatible acknowledgements cannot complete a newer revision. In MVP "required projections" means all three; query-time degradation never promotes an incomplete revision.

**Pipeline Stages:**

| State | What happens | Owner |
|---|---|---|
| `pending` | Content accepted; EventStore commit durable | Workflow |
| `extracting` | Text extraction from content (PDF, URL, file) | Workflow activity |
| `embedding` | Call embedding provider API, get vector | Throttled by per-tenant rate-limiter actor |
| `projecting` | Write RediSearch, Redis Vector, FalkorDB as rebuildable projections; retries with backoff stay in this state | Workflow + compensation |
| `indexed` | All three projections acknowledge the current authoritative revision under the active schema and embedding configuration | Terminal success |
| `failed` | Error at any stage after max retries; error, stage, and attempt count preserved (dead-letter detail) | Visible via CLI; not silently dropped |

**Failure handling:**
- Failed units retry with exponential backoff (configurable max retries) while remaining `projecting` (or the failing stage)
- After max retries, units move to `failed` with error details preserved
- `memories status --case X` shows counts per state
- `memories status --failed` shows failed units with error details and stage
- FR13: no silent two-of-three searchable unit. Retry, repair, and EventStore replay converge through the same completion contract; a unit is not `indexed` until all three projections acknowledge the current revision and configuration, and query degradation cannot promote it.
- **File/URL freshness (NFR36):** under normal conditions a ≤10 KB file unit reaches `indexed` within 60 seconds of `pending`; degradation is reported, not silent.

**Runtime split:**
- **IngestionWorkflow:** stages, retry, compensation, Durable Task persistence (NFR17).
- **EmbeddingRateLimiterActor / CorpusStatisticsActor:** singleton/budget actors only.

### Interface Capability Parity Matrix

Not all capabilities map to all interfaces. **Capability alignment, not feature parity.** CLI is the operational superset. MCP exposes what LLM agents need (search, ingest, traverse, case info). Tenant management, diagnostics, and interactive features are CLI-only.

The single source of truth for which CLI verbs are Phase 1 versus Phase 1.5, and which are shipped, is the **CLI surface** table in CLI Specification. This matrix only maps capabilities to interfaces.

| Capability | CLI | MCP (Phase 1.5) | DAPR Service Invocation |
|---|---|---|---|
| Search (syntactic, semantic, graph, hybrid) | `memories search query` | `search_memory` | `SearchAsync` |
| Search with explain | `memories search query --explain` | `search_memory` (explain field) | `SearchAsync` (explain option) |
| Result / unit inspection | `memories search inspect`, `memories search lookup` | -- | -- |
| Content ingestion | `memories ingest` | `ingest_content` | `IngestAsync` |
| Graph traversal | `memories traverse` | `traverse_relations` | `TraverseAsync` |
| Case management (create, delete, members, activity) | `memories case` | `get_case_info` | `CaseAsync` methods |
| Tenant management | `memories tenant` | -- | `TenantAsync` methods |
| Tenant isolation verification | `memories tenant verify` | -- | -- |
| Ingestion status & failed units | `memories status` | -- | -- |
| Index/graph consistency | `memories consistency verify/inspect/repair` | -- | -- |
| Portable export (FR71) | `memories export case/tenant` | -- | -- |
| Interactive exploration | `memories explore` | -- | -- |
| Handler management | `memories handlers` | -- | -- |
| Guided quickstart | `memories quickstart` (shipped; the README quickstart is the NFR31 vehicle, the command is its scripted form) | -- | -- |
| Batch directory ingestion | `memories ingest <dir>` | -- | -- |

**Design rationale:** MCP exposes agent work. CLI exposes ops. DAPR service invocation is the internal programmatic API. FR53 is satisfied per active phase; a help entry backed by `NotImplementedCommand` is not coverage.

### CLI Specification

**Distribution:** .NET global tool (`dotnet tool install -g Hexalith.Memories.Cli`)

**Global options (all commands):** `--endpoint`, `--token`, `--format`, `--verbose`, `--telemetry`. Tenant scope is passed as `--tenant <id>` on tenant-scoped commands; it names the requested tenant, and the token's tenant claims authorize it (NFR11). There is no `tenant switch` — ambient tenant state was removed from the contract on 2026-09-08.

**CLI surface with phase and delivery status (this table is the FR53 source of truth; the Phase 1 rows are the thesis surface):**

| Command | Phase | Delivery status (2026-09-12, `RootCommandFactory`) | Acceptance output |
|---|---|---|---|
| `memories search query --tenant --query [--case] [--axis] [--max-results] [--explain]` | 1 | Shipped (Story 7.2) | Ranked results with Evidence Packet fields; `--explain` shows per-axis rank contributions and weights |
| `memories search inspect` / `search lookup` | 1 | Shipped (7.2 / 18.5) | Single-unit Evidence Packet / keyed lookup |
| `memories traverse --tenant --from [--depth] [--edge-type]` | 1 | **Not started** — `NotImplementedCommand` stub | Ordered nodes with typed edges, timestamps, `[MISSING: id]` gap markers (FR47–FR49, FR52) |
| `memories ingest <file|url|dir> --tenant --case` | 1 | **Not started** — stub; **blocks G3**. Ingestion is reachable today via `POST /api/v1/ingest` and `quickstart` | Progress, unit count, per-unit ingestion state (FR1–FR3, FR10) |
| `memories case create/delete/list` | 1 | **Not started** — stub; **`create` blocks G3**. Case bootstrap exists server-side (Epic 0/3) | Case id, unit count, next-step hint (FR26–FR27, FR30) |
| `memories case add-member/remove-member/activity` | 1 | **Not started** — stub | Member list (metadata only, no authorization); activity feed attributing ingest/search/membership events (FR28–FR29, FR36) |
| `memories tenant create/delete/verify` | 1 | **Not started** — absent from the command tree; only `tenant list` is wired; **`create` blocks G3**. Provisioning exists as `TenantProvisioningWorkflow` (Epic 0/5) | Provisioned resources summary; verify prints the NFR8 isolation checks with pass/fail (FR38–FR40) |
| `memories tenant list` | 1 | Shipped (7.1) | Tenant table |
| `memories status telemetry --tenant` | 1 | Shipped (7.5) | Telemetry summary, indexes, queue depth |
| `memories status --case/--failed` | 1 | **Not started** — absent from the command tree; only `telemetry` is wired | Counts per ingestion state; failed units with stage + error (FR10–FR11) |
| `memories consistency verify/inspect/repair --tenant` | 1 | Shipped (8.2) | Divergence report; dry-run-then-apply repair receipt (FR73–FR74) |
| `memories quickstart` | 1 | Shipped (7.4) | Prerequisite check → boot hint → health → sample tenant → sample ingest → validation search |
| `memories config show` | 1 | Shipped (7.1) | Effective configuration with sources |
| `memories export case/tenant --tenant` | 2 (delivered early) | Shipped (8.3) | Portable JSON (FR71) |
| `memories handlers list/mismatches` | 1.5 | Shipped (9.3) | Registered handlers; mismatch diagnostics (FR62) |
| `memories explore` | 1.5 | Not started — stub | Interactive exploration |

**Shipped without a CLI verb (REST `/api/v1` + `Hexalith.Memories.Client.Rest` are the Phase 1 surface; no CLI verb is planned for Phase 1):** FR12 re-ingest (`POST …/memory-units/{id}/re-ingest`, `POST …/failed-units/re-ingest`), FR21–FR22 metadata filter and pagination (`GET /api/v1/search` parameters), FR35 delete unit (`DELETE …/memory-units/{id}`), FR37 annotate (`POST …/memory-units/{id}/annotations`), FR42/FR43/FR45 tenant configuration (`PATCH /api/v1/tenants/{id}`, `GET/PUT …/embedding-config`), FR51 promote edge confidence (`PATCH /api/v1/tenants/{id}/edges/confidence`). Route constants are `Contracts.V1.MemoriesRoutes`. A future CLI verb for any of these is a sprint-selected FR53 slice, not an MVP gap.

Unavailable Phase 1 CLI operations are the remaining FR53 MVP slices. They require owning stories in `epics.md` / `sprint-status.yaml`; this PRD records that the thesis surface is incomplete until the target operations are real.

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

| Example | Maps to | Demonstrates | Status (2026-09-08) |
|---|---|---|---|
| `samples/01-quickstart/` | Journey 9 (Phase 1 thesis path) | `dotnet run --project AppHost` boots full stack, ingest + search via CLI | Not started — `memories quickstart` and the README carry NFR31 today; the folder does not exist |
| `samples/02-eventstore-integration/` | Journey 1 (Phase 1.5) | Aspire AppHost with EventStore + Memories, DAPR subscription wired; the L2 stopwatch runs against it | Not started — required before L2 can be timed |
| `samples/03-mcp-agent/` | Journey 3 (Phase 1.5) | MCP server launched by Aspire, agent configuration | Not started — required before L1 can be run |

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
Current path: .NET Aspire AppHost with DAPR sidecars. Local: `dotnet run --project Hexalith.Memories.AppHost`. This is orchestration, not an extra product surface — see `addendum.md`.

**Cross-Language Future Path:**
Non-.NET external consumers can integrate today via ingress REST API (JSON payloads). Non-.NET internal services can integrate via DAPR service invocation (any DAPR SDK). Dedicated Python/TypeScript client packages are a future convenience layer.

## Functional Requirements

FR1–FR75 are the product-horizon inventory, not a claim that every FR is active thesis-MVP scope. Every FR carries a phase from the register and a delivery status from the status register below; the two are independent (an FR can be MVP and not started, or Phase 2 and shipped early).

**Canonical phase register** (August 2026 change control; confirmed 2026-09-08 — this register is the only increment source of truth; FR46–FR52 stay MVP as shipped graph mechanics, see MVP Feature Set):

- **MVP (thesis + foundation):** FR1–FR22, FR24–FR52, FR55–FR57, FR63–FR70, FR72–FR75, plus the Phase 1 rows of the CLI surface table (FR53).
- **Phase 1.5:** FR23, FR54, FR58–FR62, and the Phase 1.5 CLI slices of FR53 (`explore`, EventStore diagnostics).
- **Phase 2:** FR71 (completed early as non-MVP Story 8.3; Epic 26 covers operational backup/restore only — do not reschedule application export as new Phase 2 work).

A capability completed before its planned phase is recorded as completed non-MVP and does not silently change thesis-MVP acceptance.

**Delivery status register (2026-09-12):** `sprint-status.yaml` remains authoritative for tracking state. The final architecture spine's brownfield gap ledger is separate contract-conformance evidence: when it disproves the current wording, this register downgrades the claim and records tracking drift rather than rewriting tracker history. The register is refreshed at each PRD Update and is not edited between Updates.

| Status | FRs | Notes |
|---|---|---|
| **Implemented and product-active** | FR4–FR5, FR7, FR9, FR12, FR14–FR22, FR24, FR31–FR33, FR35, FR37, FR41–FR43, FR45–FR46, FR48–FR52, FR55–FR57, FR63–FR64, FR67–FR70, FR73–FR74 | Implementation exists under the current wording and the capability belongs to an active product phase. This is delivery, not thesis-gate qualification. |
| **Implemented preview; inactive pending Phase 1.5 gates** | FR23, FR54, FR58–FR62 | Code/artifacts exist, but L1–L3 qualification, publication, and announcement have not occurred. Tracker completion does not activate the product surface. |
| **Partial (protocol prerequisite missing)** | FR17, FR25 | FR17: shipped hybrid skips the graph axis without an explicit start node; the auto-seeding rule (Measurable Outcomes) is not implemented. FR25: shipped benchmark compares against best single axis; the BM25+semantic control does not exist. Both gate G1. |
| **Partial (architecture contract incomplete)** | FR6, FR13, FR34, FR39, FR65, FR75 | Missing or incomplete: current-revision all-axis checkpoint/replay (FR6/FR13), tenant-wide case-partitioned graph merge (FR34), end-to-end erasure plus CLI (FR39), server-derived external/system provenance (FR65), and durable authoritative duplicate suppression (FR75). |
| **Shipped against previous wording; re-verification owed** | FR8, FR66, FR72 | Existing capabilities predate the strengthened fairness, capability-aware degradation, and readiness contracts. |
| **Shipped, hardening in progress** | FR44 | Epic 0/5/20 delivered; Epic 24 (tenant-scoped principals as the isolation boundary) is in progress. NFR8 evidence must be re-run when Epic 24 closes. |
| **Partial (server shipped, Phase 1 CLI operation unavailable)** | FR1–FR3, FR10–FR11, FR26–FR30, FR36, FR38, FR40, FR47, FR53 | Server-side behaviour and tests shipped (Epics 0, 1, 3, 4, 5, 6); reachable via `POST /api/v1/...` and `quickstart`. `ingest`, `traverse`, and `case` are top-level `NotImplementedCommand` placeholders. Target `tenant create/delete/verify` and `status --case/--failed` operations are absent from the current command tree, not individually registered stubs. FR38/FR40 additionally wait on Epic 24 hardening. FR39 is in the architecture-contract row because both its erasure contract and CLI are incomplete. See the CLI surface table. |
| **Shipped early (non-MVP)** | FR71 | Story 8.3 operational export/restore; application-facing export stays Phase 2. |
| **Not started** | — | No FR is wholly unstarted; the gaps are the CLI slices above. |

### Knowledge Ingestion

- **FR1:** Developer can ingest content from local files into a specified case
- **FR2:** Developer can ingest content from URLs into a specified case
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
- **FR19:** Developer can view per-axis score breakdown for each search result, including normalization method applied (explain mode)
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
- **FR36:** Developer can view recent activity within a case (ingestion events, searches, membership changes)
- **FR37:** Developer can annotate or correct a memory unit, with annotations tracked as linked memory units

### Tenant Management

- **FR38:** Operator can create a tenant with tenant-scoped backend principals and tenant-scoped indexes (isolation *outcome* is NFR8; mechanism is architecture-owned)
- **FR39:** Operator can complete verified tenant erasure: purge every product projection, make EventStore-held tenant content irreversibly inaccessible, and record the access-telemetry erasure handoff. Replay, restart, and restore cannot resurrect erased content; unreadable restored payloads are quarantined, and the deleted tenant ID cannot be reused
- **FR40:** Operator can verify tenant isolation via automated checks
- **FR41:** Operator can list tenants
- **FR42:** Operator can update tenant configuration after creation (rate limits, display name, settings)
- **FR43:** System refuses embedding-provider or index-schema changes that require reindex unless the operator passes an explicit acknowledgment flag; the CLI states that existing vectors will be rebuilt
- **FR44:** System derives or verifies tenant and case authority server-side on every external and internal path, rejecting cross-tenant requests with clear errors; request fields, app identity, case membership, and channel credentials cannot authorize by themselves
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

- **FR53:** Developer can interact with retrieval and ingestion capabilities via CLI **per the Phase 1 rows of the CLI surface table** (CLI Specification). Real commands count; `NotImplementedCommand` does not. **Phase:** split (MVP surface vs 1.5 slices). **Status:** partial
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

- **FR63:** System returns a relevance confidence (0.0–1.0) with per-axis breakdowns for each search result
- **FR64:** System tracks metadata origin (human-declared vs AI-inferred) and metadata confidence per metadata field on every memory unit
- **FR65:** System records `ingested_by` as a mandatory field on every memory unit: normalized authenticated subject for external calls, or a canonical `system:*` principal derived from an authenticated app-ID allowlist plus an explicit tenant grant for internal calls; caller-supplied provenance cannot override it
- **FR66:** When selected search axes degrade, the system returns a safe partial result if at least one selected axis can respond and fails only when none can. The Evidence Packet distinguishes unavailable, excluded, and available-with-no-hits axes and reports freshness impact plus recovery guidance; empty results distinguish true absence, empty/wrong scope, incomplete or delayed ingestion, filters, stale evidence, authorization refusal, and backend degradation. Unauthorized responses contain no restricted evidence, and no surface promotes an incomplete ingestion revision
- **FR67:** System records search and access events per tenant as access telemetry (Glossary) — infrastructure telemetry that applications may build access records on; not a tamper-evident audit trail. Telemetry delivery failure never changes domain truth or rolls back an accepted product mutation

### Embedding Provider Management

- **FR68:** Operator can configure embedding provider and model per tenant
- **FR69:** System enforces per-tenant rate limit ceilings for embedding API calls
- **FR70:** System tracks the embedding provider and model used for each memory unit's vectors

### Data Portability & System Health

- **FR71:** Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. **Phase:** Phase 2. Completed early as non-MVP (Story 8.3). Epic 26 covers operational backup/restore only — do not reschedule application-facing export as new Phase 2 work.
- **FR72:** System exposes liveness for process viability and readiness for authentication configuration, the DAPR control boundary, and EventStore command availability. Search-backend outages report capability degradation while safe selected axes remain usable rather than making the entire server unready
- **FR73:** Operator can detect index/graph divergence via consistency check
- **FR74:** Operator can repair detected index/graph inconsistencies via a dry-run-then-apply consistency repair that cannot cross tenants, cannot silently delete units without provenance in access telemetry, and cannot invent edges the current authoritative EventStore revision does not support

## Non-Functional Requirements

*NFRs are tagged by validation phase: **[MVP]** = thesis/foundation, **[P1.5]** = EventStore product integration + MCP, **[Ongoing]** = as infrastructure matures, **[Future web]** = Epic 17+.*

**NFR delivery status (2026-09-12):** *verified* = an automated suite or recorded run exercises the current wording; *implemented* = the mechanism exists but the stated verification has no recorded run; *not started* = neither. Verification of a Phase 1.5 component does not activate its product surface; only L1–L3 do that. The final architecture spine's gap ledger corrects claims that predate the strengthened wording. The NFR delivery-status register is refreshed only during PRD Updates.

| Status | NFRs |
|---|---|
| Verified | NFR11, NFR17, NFR19–NFR20, NFR26–NFR28, NFR30 |
| Verified against the *previous* wording; re-verification owed | NFR8–NFR10 (principal-driven isolation, data-plane secret path, and layered internal authorization), NFR13 (mixed-workload fairness), NFR18 (capability-aware failure boundary), NFR21 (negative envelope case), NFR22 (durable provider retry timing), NFR24–NFR25 (canonical cross-surface fusion/explain semantics) |
| Partial mechanism and re-verification owed | NFR16 — controlled Redis restart proved the previous wording, but authoritative EventStore replay to every current projection/configuration and erased-tenant non-resurrection are not implemented |
| Implemented, verification run not recorded in PRD evidence | NFR1–NFR5, NFR7, NFR12, NFR14, NFR23, NFR29, NFR31 (Story 7.4 walkthrough, no timed clean-machine record), NFR33–NFR34 (Epic 27 in progress; revised production-admission/runtime-failure posture needs fresh evidence) |
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
| **NFR8** | Zero cross-tenant data leakage — a principal whose tenant claims name tenant A cannot read, search, traverse, or ingest into tenant B's data, whatever tenant id the request carries | Automated suite driven by *principals*, not ids: authenticate as tenant A, then search, ingest, and traverse with `--tenant B`, with B's index names, and with malformed/empty ids; every call is rejected or returns only A's data. Graph-specific test: identical graph structures in A and B, traverse as A, zero B nodes even if edge ids collide. Re-run when Epic 24 (tenant-scoped principals) closes | MVP |
| **NFR9** | Product services retrieve embedding-provider, application, and data-plane credentials exclusively through the DAPR Secrets API backed by OpenBao. Secret values never appear in application configuration, ordinary environment variables, workflow history, logs, or public contracts. Deployments may supply only documented minimum OpenBao bootstrap material outside that boundary; direct Redis/FalkorDB credential injection is an alignment gap, not an approved second path. | Structural dependency tests, secret scanning, AppHost/deployment topology tests, and integration tests proving DAPR reads from OpenBao without secret disclosure | Ongoing |
| **NFR10** | External/delegated bearer authority survives REST, CLI, MCP, and internal hops. Trusted internal calls additionally use deny-by-default DAPR workload authorization and protected app channels, then map the authenticated app ID through one operator-owned finite allowlist to a canonical `system:*` principal with an explicit tenant grant. Unknown apps and ungranted tenants fail closed; channel authentication never substitutes for tenant authorization. | End-to-end authorization tests covering delegated subjects, allowed and unknown app IDs, granted and ungranted tenants, and protected DAPR channels | Ongoing |
| **NFR11** | External product REST/CLI ingress is authenticated for the active MVP HTTP surface. Health probes and required DAPR infrastructure routes are the only deliberate anonymous exceptions and are named and tested. Additional identity-provider hardening may remain operational-readiness work; unauthenticated product ingress is not a Phase 1.5 allowance. | Integration test with unauthenticated product requests plus named anonymous exceptions | MVP |

### Scalability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR12** | Adding nine loaded tenants does not degrade an existing tenant's p95 query latency or ingestion throughput by more than 5% under the NFR13 reference mixed-load profile | Benchmark tenant 1 alone at 100K units, then repeat with 9 additional 100K-unit tenants running the NFR13 mix; compare p95 latency and throughput | Ongoing |
| **NFR13** | Under the 10-minute reference mix—tenant A runs 10 concurrent syntactic, semantic, and hybrid queries; tenant B submits 100 ≤10 KB ingests/min; tenant C repairs 100 units—A remains within NFR1–NFR3 and ≤10% of its solo p95, B meets NFR5, and C completes ≥25 units/min. Every eligible non-empty tenant queue receives an admission within 5 seconds after capacity is available. Default pending caps are 1,000 units/tenant and 10,000 globally; the first item beyond either cap is rejected within 1 second with retry guidance, while accepted work is never dropped. `[ASSUMPTION: these mixed-load, fairness, and queue-cap numbers are the initial architecture-reconciliation budgets pending load-test calibration.]` | Reproducible three-tenant mixed-load and cap-plus-one tests, including interactive, batch, repair, recovery, and migration priority | Ongoing |
| **NFR14** | Redis memory footprint per memory unit is predictable and documented — operator can estimate infrastructure costs before tenant provisioning | Published sizing guide: memory per unit by vector dimension and metadata size | Ongoing |
| **NFR15** | Architecture must not preclude backend migration (Redis → Qdrant) — concrete implementation with clear extraction points identified, no premature interfaces | Architecture review: extraction points documented, no tight coupling to Redis-specific APIs in domain logic | Ongoing |

### Reliability

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR16** | No loss of EventStore-committed memory units across Redis restart or projection loss: every non-erased unit that reached `pending` or later becomes searchable again under the current schema and embedding configuration, either because projections survived or because authoritative EventStore replay rebuilt all axes. AOF is an optimisation, not the durability contract. Replay, restart, export restore, and backup restore must never resurrect content from a tenant whose erasure completed under FR39. | Commit N units, restart Redis, then separately destroy projections and replay EventStore truth through the FR6/FR13 completion contract. **AOF-intact:** all N `indexed` within 5 minutes for 10K units/tenant. **Rebuild:** progress visible via `status`, completion at no worse than NFR5 throughput, zero non-erased units missing. Restore an erased-tenant fixture and prove zero content rehydrates. The final architecture spine records the authoritative replay mechanism as missing. | MVP |
| **NFR17** | Ingestion pipeline state survives process restarts — pending and in-progress units resume without data loss | DAPR Workflow / Durable Task history verified | MVP |
| **NFR18** | Partial backend failure returns a safe result when at least one selected axis can respond and fails only when none can. The Evidence Packet distinguishes unavailable, excluded, and available-with-no-hits axes and includes freshness impact plus recovery guidance; incomplete ingestion revisions remain incomplete. | Chaos test each backend alone and in combinations; verify safe partial responses, exact Evidence Packet meanings, and the no-safe-axis failure boundary | Ongoing |
| **NFR19** | Failed ingestion units are never silently dropped — all failures visible via CLI status with error details and failure stage | End-to-end test with intentional failures at each pipeline stage | Ongoing |

### Integration

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR20** | MCP tool responses conform to MCP protocol specification — valid tool schemas, typed parameters, structured error responses | MCP protocol conformance test suite | P1.5 |
| **NFR21** | DAPR pub/sub integration accepts the CloudEvents envelope as published by Hexalith.EventStore conventions (envelope attributes, CausationId/CorrelationId extension attributes, source-prefix routing). Envelopes from other DAPR publishers are accepted only for the fields the EventStore convention defines; processing them without custom code is the DAPR-generic *experiment* (Innovation #2), not a requirement | Integration test with EventStore-convention CloudEvents payloads; a documented negative test showing what a non-conforming envelope does | P1.5 |
| **NFR22** | Embedding provider rate limits do not crash the pipeline or lose work: the system honors provider `Retry-After` through durable workflow timers and bounded queues, never retries before the stated instant, and resumes eligible work within 5 seconds after the window when capacity is available | Rate-limit simulation per provider across restart, queue saturation, and concurrent tenants; assert retry timing and no accepted-work loss | Ongoing |
| **NFR23** | CLI connects to the memory server via configurable endpoint — supports local dev (localhost), container (docker service name), and remote (ingress URL) environments | Configuration layering test across all three environments | Ongoing |

### Algorithmic Quality

| NFR | Requirement | Target | Phase |
|---|---|---|---|
| **NFR24** | Hybrid fusion uses deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0; single-axis explain still documents axis-specific score semantics, and every evidence-bearing surface preserves the same contributions and null/empty meanings | Cross-surface fusion and explain contract tests with known rankings/weights | MVP |
| **NFR25** | Fusion produces deterministic scores and ordering — the same query against the same data and active axes yields identical results across runs and surfaces, with deterministic memory-unit-ID tie breaking | Golden-vector contract tests across REST, CLI JSON, and MCP plus 100 repeated queries with zero score or ordering variance | MVP |
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
| **NFR31** | README includes a working Phase 1 quickstart that completes in <30 minutes on a clean machine with Docker installed. **G3 is timed on the README manual path** — AppHost boot → `tenant create` → `case create` → `ingest` → `search query` — using real CLI operations; `memories quickstart` is the scripted convenience and may be used *in addition*, not instead. G3 cannot run while `tenant create` is absent and the `case`/`ingest` groups are placeholders; these operations sit on the 2026-12-01 critical path. **Clean machine (definition shared with L2):** a fresh OS user profile on Windows 11, macOS, or Ubuntu LTS with only Docker (or Docker Desktop) and the .NET SDK pinned in `global.json` preinstalled; the Aspire CLI/workload is installed on the clock; broadband network assumed; Docker image pulls are on the clock (unlike NFR7); the embedding-provider API key is **off** the clock (obtained beforehand and pasted at the prompt). Phase 1.5 EventStore 30-minute clock is a separate launch gate (L2) using the same machine definition. | Timed walkthrough on the clean machine above, recorded in `docs/dev/quickstart-walkthrough-log.md` with date, OS, and machine spec; **no run recorded as of 2026-09-08** | MVP |

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

1. `[PHASE-BLOCKER / G6]` Jerome confirmed 2026-12-01 for the former G1–G5 release gate and 2027-01-01 for Phase 1.5 launch. The 2026-09-12 architecture reconciliation added G6; `[ASSUMPTION: the expanded G1–G6 gate keeps the confirmed 2026-12-01 date]` until Jerome ratifies or resets it. Owner: Jerome. Revisit: 2026-10-31 prerequisite checkpoint.
2. `[NOTE FOR PM]` Thesis-gate corpus and labelling: who recruits the two independent reviewers and which real (non-synthetic) Phase 1 corpus is frozen for G1? Owner: Jerome. Revisit: 2026-10-31, before benchmark expansion work is sprint-selected.
3. Compact scope, source, relevance caveat, freshness/degradation, omission, and recovery remain visible on every evidence-bearing surface. Should detailed ranks, weights, matched terms, and graph diagnostics remain opt-in (`--explain`, FR19) or become default at web activation? Owner: UX. Revisit: Epic 17 activation SCP.
4. **Closed 2026-09-08.** Licence is MIT (Jerome). The PRD's Apache 2.0 text was replaced; the repository already complies. Remaining action: publish the MIT no-relicense sentence in the README (handoff, `addendum.md`).
5. FR32 stays absolute (single-case ownership) for MVP and Phase 1.5; brief R3 (lightweight cross-case references) may be restored only as a Phase 2 FR via sprint change. Owner: Jerome. Revisit: Phase 2 planning.
6. Closed 2026-09-08: ingest-from-anywhere (cloud/git/image/video) is an explicit deferral — see Non-Goals. Re-open only by sprint change.
7. **Closed 2026-09-12.** The optional Python `ai-agent` sidecar remains architecture-only and deferred until a selected product feature requires it; no current FR/NFR promotes it.
8. **Closed 2026-09-12.** Preserve the exact lowercase V1 wire values `queued`/`extracting`/`embedding`/`indexing`/`indexed`/`failed` until a versioned break; the Glossary maps product labels without changing the wire contract.
9. `[PHASE-BLOCKER / G6]` Architecture AD-14 is unphased while the approved product contract places non-disruptive embedding/schema migration in Phase 2 and backend migration in Phase 3. Owner: Architecture + Jerome. Resolve by qualifying AD-14 or approving an MVP rebaseline before the 2026-10-31 prerequisite checkpoint.
10. `[PHASE-BLOCKER / G6]` The final architecture spine binds only FR1–FR74/NFR1–NFR36; it must bind and trace FR75's epoch-aware durable-idempotency contract and active-CLI accessibility NFR37 before G6. Owner: Architecture. Revisit: 2026-10-31 prerequisite checkpoint.

## Assumptions Index

- `[ASSUMPTION]` Generic Marten/Wolverine/Axon zero-code remains an experiment until a named spike passes the DAPR-generic kill switch. (§ Executive Summary)
- `[ASSUMPTION]` Cloud-drive, git, image, and video ingest stay deferred until an owner names a phase. (§ Non-Goals)
- `[DERIVED]` G1-prerequisite sprint-selection date 2026-10-31 (one month before the confirmed 2026-12-01 release decision, which is itself one month before the confirmed 2027-01-01 launch decision). (§ Measurable Outcomes › Release decision record; Open Question 1)
- `[ASSUMPTION]` The architecture-expanded G1–G6 gate keeps the 2026-12-01 date Jerome confirmed for the former G1–G5 gate until he ratifies or resets it. (§ Release decision record; Open Question 1)
- `[ASSUMPTION]` Thesis-stress slice ≤ 20% of N; explicit + metadata-carried edges ≥ 30% of non-`contains` edges for the run to count as a three-axis (not similarity-graph) test. (§ Measurable Outcomes › Thesis-gate protocol)
- `[ASSUMPTION]` NFR36 file/URL freshness budgets (60 s for ≤10 KB, 5 min for ≤1 MB) are derived from NFR5 throughput; architecture may tighten, not remove. (§ NFR36)
- `[ASSUMPTION]` The NFR "implemented" status row reflects absence of recorded evidence in planning artifacts, not proof of absence in CI. (§ Non-Functional Requirements › NFR delivery status)
- `[ASSUMPTION]` NFR33 is Evidence Packet freshness (rerun SCP) and NFR35 is future-web interaction performance (remediation-batch SCP) — the two August 2026 patch sets used the same NFR33 id for different requirements. (§ NFR33 / NFR35; index-only by design — the collision is history, not a live requirement)

- `[ASSUMPTION]` NFR13 mixed-load budgets (10-minute three-tenant profile, 10% solo-p95 degradation, 25 repair units/min, 5-second admission bound, and 1,000/10,000 queue caps) are initial values pending load-test calibration. (§ NFR13)

Resolved in this Update (no longer assumptions): benchmark N (now a protocol number, G1); Phase 1 graph population (now a decision in MVP Feature Set).
