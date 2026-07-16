---
stepsCompleted: ['step-01-init', 'step-02-discovery', 'step-02b-vision', 'step-02c-executive-summary', 'step-03-success', 'step-04-journeys', 'step-05-domain', 'step-06-innovation', 'step-07-project-type', 'step-08-scoping', 'step-09-functional', 'step-10-nonfunctional', 'step-11-polish', 'step-12-complete']
inputDocuments: ['_bmad-output/planning-artifacts/product-brief-Hexalith.Memories-2026-03-22.md']
documentCounts:
  briefs: 1
  research: 0
  brainstorming: 0
  projectDocs: 0
classification:
  projectType: 'Developer Tool / API Backend'
  domain: 'AI Infrastructure / Knowledge Management'
  complexity: 'Medium-High'
  projectContext: 'Greenfield'
workflowType: 'prd'
---

# Product Requirements Document - Hexalith.Memories

**Author:** Jerome
**Date:** 2026-03-22

## Executive Summary

Your LLM agent forgets everything between sessions. Your team's knowledge is scattered across cloud drives, chat logs, and event stores. When someone asks "why did this happen?" — no search tool can answer. The relationships between documents, events, and decisions are invisible.

Hexalith.Memories is an open-source relational memory server that answers "why did this happen?" and "how are these connected?" — questions every team asks and no existing tool can answer. It organizes knowledge in team-scoped case containers, then searches across content, meaning, and connections in a single query. An LLM agent asks: *"What led to the API redesign?"* — and gets back a sourced narrative walking the causal chain from the original incident, through the team discussion, to the architecture decision record. Not just documents — the *story* of how they connect.

The system combines three retrieval axes — syntactic search (BM25), semantic search (vector embeddings), and graph traversal — into a unified hybrid query. This three-axis approach is the core thesis: if hybrid retrieval doesn't outperform single-axis on 80%+ of benchmark queries (scored by result relevance against ground truth), the product direction must be re-evaluated. The hybrid ranking/fusion algorithm — how results from three engines are merged and weighted — is the primary technical risk and the key R&D investment.

The hard onboarding criterion: **under 30 minutes from `dotnet add package` to first search result.** For developers already on Hexalith.EventStore, integration is zero-code: add a NuGet package, subscribe to DAPR topics, and the entire event stream is automatically indexed with causal chains (CausationId/CorrelationId as graph edges) and dual embeddings (payload + natural language description).

Teams organize knowledge in case/folder memory containers where documents, discussions, and events accumulate into shared, searchable knowledge. Every memory unit tracks whether its metadata was set by a human or inferred by AI, with a confidence score. Physically separate indexes per tenant enforce enterprise-grade isolation. The system runs on DAPR, starts on Redis (RediSearch + Vector Search + FalkorDB), with architecture designed to support backend portability (concrete implementation first, extraction points identified for future migration).

Every feature is accessible through both MCP (for LLM agents) and CLI (for developers). The MVP validates the three-axis thesis via CLI; MCP ships as a fast-follow within 4 weeks of thesis validation. Both interfaces are first-class citizens, both usable by LLMs.

### What Makes This Special

**The memory server that understands causality.** Event-sourced systems already capture *why* things happen — every command, event, and projection carries CausationId and CorrelationId. But that causal graph is locked inside infrastructure. Hexalith.Memories auto-discovers these relationships and makes them queryable: *"What happened because of this deployment?"* walks the graph and composes the story. Zero mapping code, zero configuration. You're already capturing this data — we just make it queryable.

EventStore integration is the first proof point for causal intelligence, with a clear path to support other event-sourced frameworks (Axon, Marten, Wolverine). Beyond causality, two additional differentiators compound the value:

- **Team-scoped collaborative memory** — case/folder containers with threaded discussions, memory diffing ("what changed since I last looked?"), and onboarding briefings ("brief me on this case"). No competitor offers this.
- **Integrated system over duct tape** — three-axis retrieval, case-scoped graph, physical tenant isolation, confidence tracking, async ingestion, and embedding versioning work together as one system. Replicating this with separate tools requires custom glue code that collapses under enterprise requirements.

## Project Classification

- **Project Type:** Developer Tool / API Backend (NuGet packages + DAPR service + CLI + MCP server)
- **Domain:** AI Infrastructure / Knowledge Management
- **Complexity:** Medium-High — driven by three-axis query fusion, DAPR actor model, multi-tenancy with physical isolation, and EventStore ecosystem integration
- **Project Context:** Greenfield
- **License:** Open-source

## Success Criteria

### User Success

