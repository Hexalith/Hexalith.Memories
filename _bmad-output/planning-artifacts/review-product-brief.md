# Product-Brief Reconciliation — Hexalith.Memories

**Input:** `_bmad-output/planning-artifacts/product-brief-Hexalith.Memories-2026-03-22.md`  
**Against:** `_bmad-output/planning-artifacts/prd.md` (frontmatter `inputDocuments` lists only that brief)  
**Addendum:** none  
**Stance:** source-extract only. This is not a rubric walk and not an architecture-drift review. Expansions are flagged as expansions, not automatically as defects.

## Verdict

The PRD keeps the brief’s product *identity* — three-axis retrieval that answers “why,” case-scoped team memory, physical tenant isolation, EventStore causality, Redis-first with a Qdrant escape hatch — and it keeps the named people (Alex, the LLM agent, Marcus, Kenji, Priya). It does not keep the brief’s *contract for v1*. MCP and EventStore zero-code, which the brief put in Phase 1 as beachhead-critical, become a post-thesis fast-follow, while leftover brief sentences (`dotnet add package`, “every feature is accessible through both MCP and CLI”) still read as MVP gates. The FR list then drops the brand line, the ingest-from-anywhere vision, and the jobs-to-be-done table, and it absorbs a large amount of implementation the brief never authorized (Aspire-mandatory topology, generic DAPR/Marten/Axon zero-code, compliance program, fusion algorithm as product law). Without an addendum, those expansions sit in the product contract.

## Kept (earned)

- **Three-axis thesis + kill switch** → Brief Core Vision / Three-Axis Validation Metrics: “Does syntactic + semantic + graph produce noticeably better results than semantic alone?” and “Three-axis outperforms single-axis on 80%+ of benchmark queries.” → PRD Executive Summary and Measurable Outcomes: “if hybrid retrieval doesn't outperform single-axis on 80%+ of benchmark queries … the product direction must be re-evaluated”; “80% is the hard line, not a stretch goal.”
- **Problem: amnesia, scatter, invisible relationships** → Brief Problem Statement / Impact. → PRD Executive Summary: “Your LLM agent forgets everything between sessions. Your team's knowledge is scattered across cloud drives, chat logs, and event stores. When someone asks ‘why did this happen?’ — no search tool can answer.”
- **Case/folder collaborative memory + one-memory-one-case** → Brief Moat 1 and trade-off “One memory, one case.” → PRD What Makes This Special and FR32: “System enforces strict single-case ownership per memory unit — reassignment requires deletion and re-ingestion.”
- **Physical tenant isolation, zero leaks** → Brief Proposed Solution #5, Tenant Isolation Test Matrix, Technical Quality Metrics. → PRD FR38–FR44, NFR8, Kenji Journey 5, hard go/no-go gate: “Zero cross-tenant data leaks.”
- **Causal intelligence via CausationId/CorrelationId + dual embeddings** → Brief Moat 2 / Hero “Zero-Code Event Indexing.” → PRD Executive Summary, Causal Intelligence FR46–FR52, EventStore FR59–FR61 (as Phase 1.5, but the capability is specified).
- **Hard onboarding number** → Brief Alex: “Under 30 minutes from `dotnet add package` to first search result.” → PRD Success Criteria and NFR31 keep the same number (see Dropped: the *path* the number measures is not the same).
- **Redis start, Qdrant later, concrete-first** → Brief trade-off table + “Start concrete, abstract later.” → PRD NFR15: “Architecture must not preclude backend migration (Redis → Qdrant) — concrete implementation with clear extraction points identified, no premature interfaces”; Qdrant in Phase 3.
- **Human vs AI metadata confidence** → Brief design principles: “Confidence-tracked metadata (human-declared vs AI-inferred).” → PRD FR7, FR64.
- **Async per-tenant ingestion** → Brief design principles: “Async ingestion via actors.” → PRD Async Ingestion Pipeline + FR8–FR13.
- **Debug-first `--explain`** → Brief Alex Trust Building: “`memories search "payment" --explain` shows why each result was returned.” → PRD FR19, Journey 1 Trust Deepening, MVP CLI includes `search --explain`.
- **Hero features briefing + diffing as later work** → Brief Out of Scope: memory diffing and (implicitly) briefing in Phase 2. → PRD Phase 2: “Memory diffing,” “Onboarding briefing (‘brief me on this case’).”
- **Named personas carried into journeys and success table** → Brief Target Users (Alex, LLM Agent, Marcus, Kenji, Priya). → PRD User Success table + Journeys 1–9.
- **Adoption numbers** → Brief 3/12-month GitHub, NuGet, contributors, EventStore users, MCP listing. → PRD Business Success table keeps the same aspirational numbers and adds concern thresholds.
- **Latency / freshness / durability bars** → Brief Technical Quality Metrics. → PRD NFR1–NFR4, NFR6, NFR16 match the brief’s p95 and AOF targets (PRD adds concurrency and 10K-unit conditions).
- **Open-source + DAPR-native + CLI empty-state craft** → Brief Documentation as Product; Day-2 CLI. → PRD license stance, DAPR throughout, FR56–FR57, Journey 9 empty-state copy.
- **Integrated-system-over-duct-tape moat** → Brief Moat 3. → PRD What Makes This Special, almost verbatim: “Replicating this with separate tools requires custom glue code that collapses under enterprise requirements.”
- **Graph-axis fallback if thesis fails** → Brief R1: “If graph axis fails, pivot to syntactic+semantic with case-scoped metadata.” → PRD Innovation Risk Mitigation: fallback to two-axis, graph reserved for causal traversal.

