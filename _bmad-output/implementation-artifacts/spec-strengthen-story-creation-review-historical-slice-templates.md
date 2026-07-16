---
title: 'Strengthen historical-slice story creation and review checks'
type: 'feature'
created: '2026-07-16'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'c28a1d8ce0459abb713df9f029a028efa578702d'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-historical-slice-guard-strengthening.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Historical broad, bundled, umbrella, or explicitly guarded stories can be useful evidence but are unsafe templates for new story shape. Story creation and full code review must retain an update-safe, fail-closed guard after installed BMad skills are refreshed.

**Approach:** Verify and, only where acceptance fails, correct the committed repository-owned policy, create-story customization, full-review layer, and resolver regression fixture. Preserve the already implemented behavior at `HEAD`; do not duplicate it or edit generated installed skills.

## Boundaries & Constraints

**Always:** Classify every prior-story influence semantically; treat numeric adjacency as irrelevant; distinguish narrow current-source pattern reuse from whole-story reuse; split independently demonstrable outcomes unless current epics explicitly authorize independently evidenced checkpoints; preserve all default review layers; keep enforcement and regression coverage on committed repository-owned surfaces.

**Ask First:** Any change to current epic/story scope, sprint status, the approved checkpoint exception, or the meaning/severity of a confirmed anti-template violation; any proposal to modify a root-declared submodule.

**Never:** Edit `.agents/skills/**`, product code, PRD, epics, architecture, UX, or submodule contents; reuse a historical story's task/AC/file/proof breadth by default; allow an unresolved violation to reach `ready-for-dev`; replace or remove the later phase-ledger customization; alter the unrelated `references/Hexalith.EventStore` pointer change.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Narrow reuse | Prior story contributes a focused pattern re-verified in current source | Classify as `current-narrow-pattern`; whole-story shape remains unused | Fail validation if classification or current-source proof is absent |
| Anti-template | Prior artifact is broad, bundled, guarded, superseded, or hides independent outcomes | Classify as `anti-template`; block readiness and sprint mutation pending split | Route ambiguous splits to human decision |
| Approved checkpoint | Current epics explicitly authorize one tracking story with checkpoints | Require independent owner, evidence, review, and completion state per checkpoint | Treat missing checkpoint proof as unresolved scope violation |
| Full review | Spec and diff show confirmed template reuse or hidden multi-slice scope | Produce actionable high-severity finding with spec, diff, and policy evidence | Route clear correction to patch; scope choice to `decision_needed` |
| Skill refresh | Generated defaults change while team customization remains | Resolver retains team facts/directives/layer and all default layers | Focused fixture fails before story creation or review proceeds |

</frozen-after-approval>

## Code Map

- `_bmad/custom/story-scope-guard.md` -- shared semantic classification, creation gate, review severity, and routing policy.
- `_bmad/custom/bmad-create-story.toml` -- update-safe create-story facts and fail-closed activation directive.
- `_bmad/custom/bmad-code-review.toml` -- update-safe full-review historical-slice layer merged with default layers.
- `_bmad-output/process-notes/story-creation-lessons.md` -- durable L09 lesson and supported enforcement route.
- `tests/tooling/bmad_customization/historical_slice_guard_test.py` -- isolated resolver and policy-contract regression fixture for both customized workflows.
- `.github/workflows/ci.yml` -- CI invocation for the customization fixture.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/custom/story-scope-guard.md` -- verify semantic classification, fail-closed creation, approved-checkpoint exception, observable proof, and high-severity review routing; correct only confirmed drift.
- [x] `_bmad/custom/bmad-create-story.toml` and `_bmad/custom/bmad-code-review.toml` -- resolve both configurations and verify exactly one historical-slice directive/layer while retaining later team customizations and all defaults; patch only failed invariants.
- [x] `tests/tooling/bmad_customization/historical_slice_guard_test.py` and `.github/workflows/ci.yml` -- isolate and run the regression fixture, confirming CI discovery continues to invoke it without capturing concurrent test work.
- [x] `_bmad-output/process-notes/story-creation-lessons.md` and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06-historical-slice-story-guard.md` -- verify the durable update-safe guidance and supersession record remain truthful.

