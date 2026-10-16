---
title: 'Story 27.4 sole-owner qualification and A41 close-out'
type: 'feature'
created: '2026-10-12'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'b1d8014fa0dbacaf71528ddda74ff83cf19ca3ca'
context:
  - 'docs/operations/story-27-4-execution-plan-2026-10-15.md'
  - 'docs/operations/story-27-4-isolated-target-plan-2026-10-16.md'
  - '_bmad-output/implementation-artifacts/27-4-isolated-target-approval-2026-10-16.md'
  - 'docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json'
  - 'docs/operations/access-telemetry-c1-owner-verification.md'
  - '_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-25.md'
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - 'docs/operations/access-telemetry-owner-handoff.md'
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md'
  - '_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out.md'
  - '_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4 needs an owner-approved bounded run, complete PG2 C1 predecessor, live C2–C6 evidence and A41 publication proof.

**Approach:** Use the adopted sole-owner policy, prepare one bounded run record for owner approval, collect and verify C1 and qualification evidence, then record owner acceptance and verify A41 publication.

## Boundaries & Constraints

**Always:** Bind authority and evidence to approved `PG-ONPREM-2` SHA-256 `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`. Check current owner approval, expiry and withdrawal. Require 25 verified C1 gates in one eligible session, explicit fault/purge scopes, distinct owner-accepted C5/C6 records, and disabled gate, empty Lease and zero lifecycle/clock replicas at finish. Use private owner-controlled hashed/readback evidence for non-Production qualification; separate receipt services, independent human reviewers and external immutable custody are not required for this mode.

