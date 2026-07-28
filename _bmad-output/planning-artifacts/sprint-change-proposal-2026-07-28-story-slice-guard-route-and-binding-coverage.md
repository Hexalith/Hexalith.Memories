---
change_trigger: "Strengthen story creation and review checks so historical broad slices are not reused as templates"
mode: batch
mode_basis: "Administrator selected batch on 2026-07-28; consistent with the 2026-07-06 and 2026-07-16 corrections for the same trigger"
status: approved-implemented
project: Hexalith.Memories
date: 2026-07-28
scope_classification: moderate
supersedes: none
extends:
  - sprint-change-proposal-2026-07-06-historical-slice-story-guard.md
  - sprint-change-proposal-2026-07-16-historical-slice-guard-strengthening.md
---

# Sprint Change Proposal: Story Slice Guard Route and Binding Coverage

Date: 2026-07-28
Project: Hexalith.Memories
Scope: Moderate process correction affecting story authoring routes, the guard's binding point, and deterministic verification. No product behavior change.

## 1. Issue Summary

Epic 0 retrospective action item 3 — "Strengthen story creation and review checks so historical broad slices are not reused as templates" — is still `open` in `sprint-status.yaml:515` after two approved corrections (2026-07-06, 2026-07-16).

The policy itself is correct and, where it is wired, it works. Story 31.1 carries a ten-row `Historical Context Classification` table, and the full-review `historical-slice-guard` layer produced an actionable `[Review][Patch]` finding when one row was missing its originating story. Nothing in this proposal weakens or replaces that.

The residual defect is **coverage**, in three distinct dimensions:

### 1.1 The guard is route-scoped, not policy-scoped

`_bmad/custom/story-scope-guard.md` is loaded by exactly two skills — `bmad-create-story` and `bmad-code-review` (the latter full-mode only). Stories are also authored and registered by routes that never load it:

| Route | `_bmad/custom/*.toml` | Loads `story-scope-guard.md` | Authors or registers stories |
| :---- | :-------------------- | :--------------------------- | :--------------------------- |
| `bmad-create-story` | yes | yes | yes |
| `bmad-code-review` | yes | yes (review side) | no |
| `bmad-correct-course` | **no** | **no** | **yes** |
| `bmad-create-epics-and-stories` | **no** | **no** | **yes** |
| `bmad-spec` | **no** | **no** | **yes** |
| `bmad-sprint-planning` | **no** | **no** | **yes** |

### 1.2 The gap already produced the exact failure the guard exists to prevent

`sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md:89` is titled **"CR16 — the split reproduced the shape it was executed to cure"**. Its own finding:

> The 2026-07-26 correction split Story 27.3's bundled scope into Story 30.1 and Story 31.1. Both new stories reproduce the anti-template shape that `story-scope-guard.md` forbids.
>
> - **Story 30.1** carries seven `Given/When/Then` blocks spanning dispatch hardening, manifest migration, four-image publication, partial-release recovery, cutover parity, rollback, and registry authorization; names eight "separate reviewable checkpoints" with no owner, evidence command, review state or completion state [...]
> - **Story 31.1** bundles OpenBao platform hardening and the runtime `secretstore` migration — two independently deployable outcomes — with no checkpoint table at all.

Both anti-templates were authored by `bmad-correct-course`, a route that does not load the policy. A third correction (2026-07-27) was required to split one story into four and another into two.

### 1.3 The binding point admits the violation

The same proposal states the mechanism precisely:

> `epics.md:555` and `story-scope-guard.md:30-31` do not bind while both are `backlog`, so nothing is currently violated. Selecting either as written re-creates the violation, which is why the reopen trigger is `ready-for-dev`, not `done`.

`story-scope-guard.md:30-31` gates on `ready-for-dev`; `epics.md:555` gates "when checkpoint-heavy stories are selected for implementation". A bundled story can therefore be authored, registered in `epics.md`, and written into `sprint-status.yaml` as `backlog` while remaining fully compliant. Detection is deferred to whoever later selects it — which is how two anti-templates sat in the backlog until a third correction removed them.

### 1.4 Enforcement is instruction-only; there is no deterministic check

