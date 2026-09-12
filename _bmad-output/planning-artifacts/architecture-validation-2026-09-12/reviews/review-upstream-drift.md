# Review — Upstream Source Drift Lens

**VERDICT: FAIL** — the spine (`status: final`, 2026-09-09) cannot be used as the current architecture authority: it contradicts the approved MVP/Phase 2/Phase 3 migration boundary (AD-14), is structurally invisible to the ratified hard gate G6, and stops binding at FR74/NFR36/G5 while the 2026-09-12 product contract runs to FR75/NFR37/G6.

**Lens:** Upstream source drift (Validate intent — read-only critique).
**Target:** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` (sha256 `e37070e372884925087b869bf8c96741849046b5d474f344dee8f32db77c918e`).
**Date:** 2026-09-12. **Reviewer scope:** this file only; no other file was modified, no build or test was run, nothing was staged.

## Findings summary

| Severity | Count |
| :-- | :-- |
| critical | 2 |
| high | 3 |
| medium | 10 |
| low | 5 |
| **total** | **20** |

| Classification | Count |
| :-- | :-- |
| SPINE-STALE | 17 |
| GENUINE CONFLICT | 1 |
| UPSTREAM-WRONG | 1 |
| COSMETIC | 1 |

## Sprint-change-proposal assertion verdicts (item 5)

The proposal at `sprint-change-proposal-2026-09-12.md` is an untracked draft (`:5` — "Status: Draft"). Its three claims about the spine were independently verified against the actual spine and PRD text.

| # | Assertion (`sprint-change-proposal-2026-09-12.md:45`, `:50`) | Verdict | Evidence |
| :-- | :-- | :-- | :-- |
| A1 | AD-14 is left unphased and conflicts with the PRD's approved MVP/Phase 2/Phase 3 migration boundary | **CONFIRMED** | `ARCHITECTURE-SPINE.md:134-138` carries one staged create-backfill-verify-switch-retire rule with no phase qualifier; `prd.md:273` places embedding versioning/model migration in Phase 2, `prd.md:280` places backend (Qdrant) migration in Phase 3, `prd.md:1056` keeps MVP FR43 at explicit-acknowledgement degraded rebuild, `prd.md:1227` records it as `[PHASE-BLOCKER / G6]` Open Question 9, and `addendum.md:85-88` records the conflict without manufacturing an exception. See DRF-01. |
| A2 | The spine's binding header and trace maps must extend through FR75/NFR37/G6 | **CONFIRMED, with a qualification** | `ARCHITECTURE-SPINE.md:12-14` binds `FR1-FR74` / `NFR1-NFR36` / `G1-G5`; a grep of the whole spine returns zero occurrences of `FR75`, `NFR37`, or `G6`. Qualification: the spine has **no trace map to extend** — there is no FR→AD table and no gate→AD table anywhere; traceability exists only as per-AD `Binds:` lines and the `Capability → Architecture Map` (`:274-286`). The correct fix is therefore to add `Binds:` entries plus capability-map rows, not to edit a table that does not exist. Second qualification: FR75's *substance* is already fully adopted in AD-4 (`:78`) and AD-3 (`:72`) — for FR75 the header/trace is the only real gap. NFR37 and G6 are **not** covered anywhere and need new normative text, not an ID refresh. See DRF-03, DRF-04, DRF-20. |
| A3 | Every Current Alignment Gap must acquire an owner, evidence path, resolved verdict, or approved phase exception | **CONFIRMED, with a scope qualification** | The gap table (`ARCHITECTURE-SPINE.md:288-308`) has exactly three columns — Gap / Violated rule / Required convergence — and no owner, evidence, verdict, or exception column on any of its 14 rows. G6 (`prd.md:189`) requires "every gap/exception has an owner and tracking entry in `sprint-status.yaml`", so the obligation is the PRD's, not the proposal's. Qualification: G6's wording scopes this to "every **architecture-critical active-foundation** gap", not literally every row — the proposal's own `EX-OP-01` disposition (`sprint-change-proposal-2026-09-12.md:149`) concedes this. A classification step (which rows are active-foundation critical) must precede the owner column, or the spine will over-scope G6. See DRF-02. |

## Crosswalk

| Source requirement | What it requires | Spine coverage | Classification | Severity |
| :-- | :-- | :-- | :-- | :-- |
| **FR75** (`prd.md:1017`) | Epoch-aware durable idempotency: one durable mutation per tenant/case/operation token and per tenant/case/source/id; one projection outcome per source-version/schema-generation/embedding-configuration tuple; a new epoch is not suppressed | **AD-4** (`:74-78`) + **AD-3** (`:68-72`) — substance fully adopted; **NONE** in `binds:` (`:12`), in any AD `Binds:` line, or in the capability map | SPINE-STALE | high |
| **NFR37** (`prd.md:1215`) | Active CLI accessibility: stable reading order, text label for every state/axis/score/omission/stage/recovery, bounded+wrappable output, linear table alternative, deterministic redirected output free of terminal control sequences, durable progress/failure lines, explicit cancellation/timeout, no second "created" line on duplicate delivery, keyboard-only operability, secret/restricted-identifier suppression, human/table/JSON/stderr/exit-code parity | **NONE** — zero spine matches for accessibility, keyboard, WCAG, colour, reading order, or exit code; AD-12 (`:126`) covers only Evidence Packet field semantics; the Errors convention (`:178`) covers only code/message/suggestion | SPINE-STALE | high |
| **G6** (`prd.md:189`) | MVP contract closure: every unverified MVP requirement and every architecture-critical active-foundation gap carries current evidence or an approved phase exception, with owner + tracker entry | **NONE** — gap table (`:288-308`) has no owner/evidence/verdict/exception column; `:310` still enumerates only "G1-G5" | SPINE-STALE | critical |
| **Phase register + Phase 2/3 migration** (`prd.md:982`, `:273`, `:280`, `:1056`) | MVP FR43 = acknowledged degraded rebuild; Phase 2 = zero-downtime embedding/schema migration; Phase 3 = backend migration | **AD-14** (`:134-138`) — unphased, so it reads as binding MVP today | GENUINE CONFLICT | critical |
| **NFR9** (`prd.md:1142`) vs **CLI config layering** (`prd.md:927`) | One Dapr/OpenBao secret path; direct Redis/FalkorDB injection is an alignment gap, not a second path | **AD-15** (`:144`) states the correct rule; the PRD's own `:927` still permits "direct pod inputs that DAPR cannot provide" | UPSTREAM-WRONG | medium |
| **NFR13** (`prd.md:1151`) | ≤5 s admission for every eligible non-empty tenant queue after capacity exists; 1,000/10,000 pending caps; first item beyond a cap **rejected** within 1 s with retry guidance; accepted work never dropped | **AD-18** (`:158-162`) — "bounded durable queues" only; numbers delegated to the PRD, but reject-not-drop and queue-admission liveness are behavioural, not numeric | SPINE-STALE | medium |
| **Evidence Packet states** (`prd.md:92`, `:571`; `EXPERIENCE.md:145`, `:156`) | Versioned vocabulary `complete/partial/weak/empty/stale/degraded/unauthorized/pendingExpansion`; surfaces must not fabricate an unversioned state (e.g. `Conflicting`) from degradation | **AD-12** (`:126`) names required *fields*, never the state vocabulary | SPINE-STALE | medium |
| **NFR33** (`prd.md:1201`; `EXPERIENCE.md:175`) | `current/aging/stale/unknown` thresholds, transitions, disclosure, recovery, versioned in the packet contract and activated per surface | **AD-12** (`:126`) lists "freshness" as a carried field; no owner for the vocabulary or thresholds | SPINE-STALE | medium |
| **FR39 erasure limit** (`prd.md:546`) | Cross-references to a deleted tenant's data held in *other* tenants' units are the application's responsibility, documented as a limitation | **AD-16** (`:146-150`) is silent, so it over-claims "complete tenant erasure" | SPINE-STALE | medium |
| **Embedding provider config** (`prd.md:799`, `:815`, `:820`) | MVP Google `text-embedding-004`, 768 dimensions; dimension fixes the Redis Vector index schema, so a provider switch is a migration | **Stack** (`:196-214`) pins Redis/Falkor/OpenBao/MCP/Kreuzberg but not provider/model/dimension, although AD-14 `Binds:` (`:136`) claims exactly that axis | SPINE-STALE | medium |
| **FR49/FR51/FR52 + edge semantics** (`prd.md:590`, `:602`, `:604`, `:1067-1070`) | Explicit `[MISSING: id]` gap markers, chronological ordering/timestamps, no auto-promotion of AI-inferred edge confidence, `caused_by` never collapsed into `correlated_with` | **AD-11** (`:116-120`) bounds injection/scope/depth/labels/kill switch and asserts none of these data-accuracy invariants | SPINE-STALE | medium |
| **NFR32 / NFR35** (`prd.md:1200`, `:1203`; `EXPERIENCE.md:266-272`) | WCAG 2.2 AA on activation; dated route/state evidence matrix (resize, reflow, forced colours, reduced motion, NVDA, SC 2.5.8); web performance budgets; specimen evidence never transfers to a product route | **NONE** — inside the bound range `NFR1-NFR36` yet uncovered by any AD, convention, or boundary; Deferred row `:319` names host duties only | SPINE-STALE | medium |
| **Web conformance specimen** (`prd.md:724`; `EXPERIENCE.md:42`, `:103`; `reconcile-latest-architecture.md:182`) | A runnable conformance host exists and is not a product surface | **Gap row `:307` and seed `:230` claim the opposite** (non-runnable RCL) | SPINE-STALE | high |
| **Spine active sources/companions** (`:19`, `:30`) | `architecture.md` is superseded (`addendum.md:144`, `reconcile-latest-architecture.md:193`); the traceability companion predates FR71-FR75/NFR36-NFR37 | Both still listed as active inputs of a `final` document | SPINE-STALE | medium |
| **Downstream citability** (`epics.md:3-9`) | Epics must derive from, and cite, the spine | Spine declares `epics.md` a companion (`:29`) but exposes no requirement→AD surface to cite | SPINE-STALE | medium |
| **NFR11** (`prd.md:1144`) | Anonymous ingress exceptions are a finite, named, tested set | Routes convention (`:176`) names `/events/ingest` only; no enumerate-and-test rule | SPINE-STALE | low |
| **NFR31 / G3** (`prd.md:1194`) | Clean-machine README/AppHost → first CLI search under 30 minutes, image pulls on the clock | Operational Boundaries (`:261-272`) has no onboarding/local-composition dimension | SPINE-STALE | low |
| **NFR21** (`prd.md:1169`) | Non-EventStore-convention CloudEvent publishers are an experiment, honoured only for convention-defined fields, with a documented negative test | AD-2 (`:66`) / AD-4 (`:78`) validate `source` but set no acceptance boundary for generic publishers | SPINE-STALE | low |
| **Licensing de-risk** (`prd.md:626`, `:628`) | FalkorDB AGPL image pinning in AppHost *and* production artifacts; SSPL managed-service constraint documented | Gap `:305` attributes floating images to AD-19 only; Deferred `:324` covers managed-service posture but not the pin duty | SPINE-STALE | low |
| **AD-4 epoch clarity / gate enumeration** (`addendum.md:58`; `prd.md:1017`) | State that a legitimate reprojection under a new epoch is not suppressed; gates are G1-G6 | AD-4 (`:78`) implies it by keying only; `:310` says "G1-G5" | COSMETIC | low |
| *(verified aligned)* **V1 status wire contract** (`prd.md:98`; `EXPERIENCE.md:160-167`) | `queued/extracting/embedding/indexing/indexed/failed`; `pending`≙`queued`, `projecting`≙`indexing` | **`:177` matches word for word** | — | none |
| *(verified aligned)* **AD-9 fusion** (`addendum.md:22-23`) | Preprocessing, `Double.Equals`, ranks `1,1,3`, `1/(10+rank)`, `0.30/0.35/0.35`, NL `0.20` default-off, tenant-wide case merge | **`:108` matches** | — | none |
| *(verified aligned)* **AD-3 tuple** (`addendum.md:57`) | Six-field tuple, single Dapr-state coordinator, monotonic ETag/CAS, stale-ack rejection, shared across ingest/repair/replay | **`:72` matches** | — | none |
| *(verified aligned)* **AD-4 idempotency** (`addendum.md:58`) | Token scope, CloudEvent identity, ordinal comparison, no post-validation normalisation, fail-open Redis preflight | **`:78` matches** | — | none |

## Findings

### DRF-01 — AD-14 is unphased and therefore rebaselines MVP by silence — **critical** — GENUINE CONFLICT

**Spine side:** `ARCHITECTURE-SPINE.md:134-138`. AD-14 is `[ADOPTED]` with `Binds: Embedding provider/model/dimension, index and key families, graph/search replacement, reindexing, and provider strategies` and the rule "Use versioned create-backfill-verify-switch-retire migrations with disjoint active and staging resources, reindex the full tenant corpus before atomic activation". Nothing in the rule, the Deferred table (`:312-324`), or the gap ledger scopes it to a phase. Combined with the spine's own preamble at `:52` ("Rules below are the adopted target architecture. Where current code differs, the rule remains binding"), the literal reading is that staged non-destructive migration binds MVP now.

**Upstream side:** `prd.md:273` puts "Embedding versioning and model migration (<5% relevance degradation, zero downtime)" in Phase 2; `prd.md:280` puts the Qdrant/backend migration path in Phase 3; `prd.md:1056` keeps MVP FR43 at "refuses embedding-provider or index-schema changes that require reindex unless the operator passes an explicit acknowledgment flag; the CLI states that existing vectors will be rebuilt"; `prd.md:1227` records Open Question 9 as `[PHASE-BLOCKER / G6]`; `addendum.md:87-88` states the conflict and refuses to manufacture an exception; `addendum.md:138` records "Pull architecture AD-14 into MVP without a scope decision" as a rejected alternative; `_bmad-output/planning-artifacts/.memlog.md:77` records the product-side decision "Preserve approved migration phasing: architecture AD-14 is the Phase 2/3 non-disruptive migration target; it does not silently rebaseline MVP FR43."

**Why this is a conflict, not staleness:** the product side has decided (`.memlog.md:77`), but `prd.md:1227` names two admissible resolutions — qualify AD-14 by phase, or approve an MVP rebaseline — and assigns the owner as "Architecture + Jerome". Only a recorded human ratification closes it. Until then the spine's `status: final` asserts an MVP obligation the product contract does not fund.

**Exact decision required:** Jerome + Architecture record one of — (a) phase-qualify AD-14 as the MVP/Phase 2/Phase 3 contract, which is the product-decided direction; or (b) approve an MVP rebaseline that expands FR43, FR68-FR70, the delivery register, the release gates, and the epic breakdown.

**Recommended spine edit (option (a)), replacing the AD-14 rule at `:138`:**

> - **Rule:** Migration obligations are phase-qualified and no phase's evidence satisfies another's. In **Phase 1/MVP**, FR43 permits only a tenant-scoped, explicitly acknowledged degraded rebuild of rebuildable projections; the command discloses impact and progress, fails closed on incomplete verification, preserves authoritative EventStore truth, carries the AD-3 configuration epoch, and never claims zero downtime. In **Phase 2**, embedding provider/model/dimension and index-schema changes use versioned create-backfill-verify-switch-retire migrations with disjoint active and staging resources, full-tenant reindex before atomic activation, and no availability break. In **Phase 3**, backend replacement uses the same staged cutover at the adapter boundary. Across all phases, keep embedding and LLM implementations behind strategy ports; projection evidence carries the active configuration epoch and activities resolve secret references at execution time so rotation can retry safely. No MVP implementation or completed historical migration is retroactively claimed to meet the Phase 2/3 contract.

Also add the ratification to the frontmatter (`updated:` and an approval note) and a Deferred/Exception row naming the approver and date.

---

### DRF-02 — G6 is invisible to the spine and the gap ledger is structurally incapable of satisfying it — **critical** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:288-308` — the "Current Alignment Gaps" table has three columns (Gap | Violated rule | Required convergence) across 14 rows, with no owner, no evidence path, no verdict, and no exception id on any row. `:290` frames them as "implementation obligations against adopted decisions". `:310` closes the section with "Architecture alignment does not imply that G1-G5 or L1-L3 product gates have passed" — G6 is absent from the enumeration and from the whole document.

