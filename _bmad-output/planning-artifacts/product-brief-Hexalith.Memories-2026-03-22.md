---
stepsCompleted: [1, 2, 3, 4, 5, 6]
workflow_completed: true
inputDocuments: ['_bmad-output/brainstorming/brainstorming-session-2026-03-21-1530.md']
date: 2026-03-22
author: Jerome
---

# Product Brief: Hexalith.Memories

## Executive Summary

**Hexalith.Memories: Connected knowledge that understands why.**

Hexalith.Memories is an open-source relational memory server that indexes content, meaning, and connections — then serves them to LLM agents and application search UIs. Teams work in case/folder memory containers where every discussion, document, and event accumulates into shared knowledge. Three-axis retrieval (full-text + semantic + graph) finds not just *what* exists, but *how things are connected and why they happened*.

**For any team:** Ingest documents from anywhere (cloud drives, git, local files, URLs, images, video). Organize knowledge in case-scoped containers with threaded discussions and memory diffing. Search across text, meaning, and relationships in a single query.

**For Hexalith.EventStore users:** Zero-code memory integration. Drop in the NuGet package, subscribe to DAPR topics, and your entire event stream is automatically indexed with causal chains, dual embeddings, and full metadata. No mapping code required.

Every feature is accessible through both MCP (for LLM agents) and CLI (for developers and LLMs alike). Enterprise-grade tenant isolation with physically separate indexes per tenant. Start on Redis, scale to Qdrant — seamless swap via abstraction interfaces.

**Validation approach:** 5-10 benchmark queries that require all three axes will be defined in Phase 1 to prove the three-axis hypothesis (e.g., "Find documents about payment processing referenced in discussions following the March deployment failure").

---

## Core Vision

### Problem Statement

Knowledge in modern teams is fragmented. Documents scatter across cloud drives and git repositories. Team discussions happen in ephemeral chat sessions. Application data lives in event stores and databases. When an LLM agent needs context or a user needs to find information, there is no unified system that spans these sources — and critically, no system that understands the *relationships between* these pieces of knowledge.

### Problem Impact

- **LLM agents suffer from amnesia** — no persistent memory beyond the context window, no access to organizational knowledge, no understanding of how information connects
- **Team knowledge is fragile** — onboarding takes weeks of tribal knowledge transfer, institutional knowledge is lost when people leave, teams working on similar problems never discover each other
- **Relationships are invisible** — documents reference other documents, events cause other events, decisions build on prior decisions — but no search tool understands these connections
- **Event-sourced systems are rich but opaque** — for teams using event sourcing, every command, event, and projection captures causal data, but this knowledge graph isn't discoverable through natural language or semantic search

### Why Existing Solutions Fall Short

| Solution | What it does | What it misses |
|---|---|---|
| RAG/Memory tools (Mem0, Zep, LangChain) | Basic LLM memory | No multi-tenancy, no team collaboration, no relational graph, no case model |
| Elasticsearch / OpenSearch | Strong full-text search | No vectors, no graph traversal, no team memory containers |
| Pinecone / Weaviate / Qdrant | Vector similarity | No graph, no case model, no threaded discussions |
| Notion / Confluence | Team knowledge bases | No LLM integration, no semantic search, no causal understanding |
| Duct-tape combination | Qdrant + LangChain + PostgreSQL | Works for demos, collapses under enterprise requirements — no physical tenant isolation, no integrated graph, custom glue code to maintain |

No existing solution provides an **integrated system** combining team-scoped memory, three-axis retrieval, and enterprise isolation in a single deployment.

### Why Now

Three converging trends create a window that didn't exist two years ago:
- **MCP is the standard** for LLM tool integration — agents need memory servers they can call natively
- **Team AI adoption is accelerating** — organizations need shared memory, not per-user silos
- **Enterprise AI demands isolation** — regulated industries require physical tenant separation, not shared indexes with filters

### Proposed Solution

Hexalith.Memories is a three-axis relational memory server that:

