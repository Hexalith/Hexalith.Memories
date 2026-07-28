---
change_trigger: "Add a pre-story verify-epic-AC-against-code step to story creation so planning counts and claims are reconciled before implementation (60 literals, no Client.Rest, double authorization were all false against code)"
mode: batch
status: approved-and-applied
requested_by: Administrator
approved_by: Administrator
project: Hexalith.Memories
date: 2026-07-28
scope_classification: moderate
supersedes: null
follows:
  - sprint-change-proposal-2026-07-06-historical-slice-story-guard.md
  - sprint-change-proposal-2026-07-16-access-telemetry-retention-implementation.md
---

# Sprint Change Proposal: Epic AC Verification Against Code

Date: 2026-07-28
Project: Hexalith.Memories
Scope: Moderate process/governance change to the story lifecycle. It adds a
verification obligation to story creation, development, and review. It does not
reopen any completed epic, does not change product scope, and does not by itself
schedule implementation work.

## 1. Issue Summary

### Problem statement

Epic acceptance-criteria text in `_bmad-output/planning-artifacts/epics.md`
carries quantitative counts, existence assertions, and behavioral descriptions
that were recorded at planning time and never re-derived against the codebase.
Three of them were false when the corresponding stories reached implementation:

| Claim in epic text | Story that found it false | What the code actually showed |
| :----------------- | :------------------------ | :---------------------------- |
| "60 server literals" | 25.3 | The count did not reconcile against the source. |
| "the CLI has no `Client.Rest` reference" | 25.5 | The reference existed. |
| "a redundant double authorization" (A39, four MCP tools) | 25.6 | One authorization decision plus a redundant accessor read/failure seam — not two authorizations. |

Story 25.3 additionally found the epic's premise that `TraverseAsync` is
experimental to be false; the API is stable, which turned the planned reorder
into a real breaking change requiring a maintainer decision.

### How it was discovered

Epic 25 retrospective (`epic-25-retro-2026-07-12.md`), "What Went Wrong" item 4:

> **Epic AC text drifted from the code in 3–4 stories.** The "60 server literals"
> count (25.3), "CLI has no `Client.Rest` reference" (25.5), and "double
> authorization" (25.6) were all false against the code. The stories that verified
> before implementing avoided shipping wrong work — but the drift itself means
> planning counts were never reconciled against the codebase.

The retrospective recorded it as action item 2 (owners Amelia, John; category
Process / Planning) with success criteria: *"Story Dev Notes record an AC-vs-code
check; stories stop opening with corrected premises."*

The Epic 26 retrospective (`epic-26-retro-2026-07-16.md`) follow-through table
scored the same item **In progress**: *"Epic 26 stories reconciled several stale
premises during implementation, but no formal pre-story step is enforced."* The
sprint-status entry carries the same 2026-07-16 note.

### Evidence that this is a process gap, not a one-off

1. **The check already exists as prose and did not fire.** `epics.md` line 4077
   already carries an **Audit-anchor preflight** paragraph requiring
   re-verification of code anchors before a story is selected, created, or
   implemented. Epic 25 is inside its stated `Epic 20-26` range. The drift
   happened anyway, because the paragraph lives in a planning document that story
   creation reads for *content*, not as a *gate*, and the guard is scoped to code
   anchors rather than to the acceptance claims themselves.
2. **Good practice is happening ad hoc, unenforced.** Story
   `31-2-runtime-dapr-secret-store-migration.md` (created 2026-07-28,
   `ready-for-dev`) contains a hand-rolled `### Measured Runtime State At Creation`
   section with dated probes, a re-runnable command block, and explicit
   hypothesis-versus-finding labelling. Nothing required it, nothing names it, and
   nothing checks that the next story has one.
3. **Corrections stayed local.** Each of the three stories corrected its own
   premise. None of them corrected `epics.md`, so the false claims are still in
   the planning artifact for the next reader.
4. **Two-epic recurrence.** Epic 25 found it; Epic 26 repeated the pattern with
   no enforced step added in between.

