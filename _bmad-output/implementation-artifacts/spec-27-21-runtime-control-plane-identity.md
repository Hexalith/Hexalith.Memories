---
title: 'Story 27.21 runtime and control-plane identity registration'
type: 'feature'
created: '2026-08-03'
status: 'done'
baseline_commit: '3f758f9ab019ca64a793e268470a7e4663cbc1fa'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md'
  - '{project-root}/docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** C1.15 has no owner or complete producer: Story 27.3 observes only a Server-sidecar runtime/digest subset, not the lifecycle workload's Scheduler, actor, feature, or alpha identities.

**Approach:** Add the neutral C1 runner/packet scaffold with only literal mode `C1.15`, prove it with a fake-target fixture, author gate-only Story 27.21, and register it as `backlog` only after every check passes.

## Boundaries & Constraints

**Always:** Target running `memories-access-telemetry` pods only; record runtime version, daprd `imageID`, metadata app ID, Scheduler addresses, actor types, enabled features, and explicit `ComponentIsAlpha`/`AllowAlphaComponent`. Use the in-container `DAPR_API_TOKEN` without exposing it. Emit immutable JSON for `C1.15`/`PG-ONPREM-1` with command/source hashes, `producerStatus`, and `gateStatus: not-evaluated`; incomplete observations block with nonzero exit.

**Ask First:** Runtime mutation, product/manifest change, another gate mode, or substituting Dapr features for component alpha opt-in.

**Never:** Fall back to Server pods, leak a token, reuse the broad Story 27.3 packet as proof, synthesize evidence, claim C1.15 passed, enable writes, advance Story 27.4, close A41, or register partially.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Complete observation | Running lifecycle pod with digest, metadata, and alpha settings | Packet contains every identity and stays `not-evaluated` | Exit zero proves capture only |
| Disabled/partial target | No pod, missing field, wrong app ID, malformed JSON, or token-shaped output | Blocker packet names the safe failure | Exit nonzero; no fallback |
| Unsupported gate | Value other than `C1.15` | No producer runs | Parameter validation fails |

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1` -- dispatcher, packet envelope, and sole C1.15 collector.
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` -- whole-run fake-`kubectl` fixture.
- `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` -- gate-only story and required governance records.
- Epic, sprint, context, and deferred records -- conditional C1.15 ownership reconciliation only.

## Tasks & Acceptance

**Execution:**
- [x] `tools/verify-access-telemetry-c1.ps1` -- implement strict C1.15 dispatch, read-only collection, secret-safe evidence, and fail-closed packet status.
- [x] `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` -- cover complete, blocked, secret-canary, and unsupported-gate runs.
- [x] `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` -- author only C1.15 with the mandated classifications, Slice Proof, canonical Epic AC Verification, literal command, pending review, and `not complete` state.
- [x] `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/implementation-artifacts/epic-27-context.md`, and `_bmad-output/implementation-artifacts/deferred-work.md` -- reconcile C1.15 ownership and add only the `backlog` row after fixture and slice guard pass.

**Acceptance Criteria:**
- Given the complete focused fixture, when the literal C1.15 mode runs, then the immutable packet contains every required observation, excludes the token canary, and states that the gate was not evaluated or passed.
- Given any incomplete target observation, when the mode runs, then it writes a blocker packet, exits nonzero, and never samples a different app.
- Given the completed transaction, when both checks pass, then only Story 27.21 is registered as backlog; Story 27.4, Production writes, and A41 stay unchanged.

## Spec Change Log

- 2026-08-03: Implemented and fixture-proved the sole C1.15 producer, created the one-gate story and governance records, passed the pre-registration transaction gate, and registered Story 27.21 as `backlog`. C1.15 remains `pending` / `not complete`; no Production pass is claimed.
- 2026-08-03: Final review hardened the producer's timeout, metadata schema, stable-pod and multi-pod drift handling, allowlisted hash provenance, packet write protection, authenticated fake-target contract, focused edge coverage, and CI adoption. The focused lane now passes 12/12 and the CI inventory guard passes 1/1; the gate state is unchanged.

