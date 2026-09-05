# Validation Report — Hexalith.Memories

- **PRD:** `_bmad-output/planning-artifacts/prd.md`
- **Rubric:** `.agents/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-09-05T10:24:01+02:00
- **Grade:** Poor

## Overall verdict

The PRD still has a real thesis, named trade-offs, and unusually concrete NFRs, pipeline, compliance, and licensing substance for an older BMad format. What does not hold is a single ship contract: "MVP" names both a CLI proof-of-thesis prototype and a launch checklist that includes MCP, EventStore causality, and a `dotnet add package` onboarding path the feature table deferred. At launch / chain-top stakes, with architecture, epics, UX, and months of sprint changes already downstream of this document, those forks will keep generating the wrong stories unless the PRD picks one increment and phases the rest.

Adversarial review refuses to sign the go/no-go: the 80% kill switch cannot fail as written, Phase 1 has no causal graph to fuse, and Greenfield classification already inventories nine published packages. Product-brief reconciliation shows the PRD kept the brief's identity but not its v1 contract — MCP and EventStore left MVP while leftover sentences still treat them as launch gates. Downstream drift is decisive: two approved August 2026 Major sprint-change proposals required PRD amendments that never landed, and architecture now treats EventStore as domain source of truth while the PRD still schedules it as Phase 1.5 integration.

## Dimension verdicts

- Decision-readiness — thin
- Substance over theater — adequate
- Strategic coherence — adequate
- Done-ness clarity — thin
- Scope honesty — thin
- Downstream usability — thin
- Shape fit — adequate

## Findings by severity

### Critical (9)

**[Decision-readiness]** — "MVP" means two incompatible ship contracts (§ Success Criteria › MVP Go/No-Go Gate vs § MVP Feature Set vs § Phase 1.5)

The go/no-go table is titled "MVP" and requires MCP end-to-end and causal-chain completeness as soft gates, plus a `dotnet add package` hard onboarding path, while Phase 1 explicitly excludes EventStore and MCP.

Fix: Split into a Phase 1 thesis gate (hybrid NDCG, isolation, CLI onboarding) and a launch gate (MCP, EventStore, causal completeness); point the <30 min hard gate at one onboarding path.

**[Adversarial]** — The 80% hybrid kill switch is unfalsifiable as specified (§ Measurable Outcomes › Three-Axis Kill Switch)

Queries "requiring all three axes" make hybrid win by construction. N=5–10, no ΔNDCG, no BM25+vector control, ground truth locked before queries exist, and an A/B fallback if reviewers are unavailable.

Fix: Frozen corpus and realistic (not graph-filtered) queries; graded labels after queries exist; primary comparison hybrid vs BM25+semantic; pre-registered ΔNDCG and N; named kill actions. Delete the unlabeled A/B fallback.

**[Adversarial]** — MVP proves a retrieval toy, not the product thesis (§ MVP Feature Set vs Executive Summary)

You can pass every hard gate and still have proven none of: agents getting better answers, CausationId becoming a queryable story, or `dotnet add package` auto-indexing an event stream. MCP and causal completeness are soft gates you are allowed to fail.

Fix: State that thesis MVP proves the algorithm only, then either demote EventStore/MCP/causal narrative from the exec summary until Phase 1.5, or put one of them on the hard-gate list. Make Phase 1.5 slip a product delay, not an MVP scope accordion.

**[Adversarial]** — Phase 1 has no honest graph to fuse (§ MVP Feature Set #3–#4 vs Causal Intelligence)

Causal edges (`caused_by`, `correlated_with`) are EventStore metadata scheduled for Phase 1.5. File ingest never specifies how non-trivial edges appear. "Queries requiring all three axes" cannot run against a folder tree.

Fix: Closed Phase 1 edge inventory. If causal edges are absent, forbid "three-axis" until EventStore lands; validate BM25 vs vector vs BM25+vector in Phase 1. Remove DAPR event publish from Journey 9 or move it to 1.5.

**[Adversarial]** — The document classifies a shipping system as Greenfield (§ Project Classification vs § Package Distribution)

Front matter says greenfield. The architecture chapter inventories nine published packages and a compatibility-only Redis package "retained for existing package consumers."

Fix: Reclassify as brownfield / in-progress. Status column on FR/NFR (`shipped` / `partial` / `not started`). Split must-keep topology from remaining MVP work.

**[Product-brief]** — EventStore zero-code and MCP are no longer MVP, but the beachhead contract still talks as if they are (Brief Phase 1 #3/#7 vs PRD Phase 1.5)

The brief's v1 is a memory server LLM agents can call and EventStore developers can drop in. The PRD's v1 is a CLI-operated retrieval prototype, while Journey 1 and `dotnet add package` still launder the brief's beachhead into MVP.

Fix: Either put EventStore auto-index + MCP back in Phase 1, or rewrite MVP gates, Journey 1, Executive Summary, and Alex JTBD for CLI proof-of-thesis only.

**[Product-brief]** — "Full feature parity on MCP + CLI" is contradicted by the parity matrix (Brief design principle vs PRD Interface Capability Parity Matrix)

Executive Summary repeats "Every feature is accessible through both MCP and CLI." The matrix then says not all capabilities map to all interfaces; MVP CLI is further cut to benchmark essentials.

Fix: Delete or qualify the exec-summary parity sentence. If the product is "MCP for agent work, CLI for ops," say that and list which brief features are intentionally MCP-absent.

**[Downstream drift]** — Approved August 2026 PRD amendments never landed (`prd.md` vs 2026-08-03 Major SCPs)

Two approved Major SCPs required C# 14, a phase register, identity/provenance, EventStore-commit state machine, NFR11 as current, NFR32–NFR34, and deletion of the isolation-escape hatch. The 2026-08-04 readiness report still lists those defects; PRD last patched 2026-07-19.

Fix: Apply a single reconciled PRD amendment covering both August patch sets. Do not treat either SCP as already done.

**[Downstream drift]** — EventStore is domain truth in architecture, a Phase 1.5 integration in the PRD (PRD Phase 1.5 vs Architecture driver #3 / Epic 21)

Three EventStore contracts are now in play: (1) zero-code CloudEvent ingestion, (2) aggregate source of truth for domain writes, (3) consumer pin of EventStore bits. The PRD only knows (1) and still describes indexing as an atomic Redis triple-write.

Fix: Split the three contracts. Keep FR59–62 as Phase 1.5 product integration. Rewrite FR13/NFR17 so EventStore acknowledgement is the durable commit and search/graph writes are rebuildable projections.

### High (29)

**[Decision-readiness]** — Fusion algorithm is two different decisions (§ MVP Strategy vs NFR24)

Sequencing still treats fusion as BM25/cosine/proximity weighting; NFR24 has already chosen weighted RRF.

Fix: Declare RRF as the decision; demote the three-normalization paragraph to historical context.

**[Strategic coherence]** — The kill switch is partly tautological (§ Executive Summary vs § Three-Axis Kill Switch)

The protocol restricts the suite to "5–10 queries requiring all three axes," so it cannot falsify "three-axis is the right product."

Fix: State the population (representative mix vs thesis-stress queries); make the reviewer fallback a documented downgrade of gate confidence.

**[Done-ness clarity]** — Functional requirements are not increment-scoped (§ Functional Requirements vs § MVP Feature Set)

Seventy-four FRs, one phase tag (FR71). MCP, EventStore, members, annotations, status, and batch ingest sit in the same flat list as MVP search.

Fix: Tag every FR with Phase 1 / 1.5 / 2 / 3.

**[Done-ness clarity]** — Several FRs have no single testable consequence (FR13, FR31, FR43, FR57)

FR13 authorizes rollback *or* retry. FR31 "health indicators." Soft gate "Case model correctly scopes memory" is the same adjective problem.

Fix: Pick rollback or retry for FR13; replace adjectives with observable outputs.

**[Scope honesty]** — Tensions were smoothed instead of opened (document-wide)

Zero Open Questions, `[ASSUMPTION]`, `[NOTE FOR PM]`, or Assumptions Index at launch stakes.

Fix: Add a short open-item list that names each fork and the owner.

**[Scope honesty]** — Journey scope notes instruct the wrong increment (§ Journey 2, § Journey 9)

Journey 2 points at "MVP Feature #3 (EventStore Integration)" but MVP #3 is Three-Axis Search. Journey 9 is listed as full MVP then climaxes with a DAPR topic publish.

Fix: Re-bind each journey to a phase; strip DAPR publish from Journey 9 or move it to 1.5.

**[Downstream usability]** — Domain nouns collide without a glossary (chain-top extract risk)

"Confidence" means query relevance, per-field metadata confidence, and edge-type default confidence. "Audit" is telemetry that the compliance section says is *not* a tamper-evident audit trail.

Fix: A short Glossary; then align FR/NFR wording.

**[Adversarial]** — Competitive landscape is a straw man; "no competitor offers this" is load-bearing theater (§ What Makes This Special)

Omits GraphRAG, Neo4j LLM graphs, LlamaIndex PropertyGraphIndex, Vespa, Weaviate hybrid, Azure AI Search. Collaborative memory features cited as unique are Phase 2.

Fix: Rebuild the table against hybrid-search and GraphRAG incumbents. Mark each differentiator as shipped-in-MVP, Phase 2, or EventStore-only.

**[Adversarial]** — "Zero mapping code / zero configuration" is already retracted in the same PRD (Executive Summary vs Journey 2 / FR59–FR62)

Marketing says zero mapping. Journey 2 requires registering `ClaimSubmittedV2` handlers. MVP does not ship the EventStore package at all.

Fix: Honest contract in the exec summary: EventStore happy path is subscription + conventions; schema changes require handler registration. Delete "zero configuration" or list the actual config.

**[Adversarial]** — The kill switch does not kill (§ Measurable Outcomes vs Innovation § Risk Mitigation)

Three wordings, three severities, one outcome: keep building. Fallback preserves FalkorDB, actors, tenant-isolated graphs, and the case model.

Fix: Name sunk-cost actions, or delete kill-switch language.

**[Adversarial]** — The "cannot retrofit" foundation is also the first budget cut (§ MVP Strategy vs § Resource Risks)

Isolation/cases are load-bearing architecture *and* optional if the solo developer gets tired.

Fix: If physical isolation is a hard gate, remove it from the cut list. If the true MVP is Engine+Search+CLI+Benchmarks, drop isolation from hard gates and enterprise marketing.

**[Adversarial]** — "Physically isolated per-tenant indexes" is logical namespacing in a shared Redis (Journey 5, FR38, NFR8)

Separate index names on one Redis/FalkorDB process is not physical isolation. Isolation tests are application-layer tenant-ID checks.

Fix: Define isolation tiers L1–L3. Tag MVP as L1. Strike "physical" and "enterprise-grade isolation" unless L2 is in scope.

**[Adversarial]** — The worst FRs are slogans; FR13 is the exhibit (FR13, FR43, FR53, FR59, FR74)

FR13 authorizes opposite recovery products. EventStore and MCP FRs sit in the same unphased pile as ingest-a-file.

Fix: Rewrite the dangerous FRs first. Ban "or" in recovery FRs. Phase-tag every FR.

**[Adversarial]** — The 30-minute onboarding hard gate clocks a path that does not exist in Phase 1 (User Success vs NFR31 vs Journey 1)

Clock starts at `dotnet add package`. MVP has no EventStore zero-code flow, no reusable Client package, and a runtime of Aspire + DAPR + Redis + FalkorDB + OpenBao + a Google embedding key.

Fix: Numbered Phase 1 stopwatch procedure starting at the command you actually ship. Move the EventStore 30-minute claim to a Phase 1.5 hard gate.

**[Adversarial]** — Case members, `ingested_by`, and tenant mismatch exist; a user identity model does not (Service Communication vs FR28–FR29, FR65)

Per-user identity is postponed; add/remove members, mandatory `ingested_by`, and audit telemetry are specified as free-text strings.

Fix: Minimal principal model in MVP, or remove member FRs, insider-threat claims, and "audit" until identity exists.

**[Product-brief]** — Ingest-from-anywhere (cloud, git, images, video) vanishes from the roadmap (Brief exec summary vs PRD FR1–FR4 / Phase 2–3)

Problem copy still mentions "cloud drives." FRs are files, URLs, PDF, markdown. After EventStore left MVP, "any team" is left with a file closet.

Fix: Explicit later-phase ingestion roadmap or an explicit deferral with owner and revisit condition.

**[Product-brief]** — Thirty-minute gate silently changed meaning (Brief Alex JTBD vs PRD NFR31)

Three different clocks share one slogan: NuGet+subscription, CLI ingest→search, and a Docker README walkthrough.

Fix: Split gates: (MVP) timed CLI ingest→search; (P1.5) timed `dotnet add package`→first event search.

**[Product-brief]** — Unauthorized "absolute minimum" cut drops cases and tenant isolation (§ Resource Risks)

The brief said neither is optional. The PRD authorizes deferring both to fast-follow.

Fix: Strike the cases/tenancy deferral.

**[Product-brief]** — Jobs-to-be-done, Marcus's hiring jobs, and non-users are not contracted (Brief JTBD table vs PRD Success Criteria)

No JTBD table, no non-users. Marcus success is a Phase 2 briefing. Priya has no success metric.

Fix: Restore a short JTBD + non-users block. Give Marcus decay/cross-case insight an explicit phase.

**[Product-brief]** — Competitive "Why Now," knowledge-base rivals, and the 12–18 month window are gone (Brief Why Now / R5 vs PRD Market Context)

Notion/Confluence, Weaviate, OpenSearch drop out. Speed-to-market as R5 mitigation is inverted by a thesis-only MVP.

Fix: Re-home Why Now, wiki competitors, and R5/R6 in a short Market Context.

**[Product-brief]** — Extraction phrases dropped; README-as-product and explore-as-trust-building weakened (Brief DX vs PRD MVP CLI)

README "ships with MVP but is documentation, not a story-estimated feature." `explore` is Phase 1.5. No FR for extraction phrases.

Fix: FR for custom extraction phrases in MVP, or an explicit cut. Put README/demo and `explore` on the MVP story list if debug-first DX is still a gate.

**[Product-brief]** — Aspire-mandatory topology, OpenBao secrets, and a nine-package "current inventory" (PRD Developer Tool section)

The brief's shape was `dotnet run` starts the service. The PRD binds Kenji and Alex to Aspire+OpenBao as product.

Fix: Move topology, package inventory, and secret backend to addendum (or architecture). Keep only externally visible constraints in the PRD.

**[Product-brief]** — Zero-code generalized from EventStore to "any DAPR pub/sub" (Axon, Marten, Wolverine)

Brief Moat 2 was Hexalith-ecosystem-specific. R6's expansion path was standalone case + three-axis, not zero-code for every bus.

Fix: Confirm whether generic-DAPR is a Phase 1.5 requirement, a later experiment, or marketing.

**[Product-brief]** — REST-as-ingress in MVP vs brief "REST API … Phase 2"

Plumbing REST for CLI is a reasonable elaboration. Productizing REST as the external path is a new surface.

Fix: Name internal/CLI HTTP in MVP separately from the application search API Priya needs in Phase 2.

**[Downstream drift]** — Greenfield living contract vs months of brownfield delivery (PRD classification vs epics Epic 0–31)

April 2026 SCP: "The MVP roadmap is now exhausted at the planning level." Classification still licenses a 22–32 story plan and an isolation-escape hatch.

Fix: Reclassify as brownfield / change-controlled. Point sizing at `epics.md` + `sprint-status.yaml`.

**[Downstream drift]** — External auth and identity: PRD still Phase 1.5 / tenant-only; Epic 20 already shipped JWT (NFR11 vs Story 20.1)

Extracting NFR11 from the PRD would schedule authentication as fast-follow work that change control already treated as a production-exposure blocker.

Fix: Move NFR11 to MVP/current; name anonymous health/Dapr exceptions; replace "per-user identity not in MVP" with the 08-03 identity contract.

**[Downstream drift]** — Ingestion is a DAPR Workflow in architecture, a pipeline actor in the PRD (§ Async Ingestion Pipeline vs Architecture driver #4)

The PRD's actor-queue story is the pre-D23 design. Extracting FR8/FR9/FR13/NFR17 would rebuild a discarded pipeline actor.

Fix: Rewrite the pipeline section: workflow owns stages; a per-tenant actor (if any) owns only rate-limit budget.

**[Downstream drift]** — Fusion spike text still describes magnitude blending; NFR24 and architecture settled on RRF (§ Implementation Sequencing vs Story 22.4)

An extractor using the MVP Strategy section would spike a different algorithm than Gate 1 already governs.

Fix: Replace the BM25/cosine/proximity-weighting spike with RRF + explain-of-rank-contributions. Numeric `k` and default weights: architecture owns these.

**[Downstream drift]** — MCP/CLI "every feature" vs capability alignment and Phase 1.5 MCP (Executive Summary vs Architecture Interface Philosophy)

MCP *timing* is still aligned. The exec-summary parity claim and unphased FR53 are not. Journey 2's "MVP Feature #3 = EventStore" error is still in the PRD.

Fix: Strike "every feature / both interfaces." Point FR53/FR54 at the parity matrix and a phase register.

### Medium (25)

**[Decision-readiness]** — Case membership is specified without an identity model (§ Journey 4, FR28–FR29 vs Service Communication Model)

Fix: Defer FR28/FR29/Journey 4 to Phase 2, or define the MVP member as an unauthenticated identifier and state enforcement.

**[Substance over theater]** — Several personas/journeys do not drive an MVP (or even Phase 1.5) decision (Journeys 4, 6, 8, 10)

Fix: Keep Alex / Kenji / LLM Agent as load-bearing; mark the rest as vision illustrations.

**[Strategic coherence]** — Hero differentiators are not the MVP bet (§ What Makes This Special vs § MVP Feature Set)

Fix: Lead the exec summary with the Phase 1 proof (hybrid + isolation + CLI); treat causality and collaboration as sequenced bets.

**[Strategic coherence]** — Isolation is both un-deferrable and deferrable (§ MVP Strategy vs § Resource Risks)

Fix: Strike the fallback or reclassify isolation as retrofittable with an explicit cost.

**[Done-ness clarity]** — Latency "done" disagrees with itself (User Success vs NFR1–NFR3 vs NFR7)

Fix: One latency budget per surface; point User Success at those NFR IDs.

**[Scope honesty]** — No Non-Goals section where it would do work (§ Phase 2 / Phase 3 vs hero copy)

Fix: A Non-Goals block that names MCP/EventStore/REST/briefing/members as non-goals *for Phase 1*.

**[Downstream usability]** — Authoritative shape has already moved out of the PRD (§ Evidence Packet, § Package Distribution)

Fix: Freeze PRD-owned names/IDs and say implementation SoT lives elsewhere, or add an addendum for post-architecture decisions.

**[Shape fit]** — User-journey density is consumer-product shaped (§ User Journeys)

Fix: Treat Journeys 1, 2, 5, 7, 9 as normative; demote the rest to appendix illustrations.

**[Shape fit]** — Classification and resource model were not updated when the body was (§ Project Classification vs § Package Distribution)

Fix: Reclassify as brownfield/in-progress; drop or update the solo/story estimate; date-stamp March intent vs patched fact.

**[Adversarial]** — Confidence is defined as calibrated truth and as "not accuracy" in the same chapter (Compliance Boundary vs AI Reliability)

Fix: One semantics table for the three confidence kinds; put the relevance-not-truth caveat in the Evidence Packet schema.

**[Adversarial]** — An open-source adopter outside Hexalith would refuse the runtime and license tax (§ Open-Source Licensing, Deployment Topology)

Fix: Decide the license (not "recommended"). Add an adopter persona gate without EventStore/OpenBao/submodules, or declare Hexalith-only MVP.

**[Adversarial]** — Latency, freshness, and "atomic three-backend index" NFRs disagree with the journeys (LLM Agent success vs NFR1–NFR4 vs FR6/FR13/NFR18)

Fix: Collapse to one latency budget per surface. Pick atomic-index XOR degraded-read. Add MVP freshness for file ingest.

**[Product-brief]** — Go/no-go and benchmark character no longer match the brief's proof (Brief 5-of-6 vs PRD 3 hard + 2-of-3 soft)

Fix: Publish 5–10 benchmark *scenes* that match whatever actually ships in MVP; move discussion/event scenes to P1.5.

**[Product-brief]** — Ecosystem health bars dropped or retimed (second provider in 6 months, 5x productivity, all DAPR state stores)

Fix: For each: keep with a date, defer with a phase, or record a rejected-alternative note.

**[Product-brief]** — Phase map compressed and a few items moved without saying so (Brief Phase 4 → PRD Phase 3; embedding versioning pulled forward)

Fix: Publish a one-line phase crosswalk vs the brief. Restore R3 as a Phase 2 open question.

**[Product-brief]** — Interpretive-infrastructure / compliance program (PRD Domain-Specific Requirements)

Fix: Confirm this is in-scope for the PRD vs a later enterprise phase; otherwise move to addendum.

**[Product-brief]** — Fusion algorithm, Evidence Packet, and `nl` as a scored axis freeze R&D the brief wanted to validate

Fix: Keep gap detection and caused_by vs correlated_with. Mark RRF/Evidence Packet/`nl` as current design of record, with permission to change fusion during the spike.

**[Product-brief]** — Apache 2.0 no-relicense pledge, SSPL/AGPL dependency strategy

Fix: Confirm Apache 2.0 + pledge with the author. Keep SSPL/AGPL constraints visible if Redis Stack + FalkorDB remain the MVP backends.

**[Product-brief]** — Contributor journey, samples path, extra FRs, identity/provenance, 6-month MCP listing hard target

Fix: Tie the MCP-listing clock to Phase 1.5 ship. Mark Journey 10 as community/infra. Confirm annotations as Phase 2 with briefing.

**[Downstream drift]** — C# 13 / package-count / backend-access wording still contradict repository and architecture facts

Fix: PRD language baseline `.NET 10 / C# 14`; SDK pin deferred to `global.json`. `tools/release-packages.json` is the only package count.

