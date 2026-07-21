---
title: 'Review-found workflow/runtime defect remediation checklist'
type: 'feature'
created: '2026-07-21'
status: 'done'
baseline_commit: 'ae591ce7a0f1f6aca54ccdaea303eb63980dfa25'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-21-retro-2026-07-05.md'
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
  - '{project-root}/_bmad/custom/story-scope-guard.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 21 retro Action Item #5 (owner Amelia, still `open`) requires the workflow/runtime defect classes that Epic 21 reviews caught — missing Dapr activity registration, unobserved child workflows, owner-check race gaps, rollback-marker overwrite, staging-index cleanup gaps — to become durable checklist items on future remediation stories. No loaded, enforced checklist exists; the categories live only inside the closed retrospective, so nothing carries them into Epic 27+ story creation, development, or review.

**Approach:** Add one update-safe team-owned checklist policy under `_bmad/custom/`, inject it through the committed create-story, dev-story, and code-review customizations exactly as `story-scope-guard.md` and `story-phase-ledger.md` are injected, extend the customization-resolution fixture, record a durable process lesson, and mark the action item done with evidence.

## Boundaries & Constraints

**Always:** Use committed `_bmad/custom` overrides; self-scope by surface touched (Dapr workflow/child-workflow/activity registration, cleanup/compensation/dedup of shared or tenant-scoped state, migration/rollback markers) so a story touching none records an explicit not-applicable note; reference `story-phase-ledger.md` for File List reconciliation rather than duplicating it; preserve every existing persistent_fact, activation directive, and review layer exactly once; fail closed before `ready-for-dev`, `review`, or `done` when an applicable category lacks a checklist item, passing evidence, or an accepted blocker.

**Ask First:** Adding a code-review subagent review layer (versus an activation directive only); rewriting the closed Epic 21 retrospective; any change to the resolver, generated `.agents`/`.claude` skill source, or canonical marker tokens.

**Never:** Edit generated `.agents/skills/**` or `.claude/skills/**` files, product/C# code, PRD, epics, architecture, UX, or unrelated dirty-worktree files; duplicate the phase-ledger File List logic; weaken any existing guard.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Workflow-touching story creation | New remediation story adds/changes a workflow, child workflow, activity registration, cleanup/dedup, or migration/rollback marker | Story acceptance/tasks carry one explicit item per applicable category with adversarial/negative coverage | ready-for-dev / sprint-status mutation blocked until items or a not-applicable note exist |
| Non-touching story | Story changes none of the trigger surfaces | Story records `remediation runtime checklist: not applicable — no workflow/runtime surface touched` | No item fabricated |
| Review finds a new runtime/rollback defect | Full review confirms a dispatch, cleanup, or rollback defect | Defect becomes a checklist item and is routed: unambiguous omission → `patch`, ambiguous scope → `decision_needed` | `done` blocked while an applicable category is unproven |
| Customization resolution | Resolver merges create-story / dev-story / code-review | Each resolves the checklist fact plus exactly one `REMEDIATION_RUNTIME_CHECKLIST:` directive; existing facts, directives, and layers unchanged | Fixture fails if injection is missing, duplicated, or clobbers an existing guard |

</frozen-after-approval>

## Code Map

- `_bmad/custom/remediation-runtime-checklist.md` -- NEW policy: Authority, Applicability, five categories tied to their Epic 21 defects, and creation/review fail-closed gates.
- `_bmad/custom/bmad-create-story.toml` -- load the fact + one creation directive; keep historical-slice and phase-ledger directives intact.
- `_bmad/custom/bmad-dev-story.toml` -- load the fact + one development directive; keep the phase-ledger directive intact.
- `_bmad/custom/bmad-code-review.toml` -- load the fact + one review directive; keep both existing review layers and both existing directives intact.
- `tests/tooling/bmad_customization/bmad_customization_test.py` -- add resolution + policy-content assertions; prove existing guards are preserved.
- `_bmad-output/process-notes/story-creation-lessons.md` -- add lesson L11 (CRLF file — normalize after edit).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- flip Epic 21 action item #5 `open` → `done` with dated evidence.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/custom/remediation-runtime-checklist.md` -- define Authority, Applicability (self-scoping trigger + not-applicable escape), the five categories (Dapr activity registration; observed/awaited child workflows; owner-checked cleanup/compensation/dedup; rollback-marker & staging-artifact preservation; File List reconciliation via cross-reference to `story-phase-ledger.md`) each tied to its Epic 21 review defect, and Creation/Review fail-closed gates.
- [x] `_bmad/custom/bmad-create-story.toml` -- append the checklist fact to `persistent_facts` and one `REMEDIATION_RUNTIME_CHECKLIST:` creation directive to `activation_steps_append`.
- [x] `_bmad/custom/bmad-dev-story.toml` -- append the fact and one development directive.
- [x] `_bmad/custom/bmad-code-review.toml` -- append the fact and one review directive.
- [x] `tests/tooling/bmad_customization/bmad_customization_test.py` -- add tests: each lifecycle workflow resolves the fact and exactly one checklist directive; the single HISTORICAL_SLICE_GUARD/STORY_PHASE_LEDGER directives and all existing review layers remain present exactly once; the policy file defines the five categories and both gates.
- [x] `_bmad-output/process-notes/story-creation-lessons.md` -- add L11 pointing at the checklist and its update-safe wiring; normalize the file to CRLF.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- set the Epic 21 "Add review-found workflow/runtime defects…" action item to `done` with a 2026-07-21 evidence comment naming the artifacts.

