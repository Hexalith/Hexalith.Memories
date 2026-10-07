---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-07'
status: 'in-progress'
repository_status: 'reviewed-awaiting-operator'
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

- [x] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — correct unsupported-dispatch wording and append fresh offline evidence; preserve pending states/history.
- [x] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — reuse denial/drift/cleanup/closure tests and both architecture classes; no source/test additions.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — execute existing C0/C2-C4 only after genuine prerequisites/authority; retain immutable packets/cleanup and independent C5/C6 decisions. Otherwise pending.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — exact closure after terminal acceptance and explicit post-review publication authority. Sprint action follows verified publication.

**Acceptance Criteria:**
- Given completed preparation, when reconciled, then handoff distinguishes callable neutral PG2 C1.15 from missing acceptance/interchange.
- Given missing prerequisites, when checked, then live execution stays held, Story 27.4 incomplete and A41 open.
- Given authorized expiry/fault execution, when reviewed, then evidence proves two-writer acknowledgements, recovery, purge/newer preservation, emission and tenant denial.
- Given accepted C0-C6, when terminal/postflight/publication pass, then A41 closes and Epic 20/Story 20.5 remain historical done.

## Implementation Notes

- Completed the authorized offline handoff correction and existing verification. The neutral PG2 C1.15 collector is callable with a valid session; live capture and authenticated acceptance remain separate prerequisites.
- Tasks 3–4 remain unchecked: their explicit prerequisite and authority conditions are unsatisfied. No live producer, fault, purge, A41 transition, staging, commit or publication ran.
- Reviewed the full diff from the preserved baseline. The pre-existing committed spec refresh and EventStore/Platform/Tenants gitlinks between baseline and execution HEAD are provenance, not changes made in this run. Implementation changes this spec and the canonical handoff; review also appends one pre-existing dependency verification gap to the deferred-work ledger.
- Every frozen matrix row has existing passing fixture coverage listed in the [current evidence receipt](tests/27-4-retention-verification-evidence.md#2026-10-07-story-274-current-handoff-reconciliation). Fixture checks grant no live qualification, reviewer authentication or closure credit.

## Spec Change Log

## Review Triage Log

Three review layers completed. Edge-case hunter returned no findings. Every other finding is classified below before grouping; B2, B4 and B5 have distinct causes and receive direct documentation patches. No intent or implementation loopback is needed.

| ID | Verdict | Evidence and disposition |
| :--- | :--- | :--- |
| B1 | low | The Code Map retains the investigation-time description of the stale paragraph after implementation corrected it. Rejected: the requested fix edits this build's spec, which review rules exclude. Completed work and current evidence are explicit in Implementation Notes and Verification. |
| B2 | low | The canonical handoff records the ten-command pre-append receipt but omits the existing post-edit four-command receipt recorded in this spec. Patch: add that distinct receipt and exact XML/count/exit identifiers. |
| B3 | false | The canonical verification command discovers every `test_*.py` in the lifecycle directory, including `test_pg_onprem_2_adoption.py`; both cited adoption denial tests ran and passed in the retained 79-case log. The narrower task reference caused no omission. The requested fix also edits this spec. |
| B4 | low | The broad frozen-row coverage wording can overstate authentication coverage even though the next paragraph explicitly disclaims reviewer authentication. Patch: name structural/fixture coverage and distinguish this run's deliberate no-target hold from authenticated prerequisite enforcement still awaiting interchange. |
| B5 | low | Closed-window C1.16 acceptance is explicit, but the consequence for a future new session is implicit. Patch: require session eligibility and separately authorized fresh capture/disposition for a new session; retain historical acceptance. |
| V1 | medium | Pre-verified missing adoption assertion: the earlier committed EventStore advance requires workload issuer client credentials in authority mode, while Memories' AppHost model test omits those credentials, its gateway startup test is skipped and its running fixture substitutes an in-memory command store. Defer to separate gateway-composition work; this handoff changes no executable behavior. Owner: AppHost/EventStore integration maintainers. Reopen with an authority-backed Memories model assertion and gateway startup proof. |

## Verification

Reuse the canonical offline Bash block: 79 lifecycle cases, exactly 12 retention-decision/5 A41 guards; zero failures/skips, Debug/source-reference build zero warnings/errors, ten commands/block exit zero. After handoff edits rerun architecture guards and whitespace.

Planning receipt `/tmp/story-27-4-offline.aEdElFxS` passed at the full baseline above with an empty tracked diff; XML SHA-256 `5844f521038eeb9df6b40fbfd420ece0da4d00b2a535ba092ca80cc02697bd2a`. This predates this draft refresh and grants no live credit.

External blockers: accepted PG2 C1.15; 23 registered/done owners; executable authenticated interchange; external 25-gate bundle and genuine same-profile/session decisions; authorized context/namespace/deployment/session, custody root, credential-file paths and shared-system/fault/purge scope. These are separate prerequisite work. C1.16 credit is limited to its closed window.

Offline correction has no intent gaps or irreversible actions; footprint is draft/handoff. Live faults/purge/publication require later authority. Whole-story readiness remains blocked.

Current execution: the unchanged canonical block passed at full HEAD `b350c094ab4bc10bd2daf723f9e2c90ffdddae2a`; `/tmp/story-27-4-offline.tCpEtJN6` records all ten commands/block exit 0, 79 lifecycle cases, a Debug/source-reference build with zero warnings/errors and exactly 12 + 5 architecture results, every result Pass. After handoff edits, `/tmp/story-27-4-handoff.o88ERlot` records both architecture classes, XML count validation and whitespace, all four commands/block exit 0; XML SHA-256 `4fd0c2ec49cb87d097c445a6c9416e73fbd784290b5cfe6d76a0d0409eeff657`.

Acceptance 1 and the missing-prerequisite behavior in acceptance 2 are satisfied. Acceptances 3–4 remain conditional and unexecuted; live evidence and closure are not claimed. The [canonical receipt](tests/27-4-retention-verification-evidence.md#2026-10-07-story-274-current-handoff-reconciliation) preserves exact test names, command results, hashes and external blockers.

Final review disposition: B2/B4/B5 documentation patches applied and verified; B1/B3 rejected with evidence; V1 appended to the [deferred ledger](deferred-work.md). No unresolved current-slice finding remains. Story-wide live tasks and acceptance 3–4 remain pending, so the generic workflow's `done`/sprint-review transition is not applied. The frozen approved intent requires genuine prerequisites and explicit post-review authority before staging, committing or publishing; none was provided, so this run creates no commit and preserves sprint `in-progress`.

Parent post-patch verification: `/tmp/story-27-4-offline.L9PhHyQb`, all ten commands/block exit 0; 79 lifecycle cases passed in 36.988s, no failures/errors/skips; Debug/source-reference build zero warnings/errors; exactly 12 + 5 architecture guards all Pass. Source HEAD `b350c094ab4bc10bd2daf723f9e2c90ffdddae2a`; execution-time diff SHA-256 `2ffc7a3fe5449176b26d1dad574e4a1559ffff6e59c134168e3c29e118e35ff5`; architecture XML SHA-256 `efc7e061185c40c729d3fbeb2da559675a6d88efa9b69354600530e7600e327b`. The final status/receipt notes are recorded after that run and confer no live credit.