**Upstream side:** `prd.md:189` makes G6 a **hard gate**: "every MVP requirement not marked *Verified against its current wording* … has current evidence or a phase-specific exception approved by product and architecture. Every architecture-critical active-foundation gap follows the same rule; every gap/exception has an owner and tracking entry in `sprint-status.yaml`". `prd.md:191` — "All six gates are hard gates; there is no soft tier." `prd.md:213` assigns "Architecture for binding/exception". `prd.md:62` puts the release posture at no-go partly because of this. `addendum.md:11` repeats the handoff. `EXPERIENCE.md:44` states "G6 cannot pass while missing work/evidence lacks successor-story ownership, AD-14 phasing is unratified, or architecture has not bound FR75 and NFR37."

**Consequence:** the architecture side of a ratified hard gate has no artefact. A reviewer cannot read the spine and determine, for any gap, who owns it, what evidence would close it, or whether an exception was approved.

**Recommended spine edit:** (1) change `:310` to "Architecture alignment does not imply that G1-G6 or L1-L3 product gates have passed."; (2) re-issue the gap table with five columns — `Gap | Violated rule | Required convergence | Disposition (owner / evidence path / resolved verdict / approved exception id) | Active-foundation critical (yes/no)` — where the last column is the AD-level classification G6 needs, since `prd.md:189` scopes the owner obligation to architecture-critical active-foundation gaps and not to release/operational debt; (3) add a normative decision that makes gate evidence architecture-governed rather than a table convention, for example:

