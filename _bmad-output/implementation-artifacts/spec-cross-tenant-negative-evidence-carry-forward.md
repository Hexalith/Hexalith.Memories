---
title: 'Close the bmad-spec cross-tenant evidence context gap'
type: 'bugfix'
created: '2026-07-16'
status: 'draft'
review_loop_iteration: 1
baseline_commit: 'c28a1d8ce0459abb713df9f029a028efa578702d'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-cross-tenant-negative-evidence-refresh.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved repository-lifetime cross-tenant negative-evidence guard is present in `epics.md`, `_bmad-output/project-context.md`, and sprint status, and the main story/development/review workflows load it. `bmad-spec` is the exception: its generated default references only a nonexistent root `project-context.md`, so future specs can miss the guard that the approved proposal explicitly applies to specs.

**Approach:** Add an update-safe team customization that appends the real `_bmad-output/project-context.md` as a `bmad-spec` persistent fact, and cover the resolved customization with the existing tooling test lane.

## Boundaries & Constraints

**Always:** Keep the customization in `_bmad/custom`, where it survives generated skill refreshes; preserve the existing repository-lifetime rule, its Story 20.2 denial-before-dependency anchor, its Story 24.3 verifier/tenant-marker anchor, and the ongoing `in-progress` action; prove both that the resolved fact is present and that its target file exists.

**Ask First:** Any proposal to replace the shared `bmad-spec` default, move or duplicate `project-context.md`, add a general evidence-attachment validator, or change the action from `in-progress` to `done`.

**Never:** Edit generated `.agents/skills/bmad-spec` files; rewrite the approved sprint-change proposal or completed stories; claim that historical integration evidence passed where Story 24.3 records it as blocked; touch product runtime code, tenant routing, authentication, storage, the existing dirty `references/Hexalith.EventStore` submodule, or unrelated customizations.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Resolved customization | Team override plus generated `bmad-spec` defaults | Persistent facts contain the real `_bmad-output/project-context.md` path and its file exists | Unit test fails if the fact or target disappears |
| Generated skill refresh | `.agents/skills/bmad-spec` is regenerated | Team-owned override continues to inject the guard without editing generated files | Resolver fixture exposes merge or path regressions |

</frozen-after-approval>

## Code Map

- `.agents/skills/bmad-spec/customize.toml` -- generated default currently points only to `{project-root}/project-context.md`; evidence source, never edit.
- `.agents/skills/bmad-spec/SKILL.md` -- activation contract that loads resolved persistent facts.
- `_bmad/custom/bmad-spec.toml` -- new team-owned, update-safe persistent-fact override.
- `_bmad-output/project-context.md` -- existing authoritative attached-negative-evidence rule at line 77.
- `tests/tooling/bmad_customization/bmad_customization_test.py` -- resolver-level customization regression tests.
- `.github/workflows/ci.yml` -- already executes the BMad customization unittest lane.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/custom/bmad-spec.toml` -- append `file:{project-root}/_bmad-output/project-context.md` under `[workflow].persistent_facts` -- ensure future spec creation receives the durable guard through a refresh-safe customization.
- [ ] `tests/tooling/bmad_customization/bmad_customization_test.py` -- add a focused `bmad-spec` resolution test that checks the exact fact and existing target -- fail closed on configuration or path drift.
- [ ] `_bmad-output/implementation-artifacts/spec-cross-tenant-negative-evidence-carry-forward.md` -- record verification results and reviewed file scope without changing the ongoing action status -- attach completion evidence to this governance fix.

**Acceptance Criteria:**
- Given the repository has no root `project-context.md`, when `bmad-spec` customization is resolved, then its persistent facts include `file:{project-root}/_bmad-output/project-context.md` and that target exists.
- Given the generated skill directory may be refreshed, when team customizations are reapplied, then the guard remains supplied exclusively through `_bmad/custom/bmad-spec.toml`.
- Given the approved cross-tenant carry-forward policy, when this change is completed, then `epics.md`, `_bmad-output/project-context.md`, the Story 20.2/24.3 evidence anchors, and the sprint action remain intact and no unrelated product or submodule file is changed.
- Given the customization fixture runs, when either the resolved fact or its target is absent, then the focused test fails rather than allowing a scope-sensitive spec to proceed without the guard.

## Spec Change Log

## Design Notes

This is an evidence-delivery fix, not a new tenant-isolation mechanism. The affected surface is `bmad-spec` activation context. Story 20.2 remains the canonical denial-before-dependency proof and Story 24.3 remains the verifier/tenant-marker fail-closed proof; no product path changes here, so their historical results are referenced rather than rerun as if runtime behavior changed.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` -- expected: all customization fixtures pass.
- `uv run _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-spec --key workflow` -- expected: JSON includes `file:{project-root}/_bmad-output/project-context.md` in `persistent_facts`.
- `git diff --check -- _bmad/custom/bmad-spec.toml tests/tooling/bmad_customization/bmad_customization_test.py _bmad-output/implementation-artifacts/spec-cross-tenant-negative-evidence-carry-forward.md` -- expected: no whitespace errors and no unrelated paths.

**Superseded results (2026-07-16, review loop 1):**
- The success-path implementation was reverted after parallel review found that `bmad-spec` drops all team customizations when resolver execution fails and falls back to its generated default. Because the locked intent requires durable delivery while forbidding generated-file edits and requiring approval before moving or duplicating project context, implementation is paused for a human fail-closed design decision.
- `test_spec_resolves_update_safe_project_context_fact` covers both matrix rows by resolving the generated defaults with the team override, asserting the exact persistent fact, and asserting its target file exists. The full customization lane ran 3 tests with 0 failures.
- The resolver exited 0 and returned both the generated root fact and `file:{project-root}/_bmad-output/project-context.md`; the team-owned fact therefore survives without editing `.agents/skills/bmad-spec`.
- Scoped `git diff --check` exited 0 with no output. This task changed only the team override, the focused customization test, and this completion record; unrelated concurrent working-tree changes were preserved.
- Affected scope-sensitive surface: `bmad-spec` activation context only. Story 20.2 denial-before-dependency and Story 24.3 verifier/tenant-marker evidence remain the canonical runtime anchors; no runtime route, authorization, storage, verifier, or submodule behavior changed, so no product negative test was falsely claimed as rerun.
