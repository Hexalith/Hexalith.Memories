---
date: 2026-07-26
status: approved
trigger: implementation-readiness-report-2026-07-25.md
scope_classification: Moderate
mode: Batch
findings_addressed: ['C1', 'C2', 'C3', 'Q2', 'DW 27.3-CR5', 'DW 27.3-CR6']
findings_deferred: ['H1', 'H2 (Epic 19 only)', 'M1', 'M2', 'M3', 'U1', 'U2', 'U3', 'U4', 'U5', 'Q1', 'Q3 (Epic 19 only)', 'Q4', 'Q5', 'Q6', 'P1']
artifacts_modified: ['epics.md', 'sprint-status.yaml', '27-3-production-adapter-and-deployment-profile.md', 'deferred-work.md']
---

# Sprint Change Proposal — Readiness Coherence and Story 27.3 Scope Splits

**Date:** 2026-07-26
**Project:** Hexalith.Memories
**Prepared by:** Correct Course workflow (Developer)
**Approver:** Administrator (Jerome)

---

## 1. Issue Summary

### Problem statement

Four planning artifacts have drifted out of agreement after approximately four months of
approved sprint changes. Nothing is missing — coverage is complete — but three documents now
state different things about the same subjects, and two epics carrying live work sit outside the
governance system that is supposed to classify them.

Separately, two scope splits that were **already approved by the Administrator on 2026-07-21**
during Story 27.3 code review remain unexecuted, and both are fail-closed blockers on Story 27.3
reaching `done`.

### Discovery context

The implementation readiness assessment run on 2026-07-25
(`implementation-readiness-report-2026-07-25.md`, 1181 lines) reported 21 issues — 3 critical,
5 high, 11 medium, 2 low — against a document set that passed every coverage, sequencing, and
dependency check it applied. Epics 0–26 are `done`; four stories remain open. This is a
late-stage coherence problem, not a pre-implementation readiness problem.

The two pending splits were discovered during this workflow's verification pass, not by the
readiness report. They are recorded as open `[Review][Action]` rows in the Story 27.3 file
(lines 114 and 128) and as `DW 27.3-CR5` / `DW 27.3-CR6` in `deferred-work.md` (lines 2297 and
2316).

### Evidence

**C1 — fusion algorithm contradiction.** Verified in all four sources:

| Source | Location | States |
| :--- | :--- | :--- |
| PRD | `prd.md:1007` | "deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0" |
| Architecture | `architecture.md:92` | "weighted reciprocal-rank fusion. Raw BM25, cosine, and graph-proximity magnitudes are **not averaged** in hybrid scoring" |
| **Code as built** | `src/Hexalith.Memories.Server/Search/FusionEngine.cs:12` | "weighted reciprocal rank fusion"; `RrfRankConstant = 10`; `documentCount`/`averageDocumentLength` parameters documented "Ignored for RRF" |
| **Code as built** | `src/Hexalith.Memories.Server/Search/ExplainMetadataBuilder.cs:49` | "raw BM25 magnitude is **not exposed** as the hybrid syntactic score" |
| Epics | `epics.md:171` | "All axis scores normalized to 0.0-1.0 before fusion" |
| Epics | `epics.md:1180` | "the composite score is a **weighted average of normalized axis scores**" |
| Epics | `epics.md:1153` | cites `(NFR24)` for behavior NFR24 no longer describes |

This materially strengthens the readiness report's finding. `epics.md` does not merely
contradict the architecture — it contradicts **the shipped implementation**. Reconciling it is a
documentation catch-up to as-built reality, not a design change and not a scope change.

Corroborating detail: `ScoreNormalizer.cs` still exists, but the only live call site is
`GraphScopedSearch.cs:185` → `NormalizeGraphProximity`. Per-axis normalization therefore survives
as **single-axis score semantics**, exactly as PRD NFR24's second clause requires, and is no
longer a fusion input.

**C2 — governance gap under live work.** `sprint-status.yaml` `readiness_accounting` lists
`epic-26` then jumps to `epic-29` (lines 86–87 and 116–117). `epic-27` and `epic-28` appear in
`development_status` (lines 419 and 430) but in neither `excluded_unless_sprint_selected` nor
`epic_metadata`. With `default_story_inherits_epic_metadata: true` (line 88), their stories
inherit no `track`, no `phase`, and no `mvpReadiness`. `epics.md:411` designates this file the
authority tooling "**must** use rather than inferring MVP readiness from story status, numeric
ordering, or FR coverage alone." Epic 27 is `in-progress` right now.

The Epic List (`epics.md:477`–`646`) ends at Epic 27; Epics 28 and 29 have full bodies at lines
4909 and 4966 but no list entry.

**C3 — NFR9 is two policy generations stale.** `epics.md:144` reads "Embedding API keys stored in
secure secret management — never in config files." PRD NFR9 (`prd.md`) requires retrieval
"exclusively through the DAPR Secrets API, backed by OpenBao," with Kubernetes Secrets restricted
to documented bootstrap material. Story 29.2 — one of the four open stories — exists to implement
the newer policy.

**Q2 — correction to the readiness report.** The report's highest-priority recommendation was
"add a checklist evidence table to the Story 27.3 file." **That table already exists.** Story
27.3 lines 166–169 carry an `## Implementation Checkpoints` table with precisely the columns
`epics.md:553` demands: Accountable owner / Required evidence artifact and command / Review state
/ Completion state / Completion date.

The real defect is narrower and still real: the table has **two rows** (C0, C1), and the single
C1 row absorbs **all 14 verification gates** enumerated in AC2. `epics.md:551` requires that
"each checkpoint must be implemented, reviewed, and evidenced as a separately verifiable slice."
One row with one review state and one completion date cannot satisfy that for 14 gates.

*(The readiness report said "~18 gates". The precise count from AC2 is 14; AC1, AC3, and AC4
carry the remaining obligations as separate acceptance criteria.)*

**DW 27.3-CR5 / CR6 — approved, unexecuted splits.** Both carry `Status: open` and the note
"Split approved 2026-07-21 by Administrator during code review; execution is a separate
correct-course activity, not applied by this review." Both list
`_bmad-output/planning-artifacts/epics.md` as target artifact and "before Story 27.3 advances to
done" as re-open trigger.

---

## 2. Impact Analysis

### Epic Impact

