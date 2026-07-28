---
change_trigger: "The epic-AC-verification guard reproduced the route-coverage and binding-point defect that the story-slice guard was corrected the same day to cure: it loads on 3 of 7 story-authoring routes and binds at ready-for-dev, while its own planning-side preflight binds at creation"
mode: batch
mode_basis: "Administrator selected batch on 2026-07-28; consistent with both proposals this correction reconciles"
status: approved-and-applied
requested_by: Administrator
project: Hexalith.Memories
date: 2026-07-28
scope_classification: moderate
supersedes: none
reconciles:
  - sprint-change-proposal-2026-07-28-epic-ac-code-verification.md
  - sprint-change-proposal-2026-07-28-story-slice-guard-route-and-binding-coverage.md
---

# Sprint Change Proposal: Epic AC Verification Route and Binding Coverage

Date: 2026-07-28
Project: Hexalith.Memories
Scope: Moderate process correction affecting which routes load the epic-AC-verification policy and the moment that policy binds. No product behavior change, no epic or story scope change, no story created or registered.

## 1. Issue Summary

### Problem statement

Two Sprint Change Proposals were approved and applied on 2026-07-28:

- `sprint-change-proposal-2026-07-28-story-slice-guard-route-and-binding-coverage.md` diagnosed that `story-scope-guard.md` was **route-scoped, not policy-scoped**, and that gating on `ready-for-dev` left a `backlog` exemption. It cured both: the guard now loads on seven routes and binds at authoring and registration.
- `sprint-change-proposal-2026-07-28-epic-ac-code-verification.md` created `epic-ac-verification.md` and wired it into three routes, gating at `ready-for-dev`.

The second reproduced the exact shape the first was executed to cure. This is the CR16 mechanism a second time, applied to a different policy on the same day.

| Guard | Routes resolving its directive | Binds at |
| :---- | :----------------------------- | :------- |
| `story-scope-guard.md` | **7** — create-story, code-review, correct-course, create-epics-and-stories, spec, sprint-planning (+dev-story exempt by design) | authored or registered, any status including `backlog` |
| `epic-ac-verification.md` | **3** — create-story, dev-story, code-review | `ready-for-dev` |

### 1.1 Four story-authoring routes never load the AC policy

`bmad-correct-course`, `bmad-create-epics-and-stories`, `bmad-spec`, and `bmad-sprint-planning` each author acceptance criteria, register stories, or promote them into sprint status. None resolves the `EPIC_AC_VERIFICATION` directive or the policy fact.

The most consequential of the four is **`bmad-create-epics-and-stories`**. That route is where epic acceptance text originates — it is where "60 server literals", "the CLI has no `Client.Rest` reference", and "a redundant double authorization" were first written down. `epic-ac-verification.md` was created to stop those three claims from reaching a developer, and it is not loaded on the route that produces them. The guard currently catches a false claim one or more hops downstream of the route that authors it.

The second is **`bmad-correct-course`** — the route that authored both CR16 anti-templates, and the route executing this proposal. This session is drafting acceptance-criteria-bearing content with the AC policy unloaded; it is available here only because the analysis read it as a file, which is precisely the ad-hoc, unenforced behavior the policy exists to replace.

### 1.2 The policy binds later than its own planning-side preflight

`epic-ac-verification.md` gates at `ready-for-dev` (three occurrences: canonical-section obligation, creation-gate fail-closed bullet, and the gated-status list). Its companion `epics.md:4077` preflight — written by the same proposal — binds "Before any story is selected, **created**, or implemented".

The two disagree. Proposal C of the slice-guard correction established the rule for exactly this situation:

> `story-scope-guard.md` and `epics.md:555` must bind at the same moment, or the stricter of the two is bypassed by citing the looser.

A story can therefore be authored with an unverified count, registered in `epics.md` and `sprint-status.yaml` at `backlog`, and remain fully compliant with the policy file until someone selects it. That is the same `backlog` exemption the slice-guard correction closed hours earlier.

### 1.3 The action item was closed with the defect inside it

`sprint-status.yaml:701` records Epic 25 action item 2 as `done`, and its closure note is accurate about what was delivered:

> `status: done  # 2026-07-28: enforced by _bmad/custom/epic-ac-verification.md (…), one EPIC_AC_VERIFICATION directive each in bmad-{create-story,dev-story,code-review}.toml, …, fail-closed at ready-for-dev/review/done …`

The note is not wrong; the scope it describes is incomplete. The retrospective success criterion — *"Story Dev Notes record an AC-vs-code check; stories stop opening with corrected premises"* — is forward-looking and cannot be satisfied while the origin route is unguarded.

