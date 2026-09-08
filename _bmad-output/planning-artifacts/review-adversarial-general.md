# Adversarial Review — Hexalith.Memories

## Verdict

Refuse to sign. The 2026-09-05 Update did real work: greenfield is gone, isolation is no longer the first budget cut, FR13 no longer authorizes opposite recovery products, MCP is not an MVP accordion, and mechanism theater was parked. What it did *not* do is make the remaining words expensive. Today's PRD still cannot fail its thesis (N=5–10, ΔNDCG an open question, persona table still scoring vs single-axis), still assigns causal FRs to MVP while forbidding the edges those FRs need until Phase 1.5, and still has no release veto — thesis "kill" then a four-week 1.5 commitment, launch slip only delays surfaces, front matter stays `status: draft`. A chain-top reviewer can stamp either "thesis passed" or "thesis waived" without the document catching the lie.

## Findings

### critical The thesis kill switch is still unfalsifiable after the protocol rewrite

- **Location:** § Executive Summary; § User Success (LLM Agent); § Measurable Outcomes (Three-Axis Kill Switch); § MVP Strategy & Philosophy; FR25; Open Questions 1–2; Assumptions Index
- **Trigger:** Wording changed; the defect remains. Today's phrases: "outperform BM25+semantic on 80%+ of the benchmark protocol"; "80% is the hard line, not a stretch goal"; "N remains 5–10 topics until Epic 26 (or successor) expands N; the protocol is honest about statistical weakness at that N"; "pre-registered minimum ΔNDCG@10 in the benchmark README (architecture may own the number; the PRD requires that it exist)"; "Inter-rater agreement ≥80% (name the statistic in the benchmark README)"; "Dispute resolution: Human review where automated score and reviewer judgment diverge." The same document still says the opposite control in three other places: "Three-axis outperforms single-axis on 80%+ of benchmarks"; "proves hybrid retrieval outperforms single-axis"; FR25 "hybrid vs single-axis search results." Open Question 1 asks who will expand N. Open Question 2 asks someone to register the delta the hard gate already depends on.
- **Consequence:** The Update removed the "queries that require all three axes" construction and the A/B-without-labels escape. It replaced them with a gate that *admits* it is statistically weak, then still calls 80% a hard line. At N=10, 80% is eight anecdotes; at N=5 it is four. "80% of the benchmark protocol" does not say topics, queries, or judged pairs. The minimum ΔNDCG is not in the PRD — it is an unanswered question. The inter-rater statistic is not in the PRD. Humans may override NDCG when they "diverge." The persona table and MVP philosophy still score vs single-axis, so a run that beats BM25 and vectors separately and loses to BM25+semantic can be sold as a pass. You will rubber-stamp or panic-kill on a lab notebook the authors reserved the right to finish later. A hard gate that parks its own Δ and N in Open Questions is not a gate.
- **Fix:** Put the missing numbers in this PRD, not in a future README. Name N (order of 50+ topics, not 5–10), the ΔNDCG@10, the agreement statistic, and the unit of the 80% (topics, not vibes). Delete the human-override dispute line or confine it to label QA before lock. Make User Success, MVP philosophy, and FR25 say the same control as Measurable Outcomes: hybrid vs BM25+semantic; single-axis is diagnostic only. Until those edits land, retitle the 80% line "diagnostic, not a ship gate."

### critical Causal intelligence is MVP in the register and forbidden until Phase 1.5 everywhere else

