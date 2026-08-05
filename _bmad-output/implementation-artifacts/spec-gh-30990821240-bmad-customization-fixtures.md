---
title: 'Fix BMAD customization fixtures for CI run 30990821240'
type: 'bugfix'
created: '2026-08-05'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'a79557e5b2ac5c285ea5063a21e8937ce61d813c'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** CI run 30990821240 fails `test-unit-contract` at "Run BMad customization fixtures": `bmad-generate-project-context` is a deprecate shim without `customize.toml`, and `bmad-spec` On Activation step 3 drifted to `resolve_config.py` while the fixture golden still expects `config.yaml` load.

**Approach:** Retarget the team project-context writer override and delivery contract to live `bmad-project-context`, refresh the `bmad-spec` activation golden from the installed skill, and assert the generate skill is a deprecate-only forwarder — without weakening cross-tenant bridge / writer-directive guarantees.

## Boundaries & Constraints

**Always:** Keep asserting the canonical write target `{project-root}/_bmad-output/project-context.md` and that `{project-root}/project-context.md` remains a read-only bridge; refresh goldens from live `.agents`/`.claude` skill text; keep fixture strength (exact activation text + resolved customization merge).

**Ask First:** Changing the `PROJECT_CONTEXT_BRIDGE` writer directive wording, or expanding this fix into kernel/bundle path policy beyond the existing bridge write-target rule.

**Never:** Hand-restore the pre-v7 full generate skill into `.agents`/`.claude`; weaken or delete the cross-tenant delivery contract; edit product C# / integration harness; absorb the deferred integration-fast Dapr failures.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Happy path | Fresh checkout at HEAD with current BMAD skills | `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` exits 0 | N/A |
| Spec golden | `.agents` and `.claude` `bmad-spec` On Activation | Equals refreshed `SPEC_ACTIVATION_CONTRACT` (step 3 = `resolve_config.py`) | Fail on drift |
| Project-context delivery | `bmad-project-context` + team custom | Defaults empty facts/append; resolved append includes `PROJECT_CONTEXT_WRITER_DIRECTIVE` | Fail if orphaned under old skill name |
| Deprecated generate shim | `bmad-generate-project-context` skill dir | Deprecate/forward SKILL.md only; no required `customize.toml` | Fail if full-workflow goldens reappear |

</frozen-after-approval>

## Code Map

- `tests/tooling/bmad_customization/bmad_customization_test.py` — `test_cross_tenant_project_context_delivery_contract` (~L258–388); goldens `SPEC_ACTIVATION_CONTRACT` (~L158–165), `PROJECT_CONTEXT_ACTIVATION_CONTRACT` (~L167–173); constants `PROJECT_CONTEXT_CUSTOMIZATION` (~L26–28), `PROJECT_CONTEXT_WRITER_DIRECTIVE` (~L66–72), `DEPRECATED_GENERATE_FORWARD_PATTERN` (~L174–176); resolver helper `run_resolve_workflow` (~L236–251)
- `_bmad/custom/bmad-project-context.toml` — team writer-directive override merged onto live `bmad-project-context`
- `_bmad/custom/bmad-generate-project-context.toml` — removed (orphaned; shim has no base `customize.toml`)
- `_bmad/custom/bmad-spec.toml` — keep; does not rewrite On Activation prose
- `.agents/skills/bmad-spec/SKILL.md` + `.claude/skills/bmad-spec/SKILL.md` — live On Activation (~L19–28); source for golden refresh
- `.agents|/.claude/skills/bmad-project-context/` — live replacement (`customize.toml` empty facts/append; On Activation ~L19–26)
- `.agents|/.claude/skills/bmad-generate-project-context/SKILL.md` — deprecate→ingest forwarder only; no `customize.toml`; resolver fails closed
- `_bmad/scripts/config_utils.py` (~L110–118) — `load_customization` requires base `customize.toml`
- `_bmad/_config/skill-manifest.csv` — generate deprecated → `bmad-project-context`
- `.github/workflows/ci.yml` (~L239–240) — CI invokes the same unittest discover command

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/custom/bmad-project-context.toml` -- Move/adapt the writer-directive append from `bmad-generate-project-context.toml`; remove the orphaned generate custom so the override merges onto the live skill -- team override must survive BMAD refresh
- [x] `tests/tooling/bmad_customization/bmad_customization_test.py` -- Retarget generator constants/assertions to `bmad-project-context` (empty defaults + resolved writer directive + live On Activation); assert generate shim is deprecate-only; refresh `SPEC_ACTIVATION_CONTRACT` from live `bmad-spec` -- fixtures must match installed skills without weakening bridge guarantees

**Acceptance Criteria:**
- Given current BMAD skill surfaces on `.agents` and `.claude`, when the BMAD customization unittest suite runs, then it exits 0 with zero failures/errors
- Given `bmad-project-context` team custom, when customization is resolved, then `activation_steps_append` includes the existing `PROJECT_CONTEXT_WRITER_DIRECTIVE` and defaults keep empty `persistent_facts` / append
- Given `bmad-generate-project-context`, when the delivery contract runs, then it treats the skill as a deprecate forwarder and does not require `customize.toml`
- Given `bmad-spec` On Activation on both surfaces, when compared to `SPEC_ACTIVATION_CONTRACT`, then step 3 matches the live `resolve_config.py` wording

## Spec Change Log

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` -- expected: exit 0, all tests pass
- `python3 -m unittest tests.tooling.bmad_customization.bmad_customization_test.BMadCustomizationTests.test_cross_tenant_project_context_delivery_contract` -- expected: exit 0 (the previously failing method)

## Suggested Review Order

**Team override retarget**

- Writer directive now merges onto live `bmad-project-context`.
  [`bmad-project-context.toml:6`](../../_bmad/custom/bmad-project-context.toml#L6)

**Delivery contract assertions**

- Cross-tenant delivery retargeted to `bmad-project-context` defaults + resolved append.
  [`bmad_customization_test.py:351`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L351)

- Spec activation golden refreshed for `resolve_config.py` step 3.
  [`bmad_customization_test.py:158`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L158)

- Generate shim must forward ingest to `bmad-project-context` and fail closed without `customize.toml`.
  [`bmad_customization_test.py:378`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L378)

- Orphaned generate team override must stay deleted.
  [`bmad_customization_test.py:346`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L346)

**Deferred CI remainder**

- Integration-fast Dapr Connection refused split for a follow-up pass.
  [`deferred-work.md`](deferred-work.md)