### 1.4 Verified non-defects

Two plausible defects were tested and discarded rather than carried into this proposal:

- **The policy text is not route-scoped.** `epic-ac-verification.md` §Applicability already states it applies to "**every** story, spec, or remediation slice created from epic, PRD, architecture, or audit-anchor text — regardless of epic number". Only the *wiring* is narrow. No applicability rewrite is proposed.
- **No dual-root gap.** All seven routes resolve identically under `.claude/skills/**` and `.agents/skills/**`, consistent with the slice-guard correction's §1.5 finding.

### Trigger evidence

- `_bmad/custom/` resolution across seven routes — `EPIC_AC_VERIFICATION` count 1/1/1/0/0/0/0 versus `HISTORICAL_SLICE_GUARD` count 1/0/1/1/1/1/1.
- `_bmad/custom/epic-ac-verification.md:55`, `:112`, and the gated-status list — `ready-for-dev` binding.
- `_bmad/custom/story-scope-guard.md:26-31` — the authoring/registration binding this policy lacks.
- `_bmad-output/planning-artifacts/epics.md:4077` — "selected, created, or implemented".
- `_bmad-output/implementation-artifacts/sprint-status.yaml:701` — Epic 25 item 2 `done`.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-slice-guard-route-and-binding-coverage.md:259` — the two-binding-points rule.

## 2. Epic AC Verification

Verified 2026-07-28 against working tree at `115d30b5` (uncommitted 2026-07-28 changes present).

This proposal dogfoods the policy it extends. Every load-bearing claim above is a claim of the classes the policy governs.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| `epic-ac-verification.md` loads on 3 routes | Quantitative | `for s in <7 routes>; do resolve_customization.py --skill .claude/skills/$s --key workflow \| grep -c "EPIC_AC_VERIFICATION:"; done` | `1,1,1,0,0,0,0` | confirmed |
| `story-scope-guard.md` loads on 6 of the same 7 | Quantitative | same loop, `grep -c "HISTORICAL_SLICE_GUARD:"` | `1,0,1,1,1,1,1` | confirmed |
| AC policy binds at `ready-for-dev` | Behavioral | `grep -c 'ready-for-dev' _bmad/custom/epic-ac-verification.md` | `3` (§55 canonical, §112 fail-closed, §887 gated list) | confirmed |
| Slice guard binds at authoring/registration | Behavioral | `grep -c 'authored or registered' _bmad/custom/story-scope-guard.md` | `1` (§26) | confirmed |
| `epics.md` preflight binds at creation | Existence | `grep -n "Audit-anchor" _bmad-output/planning-artifacts/epics.md` | `:4077` "selected, created, or implemented" | confirmed |
| Epic 25 action item 2 is `done` | Existence | `grep -n "epic-ac-verification" _bmad-output/implementation-artifacts/sprint-status.yaml` | `:701 status: done` | confirmed |
| `bmad-spec` persistent_facts pinned to bridge fact only | Behavioral | `bmad_customization_test.py:333-340` asserts `== [SPEC_PROJECT_CONTEXT_FACT]` | pin present | confirmed |
| AC policy is route-scoped in text | Behavioral | read `epic-ac-verification.md` §Applicability | applies to "**every** story, spec, or remediation slice" — text is already route-independent | **corrected** — hypothesis refuted; no applicability rewrite proposed (§1.4) |
| PRD, architecture, or UX carries story-governance rules | Existence | `grep -c "story-scope-guard\|_bmad/custom\|epic-ac"` on `prd.md`, `architecture.md`, `ux-design-specification.md` | `0`, `0`, `0` | confirmed |

No `unverifiable` rows. The one `corrected` verdict changed this proposal's own scope before drafting, not after — the failure mode §1.1 describes.

## 3. Impact Analysis

### 3.1 Epic impact

| Epic | Impact | Can it still complete as planned? |
| :--- | :----- | :-------------------------------- |
| Epic 25 | Action item 2 disposition amended. Epic closed; no story reopened. | Yes |
| Epic 26 | None. Follow-through row 2 already records the item; action item 7 (executable gate) stays open and is not claimed. | Yes |
| Epic 30 (5 stories, `backlog`) | Primary beneficiary — every story is authored under the widened gate, including if a future correction registers it. | Yes |
| Epic 31 (`in-progress`) | None. 31.1 `review`, 31.2 `ready-for-dev` with its measured-state section. Neither re-scoped. | Yes |
| All others | None. | Yes |

No epic is added, removed, resequenced, or reprioritized. No epic acceptance criterion changes.

### 3.2 Story impact

- **No story is created, split, renamed, or registered by this proposal.** The `HISTORICAL_SLICE_GUARD` directive binding this route is therefore satisfied with no story to classify — stated explicitly rather than left implied.
- No completed or in-progress story is reopened, re-scoped, or rolled back.
- Future stories authored by **any** of the seven routes must carry the `### Epic AC Verification` record from the moment they are written, not from the moment they are selected.

