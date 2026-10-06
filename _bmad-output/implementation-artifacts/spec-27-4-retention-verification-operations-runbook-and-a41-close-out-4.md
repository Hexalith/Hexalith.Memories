---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
repository_status: 'reviewed-awaiting-operator'
route: 'dispatch'
baseline_commit: 'b7a4377aaebce4b2d5f9e66d583ce9022f2f9fca'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - '_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/adoption.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Repository producers and the PG2 handoff exist, but accepted C1 evidence and live execution inputs are missing. The canonical offline command omits the five A41 guards.

**Approach:** Correct the offline handoff; reuse existing qualification/close-out guards after accepted prerequisites. Record blockers and retain pending live tasks.

## Boundaries & Constraints

**Always:** Use PG-ONPREM-2 SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`. Require approved/done owners, 25 distinct passed C1 artifacts, PG2 C1.15 renewal, C1.16 linkage, independent Operations/Security approvals, authorized non-Production scope and external custody. Preserve disabled Production writes; restore disabled gate, released Lease and zero lifecycle/clock replicas. A41 closes only after terminal/postflight/remote proof.

**Never:** Credit PG1/fixtures, invent approvals/owners, implement the separate C1.15 renewal, contact unauthorized targets, expose secrets/content, enable Production, or alter protected history/four-path scope. Staging/commits/publication require explicit authorization after review.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Offline | Missing live prerequisites | Both architecture selectors execute; live states remain pending | Record exact blockers |
| Qualification | Accepted PG2 predecessor and scoped target | Immutable C0/C2-C4 packets and cleanup proof | Refuse invalid inputs before enablement |
| Close-out | Accepted C0-C6, terminal and publication authority | Authenticated four-path transition and remote containment | Refuse missing proof or drift |

## Decisions

- 2026-10-06: Administrator approved the recommended offline correction and continuation. Complete the repository handoff and review; leave live qualification/publication tasks pending and Story 27.4 incomplete/A41 open.

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1:27` — PG2 C1.15 refuses execution before target/output creation. Renewal is separately owned; C1.16 linkage/review is pending and 23 gates remain unregistered.
- `tools/verify_access_telemetry_lifecycle.py:2642` — reuse predecessor, producer, terminal, preflight/postflight and publication validators without relaxing gates.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` and `AccessTelemetryA41CloseOutTests.cs` in that directory — twelve retention-decision and five A41 guards; reuse unchanged.
- `docs/operations/access-telemetry-lifecycle.md` — current PG2 producer/close-out procedure already implemented.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — change the offline command to Debug/source references and both exact architecture selectors below. Record counts/blockers; preserve all pending matrix states.
- [x] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — run existing scenario/denial/cleanup/close-out fixtures and both architecture classes; no new tests for this command correction.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — after accepted prerequisites/authority, run existing C0/C2-C4 producers and retain cleanup/evidence packets. Otherwise leave pending.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — after independent C5/C6 and terminal validation, prepare exact four-path close-out; stage/commit/publish only with explicit authority after review. Require postflight/remote containment; otherwise leave pending. The separate sprint action follows verified publication.

**Acceptance Criteria:**
- Given the offline handoff, when its commands run, then twelve retention-decision and five A41 guards pass with no skips while live states remain pending.
- Given missing, historical or unaccepted prerequisites, when qualification is evaluated, then exact blockers are recorded without target operations or A41 mutation.
- Given accepted PG2 evidence and publication authority, when close-out runs, then cleanup, C0-C6, terminal, exact postflight and remote containment pass before A41 resolves.

## Implementation Notes

### 2026-10-06 approved offline implementation

The canonical evidence handoff now builds Debug with source references and runs
both exact architecture selectors. It records 79 lifecycle fixtures and 17
architecture guards, all passing with no skips, plus a build with zero warnings
and errors. No production code or new tests were required. The C0-C6, identity
and close-out tables are unchanged. No workspace staging, commit, publication or
live target operation occurred.

Matrix audit: the offline row is covered by the two architecture classes (12 + 5
passing guards); qualification is covered by
`test_complete_c2_c3_and_c4_packets_validate`,
`test_c1_requires_unique_25_gate_artifacts_disabled_production_and_authorization`,
`test_current_target_predecessor_approvals_and_c0_reject_old_or_mixed_evidence`,
`test_producer_refuses_unverifiable_target_identity_without_mutating_target`, and
the two `test_producer_restores_disabled_state_when_*` fixtures; close-out is
covered by `test_registered_producers_and_complete_close_out_chain` and
`test_mutation_manifest_is_exact_and_verifier_owned`. All ran in the passing
79-test suite. These fixtures establish offline behavior only.

The last two tasks remain conditional and unchecked under the approved offline
scope. PG2 C1.15 renewal, C1.16 capture/linkage/review, 23 unregistered gates,
accepted external predecessor and target/evidence/credential-file/fault authority
are absent. No current-profile C0-C6/terminal/remote chain is available. Story
27.4 therefore remains incomplete/in-progress; A41 and its sprint action remain
open, and Production lifecycle writes remain disabled. The live acceptance
criterion has not been exercised.

## Spec Change Log

## Review Triage Log

### 2026-10-06 offline review

All three layers completed: edge-case review returned no findings after an
independent 79/79 + 17/17 run; verification-gap review reported no gaps.
Blind-hunter findings were verified individually before grouping:

| Finding | Verdict | Route | Evidence / disposition |
| :------ | :------ | :---- | :--------------------- |
| BH1: commands continue after failure | medium | patch | The Bash block lacks failure handling; a source-reference build failure can fall through to a stale DLL and the final successful whitespace command. Make the scoped block fail immediately. |
| BH2: success can mean zero tests or skipped guards | medium | patch | Runner help confirms skips are accepted by default; the reviewer executed an unmatched selector returning success with zero tests. Add failSkips and validate both per-class counts/pass states from the actual result report. |
| BH3: dated claims have no retained run/source receipt | low | patch | Only summarized output was retained in prose. Preserve local command logs/result XML and source/assembly/dependency identity alongside the offline check; do not grant live gate credit. |
| BH4: source-reference setup prerequisite omitted | low | patch | Directory.Build.props CheckSubmodules rejects missing root dependencies when UseHexalithProjectReferences=true. Add the README setup link and nonrecursive root-only guidance. |

The four findings have distinct root causes; each receives a direct correction
within the canonical evidence handoff. No intent gap or spec loopback is needed.


Final review resolution: all four BH patches are implemented in the evidence
handoff. Its scoped fail-fast execution, failSkips plus exact XML counts, local
run/source receipts and setup link passed focused positive/negative checks.
Parent executed the complete revised block successfully: 79 lifecycle tests,
17 architecture guards with exact 12/5 counts and all Pass, zero build warnings
or errors, and clean whitespace. All ten logged commands exited zero. Receipt:
`/tmp/story-27-4-offline.4PZnqjZw`; focused refusal receipts:
`/tmp/story-27-4-focused.EGvAOLs8`. The XML, baseline revision, built-assembly hash
and preserved matrix tables were checked independently. Review has no unresolved
findings within the approved offline scope; nothing was added to deferred-work.

The repository portion is reviewed-awaiting-operator. Full spec/story completion
and sprint advancement remain withheld because the live tasks are still pending.
The workflow's default done/local-commit actions do not apply to this approved
partial scope: the frozen decision keeps 27.4 incomplete and requires separate
post-review authorization for staging/committing/publication. No such action ran.

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py'` — offline fixtures pass.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true` — zero warnings/errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo` — seventeen guards pass.
- `git diff --check` — clean whitespace.

Planning checks, 2026-10-06: lifecycle 79/79, Debug/source-reference build zero warnings/errors, architecture 17/17 with no skips/failures. No external predecessor or target authority supplied; no live operations/A41 mutation/publication. Frozen intent preserved. The recommended offline scope was approved by Administrator on 2026-10-06.
