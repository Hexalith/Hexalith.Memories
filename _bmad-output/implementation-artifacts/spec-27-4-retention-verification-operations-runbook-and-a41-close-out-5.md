---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-07'
status: 'draft'
route: 'dispatch'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'cc754ab1487ddcec40340f9afa370aa3114851f1'
review_loop_iteration: 0
context: []
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

- `tools/verify_access_telemetry_lifecycle.py:2642` — structural/hash predecessor checks; labels do not authenticate authority. `A41_ALLOWED_MUTATION_PATHS` defines exact scope.
- `tools/verify-access-telemetry-lifecycle.py` — reuse existing producer/close-out entry point.
- `tools/verify-access-telemetry-c1.ps1` — separate PG2 C1.15 preparation requires a safe session and source/profile preflight; neutral v2 captures still need live custody and independent acceptance.
- `_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/predecessor-interchange-contract.md` — proposed assembler/registry/authority prerequisite; not implemented.
- `docs/operations/access-telemetry-lifecycle.md` — exact bounded execution commands.

## Historical Context Classification

| Source | Classification | Permitted use |
| :--- | :--- | :--- |
| Prior 27.4 specs; Stories 27.3/27.21/27.22 | historical-reference-only | Handoff/dependency provenance; no inherited gate credit |
| Existing lifecycle/architecture fixtures | current-narrow-pattern | Reverified offline behavior only |

## Slice Proof

Continue the approved checkpoint-tracking story. The canonical evidence matrix remains authoritative; register no successors.

| Checkpoint | Owner | Evidence command / artifact | Review state | Completion state |
| :--- | :--- | :--- | :--- | :--- |
| C0 | Operations | `adapter-profile` wrapper | repository-validated | live pending |
| C1 | Registered owners; Operations/Security | 25 accepted gates and authenticated decisions | operator-pending | accepted renewal/23 owners/interchange absent |
| C2 | Operations | `c2-production-replacement` | operator-pending | pending prerequisites |
| C3 | Lifecycle/adapter owners | `c3-retention-reclamation` | operator-pending | pending prerequisites |
| C4 | Operations/Security | `c4-failure-privacy-observability` | operator-pending | pending prerequisites |
| C5 | Independent Operations reviewer | C0-C4 acceptance | operator-pending | pending packets |
| C6 | Independent Security reviewer | Same packets; different reviewer | operator-pending | pending packets |
| Terminal/A41 | Operations/repository owner | Inventory/preflight/postflight/publication | operator-pending | pending C0-C6/authority |

## Tasks & Acceptance

**Execution:**
- [ ] `tools/verify-access-telemetry-lifecycle.py` — validate completed authenticated prerequisites/authority, then execute existing C0/C2-C4 producers; retain packets/cleanup.
- [ ] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — reuse denial/drift/cleanup/closure fixtures and both architecture classes.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — record actual evidence and independent C5/C6 decisions; otherwise retain pending states.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — prepare exact approved closure after terminal acceptance; obtain authority for reviewed publication. The separate sprint action follows verified publication.

**Acceptance Criteria:**
- Given authorized execution, when expiry/fault scenarios run, then accepted evidence proves two-writer acknowledgements, recovery, purge, newer records, emission and tenant denial.
- Given accepted C0-C6, when terminal/postflight/publication pass, then A41 closes while historical Epic 20/Story 20.5 remain done.
- Given missing prerequisites, when evaluated, then Story 27.4 stays incomplete/A41 open.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Use the canonical offline block in the evidence handoff and accepted-input runbook commands. Planning: clean baseline above, 79 lifecycle tests + exact 12 retention-decision/5 A41 guards passed, no skips, build 0 warnings/errors, ten commands/block exit 0. Local receipt `/tmp/story-27-4-offline.MH3EbAdC`; architecture XML SHA-256 `bbc5003e14844bcf2c5b9657ada6143363bf2cd2339cf44eec3a8353beefc632`. This predates the draft and grants no live credit.

Missing inputs: PG2 C1.15 renewal; 23 registered/done owners; executable authenticated interchange; external 25-gate bundle/decisions; authorized context/namespace/deployment ID; external custody; credential-file paths; shared-system/fault/purge scope. C1.16 acceptance covers one closed window only. These dependencies are outside this draft's implementation.