### 3.3 Artifact conflicts

| Artifact | Conflict | Resolution |
| :------- | :------- | :--------- |
| PRD | None. Zero governance references (verified). MVP scope, FR/NFR coverage untouched. | No edit |
| `architecture.md` | None. Decision D17 already provides for module-specific local verification lanes (`architecture.md:564`); this is an instance of it. | No edit |
| UX specification | None. No component, flow, wireframe, interaction, or accessibility surface. | No edit |
| `epics.md:4077` preflight | Binds at creation but omits *registration*; a story row written into `epics.md` at `backlog` is not obviously "a story created". | Minor amendment (Proposal D) |
| `epic-ac-verification.md` | Binds at `ready-for-dev`, looser than its own preflight. | Amend binding (Proposal B) |
| `bmad_customization_test.py` | Pins the exact `epics.md` preflight paragraph (`EPICS_AC_PREFLIGHT_GUARD`, line 104) and covers 3 routes. | Extend (Proposal C) |

### 3.4 Technical and operational impact

- No C#, package, schema, API, persistence, deployment, container, or submodule change.
- No new tooling gate and no CI change. Enforcement remains LLM-obeyed prose plus the resolver fixture, matching the guard being extended.
- All changes live under committed repository-owned paths and survive BMad skill refreshes. No file under `.claude/skills/**` or `.agents/skills/**` is touched.

## 4. Recommended Approach

**Option 1 — Direct Adjustment. Selected.**

- **Option 1 — Direct Adjustment:** Viable. Extend an approved, applied correction along the two axes a sibling correction already proved necessary, reusing its exact route list, its `bmad-spec` deviation, and its fixture shape. Effort **Low**, product risk **none**, process risk **low**.
- **Option 2 — Rollback:** Not viable. Both proposals are correct in what they deliver; neither should be reverted. There is no bad product code and no wrong story to unwind.
- **Option 3 — PRD MVP Review:** Not viable and not warranted. MVP scope, product goals, and FR/NFR coverage are untouched (verified: zero governance references in `prd.md`).

### Rationale

The slice-guard correction paid the full diagnostic cost of discovering that a story-governance policy must be wired to every authoring route and must bind at authoring rather than selection. That finding is policy-independent: it is a property of how stories enter this repository, not of what any one guard checks. Applying it to `epic-ac-verification.md` is the cheapest possible correction because the route list, the `bmad-spec` persistent-fact deviation, and the fixture structure are all already approved and implemented — this proposal copies a proven shape rather than inventing one.

Leaving the asymmetry in place has a specific cost, not a theoretical one: the route that writes epic acceptance claims is the route that does not verify them.

### Alternatives considered and rejected

- **Add a deterministic `tools/check-epic-ac-verification.py` gate.** Rejected for this proposal. Proposal 1 explicitly deferred it (follow-on item 2), and Epic 26 action item 7 owns the executable-pre-review-gate question. Claiming it here would double-book an open item. Recorded as a follow-on.
- **Add a `[[workflow.review_layers]]` AC-verification subagent.** Rejected — unchanged from Proposal 1's non-goals; directive-only matches the siblings.
- **Merge the two policies into one file.** Rejected. They check different things (outcome breadth versus claim truth), have different verdict vocabularies, and are pinned separately by two fixture suites. Merging would couple two independently-evolving guards.
- **Close Epic 25 item 2 as-is and open a new action item.** Rejected. The item's own success criterion is not met while the origin route is unguarded; a new item would obscure that this is the same gap.

### Estimate

Effort **Low** — approximately 2–3 hours: four toml edits, one policy-file section, one `epics.md` sentence, one pinned test constant, two fixture tests, one lesson, one status line. No product milestone impact. Apply before the next story-authoring run on any route.

### Known limitation, stated up front

This correction changes **which routes load the policy and when it binds**. It does not make the verification itself deterministic. The fixture proves the wiring resolves; it cannot prove any given story actually re-derived its claims. That gap is real, is unchanged by this proposal, and stays owned by Epic 26 action item 7.

## 5. Detailed Change Proposals

### Proposal A — Load `epic-ac-verification.md` on the four unguarded authoring routes

Artifacts: `_bmad/custom/bmad-correct-course.toml`, `_bmad/custom/bmad-create-epics-and-stories.toml`, `_bmad/custom/bmad-sprint-planning.toml`, `_bmad/custom/bmad-spec.toml`

