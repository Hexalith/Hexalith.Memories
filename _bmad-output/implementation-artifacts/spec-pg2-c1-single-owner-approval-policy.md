---
title: 'PG2 C1 bundle policy: named owner may review both roles'
type: 'feature'
created: '2026-10-07'
status: 'done'
route: 'oneshot'
baseline_commit: '528baca2631df26e5fdcd408e830b0e1e45a60ad'
review_loop_iteration: 1
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The owner explicitly approved continuing the work, designated Jérôme Piquot as approver, and selected “Allow Jérôme to approve both roles”. Adopt a named-owner exception to the proposed authenticated C1 bundle's Operations/Security principal separation. The read-only authenticated GitHub profile lookup identifies account `jpiquot`, stable ID `6775094`; its canonical policy subject is `github:user:6775094`.

Implement a pure, bounded principal-separation predicate in `tools/access_telemetry_c1_approval_policy.py` and focused tests in the existing C1 interchange test directory. It accepts two distinct canonical GitHub user principals, or the exact owner principal for both roles, only when neither is any capture producer principal. Require a nonempty immutable producer-principal set, reject malformed/alias/display-name identities and content-free errors. This predicate consumes facts from the future authenticated authority adapter; it does not authenticate those facts, authorize a role or accept an artifact.

Reconcile the proposed bundle schema, authority contract, implementation P4 record and operations runbook with that owner decision. Retain exactly two separately attributable role decisions and distinct authenticated receipts/artifact references binding the same immutable manifest. Do not merge decisions, reuse an approval for another gate, invent another username, weaken producer/reviewer independence, or mark any actual gate approved. The approved exception reduces separation between the two review roles and is limited to the named owner; reconsider it before Production activation or an account/role/producer identity change.

Keep the legacy unversioned predecessor's different-reviewer guard intact: it has no authenticated identity facts and cannot safely consume this exception. Application to actual execution requires the separate authenticated interchange/provider/consumer migration. Leave P1-P3/P5-P7, gate registration, historical captures, Story 27.4/sprint/A41 and disabled Production states unchanged. No live collector, deployment or fault/purge action; no staging, commit or publication. Review and execute meaningful focused positive/negative cases.

</frozen-after-approval>

## Implementation Notes

- Authorization is already supplied by the owner's explicit policy choice; no repeated approval request is needed for this bounded implementation.
- This is a separate principal-policy prerequisite, not full authenticated interchange or an authentication implementation. The common wire foundation remains a separate completed spec.
- Local Aspire startup/inspection/stop was completed earlier in this session. The isolated qualification overlay rendered successfully with an inert lifecycle boundary; no deployment was applied.
- No code change to the legacy validator is made because its label-only structure cannot authenticate the owner. Actual single-owner execution remains held until strict authority and versioned migration exist.

- Integration baseline correction: the owner externally committed the reviewed wire foundation and advanced three root gitlinks as `528baca2` during this turn. The policy baseline is updated to that commit so cumulative review excludes those external changes. The initial session baseline and earlier receipts remain historical evidence; no external change was reverted or authored by this agent.

## File Scope

Allowed to modify:

- `tools/access_telemetry_c1_approval_policy.py`
- `tests/tooling/access_telemetry_c1_interchange/test_approval_policy.py`
- `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py`
- `docs/operations/access-telemetry-lifecycle.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/schemas.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/authority-and-sessions.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/failure-modes.md`
- `_bmad-output/implementation-artifacts/spec-pg2-c1-single-owner-approval-policy.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-7.md`

Read/verify only:

- `tools/verify_access_telemetry_lifecycle.py`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/project-context.md`
- `docs/dev/telemetry.md`
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`
- `deploy/kubernetes/**`
- `references/**`

## Implementation record and limits

- Implemented `validate_bundle_principal_separation` as a pure predicate over canonical GitHub numeric principal IDs. Principal strings are bounded and signed-64-bit IDs are positive; the immutable producer set contains 1-25 principals. The exception matches exactly `github:user:6775094`, not the owner's name/login, leading-zero aliases or another account. Errors are content-free.
- Twelve focused policy tests cover distinct reviewers, the named owner in both/either role, denial for another repeated account, both-role producer conflicts (including owner), missing/mutable/excess producer sets, malformed/alias/display-name principals, numeric boundaries, safe diagnostics and zero filesystem/network/process calls. The existing interchange CI discovery automatically includes the added fixture file.
- Added one real legacy-validator regression to the existing lifecycle lane. It first validates an ordinary complete structural fixture, accepts the owner in the pure separation predicate, then proves the same owner label in both legacy approval roles is still refused. This demonstrates that the exception is not an operational label bypass.
- Reconciled schema, authority, P4 implementation/readiness and negative/positive verification contracts plus the operations runbook. Only the owner's reviewer-role separation decision is adopted. Exactly two distinct authenticated role decisions/receipts, role/scope/time/revocation checks and producer separation remain required. Gate approvals C1.23/C1.24/C1.25 and C5/C6 remain separately attributable.
- Historical Context Classification: prior captures, older story intents and legacy labels remain historical/reference-only; they do not supply current-profile authenticated approvals. Slice Proof: one named-owner principal-policy outcome, with no accepting provider, deployment, session, gate registration or live qualification absorbed here.

