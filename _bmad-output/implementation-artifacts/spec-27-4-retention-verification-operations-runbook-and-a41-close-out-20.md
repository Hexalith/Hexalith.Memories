---
title: 'Story 27.4 multi-Pod C1.15 registration command inspection'
type: 'feature'
created: '2026-10-13'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '24b1fa7408eabc1db24b7536fd35f575c7a572a7'
context:
  - '_bmad-output/project-context.md'
  - '_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Offline registration inspection requires exactly six command receipts. The real C1.15 grammar emits three receipts per Pod plus three shared receipts, so valid multi-Pod captures cannot complete registration inspection.

**Approach:** Add explicitly dispatched `hexalith.access-telemetry.c1.fixture-command-contract/v2`, using the existing strict C1.15 command inspector. Preserve v1 behavior and return the unchanged frozen registration inspection.

## Boundaries & Constraints

**Always:** Keep v2's seven fields identical to v1. Its six ordered `purposes` are grammar kinds: context, selection, version/metadata/alpha for each observed Pod in order, then recheck. Validate exact pinned argv through `inspect_c115_command_declarations`; bind all existing Ref bytes, clean source receipts, nineteen source snapshots and budgets. Require `cleanupRequired: false`. Preserve the existing registration and independent role-policy schemas. Document v2 as an offline fixture declaration, with no owner-registration claim.

**Never:** Change v1 acceptance/refusals, owner policy or deployed lookup; reinterpret producer exclusion; execute collectors/verifiers or contact targets; change Production, sprint status, historical evidence or A41; stage, commit, push or update dependencies.

## I/O & Edge-Case Matrix