## Dropped or weakened

### critical EventStore zero-code and MCP are no longer MVP, but the beachhead contract still talks as if they are

- **Brief:** Phase 1 Core Features #3 EventStore Integration and #7 MCP Server. First-week Day 5: “Publish event to DAPR topic → appears in memory.” Design principle: “Full feature parity on MCP + CLI.” Alex JTBD: “Help me add AI features to my EventStore app without building memory infrastructure” with hiring criteria “zero-code integration.” Why Now: “MCP is the standard for LLM tool integration.” MVP success includes “MCP integration works” and “Causal chain works.” Go/no-go: 5 of 6 criteria.
- **PRD:** MVP Feature Set is Engine, Ingestion, Three-Axis Search, Case/Folder, Tenant Isolation, trimmed CLI, Benchmarks. “Journey 1 … partial: CLI-only, no EventStore zero-code flow.” Phase 1.5 (within 4 weeks of thesis validation): EventStore + MCP + CLI expansion. Soft gates: causal-chain completeness and “MCP end-to-end integration works” — even though neither ships in the MVP that the gate is supposed to judge. Executive Summary still says “under 30 minutes from `dotnet add package` to first search result” and “Every feature is accessible through both MCP … and CLI,” then immediately: “The MVP validates the three-axis thesis via CLI; MCP ships as a fast-follow.”
- **Note:** This is the largest fidelity break. The brief’s v1 *is* a memory server LLM agents can call and EventStore developers can drop in. The PRD’s v1 is a CLI-operated retrieval prototype. A 4-week fast-follow is a real mitigation, but it is a different product contract: launch is now “thesis validated,” not “Alex shipped the AI feature.” Keeping Journey 1 as the success path and keeping `dotnet add package` as a hard MVP gate launders the brief’s beachhead into a phase that explicitly excludes it.
- **Fix:** Either put EventStore auto-index + MCP back in Phase 1 (brief-faithful), or rewrite the MVP gates, Journey 1, Executive Summary, and Alex JTBD so they describe file/URL CLI proof-of-thesis only — and state that the beachhead aha is a Phase 1.5 launch gate, not an MVP gate. Do not leave both stories in force.

### critical “Full feature parity on MCP + CLI” is contradicted by the parity matrix