`tests/tooling/bmad_customization/historical_slice_guard_test.py` asserts the guard is **configured**. It cannot assert the guard **ran**. `spec-strengthen-story-creation-review-historical-slice-templates.md:82` records this limitation honestly: "evidence no longer overclaims execution of nondeterministic LLM behavior."

The sibling Epic 0 action item (cross-tenant negative evidence) received a deterministic gate — `tools/check-tenant-isolation-evidence.py` plus `tests/tooling/tenant_isolation_evidence` and a CI step. This action item received no equivalent. `tools/check-story-file-scope.py` validates `## File Scope` (which files a story may touch), not slice scope (how much outcome a story may carry).

### 1.5 Verified non-defect

The `.claude/skills/**` and `.agents/skills/**` installations both resolve the guard, because `_bmad/scripts/resolve_customization.py` keys team overrides on the skill directory basename. Confirmed on 2026-07-28: both roots return the policy fact and the `HISTORICAL_SLICE_GUARD` directive. No dual-root gap exists and none is proposed.

### Trigger evidence

- `_bmad-output/implementation-artifacts/sprint-status.yaml:515` — action item `status: open`.
- `_bmad-output/implementation-artifacts/epic-0-retro-2026-07-06.md:134` — original action item and success criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md:89-104` — CR16 recurrence, both anti-templates, and the backlog non-binding statement.
- `_bmad-output/planning-artifacts/epics.md:5131`, `epics.md:5298` — the two split notes recording the reproduced shape.
- `_bmad-output/planning-artifacts/epics.md:555` — the shape-based checkpoint rule and its "selected for implementation" binding.
- `_bmad/custom/` directory listing — no toml for `bmad-correct-course`, `bmad-create-epics-and-stories`, `bmad-spec`, `bmad-sprint-planning`.
- `tools/check-tenant-isolation-evidence.py` — the deterministic-gate pattern this action item lacks.
- `_bmad-output/implementation-artifacts/spec-strengthen-story-creation-review-historical-slice-templates.md:82` — recorded configuration-vs-execution limitation.

## 2. Impact Analysis

### 2.1 Epic impact

| Epic | Impact | Can it still complete as planned? |
| :--- | :----- | :-------------------------------- |
| Epic 0 | Action item 3 gains a dated disposition and a defined closure condition. No story reopened. | Yes — Epic 0 is closed; only its retrospective ledger changes. |
| Epic 30 | None. Stories 30.1/30.3/30.4/30.5 already carry per-gate checkpoint tables from the 2026-07-27 split. The new gate codifies what they already satisfy. | Yes |
| Epic 31 | None. Story 31.1 is `in-progress` with its classification table; Story 31.2 is `ready-for-dev`. Neither is re-scoped. | Yes |
| Epic 27 | None. Story 27.3's C1 gate table already satisfies the one-row-per-gate rule. | Yes |
| All other epics | None. No scope, sequencing, or acceptance-criteria change. | Yes |

No epic is added, removed, resequenced, or reprioritized. No epic is invalidated.

### 2.2 Story impact

- **No completed or in-progress story is reopened, re-scoped, or rolled back.**
- Story 30.1, 30.3, 30.4, 30.5, 31.1, 31.2 and 27.3 are unaffected in content; they become the reference examples the gate is calibrated against.
- Future stories authored by **any** route must carry the classification and slice-proof record from the moment they are written, not from the moment they are selected.
- A sprint change proposal that splits a story must itself satisfy the guard for every story it creates — the direct CR16 lesson.

### 2.3 Artifact conflicts

- **PRD:** No conflict, no modification. `prd.md` defines product goals, MVP scope, and FR/NFR coverage; it contains no story-governance rule. Reviewed 2026-07-28.
- **Epics:** One governance sentence amended (`epics.md:555` binding point). No epic scope, story scope, acceptance criterion, or sequencing change.
- **Architecture:** No conflict and no modification. Decision **D17** already provides for "Memories-specific tenant evidence, tooling, web E2E, integration, deployment, benchmark, and recovery lanes [that] remain local and explicit" (`architecture.md:564`). The proposed gate is an instance of that existing decision, not a new one.
- **UX:** No conflict, no modification. No UI component, user flow, wireframe, interaction pattern, or accessibility surface is touched.
- **Other artifacts:** Team customization tomls, the shared policy, the durable process lesson, a new tooling gate, its fixture suite, CI wiring, and the Epic 0 action-item ledger require changes.

### 2.4 Technical and operational impact

- No C#, package, schema, API, persistence, deployment, container, or submodule change.
- No `Directory.Packages.props` change; the gate is stdlib Python 3, matching `tools/check-tenant-isolation-evidence.py` and `tools/check-story-file-scope.py`.
- CI gains one gate invocation in the existing `story-file-scope` job (which already materializes `.story-scope/changed-files.txt` and `.story-scope/commit-message.txt`) and one fixture-discovery step in `test-unit-contract`.
- All new enforcement lives under committed repository-owned paths and survives BMad skill refreshes.

## 3. Recommended Approach

**Direct Adjustment**, extending the 2026-07-16 update-safe route rather than replacing it.

### Rationale

- The policy text is approved and demonstrably effective on the routes that load it. The defect is which routes load it, when it binds, and whether anything can prove it ran.
- `_bmad/custom/{skill-name}.toml` is the supported override surface, and all four unguarded skills were verified on 2026-07-28 to expose `persistent_facts` and `activation_steps_append`, so Proposal A is implementable without touching generated skills.
- Moving the binding point from selection to authoring is the minimal change that would have prevented CR16: both anti-templates were compliant at `backlog` under the current wording.
- A deterministic gate converts "the guard should have run" into "the guard's record is present and well-formed", which is the only part of this policy a machine can honestly check.

### Alternatives considered

- **Close the action item as already-done: rejected.** CR16 is a documented recurrence after the second correction. Closing now would record a fix that the evidence contradicts.
- **Broaden the code-review layer only: rejected.** Review runs after implementation. Story 30.1's shape needed to be blocked at authoring, not audited afterwards.
- **Make the deterministic gate judge anti-template semantics: rejected.** Whether a reuse is genuinely narrow, or two outcomes genuinely independently deployable, is not machine-decidable. Overreaching would produce false failures and train the team to bypass the gate.
- **Edit `.agents/skills/**` again: rejected.** Generated files; overwritten on refresh. This is the failure mode the 2026-07-16 correction was written to cure.
- **Rollback of Stories 30.x/31.x: rejected.** The 2026-07-27 split already resolved them; nothing is left to revert.
- **PRD/MVP review: rejected.** Product direction, MVP scope, and FR/NFR coverage are untouched.

### Estimate and risk

- Effort: medium — approximately 1 to 1.5 working days including the gate, its fixtures, and CI wiring.
- Product risk: none; no product behavior changes.
- Process risk: medium until the gate is green and calibrated against the seven existing reference stories.
- Timeline impact: no product milestone change. Apply before the next story-authoring run on any route.

### Known limitation, stated up front

The deterministic gate checks that the **record** exists and is well-formed. It cannot decide whether a classification label is **correct**, whether a reused pattern is genuinely narrow, or whether two outcomes are genuinely independently deployable. Those judgments remain with the create-story and code-review LLM layers. This proposal deliberately does not claim otherwise, consistent with `spec-strengthen-story-creation-review-historical-slice-templates.md:82`.

## 4. Detailed Change Proposals

### Group 1 — Team customization (route coverage)

#### Proposal A: Load the policy on every story-authoring route

Artifacts: `_bmad/custom/bmad-correct-course.toml`, `_bmad/custom/bmad-create-epics-and-stories.toml`, `_bmad/custom/bmad-spec.toml`, `_bmad/custom/bmad-sprint-planning.toml`

OLD:

```text
All four files absent. Each workflow resolves only generated defaults and never
loads _bmad/custom/story-scope-guard.md.
```

NEW (shape shown for `bmad-correct-course.toml`; the other three follow the same pattern with a route-specific directive):

```toml
# Team-owned story-scope guard for the correct-course route. This file is merged
# over generated skill defaults and survives BMad skill refreshes.

[workflow]

persistent_facts = [
  "file:{project-root}/_bmad/custom/story-scope-guard.md",
  "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md",
]

activation_steps_append = [
  "HISTORICAL_SLICE_GUARD: This route authors and registers stories. Apply the team story-scope policy to every story this correction creates, renames, or splits, before writing it into epics.md or sprint-status.yaml at any status including backlog. A split must not reproduce the shape it was executed to cure: each created story must carry one independently demonstrable outcome, or an explicitly approved checkpoint table with one row per gate carrying owner, evidence command or artifact, review state, and completion state. Fail closed on the proposal before recording an approval that registers a story the policy forbids.",
]
```

Route-specific directives:

- `bmad-create-epics-and-stories` — apply the policy while decomposing requirements; a generated story list must not carry an anti-template shape into `epics.md`.
- `bmad-spec` — classify every prior story or spec influence before reuse; a spec covering multiple independently demonstrable outcomes must be split or must declare an approved checkpoint table.
- `bmad-sprint-planning` — do not promote a story into sprint status when its registered shape violates the policy; report it instead of transcribing it.

Rationale: the CR16 recurrence was authored by `bmad-correct-course`, which never loaded the policy. Route coverage is the direct cure.

### Group 2 — Shared policy and epics governance (binding point)

#### Proposal B: Bind the gate at authoring and registration, not at selection

Artifact: `_bmad/custom/story-scope-guard.md`
Section: `## Creation gate`

OLD:

```markdown
## Creation gate

- Select work from current epic intent and current code evidence, not numeric
  story adjacency.
- Do not copy an anti-template's tasks, AC density, file list, or proof shape.
- Split multiple independently demonstrable outcomes into newly numbered
  stories before setting `ready-for-dev`.
```

NEW:

```markdown
## Creation gate

This gate binds when a story is **authored or registered** — the moment it is
written into a story file, `epics.md`, or `sprint-status.yaml` — at any status,
including `backlog`. `ready-for-dev` is a second, stricter checkpoint, not the
first one. A story that violates this policy is in violation while it sits in
the backlog; it does not become compliant by not being selected yet.

- Select work from current epic intent and current code evidence, not numeric
  story adjacency.
- Do not copy an anti-template's tasks, AC density, file list, or proof shape.
- Split multiple independently demonstrable outcomes into newly numbered
  stories before the story is registered at any status.
- A correction, split, or replan that creates stories must satisfy this policy
  for every story it creates. A split must not reproduce the shape it was
  executed to cure.
```

Rationale: closes the exemption the 2026-07-27 proposal identified in writing. Both CR16 anti-templates were compliant under the old wording for as long as they stayed `backlog`.

#### Proposal C: Align the epics.md shape rule to the same binding moment

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: Engineering/Operational Readiness Track preamble, line 555

OLD:

```markdown
When checkpoint-heavy stories are selected for implementation, the story file
must either split checkpoints into separately tracked child story files or
include a checklist evidence table with owner, validation command or artifact,
review status, and completion date for each checkpoint.
```

NEW:

```markdown
When a checkpoint-heavy story is authored or registered — at any status,
including `backlog` — the story file must either split checkpoints into
separately tracked child story files or include a checklist evidence table with
owner, validation command or artifact, review status, and completion date for
each checkpoint. A story created by an approved sprint change is bound at the
moment that change registers it, not at the moment it is later selected.
```

The remainder of the paragraph — the shape-based scope, the "more than five independently verifiable gates" threshold, the one-row-per-gate requirement, and the known-instances list — is unchanged.

Rationale: `story-scope-guard.md` and `epics.md:555` must bind at the same moment, or the stricter of the two is bypassed by citing the looser.

### Group 3 — Deterministic verification

#### Proposal D: Add an executable story-slice-scope gate

Artifacts: `tools/check-story-slice-scope.py`, `tests/tooling/story_slice_scope/story_slice_scope_test.py`, `.github/workflows/ci.yml`

OLD:

```text
No deterministic check exists. The only automated coverage is
tests/tooling/bmad_customization/historical_slice_guard_test.py, which asserts
the guard is configured, not that its record was produced.
```

NEW — a fail-closed stdlib gate mirroring `tools/check-tenant-isolation-evidence.py`:

**Triggers** on changed files matching a story file
(`_bmad-output/implementation-artifacts/<n>-<n>-<slug>.md`),
`_bmad-output/planning-artifacts/epics.md`, or
`_bmad-output/implementation-artifacts/sprint-status.yaml`.

**Rules — all structural and countable, no semantic judgment:**

| Rule | Check | Failure |
| :--- | :---- | :------ |
| R1 | A changed story file that cites any prior story (`Story <n>.<n>` or a foreign story key) carries both a `Historical Context Classification` section and a `Slice Proof` section | Missing record |
| R2 | Every classification row carries exactly one of the three literal labels `current-narrow-pattern`, `historical-reference-only`, `anti-template` | Unlabeled or multi-labeled row |
| R3 | An `anti-template` row states a non-empty permitted use | Bare label used as a checkbox |
| R4 | If the story enumerates more than five independently verifiable gates (numbered ACs plus `**Then**` blocks plus `C<n>[.<n>]` checkpoint identifiers), a checkpoint table exists with one row per gate identifier, each row carrying four non-empty cells for owner, evidence command or artifact, review state, completion state | Missing table, shared row covering multiple gates, or empty cell |
| R5 | When the change registers a new story key in `sprint-status.yaml` or `epics.md`, R1-R4 apply to the corresponding story file regardless of its status value; `backlog` grants no exemption | Late binding |

**Escape hatch:** a `Story-Slice-Scope: not-applicable - <reason>` commit trailer, exactly mirroring the `Tenant-Isolation-Evidence` trailer contract.

**Non-goals, asserted in the tool docstring and its fixtures:** the gate does not judge whether a label is correct, whether a reuse is genuinely narrow, or whether outcomes are genuinely independently deployable. Those stay with the create-story and code-review layers.

**Calibration:** the fixture suite pins the gate against the seven stories that already satisfy the policy — 27.3, 30.1, 30.3, 30.4, 30.5, 31.1, 31.2 — as passing cases, and against the pre-split shapes recorded in `epics.md:5131` and `epics.md:5298` as failing cases. A frozen inline fixture guards against parser drift, following `tests/tooling/story_scope/story_scope_validator_test.py`.

**CI wiring:**

```yaml
      - name: Run story slice-scope gate
        run: python3 tools/check-story-slice-scope.py --commit-message-file "$COMMIT_MESSAGE_FILE" --changed-files-file "$CHANGED_FILES_FILE"
```

added to the existing `story-file-scope` job (which already produces both files), plus a discovery step in `test-unit-contract`:

```yaml
      - name: Run story slice-scope gate fixtures
        run: python3 -m unittest discover -s tests/tooling/story_slice_scope -p "*_test.py"
```

Rationale: gives this action item the same class of deterministic proof the sibling cross-tenant action item already has, and makes "did the guard run?" answerable from CI rather than from narrative.

#### Proposal E: Extend refresh-safety coverage to the new routes

Artifact: `tests/tooling/bmad_customization/historical_slice_guard_test.py`

OLD:

```text
Asserts resolved create-story and code-review configurations retain exactly one
policy fact, lesson fact, and historical-slice directive/layer after a refresh.
```

NEW:

```text
Additionally asserts that bmad-correct-course, bmad-create-epics-and-stories,
bmad-spec, and bmad-sprint-planning each resolve exactly one story-scope-guard
policy fact and exactly one HISTORICAL_SLICE_GUARD directive, and that each
retains its generated defaults. Existing create-story and code-review assertions
are unchanged.
```

Rationale: the four new overrides must be as refresh-safe as the two existing ones, or this correction decays the same way the 2026-07-06 one did.

### Group 4 — Durable process record

#### Proposal F: Record the route, binding, and determinism lessons

Artifact: `_bmad-output/process-notes/story-creation-lessons.md`
Section: `L09 - Historical Broad Slices Are Anti-Templates`

Append:

```markdown
- The policy binds at authoring and registration, at any status. A story does
  not become compliant by staying in the backlog.
- Every route that authors or registers stories must load the policy —
  create-story, correct-course, create-epics-and-stories, spec, and
  sprint-planning. A correction that splits a story is a story-authoring route
  and is bound by the policy for every story it creates.
- CR16 (2026-07-27) is the reference failure: the 2026-07-26 split produced two
  anti-templates because the splitting route did not load the policy and the
  gate did not bind at `backlog`.
- Configuration coverage is not execution proof. `tools/check-story-slice-scope.py`
  checks that the required record exists and is well formed; it does not judge
  classification correctness or outcome independence.
```

Rationale: keeps the durable lesson truthful about why a third correction was needed.

#### Proposal G: Give the Epic 0 action item an honest disposition

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`
Section: `action_items`, Epic 0 item 3 (line 515)

OLD:

```yaml
  - epic: 0
    action: "Strengthen story creation and review checks so historical broad slices are not reused as templates"
    owner: "Amelia, Murat"
    status: open
```

NEW:

```yaml
  - epic: 0
    action: "Strengthen story creation and review checks so historical broad slices are not reused as templates"
    owner: "Amelia, Murat"
    status: in-progress  # 2026-07-28: Guard extended to all story-authoring routes (correct-course, create-epics-and-stories, spec, sprint-planning), bound at authoring/registration rather than ready-for-dev, and backed by tools/check-story-slice-scope.py per sprint-change-proposal-2026-07-28-story-slice-guard-route-and-binding-coverage.md. Remains active: the retro success criterion is forward-looking, so this closes only when a subsequent analogous split is authored compliantly on the first pass with no follow-up correction.
```

Rationale: `open` understates two delivered corrections; `done` would contradict CR16 and a forward-looking success criterion. `in-progress` with a named closure condition matches how Epic 0 action item 4 (cross-tenant negative evidence) is carried.

## 5. Change Analysis Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger identified. No single triggering story: the trigger is a documented recurrence (`CR16`) after the second correction for this action item, plus the item's still-`open` state.
- [x] 1.2 Core problem defined. Category: **failed approach requiring different solution** — the policy is correct but under-bound in route, timing, and verifiability.
- [x] 1.3 Evidence collected and cited in section 1, including the exact CR16 language, the backlog non-binding statement, the `_bmad/custom/` coverage table, and the verified non-defect in 1.5.

### 2. Epic impact assessment

- [x] 2.1 Epic 0 is closed; only its retrospective ledger changes. Epics 27, 30, 31 complete as planned.
- [N/A] 2.2 No epic scope or acceptance-criteria change.
- [x] 2.3 Remaining epics reviewed. The guard must be shape-based and route-independent; no epic-specific carve-out is required.
- [N/A] 2.4 No epic invalidated; no new epic needed.
- [N/A] 2.5 No epic order or priority change.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed. No conflict, no modification; MVP scope unaffected.
- [x] 3.2 Architecture reviewed. No conflict, no modification. Decision D17 already covers module-specific local verification lanes (`architecture.md:564`); the new gate is an instance of it.
- [N/A] 3.3 UX reviewed. No UI, flow, wireframe, interaction, or accessibility impact.
- [x] 3.4 Other artifacts reviewed. Four new customization tomls, the shared policy, one `epics.md` governance sentence, a new tooling gate, two fixture suites, CI wiring, the durable lesson, and the action-item ledger require changes.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment — **viable**. Effort medium, product risk none, process risk medium until the gate is calibrated green.
- [N/A] 4.2 Rollback — **not viable and not needed**. The 2026-07-27 split already resolved both anti-templates; there is no bad product code to revert.
- [N/A] 4.3 PRD MVP review — **not required**. MVP scope, core goals, and FR/NFR coverage are untouched.
- [x] 4.4 Selected path: **Option 1, Direct Adjustment**, extending the 2026-07-16 update-safe customization route with binding-point and deterministic-gate coverage.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary written with recurrence evidence and a verified non-defect.
- [x] 5.2 Epic and artifact impacts documented per artifact.
- [x] 5.3 Recommended path and six rejected alternatives documented.
- [x] 5.4 MVP impact: none. Action plan, sequencing, and dependencies are explicit in section 6.
- [x] 5.5 Moderate-scope Product Owner / Developer handoff defined in section 6.

### 6. Final review and handoff

- [x] 6.1 All applicable checklist items addressed; no `[!]` action-needed item is left undocumented.
- [x] 6.2 Proposal checked against current repository evidence on 2026-07-28.
- [x] 6.3 Administrator explicitly approved the complete proposal for implementation on 2026-07-28.
- [x] 6.4 `sprint-status.yaml` change is limited to the Epic 0 action-item disposition (Proposal G). No epic or story entry is added, removed, renumbered, or re-statused by this proposal.
- [x] 6.5 Moderate-scope Product Owner / Developer handoff accepted on 2026-07-28. Implementation of Proposals A-G is not yet performed; it is the Developer agent's next action.

## 6. Implementation Handoff

Scope classification: **Moderate** — workflow governance, process-policy, and CI verification changes with no product behavior change.

Route: **Product Owner / Developer agent.**

### Responsibilities

Product Owner / workflow owner:

1. Confirm the binding-point change (authoring and registration, at any status) is the intended standard.
2. Confirm the `epics.md:555` amendment is a governance clarification, not an epic-scope change.
3. Confirm the action-item disposition and its named closure condition.

Developer agent:

1. Apply Proposals A-G without editing `.agents/skills/**`, `.claude/skills/**`, product code, PRD, architecture, UX, or submodule contents.
2. Resolve all six customized workflows and verify exactly one policy fact and one directive each, with all generated defaults retained.
3. Implement `tools/check-story-slice-scope.py` fail-closed, with the commit-trailer escape hatch and the stated non-goals in its docstring.
4. Calibrate the fixture suite against Stories 27.3, 30.1, 30.3, 30.4, 30.5, 31.1, 31.2 as passing cases and the two recorded pre-split shapes as failing cases.
5. Wire both CI steps and confirm the gate runs on a changed-files input, not as a vacuous no-op.
6. Run `git diff --check` and report exact resolved evidence.
7. Leave PRD, architecture, UX, product code, epic/story scope, and root submodule pointers unchanged.

### Implementation sequence

1. Proposal B and C — align the two binding points first, so the gate is written against the final rule.
2. Proposal A — add the four route overrides.
3. Proposal E — extend refresh-safety coverage to those routes.
4. Proposal D — implement the gate, its fixtures, and CI wiring.
5. Proposal F — update the durable lesson.
6. Proposal G — set the action-item disposition last, once the preceding evidence exists.

### Success criteria

- Every story-authoring route resolves `story-scope-guard.md` exactly once, and a BMad skill refresh cannot remove it.
- A bundled or checkpoint-heavy story cannot be registered at `backlog` without its classification, slice-proof, and per-gate checkpoint record.
- `tools/check-story-slice-scope.py` fails closed on the two recorded pre-split shapes and passes on all seven current reference stories.
- Both CI steps run and the gate is proven non-vacuous on a real changed-files input.
- A correction that splits a story is itself bound by the policy for every story it creates.
- No generated skill file, product code, PRD, architecture, UX, epic/story scope, or submodule is changed.

## 7. Approval Record

Approved by Administrator on 2026-07-28 and implemented in the same workflow run, as the 2026-07-16 correction for this trigger also was.

### Deviation from the approved text — Proposal A, `bmad-spec`

`_bmad/custom/bmad-spec.toml` adds **no** `persistent_facts` entry, unlike the other three route overrides. The cross-tenant project-context delivery contract in `tests/tooling/bmad_customization/bmad_customization_test.py` pins `bmad-spec`'s resolved `persistent_facts` to exactly the project-context bridge fact, so that the bridge's fail-closed control cannot be diluted. Adding the policy fact broke that gate.

The gate was not weakened. The policy reaches `bmad-spec` through its activation directive, which names both files to read. A dedicated test, `test_spec_route_receives_policy_without_diluting_context_bridge`, pins both halves of that arrangement so neither can silently regress.

### Deviation from the approved text — Proposal D, rule R4

R4 as proposed required "one row per gate identifier" for every gate the story enumerates. Calibration against live stories showed that rule produces false positives on compliant work, because nothing mechanical distinguishes a gate the story **owns** from a gate it merely **names**:

- Story 27.3 names `C1.1`-`C1.12` and `C1.14` repeatedly precisely because those gates were **transferred to Story 27.5** by the 2026-07-27 split. It would have failed on 15 gates it no longer owns.
- Story 27.2 refers to `C1`'s ratified mapping in task prose.
- Story 27.3's `| Checkpoint | Exact Python discovery command | Required case inventory |` table and Story 31.1's `| Decision | Resolution |` table both legitimately begin rows with a gate identifier while carrying no completion state.

R4 as implemented therefore checks two things it can decide: a story naming more than five gates must carry a checkpoint **evidence** table at all (the recorded pre-split shape was "no checkpoint table at all"), and every row of a table that promises owner/review/completion columns must be fully populated. Table identity is determined by header columns, not by row content. All three false-positive shapes are pinned as passing fixtures.

### Implementation evidence

- **Proposals A, B, C, D, E, F, G applied.** No generated skill file (`.agents/skills/**`, `.claude/skills/**`), product code, PRD, architecture, UX, or submodule content was changed. No epic or story scope was changed; the only `epics.md` edit is the governance sentence in Proposal C, and the only `sprint-status.yaml` edit is the Proposal G action-item disposition.
- **Route resolution.** All six customized routes resolve exactly one `HISTORICAL_SLICE_GUARD` directive under **both** `.claude/skills/**` and `.agents/skills/**`. Generated defaults survive the merge (`bmad-spec` retains `spec_template`, `spec_filename: SPEC.md`, and the bridge fact).
- **Gate calibration.** `tools/check-story-slice-scope.py` passes on all six live reference stories (27.1, 27.2, 27.3, 29.1, 31.1, 31.2) under `--require-record`, and fails on both reconstructed pre-split shapes, on an unclassified row, on a bare `anti-template` label, on a half-populated checkpoint row, and on a registered story with no record.
- **Two parser defects found and fixed during calibration.** A sentence-ending period defeated both the story-citation and checkpoint-identifier lookaheads (`Story 26.1.` and `C1.` were invisible to the gate). Both are pinned by fixtures.
- **Binding point proven.** The same story file passes when merely edited and fails when the change also touches `epics.md` or `sprint-status.yaml` — the registration binding that closes the `backlog` exemption.
- **Test results.** `bmad_customization` 26 passed, `story_slice_scope` 20 passed, `story_scope` 42 passed, `tenant_isolation_evidence` 34 passed, `line_endings` 4 passed — 126 total, 0 failures.
- **CI inventory guard.** `Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` passes 62/62, confirming the new `tests/tooling/story_slice_scope` suite is executed by a workflow step and is not an unwired guard. Command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`.
- **Whitespace.** `git diff --check` exits 0. New `.toml` and `.md` files are CRLF per `.gitattributes`; the new `.py` files and the `ci.yml` edit are LF.

### Not verified here

No full solution build or .NET suite beyond `CiTestInventoryTests` was run: this correction changes no C# source. The `references/*` gitlink drift and the concurrent edits to `31-1`, `epics.md`, `sprint-status.yaml`, `story-creation-lessons.md`, and three `_bmad/custom/*.toml` files present in the working tree originate from other sessions and were left untouched.

## 8. Workflow Execution Log

- 2026-07-28: Hexalith LLM baseline and canonical `_bmad-output/project-context.md` loaded; bridge precondition verified.
- 2026-07-28: Administrator confirmed the change trigger, selected **batch** mode, and scoped the correction to route coverage, binding point, and deterministic gate.
- 2026-07-28: PRD, epics, architecture, and UX artifacts reviewed; Change Navigation Checklist completed with no product-scope, epic-order, architecture, or UX change required.
- 2026-07-28: Dual-root skill resolution verified as a non-defect; hypothesis discarded rather than carried into the proposal.
- Scope classification: Moderate.
- Routed to: Product Owner / workflow owner and Developer agent.
- 2026-07-28: Administrator explicitly approved the complete Sprint Change Proposal for implementation.
- 2026-07-28: Proposals A-G implemented in the same run, with two recorded deviations (Proposal A `bmad-spec` fact delivery; Proposal D rule R4 narrowing). Both are documented in the Approval Record with the evidence that forced them.
- Handoff status: complete. Implementation applied and verified; 126 tooling tests and the CI inventory guard pass.