| Scenario | Input | Outcome |
| --- | --- | --- |
| Complete v2 | One, two or three Pods; matching ordered argv and source/Ref bytes | Frozen inspection, including reversed Pod order |
| Substitution | Rehashed later-Pod argv, probe, scope or reordered receipt blocks | Content-free refusal before local Git/filesystem access |
| Binding drift | Source, Ref, schema, kinds, cleanup or role mismatch | Existing bounded refusal; no inspection |
| Legacy | Existing v1 fixtures, including multi-Pod refusal | Identical behavior |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_producer_bindings.py:319` — `inspect_registration` authenticates materials, then compares six flattened purposes. Extend only command-schema dispatch; reuse strict inspection from `access_telemetry_c1_capture_semantics.py`.
- `tools/access_telemetry_c1_source_provenance.py:219` — calls registration before any local dependency. Preserve its implementation and return type.
- `tests/tooling/access_telemetry_c1_interchange/test_capture_commands.py:49` — independently authored `command_fixture`; combine its strict receipt/Pod facts with nineteen source snapshots from `test_producer_bindings.fixture_sources`.

## Tasks & Acceptance

**Execution:**

- [x] `tools/access_telemetry_c1_producer_bindings.py` — add closed v2 dispatch without altering v1 or role checks.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_registration_commands.py` — cover every matrix row with independent fixtures and counted process/network/filesystem/configuration/clock denial barriers.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_registration_provenance.py` — prove valid v2 reaches clean local corroboration and malformed v2 refuses before its dependencies.
- [x] `docs/operations/c1-producer-binding-inspection-contract.md` — document version selection, expanded grammar and unchanged assurance limits; preserve the historical manifest/results.

**Acceptance Criteria:**

- Given a clean multi-Pod capture and matching retained materials, when v2 registration and isolated local corroboration run, then both retain exact byte hashes and return inspection without gate or execution authority.
- Given another tenant/namespace/Pod or unsafe command with recomputed hashes, when v2 is inspected, then denial precedes local dependencies.
- Given existing consumers and v1 fixtures, when the affected lane runs, then compatibility, deployed refusal, Production disablement and open A41 are preserved.

## Implementation Notes

V2 explicitly dispatches to `inspect_c115_command_declarations` after the existing
source, exact Ref, registration-field and shared-budget checks. V1 keeps its
six-receipt flattened-purpose comparison and existing refusals. Both versions
keep the seven command fields, six ordered kinds, false cleanup requirement,
unchanged independent role policy and frozen `RegistrationInspection` surface.
No deployed entry or execution authority is introduced.

Independent fixtures combine `test_capture_commands.command_fixture` with all
nineteen retained snapshots and source receipts from `test_producer_bindings`.
Positive cases cover all one/two/three-Pod observation permutations and exact
retained bytes. Cross-tenant/scope/Pod denial evidence is
`test_every_rehashed_argument_including_later_pods_refuses`,
`test_probe_scope_unsafe_verb_and_cross_pod_substitution_refuse`,
`test_rehashed_target_identity_and_image_pin_drift_refuse`, and
`test_v2_rehashed_later_pod_commands_refuse_before_every_local_dependency`.
Counted barriers cover filesystem, process, network, configuration and clocks,
including prebound aliases and local provenance entry points. Successful local
corroboration uses disposable clean Git repositories only. These tests carry
forward the denial-before-dependency pattern of Stories 20.2/24.3; they provide
no live target observation, owner-registration claim or gate credit.

The original `baseline_commit` is preserved. The eleven unrelated committed
paths below were already present at current-start HEAD
`42b4dc4d58731d4e9ee777285ba1f65f26e4a0c0`; they belong to Jérôme Piquot's
module/clock readiness commits
`a2825bbf7d5da9c71b4f7096032fad67b03aa74d` and
`7034b839791fd8dbe0c6aff50b907fbecd90701a`. They are excluded from this spec's
cumulative changed-file set and Python test deltas. The earlier create row's
one-file result describes its bounded ownership set; a raw comparison against
the older baseline additionally includes that pre-existing committed work.
The current-start HEAD is an additional preservation comparison, not a
replacement baseline. Sprint status, Production disablement, historical
evidence and open A41 remain unchanged.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| Blind 1: incomplete prebound dependency scan | medium | patch | Reproduced a prebound `time.perf_counter` call returning a float with zero barrier counts. The shared harness denies that dependency, but the new bindings/provenance alias scan omits it; extend that scan and its sentinel checks. |
| Blind 2: successful corroboration process barrier | medium | patch | The new positive provenance case patches sockets/HTTP and `os.system`, while allowing every `subprocess.Popen` launch. Actual provenance currently emits only fixed local Git commands; assert that closed process/environment grammar in the v2 positive case so a collector/target process regression cannot escape. |
| Blind 3: unknown-schema fallback control | low | patch | Unknown-version cases all use two Pods, and v1 refuses those too. A clean one-Pod v1 control accepts; include it when checking unknown-version refusal to distinguish closed dispatch from fallback. |
| Blind 4: arbitrary renamed-Pod fixture | low | rejected | Samples use three fixture names, but the unchanged strict inspector derives all names directly from observations and the v2 wrapper introduces no Pod-name constant. All observation permutations and cross-Pod substitutions are covered. Additional source/receipt renaming machinery supplies negligible protection for this dispatch-only change. |
| Blind 5: Kubernetes clock verifier settings | high | rejected outside intent | Confirmed the earlier clock health-check requires both lifecycle verifier settings, absent from the clock Deployment; its startup/readiness probes call `/ready`. This is committed module/clock work, excluded from the offline registration intent and its explicit runtime/Production preservation boundary. Preserve it here as a separate-owner follow-up, with no fix or qualification credit in this build. |
| Blind 6: malformed key and missing epoch clock cases | low | rejected outside intent | The prior clock theory lacks those three cases, although the decoder is inside its fail-closed catch and missing configuration returns unhealthy. This concerns earlier C# readiness work, outside the offline command-inspection intent. |
| Blind 7: wall-clock source fixture scheduling | medium | rejected outside intent | The prior handler samples `UtcNow` separately with 50ms intervals; a scheduling gap can remove their required majority overlap. This earlier C# fixture is outside the offline registration intent and is preserved. |
| Blind 8: reminder state bookkeeping fixture | low | rejected outside intent | The prior reminder test deliberately lacks actor state and expects the post-purge exception; it proves disablement and the remaining record, but cannot prove successful persisted bookkeeping. That runtime test is outside this command-inspection intent. |
| Blind 9: first retry terminal finalization | low | rejected outside intent | The earlier worker test leaves completion state null; `TryFinalizeRetryAsync` then returns false. Its assertions prove second-record disablement, without proving completed first-record disposition. This prior worker behavior is outside the offline registration intent. |
| Blind 10: temporary log references | low | rejected | Raw logs are temporary; the tracked Verification section retains dated exact commands, results and counts. The suggested fix edits this build's spec, which the review workflow expressly rejects as a finding-driven fix. No durable-log authority is claimed. |
| Edge 1: Kubernetes clock verifier settings | high | rejected outside intent | Independently traced the same required-setting guard and absent Deployment environment. The earlier committed runtime change is outside the offline registration intent; retain separate ownership and no Production mutation. |
| Verification 1: Kubernetes clock verifier settings | high | rejected outside intent | Reviewer read the readiness tests and rendered the Production overlay: telemetry has the verifier settings and the clock does not. The test injects them rather than consuming deployed clock configuration. This independently confirmed earlier runtime defect remains outside the approved offline intent. |

All three review layers completed before triage. Findings were classified individually
before grouping; the three surviving test-harness/control defects have separate
root causes and receive direct test-only patches. No findings are deferred by this
build, and no outside-intent file or historical evidence is modified.

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_registration*.py' -v` — focused positive, denial and compatibility checks pass.
- Same command without `-p 'test_registration*.py'` — full affected lane passes without skips.
- `git diff --check` and story-key file-scope/readiness gates — only declared paths, protected state unchanged.