| Epic | Status | Impact |
| :--- | :--- | :--- |
| Epic 2 — Three-Axis Search, Fusion & Benchmark Validation | `done` | Specification text reconciled to as-built. No implementation change, no re-validation, no gate re-run. Stories 2.4 and 2.5 text only. |
| Epic 27 — Access Telemetry Lifecycle Hardening | `in-progress` | Gains readiness-accounting metadata. Story 27.3 gains per-gate checkpoint tracking and sheds two scope blocks. |
| Epic 28 — Owner-Approved EventStore Runtime Adoption | `backlog` | Gains readiness-accounting metadata and an Epic List entry. No scope change. |
| Epic 29 — OpenBao-First Dapr Secret Management | `in-progress` | Gains an Epic List entry. Boundary against new Epic 31 stated explicitly so the two do not collide. |
| **Epic 30 — Container Release Pipeline Ownership** | **new, `backlog`** | Receives the four-image release/publish pipeline split out of Story 27.3 (DW 27.3-CR5). |
| **Epic 31 — OpenBao Secrets Platform and Runtime Secret-Store Migration** | **new, `backlog`** | Receives the OpenBao platform and runtime `secretstore` migration split out of Story 27.3 (DW 27.3-CR6). |

No epic is removed, deferred, resequenced, or redefined. No epic becomes obsolete.

### Story Impact

| Story | Status | Change |
| :--- | :--- | :--- |
| 2.4 Score Normalization | `done` | Story statement reframed from "before fusion" to single-axis explain semantics; final AC citation clarified. ACs describing normalization behavior are unchanged — they describe live code. |
| 2.5 Fusion Algorithm & Hybrid Search | `done` | One AC line replaced (weighted average → weighted RRF); one AC line added (explain exposes rank contributions and fusion weights). Dated reconciliation note appended. |
| **27.3 Production Adapter and Deployment Profile** | **`in-progress`** | C1 checkpoint row expands into a 14-row gate evidence table. Two `[Review][Action]` split rows close. File List shrinks by 13 paths. Two `Scope Transferred` entries added. **No acceptance criterion is edited.** |
| 27.4 Retention Verification, Runbook, A41 Close-Out | `backlog` | Unchanged. Still gated behind 27.3. |
| 28.1 Adopt Owner-Approved EventStore Runtime Identity | `backlog` | Unchanged. Still externally gated on EventStore Story 1.20. |
| 29.2 Provider-Neutral Aspire Composition and Secret Verification | `backlog` | Unchanged in scope. Boundary against Story 31.1 stated so neither can claim the other's evidence. |
| **30.1 Four-Image Container Release and Partial-Recovery Pipeline** | **new, `backlog`** | Created. |
| **31.1 OpenBao Platform Hardening and Runtime Secret-Store Migration** | **new, `backlog`** | Created. |

### Artifact Conflicts

| Artifact | Conflict | Resolution |
| :--- | :--- | :--- |
| `epics.md` | NFR24 inventory + Stories 2.4/2.5 contradict PRD, architecture, and shipped code | Reconcile to weighted RRF (Edits 1–5) |
| `epics.md` | NFR9 inventory predates OpenBao policy | Restate from PRD NFR9 (Edit 6) |
| `epics.md` | Epic List ends at Epic 27 | Add Epics 28–31 (Edit 9) |
| `epics.md` | Implementation Readiness Boundary classifies 26 of 30 epics | Classify Epics 27–31 (Edit 10). **Epic 19 remains unclassified — out of approved scope.** |
| `epics.md` | Checkpoint guard at line 553 names only done stories 21.9 and 26.5 | Make the guard shape-based (Edit 11) |
| `sprint-status.yaml` | `readiness_accounting` omits epic-27, epic-28 | Register epic-27, epic-28, epic-30, epic-31 (Edits 7–8) |
| `27-3-...md` | C1 row bundles 14 gates; two open split actions; File List spans three scopes | Gate table, close actions, shrink File List (Edits 12–14) |
| `deferred-work.md` | DW 27.3-CR5 / CR6 `Status: open` | Mark resolved with pointers to Stories 30.1 / 31.1 (Edit 15) |
| `prd.md` | **No conflict.** PRD is the current side on every finding in this proposal. | No edit |
| `architecture.md` | **No conflict.** Architecture is the current side on C1. | No edit |
| `ux-design-specification.md` | U1–U5 remain open | **Out of approved scope.** Deferred (§6) |

### Technical Impact

**None.** Every edit in this proposal is a document edit.

- No source file changes. No test changes. No build, CI, or deployment changes.
- No re-validation of any completed gate. Epic 2's Gate 1 three-axis validation, its NDCG@10
  benchmark results, and Epic 26's fusion calibration are untouched and remain valid — the
  reconciliation makes `epics.md` agree with what those gates actually validated.
- Story 27.3's acceptance criteria are **not** edited, preserving its frozen-section rule
  (story file line 49) and its immutability contract.
- The two new stories describe work whose artifacts **already exist in the repository**; they
  formalize ownership and remaining hardening, they do not commission new subsystems.

### Sequencing risk introduced by the CR6 split — assessed and closed

Splitting the OpenBao platform out of Story 27.3 could create a forward dependency: Story 27.3's
`PG-ONPREM-1` connection string, including `sslmode=verify-full`, resolves from OpenBao.

This does **not** become a blocking forward dependency, because the platform is already deployed
and operational — the chunk-2 code review records that "the 2026-07-20 live probe loaded the
store" (Story 27.3 line 134). Story 31.1 formalizes ownership, hardening, and documentation of a
running platform; it does not stand it up.

Recorded explicitly as an **environment prerequisite**, not a story dependency, in both Epic 31
and the Story 27.3 scope-transfer note. The epic set's zero-forward-dependency property is
preserved.

---

## 3. Recommended Approach

**Selected path: Direct Adjustment (Option 1), hybrid with new-epic registration.**

### Options evaluated

| Option | Assessment |
| :--- | :--- |
| **1. Direct Adjustment** | **Viable — selected.** Every finding resolves by editing existing artifacts plus registering two new epics for already-approved splits. Effort: Low. Risk: Low. |
| **2. Potential Rollback** | **Not viable, and not desirable.** Rolling back Epic 2 would revert a working weighted-RRF implementation to match stale prose. The code is correct; the document is stale. Rollback inverts the fix. |
| **3. PRD MVP Review** | **Not applicable.** MVP scope is unaffected. All 74 FRs remain covered; no requirement is added, removed, or rephased. The PRD needs no edit — it is the current side of every conflict here. |

### Rationale

1. **The direction of drift decides the fix.** PRD, architecture, and shipped code agree on
   weighted RRF. Only `epics.md` disagrees. Editing the single stale artifact is the minimal,
   lowest-risk correction, and it cannot regress behavior because it changes no behavior.
2. **The governance gap sits under live work.** Epic 27 is `in-progress` and inherits no
   readiness metadata. This is a two-line YAML fix that restores the accounting authority over
   the only actively-worked epic in the project.
3. **The splits are already approved; only execution was pending.** Both DW entries name
   correct-course as the execution vehicle and `epics.md` as the target artifact. This workflow
   is that vehicle. Leaving them open keeps Story 27.3 fail-closed indefinitely.
4. **Per-gate tracking is the difference between a table and evidence.** Story 27.3's existing
   table is well-formed but records one review state for 14 independent gates. Since C1 is
   `pending` and Production writes are disabled, expanding it now costs nothing and prevents a
   partial pass from being recorded as a whole pass later.

