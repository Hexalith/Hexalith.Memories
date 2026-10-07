---
title: 'Story 27.4: Remaining live qualification and A41 close-out'
type: 'feature'
created: '2026-10-07'
status: 'draft'
route: 'dispatch'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'f4e7eb8626513c83f392a7cabf223b1a4673daa3'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Repository preparation is complete; accepted prerequisites for live retention verification and A41 close-out are missing.

**Approach:** Reuse reviewed producers after genuine current-profile evidence and scoped authority exist. Retain pending states until then.

## Boundaries & Constraints

**Always:** Bind PG-ONPREM-2 SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`. Require authenticated independent Operations/Security decisions, 25 accepted gates with registered/done owners and eligible session. Preserve disabled Production writes; prove final disabled qualification gate, released Lease and zero lifecycle/clock replicas. Keep A41 open until remote containment passes.

**Never:** Credit PG1, fixtures or reviewer labels; reuse closed C1.16 execution scope; implement separate prerequisites here; alter historical Epic 20/Story 20.5 or sprint bytes. Staging, committing and publication require existing explicit post-review authority.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :--- | :--- | :--- | :--- |
| Held | Missing prerequisite or execution grant | No live launch; pending matrix | Identify missing evidence |
| Qualification | Accepted predecessor and authorized target | Immutable C0/C2-C4 packets and cleanup | Reject drift, loss, skips or tenant leakage |
| Closure | Accepted C0-C6, terminal proof and publication authority | Exact four-path transition and remote proof | Refuse incomplete chain or protected-byte drift |

</frozen-after-approval>

## Prerequisite progress

2026-10-07: The user asked us to handle the technical prerequisites ourselves. The bounded offline foundation is completed and tracked separately in [the wire-primitives spec](spec-pg2-c1-interchange-wire-primitives.md): strict immutable snapshots, J1 encoding and exact Ref validation, 32 new tests and 3 existing regressions passed, and all six independent review findings fixed. We prepared the implementation and test receipts rather than asking the user to assemble them. This is common wire preparation, not completed authenticated interchange or live qualification. Independent acceptance and scoped live authority remain prerequisites; no such decision is inferred from this request. The conditional live tasks below stay pending.

## Code Map

- `tools/verify_access_telemetry_lifecycle.py` — `_validate_predecessor` checks structure/hashes/ledgers; reviewer authentication and artifact semantics remain missing. Reuse `A41_ALLOWED_MUTATION_PATHS`; no launch through this gap.
- `docs/operations/access-telemetry-lifecycle.md` — existing bounded checkpoint and close-out commands.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — canonical matrix and offline block; PG2 preparation already reconciled.
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/` — separate unready contract with unresolved P1-P7 decisions.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/verify-access-telemetry-lifecycle.py` — after authenticated interchange, accepted predecessor and execution authority, run C0/C2-C4 with external custody/C3 journal; obtain independent C5/C6 decisions.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — reconcile matrix only to accepted actual packets; otherwise retain pending/rejected states.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` and canonical matrix — exact four-path closure after terminal/preflight; authorized staging/publication; postflight and remote containment.
- [ ] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — reuse denial/drift/cleanup/closure cases and architecture guards; attach tenant-negative commands/results.

**Acceptance Criteria:**
- Given missing prerequisites, when readiness is checked, then no live producer runs and Story 27.4/A41 remain incomplete/open.
- Given an authorized exact-profile target, when expiry/faults execute, then accepted evidence proves two-writer acknowledgement, recovery, expired purge, newer preservation, emission and denial before dependencies.
- Given actual accepted C0-C6, when terminal/postflight/publication verification succeeds, then all A41 summaries cite the same evidence and protected historical records remain byte-identical.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Clean-baseline investigation: canonical ten-command block exited 0; 79 lifecycle cases passed; Debug/source-reference build zero warnings/errors; exact 12 retention-decision + 5 A41 guards passed. Receipt `/tmp/story-27-4-offline.o6JxCzd3` retains commands/logs/source/dependency/build identities/XML. Reproduce using the canonical block; require every exit 0 and every case Pass.