> ### AD-20 — Treat release gates as evidence contracts [ADOPTED]
>
> - **Binds:** G1-G6, L1-L3, the alignment-gap ledger, phase exceptions, and every claim of current qualification.
> - **Prevents:** Historical `done` tracking state, phase-inactive surfaces, or an unowned alignment gap being read as current gate evidence.
> - **Rule:** Every governed requirement and every gap classified active-foundation critical carries exactly one of a current evidence path or a dated phase exception approved by product and architecture, each with a named owner and a tracker entry. Phase-inactive surfaces earn no gate credit even when their assets exist. A row leaves the ledger only on a `confirmed resolved` verdict backed by re-runnable evidence.

---

### DRF-03 — The binding header stops at FR74 / NFR36 / G5 — **high** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:11-15`:

```
binds:
  - 'FR1-FR74'
  - 'NFR1-NFR36'
  - 'G1-G5'
  - 'L1-L3'
```

A whole-file grep returns **zero** occurrences of `FR75`, `NFR37`, or `G6`.

**Upstream side:** `prd.md:40` — "capabilities (FR1–FR75), and cross-cutting quality (NFR1–NFR37)"; `prd.md:1228` — Open Question 10, `[PHASE-BLOCKER / G6]`, "The final architecture spine binds only FR1–FR74/NFR1–NFR36; it must bind and trace FR75's epoch-aware durable-idempotency contract and active-CLI accessibility NFR37 before G6"; `addendum.md:155` — the same handoff; `reconcile-latest-architecture.md:34` and `:193` predicted it; `.memlog.md:103` records it as a logged gap.

**Whether the header is the only gap — asked explicitly, answered per id:**

- **FR75 — yes, header only.** `prd.md:1017` requires (a) operation-scoped V1 token identity, (b) tenant/case/exact-source/id CloudEvent identity, (c) one idempotent projection outcome per source-version/schema-generation/embedding-configuration tuple with a new epoch *not* suppressed, (d) no second memory unit on duplicate delivery. AD-4 (`:78`) already states (a), (b) with ordinal comparison and no post-validation normalisation, (d) via "durable EventStore/workflow duplicate suppression", and (c) via "make every projection an idempotent upsert for AD-3's tuple" — AD-3 (`:72`) supplies the tuple. Only the non-suppression of a *new* epoch is implicit rather than stated (see DRF-19). So for FR75 the substantive architecture already exists and the header plus `Binds:` lines are the gap.
- **NFR37 — no.** See DRF-04: nothing in the spine covers it, so the header refresh alone would create a false claim of coverage.
- **G6 — no.** See DRF-02.

**Recommended spine edit:** replace `:12-14` with

```
  - 'FR1-FR75'
  - 'NFR1-NFR37'
  - 'G1-G6'
```

and add FR75 to the `Binds:` lines of AD-2 (`:64`), AD-3 (`:70`) and AD-4 (`:76`), NFR37 to AD-12 (`:124`), and G6 to the new AD-20. Add capability-map rows (`:274-286`), for example:

> | Durable command and event idempotency | Contracts.V1 idempotency token, EventStore acceptance boundary, workflow suppression, projection upserts | AD-2, AD-3, AD-4, AD-14 |
> | Active CLI surface semantics and accessibility | CLI command tree, shared output formatters, progress/error paths, exit-code map | AD-12, AD-13, AD-15 |
> | Release-gate evidence closure | Gap ledger, exception register, evidence artifacts, tracker entries | AD-19, AD-20 |

---

### DRF-04 — NFR37 has no architectural home anywhere in the spine — **high** — SPINE-STALE

