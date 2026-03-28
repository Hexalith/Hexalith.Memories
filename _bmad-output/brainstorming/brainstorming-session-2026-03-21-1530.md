---
stepsCompleted: [1, 2, 3, 4]
inputDocuments: []
session_topic: 'Memory/Knowledge Indexing Server - full-spectrum design'
session_goals: 'Architecture, ingestion pipeline, search strategies, API design, use cases, feature innovation, technical trade-offs'
selected_approach: 'ai-recommended'
techniques_used: ['First Principles Thinking', 'Morphological Analysis', 'Cross-Pollination', 'Chaos Engineering', 'Deep Dive', 'UX Exploration', 'Security Analysis', 'DevEx Design', 'Business Value', 'Edge Cases']
ideas_generated: 66
session_active: false
workflow_completed: true
context_file: ''
---

# Brainstorming Session Results — Hexalith.Memories

**Facilitator:** Jerome
**Date:** 2026-03-21/22
**Ideas Generated:** 66 named ideas across 9 themes
**Techniques:** First Principles, Morphological Analysis, Cross-Pollination, Chaos Engineering + targeted deep dives

---

## Session Overview

**Topic:** Memory/Knowledge Indexing Server — a system that ingests content from files/URLs, extracts and indexes metadata (syntactic + semantic + relational), and serves enriched context for LLM chats and application search UIs.

**Goals:**
- Architecture & components
- Ingestion & metadata extraction pipeline
- Search & retrieval strategies (syntactic + semantic + graph)
- API design & integration patterns
- Use cases & user experiences
- Feature innovation & differentiation
- Technical alternatives & trade-offs

**Key Discovery:** This is not a search engine. It is a **relational memory layer** — a three-axis system (syntactic + semantic + graph) that indexes content, meaning, AND connections. Built as a DAPR-native sibling to Hexalith.EventStore.

---

## Technique Execution Results

### First Principles Thinking
- **Focus:** Strip away assumptions, rebuild from fundamental truths
- **Key Breakthroughs:** Relational memory as core concept, three-axis retrieval, reactive memory via DAPR pub/sub, dual API surface
- **Ideas Generated:** 8 foundational principles

### Morphological Analysis
- **Focus:** Systematically map every parameter dimension and explore combinations
- **Key Breakthroughs:** Redis unified stack with abstraction layer, MCP + CLI primary interfaces, strict tenant isolation with per-tenant physical indexes, case ownership with tenant-wide search
- **Ideas Generated:** 8 design decisions + full dimension matrix

### Cross-Pollination
- **Focus:** Transfer solutions from biology, git, recommendation engines, DNS, compilers, email, Wikipedia, version control, OS virtual memory, immune systems
- **Key Breakthroughs:** Case/folder as shared memory container, memory types (episodic/semantic/procedural), content-addressed deduplication, discussion threading, automatic entity resolution
- **Ideas Generated:** 16 cross-domain innovations

### Chaos Engineering
- **Focus:** Stress-test every idea against worst-case scenarios
- **Key Breakthroughs:** Embedding versioning for model migration, content freshness contracts, confidence-gated retrieval, async ingestion with progress
- **Ideas Generated:** 10 resilience patterns

### Additional Deep Dives
- **Redis vs Qdrant analysis** → IMemoryIndex/IMemoryGraph abstraction layer (start Redis, swap to Qdrant)
- **UX, Security, DevEx, Business Value, Edge Cases** → 18 additional ideas

---

## Architecture Blueprint

### Core Principles

1. **Relational Memory over Content Memory** — Index connections, not just content
2. **Three-Axis Retrieval** — Syntactic (BM25) + Semantic (vectors) + Graph (relations)
3. **Shared DAPR Plane** — Sibling service to EventStore, same infrastructure
4. **Dumb Memory, Smart Clients** — Server indexes faithfully, intelligence in callers
5. **Strict Tenant Isolation** — Physical index separation per tenant
6. **Case Ownership, Tenant Search** — Memory units owned by one case, searchable across tenant
7. **Dual-Origin Metadata** — Human-declared + AI-inferred, confidence tracked
8. **Backend Abstraction** — IMemoryIndex + IMemoryGraph interfaces, swappable implementations