- **Brief:** “Every feature is accessible through both MCP (for LLM agents) and CLI.” Ecosystem Health: “CLI completeness: 100% of features accessible via CLI” and “MCP completeness: 100% of features accessible via MCP tools.” Phase 1 CLI includes `explore --case`, `tenant`, `case`, ingest, search, `--explain`.
- **PRD:** Executive Summary repeats “Every feature is accessible through both MCP and CLI.” Interface Capability Parity Matrix then: “Not all capabilities map to all interfaces.” MCP gets search/ingest/traverse/case-info only. Tenant management, verify, status, explore, handlers, quickstart are CLI-only (and several of those are Phase 1.5). MVP CLI is further cut to “benchmark essentials.” FR53 still says “all retrieval and ingestion capabilities via CLI”; FR54 limits MCP.
- **Note:** Using this PRD as the contract, an implementer will ship a CLI-superset / MCP-subset and call it done — the opposite of the brief’s MCP-first, dual-surface promise. The leftover exec-summary sentence makes the contradiction invisible to a skim reader.
- **Fix:** Delete or qualify the exec-summary parity sentence. If the product is “MCP for agent work, CLI for ops,” say that as a decided principle and list which brief features are intentionally MCP-absent. If parity remains the principle, the matrix is the defect — restore MCP coverage (or a dated plan that reaches 100%).

### high Ingest-from-anywhere (cloud, git, images, video) vanishes from the roadmap

- **Brief:** Executive Summary: “Ingest documents from anywhere (cloud drives, git, local files, URLs, images, video).” Proposed Solution #3: same list “via pluggable embedding providers.” MVP ingestion is already narrower (URL/file), so the vision was future-facing — but it was still a promised destination.
- **PRD:** Problem copy still mentions “cloud drives.” FRs: local files, URLs, directory, text from “plain text, PDF, markdown.” Phase 2–3 lists have no cloud-drive connectors, git ingestion, image, or video. Competitive/docs never restore them.
- **Note:** The dual story “For any team / For EventStore users” depended on anywhere-ingest for the non-EventStore half. After EventStore left MVP, this drop leaves “any team” with files and URLs only — a different product than the brief sold.
- **Fix:** Add an explicit later-phase ingestion roadmap (cloud drives, git, images, video) or an explicit deferral with owner and revisit condition. Do not leave “cloud drives” only as problem-statement atmosphere.

### high Thirty-minute gate silently changed meaning

- **Brief:** Clock starts at `dotnet add package Hexalith.Memories.Client`, includes DAPR subscription, ends at first search on auto-indexed events. “If it takes longer, Alex bounces.” Validation Approach in the brief is this path.
- **PRD:** Same words in User Success and in Innovation Validation (“Timed onboarding test: `dotnet add package` to first search result”). MVP coverage: Journey 1 is not in MVP; Journey 9 (create case, ingest files, search) is. NFR31 times a README quickstart “on a clean machine with Docker,” not the NuGet+subscription path.
- **Note:** Three different clocks share one slogan. Stakeholders will think the brief’s hard criterion survived. It did not.
- **Fix:** Split gates: (MVP) timed CLI ingest→search; (P1.5) timed `dotnet add package`→first event search. Stop using one sentence for both.

### high Unauthorized “absolute minimum” cut drops cases and tenant isolation

- **Brief:** Case model is Moat 1; physical isolation is Proposed Solution #5 and a named MVP feature. Neither is optional. R2/R7 allow delaying *interfaces*, not the case or tenant capabilities.
- **PRD:** Resource Risks: “Absolute minimum if resources tighten further: Engine + Search + CLI (ingest/search) + Benchmarks — 4 features, ~13-18 stories. Cases and tenant isolation deferred to fast-follow alongside EventStore/MCP.”
- **Note:** That sentence authorizes a product that cannot keep either remaining moat. It is the most dangerous new discretion in the PRD.
- **Fix:** Strike the cases/tenancy deferral. If a further cut exists, name only CLI polish or benchmark automation — not the isolation or case model the brief said cannot be retrofitted (the PRD’s own MVP philosophy already says this).

### high Jobs-to-be-done, Marcus’s hiring jobs, and non-users are not contracted

- **Brief:** JTBD table with hiring criteria per persona. Marcus: “Onboarding speed, knowledge visibility, decay detection”; key interactions include “cross-case insight discovery” and “knowledge decay alerts.” Priya “hires Alex's application, not Hexalith.Memories directly.” No explicit non-users section; implied non-users are direct end-users of Memories (Priya) and, for beachhead sequencing, teams outside .NET/DAPR.
- **PRD:** No JTBD table, no non-users. Personas appear as success rows and journeys. Marcus success is “brief me” in 5 minutes (a Phase 2 capability) and growing active cases. Decay detection is only a Phase 3 bullet. FR34 is tenant-wide keyword search, not insight discovery. Priya has Journey 8 (REST, Phase 2) and no success metric (brief had “Search success rate in applications built on Memories”).
- **Note:** Downstream epic/UX work cannot source-extract *why Alex hires this instead of Qdrant* or *what Marcus is monitoring*. The FR list encodes nouns (case, tenant, search), not jobs.
- **Fix:** Restore a short JTBD + non-users block (even if personas stay inline in journeys). Give Marcus decay/cross-case insight an explicit phase. Restore Priya as a lagged success metric on Alex’s app, or say she is narrative-only until REST ships.

