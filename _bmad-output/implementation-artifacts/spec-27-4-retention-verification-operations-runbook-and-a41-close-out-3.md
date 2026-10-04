---
title: 'Story 27.4 current-profile retention verification and A41 close-out'
type: 'feature'
created: '2026-10-04'
baseline_commit: '1624f0281ab07076da32f88081727eba02890a8b'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - '_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/adoption.md'
  - 'docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 has working repository producers but lacks the current-profile, independently accepted running-target chain needed to verify retention and close A41. Its operator runbook example and canonical matrix still display historical PG-ONPREM-1, while the approved verifier requires PG-ONPREM-2.

**Approach:** Align the operator-facing handoff with the approved immutable PG-ONPREM-2 identity. Preserve pending states, verify every prerequisite, then use the existing guarded C0–C6 producers and exact A41 publication chain only when the separately owned C1 gates, target authority, and independent approvals are present.

## Boundaries & Constraints

**Always:** Bind C0–C6 and terminal evidence to PG-ONPREM-2 SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`; retain one distinct passing running-target artifact per C1.1–C1.25 gate, current-profile C1.15 renewal, C1.16 linkage, separate Operations and Security acceptance, an authorized non-Production target, and an external immutable evidence root. Fail closed and preserve Production disabled, gate disabled, Lease released, and lifecycle/clock replicas at zero after every run. Keep A41 open until exact postflight and fetched-remote publication proof passes.

**Never:** Credit the historical PG1 C1.15 capture or offline fixtures as current-profile gate acceptance; invent successor registrations or approvals; contact an unauthorized target; expose credentials or tenant content; enable Production lifecycle writes; alter the four-path A41 mutation boundary or protected historical/sprint records; claim legal or tamper-evident audit retention.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Offline handoff | PG2 approved, live chain absent | Runbook and matrix identify PG2 and retain `operator-pending`; no target call or A41 mutation | Reject PG1 or mixed-profile input before target access |
| Qualification | All distinct PG2 C1 gates, independent approval, authorized target and credentials | Existing C0/C2–C4 commands produce immutable packets and cleanup proof | Record bounded blocker, exit nonzero, restore disabled/zero state |
| Close-out | Passing C0–C6, terminal evidence, approved publication authority | Exact A41 preflight/postflight and remote verification close the carried-forward action | Refuse drift, skipped evidence, unapproved paths, or missing remote containment |

</frozen-after-approval>

## Code Map

- `tools/verify_access_telemetry_lifecycle.py:259` -- current PG2 hash, predecessor validator, C0–C6 checks, and exact A41 mutation/publish guards; reuse without relaxing gates.
- `tools/access_telemetry_producer_common.py` and `tools/access_telemetry_c{2,3,4}_producer.py` -- completed host-side producers and cleanup; do not reimplement.
- `docs/operations/access-telemetry-lifecycle.md:101` -- stale PG1 scenario example and profile ID in the operator procedure.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md:20` -- canonical pending matrix still identifies PG1; update decision identity without implying live proof.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryA41CloseOutTests.cs` -- matrix and protected-path structure guard; add current-profile assertion.
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-2.md` -- historical operator handoff; preserve its frozen decisions.

## Tasks & Acceptance

**Execution:**
- [x] `docs/operations/access-telemetry-lifecycle.md` -- replace the active PG1 scenario/profile example with exact PG2 values from the canonical constructor; keep historical PG1 discussion clearly dated.
- [x] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` -- name PG2 in the canonical decision identity and C0 row; retain all unexecuted rows as pending and cite the adoption receipt as offline evidence only.
- [x] `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryA41CloseOutTests.cs` -- require the matrix identity to equal the current verifier profile and reject historical PG1 as active identity.
- [ ] `tools/verify-access-telemetry-lifecycle.py` -- after all separately owned PG2 C1 gates, authorizations, and external inputs exist, execute reviewed C0/C2–C4 and retain immutable cleanup/evidence packets.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` -- only after C5/C6 and terminal validation, use the verifier's exact four-path A41 preflight/postflight and publication checks. A separately governed `sprint-status.yaml` action follows publication.

**Acceptance Criteria:**
- Given an offline checkout, when focused guards run, then operator examples and the canonical pending matrix use PG2, historical PG1 receives no gate credit, and Production/A41 remain unchanged.
- Given any absent C1 owner, current-profile producer, running-target packet, approval, or target authority, when 27.4 is evaluated, then live execution and close-out are refused with the exact blocker.
- Given all current-profile evidence and approvals, when the existing qualification and close-out chain executes, then C0–C6, cleanup, terminal, exact four-path postflight, and fetched-remote publication are independently verifiable before A41 resolves.

## Implementation Notes

### 2026-10-04 offline implementation and prerequisite audit

The active runbook example and canonical C0–C6 matrix now use the approved
PG-ONPREM-2 profile ID and SHA-256. The matrix cites the dated adoption receipt
as offline evidence only. All live checkpoint rows remain `operator-pending`,
Production writes remain disabled, and DW-17/A41 remains carried-forward/open.
The focused architecture test binds the matrix and runbook identities to the
current verifier selectors and rejects the historical PG1 identity as active.

The C1 predecessor is absent. `sprint-status.yaml` records Story 27.21 `done`
for historical PG1 C1.15 capture, Story 27.22 `backlog` for C1.16, and 27.4
`in-progress`; no other 27.7–27.31 implementation story file is registered.
Fresh PG2 C1.15 producer/review, C1.16 live connection linkage/review, and
twenty-three separately owned gates are outstanding. No authorized current-
profile non-Production target, external evidence root, credentials, complete
C0–C6 chain, or independent current-profile approvals were supplied. Therefore
the last two execution tasks remain unchecked. No target, A41 preflight,
postflight, commit, remote operation, or Production-enable command ran.

Verification: lifecycle tooling `79/79` passed with no skips; Server.Tests
Release build passed with zero warnings/errors; focused
`AccessTelemetryA41CloseOutTests` passed `5/5` with no skips. The matrix rows
are covered offline by the focused identity guard and lifecycle tests for
distinct C1 artifacts/old-profile refusal, producer cleanup and packet
validation, exact four-path mutation, and complete close-out chain. Those
fixtures do not satisfy running-target or publication acceptance criteria.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py'` -- expected: all offline lifecycle tests pass without target access.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release -m:1` -- expected: zero errors and warnings.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests` -- expected: current-profile and protected-path guards pass.
- `git diff --check` -- expected: no whitespace errors.