**Never:** Treat policy adoption, historical PG1 C1.15, closed-window C1.16, fixtures or role labels as a live run approval. Do not claim independent human review for the sole owner’s decisions. Contact an unapproved target, enable Production writes, weaken predecessor refusal, expose secrets, change the zero-replica overlay, or resolve A41 and its sprint action before publication proof.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Outcome | Failure Behavior |
|----------|---------------|------------------|------------------|
| Hold | Missing, expired, changed or withdrawn owner run approval or C1 | `RECHECK_ONLY`; no target or A41 mutation | Name the missing fact |
| Qualification | Current owner run approval, verified C1, scoped C2–C4 actions | Hash-bound packets prove acknowledgement, retention, faults, audit continuity and privacy denial | Reject partial proof and restore disabled/empty/zero |
| Close-out | Owner-accepted C0–C6, terminal proof and publication approval | Exact A41 transition, protected bytes intact, remote containment proven | Reject drift or missing approval |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — current owner handoff; earlier institutional response request is historical.
- `tools/access_telemetry_c1_authority_boundary.py`, `tools/access_telemetry_c1_producer_bindings.py`, `tools/verify_access_telemetry_lifecycle.py` — authority and predecessor refusal; preserve fail-closed behavior.
- `tools/verify-access-telemetry-lifecycle.py`, `tools/access_telemetry_c{2,3,4}_producer.py` — fixed scenarios and close-out commands; use `docs/operations/access-telemetry-lifecycle.md`.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — canonical C0–C6 matrix.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/pg2-c1-i2-operations-security-request.md` — prepare the owner run record with actual target, private evidence root, exact actions and windows; obtain scoped owner approval.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — verify PG2 C1.15 renewal, C1.16 eligibility or recapture, 23 other successors, 25 distinct gate passes and distinct owner Operations/Security records.
- [ ] `tools/access_telemetry_c1_authority_boundary.py`, `tools/access_telemetry_c1_producer_bindings.py`, `tools/verify_access_telemetry_lifecycle.py` — implement/verify migrated owner-mode interchange accepts the eligible predecessor; preserve refusal while absent.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — with the approved non-Production action scopes, run C0/C2–C4 and retain hash-bound packets and cleanup proof.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — record owner-accepted C5/C6 and terminal proof.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — apply preflight-approved A41 changes; verify postflight and remote publication.

**Acceptance Criteria:**
- Given missing, withdrawn, expired or changed owner run approval, when readiness or a producer is invoked, then it refuses before target access and leaves Production writes and A41 unchanged.
- Given accepted same-session C1 and scoped qualification approval, when C0/C2–C4 run, then hash-bound packets prove required lifecycle behavior and final disabled/empty/zero state.
- Given accepted C0–C6 and terminal evidence, when close-out runs, then only registered A41 paths change, protected history stays byte-identical, and the published commit contains the approved transition.

## Implementation Notes

2026-10-12: Platform P1 v1 infeasibility at `e229df9a58ee6899f2b24ba51f652435bc36dc62` (`docs/implementation/pg2-p1-v1-feasibility-2026-10-12.md`, SHA-256 `b879819874934937cea19b406edd66c2657ded9527348ce4a8b626227865dd6e`) is recorded in the I2 request. The C1.15-only packet is not enrolled. The owner-choice and operational-record blocker is `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/c1-15-only-signed-evidence-packet-decision.md`, SHA-256 `d85537fbbf3a50eeff913a6c3363fbd3f0046ae112b0ce3d1f2b37cbce36eb04`. The first execution task stays open because no adoption, target, custody, principal, or grant record was returned. Disposition remains `RECHECK_ONLY`.

2026-10-12 current-pin recheck: Memories HEAD `f4c7d68f619f376debca440387428139a61080cc` records Platform `977f2e01347226ac727b707aee4f140f5dc8b010`, which contains the same P1 v1 infeasibility report and unchanged cited contract/qualification blobs. The [response audit](pg2-c1-i2-operations-security-request.md#2026-10-12-current-pin-response-audit-for-story-274-spec-14) records that no authenticated operational response, current status/revocation proof, accepted eligible C1 or separate action grant was supplied. Preserve the refusal and `RECHECK_ONLY` disposition. All six execution tasks remain open; live qualification, independent decisions, terminal proof and A41 publication remain pending. No target or A41 mutation occurred.


### 2026-10-15 authorized operational continuation

The owner explicitly resumed these six live tasks, with completed Spec 25 as a
dependency, and requested standalone preparation, missing collector integration,
actual same-session C1 and C0/C2-C6, terminal cleanup, governed A41 publication,
and a separately approved Production rollout. Standing sole-owner approval
persists; never ask for it again. Actual target/action/window approval and
post-execution result decisions remain separate. Preserve protected history and
mark no closure without its required live/publication proof.

Current preparation and the concrete downstream plan are in
[execution plan](../../docs/operations/story-27-4-execution-plan-2026-10-15.md).
A real standalone checkout, without initialized Git links, passed the existing
owner safety preflight and isolated revision/clean check. Exact source and
guidance snapshots are retained in the private evidence root named in that plan.
No cluster contact or live passing evidence has occurred in this continuation.

Named immediate operational blocker **D0**: fresh bounded read-only discovery
needs a source-bound collector that consumes outside-approved exact plan bytes,
current local approval/withdrawal, expiry, credential metadata and local API/CA
identity, and executes only the prepared twelve metadata projections. Implement
only this directly usable collector integration while run approval is missing;
do not broaden into speculative gate implementations without actual target facts.
Use `tools/access_telemetry_owner_discovery.py` and focused tests under
`tests/tooling/access_telemetry_owner_discovery/`. It must not create approval,
relax the existing owner/predecessor APIs or confer gate acceptance. It accepts
unknown target classification only for this exact metadata-discovery scope.

Read the private `discovery-plan.json` named by the execution plan. Preserve
the twelve fixed query vectors and bounds. Bind runner bytes into that plan
before final owner presentation. Require an outside approved plan digest and a
separate actual owner-decision record binding the plan and runner, plus freshly
read matching withdrawal state before each command and at final readback.
Absent approval, withdrawn/mismatched/malformed/private-path drift, expired
window, source or local API/CA/credential metadata drift must refuse before
network contact. Never follow a symlink for plan/decision/status/credential
inputs; the actual credential file is canonical, whereas `.kube/config` is a
symlink and must not be used. Credential values never enter evidence.

Retain command IDs/argv, UTC start/end, exit codes and bounded raw projection
stdout/stderr in exclusive private files, with hashes and readback. Use fixed
Kubernetes `get` vectors only; no exec, log, Secret, writes, retry loops or
ambient alternate context. Enforce aggregate/per-command time and output
bounds, close children on failure, and mark failures/NotFound as observations
requiring disposition, never qualification passes. Metadata identity/classification
observations cannot themselves authorize later mutations. Test denial-before-
spawn, exact allowed command selection, expiry/withdrawal/drift and retained
receipt behavior using offline test doubles; label them as offline verification.

After D0 preparation, report remaining gates and approvals concretely. Do not
contact the cluster without the owner's fresh discovery-plan approval; no such
approval has yet been supplied. Full live qualification remains pending. The
existing 72-hour owner collection bound cannot cover C3's 168-hour observation;
prepare explicit phase/renewal handling without weakening either technical bound.
Do not stage, commit, push, change branches/dependencies, mutate sprint/A41 or
Production during this preparation. Leave the six live tasks unchecked.

## Spec Change Log

- 2026-10-16: Owner explicitly approved the exact isolated KVM bootstrap and target-profile replacement after reviewing both hashes. Frozen current PG2 hash changes to the approved isolated-target manifest; technical gates, workload, history protection and separate Production authority remain unchanged. The first bounded bootstrap stopped at its OS allocation timeout and proves no qualification pass.

- 2026-10-12: Explicit owner “yes” adopts `sole-owner-qualification/2026-10-12` and renegotiates the frozen approval/custody requirements under the approved sprint change. Same owner may produce and accept evidence with mandatory self-review disclosure. Technical checks and live/publish scopes remain required. Prior implementation notes are dated history under the superseded policy; they do not reinstate institutional prerequisites.

## Review Triage Log

## Verification

**Commands:**
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — authority refusal tests pass.
- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — lifecycle and A41 guards pass.


2026-10-12 executed results: the authenticated-interchange suite passed 242 tests in 85.213s and the lifecycle suite passed 83 tests in 34.426s, both exit 0 with bare `OK` and zero failures/errors/skips. Local receipt `/tmp/story-27-4-spec14-oo85qkwd` retains commands, logs, exits, source/dependency identity and byte baselines; durable identifiers and the offline-only boundary are in the [canonical evidence addendum](tests/27-4-retention-verification-evidence.md#2026-10-12-spec-14-current-pin-readiness-hold). `git diff --check` exited 0 before these notes. The hold row is verified by refusal tests; no authorized live qualification or close-out row is claimed as executed.


2026-10-12 owner-mode first slice: the separately tracked
[owner-record inspection spec](spec-pg2-c1-sole-owner-run-record-inspection.md) is
done after three review layers, corrected timezone regressions and 25 focused /
267 full affected tests, with no deferred review finding. Policy adoption and
pure inspection do not complete the six live tasks above; existing deployed
consumers remain closed. See the canonical evidence addendum for exact results
and preservation proof.


2026-10-12 owner approval/acceptance continuation: the owner said “I approve and
accept”, accepting the sole-owner approach and presented 25 focused / 267 full
offline test results. The
[acceptance record](27-4-owner-approval-and-acceptance-2026-10-12.md) retains
exact snapshots in a private readback-checked archive. Bounded read-only queries
reached the configured cluster: the existing telemetry deployment has one ready
replica and the isolated qualification namespace is absent. No target mutation
or live gate acceptance occurred. Future collectors, full owner-mode integration
and concrete mutation setup remain agent implementation work; no repeat policy
approval or institutional grant is requested.


### 2026-10-15 D0 preparation implementation

Implemented only the authorized immediate D0 slice in
`tools/access_telemetry_owner_discovery.py`, with focused offline cases in
`tests/tooling/access_telemetry_owner_discovery/test_owner_discovery.py`.
Existing owner/predecessor APIs are unchanged and all six live tasks remain open.
The collector requires the outside exact plan digest, a separate actual owner
plan/runner decision and fresh matching withdrawal status; absent approval,
expiry, drift, unsafe paths or widened vectors refuse before a target query.
Unknown target classification is accepted only for these twelve metadata reads.

The runner separately binds its uncommitted bytes, rather than claiming them
inside baseline commit `8cc8fda7`. Current runner SHA-256 is
`71deee01aacdf88cc27bdb6113ed3dd7e0b8564c45e6f3a9dbccdedca49504d5`;
proposed plan SHA-256 is
`25adcae1ae4aa35c596bf6d121de1f0235e8ac19368cb1b7c7bad2cb22dca6d5`.
The plan retains the original twelve vectors/bounds, adds exact credential
metadata and fixed profile/workload pins, and leaves approval absent. Current
closed status matches that plan and is `withdrawn: true`, with an all-zero
missing-decision sentinel. No executable approval was created.

Canonical credential bytes are locally validated and passed only through a
sealed anonymous read-only input to the installed client. A pathname replacement
at spawn cannot redirect the API/credentials consumed by that child; named-file
drift then prevents a subsequent query. Owner-only regular credential-file
privacy is enforced without changing its public parent directory. Inputs and
ancestors are acquired without following symlinks. Raw bounded projection streams,
argv/IDs, UTC times, exits and hashes receive exclusive private creation and exact
readback; all observations require owner disposition and grant no qualification
credit. Child groups/pipes close on timeout, overflow, interruption and failure.

The execution plan documents the runnable invocation, closed plan/decision/status
formats, current missing approval, later concrete gates and a proposed C3
60-hour phase/renewal schedule. That schedule is non-executable: existing 72-hour
owner, C1/P7 eligibility/freshness, 168-hour cohort, 24-hour reclamation and
nine-day producer bounds remain unchanged; separately implemented/verified
continuation and actual phase authority are still required. It cannot extend an
expired grant, refresh C1 evidence, relabel a session or reset cohort clocks.

Named denial evidence includes
`test_missing_actual_decision_denies_before_process_or_target`,
`test_exact_twelve_queries_no_secret_log_exec_write_or_foreign_namespace`,
`test_local_api_ca_context_exec_proxy_or_external_key_drift_refuse`,
`test_mutable_credential_replacement_cannot_redirect_spawned_child`,
`test_mid_command_runner_source_drift_stops_before_second_command`, and
`test_terminal_status_is_checked_after_receipt_readback`.
These attach fail-closed target/namespace/attribution evidence to the new discovery
authority surface, preserving the Story 20.2 denial-before-dependency and Story
24.3 verifier-marker boundaries. Tests are offline doubles or local processes;
they prove no live gate, C1 registration, acceptance, terminal or publication result.

Actual proposed-plan denial is retained at
`evidence/discovery/missing-approval-refusal-1b752f9deaa543c099ccafb0b60ab55a.json`
under the execution plan's private root: `owner-discovery-owner-decision-required`,
zero process spawns and zero target calls. The supplied test digest was a
preparation binding, explicitly not owner approval. The independently evaluated
local `kubectl config view` fixture reads the dotted environment/gate JSON keys
and produces literal tabs/newline. Its namespace projection returns exact hex
`66697874757265097569642d666978747572650954657374095175616c696669636174696f6e0a`.
That local-only evaluation uses an inert private config, never target credentials.
No cluster was contacted by this implementation, no A41/sprint/Production bytes
changed, and no stage/commit/push/branch/dependency operation occurred.


D0 preparation verification completed at `2026-10-15T16:54:51Z`. All commands
below exited 0 with bare `OK`, zero failures, errors or skips. These are offline
verification results; the parent's later bounded owner-approved discovery has
its own actual decision/status and command receipts.

| Exact command | Measured result |
| :------------ | :-------------- |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_owner_discovery -p 'test_*.py' -v` | 31 tests in 4.132s. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` | 83 tests in 23.727s. |
| `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` | 640 tests in 463.820s. |
| `git diff --check` | Exit 0 after the implementation/verification notes. |

The execution plan's private evidence root retains exact raw combined logs,
commands, exits, counts and hashes in
`logs/d0-verification-20261015T165451Z-a4513ab3/commands-exits.json`, SHA-256
`a6b51ca3d76e22f33a94904a9a5724e13e9f40af3b9ba7c4a96be13bdb5e66ed`.
The focused/lifecycle/interchange log SHA-256 values are respectively
`7369c6feeee5b492683208f636b5d36568afb75e24244914a83706111dc24fff`,
`61369df04ac3c40de8316a7a2654eeb49a8c879ab9e12e71dac1bff21d8a8f44`,
and `1d04af96119e00dc133fac95910cd69d07e5e5e6bce4b59479f4fb1a67837e75`.
Exclusive owner-only files/directories, exact-byte readback and hashes were
verified at retention. Later owner decision/status and target observations must
be read from their current dedicated records, not these earlier held-state notes.

### 2026-10-15 actual owner-approved bounded discovery continuation

The owner approved exact discovery plan
`25adcae1ae4aa35c596bf6d121de1f0235e8ac19368cb1b7c7bad2cb22dca6d5`.
The root agent retained the actual decision, performed fresh binding/expiry/scope
checks, executed all twelve metadata queries and independently verified all
retained stream hashes. Exit 0 signifies completed discovery, not passing gates.
The receipt SHA-256 is
`cf26b491256421c931c65bf5e1921b82bb61aef5990b0bd94508409192c7079e`.
Observed qualification namespace is absent; existing namespace classification
remains unknown. Runtime application image digest candidates now exist in
metadata, but their signature/source/compatibility evidence is unverified.
The local discovery budget is complete and its execution status was held to
prevent reuse; the standing sole-owner policy persists. See the actual-discovery
section of the [execution plan](../../docs/operations/story-27-4-execution-plan-2026-10-15.md)
for UIDs, evidence locations, exact results and remaining target/impact question.
All six live tasks remain unchecked. No C1/C0-C6/terminal/A41/Production pass or
closure is claimed. This supersedes the earlier dated pending-discovery notes
for the D0 action only; it supplies no qualification mutation or fault authority.

### 2026-10-16 separate qualification target selected

The owner said “apply recommended” after considering reuse versus a separate
cluster. Apply the separate-target route while existing `jpiquot@local` remains
unclassified; do not reuse its completed discovery budget or touch its workloads.
The [isolated target plan](../../docs/operations/story-27-4-isolated-target-plan-2026-10-16.md)
records the prepared one-guest KVM configuration, verified public OS image, private
seed/keys, actual filesystem/ACL/KVM checks, exact bounded bootstrap and transparent
candidate profile delta. This is independent preparation, not exact bootstrap or
replacement-profile approval, gate acceptance, live qualification or closure.

Named blocker **QT1**: the original frozen PG2 hash cannot silently describe a
substituted node/resource envelope. The candidate and its exact hash are retained
outside the approved profile, with unchanged technical pins/capabilities/workload.
Obtain only the genuinely missing exact bootstrap/target-profile decision; never
ask for the adopted sole-owner policy again. No VM or new cluster is running, no
640-GiB disk reservation exists, and current consumers still refuse missing or
foreign-profile evidence. Do not migrate fixed profile constants or frozen intent
before the owner explicitly adopts the concrete change. Later real integration
work must be tied to observed target/gate blockers. Existing 72-hour authority,
168-hour retention and predecessor-freshness gates remain required. Protected
history, all six unchecked tasks, A41 and Production remain unchanged.

### 2026-10-16 exact bootstrap/profile approval and stopped attempt

The owner answered “yes I approve” to the exact prepared bootstrap and replacement
profile. The [actual decision/result](27-4-isolated-target-approval-2026-10-16.md)
binds the hashes and records the real one-attempt bootstrap: base conversion
passed, OS expansion hit its approved 300-second timeout (exit 124), no data disk
or VM was created, and zero guest commands executed. The retained 40-GiB OS file
has full Windows/Linux allocation readback and private DACL; no matching child
process, QEMU PID or QMP socket remains. The budget is consumed/closed, not an
owner withdrawal. Never replay or widen that failed plan. No gate pass or task
closure follows from the adopted profile or these disk observations.

This owner's concrete adoption renegotiates only the frozen current PG2 hash to
`95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`.
All other frozen requirements stay unchanged. The canonical approved manifest is
[retained in source](../../docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json);
its exact canonical identity/capabilities/workload must hash to this value.

The immediately authorized implementation slice is **QT1-P**, necessary to use
the approved new target: migrate the active PG2 canonical profile, current owner/
C1.15 target pins, current authority/tooling selectors, non-Production C# gate and
qualification overlay profile references to the exact adopted manifest. Preserve
all backend/runtime/security/deployment input hashes, 400-GiB boundaries, workload,
closed schemas, existing time limits and unconditional deployed-binding refusal.
Keep PG1 and the former PG2 canonical identity recoverable/unchanged as historical
material; update current-selector tests without rewriting their historical source
fixtures, planning artifacts, ADR, ledgers, sprint or acceptance history. Add
meaningful negative tests for former/foreign hashes and target/envelope drift;
assert the full canonical new manifest, technical invariants and default disabled/
empty/zero render. Run narrow affected Python lanes and appropriate C# gate/
deployment checks, retaining exact commands/results and any environment blocker.

Do not change the source-bound D0 runner `tools/access_telemetry_owner_discovery.py`
or its historical approved plan in this slice. Its digest and old-target twelve
queries are retained discovery-only history, not current qualification authority.
Do not implement speculative accepting gate registrations or C3 renewal admission
without actual target facts. No target operations, disk changes, retry, Kubernetes
install, source contact, Git stage/commit/push or Production write is authorized
by this implementation handoff. The parent handles operational recovery; tie any
additional implementation to a demonstrated blocker. Leave all six live tasks
unchecked and do not advance workflow closure.