**[Downstream drift]** — Physical isolation: PRD still "separate indexes"; architecture moved the security boundary to ACL users (FR38 vs Story 24.3)

Fix: Keep NFR8/FR38/FR40 outcomes. Add one sentence that the isolation *target* is tenant-scoped backend principals; indexes remain lifecycle resources.

**[Downstream drift]** — Dapr Agents / polyglot runtime never entered the PRD (Architecture D27/D28)

Fix: Architecture owns Dapr Agents. PRD should either defer or add one constraint: optional Python `ai-agent` sidecar via DAPR invocation.

**[Downstream drift]** — FR71 / export: PRD Phase 2 tag vs "already shipped" vs Epic 26 backup slice

Fix: Keep FR71 out of MVP acceptance. Add "completed non-MVP (Story 8.3); Epic 26 covers operational backup/restore only."

**[Downstream drift]** — Rate limiting: deferred to ingress in one PRD table, a product FR in the next

Fix: Delete "Deferred to infrastructure." Distinguish embedding-provider throttle, inbound request quotas, and ingress.

**[Downstream drift]** — Journey 1 / samples still sell EventStore zero-code as day-one onboarding

Fix: Relabel Journey 1 as Phase 1.5 success path; make Journey 9 the MVP success path in the summary table.

### Low (8)