All four files already exist (created by the slice-guard correction). Each gains one `persistent_facts` entry and one `activation_steps_append` directive — **except `bmad-spec`, which gains the directive only**.

OLD (shape, `bmad-correct-course.toml`):

```toml
persistent_facts = [
  "file:{project-root}/_bmad/custom/story-scope-guard.md",
  "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md",
]
```

NEW:

```toml
persistent_facts = [
  "file:{project-root}/_bmad/custom/story-scope-guard.md",
  "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md",
  "file:{project-root}/_bmad/custom/epic-ac-verification.md",
]
```

Route-specific directives, each carrying its own fail-closed clause so a gutted or cross-wired directive fails the fixture:

- **`bmad-correct-course`** — authors and amends acceptance criteria and registers stories. Verify every verifiable claim the correction inherits *and* every claim the proposal itself asserts, before writing an AC into the proposal, `epics.md`, `sprint-status.yaml`, or a story file at any status including `backlog`. Fail closed before recording an approval while any verifiable claim lacks a verdict.
- **`bmad-create-epics-and-stories`** — *the origin route*. Verify every count, existence/absence assertion, behavioral description, and location before writing it into an epic or story; record the re-runnable command and observed value with the claim, or state the claim as intent rather than as fact. Do not write a count or anchor that no command in this run confirmed. Fail closed before registering the generated list at any status.
- **`bmad-spec`** — freezes intent into a machine contract, so an unverified claim becomes a binding requirement. Verify every inherited claim before freezing; never carry an `unverifiable` claim as load-bearing justification for scope. Fail closed before approval.
- **`bmad-sprint-planning`** — do not promote or re-status a story whose Epic AC Verification record is missing, whose `corrected` row lacks its planning-artifact correction or recorded escalation, or whose `unverifiable` row lacks its blocker record. Report the gap instead of transcribing the story. Registration at `backlog` grants no exemption.

**Deliberate asymmetry — `bmad-spec` adds no `persistent_facts` entry.** `bmad_customization_test.py:333-340` pins `bmad-spec`'s resolved `persistent_facts` to exactly the project-context bridge fact so the bridge's fail-closed control cannot be diluted. That contract is not weakened. The policy reaches `bmad-spec` through its directive, which names the file to read — identical to how the slice-guard correction delivered `story-scope-guard.md` to this route, and pinned by the same style of dedicated test.

Rationale: the guard that exists to stop false epic claims must run on the route that writes them.

### Proposal B — Bind the policy at authoring and registration

Artifact: `_bmad/custom/epic-ac-verification.md`, sections `## Canonical story section` and `## Creation gate`

OLD (`## Canonical story section`):

```markdown
Every governed story must contain this section under `## Dev Notes` before it
enters `ready-for-dev`:
```

NEW:

```markdown
Every governed story must contain this section under `## Dev Notes`. The
obligation attaches when the story is **authored or registered** — the moment it
is written into a story file, `epics.md`, or `sprint-status.yaml` — at any
status, including `backlog`. `ready-for-dev` is a second, stricter checkpoint,
not the first one. A story registered with an unverified claim is in violation
while it sits in the backlog; it does not become compliant by not being selected
yet.
```

OLD (`## Creation gate`, fail-closed bullet):

```markdown
- Fail closed: do not set `ready-for-dev` and do not mutate sprint status while
  any verifiable claim lacks a verdict, any `corrected` claim lacks its
  planning-artifact correction or recorded escalation, or any `unverifiable`
  claim lacks its blocker record.
```

NEW — prepend a binding paragraph to the section and widen the bullet:

```markdown
This gate binds every route that authors or registers a story or an epic
acceptance claim — story creation, correct-course, epic-and-story generation,
spec authoring, and sprint planning — not only the story-creation route. The
route that writes a claim owns verifying it; a claim written by one route and
verified by a later one has already reached a reader as fact.

- Fail closed: do not write a verifiable claim into a story file, `epics.md`, or
  `sprint-status.yaml` at any status, do not set `ready-for-dev`, and do not
  mutate sprint status while any verifiable claim lacks a verdict, any
  `corrected` claim lacks its planning-artifact correction or recorded
  escalation, or any `unverifiable` claim lacks its blocker record.
```

The remaining creation-gate bullets, the Development gate, and the Review gate are unchanged; `ready-for-dev`, `review`, and `done` all remain named, so the existing pinned assertions still hold.

Rationale: closes the same `backlog` exemption the slice-guard correction closed, and removes the disagreement with `epics.md:4077`.