- **Location:** § Canonical phase register; § Causal Intelligence (FR46–FR52); § Edge Type Taxonomy (MVP minimum); § MVP Feature Set graph inventory; § Non-Goals; Glossary EventStore senses
- **Trigger:** The Update split EventStore "domain truth" vs "product integration" and wrote: "Typed causal edges `caused_by` / `correlated_with` populate from EventStore product integration (Phase 1.5) or explicit annotation" and "do not claim causal three-axis on a folder tree." The same Update left this intact: "MVP (thesis + foundation): FR1–FR22, FR24–FR52" — which includes FR46 "index CausationId and CorrelationId from events," FR47–FR49 traversal/gaps, and FR50–FR52 typed edges with chronology. The taxonomy headed **"Edge Type Taxonomy (MVP minimum)"** still sources `caused_by` from "Explicit CausationId from EventStore" at default confidence 1.0. Phase 1.5 owns only FR59–FR62. FR46 has no phase tag on the line.
- **Consequence:** This is not a wording leftover. It is two ship contracts stapled together. A story owner can mark FR46–FR52 done in thesis MVP by storing a `contains` tree and calling it a graph, or fail them as "blocked on Epic 9," or invent annotation-time causal edges to make the hybrid benchmark look three-axis. Testers have no single expected inventory. The thesis hard gate still requires hybrid (available axes, including graph) to beat BM25+semantic on a "Phase 1 corpus (files/URLs/cases)." If the graph is case membership, hybrid is folder-reranked BM25+vector. The PRD already knows that ("do not claim causal three-axis on a folder tree") and still uses three-axis hybrid as the thesis gate. Implementers will p-hack an edge story. That is the same defect as the pre-Update "Phase 1 has no honest graph" finding; the assumption paragraph documents the hole instead of closing it.
- **Fix:** Move FR46–FR52 (or at least FR46–FR49 and EventStore-sourced rows of FR50) to the Phase 1.5 register next to FR59–FR61. Retitle the taxonomy "full product minimum" and add a closed Phase 1 edge list: `contains` plus optional explicit `references` only. If thesis MVP has no causal edges, the thesis comparison is hybrid-of-available vs BM25+semantic *and* the README/exec thesis sentence may not say three-axis causal intelligence. Do not leave FR46 in MVP "from events" while event integration is a non-goal.

### critical There is no product no-go — only delay, draft, and gates that cannot fail

- **Location:** Front matter `status: draft`; § Phase 1 thesis go/no-go; § Phase 1.5 launch go/no-go; § Phase 1.5 — Fast-Follow; § Measurable Outcomes kill-switch actions; FR59–FR62 vs published package table
- **Trigger:** "Soft gate … Must pass"; "All 3 hard gates must pass. Both soft gates must pass." Launch: "MCP end-to-end: agent task on held-out queries within token budget"; "Causal chain completeness ≥95% on known CausationId/CorrelationId chains"; "Phase 1.5 slip **delays those surfaces**." Sequencing: "committed: within 4 weeks of thesis validation." Kill switch: "change README positioning *before* Phase 1.5 MCP/EventStore product expansion; re-scope and re-estimate." Published inventory already lists `Hexalith.Memories.Mcp` and `Hexalith.Memories.EventStore`. YAML still says `status: draft` after `step-12-complete`.
- **Consequence:** Soft means nothing if both soft gates must pass — unless someone later waives them because they were labeled soft. That is a hidden escape. Thesis failure does not halt launch work; it edits the README and then the four-week 1.5 clock still starts. Launch failure is defined as delay, not stop. MCP and EventStore package IDs are already in the product inventory, so "before Phase 1.5 expansion" is unfalsifiable: expansion already has a row. "Held-out queries" has no hold-out set, no N, no scoring rule. "Known" causal chains are whatever the authors later call known. A draft that refuses to be the contract cannot be the launch instrument. Combined with the unfalsifiable 80% and 95% lines, every outcome is pre-authorized: pass, waive, delay, or keep shipping packages.
- **Fix:** Pick one release decision and date it. Either (a) thesis is a research checkpoint with no NuGet/README launch, and launch is Phase 1.5 with a real stop rule, or (b) launch is thesis CLI and the 1.5 table is not a go/no-go. Delete "soft" or delete "both soft gates must pass." Define held-out (frozen set, owner, scorer) and define "known chains" (frozen EventStore fixtures, not curator-selected souvenirs). If Mcp/EventStore packages already exist, say whether 1.5 is remaining work or already in-flight and drop the "4 weeks after thesis" fiction. Set `status` to the contract you want signed, or stop calling this a go/no-go.

### high The opening contract still sells the Phase 1.5 product as the product