**[Decision-readiness]** — License is a public trust commitment and still a recommendation (§ Open-Source Licensing)

Fix: Record Apache 2.0 as the decision, or list the remaining blocker.

**[Substance over theater]** — Novelty claims that the rest of the PRD does not need (§ What Makes This Special)

Fix: Keep the competitor table; drop uniqueness superlatives unless Discovery named a disconfirming competitor.

**[Product-brief]** — Brand line and first-week critical path are not in the contract

Fix: Decide the brand line. Replace the week-1 table with an MVP demo scene that matches actual Phase 1 scope.

**[Product-brief]** — Solo-developer / 22–32 story sizing and fusion-as-primary-R&D

Fix: Treat sizing as planning, not product. Keep both kill switches: fusion quality *and* user-visible three-axis value.

**[Downstream drift]** — NFR6 / FR71 inventory wording drifted in epics, not in architecture's count

Fix: Restore omitted clauses in epics inventory, or mark the inventory as a pointer to the PRD.

**[Downstream drift]** — Epics Additional Requirements still freeze architecture D3 as "eventual consistency"

Fix: Update epics D3 bullet to match architecture. PRD FR13 should use EventStore-commit language.

**[Downstream drift]** — UX makes explain mandatory on every search; PRD keeps `--explain` opt-in (FR19 vs UX-DR7)

