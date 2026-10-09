# Implementation tasks and unresolved prerequisites

Tracking ID: `SPEC-pg2-c1-authenticated-predecessor-interchange`.
Review: proposed. Integrated I1-I6 verifier: incomplete; common wire and named-owner
principal-separation preparation and narrow read-only GitHub bundle review
observations are implemented separately. No story number, sprint registration,
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

Focused command executes wire/principal preparation and real local TLS GitHub
review observation fixtures; it does not yet prove the complete proposed I1-I6
authenticated verdict:

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
```

Use existing tooling regression lanes only where implementation changes touch
their behavior. Record the exact commands and results. New verifier tests must
exercise artifact contents, authenticated provider behavior and denial ordering,
not merely repeat field names from the implementation.

## Decisions and ownership still required

These are blockers for real acceptance, not requests for additional authority.
Jérôme Piquot is the owner-designated Operations/Security bundle approver
following the explicit 2026-10-07 single-owner decision. Other accountable people
remain unassigned; role allocations express the source contract or a proposed
cross-owner decision route. The separately authorized GitHub prerequisite below prepares only bundle
review identity/state consumption; this appointment supplies no gate acceptance,
role grants, session or custody evidence.

| ID | Missing decision/input | Accountable role / collaborators | Consequence now | Objective reopen evidence |
| --- | --- | --- | --- | --- |
| P1 | Complete producer/gate/session identity and decision service/receipt protocol, issuer/trust roots, authenticated retrieval, principal mapping and revocation mechanism; owning repository and maintainer. Narrow GitHub bundle reviewer/state retrieval is prepared separately below. | Security; platform identity/McpCli maintainers if applicable. | Narrow bundle reviewer/state observations are prepared; complete I3 production integration, other authority facts and accepted output remain blocked. | Named owner approves the concrete existing/added provider contract and deployment/support boundary; selected adapter verifies authentic bound receipts and rejects forged/wrong-scope/revoked test receipts. An external service URL alone is insufficient. |
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

## Recorded owner decision: partial P4 resolution (2026-10-07)

The owner chose “Allow Jérôme to approve both roles”, after being told that this
changes the existing two-reviewer rule. The permitted same-principal exception
is limited to authenticated `github:user:6775094` (`jpiquot`, Jérôme Piquot).
Exactly two distinct role decisions and receipts remain required. Neither role
may be a producer on any capture in the manifest; all scope, time, role, issuer,
revocation and session requirements remain. This reduces separation of duties
between review roles and must be reconsidered before Production activation or
an account/role/producer-identity change.

The bounded pure separation predicate and fixtures are implemented as the
separate single-owner-policy prerequisite. They do not implement I3's authority
adapter or any deployed acceptance path. P4 is partially decided, not complete:
delegation/quorum and the approval-gate dependency topology remain open. P1-P3,
P5-P7 and real acceptance remain unresolved; the previously completed runtime
PG2 source/test correction does not resolve P7 consumer migration.

## Readiness and completion

Spec review can assess the proposed schema and task boundaries now. P1–P7 are
not fully resolved; retain the explicit partial P4 owner decision above without
claiming completion of the remaining prerequisites or actual gate acceptance.
Parser scaffolding and refusal fixtures can proceed under separate implementation
tasks; acceptance and provider integration remain blocked on their relevant
decisions.

Repository preparation completion would mean the implemented consumer and
negative/positive fixtures have passed review. Operational acceptance additionally
requires actual eligible capture/custody/authority and all real registered gate
evidence. Neither outcome closes Story 27.4/A41 or enables Production; those
retain their separate full close-out requirements.


## Prepared read-only GitHub review consumption (2026-10-07)

The separately scoped implementation spec is
`_bmad-output/implementation-artifacts/spec-pg2-c1-github-review-consumption.md`.
Its prerequisite library adds authenticated bounded Platform HTTPS retrieval,
strict Memories two-role/body/scope/time checks and genuine local TLS fixtures.
The owner exception and existing wire primitives are reused unchanged. It
returns checked immutable observations only; neither I3 nor I5 is marked done,
no accepting provider/registry entry is added, and P1-P7 remain incompletely
resolved. Numerical policy values and current scoped role grants are required
independently authenticated caller inputs, not defaults or adopted policy.

The supported GitHub machine review body and explicit trust prerequisites are
published as local reviewable documentation in
[the operations contract](../../../docs/operations/c1-github-review-contract.md).
The local TLS issuer is visibly distinct from production, and missing Platform
source fails test discovery. CI initializes only the root-declared Platform
checkout at its pinned commit. Platform publication and a separate authorized
root gitlink advance are outstanding; this unpublished pinned checkout is not
shippable. The legacy consumer and Story 27.4/A41/Production holds persist.

Exact executed verification results are recorded in the implementation spec's
Verification section. The focused tests include unchanged wire/separation
regressions, current owner and distinct-reviewer positive cases, edit/dismissal/
scope/producer/time denials, status ageing across both requests, trusted-context
reuse expiry, and bounded real HTTPS/TLS/redirect/HTTP/body failures. A resolver
stall inside the fixture HTTPS worker verifies the deadline includes DNS.
Fixture success proves no live grant, session, custody or eligible producer.


Resumed review (2026-10-08): independent review corrections add bounded inherited
worker-bootstrap checks, suppress keylog configuration before context creation,
make repository-root imports usable, reject elapsed regressions, and cover the
remaining real TLS and temporal boundaries. Current verification and source
identities are recorded in the implementation spec. Platform's separate published
commit `7192c313edc39c6f698bc25c5475ff41d12197cd` contains the earlier library;
final corrections still need Platform publication followed by the separately
authorized Memories gitlink advance. This workflow preserves the existing pinned
gitlink and performs neither action. Full I1-I6 acceptance and all live holds
remain unchanged.


## Owner-directed policy/session authority preparation (2026-10-08)

The owner selected the recommended bounded GitHub-backed policy/session adapter,
bundle role integration and genuine TLS gate-review chain. The isolated scope is
`spec-pg2-c1-github-authority.md`; the reviewable contract is
[the authority operations contract](../../../docs/operations/c1-github-authority-contract.md).

Platform adds generic immutable exact-review/body authentication over its existing
transport. Memories adds closed policy/session/gate bodies, independent bootstrap
root and explicit bounded time/permissions, exact current PG2 scope and Ref
bindings, subset grants, acyclic parent dependencies and complete-chain freshness
revalidation. Existing bundle role policy is derived from authenticated session
grants. All actual capture execution/provenance/custody/target/manifest facts
remain independent inputs. The named-owner two-role exception retains separate
decisions and producer exclusion. No target/deployment operation or actual grant
occurs, and no accepted gate/predecessor/execution output is added.

Dedicated real TLS authority cases cover positive policy/session/gate-parent/
bundle chains and early target/tenant/session/role/action/producer refusals,
withdrawal and exact bytes/commit/user matching, strict schema/envelope handling,
elapsed regression/reuse/suspension and earliest whole-chain expiry/status age.
Exact commands, counts and logs are recorded only after execution in the scoped
implementation spec and `/tmp/pg2-c1-authority-lbzyqupl`.

I3/I5 and P1–P7 are still incomplete. Operational bootstrap adoption, eligible
capture provenance, custody, registrations/semantic verification and accepting
consumer migration retain their recorded owners/prerequisites. No operational
dependency topology is chosen by fixtures. Publication of Platform changes and
a separately authorized matching root gitlink advance remain outstanding;
27.4/A41/Production and the prior history remain held.


## Prepared offline I2 registry/source inspection (2026-10-08)

The separately authorized bounded slice is
`spec-pg2-c1-offline-producer-bindings.md`; its actual API and proof limits are
published in [the inspection contract](../../../docs/operations/c1-producer-binding-inspection-contract.md).
`tools/access_telemetry_c1_producer_bindings.py` adds frozen thirteen-field
registry inventories and J1 hashes, explicit retained UTF-8 byte pairs and
normalization metadata, exact C1.15 source-set/commit-label comparisons and
independently recomputed SHA-256/Git blob SHA-1 receipts. It reuses I1 unchanged,
bounds each byte form to 1 MiB, and counts registry/capture JSON and both source
forms together under a deduplicated 32 MiB limit. Deployed lookup always refuses.

Independent isolated fixtures in `test_producer_bindings.py` cover literal
J1/Git-OID vectors, all nineteen source identities/commit labels, exact registry
shape/order/path/Ref checks, missing/extra/substituted sources, clean/blocked/
recheck state, transform/mode refusals, immutable results, exact byte budgets
and cross-JSON/source deduplication. Named negative evidence includes
`test_missing_extra_substituted_duplicate_and_mutable_sources_refuse`,
`test_each_source_receipt_digest_and_oid_are_recomputed`,
`test_transform_and_nonregular_mode_metadata_refuse`,
`test_valid_neutral_blocked_and_recheck_drift_captures_remain_ineligible`,
`test_manually_constructed_registry_wrappers_cannot_establish_success` and
`test_every_api_has_zero_filesystem_process_network_and_ambient_calls`.

Executed commands from the repository root:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_producer_bindings.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
```