- **Location:** § Executive Summary (lead paragraphs); § What Makes This Special; § Market Context (falsifiable claim); Journey 1 climax
- **Trigger:** Prior "no competitor offers this" table work was mostly done. The lead paragraph was not: "answers 'why did this happen?' and 'how are these connected?' — questions every team asks and no existing tool can answer"; "sourced narrative walking the causal chain from the original incident, through the team discussion, to the architecture decision record. Not just documents — the *story* of how they connect." Two paragraphs later: "Phase 1 proves hybrid retrieval plus non-retrofittable isolation. The sequenced bet — queryable causality … is Phase 1.5." Competitive table now admits GraphRAG/Neo4j/LlamaIndex as closest "why" competitors and says "not a surveyed uniqueness proof." Journey 1 still climaxes at 14 minutes to a CausationId chain.
- **Consequence:** The Update added a honesty paragraph without rewriting the sentence that decision-makers will quote. Sign this PRD and you have signed "no existing tool can answer why" as the product, while the thesis go/no-go explicitly does **not** require causal completeness, MCP, or EventStore auto-index. Open-source evaluators and EventStore users will judge a README that the Non-Goals list scheduled not to be true yet. The competitive table cannot save a lead that contradicts it. This is the old "MVP proves a retrieval toy" finding with the accordion removed and the brochure left in place.
- **Fix:** Rewrite the first two executive paragraphs to the Phase 1 contract: CLI hybrid search on file/URL ingest, case containers, tenant isolation. Move the causal narrative, "why did this happen?", and Journey 1 14-minute miracle to a Phase 1.5 launch abstract. Replace "no existing tool can answer" with the table's actual claim: documented RRF on a DAPR/EventStore causal graph after that integration ships.

### high Brownfield was declared; shipped vs remaining was not

- **Location:** § 0. Document Purpose; § Project Classification; § Resource Requirements; § Functional Requirements preamble; FR71 only
- **Trigger:** Prior critical "Greenfield" is fixed: "brownfield / change-controlled"; "This file does not re-estimate the backlog." The required companion edit did not happen. FR1–FR74 remain an unstatused inventory: "product-horizon inventory, not a claim that every FR is active thesis-MVP scope." The only completion note is FR71 "Completed early as non-MVP (Story 8.3)." No shipped / partial / not-started on any other FR or NFR. Remaining work is waved at `epics.md` and `sprint-status.yaml`.
- **Consequence:** A chain-top PRD that will not mark must-keep vs already-shipped vs still-owed will be used both ways: "don't touch it, it's specified" and "we can slip it, epics own the truth." Reviewers cannot audit drift from this file. Seventy-four FRs plus a phase register without evidence is still an encyclopedia, just a brownfield-labeled one. Signing "FR1–FR74" as the outcome contract is signing a fog bank.
- **Fix:** Add a status on every FR/NFR that this PRD is willing to be sued over: `shipped` (evidence: story or package version), `partial`, `not started`. If that table would live in epics, then this PRD may not pretend FR1–FR74 is the acceptance surface — cut the horizon list to the active ship contract only.

### high Isolation is principals in the journey, IDs in the gate, and "physical" in compliance

- **Location:** § Compliance Boundary; Journey 5; FR38; NFR8; CLI Specification; addendum Isolation mechanism (does not override)
- **Trigger:** Prior "physical isolation on shared Redis" was rewritten in Journey 5 and FR38: "tenant-scoped Redis/FalkorDB resources (indexes plus backend principals)"; "the security boundary is tenant-scoped principals, not index names alone." Addendum: "shared cluster, tenant-scoped principals and indexes." Compliance still says: "**Physical tenant isolation** ensures no cross-tenant data leakage, a prerequisite for downstream compliance." NFR8 verification is still "search, ingest, graph across all axes with malformed/empty/swapped tenant IDs." Journey 5 failure beat uses `memories search "test" --tenant bu-operations`. MVP CLI search flags are `<query>, --case, --explain, --axes, --format` — no `--tenant`. Command table still lists `tenant switch`.
- **Consequence:** The Update moved mechanism to the addendum and then left the compliance chapter selling physical isolation as a compliance prerequisite. NFR8 does not test the stated boundary (principals/ACLs); it tests ID swapping at the API. If tenant claims authorize, a swapped `--tenant` flag is an auth bug or a flag that should not exist, not an isolation victory. Shared embedding keys remain an admitted noisy-neighbor channel and are still not on the go/no-go. Operators who read "physical" and "prerequisite for downstream compliance" will believe a tier the addendum already downgraded. Testers will green NFR8 without ever proving a stolen or confused principal cannot read another tenant's index.
- **Fix:** Strike "Physical tenant isolation" in Compliance; say shared-cluster, tenant-scoped principals and indexes, API fail-closed (the addendum tier). Rewrite NFR8 verification to the outcome: authenticated principal A cannot search/ingest/traverse tenant B, including identical graph shapes and colliding edge IDs — not "swapped tenant IDs." Remove `--tenant` and `tenant switch` from MVP journeys/CLI or add them to the MVP command list and to NFR11. Put shared-key residual risk on the isolation hard gate as accepted debt or a fail.