### high Competitive “Why Now,” knowledge-base rivals, and the 12–18 month window are gone

- **Brief:** Why Now: MCP standard, team AI needs shared memory, enterprise isolation. Competitive table includes Mem0/Zep/LangChain, Elasticsearch/OpenSearch, Pinecone/Weaviate/Qdrant, Notion/Confluence, and duct-tape. R5: competitors adding tenancy/collaboration; “Estimated defensibility window: 12-18 months.” R6: EventStore market is small; standalone case+three-axis is the mitigation; EventStore is “superpower, not prerequisite.”
- **PRD:** Competitive table is Mem0, Zep, LangChain Memory, Elasticsearch, Qdrant+glue. Notion/Confluence, Weaviate, OpenSearch drop out (Pinecone survives only as a one-line axis example). No Why Now. No 12–18 month window. Market risk is “thesis-only MVP is not adoptable” and reviewer availability. Addressable-market mitigation *changes*: generic DAPR / Marten / Wolverine / Axon (see Expansions).
- **Note:** Positioning for a knowledge-layer vs a wiki, and the urgency that justified shipping MCP in Phase 1, are no longer in the contract. Speed-to-market as the R5 mitigation is inverted by a thesis-only MVP.
- **Fix:** Re-home Why Now, the wiki competitors, and R5/R6 in a short Market Context that architecture/UX can quote. If generic-DAPR is the new R6 mitigation, say it replaces the brief’s standalone-search story — don’t leave both implied.

### high Extraction phrases dropped; README-as-product and explore-as-trust-building weakened

- **Brief:** MVP ingestion includes “extraction phrases”; templates only are Phase 2. README is “a product deliverable, not an afterthought,” written “as carefully as the code,” with a “30-second zero-code demo.” CLI help “should be as good as a docs page.” Error messages are onboarding. Trust Building includes `memories explore --case` for interactive graph browsing. First-week demo: event auto-indexed, searchable on all three axes via CLI.
- **PRD:** Phase 2 keeps “Extraction phrase templates”; no FR for phrases themselves. README “ships with MVP but is documentation, not a story-estimated feature.” Docs strategy still lists a 30-second demo. NFR30–31 test `--help` examples and a 30-minute quickstart, not README craft or a 30-second zero-code demo. `explore` is Phase 1.5. Journey 9 keeps empty-state craft (earned). First-week EventStore demo is absent.
- **Note:** Qualitative DX the brief treated as product, not polish, becomes optional documentation. Explore was how Alex was supposed to *understand* the graph before shipping.
- **Fix:** FR for custom extraction phrases in MVP (templates later), or an explicit cut. Put README/demo and `explore` (even read-only) on the MVP story list if debug-first DX is still a gate. Otherwise defer “debug-first” language so it doesn’t outrun the CLI cut.

### medium Go/no-go and benchmark character no longer match the brief’s proof

- **Brief:** Six MVP criteria, proceed if 5 of 6 pass. Example benchmark: “Find documents about payment processing referenced in discussions following the March deployment failure” — a query that needs documents + discussions + events. Hybrid-value metric: “returns results that no single axis alone would find” (manual top-20 review). Causal completeness 95%+ of EventStore events.
- **PRD:** Three hard gates (three-axis, zero leaks, 30-minute onboarding) plus 2 of 3 soft (causal 95%, MCP e2e, case model). Isolation is promoted (brief could theoretically pass 5/6 with isolation failing). MCP and causal are demoted *and* moved out of MVP. Scoring becomes NDCG@10 with independent reviewers. The motivating multi-source example is gone. Discussions remain Phase 2, EventStore Phase 1.5 — so the brief’s example query cannot be a Phase 1 benchmark.
- **Note:** The thesis the brief wanted to prove was relational-and-causal, not “hybrid rankers beat BM25 on a document corpus.” NDCG is a tightening, not a defect; losing the example *scene* is the fidelity loss.
- **Fix:** Publish 5–10 benchmark *scenes* that match whatever actually ships in MVP (file+URL+manual relations if that is the thesis), and move discussion/event scenes to P1.5. Align go/no-go with shipped surfaces.