Executed offline on 2026-10-13: the focused command above passed 34 tests in
7.488s; the full command without `-p 'test_registration*.py'` passed 447 tests
in 124.598s. Both returned `OK` with zero failures, errors or skips. Logs are
retained at `/tmp/c1-registration-v2-focused-tests.log` and
`/tmp/c1-registration-v2-interchange-tests.log` for the review handoff.

`git diff --check` exited zero. The following gates selected this bounded
standalone artifact explicitly, using the five changed paths recorded in File
List; they do not claim readiness for the umbrella Story 27.4:

- `python3 tools/check-story-file-scope.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-20 --changed-files-file /tmp/c1-registration-v2-changed-files.txt` — exit 0; final line `Story file scope validation passed.`
- `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-20 --changed-files-file /tmp/c1-registration-v2-changed-files.txt` — exit 0; final line `Story review readiness validation passed.`

The changed-path receipt was derived from `git diff --name-only
42b4dc4d58731d4e9ee777285ba1f65f26e4a0c0` plus `git ls-files --others
--exclude-standard`. An additional read-only preservation comparison against
that HEAD found all twelve manifest paths unchanged and verified the historical
manifest/results section and frozen intent are unchanged. HEAD and root
gitlinks remain unchanged. No staging, commits, pushes or dependency updates
were performed. Implementation is complete; subsequent review owns status
advancement. Production, sprint status, historical evidence and A41 receive no
new acceptance credit.

The workflow's intermediate `in-review` label was rejected by the repository
readiness gate as unrecognized. This artifact uses the repository's canonical
`review` label for the same phase, without synchronizing the umbrella sprint
story. Final completion remains limited to this bounded spec.

Review patches completed offline: the alias scan now covers the shared denial
set and counts a prebound `perf_counter` sentinel; successful v2 provenance
allows only exact local Git argv/environment/launch options; unknown-schema
refusals use an accepting one-Pod v1 control. The edited-file run passed all
34 tests in 6.069s; discovery remains unchanged. Final full verification and the
code-review ledger are recorded below.

Final review verification on 2026-10-13: the full affected command passed
447 tests in 120.796s, zero failures/errors/skips, with its log at
`/tmp/c1-registration-v2-final-interchange-tests.log`. All nineteen new matrix
methods appear as `ok` in that final run. Independent live discovery returned
447/16/18/34 for the affected lane/command scope/provenance scope/focused lane.
All twelve review findings received an individual verdict, and all three
in-scope patches are applied and verified. No deferred entry is created.

Final preservation checks compare the five-path working set against current-start
HEAD, verify all twelve protected manifest paths and the historical manifest/results
section, and compare the frozen intent with its committed approved bytes. All
pass. The umbrella sprint story remains `in-progress`; this spec alone completes.
The earlier Kubernetes clock readiness finding remains explicitly recorded above
for its module/clock owner. No outside-intent runtime change is included.

The final bounded file-scope and review-readiness gates both returned exit 0
with `Story file scope validation passed.` and
`Story review readiness validation passed.`; `git diff --check` also returned
exit 0. Review readiness was checked at canonical `review` before the final
`done` transition. The frozen intent forbids staging, commits and sprint changes,
so this completed spec does not run the generic workflow's commit or umbrella
status synchronization actions.

## File Scope

**Allowed to modify:**

- `tools/access_telemetry_c1_producer_bindings.py`
- `tests/tooling/access_telemetry_c1_interchange/test_registration_commands.py`
- `tests/tooling/access_telemetry_c1_interchange/test_registration_provenance.py`
- `docs/operations/c1-producer-binding-inspection-contract.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-20.md`

## File List

- `tools/access_telemetry_c1_producer_bindings.py`
- `tests/tooling/access_telemetry_c1_interchange/test_registration_commands.py`
- `tests/tooling/access_telemetry_c1_interchange/test_registration_provenance.py`
- `docs/operations/c1-producer-binding-inspection-contract.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-20.md`

### File List Exclusions