The focused lane passed 24 tests and the full interchange regression lane passed
191 tests, each with zero failures/errors/skips. All twelve protected receipt
hashes and both unchanged canonical history/workspace receipts matched
`/tmp/pg2-c1-i2-2z7veqau`; the parent scope-decision draft edit was separately
authorized and left untouched. Existing I1/source/consumer files and nonrecursive
root gitlinks remain unchanged; `git diff --check` passed.

This preparation authenticates no Git repository/commit, actual attributes,
source custody, producer execution, registration, command grammar or role policy.
No accepting entry, accepted artifact, authority integration, consumer migration
or dependency is added. I2–I6 and P1–P7 remain incomplete, and no checkpoint,
whole-story/sprint/history status, Story 27.4/A41 hold or disabled Production
configuration advances.

Review correction receipt (2026-10-08): the same implementation agent applied
bounded source/registry cardinality guards, content-free UTF-8/checksum-provider
refusals and stronger named-digest, ordering and denial-barrier coverage. Final
parent verification passed 28 focused and 195 full interchange tests with zero
failures/errors/skips. The strengthened positive test rejected an in-memory
executed/blob digest swap. Three independent review layers were triaged in the
separate slice spec; nothing was deferred. The operations contract now retains
the exact twelve-file offline preservation manifest and executed recheck result.
The initial 24/191 receipt above remains historical; all stated operational
holds and incomplete I2–I6/P1–P7 prerequisites remain unchanged.