1. **Organizes knowledge in team-scoped cases** — case/folder containers where every member's discussions, documents, and events accumulate into shared knowledge with threaded discussions and episodic chronology
2. **Searches across content, meaning, AND connections** — full-text (BM25), semantic (vector embeddings), and relational (graph traversal) in a single query — behind swappable abstraction interfaces (IMemoryIndex, IMemoryGraph, IEmbeddingProvider)
3. **Ingests from everywhere** — cloud drives, git, local files, URLs, images, video via pluggable embedding providers. For Hexalith.EventStore users: zero-code integration via auto-discovered event handlers
4. **Scales transparently** — start on Redis (RediSearch + Vector Search + FalkorDB), scale to Qdrant + dedicated graph when needed. Swap backends via configuration, not code changes
5. **Enforces strict enterprise isolation** — physically separate indexes per tenant, matching production security requirements

**Design principles:** Full feature parity on MCP + CLI (both usable by LLMs and developers). DAPR-native infrastructure abstraction. Async ingestion via actors. Confidence-tracked metadata (human-declared vs AI-inferred).

**Demo-Worthy Hero Features:**
- **Case Briefing:** New team member asks "brief me on this case" — LLM composes chronological narrative from case memory
- **Memory Diffing:** "What changed since I last looked?" — returns new documents, discussions, events, changed relations
- **Causal Chain Traversal:** "What led to this decision?" — walks CausationId/CorrelationId graph and composes the story
- **Zero-Code Event Indexing:** Drop in NuGet package, all events appear in memory with dual embeddings

### Key Differentiators

**Competitive Moat 1: Team-Scoped Collaborative Memory**
No competitor offers case/folder memory containers where every team member's LLM conversations, documents, and events accumulate into shared knowledge. Threaded discussions, episodic chronology, memory diffing ("what changed since I last looked?"), and onboarding briefings ("brief me on this case") are unique to Hexalith.Memories.

**Competitive Moat 2: Causal Intelligence (Hexalith Ecosystem)**
For EventStore users: the only memory system that understands *why* things happened. CausationId/CorrelationId chains are indexed as graph edges. Your LLM can answer "what led to this decision?" and "what happened because of it?" Zero-code integration — drop in the package and all events are indexed with dual embeddings (payload + natural language description).

**Competitive Moat 3: Integrated System over Duct Tape**
The real moat isn't any single feature — it's the depth of integration. Three-axis retrieval, case-scoped graph, physical tenant isolation, threaded discussions, confidence tracking, async ingestion, and embedding versioning all work together as one system. Replicating this with separate tools requires custom glue code that collapses under enterprise requirements.

### Known Trade-offs and Hypotheses

| Decision | Trade-off | Rationale |
|---|---|---|
| One memory, one case | Same document in two cases = two copies | Clean ownership, clean deletion, no orphan references. Acknowledged cost: potential divergence over time |
| Redis as starting backend | Memory-bound at scale | IMemoryIndex abstraction enables seamless migration to Qdrant. Validated migration path is a Phase 2 priority |
| Three-axis retrieval value | Unvalidated hypothesis | Does syntactic + semantic + graph produce noticeably better results than semantic alone? Must be validated with real users early |
| Pluggable embedding provider | No model lock-in | IEmbeddingProvider abstraction prevents dependency on any single model vendor (Google, OpenAI, etc.) |

### Beachhead Persona

Developers building AI-powered applications on .NET/DAPR — already in the Hexalith ecosystem, feeling the LLM amnesia pain daily, able to validate the three-axis hypothesis fastest.

---

## Target Users

### Jobs-to-be-Done

| Persona | Job to be Done | Hiring Criteria |
|---|---|---|
| Alex (Developer) | "Help me add AI features to my EventStore app without building memory infrastructure" | Speed to first result (<30 min), zero-code integration, works with DAPR |
| LLM Agent | "Give me relevant, sourced context within my token budget" | Response quality, source attribution, token efficiency |
| Marcus (Team Lead) | "Help me keep my team's knowledge alive and accessible" | Onboarding speed, knowledge visibility, decay detection |
| Kenji (Operator) | "Help me run this reliably without surprises" | Single-command provisioning, clear scaling path, observability |
| Priya (End Beneficiary) | "Help me find what I need and understand the context" | She hires Alex's application, not Hexalith.Memories directly |

