---
title: 'Story 27.21 decoded metadata secret rejection'
type: 'bugfix'
created: '2026-10-03'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '42995692634af9ba35982ec0e1aedec4f5576e71'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-27-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The C1.15 collector scans raw metadata and its allowlisted projection, but misses secrets decoded in discarded fields. A DW-719 reproduction with a Unicode-escaped canary in a nested diagnostic returns exit zero, `producerStatus: observed`, and no blocker.

**Approach:** The proposed repository slice scans the entire decoded object before extracting/hashing observations, using the existing secret predicate and safe blocker. Real capture and gate review remain operator-owned.

## Boundaries & Constraints

**Always:** Preserve literal C1.15 / PG-ONPREM-1 dispatch, context `jpiquot@local`, namespace `hexalith-memories`, lifecycle-only selection, in-container authentication, output/time/depth bounds, ordinal identity preflight/recheck, immutable packets, and allowlisted provenance. Retain raw/projection scans and constant `secret-shaped-output` errors. Packets remain `gateStatus: not-evaluated`, `productionGatePassed: false`; the story stays `in-progress` with C1.15 pending/not complete.

**Never:** Mutate a cluster or Production manifests, expand secret classifications, persist/hash decoded diagnostics, synthesize real artifacts, close DW-718/DW-645 or A41, advance Story 27.4, change dependencies, or stage/commit/push work.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior | Error handling |
|----------|-------|-------------------|----------------|
| Benign extras | Unknown nested objects/arrays, strings, null, numbers, booleans, empty collections | Existing complete observation and projection hashes | No false rejection |
| Encoded secret | Unicode-escaped canary or recognized token in discarded fields/arrays | Reject before projection/alpha probe | Nonzero exit; constant safe blocker |
| Encoded credential key | Escaped recognized property with nonempty credential | Existing secret semantics apply after decoding | No secret in output or provenance |
| Invalid metadata | Malformed, excessive, missing identity, or raw secret | Existing bounded failure behavior | No successful observation |

**Decision (2026-10-03):** The user selected the recommended DW-719 repository fix and authorized implementation; DW-718 capture remains deferred.

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1:49` — reuse `Assert-SecretSafeOutput`; scan decoded metadata immediately after parsing at line 499, outside the malformed-JSON catch. Preserve projection/hash at line 570; never hash the full object.
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py:163` — reuse `run_gate` and `metadataRaw`; extend secret tests at line 731. Ensure single-backslash JSON Unicode escapes actually decode to the intended values.
- `_bmad-output/implementation-artifacts/deferred-work.md` — DW-719 starts open and closes only after passing verification; DW-718/DW-645 are separate.
- `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` — append evidence/phase accounting; preserve historical rows and pending Slice Proof.
- `_bmad-output/implementation-artifacts/epic-27-context.md` — regenerated this run; preserve tested ownership wording and safeguards.
- `spec-27-21-runtime-control-plane-identity.md` in the implementation-artifacts directory — authoritative `awaiting-operator` handoff, unchanged. Live checks on 2026-10-03 found no lifecycle pods and both Deployments at zero replicas; real C1.15 artifacts are absent.

## Tasks & Acceptance