### high The launch 30-minute clock starts at a package the inventory does not publish

- **Location:** § Executive Summary (Phase 1.5 onboarding); § User Success (Alex launch); Phase 1.5 hard gate; Journey 1; § Language & Platform Matrix; § Published packages; NFR31
- **Trigger:** Thesis clock was split — NFR31 is now "AppHost → first CLI search." The launch clock is: "under 30 minutes from `dotnet add package` plus DAPR subscription to first search on auto-indexed events"; hard gate copies that phrase. Journey 1: "`dotnet add package Hexalith.Memories.Client`"; "`docker compose up` for Redis + FalkorDB"; climax "It's been 14 minutes." Published IDs: `Client.Rest`, `EventStore` — not `Hexalith.Memories.Client`. Client libraries: "reusable `.NET` client packages are not MVP blockers" and `Client` is in the Future column. NFR9 still requires OpenBao-backed secrets. Embedding MVP is Google `text-embedding-004`.
- **Consequence:** The Update fixed the old lie (timing EventStore in Phase 1). It created a new one: the launch hard gate is a stopwatch on an unnamed or unpublished package, a compose path that is not the AppHost path NFR31 uses, and a 14-minute story that omits the Google key and OpenBao. You will pass the gate on a pre-warmed machine that already has `Client` as a project reference, or fail it because the documented command does not restore. Alex's beachhead aha is not an executable procedure.
- **Fix:** Name the exact package ID from `tools/release-packages.json` in the Phase 1.5 gate and in Journey 1. Write the launch stopwatch as a numbered script: machine state, required secrets, embedder, excluded steps, pass/fail. If `Hexalith.Memories.Client` is not a published ID, delete it from the journey. If Google/OpenBao are required, they are on the clock — or the launch path must include a documented no-network embedder so the gate is not "did the reviewer already have GCP."

### high Phase 1 still gates "three-axis" hybrid on a graph the PRD forbids you to call three-axis

- **Location:** § Executive Summary thesis sentence; § MVP Feature Set #3 and graph inventory; FR16–FR17; NFR24; Assumptions Index
- **Trigger:** Distinct from the FR-register fork: even if you ignore FR46, the thesis sentence is still "This three-axis approach is the core thesis" and Feature #3 is "Hybrid Search (syntactic, semantic, graph …) | Core hypothesis." The assumption: "Phase 1 graph axis may run on `contains` / case-scoped edges." Then: "Hybrid fuses *available* axes; do not claim causal three-axis on a folder tree."
- **Consequence:** Available-axis fusion is a real product rule (FR66). Using it as the *thesis* lets a two-and-a-half-axis system pass a three-axis kill switch. Graph proximity over `contains` is "same case," which `--case` already filters. If graph adds no ranking value, the honest result is "BM25+semantic won; graph-in-fusion died." The current wording lets graph ride along as "available," contribute noise or case-membership rerank, and still count toward "hybrid vs BM25+semantic." The Update documented the folder-tree problem and kept the three-axis gate anyway.
- **Fix:** Either put typed non-hierarchy edges in Phase 1 (explicit `references` with examples, or a frozen annotation corpus) and keep the three-axis thesis, or drop graph from the thesis comparison and retitle Phase 1 "BM25+semantic plus isolation." Do not keep both "do not claim causal three-axis" and "three-axis is the core thesis."

### medium "Soft" is a waiver costume on gates that already say must-pass

