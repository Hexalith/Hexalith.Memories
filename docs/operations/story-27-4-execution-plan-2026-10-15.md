# Story 27.4 bounded operational execution plan

Prepared 2026-10-15; updated 2026-10-16. Bounded discovery is complete. The owner
selected a separate qualification target; current concrete preparation and the
remaining scoped decision are in the [isolated target plan](story-27-4-isolated-target-plan-2026-10-16.md).
The existing target is excluded from qualification faults.
Policy `sole-owner-qualification/2026-10-12` remains approved. All results and
owner decisions disclose `sole-owner/self-reviewed`. This document grants no
target action or gate acceptance. Spec 14 remains in progress with six open
live tasks; completed Spec 25 supplies retained-file corroboration only.

## Prepared source and custody

- Source commit: `8cc8fda7824bf2385d5c209c8222264f926d7792`.
- Clean standalone checkout: `/home/administrator/.local/state/hexalith/memories/access-telemetry/qualification-20261015T163246Z-13fd23/checkout`. It has a real `.git` directory,
  no initialized Git links/alternates, and passed the existing owner safety
  preflight and isolated revision/clean checks.
- Private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/qualification-20261015T163246Z-13fd23/evidence`. Directories are owner-only `0700`; retained
  files are `0600`. Fourteen guidance/source snapshots have exact-byte readback
  and hashes in `preparation.json`; `standalone-validation.json` retains
  the local checkout check. These are preparation evidence, not live gates.
- Layout: `snapshots/`, `discovery/`, `c0/`, `c1/`, `c2/`, `c3/`, `c4/`,
  `decisions/`, `terminal/`, `a41/`, `production/`, `logs/`. Each execution
  gets exclusive files; failure never overwrites a previous packet.

## D0: fresh discovery plan for owner decision

Exact machine plan: `/home/administrator/.local/state/hexalith/memories/access-telemetry/qualification-20261015T163246Z-13fd23/evidence/discovery-plan.json`. Final plan/runner hashes will
be retained after collector integration and local verification. No approval
record exists; the initial withdrawal file is deliberately held.

Local-only client metadata, observed 2026-10-15: context `jpiquot@local`, API
`https://192.168.1.30:6443`, TLS verification enabled, CA PEM SHA-256
`6d694314b130ce94db7648c3c677f94ca5c9be7137c0cc8f7e8dbc69c50f9379`. Canonical credential file
`/home/administrator/.kube/hexalith-production` is a regular single-link file,
UID 1000, mode `0600`; configured `.kube/config` is a symlink and will not be
used. Only credential metadata and local configured endpoint/CA identity are
retained. No credential value is included. The filename and context do not
establish cluster or namespace classification.

Proposed discovery window: `2026-10-15T16:30:00Z` through
`2026-10-16T16:30:00Z`, maximum 300 seconds of actual execution, exactly
twelve allowlisted queries, request timeout 10 seconds, child deadline
20 seconds, at most 1 MiB for each stdout/stderr stream. Current plan approval,
expiry, withdrawal, credential metadata and local API/CA bindings are checked
before target contact and before each query; final readback detects drift.

The query list contains only:

1. `kube-system` namespace identity as a cluster correlation value.
2. `hexalith-memories` namespace identity and selected environment labels.
3. `hexalith-memories-qualification` namespace identity and selected labels.
4. Existing `memories-access-telemetry` Deployment UID, replicas and images.
5. Existing `memories-access-telemetry-clock` Deployment UID, replicas and images.
6. Existing `memories` Deployment UID, replicas and images.
7. Existing selected lifecycle Pod UIDs, phase and image IDs.
8. Qualification lifecycle/clock/Server Deployment UIDs, replicas and images.
9. Qualification selected Pod UIDs, phase and image IDs.
10. Qualification gate ConfigMap UID and its fixed gate JSON.
11. Qualification Lease UID, holder and duration.
12. `dapr-system` Pod UIDs, phase and image IDs to identify the shared impact
    boundary for a later concrete fault plan.