### System Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    Hexalith.Memories                          │
│                                                              │
│  ┌─────────┐  ┌─────────┐  ┌──────────┐  ┌──────────────┐  │
│  │   MCP   │  │   CLI   │  │   REST   │  │ DAPR Service │  │
│  │ Server  │  │  Tool   │  │   API    │  │  Invocation  │  │
│  └────┬────┘  └────┬────┘  └────┬─────┘  └──────┬───────┘  │
│       └─────────┬──┴───────────┬┘               │           │
│                 ▼              ▼                 ▼           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              Memory Engine (DAPR Actors)              │   │
│  │                                                      │   │
│  │  ┌──────────────┐  ┌──────────────┐                  │   │
│  │  │IMemoryIndex  │  │IMemoryGraph  │                  │   │
│  │  │(Syntactic +  │  │(Relations +  │                  │   │
│  │  │ Semantic)    │  │ Traversal)   │                  │   │
│  │  └──────┬───────┘  └──────┬───────┘                  │   │
│  └─────────┼─────────────────┼──────────────────────────┘   │
│            ▼                 ▼                               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                   DAPR Sidecar                        │   │
│  │  State Store │ Pub/Sub │ Actors │ Secrets │ Service   │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│                    Redis (per tenant)                         │
│                                                              │
│  ┌───────────────┐  ┌─────────────────┐  ┌───────────────┐  │
│  │  RediSearch    │  │  Vector Search   │  │   FalkorDB    │  │
│  │  (BM25/FTS)   │  │  (Embeddings)    │  │   (Graph)     │  │
│  └───────────────┘  └─────────────────┘  └───────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### Data Scoping Model

```
Tenant (strict physical isolation — separate Redis indexes)
  └── Case/Folder (strict ownership — memory units belong to exactly one case)
       ├── Memory Units (content + embeddings + metadata)
       │   ├── Dual embeddings for events (payload + NL description)
       │   ├── Metadata (human-declared + AI-inferred, with confidence)
       │   └── Freshness policy (never/on_access/periodic/webhook)
       ├── Relations (graph edges — internal to case only)
       │   ├── Causal (Command → Event → Projection)
       │   ├── Correlation (shared CorrelationId)
       │   ├── Reference (document mentions document)
       │   ├── Temporal (same time window)
       │   ├── Semantic similarity (AI-inferred)
       │   ├── Cross-modal (image illustrates document)
       │   └── User-declared (explicit linking)
       └── Discussions (threaded user interactions per case)
```

### Content Sources

| Source | Ingestion Trigger | Extraction Method |
|---|---|---|
| EventStore events | DAPR pub/sub subscription (real-time) | Event envelope metadata (15 fields) + dual embedding |
| EventStore commands | DAPR pub/sub subscription | Command envelope metadata + payload embedding |
| EventStore projections | DAPR pub/sub subscription | Projection state embedding |
| Git repositories | Polling / webhooks | Full-text + code parsing |
| Local files (PDF, Word, PPT, MD, TXT) | API call / file watcher | Full-text + extraction phrases + AI extraction |
| Cloud drives (OneDrive, Google Drive) | Webhooks / polling | Full-text + extraction phrases + AI extraction |
| URLs / web content | API call | Full-text + extraction phrases + AI extraction |
| Images | API call | Google multimodal embedding + OCR |
| Videos | API call | Google multimodal embedding + transcript extraction |
| Structured data (JSON, XML, CSV) | API call | Schema-aware extraction + embedding |

### Interfaces

| Interface | Audience | Response Shaping |
|---|---|---|
| **MCP Server** | LLM agents (Claude, GPT, etc.) | Token-budget aware, graph-walked narrative context |
| **CLI** | Developers, operators | Raw results + metadata, interactive REPL mode |
| **DAPR Service Invocation** | Hexalith ecosystem services | Typed contracts, auto-registered event handlers |
| **REST API** | Application UIs | Ranked results, facets, pagination, drill-down |

