---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-06'
status: 'draft'
route: 'dispatch'
baseline_commit: 'ed4528d51279f488a9be81a762fa5522ceb46203'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
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

</frozen-after-approval>

## Open Questions

1. Are live prerequisites available? **Provide accepted inputs:** external bundle path, approved/done owners, approvals, target/context/namespace, evidence root, credential-file paths and fault/purge authority. **Keep live work pending:** complete the offline correction; 27.4/A41 remain incomplete. Supply no credential values.

## Code Map

- `tools/verify-access-telemetry-c1.ps1:27` — PG2 C1.15 is rejected as `unsupported-successor-gate-or-historical-opt-in`.
- `tools/verify_access_telemetry_lifecycle.py:2642` — predecessor, producer, terminal, preflight/postflight and publication validation; reuse.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryA41CloseOutTests.cs` — five profile, pending-state and close-out guards.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — document retention-decision and A41 selectors with a local Debug build; preserve pending states.
- [ ] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — run existing scenario/denial/close-out fixtures and architecture guards; record results.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — after accepted prerequisites/authority, run C0/C2-C4 and retain cleanup/evidence packets.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — after independent C5/C6 and terminal validation, prepare exact close-out; stage/commit/publish with explicit authority. The separate sprint action follows verified publication.

**Acceptance Criteria:**
- Given the offline handoff, when its commands run, then retention-decision and all five A41 guards pass while live states remain pending.
- Given invalid/missing prerequisites, when qualification is considered, then exact blockers are recorded without live operations or A41 mutation.
- Given accepted PG2 evidence and publication authority, when close-out runs, then cleanup, C0-C6, terminal, exact postflight and remote containment pass before A41 resolves.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py'` — fixtures pass.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1` — zero warnings/errors.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests` — five passing guards.
- `git diff --check` — clean whitespace.

Planning checks, 2026-10-06: lifecycle 79/79, C1.16 19/19, Debug build 0 warnings/errors, A41 5/5; render also passed with installed kubectl v1.36.1. PG2 C1.15 is unsupported, C1.16 remains pending, and 23 gates are held/unregistered. Accepted external predecessor/target authority was not supplied.