### Effort, risk, and timeline

| Dimension | Assessment |
| :--- | :--- |
| Effort | **Low.** 15 edits across 4 files. All mechanical except the two new epic bodies. |
| Technical risk | **None.** No executable artifact changes. |
| Governance risk | **Low, and reduced by this change.** Registering four epics and closing two approved splits reduces the number of ungoverned surfaces from four to one (Epic 19). |
| Timeline impact | **None on the critical path.** Story 27.3 remains blocked by its own unreviewed chunk 3 and the DW 27.3-CR4 recount — this proposal removes two of its blockers without adding any. |
| Story 27.3 status | **Stays `in-progress`.** Nothing here advances it. AC5 fail-closed behavior is preserved verbatim. |

---

## 4. Detailed Change Proposals

> All `epics.md` and `.md` edits must preserve CRLF line endings (`.gitattributes` policy).
> `sprint-status.yaml` stays LF.

### Group A — `epics.md`: C1 fusion reconciliation

#### Edit 1 — NFR24 requirements inventory

**File:** `_bmad-output/planning-artifacts/epics.md:171`

**OLD:**
```
- NFR24: All axis scores normalized to 0.0-1.0 before fusion [MVP]
```

**NEW:**
```
- NFR24: Hybrid fusion uses deterministic weighted reciprocal-rank fusion with per-axis rank contributions in 0.0-1.0; single-axis explain still documents axis-specific score semantics [MVP]
```

**Rationale:** Restores PRD NFR24 verbatim (`prd.md:1007`). The inventory entry was the pre-RRF
text and is the root source a story author would read.

---

#### Edit 2 — Story 2.4 story statement

**File:** `_bmad-output/planning-artifacts/epics.md:1124-1126`

**OLD:**
```
As a developer,
I want all search axis scores normalized to 0.0-1.0 before fusion,
So that scores from different axes are comparable and the fusion algorithm produces meaningful composite rankings.
```

**NEW:**
```
As a developer,
I want each search axis to expose a documented, deterministic 0.0-1.0 score for single-axis results and explain output,
So that per-axis relevance is interpretable on its own terms, independent of how the hybrid composite is produced.
```

**Rationale:** "Before fusion" is the superseded model. Under weighted RRF the composite consumes
per-axis **ranks**, not normalized magnitudes. Normalization survives for single-axis score
semantics — which is exactly what PRD NFR24's second clause preserves, and what
`GraphScopedSearch.cs:185` still calls.

---

#### Edit 3 — Story 2.4 final acceptance criterion citation

**File:** `_bmad-output/planning-artifacts/epics.md:1153`

**OLD:**
```
**Then** outputs match expected values exactly (NFR24)
```

**NEW:**
```
**Then** outputs match expected values exactly (NFR24 single-axis explain semantics, NFR25 determinism)
```

**Rationale:** The citation is retained rather than removed — NFR24 genuinely still governs
single-axis explain semantics — but it is now scoped to the clause that applies, and the
determinism obligation is attributed to NFR25 where it belongs.

---

#### Edit 4 — Story 2.5 fusion acceptance criterion

**File:** `_bmad-output/planning-artifacts/epics.md:1180`

**OLD:**
```
**And** the composite score is a weighted average of normalized axis scores
```

**NEW:**
```
**And** the composite score is a deterministic weighted reciprocal-rank fusion of per-axis result ranks — raw BM25, cosine, and graph-proximity magnitudes are not averaged into the composite (NFR24)
**And** explain output exposes each axis's rank contribution and the fusion weights applied, rather than its raw magnitude
```

**Rationale:** This is the single most important line in the document to correct. It is the MVP
thesis epic's statement of the product's central algorithm, and it currently states the opposite
of `architecture.md:92`, `prd.md:1007`, and `FusionEngine.cs`. The added second line binds the
explain surface to rank-contribution semantics, matching `ExplainMetadataBuilder.cs:49-64`.

---

#### Edit 5 — Epic 2 reconciliation note

**File:** `_bmad-output/planning-artifacts/epics.md` — append after Story 2.5's existing
`**Ownership note:**` paragraph (line ~1192)

**NEW (insertion):**
```
**Fusion supersession note (2026-07-26):** Stories 2.4 and 2.5 were originally specified against a
normalize-then-weighted-average model. Story 22.4 selected corpus-invariant weighted
reciprocal-rank fusion, and Epic 26 calibrated it (RRF `k=10`; live syntactic/semantic/graph
weights `0.30/0.35/0.35`; optional NL weight `0.20`, default-off). The text above was reconciled
to that as-built model by the approved Sprint Change Proposal 2026-07-26; the implementation in
`FusionEngine` and `ExplainMetadataBuilder` was already weighted RRF and is unchanged by that
reconciliation. Per-axis normalization is retained for single-axis score semantics and explain
output only. Authority for fusion behavior is `architecture.md` section 8; `prd.md` NFR24-NFR26
owns the requirement.
```

**Rationale:** Preserves history rather than erasing it, names the supersession chain
(Story 22.4 → Epic 26 calibration → this proposal), and points future readers at the authoritative
documents so the drift does not recur.

---

#### Optional Edit 5b — Story 22.3 ambiguity (flagged, not recommended for this pass)

`epics.md:4324` reads "fusion uses a scale-free method (RRF **or** per-axis min-max)". Story 22.3
is `done` and the ambiguity is now resolved elsewhere by Edits 1–5 plus `architecture.md`. Listed
for completeness; **recommend leaving as-is** per the readiness report's own guidance not to
retrofit completed audit-remediation stories.

---

### Group B — `epics.md`: C3 secret-management policy refresh

#### Edit 6 — NFR9 requirements inventory

**File:** `_bmad-output/planning-artifacts/epics.md:144`

**OLD:**
```
- NFR9: Embedding API keys stored in secure secret management — never in config files [Ongoing]
```

**NEW:**
```
- NFR9: Product services retrieve embedding-provider and other application runtime secrets exclusively through the DAPR Secrets API backed by OpenBao; secret values never live in application configuration or ordinary environment variables. Kubernetes Secrets are restricted to documented, unavoidable OpenBao bootstrap credentials or direct pod inputs outside the DAPR secret-store boundary. Verified by structural dependency tests, secret scanning, AppHost topology tests, and integration tests [Ongoing]
```

**Rationale:** Story 29.2 is open and would be written against this entry. The current text
predates both the OpenBao decision and Epic 29, and would cause it to under-specify its own NFR.
Text restated from PRD NFR9.

---

### Group C — `sprint-status.yaml`: C2 readiness-accounting registration

#### Edit 7 — `excluded_unless_sprint_selected`

**File:** `_bmad-output/implementation-artifacts/sprint-status.yaml:86-87`

**OLD:**
```yaml
    - epic-26
    - epic-29
```

**NEW:**
```yaml
    - epic-26
    - epic-27
    - epic-28
    - epic-29
    - epic-30
    - epic-31
```