Separate built-assembly `dotnet exec -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryQualificationWorkloadTests -parallelMode none -noLogo -failSkips` exited 0: 18/18 passed, zero errors/failures/skips/not-run; XML retained as `runtime-workload.xml` in that receipt. Includes PG1/non-Qualification/revocation negatives. Offline results confer no live acceptance. At that initial story-only investigation, only this draft changed; no source/test addition or target operation occurred. Subsequent offline preparation is recorded in the separately scoped wire-primitives spec.

Blockers: accepted PG2 C1.15; 23 registered/done owners; authenticated interchange/policy; eligible C1.16/session; bundle decisions; runtime migration/deployment approval; target, custody, credential-file paths and fault/purge grant. People remain unassigned. Reopen on actual approved artifacts.

## Owner direction and concrete next decisions

The user explicitly said “as the owner I approve” on 2026-10-07, then named “me Jérôme Piquot” when asked who should provide Operations/Security sign-offs and where approvals are recorded. Record Jérôme Piquot as the owner-designated approver and continue authorized technical preparation without asking for the same approval again. The reply did not select an approval-recording system or expressly change the existing two-reviewer requirement. These conversation directions are not fabricated issuer receipts or current-profile gate acceptance.

The next policy decision is concrete:

- **Keep the existing rule:** Jérôme Piquot provides one review role; a different authenticated reviewer provides the other. The current `_validate_predecessor` requires exactly two approvals, one Platform Operations and one Security, with different reviewer identities. The future authority adapter must establish real principal independence.
- **Change the rule:** Jérôme Piquot may provide both roles under an explicitly revised single-owner policy. Implement and review a versioned policy/schema/validator change before relying on this arrangement. Record the weaker separation of duties and its compensating controls; do not invent a second username for the same person or mark the existing two-person check passed. The owner's approval of technical work alone does not resolve this deliberate requirement change.

The selected approval system must then supply a real authenticated principal and bound review record. Existing product JWT/OIDC, clock signatures and local Kubernetes credentials do not implement that contract. A read-only authenticated GitHub connector profile lookup returned name `Jérôme Piquot`, account `jpiquot`, stable account ID `6775094`, matching the owner's designation. This supplies a concrete candidate account, not an approval on any capture or manifest. GitHub review recording is the proposed mechanism to prepare because this session supports authenticated profile/review access and the repository is hosted there; no review was created and no issuer/receipt policy is adopted by this lookup. We will prepare the implementation and review packets rather than require the owner to produce technical files.

### Environment preparation already performed

- Read-only local configuration identified context `jpiquot@local`, default namespace `default`.
- `kubectl --context jpiquot@local --request-timeout=15s get namespaces -o name` exited 0. The cluster is reachable and `hexalith-memories` exists; `hexalith-memories-qualification` does not. No resources were created, patched, scaled or deleted.
- `kubectl kustomize deploy/kubernetes/overlays/qualification` exited 0. The rendered setup contains 61 resources and is retained at `/tmp/pg2-c1-wire-final-qyqhy9wi/qualification-render.yaml`, SHA-256 `5d2a9212b320a6af130ba0f8e395e279764f78344f2b1e18366e3adcdaa6fab0`, with a resource inventory in `qualification-render-summary.json`. Nothing was applied.
- The proposed target is the separate `hexalith-memories-qualification` namespace on the existing context. Rendering keeps lifecycle and clock replicas at zero, the gate disabled, Lease holder empty and the physical-evidence reporter Job suspended. It also includes application/storage workloads and two namespaced RBAC resources in shared `dapr-system`; those shared resources must appear in the eventual reviewed deployment/execution scope. This overlay is not a claim that runtime prerequisites, images, secrets, capacity or all C1 gates are ready.
- Before fault/purge execution, complete the selected authority contract and registered gate prerequisites, prepare the exact isolated deployment inputs/custody/session, verify all prerequisite evidence, and use the reviewed producer. The namespace's absence is setup work we can prepare and carry out under a concrete approved scope, rather than an instruction for the user to provision it manually.
- Protected tracking/context/telemetry/canonical evidence/runtime-gate files match their baseline Git-normalized blobs. Literal worktree hashes are separately retained in `/tmp/pg2-c1-wire-final-qyqhy9wi/protected-files.json`; checkout CRLF conversion is not treated as a code change. No staging, committing or publication occurred.