## 2. Impact Analysis

### Epic impact

| Epic | Impact |
| :--- | :----- |
| Epic 25 | Closed. Not reopened. Its retrospective action item 2 is the driver of this proposal. |
| Epic 26 | Closed. Its follow-through row 2 records the same item as in progress. Not reopened. |
| Epic 30 (5 stories, all `backlog`) | **Primary beneficiary.** No story file exists yet, so every story is created under the new gate. |
| Epic 31 (`in-progress`) | 31.1 is `review`, 31.2 is `ready-for-dev` and already carries an equivalent hand-rolled section. Future 31.x stories are created under the gate. Existing files adopt on first participating workflow, not retroactively. |

No epic is added, removed, resequenced, or redefined. Epic priority is unchanged.

### Story impact

- **Future stories:** must carry an `### Epic AC Verification` section under
  `## Dev Notes` before `ready-for-dev`.
- **In-flight stories:** adopt via a dated adoption row on the first participating
  workflow, mirroring how `story-phase-ledger.md` handles stories that predate it.
  Story 31.2's existing measured-state section satisfies the intent; it is
  relabelled on next touch, not rewritten.
- **Completed stories:** untouched. Retrospective documents are point-in-time
  records and are not edited.

### Artifact conflicts

| Artifact | Conflict | Resolution |
| :------- | :------- | :--------- |
| `epics.md` "Audit-anchor preflight" | Scoped to `Epic 20-26` and to code anchors only; does not cover AC claims and is not epic-number-independent. | Broaden it, as the cross-tenant carry-forward guard was broadened on 2026-07-16, and point it at the new policy file. |
| `epics.md` false claims (60 literals, `Client.Rest`, double authorization) | Still present in the planning artifact. | The policy's "Correcting the planning artifact" clause requires correcting the source, not just the story. Correcting the three historical Epic 25 claims is **out of scope here** and is listed under Section 5 as a follow-on, because Epic 25 is closed and its epic text is a historical record of what was planned. |
| PRD | No conflict. The PRD states product goals; this change adds no requirement and removes none. | No edit. |
| `architecture.md` | No conflict. No component, pattern, technology, data model, API contract, or integration point changes. | No edit. |
| UX specifications | No conflict. No screen, flow, wireframe, interaction, or accessibility change. | No edit. |

### Technical impact

No production code, deployment script, IaC, monitoring, or CI/CD pipeline
changes. One test file is extended (`tests/tooling/bmad_customization/`), adding
2 test methods to a Python `unittest` fixture that already runs in the
tooling-contract lane. No container, cluster, or runtime dependency is involved.

### Deliberate non-goals