- `module/hexalith.module-manifest.v2.json` — owner: Jérôme Piquot; pre-existing module/clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d` and `7034b839791fd8dbe0c6aff50b907fbecd90701a`, outside this spec.
- `src/Hexalith.Memories.AccessTelemetry.Clock/ClockSigningReadinessHealthCheck.cs` — owner: Jérôme Piquot; pre-existing clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d`, outside this spec.
- `src/Hexalith.Memories.AppHost/Program.cs` — owner: Jérôme Piquot; pre-existing module/clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d`, outside this spec.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/ClockSigningReadinessHealthCheckTests.cs` — owner: Jérôme Piquot; pre-existing clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d`, outside this spec.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/ConfiguredUtcSourceHandler.cs` — owner: Jérôme Piquot; pre-existing clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d` and `7034b839791fd8dbe0c6aff50b907fbecd90701a`, outside this spec.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/InvalidSigner.cs` — owner: Jérôme Piquot; pre-existing clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d`, outside this spec.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/StaleSource.cs` — owner: Jérôme Piquot; pre-existing clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d`, outside this spec.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/AccessTelemetryRetentionDisableTests.cs` — owner: Jérôme Piquot; pre-existing clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d`, outside this spec.
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` — owner: Jérôme Piquot; pre-existing module/clock work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d` and `7034b839791fd8dbe0c6aff50b907fbecd90701a`, outside this spec.
- `tests/Hexalith.Memories.Server.Tests/Hosting/StartupWorkerDisableRegistrationTests.cs` — owner: Jérôme Piquot; pre-existing workspace work in `7034b839791fd8dbe0c6aff50b907fbecd90701a`, outside this spec.
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedServiceTests.cs` — owner: Jérôme Piquot; pre-existing workspace work in `a2825bbf7d5da9c71b4f7096032fad67b03aa74d`, outside this spec.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-13 | create-story | Prepare versioned command inspection; owner: current build agent. Offline reproduction: v1 returns inspection for one Pod; two/three Pods refuse `command-contract-unsupported`. No live observation or gate credit. | Phase +0, cumulative +0; interchange 428 -> 428 unittest methods. New command-test scope baseline 0 methods. Command: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; print(unittest.TestLoader().discover('tests/tooling/access_telemetry_c1_interchange').countTestCases())"` — 428. | Matched 1/1 against `git diff --name-status 24b1fa7408eabc1db24b7536fd35f575c7a572a7` plus `git ls-files --others --exclude-standard`: this spec only, no exclusions. |
| 2026-10-13 | dev-story | Implement explicit v2 strict grammar dispatch, independent multi-Pod/source/Ref fixtures, counted dependency denial and isolated local corroboration; document unchanged assurance limits. Owner: current implementation agent. No live observation or gate credit. | Phase +19, cumulative +19; interchange 428 -> 447 unittest methods. New `test_registration_commands.py` scope 0 -> 16 methods (+16); `test_registration_provenance.py` 15 -> 18 methods (+3); focused registration scope 15 -> 34 methods (+19). External Python discovery delta +0. Evidence: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_interchange',pattern='test_registration_commands.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_interchange',pattern='test_registration_provenance.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_interchange',pattern='test_registration*.py').countTestCases())"` — 447, 16, 18, 34. | Matched 5/5 against `git diff --name-status 24b1fa7408eabc1db24b7536fd35f575c7a572a7` plus `git ls-files --others --exclude-standard`, excluding exactly the eleven named pre-existing committed paths above. Additional `git diff --name-status 42b4dc4d58731d4e9ee777285ba1f65f26e4a0c0` plus untracked inventory contains only the five declared paths. |
| 2026-10-13 | code-review | All three independent review layers and all in-scope chunks completed. Owner: current build reviewer. Classified all twelve findings; applied three direct test-only corrections, rejected the negligible renamed-Pod suggestion, spec-edit suggestion and earlier outside-intent findings. No deferred entries; earlier clock deployment defect retains separate ownership. Final matrix audit and preservation checks pass. | Phase +0, cumulative +19; interchange 447 -> 447 unittest methods; command scope 16 -> 16; provenance 18 -> 18; focused registration 34 -> 34. External Python delta +0. Exact discovery: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; loader=unittest.TestLoader(); print(loader.discover('tests/tooling/access_telemetry_c1_interchange').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_interchange',pattern='test_registration_commands.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_interchange',pattern='test_registration_provenance.py').countTestCases()); print(loader.discover('tests/tooling/access_telemetry_c1_interchange',pattern='test_registration*.py').countTestCases())"` — 447, 16, 18, 34. Final full lane: 447 passed, zero failures/errors/skips; focused patches: 34 passed. | Matched 5/5 against `git diff --name-status 24b1fa7408eabc1db24b7536fd35f575c7a572a7` plus `git ls-files --others --exclude-standard`, excluding the eleven named pre-existing committed paths. Additional current-start comparison `git diff --name-status 42b4dc4d58731d4e9ee777285ba1f65f26e4a0c0` plus untracked inventory equals File List exactly. No generated or session artifact enters scope. |
