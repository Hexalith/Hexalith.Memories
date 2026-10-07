# Implementation tasks and unresolved prerequisites

Tracking ID: `SPEC-pg2-c1-authenticated-predecessor-interchange`.
Review: proposed. Implementation: unstarted. No story number, sprint registration,
checkpoint approval or permission to operate a target is assigned by this folder.

## Tasks in this slice

All tasks contribute to one offline interchange verdict. Roles below come from
the D3 ownership contract; named assignees remain **unassigned**. None is marked
complete by this spec's source investigation.

| Task | Concrete implementation and proposed paths | Responsible role / prerequisites | Observable done evidence |
| --- | --- | --- | --- |
| I1 — closed wire reader | Add pure parsing/types/encoding to `tools/access_telemetry_c1_interchange.py`; formalize exact nested schemas from `schemas.md`, adopt C1.15 v2 unchanged, enforce bounds and snapshot references. Add schema and cross-language hash vectors in `tests/tooling/access_telemetry_c1_interchange/`. | Deployment Adapter Developer; schema/budget review; P2 disposition time rules. | Malformed, duplicate, unknown, wrong-type, BOM, oversized and inconsistent-hash inputs refuse; valid encoding vectors match the actual PowerShell producer. |
| I2 — closed producer binding | Implement registry loading/validation in that module; require exact approved entry, source/helper/input sets, verifier and command/cleanup/role contracts. Keep deployed accepting entries absent until their owning registrations supply them. | Deployment Adapter Developer; P5. | Genuine test entries bind implemented source; missing/held entries and unrelated `.editorconfig` or C2 sources refuse. Tests cannot register a deployed entry. |
| I3 — authority/session consumption | Implement a required adapter boundary and the selected provider integration after P1; no permissive fallback. Normalize authenticated producer/reviewer/decision/scope/status facts; enforce P2/P3/P4 policy. Place generic authority implementation in its owning technical module if required. | Security owns provider/security contract; Deployment Adapter Developer owns consumer; P1–P4. | Through the selected adapter with an isolated test issuer/service: valid decisions resolve; forged, self-approved, wrong-role/scope, expired/revoked and status-unavailable inputs refuse. A mocked boolean alone is insufficient provider verification. |
| I4 — gate semantic verification | Implement registered-schema dispatch, content-versus-record comparisons, source-byte validation, command/count/freshness/cleanup checks and safe custody traversal. Future gate-specific schemas/verifiers remain delivered by their separate owners. | Deployment Adapter Developer; P3/P5/P6. | N01–N13 fixtures reject; genuine complete capture/disposition inputs produce one derived accepted-gate record; no live collector/target is called. |
| I5 — assembly and consumer preflight | Assemble immutable manifest, consume real two-role bundle decisions and publish predecessor/v2. Update `_validate_predecessor` and each C1-reading path in `tools/verify_access_telemetry_lifecycle.py` plus the existing `tools/verify-access-telemetry-lifecycle.py` boundary to require strict versioned validation before use. Do not add a new module-specific CLI/server. | Deployment Adapter Developer / Story 27.4 machinery owner; P7 and prior tasks. | Exactly 25 fixture gates with genuine test receipts pass; missing, mixed, reused, cyclic, changed or downgraded inputs refuse. Every launcher and terminal/offline authorization path proves denial before target/dependency calls. |
| I6 — contract verification and handoff | Add the complete negative matrix and positive nonzero case; update operations interchange guidance with actual schema/provider/consumer contracts and commands. Record exact test totals and limitations after execution, never before. | Deployment Adapter Developer; Security reviews authority cases, Operations reviews custody/session/cleanup; all blocking prerequisites for real use. | The new focused lane passes with zero failures/skips and explicit zero-dependency assertions. Protected status/deployment/history bytes match the baseline; fixture results grant no operational acceptance. |