This scope excludes Pod exec/logs, Secrets, writes, faults, purge, reclaim,
publication and Production activation. Unexpected/NotFound results are retained
observations requiring disposition, not passing gates. No retries substitute
for new authorization. Unknown current classification blocks mutations. The
2026-10-12 observations are historical and give no current target authority.

## Prepared D0 collector and owner handoff

`tools/access_telemetry_owner_discovery.py` now implements the bounded discovery
scope. The final proposed raw plan SHA-256 is
`25adcae1ae4aa35c596bf6d121de1f0235e8ac19368cb1b7c7bad2cb22dca6d5`;
its exact runner SHA-256 is
`71deee01aacdf88cc27bdb6113ed3dd7e0b8564c45e6f3a9dbccdedca49504d5`.
The runner is separately bound uncommitted implementation at
`/home/administrator/projects/hexalith/memories/tools/access_telemetry_owner_discovery.py`;
it is not represented as part of the baseline `8cc8fda7` commit. The plan also
binds exact canonical credential-file metadata and the fixed PG2 profile/workload
pins. Its twelve prepared query vectors are unchanged.

The closed plan schema is `hexalith.access-telemetry.owner-discovery-plan/v1`.
The same prefix with `decision/v1` identifies the separate actual owner decision:
exactly `schemaVersion`, `planId`, `planSha256`, `runnerSha256`, `ownerPrincipal`,
`assuranceMode`, `decision`, `allowedAction`, `approvedAtUtc`, `expiresAtUtc`.
It must bind the plan/runner, this owner and assurance, `decision: approved`, the
exact discovery action and plan expiry. Approval time is an actual UTC decision,
not an invented fixture value. The collector has no approval-writing operation.
Only after the owner explicitly approves this exact plan may that actual response
be retained as `discovery-approval.json` and matching current status prepared.
The outside-approved digest remains a required invocation argument.

The closed `hexalith.access-telemetry.owner-discovery-status/v1` status contains
exactly `schemaVersion`, `planId`, `planSha256`, `decisionSha256`, `withdrawn`.
Current `discovery-status.json` matches the proposed plan and deliberately holds
`withdrawn: true`, with an all-zero decision-digest sentinel because no decision
exists. This is not executable authority. After actual approval, the status must
bind that decision's exact bytes and reflect the owner's current withdrawal state.
Prepare complete private replacements and atomically replace control files;
concurrent updates can safely refuse and require a fresh invocation.

The ready invocation, after that actual approval and current status exist, is:

```bash
python3 -I -B tools/access_telemetry_owner_discovery.py \
  --plan /home/administrator/.local/state/hexalith/memories/access-telemetry/qualification-20261015T163246Z-13fd23/evidence/discovery-plan.json \
  --approved-plan-sha256 25adcae1ae4aa35c596bf6d121de1f0235e8ac19368cb1b7c7bad2cb22dca6d5
```

All inputs are freshly checked without following links. Credential-file privacy
is required; its existing public parent is not chmodded. The validated credential
bytes are passed only through a sealed anonymous Linux memfd, so replacement of
the named file cannot redirect the child. Receipts disclose both approved logical
argv and executed argv with the `/proc/self/fd/` input. No credential value is
retained. Local safe YAML parsing rejects exec/auth-provider, external credential,
proxy and TLS-bypass configurations. Unsupported Linux sealing or absent installed
PyYAML refuses; no dependency is installed or updated.

Before each query and at final readback, the collector checks current plan,
decision, runner and credential bytes/metadata, UTC/aggregate deadlines and freshly
read withdrawal status. The fixed `/usr/local/bin/kubectl` receives no ambient
context/proxy environment, uses no stdin and has a bounded process group. It kills
and reaps children on timeout, output overflow or interruption. The installed
interpreter, PyYAML and kubectl are trusted local tooling; their hashes and the
cluster's eventual classification are not established by this collector.

Exclusive `0700` directories under `evidence/discovery/` contain `0600` raw
stdout/stderr, per-command records and `receipt.json`, with SHA-256 and exact
readback. `.anchor` is the private no-follow output-directory guard. Each stream
is capped at 1 MiB (24 MiB maximum across twelve pairs); commands share the
300-second budget. Raw credential-bearing output is refused and omitted. A
`final-refusal.json` bound to a receipt invalidates any earlier apparent completion
if the final status check fails. Nonzero exits, NotFound and empty observations
require owner disposition; every receipt has `qualificationCredit: false`.