| Persona | Success Criterion | Measurement | Target |
|---|---|---|---|
| **Alex (Developer)** | Onboards without hand-holding | Time from `dotnet add package` to first successful search result | <30 minutes — hard gate |
| **Alex (Developer)** | Ships AI features using Memories | Projects integrating Hexalith.Memories client | Tracked via NuGet dependency graph |
| **Alex (Developer)** | Trusts the system enough to ship | Deploys an application using Memories to production | Within 60 days of first use |
| **LLM Agent** | Gets better answers than single-axis retrieval | Retrieval relevance score (NDCG@10) on benchmark queries | Three-axis outperforms single-axis on 80%+ of benchmarks |
| **LLM Agent** | Respects token budget | Response size stays within caller-specified limits | 100% compliance on budget-constrained queries |
| **LLM Agent** | Low latency | Search-to-response time at 10 concurrent queries/tenant | <200ms cached, <2s cold |
| **Marcus (Team Lead)** | Instant case context | New member asks "brief me on this case" and gets accurate, sourced answer | Within 5 minutes of being added to the case |
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
| MCP directory listing | Listed in at least 1 directory | Not listed | Referenced in LLM agent tutorials | Still unlisted — **6-month hard target** |

**Concern thresholds** trigger a retrospective on positioning, documentation, or developer experience — not necessarily a pivot, but a mandatory "why" investigation.

**Sustainability signals (12-month "this is working" test):**
- **Community contributions:** External PRs beyond typo fixes — feature PRs, new embedding providers, backend implementations
- **Company adoption:** At least 2 organizations that have engaged with the project (issues, PRs, discussions) AND confirmed production usage

Both signals must be present. Community without production usage means it's interesting but not trusted. Production usage without community means it's useful but fragile.

### Technical Success

Detailed performance targets, verification methods, and phase tags are defined in the **Non-Functional Requirements** section (NFR1-31). Key hard gates: search latency <1s hybrid at 10K units/tenant, zero cross-tenant leaks, zero data loss on restart.

### Measurable Outcomes

**The Three-Axis Kill Switch:**
80% of benchmark queries (5–10 queries requiring all three axes) must show measurably better results from hybrid retrieval than any single axis alone. 80% is the hard line, not a stretch goal.

**Scoring protocol:**
- **Ground truth:** Defined by Jerome + 2 independent reviewers before benchmark queries are written
- **Automated scoring:** NDCG@10 (Normalized Discounted Cumulative Gain at rank 10)
- **Dispute resolution:** Human review for cases where automated score and reviewer judgment diverge
- **Validity gate:** Inter-rater agreement ≥80% required before a benchmark is considered valid

If this threshold is not met, re-evaluate the graph axis investment before expanding scope.

**Causal Chain Completeness:**
For 95%+ of EventStore events with known CausationId/CorrelationId chains, graph traversal returns the complete causal path. Validated by automated tests against known event chains.

**MVP Go/No-Go Gate:**

| Gate Type | Criterion | Requirement |
|---|---|---|
| **Hard gate** | Three-axis validation passes at 80% | Must pass |
| **Hard gate** | Zero cross-tenant data leaks | Must pass |
| **Hard gate** | Developer onboarding <30 minutes | Must pass |
| Soft gate | Causal chain completeness ≥95% | 2 of 3 must pass |
| Soft gate | MCP end-to-end integration works | 2 of 3 must pass |
| Soft gate | Case model correctly scopes memory | 2 of 3 must pass |