### Abstraction Interfaces

```
IMemoryIndex
├── Ingest(content, metadata, extractionPhrases, caseId, tenantId)
├── SearchSyntactic(query, filters, tenantId, caseId?)
├── SearchSemantic(embedding, filters, tenantId, caseId?)
├── SearchHybrid(query, embedding, filters, weights, tenantId, caseId?)
├── Get(memoryUnitId, tenantId)
├── Delete(memoryUnitId, tenantId, caseId)
└── UpdateMetadata(memoryUnitId, metadata, tenantId, caseId)

IMemoryGraph
├── AddRelation(sourceId, targetId, relationType, confidence, caseId, tenantId)
├── Traverse(startId, relationTypes, depth, caseId, tenantId)
├── GetRelations(memoryUnitId, caseId, tenantId)
├── DeleteRelation(relationId, tenantId, caseId)
└── FindPaths(sourceId, targetId, caseId, tenantId)
```

---

## Complete Idea Inventory

### Theme 1: Core Architecture & Foundations (8 ideas)

| ID | Idea | Description |
|---|---|---|
| FP-1 | **Relational Memory** | Index connections, not just content. Every piece of information carries its causal chain, context, and relationships. Most search systems treat documents as isolated atoms — this treats them as nodes in a living graph. |
| FP-4 | **Three-Axis Retrieval** | Syntactic (exact match, facets, BM25) + Semantic (meaning similarity via vectors) + Relational (graph traversal across causal/reference/temporal links). Most powerful queries combine all three. Nobody does all three natively. |
| FP-6 | **Shared DAPR Plane** | Hexalith.Memories and EventStore share DAPR as infrastructure abstraction. Zero-config integration for pub/sub, state, service invocation, actors. Native sibling service, not bolted-on external tool. |
| FP-7 | **Actor-Per-Memory-Unit** | DAPR actors per indexed resource (per document, per aggregate event stream, per domain). Each actor manages its own content extraction, embedding, metadata, and relation graph. Scales naturally, self-healing. |
| M-1 | **Dumb Memory, Smart Clients** | The memory server has no opinion about what should be stored. It receives content + metadata + extraction instructions and faithfully indexes. Intelligence about what's worth memorizing lives in calling applications. |
| M-3 | **Redis Unified Stack** | Redis handles everything — RediSearch for full-text, Redis Vector Search for embeddings, FalkorDB for graph. One infrastructure, three capabilities, fully DAPR-compatible. |
| DD-1 | **IMemoryIndex Abstraction** | Clean interface abstracting syntactic + semantic operations. Start Redis, swap to Qdrant when scale demands. Application code never changes. Mirrors EventStore's DAPR state store abstraction. |
| DD-2 | **IMemoryGraph Abstraction** | Same principle for graph — start FalkorDB, swap to Neo4j or Apache AGE later. Two clean interfaces cover the entire memory engine. |

### Theme 2: Data Ingestion & Extraction Pipeline (8 ideas)

