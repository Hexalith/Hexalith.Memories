---
title: 'Story 27.4: Remaining live qualification and A41 close-out'
type: 'feature'
created: '2026-10-07'
status: 'in-progress'
route: 'dispatch'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'f4e7eb8626513c83f392a7cabf223b1a4673daa3'
review_loop_iteration: 0
investigated: '2026-10-08'
investigation_commit: 'd172165ebe1ab39efebd38b875b94f6d4ef6d191'
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

**Decision 2026-10-08 (source labels):** The owner selected the recommended separate C1.15 source-receipt correction, tracked in `spec-pg2-c1-15-source-receipt-labels.md`. This authorizes offline producer/tests only; 27.4 remains pending.

**Decision 2026-10-08 (I2):** The owner said “apply recommended”, authorizing a separately tracked offline registry/source-validation prerequisite. Its approved scope uses isolated fixture contracts and no deployed accepting entries; live Story 27.4 remains held. See `spec-pg2-c1-offline-producer-bindings.md`.

**Decision 2026-10-08 (session scope):** The owner selected RECHECK_ONLY: rerun the offline block at `d172165e`, refresh readiness/evidence, and keep the draft held. No live progress is authorized; holds are re-confirmed at current HEAD.

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_interchange.py` — reuse I1 readers; semantic assembly absent.
- `tools/access_telemetry_c1_producer_bindings.py` — done offline I2 registry/source inspection (isolated fixture contracts, no deployed accepting entries); confers no registration or acceptance.
- `tools/access_telemetry_c1_capture_semantics.py` — done offline C1.15 declared-observation pin inspection; no live gate credit.
- `tools/access_telemetry_c1_github_authority.py` — authenticated observations confer no acceptance.
- `tools/verify_access_telemetry_lifecycle.py` — legacy labels cannot authenticate C1.
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md` — offline I2 preparation done; closed I2 registration/provenance, I3–I6 and P1–P7 holds remain.

## Tasks & Acceptance

**Execution (live work blocked):**
- [ ] `tools/verify-access-telemetry-lifecycle.py` — execute C0/C2–C4 only after accepted PG2 predecessors, approved migration and scoped target/custody/credential/fault/purge grants; retain cleanup and independent C5/C6 decisions.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — reconcile accepted evidence and runbook commands.
- [x] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — rerun offline refusal/drift/cleanup/tenant-negative and architecture guards.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md`, canonical evidence — exact four-path closure after terminal/preflight, explicit staging/publication authority, postflight and remote containment.

**Acceptance Criteria:**
- Given missing prerequisites, when readiness is checked, then no live launch occurs and 27.4/A41 remain incomplete/open.
- Given authorized PG2 execution, when expiry/faults run, then evidence proves acknowledgement/recovery, purge/newer preservation, emission and tenant denial, with final disabled gate/released Lease/zero lifecycle-clock replicas.
- Given accepted C0–C6 and publication authority, when terminal/postflight/remote proof passes, then summaries bind one evidence set and protected history/sprint bytes remain identical.

## Design Notes

Runtime PG2 correction and both offline prerequisites (source-receipt labels, I2 registry/source inspection, C1.15 observation pins) are complete; deployment approval pending. Footprint: draft, context and offline receipt only. Live faults/purge/publication require their recorded authority.

## Implementation Notes

The owner approved the PG2 identity-digest extension with “apply recommendation”.
Implementation and offline verification are recorded in
[the separate C1.15 prerequisite](spec-pg2-c1-15-source-receipt-labels.md);
independent review and both follow-ups are complete. This fixes producer/reader
compatibility only.
27.4 remains draft, A41 open and Production writes disabled pending the accepted
prerequisites and scoped execution inputs listed above.

2026-10-08: Rechecked holds; updated readiness only. No source/test/target changes.

2026-10-09: Rechecked the offline block at current HEAD
`5d0cbefedc828ab829b0b9104c5ab77788dcd5b2`, which contains
`d172165ebe1ab39efebd38b875b94f6d4ef6d191`. Refreshed canonical readiness and
compiled epic context only. No source, test, target, sprint, history, or
Production-byte changes. Live C0/C2–C4, C5/C6, and A41 close-out stay pending;
the draft remains held.

## Spec Change Log

## Review Triage Log

## Verification

Earlier offline/135-interchange receipts at `3e18d0dcdceb387eff89862c382637da89ad7e47`: `/tmp/story-27-4-offline.vr3cQrDZ`, `/tmp/story-27-4-readiness-ucu_dcz1`.

Current canonical offline block: exit 0; 80 lifecycle passes, zero-warning/error Debug/source-reference build, exact 12 retention + 5 A41 passes. Commands/source/dependencies/assembly/XML: `/tmp/story-27-4-offline.Ga42WIRM`.

`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`: exit 0; 167 passes. All lanes have zero failures/errors/skips. Logs/source/protected hashes and label-collapse probe: `/tmp/story-27-4-resume-4e1vv77o`. Offline only; draft unresolved.

Current resumption: 80 lifecycle + 12/5 architecture + 167 interchange passes; zero-warning/error Debug/source-reference build. Canonical receipt `/tmp/story-27-4-offline.fq11FV9r`; investigation/interchange/protected hashes `/tmp/story-27-4-current-readiness-3gv1qqwq`. Offline only; draft unresolved.

2026-10-08 planning recheck at `d172165ebe1ab39efebd38b875b94f6d4ef6d191`: `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py'` — exit 0; 219 passes (includes the new producer-bindings and capture-semantics lanes), zero failures/errors/skips. Offline only; draft unresolved.

2026-10-09 current-HEAD recheck at `5d0cbefedc828ab829b0b9104c5ab77788dcd5b2` (contains `d172165e`): canonical ten-command block exit 0; 80 lifecycle passes in 29.041s; zero-warning/error Debug/source-reference build; exact 12 retention + 5 A41 passes. Receipt `/tmp/story-27-4-offline.RJE0jKex`. Interchange discover exit 0; 219 passes in 48.603s, zero failures/errors/skips. Receipt `/tmp/story-27-4-interchange.48qhOxQj`. Readiness text and compiled epic context refreshed. Offline only; draft held; no live launch. A41 remains open and Production writes remain disabled.
