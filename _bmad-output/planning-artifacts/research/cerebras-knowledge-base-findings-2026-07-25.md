# Research: Cerebras Knowledge-Base Findings Applied to Hexalith.Memories

**Date:** 2026-07-25
**Source:** Isaac Tai, Daniel Kim, Mike Gao — "How We Built Our Knowledge Base", Cerebras engineering blog, 2026-07-15, <https://www.cerebras.ai/blog/how-we-built-our-knowledge-base>
**Companion:** `../sprint-change-proposal-2026-07-25-cerebras-knowledge-base-findings.md` (approved)

Cerebras built an internal enterprise knowledge base serving 15,000+ questions per day across humans, automations, and agents, launched roughly three months before publication. Their system was designed independently of Hexalith.Memories yet converges on several of its core decisions, and additionally describes production-proven retrieval techniques Memories does not implement yet. This document is the durable finding-by-finding mapping.

## System summary (Cerebras)

- One Postgres table holds embeddings, raw summaries, and metadata from every source (Slack, code, documents, custom databases); anything in the table is queryable through the same interface.
- Slack ingestion runs in Socket Mode (WebSocket push, no polling); each event is acknowledged, deduplicated by stable event ID, and the ingest consumer re-fetches the entire thread and writes it back as one row.
- Retrieval combines full-text search (exact tokens: error strings, flags, hosts), embedding search (paraphrase), inverse document frequency (signal vs filler), and age decay (newer wins ties). No single scorer is trusted alone.
- Threads are distilled by an LLM before embedding: a one-line searchable question, a short summary, the resolution, and referenced systems/code. The raw transcript is not embedded directly — "accuracy increased significantly when the thread was normalized into a consistent format."
- "Bursting": consecutive same-author message runs are embedded individually with the thread topic prepended (Anthropic Contextual Retrieval pattern), gated by IDF ≥ 4.0 on at least one token, ≥ 200 characters combined, and social signals (reactions), so low-signal content never reaches the store.
- Code repositories are embedded via CocoIndex: language-specific coarse-to-fine splitting (classes → methods → blocks), multi-level records (file-level and function-level), incremental re-embedding of only changed chunks with sync metadata colocated in the same Postgres database.
- Custom sources are plugin scripts: a small Python module emits rows in the shared embeddings schema; the rest of the stack works unchanged.
- Query pipeline: a short LLM planning pass selects tools/sources (search, search_slack, search_code, subsystem_index, recent_prs, who_knows); the executor fans out in parallel and normalizes results into a shared evidence schema; a synthesis LLM produces the answer with citations and caveats.
- Fusion: reciprocal rank fusion adds `weight / (60 + rank)` per list (default weight 1.0, smoothing constant 60 — consensus beats a single strong vote); duplicates merge to one source; per-file contribution caps produce a diverse top ~20; a small reranker scores each candidate 0–10 and the top 10 are kept; winners are then re-expanded with neighboring sections so headings/preconditions split off by chunking are restored.
- MCP integration exposes retrieval primitives as intentionally simple, LLM-free tools with narrow, structured, stable inputs/outputs; the MCP client agent (e.g. Claude Code) is the orchestration engine.
- Projects: lightweight, non-exclusive named bundles of data sources; the same source can belong to many projects; onboarding stores a default project on the user profile that scopes queries automatically. "Search everything everywhere rapidly stopped being useful."

## Convergent validations (evidence for existing Memories decisions)

| # | Cerebras practice | Memories equivalent |
|---|---|---|
| V1 | Multi-scorer retrieval fused with RRF (k=60), no scorer trusted alone | Weighted RRF over syntactic/semantic/NL/graph axes; production k=10, live weights 0.30/0.35/0.35, 8/8 strict benchmark wins (Epic 26) |
| V2 | LLM-free MCP retrieval primitives; the agent orchestrates | Memories MCP tool design: typed schemas, token-budget-aware, structured errors, no hidden orchestration (Epic 10, Story 25.6) |
| V3 | One uniform embeddings-row schema; custom sources as plugins emitting it | Uniform memory-unit contract + stable ingest contract with idempotency token (Story 18.4) |
| V4 | Scoped search is mandatory for relevance at corpus scale | Case-scoped search as a founding decision (Epic 3) |
| V5 | Retriever results normalized into a shared evidence schema before synthesis; answers carry citations | Evidence Packet in `Contracts.V1` (FR23/FR24/FR63/FR66) |
| V6 | Incremental re-embedding of changed chunks; sync state colocated with the embedding store | Re-ingestion/dedup (18.4, 23.4) and embedding-migration tooling (Epic 13, 21.9); colocated-sync-state pattern noted for future continuous connectors |

## New findings → dispositions

| # | Finding | Disposition |
|---|---|---|
| D1 | Age decay as a first-class ranking signal | Phase 2 placeholder: Recency-Aware Ranking (Age Decay) |
| D2 | LLM distillation into a normalized searchable form before embedding | Phase 2 placeholder: Ingestion Distillation & Normalized Embedding |
| D3 | Bursting: context-prepended sub-unit embeddings gated by IDF/length/social signals | Phase 2 placeholder: Context-Prepended Chunk Embedding & Low-Signal Gating |
| D4 | Small-model rerank of fused candidates + neighbor context re-expansion | Phase 2 placeholder: Reranker Activation & Context Re-Expansion (behind the existing `IResultFuser` seam) |
| D5 | Projects: non-exclusive scope bundles + per-user default scope | Phase 2/3 placeholder: Scope Bundles & Default Scope (additive; strict case ownership and tenant isolation unchanged) |
| D6 | Planner → executor → synthesis web query pipeline | Context note on Epic 17 preamble; no new stories |
| D7 | Per-source ingestion freshness cadence | Note only — relevant once continuous/pull connectors exist |
| D8 | who_knows expertise surface | Note only — aligns with Phase 3 entity resolution; `ingested_by` provenance is the existing seed |

## References cited by the article

1. Malkov and Yashunin, Efficient and Robust Approximate Nearest Neighbor Search Using Hierarchical Navigable Small World Graphs, arXiv:1603.09320 / IEEE TPAMI 2018.
2. Anthropic, Introducing Contextual Retrieval, 2024.
3. Cormack, Clarke, and Büttcher, Reciprocal Rank Fusion Outperforms Condorcet and Individual Rank Learning Methods, SIGIR 2009.
4. Li et al., Search-o1: Agentic Search-Enhanced Large Reasoning Models, arXiv:2501.05366, 2025.
5. Anthropic, Code Execution with MCP, 2025.
6. Liu et al., Lost in the Middle: How Language Models Use Long Contexts, arXiv:2307.03172, 2023.
7. Anthropic, Use XML Tags.
8. Salesforce/Slack Engineering, How Slack AI Processes Billions of Messages.
9. Improving Agents, Best Nested Data Format.
10. Cursor, Improving Agent with Semantic Search, 2025.
