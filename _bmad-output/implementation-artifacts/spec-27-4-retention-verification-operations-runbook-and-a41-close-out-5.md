---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-07'
status: 'ready-for-dev'
route: 'dispatch'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: '75046976af3dbd573b360b1ec9eb579fa208fc83'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - '_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/predecessor-interchange-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository handoff is reviewed; live retention verification and A41 close-out lack accepted prerequisites.

**Approach:** Reuse existing producers after genuine prerequisites and execution authority exist. Keep checkpoints pending until then.

## Boundaries & Constraints

**Always:** Use PG-ONPREM-2 SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`; require registered/done owners, 25 accepted gates, authenticated independent Operations/Security decisions and authorized non-Production scope. Preserve disabled Production writes, protected history and four-path mutation scope. Prove final disabled gate, released Lease and zero lifecycle/clock replicas. Close A41 only after terminal/postflight/publication proof.

**Never:** Credit fixtures or PG1; invent authority; reuse closed C1.16 scope; implement separate renewal/interchange work here; stage/commit/publish without explicit post-review authority.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :--- | :--- | :--- | :--- |
| Denial | Missing/historical/unaccepted prerequisite | Pending; no target operation | Record blocker |
| Qualification | Accepted PG2 bundle and scoped target | Immutable packets; cleanup proof | Refuse drift; restore disabled state |
| Closure | Accepted C0-C6, terminal and publication authority | Exact transition; remote containment | Refuse missing proof/drift |

## Decisions

- 2026-10-07: Administrator selected the recommendation: keep 27.4 pending; implement separately scoped PG2 C1.15 producer preparation after defining its capture/disposition contract. Live qualification and the separate bundle-validation slice remain conditional. This does not approve the live tasks below.

</frozen-after-approval>

## Code Map

- `tools/verify_access_telemetry_lifecycle.py::_validate_predecessor` checks structure/hashes/ledgers, not reviewer authentication, artifact semantics or registered producers. Reuse `A41_ALLOWED_MUTATION_PATHS` and terminal guards.
- `docs/operations/access-telemetry-lifecycle.md` contains bounded execution/close-out commands.
- `tools/verify-access-telemetry-c1.ps1` now supports neutral PG2 C1.15 v2 with a valid session; preparation is done, acceptance absent.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` is canonical; its unsupported-dispatch paragraph is stale despite its later preparation append.
- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md` records C1.16 acceptance for one closed window; preserve the failed single-session attempt.

## Historical Context Classification

| Source | Classification | Use |
| :--- | :--- | :--- |
| Prior 27.4 specs; Stories 27.3/27.21/27.22 | historical-reference-only | Provenance; no inherited session credit |
| Lifecycle/architecture fixtures; PG2 C1.15 preparation | current-narrow-pattern | Reverified mechanics only |

## Slice Proof

Continue checkpoint tracking; register no successors. Canonical matrix states remain authoritative.

| Checkpoint | Owner / evidence | Completion |
| :--- | :--- | :--- |
| C0 | Operations; adapter-profile wrapper | repository-validated; live pending |
| C1 | Registered owners/Operations/Security; 25 accepted gates | pending renewal acceptance/23 owners/interchange |
| C2 | Operations; replacement producer | pending prerequisites |
| C3 | Lifecycle/adapter; retention/reclamation producer | pending prerequisites |
| C4 | Operations/Security; failure/privacy producer | pending prerequisites |
| C5 | Independent Operations; actual C0-C4 review | pending packets |
| C6 | Independent Security; same packets, different reviewer | pending packets |
| Terminal/A41 | Operations/repository owner; terminal/postflight/publication | pending C0-C6/authority |

## Tasks & Acceptance

- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — correct unsupported-dispatch wording and append fresh offline evidence; preserve pending states/history.
- [ ] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — reuse denial/drift/cleanup/closure tests and both architecture classes; no source/test additions.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — execute existing C0/C2-C4 only after genuine prerequisites/authority; retain immutable packets/cleanup and independent C5/C6 decisions. Otherwise pending.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — exact closure after terminal acceptance and explicit post-review publication authority. Sprint action follows verified publication.

**Acceptance Criteria:**
- Given completed preparation, when reconciled, then handoff distinguishes callable neutral PG2 C1.15 from missing acceptance/interchange.
- Given missing prerequisites, when checked, then live execution stays held, Story 27.4 incomplete and A41 open.
- Given authorized expiry/fault execution, when reviewed, then evidence proves two-writer acknowledgements, recovery, purge/newer preservation, emission and tenant denial.
- Given accepted C0-C6, when terminal/postflight/publication pass, then A41 closes and Epic 20/Story 20.5 remain historical done.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Reuse the canonical offline Bash block: 79 lifecycle cases, exactly 12 retention-decision/5 A41 guards; zero failures/skips, Debug/source-reference build zero warnings/errors, ten commands/block exit zero. After handoff edits rerun architecture guards and whitespace.

Planning receipt `/tmp/story-27-4-offline.aEdElFxS` passed at the full baseline above with an empty tracked diff; XML SHA-256 `5844f521038eeb9df6b40fbfd420ece0da4d00b2a535ba092ca80cc02697bd2a`. This predates this draft refresh and grants no live credit.

External blockers: accepted PG2 C1.15; 23 registered/done owners; executable authenticated interchange; external 25-gate bundle and genuine same-profile/session decisions; authorized context/namespace/deployment/session, custody root, credential-file paths and shared-system/fault/purge scope. These are separate prerequisite work. C1.16 credit is limited to its closed window.

Offline correction has no intent gaps or irreversible actions; footprint is draft/handoff. Live faults/purge/publication require later authority. Whole-story readiness remains blocked.