## File List

The exact ten allowed paths in File Scope above comprise this change. Seven are tracked modifications and three are new files. The separately committed wire foundation and three owner-updated gitlinks are excluded by the `528baca2` baseline. No protected story/sprint/A41/history/deployment path was changed.

## Spec Change Log

2026-10-07: The owner explicitly selected “Allow Jérôme to approve both roles”, renegotiating reviewer-role separation. The supporting Story 27.4 draft records that decision while retaining all actual evidence, authority and live prerequisites. This spec's intent implements only the named-owner exception. File Scope and the integration baseline were clarified during review; no further approval or scope expansion was inferred.

## Review Triage Log

The configured context-free Blind Hunter reported five findings, each verified and patched. Other review layers were not configured for this rendered oneshot route. Its follow-up confirmed all five fixes and independently passed 12 policy tests and 3 C1 regressions with no failures/skips. A final independent check after an explicit verifier reload confirmed the new legacy refusal assertion remained specific and passed 1/1. No review finding was deferred.

| Finding | Verdict / disposition | Evidence and correction |
| --- | --- | --- |
| B1 — runbook still mandated two named people | medium / patch | The ownership paragraph contradicted step 5. Updated it to distinguish the named-owner future authenticated C1 exception, required producer separation and unchanged legacy/other checkpoint requirements. Prevents contradictory operator instructions. |
| B2 — verification matrix rejected every same-principal bundle | medium / patch | N13 and the positive case omitted the authorized exception. Added owner positive, non-owner repeated-principal negative, copied unauthenticated owner-label negative and owner/producer conflict. Provider-level cases remain explicitly unimplemented until I3. Prevents the policy contract testing the wrong rule. |
| B3 — File Scope missing | medium / patch | The file-scope gate exited 1 with no parseable scope. Added the exact ten authorized paths and read-only exclusions. The final guard passed all ten, making the bounded change reviewable. |
| B4 — old baseline included external work | medium / patch | The owner committed the foundation and three gitlinks during implementation. Updated the policy baseline to `528baca2631df26e5fdcd408e830b0e1e45a60ad`, preserving the external changes and their earlier receipts. Prevents cumulative tooling attributing them to this policy slice. |
| B5 — no legacy denial regression | medium / patch | Added the actual legacy validator test, which proves only the future pure predicate accepts the owner and existing label-only execution still refuses. Prevents an accidental owner-label bypass. |

The first full lifecycle rerun exposed a test issue caused by another fixture reloading the verifier: the new test caught a stale imported exception class. The validator correctly refused. The test now calls the current module object and catches its current specific `EvidenceValidationError`, matching `independent reviewers`. The final full 80-test rerun and explicit-reload independent check passed. Preserve the initial failing receipt rather than describing it as green.

## Verification

Final receipt: `/tmp/pg2-c1-owner-policy-final-c55cum0p/manifest.json`. It retains exact argv/environment/exit codes, source hashes, protected normalized blobs, 12+5 result XML and both the original failed lifecycle log and successful corrected rerun. Source HEAD before/after is `528baca2631df26e5fdcd408e830b0e1e45a60ad`.

| Command / check | Result |
| --- | --- |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p "test_*.py" -v` | Exit 0; 44 passed, no failures/errors/skips (32 wire + 12 policy). |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p "test_*.py" -v` | Corrected rerun exit 0; 80 passed, no failures/errors/skips, 37.315 seconds. |
| `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true` | Exit 0; zero warnings/errors, 10.85 seconds, after the owner's dependency advances. |
| `env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo -failSkips -result-xml /tmp/pg2-c1-owner-policy-final-c55cum0p/retention-architecture.xml` | Exit 0; exact 12+5 cases all Pass; zero errors/failures/skips/not-run. |
| `python3 tools/check-story-file-scope.py --story-key spec-pg2-c1-single-owner-approval-policy --changed-files-file /tmp/pg2-c1-owner-policy-final-c55cum0p/file-list.txt` | Exit 0; ten paths in scope. |
| `git diff --check`, new-file whitespace and line endings | Exit 0; Python LF, Markdown CRLF. |
| Protected normalized Git blobs and index | Six protected files unchanged; index empty; no agent commit or publication. |

The separate baseline CI inventory failure remains: `tests/tooling/access_telemetry_c1_sql_contract is executed by no workflow step, so reverting anything it guards ships green.` The exact failing command and existing ownership deferral are retained in the wire-primitives spec and `/tmp/pg2-c1-wire-final-qyqhy9wi/ci-inventory.log`. The policy fixtures are correctly discovered; this run does not claim green whole CI.

This supporting policy outcome is complete. Full authenticated interchange/provider integration, 23 held gate owners and actual same-session evidence/authority remain prerequisites. Story 27.4 remains `in-progress`, A41/action remains open and Production writes remain disabled. No actual evidence review, new session, deployed accepting entry, target mutation, fault/purge, stage, commit or publication was performed by this agent.
