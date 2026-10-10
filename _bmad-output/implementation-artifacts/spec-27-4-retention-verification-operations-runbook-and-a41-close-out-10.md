---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-10'
status: 'in-progress'
route: 'dispatch'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '61ee54570adf5bf2d2c24b930106cf377f4a34db'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - 'docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 has repository-validated lifecycle producers and guards, but no accepted same-profile C1 predecessor or scoped live authority. Its C1-C6 gates remain operator-pending, Production lifecycle writes are disabled, and A41 is open.

**Approach:** Obtain and authenticate the missing Operations/Security response and C1 predecessor first. Then use existing producers for PG-ONPREM-2 qualification; reconcile A41 only after independent acceptance, terminal checks, and publication verification succeed.

**Decision 2026-10-10:** The owner chose prerequisite collection over another offline recheck. This grants no live execution or A41 mutation. Recheck when executable inputs change or before an authorized live run.

**Delivery decision 2026-10-10:** The owner directed GitHub issue [#57](https://github.com/Hexalith/Hexalith.Memories/issues/57) to track the I2 response; it grants no authority.

## Boundaries & Constraints

**Always:** Bind packets to PG-ONPREM-2 SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`, one eligible session, registered/done owners, and twenty-five accepted C1 gates. Require authenticated Operations/Security decisions, separate C5/C6 reviewers, external custody, and scoped target, credential, fault, purge and publication grants. Preserve final disabled gate, empty Lease and zero lifecycle/clock replicas.

**Never:** Treat offline fixtures, historical PG1 evidence, local labels, the prepared I2 request, or the closed C1.16 window as new authority. Never run a live producer before its grant and predecessor pass. Keep Production writes disabled and A41 open until the complete chain passes; preserve historical Epic 20/Story 20.5 and sprint bytes outside the exact authorized close-out.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :--- | :--- | :--- | :--- |
| Held | Missing approval, custody, eligible session or C1 gate | No live target contact or state advance | Record exact missing input and owner |
| Qualification | Accepted predecessor and scoped grants | Immutable C0/C2-C4 packets prove recovery, expiry, purge, newer records, continuity and tenant denial | Reject skipped faults, drift, loss, leakage or failed cleanup |
| Closure | Accepted C0-C6 and publication grant | Terminal/postflight/remote checks bind one evidence set; exact A41 transition | Refuse incomplete chain or protected-byte drift |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-7.md` — parent plan; retain its frozen RECHECK_ONLY decision and history.
- `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — exact proposed input and release boundary; a prepared request, not a grant.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — canonical C0-C6 matrix, qualification commands and offline receipts; preserve historical packets.
- `tools/verify_access_telemetry_lifecycle.py` and `tools/verify-access-telemetry-lifecycle.py` — predecessor, producer, terminal, pre/postflight and publish guards; reuse existing checks.
- `tools/access_telemetry_c{2,3,4}_producer.py` — reviewed live packet producers.
- `tools/access_telemetry_c1_authority_boundary.py` and `tools/access_telemetry_c1_producer_bindings.py` — offline I2/I3 refusal.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — inspect the returned immutable decision/grant records and their current status before any live action.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — authenticate every prerequisite and bind C0-C6 to one source/profile/session and external custody before any live action.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — run existing C0/C2-C4 producers only within approved target and action windows; retain immutable receipts and final disabled/empty/zero cleanup proof.
- [ ] `docs/operations/access-telemetry-lifecycle.md` and `docs/operations/access-telemetry-adapter-production.md` — reconcile observed RPO/RTO, alarms, capacity, purge, recovery and rollback with the accepted packets.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — record independent C5/C6 decisions, terminal validation, exact close-out preflight/postflight and remote containment.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md`, and the canonical evidence — perform only the preflight-approved four-path A41 mutation after explicit publication authority.

**Acceptance Criteria:**
- Given missing authentic prerequisites, when readiness runs, then no live target is contacted and C1-C6, Production writes and A41 retain their held states.
- Given accepted PG2 C1 and scoped grants, when expiry and declared faults run across two writers, then packets prove acknowledgement, recovery, purge, newer-record preservation, audit continuity, tenant denial and final cleanup.
- Given accepted C0-C6 and publication authority, when terminal, pre/postflight and remote containment pass, then A41 summaries bind one canonical evidence set while protected history remains intact.

## Implementation Notes

Issue #57 was opened and verified, assigned to `jpiquot`, with no credential values. The detailed request remains in a local commit ahead of `origin/main`; the issue body carries the decision categories. No response or gate advance occurred.

2026-10-10 ownership correction: the owner confirmed `Hexalith.Platform` manages the target and operational access boundary, and Jérôme Piquot is the sole project contributor and authority. The canonical request and issue #57 now direct target facts to Platform; no second P1–P4 policy signer is expected. Current distinct-principal gates (including C1.25 and C5/C6) remain unresolved pending actual evidence or an approved policy correction. The Platform checkout at `8a56bd57837784806aae989159fbcbc71fbd41d4` contains no exact PG-ONPREM-2 profile hash, so its existing cluster evidence is not a current-profile grant.

2026-10-10 prerequisite audit: The local I2 request still says that no execution grant or approval was returned. The P1–P4 decision packet retains explicit `NO_*` denials for the receipt issuer and trust root, authenticated target/session and external custody, and producer/reviewer grants. Closed I2 registration/provenance, I3–I6, P5–P7, accepted PG2 C1.15 renewal, an eligible C1.16 session, and the other twenty-three registered/done C1 owners remain absent. The canonical C0–C6 ledger still holds live qualification, independent review, publication and A41 close-out. No live target was contacted or gate state advanced in this run; the execution tasks above remain unchecked. Resume only after the Platform-produced immutable response and independently verifiable current status are available, then authenticate the complete predecessor and action grants before any producer run. The frozen 2026-10-10 decision defers another offline recheck until those inputs change or an authorized live run is imminent.

2026-10-10 delivery-route adoption: The owner selected the [prerequisite route](../specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md#owner-selected-delivery-route-2026-10-10-planning-decision-only). This approves a bounded implementation direction for P7 and a C1.16 audit/fresh-capture fallback; it does not complete P1–P7, approve any live action or alter the frozen 27.4 gate. Platform still owes authenticated operational facts and independent role assignments. The exact PG2 workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` remains mandatory alongside the profile digest in every applicable packet and acceptance check. The frozen predecessor-pass rule governs Story 27.4 C0/C2–C4 launch; predecessor C1 capture has its own scoped grant and session gate.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — all focused offline lifecycle methods pass without failures or skips.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — interchange remains fail closed without authentic records.
- `git diff --check` — no whitespace errors; inspect canonical packet and protected-file hashes before any close-out.