Current actual-plan verification refused with
`owner-discovery-owner-decision-required` before **zero process spawns and zero
target calls**. The retained offline refusal is
`discovery/missing-approval-refusal-1b752f9deaa543c099ccafb0b60ab55a.json`.
`discovery/prepared-bindings-ea8e9b6602874e60818bf5b7b793d6d2.json` retains final
plan/runner/metadata preparation bindings. Older preparation receipts preserve
their earlier hashes; they do not override the current plan.

## Qualification after discovery

1. Compare observed API/CA, namespace UIDs, resources, images and profile with
   approved PG-ONPREM-2 profile
   `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and workload
   `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`. The owner
   confirms classification before any qualification write. Resolve actual
   non-Production target and immutable signed lifecycle/clock image identities;
   checked-in `:0.0.0` placeholders cannot be deployed for a passing run.
2. Prepare exact source-bound namespace bootstrap and runtime migration manifest,
   named credential-file metadata/expiry/audience, session/action windows and
   resource selectors. Keep qualification overlays at zero. Discovery grants
   no namespace creation, Secret bootstrap or service-account provisioning.
3. Register/implement missing real gate producers/verifiers without bypassing
   `lookup_deployed_binding` or predecessor refusal. Current live consumers
   still reject all accepting bindings; Spec 25 does not change that.
4. Collect twenty-five distinct gate packets in one eligible C1 session. Renew
   C1.15 and recapture C1.16 for that session; old closed-window evidence is
   historical. Follow the approved acyclic dependency graph: technical roots
   and tests, C1.24 non-HA acknowledgement, C1.23 operations acceptance, then
   C1.25 owner security risk acceptance. Present measured C1 summary for owner
   acceptance, producing distinct gate and Operations/Security role records.
5. Only after C1 acceptance and explicit action approval run C0 and fixed C2-C4
   producers. C2 uses two writers for 30 minutes, exact acknowledgements,
   150,000-record purge backlog and PostgreSQL Pod replacement. Inventory
   individual writer/sidecar/lifecycle/clock/actor and all three Placement and
   Scheduler members. Shared `dapr-system` faults can affect other workloads:
   resource identities, affected services and restoration must be approved
   before a test. No shared fault approval currently exists.
6. C3 observes actual 1/24/168-hour cohorts, preserves newer controls, and proves
   logical purge and allocator reclamation within 24 hours after active purge.
   Retain fsync/hash-chained progress throughout. Its maximum 9-day producer
   bound and 168-hour cohort cannot be squeezed into the owner inspector's
   72-hour collection window: prepare separately approved phases and renewed
   authority checks; never backdate or widen the existing bound silently.
7. C4 proves every fault/privacy/tenant denial and console/configured-OTLP
   continuity, with no business failures. Business bearer input must be a
   short-lived private file for tenant `story-27-4-qualification`; no currently
   observed bearer is claimed. Any defect, scope drift, expiry or withdrawal
   stops tests and invokes only approved restoration for run-owned resources.
8. Present actual C0-C4 and terminal summaries; retain distinct owner-accepted
   C5 Operations and C6 Security decisions bound to exact evidence hashes.

## Prepared C3 phase and renewal handling

This is a proposed continuation contract, not executable authority or an
implemented renewal consumer. Anchor hour zero to the actual first C3 cohort
emission and retain the original producer/journal start; no renewal resets the
nine-day (216-hour) technical deadline or backdates cohort observations.

| Phase | Collection window relative to hour zero | Prepared scope |
| :---- | :-------------------------------------- | :------------- |
| A | 0 through 60 hours | Emit the three cohorts; observe 1/24-hour expiry and bounded purge/reclamation with the newer controls. |
| B | 60 through 120 hours | Observe retained journal/identity continuity with gate disabled, empty Lease and lifecycle/clock zero during waiting. |
| C | 120 through 180 hours | Continue waiting, observe the actual 168-hour cohort, then perform only separately approved active purge/reclamation actions. |
| D | 180 through 204 hours | Finish allocator readback within 24 hours of actual active purge, verify newer controls and terminal restoration. |

Each phase needs its own exact approved plan/owner record and fresh status,
issued before use with a collection window at most 72 hours, the existing
review extension at most 24 hours and expiry at most seven days after that
record's start. Each preserves the same eligible C1 session, target/profile/
workload/source, cohort IDs and complete hash-chained journal prefix. Exact
measured times determine whether the proposed windows fit; a late purge cannot
extend its 24-hour physical-proof deadline, and all work still stops at the
original 216-hour wrapper deadline.

Renew before the current phase ends, at a disabled/empty/zero checkpoint with
retained readback. Retain old/new plan, record and status hashes and the owner's
actual phase decision. A later phase is usable only after a future source-bound
consumer verifies both the same-session predecessor and the complete immutable
continuation prefix plus new authority. Current deployed consumers reject that
admission; preparing this schedule grants no bypass. Renewed action authority
cannot refresh C1/P7 evidence eligibility, extend an expired C1 grant, relabel a
session or reset cohort clocks. Existing freshness and predecessor refusal remain
enforced until a separately implemented and verified permitted continuation exists.
Missing renewal, target/
source/configuration drift, withdrawal or an interrupted journal stops new work;
only separately preapproved restoration of run-owned resources may finish.
No waiting interval, new phase, static manifest or renewal is itself gate proof.

## Terminal restoration and A41

Before enabling, prove disabled gate, empty Lease and zero desired/ready
lifecycle/clock replicas. Acquire Lease by resource-version/empty-holder test;
enable only the approved non-Production profile for at most 15 minutes and
renew before expiry. Stop on drift or loss of ownership. Approved restoration
must disable gate, release the run-owned Lease and scale only run-owned
qualification lifecycle/clock Deployments to zero. Retain final API readback
showing disabled/empty/zero and Pod termination; absence of a namespace or a
static manifest alone is not cleanup proof for an executed run. Never stop the
existing namespace's workload on the strength of its candidate name.

With accepted same-profile C0-C6 and terminal proof, run A41 inventory and
recoverable preflight from a clean commit. Produce exact four-path mutation
manifest, allowed/forbidden semantics and protected-history hashes. Obtain
any missing exact close-out/publication scope, stage only registered paths,
run postflight, validate Conventional Commit with commitlint before and after
commit, publish to the approved `origin` branch, and verify remote containment
of that exact commit and packet hashes. Update completion tracking only once
`published-close-out-verified` exists. All earlier history remains unchanged.

## Separate Production rollout

Production activation is outside Spec 14's authority. Prepare a distinct plan
after qualification: intended cluster/namespace UID, deployment ID, immutable
signed images and verification, exact rendered manifest diff, configuration
epoch, key generation, capacity admission, backup/restore RPO/RTO, console/OTLP
baseline, start/expiry window, owner action scope and abort/rollback budget.
The owner explicitly reconsiders the self-review assurance for Production;
non-Production policy approval cannot be promoted into that decision.

Activation order: capture disabled baseline and prior digest/config bytes,
verify accepted C0-C6/terminal/A41 publication, deploy approved lifecycle and
clock with writes disabled, verify runtime/clock/secret identities and health,
then enable the approved provider under a separately approved rollout and
observe the bounded acceptance sample. Stop on clock/profile/identity drift,
queue/capacity/acknowledgement failure, privacy or audit-continuity loss.

Rollback: disable the application provider through its independent switch,
verify business/console/configured-OTLP continuity and stopped new writes,
restore prior approved Server digest/config only if necessary. Retain lifecycle,
clock, component, actor state, secrets, records and required marker keys until
normal expiry/purge/reclamation; scaling them to zero during Production
rollback would interrupt retention. No storage/Secret deletion is implied.
Retain actual post-rollback observations and owner incident disposition.

Production target classification, live immutable image set, current deployment
state, exact rollback diff and rollout approval remain unverified. Static
rendering can validate preparation, never live activation readiness.

## Actual bounded discovery, 2026-10-15

The owner explicitly answered **“Approve read-only discovery”** for plan SHA-256
`25adcae1ae4aa35c596bf6d121de1f0235e8ac19368cb1b7c7bad2cb22dca6d5`.
The exact separate decision and conversation source are retained under the
private evidence root; no new policy approval was requested. The source-bound
runner SHA-256 is `71deee01aacdf88cc27bdb6113ed3dd7e0b8564c45e6f3a9dbccdedca49504d5`.

The collector ran `2026-10-15T16:54:02Z`–`2026-10-15T16:54:03Z`, exited 0 and
completed all twelve approved queries. Eight command exits were 0; four were
NotFound observations. An empty qualification Pod list is not namespace or
cleanup proof. All twenty-four retained stream hashes/lengths/private modes
and the complete receipt hash were independently read back.

Receipt: `/home/administrator/.local/state/hexalith/memories/access-telemetry/qualification-20261015T163246Z-13fd23/evidence/discovery/discovery-c8068612612d405da3c0cefaac9e2e1b/receipt.json`.
Receipt SHA-256: `cf26b491256421c931c65bf5e1921b82bb61aef5990b0bd94508409192c7079e`.
The result is `requires-owner-disposition`, with `qualificationCredit: false`.
`target-observations.json` binds all observed UIDs, replicas and images; its
observation digest is `227eee5dc1265d0a75d1e3e8334dcda155b40585a47c2731ed614ea0bdf599ab`.

Observed cluster correlation: `kube-system` UID `b5860674-1d1e-4a3b-b413-e1bb3b9a4557`.
Existing namespace `hexalith-memories` has UID `b4890577-70a6-4296-8025-d1e43de1e0e4` and neither inspected
environment label. The qualification namespace is absent. Existing `memories`
has 2 desired/2 ready replicas; lifecycle has 1/1; clock has desired 0 and no
ready-replica value. The lifecycle Pod's Dapr digest matches the approved Dapr
OCI index, but this is metadata observation, not C1.15 technical acceptance.

Observed application image candidates are:

- Lifecycle: `b3790e08c0091dfb723f6819e4706a6b1ef36a051b8c4afe38f5e362c5bdf8ea`.
- Clock: `50413b716ca051fad7c4d68119527a1fc5f64c566eea22827769fd85d9de4c70`.

Their signatures, source revisions, scans and runtime qualification compatibility
are unverified. They do not authorize replacing the source's `:0.0.0` placeholders.
Three Placement and three Scheduler members were observed. Their shared failure
impact cannot be approved from the namespace name or these Pod identities alone.
At this dated discovery, classification and shared-fault permission were missing.
The owner subsequently selected the separate-target recommendation on 2026-10-16;
classification of this existing cluster no longer blocks independent preparation.

The twelve-query budget is complete. The active status was retained, then the
local execution status was atomically held (`withdrawn: true`) to prevent reuse,
with reason in `decisions/discovery-budget-closed.json`. This closes only that
discovery budget and does not withdraw the standing owner policy. No namespace,
Secret, Pod, Deployment, gate, Lease or replica was mutated. No qualification
gate or C0-C6 checkpoint passed, no terminal cleanup was claimed, and no A41,
sprint, protected-history or Production transition occurred.

Static preparation retained in `production/static-validation.json` rendered
51 Production and 61 qualification objects. Both lifecycle/clock replica sets
render at zero; qualification renders disabled gate and empty Lease. These
checks cannot establish the live terminal state or Production activation readiness.
A41 inventory exited 0 and bound 171 references and the exact four mutable paths
at the baseline commit; it is preparation only, not close-out preflight/publication.

Root independently reran the D0 lane: 31 tests in 4.925s, exit 0, no failures,
errors or skips; raw logs/command are retained under `logs/root-d0-focused.*`.
The actual prepared-plan negative check before approval returned
`owner-discovery-owner-decision-required` with zero target/spawn calls, retained
as `root-d0-missing-approval.json`. Those are offline refusal/verification checks;
the twelve approved queries above are actual live metadata observations.