---

#### Edit 8 — `epic_metadata`

**File:** `_bmad-output/implementation-artifacts/sprint-status.yaml:116-117`

**OLD:**
```yaml
    epic-26: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
    epic-29: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
```

**NEW:**
```yaml
    epic-26: { track: remediation, phase: audit-remediation, mvpReadiness: excluded }
    epic-27: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
    epic-28: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
    epic-29: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
    epic-30: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
    epic-31: { track: operational, phase: operational-readiness, mvpReadiness: excluded }
```

**Rationale for both:** With `default_story_inherits_epic_metadata: true`, Stories 27.3, 27.4, and
28.1 currently inherit no `track`, `phase`, or `mvpReadiness`. Metadata values follow the epics'
declared lifecycle labels — all four are Operational Readiness — and match the shape already used
for `epic-29`. Every one of these epics is correctly excluded from MVP readiness accounting;
registering them makes that exclusion explicit rather than accidental.

**Note:** these are readiness-accounting entries only. `development_status` rows for `epic-27`,
`epic-28`, and `epic-29` already exist (lines 419, 430, 436) and are unchanged; rows for the new
epics are added in Edits 16–17.

---

### Group D — `epics.md`: Epic List and boundary classification

#### Edit 9 — Epic List entries

**File:** `_bmad-output/planning-artifacts/epics.md` — after the Epic 27 entry ending at line 645

**NEW (insertion):**
```
### Epic 28: Owner-Approved EventStore Runtime Adoption
Memories source and package modes converge on the exact EventStore runtime identity authorized by EventStore Story 1.20 while preserving the existing zero-code DAPR ingestion contract.
**Lifecycle label:** Operational Readiness / EventStore Dependency Adoption
**Driven by:** Approved direct course correction 2026-07-17
**Activation gate:** Externally gated on EventStore Story 1.20 recording `final_decision: available`, `authorize_consumer_migration: true`, a 40-hex `tested_runtime_sha`, named owner approval, and the approved package version and SHA-256 inventory.

### Epic 29: OpenBao-First Dapr Secret Management
Aspire-hosted services resolve application secrets exclusively through Dapr secret-store components backed by OpenBao. Kubernetes Secrets remain permitted only for unavoidable bootstrap credentials or direct pod inputs that Dapr cannot inject.
**Lifecycle label:** Operational Readiness / Secret Management Hardening
**Driven by:** Sprint Change Proposal 2026-07-19 — OpenBao-First Aspire Secret Management
**NFRs reinforced:** NFR9
**Scope boundary:** Epic 29 owns the Aspire/AppHost-local secret topology and provider-neutral composition. The deployed-cluster OpenBao platform and the runtime Dapr `secretstore` migration are Epic 31.

### Epic 30: Container Release Pipeline Ownership
The four-image container release and partial-recovery pipeline is owned, tested, and documented as an independently demonstrable release lane rather than as incidental scope inside an adapter-qualification story.
**Lifecycle label:** Operational Readiness / Release Engineering
**Driven by:** Sprint Change Proposal 2026-07-26 — DW 27.3-CR5 split approved by Administrator 2026-07-21

### Epic 31: OpenBao Secrets Platform and Runtime Secret-Store Migration
The deployed OpenBao `hexalith-keys` platform and the runtime Dapr `secretstore` migration from Kubernetes Secrets to `hashicorp.vault` are owned, hardened, documented, and security-reviewed as an independently deployable operations platform.
**Lifecycle label:** Operational Readiness / Secret Management Platform
**Driven by:** Sprint Change Proposal 2026-07-26 — DW 27.3-CR6 split approved by Administrator 2026-07-21
**NFRs reinforced:** NFR9
**Scope boundary:** Epic 31 owns the deployed-cluster platform and runtime secret-store migration. Aspire/AppHost-local topology and provider-neutral composition are Epic 29.
```

**Rationale:** Closes the readiness report's Epic List gap and pre-registers the two new epics so
they never repeat the C2 defect.

---

#### Edit 10 — Implementation Readiness Boundary classification

**File:** `_bmad-output/planning-artifacts/epics.md` — append to the boundary section
(after the "Post-MVP audit remediation" paragraph, ~line 421)

**NEW (insertion):**
```
**Post-MVP operational hardening:** Epics 27-31 are Operational Readiness track. Epic 27 hardens the access-telemetry lifecycle; Epic 28 adopts the owner-approved EventStore runtime identity; Epic 29 owns Aspire-local OpenBao secret topology; Epic 30 owns the container release pipeline; Epic 31 owns the deployed OpenBao platform and runtime secret-store migration. None is counted toward MVP product readiness, each requires explicit sprint selection, and each is judged by the Engineering/Operational Readiness Track acceptance rules below.
```

**Rationale:** Resolves the readiness report's H2 for Epics 27, 28, and 29 and pre-empts it for
the two new epics. **Scope note:** Epic 19 remains unclassified — that item fell outside the
approved scope for this proposal and is carried to §6.

---

#### Edit 11 — Checkpoint guard generalization

**File:** `_bmad-output/planning-artifacts/epics.md:553`

**OLD:**
```
When checkpoint-heavy stories are selected for implementation, the story file must either split checkpoints into separately tracked child story files or include a checklist evidence table with owner, validation command or artifact, review status, and completion date for each checkpoint. This guard applies especially to future or backlog stories such as 21.9 and 26.5.
```

**NEW:**
```
When checkpoint-heavy stories are selected for implementation, the story file must either split checkpoints into separately tracked child story files or include a checklist evidence table with owner, validation command or artifact, review status, and completion date for each checkpoint. This guard is shape-based, not story-specific: it binds any story whose acceptance criteria enumerate more than five independently verifiable gates, however those gates are grouped into criteria. A single table row covering multiple gates does not satisfy it — one row per gate is required, because a shared review state and completion date cannot record partial completion. Stories 21.9, 26.5, and 27.3 are the known instances to date.
```

**Rationale:** The guard currently names only stories that are already `done`, so it has never
bound anything live. Making it shape-based means it binds Story 27.3 today and any future story
of the same shape without needing another amendment. The explicit "one row per gate" clause is
what Edit 12 implements.

**Scope note:** this edit belongs to the readiness report's recommendation #2, which sits at the
boundary of the approved scope. It is included because without it Edit 12 has no durable policy
anchor. Flag it in review if you would rather drop it.

---

### Group E — Story 27.3 file: per-gate evidence and scope shrink

#### Edit 12 — Expand the C1 checkpoint into a 14-row gate evidence table

**File:** `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`
— insert after the existing checkpoint table (line 169)

The existing two-row `Checkpoint` table is **kept unchanged** as the umbrella, per
`epics.md:551` ("Operational checkpoint stories may remain umbrella tracking stories"). The
following nested table is added beneath it.

