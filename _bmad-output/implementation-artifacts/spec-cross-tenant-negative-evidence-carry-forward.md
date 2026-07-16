---
title: 'Add a fail-closed project-context bridge for bmad-spec'
type: 'bugfix'
created: '2026-07-16'
status: 'in-progress'
review_loop_iteration: 2
baseline_commit: '56faf29454be613a09ca3865b7ba3c9844dc5f9b'
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

- `project-context.md` -- new stable root bridge consumed by the generated normal and fallback fact path; contains no duplicated policy body.
- `_bmad-output/project-context.md` -- canonical attached-negative-evidence rule and Story 20.2/24.3 anchors.
- `.agents/skills/bmad-spec/customize.toml` -- generated default that names the root fact; evidence source, never edit.
- `.agents/skills/bmad-spec/SKILL.md` -- generated activation/fallback contract that consumes persistent file facts; evidence source, never edit.
- `.agents/skills/bmad-generate-project-context/steps/step-01-discover.md` -- generated discovery contract whose broad context search can see both bridge and canonical files; evidence source, never edit.
- `_bmad/custom/bmad-generate-project-context.toml` -- new team-owned directive selecting the canonical file for project-context updates.
- `tests/tooling/bmad_customization/bmad_customization_test.py` -- existing resolver/contract fixture lane, including concurrent team customizations that must be preserved.
- `.github/workflows/ci.yml` -- already runs the BMad customization unittest lane.

## Tasks & Acceptance

**Execution:**
- [ ] `project-context.md` -- add an exact, forwarding-only bridge that requires canonical loading, halts when canonical context is unavailable, and forbids generators from updating the bridge -- make all normal, fallback, and glob consumers fail closed without creating a second policy source.
- [ ] `_bmad/custom/bmad-generate-project-context.toml` -- add an update-safe `PROJECT_CONTEXT_BRIDGE:` activation directive that selects `_bmad-output/project-context.md` as the only project-context read/update target -- remove generator ambiguity introduced by the root bridge.
- [ ] `tests/tooling/bmad_customization/bmad_customization_test.py` -- add focused contract coverage for exact bridge content, exact `bmad-spec` fact list, active canonical rule structure and operative clauses, fallback consumption, generator selection, epics guard, and ongoing action -- fail closed on delivery, payload, or writer-selection drift while preserving concurrent test work.
- [ ] `_bmad-output/implementation-artifacts/spec-cross-tenant-negative-evidence-carry-forward.md` -- attach matrix coverage, baseline-scoped inventory/checks, implementation and owned-patch digests, and reviewed exclusions while leaving the ongoing action `in-progress` -- make completion auditable.

**Acceptance Criteria:**
- Given generated customization resolves normally, when its persistent facts are inspected, then they are a list containing exactly one `file:{project-root}/project-context.md`, whose readable bridge directs agents to canonical project context.
- Given resolver execution fails, when `bmad-spec` follows its documented generated-default fallback, then the fallback still consumes the same root file fact and reaches the canonical guard through the bridge.
- Given the bridge remains but the canonical attached-negative-evidence rule or Story 20.2/24.3 anchors drift, when the customization fixture runs, then it fails.
- Given the bridge is inspected, when its content is compared with the approved forwarding contract, then it matches exactly, contains no project policy body, and requires halt/report behavior when canonical context is unavailable.
- Given workflows load `**/project-context.md`, when they encounter both bridge and canonical context, then the bridge remains an inert forwarding/control fact and the canonical file remains the only policy source.
- Given `bmad-generate-project-context` discovers both files, when its resolved activation directive runs, then it selects and updates only `_bmad-output/project-context.md` and never rewrites the root bridge.
- Given this change is completed, when owned scope is reviewed, then existing epics/project-context guards and the ongoing sprint action remain intact, while generated files, runtime code, submodules, and unrelated concurrent changes remain untouched.

## Spec Change Log

- 2026-07-16, review loop 1: Parallel review found the success-path team override vanished during `bmad-spec` resolver fallback, leaving scope-sensitive specs fail-open. Human authorization changed the frozen approach to a root forwarding bridge; tasks now test normal resolution, fallback consumption, and canonical payload rather than only file existence. This avoids generated-file edits and policy duplication. KEEP the existing canonical guard, Story 20.2/24.3 anchors, ongoing action status, focused customization lane, and strict unrelated-change boundary.
- 2026-07-16, review loop 2: Review found the root bridge also enters broad `**/project-context.md` consumers and can be selected by `bmad-generate-project-context`, while substring assertions allowed bridge contradictions and canonical-policy weakening. The plan now treats glob consumers as affected, makes the bridge exact and fail-closed, adds a team-owned canonical-writer directive, protects every operative evidence clause and ongoing governance anchor, and uses baseline-relative scope/digest evidence. This avoids a ruleless bridge becoming the update target or a neutered policy passing CI. KEEP the forwarding design, normal/fallback convergence, generated-file boundary, passing customization lane, and review-loop 1 canonical anchors.

## Design Notes

The root bridge is an exact control document, not a second policy source. Normal resolution and resolver fallback use it directly; glob-based workflows may load it alongside canonical context, where its only effect is to identify the canonical file and halt if that file is unavailable. Because project-context generation has singular discovery semantics, a refresh-safe activation directive explicitly pins its writer to `_bmad-output/project-context.md`. Story 20.2 and Story 24.3 remain historical runtime anchors and are not falsely reported as newly rerun.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` -- expected: all customization fixtures pass.
- `uv run _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-spec --key workflow` -- expected: JSON contains exactly one root `project-context.md` persistent fact.
- `uv run _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-generate-project-context --key workflow` -- expected: exactly one `PROJECT_CONTEXT_BRIDGE:` directive selecting the canonical writer target.
- `git diff --check <baseline_commit> -- tests/tooling/bmad_customization/bmad_customization_test.py _bmad-output/implementation-artifacts/spec-cross-tenant-negative-evidence-carry-forward.md` -- expected: no whitespace errors in baseline-relative tracked deltas.
- `git diff --no-index --check /dev/null project-context.md` and the equivalent command for `_bmad/custom/bmad-generate-project-context.toml` -- expected: content-difference exit 1 with no whitespace-error output.
- `git diff --name-status <baseline_commit>` plus `git ls-files --others --exclude-standard` -- expected: record the three owned paths and list unrelated submodule/spec exclusions separately.
- `sha256sum project-context.md _bmad/custom/bmad-generate-project-context.toml` and `git diff <baseline_commit> -- tests/tooling/bmad_customization/bmad_customization_test.py | sha256sum` -- expected: record implementation-file and isolated owned-patch snapshots.