Proposed focused command, **not executable evidence yet**:

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
```

Use existing tooling regression lanes only where implementation changes touch
their behavior. Record the exact commands and results. New verifier tests must
exercise artifact contents, authenticated provider behavior and denial ordering,
not merely repeat field names from the implementation.

## Decisions and ownership still required

These are blockers for real acceptance, not requests for additional authority.
Every accountable **person** is still unassigned in this spec. Role allocations
express the source contract or a proposed cross-owner decision route.

| ID | Missing decision/input | Accountable role / collaborators | Consequence now | Objective reopen evidence |
| --- | --- | --- | --- | --- |
| P1 | Supported producer/reviewer identity, decision service/receipt protocol, issuer/trust roots, authenticated retrieval, principal mapping and revocation mechanism; owning repository and maintainer. | Security; platform identity/McpCli maintainers if applicable. | No reviewer authentication or receipt validation; I3 production integration and accepted output blocked. | Named owner approves the concrete existing/added provider contract and deployment/support boundary; selected adapter verifies authentic bound receipts and rejects forged/wrong-scope/revoked test receipts. An external service URL alone is insufficient. |
| P2 | Numeric collection duration/age, review lag, disposition precision, approval maximum lifetime, session use lifetime, trusted time/skew, status-proof age, key rotation and unavailable-service policy. Reconcile existing 15-minute pre-launch freshness with long C1 evidence windows. | Security with Platform Operations. | No authorization defaults; absent time/status policy refuses. | Approved immutable policy artifact with every numerical limit and boundary case; clock/expiry/status tests pass, including equality-at-expiry and unavailable status/time. |
| P3 | Authenticated actual cluster identity and scoped session grant, approved capture/review/use actions and principals; custody root/retention/access, immutable receipt retrieval, retained source/command support, cleanup and incident ownership. | Platform Operations; Security validates grants; archive custodian to be named. | Labels do not authorize target access; genuine custody/session eligibility cannot be claimed. | Approved scoped grant and custody/cleanup contract, authenticated immutable retrieval receipts, protected supporting evidence and independently verified actual target identity. No target is contacted in this task. |
| P4 | Authorized review role for each gate, delegation/quorum and independence rules; exact decision dependencies for C1.23 operations, C1.24 non-HA acknowledgement and C1.25 security, and whether their authors may also approve the bundle. | Security / Independent Security Reviewer and Platform Operations Approver; Architecture for dependency topology. | Cannot substitute aggregate decisions or resolve a self-reference/self-approval cycle. | Approved role/action/scope matrix and acyclic evidence dependency contract; alias-principal, self-review, cross-role and cycle tests refuse. Different strings alone do not qualify. |
| P5 | Approved/done gate owner records and exact implemented source/helper/config/schema/verifier/command/cleanup registrations for all 25 gates. PG2 C1.15 needs its own successor registration; the other 23 remain held. | Deployment Adapter Developer for interchange; each separately allocated gate owner for implementation and approval. | Current accepting registry remains empty; real complete assembly refused. | Each separately approved transaction supplies actual producer/verifier files, versioned captures, semantic negatives, independent review and registration/done evidence. This spec adds no story or registry entry. |
| P6 | Whether existing C1.16 closed-window artifacts can establish one eligible authenticated session with complete original facts, or require a fresh versioned capture and independent disposition; disposition/custody availability. Assess recorded incarnation-continuity and remote-response-buffering maintenance limitations separately. | Architecture/Security/Platform Operations; Deployment Adapter Developer owns capture/maintenance evidence. | Closed-window acceptance persists but cannot authorize a new session or be retrofitted into v2. Live eligibility remains unproven. | Independent existing authority/custody and all required capture facts prove eligibility, or fresh authorized capture/review supplies them. Any missing provenance forces renewal; separately scoped maintenance evidence closes only its own limitation. |
| P7 | Approve predecessor/v2 compatibility/dispatch for every consumer and hard refusal of legacy authorization. Resolve the C# runtime qualification gate's historical profile pin separately. | Deployment Adapter Developer / Story 27.4 machinery owner; Architecture and Operations approve runtime gate migration. | Existing structurally valid predecessor remains insufficient authenticated evidence; runtime gate is not demonstrated PG2-compatible. | Approved consumer inventory and migration contract; no bypass or downgrade in focused denial tests. Separate source/test change proves the exact current runtime profile while preserving its non-Production expiry/renewal boundary. |

An owner resolves a prerequisite by supplying a reviewable artifact and real
evidence, not by adding `approved`, `done` or `independent` strings. If provider,
producer renewal, runtime-gate migration or registration work has its own
independently demonstrable outcome, author a separate compliant scope; do not
expand I1–I6 into an umbrella implementation.

## Readiness and completion

Spec review can assess the proposed schema and task boundaries now. P1–P7 remain
unresolved and must not be represented as approvals. Parser scaffolding and
refusal fixtures can proceed under a later implementation task; acceptance and
provider integration remain blocked on their relevant decisions.

Repository preparation completion would mean the implemented consumer and
negative/positive fixtures have passed review. Operational acceptance additionally
requires actual eligible capture/custody/authority and all real registered gate
evidence. Neither outcome closes Story 27.4/A41 or enables Production; those
retain their separate full close-out requirements.
