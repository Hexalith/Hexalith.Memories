---
title: 'Story 27.4 operator qualification and A41 close-out'
type: 'feature'
created: '2026-09-10'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - 'docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4's repository work is complete, but C0-C6 evidence, approvals, and A41 close-out remain operator-pending. The C1 predecessor does not exist: only Story 27.21 is registered, C1.15 is pending, and 24 gates remain unowned.

**Approach:** Resume only after governance, security, authorization, and credential prerequisites pass. Execute the reviewed non-Production producers, retain same-profile evidence, obtain independent approvals, and apply exact A41 close-out only after terminal and publication verification.

## Decisions

- This invocation stops at verified `awaiting-operator`; it authorizes no live target or A41 action.
- Stories 27.7-27.31 and their C1 gates remain external blockers. Any work to create them requires a separate correct-course effort.
- Qualification requires OpenBao 2.6.2+ requalification and Helm chart 0.28.6 assessment; no security exception is approved by this spec.

## Boundaries & Constraints

**Always:** Require all 25 distinct C1 gates on `PG-ONPREM-1` before C2-C4; use one authorized non-Production target and external evidence root; obtain independent C1 and fresh C5/C6 approvals; restore the gate disabled, Lease released, and lifecycle/clock replicas at zero; keep Production disabled until the governed chain passes.

**Never:** Fabricate/share-discharge C1; contact a target without authorization; expose credentials or tenant content; weaken the profile, Dapr-only authority, zero overlays, or assurance claims; change `sprint-status.yaml`, `epics.md`, or historical Epic 20/Story 20.5; close A41 before postflight and publication proof.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Missing predecessor | Any C1 gate absent, reused, unowned, or not independently approved | No live C2-C4 execution and no A41 mutation | Report exact missing gate/owner and remain operator-pending |
| Authorized qualification | Complete C1 and required operator inputs | C0/C2-C4 emit immutable same-profile packets and finish disabled/zero | Emit a rejection packet, clean up, and exit nonzero |
| Retention proof | Valid journal and 1h/24h/168h cohorts | Prove purge, survivor preservation, and allocator reclamation | Reject drift, mutation, ambiguity, or skipped observations |
| A41 close-out | Passing C0-C6 and terminal packet | Change only four registered paths and prove publication | Reject dirt, races, drift, or protected changes |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out.md` -- completed contract and operator actions; do not reopen its implementation.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` -- canonical matrix; keep live rows operator-pending until packets pass.
- `tools/verify-access-telemetry-lifecycle.py` -- CLI for C0, C2-C4, inventory, preflight, postflight, and publication.
- `tools/verify_access_telemetry_lifecycle.py` -- schemas, validation, terminal chain, and exact A41 registry.
- `tools/access_telemetry_producer_common.py` -- fixed orchestration, cleanup, C3 journal, and C2-C4 producers; never substitute operator commands.
- `docs/operations/access-telemetry-lifecycle.md` -- canonical procedure and recovery.
- `_bmad-output/planning-artifacts/epics.md` and `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` -- current C1 blockers; read only.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/27-{7..31}-*.md`, and external C1 packets -- verify 25 owned, complete, unique same-profile gates and independent approvals before target contact.
- [ ] `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` -- verify the OpenBao upgrade/requalification or approved exception.
- [ ] `tools/verify-access-telemetry-lifecycle.py` and `docs/operations/access-telemetry-lifecycle.md` -- with authorization and inputs, run C0/C2-C4 and retain cleanup proof and packets.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` -- obtain fresh independent C5/C6 approvals and validate exact C0-C6 with no skip/failure.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, and `docs/dev/telemetry.md` -- after preflight, apply approved semantics and complete postflight, commit, publish, and remote verification.

**Acceptance Criteria:**
- Given any prerequisite is missing, when 27.4 is evaluated, then no live mutation occurs, Production stays disabled, A41 stays open, and the blocker is named.
- Given an authorized run, when a producer ends, then immutable evidence captures the result and proves disabled gate, released Lease, and zero replicas.
- Given passing C0-C6 and terminal evidence, when close-out runs, then only four registered paths change, protected bytes remain identical, and fetched-remote containment proves publication.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

No code delta remains: focused checks pass and the prior spec is `awaiting-operator`. C2-C4 disrupt the cluster, C3 purge/reclamation is irreversible, evidence writes are exclusive, and commit/push changes external state.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` -- expected: 69 pass without target access.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Release` -- expected: no warnings/errors.
- `dotnet build tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj -c Release` -- expected: no warnings/errors.
- `git diff --check` -- expected: no whitespace errors and no protected-path mutation.
