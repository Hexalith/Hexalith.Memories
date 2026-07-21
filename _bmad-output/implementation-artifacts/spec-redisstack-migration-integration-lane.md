---
title: 'Enforce RedisStack Migration Integration Test Lane'
type: 'chore'
created: '2026-07-21'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'ae591ce7a0f1f6aca54ccdaea303eb63980dfa25'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 21 retrospective action #4 (`sprint-status.yaml`, epic-21, `status: open`) requires a runnable or explicitly accepted infrastructure lane for the RedisStack migration integration tests (`EmbeddingVectorMigrationRedisIntegrationTests`). The Docker-backed CI `integration-fast` lane already runs the class and it already passes there, but its execution is not asserted by the coverage gate, so the action stayed open and the coverage could silently regress.

**Approach:** Promote the migration test class to a required surface in the `integration-fast` coverage gate so its execution is enforced on every green lane run, then close the retro action in `sprint-status.yaml` with dated CI evidence. This satisfies the "runnable lane" arm — not accepted-debt — because the tests demonstrably pass in the approved CI lane.

## Boundaries & Constraints

**Always:**
- Use the exact fully-qualified class name `Hexalith.Memories.IntegrationTests.Migration.EmbeddingVectorMigrationRedisIntegrationTests` and a unique kebab-case surface key, matching the existing `surface|ClassName` row format and the file's line-ending convention.
- Keep the sprint-status closure evidence factual and dated: cite the approved lane (`ci.yml` job `integration-fast`), the passing run, and the newly enforced surface.

**Ask First:**
- Any change that would touch the migration test code, `RedisStackFixture`, or the CI job definitions (`ci.yml` / `nightly.yml`) rather than only the surface list and the tracker.

**Never:**
- Do not modify the migration tests, the Testcontainers fixture, or the CI workflow job steps.
- Do not fix or suppress the unrelated `OpenBaoTopologyIntegrationTests` timeout failure — out of scope.
- Do not rewrite the historical Epic 21 retro document; `sprint-status.yaml` is the live tracker.
- Do not close the action as accepted-debt — the runnable-lane arm applies.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Green lane, migration ran | TRX from a green `integration-fast` run containing the migration class | Coverage verifier reports the `migration` surface satisfied; gate passes | N/A |
| Migration silently dropped | Green lane TRX missing the migration class | Verifier returns exit 1 listing the missing `migration` surface | Fails CI, surfacing the regression |
| Red lane (unrelated fail) | Test step fails before verifier step | Verifier does not run; added surface has no effect until lane is green again | N/A |

</frozen-after-approval>

## Code Map

- `tools/integration-fast-required-surfaces.txt` -- required-surface list consumed by the CI coverage gate; the migration surface is added here.
- `tools/verify-integration-fast-coverage.py` -- gate asserting each required surface's class appears among executed TRX classes; runs after a green `integration-fast` test step.
- `.github/workflows/ci.yml` (job `integration-fast`, ~L320-370) -- the approved Docker lane running `Category=Integration&Category!=IntegrationSlow&Category!=Performance`; reference only.
- `tests/Hexalith.Memories.IntegrationTests/Migration/EmbeddingVectorMigrationRedisIntegrationTests.cs` -- enforced class (`[Trait("Category","Integration")]`, 5 `[Fact]`, self-provisions Redis Stack via `RedisStackFixture`); reference only.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (epic-21 action, ~L559-562) -- retro action to close with evidence.

## Tasks & Acceptance

**Execution:**
- [x] `tools/integration-fast-required-surfaces.txt` -- append `migration|Hexalith.Memories.IntegrationTests.Migration.EmbeddingVectorMigrationRedisIntegrationTests` -- enforce migration-lane execution in the coverage gate.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- set the epic-21 "Provide a runnable or explicitly accepted infrastructure lane for RedisStack migration integration tests" action to `status: done` with a dated comment citing CI run `29798593273` (261 passed incl. the 5 migration tests, 8 skipped, 1 unrelated fail) and the new required surface.

**Acceptance Criteria:**
- Given the required-surfaces file, when parsed by `verify-integration-fast-coverage.py`'s `load_requirements`, then it loads without error and maps a `migration` surface to the exact migration class name.
- Given `sprint-status.yaml`, when parsed as YAML, then it is valid and the epic-21 migration-lane action reads `status: done` with factual dated evidence.
- Given the enforced surface, when a future green `integration-fast` run's verifier executes, then the `migration` surface is satisfied and a dropped migration class would fail the gate.

