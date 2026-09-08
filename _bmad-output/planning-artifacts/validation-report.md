# Validation Report — Hexalith.Memories

- **PRD:** `_bmad-output/planning-artifacts/prd.md`
- **Rubric:** `.agents/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-09-08T12:22:44+02:00
- **Grade:** Poor

## Overall verdict

The 2026-09-05 Update did the work the last Poor pass demanded: Phase 1 and Phase 1.5 are two clocks, weighted RRF is the fusion decision, isolation is not a resource fallback, a Glossary and Non-Goals section exist, and open tensions are no longer edited into silence. What is still unsafe for chain-top extract is increment *inside* the thesis: the canonical FR phase register parks Causal Intelligence (FR46–FR52) and NFR4 traversal in MVP, the CLI thesis surface does not, NFR16 still treats Redis AOF as the durability contract after FR13 made EventStore the durable commit, and the Phase 1.5 onboarding gate names a NuGet ID that is not in the published inventory.

Adversarial review refuses to sign: the thesis kill switch still cannot fail (N=5–10, ΔNDCG an open question), causal FRs are MVP and Phase 1.5 at once, and there is no product no-go — only delay, draft, and gates that cannot fail. Product-brief reconciliation says the dual split is real in the gate tables, but identity and FR placement still leak the brief’s v1 causal/agent hero into thesis language. Downstream drift reversed: the August SCP PRD amendments landed; architecture overview/coverage and the epics inventory still describe the pre-Update PRD.

## Dimension verdicts
- Decision-readiness — adequate
- Substance over theater — adequate
- Strategic coherence — adequate
- Done-ness clarity — thin
- Scope honesty — adequate
- Downstream usability — thin
- Shape fit — adequate

## Findings by severity

### Critical (3)

**[Adversarial]** — The thesis kill switch is still unfalsifiable after the protocol rewrite (§ Measurable Outcomes; Open Questions 1–2; FR25)

Wording changed; the defect remains. 80% is a hard line at N=5–10, ΔNDCG and the inter-rater statistic live in Open Questions / a future README, humans may override NDCG, and User Success / FR25 still score vs single-axis.

Fix: Put N (order of 50+ topics), ΔNDCG@10, the agreement statistic, and the unit of the 80% in this PRD. Make User Success, MVP philosophy, and FR25 say hybrid vs BM25+semantic. Until then, retitle 80% as diagnostic, not a ship gate.

**[Adversarial]** — Causal intelligence is MVP in the register and forbidden until Phase 1.5 everywhere else (§ Canonical phase register vs § Non-Goals vs FR46–FR52 vs FR59–FR62)

The register is `FR1–FR22, FR24–FR52`. That includes CausationId indexing and typed causal edges. Non-Goals and the graph assumption forbid claiming causal three-axis on a folder tree. Rubric and product-brief name the same fork.

Fix: Move FR46–FR52 (or FR46–FR49 and EventStore-sourced FR50 rows) to the Phase 1.5 register. Closed Phase 1 edge list: `contains` plus optional explicit `references` only.

**[Adversarial]** — There is no product no-go — only delay, draft, and gates that cannot fail (front matter `status: draft`; both go/no-go tables; Phase 1.5 slip)

Soft gates also say must-pass. Thesis failure edits the README then the four-week 1.5 clock still starts. Launch failure is delay. MCP and EventStore package IDs already exist. YAML stays `draft` after `step-12-complete`.

Fix: Pick one release decision and date it. Delete “soft” or delete “both soft gates must pass.” Define held-out queries and known chains. Set `status` to the contract you want signed.

### High (12)

**[Decision-readiness]** — Generic DAPR is both an experiment and a P1.5 NFR (§ Executive Summary vs NFR21)

Exec summary: non-EventStore DAPR publishers are a later adapter path. NFR21: events from any DAPR-compatible publisher are processable.

Fix: Rewrite NFR21 as EventStore-convention envelope conformance; keep generic publishers behind the named spike.

**[Done-ness]** — CLI thesis surface does not implement the MVP FRs and NFR4 (§ CLI Specification vs FR28–FR29, FR36, FR47, NFR4)

Phase 1 essentials omit `traverse`, `add-member`, and `activity` while those FRs and NFR4 are MVP.

Fix: Add the verbs to the Phase 1 command list with acceptance output, or move those FRs/NFR4 to Phase 1.5.

**[Done-ness]** — Restart durability is two products (§ FR13 vs NFR16)

FR13: EventStore ack is durable commit; projections rebuild. NFR16: zero loss during Redis restart via AOF.

Fix: After Redis restart, every EventStore-committed unit is searchable again via rebuild or verified AOF; name the recovery command and time bound.

**[Downstream usability]** — Launch onboarding package ID does not exist in the published inventory (§ Journey 1 vs Package Distribution)

Journey 1: `dotnet add package Hexalith.Memories.Client`. Published IDs: `Client.Rest`, `EventStore`. Adversarial repeats this as a launch-clock defect.

Fix: Name an ID from `tools/release-packages.json` in the Phase 1.5 gate and Journey 1. Write the launch stopwatch as a numbered script including secrets/embedder.

**[Adversarial]** — The opening contract still sells the Phase 1.5 product as the product (§ Executive Summary; Journey 1 climax)

“No existing tool can answer why” and the causal-chain hero remain the lead; discussions are a Non-Goal; causal completeness is a 1.5 gate. Product-brief flags the same leak plus Innovation #1.

Fix: Lead with file/URL hybrid search plus isolation. Move the causal narrative and Journey 1 14-minute miracle to a Phase 1.5 abstract.

**[Adversarial]** — Brownfield was declared; shipped vs remaining was not (§ Functional Requirements preamble)

FR1–FR74 remain an unstatused horizon inventory. Only FR71 notes early completion.

Fix: Status every FR/NFR this PRD will be sued over (`shipped` / `partial` / `not started`), or cut the horizon list to the active ship contract.

**[Adversarial]** — Isolation is principals in the journey, IDs in the gate, and “physical” in compliance (§ Compliance Boundary vs NFR8 vs Journey 5)

Compliance still says physical tenant isolation. NFR8 tests swapped tenant IDs. Journey 5 uses `--tenant`, which is not an MVP search flag.

Fix: Strike “Physical”; rewrite NFR8 to principal A cannot read tenant B. Remove `--tenant` / `tenant switch` or add them to MVP CLI and NFR11.

**[Adversarial]** — Phase 1 still gates “three-axis” hybrid on a graph the PRD forbids you to call three-axis (§ Executive Summary thesis vs graph assumption)

Available-axis fusion lets a folder-tree graph pass a three-axis kill switch.

Fix: Put typed non-hierarchy edges in Phase 1, or drop graph from the thesis comparison and retitle Phase 1 “BM25+semantic plus isolation.”

**[Downstream-drift]** — Architecture overview still extracts the pre-2026-09-05 PRD (`architecture.md` Requirements Overview / Coverage / PRD Deviations)

C# 13, 31 NFRs, P1.5 auth, actor-pipeline, and deleted atomic-write sentences still sit in the overview tables.

Fix: Re-extract those tables from today’s PRD + addendum. Do not edit the PRD to match the fossils.

**[Downstream-drift]** — Epics Requirements Inventory is a stale second PRD (`epics.md` inventory)

Auth still `[P1.5]`, actor pipeline, indexes as isolation boundary, NFR32–NFR35 missing, single onboarding clock.

Fix: Replace inventory bullets with pointers to the PRD, or refresh them to current wording.

**[Done-ness]** — The thesis kill switch is missing its own Δ (§ Measurable Outcomes vs Open Questions › 2)

Folded with the critical kill-switch finding; listed here as the rubric’s independent high on the same hole.

Fix: Put the minimum Δ (or an explicit “any positive ΔNDCG@10 counts” decision) in this PRD.

**[Downstream usability]** — The phase register cannot be extracted alone (§ Canonical phase register)

Same defect as the critical causal-register finding; rubric high on extract safety.

Fix: Make the register the only increment SoT; every FR in FR46–FR52 and every thesis CLI verb must match it.

### Medium (17)

**[Decision-readiness]** — FR32 is still reversible (§ Open Questions › 5 vs FR32)

**[Substance]** — Success Criteria still perform late-phase people as current users (§ User Success vs Journeys 4, 6, 8, 10)

**[Strategic coherence]** — The hero paragraph is still the launch product (§ Executive Summary)

**[Done-ness]** — Ingestion “done” uses three state vocabularies (§ Async Ingestion Pipeline vs FR10 vs FR31)

**[Done-ness]** — `axis=nl` reads as a Phase 1 retrieval axis (§ score table vs Glossary vs FR60)

**[Scope honesty]** — Causal edges are scoped in an assumption and a Non-Goal, not on the FRs that ship them

**[Downstream usability]** — CLI and audit nouns still collide (§ Journey 5 vs CLI vs FR67 vs Glossary)

**[Shape fit]** — User-journey density is still consumer-product shaped (§ User Journeys)

**[Adversarial]** — “Soft” is a waiver costume on gates that already say must-pass

**[Adversarial]** — NFR21 already declares the DAPR-generic experiment won

**[Adversarial]** — FR67 still sells audit; the glossary forbids that word

**[Adversarial]** — Case members are MVP capabilities that must not authorize, with no other outcome

**[Adversarial]** — The score table still documents a magnitude graph axis the fusion decision rejected

**[Adversarial]** — File ingest has no freshness outcome; “required active” projections are an accordion

**[Adversarial]** — The CLI contract is two lists; only one is cut to MVP

**[Downstream-drift]** — CLI `status` / FR53 phase still disagree across spines

**[Downstream-drift]** — Onboarding boot path and second clock are not shared (AppHost vs `docker compose`; epics NFR31 has one clock)

**[Downstream-drift]** — Ingestion state vocabulary is still three-way (PRD vs architecture field inventory vs epics FR10 vs UX-DR22)

**[Product-brief]** — Silent drops: custom extraction phrases; 6-month second embedding provider; “all DAPR state stores”; 5x productivity; Why Now / 12–18 month window; Priya’s search-success metric; motivating benchmark scene; cross-case insight discovery

**[Product-brief]** — Expansions still sitting in `prd.md` that addendum said must leave the contract: Aspire-mandatory topology, OpenBao as NFR, package inventory tables, interpretive-compliance program

### Low (6)

**[Adversarial]** — Journey 8 still trains readers to treat 0.95 as a reason to relax

**[Adversarial]** — Front matter calls the PRD complete and draft in the same breath

**[Downstream-drift]** — Epic 9 still markets “zero-code” against the PRD glossary

**[Downstream-drift]** — `--explain` vs UX-DR7 remains an open product pick (Open Question 3)

**[Downstream-drift]** — NFR11 is a current product invariant scheduled as post-MVP remediation (Epic 20)

**[Downstream-drift]** — Post-Update 27.4 live-producer contract must stay out of the PRD

## Mechanical notes
- Glossary, addendum, and Assumptions Index exist; the 2026-09-05 “no Glossary / no addendum” note does not hold.
- Assumptions Index roundtrip: four of five `[ASSUMPTION]` tags appear inline. The NFR33/NFR35 collision entry is index-only.
- FR1–FR74 and NFR1–NFR35 are unique and contiguous.
- Soft gates in both go/no-go tables are labeled **Soft gate** and **Must pass** — naming drift, not a second ship contract.
- `memories tenant switch` remains in the Command Structure table and not in MVP command scope.
- FR63 still says “composite confidence scores”; Glossary term is **Relevance confidence**.
- Evidence Packet concrete shape remains architecture-owned; that is now an intentional SoT split.

## Reviewer files
- `review-rubric.md`
- `review-adversarial-general.md`
- `review-product-brief.md`
- `review-downstream-drift.md`
