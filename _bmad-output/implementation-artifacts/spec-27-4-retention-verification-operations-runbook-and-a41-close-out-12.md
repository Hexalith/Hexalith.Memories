---
title: 'Story 27.4 current-revision readiness reconciliation'
type: 'chore'
created: '2026-10-11'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '021169109253c4a27ffcd2ee47c47c69943cbe8a'
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - '_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-11.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 remains held while a new sole-owner review policy draft has appeared after the previous readiness audit. The draft may be mistaken for an adopted grant or completed predecessor.

**Approach:** Reconcile the current repository and linked Platform/Memories issue state against the approved Story 27.4 prerequisites, record whether any authenticated input changes the `RECHECK_ONLY` disposition, and leave live execution, Production writes, C1-C6, A41, sprint and protected history untouched unless their existing gates actually pass.

</frozen-after-approval>

## Implementation Notes

- 2026-10-11 repository baseline: `021169109253c4a27ffcd2ee47c47c69943cbe8a`. The new [sole-owner policy revision draft](../specs/spec-pg2-c1-authenticated-predecessor-interchange/sole-owner-policy-revision-draft.md) explicitly says “Proposed for owner review; not adopted or operational.” It does not replace the current distinct-principal policy or authenticate a reviewer, grant, target, session, or decision. Adoption would require a separately authenticated versioned owner decision and consumer migration; neither is present in this task input.
- The [P1–P4 decision packet](../specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md) still records `NO_SESSION_ID`, `NO_GRANT_REF`, and `NO_TARGET_DIGEST`. The [I2 request](pg2-c1-i2-operations-security-request.md) still describes proposed target, custody, credential and reviewer values rather than an authenticated Platform response. At 2026-10-11 07:01 UTC, read-only `gh api repos/Hexalith/Hexalith.Memories/issues/57/comments` returned comments `6095992898` and `6101097948`; `gh api repos/Hexalith/Hexalith.Platform/issues/5/comments` returned comment `6106379399`. Both public issues remained open. The [new Platform comment](https://github.com/Hexalith/Hexalith.Platform/issues/5#issuecomment-6106379399) asks for an operational inventory and an adoption manifest or a bounded alternative, and explicitly says it adopts no sole-owner policy. It returns no authenticated manifest or grant. This audit inspected those issue comments and the cited local files, not a Platform source revision or private custody. A private record outside this workspace cannot be ruled out; it must be supplied and independently verified before use.
- The deployed binding lookup in `tools/access_telemetry_c1_producer_bindings.py` still refuses every entry; `tools/access_telemetry_c1_authority_boundary.py` and `tools/verify_access_telemetry_lifecycle.py` still refuse unauthenticated authority and predecessor consumption. The [canonical ledger](tests/27-4-retention-verification-evidence.md) and `sprint-status.yaml` retain the held states. There is no accepted same-session, 25-gate PG-ONPREM-2 C1 predecessor or separate C2–C4 action authority in the inspected inputs.
- The current policy still needs independently granted C1 gate reviewers, a distinct C1.25 Security author/reviewer pair and separate C5/C6 reviewers. Operations/Security must name and authenticate those principals, or the owner must explicitly adopt and implement a versioned policy replacement before it can be used. The draft and the new Platform comment do neither.
- Disposition: `RECHECK_ONLY`. No live target was contacted and no C1–C6 gate, Production lifecycle write, Story 27.4 completion, A41/action status, sprint row or protected history was advanced. The C1 capture itself requires its own exact-target session and action grants. C0/C2–C4 live work must wait for all 25 separately accepted C1 gates in one eligible PG-ONPREM-2 session, authenticated Operations/Security bundle decisions, verified strict predecessor consumer migration, and separately scoped fault/purge action grants. This is an offline input audit, not a custody receipt, producer packet or reviewer decision.
- Focused offline verification: `test_authority_boundary.py` 8/8, `test_registration_provenance.py` 15/15 and `test_retention_verification.py` 41/41 passed with `PYTHONDONTWRITEBYTECODE=1` and `PYTHONHASHSEED=0`. The Story 27.4 sprint row stays `in-progress` because this completed audit does not complete the story or authorize the workflow's generic review transition.

## Review Triage Log

- `medium`, patched — the original audit had no exact issue snapshot. Recorded the 2026-10-11 07:01 UTC API read, comment IDs and inspected scope; no Platform source revision is claimed because none was inspected.
- `medium`, patched — the original release sentence could confuse registration with acceptance. Stated all 25 same-session accepted C1 gates, bundle decisions, strict consumer migration and separate action grants.
- `medium`, patched — the original audit omitted reviewer staffing. Recorded the current independent-review and C1.25/C5/C6 principal barriers, and the separate adoption path for any revised policy.