**Acceptance Criteria:**
- Given create-story, dev-story, and code-review, when customization is resolved, then each contains the checklist fact and exactly one `REMEDIATION_RUNTIME_CHECKLIST:` directive.
- Given the merged create-story and code-review workflows, when resolved, then every prior persistent_fact, the single HISTORICAL_SLICE_GUARD and STORY_PHASE_LEDGER directives, and all existing review layers remain present exactly once.
- Given the checklist policy, when inspected, then it names the five categories, the self-scoping applicability rule with a not-applicable escape, and fail-closed creation/review gates, and references the phase-ledger for File List reconciliation.
- Given the fixture command, when it runs, then all prior tests and the new tests pass.
- Given a path-scoped `git status --porcelain` over this change's own artifacts, when inspected, then only the new policy, the three tomls, the fixture test, the lesson file, `sprint-status.yaml`, and this spec appear as added/modified (files left dirty by concurrent work are out of scope), and the Epic 21 action item shows `done` with dated evidence.

## Spec Change Log

- 2026-07-21 (step-04 review, patch-only — no loopback): Three adversarial layers (blind-hunter, edge-case-hunter, verification-gap) confirmed the wiring resolves and clobbers no existing guard, and raised policy/test/spec-record refinements. Applied as patches: broadened the create-story trigger surfaces to match the policy (blue/green cutover, abort, staging keys/indexes/aliases); gave Category 5 (File List reconciliation) an explicit carve-out from the per-category item/test rule (satisfied by the phase ledger); de-bundled Category 3 sub-defects; added independent applicability re-derivation at dev and review; defined "touch" as behavioral; hardened the fixture with per-skill directive-body and both-surface (`.agents`/`.claude`) assertions plus obligation-sentence checks. **Corrected this Verification block:** `git diff --name-only` cannot see untracked files (the new policy and this spec) and would also surface unrelated concurrent dirt, so the scope check now uses a path-scoped `git status --porcelain`. KEEP: the resolver-fixture-as-verification approach and the self-scoping-by-touched-surface design.

## Verification

**Commands:**
- `python3 -m unittest tests.tooling.bmad_customization.bmad_customization_test -v` -- expected: all 14 tests pass (prior 11 plus 3 checklist tests).
- `uv run _bmad/scripts/resolve_customization.py --skill "$(pwd)/.claude/skills/bmad-create-story" --key workflow.persistent_facts` -- expected: output includes `file:{project-root}/_bmad/custom/remediation-runtime-checklist.md`.
- `git status --porcelain -- _bmad/custom/remediation-runtime-checklist.md _bmad/custom/bmad-create-story.toml _bmad/custom/bmad-dev-story.toml _bmad/custom/bmad-code-review.toml tests/tooling/bmad_customization/bmad_customization_test.py _bmad-output/process-notes/story-creation-lessons.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/spec-remediation-runtime-defect-checklist.md` -- expected: exactly these 8 entries (the new policy and this spec as `??`, the rest as ` M`); no other path.

## Suggested Review Order

**The contract (start here)**

- The whole checklist: five categories tied to their Epic 21 defects, plus the fail-closed creation/review gates.
  [`remediation-runtime-checklist.md:43`](../../_bmad/custom/remediation-runtime-checklist.md#L43)

**Enforcement wiring — loaded into every story lifecycle**

- Creation gate: classify applicability, add a per-category item or the not-applicable note, fail closed before `ready-for-dev`.
  [`bmad-create-story.toml:16`](../../_bmad/custom/bmad-create-story.toml#L16)

- Review gate: independently re-derive applicability, reject a contradicted not-applicable note, block `done` on an unproven category.
  [`bmad-code-review.toml:16`](../../_bmad/custom/bmad-code-review.toml#L16)

- Development gate mirrors the two above (re-derive applicability from the dev diff, fail closed before review).
  [`bmad-dev-story.toml`](../../_bmad/custom/bmad-dev-story.toml)

**Verification**

- Resolver fixture: per-skill directive-body assertions across both `.agents`/`.claude` surfaces; proves guards are not clobbered.
  [`bmad_customization_test.py:675`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L675)

**Trackers (peripherals)**

- Durable lesson L11 pointing at the checklist and its update-safe wiring.
  [`story-creation-lessons.md:52`](../process-notes/story-creation-lessons.md#L52)

- Epic 21 action item flipped to `done` with dated evidence.
  [`sprint-status.yaml:564`](sprint-status.yaml#L564)