Fix: PRD should pick: compact trust fields on every search with `--explain` expanding math, or UX-DR7 is opt-in.

**[Downstream drift]** — Access-telemetry PostgreSQL / OpenBao platform are operational, not a Redis-only product pivot

Fix: Architecture owns telemetry substrate. PRD FR67 should defer store choice and repeat that access telemetry is not a compliance audit trail.

## Mechanical notes

- No Glossary, no Assumptions Index, no addendum.md, no .memlog.md — empty roundtrip by construction, not by discipline.
- FR1–FR74 and NFR1–NFR31 are unique and contiguous; only FR71 carries a phase tag. Journey summary uses persona titles instead of Journey IDs, which is fine until "MVP Feature #3 (EventStore Integration)" fails to resolve (MVP #3 is Three-Axis Search).
- `memories tenant switch` appears in Journey 5 and the CLI command table but not in MVP command scope (`create/delete/verify` only).
- Dual embedding (payload + natural-language description) is both an EventStore Phase 1.5 feature (FR60) and an `axis=nl` score in the MVP Evidence Packet table — easy to extract as a fourth retrieval axis.
- User Success "cached / cold" latency has no matching NFR; NFR7 "Cold start time" is process boot, not cache warmth.
- Evidence Packet, `Contracts.V1`, and `tools/release-packages.json` are forward references to architecture/repo layout, not intra-PRD links.
- UJ protagonists are named (Alex, Marcus, Kenji, Priya, Dani); Journey 7 correctly has none.
- Required-for-stakes gaps: Non-Goals, Open Questions, Glossary — missing in ways that already appear in the dimensions above.
- FR/NFR *identifiers* still match across PRD, architecture counts, and epics inventory; drift is meaning, phase, and unapplied change control.

## Reviewer files

- `review-rubric.md`
- `review-adversarial-general.md`
- `review-product-brief.md`
- `review-downstream-drift.md`
