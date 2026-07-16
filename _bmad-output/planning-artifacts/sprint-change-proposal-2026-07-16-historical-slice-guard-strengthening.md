---
change_trigger: "Strengthen story creation and review checks so historical broad slices are not reused as templates"
mode: batch
mode_basis: "Carried forward from the approved 2026-07-06 correction for the same trigger"
status: approved-implemented
project: Hexalith.Memories
date: 2026-07-16
scope_classification: moderate
---

# Sprint Change Proposal: Update-Safe Historical Slice Guard

Date: 2026-07-16
Project: Hexalith.Memories
Scope: Moderate process correction affecting story creation, story validation, code review, and customization-regression checks.

## 1. Issue Summary

The project already has the correct product-level rule: historical broad or bundled stories are context and evidence, not templates for future story shape. `epics.md` applies this explicitly to Stories 1.2, 1.5, 1.6, and 8.5, and it gives equivalent independently-verifiable checkpoint rules to later umbrella stories.

The July 6 correction attempted to enforce that rule by editing installed files under `.agents/skills/bmad-create-story` and `.agents/skills/bmad-code-review`. Those files are generated installation artifacts. Their `customize.toml` files explicitly say they are overwritten on every update. The repository has since refreshed the installed skills: the direct workflow, template, checklist, acceptance-auditor, and severity edits from the approved proposal are no longer present. Only the repository-owned lesson in `_bmad-output/process-notes/story-creation-lessons.md` survived, and neither affected workflow loads that lesson by default.

The current creation path therefore still:

- loads the numerically previous story automatically;
- asks for all learnings and established patterns without first classifying whether the story is an anti-template;
- uses a generated template with no historical-context classification section;
- validates previous-story intelligence without an explicit historical-slice fail-closed gate; and
- can set the story to `ready-for-dev` and update sprint status without proving the guard ran.

The current full code-review path still has only a generic Acceptance Auditor. It does not have an independent layer that reads the repository's historical-slice policy and audits both the story specification and implementation diff against it.

### Trigger evidence