### Primary Users

**Persona 1: Alex — .NET/DAPR Developer (Beachhead)**

- **Role:** Senior .NET developer building event-sourced applications on Hexalith.EventStore
- **Context:** Works in a mid-to-large enterprise team (5-20 developers). Uses DAPR, .NET 10, C# 13. Recently tasked with adding AI-powered features.
- **Problem:** LLM integrations have no memory. EventStore has years of rich causal data but the LLM can't access it. Duct-taped Qdrant + LangChain fell apart when the team needed multi-tenancy.
- **Workarounds:** Manual context stuffing, copy-pasting event data into prompts, separate Elasticsearch always out of sync.
- **Aha moment:** `dotnet add package Hexalith.Memories.Client` → configure DAPR subscription → all events appear with causal chains. First MCP query returns: "This projection changed because of event X, caused by command Y issued by user Z." Zero mapping code.
- **Success:** "I shipped the AI feature in days, not weeks. My LLM agent knows what happened and why."
- **Hard criterion:** Under 30 minutes from `dotnet add package` to first search result. If it takes longer, Alex bounces.

**Persona 2: The LLM Agent (Non-Human User)**

- **Role:** AI agent interacting via MCP or CLI
- **Context:** Orchestrated by application code or user prompts. Needs contextual knowledge within token budget constraints.
- **Problem:** Stateless by nature. No access to organizational history, team discussions, or event-sourced data.
- **Aha moment:** `search_memory(query="what led to the API redesign?", case="project-alpha")` returns token-budget-aware narrative with source attribution, confidence scores, and causal chains.
- **Success:** Produces responses grounded in actual organizational knowledge with traceable sources.

**Persona 3: Marcus — Team Lead / Case Owner**

- **Role:** Engineering or project lead managing multiple cases and teams
- **Context:** Responsible for knowledge continuity. Onboards new members regularly.
- **Problem:** Hours briefing new members. No visibility into what knowledge exists across cases. Institutional knowledge lost when people leave.
- **Key interactions:** Creates cases, manages access, reviews case health, runs cross-case insight discovery, monitors knowledge decay alerts.
- **Success:** "New team members are productive on day one."

### Secondary Users

**Persona 4: Kenji — Platform Operator**

- **Role:** DevOps / platform engineer managing Hexalith infrastructure
- **Context:** Manages DAPR deployments, Redis clusters, tenant provisioning, scaling, monitoring.
- **Key interactions:** CLI for tenant management, index health monitoring, scaling decisions, backend migration (Redis → Qdrant), geographic pinning.
- **Success:** "Tenant provisioning is one command. Backend swap with zero downtime."

### End Beneficiary

**Persona 5: Priya — Application End-User**

- **Role:** Business analyst, case worker, or knowledge worker using applications *built on* Hexalith.Memories
- **Context:** Not technical. Uses web applications daily. Never sees the memory server — only sees what Alex's application presents via REST API.
- **Problem:** Information scattered across SharePoint, Teams, email. Days to understand a new case. Knows the answer is "somewhere."
- **How she benefits:** Searches across documents, events, and discussions in one query. Asks the AI "brief me on this case." Gets answers with context and attribution.
- **Why she matters:** Priya is the ultimate validation. If she can find what she needs without knowing what's under the hood, the system works. She's the screenshot on the landing page.

### Developer Journey — Alex