| ID | Idea | Description |
|---|---|---|
| FP-2 | **Dual-Origin Metadata** | Both metadata extraction and relationship discovery operate in two modes — human-declared (explicit, high-confidence) and AI-inferred (automatic, probabilistic). System tracks confidence levels and origin for each. |
| FP-3 | **Four Ingestion Modes** | Human/AI x Metadata/Relations = 4 paths. Each with different confidence profiles. Human "this is architecture doc for X" = near-certain. AI "seems related to these events" = probabilistic. |
| FP-5 | **Reactive Memory** | For EventStore data, subscribe to DAPR pub/sub topics in real-time. Memory grows with the system as events happen. For external files, use webhooks/polling. Memory is alive, not a batch job. |
| M-5 | **Dual Embedding per Event** | Every EventStore event gets two embeddings — raw serialized payload (technical/structural similarity) + AI-generated natural language description (semantic/business meaning). Two discovery paths to same event. |
| CH-10 | **Async Ingestion with Progress** | Ingestion is always asynchronous. API returns immediately with memory unit ID in 'ingesting' state. Extraction/embedding/relations happen via DAPR actors in background. Partial results searchable before full processing completes. |
| DX-4 | **Event Handler Auto-Registration** | DAPR event handler auto-discovers event types from EventStore domain assemblies. Drop in NuGet package, configure DAPR subscription, all events automatically indexed. Zero-code EventStore integration. |
| DX-5 | **Extraction Phrase Templates** | Pre-built templates: "legal-contract" extracts parties/dates/obligations, "meeting-notes" extracts decisions/action items/attendees, "architecture-decision" extracts context/decision/consequences. Custom templates supported. |
| E-4 | **Massive Single Document** | 2000-page documents get hierarchical representation — parent memory unit (document-level metadata, summary embedding) with child chunks (section-level embeddings, full-text). Relations link parent to children. Search at right granularity. |

### Theme 3: Tenant & Case Isolation Model (7 ideas)