- `_bmad-output/planning-artifacts/epics.md` — Epic 1 Implementation Readiness Amendment and Historical Scope Guards for Stories 1.2, 1.5, and 1.6; equivalent guard for Story 8.5; checkpoint evidence rules for later umbrella stories.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06-historical-slice-story-guard.md` — approved policy and direct-edit implementation record.
- `_bmad-output/process-notes/story-creation-lessons.md` — surviving L09 anti-template lesson.
- `.agents/skills/bmad-create-story/SKILL.md` — current previous-story analysis contains no classification or exclusion gate.
- `.agents/skills/bmad-create-story/template.md` — current generated template contains no Historical Context Classification section.
- `.agents/skills/bmad-create-story/checklist.md` — current validation checklist contains no historical broad-slice Critical Miss rule.
- `.agents/skills/bmad-code-review/customize.toml` — generated file says it is overwritten on every update and exposes team overrides through `_bmad/custom/bmad-code-review.toml`.
- `.agents/skills/bmad-code-review/steps/step-02-review.md` — current review executes resolved layers but the default layer set has no historical-slice guard.
- `_bmad/scripts/resolve_customization.py` — repository-supported, update-safe merge point for team customizations.

## 2. Impact Analysis

### Epic impact

- No product epic needs new scope, replacement, or resequencing.
- Epic 1 remains the authoritative source for the original anti-template rule.
- Story 8.5 and checkpoint-heavy operational stories confirm the rule must be semantic, not a fixed denylist of three story IDs.
- Epics 20-26 remain valid. Their audit-anchor and cross-tenant evidence carry-forward rules are complementary: they preserve current evidence without copying historical scope shape.

### Story impact

- No completed implementation story is reopened.
- Future story creation must classify every previous or historical story reference before transferring any task shape, acceptance criteria, file breadth, or evidence pattern.
- Historical broad, bundled, umbrella, superseded, alias-only, reserved, or explicitly guarded stories must default to `historical-reference-only` or `anti-template`.
- A future story may reuse a narrow code or test pattern from current source, but it may not cite a historical story as a whole-story shape template.
- Any story with multiple independently demonstrable outcomes must be split before `ready-for-dev`, unless current epics explicitly authorize an umbrella/checkpoint story and require independently verifiable checkpoint evidence.

### Artifact conflicts

- **PRD:** No conflict and no modification. Product goals, MVP scope, and FR/NFR coverage are unchanged.
- **Epics:** No content modification required. Existing scope and evidence guards are sufficient.
- **Architecture:** No product architecture modification. This correction uses the supported customization architecture instead of generated installation files.
- **UX:** No UI or user-flow change.
- **Other artifacts:** Team customization, process lessons, workflow-regression tests, and CI wiring require updates.

### Technical and operational impact

- No product C#, package, schema, API, persistence, deployment, or submodule change.
- New committed files under `_bmad/custom/` survive BMad skill refreshes.
- A focused Python fixture proves the resolved create-story and code-review configurations still contain the guard after updates.
- The existing installed `.agents/skills/**` files remain untouched.

## 3. Recommended Approach

Use **Direct Adjustment with process/backlog coordination**, implemented only through repository-owned customization and verification artifacts.

### Rationale

- The policy is already approved; the failed part was the installation location.
- `_bmad/custom/{skill-name}.toml` is the explicit team override surface and structurally merges with refreshed skill defaults.
- A shared policy file prevents the creation and review workflows from drifting into different definitions.
- A dedicated review layer provides independent detection instead of relying on a generic acceptance prompt to infer the rule.
- A resolver-level regression fixture catches the exact failure mode that invalidated the July 6 implementation.

### Alternatives considered

- **Repeat direct edits to `.agents/skills/**`: rejected.** The files are overwritten on updates; this repeats the failed approach.
- **Rollback historical broad stories: rejected.** The completed work is not the defect and remains useful trace evidence.
- **PRD/MVP review: rejected.** Product direction and scope are unchanged.
- **Fixed story-ID denylist only: rejected.** It would miss Story 8.5 and future broad/umbrella slices.

### Estimate and risk

- Effort: low to medium, approximately 0.5-1 working day including fixtures.
- Product risk: low; no product behavior changes.
- Process risk: medium until the resolved-config fixture is green.
- Timeline impact: no product milestone change; apply before the next create-story or full code-review run.

## 4. Detailed Change Proposals

### Proposal A: Make the durable lesson name the update-safe enforcement point

Artifact: `_bmad-output/process-notes/story-creation-lessons.md`
Section: `L09 - Historical Broad Slices Are Anti-Templates`

OLD:

```markdown
- Story creation must classify previous-story context as reusable pattern,
  historical reference only, or anti-template before carrying lessons forward.
- Review must flag a story or implementation that reuses a historical broad
  slice as a template, hides broad scope behind one story, or accepts internal
  classes/unit tests as sufficient proof where observable API/CLI/contract,
  trace, or integration evidence is required.
```

NEW:

```markdown
- Story creation must classify every previous or historical story reference as
  current narrow pattern, historical reference only, or anti-template before
  carrying anything forward. Numeric adjacency is not evidence of relevance.
- Reusable implementation details must be re-verified against current source;
  a historical story's task structure, acceptance-criteria breadth, file list,
  or completion evidence is never reusable by default.
- Review must flag a story or implementation that reuses a historical broad
  slice as a template, hides independently demonstrable outcomes behind one
  story, or accepts internal-only proof where observable evidence is required.
- Enforce this rule through `_bmad/custom/story-scope-guard.md` and committed
  `_bmad/custom/bmad-{create-story,code-review}.toml` overrides. Do not enforce
  it by editing generated `.agents/skills/**` files, which updates overwrite.
- After any BMad skill refresh, run the customization-resolution fixture before
  the next story creation or review.
```

Rationale: records both the policy and the reason the previous implementation disappeared.

### Proposal B: Add one team-owned story-scope policy

Artifact: `_bmad/custom/story-scope-guard.md`
Section: new file

OLD:

```text
File absent.
```

NEW:

```markdown
# Historical Slice Story Scope Guard

## Authority

Current PRD, epics, architecture, approved sprint changes, current source, and
current tests define present scope. Historical story artifacts are evidence of
what happened; they do not define the shape of new work.

## Mandatory classification

Before reusing any previous or historical story reference, classify it as:

1. `current-narrow-pattern` — only a focused implementation/test pattern that
   has been re-verified against current source; whole-story shape is not reused.
2. `historical-reference-only` — dependency, decision, or evidence context.
3. `anti-template` — broad, bundled, umbrella, checkpoint-heavy, superseded,
   alias-only, reserved, or explicitly guarded scope that must not shape a new
   story.

Any artifact containing `Historical Scope Guard`, `historical broad`,
`bundled infrastructure`, `not valid patterns for future story creation`,
`must split`, `do not reopen`, or equivalent language is an anti-template
unless current epics explicitly approve a narrower use.

## Creation gate

- Select work from current epic intent and current code evidence, not numeric
  story adjacency.
- Do not copy an anti-template's tasks, AC density, file list, or proof shape.
- Split multiple independently demonstrable outcomes into newly numbered
  stories before setting `ready-for-dev`.
- An explicitly approved umbrella/checkpoint story may remain one tracking
  story only when every checkpoint has its own owner, evidence command/artifact,
  review state, and completion state.
- Add `Historical Context Classification` and `Slice Proof` sections to the
  generated story whenever any prior story influences it.
- Treat an unresolved violation as a Critical Miss: do not set
  `ready-for-dev` and do not update sprint status.

## Review gate

- In full review, inspect both the story specification and implementation diff.
- Confirm the implementation stays within one approved slice or independently
  proves every explicitly approved checkpoint.
- Confirm externally observable proof is present wherever current artifacts
  require API, CLI, contract, trace, integration, or downstream-consumer proof.
- Rate confirmed anti-template reuse or hidden multi-slice scope as `high`.
- Route to `decision_needed` when the correct split requires a human scope
  choice; otherwise route an unambiguous correction to `patch`.
- Never dismiss a confirmed violation as editorial or historical noise.
```

Rationale: gives creation, validation, and review one update-safe policy with semantic detection and a documented exception for explicitly approved checkpoint stories.

### Proposal C: Enforce the gate in resolved create-story customization

Artifact: `_bmad/custom/bmad-create-story.toml`
Section: new team override

OLD:

```text
File absent. The workflow resolves only generated defaults.
```

NEW:

```toml
[workflow]

persistent_facts = [
  "file:{project-root}/_bmad/custom/story-scope-guard.md",
  "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md",
]

activation_steps_append = [
  "HISTORICAL_SLICE_GUARD: Apply the team story-scope policy during artifact analysis and final validation. Classify every previous/historical story before reuse; ignore numeric adjacency as a relevance signal; include Historical Context Classification and Slice Proof when prior stories influence the draft; fail closed without ready-for-dev or sprint-status mutation until every anti-template or required-split violation is resolved.",
]
```

Rationale: overrides append to refreshed defaults and make the rule foundational throughout the run, including the final validation step.

### Proposal D: Add an independent full-review historical-slice layer

Artifact: `_bmad/custom/bmad-code-review.toml`
Section: new team override

OLD:

```text
File absent. Full review has only the default generic Acceptance Auditor.
```

NEW:

```toml
[workflow]

persistent_facts = [
  "file:{project-root}/_bmad/custom/story-scope-guard.md",
  "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md",
]

activation_steps_append = [
  "HISTORICAL_SLICE_GUARD: During triage, apply the policy's fail-closed severity and routing rules to confirmed historical-template reuse or hidden multi-slice scope.",
]

[[workflow.review_layers]]
id = "historical-slice-guard"
name = "Historical Slice Guard"
when = 'Only when {review_mode} = "full".'
instruction = """
Launch a subagent with no prior conversation context, with this prompt:

> Read `_bmad/custom/story-scope-guard.md`, `{spec_file}`, and the provided
> diff. Audit whether the story or implementation copied task shape, AC breadth,
> file breadth, or completion evidence from a historical anti-template; hides
> independently demonstrable slices in one story; omits the required historical
> classification/slice-proof record; or closes on internal-only proof where the
> current artifacts require observable API/CLI/contract/trace/integration or
> downstream-consumer evidence. Distinguish narrow current-source pattern reuse
> from whole-story template reuse. Output only actionable Markdown findings.
> Each finding must cite the violated policy rule, the spec location, the diff
> evidence, and whether correction is an unambiguous patch or requires a human
> split decision.
>
> Diff:
> {diff_output}
"""
```

Rationale: makes the failure mode an independently executed review concern. Existing triage deduplication handles overlap with the Acceptance Auditor.

### Proposal E: Add customization-resolution regression coverage

Artifacts:

- `tests/tooling/bmad_customization/bmad_customization_test.py`
- `.github/workflows/ci.yml`

OLD:

```text
No fixture proves that a BMad skill refresh preserves repository story-scope guards.
```

NEW:

```text
Add a stdlib unittest fixture that runs resolve_customization.py for
bmad-create-story and bmad-code-review and asserts:

- both resolved persistent_facts arrays include story-scope-guard.md;
- create-story has exactly one HISTORICAL_SLICE_GUARD activation directive;
- code-review has exactly one historical-slice-guard review layer;
- that layer is full-review-only and references {spec_file}, {diff_output}, and
  the team policy file;
- generated default review layers remain present after the keyed merge.

Run the fixture in the existing test-unit-contract tooling section.
```

Rationale: catches the precise regression that removed the July 6 enforcement and proves the team override merges without deleting default layers.

### Proposal F: Preserve and annotate the superseded implementation route

Artifact: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06-historical-slice-story-guard.md`
Section: append a supersession note; do not rewrite the historical approval record

OLD:

```markdown
Implementation status: approved by Jerome and applied on 2026-07-06.
```

NEW:

```markdown
## Supersession Note (2026-07-16)

The policy remains approved, but the direct edits under `.agents/skills/**`
were installation-scoped and were overwritten by a later BMad refresh. The
update-safe enforcement route is superseded by
`sprint-change-proposal-2026-07-16-historical-slice-guard-strengthening.md`:
committed `_bmad/custom/**` overrides plus resolver-level regression coverage.
```

Rationale: preserves truthful history while preventing future readers from assuming the vanished direct edits are still active.

## 5. Change Analysis Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger identified: recurrence of the July 6 process defect after installed skill files were refreshed; no product story triggered the change.
- [x] 1.2 Core problem defined: failed enforcement approach plus process-control gap. The policy is correct, but it was implemented in overwrite-prone artifacts.
- [x] 1.3 Evidence collected from current epics, prior proposal, surviving lesson, current installed skills, customization resolver, and absent team overrides.

### 2. Epic impact assessment

- [x] 2.1 Current epics remain completable as planned.
- [N/A] 2.2 No epic scope or acceptance-criteria change is required.
- [x] 2.3 Remaining epics reviewed; the semantic guard must cover Story 8.5 and approved checkpoint stories, not only Stories 1.2/1.5/1.6.
- [N/A] 2.4 No epic is invalidated and no new product epic is needed.
- [N/A] 2.5 No epic priority or execution-order change is needed.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; no conflict or modification.
- [x] 3.2 Architecture reviewed; no product conflict. The supported customization resolver defines the correct process architecture.
- [N/A] 3.3 UX reviewed; no UI/UX effect.
- [x] 3.4 Other artifacts reviewed; team overrides, process lesson, regression fixture, CI wiring, and prior proposal annotation require changes.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable: low-medium effort, low product risk, medium process risk until regression proof passes.
- [N/A] 4.2 Rollback is not viable; completed historical stories are valid evidence and no bad product code is being reverted.
- [N/A] 4.3 PRD/MVP review is unnecessary.
- [x] 4.4 Recommended path selected: update-safe direct adjustment through team customization plus regression coverage.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed with recurrence evidence.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and rejected alternatives documented.
- [x] 5.4 MVP impact documented as none; implementation actions and sequencing are explicit.
- [x] 5.5 Moderate-scope Product Owner/Developer handoff defined below.

### 6. Final review and handoff

- [x] 6.1 Applicable checklist items reviewed; pending items are explicit.
- [x] 6.2 Proposal checked for consistency with current repository evidence.
- [x] 6.3 Administrator explicitly approved the proposal on 2026-07-16.
- [N/A] 6.4 `sprint-status.yaml` needs no change because no epic/story entry is added, removed, or renumbered.
- [x] 6.5 Product Owner/Developer handoff was accepted and implemented in the approval workflow.

## 6. Implementation Handoff

Scope classification: **Moderate** — repository workflow/backlog governance and CI fixture changes, with no product behavior change.

Route: **Product Owner / Developer agent**. Administrator approved the route,
and the Developer agent applied Proposals A-F in the same workflow.

### Responsibilities

Product Owner / workflow owner:

1. Confirm the semantic classification and explicitly approved umbrella/checkpoint exception.
2. Confirm the guard is required before the next story creation or full review.
3. Preserve the current epic/story structure and avoid reopening completed historical stories.

Developer agent:

1. Apply Proposals A-F without editing `.agents/skills/**` or submodule contents.
2. Validate both TOML files through `_bmad/scripts/resolve_customization.py`.
3. Add and run the focused Python fixture.
4. Run `git diff --check` and report exact resolved layer/fact evidence.
5. Leave `sprint-status.yaml`, PRD, epics, architecture, UX, and product code unchanged.

### Implementation sequence

1. Add the shared policy and team overrides.
2. Resolve both configurations and inspect the merged JSON.
3. Update the durable lesson and supersession note.
4. Add the regression fixture and CI invocation.
5. Run focused verification.
6. Perform a dry-run create-story validation against one anti-template fixture and a full-review layer dry run before handoff.

### Success criteria

- A BMad skill refresh cannot remove the guard because enforcement lives under committed `_bmad/custom/**`.
- Resolved create-story configuration loads the shared policy and contains the fail-closed activation directive.
- Future generated stories influenced by historical work contain Historical Context Classification and Slice Proof sections.
- A historical anti-template cannot reach `ready-for-dev` until it is split or an explicitly approved checkpoint exception is evidenced.
- Full code review runs an independent Historical Slice Guard layer against both spec and diff.
- Confirmed anti-template reuse is treated as high severity and never dismissed as editorial noise.
- The customization-resolution fixture passes and remains in CI.
- No generated skill file, product code, product planning scope, sprint status, or submodule is changed.

## 7. Approval Record

Approved by Administrator on 2026-07-16 and implemented in the same workflow.

Implementation evidence:

- Added the shared policy and both team overrides under `_bmad/custom/`;
  generated `.agents/skills/**` files and submodules remain untouched.
- Resolved both workflows successfully. The create-story facts and activation
  directive are present; code-review retains all four default layers and adds
  exactly one full-only `historical-slice-guard` layer.
- Added two resolver regression tests to CI; both pass.
- Create-story dry run classified Story 1.2 as `anti-template` from its
  `Historical Scope Guard` and `Do not reopen` signals, with `ready-for-dev`
  and sprint-status transitions blocked pending a split.
- Full-review dry run rendered the new layer with both story-spec and diff
  evidence while retaining the full-mode gate.
- Existing coverage-gate, NuGet-publish, container-publish, and production
  deployment evidence fixture suites pass. The release-package suite has one
  unrelated baseline failure: the root-declared Builds submodule currently
  supplies `HexalithPolymorphicSerializationsVersion` as `v1.16.3`, which NuGet
  rejects as an invalid version string in its Redis compatibility consumer.
- PRD, epics, architecture, UX, product code, sprint status, and root submodule
  pointers are unchanged.

## 8. Workflow Execution Log

- 2026-07-16: Administrator confirmed the change trigger and selected batch
  review mode.
- 2026-07-16: PRD, epics/stories, architecture, and UX artifacts were fully
  reviewed; the Change Navigation Checklist completed with no product-scope,
  epic-order, architecture, UX, or sprint-status change required.
- 2026-07-16: Administrator explicitly approved the complete Sprint Change
  Proposal for implementation.
- Scope classification: Moderate.
- Routed to: Product Owner / workflow owner and Developer agent.
- Handoff status: complete. Proposals A-F are present in the working tree; no
  generated `.agents/skills/**` file or submodule content was changed by this
  correction.
- Focused verification command: `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"`.
  It passed 2 tests; both resolved workflow configurations contain the
  historical-slice guard and all four default review layers remain. The Git
  whitespace check also passed.
