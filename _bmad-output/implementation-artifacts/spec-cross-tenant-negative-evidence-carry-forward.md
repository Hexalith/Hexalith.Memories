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

**Problem:** The approved repository-lifetime cross-tenant negative-evidence guard is present in `epics.md`, `_bmad-output/project-context.md`, and sprint status. `bmad-spec` references a nonexistent root `project-context.md`; a team-only customization can repair normal resolution but disappears when resolver execution fails and the skill falls back to generated defaults, so future specs can still miss the guard.

**Approach:** Add a root `project-context.md` forwarding bridge to the canonical `_bmad-output/project-context.md`, making both normal and resolver-fallback activation safe without editing generated files or duplicating policy content. Cover the resolved default, bridge contract, canonical guard payload, and fallback consumption contract in the existing tooling test lane.

## Boundaries & Constraints

**Always:** Keep `_bmad-output/project-context.md` canonical and make the root file a forwarding bridge only; preserve the existing repository-lifetime rule, its Story 20.2 denial-before-dependency anchor, its Story 24.3 verifier/tenant-marker anchor, and the ongoing `in-progress` action; prove the generated default resolves to the readable bridge, the bridge directs agents to the canonical file, the canonical guard payload remains intact, and the documented fallback still consumes persistent facts.

**Ask First:** Any proposal to replace the shared `bmad-spec` default, move or duplicate the canonical project-context content, add a general evidence-attachment validator, or change the action from `in-progress` to `done`.

**Never:** Edit generated `.agents/skills/bmad-spec` files; copy the canonical policy body into the root bridge; rewrite the approved sprint-change proposal or completed stories; claim that historical integration evidence passed where Story 24.3 records it as blocked; touch product runtime code, tenant routing, authentication, storage, submodules, or unrelated customizations.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Normal resolution | Generated `bmad-spec` defaults resolve successfully | Root persistent fact names an existing bridge that directs agents to canonical project context | Unit test fails if the fact, bridge, or canonical target disappears |
| Resolver fallback | Customization resolution fails and `bmad-spec` reads generated defaults | The same root bridge still directs activation to canonical project context | Contract test fails if fallback or persistent-fact consumption is removed |
| Canonical policy drift | Bridge and canonical file exist but the attached-evidence rule or anchors are removed | CI rejects the change before future specs lose the guard | Payload assertion fails on the stable rule and Story 20.2/24.3 anchors |

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