**Acceptance Criteria:**
- Given refreshed installed BMad defaults, when create-story customization resolves, then it loads the shared policy and lesson exactly once and retains one fail-closed `HISTORICAL_SLICE_GUARD` directive.
- Given full review, when code-review customization resolves, then exactly one full-only `historical-slice-guard` reads policy, spec, and diff while every default review layer remains present.
- Given any prior-story influence, when a new story is validated, then classification and slice proof are required and unresolved anti-template or required-split violations prevent readiness and sprint-status mutation.
- Given current `HEAD`, when focused verification runs, then all historical-slice customization tests pass without generated-skill, product-planning, product-code, sprint-status, or submodule changes.

## Spec Change Log

## Design Notes

The requested behavior is already implemented by commit `0b5d0160`. This spec is verification-first: existing compliant artifacts are the baseline, and implementation edits are justified only by reproducible acceptance failure. The later phase-ledger work is a distinct concern and must compose with, not replace, these guards.

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` -- expected: the complete customization contract suite passes.
- `uv run _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-create-story --key workflow` -- expected: policy facts and exactly one fail-closed historical-slice directive are present.
- `uv run _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-code-review --key workflow` -- expected: exactly one full-only historical-slice layer plus all default layers are present.
- `git diff --check` -- expected: no whitespace errors; the unrelated EventStore pointer remains untouched.

**Results:**
- Complete customization suite: 13 tests passed; five isolated historical-slice tests cover every configuration-contract matrix scenario.
- Create-story resolution: exactly one policy fact, lesson fact, and historical-slice directive; the concurrent phase-ledger fact/directive remains present.
- Code-review resolution: exactly one policy fact, lesson fact, historical-slice directive, and full-only historical layer; every installed default layer and the concurrent phase-ledger layer remain present.
- Matrix contract coverage: section-scoped complete rules cover narrow reuse, historical-reference-only context, anti-template breadth and split blocking, approved-checkpoint evidence, full-review severity/routing, observable proof, and refresh-safe merging.
- Adversarial review patches added exact-once fact assertions and dynamic default-layer comparison; evidence no longer overclaims execution of nondeterministic LLM behavior.
- Whitespace validation passed with unrelated line-ending conversion warnings; no generated skill, product/planning scope, sprint-status, CI, or submodule file was changed by this implementation.

## Suggested Review Order

**Guard contract**

- Approved intent defines the verification-first boundary and protected surfaces.
  [spec-strengthen-story-creation-review-historical-slice-templates.md:14](spec-strengthen-story-creation-review-historical-slice-templates.md#L14)

- Matrix captures classification, blocking, checkpoint, review, and refresh scenarios.
  [spec-strengthen-story-creation-review-historical-slice-templates.md:28](spec-strengthen-story-creation-review-historical-slice-templates.md#L28)

**Regression enforcement**

- Create-story contract pins exact-once facts and readiness blockers.
  [historical_slice_guard_test.py:72](../../tests/tooling/bmad_customization/historical_slice_guard_test.py#L72)

- Policy contract rejects broad reuse, hidden slices, and ambiguous routing.
  [historical_slice_guard_test.py:120](../../tests/tooling/bmad_customization/historical_slice_guard_test.py#L120)

- Checkpoint contract requires independent ownership and observable proof.
  [historical_slice_guard_test.py:158](../../tests/tooling/bmad_customization/historical_slice_guard_test.py#L158)

- Review resolution preserves every installed default and team layer dynamically.
  [historical_slice_guard_test.py:190](../../tests/tooling/bmad_customization/historical_slice_guard_test.py#L190)

**Evidence**

- Results distinguish configuration contracts from nondeterministic LLM execution.
  [spec-strengthen-story-creation-review-historical-slice-templates.md:77](spec-strengthen-story-creation-review-historical-slice-templates.md#L77)
