# Access-telemetry full qualification — pending campaign

This is the current requirements register for the separately scheduled full campaign. The [local functional contract](../../_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) has its own finish line. Functional success cannot discharge these gates or close A41. The [current report](../../_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md) records results.

Adopt one exact full Platform target/profile before scheduling this campaign. Prior KVM/PG captures do not transfer. Each gate needs eligible same-session evidence, its own review and completion state; no additional story-file registration is required. Jérôme Piquot owns the gates with Platform/Builds environment support and Memories assertions. The approved sole-owner policy permits distinct Operations/Security records labelled `sole-owner/self-reviewed` without independent-review claims.

C1.24 precedes C1.23/C1.25. Full C2 retains two writers, 30 minutes and 150,000 backlog records. Full C3 retains real 1/24/168-hour cohorts, 24-hour allocator proof and the 216-hour producer bound. No deadline extension is implicit. Use the existing [lifecycle runbook](access-telemetry-lifecycle.md) and [adapter contract](access-telemetry-adapter-production.md). Production activation/publication remain separate decisions.

## Recommended target and provisional schedule — 2026-10-25

After the local pass, the owner answered **“I dont know, do recommended”** when asked which full Platform target/profile and UTC window to prepare. This selects the following preparation direction. A deployable full profile, capacity reservation and operational launch plan remain prerequisites; no campaign has started.

Use a fresh, single-node **Platform-owned kind qualification environment**, built from Platform's maintained application chart and isolated-cluster ownership. Reserve the proposed cluster name `memories-qualification-27-4-20261026`, explicit context `kind-memories-qualification-27-4-20261026`, and application namespace `hexalith-memories-qualification` in the eventual plan; refuse a pre-existing name rather than attaching or deleting it. This creates a new full-profile identity. The retained KVM target and its disks remain historical resources outside this proposal's ownership.

The starting toolchain is Platform's existing [toolchain.json](../../references/Hexalith.Platform/eng/helm-qualification/toolchain.json): kind **0.31.0**, Kubernetes **1.35.0** at the recorded node-image digest, Helm **4.2.0**, Dapr **1.18.7**, and Aspire **13.6.1**. Preserve the PostgreSQL **18.6** and OpenBao **2.6.4** immutable image identities, two-writer **500 component operations/s /250 accepted records/s** workload and no-HA boundary from the [former full profile](qualification/pg2-kvm-approved-profile-2026-10-16.json). Bind the new runtime images, chart/values/render, application/reporter images, storage and exact input hashes before adoption. The former400GiB allocation remains historical. The smaller finite candidate below supersedes that allocation only as a new provisional preparation target; it is not an adopted or qualified replacement profile.

The current verifier still accepts canonical profile **`95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`**, including KVM and Dapr 1.18.1. Offline `validate_current_profile_inputs(Path.cwd())` passes on 2026-10-25. A kind/Dapr 1.18.7 target must receive a new canonical identity and corresponding semantic checks; it cannot use that hash or WINDOW's local evidence. No dependency, manifest or version pin was changed during this preparation.

### Prerequisites and owning work

Platform's [maintained chart documentation](../../references/Hexalith.Platform/eng/helm-qualification/README.md) and `eng/helm-qualification/qualify.py` provide a tested foundation: unique cluster, private kubeconfig, explicit context, bounded commands and owned cleanup. Their retained 2026-10-22 qualification establishes chart/admission behavior only. The qualifier disables Scheduler, uses synthetic bindings, and provides no real PostgreSQL/OpenBao environment. Running that qualifier alone cannot discharge this campaign.