**NEW (insertion):**
```
#### C1 Gate Evidence Table

Every row below is an AC2 gate and must pass independently. Per `epics.md:551`, a gate cannot be
marked complete because the C1 umbrella row is complete, and per AC5 any row not in `passed`
state keeps Production writes disabled, Story 27.3 `in-progress`, Story 27.4 `backlog`, and A41
open. All rows begin `pending`; no completion is claimed by adding this table.

Shared evidence packet for every row: `_bmad-output/implementation-artifacts/tests/27-3-adapter-profile-evidence.md`,
produced by the Production-Shaped Execution Contract command above against the immutable
`PG-ONPREM-1` profile. Each row names the observation within that packet that proves it.

| Gate | AC | Accountable owner | Required evidence observation and command | Review state | Completion date |
| :--- | :-- | :---------------- | :---------------------------------------- | :----------- | :-------------- |
| C1.1 CRUD | AC2 | Deployment adapter owner | `adapter-profile` probe: create/read/update/delete round trip on `state.postgresql/v2` | pending | — |
| C1.2 Strong reads | AC2 | Deployment adapter owner | `adapter-profile` probe: post-write strong-consistency read observation | pending | — |
| C1.3 ETags | AC2 | Deployment adapter owner | `adapter-profile` probe: ETag match/mismatch and `FirstWrite` insert semantics | pending | — |
| C1.4 Rollback-atomic multi-key transactions | AC2 | Deployment adapter owner | `adapter-profile` probe: fault injected on a later operation; no partial record or expiry-index commit | pending | — |
| C1.5 TTL | AC2 | Deployment adapter owner | `adapter-profile` probe: effective TTL expiry observation | pending | — |
| C1.6 Actor reactivation | AC2 | Deployment adapter owner | `adapter-profile` probe: actor state survives deactivation/reactivation | pending | — |
| C1.7 Placement / Scheduler / reminder recovery | AC2 | Deployment adapter owner | `adapter-profile` probe: Placement and Scheduler reconnection and reminder firing after control-plane disruption | pending | — |
| C1.8 Request bounds | AC2 | Deployment adapter owner | `adapter-profile` probe: request size/count bound enforcement | pending | — |
| C1.9 Two-writer 500 events/s throughput | AC2 | Deployment adapter owner + Hexalith Platform Operations | `adapter-profile` probe `--workload-profile adr-27.1-two-writer-500eps --steady-state-minutes 30`: ADR operation mix, zero acknowledged loss, p99 below the 3s Dapr client timeout, ≤10% p95 regression vs same-profile no-purge baseline | pending | — |
| C1.10 150,000-record purge catch-up | AC2 | Deployment adapter owner + Hexalith Platform Operations | `adapter-profile` probe `--purge-backlog-records 150000`: 10-minute backlog drained within five minutes, oldest-due age below 15 minutes | pending | — |
| C1.11 Isolation | AC2 | Deployment adapter owner + security reviewer | `adapter-profile` probe: cross-tenant denial evidence per the project-context tenant-isolation negative-evidence rule | pending | — |
| C1.12 Encryption | AC2 | Deployment adapter owner + security reviewer | `adapter-profile` probe: TLS `verify-full` in force and at-rest encryption posture | pending | — |
| C1.13 Capacity | AC2 | Deployment adapter owner + Hexalith Platform Operations | `AdapterProfileTests.test_capacity_inputs_fail_closed` plus the checked-arithmetic capacity result fitting the exact 400 GiB profile at 1h / 24h / 7d | pending | — |
| C1.14 Cohort-attributable physical reclamation | AC2 | Deployment adapter owner + Hexalith Platform Operations | `adapter-profile` probe: collector bound and cohort-attributable physical space reclamation | pending | — |
```

**Rationale:** Applies `epics.md:551` and the Edit 11 guard to the one story that needs it today.
Owner assignment follows AC4's split — Platform Operations owns capacity, cost, fault,
backup/restore, upgrade, rollback, and reclamation; the security reviewer owns identity, secrets,
TLS, network, authorization, encryption, privacy, and evidence integrity.

**This edit claims no evidence.** Every row is `pending` with no completion date, which is the
accurate current state: the C1 umbrella row is already `pending` and Production writes are
disabled.

**No acceptance criterion is modified.** The table is added to `## Implementation Checkpoints`,
which is not a frozen section under the story's own rule (line 49).

---

#### Edit 13 — Close the two split action rows

**File:** `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:114` and `:128`