| Stage | Experience |
|---|---|
| **Discovery** | Finds Hexalith.Memories referenced in EventStore docs, DAPR ecosystem listing, or GitHub trending. README shows 30-second zero-code demo. |
| **Onboarding (<30 min)** | `dotnet add package` → DAPR subscription config → `memories search "test"` in CLI → first results. Under 30 minutes or we've failed. |
| **Trust Building** | `memories search "payment" --explain` shows why each result was returned, which index matched, confidence scores. `memories explore --case alpha` for interactive graph browsing. Debug-first DX — Alex understands before Alex ships. |
| **Core Usage** | Adds MCP tool definitions to AI assistant. Builds search UI with REST API. Uses CLI for debugging and exploration. |
| **Aha Moment** | First causal chain query returns narrative connecting events, documents, and discussions. "This is what I've been building manually." |
| **Long-term** | Memories becomes standard infrastructure for every new Hexalith app. Team creates cases per project. Knowledge accumulates organically. |

---

## Success Metrics

### User Success Metrics

| Persona | Success Indicator | Measurement |
|---|---|---|
| **Alex (Developer)** | Onboards in <30 minutes | Time from `dotnet add package` to first successful search result |
| **Alex (Developer)** | Ships AI features using Memories | Number of projects integrating Hexalith.Memories client |
| **Alex (Developer)** | Trusts the search results | Uses `--explain` less over time; ships to production without manual verification |
| **LLM Agent** | Gets better answers | Retrieval relevance score (measured via benchmark queries) |
| **LLM Agent** | Stays within token budget | Response size respects caller-specified token limits |
| **LLM Agent** | Low latency | Search-to-response time <200ms for cached, <2s for cold queries |
| **Marcus (Team Lead)** | Faster onboarding | Time for new team member to make first productive contribution on a case |
| **Marcus (Team Lead)** | Knowledge visibility | Number of cases with active memory (>10 memory units, accessed in last 30 days) |
| **Kenji (Operator)** | Friction-free operations | Tenant provisioning in single CLI command, <5 min |
| **Kenji (Operator)** | No surprises | Zero data leaks across tenant boundaries; no unplanned downtime from index growth |
| **Priya (End Beneficiary)** | Finds what she needs | Search success rate in applications built on Memories (measured by Alex's app analytics) |

### Three-Axis Validation Metrics

The core hypothesis — three-axis retrieval produces noticeably better results than semantic alone — must be validated early.

| Metric | Target | How to Measure |
|---|---|---|
| **Benchmark query accuracy** | Three-axis outperforms single-axis on 80%+ of benchmark queries | 5-10 benchmark queries requiring all three axes, scored by result relevance |
| **Causal chain completeness** | Graph traversal returns complete causal chain for 95%+ of EventStore events | Automated test: for known CausationId chains, verify full path is traversable |
| **Hybrid query value** | Combined syntactic+semantic+graph returns results that no single axis alone would find | Manual review of top-20 hybrid query results vs. single-axis results |

### Adoption Metrics

| Metric | 3-Month Target | 12-Month Target |
|---|---|---|
| **GitHub stars** | 100+ | 1,000+ |
| **NuGet downloads** | 500+ | 5,000+ |
| **Contributors** | 3+ (beyond core team) | 10+ |
| **Issues / discussions** | Active community engagement (>20 issues, >10 discussions) | Self-sustaining community with external PRs |
| **EventStore integration users** | 5+ projects using auto-registration | 50+ projects |
| **MCP tool adoption** | Listed in MCP tool directories | Referenced in LLM agent tutorials/guides |

### Ecosystem Health Metrics

| Metric | Measurement |
|---|---|
| **EventStore integration depth** | % of EventStore event types automatically indexed in projects using Memories |
| **DAPR compatibility** | Works with all DAPR state store components without custom code |
| **Embedding provider diversity** | At least 2 providers implemented (Google + one other) within 6 months |
| **Backend portability** | Validated migration path: Redis → Qdrant with zero application code changes |
| **CLI completeness** | 100% of features accessible via CLI |
| **MCP completeness** | 100% of features accessible via MCP tools |

### Technical Quality Metrics

| Metric | Target |
|---|---|
| **Ingestion throughput** | >100 documents/minute per tenant (async pipeline) |
| **Search latency (p95)** | <200ms syntactic, <500ms semantic, <1s hybrid, <2s graph traversal |
| **Tenant isolation** | Zero cross-tenant data leaks (verified by automated security tests) |
| **Index freshness** | EventStore events indexed within <5s of publication to DAPR pub/sub |
| **Embedding versioning** | Model migration completes with zero downtime and <5% relevance degradation |
| **Data durability** | Zero memory unit loss during Redis restart (AOF persistence verified) |

### Business Objectives (Open-Source Project)

| Objective | Success Criteria |
|---|---|
| **Hexalith ecosystem completeness** | Memories fills the knowledge/search gap — EventStore + Memories covers the full event-sourced application lifecycle |
| **Developer productivity multiplier** | Teams using Memories ship AI features 5x faster than teams building custom memory infrastructure |
| **Knowledge preservation** | Organizations using case memory report measurably faster onboarding and reduced knowledge loss |
| **Project sustainability** | Active contributor community, regular releases, no single-person dependency |

---

## Risks

### Consolidated Risk Register

| # | Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|---|
| R1 | **Three-axis hypothesis doesn't hold** — hybrid retrieval doesn't noticeably outperform pure semantic search | Critical — undermines core product thesis | Medium | Benchmark suite in MVP validates early. If graph axis fails, pivot to syntactic+semantic with case-scoped metadata (still differentiated). |
| R2 | **Redis scale wall** — in-memory cost becomes prohibitive with large tenants (5M+ documents) | High — blocks enterprise adoption | Medium | IMemoryIndex abstraction enables Qdrant migration. But: start concrete (Redis implementation first), extract interface when second backend is needed. Don't over-abstract day one. |
| R3 | **Case rigidity frustrates users** — one-memory-one-case means duplicate ingestion for cross-case content | Medium — user friction | Medium | Monitor user feedback. If pattern emerges, consider lightweight cross-case references (not shared ownership) in Phase 2. |
| R4 | **Embedding model lock-in** — Google changes pricing, deprecates model, or better model emerges | Medium — cost/quality impact | Low | IEmbeddingProvider abstraction. Implement Google first, add second provider within 6 months. |
| R5 | **Competitive catch-up** — Mem0, Zep, or LangChain add multi-tenancy and collaborative features | High — erodes differentiation | Medium | Speed to market. The causal intelligence moat (EventStore integration) is ecosystem-specific and hardest for competitors to replicate. Collaborative memory is replicable but requires significant design effort. Estimated defensibility window: 12-18 months. |
| R6 | **Small addressable market** — Hexalith.EventStore user base is small, limiting zero-code integration value | Medium — slows adoption | High | Brief includes standalone value story for non-EventStore users. Case/folder memory + three-axis search valuable independently. EventStore integration is superpower, not prerequisite. |
| R7 | **Over-abstraction slows development** — three interfaces (IMemoryIndex, IMemoryGraph, IEmbeddingProvider) before first line of business logic | Medium — delays shipping | Medium | Start with concrete Redis implementation. Extract interfaces when adding second backend. YAGNI until proven otherwise. |

---

## MVP Scope

### Implementation Approach

**Start concrete, abstract later.** Build the Redis implementation directly. Extract IMemoryIndex/IMemoryGraph/IEmbeddingProvider interfaces when the second backend is needed (Phase 3). This avoids premature abstraction while preserving the architectural escape hatch.

**Project structure:** Multi-project solution:
- `Hexalith.Memories.Contracts` — domain types, memory unit model, envelopes
- `Hexalith.Memories.Server` — DAPR service, actors, ingestion pipeline
- `Hexalith.Memories.Client` — client library for .NET consumers
- `Hexalith.Memories.Redis` — Redis/RediSearch/Vector/FalkorDB implementation
- `Hexalith.Memories.Cli` — CLI tool
- `Hexalith.Memories.Mcp` — MCP server
- `Hexalith.Memories.EventStore` — EventStore integration (auto-registration, dual embedding)

### First Week Build Sequence (Critical Path)

| Day | Deliverable | What's working |
|---|---|---|
| **Day 1** | DAPR service scaffold + Redis connection + project structure | `dotnet run` starts service, connects to Redis |
| **Day 2** | RediSearch index + basic ingestion + syntactic search via CLI | `memories ingest "file.txt"` → `memories search "keyword"` returns results |
| **Day 3** | Redis Vector Search + embedding + semantic search | `memories search --semantic "concept"` returns similar content |
| **Day 4** | FalkorDB + relations + graph traversal | `memories traverse --from id1 --depth 2` walks the graph |
| **Day 5** | DAPR pub/sub + EventStore event auto-indexing | Publish event to DAPR topic → appears in memory with causal metadata |
| **End of week 1** | **Demo:** Event published → auto-indexed → searchable via all three axes via CLI |

### Core Features (Phase 1)

| # | Feature | What it delivers | Validates |
|---|---|---|---|
| 1 | **Memory Engine** | Redis implementation (RediSearch + Vector Search + FalkorDB) — concrete, no interfaces yet | Three-axis foundation works |
| 2 | **Content Ingestion API** | Ingest URL/file content with metadata, extraction phrases, human-declared + AI-extracted metadata with confidence tracking | Dual-origin metadata model, async pipeline |
| 3 | **EventStore Integration** | DAPR pub/sub subscription, auto-discovered event handlers, dual embedding (payload + NL description), causal chain indexing (CausationId/CorrelationId) | Zero-code integration promise, <30 min onboarding |
| 4 | **Three-Axis Search** | Syntactic (BM25), semantic (vector similarity), graph traversal — each axis independently, then hybrid queries combining them | Core hypothesis — three-axis > single-axis |
| 5 | **Case/Folder Model** | Create/delete cases, assign memory units (strict ownership), case-scoped graph edges, tenant-wide search with case filter | Collaborative memory moat |
| 6 | **CLI** | `memories ingest`, `memories search`, `memories search --explain`, `memories explore --case`, `memories tenant`, `memories case`. Helpful error messages and empty state guidance ("No results. Try: ...") | Developer experience, <30 min onboarding, debug-first DX |
| 7 | **MCP Server** | `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info` tools with typed parameters and token-budget aware responses | LLM agent integration, MCP-first positioning |
| 8 | **Tenant Isolation** | Physically separate Redis indexes per tenant, tenant enforced at API/CLI/MCP layer, DAPR actor scoping | Enterprise requirement, zero-leak guarantee |
| 9 | **DAPR Infrastructure** | Service scaffold with actor model, pub/sub subscription, state management, service invocation | DAPR-native architecture |
| 10 | **Benchmark Query Suite** | 5-10 queries that require all three axes, automated scoring of three-axis vs single-axis results | Three-axis hypothesis validation |
| 11 | **README** | 30-second zero-code demo, getting started guide, architecture overview — the README is a product deliverable, not an afterthought | Discovery, onboarding, first impression |

### Tenant Isolation Test Matrix

The "zero cross-tenant data leaks" criterion requires a dedicated test plan:
- Create tenant A and tenant B with identical content
- Search from tenant A context → verify zero results from tenant B
- Search with malformed tenant ID → verify rejection
- Search with empty tenant → verify rejection
- Search with tenant A's ID from tenant B's auth context → verify rejection
- Ingest into tenant A → verify not searchable from tenant B across all three axes (syntactic, semantic, graph)
- Delete tenant A → verify all indexes and graph data cleanly removed

### Out of Scope for MVP

| Feature | Why deferred | When |
|---|---|---|
| Memory diffing ("what changed?") | High value but requires versioned snapshots of case state | Phase 2 |
| Discussion threading | Requires user identity integration and thread management | Phase 2 |
| Hot/cold memory tiers | Optimization — not needed until scale demands it | Phase 3 |
| Embedding versioning / model migration | Not needed until first model change | Phase 3 |
| Knowledge decay detection | Requires usage analytics accumulation | Phase 3 |
| Access pattern learning | Requires usage analytics accumulation | Phase 3 |
| Memory Explorer UI / Timeline View | REST API ships in MVP; UI is a separate application concern | Phase 4 |
| Geographic pinning | Enterprise feature — DAPR config, no code change needed later | Phase 4 |
| Per-unit ACLs / LLM context redaction | Enterprise security — case-level isolation sufficient for MVP | Phase 4 |
| Extraction phrase templates | Convenience — users can write custom phrases in MVP | Phase 2 |
| REST API for search UIs | MCP + CLI + DAPR invocation are sufficient for MVP validation | Phase 2 |
| Content-addressed deduplication | Optimization — can be added transparently later | Phase 3 |
| Entity resolution | AI feature — requires more extraction maturity | Phase 3 |

### MVP Success Criteria

| Criterion | Gate | How to measure |
|---|---|---|
| **Developer onboarding** | Alex onboards in <30 minutes | Timed walkthrough: `dotnet add package` → first search result |
| **Three-axis validation** | Three-axis outperforms single-axis on 80%+ of benchmark queries | Benchmark suite automated scoring |
| **Causal chain works** | Graph traversal returns complete CausationId chains for EventStore events | Automated test against known event chains |
| **MCP integration works** | LLM agent produces sourced, contextual answer using memory | End-to-end test: LLM query → MCP → memory → response with attribution |
| **Tenant isolation holds** | Zero cross-tenant data leaks | Automated security test suite |
| **Case model works** | Memory units correctly scoped to cases, tenant-wide search returns cross-case results | Integration tests |

**Go/no-go decision:** If 5 of 6 criteria pass, proceed to Phase 2. If three-axis validation fails, re-evaluate the graph axis before expanding.

### Future Vision

**Phase 2: Collaboration & Polish**
- Discussion threading within cases
- Memory diffing ("what changed since X?")
- REST API for application search UIs
- Extraction phrase templates
- Onboarding briefing ("brief me on this case")

**Phase 3: Intelligence & Scale**
- Hot/cold memory tiers (Redis → blob storage)
- Embedding versioning for model migration
- Content-addressed deduplication
- Entity resolution
- Access pattern learning / query pattern memory
- Knowledge decay detection
- IMemoryIndex Qdrant implementation (validated migration path)

**Phase 4: Enterprise & UI**
- Memory Explorer UI (visual graph browser)
- Timeline View (chronological case evolution)
- Per-unit ACLs and role-based access
- LLM context redaction (PII masking)
- Geographic pinning via DAPR configuration
- Encryption at rest per tenant
- Compliance evidence gathering
- Audit trail on every memory operation

**Full vision (2-3 years):** Hexalith.Memories becomes the standard knowledge layer for event-sourced applications — every Hexalith app ships with memory by default, teams accumulate organizational intelligence organically, and LLM agents have deep, causal, team-scoped context for every interaction.

### Epic Structure (for sprint planning)

| Epic | Scope | Est. Stories |
|---|---|---|
| **E1: Memory Engine Foundation** | Redis setup, basic CRUD, tenant isolation, project scaffold | 5-8 |
| **E2: Ingestion Pipeline** | Content extraction, metadata handling, async actors, confidence tracking | 4-6 |
| **E3: Search** | Syntactic, semantic, graph traversal, hybrid — one story per axis + hybrid | 4-5 |
| **E4: EventStore Integration** | DAPR sub, auto-registration, dual embedding, causal chain indexing | 4-6 |
| **E5: CLI** | Ingest, search, explain, explore, case/tenant management, error messages, help text | 5-8 |
| **E6: MCP Server** | Tool definitions, token-budget responses, search/ingest/traverse/case-info | 3-5 |
| **E7: Validation & Docs** | Benchmark suite, tenant isolation tests, security tests, README, onboarding timer | 4-6 |
| **Total** | | **29-44 stories** |

### Documentation as Product

- **README** is a product deliverable — 30-second demo, getting started, architecture diagram. Written as carefully as the code.
- **CLI help text** is documentation — every command needs clear examples, not just flag descriptions. `memories search --help` should be as good as a docs page.
- **Error messages** are onboarding — when first search returns zero results: "No results found. Try: broader keywords, check case name, verify content was ingested (`memories status --case alpha`)". First failures are part of first impressions.
