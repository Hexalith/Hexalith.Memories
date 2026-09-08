# Reconciliation — validation-report.md (2026-09-08T12:22) vs prd.md / addendum.md (Updated 2026-09-08)

- **Inputs:** `validation-report.md` (run against the pre-Update PRD), `prd.md` (1202 lines, `updated: 2026-09-08`), `addendum.md` (`Updated: 2026-09-08`), `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` (to confirm the CLI delivery-status column).
- **Line numbers** refer to the current `prd.md` (`L…`) or `addendum.md` (`A…`).
- **Settled decisions (not re-litigated):** FR46–FR52 + NFR4 stay MVP with the Phase 1 population rule and L3 as the causal-completeness gate; N=8 is a diagnostic and G1 is a protocol not yet run; dated Release decision record with failure = no-go; FR/NFR status registers; `--tenant` stays, `tenant switch` removed; NFR9/package tables/Aspire stay; architecture.md / epics.md drift is out of scope (handoff).
- **Disposition rule:** *resolved* = the text a downstream reader extracts no longer carries the defect; *partial* = the defect is fixed in the primary location but survives in at least one extractable location; *overridden* = a settled decision consciously rejects the reviewer's fix (with mitigation where recorded); *unaddressed* = no change and no recorded decision.

## 1. Summary counts

| Disposition | Count |
|---|---|
| Resolved | 32 |
| Partial | 6 |
| Overridden (settled decision / explicit handoff) | 8 |
| Unaddressed | 2 |
| **Total findings reconciled** | **48** (3 Critical, 12 High, 20 Medium, 6 Low, 7 Mechanical) |

Note: the validation report's Medium header says "(17)" but enumerates 20 items; all 20 are reconciled below.

By severity: Critical 0 resolved / 2 partial / 1 overridden; High 10 resolved / 0 partial / 2 overridden; Medium 13 resolved / 3 partial / 3 overridden / 1 unaddressed; Low 2 resolved / 1 partial / 2 overridden / 1 unaddressed; Mechanical 7 resolved.

## 2. Table of every finding

### Critical