### medium Ecosystem health bars dropped or retimed

- **Brief:** Second embedding provider (Google + one other) within 6 months. “Works with all DAPR state store components without custom code.” CLI/MCP 100% completeness. “Teams using Memories ship AI features 5x faster.” “Validated migration path is a Phase 2 priority.” Kenji: “no unplanned downtime from index growth.” Alex trust: “Uses `--explain` less over time.” Full vision: “every Hexalith app ships with memory by default.”
- **PRD:** Google-only in MVP; other providers “post-MVP” with no 6-month clock. No “all DAPR state stores” claim. 5x productivity gone. Qdrant implementation is Phase 3 (extraction points mentioned in Phase 2 only as licensing insurance). Kenji success is leaks + provision time, not growth-induced downtime. Alex trust becomes “Deploys … to production / Within 60 days of first use.” Vision line is shortened to “standard knowledge layer for event-sourced applications.”
- **Note:** Several of these were poorly measurable (5x) or over-claimed (all state stores); dropping them can be honesty. They are still brief commitments the PRD does not absorb or defer by name.
- **Fix:** For each: keep with a date, defer with a phase, or record a rejected-alternative note. Don’t silently evaporate.

### medium Phase map compressed and a few items moved without saying so

- **Brief:** Phase 2 collaboration/polish; Phase 3 intelligence/scale (including embedding versioning and Qdrant); Phase 4 enterprise & UI (Explorer, Timeline, ACLs, redaction, geo pinning, encryption, compliance evidence, audit trail on every operation). R3: consider lightweight cross-case references in Phase 2 if one-case rigidity hurts.
- **PRD:** Phase 2 adds embedding versioning (pulled forward from Phase 3). Phase 3 swallows brief Phase 3 + Phase 4. No Phase 4. No R3 cross-case-reference escape hatch. Audit vision is rewritten: access telemetry “is *not* a tamper-evident audit trail.”
- **Note:** Compression is a planning choice; the audit rewrite is more honest than the brief. The missing R3 hatch matters because FR32 is otherwise absolute.
- **Fix:** Publish a one-line phase crosswalk vs the brief. Restore R3 as a Phase 2 open question. Keep the telemetry-vs-audit distinction — that is a good PRD clarification — and state that it supersedes the brief’s “audit trail on every memory operation.”

### low Brand line and first-week critical path are not in the contract

- **Brief:** Title line “Connected knowledge that understands why.” First Week Build Sequence as the critical path to a three-axis EventStore demo.
- **PRD:** Positioning is “The memory server that understands causality” / answers “why did this happen?” First-week table absent (appropriate for a PRD, but the *demo promise* is not replaced).
- **Note:** Tagline change is tone, not scope. The week-1 demo is the same sequencing issue as the critical finding.
- **Fix:** Decide the brand line (keep, replace, or park for UX). Replace the week-1 table with an MVP demo scene that matches actual Phase 1 scope.

## Silent expansions (brief never said this)

### high Aspire-mandatory topology, OpenBao secrets, and a nine-package “current inventory”

- **PRD:** Developer Tool section: “No standalone server deployment — .NET Aspire AppHost orchestrates all services.” Product services “retrieve them through DAPR secret-store components” backed by OpenBao. “Current release inventory: 9 published NuGet packages” including `Client.Rest`, `Telemetry`, `Aspire`, `ServiceDefaults`. Language/platform matrix, YARP/nginx ingress, Hexalith.Commons error envelopes.
- **Note:** The brief’s shape was Contracts / Server / Client / Redis / Cli / Mcp / EventStore and Day 1 `dotnet run` starts the service. The PRD reads like a snapshot of an existing repo, not a requirements derivation. That binds Kenji and Alex to Aspire+OpenBao as product, not as one implementation.
- **Fix:** Move topology, package inventory, and secret backend to addendum (or architecture). In the PRD, keep only externally visible constraints (DAPR-native, Redis-first, CLI/MCP/EventStore packages). If Aspire is a real product decision, confirm it explicitly against the brief’s simpler `dotnet run` onboarding.