**Spine side:** a whole-file case-insensitive grep for `exit code`, `accessib`, `keyboard`, `WCAG`, `reading order`, and `color` returns **no matches**. The nearest partial coverage is AD-12 (`:122-126`, cross-surface Evidence Packet *field* semantics), the Errors convention (`:178`, "stable code, human message, and actionable suggestion"), the Configuration convention (`:179`, flags → environment → config file), and the structural note at `:228` ("Operator surface; incomplete commands fail explicitly").

**Upstream side:** `prd.md:1215` (NFR37, **MVP**, status not started at `prd.md:1121`) and `EXPERIENCE.md:255-264` and `:71`. The obligations with **no** spine counterpart are: the stable reading order scope → result → sources → reasoning/state → recovery; a text label for every state, axis, score meaning, omission, progress stage, and recovery, with colour/glyph/motion/position supplementary only; bounded and wrappable human output; a complete linear alternative for wide tables; deterministic redirected output that does not depend on terminal-control sequences; durable progress stage/failure lines with last update and delay reason; explicit cancellation and timeout; no second "created unit" line on duplicate delivery (the surface half of FR75); keyboard-only completion of prompts, confirmations, help, and error recovery; suppression of secrets *and restricted identifiers* from copied output, accessible names, diagnostics, and suggestions; and semantic parity across human, table, JSON, stderr, and exit code. `EXPERIENCE.md:71` additionally pins the machine-readable half — exit codes `0/1/2/4/130`, the `{ schemaVersion, command, data }` / `{ schemaVersion, command, error }` envelope with exactly one of `data`/`error`, JSON errors to stdout, and the documented export/cancellation exceptions. `prd.md:1077` (FR56) pins the nine minimum recovery conditions and names `CliExitCodes` as the machine-readable side.

**Why this matters to architecture and not only to UX:** an exit-code map, a JSON envelope shape, and a cross-format semantic-parity guarantee are public contract surface — exactly what AD-12 exists to own — yet AD-12's rule only reaches "Evidence Packet semantics".

**Recommended spine edit:** extend AD-12's rule at `:126` with a sentence, and add a Consistency Convention row after `:178`.

> …Capability subsets may vary operations, never Evidence Packet semantics. Each active surface additionally publishes an operator-observable output contract: a stable presentation order, a text label for every state, axis, score meaning, omission, progress stage and recovery, a machine-readable envelope and exit-code map, and identical semantics across every emitted form; colour, glyph, motion, and screen position are supplementary only.

> | Active CLI output contract | Reading order is scope → result → sources → reasoning/state → recovery. Human output is bounded and wrappable and every wide table has a complete linear alternative; redirected output is deterministic and free of terminal-control sequences. Progress emits durable stage/failure lines with last update and delay reason; cancellation and timeout are explicit; duplicate delivery never emits a second created unit. Prompts, confirmations, help, and error recovery are operable by keyboard alone. Secrets and restricted identifiers never enter emitted output, accessible names, diagnostics, or suggestions. Human, table, JSON, stderr, and exit-code forms preserve the same semantics; `CliExitCodes` and the JSON envelope are versioned contract surface under AD-12. |

---

### DRF-05 — The "Web is a non-runnable RCL" alignment gap is factually stale — **high** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:307` — "Web is currently a non-runnable RCL while the PRD requires a runnable conformance specimen; product Web remains future. | AD-12 | Add the conformance host without activating a product UI". Reinforced by the structural seed at `:230` — "Current RCL; target runnable conformance specimen, not product UI".

**Upstream side:** `prd.md:724` lists `Hexalith.Memories.Web` as "Epic 17 conformance specimen (**runnable web shell** for accessibility/telemetry conformance); **not an activated product surface**". `EXPERIENCE.md:42` — "`Hexalith.Memories.Web` specimen host | **Implemented** conformance evidence only". `EXPERIENCE.md:103` — "The Web conformance specimen is excluded from product IA: it is a fixture-backed validation surface, not an entry point." `reconcile-latest-architecture.md:182`, written the day the PRD was updated, lists it under **Already Aligned**: "The Web project remains a runnable conformance specimen rather than an activated product UI".

**Repository evidence (existence only; no build was run):** `tests/Hexalith.Memories.Web.SpecimenHost/Hexalith.Memories.Web.SpecimenHost.csproj:1` is `<Project Sdk="Microsoft.NET.Sdk.Web">` and project-references `src/Hexalith.Memories.Web`; `tests/Hexalith.Memories.Web.SpecimenHost/Program.cs:10-25` builds a `WebApplication`, adds Razor components with interactive server rendering plus Fluent UI, and maps the specimen route prefix; the project is registered in `Hexalith.Memories.slnx:34` alongside `Hexalith.Memories.Web.E2E`. `git log --reverse` on that directory shows it landed on **2026-07-06** (`8b9439dd`, "test(web): add Epic 17 browser specimen lane") — two months *before* the spine was finalised, so this row was already wrong at `final`.

**Note on a second stale artefact:** `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj:4-6` still carries the comment "this RCL … has no runnable host yet", which is presumably where the spine's claim came from. That comment is source-side debt and out of this lens's write scope, but it should not be re-cited as evidence.

**Recommended spine edit:** replace the `:307` row and correct `:230`.

> | Web conformance evidence is hosted from `tests/Hexalith.Memories.Web.SpecimenHost` while `src/Hexalith.Memories.Web` remains a non-packable RCL. | AD-12 | Resolved for host existence (2026-07-06). Re-run the browser/AT specimen evidence before recording `confirmed resolved`; product Web stays future and specimen evidence never transfers to a product route. |

> `Hexalith.Memories.Web/           # Non-packable RCL; runnable conformance specimen hosted from tests/, not product UI`

---

### DRF-06 — The PRD still permits a second secret path that AD-15 forbids — **medium** — UPSTREAM-WRONG

**Spine side:** `ARCHITECTURE-SPINE.md:144` (AD-15) — "Direct Kubernetes-secret injection of Redis/FalkorDB credentials is a temporary alignment gap, not a second approved application secret path." Reinforced by the gap row at `:298`.

**Upstream side:** the PRD contradicts itself. `prd.md:1142` (NFR9, tightened on 2026-09-12) agrees with the spine — "direct Redis/FalkorDB credential injection is an alignment gap, not an approved second path". But `prd.md:927`, in CLI Specification › Configuration Layering, was not updated and still reads "Kubernetes Secrets are permitted only where required for OpenBao bootstrap material **or direct pod inputs that DAPR cannot provide**." `addendum.md:69` was corrected and matches the spine.

**Landed-vs-not analysis:** `reconcile-latest-architecture.md:146` required "Tighten NFR9 outcome, correct addendum wording, and mark NFR9 re-verification owed". NFR9 landed (`prd.md:1142`), the addendum landed (`addendum.md:69`), the status landed (`prd.md:1118`) — `prd.md:927` did not. This is a partially applied proposal, not architecture drift.