| ID | Idea | Description |
|---|---|---|
| M-6 | **Strict Tenant Isolation** | Enforced at 4 layers: API/MCP/CLI (auth context), index naming (tenant prefix), DAPR actor scoping, graph isolation. Mirrors EventStore's 4-layer model. |
| M-7 | **Physical Index Isolation** | Every tenant gets physically separate Redis indexes — own RediSearch, own vector HNSW, own FalkorDB graph. Created automatically on tenant onboarding. Deletion cleanly drops all indexes. |
| CP-7 | **Case/Folder as Memory Container** | A case is a first-class memory container aggregating all discussions, documents, events, and searches from every user working on it. Case memory grows richer with every interaction. |
| CP-10 | **One Memory, One Case** | Every memory unit belongs to exactly one case. No shared references. Same document relevant to two cases = ingested twice, each with own metadata/relations/context. Clean deletion, no orphans. |
| CP-11 | **Owned by Case, Searchable Across Tenant** | Write/delete scoped to case. Search queries span all cases within tenant. Ownership is per-case, discovery is per-tenant. |
| M-8 | **Hybrid Scoping** | One RediSearch + vector + graph index per tenant. Every record tagged with case_id. Ownership ops require case match. Search defaults tenant-wide, filterable by case. Graph traversal constrained within-case. |
| CP-8 | **Memory Layering** | Three layers: Personal memory (user's own interactions), Case memory (shared across case members), Domain memory (global reference material). LLM queries walk all three with appropriate permissions. |

### Theme 4: Relation & Graph Intelligence (6 ideas)

| ID | Idea | Description |
|---|---|---|
| CP-9 | **Case as Episodic Collective Memory** | Case memory is chronological — "User A uploaded doc Monday, User B asked about deadlines Tuesday, User C added note Wednesday." New team member gets narrative briefing of what happened. |
| CP-12 | **Discussion Threading** | Every LLM discussion within a case is a thread. Messages linked. New conversations can create new thread or continue existing. LLM says "In a previous thread on March 15, user A discussed the same topic." |
| CP-13 | **Automatic Entity Resolution** | System identifies entities (people, projects, services) and resolves synonyms. "Project Alpha" = "Alpha project" = "the alpha initiative" = same node. AI-inferred with human override. Prevents fragmented memory. |
| CP-6 | **Change Propagation** | When source changes (new PDF version, updated projection), system propagates "stale" signal along relation graph. Dependent embeddings marked for re-extraction. Freshness maintained via dependency tracking, not batch re-indexing. |
| E-1 | **Circular Relations** | Graph traversal detects and handles cycles. Returns cycle as signal ("mutually dependent") rather than infinite-looping. Circular dependencies are information, not bugs. |
| E-3 | **Conflicting Metadata** | Human says "technical spec," AI says "legal contract." Both stored with sources and confidence. Search surfaces both. System flags conflicts for resolution. Conflict is signal, not error. |

### Theme 5: Search, Retrieval & Context Delivery (7 ideas)

| ID | Idea | Description |
|---|---|---|
| FP-8 | **Dual API Surface** | LLM Context API (rich narrative, causal chains, token-budget aware) + Search API (structured, filterable, paginated, faceted). Same engine, different response shaping. |
| M-4 | **MCP + CLI Primary** | Agent-first (MCP) and developer-first (CLI). DAPR service invocation for ecosystem. REST for application UIs. Four interface layers. |
| CP-1 | **Memory Types** | Episodic (events, what happened), Semantic (facts, definitions), Procedural (how-to, workflows). Each type has different retrieval strategies — episodic is time-traversed, semantic is similarity-searched, procedural is pattern-matched. |
| CP-5 | **Hierarchical Resolution** | Queries resolve through layers: local cache → tenant index → cross-domain links → external source re-fetch. Fast path for common queries, deep path for discovery, freshness checking for external content. |
| CP-14 | **Memory Diffing** | Diff case memory between two points in time. "What's new since I last looked?" Returns new documents, discussions, events, changed relations. LLM can brief a returning user. |
| UX-5 | **Chat-Native Search via MCP** | MCP tool returns pre-composed narrative, not raw results. "Three decisions were made: (1) REST over gRPC on March 5th [source: ArchDecision.md, confidence: high]..." LLM incorporates directly. |
| CH-5 | **Confidence-Gated Retrieval** | Every result carries origin (human/ai) and confidence score. Filterable by minimum confidence. LLM Context API defaults high-confidence, option to include lower. AI-extracted metadata always reviewable and correctable. |

### Theme 6: Resilience & Operations (9 ideas)

| ID | Idea | Description |
|---|---|---|
| CH-1 | **Embedding Storage Separation** | Hot vectors in Redis for active cases, cold vectors in blob storage. Only load vectors for cases with recent activity. Tenant-wide semantic search falls back to batch query against cold storage when needed. |
| CH-2 | **Sharded Tenant Indexes** | When tenant crosses threshold (e.g., 100K records), auto-shard indexes. Partition by case ranges or domain. Search fans out across shards, merges results. Transparent to caller. Horizontal growth. |
| CH-3 | **Content Freshness Contract** | Every external memory unit has freshness_policy: never (snapshot), on_access (re-check when queried), periodic (poll on schedule), webhook (source pushes). Stale content flagged in results. |
| CH-4 | **Dead Source Handling** | Source URL returns 404/403 → memory unit marked source_unavailable. Content, embeddings, relations preserved. Search results show warning. Periodic retry or owner notification. Knowledge not lost when files move. |
| CH-7 | **Embedding Versioning** | Vector index tagged with model version. New model → new content gets new embeddings, old re-embedded in background. Queries run against both indexes during migration, merge results. Zero-downtime model upgrades. |
| CH-8 | **Discussion Isolation with Merge** | Each user's active discussion is separate thread with own working context. Writes don't immediately affect other threads. Insights merged on conclusion. Other threads notified. Git-branch model for discussions. |
| CH-9 | **Geographic Pinning** | Tenant metadata includes data_region. DAPR routes Redis to correct geographic region. Application code unchanged. Data residency compliance via infrastructure configuration. |
| CP-15 | **Hot/Cold Memory Tiers** | Frequently accessed in Redis (hot). Rarely accessed offloaded to cheaper storage with stub in Redis (cold). On access, cold rehydrated. Case activity drives promotion/demotion. Keeps Redis lean. |
| CP-3 | **Content-Addressed Deduplication** | Content hash per memory unit. Same PDF uploaded via OneDrive and local file → recognized as same content. Indexed once, two references. Saves storage, reveals multi-context content. |

### Theme 7: Security & Compliance (5 ideas)

| ID | Idea | Description |
|---|---|---|
| S-1 | **Audit Trail** | Every ingest, search, update, delete, relation creation logged — who, when, what, which interface. Immutable per-tenant log. Enables access-pattern learning. Critical for compliance. |
| S-2 | **Memory Unit Access Control** | Beyond tenant/case isolation, per-unit ACLs. Confidential documents visible only to specific roles. Search engine respects ACLs — restricted content invisible to unauthorized users. |
| S-3 | **LLM Context Redaction** | MCP API applies redaction rules — PII masking, confidential field removal, classification-based filtering. Full memory exists, LLM sees only permitted content. Critical for regulated industries. |
| S-4 | **Encryption at Rest** | Tenant-specific encryption keys via DAPR secret store. Physical isolation + encryption = defense in depth. Compliant with data sovereignty requirements. |
| CH-6 | **Human Override with Audit Trail** | Any AI metadata/relation overridable by human. Override logged with who/when/why. AI original preserved. Corrections can improve future extraction quality. |

### Theme 8: User & Developer Experience (8 ideas)

| ID | Idea | Description |
|---|---|---|
| UX-1 | **Memory Explorer UI** | Visual graph explorer — nodes are documents/events/discussions, edges are relations. Click to see content, double-click to expand relations. Filter by type/time/confidence. Graph view reveals structure flat lists hide. |
| UX-2 | **Timeline View** | Chronological timeline of everything in a case. Scrub through time to see memory evolution. "What did we know on March 1st vs today?" Memory isn't static — showing evolution reveals patterns and gaps. |
| UX-3 | **"Why This Result?" Explainability** | Every result explains its retrieval path — "semantic similarity 0.87," "causal chain X→Y→Z," "keyword match in human-declared metadata." Transparent retrieval builds trust. |
| UX-4 | **Natural Language CLI** | `memories ingest "url" --extract "decisions" --case "alpha"`. One command. As easy as `git add`. Developer experience determines adoption. |
| DX-1 | **.NET SDK with Fluent API** | `await memories.InTenant("A").InCase("alpha").Search().Semantic("API design").WithMinConfidence(0.8).ExecuteAsync()`. Typed, discoverable, composable. Matches Hexalith .NET ecosystem. |
| DX-2 | **MCP Tool Definitions** | Ship pre-built MCP tools: search_memory, ingest_content, traverse_relations, get_case_timeline. Typed parameters, documented schemas. Any MCP-compatible agent gets memory out of the box. |
| DX-3 | **CLI Interactive Mode** | Both one-shot commands and interactive REPL. Explore results, traverse relations, drill into cases, ingest content conversationally. Faster than any web UI for power users. |
| E-2 | **Empty Case Bootstrap** | New case with zero content responds intelligently: "This case is new. Ingest documents, link events, or start a discussion to build context." Graceful cold start, not "no results found." |

### Theme 9: Business Intelligence & Value (7 ideas)

| ID | Idea | Description |
|---|---|---|
| B-1 | **Onboarding Accelerator** | New team member asks LLM "brief me on this case." Memory server composes chronological narrative — key events, decisions, open questions, who's involved. Weeks of onboarding → minutes. |
| B-2 | **Knowledge Decay Detection** | Surfaces old, never-accessed, unconnected memory units. "These 15 documents haven't been referenced in 6 months. Still relevant?" Active memory hygiene prevents digital junk drawer. |
| B-3 | **Cross-Case Insight Discovery** | Tenant-wide semantic search reveals Case A and Case B dealing with similar problems independently. "Both cases have payment gateway docs. Teams may benefit from connecting." Breaks silos. |
| B-4 | **Compliance Evidence Gathering** | "Show every decision related to data retention in last 12 months across all cases." Graph walk + search = audit-ready evidence package. Audit prep from weeks to single query. |
| CP-4 | **Access Pattern Learning** | Track what memory units are retrieved together. "People who needed this also needed..." Pre-suggest related content. System gets smarter with use without explicit training. |
| CP-16 | **Query Pattern Memory** | Remember queries with poor/great results. Build map of "what works" for finding things. Suggest improvements: "Did you mean...?" Search improves from observing satisfaction signals. |
| CP-2 | **Memory Consolidation** | Frequently accessed content promoted (higher relevance, hot index). Unused content decays (deprioritized, not deleted). Like brain consolidation. Prevents ever-growing dump. |

---

## Key Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Infrastructure abstraction | DAPR | Matches EventStore, portable across cloud providers |
| Search + Vector engine | Redis (RediSearch + Vector Search) via IMemoryIndex | Start simple, swap to Qdrant when scale demands |
| Graph engine | FalkorDB (Redis-based) via IMemoryGraph | Same Redis infrastructure, Cypher queries |
| Embedding model | Google multimodal | Text + images + video in one space |
| Event embedding | Dual (payload + NL description) | Two semantic entry points per event |
| Tenant isolation | Physical index separation | Separate Redis indexes per tenant, no shared state |
| Case model | Strict ownership, tenant-wide search | One memory = one case, but discoverable across tenant |
| Primary interfaces | MCP + CLI | Agent-first, developer-first |
| Secondary interfaces | DAPR invocation + REST | Ecosystem integration + application UIs |
| Ingestion model | Always async, actor-based | No timeouts, partial results available immediately |
| Metadata confidence | Origin (human/ai) + confidence score on every field | Trust calibration, transparent retrieval |

---

## Implementation Roadmap Suggestion

### Phase 1: Foundation
- DAPR service scaffold with actor model
- IMemoryIndex + IMemoryGraph interfaces
- Redis implementation (RediSearch + Vector + FalkorDB)
- Tenant isolation (physical index separation)
- Case/folder ownership model
- Basic ingestion API (URL + metadata + extraction phrases)
- CLI with core commands (ingest, search, get)

### Phase 2: EventStore Integration
- DAPR pub/sub subscription for events/commands/projections
- Event handler auto-registration from domain assemblies
- Dual embedding (payload + NL description)
- Causal chain indexing (CausationId/CorrelationId)
- Basic graph relations (causal, correlation, aggregate membership)

### Phase 3: Intelligence Layer
- AI metadata extraction
- AI relation discovery
- Confidence scoring and tracking
- Extraction phrase templates
- Entity resolution
- Content-addressed deduplication

### Phase 4: LLM Integration
- MCP server with tool definitions
- Token-budget aware context composition
- Graph-walked narrative responses
- Chat-native search
- Discussion threading per case

### Phase 5: Advanced Features
- Memory diffing (what changed since X)
- Hot/cold memory tiers
- Embedding versioning for model migration
- Content freshness contracts
- Access pattern learning
- Cross-case insight discovery

### Phase 6: Enterprise
- Per-unit access control
- LLM context redaction
- Encryption at rest per tenant
- Geographic pinning via DAPR
- Audit trail
- Compliance evidence gathering
- Memory Explorer UI + Timeline View

---

## Session Insights

**Key Breakthrough:** The fundamental insight that this is a **three-axis relational memory system** (syntactic + semantic + graph), not a two-axis search engine, transforms the entire architecture. The graph dimension — causal chains from EventStore, cross-modal references, entity resolution — is what differentiates Hexalith.Memories from every existing solution.

**Architecture DNA:** The decision to mirror EventStore's architectural patterns (DAPR-native, actor-based, interface-abstracted, strictly tenant-isolated) ensures the two systems feel like one coherent ecosystem. Drop-in integration via auto-registered event handlers is the killer developer experience feature.

**Redis Convergence with Escape Hatch:** Starting with Redis (RediSearch + Vector + FalkorDB) for all three axes keeps operational complexity minimal. The IMemoryIndex/IMemoryGraph abstraction provides a clean migration path to Qdrant + Neo4j if scale demands it — without application code changes.

**Case as Memory Container:** The case/folder model — strict ownership, tenant-wide search, threaded discussions, episodic chronology — transforms the system from a search engine into a collaboration intelligence platform. The onboarding accelerator use case alone justifies this design.