All 3 hard gates must pass. At least 2 of 3 soft gates must pass. If a hard gate fails, it's a blocker — no shipping until resolved.

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Proof of Thesis — validate three-axis retrieval before building integration surfaces. Ship the smallest thing that proves hybrid retrieval outperforms single-axis, with cases and multi-tenancy from day one (architectural decisions that can't be retrofitted).

**Resource Requirements:** Solo developer. Estimated 22-32 stories across 7 features (DAPR scaffolding absorbed into feature work, CLI trimmed to benchmark essentials).

**Implementation Sequencing:** Establish the complete foundation path before any ingestion, indexing, search, or graph story writes data: buildable scaffold/AppHost/ServiceDefaults first, minimum build/test feedback second for any greenfield or restarted implementation sequence, then tenant provisioning, minimal case bootstrap, and tenant/case validation guards. `TenantProvisioningWorkflow` owns physically isolated tenant infrastructure creation, minimal case bootstrap happens inside an active tenant, and ingestion/indexing fail before backend writes if tenant or case context is missing or mismatched. After that foundation exists, build each search axis independently and get it working before tackling fusion. The fusion algorithm (BM25 normalization + cosine + graph proximity weighting) is research-grade R&D — treat it as a dedicated spike, not interleaved with infrastructure plumbing.

### MVP Feature Set (Phase 1 — "Proof of Thesis")

**Core User Journeys Supported:**
- Journey 1 (Alex — Zero to First Search) — partial: CLI-only, no EventStore zero-code flow
- Journey 9 (Alex — The First Case) — full: empty state, ingestion, first search
- Journey 5 (Kenji — New Tenant) — partial: provisioning and isolation verification

**Must-Have Capabilities:**

| # | Feature | Stories (Est.) | Validates |
|---|---|---|---|
| 1 | Memory Engine (Redis: RediSearch + Vector + FalkorDB) | 5-8 | Three-axis foundation |
| 2 | Content Ingestion API (file/URL, metadata, confidence tracking, async actors) | 3-5 | Pipeline, dual-origin metadata |
| 3 | Three-Axis Search (syntactic, semantic, graph — independent first, then fusion spike) | 4-5 | Core hypothesis |
| 4 | Case/Folder Model (create/delete, strict ownership, case-scoped graph) | 3-4 | Collaborative memory structure |
| 5 | Tenant Isolation (physically separate indexes, enforced at all layers) | 3-4 | Enterprise requirement |
| 6 | CLI — benchmark essentials only: `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify` | 2-3 | Thesis validation tooling |
| 7 | Benchmark Suite (5-10 queries, automated NDCG@10 scoring) | 2-3 | Three-axis hypothesis validation |
| | **Total** | **22-32** | |

**Note:** DAPR infrastructure (actors, state management, sidecar configuration) is scaffolding built as part of features 1-5, not a separate work item. README ships with MVP but is documentation, not a story-estimated feature.

**Benchmark Validation Protocol:** See detailed scoring protocol in Success Criteria § Measurable Outcomes (primary: NDCG@10 with independent reviewers; fallback: automated A/B scoring if reviewers unavailable).

### Phase 1.5 — Fast-Follow (committed: within 4 weeks of thesis validation)

| # | Feature | Validates |
|---|---|---|
| 1 | EventStore / Hexalith Module Event Integration (DAPR pub/sub through the Memories Server sidecar, auto-discovery, dual embedding, causal chains) | Zero-code promise, <30 min onboarding |
| 2 | MCP Server (search, ingest, traverse, case-info with token-budget awareness) | LLM agent integration |
| 3 | CLI expansion: `explore`, `status`, `handlers`, `quickstart`, batch directory ingestion | Full developer experience |

The Memories Server is the sidecar-managed event subscriber. Hexalith modules publish CloudEvents to the configured DAPR pub/sub topic; the server sidecar delivers them to `/events/ingest`, where source-prefix routing maps events to tenant/case memory. Modules should not bypass this path with direct REST pushes for domain event streams.

**Hard commitment:** Phase 1.5 ships within 4 weeks of thesis validation. If this timeline can't be met, MCP Server moves back into the MVP to ensure the product is usable — not just validated — at launch.

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
- Fusion algorithm is the primary R&D risk. Mitigation: build axes independently, validate each works, then spike the fusion. If graph axis adds no value to general search, fall back to two-axis (syntactic + semantic) with graph reserved for causal chain traversal only.
- Three normalization problems (BM25 unbounded → 0-1, cosine native 0-1, graph proximity custom decay) must each be solved and documented before fusion weighting begins.

**Market Risks:**
- Thesis-only MVP is not adoptable — it's a validated prototype. Mitigation: hard 4-week fast-follow commitment. If slipping, pull MCP back into MVP.
- Independent reviewer availability for benchmark scoring. Mitigation: fallback automated A/B protocol + early community engagement.

**Resource Risks:**
- Solo developer, 22-32 stories. Absolute minimum if resources tighten further: Engine + Search + CLI (ingest/search) + Benchmarks — 4 features, ~13-18 stories. Cases and tenant isolation deferred to fast-follow alongside EventStore/MCP.

**Operational Risks:**
- Shared embedding API key exhaustion — one tenant's batch ingestion starves others' real-time ingestion. Mitigation: per-tenant pipeline actor enforces throttle ceiling. For full isolation, tenants use separate API keys. Document shared-key limitation in operator guide.

## User Journeys

### Journey 1: Alex — "Zero to First Search" (Success Path)

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

> **Scope note:** `memories handlers --list` and event replay are capabilities implied by this journey. They must be explicitly included in MVP Feature #3 (EventStore Integration) or this journey overpromises.

---

### Journey 3: Alex — "Wiring Up the AI Assistant" (MCP Integration)

Alex has the memory server running and CLI working. Now the product owner wants the team's AI assistant to use it.

**Opening Scene:** Alex opens the MCP tool documentation. Four tools: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`. Each has typed parameters with descriptions.

**Rising Action:** Alex adds the MCP tool definitions to the AI assistant configuration. First test: "What happened with claim 4821?" The assistant calls `search_memory(query="claim 4821", case="claims-q1", axes="hybrid")` and returns results with source attribution. It works — but the response is too long, blowing past the context window.

**Climax:** Alex adds `token_budget=2000` to the tool configuration. The assistant calls again — same query, but the response is now concise: top-ranked results, truncated by relevance, with a note "8 additional results omitted." The assistant composes a focused answer. Alex tests three more queries, each producing grounded, sourced responses.

**Resolution:** The product owner asks "why was claim 4821 denied?" and the assistant walks the causal chain: SubmitClaim → FraudCheckTriggered → FraudScoreExceeded → ClaimDenied. Sourced, attributed, traceable. The AI feature ships to the team by end of week.

**Capabilities revealed:** MCP tool definitions, token-budget-aware responses, multi-axis search control, source attribution, assistant configuration workflow.

---

### Journey 4: Marcus — "Brief the New Person"

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

**Opening Scene:** Kenji opens the CLI: `memories tenant create --id bu-compliance --display-name "Compliance Unit"`. The command provisions physically separate Redis indexes — RediSearch, vector, and FalkorDB graph — all namespaced and isolated. It takes 8 seconds.

**Rising Action:** Kenji runs the tenant isolation verification: `memories tenant verify --id bu-compliance`. Automated checks confirm: zero shared indexes with existing tenants, search from bu-compliance context returns zero results from other tenants, ingestion into bu-compliance is not visible from other tenant contexts. All green.

**Failure beat:** Next month, a new intern accidentally runs `memories search "test" --tenant bu-operations` from the bu-compliance service context. The CLI returns: `Error: Tenant mismatch. Authenticated as bu-compliance, cannot query bu-operations. Use 'memories tenant switch' to change context.` The isolation holds. Kenji sees the rejected request in the audit log with full details: who, when, what was attempted.

**Resolution:** Kenji's Thursday deadline was met in under 10 minutes. The monitoring dashboard shows all four tenants healthy, isolated, with clear resource consumption per tenant.

**Capabilities revealed:** CLI tenant provisioning, physical index isolation, automated isolation verification, boundary violation errors, audit logging.

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

**Climax:** First search on real data: `memories search "water damage" --case claims-pilot`. Three results, ranked by hybrid score. The system works. Alex publishes a test event to the DAPR topic — it appears in the case within seconds. The empty state is gone, and Alex never felt lost getting here.

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
| **Alex — Success Path** | EventStore auto-integration, CLI search + explain, causal traversal, <30 min onboarding |
| **Alex — Debug Path** | CLI diagnostics (status, explain, handlers list), error messages, event replay |
| **Alex — MCP Integration** | MCP tool definitions, token-budget responses, assistant configuration, source attribution |
| **Marcus — Onboarding** | Case member management, briefing via MCP, source verification, memory corrections |
| **Kenji — MVP Operations** | Tenant provisioning, physical isolation verification, boundary violation errors, audit logging |
| **Kenji — Growth Operations** | Backend assessment, migration dry-run, Redis → Qdrant swap *(Phase 3)* |
| **LLM Agent — Integration** | MCP tools, token-budget, multi-axis control, confidence scoring, degradation signaling |
| **Priya — End User** | REST API (via app), narrative composition, source verification links, confidence scores |
| **Alex — Empty State** | Helpful empty messages, quickstart, batch ingestion, case creation, progress feedback |
| **Contributor — First PR** | Build experience, CI, code organization, maintainer responsiveness *(infrastructure)* |

**Coverage check:**
- Primary user success path: Journey 1 (Alex CLI onboarding)
- Primary user MCP integration: Journey 3 (Alex MCP wiring)
- Primary user edge case / debug: Journey 2 (Alex debug)
- Primary user empty state: Journey 9 (Alex first case)
- Management / onboarding: Journey 4 (Marcus)
- Operations MVP: Journey 5 (Kenji provision + verify)
- Operations growth: Journey 6 (Kenji scale) *(Phase 3)*
- API / integration: Journey 7 (LLM Agent)
- End beneficiary: Journey 8 (Priya)
- Ecosystem / community: Journey 10 (Contributor)
- Graceful degradation: Covered in Journey 7 (LLM Agent edge cases)
- Verification / trust: Covered in Journey 4 (Marcus) and Journey 8 (Priya)

## Domain-Specific Requirements

### Compliance Boundary

Hexalith.Memories is **interpretive infrastructure** — it occupies a middle ground between raw storage and application logic. It doesn't make decisions, but it *does* make interpretations (embeddings, causal chains, confidence scores). This framing is more honest and defensible than "just infrastructure."

**Three-tier responsibility model:**

| Layer | Responsible for | Example |
|---|---|---|
| **Storage** | Data durability, isolation, encryption at rest | Redis, FalkorDB |
| **Interpretation (Memories)** | Accurate embeddings, correct causal chains, calibrated confidence, complete edge graphs | Confidence score of 0.8 must reflect actual ~80% reliability |
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

**Memory unit provenance:** Every memory unit tracks `ingested_by` (user or system identity that created it) as a mandatory MVP field. This enables:
- Case owners to review recent ingestion activity (`memories case activity --case X`)
- Insider threat detection (anomalous ingestion patterns)
- Data lineage for compliance and trust

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

**Confidence calibration (Growth phase):** Periodic review of confidence tier accuracy against actual relevance judgments. Do 0.8-confidence edges reflect ~80% actual accuracy? Feedback loop to validate and adjust default tiers.

**Responsibility boundary:** Memories owns data accuracy (correct ordering, complete chains, accurate edge types, gap detection). The LLM owns narrative quality (prose composition, summarization). If the structured data has wrong ordering, missing links, or silent gaps, that's a Memories bug. If the prose misrepresents correct structured data, that's an LLM problem.

### Open-Source Licensing

**Hexalith.Memories license: Apache 2.0** (recommended)

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
No existing system ships syntactic (BM25), semantic (vector), and graph traversal fused in a single query with a documented, deterministic fusion algorithm. Existing tools offer one or two axes — Elasticsearch (syntactic), Pinecone/Qdrant (semantic), Neo4j (graph) — but integration requires custom glue code. Hexalith.Memories makes the fusion algorithm a first-class, explainable product feature where users can see *why* each result appeared and which axis contributed.

**2. Zero-Code Event Memory via DAPR Pub/Sub (Platform Innovation)**
Any event-sourced system that publishes events to a DAPR-compatible bus gets automatic memory integration: events are indexed with dual embeddings (payload + natural language description), and CausationId/CorrelationId metadata becomes graph edges — without mapping code, without configuration beyond a subscription. This is not locked to Hexalith.EventStore; it works with Marten, Wolverine, Axon, or any framework that publishes to DAPR topics. The innovation is the *pattern*: subscribe to a bus, auto-discover event types, and transform an opaque event stream into a queryable knowledge graph.

**3. Causal Intelligence as a Query Interface (Domain Innovation)**
Event-sourced systems already capture *why* things happen — but that causal data is locked inside infrastructure, queryable only by developers who know the event store schema. Memories makes causal chains queryable via natural language: "What led to this decision?" walks the CausationId graph and returns structured, ordered, gap-aware results. This transforms event sourcing from a persistence pattern into a knowledge management pattern.

**4. Interpretive Infrastructure (Positioning Innovation)**
The three-tier responsibility model — Storage → Interpretation → Application — is a novel positioning for AI infrastructure. Memories is not "just a database" (it interprets content) and not "an AI application" (it doesn't make decisions). This framing creates a defensible product category and a clear responsibility boundary.

### Market Context & Competitive Landscape

| Competitor | Axes | Team Memory | Causal Intelligence | DAPR Integration |
|---|---|---|---|---|
| Mem0 | Semantic | No | No | No |
| Zep | Semantic + metadata | No | No | No |
| LangChain Memory | Semantic | No | No | No |
| Elasticsearch | Syntactic | No | No | No |
| Qdrant + custom glue | Semantic + custom | Custom build | Custom build | Custom build |
| **Hexalith.Memories** | **Syntactic + Semantic + Graph** | **Yes (case model)** | **Yes (auto-discovered)** | **Native** |

The primary competitive moat is the *integration depth* — three axes + case model + causal intelligence + DAPR-native. Replicating any single feature is straightforward; replicating the integrated system is a multi-month engineering effort that most competitors won't prioritize because their architectures weren't designed for it.

**Addressable market expansion:** The zero-code integration works with *any* DAPR pub/sub source, not just Hexalith.EventStore. This means the addressable market is every .NET team using DAPR for event-driven architecture — significantly larger than the EventStore user base alone. EventStore integration is the *best* experience (CausationId/CorrelationId chains), but generic DAPR events still get syntactic + semantic indexing with correlation metadata.

### Validation Approach

| Innovation | Validation Method | Kill Switch |
|---|---|---|
| Three-axis fusion | Benchmark suite: 5–10 queries scored by NDCG@10. Three-axis vs single-axis. | If hybrid doesn't outperform single-axis on 80%+ of benchmarks, re-evaluate graph axis |
| Zero-code event memory | Timed onboarding test: `dotnet add package` to first search result | If onboarding exceeds 30 minutes, the "zero-code" promise is broken |
| Causal intelligence | Causal chain completeness test: 95%+ of known CausationId chains fully traversable | If chains are incomplete, the "why did this happen" story can't be told |
| DAPR-generic pattern | Test with non-EventStore event source (e.g., Marten publishing to DAPR) | If integration requires custom code beyond DAPR subscription config, the pattern isn't generic |

### Risk Mitigation

**If three-axis fusion doesn't validate (graph axis adds no value):**
Fallback to two-axis system (syntactic + semantic) with case-scoped metadata enrichment. The product remains differentiated by team-scoped collaborative memory, zero-code event integration, and the case model — none of which depend on the graph axis. Reposition from "three-axis retrieval" to "team memory with intelligent search." The graph infrastructure stays in place for causal chain traversal (its highest-value use case) even if it doesn't improve general search relevance.

**If zero-code integration requires custom code for non-EventStore sources:**
Accept that EventStore gets the premium experience and other frameworks get a "low-code" experience with a thin adapter layer. Document the adapter pattern. The innovation claim narrows from "zero-code for any DAPR source" to "zero-code for EventStore, minimal-code for others."

**If causal chains are incomplete:**
Missing nodes are already handled by gap detection (`[MISSING: event-id]`). If completeness falls below 95%, investigate: is it an ingestion latency issue (events not yet indexed) or a structural issue (CausationId metadata not propagated)? Latency is fixable; structural gaps require working with the event source framework.

## Developer Tool / API Backend Specific Requirements

### Project-Type Overview

Hexalith.Memories is a hybrid Developer Tool + API Backend delivered as NuGet packages with a DAPR-native service architecture orchestrated by .NET Aspire. Internal services communicate via DAPR service invocation; external consumers (CLI, LLM agents, third-party apps) connect through a REST API behind infrastructure-managed ingress. The system reuses shared infrastructure from the Hexalith ecosystem via root-declared git submodules under `references/` (`references/Hexalith.Commons` for error handling, `references/Hexalith.EventStore` for versioning conventions).

### Technical Architecture Considerations

**Language & Platform Matrix:**

| Aspect | MVP | Future |
|---|---|---|
| Server runtime | .NET 10 / C# 13 | .NET only (DAPR handles polyglot) |
| Client libraries | MVP CLI uses a minimal direct HTTP/ingress adapter inside the CLI; reusable `.NET` client packages are not MVP blockers | `.NET` (`Client` for DAPR consumers, `Client.Rest` for external consumers), Python, TypeScript clients targeting ingress REST API |
| CLI | .NET global tool (`dotnet tool install -g Hexalith.Memories.Cli`) | Same |
| Cross-language access | Via ingress REST API (any HTTP client) or DAPR service invocation (any DAPR SDK) | Dedicated language-specific client packages |
| IDE tooling | None | VS/Rider templates, analyzers (deferred) |

**Package Distribution:**

Current release inventory: 9 published NuGet packages + 3 non-packable service/orchestration projects. `tools/release-packages.json` is the authoritative package source of truth for release tooling.

| Package | Purpose | Dependencies |
|---|---|---|
| `Hexalith.Memories.Contracts` | Domain types, memory unit model, envelopes | Hexalith.Commons |
| `Hexalith.Memories.Client.Rest` | Phase 1.5 typed HTTP client for external consumers via ingress REST API, with resilience (retry, circuit breaker) | Contracts, HttpClient, `Microsoft.Extensions.Http.Resilience` |
| `Hexalith.Memories.Server` | DAPR service, actors, ingestion pipeline, REST controllers for ingress (non-packable) | Contracts, EventStore, ServiceDefaults, Telemetry, and direct Redis/FalkorDB client packages |
| `Hexalith.Memories.Redis` | Compatibility-only Redis/FalkorDB API retained for existing package consumers | NFalkorDB, NRedisStack, StackExchange.Redis |
| `Hexalith.Memories.Cli` | CLI tool (dotnet global tool). MVP readiness did not require the reusable REST client, but the current package may use `Client.Rest` after Phase 1.5 extraction. | Client.Rest, Contracts, Telemetry |
| `Hexalith.Memories.Mcp` | MCP server (DAPR service with sidecar) | Client.Rest, Contracts, ServiceDefaults, Telemetry |
| `Hexalith.Memories.Aspire` | Reusable Aspire resource-model integration for consuming AppHosts | Aspire hosting and DAPR integration packages |
| `Hexalith.Memories.EventStore` | Auto-registration, dual embedding, causal chain indexing | Contracts, DAPR, Hexalith.EventStore |
| `Hexalith.Memories.Telemetry` | Shared telemetry constants, collectors, and test-support abstractions | OpenTelemetry |
| `Hexalith.Memories.AppHost` | .NET Aspire orchestration (internal project, not published) | Server, Redis, Aspire |
| `Hexalith.Memories.ServiceDefaults` | Shared packaged service defaults — telemetry, health checks, discovery, and resilience | Contracts, Telemetry, OpenTelemetry, Microsoft.Extensions hosting packages |

**Package dependency design principle:** `Server` declares the backend client packages it directly uses and does not depend on the compatibility-only `Hexalith.Memories.Redis` package. Backend implementations remain registered at the composition root, preserving the future extraction path for `IMemoryIndex`/`IMemoryGraph` interfaces without using a placeholder package as a transitive dependency facade.

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
| Internal (Server ↔ Redis/FalkorDB) | DAPR state / direct connection via DAPR sidecar |
| Internal (EventStore → Server) | CloudEvents via DAPR pub/sub |
| External → Internal (CLI, LLM agents, third-party) | REST API via infrastructure ingress |
| Serialization | JSON exclusively |
| Authentication | DAPR API token (internal); ingress-level auth (external) |
| Tenant context | Passed as parameter in payloads, validated by server |
| Rate limiting | Deferred to infrastructure (ingress, DAPR middleware) |
| Per-user identity | Not in MVP — tenant-level isolation sufficient |

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
| `apiKey` | Provider API key | .NET User Secrets (local dev), DAPR Secrets API (deployed) |
| `rateLimitPerMinute` | Throttle ceiling for ingestion pipeline actor | Tenant config |

**Critical constraints:**
- Redis Vector Search index schema is fixed at creation — **switching embedding providers requires full reindex of that tenant's data**. This is a migration operation, not a configuration change. Must be documented in operator guide.
- Shared API keys mean shared rate limits across tenants. For rate limit isolation, tenants should use separate API keys. The pipeline actor enforces per-tenant throttle ceilings, but the actual provider API ceiling is the shared bottleneck.

### Async Ingestion Pipeline

Ingestion uses a **per-tenant pipeline actor** managing a bounded queue. The pipeline actor owns throttling (embedding API rate limits), ordering, and progress tracking.

**Pipeline Stages:**

| Stage | What happens | Actor responsibility |
|---|---|---|
| `queued` | Content received, waiting for processing slot | Pipeline actor queues, respects backpressure |
| `extracting` | Text extraction from content (PDF, URL, file) | Stateless work dispatched by pipeline actor |
| `embedding` | Call embedding provider API, get vector | Throttled by per-tenant rate limit config |
| `indexing` | Write to RediSearch (syntactic), Redis Vector (semantic), FalkorDB (graph) | Atomic write across all three backends |
| `indexed` | Successfully searchable across all axes | Terminal success state |
| `failed` | Error at any stage, max retries exceeded | Dead letter state, visible via CLI |

**Failure handling:**
- Failed units retry with exponential backoff (configurable max retries)
- After max retries, units move to `failed` state with error details preserved
- `memories status --case X` shows: "47 indexed, 12 embedding (retrying), 141 queued, 3 failed"
- `memories status --failed` shows failed units with error details and stage where failure occurred

**Actor model:**
- **Per-tenant pipeline actor:** Manages ingestion queue, enforces rate limits, dispatches work. DAPR actor with state persistence — survives process restarts.
- **Document processing:** Stateless work items dispatched by pipeline actor. No per-document actors — avoids thousands of concurrent actor activations during batch ingest.

### Interface Capability Parity Matrix

Not all capabilities map to all interfaces. CLI is the superset for operational and diagnostic work, but implementation is split by phase: MVP CLI essentials are `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, and benchmark support; Phase 1.5 expands CLI polish with `explore`, `status`, `handlers`, `quickstart`, batch directory ingestion, and richer diagnostics.

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

**Design rationale:** MCP exposes what LLM agents need (search, ingest, traverse, case info). Tenant management, diagnostics, and interactive features are operational concerns handled via CLI. DAPR service invocation is the internal programmatic API.

### CLI Specification

**Distribution:** .NET global tool (`dotnet tool install -g Hexalith.Memories.Cli`)

**MVP command scope:** `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, and benchmark support.

**Phase 1.5 expansion scope:** `explore`, `status`, `handlers`, `quickstart`, batch directory ingestion, and richer diagnostics.

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
4. DAPR Secrets API (deployed environments — for sensitive values: API tokens, embedding provider keys)
5. .NET User Secrets (local development — for sensitive values)
6. DAPR configuration (sidecar discovery, app-id)

### Developer Experience & Documentation

**In-Repo Examples (`samples/` folder):**

| Example | Maps to | Demonstrates |
|---|---|---|
| `samples/01-quickstart/` | Journey 1 (Alex success path) | `dotnet run --project AppHost` boots full stack, ingest + search via CLI |
| `samples/02-eventstore-integration/` | Zero-code promise | Aspire AppHost with EventStore + Memories, DAPR subscription auto-wired |
| `samples/03-mcp-agent/` | Journey 3 (Alex MCP) | MCP server launched by Aspire, agent configuration |

Numbered naming signals the learning path and mirrors user journey progression.

**Documentation Strategy:**

| Artifact | Scope |
|---|---|
| README | 30-second demo, getting started guide, architecture overview |
| CLI `--help` | Built-in documentation with examples per command |
| Getting started guide | `dotnet add package` to first search result in <30 min |
| API reference | Auto-generated from `Contracts` XML docs |
| Compliance enablement guide | Building compliant apps on Memories |
| Operator guide | Tenant management, embedding provider migration (reindex), scaling |

No dedicated migration guide — the getting started guide covers the path from duct-tape solutions naturally.

### Test Infrastructure Strategy

| Test Layer | Approach | What It Validates |
|---|---|---|
| **Unit tests** | Mock `DaprClient` — no sidecar required | Business logic, domain model, fusion algorithm, score normalization |
| **Integration tests** | Aspire `DistributedApplicationTestingBuilder` or DAPR testcontainers | End-to-end ingestion pipeline, search across all axes, tenant isolation, actor lifecycle, index/graph consistency |
| **Contract tests** | Serialization round-trip tests | CloudEvent payloads, service invocation contracts, REST API contracts, error envelopes |

Contributors can run unit tests without Docker. Integration tests require Docker (documented in CONTRIBUTING.md). CI runs all layers.

### Implementation Considerations

**Git Submodule Dependencies:**
- `references/Hexalith.Commons` — Error handling, shared utilities, base types
- `references/Hexalith.EventStore` — Event types, versioning conventions, DAPR integration patterns

**DAPR + Aspire Orchestration:**
- No standalone server deployment — .NET Aspire AppHost orchestrates all services with DAPR sidecars
- Local development: `dotnet run --project Hexalith.Memories.AppHost` launches Server, MCP Server, Redis, FalkorDB, and all DAPR sidecars
- CI/CD: Aspire-based test infrastructure or DAPR testcontainers
- Production: Aspire manifest export for container orchestrator deployment

**Cross-Language Future Path:**
Non-.NET external consumers can integrate today via ingress REST API (JSON payloads). Non-.NET internal services can integrate via DAPR service invocation (any DAPR SDK). Dedicated Python/TypeScript client packages are a future convenience layer.

## Functional Requirements

### Knowledge Ingestion

- **FR1:** Developer can ingest content from local files into a specified case
- **FR2:** Developer can ingest content from URLs into a specified case
- **FR3:** Developer can batch-ingest content from a directory into a specified case
- **FR4:** System can extract text from ingested content (plain text, PDF, markdown)
- **FR5:** System can generate embeddings for ingested content via a configurable embedding provider
- **FR6:** System ensures a memory unit is fully searchable across all axes after ingestion completes
- **FR7:** Developer can attach metadata to ingested content, with each field tracking its origin (human-declared vs AI-inferred) and confidence score
- **FR8:** System manages ingestion load per tenant independently
- **FR9:** System retries failed ingestion automatically with configurable limits
- **FR10:** Developer can view ingestion status per case (queued, embedding, indexed, failed counts)
- **FR11:** Developer can view failed ingestion units with error details and failure stage
- **FR12:** Developer can manually trigger re-ingestion of failed or previously ingested content, individually or in bulk
- **FR13:** System handles partial backend write failures with defined recovery behavior (rollback or retry to achieve consistency across all axes)

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
- **FR23:** LLM Agent can constrain search response size by token budget
- **FR24:** System returns the origin identifier (file path, URL, or event ID) and origin type for each search result
- **FR25:** Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output

### Memory Organization

- **FR26:** Developer can create a case within a tenant
- **FR27:** Developer can delete a case and all its memory units
- **FR28:** Developer can add members to a case
- **FR29:** Developer can remove members from a case
- **FR30:** Developer can list cases within a tenant
- **FR31:** Developer can view case status including memory unit count, last activity timestamp, and health indicators
- **FR32:** System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion
- **FR33:** System maintains case-scoped graph edges between memory units within a case
- **FR34:** Developer can search across all cases within a tenant by keyword, returning results with case attribution
- **FR35:** Developer can delete an individual memory unit from a case
- **FR36:** Developer can view recent activity within a case (ingestion events, searches, membership changes)
- **FR37:** Developer can annotate or correct a memory unit, with annotations tracked as linked memory units

### Tenant Management

- **FR38:** Operator can create a tenant with physically separate indexes
- **FR39:** Operator can delete a tenant and all its indexes, graph data, and memory units
- **FR40:** Operator can verify tenant isolation via automated checks
- **FR41:** Operator can list tenants
- **FR42:** Operator can update tenant configuration after creation (rate limits, display name, settings)
- **FR43:** System prevents configuration changes that would create data inconsistency without explicit operator acknowledgment
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

- **FR53:** Developer can interact with all retrieval and ingestion capabilities via CLI
- **FR54:** Developer can interact with search, ingestion, traversal, and case-info capabilities via MCP tools
- **FR55:** CLI supports multiple output formats: human-readable (default), JSON, and table
- **FR56:** CLI provides actionable error messages with recovery suggestions for common failure modes
- **FR57:** Developer can discover available actions from any system state, including empty states and error conditions
- **FR58:** MCP tools include typed parameter schemas with descriptions for LLM agent consumption

### EventStore Integration

- **FR59:** System can auto-discover event types published to DAPR pub/sub topics
- **FR60:** System can generate dual embeddings for events (raw payload + natural language description)
- **FR61:** System can automatically index CausationId/CorrelationId metadata as graph edges without developer mapping code
- **FR62:** Developer can list registered event handlers and detect handler registration mismatches

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

- **FR71:** Developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format. **Phase:** Phase 2 unless a later sprint change explicitly pulls export into MVP.
- **FR72:** System exposes readiness and liveness health checks verifying all backends
- **FR73:** Operator can detect index/graph divergence via consistency check
- **FR74:** Operator can repair detected index/graph inconsistencies via consistency repair operation

## Non-Functional Requirements

*NFRs are tagged by validation phase: **[MVP]** = must verify before thesis validation, **[P1.5]** = verify when EventStore + MCP ship, **[Ongoing]** = validate as infrastructure matures.*

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
| **NFR9** | Embedding provider API keys stored in secure secret management (.NET User Secrets for local dev, DAPR Secrets API for deployed) — never in config files or environment variables in production | Code review + secret scanning in CI | Ongoing |
| **NFR10** | All inter-service communication authenticated via DAPR API tokens | DAPR configuration validation | Ongoing |
| **NFR11** | External access authenticated at ingress layer — no unauthenticated access to REST API endpoints | Integration test with unauthenticated requests | P1.5 |

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
| **NFR17** | Ingestion pipeline state survives process restarts — queued and in-progress units resume without data loss | DAPR actor state persistence verified | MVP |
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
| **NFR31** | README includes working quickstart that completes in <30 minutes on a clean machine with Docker installed | Timed walkthrough on clean environment | MVP |