- **Location:** § Phase 1 thesis go/no-go table
- **Trigger:** Two rows labeled **Soft gate** with Requirement **"Must pass"**; summary sentence "Both soft gates must pass."
- **Consequence:** Readers will treat case-scope and deterministic explain as optional when schedule bites, because the word soft is sitting there. Other readers will treat them as hard, because the table says must pass. The Update kept the adjective after making the content mandatory. That is how a later SCP "waives a soft gate" without an MVP rebaseline.
- **Fix:** Promote those two rows to Hard gate, or give Soft a real meaning (e.g. "fail → launch with documented debt, not a thesis pass") and stop saying must pass.

### medium NFR21 already declares the DAPR-generic experiment won

- **Location:** NFR21; § Executive Summary assumption; § Innovation Event Memory; § Validation Approach DAPR-generic kill switch; § Risk Mitigation
- **Trigger:** NFR21 (P1.5): "events from any DAPR-compatible publisher are processable" / "Integration test with standard CloudEvents payloads." Executive Summary: "Non-EventStore DAPR publishers are a later adapter path"; "generic Marten/Wolverine/Axon zero-code remains an experiment until a named … spike passes the DAPR-generic kill switch." Kill switch: "If integration requires custom code beyond DAPR subscription config, the pattern isn't generic." Journey 2 already requires handler registration on schema change — "expected," not zero-config.
- **Consequence:** Phase 1.5 integration success can be claimed by parsing a CloudEvent envelope (NFR21) while the beachhead promise is EventStore conventions only. Or the generic kill switch can be declared already failed because handlers are custom code, while NFR21 still sits on the launch surface. Two opposite product claims, one phase tag.
- **Fix:** Narrow NFR21 to EventStore-convention CloudEvents on the Memories Server subscription path. Keep generic publishers in Open Questions / the named spike. Do not let a P1.5 NFR say "any DAPR-compatible publisher" until that spike passes.

### medium FR67 still sells audit; the glossary forbids that word

- **Location:** FR67; Glossary Access telemetry; NFR34; Journey 5
- **Trigger:** FR67: "logs search and access events per tenant for **audit purposes**." Glossary: "Not a tamper-evident audit trail." NFR34: "remains infrastructure telemetry, not a tamper-evident compliance audit trail." Journey 5 is careful; FR67 is not.
- **Consequence:** A chain-top "FR67 done" will be demoed as audit. Compliance readers will believe the product grew a trail the Non-Goals list excluded. The Update cleaned journeys and NFR34 and left the FR title-word that does the damage.
- **Fix:** Rewrite FR67 to the glossary noun: access telemetry for operator visibility, not audit. If a future audit product exists, it is a new FR with integrity/retention semantics, not this one.

### medium Case members are MVP capabilities that must not authorize, with no other outcome

- **Location:** FR28–FR29, FR36; Glossary Member; Journey 4; § Memory unit provenance
- **Trigger:** "Developer can add members to a case" / "remove members" are in the MVP register. Glossary: "Does not grant authorization in the current phase." Journey 4: "`memories case add-member --case project-alpha --user tomas`" is Phase 2 narrative but the commands sit in the unphased CLI table. Provenance binds to authenticated `sub` — good, and it does not explain what a member *does* in thesis MVP.
- **Consequence:** The Update fixed the missing-principal hole for ingest. It left membership as a write-only souvenir: a string on a case that journeys treat as access (`creates Tomás's access`) and the glossary treats as inert. Implementers will either wire members into authorization (contradicts glossary) or ship dead commands that look like ACL. Testers cannot fail a story: any write succeeds.
- **Fix:** Pull FR28–FR29–member activity out of thesis MVP, or state the Phase 1 outcome in one sentence (e.g. membership is listed metadata only; every access check ignores it; CLI says so). Delete "creates access" from Journey 4 or keep that journey Phase 2 and off the MVP CLI table.

### medium The score table still documents a magnitude graph axis the fusion decision rejected

- **Location:** § Confidence Score Semantics table (Graph score row); NFR24; addendum Fusion
- **Trigger:** Graph row: "Proximity in the relationship graph (hop distance, edge weight)" / "Inverse hop distance with decay function" / range 0.0–1.0. Immediately below: "Hybrid per-axis scores are rank-contribution scores, not raw BM25, cosine, or graph-proximity magnitudes." Addendum: magnitude-blend is a rejected alternative. NFR24: weighted RRF.
- **Consequence:** `--explain` and Evidence Packet consumers have two graph contracts. A story can ship hop-distance 0.0–1.0 and call NFR24 done, or ship RRF rank contribution and leave the table lying. Priya's 0.95 "for the causal chain" (Journey 8, heading still unphased) will be read against whichever column is convenient. The Update split relevance vs metadata vs edge in the glossary and left this row as blend-era residue.
- **Fix:** Make the graph row match NFR24: hybrid graph contribution is RRF rank contribution; hop-distance may exist only as a single-axis `axis=graph` explain. Move Journey 8's 0.95 beat to Phase 2 and stop implying factual warranty.