## Prepared offline C1.15 declared-observation pins (2026-10-08)

The separately authorized bounded slice is
`spec-pg2-c1-15-offline-observation-pins.md`; its actual API, literal pins and
proof limits are published in [the observation inspection contract](../../../docs/operations/c1-observation-pin-inspection-contract.md).
`tools/access_telemetry_c1_capture_semantics.py` reuses the unchanged immutable
snapshot, structural capture reader and content-free refusal helpers. It
requires observed declarations and compares the published PG2 profile/workload,
five target fields, per-Pod runtime and either approved OCI index/child digest
exactly. Its frozen result retains the identical capture snapshot and derives
Pod count/names from Pods in their input order. Mixed index/child and independently
supported raw prefixes remain inspectable; no repository-prefix policy is added.

Independent fixtures in `test_capture_semantics.py` execute every API case under
filesystem/process/network/environment/clock denial barriers, with zero calls.
Named evidence includes
`test_each_profile_and_workload_semantic_substitution_is_structurally_admitted`,
`test_each_target_substitution_is_self_consistent_before_pin_refusal`,
`test_runtime_changes_are_structurally_valid_when_all_pods_agree`,
`test_each_pod_image_substitution_refuses_including_a_later_bad_pod`,
`test_single_pod_captures_derive_nonconstant_count_and_selected_name`,
`test_dirty_declarations_are_inspectable_but_source_eligibility_independently_refuses`
and `test_result_and_retained_nested_values_are_frozen`.
Every new semantic-negative fixture first succeeds through structural parsing.
Changed profile IDs and contradictory one-Pod runtime declarations already
refuse structurally and are covered separately, preserving that existing reader.
A parent acceptance audit found that the initial two-Pod-only positives did not
reject a constant-count mutation; one-Pod positives now verify both possible
selected names and a different count. Independent final review is tracked in
the separate spec.