**OLD (line 114):**
```
- [ ] [Review][Action] Split the four-image release/publish pipeline (`CiTestInventoryTests.cs` + `tests/tooling/publish_containers/*`) into a newly numbered story via correct-course, and shrink the 27.3 File List + ledger to adapter/qualification scope. (Split approved 2026-07-21 by Administrator during code review; execution is a separate correct-course activity, not applied by this review. DW 27.3-CR5) [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1]
```

**NEW (line 114):**
```
- [x] [Review][Action] Split the four-image release/publish pipeline (`CiTestInventoryTests.cs` + `tests/tooling/publish_containers/*`) into a newly numbered story via correct-course, and shrink the 27.3 File List + ledger to adapter/qualification scope. (Split approved 2026-07-21 by Administrator during code review; execution is a separate correct-course activity, not applied by this review. DW 27.3-CR5) [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1] — executed 2026-07-26 by approved Sprint Change Proposal 2026-07-26; scope moved to Epic 30 / Story 30.1.
```

**OLD (line 128):**
```
- [ ] [Review][Action] Split the OpenBao `hexalith-keys` secrets platform + runtime `secretstore` migration (k8s -> Vault, scopes `eventstore`/`memories`) into a newly numbered story via correct-course, and shrink 27.3 to the PG-ONPREM-1 adapter/secret-backing scope. (Split approved 2026-07-21 by Administrator during code review; execution is a separate correct-course activity, not applied by this review. DW 27.3-CR6) [deploy/openbao/values.yaml:140]
```

**NEW (line 128):**
```
- [x] [Review][Action] Split the OpenBao `hexalith-keys` secrets platform + runtime `secretstore` migration (k8s -> Vault, scopes `eventstore`/`memories`) into a newly numbered story via correct-course, and shrink 27.3 to the PG-ONPREM-1 adapter/secret-backing scope. (Split approved 2026-07-21 by Administrator during code review; execution is a separate correct-course activity, not applied by this review. DW 27.3-CR6) [deploy/openbao/values.yaml:140] — executed 2026-07-26 by approved Sprint Change Proposal 2026-07-26; scope moved to Epic 31 / Story 31.1.
```

---

#### Edit 14 — Scope-transfer notes and File List shrink

**File:** `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`
— extend the `### Scope Transferred to Story 27.4` section (line 66) with a sibling section

**NEW (insertion):**
```
### Scope Transferred to Stories 30.1 and 31.1 (2026-07-26)

The approved 2026-07-26 course correction executes the two splits the Administrator approved on
2026-07-21 during code review.

**To Story 30.1 (Epic 30, DW 27.3-CR5)** — the four-image container release and partial-recovery
pipeline, with its CI wiring, tooling, tests, and the release-runbook four-image expansion.

**To Story 31.1 (Epic 31, DW 27.3-CR6)** — the OpenBao `hexalith-keys` platform and the runtime
Dapr `secretstore` migration from Kubernetes Secrets to `hashicorp.vault` for the `eventstore` and
`memories` scopes. Story 27.3 retains the access-telemetry-specific secret components and the
`PG-ONPREM-1` secret backing, which remain in adapter scope.

**Environment prerequisite, not a story dependency:** the `PG-ONPREM-1` connection string,
including `sslmode=verify-full`, resolves from the deployed OpenBao platform. That platform is
already running — the 2026-07-20 live probe loaded the store — so Story 31.1 formalizes ownership
and hardening of an operational platform rather than standing one up. Story 27.3's C1 probe is
not blocked on Story 31.1, and no forward dependency is created.

Neither transferred block is executable under Story 27.3, and neither may be claimed as Story
27.3 evidence. Both target stories are `backlog`.
```

**File List reductions — 13 paths move out of Story 27.3:**

To Story 30.1:
```
- `.github/workflows/recover-partial-release.yml`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/tooling/publish_containers/partial_release_completion_test.py`
- `tests/tooling/publish_containers/publish_containers_test.py`
- `tests/tooling/publish_containers/registry_authorization_test.py`
- `tests/tooling/publish_containers/release_orchestration_test.py`
- `tools/complete-partial-release.ps1`
- `tools/publish-containers.ps1`
- `tools/verify-container-registry.ps1`
- `docs/dev/release-runbook.md`
```

To Story 31.1:
```
- `deploy/openbao/values.yaml`
- `deploy/openbao/namespace.yaml`
- `deploy/openbao/service-account-hardening.yaml`
- `deploy/openbao/smoke-test.yaml`
- `docs/operations/openbao.md`
- `deploy/kubernetes/base/dapr/secretstore.yaml`
```

Retained by Story 27.3 (adapter and its secret backing):
`deploy/kubernetes/base/dapr/access-telemetry-secrets.yaml`,
`deploy/kubernetes/base/dapr/access-telemetry-store.yaml`,
`deploy/kubernetes/base/dapr/access-telemetry-lifecycle-config.yaml`,
`deploy/kubernetes/base/access-telemetry-*.yaml`, `deploy/kubernetes/base/dapr/config.yaml`,
`deploy/kubernetes/base/service-accounts-rbac.yaml`,
`deploy/kubernetes/base/kustomization.yaml`, the `src/` and adapter test paths, and the
`render`/`verify`/`validate`-production-deployment tooling.

**Note:** `.github/workflows/ci.yml` is retained by Story 27.3 (it also carries adapter-lane
wiring); Story 30.1 owns only its release/publish steps. The exact hunk attribution is
reconciled at the chunk-3 final ledger row, alongside the already-deferred DW 27.3-CR4 recount.

> ⚠ **This edit does not by itself unblock Story 27.3.** Its remaining fail-closed blockers —
> chunk 3 unreviewed, live method/case recount not runnable in this sandbox, and the DW 27.3-CR4
> Server story/external split recompute (+1/+30 → +5/+26) — are untouched and still bind.

---

### Group F — New epic and story bodies

#### Edit 15 — Epic 30 and Story 30.1

**File:** `_bmad-output/planning-artifacts/epics.md` — append after the Epic 29 body (end of file)

**NEW (insertion):**
```
## Epic 30: Container Release Pipeline Ownership

The four-image container release and partial-recovery pipeline is owned, tested, and documented as
an independently demonstrable release lane rather than as incidental scope inside an
adapter-qualification story.

**Lifecycle label:** Operational Readiness / Release Engineering.

**Driven by:** Sprint Change Proposal 2026-07-26, executing DW 27.3-CR5 (split approved by the
Administrator on 2026-07-21 during Story 27.3 code review).

**Scope origin:** The pipeline artifacts already exist in the repository and were ledgered as an
external CI/CD lane while bundled into Story 27.3's single C1 adapter-qualification slice. This
epic gives them an owner, not a new implementation.

### Story 30.1: Four-Image Container Release and Partial-Recovery Pipeline

**Status:** backlog. **Owner:** Memories Maintainer.

As a maintainer,
I want the four-image container release and partial-recovery pipeline to be an independently
demonstrable, tested release lane,
So that release integrity is provable on its own evidence instead of inside an unrelated adapter
qualification.

**Acceptance Criteria:**

**Given** the four-image publish pipeline and its partial-recovery path,
**When** the release lane is exercised,
**Then** `tools/publish-containers.ps1`, `tools/complete-partial-release.ps1`, and
`tools/verify-container-registry.ps1` are covered by the `tests/tooling/publish_containers`
suites with named discovery commands and pass counts
**And** `.github/workflows/recover-partial-release.yml` and the release/publish steps of
`.github/workflows/ci.yml` are bound by `CiTestInventoryTests` assertions.

**Given** a publish run that fails after some images are pushed,
**When** partial recovery runs,
**Then** the remaining images publish without republishing or corrupting the already-pushed set
**And** the recovery path is evidenced by a named test result, not by inspection.

**Given** `docs/dev/release-runbook.md`,
**When** Story 30.1 completes,
**Then** the runbook documents the four-image expansion, the partial-recovery procedure, and the
registry-authorization contract.

**Given** registry push authorization,
**When** the pipeline authenticates,
**Then** the authorization mode in use is recorded, and any registry-side limitation that blocks
an authenticated push is captured as a named blocker with owner, consequence, and reopen trigger
rather than worked around silently.

**Known risk (carried, not yet re-verified):** a prior investigation recorded that the zot
registry rejected challenge-response push authentication — Docker and skopeo pushes returned 401
while a preemptive single-connection preflight probe succeeded — and suspected a registry-side
replica/state issue requiring server-side diagnosis. Re-verify this against the current registry
before treating the publish path as green; if it reproduces, record it as an accepted blocker per
the acceptance criterion above.

**Scope boundary:** Story 30.1 owns the release/publish lane only. Production deployment
rendering, verification, and evidence tooling
(`tools/render-production-deployment.ps1`, `tools/verify-production-deployment.ps1`,
`tools/validate-production-deployment-evidence.ps1`) remain owned by Story 27.3.
```

---

#### Edit 16 — Epic 31 and Story 31.1

**File:** `_bmad-output/planning-artifacts/epics.md` — append after Epic 30

**NEW (insertion):**
```
## Epic 31: OpenBao Secrets Platform and Runtime Secret-Store Migration

The deployed OpenBao `hexalith-keys` platform and the runtime Dapr `secretstore` migration from
Kubernetes Secrets to `hashicorp.vault` are owned, hardened, documented, and security-reviewed as
an independently deployable operations platform.

**Lifecycle label:** Operational Readiness / Secret Management Platform.

**Driven by:** Sprint Change Proposal 2026-07-26, executing DW 27.3-CR6 (split approved by the
Administrator on 2026-07-21 during Story 27.3 code review).

**Scope boundary:** Epic 29 owns the Aspire/AppHost-local OpenBao topology and provider-neutral
composition. Epic 31 owns the deployed-cluster platform and the runtime secret-store migration.
Neither epic's stories may be closed on the other's evidence.

**Scope origin:** The platform is already deployed and operational — the 2026-07-20 live probe
resolved the access-telemetry store through it. This epic formalizes ownership, hardening, and
documentation of a running platform; it does not stand one up.

### Story 31.1: OpenBao Platform Hardening and Runtime Secret-Store Migration

**Status:** backlog. **Owner:** Memories Maintainer + security reviewer.

As an operator and security reviewer,
I want the deployed OpenBao platform and the runtime Dapr secret-store migration owned by one
reviewable story,
So that the secret-management boundary is hardened and approved on its own evidence rather than
inside an adapter-qualification slice.

**Acceptance Criteria:**

**Given** the deployed OpenBao `hexalith-keys` platform,
**When** its topology is reviewed,
**Then** `deploy/openbao/values.yaml`, `namespace.yaml`, `service-account-hardening.yaml`, and
`smoke-test.yaml` are documented in `docs/operations/openbao.md` with their exact deployed
configuration
**And** the smoke test is runnable with a named command and recorded result.

**Given** the runtime `secretstore` component,
**When** the migration completes,
**Then** `deploy/kubernetes/base/dapr/secretstore.yaml` uses `hashicorp.vault` with the
`eventstore` and `memories` scopes
**And** every remaining Kubernetes Secret is documented as an unavoidable OpenBao bootstrap
credential or a direct pod input outside the DAPR secret-store boundary (NFR9).

**Given** the single-node deployment profile,
**When** the security reviewer evaluates it,
**Then** the static file-based OpenBao seal — the unseal key held in a Kubernetes Secret beside
the data — and the namespace-wide port 8200 ingress are surfaced explicitly as accepted
single-node limitations with owner, consequence, compensating controls, and a reopen trigger
**And** neither limitation is described as hardened or production-HA.

**Given** secret-resolution behavior,
**When** structural and integration tests run,
**Then** no product project contains an OpenBao SDK, HTTP client, endpoint, or provider
credential, and secret values are never exposed in logs, telemetry, CLI output, or test snapshots
(NFR9, project-context "Never expose secrets").
```

---

### Group G — Status and ledger registration

#### Edit 17 — `development_status` rows for the new epics

**File:** `_bmad-output/implementation-artifacts/sprint-status.yaml` — after the `epic-29` block
(line 439)

**NEW (insertion):**
```yaml
  # Epic 30: Container Release Pipeline Ownership
  # Registered on 2026-07-26 by the approved Sprint Change Proposal 2026-07-26, executing the
  # DW 27.3-CR5 split approved by the Administrator on 2026-07-21 during Story 27.3 code review.
  epic-30: backlog
  30-1-four-image-container-release-and-partial-recovery-pipeline: backlog
  epic-30-retrospective: optional

  # Epic 31: OpenBao Secrets Platform and Runtime Secret-Store Migration
  # Registered on 2026-07-26 by the approved Sprint Change Proposal 2026-07-26, executing the
  # DW 27.3-CR6 split approved by the Administrator on 2026-07-21 during Story 27.3 code review.
  # Epic 29 owns Aspire-local topology; Epic 31 owns the deployed-cluster platform.
  epic-31: backlog
  31-1-openbao-platform-hardening-and-runtime-secret-store-migration: backlog
  epic-31-retrospective: optional
```

Also update the file header `# last_updated:` and the `last_updated:` key from `2026-07-21` to
`2026-07-26`.

---

#### Edit 18 — Resolve the two deferred-work entries

**File:** `_bmad-output/implementation-artifacts/deferred-work.md:2299` and `:2319`

**OLD (DW 27.3-CR5, line 2299):**
```
  - Status: open
```

**NEW:**
```
  - Status: resolved 2026-07-26 — split executed by the approved Sprint Change Proposal 2026-07-26 into Epic 30 / Story 30.1 (`epics.md`), registered in `sprint-status.yaml`, and removed from Story 27.3's File List.
```

**OLD (DW 27.3-CR6, line 2319):**
```
  - Status: open
```

**NEW:**
```
  - Status: resolved 2026-07-26 — split executed by the approved Sprint Change Proposal 2026-07-26 into Epic 31 / Story 31.1 (`epics.md`), registered in `sprint-status.yaml`, and removed from Story 27.3's File List. The static file-based seal and namespace-wide 8200 ingress are carried into Story 31.1's security-approval acceptance criterion.
```

**Rationale:** Both entries name "before Story 27.3 advances to done" as their reopen trigger and
`epics.md` as their target artifact. This proposal satisfies both.

---

## 5. Implementation Handoff

### Scope classification: **Moderate**

Backlog reorganization is required — two epics are created, two stories are registered, and one
in-progress story's scope is reduced. That exceeds Minor (direct developer implementation) but
does not approach Major: no requirement changes, no architecture decision is revisited, no MVP
scope moves, and the PRD needs no edit.

### Handoff

| Recipient | Responsibility |
| :--- | :--- |
| **Developer agent** | Apply Edits 1–18 exactly as specified. Preserve CRLF in all `.md` files; keep `sprint-status.yaml` LF. Make no source, test, CI, or deployment change. |
| **Product Owner / Administrator** | Confirm the Epic 30/31 numbering and the Epic 29 ↔ Epic 31 scope boundary. Confirm that Edit 11's guard generalization is wanted, or drop it. |
| **Story 27.3 owner** | Own the 14 C1 gate rows going forward. Do not mark any row complete without its named evidence observation in the adapter-profile packet. |
| **Security reviewer** | Note the two limitations carried into Story 31.1 (static file seal; namespace-wide 8200 ingress). They are now tracked rather than embedded in an adapter story. |

### Success criteria

1. `epics.md` contains no statement that hybrid composite scores are a weighted average of
   normalized axis scores. `grep -n "weighted average" epics.md` returns nothing in Epic 2.
2. `epics.md` NFR24 and NFR9 inventory entries match `prd.md`.
3. `sprint-status.yaml` `readiness_accounting` covers every epic 0–31 with no gaps in either
   `excluded_unless_sprint_selected` or `epic_metadata`.
4. Story 27.3's C1 checkpoint has 14 independently reviewable gate rows, all `pending`.
5. Story 27.3's File List contains no `deploy/openbao/`, `tests/tooling/publish_containers/`, or
   `recover-partial-release` path.
6. `deferred-work.md` DW 27.3-CR5 and DW 27.3-CR6 are `resolved` with pointers to Stories 30.1
   and 31.1.
7. Story 27.3 remains `in-progress` and Production writes remain disabled. **Nothing in this
   proposal advances any story status.**

---

## 6. Explicitly Deferred (not in this proposal)

Carried forward from the readiness report, unresolved by this change, and requiring a separate
decision:

| ID | Finding | Suggested trigger |
| :--- | :--- | :--- |
| H1 | 14 FRs (19%) have no acceptance-criterion ID anchor, including FR14–FR17 (the three axes plus hybrid fusion) and FR46/FR47 | Before any Epic 1–5 story is reopened |
| H2 / Q3 | **Epic 19** remains unclassified by the Implementation Readiness Boundary and uncovered by the operational-track exemption. Epics 27–31 are resolved by Edit 10. | Governance hygiene pass |
| M1 | Six epics-side NFR restatements silently drop verification detail (NFR8 graph-collision design, NFR5 batching, NFR6 rate-limit degradation, NFR12 methodology, NFR25 tie-order, NFR29 queue depth) | Same pass as H1; consider replacing the transcribed inventory with a pointer to the PRD to stop the drift recurring |
| M2 | Phase 2 Backlog Placeholders sit between Epic 8 and Epic 9, inside the MVP reading path | Cosmetic; governance itself is sound |
| M3 | FR26/FR38 are ID-anchored only at their Epic 0 thin slices | With H1 |
| U1–U5 | UX spec has no requirement identifiers; content frozen 2026-06-24; UX-DR17 specifies a hybrid "raw score" the fusion model does not produce; the NL axis appears zero times; PRD Journey 8 is unowned | **Before any Epic 17 web UI story is sprint-selected.** U1, U3, U4 must all close first. |
| Q1 | AC density collapses ~4× in Epics 20–25 (45 stories at ~1.1 ACs/story) | Do not retrofit. State a standing AC-granularity rule for new stories instead. |
| Q4–Q6 | Epic 0 AC density; intentional numeric story gaps (8.3, 12.7, 12.8); three coexisting AC styles with no stated rule | Low priority |
| P1 | PRD states server runtime "C# 13"; `project-context.md` states C# 14 | One-line PRD fix, any time |

**Also unchanged and still blocking Story 27.3:** chunk 3 (tooling + CI) unreviewed; live
method/case recount not runnable in this sandbox; DW 27.3-CR4 Server story/external split
recompute (+1/+30 → +5/+26).

---

## 7. Change Analysis Checklist Record

| § | Item | Status | Finding |
| :-- | :--- | :--- | :--- |
| 1.1 | Triggering story identified | ✅ Done | Not a single story — the 2026-07-25 readiness assessment plus two open `[Review][Action]` rows in Story 27.3 |
| 1.2 | Core problem defined | ✅ Done | Category: **misunderstanding/drift of original requirements** — document set diverged after ~4 months of approved sprint changes |
| 1.3 | Impact and evidence gathered | ✅ Done | Verified against source in all four documents plus the shipped implementation (§1) |
| 2.1 | Current epic completable as planned | ✅ Done | Yes. Epic 27 completes unchanged; its story sheds out-of-slice scope |
| 2.2 | Epic-level changes required | ⚠️ Action-needed | **Two new epics added** (30, 31) for already-approved splits. No epic modified in scope, removed, or redefined |
| 2.3 | Remaining epics reviewed | ✅ Done | Epics 28, 29 gain governance registration only. Epic 2 is `done`; its text is reconciled, not its scope |
| 2.4 | Issue invalidates future epics / needs new ones | ✅ Done | No epic invalidated. Two new epics needed — see 2.2 |
| 2.5 | Epic order or priority change | ✅ N/A | No resequencing. New epics are `backlog`, excluded unless sprint-selected |
| 3.1 | PRD conflicts | ✅ Done | **None.** PRD is the current side on every finding. No PRD edit proposed |
| 3.2 | Architecture conflicts | ✅ Done | **None.** `architecture.md:92` is the authority C1 reconciles *to* |
| 3.3 | UI/UX conflicts | ⚠️ Action-needed | U1–U5 confirmed open; **deferred** — binding only when Epic 17 is sprint-selected (§6) |
| 3.4 | Other artifacts | ✅ Done | `sprint-status.yaml`, Story 27.3 file, `deferred-work.md`. No deployment/IaC/CI/test artifact changes |
| 4.1 | Option 1 — Direct Adjustment | ✅ **Viable — selected** | Effort Low / Risk Low |
| 4.2 | Option 2 — Rollback | ❌ Not viable | Would revert working weighted-RRF code to match stale prose — inverts the fix |
| 4.3 | Option 3 — MVP Review | ✅ N/A | MVP unaffected; all 74 FRs still covered |
| 4.4 | Path selected | ✅ Done | Hybrid: Direct Adjustment + new-epic registration for pre-approved splits |
| 5.1–5.5 | Proposal components | ✅ Done | §1–§5 of this document |
| 6.1 | Checklist reviewed | ✅ Done | This table |
| 6.2 | Proposal accuracy verified | ✅ Done | Every line/quote re-read from source; one readiness-report claim corrected (Q2 table already exists) and one count corrected (14 gates, not ~18) |
| 6.3 | User approval | ⏳ Pending | Awaiting explicit approval |
| 6.4 | `sprint-status.yaml` updated | ⏳ Pending approval | Edits 7, 8, 17 |
| 6.5 | Handoff confirmed | ⏳ Pending approval | §5 |

---

## 8. Approval

- [ ] **Approved** — proceed with Edits 1–18
- [x] **Approved with changes** — note exceptions below
- [ ] **Rejected / revise**

**Approver:** Administrator (Jerome)  **Date:** 2026-07-26

**Notes:**

Approved and executed on 2026-07-26 with three corrections agreed during execution:

1. **File List arithmetic corrected.** §2 and Edit 14 state "13 paths move out"; the enumerated set
   is **16**, and all 16 were verified present in the 52-path File List. The result of record is
   **52 - 16 = 36 remaining**, not 39. Recorded in the story's File List scope-change note.
2. **Drifted line anchors.** Edit 4 cites `epics.md:1180` (actual 1169) and Edit 18 cites
   `deferred-work.md:2319` (actual 2328, shifted by the DW 27.3-CR7 entry added earlier the same
   day). Both were applied by unique-text match rather than line number; target text unchanged.
3. **Ledger reconciliation added.** The proposal did not address the interaction with the
   `dev-story` row appended earlier on 2026-07-26 recording `matched 52/52`. That append-only row
   is preserved unrewritten; a dated scope-change note in the File List section records that the
   reconciliation baseline for all subsequent phases is 36. No non-canonical ledger phase row was
   appended, per the story phase ledger's fixed phase vocabulary.

**Edit 11** (checkpoint-guard generalization) was explicitly confirmed and applied.
**Edit 5b** (Story 22.3 ambiguity) was left as-is per the proposal's own recommendation.

Story 27.3 remains `in-progress`; Production writes remain disabled; no story status advanced.