## Spec Change Log

- 2026-07-21 (step-04 review, no loopback): Three fresh-context reviewers (adversarial, edge-case, verification-gap) converged on two substantive points about the enforcement's strength; classified as **patch** + **defer**, not bad_spec, because the deliverable (the required-surfaces line and the `done` closure) is correct on the action's actual success criterion — the 5 migration `[Fact]`s execute and pass in the approved CI lane.
  - **patch (applied):** the `sprint-status.yaml` closure comment was reworded — it had overstated the gate as "asserts the class executes on every green lane run" and cited run `29798593273` as if it exercised the gate. Corrected wording: the gate asserts the class is *present* in a fully-green integration-fast TRX (catches removal/rename/non-discovery), the verifier checks presence not per-class pass/skip, is `success()`-gated (green lanes only), and run `29798593273` was itself red (OpenBao in-lane) so its verifier step did not run — the gate first fires on the next fully-green run. Re-open triggers were corrected to cover the skip and surface-removal blind spots.
  - **defer (recorded):** `deferred-work.md` ID `21.10-A4-VERIFY` — make `verify-integration-fast-coverage.py` outcome-aware (require ≥1 executed/Passed per required surface, not mere TRX presence) and add a red-lane/nightly enforcement backstop. Pre-existing; affects all required surfaces.
  - **frozen-block limitation (human renegotiation required):** the `<frozen-after-approval>` **Intent** ("its execution is enforced on every green lane run") and the **I/O & Edge-Case Matrix** carry the same presence-vs-execution imprecision and omit a "class present but tests skipped/NotExecuted → gate falsely green" row. These were left unmodified because frozen content is human-owned. The corrected, accurate framing lives in the non-frozen closure comment and this log; renegotiate the frozen block if you want it aligned.

## Verification

**Commands:**
- `python3 -c "import importlib.util; from pathlib import Path; s=importlib.util.spec_from_file_location('v','tools/verify-integration-fast-coverage.py'); m=importlib.util.module_from_spec(s); s.loader.exec_module(m); r=m.load_requirements(Path('tools/integration-fast-required-surfaces.txt')); assert r['migration']=='Hexalith.Memories.IntegrationTests.Migration.EmbeddingVectorMigrationRedisIntegrationTests'; print('surfaces', len(r))"` -- expected: prints `surfaces 11`, no assertion error.
- `python3 -c "import yaml; yaml.safe_load(open('_bmad-output/implementation-artifacts/sprint-status.yaml'))"` -- expected: no exception (valid YAML).
- `git diff --check` -- expected: no whitespace errors.

**Manual checks:**
- The migration test class cannot be executed in this sandbox (no Docker socket). Closure rests on the approved CI lane; the next green `integration-fast` run exercises the newly enforced surface.

## Suggested Review Order

**Decision & evidence (start here)**

- Why the action is `done`: runnable-lane arm + honest gate caveats + re-open triggers
  [`sprint-status.yaml:562`](sprint-status.yaml#L562)

**The enforcement wiring (the actual change)**

- The one line that makes the coverage gate assert the migration class
  [`integration-fast-required-surfaces.txt:13`](../../tools/integration-fast-required-surfaces.txt#L13)

- How the gate decides a surface "ran" — harvests classNames from TRX, presence-based (the caveat driving the deferral)
  [`verify-integration-fast-coverage.py:66`](../../tools/verify-integration-fast-coverage.py#L66)

- The `success()`-gated verifier step that runs the gate only on a green lane
  [`ci.yml:369`](../../.github/workflows/ci.yml#L369)

**What is enforced (reference)**

- The enforced class: `Category=Integration`, 5 plain `[Fact]`s, Testcontainers Redis Stack
  [`EmbeddingVectorMigrationRedisIntegrationTests.cs:30`](../../tests/Hexalith.Memories.IntegrationTests/Migration/EmbeddingVectorMigrationRedisIntegrationTests.cs#L30)

**Follow-up (peripheral)**

- Deferred verifier hardening: make the gate outcome-aware + add a red-lane/nightly backstop
  [`deferred-work.md:2325`](deferred-work.md#L2325)