### high Zero-code generalized from EventStore to “any DAPR pub/sub” (Marten, Wolverine, Axon)

- **PRD:** What Makes This Special: “clear path to support other event-sourced frameworks (Axon, Marten, Wolverine).” Innovation #2 and Addressable market expansion: “not locked to Hexalith.EventStore”; “every .NET team using DAPR.” Kill switch if generic integration isn’t zero-code: narrow the claim.
- **Note:** Brief Moat 2 was Hexalith-ecosystem-specific on purpose (“hardest for competitors to replicate”). R6’s expansion path was *standalone case + three-axis for non-EventStore users*, not zero-code for every bus. This changes who the beachhead is.
- **Fix:** Confirm whether generic-DAPR is a Phase 1.5 requirement, a later experiment, or marketing. If confirmed, update Moat 2 and R6 so they don’t still read as EventStore-exclusive.

### high REST-as-ingress in MVP vs brief “REST API … Phase 2”

- **PRD:** External consumers “connect through a REST API behind infrastructure-managed ingress.” CLI “uses a minimal direct HTTP/ingress adapter.” Phase 2 still lists “REST API for application search UIs.” Journey 8 (Priya) depends on that app-facing REST.
- **Note:** Plumbing REST for CLI is a reasonable elaboration. Productizing REST as the external path, while the brief said MCP + CLI + DAPR invocation suffice for MVP, is a new surface. It also collides with MCP-deferred: something must sit in front of the server.
- **Fix:** Name two things: (1) internal/CLI transport may be HTTP in MVP; (2) the application search API Priya’s screenshot needs remains Phase 2. Don’t let “REST controllers for ingress” read as the Phase 2 REST product.

### medium Interpretive-infrastructure / compliance program

- **PRD:** Three-tier Storage → Interpretation → Application model, GDPR-enabling tenant delete with cross-reference limitation, “Building Compliant Applications on Memories,” legal disclaimer, “Security Posture for Auditors,” confidence-vs-accuracy caveats.
- **Note:** Brief had isolation, deletion cleanliness, and Phase 4 “compliance evidence gathering.” It did not create a compliance-enablement product line or an “interpretive infrastructure” category. Useful, but it will drive docs and legal review the brief never budgeted.
- **Fix:** Confirm this is in-scope for the PRD vs a later enterprise phase. If yes, keep; if it is architecture/legal depth, move to addendum.

### medium Fusion algorithm, Evidence Packet, and `nl` as a scored axis

- **PRD:** Weighted reciprocal-rank fusion is mandatory (NFR24–25). Evidence Packet is the cross-surface trust envelope. Confidence table includes an NL score / `axis=nl`. Gap detection `[MISSING: event-id]`. Edge taxonomy `caused_by` vs `correlated_with`.
- **Note:** The brief required three-axis hybrid and causal graphs, not a specific fusion, a named packet, or a fourth query axis. These are mostly healthy PRD deepenings of Moat 2/3. They become defects only if they freeze R&D the brief wanted to *validate*.
- **Fix:** Keep gap detection and caused_by vs correlated_with — they protect the “why” promise. Mark RRF/Evidence Packet/`nl` as the current design of record, with permission to change the fusion during the spike without a PRD rewrite.

### medium Apache 2.0 no-relicense pledge, SSPL/AGPL dependency strategy

- **PRD:** “Hexalith.Memories is committed to the Apache 2.0 license. We will not change to a restrictive license.” LICENSE-DEPENDENCIES.md, FalkorDB pinning, SSPL hosted-service constraint in README.
- **Note:** Brief only said “open-source.” A public no-relicense pledge is a business commitment. Dependency licensing is addendum-grade unless it constrains the Redis/FalkorDB MVP choice (it does).
- **Fix:** Confirm Apache 2.0 + pledge with the author. Keep SSPL/AGPL constraints visible if Redis Stack + FalkorDB remain the MVP backends.

### medium Contributor journey, samples path, extra FRs, identity/provenance