- **No executable gate.** Per the selected enforcement depth, this guard is
  LLM-obeyed prose loaded through committed customizations, matching its three
  siblings. It does **not** discharge Epic 26 action item 7 ("executable
  pre-review gate"), which remains open and separately owned.
- **No `[[workflow.review_layers]]` subagent.** The most recent sibling
  (`remediation-runtime-checklist.md`) is directive-only in `bmad-code-review`.
  This one matches. A dedicated review layer is recorded as a deferred option.
- **No edits to generated skill files.** `.claude/skills/**` and `.agents/skills/**`
  are overwritten by BMad refreshes. The canonical story section is specified by
  the policy file, exactly as `story-phase-ledger.md` specifies the Change Log
  table without touching `template.md`.

## 3. Recommended Approach

**Selected path: Option 1 — Direct Adjustment.**

- **Option 1 — Direct Adjustment:** Viable. Add a durable policy artifact and wire
  it into the three lifecycle workflows through committed `_bmad/custom` overrides.
  Effort **Low**, risk **Low**. Follows the established 4-part enforcement pattern
  proven three times (`story-scope-guard`, `story-phase-ledger`,
  `remediation-runtime-checklist`).
- **Option 2 — Rollback:** Not viable. Nothing to roll back. The three false claims
  were caught before wrong work shipped; the stories that found them are done and
  reviewed.
- **Option 3 — PRD MVP Review:** Not viable and not warranted. MVP scope, product
  goals, and core requirements are unaffected. This is an internal process control.

**Rationale.** The failure mode is not that the check is hard — three developers
performed it correctly on their own initiative. The failure mode is that nothing
*required* it, nothing named the artifact it produces, and nothing propagated the
correction back to the planning document. The fix must therefore (a) name a
canonical, greppable story section, (b) load at activation time in all three
lifecycle workflows so it cannot be skipped by not reading `epics.md` closely, and
(c) close the loop by requiring the planning artifact to be corrected or the
correction escalated. A fixture pins the wiring so a BMad skill refresh cannot
silently drop it.

**Timeline impact:** none. Story creation gains a verification pass whose output
is the input to the acceptance criteria — work that the three Epic 25 stories
already performed, just later and without a record.

## 4. Detailed Change Proposals

### 4.1 NEW — `_bmad/custom/epic-ac-verification.md`

Team-owned policy file, sibling to `story-scope-guard.md`,
`story-phase-ledger.md`, and `remediation-runtime-checklist.md`. Structure:
Authority / Applicability / Canonical story section / Correcting the planning
artifact / Creation gate / Development gate / Review gate.

Key contents:

- **Authority:** current source, tests, and configuration are authoritative;
  epic/PRD acceptance text is planning intent, advisory until re-derived; where
  they disagree the code wins and the planning artifact is corrected.
- **Four always-verifiable claim classes:** Quantitative, Existence and absence,
  Behavioral, Location.
- **Canonical section** under `## Dev Notes`:

  ```markdown
  ### Epic AC Verification

  Verified <date> against <commit-or-branch>.

  | Epic claim | Class | Command / evidence | Observed | Verdict |
  | :--------- | :---- | :----------------- | :------- | :------ |
  ```

- **Three verdicts:** `confirmed`, `corrected`, `unverifiable` (the last carries
  blocker, owner, consequence, reopen trigger, mirroring the phase ledger's
  blocked-discovery record).
- **Explicit escape** when the inherited epic text contains no verifiable claim:
  `epic AC verification: no verifiable claim in inherited epic text`.
- **Loop closure:** a `corrected` verdict is not discharged by fixing the story
  alone — the source planning artifact is corrected or carries a dated correction
  note; a correction that changes scope, epic intent, or a ratified decision is
  escalated for a human decision (Story 25.3's `TraverseAsync` reorder is the
  named exemplar).
- **Anti-gaming clauses:** quote the claim rather than paraphrasing it into
  something easier to confirm; "reviewed the code" is not evidence; never weaken
  an acceptance criterion to match the code when the criterion states a desired
  end state the code has not reached (that is a `confirmed` gap, not a
  `corrected` claim).
- **Fail-closed gates** at `ready-for-dev`, `review`, and `done`.

**Rationale:** an artifact that names the obligation, the output shape, and the
verdict vocabulary, so review has something objective to check.

### 4.2 UPDATE — `_bmad/custom/bmad-create-story.toml`

OLD (`persistent_facts` tail):

```toml
  "file:{project-root}/_bmad/custom/remediation-runtime-checklist.md",
]
```

NEW:

```toml
  "file:{project-root}/_bmad/custom/remediation-runtime-checklist.md",
  "file:{project-root}/_bmad/custom/epic-ac-verification.md",
]
```

Plus one appended `activation_steps_append` directive beginning
`EPIC_AC_VERIFICATION:` requiring verification *before drafting acceptance
criteria*, the canonical table, acceptance criteria written from observed truth,
planning-artifact correction or escalation for every `corrected` claim, the
no-verifiable-claim note when applicable, and fail-closed behaviour before
`ready-for-dev` or sprint-status mutation.

**Rationale:** creation is where the premise enters the story; verifying after
drafting produces a story argued backwards from a false claim.

### 4.3 UPDATE — `_bmad/custom/bmad-dev-story.toml`

Adds the same persistent fact plus an `EPIC_AC_VERIFICATION:` directive requiring
the developer to re-derive any verdict the implementation contradicts, treat a
falsified `confirmed` verdict as a defect in the creation-time verification, append
the corrected row, correct the planning artifact, escalate scope-changing
corrections, create a dated adoption row for stories predating the policy, and
fail closed before setting `review`.

**Rationale:** the Epic 25 failure was found *during* implementation. Development
must have a defined action for that case instead of silently absorbing it.

### 4.4 UPDATE — `_bmad/custom/bmad-code-review.toml`

Adds the same persistent fact plus an `EPIC_AC_VERIFICATION:` directive requiring
the reviewer to independently re-run the commands behind every `corrected` row and
every row the diff touches, reject verdicts the diff contradicts, reject an
`unverifiable` record whose blocker the environment does not actually impose,
reject a paraphrased-weaker claim, confirm the planning-artifact correction or the
recorded human decision, route `patch` versus `decision_needed`, and treat an
unverified or contradicted claim as a fail-closed blocker for `done`.

No `[[workflow.review_layers]]` entry is added — see Section 2 non-goals.

**Rationale:** without an independent re-run, a self-declared verdict is exactly as
trustworthy as the epic claim it replaced.

### 4.5 UPDATE — `_bmad-output/planning-artifacts/epics.md`

Section: `## Phase: Post-MVP — Audit Remediation (2026-07-04)`, the
**Audit-anchor preflight** paragraph.

OLD:

> **Audit-anchor preflight:** Before any Epic 20-26 story is selected, created, or
> implemented, re-verify the current code anchors and implementation-state
> assumptions cited by that story against the repository. Story files must record
> the re-verification date, moved or renamed anchors, and how the implementation
> adapts. If an anchor is stale enough to change scope or acceptance evidence,
> update the story from current code evidence before development begins.

NEW: **Audit-anchor and AC-claim preflight (2026-07-04; broadened 2026-07-28)** —
epic-number-independent; extends the obligation from code anchors to every
verifiable claim (quantitative, existence/absence, behavioral, location) in the
inherited epic, PRD, architecture, or audit text; states that epic acceptance text
is advisory until re-derived and that the code wins on disagreement; requires the
per-claim command, verdict, and re-verification date per
`_bmad/custom/epic-ac-verification.md`; requires a `corrected` claim to correct
this file or carry a dated correction note here, and a scope- or intent-changing
correction to be escalated rather than absorbed; names the three Epic 25 claims as
recorded exemplars.

**Rationale:** the existing paragraph is the closest thing to this guard that
already existed, and it did not fire. Broadening it in place — rather than adding a
fourth adjacent paragraph — keeps one preflight rule instead of two overlapping
ones, and matches the precedent set when the cross-tenant carry-forward guard was
broadened on 2026-07-16.

### 4.6 UPDATE — `tests/tooling/bmad_customization/bmad_customization_test.py`

Adds constants (`AC_VERIFICATION_POLICY`, `AC_VERIFICATION_FACT`,
`AC_VERIFICATION_MARKER`, `EPICS_AC_PREFLIGHT_GUARD`) and two test methods:

1. `test_epic_ac_verification_wired_into_lifecycle_workflows` — loops both
   `.agents` and `.claude` surfaces × 3 skills; asserts the policy fact resolves,
   exactly one `EPIC_AC_VERIFICATION:` directive per skill, per-skill body phrases
   (so a gutted or cross-wired directive fails), and that the phase-ledger and
   runtime-checklist directives still resolve exactly once each (non-clobber).
2. `test_epic_ac_verification_policy_defines_claims_verdicts_and_gates` — asserts
   the canonical section heading and table header, all four claim classes, all
   three verdicts, the three Epic 25 regression claims by name, the
   no-verifiable-claim escape, the three gate headings, the load-bearing obligation
   sentences, and the exact broadened `epics.md` preflight paragraph.

Run: `python3 -m unittest tests.tooling.bmad_customization.bmad_customization_test -v`

**Rationale:** the fixture is the only thing that survives a BMad skill refresh
dropping the wiring. Asserting directive *bodies* and the pinned `epics.md`
paragraph — not just marker counts — is what makes the guard non-vacuous.

### 4.7 UPDATE — `_bmad-output/process-notes/story-creation-lessons.md`

Appends numbered lesson **L12 — Epic Acceptance Claims Are Advisory Until
Re-Derived**, following L09/L10/L11.

**Rationale:** the lessons file is loaded as a persistent fact by both
`bmad-create-story` and `bmad-code-review`; it is where the *why* survives after
the retrospective is archived.

### 4.8 UPDATE — `_bmad-output/implementation-artifacts/sprint-status.yaml`

Epic 25 `action_items` entry (the trigger of this proposal).

OLD:

```yaml
    status: in-progress  # 2026-07-16 (Epic 26): stories reconciled stale premises during implementation, but story creation still lacks an enforced pre-story AC-vs-code step.
```

NEW:

```yaml
    status: done  # 2026-07-28: enforced via _bmad/custom/epic-ac-verification.md, EPIC_AC_VERIFICATION directives in bmad-{create-story,dev-story,code-review}.toml, the broadened epics.md AC-claim preflight, 2 resolver-fixture tests, and lesson L12.
```

No `development_status` entry changes: no epic or story is added, removed, or
renumbered.

**Rationale:** closes the action item with dated, checkable evidence rather than a
narrative claim.

## 5. Implementation Handoff

**Scope classification: Moderate.** It spans planning artifacts, lifecycle-workflow
configuration, and a test fixture, and it changes the sprint backlog's governing
process — but it needs no replan and no architectural decision.

| Recipient | Responsibility |
| :-------- | :------------- |
| Developer (Amelia) | Apply 4.1–4.8; run the resolver fixture; preserve each file's existing line endings. |
| Product Owner / PM (John) | Co-owner of the action item; confirms the broadened `epics.md` preflight reads correctly as planning policy. |

### Success criteria

1. `python3 -m unittest tests.tooling.bmad_customization.bmad_customization_test -v`
   passes, including the 2 new tests, with no pre-existing test regressing.
2. `resolve_customization.py --key workflow` on `bmad-create-story`,
   `bmad-dev-story`, and `bmad-code-review` each resolve exactly one
   `EPIC_AC_VERIFICATION:` directive and still resolve exactly one
   `STORY_PHASE_LEDGER:` and one `REMEDIATION_RUNTIME_CHECKLIST:` directive, on
   both the `.agents` and `.claude` surfaces.
3. The next story created (Epic 30 or Epic 31) contains an
   `### Epic AC Verification` section with at least one row carrying a re-runnable
   command, or the explicit no-verifiable-claim note.
4. No file under `.claude/skills/**` or `.agents/skills/**` is modified.
5. `git diff --stat` shows no line-ending churn on the touched files.

### Follow-on items (not in this proposal)

| # | Item | Owner | Why deferred |
| :- | :--- | :---- | :----------- |
| 1 | Correct the three false Epic 25 claims in `epics.md` (60 literals, `Client.Rest`, double authorization), or annotate them as historically-recorded-and-refuted with the story that measured each. | John, Amelia | Epic 25 is closed; its epic text is a record of what was planned. Needs a product-owner call on correct-in-place versus annotate. |
| 2 | Add a `[[workflow.review_layers]]` independent subagent for AC verification in `bmad-code-review`. | Murat | Directive-only matches the most recent sibling; add only if directive-level enforcement proves insufficient in practice. |
| 3 | Epic 26 action item 7 — executable pre-review gate. | Murat, Amelia | Explicitly out of scope; this proposal adds prose enforcement only, and does not claim to discharge it. |

### Recorded risk

Enforcement is LLM-obeyed prose. It reduces the probability that a false premise
reaches a developer; it does not make it impossible. The fixture proves the
*wiring* resolves, not that any given story actually verified its claims. Item 3
above is the durable answer to that gap and stays open.
