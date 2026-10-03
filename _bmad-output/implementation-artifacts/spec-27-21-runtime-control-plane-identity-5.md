---
title: 'Story 27.21 lossless metadata JSON validation'
type: 'bugfix'
created: '2026-10-03'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'de3c6c2326b038cb35937ca1e4c9995473756a53'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.21's C1.15 producer accepts malformed metadata after PowerShell conversion overwrites duplicate JSON properties or unwraps a singleton root array. The latest independent review deferred both defects, and current fake-target reproductions confirm exit-zero observations, including overwritten encoded secrets.

**Approach:** Validate bounded metadata JSON before conversion: require an object root and reject repeated decoded property names throughout nested objects and arrays. Retain existing secret scans, authentication, depth/output bounds, immutable packets, and allowlisted hashes; use the existing constant malformed-metadata blocker. Add whole-run regressions and record the two parser follow-ups as resolved after verification. This is repository-only producer hardening: the parent story/sprint remain in-progress, C1.15 pending/not complete, and DW-718 capture/review awaiting-operator. Production writes, manifests, other gates, Story 27.4, A41, dependencies, and submodules remain unchanged.

</frozen-after-approval>

## Implementation Notes

- Route facts: no unresolved intent choices; no irreversible operations; footprint is the producer, its existing fake-target test file, this spec, and bounded append-only evidence in the story/deferred ledger. The two defects share the lossy metadata conversion boundary and one user-facing fail-closed goal.
- Investigation: metadata conversion is at `tools/verify-access-telemetry-c1.ps1:499`; reuse the existing decoded secret walk and `RuntimeControlPlaneIdentityTests.run_gate`/`metadataRaw`. `System.Text.Json.JsonDocument` preserves repeated decoded names; a per-object ordinal name set avoids treating identical names in separate objects as duplicates. Dispose the temporary document; persist/hash only the existing allowlisted projection.
- Current target checks: `kubectl --context jpiquot@local -n hexalith-memories get pods -l app.kubernetes.io/name=memories-access-telemetry --request-timeout=10s -o json` returned `items: []`; the deployment replica query returned zero for both lifecycle Deployments. Explicit alpha-option names remain absent from the tracked Production deployment input. No capture packet was synthesized or cluster mutation performed.
- Keep the original operator handoff, completed DW-719 spec, and sprint status unchanged. The user-supplied AGENTS baseline does not authorize staging/committing/pushing merely to record this implementation, so leave changes uncommitted.
- Verification lane: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v`; also run the Story 27.21 slice guard and whitespace checks. Review the final diff with an independent blind reviewer.

- Baseline fixture command passed 24/24 methods in 227.968 seconds (`/tmp/c1-parser-baseline.log`). Before the guard, new duplicate tests failed eight subcases with exit-zero observations; the new root tests failed five subcases (three accepted array roots and two generic blockers instead of the malformed-JSON code). Red evidence: `/tmp/c1-parser-red.log` and `/tmp/c1-parser-root-red.log`. Three regression methods bring discovery to 27.
- Implemented a disposable, depth-30 JSON document preflight and recursive per-object decoded-name uniqueness check. Constant `malformed-metadata-json` failures occur before conversion, projection hashing, alpha probing, or target recheck; existing raw/decoded/projection secret scans remain intact.
- Aspire baseline command `aspire start --non-interactive --isolated --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` exited 2 after its 120-second startup timeout, with output stuck at `Determining projects to restore...` and an OpenSSL certificate trust warning. `aspire describe --non-interactive --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` and subsequent `aspire stop` reported no running AppHost. The tooling fixture lane, PowerShell syntax parser, Story 27.21 slice guard, and whitespace checks are independently runnable.

- First full verification discovered 27 methods; 26 passed. The empty-array subcase failed only because the regression helper searched the complete JSON packet for `[]`, which is normal empty-collection syntax. Corrected that assertion to check console/command strings; packet checks still require the constant safe blocker, empty pod observations, secret exclusion, immutable files, and no metadata provenance or raw-response hash. Rerunning both affected regression methods.
- Concurrent user-owned staged additions appeared after the clean-start check: `.gitmodules` and the new `references/Hexalith.McpCli` / `references/Hexalith.Platform` gitlinks. They are excluded from this slice and remain untouched.

- Independent blind review returned four findings. (1) `medium`, patch: malformed Unicode property names decode to null through the JSON-document accessor but to U+FFFD during conversion; two whole-run review probes confirmed secret overwrite. Reject undecodable names and regress high/low surrogate collisions. (2) `low`, patch: empty/whitespace responses bypass the normalized parser failure at mandatory string binding; allow empty strings into the parser and regress both cases. (3) `low`, reject: exact depth-boundary regression coverage is absent; direct runtime comparison confirmed both parsers accept 30 nested objects and reject 31. Existing benign/depth-overflow fixtures also pass; adding a separate regression is beyond a direct correction for this rare case. (4) `low`, reject pending direct evidence: malformed-second-pod coverage would require broadening the single-pod helper; verify the actual two-pod path before finalizing this verdict.
- The affected duplicate/root regression methods passed 2/2 in 30.999 seconds after the assertion correction (`/tmp/c1-parser-focused-verification.log`). After the two review patches, final directory-wide verification is running against the complete 27-method suite. No secret-policy expansion or additional gate mode was introduced.

- Additional whole-run fake-target evidence: a clean first pod followed by malformed Unicode metadata on the second exits nonzero/blocked, keeps final pod observations empty, retains only the first pod's allowlisted projection hash, and never probes alpha settings on the second. Complete valid metadata with 30 object/array nesting levels is observed; increasing nesting to 31 gives the constant malformed blocker. Both checks use synthetic fixtures only and change no acceptance status.

## Review Triage Log

- Blind 1 — `medium`, `patch`: two independent synthetic whole-run observations confirmed undecodable property names bypass uniqueness before conversion collapses them. Reject null decoded names; high/low surrogate collision subcases now reject before projection.
- Blind 2 — `low`, `patch`: empty and whitespace-only responses reached the outer generic blocker through mandatory parameter binding. Allow empty strings into bounded parsing; both now require `malformed-metadata-json`. For empty input, shared empty-stderr hashes and empty packet collections are legitimate, so the helper checks metadata provenance rather than banning empty syntax.
- Blind 3 — `low`, `reject`: permanent exact depth-boundary regression coverage is absent, but direct runtime comparison and whole-run fixtures confirmed depth 30 succeeds and 31 blocks. Existing benign and excessive-depth regression methods pass; a new test is beyond a direct correction for this rare coverage improvement.
- Blind 4 — `low`, `reject`: the new helper covers first-pod corruption only. A complete two-pod probe confirmed malformed second-pod metadata blocks, leaves final observations empty, retains only the clean first-pod projection, and prevents later second-pod alpha probing. Expanding the helper or adding another test exceeds a direct correction; no behavior defect was demonstrated.

## Completion

- Final directory-wide command `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` passed all 27 methods in 246.606 seconds; output is `/tmp/c1-parser-final-verification.log`. Three new methods (`24 -> 27`) plus review subcases resolve both recorded parser follow-ups. The initial assertion failure and its corrected focused rerun remain documented above.
- Final governance records append the tested producer behavior, exact counts, five-file scope, and the two dated follow-up resolutions. The parent story and sprint remain in-progress; original operator handoff, DW-718/DW-645, other gates, Production manifests/writes, Story 27.4, A41, dependencies, and submodules are unchanged. No review finding remains that requires a new deferred entry.
- This repository fix is done. Generic workflow sprint-review promotion would contradict the human-owned running-target completion contract, and generic committing would conflict with the user-supplied AGENTS safeguard; neither operation was performed.

- Post-documentation checks: the focused unavailable-target/governance method passed 1/1; Story 27.21 slice guard and tracked/new-spec whitespace checks passed. Parent/operator/DW-719/context invariants, DW-17/645/718 open states, and concurrent user staging were verified unchanged. The five current-pass paths exactly match the story File List.
- Additional legacy scope check: `python3 tools/check-story-file-scope.py --story-key 27-21-runtime-and-control-plane-identity --changed-file tools/verify-access-telemetry-c1.ps1 --changed-file tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py --changed-file _bmad-output/implementation-artifacts/deferred-work.md --changed-file _bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md --changed-file _bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-5.md` exited 1 with `Story file has no parseable File Scope section or has an empty allowed scope`. This legacy story has a File List rather than the newer File Scope schema. No guard was weakened; the required slice guard passes and direct File List reconciliation verified 5/5 paths.