- **PRD:** Journey 10 (Dani), `samples/01-03`, FR12 re-ingest, FR21 metadata filters, FR22 pagination, FR36 activity, FR37 annotations, FR65 `ingested_by` / insider threat, FR71 export (Phase 2), FR74 consistency repair, Python/TypeScript clients as future, “2 organizations … production usage” as a 12-month sustainability test, MCP directory listing as a 6-month hard target.
- **Note:** None of this is in the brief. Most is normal PRD elaboration. The 6-month MCP listing *hard target* is new pressure given MCP itself left MVP. Annotations in Journey 4 (Phase 2 briefing) create a feature the FR list treats as present.
- **Fix:** Keep operational FRs. Tie the MCP-listing clock to Phase 1.5 ship, not to thesis day. Mark Journey 10 as community/infra, not product scope (the PRD already says this — keep it out of MVP feature counts). Confirm annotations as Phase 2 with briefing.

### low Solo-developer / 22–32 story sizing and fusion-as-primary-R&D

- **PRD:** “Solo developer. Estimated 22-32 stories.” Fusion called “the primary technical risk and the key R&D investment.”
- **Note:** Brief estimated 29–44 stories across 7 epics including MCP, EventStore, and full CLI — a different staffing and scope assumption. Elevating fusion (how to merge scores) slightly displaces the brief’s risk (whether three axes *help users*).
- **Fix:** Treat sizing as planning, not product. Keep both kill switches: fusion quality *and* user-visible three-axis value.

## Qualitative residue

These are tone, voice, and feel the brief asked for and the FR structure did not carry. Some survive in journeys or exec prose; they will still die in epics unless someone re-homes them.

- **Brand feel:** “Connected knowledge that understands why” — not just causality-as-feature, but knowledge that is *connected*. FRs specify axes, scores, and edge types; they never require the composed *story* except as LLM-side narrative (the PRD even assigns prose to the LLM). The hero was “the story of how they connect.”
- **MCP-first social proof:** Why Now’s “agents need memory servers they can call natively.” The FR list makes MCP a tool schema (FR54, FR58), not a launch posture. Combined with Phase 1.5, the product no longer *feels* like it showed up where agents already are.
- **Anywhere-ingest abundance:** “from anywhere” (drives, git, images, video) was a feeling of completeness for “any team.” FRs feel like a file closet.
- **Debug-first delight:** `--explain`, `explore`, errors that teach, README as carefully written as code, “first failures are part of first impressions,” “This is what I've been building manually.” FR56–57 and NFR30 are the residue; `explore` and README-as-product are gone from the feature table.
- **Day-one onboarding warmth:** “New team members are productive on day one”; Priya as “the screenshot on the landing page”; Marcus getting Friday afternoon back. No FR for briefing quality, screenshot-worthiness, or “productive contribution.” Journey 4/8 hold the feeling; Phase 2 holds the feature.
- **Enterprise seriousness without duct tape:** physically separate indexes, “Tenant provisioning is one command. Backend swap with zero downtime.” Provisioning is specified; seamless swap is Phase 3 and not a Kenji success metric.
- **Honesty of origin:** human-declared vs AI-inferred as a *trust posture*, not only FR64 fields. The Evidence Packet expansion actually serves this feel — but it is an expansion, not a brief carry.
- **Shared memory, not per-user silos:** Why Now’s team-AI line. Cases are in the FRs; “not per-user silos” never appears as a non-goal/non-user (no personal-only memory SKU, no “this is not ChatGPT memory”).
- **Fragile tribal knowledge / teams that never find each other:** Problem Impact in the brief. No FR for cross-case discovery or “teams working on similar problems.” Tenant-wide search (FR34) is the pale remainder.
- **Zero-mapping magic:** “Drop in the NuGet package … No mapping code required.” FRs 59–61 specify it; MVP won’t ship it, so the *magic* is not part of first contact.
- **Open-source gravity:** 30-second demo, GitHub trending, DAPR ecosystem listing, “the README is a product deliverable.” Adoption metrics remain; the *gravity* of first impression is not an FR.

**Re-home these before polish:** brand line + non-users/non-goals; ingest-from-anywhere destination; MCP/EventStore as launch feel vs thesis prototype; README/`explore`/error craft as MVP features or explicit deferrals; Priya’s landing-page job as a Phase 2 UX constraint, not only a journey.