**The spine is right.** Recommended upstream edit (PRD, not this review's scope to apply): delete "or direct pod inputs that DAPR cannot provide" from `prd.md:927` so it reads "Kubernetes Secrets are permitted only where required for OpenBao bootstrap material." No spine edit is needed; optionally the spine may cite `prd.md:927` in the `:298` gap row as the upstream text that must be corrected in parallel.

---

### DRF-07 — AD-18 does not carry NFR13's new admission and queue-cap semantics — **medium** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:158-162`. AD-18 binds `NFR1-NFR7, NFR12-NFR14, NFR36` and requires "tenant-partitioned admission/concurrency and bounded durable queues, honor provider `Retry-After` through workflow timers, and keep repair/migration below interactive priority. Numeric budgets live in the PRD and validated configuration".

**Upstream side:** `prd.md:1151` (NFR13, restated 2026-09-12) adds three behaviours, not merely numbers: "Every eligible non-empty tenant queue receives an admission within 5 seconds after capacity is available" (an anti-starvation liveness property); "the first item beyond either cap is **rejected** within 1 second with retry guidance" (an admission-refusal contract); "accepted work is never dropped" (a durability invariant distinguishing rejection from loss). `addendum.md:81-83` records the mechanism but likewise not the reject-vs-drop distinction. `EXPERIENCE.md:171` requires the user-visible half.

The delegation clause at `:162` correctly hands the *numbers* to the PRD, but reject-not-drop, bounded rejection latency, and eligible-queue admission liveness are architecture-shaped rules that determine whether a queue implementation is compliant.

**Recommended spine edit** — append to the AD-18 rule at `:162`:

> …Bounded queues refuse admission beyond their configured cap with actionable retry guidance rather than dropping or silently deferring work; once admitted, work is never dropped. Every eligible non-empty tenant queue receives an admission within the PRD's stated bound after capacity becomes available, so no tenant queue can be starved by a busier one. Numeric budgets live in the PRD and validated configuration.

---

### DRF-08 — AD-12 never names the versioned Evidence Packet state vocabulary — **medium** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:126` enumerates required Evidence Packet *fields* ("tenant/case scope, source/origin, confidence and per-axis contribution, degradation/excluded axes, omitted-detail handles, freshness, and recovery with equivalent null/omission meaning") and never states the packet's state enumeration.

**Upstream side:** `prd.md:92` (Glossary) and `prd.md:571` both fix it — "Its versioned states are `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, and `pendingExpansion`; surfaces must not fabricate states that current mappers do not yet emit." `EXPERIENCE.md:145` repeats it and `EXPERIENCE.md:156` records a live violation: "Current Web code has no contract-backed conflict field and can map packet degradation or unavailable axes to `Conflicting`; this is a delivery divergence, not current conformance. Until a versioned conflict signal exists, activated UX must classify backend/axis loss as degradation and must not tell users that sources conflict." `reconcile-latest-ux.md` item 3 imported exactly this into the PRD.

Because AD-12's own rule is "Evolve Contracts.V1 additively and preserve deliberate wire names until a versioned break", a state vocabulary that AD-12 never enumerates cannot be protected by it.

**Recommended spine edit** — insert into AD-12's rule at `:126`, immediately after "Capability subsets may vary operations, never Evidence Packet semantics":

> The versioned Evidence Packet state vocabulary is `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, and `pendingExpansion`. No surface synthesises a state outside that vocabulary — backend or axis loss is reported as degradation, never as an invented condition — and a state a mapper does not yet emit is reported as unavailable rather than fabricated.

---

### DRF-09 — Freshness vocabulary and thresholds have no owner in the spine — **medium** — SPINE-STALE

**Spine side:** "freshness" appears in AD-10 (`:114`, "freshness impact"), AD-12 (`:126`, a carried field), and AD-13 (`:130`, a preserved provenance element). No AD, convention, or boundary assigns ownership of the vocabulary, its thresholds, its transitions, or its versioning.

**Upstream side:** `prd.md:1201` (NFR33, `Ongoing / per surface`) — "authoritative `current`, `aging`, `stale`, and `unknown` thresholds, transitions, disclosure, and recovery actions, **versioned in the Evidence Packet contract** and activated per delivery surface". `EXPERIENCE.md:175` — "Human freshness labels are `current`, `aging`, `stale`, and `unknown`. Thresholds and transitions remain contract-owned; current implementation enum differences are delivery debt. Freshness never modifies relevance confidence." NFR33 is inside the spine's bound range `NFR1-NFR36`, so this is coverage failure inside a claimed binding, not an out-of-range omission.

**Recommended spine edit** — extend the AD-12 sentence added in DRF-08, or add a Consistency Convention row:

> | Freshness | Evidence Packet freshness is one of `current`, `aging`, `stale`, `unknown`. Thresholds, transitions, disclosure, and recovery are versioned in the Contracts.V1 packet and activated per surface; freshness never modifies relevance confidence and is never inferred by a presenter. |

---

### DRF-10 — AD-16 omits the erasure scope limitation and therefore over-claims — **medium** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:146-150`, "AD-16 — Complete tenant erasure through crypto-shredding", whose rule ends "Retain only content-free deletion evidence/tombstones, reject replay and reuse of the tenant ID, and quarantine unreadable payloads during restore rather than rehydrating them." The word "Complete" in the title is unqualified.

**Upstream side:** `prd.md:546` — after describing the same end-to-end outcome — "**Limitation:** Cross-references to that tenant's data in *other* tenants' memory units are the application's responsibility to handle. The compliance guide must document this explicitly." `prd.md:541` places gap-marked, accurate interpretation in the Memories tier while `prd.md:542` places legal obligations in the Application tier.

An architecture decision titled "Complete tenant erasure" that never states what erasure does *not* cover is the exact over-claim the compliance boundary exists to prevent, and it is the boundary an implementer would need when deciding whether to scan other tenants' units.

**Recommended spine edit** — append to the AD-16 rule at `:150`:

> …Erasure is scoped to the deleted tenant's own content, evidence, and derived state. References to that tenant's data held inside other tenants' memory units are outside this guarantee and remain the consuming application's responsibility; the deletion evidence states that boundary rather than implying global removal.

---

### DRF-11 — The Stack table omits the embedding provider/model/dimension that AD-14 binds — **medium** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:196-214` pins .NET, Aspire, Dapr, EventStore, Redis Stack, FalkorDB, OpenBao, MCP, Kreuzberg, FrontComposer/Fluent, and OpenTelemetry — but no embedding provider, model, or vector dimension. Yet AD-14's `Binds:` line at `:136` opens with "Embedding provider/model/dimension", and AD-14's `Prevents:` at `:137` names "mixed embedding dimensions".

**Upstream side:** `prd.md:799` — MVP provider Google, model `text-embedding-004`, 768 dimensions, 1500 req/min default; `prd.md:815` — "`dimensions` | Vector dimensions (**determines Redis Vector index schema**) | Derived from provider/model"; `prd.md:820` — "Redis Vector Search index schema is fixed at creation — **switching embedding providers requires full reindex of that tenant's data**. This is a migration operation, not a configuration change." `addendum.md:116` keeps second-provider timing out of the contract.

The dimension is a load-bearing pin: it fixes an index schema and triggers AD-14's migration contract. Its absence makes the Stack table incomplete against the spine's own binding claim.

**Recommended spine edit** — add a Stack row after `:210`:

> | Embedding provider / model / dimension (MVP) | Google / `text-embedding-004` / `768` |

and note in the paragraph at `:214` that the dimension fixes the Redis Vector index schema, so a change is an AD-14 migration and not configuration.

---

### DRF-12 — Graph data-accuracy obligations have no AD home — **medium** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:116-120` (AD-11) covers injection safety, contract-enum labels, tenant/case scope, server-owned depth/result/time limits, a kill switch, and the Phase 1 vs Phase 1.5 population split. A whole-file grep for "gap marker", "MISSING", and "chronolog" returns no matches.

**Upstream side, four uncovered obligations:**

- **Gap markers (FR49).** `prd.md:590` — "the chain must flag the gap explicitly: `A → [MISSING: event-id-B] → C`. Never silently skip missing nodes. This is a data accuracy responsibility of the Interpretation layer." Restated at `prd.md:541`, `prd.md:892`, `prd.md:1067`, and `EXPERIENCE.md:126` ("literal gaps") and `:230`.
- **Chronology (FR52).** `prd.md:1070`, `prd.md:588` — ordered nodes with timestamps establishing chronological order.
- **No auto-promotion (FR51).** `prd.md:604` — "Users can promote AI-inferred edge confidence … The system **never auto-promotes**."
- **Non-collapse invariant.** `prd.md:602` — "Collapsing CorrelationId into causation makes every event in a correlation group appear to cause every other event — exactly the misrepresentation the structured data model exists to prevent." `EXPERIENCE.md:230` — "`caused_by` is not `correlated_with`."

AD-11's `Prevents:` list (`:119`) covers injection, cross-scope paths, unbounded traversal, arbitrary labels, and premature causal claims — none of these four.

**Recommended spine edit** — append to the AD-11 rule at `:120`:

> …Traversal results are honest about their own completeness: a causal chain whose intermediate node is not indexed emits an explicit gap marker naming the missing identifier rather than skipping it, nodes carry timestamps and chronological order, edge confidence is promoted only by an authorized explicit act and never automatically, and `caused_by` is never merged into or derived from `correlated_with`.

---

### DRF-13 — NFR32 and NFR35 are inside the bound range with zero coverage — **medium** — SPINE-STALE

**Spine side:** the spine binds `NFR1-NFR36` (`:13`) yet contains no match for WCAG, accessibility, keyboard, forced colours, reduced motion, or a web performance budget. The only web content is the Deferred row at `:319` — "Hosted product Web surface | Phase 2 selects a host; it owns navigation, authentication, authorization, global state, render mode, and activation of the conformance components" — which names host responsibilities but no activation evidence gate.

**Upstream side:** `prd.md:1200` (NFR32) requires, on activation, WCAG 2.2 AA, keyboard completion of the trust workflow, non-colour-only state, equivalent ordered representation for tabular/graph content, SC 2.5.8 pointer targets, and "a dated route/state evidence matrix covering viewport, 200% text resize, 400% zoom/320-CSS-pixel reflow, focus-not-obscured, theme, forced colors, reduced motion, input mode, supported browser/assistive technology (including NVDA on supported Edge/Chrome), keyboard start/end focus, artifact, tester/date, defect or waiver owner, and release disposition. Automated component/axe checks do not replace manual browser/AT evidence." `prd.md:1203` (NFR35) fixes the budgets (2.5 s usable trust packet, p95 200 ms interaction, CLS ≤ 0.1, ≤ 256 KiB initial route) and forbids removing the gate. `EXPERIENCE.md:272` adds the rule the spine most needs: "It must consume the then-current `Epic17ValidationInventory.Gaps` and durable browser summary, giving every applicable row a dated closure or named waiver; **specimen evidence never transfers automatically to a product route.**"

A further architecture-shaped constraint sits in `DESIGN.md:20-32` and `:183`, declared normative: `inheritance` (FrontComposerShell, FC-A11Y, Fluent UI Blazor V5, Fluent 2, "version-owner: central Hexalith.Builds package catalogue") and `deltas: colors none / typography none / rounded none / spacing none / elevation none` — "Memories introduces no independent theme scale". `EXPERIENCE.md:238` and `:268` add "Do not recreate shell mechanics in Memories." The spine pins the FrontComposer/Fluent versions at `:211` but states no dependency-direction rule for the presentation layer, even though this is precisely an AD-1/AD-8-shaped "no reimplementation across the boundary" constraint.

**Recommended spine edit** — replace the Deferred row at `:319` and add a convention row:

> | Hosted product Web surface | Phase 2 selects a host; it owns navigation, authentication, authorization, global state, render mode, and activation of the conformance components. Activation additionally requires the PRD's dated route/state accessibility evidence matrix and measured interaction budgets, consumes the then-current specimen gap inventory with a dated closure or named waiver per applicable row, and never inherits specimen evidence as product-route evidence. |

> | Presentation dependencies | Memories web components are a delta over the shell and component library owned by the central build catalogue: no Memories-owned theme, colour, typography, spacing, radius, or elevation scale, and no reimplementation of shell chrome, overlays, or accessibility mechanics where an owned primitive exists. |

---

### DRF-14 — The spine's active sources and companions include superseded and stale artefacts — **medium** — SPINE-STALE

**Spine side:** `ARCHITECTURE-SPINE.md:19` lists `_bmad-output/planning-artifacts/architecture.md` as an active `source`. `:30` lists `_bmad-output/test-artifacts/traceability/traceability-matrix.md` as a `companion`.

**Upstream side:** `addendum.md:144` — "The final 2026-09-09 architecture spine **supersedes** legacy `architecture.md` for architecture decisions." `reconcile-latest-architecture.md:193` — "Flag `architecture.md` as legacy/superseded in downstream guidance". `sprint-change-proposal-2026-09-12.md:41` classifies it `historical-reference-only`, and `:126` asks for it to move to a `historicalSources` block with `status: superseded`. A `final` document that still lists its own superseded predecessor as an active input invites a derivation to read both as current — which is exactly what `epics.md` does today (DRF-20).

The companion is stale in a different way: `_bmad-output/test-artifacts/traceability/traceability-matrix.md:8` records `lastSaved: '2026-05-19'`, `:5` records `gateDecision: 'PASS'`, and `:16` lists legacy `architecture.md` among its oracle sources; the file contains zero references to FR75, NFR36, or NFR37 (and no FR70-79 references at all), so it predates the current requirement set while asserting PASS.

**Recommended spine edit:** move `architecture.md` from `sources:` to a new `historicalSources:` block with `status: superseded` and `permittedUse: historical decision and evidence provenance only`; add `_bmad-output/planning-artifacts/ux-designs/ux-memories-2026-09-12/DESIGN.md` and `EXPERIENCE.md` to `sources:`; and either refresh the traceability companion or annotate it `stale as of 2026-05-19; predates FR71-FR75 and NFR36-NFR37; its PASS verdict is not current gate evidence`.

---

### DRF-15 — NFR11's finite, named, tested anonymous-ingress exception set has no rule — **low** — SPINE-STALE

**Spine side:** the Routes convention (`:176`) names `/events/ingest` as "the named infrastructure exception"; the Health convention (`:181`) implies probes are reachable; AD-5 (`:84`) governs authority on authenticated paths. No rule states that the anonymous surface is a finite enumerated set that must be named and tested.

**Upstream side:** `prd.md:1144` (NFR11, MVP) — "Health probes and required DAPR infrastructure routes are the only deliberate anonymous exceptions and are **named and tested**. … unauthenticated product ingress is not a Phase 1.5 allowance." `prd.md:755` repeats "Anonymous: enumerated health/DAPR infrastructure routes only." `addendum.md:64` records Epic 20 JWT on `/api/**` as the current reality.

**Recommended spine edit** — append to the Routes convention at `:176`: "Anonymous reachability is a finite enumerated set — health probes plus the named Dapr infrastructure routes — declared in one place and covered by a negative test; nothing else is anonymous in any phase."

---

### DRF-16 — The NFR31/G3 onboarding budget has no operational-boundary home — **low** — SPINE-STALE

**Spine side:** `:259` describes what AppHost composes locally and `:232` calls AppHost "Local composition only"; the Operational Boundaries table (`:261-272`) has dimensions for Consistency, Security, Reliability, Capacity, Observability, Erasure, Deployment, and Evolvability — none for developer onboarding.

**Upstream side:** `prd.md:1194` (NFR31, MVP, and gate G3 at `prd.md:186`) times "AppHost boot → `tenant create` → `case create` → `ingest` → `search query`" on a clean machine in under 30 minutes with Docker image pulls **on** the clock (unlike NFR7), and `prd.md:1135` (NFR7) separately requires operational readiness within 60 s excluding pulls. `EXPERIENCE.md:209` restates the gate. A local composition that grows another container or a slow first-run path can fail a hard gate with no architectural signal.

**Recommended spine edit** — add an Operational Boundaries row: "| Onboarding | Local AppHost composition is a gated developer path: a clean-machine boot to first successful CLI search stays inside the PRD's G3 budget with image pulls counted, so added local resources and first-run work are budgeted against that gate. |"

---

### DRF-17 — NFR21's generic-publisher acceptance boundary is unstated — **low** — SPINE-STALE

**Spine side:** AD-2 (`:66`) separates "Phase 1.5 CloudEvent indexing/causal integration" as a distinct contract; AD-4 (`:78`) requires "exact validated `source`". Neither states what happens to a CloudEvent from a publisher that is not following the EventStore convention.

**Upstream side:** `prd.md:1169` (NFR21) — "Envelopes from other DAPR publishers are accepted only for the fields the EventStore convention defines; processing them without custom code is the DAPR-generic *experiment* (Innovation #2), not a requirement", verified by "a documented negative test showing what a non-conforming envelope does". `prd.md:1232` keeps the generic-publisher claim an assumption behind a kill switch.

**Recommended spine edit** — append to AD-2's rule at `:66`: "CloudEvent ingestion honours only the fields the EventStore convention defines; envelopes from other publishers are neither rejected as a product feature nor interpreted beyond those fields, and the boundary carries a documented negative test."

---

### DRF-18 — The floating-image gap omits its licensing dimension — **low** — SPINE-STALE

**Spine side:** `:305` — "AppHost and Aspire defaults use floating Redis/Falkor images. | **AD-19** | Pin qualified defaults with explicit consumer overrides." Only AD-19 (dependency pinning / release evidence) is cited.

**Upstream side:** `prd.md:626` makes FalkorDB pinning a licensing control, not only a build control — "**FalkorDB version pinning** — Pin to a specific AGPL-licensed image version in the AppHost resource definition **and the production deployment artifacts** (Epic 26). If FalkorDB relicenses, users can stay on the pinned version while alternatives are built." `prd.md:628` adds the SSPL managed-service constraint for Redis Stack, and `prd.md:625` requires `LICENSE-DEPENDENCIES.md`. The spine's Deferred row at `:324` covers "Managed-service and redistribution license posture" but not the pin duty, and the gap row does not say that an unpinned Falkor image is also a relicensing exposure.

**Recommended spine edit:** change the `:305` row's Violated rule to `AD-19` plus a reference to the licensing posture, and extend Required convergence to "Pin qualified defaults with explicit consumer overrides in AppHost **and** production artifacts; the Falkor pin is also the relicensing containment control."

---

### DRF-19 — Two residual wording refreshes — **low** — COSMETIC

**(a)** `ARCHITECTURE-SPINE.md:78` says "make every projection an idempotent upsert for AD-3's tuple", which implies but never states the negative that `prd.md:1017` makes explicit — "a legitimate reprojection under a new epoch is **not suppressed**". `addendum.md:58` has the same omission. Recommended: append to AD-4's rule "…and a new authoritative tuple is a distinct outcome that duplicate suppression must not swallow."

**(b)** `ARCHITECTURE-SPINE.md:310` — "Architecture alignment does not imply that G1-G5 or L1-L3 product gates have passed." Now G1-G6 (`prd.md:189`, `:191`). Recommended: change `G1-G5` to `G1-G6` (also covered by DRF-02).

---

### DRF-20 — The spine offers no requirement→AD surface, so its companion cannot cite it — **medium** — SPINE-STALE

Per the brief, `epics.md` itself was not audited; only citability and the spine-side contribution to the epic-level divergence.

**Downstream side (existence checks only):** `epics.md:3-9` lists `inputDocuments` as `prd.md`, legacy `architecture.md`, legacy `ux-design-specification.md`, and three 2026-07 readiness/change artifacts — the final spine, `addendum.md`, `DESIGN.md`, and `EXPERIENCE.md` are absent. `grep -c 'AD-[0-9]' epics.md` returns **0**; `grep -c 'ARCHITECTURE-SPINE' epics.md` returns **0**. Its functional inventory ends at FR74 (`epics.md:127`, `:533`) with no FR75/NFR36/NFR37 rows.

**Spine side (the part this lens owns):** the spine declares `epics.md` a companion at `:29` but exposes nothing a derivation can cite by requirement. Traceability exists only as prose `Binds:` lines that name a handful of ids (AD-3 → FR6/FR13; AD-5 → NFR8-NFR11; AD-7 → FR32-FR34, G4; AD-9/AD-11 → G1; AD-10 → FR66/NFR18; AD-18 → NFR1-NFR7, NFR12-NFR14, NFR36) plus the coarse `Capability → Architecture Map` (`:274-286`). There is no FR→AD table, no NFR→AD table, no gate→AD table, and no story/owner column on the gap ledger. So even a compliant epics regeneration could not mechanically cite the ADs that govern each requirement.

This is the spine-side half of the divergence the proposal describes at `sprint-change-proposal-2026-09-12.md:51`; the proposal's own handoff at `:131` ("Add G1–G6 to the architecture trace table") presumes a table that does not exist.

**Recommended spine edit:** add a compact trace section after the Capability → Architecture Map with three tables — `FR range → governing ADs`, `NFR range → governing ADs`, `G1-G6 / L1-L3 → governing ADs and evidence owner` — covering FR1-FR75, NFR1-NFR37, and G1-G6, and add a `Story/owner` column to the gap ledger per DRF-02 so a derivation can join on it.

---

## Verified aligned — no drift found

These were checked for contradiction, not merely for omission, and matched:

- **V1 status wire contract.** `ARCHITECTURE-SPINE.md:177` vs `prd.md:98` (Glossary), `prd.md:829` (Async Ingestion Pipeline), and `EXPERIENCE.md:160-167`. Wire values `queued/extracting/embedding/indexing/indexed/failed`, product mapping `pending`≙`queued` and `projecting`≙`indexing`, unchanged until a versioned break — identical on all four sides. PRD Open Question 8 was closed on 2026-09-12 (`prd.md:1226`) in the spine's favour.
- **AD-9 fusion algorithm.** `ARCHITECTURE-SPINE.md:108` vs `addendum.md:22-23`: provider order, blank-id and non-finite discard, first-occurrence retention, `Double.Equals` ties with no epsilon, competition ranks `1,1,3`, contribution `1/(10+rank)`, weights `0.30/0.35/0.35`, NL `0.20` default-off limited to Phase 1.5 event units, and the tenant-wide merge (ascending ordinal case, per-case max finite graph score, descending score → ascending case → ascending `MemoryUnitId`) match clause for clause. The spine additionally states the top-rank normalisation and depth-two auto-seeding, which the addendum omits without contradicting; `prd.md:571` (composite row) and `prd.md:167` agree.
- **AD-3 completion tuple.** `ARCHITECTURE-SPINE.md:72` vs `addendum.md:57` — the six-field tuple, the single Dapr-state coordinator, monotonic ETag/CAS, stale-ack rejection, and the shared ingest/repair/replay protocol are identical; `prd.md:1009` (FR6), `prd.md:1016` (FR13), and `prd.md:829`/`:839` are consistent.
- **AD-4 idempotency semantics.** `ARCHITECTURE-SPINE.md:78` vs `addendum.md:58` — token scope, CloudEvent identity, ordinal comparison with no post-validation normalisation, durable suppression ownership, idempotent projection upserts, and fail-open Redis preflight reservation are identical. Only the epoch-non-suppression sentence is missing on both sides (DRF-19a).
- **AD-18 delegation of numbers.** `:162` correctly declines to restate `prd.md:1151`/`:1209` budgets; only the behavioural clauses are missing (DRF-07).
- **AD-5 / AD-7 / AD-10 / AD-16 / AD-17 imports.** Every product-observable guarantee proposed in `reconcile-latest-architecture.md` sections 1.C, 1.D, 1.E, 1.F, and 1.G landed upstream (`prd.md:1143`, `:1057`, `:1092`; `:1024`, `:1043`, `:1044`, `:187`; `:1093`, `:1105`, `:1161`; `:546`, `:1052`, `:1159`, `:1202`; `:1094`) and none contradicts the corresponding AD.
- **Gap ledger vs delivery register.** With one exception, every spine gap now has a matching downgraded PRD status: FR6/FR13/FR34/FR39/FR65/FR75 partial (`prd.md:995`), FR17/FR25 partial (`prd.md:994`), NFR8-NFR10 re-verification owed (`prd.md:1118`), NFR16 partial mechanism (`prd.md:1119`), NFR24-NFR25 re-verification owed (`prd.md:1118`), MCP preview inactive (`prd.md:993`). The single competing claim is the Web row (DRF-05).
- **Phase-gated MCP.** `:126` and `:308` vs `prd.md:993`, `prd.md:264`, and `EXPERIENCE.md:38` — asset presence is not activation on both sides.

## Reconciliation proposals: landed vs not

Derived from `reconcile-latest-architecture.md` (the analysis that drove the 2026-09-12 PRD update). A proposal that landed upstream with no spine-side counterpart is drift.

| Proposal | Landed upstream? | Spine counterpart needed? | Result |
| :-- | :-- | :-- | :-- |
| §1.A projection completion / replay (`:20-26`) | Yes — `prd.md:1009`, `:1016`, `:1159`, `:995`, `:1119`; `addendum.md:57`, `:59` | Already in AD-3 | aligned |
| §1.B FR75 durable idempotency (`:32`) | Yes — `prd.md:1017`, `:982`, `:995`; `addendum.md:58` | Header + `Binds:` refresh (`:34`, `:193` explicitly predicted it) | **not done → DRF-03** |
| §1.C internal authorization (`:40-46`) | Yes — `prd.md:1143`, `:1057`, `:1092`, `:755`, `:1118` | Already in AD-5 | aligned |
| §1.D tenant-wide case partition (`:52-58`) | Yes — `prd.md:1024`, `:1043`, `:1044`, `:187`, `:995`; `addendum.md:23` | Already in AD-7/AD-9 | aligned |
| §1.E capability-aware degradation/readiness (`:64-69`) | Yes — `prd.md:1093`, `:1105`, `:1161`, `:996` | Already in AD-10 + Health convention | aligned |
| §1.F verified erasure (`:75-81`) | Yes — `prd.md:546`, `:1052`, `:1159`, `:1202`; `addendum.md:73-77` | AD-16 present but omits the scope limitation | **partial → DRF-10** |
| §1.G telemetry two-stage posture (`:87-90`) | Yes — `prd.md:1202`, `:1094` | Already in AD-17 | aligned |
| §1.H fairness (`:96-102`) | Yes — `prd.md:1011`, `:1151`, `:1170`, `:1150`, `:1209`; `addendum.md:79-83` | AD-18 lacks the new behavioural clauses | **partial → DRF-07** |
| §1.I exact V1 wire values (`:108-110`) | Yes — `prd.md:98`, `:1226` | Already in `:177` | aligned |
| §2 AD-14 phase resolution (`:112-120`) | No — deferred to `prd.md:1227`, `addendum.md:88` | Required | **not done → DRF-01** |
| §4 data-plane secrets (`:146`) | Partly — NFR9 and addendum tightened; `prd.md:927` not corrected | Spine already correct | **upstream residue → DRF-06** |
| §4 cross-case wording (`:148`) | Yes — `addendum.md:122` | n/a | aligned |
| §4 release dates (`:149`) | Yes — `addendum.md:44` | n/a | aligned |
| §4 status-register refresh (`:150`) | Yes — `prd.md:990-1000`, `:1115-1121` | Gap ledger matches except Web | **one conflict → DRF-05** |
| Order item 6 — refresh the `binds` range (`:193`) | n/a (spine-side action) | Required | **not done → DRF-03** |
| `reconcile-latest-ux.md` items 3, 4, 5 | Yes — `prd.md:571`, `:1215`, `:1200` | Required (packet states, NFR37, NFR32) | **not done → DRF-08, DRF-04, DRF-13** |

## Recommended remediation order

1. **DRF-01** — obtain the AD-14 phase ratification (blocks everything else; the spine cannot return to `final` without it).
2. **DRF-02** and **DRF-20** — add the release-gate decision, the five-column gap ledger with active-foundation classification, and the requirement→AD trace tables.
3. **DRF-04**, **DRF-08**, **DRF-09**, **DRF-13** — write the missing normative text *before* the header refresh, so the binds range does not assert coverage that does not exist.
4. **DRF-03** — refresh `binds`, `Binds:` lines, and the capability map.
5. **DRF-05**, **DRF-14** — correct the stale Web row, the seed line, and the source/companion lists.
6. **DRF-07**, **DRF-10**, **DRF-11**, **DRF-12** — extend AD-18, AD-16, the Stack table, and AD-11.
7. **DRF-15** … **DRF-19** — convention and wording refreshes.
8. **DRF-06** — hand back to the PRD owner; no spine change.