| Finding | Severity | Disposition | PRD location or reason |
|---|---|---|---|
| C1 — Thesis kill switch unfalsifiable (N=5–10, ΔNDCG open, no agreement stat, single-axis control) | Critical | **Partial** | Protocol numbers are now in the PRD: L156–L162 (N ≥ 50, BM25+semantic two-axis RRF control, ΔNDCG@10 ≥ 0.02, Cohen's κ ≥ 0.6, unit = topic); Glossary L94 "Thesis gate / diagnostic run"; User Success L120 "hybrid vs BM25+semantic … ΔNDCG@10 ≥ 0.02 — gate G1"; MVP philosophy L212 "outperforms BM25+semantic under the thesis-gate protocol"; N=8 demoted L164; G1 row L175. **Residual:** the fix explicitly asked for FR25 to say hybrid vs BM25+semantic; FR25 (L1005) still reads "automated benchmark comparisons of hybrid vs **single-axis** search results", while L158 says "Single-axis runs are diagnostics only." A reader extracting FR25 alone still gets the pre-Update benchmark contract. |
| C2 — Causal intelligence is MVP in the register and forbidden until Phase 1.5 elsewhere | Critical | **Overridden** (settled) | Fix (move FR46–FR52 to Phase 1.5) rejected; addendum A111. Mitigation makes the two spines agree: register L958–L960 "FR46–FR52 stay MVP as shipped graph mechanics"; Phase 1 graph inventory decision L239 (population = `contains`/`references`/`annotates` + `caused_by`/`correlated_with` only when metadata carries ids); Exec Summary L47 and L61; Graph section preamble L1035; FR46 reworded L1037 ("carried by ingested content metadata"); taxonomy Source column L580–L584 per phase. Non-Goals (L99–L108) no longer forbid the claim; the contradiction the reviewer named is gone, by decision rather than by the reviewer's fix. |
| C3 — No product no-go (soft gates that must pass; failure = delay/draft; `status: draft` after step-12) | Critical | **Partial** | Release decision record L198–L206 with dated decisions and "If it fails" = kill-switch actions / **No launch**; L206 "A Phase 1.5 launch failure is a no-go, not a slip"; L253 and L283 repeat; "soft" deleted — L181 "All five gates are hard gates; there is no soft tier"; held-out queries defined L187 (L1: "topics not used in G1 labelling, ≥10"); known chains named L169/L189 (`samples/02-eventstore-integration/` fixture). **Residual:** front matter L3 still `status: draft` while L6 lists `step-12-complete`; the fix "Set `status` to the contract you want signed" was not applied and no decision about it is recorded in the addendum. |

### High

| Finding | Severity | Disposition | PRD location or reason |
|---|---|---|---|
| H1 — Generic DAPR is both an experiment and a P1.5 NFR (Exec Summary vs NFR21) | High | Resolved | NFR21 L1140 rewritten as EventStore-convention envelope conformance; "processing them without custom code is the DAPR-generic *experiment* (Innovation #2), not a requirement"; Exec Summary L51 and Innovation #2 L624 unchanged and now consistent. |
| H2 — CLI thesis surface omits `traverse`, `add-member`, `activity` while FRs/NFR4 are MVP | High | Resolved | Phase 1 CLI surface table L866–L885 lists `traverse` (L872), `case add-member/remove-member/activity` (L875) as Phase 1 with acceptance output and honest "Not started — stub" status; MVP Feature #6 L236 names them; NFR4 stays MVP (settled). Stub status confirmed against `RootCommandFactory.cs` L84–L87, L128–L132, L159–L163. |
| H3 — Restart durability is two products (FR13 vs NFR16 AOF) | High | Resolved | NFR16 L1130: "either because the projections survived (Redis AOF) or because they were rebuilt from the EventStore commit. AOF is an optimisation, not the durability contract (FR13)"; names the recovery command (`consistency verify` → `consistency repair --yes`) and time bound (5 min for 10K units). FR13 L990 unchanged; Technical Success L149 says "no loss of EventStore-committed units across a Redis restart". See new-contradiction N4 on the "Verified" status claim. |
| H4 — Launch onboarding package ID (`Hexalith.Memories.Client`) not in inventory | High | Resolved | L49, L192 (stopwatch step 1) and Journey 1 L302 all use `Hexalith.Memories.EventStore`, which is in the published table L700; launch stopwatch written as a numbered script incl. secret and boot (L191–L196). `Hexalith.Memories.Client` appears only as a Future-column item (L681). |
| H5 — Opening contract sells the Phase 1.5 product | High | Resolved | Exec Summary now leads with file/URL ingest + hybrid + isolation (L45–L47); causal narrative moved under an explicit "Phase 1.5 abstract (launch, not thesis)" L51; Journey 1 titled/banner "(Phase 1.5 launch path)" L294–L296 and its climax tied to the L2 clock (L304). |
| H6 — Brownfield declared; shipped vs remaining not statused | High | Resolved | FR delivery status register L966–L974 (Shipped / hardening / Partial / early / Not started — covers FR1–FR74 without gaps); NFR delivery status L1086–L1094; FR53 L1047 carries "**Status:** partial". |
| H7 — Isolation is principals / IDs / "physical"; `--tenant`, `tenant switch` not MVP | High | Resolved | "Physical" no longer appears; Compliance L531 "shared-cluster isolation tier"; NFR8 L1112 rewritten around principals ("authenticate as tenant A, then … `--tenant B` … every call is rejected or returns only A's data"); Journey 5 L378 declares `--tenant` an MVP search flag authorised by NFR11 claims; L864 "There is no `tenant switch`"; L870 shows `--tenant` on `search query`. |
| H8 — Phase 1 gates "three-axis" on a graph the PRD forbids calling three-axis | High | Resolved (fix option A taken) | Typed non-hierarchy edges are in Phase 1: L239 and taxonomy L582–L584 (`references`, `annotates` — Phase 1), conditional causal edges L580–L581; L157 requires the G1 corpus to reflect the Phase 1 population; Innovation #1 L621 says Phase 1 tests the fusion, the marketed causal graph arrives in 1.5; Glossary L86 defines three-axis as fusion of available axes. |
| H9 — architecture.md overview extracts the pre-Update PRD | High | Overridden (handoff) | Out of scope per settled decision; addendum A118–A120 records the handoff ("Re-extract those sections from `prd.md` + this addendum via `bmad-correct-course`"). The PRD was not edited to match the fossils, as the fix demanded. |
| H10 — epics.md Requirements Inventory is a stale second PRD | High | Overridden (handoff) | Same as H9; A120 enumerates the stale items (auth `[P1.5]`, actor pipeline, indexes as isolation boundary, NFR32–NFR35 missing, single onboarding clock). |
| H11 — Kill switch missing its own Δ | High | Resolved | L158 "pre-registered **ΔNDCG@10 ≥ 0.02**"; repeated L120, L175, L237, L652. |
| H12 — Phase register cannot be extracted alone | High | Resolved | L958 "this register is the only increment source of truth"; FR46–FR52 in the MVP line L960 agree with L61/L239/L1035; Parity Matrix L838 defers phase/ship questions to the Phase 1 CLI surface table; FR53 L1047 points to the same table. (See N1/N2 for two minor drifts introduced elsewhere.) |

### Medium

| Finding | Severity | Disposition | PRD location or reason |
|---|---|---|---|
| M1 — FR32 still reversible (Open Q5) | Medium | Resolved | Open Q5 L1188: "FR32 stays absolute (single-case ownership) for MVP and Phase 1.5; brief R3 … may be restored only as a Phase 2 FR via sprint change." FR32 L1015 unchanged and absolute. |
| M2 — Success Criteria perform late-phase people as current users | Medium | Resolved | User Success table gained a Phase column L114–L126; Marcus rows L123–L124 "Narrative only until briefing ships; not a Phase 1 or 1.5 metric" / "2 (post-launch)". |
| M3 — Hero paragraph is still the launch product | Medium | Resolved | L45 opens with "The product this PRD ships first (Phase 1, thesis)"; causal hero demoted to L51 abstract. |
| M4 — Ingestion "done" uses three state vocabularies | Medium | Resolved | One vocabulary: Glossary L93, pipeline L809 ("the one vocabulary"), stage table L813–L820, FR10 L987, FR31 L1014, FR13 L990; the shipped `Contracts.V1` enum is declared a mapping, "not a third vocabulary" (L93), with rename tracked in Open Q8 L1191. |
| M5 — `axis=nl` reads as a Phase 1 retrieval axis | Medium | Resolved | Score table L549 "NL score (**Phase 1.5**, event units only) … Not a Phase 1 retrieval axis: file/URL units have no NL description embedding (FR60)"; Glossary L85. |
| M6 — Causal edges scoped in an assumption/Non-Goal, not on the FRs | Medium | Resolved | Graph section preamble L1035 states the phase rule on the FRs; FR46 L1037 reworded; taxonomy L576 header "the 'Source' column says which phase can populate each type"; Assumptions Index L1202 records the old assumption as resolved. |
| M7 — CLI and audit nouns collide | Medium | Resolved | Journey 5 L378/L382 "access telemetry"; FR67 L1067; NFR34 L1173; CLI `status telemetry` L878; Glossary L91. |
| M8 — User-journey density is consumer-product shaped | Medium | **Unaddressed** | All 10 journeys retained (L292–L512) at similar length; phase banners and the Coverage check L504–L512 were added, but no journey was cut or condensed and neither PRD nor addendum records a decision to keep the density. |
| M9 — "Soft" is a waiver costume | Medium | Resolved | L181; every G/L row reads "Must pass" (L175–L179, L187–L189); no other occurrence of "soft". |
| M10 — NFR21 declares the DAPR-generic experiment won | Medium | Resolved | NFR21 L1140 (see H1). |
| M11 — FR67 still sells audit | Medium | Resolved | FR67 L1067 "access telemetry (Glossary) … not a tamper-evident audit trail". |
| M12 — Case members are MVP capabilities with no outcome | Medium | Resolved | Glossary L92 "Its Phase 1 outcome is attribution: members appear in case listings and the case activity feed (FR36), and membership is the seed for per-unit ACLs in Phase 3"; FR28 L1011; acceptance output L875. |
| M13 — Score table documents a magnitude graph axis | Medium | Resolved | L550 "Proximity magnitude is never fused directly"; L551 composite = weighted RRF; L553. |
| M14 — File ingest has no freshness outcome; "required active" projections are an accordion | Medium | **Partial** | Freshness added: NFR36 L1180 and pipeline note L828; accordion closed at L809 "In MVP 'required projections' means all three; a degraded projection set is not an MVP configuration." **Residual:** FR6 L983 still says "searchable across all *required active* projections/axes" — the accordion phrase survives in the FR a reader extracts. |
| M15 — CLI contract is two lists; only one is cut to MVP | Medium | **Partial** | Parity Matrix L838 now says the Phase 1 CLI surface table is the single SoT and "This matrix only maps capabilities to interfaces"; table L866–L885 phases every verb. **Residual:** a third list, Phase 1.5 row L249 "CLI expansion: `explore`, `handlers`, EventStore diagnostics, **remaining FR53 slices**", contradicts L887 "Stubbed Phase 1 verbs are the remaining FR53 **MVP** slices" (see N1); MVP Feature #6 L236 enumerates a verb list that differs from the table (see N2). |
| M16 — CLI `status` / FR53 phase disagree across spines | Medium | Overridden (handoff) | Inside the PRD, `status` is Phase 1 (L878–L879), FR53 split is defined (L961, L1047); the cross-spine (epics) disagreement is the downstream drift handed off at A120. |
| M17 — Onboarding boot path and second clock not shared (AppHost vs docker compose; epics one clock) | Medium | **Partial** | PRD side: AppHost is the boot path (L49, L116, L195, L913, L949) and the two clocks are explicit (L49, L116–L117, L925, L1165). **Residual:** L612 still says "Pin to a specific AGPL-licensed version in the default docker-compose.yml", the only remaining docker-compose boot artefact in the PRD (see N6). Epics single-clock drift is handed off (A120). |
| M18 — Ingestion state vocabulary three-way across PRD / architecture / epics / UX | Medium | Overridden (handoff) | PRD side unified (M4); architecture/epics/UX re-extraction is the A120 handoff. |
| M19 — Product-brief silent drops | Medium | Resolved | Each named drop has a recorded disposition in addendum A88–A99 (custom extraction phrases, 6-month second provider, "all DAPR state stores", 5×, Why Now, Priya metric, motivating benchmark scene, cross-case insight → Open Q5). No longer silent. |
| M20 — Expansions that addendum said must leave the PRD (Aspire, OpenBao NFR, package tables, compliance program) | Medium | Overridden (settled) | A116 "August 2026 SCP amendments; they stay, with mechanism detail here"; PRD L55 frames them as "not additional product surfaces"; NFR9 L1113; package tables L686–L711; addendum A3/A9 no longer claims they must leave (the old addendum sentence the reviewer quoted is gone). |

### Low

| Finding | Severity | Disposition | PRD location or reason |
|---|---|---|---|
| L1 — Journey 8 trains readers to treat 0.95 as a reason to relax | Low | Resolved | L443 "the UI labels it 'relevance, not verified fact' (Glossary) … 0.95 is not why she relaxes; she trusts it because she can verify every link." |
| L2 — Front matter calls the PRD complete and draft in the same breath | Low | **Unaddressed** | L3 `status: draft`; L6 `stepsCompleted: [... 'step-12-complete']`; no decision recorded. (Also the residual of C3.) |
| L3 — Epic 9 still markets "zero-code" against the Glossary | Low | Overridden (handoff) | PRD itself is clean (L51, L61, L646, L663 all negate zero-code/zero-config); Epic 9 wording is in A120's handoff list. |
| L4 — `--explain` vs UX-DR7 remains an open product pick | Low | **Partial** | Open Q3 L1186 now has an owner (UX) and a revisit trigger (Epic 17 activation SCP); the pick itself is still open. |
| L5 — NFR11 is a current invariant scheduled as post-MVP remediation (Epic 20) | Low | Overridden (handoff) | PRD NFR11 L1115 is MVP and says "unauthenticated product ingress is not a Phase 1.5 allowance"; addendum A58 records Epic 20 JWT as the current MVP ingress reality; epics phase tag is downstream. |
| L6 — Post-Update Story 27.4 live-producer contract must stay out of the PRD | Low | Resolved | No occurrence of "27.4" in `prd.md`; A120 "the Story 27.4 live-producer contract stays out of the PRD". |

### Mechanical notes

| Note | Severity | Disposition | PRD location or reason |
|---|---|---|---|
| Glossary, addendum, Assumptions Index exist | Mechanical | Resolved (no action needed) | Glossary L78–L97; Assumptions Index L1193–L1202; `addendum.md` present. |
| Assumptions Index roundtrip: NFR33/NFR35 collision entry index-only | Mechanical | Resolved | L1200 now states "index-only by design — the collision is history, not a live requirement"; the other five index entries have inline tags at L51, L106, L203–L204, L1094, L1180. |
| FR1–FR74 and NFR1–NFR35 unique and contiguous | Mechanical | Resolved (maintained) | FR1–FR74 contiguous L978–L1080; NFR1–NFR36 contiguous with NFR36 added in its own section L1176–L1180; headline counts updated at L37 and L149 ("NFR1–NFR36"). |
| Soft gates labelled both "Soft gate" and "Must pass" | Mechanical | Resolved | L181; all rows "Must pass". |
| `memories tenant switch` remains in the Command Structure table | Mechanical | Resolved | Removed; L864 records the removal date. `RootCommandFactory.cs` has no `switch` verb. |
| FR63 says "composite confidence scores"; Glossary term is Relevance confidence | Mechanical | Resolved | FR63 L1063 "returns a relevance confidence (0.0–1.0) with per-axis breakdowns". |
| Evidence Packet shape architecture-owned (intentional SoT split) | Mechanical | Resolved (intentional, unchanged) | L87, L555; addendum A5. |

## 3. New contradictions introduced or exposed by the Update

Each item quotes both locations. N1–N4 are introduced by text added on 2026-09-08; N5–N6 are pre-existing wording made contradictory by the new single-source-of-truth statements; N7 is the C1 residual restated as a contradiction.

**N1 — "remaining FR53 slices" placed in Phase 1.5 and in MVP.**
- L249 (Phase 1.5 table, row 3): "CLI expansion: `explore`, `handlers`, EventStore diagnostics, remaining FR53 slices | Full developer experience".
- L887: "Stubbed Phase 1 verbs are the remaining FR53 MVP slices." and L961: "Phase 1.5: FR23, FR54, FR58–FR62, and the Phase 1.5 CLI slices of FR53 (`explore`, EventStore diagnostics)."
- Effect: a reader of the Phase 1.5 table can assign the stubbed `ingest`/`traverse`/`case`/`tenant`/`status --case` verbs to Phase 1.5. Fix: drop "remaining FR53 slices" from L249 or change it to "the Phase 1.5 FR53 slices only".

**N2 — MVP Feature #6 verb list disagrees with the Phase 1 CLI surface table it cites.**
- L236: "`search query --explain`, `traverse`, `ingest`, `case create/delete/add-member/activity`, `tenant create/delete/verify/list`, `status`, `quickstart`, `consistency verify`".
- L866–L885 (the declared FR53 SoT) additionally lists as Phase 1: `search inspect` / `search lookup` (L871), `case list` and `case remove-member` (L874–L875), `status telemetry` (L878), `consistency inspect/repair` (L880), `config show` (L882).
- Effect: low, because L236 says "per the Phase 1 CLI surface table", but the enumerated list is shorter than the SoT and omits shipped verbs. Fix: make L236 "see table" without enumerating, or enumerate identically.

**N3 — Journey 9 (the Phase 1 thesis success path) uses a CLI syntax the new SoT table rules out.**
- L864: "Tenant scope is passed as `--tenant <id>` on tenant-scoped commands"; L870: "`memories search query --tenant --query [--case] …`"; L873: "`memories ingest <file|url|dir> --tenant --case`".
- L461: "`memories ingest ./sample-claims/ --case claims-pilot`" and "Try: 'memories search "claim" --case claims-pilot'"; L463: "`memories search "water damage" --case claims-pilot`" — no `query` subcommand, no `--query`, no `--tenant`, in the same journey whose opening scene (L457) uses the correct form `memories search query --tenant pilot --query "anything"`.
- Same pattern in Phase 1.5 Journey 2 (L318, L320, L324) and Journey 10 (L473 "`memories search --explain`"). Fix: normalise journey commands to the L870/L873 shapes.

**N4 — NFR status register marks "Verified" three NFRs whose verification text was rewritten in this Update and whose evidence is explicitly pending.**
- L1090: "Verified | NFR8 (Epics 5, 20, 24-in-progress isolation suites), … NFR16–NFR19 (Epics 21, 26.7 restart gate), … NFR20–NFR21 (Epics 9–10 conformance tests)".
- NFR8 L1112: "Automated suite driven by *principals*, not ids … Re-run when Epic 24 (tenant-scoped principals) closes"; FR44 L971: "Epic 24 … is in progress. NFR8 evidence must be re-run when Epic 24 closes."
- NFR16 L1130 now requires "`memories consistency verify` then `consistency repair --yes` … all N are `indexed` within 5 minutes" — a target defined today; Epic 26.7 evidence predates it.
- NFR21 L1140 now requires "a documented negative test showing what a non-conforming envelope does" — no evidence is cited.
- Effect: "Verified" (defined at L1086 as "an automated suite or recorded run in a done epic exercises the *stated* verification") is claimed for verifications whose stated form did not exist when the evidence was recorded. Fix: move NFR8/NFR16/NFR21 to "Implemented, verification run not recorded" or cite the run that exercises the new wording.

**N5 — Phase label "Growth" means Phase 2 in one place and Phase 3 in another.**
- L255: "### Phase 2 (Growth)".
- L386: "### Journey 6: Kenji — "Time to Scale" (Growth / Phase 3)" while L498 says "Kenji — Growth Operations (J6, Phase 3)" and L590 says "Confidence calibration (Growth phase)" without a number.
- Fix: label Journey 6 "(Phase 3)" and L590 "(Phase 2)" or "(Phase 3)" explicitly.

**N6 — Boot artefact: docker-compose.yml vs AppHost.**
- L612: "Pin to a specific AGPL-licensed version in the default docker-compose.yml."
- L949: "Current path: .NET Aspire AppHost with DAPR sidecars. Local: `dotnet run --project Hexalith.Memories.AppHost`"; L116/L1165 make "AppHost → first CLI search" the G3/NFR31 path; A62 "Aspire AppHost is the current orchestration path".
- Fix: "in the AppHost resource definition (and any compose export)".

**N7 — FR25 benchmark contract vs the thesis-gate protocol.**
- L1005 (FR25): "Developer can run automated benchmark comparisons of hybrid vs single-axis search results with scored output."
- L158: "Single-axis runs are diagnostics only."; L237 (MVP Feature #7): "Benchmark Suite (thesis-gate protocol: N ≥ 50, BM25+semantic control, ΔNDCG@10 ≥ 0.02) | Thesis validation (G1)".
- Effect: the only FR that describes the benchmark capability describes a run the PRD itself says cannot pass G1. Fix: "hybrid vs BM25+semantic (two-axis RRF) control, with single-axis diagnostics, scored as NDCG@10 per topic".

Checked and **not** contradictory: gate ids (G1–G5, L1–L3 used consistently at L116–L126, L175–L189, L203–L204, L627, L652–L654); NFR count (L37 and L149 both say NFR1–NFR36; no "NFR35" total survives); "audit" appears only in negations or for the distinct tamper-evident-trail Non-Goal (L91 Glossary rule holds for FR67/NFR34); `tenant switch` absent except the removal note (L864); "physical" absent; Phase 1.5 "within 4 weeks of thesis validation" (L243) vs dates 2026-10-31 → 2026-11-30 (L203–L204) is consistent to within two days; FR status register covers FR1–FR74 with no gaps or overlaps; NFR status register covers NFR1–NFR36 with no gaps or overlaps.

## 4. Qualitative items dropped (consciously, not silently)

Recorded in `addendum.md` and therefore no longer "silent drops" in the validation report's sense:

| Item | Where dropped / re-homed | Reference |
|---|---|---|
| Custom extraction phrases | Phase 2 list only; no FR until Phase 2 planning | A92; PRD L260 |
| Second embedding provider within 6 months | Post-MVP provider table, no dated commitment | A93; PRD L781–L787 |
| "Works with all DAPR state stores" | Dropped; NFR15 keeps the migration path only | A94; PRD L1124 |
| "5× productivity" | Dropped as unmeasurable | A95 |
| "Why now / 12–18-month window" | README/marketing context, not a requirement | A96 |
| Priya's search-success metric | Phase 2 web surface (Epic 17) | A97; PRD L500 |
| Motivating benchmark scene | Replaced by the thesis-gate protocol | A98; PRD L156–L164 |
| Cross-case insight discovery | Blocked by FR32; Open Q5 | A99; PRD L1188 |
| Brand line, README-as-product, `explore` as trust, Priya screenshot, "not ChatGPT memory" | Re-home in UX/docs, not FRs | A80–A86 |

Reviewer fixes consciously **rejected** by the Update (recorded as rejected alternatives, A101–A116): retitle 80% as diagnostic-only (A113); move FR46–FR52 + NFR4 to Phase 1.5 (A111); accept N=8 as the gate (A112); cut FR1–FR74 to the active ship contract (A114); remove `--tenant` (A115 — `tenant switch` removed instead); move NFR9 / package tables / Aspire out (A116).

Reviewer fixes **not applied and not recorded** as decisions (these are the two Unaddressed rows): trimming user-journey density (M8); setting front-matter `status` to the signed contract (L2 / C3 residual).