## Design Notes

Treat Dapr metadata and lifecycle alpha settings as distinct evidence. Missing explicit alpha values block even when `enabledFeatures` exists. Fixture success proves only producer behavior. Real command:

`pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.15 -ProfileId PG-ONPREM-1 -EvidenceDirectory ./artifacts/access-telemetry-c1/C1.15`

## Dev Notes

### Epic AC Verification

Verified 2026-08-03 against worktree HEAD `3f758f9a`.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "Story 27.21 / C1.15 is unregistered and needs a literal producer" | Existence | `test ! -e tools/verify-access-telemetry-c1.ps1 && ! rg -q '27-21-runtime-and-control-plane-identity' _bmad-output/{planning-artifacts/epics.md,implementation-artifacts/sprint-status.yaml}` | Runner and registration are absent | `confirmed` |
| "The current lifecycle target cannot provide C1.15 running evidence" | Behavioral | `rg -n 'replicas: 0' deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` | Both lifecycle deployments are zero-scaled | `confirmed` |
| "The old packet is not a complete C1.15 producer" | Behavioral | `rg -n 'sidecar_image_digests|daprd_version|scheduler|actor|enabled_features|alpha' tools/verify_access_telemetry_lifecycle.py tests/tooling/access_telemetry_lifecycle/test_adapter_profile.py` | Existing tests cover runtime/digest mechanics but not the four remaining lifecycle observations or literal C1.15 dispatch | `confirmed` |

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'runtime_control_plane_identity_test.py' -v` -- expected: focused runner fixtures all pass.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-21-runtime-and-control-plane-identity` -- expected: exactly one governed story checked, status OK.
- `git diff --check` -- expected: no whitespace errors while unrelated changes remain untouched.

## Suggested Review Order

**Producer boundary**

- Start with the sole-gate dispatcher, bounded process execution, and fail-closed envelope.
  [`verify-access-telemetry-c1.ps1:2`](../../tools/verify-access-telemetry-c1.ps1#L2)

- Trace authenticated metadata projection, strict typing, alpha identity, and multi-pod drift.
  [`verify-access-telemetry-c1.ps1:335`](../../tools/verify-access-telemetry-c1.ps1#L335)

- Confirm observed and blocked packets always leave the Production gate unevaluated.
  [`verify-access-telemetry-c1.ps1:497`](../../tools/verify-access-telemetry-c1.ps1#L497)

**Gate contract and registration**

- Review the frozen one-gate intent before the implementation-shaped records.
  [`spec-27-21-runtime-control-plane-identity.md:13`](spec-27-21-runtime-control-plane-identity.md#L13)

- Verify classification, slice proof, evidence table, and incomplete checkpoint state.
  [`27-21-runtime-and-control-plane-identity.md:5`](27-21-runtime-and-control-plane-identity.md#L5)

- Check the Epic 27 copy registers only Story 27.21 and C1.15.
  [`epics.md:4977`](../planning-artifacts/epics.md#L4977)

- Confirm execution order and development status remain backlog-only.
  [`sprint-status.yaml:157`](sprint-status.yaml#L157)

- Confirm compiled context assigns C1.15 while holding the other gates.
  [`epic-27-context.md:14`](epic-27-context.md#L14)

- Confirm the historical producer gap remains open pending real reviewed evidence.
  [`deferred-work.md:2545`](deferred-work.md#L2545)

**Verification and adoption**

- Inspect whole-run authentication, hash provenance, immutability, and secret-exclusion assertions.
  [`runtime_control_plane_identity_test.py:176`](../../tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py#L176)

- Inspect multi-pod consistency and every gate-relevant drift branch.
  [`runtime_control_plane_identity_test.py:300`](../../tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py#L300)

- Review the complete running-target observation fixture.
  [`c1_15_complete.json:2`](../../tests/tooling/access_telemetry_c1/fixtures/c1_15_complete.json#L2)

- Confirm CI runs the exact focused producer fixture lane.
  [`ci.yml:254`](../../.github/workflows/ci.yml#L254)