| Owner | Required change before a full run | Completion evidence |
| :--- | :--- | :--- |
| Platform / Builds | Extend the existing environment lifecycle and descriptor handoff to own this isolated Kubernetes campaign, with finite startup/execution/stopping budgets, persistent run identity, monitoring, exact stop/disposal and failure disposition. Declare the full suites; the current Memories manifest declares only `functional-retention` at 14400 seconds. | Supported command and actual descriptor/loaded-input proof, exercised cancellation/failure/cleanup/isolation checks; no new Memories environment runner. |
| Platform | Supply real PostgreSQL, OpenBao/secret references, authenticated independent UTC, component configuration, working Scheduler/reminders and the full declared Placement/Scheduler fault topology. Build and bind actual application/reporter images and authority inputs. | Readiness and runtime/security/identity evidence over the exact deployed inputs; fresh C1 packets, including C1.15/C1.16. |
| Platform Operations | Reserve actual CPU/memory and Linux-backed storage for the whole interval, including application/control-plane load, PostgreSQL's existing8CPU/16GiB cap, the new finite candidate's measured allocations/rewrite workspace and a separate recoverable backup destination. Replace the chart fixture's 10m/32Mi requests and 1 CPU/512Mi limits with measured full-profile allocations. | Capacity/cost admission and backup/restore proof; a PVC request or free space on another filesystem is insufficient. |
| Memories | The explicit owner-predecessor/v1 authority consumer and durable C3 continuation are implemented and offline verified; see the [runbook contract](access-telemetry-lifecycle.md#owner-authority-and-c3-continuation-consumer--2026-10-25) and [current report](../../_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md#owner-authority-and-continuation-implementation--2026-10-25). The single ordinary review is complete; all four findings are fixed and final focused verification passes. Supply actual outside-approved authority, fresh independent UTC/status and physical target/runtime identity before use. Historical predecessor/v2 still refuses. | Focused temporary-file authority/continuation verification is recorded in the report, including distinct owner roles and unchanged full-mode bounds. These fixtures provide no live gate credit. Actual Platform-supplied authority/time/target inputs and separately adopted kind profile remain required; profile rebinding alone cannot enable C2–C4. |
| Memories | Adopt the exact new canonical profile and source/input bindings in `tools/verify_access_telemetry_lifecycle.py`, the C1 identity/authority consumers and qualification overlays, with mismatch, tenant-denial and fail-closed regressions. Preserve all existing behavioral gates and historical profile/evidence identities. | Reviewed profile manifest, source-bound C0 and all 25 eligible same-session C1 packets before C2–C4. |
| Jérôme Piquot | Bind the concrete plan to the actual reserved target/window, including bounded retries, fault authority, prior-resource exclusions and disposition; later record distinct Operations/Security acceptance. | One operational decision on the complete launch plan; later C5/C6 acceptance over actual evidence under `sole-owner/self-reviewed`. |

**Historical 400 GiB comparison.** A prior read-only check found **120,523,268,096 bytes** free on the Linux workspace filesystem, below the former **429,496,729,600-byte** data allowance. `/mnt/c` had **1,214,942,855,168 bytes** free. These observations established no reservation, Linux-backed retained volume or backup. The new finite profile below replaces that inherited allowance for preparation; actual schedule-derived capacity and reservations remain prerequisites. No storage was allocated or cluster contacted.

## Smaller finite campaign profile — 2026-10-25

Platform now supplies offline sizing/configuration and bind/admit checks through
its existing `eng/helm-qualification/qualify.py --campaign-capacity` boundary.
Memories selects the bound profile explicitly, validates finite C1.13/C1.18 and
Dapr identity semantics, and uses the same full authority/continuation/terminal
consumers and frozen producer import set. Historical PG1/PG2/KVM constructors,
400GiB thresholds and identities remain unchanged. The default current KVM hash
stays `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`.
The new path has no actual operational canonical hash until complete inputs exist.

Let N be the number of complete C1 collections in the validated finite renewal
schedule. Each collection reserves450000 C1.9 records,150000 C1.10 records and
50000 other C1/C0 records. C2 reserves450000 writers +150000 separate backlog
+50000 other; C3 reserves375 seeds +125 newer controls; C4 reserves50000 other.
The cumulative live/dead bound is therefore R=650000*N+700500 records, with no
credit for earlier vacuum/reuse. A complete source/normalized-argument inventory
has exactly one invocation per command per round; retries earn no extra allowance.
The50000 terms are configured admission envelopes, not observed producer counts.
Missing or excess inventory refuses; producer behavior remains unchanged.

For the admitted R, raw relation/support bytes are4096*R (2048 heap/TOAST +2048
all index/actor/due/catalogue/cursor/reminder state); M=ceil(5*4096*R/4) adds25%.
Reserve one full M-byte rewrite,4GiB WAL and1GiB fixed DB overhead:
PG=round_up_4GiB(ceil(5*(2*M+5GiB)/4)). The independent recoverable backup is
round_up_4GiB(ceil(5*(M+5GiB)/4)). Both retain20% free at modeled peak. Fixed
other primary allocations are8GiB control (etcd4/OpenBao1/logs+other3), three1GiB
Scheduler volumes,4GiB app spill and18GiB host workspace:33GiB. Primary=PG+33GiB.
These configured bounds require actual scoped physical/support/WAL/rewrite and
performance measurements; no exact PG/backup allocation is selected without a
compatible measured schedule. The former24GiB PG /57GiB primary /16GiB backup
numbers are only a single-collection lower bound, superseded for the full campaign.
Aggregate compute remains16250m CPU/31GiB (PG8CPU/16GiB, apps4.5CPU/7GiB,
control2CPU/3GiB, Scheduler0.75CPU/1GiB and host1CPU/4GiB).

A compatible renewal schedule remains a missing Platform prerequisite. The
unchanged C1.15->C1.16 parent chain requires all25 descendants to be recaptured
for each root refresh. A round reserves measured source-bound gate capture,
decision and cleanup slots in parent order, at most24h capture/review lag, the
original<=72h collection and<=24h review/use windows, and<=96h technical age.
Their collection coverage must span the full C2/C3/C4 intervals inside336h.
Each actual owner plan binds `campaignOriginUtcMs` and `renewalRound`; the
consumer checks actual capture/collection times against that admitted round.
A new round cannot move the immutable profile or original C3 origin/deadline.

C1 fault/workers and whole-table maintenance can purge C3 cohorts early or
confound exact125-row seeds/newer controls and physical attribution. The schedule
therefore reserves no-overlap intervals for empty/seeding and every cohort's
expiry/purge/reclamation, with newer-control/report isolation at168h. Shorter
operation reservations require retained source-bound timing measurements;900s
purge,24h reclamation and216h overall C3 policies remain unchanged. Every round
requires exact retained zero unrelated live/dead/support-state readbacks and
unchanged C3 cohort/due/reminder inventory, plus physical-attribution proof.
C2's24h records must be proven drained/purged/reclaimed before C3's empty boundary.
The proposed phase times alone cannot prove that fit. Missing timing, cleanup,
C2 drain or C3 isolation measurements reject admission. No operational renewal
count or compatible timeline is invented; unit schedules/receipts are simulations
and supply no adopted evidence. Platform must supply a real compatible plan and
exercise its native lifecycle; this change supplies no second orchestrator.

Retained WINDOW canonical values provide a narrowly reusable observation:250
records encode to692 compact UTF-8 bytes each using
`json.dumps(record, ensure_ascii=False, separators=(',', ':'))`. This is a local
serialization measurement, not PostgreSQL physical amplification or full-rate
performance. WINDOW's pre-expiry SQL packet contains timings, not physical size.
`/home/administrator/evidence/hexalith-memories` contains only C1.16; C1.13 and
C1.18 directories/measurements are absent. The current report records exact paths.

A fresh read-only host observation at2026-10-25T11:29:44.721523Z found14 CPU
slots,97021218816 total /67109687296 available memory bytes, and282774736896
available workspace filesystem bytes (device2096). This is neither an aggregate
reservation nor backup proof. CPU alone falls below16250m; no actual reservation,
separate backup, operational measurements or complete source/command plan exists.
The earlier120523268096-byte free-space observation remains its historical result;
changed free space does not establish reserved backing storage.

Admission reopens exact tool/toolchain/image/chart/values/render/component/
application/reporter/source/storage/workload-plan and renewal-evidence bytes.
Missing measurements, reservations, aggregate competing capacity or independent
backup refuse. The late-bound runtime ConfigMap still avoids self-reference and
actual readback plus the Qualification-only Server gate bind its exact data.

Initial measurement freshness cannot be renewed by a fresh capacity receipt.
A first use needs<=24h measurements. Before C3 dispatch, the private outside-approved
owner plan must additionally bind `initialCapacityAdmission` (exact Ref digest),
`capacityAdmissionIssuer`, `capacityAdmissionKeyPath` and
`capacityAdmissionKeySha256`. Platform's small initial-admission artifact signs
the exact immutable profile after strict fresh validation. Its issuer advances an
authenticated independent UTC sample with same-boot CLOCK_BOOTTIME (sample<=60s)
and stamps actual admission time; there is no user backdate argument. Verification
uses only outside-approved issuer/key/receipt bytes, rechecks initial freshness at
that authenticated original stamp, and preserves original identity/C3 time through
multi-day continuation. The immutable initial300s compute/storage observations do
not start expiring again during a valid continuation; separate current capacity
receipts still require<=300s age before every launch. They may renew only observed
available/reserved/competing quantities and receipt metadata over the original
poolId/failureDomain/Linux backing/allocation ownership; backing changes need new
adoption. The real issuer, key custody and trusted-UTC service remain absent
operational prerequisites. No operational key, timestamp or receipt was created.

Owned cleanup authenticates the original outside-approved profile, physical API/CA/
cluster/namespace/runtime, session and exact Lease independently of changed capacity
inputs. Actual disable-only and parent fallback retain this authority and preserve
the original failure-packet profile. Foreign targets/sessions/Leases still refuse.
Historical source closure is authenticated from the committed verifier's actual
imports: authentic old C0/C2-C4 archives retain their original exact source sets;
current exports require every actual import, including the finite-profile loader.
Historical source cannot earn finite-profile credit.

### Conditional concrete launch and disposition plan — not ready for authorization

Logical target remains cluster `memories-qualification-27-4-20261026`, context
`kind-memories-qualification-27-4-20261026`, namespace
`hexalith-memories-qualification`; Jérôme Piquot owns the reservation and distinct
Operations/Security decisions. Platform owns environment/lifecycle; Memories owns
assertions. Refuse pre-existing cluster names. Proposed private custody is
`/home/administrator/.local/state/hexalith/memories/access-telemetry/full-finite-20261026`;
it was not created. Actual API/CA/cluster/namespace UIDs, engine ownership,
application/reporter/runtime digests, full values/render/component set, source
revision, measurement/reservation receipts and adopted profile hash are **missing**.
The provision/observe/stop command for the full environment and its actual native
run ID are **missing**; the synthetic qualifier is not an alternative full launcher.

Preparation/admission uses the exact offline command documented in Platform's
[README](../../references/Hexalith.Platform/eng/helm-qualification/README.md#finite-memories-campaign-capacity-offline).
After Platform supplies the environment and one complete outside-approved owner
plan, use the existing Memories `verify-access-telemetry-lifecycle.py` C0/C2/C3/C4
routes, with `--campaign-profile`, `--approved-campaign-profile-sha256`,
`--campaign-capacity-receipt`, `--approved-campaign-capacity-sha256` and existing
owner-plan/custody flags. The actual complete command cannot be finalized while
those private paths/hashes and native lifecycle route are missing. This is a
concrete bounded target/ownership plan with explicit blockers, not a deployable plan.
No operational authorization is requested.

Keep the336-hour proposal; phase intervals require a compatible renewal/drain plan, C2/C4 ceilings50400s,
C3 original777600s (including resumptions), real retention/reclamation and all
existing producer cleanup bounds. One launch per declared phase, zero retries or
automatic restart; same-identity uncertain submissions use only existing bounded
reconciliation. Admission before any launch requires complete source/inputs,
fresh capacity/authority and reservations fitting the schedule-derived PG+33GiB
primary and separate schedule-derived backup, plus16250m/31GiB and competing demands. Continuous monitoring must stop on
WAL/phase/resource bound violation, source/identity drift, withdrawal or deadline.
Bind native process/resources before faults; retain bounded private native logs,
UTC/elapsed/exit and failure/TRX evidence before stopping, with missing exit data
explicit. Stop only the actual owned native run, verify its disabled gate, empty
Lease, zero lifecycle/clock replicas and no owned workloads, then delete only
its owned kind cluster with explicit private kubeconfig/context. Fresh exact-name
cluster/node/container/namespace scans must verify removal; uncertain disposition
stays failed/unverified. Final disposal gets900s within the proposed last15 minutes;
per-producer disable/cleanup stays within its existing original bounds. Preserve
all old KVM/WINDOW/other-session resources. No Production, publication or A41 action
is included. A missed readiness/phase boundary requires a replacement schedule,
never extension of a running budget. All34 gate rows below remain unchanged/open.

The continuation's one ordinary review is complete; its four accepted findings
are fixed, with none deferred. Coordinator verification passes33 authority,41
retention,18 capacity and16 finite-consumer tests; exact results and the disclosed
C# build limitation are in the single report. Actual
full environment, monitored resource bounds, measurement/reservation, source/input
adoption, runtime identity and operational launch authorization remain prerequisites.

### Provisional UTC reservation

Prepare a **336-hour / 14-day** reservation, **2026-10-26T08:00:00Z–2026-11-09T08:00:00Z**. This is a proposal, not a booked window or launch authorization. It contains C3's 216-hour ceiling plus the other sequential producer bounds and explicit setup/acceptance/disposition time. A nine-day reservation alone cannot contain the full campaign. Every phase requires its actual predecessors; unused time grants no retry or budget reset.

The earlier dated per-phase sketch is superseded: it omitted C1 renewals and
C2 drain before C3. The336-hour date envelope remains provisional; actual phase
origins, recapture slots, C3 protected intervals and final<=24h review/use fit must
come from the source-bound measured schedule before any operational approval.
C2/C4 retain50400s each; C3 retains777600s from its original origin; final owned
disposition retains900s. No phase start is presently admitted.

The proposed reservation expires if target readiness/capacity, Memories authority/continuation verification and the complete operational plan are not settled by its first start, or a required phase misses its boundary. Record the missed window and prepare a replacement schedule; do not silently move deadlines, revive consumed local permissions or weaken a gate. Current state is **preparation blocked on Platform's full environment/capacity and actual authority/time/target inputs**, with the Memories prerequisites implemented/reviewed and exact new-profile adoption and operational authorization pending. All gate rows below remain open.

## Full campaign gates

| Gate | Required observed evidence / artifact | Owner | Review state | Completion state |
| :--- | :--- | :--- | :--- | :--- |
| C1.1 | `c1/C1.1.json`: real CRUD round trip | Jérôme Piquot | pending | open |
| C1.2 | `c1/C1.2.json`: post-write strong read | Jérôme Piquot | pending | open |
| C1.3 | `c1/C1.3.json`: matched/stale ETags and FirstWrite | Jérôme Piquot | pending | open |
| C1.4 | `c1/C1.4.json`: later-operation failure with no partial transaction | Jérôme Piquot | pending | open |
| C1.5 | `c1/C1.5.json`: effective component TTL | Jérôme Piquot | pending | open |
| C1.6 | `c1/C1.6.json`: actor state survives reactivation | Jérôme Piquot | pending | open |
| C1.7 | `c1/C1.7.json`: Placement/Scheduler disruption and reminder recovery | Jérôme Piquot | pending | open |
| C1.8 | `c1/C1.8.json`: actual record/request limits | Jérôme Piquot | pending | open |
| C1.9 | `c1/C1.9.json`: full two-writer throughput and latency evidence | Jérôme Piquot | pending | open |
| C1.10 | `c1/C1.10.json`: 150,000-record catch-up and drain bounds | Jérôme Piquot | pending | open |
| C1.11 | `c1/C1.11.json`: tenant/isolation denials before dependencies | Jérôme Piquot | pending | open |
| C1.12 | `c1/C1.12.json`: TLS verify-full and at-rest posture | Jérôme Piquot | pending | open |
| C1.13 | `c1/C1.13.json`: measured amplification/headroom and 1/24/168-hour admission | Jérôme Piquot | pending | open |
| C1.14 | `c1/C1.14.json`: cohort-attributable allocator reuse and deadline | Jérôme Piquot | pending | open |
| C1.15 | `c1/C1.15.json`: fresh current-session runtime/control-plane capture | Jérôme Piquot | pending | open |
| C1.16 | `c1/C1.16.json`: fresh component/backend identity and actual connection linkage | Jérôme Piquot | pending | open |
| C1.17 | `c1/C1.17.json`: deployed image/manifest/epoch/source identity | Jérôme Piquot | pending | open |
| C1.18 | `c1/C1.18.json`: node/storage reservation, headroom and operating cost | Jérôme Piquot | pending | open |
| C1.19 | `c1/C1.19.json`: declared PostgreSQL process/pod fault, zero acknowledged loss | Jérôme Piquot | pending | open |
| C1.20 | `c1/C1.20.json`: exact out-of-profile exclusions | Jérôme Piquot | pending | open |
| C1.21 | `c1/C1.21.json`: real backup destination and successful restore | Jérôme Piquot | pending | open |
| C1.22 | `c1/C1.22.json`: measured RPO/RTO and no-HA claim | Jérôme Piquot | pending | open |
| C1.23 | `c1/C1.23.json`: owner Operations acceptance of required parents | Jérôme Piquot | pending | open |
| C1.24 | `c1/C1.24.json`: owner non-HA acknowledgement | Jérôme Piquot | pending | open |
| C1.25 | `c1/C1.25.json`: owner security risk acceptance after required parents | Jérôme Piquot | pending | open |
| C0 | `c0/adapter.json`: exact approved adapter/source/input evidence | Jérôme Piquot | pending | open |
| C1 aggregate | 25 verified same-session packets plus distinct owner role records | Jérôme Piquot | pending | open |
| C2 | Existing `c2-production-replacement` producer packet, all fixed replacements/accounting | Jérôme Piquot | pending | open |
| C3 | Existing `c3-retention-reclamation` packet and complete journal, all three cohorts | Jérôme Piquot | pending | open |
| C4 | Existing `c4-failure-privacy-observability` packet, every fault/privacy/continuity lane | Jérôme Piquot | pending | open |
| C5 | Operations result record over actual C0–C4, runbooks, risk and cleanup | Jérôme Piquot | pending | open |
| C6 | Distinct Security result record over the same evidence, accepted with C5 in one final owner response | Jérôme Piquot | pending | open |
| Terminal | Actual disabled gate, empty Lease, zero desired/ready lifecycle/clock and terminated Pods, bound to the result set | Jérôme Piquot | pending | open |
| A41 publication | Existing inventory → recoverable preflight → exact mutation → postflight → authorized publication → remote containment proof | Jérôme Piquot | pending | open |