**Execution:**
- [x] `tools/verify-access-telemetry-c1.ps1` — check all decoded names/values before extraction; handle benign scalar/null/array metadata and preserve the existing blocker.
- [x] `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` — regress encoded ignored values, nested arrays/objects, escaped credential names, and token shapes; accept harmless extras. Assert safe packets/console/ledgers, no full-object source hash, and no later alpha probe after rejection.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` — close only DW-719 after passing verification, retaining origin and dated evidence.
- [x] `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` — append current counts and scoped file accounting without promoting story/gate status.

**Acceptance Criteria:**
- Given encoded secrets beyond the allowlist, when C1.15 runs, then rejection precedes projection and no secret or discarded metadata enters packets, console, or provenance hashes.
- Given complete benign metadata, when capture runs, then original observations, packet immutability, and allowlisted hashes remain valid.
- Given fixture verification alone, when records are reconciled, then only DW-719 closes; DW-718 remains open, C1.15 stays pending/not complete, and the operator handoff/write restrictions remain authoritative.

## Implementation Notes

- Aspire baseline attempt: `aspire start --non-interactive --isolated --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` exited 2 after 120 seconds, stalled at package restore; output also reported an OpenSSL certificate trust warning. `aspire describe --non-interactive --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` reported no running AppHost. This tooling-only slice is verified through its independent fake-target fixture lane.

- User-owned submodule pointer changes in `references/Hexalith.Builds` and `references/Hexalith.Commons` appeared before implementation; preserve them outside this slice. The existing regenerated Epic 27 context and this spec are workflow-owned.
- 2026-10-03 implementation: `Assert-SecretSafeMetadata` walks decoded object names, strings, and nested arrays; string property pairs preserve the unchanged `Assert-SecretSafeOutput` predicate's nonempty credential rule. It runs immediately after parsing, outside the malformed-JSON catch and before extraction. The full decoded object is never serialized, persisted, or hashed. The two encoded-secret regression methods reproduced 14 exit-zero bypasses before the fix and passed afterward; the complete fixture lane passed 24/24 methods (`20 -> 24`, phase delta `+4`).

- Root diff audit: all four execution tasks and three acceptance criteria are satisfied by the scoped implementation. Matrix coverage ran and passed: benign extras (`test_benign_discarded_metadata_preserves_observations_and_allowlisted_hashes`), encoded discarded names/values (`test_encoded_secrets_in_discarded_names_and_values_block_before_projection`), encoded credential keys (`test_encoded_credential_property_names_block_in_discarded_nested_objects`), and invalid metadata (`test_invalid_metadata_types_and_depth_block_without_later_probes` plus existing malformed/output-bound/raw-secret methods).

- Final verification after the three context patches: directory-wide fixture discovery passed all 24 methods in 225.698 seconds; retained output is `/tmp/dw719-final-verification.yj1jmc.log`. All four matrix covering methods reported `ok`. Story-slice scope and tracked/new-spec whitespace checks passed; original operator handoff and sprint status remain byte-equivalent after Git normalization, user-owned submodule revisions remain unchanged, DW-17/645/718 remain open, and no real C1.15 artifact was synthesized.

- Completion: this approved DW-719 spec is done. Its frozen constraints keep the parent story/sprint in-progress with C1.15 pending and prohibit staging/committing/pushing; these take precedence over the workflow generic review-status sync and local-commit default. All three independent review layers returned, three context omissions were patched, and two demonstrated pre-existing parser defects were deferred.

## Spec Change Log

## Review Triage Log

- Blind 1 — `medium`, `defer`: duplicate diagnostic/credential properties erase earlier encoded secret values during `ConvertFrom-Json`, before the decoded walk. Both cases return exit zero/observed with no blockers on the full canonical baseline `42995692634af9ba35982ec0e1aedec4f5576e71` and the current collector; this is a pre-existing parser defect, not caused by DW-719. Group with Edge 1/2.
- Blind 2 — `medium`, `defer`: a singleton root array containing valid metadata is unwrapped and accepted. The same fake-target case returns exit zero/observed on baseline and current scripts; root-shape validation is a pre-existing independent parser defect.
- Blind 3 — `medium`, `patch`: context regeneration omitted the explicit Story 27.3 review-entry prerequisite, allowing a reader to infer that C0 closure alone is the only restriction. Restore the conditional executed-and-independently-accepted checkpoint-gap prerequisite. Do not describe historical CR42-CR46 as currently open: the authoritative Story 27.3 is now done.
- Blind 4 — `medium`, `patch`: the regenerated health/metrics clause omitted the original raw-content prohibition. The separate content-free record rule does not cover those output surfaces; restore the explicit prohibition.
- Blind 5 — `medium`, `patch`: the Story 27.4 dependency clause dropped ownership of deployment-shaped proof, the operations runbook, and A41 close-out. Restore the assignments so implementers retain the deliverable ownership.
- Blind 6 — `low`, `reject`: the new rejection helper targets single-pod pre-projection cases and cannot directly assert a later-pod rejection. A current whole-run two-pod probe confirmed the secret on pod two blocks, excludes the secret, and retains only the clean first-pod projection hash. Generalizing the helper or adding another regression is a low coverage improvement requiring more than a direct correction; no current production behavior defect was demonstrated.
- Blind 7 — `false`, `reject`: the new spec is present in the full audit snapshot and was supplied as claims only to the edge reviewer. Its omission from the blind diff intentionally follows the review workflow's claims separation; the artifact and documentation links are present.
- Edge 1 — `medium`, `defer`: verified duplicate-key secret overwrite bypass on both baseline and current scripts. Same parser root cause and deferred entry as Blind 1; this finding receives its own row before grouping.
- Edge 2 — `medium`, `defer`: the broader encoded-secret claim is not enforced for an earlier overwritten property, confirmed on both scripts. Same pre-existing duplicate-key parser root cause as Blind 1/Edge 1; recorded once after grouping rather than changing the frozen intent.
- Verification-gap reviewer — no findings. Final grouping: three context patches, two pre-existing parser follow-ups, one low coverage rejection, and one false bundle finding; no intent-gap or bad-spec loopback.

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` — 24/24 methods passed in 181.137 seconds; investigation baseline: 20 passed, implementation phase delta `+4`.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-21-runtime-and-control-plane-identity` — one story passes; investigation result: OK.
- `git diff --check` — tracked whitespace clean; check new spec separately with `git diff --no-index --check -- /dev/null <spec-path>`.