Executed from the repository root with zero failures/errors/skips:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture_semantics.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
git diff --check
```

The final focused run passed **20 tests**; the full interchange run passed
**215 tests**. Exact logs and working-byte preservation receipts reside in
`/tmp/pg2-c1-i4-observations-41meebiy/`. Rechecks compare the original 6,137
nonledger working-file hashes and this ledger's complete 21,169-byte prior
prefix, plus HEAD/root gitlinks and the permitted changed-path inventory.
The source reader, PowerShell producer, prior I2 preparation and existing user
changes retain their original bytes.

I2–I6 remain incomplete: source inspection is separate, deployed lookup still
refuses, full authority consumption and registered semantic dispatch are absent,
and no accepted-gate/all-gate assembly or strict consumer migration is supplied.
P1–P7 retain their recorded unresolved/partial decisions and owners: operational
provider adoption, numerical time/status policy, actual target/session and
custody/cleanup, complete role/dependency policy, gate registrations, C1.16
eligibility/renewal and strict predecessor/v2 consumer dispatch remain required.
The previously prepared PG2 runtime profile correction does not resolve P7;
Platform authority publication and a separately authorized root gitlink advance
remain outstanding. Matching dirty-development declarations here do not satisfy
independent source eligibility. Simplified structural commands do not prove
registered grammar, execution, selection completeness, streams or provenance.

No registry/acceptance entry, accepted artifact, execution handle, authority or
consumer integration, dependency, live operation or submodule change is added.
Story 27.4/A41, Production, sprint/checkpoint and historical holds remain exactly
as before this standalone preparation.

Final independent review correction receipt (2026-10-08): all three review
layers returned; seven blind-review findings were individually triaged and
corrected, with no deferred work or intent change. Known prebound dependency
aliases and access/descriptor/removal/rename/write operations now have counted
denial coverage. Self-consistent fullwidth declarations refuse without Unicode
normalization; three-Pod captures prove complete derived counts/names and
final-Pod image refusal. Fixture wording identifies the actual neutral labels
and evidence-directory argument. The inspector source remains unchanged.

Parent final verification passed **24 focused** and **219 full interchange**
tests, with zero failures/errors/skips. All four reproduced mutation gaps are
now detected without source edits. The supporting
[verification receipt archive](../../implementation-artifacts/tests/pg2-c1-15-offline-observation-pins/verification-receipts.zip)
retains the original 6,138-file byte manifest, exact 21,169-byte ledger prefix,
preservation checker, executed logs and source/hash records. Its operations
contract gives the extraction/recheck command; the separately tracked spec
records exact final identities and results. I2–I6/P1–P7 remain incomplete,
Story 27.4 pending, A41 open and Production disabled. No acceptance, live
operation, commit, push, dependency or root gitlink change occurred.


## Offline I2 registration and Git-source corroboration (2026-10-09)

The isolated follow-on scope is
`_bmad-output/implementation-artifacts/spec-pg2-c1-i2-registration-source-provenance.md`.
The proposed fixture registration statement binds every thirteen-field entry
declaration except its self-referential registration Ref, plus the full source
commit. Its three Refs authenticate exact retained registration, command and
role-policy snapshots. The added local corroborator verifies one explicit clean
Git checkout and HEAD, exact tracked regular blobs, effective supported
attributes and safely read working bytes. The precise API, fixture schemas and
proof limits are in
[the producer-binding inspection contract](../../../docs/operations/c1-producer-binding-inspection-contract.md).

This remains nonauthoritative offline corroboration. No actual owner approval,
registration authority, custody, execution, disposition or deployed eligibility
is established; deployed lookup still refuses. P1/P5 owner evidence and the
remaining I3–I6/P1–P7 prerequisites stay held. Story 27.4 remains RECHECK_ONLY
and in progress, A41 remains open, and Production writes remain disabled.

Final offline verification passed 15 focused registration/provenance tests and
234 full interchange tests with zero failures, errors or skips; `git diff --check`
passed. Three independent review layers were triaged and their concrete
provenance/test gaps corrected, with no deferred finding. The two preexisting
Story 27.4 edits, sprint status and Production-disabled overlay retain their
pre-work byte hashes. These fixture results confer no I2 operational closure,
gate registration or Production authority.
