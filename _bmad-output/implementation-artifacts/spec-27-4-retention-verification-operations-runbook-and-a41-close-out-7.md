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
investigation_commit: 'aac6d9054cb138881e6e49c8e48233553123ffce'
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

## Code Map

- `tools/verify_access_telemetry_lifecycle.py` — legacy `_validate_predecessor`, launch and terminal paths check structure/hashes/reviewer strings; require completed authenticated interchange before launch.
- `tools/access_telemetry_c1_github_approvals.py` — reuse reviewed wire/separation imports and GitHub observations; no session/custody/role authority.
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md` — separate I1-I6/P1-P7 prerequisites; accepting registry/consumer, numerical/time/status policy and scoped authority remain incomplete.
- `docs/operations/access-telemetry-lifecycle.md` — reuse checkpoint/custody/C3-journal/close-out commands.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — canonical matrix and offline block. Legacy reviewer checks persist until authenticated migration; C5/C6 keep separate reviewers.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/verify-access-telemetry-lifecycle.py` — after accepted PG2 predecessors, completed interchange, migration approval and scoped target/custody/credential/fault/purge grants, execute C0/C2-C4; retain immutable packets/journal and obtain separate post-evidence C5/C6 decisions.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — reconcile only actual accepted evidence.
- [ ] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — reuse refusal/drift/cleanup/tenant-negative tests and architecture guards; retain commands/results.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md`, canonical matrix — reviewed four-path closure after terminal/preflight; explicit post-review staging/publication authority, postflight and remote containment.

**Acceptance Criteria:**
- Given missing prerequisites, when readiness is checked, then no live launch occurs and Story 27.4/A41 remain incomplete/open.
- Given authorized PG2 execution, when expiry/faults run, then accepted evidence proves two-writer acknowledgement/recovery, expired purge, newer preservation, emission and denial before dependencies, with final disabled gate/released Lease/zero lifecycle-clock replicas.
- Given accepted C0-C6 and publication authority, when terminal/postflight/remote verification passes, then A41 summaries bind the same evidence and protected historical/sprint bytes remain identical.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

2026-10-08, clean source `aac6d9054cb138881e6e49c8e48233553123ffce`: canonical ten-command block exited 0; 80 lifecycle cases passed, Debug/source-reference build zero warnings/errors, exact 12 retention-decision + 5 A41 guards passed without skips. Receipt `/tmp/story-27-4-offline.MHHDT1CO` retains commands/exits/logs/dependency identities/assembly hash/XML.

Interchange discovery: exit 0, 92 passed without skips; command/log receipt `/tmp/story-27-4-readiness-5epa4681`. Its facts verify sixteen inputs, matching PG2 pins and pinned reviewed transport. Runtime/transport preparation is complete; consumer/deployment approval remains pending.

Two registered/done stories: 27.21 accepted historical PG1 C1.15; 27.22 accepted PG2 C1.16 for a closed window. PG2 C1.15 registration/capture/disposition, 23 gate registrations and authenticated interchange remain required. C1.16 needs proven session eligibility or a separately authorized fresh capture. No accepted eligible C1 bundle exists. Reopen on accepted prerequisite artifacts.

The initial readiness investigation changed only this draft and preserved the prior frozen intent. The subsequent owner-selected provider decision above is tracked in its separate prerequisite spec. Prior directions/receipts remain at the investigation commit. No live/status/A41/publication action occurred.

Provider implementation is tracked separately in [the authority spec](spec-pg2-c1-github-authority.md). Story 27.4 stays draft pending accepted prerequisites.