### medium File ingest has no freshness outcome; "required active" projections are an accordion

- **Location:** FR6, FR13; NFR6; NFR18; § Async Ingestion Pipeline
- **Trigger:** NFR6 freshness is P1.5 events only. FR6: "`indexed` … only after every **required active** projection acknowledges the same EventStore source version." FR13: "not searchable as complete until all required projections ack." No MVP T for "file ingest → searchable on available axes."
- **Consequence:** Thesis onboarding (NFR31) can pass on a unit that is `projecting` forever if embeddings starve, or pass because graph was declared not "required active" for Phase 1. "Required active" is how FR6/FR13 become unfalsifiable after the atomic-write slogan was removed. NFR18 degraded reads and FR13 incomplete ingest can both be "correct" for the same missing FalkorDB write.
- **Fix:** Add an MVP freshness outcome for file/URL ingest (searchable on each required axis within T, or `status` shows `embedding`/`projecting` and NFR31 may not complete until `indexed`). Define the Phase 1 required projection set as a closed list, not "active."

### medium The CLI contract is two lists; only one is cut to MVP

- **Location:** § Interface Capability Parity Matrix; § CLI Specification (MVP command scope vs Command Structure table)
- **Trigger:** "MVP command scope: `ingest`, `search --explain`, `case create/delete`, `tenant create/delete/verify`, `status`." The table below still lists `explore`, `traverse`, `case add-member/activity/list`, `tenant switch/list`, `handlers`, `quickstart` with no phase column. Parity matrix same.
- **Consequence:** FR53 says the phase register wins and `NotImplementedCommand` is not coverage. The table will still be copied into stories as the command set. That is how you get fake verbs and a "CLI complete" stamp.
- **Fix:** Split the command table into MVP vs 1.5 vs later, or delete non-MVP rows from this PRD and leave them to epics.

### low Journey 8 still trains readers to treat 0.95 as a reason to relax

- **Location:** Journey 8 verification beat; § AI Reliability critical distinction
- **Trigger:** "Confidence scores show 0.95 for the causal chain. … Her stomach unknots." The chapter also says a score "does not mean the underlying data is complete, correct, or current." Journey 8's heading has no phase tag; the summary says Phase 2.
- **Consequence:** The distinction exists as a disclaimer and loses to the story. Applications will copy the beat.
- **Fix:** Tag Journey 8 Phase 2 on the heading. Make the 0.95 explicitly relevance-to-query, and keep the unknotting beat on source documents she read, not on the number.

### low Front matter calls the PRD complete and draft in the same breath

- **Location:** YAML `status: draft`; `stepsCompleted: … 'step-12-complete'`; § 0. Document Purpose
- **Trigger:** Workflow is marked complete; status remains draft; body claims this is the product-outcome contract.
- **Consequence:** Change control can treat every amendment as "still drafting." A go/no-go instrument that will not leave draft is a memo.
- **Fix:** Set `status` to the actual control state (`in-force`, `change-controlled`, or equivalent) or stop claiming step-12 complete.

## Dropped from the 2026-09-05 review (actually closed)

- Greenfield classification vs published packages.
- Isolation/cases as the first resource cut.
- FR13 "rollback or retry" as two products.
- Pulling MCP into thesis MVP if 1.5 slips.
- Exec-summary "zero mapping / zero configuration" as the happy-path claim (retracted in-place; Journey 1 package path remains, see above).
- Kill-switch wording that only said "re-evaluate" with no named sunk-cost actions (actions are named; they still do not stop launch — see critical go/no-go).
- License "recommended" (now a decision).
- Persona 200ms cached vs NFR latency split.
- Interpretation-layer "0.8 ≈ 80% reliability" sentence.