### Proposal C — Extend the resolver fixture to the seven routes and the new binding

Artifact: `tests/tooling/bmad_customization/bmad_customization_test.py`

1. Add `AC_AUTHORING_ROUTES` and `AC_AUTHORING_ROUTES_WITH_POLICY_FACT` constants mirroring `historical_slice_guard_test.py:18-32`, with the same recorded reason for the `bmad-spec` exclusion.
2. Extend `test_epic_ac_verification_wired_into_lifecycle_workflows` to assert exactly one `EPIC_AC_VERIFICATION:` directive on all seven routes across both surfaces, with a distinct load-bearing phrase pinned per route, and the sibling guards still resolving exactly once each.
3. Add `test_epic_ac_verification_binds_at_authoring_not_only_at_ready_for_dev`, mirroring `historical_slice_guard_test.py:160` — asserts `authored or registered`, `at any status, including \`backlog\``, the second-stricter-checkpoint sentence, and the widened fail-closed bullet.
4. Add `test_spec_route_receives_ac_policy_without_diluting_context_bridge`, asserting `bmad-spec` resolves the directive, that the directive names the policy file, and that resolved `persistent_facts` still equals exactly `[SPEC_PROJECT_CONTEXT_FACT]`.
5. Update the pinned `EPICS_AC_PREFLIGHT_GUARD` constant (line 104) to the Proposal D text.
6. Assert the four new routes retain their generated defaults and their `HISTORICAL_SLICE_GUARD` directive — the non-clobber check in the other direction.

Run: `python3 -m unittest tests.tooling.bmad_customization.bmad_customization_test -v`

Rationale: the four new overrides must be as refresh-safe as the three existing ones, and asserting directive *bodies* rather than marker counts is what keeps the guard non-vacuous.

### Proposal D — Align the `epics.md` preflight to the same binding moment

Artifact: `_bmad-output/planning-artifacts/epics.md:4077`

OLD (opening clause):

> **Audit-anchor and AC-claim preflight (2026-07-04; broadened 2026-07-28):** Before any story is selected, created, or implemented—regardless of epic number—re-verify against the current repository …

NEW (opening clause; remainder of the paragraph unchanged):

> **Audit-anchor and AC-claim preflight (2026-07-04; broadened 2026-07-28; bound at authoring and registration 2026-07-28):** Before any story is authored, registered, selected, created, or implemented—regardless of epic number, and at any status including `backlog`—re-verify against the current repository …

Plus one appended sentence: *A story created by an approved sprint change is bound at the moment that change registers it, not at the moment it is later selected.* — the same clause Proposal C of the slice-guard correction added to `epics.md:555`.

Rationale: makes both binding points identical in wording as well as in intent, so neither can be bypassed by citing the other. Requires the companion constant update in Proposal C item 5.

### Proposal E — Record the route and binding lessons

Artifact: `_bmad-output/process-notes/story-creation-lessons.md`, section `## L12 - Epic Acceptance Claims Are Advisory Until Re-Derived`

Append:

```markdown
- The policy binds at authoring and registration, at any status. A story
  registered with an unverified claim is in violation while it sits in the
  backlog.
- Every route that authors or registers a story or an epic acceptance claim must
  load the policy — create-story, dev-story, code-review, correct-course,
  create-epics-and-stories, spec, and sprint-planning. The route that writes a
  claim owns verifying it.
- `bmad-create-epics-and-stories` is the origin route: the three Epic 25 false
  claims were written there. A guard that skips it catches false claims one or
  more hops downstream of where they enter.
- A process correction is itself bound by the guards it extends. This guard was
  created on 2026-07-28 with the same route-coverage and binding-point defect
  that a sibling correction had cured hours earlier; the recurrence was found by
  cross-reading the two proposals, not by either one's own review.
```

Rationale: the lessons file is loaded by both `bmad-create-story` and `bmad-code-review`; the fourth bullet is the durable, generalizable finding — new guards inherit known defect shapes unless someone checks.

### Proposal F — Give Epic 25 action item 2 an honest disposition

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml:701`

OLD:

```yaml
    status: done  # 2026-07-28: enforced by _bmad/custom/epic-ac-verification.md (…), one EPIC_AC_VERIFICATION directive each in bmad-{create-story,dev-story,code-review}.toml, the epics.md preflight broadened to all epics, 2 resolver-fixture tests (16/16 pass), and lesson L12. Sprint change proposal 2026-07-28-epic-ac-code-verification.
