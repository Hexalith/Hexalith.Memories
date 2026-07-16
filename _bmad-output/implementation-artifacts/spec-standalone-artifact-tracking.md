---
title: 'Standalone Artifact Tracking'
type: 'refactor'
created: '2026-07-16'
status: 'done'
outcome: 'superseded-unimplemented'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-standalone-artifact-tracking.md'
---

> **Superseded on 2026-07-16.** No tasks in this spec were implemented. The
> approved one-shot self-tracking convention is authoritative, and this draft
> is retained only as rejected-alternative decision history.

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Registered stories have authoritative sprint rows, but bounded artifacts outside that registry have no durable lifecycle lane. Existing one-shot files are therefore ambiguous, and repository workflows can omit, misclassify, or accidentally count them toward epic completion.

**Approach:** Define a mutually exclusive registered-companion versus standalone policy, add an authoritative standalone register, teach spec creation and review to classify and synchronize the correct lane, and reconcile every currently discovered `route: one-shot` artifact to that policy.

## Boundaries & Constraints

**Always:** Preserve unrelated dirty-tree edits; resolve registered stories against both `epics.md` and `development_status`; keep standalone IDs stable and frontmatter synchronized with exactly one register entry; retain the two retrospective actions for audit history; preserve existing customization facts, directives, and review layers; classify the discovered 26.2 and 26.6 artifacts as registered-story companions.

**Ask First:** Any change to a registered story row or epic lifecycle; any additional ambiguous `route: one-shot` artifact discovered after the known four; any work that requires product scope, new acceptance criteria, multi-phase standalone execution, or an epic-completion dependency and therefore needs promotion to a registered story.

**Never:** Create Story 19.5 or another invented `development_status` row; let standalone state affect readiness, execution order, or epic completion; modify generated `.agents/skills/**` or `.claude/skills/**` files; alter production code, dependencies, submodules, PRD, architecture, or UX; discard pre-existing working-tree changes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Registered companion | Numeric story exists in both planning and sprint registries | Artifact declares `story: Epic.Story`, inherits its lifecycle, and has no standalone entry | Halt on missing or conflicting registry evidence |
| Standalone artifact | No governing story and work stays within the approved boundary | Non-story filename plus required metadata and one matching standalone row | Fail close on missing, duplicate, or mismatched metadata |
| Historical exception | `19-5-ci-submodule-metadata-cleanup.md` | Remains standalone under its approved stable ID | Permit only this named unregistered numeric exception |
| Boundary crossing | Proposed standalone work gains story-scale scope or epic dependency | Implementation stops for story promotion | Do not mutate either lifecycle lane |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md` -- Story Key Policy and standalone boundary; contains overlapping uncommitted policy text to reconcile.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- registered story state, new sibling standalone register, and two retrospective resolutions.
- `_bmad/custom/standalone-artifact-tracking.md` -- shared update-safe classification and synchronization contract.
- `_bmad/custom/bmad-spec.toml` -- new team override for creation-time classification.
- `_bmad/custom/bmad-code-review.toml` -- existing review customization that must retain all current layers.
- `_bmad-output/implementation-artifacts/{19-5-ci-submodule-metadata-cleanup.md,spec-clarify-epic-26-closure-status.md}` -- approved standalone metadata backfill targets.
- `_bmad-output/implementation-artifacts/{26-2-restore-target-busy-catalog.md,26-6-production-rollout-recovery-hardening.md}` -- discovered registered-story companions currently mislabeled one-shot.
- `tests/tooling/bmad_customization/bmad_customization_test.py` -- resolver, policy, tracker, artifact, and YAML invariants.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/planning-artifacts/epics.md` -- replace the conflicting older non-story text with the approved registered-companion/standalone boundary and sole `19-5` filename exception.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- add the sibling `standalone_artifacts` mapping, reconcile header guidance, and update only the two target action comments while preserving all registered rows and unrelated edits.
- [ ] `_bmad/custom/standalone-artifact-tracking.md`, `_bmad/custom/bmad-spec.toml`, `_bmad/custom/bmad-code-review.toml` -- encode one shared policy and additive creation/review directives through repository-owned overrides.
- [ ] `_bmad-output/implementation-artifacts/{19-5-ci-submodule-metadata-cleanup.md,spec-clarify-epic-26-closure-status.md,26-2-restore-target-busy-catalog.md,26-6-production-rollout-recovery-hardening.md}` -- backfill the two standalone records and reclassify the two registered companions without changing their completed evidence.
- [ ] `tests/tooling/bmad_customization/bmad_customization_test.py` -- parse YAML and verify both installed resolver surfaces, exact lane membership, metadata/status agreement, action retention, historical exception, and preservation of existing customization contracts.

**Acceptance Criteria:**
- Given any tracked artifact, when its governing-story evidence is resolved, then it belongs to exactly one lifecycle lane and the workflow fails closed on ambiguity or drift.
- Given the two approved standalone artifacts, when the tracker is parsed, then each stable ID has exactly one matching row and synchronized metadata/status, with neither ID under `development_status`.
- Given the 26.2 and 26.6 companion artifacts, when classification is checked, then each declares its registered story and no longer declares the standalone route.
- Given the two retrospective actions, when implementation completes, then both original rows remain once with owner `Amelia`, status `done`, and register-backed resolution comments.
- Given resolved `bmad-spec` and `bmad-code-review` workflows on `.agents` and `.claude`, when customization tests run, then each loads the shared fact and exactly one standalone directive while all prior facts, directives, and review layers remain.

## Spec Change Log

## Design Notes

The top-level registers are siblings: `development_status` remains the sole registered-story authority, while `standalone_artifacts` uses stable IDs and the same five lifecycle values. Classification is based on registry evidence, not filename shape; the historical `19-5` name is a single explicit exception.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` -- expected: all customization, tracker, artifact, and YAML fixtures pass.
- `python3 -c "import yaml; yaml.safe_load(open('_bmad-output/implementation-artifacts/sprint-status.yaml', encoding='utf-8'))"` -- expected: exits zero with one valid YAML document.
- `git diff --check -- _bmad/custom _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts tests/tooling/bmad_customization/bmad_customization_test.py` -- expected: no whitespace errors in the scoped change.
