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
investigated: '2026-10-08'
investigation_commit: '3e18d0dcdceb387eff89862c382637da89ad7e47'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Repository preparation is complete; accepted prerequisites for live retention verification and A41 close-out are missing.

**Approach:** Reuse reviewed producers after genuine current-profile evidence and scoped authority exist. Retain pending states until then.

## Boundaries & Constraints

**Always:** Bind PG-ONPREM-2 SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`. Require authenticated separate Operations/Security decisions; the owner explicitly allows Jérôme Piquot to approve both roles under the named-owner policy. Require 25 accepted gates with registered/done owners and eligible session. Preserve disabled Production writes; prove final disabled qualification gate, released Lease and zero lifecycle/clock replicas. Keep A41 open until remote containment passes.

**Never:** Credit PG1, fixtures or reviewer labels; reuse closed C1.16 execution scope; implement separate prerequisites here; alter historical Epic 20/Story 20.5 or sprint bytes. Staging, committing and publication require existing explicit post-review authority.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :--- | :--- | :--- | :--- |
| Held | Missing prerequisite or execution grant | No live launch; pending matrix | Identify missing evidence |
| Qualification | Accepted predecessor and authorized target | Immutable C0/C2-C4 packets and cleanup | Reject drift, loss, skips or tenant leakage |
| Closure | Accepted C0-C6, terminal proof and publication authority | Exact four-path transition and remote proof | Refuse incomplete chain or protected-byte drift |

**Decision 2026-10-08:** The owner selected “do recommended”: implement the narrow GitHub-backed Platform adapter and qualification contract in a separately tracked prerequisite. This selects the provider direction, not actual operational grants or gate acceptance.

</frozen-after-approval>

## Open Questions

1. Continue with **separate strict artifact readers** (recommended: finish I1 as a library-only prerequisite; no acceptance, consumer migration or target access), or **keep 27.4 pending** until accepted prerequisites and scoped execution inputs exist? Selecting I1 requires its own build spec under the existing scope boundary.

## Code Map

- `tools/access_telemetry_c1_interchange.py` — snapshots/J1/Refs complete; closed artifact readers absent. Reuse primitives for separately selected I1.
- `tools/access_telemetry_c1_github_authority.py` — reviewed authenticated observations, no capture acceptance or custody proof. Platform is published/pinned at `48d5c6e64087bb33232651d8b59422e95185c699`.
- `tools/verify_access_telemetry_lifecycle.py` — legacy predecessor checks hashes/reviewer labels. Approved P7 must migrate checkpoint validation, producer launch and terminal bundle consumers together.
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md` — accepting registry, semantic verification, assembly and migration remain absent despite completed authority preparation.

## Tasks & Acceptance

**Execution (blocked):**
- [ ] `tools/verify-access-telemetry-lifecycle.py` — after accepted PG2 predecessors, migration approval and scoped target/custody/credential/fault/purge grants, execute C0/C2-C4; retain packets/journal/cleanup and separate post-evidence C5/C6 decisions.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — reconcile actual accepted evidence; retain checkpoint/custody/cleanup commands from the operations runbook.
- [ ] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — rerun refusal/drift/cleanup/tenant-negative and architecture guards, retaining results.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md`, canonical evidence — reviewed four-path closure after terminal/preflight, explicit staging/publication authority, postflight and remote containment.

**Acceptance Criteria:**
- Given missing prerequisites, when readiness is checked, then no live launch occurs and 27.4/A41 remain incomplete/open.
- Given authorized PG2 execution, when expiry/faults run, then evidence proves acknowledgement/recovery, purge/newer preservation, emission and tenant denial, with final disabled gate/released Lease/zero lifecycle-clock replicas.
- Given accepted C0-C6 and publication authority, when terminal/postflight/remote proof passes, then summaries bind one evidence set and protected history/sprint bytes remain identical.

## Design Notes

Live purge/faults/publication require scoped authority. PG2 C1.15 lacks registration/accepted renewal; 23 owners remain held; closed-window C1.16 eligibility is unproven. No eligible bundle or execution inputs supplied. Footprint: draft/context only.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

2026-10-08 at the investigation commit: canonical offline block exit 0; 80 lifecycle tests; Debug/source-reference build zero warnings/errors; exact 12 retention + 5 A41 guards, no skips. Receipt `/tmp/story-27-4-offline.vr3cQrDZ` retains commands/exits/source/dependencies/assembly/XML.

`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`: exit 0, 135 passed, no skips. Receipt `/tmp/story-27-4-readiness-ucu_dcz1` retains log/exit, pre-edit draft and protected hashes. Earlier receipts remain at the investigation commit. User edits preserved; no live/status/A41/publication action. Draft remains unresolved.
