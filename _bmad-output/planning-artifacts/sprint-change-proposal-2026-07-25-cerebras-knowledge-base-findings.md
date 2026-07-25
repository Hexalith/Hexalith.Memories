# Sprint Change Proposal — Cerebras Knowledge-Base Findings Intake

**Date:** 2026-07-25
**Author:** Amelia (Developer agent), on behalf of Jerome
**Status:** Approved by Jerome on 2026-07-25 — Section 4 edits applied (research artifact created; epics.md placeholders and Epic 17 note added)
**Mode:** Batch (autonomous session)
**Trigger:** External research intake — analysis of "How We Built Our Knowledge Base" (Cerebras engineering blog, 2026-07-15, <https://www.cerebras.ai/blog/how-we-built-our-knowledge-base>) for findings applicable to Hexalith.Memories.

---

## Section 1: Issue Summary

This is a research-intake course correction, not a defect. The Cerebras engineering team published a detailed account of their internal enterprise knowledge base (15,000+ questions/day, launched ~3 months before publication, used by humans, automations, and agents). Their independently built system converges on several of Hexalith.Memories' core design decisions and additionally describes production-proven retrieval techniques that Memories does not yet implement. The purpose of this proposal is to (a) record the convergent validations as evidence for existing architecture decisions, and (b) convert the genuinely new techniques into explicitly scoped Phase 2/Phase 3 backlog placeholders so they are not lost.

**Evidence source:** Full article text recovered on 2026-07-25 (the page returns HTTP 500 with content embedded in Next.js flight data; text was extracted and verified). Key referenced techniques: RRF fusion with smoothing constant 60, LLM distillation before embedding, "bursting" (multi-granularity gated embeddings), IDF signal gating (threshold 4.0), age decay, small-model reranking (score 0–10, keep top 10), context re-expansion of neighboring sections, planner→executor→synthesis query pipeline, LLM-free MCP retrieval primitives, projects/scoped search with per-user default scope, CocoIndex incremental re-embedding.

## Section 2: Impact Analysis

### 2.1 Convergent validations (no change required — record as supporting evidence)

| # | Cerebras practice | Memories equivalent | Disposition |
|---|---|---|---|
| V1 | Multiple scorers (full-text, embeddings, IDF, recency), none trusted alone, fused at query time with RRF `weight/(60+rank)` | Weighted RRF fusion over syntactic/semantic/NL/graph axes; production `k=10`, live weights 0.30/0.35/0.35; 8/8 strict benchmark wins (Epic 26 close-out) | Validates the three-axis thesis and the RRF fusion choice (Story 22.4). No action. |
| V2 | MCP tools are thin, LLM-free retrieval primitives with narrow, structured, stable inputs/outputs; the agent is the orchestration engine | Memories MCP tools: typed schemas, token-budget-aware, structured errors, no hidden orchestration (Epic 10, Story 25.6) | Validates MCP design philosophy. No action. |
| V3 | Every source lands in one uniform embeddings-row schema; custom sources are plugin scripts emitting that schema | Uniform memory-unit contract + stable ingest contract with idempotency token (Story 18.4); connectors publish through the same intake | Validates the uniform-contract design. No action. |
| V4 | "Search everything everywhere rapidly stopped being useful" — scoping became mandatory for relevance | Case-scoped search is a founding design decision (Epic 3) | Validates case scoping; see D5 for the additive gap. No action here. |
| V5 | Retriever results normalized into a shared evidence schema (scores, recency, source hints) before synthesis; answers carry citations and caveats | Evidence Packet (`Contracts.V1`) as the cross-surface trust envelope (FR23/FR24/FR63/FR66) | Validates the Evidence Packet concept. No action. |
| V6 | Incremental re-embedding of only changed chunks, with sync metadata colocated in the same database | Re-ingestion/dedup design (Stories 18.4, 23.4); embedding migration tooling (Epic 13, 21.9) | Largely covered; colocated sync-state pattern noted for future continuous connectors. No action. |

### 2.2 New findings → candidate enhancements

| # | Finding (Cerebras) | Gap in Memories | Proposed disposition |
|---|---|---|---|
| D1 | **Age decay**: recency is a first-class ranking signal — "Slack answers expire"; newer wins when relevance is otherwise equal | Memories flags stale units (>90 days) in confidence metadata but ranking is recency-blind | Phase 2 backlog placeholder: deterministic, tunable recency prior as a fusion input (fits Story 22.7's per-query/tenant tunable weights and the "fusion must stay deterministic" rule) |
| D2 | **LLM distillation before embedding**: raw transcripts are NOT embedded; an LLM extracts a searchable one-line question, short summary, resolution, and systems/code references; "accuracy increased significantly when the thread was normalized into a consistent format" | Memories embeds Kreuzberg-extracted content directly; the NL-description dual embedding (Epic 9) applies the pattern to events only | Phase 2 backlog placeholder: optional distillation activity in `IngestionWorkflow` for noisy conversational/document content; embed distilled normalized form alongside (not instead of) full-text indexing |
| D3 | **Bursting + signal gating**: sub-thread runs by one author embedded with parent topic prepended (Anthropic Contextual Retrieval); gated by IDF ≥ 4.0, ≥ 200 chars, social signals, so low-signal rows never reach the store | Chunk embeddings (Story 23.1) exist but chunks are not context-prepended and there is no signal gate on embedding-row creation | Phase 2 backlog placeholder: context-prepended chunk embedding + deterministic low-signal gating; prerequisite thinking for Phase 2 discussion threading |
| D4 | **Rerank + context re-expansion**: RRF top ~20 → small reranker scores 0–10 → keep top 10 → re-attach neighboring sections so headings/preconditions split off by chunking are restored | `IResultFuser` reranker seam exists (Story 22.7) but no implementation; Evidence Packet returns matched units without neighbor re-expansion | Phase 2 backlog placeholder: optional, degradable reranker behind the existing seam + neighbor re-expansion into the Evidence Packet (graceful-degradation rules apply — reranker outage must not fail search) |
| D5 | **Projects + default scope**: lightweight, non-exclusive named bundles of data sources; the same source can belong to many projects; a default project on the user profile scopes queries automatically | Cases are strict single-ownership containers (deliberate); cross-case search exists (Story 3.4) but there is no named, reusable scope bundle and no per-identity default scope | Phase 2/3 backlog placeholder: additive "scope bundles" referencing cases/sources without duplicating them + default scope per user/agent identity; must not weaken strict case ownership or physical tenant isolation |
| D6 | **Planner → executor → synthesis** web pipeline: a light LLM planning pass chooses retrieval tools, executor fans out in parallel and normalizes to the evidence schema, synthesis LLM answers with citations | Epic 17 (future Web UX) composes evidence display but does not yet name a query-pipeline pattern | Context note added to Epic 17 preamble (no new stories) — when web Q&A is pulled forward, adopt planner/executor/synthesis over the Evidence Packet |
| D7 | **Per-source freshness**: every Slack channel is its own data source with its own fetch cadence | Per-tenant config exists; no per-source ingestion cadence (no continuous pull connectors yet) | Note only — becomes relevant when continuous/pull connectors are introduced; no placeholder yet |
| D8 | **who_knows / expertise surface**: people with demonstrated expertise on a topic as a retrieval primitive | `ingested_by` provenance and case members exist; no expertise surface | Note only — aligns with Phase 3 entity resolution; no placeholder yet |

### 2.3 Epic impact

- **Epics 0–26 (done):** no impact; no rework proposed. Findings V1–V6 are recorded as supporting evidence, not change drivers.
- **Epic 27 (in progress), Epic 29 (in progress), Epic 28 (backlog):** untouched; nothing in this intake competes with or resequences them.
- **No new epic is created now.** All D1–D5 items enter as Phase 2 backlog placeholders following the existing placeholder convention (activation creates normal stories via sprint planning; placeholders receive no `development_status` rows).
- **Epic 17:** one context-note edit (D6).

### 2.4 Artifact conflicts

- **PRD:** No conflict. Findings reinforce the three-axis thesis, Evidence Packet, and MCP-first design. D1 (recency-aware ranking) and D5 (scope bundles) are candidate FR additions when Phase 2 PRD scoping happens; nothing in the current PRD must change now.
- **Architecture:** No conflict. D1–D4 all fit existing seams (fusion weights, `IngestionWorkflow` activities, `IResultFuser`, Evidence Packet) and respect the standing rules: deterministic fusion, graph/reranker optional and degradable, physical tenant isolation, no infra in product code.
- **UX Design:** No conflict; D6 is additive context for Epic 17.
- **Testing:** any activated placeholder inherits the standing rules — benchmark NDCG/scoring tests must be preserved when fusion/ranking behavior changes (project-context Testing Rule), and tenant-isolation negative evidence applies to D5 scope-bundle work.

## Section 3: Recommended Approach

**Direct Adjustment (Option 1) — Effort: Low. Risk: Low. Timeline impact: none.**

Record the analysis as a research artifact, add five Phase 2 backlog placeholders plus one Epic 17 context note to `epics.md`, and stop. No stories are created, no epics resequenced, no in-flight work disturbed. Rollback (Option 2) is not applicable — nothing is being reverted. MVP review (Option 3) is not applicable — the MVP shipped; findings validate rather than threaten it.

Rationale: the intake's value is (a) evidence that independent production systems converge on Memories' architecture and (b) a small set of well-bounded relevance techniques with published production results. Placeholders keep them visible for Phase 2 prioritization at near-zero cost, in line with the established Phase 2 Backlog Placeholder convention.

## Section 4: Detailed Change Proposals

### 4.1 New research artifact (new file)

`_bmad-output/planning-artifacts/research/cerebras-knowledge-base-findings-2026-07-25.md` — the finding-by-finding mapping (Sections 2.1/2.2 above, with the recovered article reference list). Serves as the durable citation target for the placeholders.

### 4.2 epics.md — extend "Phase 2 Backlog Placeholders" (addition, after the Data Export placeholder)

Five new placeholders, each following the existing convention (no story keys reserved, no sprint-status rows until activation):

1. **Recency-Aware Ranking (Age Decay)** — As a developer, I want an optional deterministic recency prior as a fusion input, tunable per query/tenant, so that when relevance is otherwise equal, newer memory wins. Guards: fusion stays deterministic and pure; benchmark NDCG suite must be re-validated (PRD hard line ≥ 7/8 hybrid wins) before any default change ships.
2. **Ingestion Distillation & Normalized Embedding** — As a developer, I want an optional LLM distillation activity in `IngestionWorkflow` that normalizes noisy content into a consistent searchable form (question / summary / resolution / referenced systems) embedded alongside full-text indexing, so semantic recall improves on conversational and long-form content. Guards: side effects in activities only; raw content remains indexed; distilled fields carry `ai-inferred` origin + confidence.
3. **Context-Prepended Chunk Embedding & Low-Signal Gating** — As a developer, I want chunk embeddings prepended with parent-document/thread context and a deterministic low-signal gate (IDF/length thresholds) before embedding rows are created, so tangent content is findable and filler never pollutes the vector index. Cites Anthropic Contextual Retrieval and Cerebras bursting (IDF ≥ 4.0, ≥ 200 chars).
4. **Reranker Activation & Context Re-Expansion** — As a developer, I want a small-model reranker implemented behind the existing `IResultFuser` seam (fused top-N → scored → top-K) and neighbor re-expansion of winning chunks into the Evidence Packet, so results are relevant to the actual question and never lose surrounding context. Guards: reranker is optional and degradable (outage → deterministic fusion order, degraded flag per FR66); token-budget rules still apply.
5. **Scope Bundles & Default Scope** — As a team member or agent, I want named, non-exclusive scope bundles that reference cases/sources without duplicating them, and a default scope stored per user/agent identity, so search is relevant by default without weakening strict case ownership. Guards: physical tenant isolation unchanged; cross-tenant bundles forbidden; tenant-isolation negative evidence required on activation.

### 4.3 epics.md — Epic 17 context note (one-line addition to the epic preamble)

Add: "When web question-answering is pulled forward, adopt a planner → executor → synthesis pipeline over the Evidence Packet (planning pass selects axes/tools; executor fans out and normalizes; synthesis cites sources), per the Cerebras knowledge-base findings (research/cerebras-knowledge-base-findings-2026-07-25.md)."

## Section 5: Implementation Handoff

**Scope classification: Minor** (backlog documentation only — no code, no story activation, no resequencing).

| Role | Responsibility |
|---|---|
| Developer agent (Amelia) | Apply the Section 4 edits verbatim upon approval (research artifact + epics.md placeholders + Epic 17 note) |
| Product Owner / PM (John) | Prioritize D1–D5 placeholders during the next Phase 2 scoping pass; decide whether D1/D5 become FRs |
| Architect (Winston) | On activation of any placeholder, confirm seam fit (fusion determinism, `IResultFuser`, `IngestionWorkflow` activities, isolation invariants) |

**Success criteria:** research artifact exists and is linkable; the five placeholders and Epic 17 note are present in `epics.md`; no `development_status` or sprint-status changes (placeholders are not registered work); Epics 27/29 proceed undisturbed.

**Sprint-status update:** N/A — no epics or stories are added, removed, or renumbered by this proposal.

---

## Checklist Record

| Item | Status | Note |
|---|---|---|
| 1.1 Triggering story | N/A | Not story-triggered; external research intake requested by Jerome |
| 1.2 Problem statement | Done | New knowledge from stakeholder-supplied industry source |
| 1.3 Evidence | Done | Full article text recovered and mapped (Section 1) |
| 2.1–2.5 Epic impact | Done | No completed/in-flight epic affected; no resequencing (Section 2.3) |
| 3.1 PRD conflicts | Done | None; candidate Phase 2 FRs noted (Section 2.4) |
| 3.2 Architecture conflicts | Done | None; all candidates fit existing seams (Section 2.4) |
| 3.3 UI/UX conflicts | Done | None; Epic 17 additive note only |
| 3.4 Other artifacts | Done | Testing guards restated (Section 2.4) |
| 4.1 Direct Adjustment | Viable | Selected — Low effort / Low risk |
| 4.2 Rollback | Not viable | Nothing to revert |
| 4.3 MVP review | Not viable | MVP complete; findings validate it |
| 4.4 Path selected | Done | Option 1 (Section 3) |
| 5.1–5.5 Proposal components | Done | Sections 1–5 |
| 6.3 User approval | Done | Approved by Jerome on 2026-07-25 ("Yes — apply all edits") |
| 6.4 sprint-status.yaml | N/A | No epic/story registration changes |