```

NEW:

```yaml
    status: in-progress  # 2026-07-28: enforced by _bmad/custom/epic-ac-verification.md and one EPIC_AC_VERIFICATION directive on all seven story-authoring routes, bound at authoring/registration rather than ready-for-dev, per sprint-change-proposal-2026-07-28-epic-ac-verification-route-and-binding-coverage.md extending 2026-07-28-epic-ac-code-verification.md. Remains active: the retro success criterion ("stories stop opening with corrected premises") is forward-looking, so this closes only when a subsequent story is authored on a newly-covered route with a complete Epic AC Verification record on the first pass and no follow-up correction.
```

Rationale: `done` overstates a guard that skipped the origin route and bound later than its own preflight. `open` would understate a delivered policy, four wired routes, and a passing fixture. `in-progress` with a named closure condition is exactly how Epic 0 items 3 and 4 are carried, and how the slice-guard correction dispositioned the same class of forward-looking criterion.

**This is the one change that moves a status backwards, and it is the item most worth your explicit confirmation.**

## 6. Change Analysis Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger identified. No single triggering story: the trigger is a same-day recurrence found by cross-reading two approved proposals named in the invocation.
- [x] 1.2 Core problem defined. Category: **failed approach requiring different solution** — the policy text is correct but under-bound in route and timing, identically to its sibling before correction.
- [x] 1.3 Evidence collected. Seven-route resolution counts, three `ready-for-dev` bindings, the `epics.md:4077` clause, `sprint-status.yaml:701`, and the two-binding-points rule — all re-run 2026-07-28 and tabled in §2.

### 2. Epic impact assessment

- [x] 2.1 Epic 25 closed; only its retrospective ledger changes. Epics 26, 30, 31 complete as planned.
- [N/A] 2.2 No epic scope or acceptance-criteria change.
- [x] 2.3 Remaining epics reviewed. The guard is route-based and epic-number-independent; no epic-specific carve-out required.
- [N/A] 2.4 No epic invalidated; no new epic needed.
- [N/A] 2.5 No epic order or priority change.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed — zero governance references (verified, §2). No conflict, no modification, MVP unaffected.
- [x] 3.2 Architecture reviewed — zero references; D17 already covers module-local verification lanes. No conflict, no modification.
- [N/A] 3.3 UX reviewed — zero references. No UI, flow, wireframe, interaction, or accessibility impact.
- [x] 3.4 Other artifacts reviewed. Four customization tomls, the shared policy, one `epics.md` sentence, one fixture suite, the durable lesson, and the action-item ledger require changes. No CI, tooling-gate, deployment, IaC, or monitoring change.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment — **viable**. Effort low, product risk none, process risk low.
- [N/A] 4.2 Rollback — **not viable and not needed**. Both proposals are correct in what they deliver.
- [N/A] 4.3 PRD MVP review — **not required**. MVP scope, goals, and FR/NFR coverage untouched.
- [x] 4.4 Selected path: **Option 1, Direct Adjustment**, reusing the approved route list, `bmad-spec` deviation, and fixture shape from the sibling correction.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary written with recurrence evidence and two verified non-defects.
- [x] 5.2 Epic and artifact impacts documented per artifact.
- [x] 5.3 Recommended path and four rejected alternatives documented.
- [x] 5.4 MVP impact: none. Action plan and sequencing explicit in §7.
- [x] 5.5 Moderate-scope Product Owner / Developer handoff defined in §7.

### 6. Final review and handoff

- [x] 6.1 All applicable items addressed; no undocumented `[!]` item.
- [x] 6.2 Proposal checked against current repository evidence on 2026-07-28; every quantitative claim carries a re-runnable command in §2.
- [x] 6.3 Administrator approval — Administrator approved Proposals A–F on 2026-07-28.
- [x] 6.4 `sprint-status.yaml` change is limited to the Epic 25 action-item disposition (Proposal F). No epic or story entry is added, removed, renumbered, or re-statused.
- [x] 6.5 Moderate-scope Product Owner / Developer handoff accepted on 2026-07-28; Proposals A–F implemented in the same run.

### Story-scope guard applicability

This correction **creates, renames, splits, and registers no story**. The `HISTORICAL_SLICE_GUARD` directive binding this route is satisfied with nothing to classify. Stated explicitly rather than left implied.

## 7. Implementation Handoff

Scope classification: **Moderate** — workflow-governance and process-policy changes across planning artifacts, lifecycle configuration, and a test fixture. No replan and no architectural decision required.

Route: **Product Owner / Developer agent.**

### Responsibilities

Product Owner / workflow owner (John):

1. Confirm the binding-point change (authoring and registration, any status) is the intended standard for AC verification, as it already is for slice scope.
2. Confirm the `epics.md:4077` amendment reads as a governance clarification, not an epic-scope change.
3. Confirm the Epic 25 action item 2 disposition — specifically the `done` → `in-progress` move and its named closure condition.

Developer agent (Amelia):

1. Apply Proposals A–F without editing `.claude/skills/**`, `.agents/skills/**`, product code, PRD, architecture, UX, or submodule contents.
2. Resolve all seven customized routes on both surfaces; verify exactly one `EPIC_AC_VERIFICATION` directive each, generated defaults retained, and the `HISTORICAL_SLICE_GUARD`, `STORY_PHASE_LEDGER`, and `REMEDIATION_RUNTIME_CHECKLIST` directives still resolving exactly once where they already did.
3. Preserve each file's existing line endings — `.toml` and `.md` are CRLF per `.gitattributes`; the `.py` fixture is LF.
4. Run `git diff --check` and report exact resolved evidence.

### Implementation sequence

1. **Proposal B and D** — align the two binding points first, so the fixture is written against the final rule.
2. **Proposal A** — add the four route directives (and three persistent facts; not `bmad-spec`).
3. **Proposal C** — extend the fixture, including the `EPICS_AC_PREFLIGHT_GUARD` constant update that Proposal D forces.
4. **Proposal E** — append the L12 lessons.
5. **Proposal F** — set the action-item disposition last, once the preceding evidence exists.

### Success criteria

1. All seven story-authoring routes resolve `epic-ac-verification.md`'s directive exactly once on both `.claude` and `.agents`; `bmad-spec` resolves it without gaining a `persistent_facts` entry.
2. `python3 -m unittest tests.tooling.bmad_customization.bmad_customization_test -v` passes, including the new tests, with no pre-existing test regressing.
3. `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` passes — the slice-guard suite included, proving non-clobber in both directions.
4. `epic-ac-verification.md` and `epics.md:4077` state the same binding moment in the same words.
5. `tools/check-story-slice-scope.py` passes on this change set (no story key registered).
6. No file under `.claude/skills/**` or `.agents/skills/**`, no product code, no PRD/architecture/UX, no epic or story scope, and no submodule pointer is changed.
7. `git diff --check` exits 0 with no line-ending churn on touched files.

### Follow-on items (not in this proposal)

| # | Item | Owner | Why deferred |
| :- | :--- | :---- | :----------- |
| 1 | Deterministic `tools/check-epic-ac-verification.py` gate mirroring `check-story-slice-scope.py`. | Murat, Amelia | Epic 26 action item 7 owns the executable-pre-review-gate question; claiming it here would double-book an open item. |
| 2 | Correct or annotate the three false Epic 25 claims still in `epics.md` (60 literals, `Client.Rest`, double authorization). | John, Amelia | Carried unchanged from `2026-07-28-epic-ac-code-verification.md` follow-on 1. Needs a product-owner call on correct-in-place versus annotate. |
| 3 | Audit whether `remediation-runtime-checklist.md` and `story-phase-ledger.md` carry the same route-coverage and binding-point defect. | Amelia | The recurrence found here suggests it is a defect *class*, not two instances. Out of scope; worth its own pass. |

### Recorded risk

Enforcement remains LLM-obeyed prose. This correction widens *where* and *when* the obligation applies; it does not make the verification deterministic. The fixture proves the wiring resolves, not that any story actually re-derived its claims. Follow-on item 1 is the durable answer and stays open.

## 8. Workflow Execution Log

- 2026-07-28: Invoked as `/bmad-correct-course epic-ac-code-verification, story-slice-guard-route-and-binding-coverage`. Customization resolved; `HISTORICAL_SLICE_GUARD` activation directive loaded and applied.
- 2026-07-28: Concurrent-session check run first (`git status --short _bmad-output/planning-artifacts/ _bmad/custom/`); five same-day proposals present, both named proposals confirmed applied in the working tree.
- 2026-07-28: Trigger derived by cross-reading the two named proposals, then confirmed with Administrator, who selected scope **route + binding** and mode **batch**.
- 2026-07-28: Change Navigation Checklist completed; no product-scope, epic-order, architecture, or UX change required.
- 2026-07-28: Two hypotheses tested and discarded rather than carried into the proposal — policy-text route-scoping and a dual-root resolution gap (§1.4).
- 2026-07-28: Epic AC Verification table produced for this proposal's own claims (§2); one `corrected` verdict changed the proposal's scope before drafting.
- Scope classification: Moderate. Routed to: Product Owner / workflow owner and Developer agent.
- 2026-07-28: Administrator approved Proposals A–F, including the Epic 25 action item 2 `done` → `in-progress` disposition.
- 2026-07-28: Proposals A–F implemented in the same run in the stated sequence, with two recorded deviations (§9).
- Handoff status: complete. Implementation applied and verified.

## 9. Approval Record

Approved by Administrator on 2026-07-28 and implemented in the same workflow
run, as the two proposals it reconciles also were.

### Deviation from the approved text — Proposal E, stale route list in L12

L12 already carried a bullet naming enforcement as living in
`_bmad/custom/bmad-{create-story,dev-story,code-review}.toml`. Proposal E
specified appended bullets only, but leaving that sentence would have left the
lesson asserting the exact three-route coverage this correction removes. The
sentence was rewritten to "`_bmad/custom/*.toml` overrides … on all seven
story-authoring routes (2026-07-28)" rather than appending a contradicting
bullet beneath it.

### Deviation from the approved text — Proposal D, comma alignment

Proposal D's NEW text read "at any status including `backlog`".
`story-scope-guard.md` and `epics.md:555` both use "at any status, including
`backlog`". Success criterion 4 requires the same binding moment in the same
words, so the comma was added and the pinned test constant matched to it. All
three artifacts now contain the identical phrase (verified under whitespace
normalization: 1, 1, and 2 occurrences).

### Implementation evidence

- **Proposals A–F applied.** No file under `.claude/skills/**` or
  `.agents/skills/**`, no product code, no PRD, architecture, or UX artifact,
  and no submodule pointer was changed. No epic or story scope changed: the only
  `epics.md` edit is the Proposal D governance clause, and the only
  `sprint-status.yaml` edit is the Proposal F action-item disposition.
- **Route resolution.** All seven story-authoring routes resolve exactly one
  `EPIC_AC_VERIFICATION` directive under **both** `.claude/skills/**` and
  `.agents/skills/**` (`1 1 1 1 1 1 1` on each surface). Sibling guards are
  intact where they applied before: `HISTORICAL_SLICE_GUARD` on six routes,
  `STORY_PHASE_LEDGER` and `REMEDIATION_RUNTIME_CHECKLIST` on three each.
- **`bmad-spec` contract preserved.** Resolved `persistent_facts` is still
  exactly `["file:{project-root}/project-context.md"]`; the policy is delivered
  by directive, which names the file. Generated defaults survive (`spec_filename:
  SPEC.md`, `spec_template` present).
- **Binding alignment.** `epic-ac-verification.md`, `story-scope-guard.md`, and
  `epics.md` all state "at any status, including `backlog`".
- **Test results.** `bmad_customization` 29 passed (26 → 29; the AC-related
  subset is 5 tests and the file total is 19), `story_scope` 42, `story_slice_scope`
  20, `tenant_isolation_evidence` 34, `line_endings` 4, `coverage_gate` 31,
  `integration_stub_closure` 7, `production_deployment_evidence` 30 — 197 total,
  0 failures.
- **Non-vacuity proven by mutation.** Three independent mutations were applied
  and reverted, each caught: stripping the `EPIC_AC_VERIFICATION` directive from
  `bmad-sprint-planning.toml` (2 failures), reverting the canonical-section
  binding to `ready-for-dev` only (1 failure), and reverting the `epics.md`
  preflight binding clause (1 failure). Baseline green after each restore.
- **Governance gates.** `tools/check-story-slice-scope.py` exits 0 on the change
  set — "registration surface changed with no story file in the same change" —
  consistent with this correction registering no story.
- **Whitespace.** `git diff --check` exits 0. Each file kept its existing line
  endings: the four route `.toml` files, `epics.md`, `story-creation-lessons.md`,
  and `sprint-status.yaml` are CRLF; `epic-ac-verification.md` and the `.py`
  fixture are LF, matching their pre-existing state.

### Not verified here

- No .NET build or test suite was run: this correction changes no C# source.
  `CiTestInventoryTests` was not re-run because no test suite was added or
  removed — only existing test methods within an already-wired suite changed.
- `tools/check-story-file-scope.py` exits 0 only because no story key resolves
  for this change set. That is the known vacuous-pass shape of that gate and is
  **not** cited as scope evidence here.
- `tests/tooling/access_telemetry_lifecycle` reports "Ran 0 tests" under the
  `*_test.py` discovery pattern because its file is named `test_adapter_profile.py`.
  This is pre-existing and unrelated to this correction; recorded as an
  observation, not fixed here.
- The `references/*` gitlink drift and the concurrent working-tree edits to
  `bmad-{create-story,dev-story,code-review}.toml`, `story-phase-ledger.md`,
  `story-scope-guard.md`, `historical_slice_guard_test.py`, and Story 31.1
  originate from other sessions and were left untouched.
