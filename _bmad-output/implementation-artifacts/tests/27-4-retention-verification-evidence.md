# Story 27.4 — current retention results

## Owner approval and launch readiness — 2026-10-27 (current)

The owner's **“I approve”** is recorded and accepted for the proposed isolated
non-Production calibration run within the existing one-attempt/zero-retry,
16250m/31GiB,600s startup,50400s execution and900s cumulative stop/remove limits
(total51900s). No repeat approval is required for unchanged scope. Approval was
raised before preparation was complete; missing technical inputs remain explicit.

The source-bound calibration draft still has31 null entries: actual target/run/
profile identity, isolated Unix Docker engine, primary and different-host Linux
backup paths, allocation basis/amounts, compute/storage reservations, CNI, runtime
secrets/components/providers/reporters/monitoring, independent clock/current
authority and absolute execution window. The verified Kubernetes capacity and
Scaleway object-store observations remain valid historical observations, but they
do not fill those supported-path inputs. No fabricated identity, measurement,
reservation, key/signature or executable plan digest was created.

Exact supported command, cwd `references/Hexalith.Platform`:
`python3 -B eng/helm-qualification/qualify.py --calibration inspect --calibration-state /home/administrator/.local/state/hexalith/memories/access-telemetry/full-campaign-offline-20261025-buc4thfg/independent-calibration-state`.
It returned **exit1**, `{"status":"refused","qualified":false,"causeCode":"invalid-operational-input"}`,
in0.544744s. The state directory was absent before and after. This is an inspection
refusal, not an attempted calibration or a consumed launch. No prepare, workload,
cluster, storage or credential mutation occurred. A complete supported plan still
precedes prepare/launch; owner permission is no longer the outstanding input.

Private `run-approval-20261027-c8x3x7cg/approval-and-readiness.json` under the
existing preparation root binds the literal reply, original limits, actual nulls
and command result. SHA256:
`44d8ef3d1c600dbc3c705c91492c292028120d25cd9cc6e189c654ff2aceaefb`.
The preparation index/catalogue retain this receipt and distinguish accepted
owner approval from missing operational bindings. Story27.4 stays in progress;
full qualification/Production unproved and A41 open.

### Approval record reconciliation — 2026-10-27

The preparation index's stale `launchAuthorized:false` is corrected to match the
catalogue and the accepted one-run approval. Both current capability records now
state `launchAuthorized:true` and `launchReady:false`; the old flags and missing-input
wording are retained under `history`. The outstanding list now names the absolute
window, exact executable plan and operational authority bindings. Unchanged owner
permission is already accepted and is not requested again. The catalogue hash in
the index is refreshed; original approval/draft bytes, limits, technical nulls and
unused launch/host-capture attempts are unchanged.

Validation command: `python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/full-campaign-offline-20261025-buc4thfg/approval-record-reconciliation-20261027-qg3xz3uz/verify-preparation-records.py`.
Result: **exit0, 19/19 checks passed**; `git diff --check` also passed.
The existing preparation index binds the checker and its retained verification receipt.
It checks JSON parity, catalogue and retained approval/draft hashes, unchanged
technical prerequisites and historical Markdown preservation. Product source and
runtime admission are unchanged; completed implementation suites, inspections and
absent-state inspection are not repeated. No infrastructure or remote action is
performed. Story27.4 remains in progress; full qualification/Production unproved
and A41 open.

## Kubernetes metadata capture — 2026-10-27 (current)

The owner's **“you can access to kubectl”** authorizes bounded read-only metadata
through the configured `jpiquot@local` context, superseding the previous
Kubernetes-contact restriction for this scope. The API at `https://192.168.1.30:6443`
returns Ready node1, UID `46d8d528-420d-4501-a6d8-12680d8a6ac6`. Seven exact
commands, each with `--context=jpiquot@local --request-timeout=6s`, passed in
**1.735262s**, zero retries: `get node node1 -o json`, `describe node node1`,
`get --raw /apis/metrics.k8s.io/v1beta1/nodes/node1`,
`get --raw /api/v1/nodes/node1/proxy/stats/summary`, `get storageclasses -o json`,
`get persistentvolumes -o json`, and `get persistentvolumeclaims --all-namespaces -o json`.
Sanitized projections exclude workload environment and credential payloads.

Node1 reports 32 CPUs / 134924496896 RAM bytes; allocatable 31600m CPU /
127653983951 RAM bytes. Existing 72 nonterminated Pods request 13800m / 
23653777408 bytes, leaving 17800m / 104000206543 bytes against requests only.
The unchanged 16250m campaign would leave 1550m CPU by that arithmetic; existing
CPU limits total 61700m and zero-request workloads exist, so this is no guaranteed
net reservation. At 17:21:41Z, kubelet reports 116593053696 available memory bytes
and 846266871808 filesystem bytes available. All 30 observed PVs use node-local
storage; 29 PVCs and the existing protected 400GiB telemetry claim establish no
separate calibration allocation or independent Linux backup. Runtime is
containerd 2.3.3; the planned isolated Docker engine remains unobserved.

Private `HOST_DISCOVERY/kubectl-observation-20261027-9dm9nx59/receipt.json` SHA256
`ab0607174398bf190c0e77b30511bb9fc4f3afb020f67e3e6fe64ac492a6f57a` binds the
seven commands/artifacts. `capacity-observation.json` SHA256
`41640770b8153082fa9fdd45d9c2a446f37d0ae63460da8a83aa8bb3628925b2` records the
arithmetic and explicit missing inputs; the existing index/catalogue bind both.
SSH is not needed for these observations. The exact earlier host-local script was
not dispatched; its single approved attempt remains unused. No exec, pod creation,
workload/credential/storage mutation, calibration or launch occurred. Local/API
UTC observations are not independent campaign time. The missed October 26
provisional start cannot be reused; schedule follows actual prerequisites.
Story27.4 remains in progress; full qualification/Production unproved and A41 open.

Validation passed: seven successful bounded commands, artifact SHA256 chains,
index/catalogue consistency, private 0700/0600 permissions, unchanged earlier
Markdown bytes and original host-script approval/unused attempt, null campaign
allocations/renewals, absent operational state and `git diff --check`. Product
source is unchanged; no implementation test suite or completed review was repeated.

### Existing off-node backup service — 2026-10-27

Five bounded read-only Kubernetes queries succeeded in 1.671784s: Velero backup
locations, repositories and backups; CNPG ObjectStores; KubeSphere clusters. The
available Velero location `scaleway-kube-green` uses bucket
`hexalith-velero-backups` at `https://s3.fr-par.scw.cloud`, with current Available
status and a Ready Kopia repository. Retained Completed backups cover Forgejo,
not access telemetry; their latest completion is 2026-10-27T17:00:39Z. The CNPG
ObjectStore points to `s3://hexalith-recovery-points/keycloak/`. These are concrete
existing off-node service leads, not campaign allocation, credential, restore or
Linux-path proof. The registered cluster inventory contains one single-node
cluster. No secret value was read and no backup/restore/cluster change occurred.

`HOST_DISCOVERY/backup-observation-20261027-_ggxjwrg/receipt.json`, SHA256
`4078d5d9d4c759a3ad015dd7e1630c866c9b9bc7cdd7376e7a137ab0b5a64fb5`, binds
all five commands and sanitized artifacts. Platform's existing
`independent_calibration.py` requires canonical primary/backup directories and
separate host/pool/filesystem/failure-domain identities; its S3 URLs cannot fill
`paths.backup`. That supported-path gap remains preparation work, not a request
for the owner to supply credentials or manufacture evidence. Exact resource/run
approval follows a complete supported plan; no launch approval is requested now.
Artifact hashes, five exit0 results and private 0700/0600 permissions passed.

## Approved host observation — 2026-10-26

The owner's reply **“approve”** authorizes exactly the prepared automated node1
metadata capture: one attempt, <=60s, zero retries or workload changes. The exact
script SHA256 is `d2410555614d483691f0c12876251cf9d840cd70ace854110ffba5e039eaed52`.
Approval is recorded in the existing preparation index/catalogue and private
`host-discovery-20261026/host-observation-approval-20261026.json`.

**Approved; not dispatched.** Local `ssh-add -l` reports no authentication agent;
Windows `ssh-add.exe -l` reports no agent connection. Neither SSH configuration
file exists and no host-console tool is available. The prior strict-host-key
transports remain exit255 authentication failures; rejected credentials/accounts
were not retried or guessed. No remote command ran. The single capture attempt
is unused and remains authorized for this unchanged scope once a working existing
trusted host-local channel is available; no repeat permission is needed.

Current capacity, signed net16250m/31GiB reservation, Linux primary and independent
Linux backup remain unverified. This approval grants no reservation or calibration
launch. No infrastructure, cluster, credential, dependency/submodule or Git write
occurred. Story27.4 remains in progress and A41 open.

### Access resumption — 2026-10-27

The approved capture remains **not dispatched, one attempt unused**. Bounded
local-only checks at **2026-10-27T17:12:46Z** returned:

| Exact command / check | Result |
| :--- | :--- |
| `ssh-add -l` | Exit2; authentication agent unavailable (0.003473s). |
| `/mnt/c/Windows/System32/OpenSSH/ssh-add.exe -l` | Exit2; authentication agent unavailable (0.055089s). |
| Local SSH config presence | `/home/administrator/.ssh/config` and `/mnt/c/Users/JeromePiquot/.ssh/config` absent; `SSH_AUTH_SOCK` unset. |
| Existing host-local/console channel | This shell is not node1; exposed tools provide no host-console/SSH dispatch capability. |

The private receipt is
`HOST_DISCOVERY/access-resumption-20261027-4txekp_v/access-checks.json`, SHA256
`01535db7962488f96109cf93a4a4fbae6063cb07c3a3d030585e135e9f2a30e5`,
where `HOST_DISCOVERY` is the existing preparation root's `host-discovery-20261026`.
The index/catalogue bind this new check without changing the approved argv,
script, approval, original transports or unused-attempt state. Local UTC is an
observation, not an independently authenticated campaign clock.

Verification checks the bound files/hashes, JSON consistency, retained historical
bytes, private0700/0600 receipt permissions and `git diff --check`. No product
source changed, so completed capability tests and the one ordinary inspection
were not repeated. No remote command, SSH authentication retry, Kubernetes,
Docker query, calibration, infrastructure launch, credential or dependency change
occurred. Current capacity, signed net16250m/31GiB reservation, separate actual
allocations and independent Linux backup remain null. The existing approval is
still sufficient for its exact capture once a working trusted channel is
available; no second approval or manually authored prerequisite packet is needed.
Story27.4 remains in progress, full qualification/Production unproved and A41 open.

## Independent calibration capability and blocked host access — 2026-10-26

**Implementation/offline preparation is complete; no live target is admitted.**
Platform exposes distinct `--calibration prepare|inspect|launch|monitor|stop|remove`
commands through its existing Qualification process/lifecycle/ownership boundary.
The native workload, measurement plan, bounded host observation next action and
actual null prerequisites are in the [current campaign register](../../../docs/operations/access-telemetry-full-qualification.md#independent-calibration-and-host-discovery--2026-10-26-current)
and [existing preparation index](../../../docs/operations/qualification/full-campaign-preparation-2026-10-25/preparation.json).
One ordinary coordinator inspection is complete; all five findings are resolved. No live/Production/full-gate/A41
credit follows; local WINDOW evidence and original baseline remain intact.

The changed Platform sources are `qualify.py`, `campaign_lifecycle.py`,
`campaign_environment.py`, chart templates, new `independent_calibration.py`,
`calibration_workload.py`, focused tests and README. Memories adds explicit
`PG-INDEPENDENT-CALIBRATION-1` gate/run/plan binding and refusal cases. Campaign
`campaign_capacity.py` admission is unchanged. The shared lifecycle now exposes
schema/file/composition/budget/admission hooks; original campaign defaults remain.
The native child rechecks independent original authority and budget before writes,
uses the real fixed endpoint, and inherits the supervised process group. No new
Memories runner, dependency/version/submodule/index/ref mutation or remote action
was introduced.

The separate lane accounts for150000 real accepted records and requires an exact
SQL snapshot; the two-writer lane uses1801 one-second125-record segments per
writer, >=1800s common measured overlap and >=900000 actual state-operation counter
delta. Physical SQL includes heap/TOAST, every access-telemetry/support/index and
TOAST relation, database size and WAL. Fresh complete metrics-API CPU/memory,
target-side receipt latency and quiesced full-table rewrite readbacks are required.
Missing providers/readbacks stop the attempt. Configured bounds and unit receipts
never become measurements. This lane's occupancy/rewrite is not expired-backlog
drain, C2 replacement/fault, C3 real1/24/168h/24h reclamation or compatible renewal
proof. Existing campaign admission must independently accept actual future evidence;
the independent schema itself refuses there.

Offline evidence is retained under `HOST_DISCOVERY/offline-verification/`, where
`HOST_DISCOVERY` is the existing private preparation root's `host-discovery-20261026`.
Exact commands and hashed logs are in its `verification.json` (also indexed).

| Cwd / exact command | Executed result |
| :--- | :--- |
| Platform `eng/helm-qualification`: `python3 -B -m unittest -v test_campaign_capacity test_campaign_lifecycle` | 55/55 passed,5.028s; unchanged campaign sizing/admission/lifecycle. |
| Same cwd: `HELM_BIN=/home/administrator/.local/state/hexalith/memories/access-telemetry/full-campaign-offline-20261025-buc4thfg/tools/helm python3 -B -m unittest -v test_independent_calibration test_chart.ChartTests.test_finite_campaign_resources_replicas_and_sidecars_are_explicit test_qualify.QualifierFailureTests.test_timeout_terminates_spawned_process_group_and_disables_node_reuse test_qualify.QualifierFailureTests.test_early_leader_exit_and_cancellation_reap_descendant_writer` | 20/20 passed,10.565s; this selection preceded two additional native cases. |
| Same cwd: `HELM_BIN=/home/administrator/.local/state/hexalith/memories/access-telemetry/full-campaign-offline-20261025-buc4thfg/tools/helm python3 -B -m unittest -v test_independent_calibration` | Final19/19 passed,7.958s; actual local process tree, private files, signed controlled receipts, native writer/orchestration doubles and real Helm rendering. |
| Memories: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --no-restore -m:1 -p:UseHexalithProjectReferences=true` | Initial NETSDK1004 for missing EventStore Contracts/Client/DomainService assets. After the focused pinned restore below, passed with zero warnings/errors,24.73s. |
| Memories: `dotnet restore tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -p:UseHexalithProjectReferences=true` | Passed; no version changes. |
| Memories: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -method '*Gate_ExplicitProfileRequiresExactApprovedComponentAndShortMountedGateInQualification' -method '*Gate_AcceptsOnlyCurrentExactProfileInQualification' -method '*Gate_RejectsPg2OutsideQualification'` | 6/6 passed,0.155s; zero errors/failures/skips/not-run. |

An intermediate19-case run exposed an unsupported `Path.open(opener=...)` call
in native segment proof persistence (18 passed,1 error). Built-in `open` now uses
0600 and `O_NOFOLLOW`; the same19-case command passes. The earlier16-case and20-case
passing selections remain separate snapshots, without a single all-final-suite
claim. New authority/backing/measurement cases were not all executed red before
implementation; they establish final offline behavior only. No broad suite or
historical review was repeated.

Discovery inspected41 private,835 Memories evidence and159 Platform implementation
JSON files, selecting eight relevant records and retaining hashes in
`HOST_DISCOVERY/discovery-results.json`. No eligible signed current reservation or
same-input calibration was found in that stated scope. The retained Linux and
Windows strict-host-key transports both exit255 before remote execution. Their
exact argv/result hashes remain bound; credentials were not retried. Node1's current
CPU/RAM/engine/storage/net commitments and independent Linux backup remain null;
October2 gross32-thread/134924496896-byte hardware is historical only. Known-host
pins do not establish independent trust. No container/Kubernetes/provider call ran.

The incomplete private `calibration-plan.draft.json` binds actual retained tools,
pinned Dapr/Gateway/backend inputs and changed source/chart hashes. It preserves
null actual run/target/engine, independent storage allocation/reservation, runtime/
provider/reporter/CNI/credential, UTC/status/monitoring, adopted clean source and
outside-approval inputs. It was never supplied to prepare. No state/run identity,
measurement, renewal count, reservation term or grant was invented.

The only prepared next external action is the catalogue's host-local metadata
capture on node1: `timeout --signal=TERM --kill-after=2s 58s python3 -B -
--expected-hostname node1 --docker-host unix:///var/run/docker.sock`, with the
retained `host-observation-bounded.py` on stdin (SHA256
`4fdd0ef9f78257755f510cc1ce65cc2aa19c36b96db88578ef0d71715dbe88cb`).
It produces bounded JSON observations automatically, performs no infrastructure/
credential/cluster mutation, and leaves authority/reservations/independent-backup
null. Read-only children end within individual/55s script/60s outer bounds; there
is no infrastructure to dispose. It needs the host custodian's working trusted
local access channel, which this session does not have. Approval cannot replace
that access, actual capacity or independent backup; no launch decision is requested.

Session source heads remain Memories `092191bc67e0439f8d79ce60109c98e594b81734`
and Platform `935d74b89e1710abebdba2267b280814f8dc6b08`. Initial dirty Spec14
edits are preserved. Story27.4 stays in progress; full qualification, Production and
A41 remain open. Coordinator owns the one ordinary inspection of this delta and
its final bounded-action presentation.

### Final ordinary inspection and coordinator checks — 2026-10-26

Five findings were resolved within this delta; none deferred. R1 corrected native
transport cadence and uses acknowledgedUtcMs for durable acknowledgement latency.
R2 gives the suspended reporter an explicit zero-width window. R3 keeps measurement
quiescence inside original execution, waits for actual settled zero replicas,
checks original identities and monitors the disabled target throughout rewrite
without starting/resetting stop time. R4 retains observed operands before refusing
negative/less-than-20% allocation headroom and requires signed reservation ID,
owner, valid-from/end and active/non-revoked disposition. R5 supervises startup,
measurement and removal in one foreground launch, returns truthful CLI status,
and enforces the unchanged4GiB WAL ceiling. The native separate backlog lane still
proves occupancy only; no drain/renewal/campaign evidence is inferred.

The final exact command, cwd, HELM_BIN and SHA256-bound log are in
`HOST_DISCOVERY/offline-verification/coordinator-final-platform-v2.json`:
`python3 -B -m unittest -v test_independent_calibration test_campaign_capacity
test_campaign_lifecycle test_chart.ChartTests.test_finite_campaign_resources_replicas_and_sidecars_are_explicit
test_qualify.QualifierFailureTests.test_timeout_terminates_spawned_process_group_and_disables_node_reuse
test_qualify.QualifierFailureTests.test_early_leader_exit_and_cancellation_reap_descendant_writer`.
It passes **86/86**. This covers28 independent,55 existing capacity/lifecycle,
one existing chart and two existing process cases. The earlier final84-case
snapshot remains retained as a prior source snapshot. The direct Memories command
above was rerun after inspection: **6/6**,0.144s, zero errors/failures/skips/not-run.
The source Debug build result above remains applicable; no C# code changed after it.
Focused review-fix selections pass9/9 and7/7; their exact logs are retained beside
the final receipt. A coordinator edit first used the wrong working-directory path
and raised FileNotFoundError before mutation; the corrected absolute-path edit
and subsequent final check are the applicable result. No failure is live evidence.

The bounded host-local script now additionally captures selected existing Docker
CPU/memory limits and instantaneous stats without environment/credential payloads.
Its current raw SHA256 is bound in the catalogue/index; the earlier script hash
above is its historical preparation snapshot. The exact next action remains one
<=60s automated host-local observation, no retries, on node1 through an existing
trusted custodian channel. This session has no working dispatch access. It creates
no infrastructure, changes no workload and generates observations automatically;
no owner-authored packet is requested. Approval cannot supply the missing access,
net reservations, independent backup or operational authority. No calibration
launch is requested. The one ordinary inspection is complete; Story27.4 remains
in progress and A41 open.

Final preservation check: Memories/Platform/Builds HEAD and index hashes are
unchanged; dependency/configuration pins and root gitlinks are unchanged. Original
Spec14, runbook and single-report historical sections compare byte-for-byte.
The current continuation's14 references matched their actual bytes; no operational
state directory exists. Exact results are retained in
`HOST_DISCOVERY/coordinator-preservation.json`. Both owning `git diff --check`
commands exit0. Final86-case elapsed time is19.850s. The requested single bounded
action is now presented for approval; it does not include calibration or launch.

## Operational prerequisites continued — 2026-10-25

**Partial resolution: actual Dapr/Gateway inputs are bound; host reservation and
calibration are still blocked.** The [current operational result](../../../docs/operations/access-telemetry-full-qualification.md#operational-prerequisite-resolution--2026-10-25-current)
and [existing preparation index](../../../docs/operations/qualification/full-campaign-preparation-2026-10-25/preparation.json)
record the exact inputs, owner action and blockers. This continuation adds no
implementation, review, inventory rerun, qualification credit or Memories runner.

New retained Platform evidence identifies `node1` / `192.168.1.30`, node UID
`46d8d528-420d-4501-a6d8-12680d8a6ac6`. Its October 2 hardware observation
has 32 threads and 134924496896 RAM bytes; raw SHA256
`83e96612fc89364bf2f1ab4f23afbeee2cdb3a79e9c4e75423541b22f99911eb`
matches the retained checksum. October 25 installation evidence confirms identity
and protected workloads, without current available-minus-competing capacity or a
reservation. No suitable host is admitted. The 14-slot host still fails 16250m.

Actual off-node provider lead: Scaleway `hexalith-recovery-points`, project
`bed84924-0e48-4275-8b7c-5d9a1f57a8dd`, `fr-par`. Existing writer/validator
reference paths and distinct role/policy IDs are retained without credential
values. They are Story 4.0 project-scoped/no-expiry references, with no current
campaign grant or Linux-backed backup path. Its Redis/FalkorDB restore expired
`2026-10-02T07:30:43Z`; no finite PostgreSQL restore/measurement credit transfers.

New private `prerequisite-resolution/` under the existing custody root holds the
evidence bindings and receipts. Bounded HTTPS asset retrievals both exit 0 and
match the unchanged pinned SHA256. Six anonymous GHCR index/platform/config
chains verify by digest/descriptor size and linux/amd64 configuration; no layers
were pulled, no registry credential used and no publisher/rollout claim made.
Actual image references and both asset Refs are bound into the existing drafts.
Existing Platform `dapr_values` plus the retained Helm `template` command exits 0;
`validate_dapr_render` passes for 44 objects. Exact argv, source/values/render
hashes and timing are in `dapr-render-verification.json`. This is changed-input
validation, not supported prepare or repeated implementation tests/review.

No eligible same-input calibration or reservation appeared in the added evidence.
The catalogue specifies the host custodian's <=60s read-only commands and one
Platform calibration attempt: 2x125 records/s for 1800s overlap, >=450000 accepted,
>=900000 component operations, separate 150000 backlog, actual physical/support/
WAL/rewrite/compute readings; <=600s startup, <=50400s producer, <=900s cumulative
stop/remove, no retry. Actual target/native bootstrap/monitor/cleanup and backlog
command fields remain unfilled. Full campaign prepare and the admitted C2 producer
cannot bootstrap missing measurements. Platform must return eligible prior
receipts or its independently admitted prerequisite target and exact native plan.
The action does not replace all25 renewal timings, C2 drain or real1/24/168h proof.

Minimum next owner input is the path/hash of a Platform host/reservation packet
with current net16250m/31GiB, exact Linux primary/independent backup and term,
scoped provider/credential references, plus eligible calibration receipts or its
native prerequisite packet. Application/provider/reporter/CNI closure, compatible
renewals, independent UTC/status/monitoring, clean adopted source and authority
remain required before prepare. No launch permission is requested. Story27.4 and
A41 stay open; no infrastructure/cluster/daemon contact, staging, commit or push
was performed by this continuation.

Changed-input validation passed with 48 distinct referenced files checked,
including artifact digest chains, draft-only substitutions, preserved original
document bytes, unchanged sizing/workload/source inventory and private custody.
The exact replay command is `python3 -B
/home/administrator/.local/state/hexalith/memories/access-telemetry/full-campaign-offline-20261025-buc4thfg/prerequisite-resolution/verify-delta.py`;
its receipt is `prerequisite-resolution/delta-verification.json`, linked from the
preparation index. `git diff --check` exited 0. No implementation tests or ordinary
review were repeated. Concurrent commit `bbb51b4b58882d032ed1f729c0c8d453d14eb5e7`
consumed the turn's initially staged changes before this continuation edited
files. HEAD, index and staged-diff hashes remained unchanged from the editing
snapshot; original document content survives. No concurrent change was reversed.

## Actual operational input preparation — 2026-10-25

**Offline preparation is recorded; supported prepare remains blocked.** Platform's
completed implementation and its one ordinary review were not repeated. The
[concrete operational/disposal plan](../../../docs/operations/access-telemetry-full-qualification.md#operational-input-preparation--2026-10-25-current)
contains the exact supported command sequence and bounded external prerequisite
actions. The [input index](../../../docs/operations/qualification/full-campaign-preparation-2026-10-25/preparation.json)
binds the actual local artifacts. This is preparation evidence only; Story27.4
remains in progress and A41/full qualification/Production remain open.

Private custody:
`/home/administrator/.local/state/hexalith/memories/access-telemetry/full-campaign-offline-20261025-buc4thfg`.
It holds private executable/Builds support copies, tool/input drafts, offline
stdout/stderr/host trace and preservation receipts. `lifecycle-plan.draft.json`
contains nulls for unresolved actual inputs; it is not an approved plan and was
not submitted to `prepare`. `lifecycle-plan.json` and `campaign-state/` are absent.
No run/attempt/environment/session/target identity, signing key, credential,
reservation, measurement or authorization was fabricated.

Retained repository JSON artifacts bind26 source/import/command files, all6 chart
files,7 configuration templates, the toolchain, actual executable/support hashes,
PostgreSQL18.6/OpenBao2.6.4 pins, all7 graph workloads, all25 C1 parents, finite
record envelopes, complete monitor series and missing credential/trust bindings.
Local Helm archive SHA256 matches the existing4.2.0 pin, and the retained executable
matches its archive bytes. The current Builds CLI SHA256 is
`690f4cd11ec769040da125e5f6eca6b8ceabe47813664f3e596c49b774ee0e60`;
its existing `validate --environment` command is present. Raw working-file and
HEAD-blob hashes are distinguished, including PowerShell newline normalization.
Controlled C2–C4 producers require a clean worktree and approved committed source;
current staged/unstaged user changes remain untouched and are not declared eligible.

The local17:39:05.494985Z observation found14 CPU slots,97021218816 total /
67167330304 available RAM bytes and357742424064 available workspace bytes on
device2096. This local time/free-space observation is not an authenticated clock,
capacity reservation or backup proof. The CPU shortfall is at least2250m before
competing demand against16250m/31GiB. The existing evidence root contains historical
C1.16 only. Historical WINDOW serialization/TTL results, old1.18.1 image receipts
and synthetic1.18.7 render logs cannot supply the absent finite physical/renewal,
independent-clock or immutable operational image inputs.

Exact cwd/argv/exit/stdout/stderr hashes are in
[offline-checks.json](../../../docs/operations/qualification/full-campaign-preparation-2026-10-25/offline-checks.json).
All8 commands exited0: retained `kind version`, `kubectl version --client=true -o
json`, `helm version --short`, `docker --version`, the existing Builds
`validate --help`, Platform `qualify.py --help`, Platform `qualify.py
--campaign-capacity`, and Memories `verify-access-telemetry-lifecycle.py --help`.
Observed versions are kind0.31.0, Helm4.2.0, kubectl1.36.1 and Docker client29.8.2;
no client/server compatibility or running-target result is inferred. Supported
sizing returned `sizing.status: blocked` / `compatible-finite-renewal-plan-required`,
with null operational PG/backup allocation. Exit0 describes successful inspection,
not admission. No implementation tests, review, cluster/daemon commands or prepare
attempt were needed to establish these results.

Remaining prerequisites are actual source/argument-bound gate and renewal timings,
C2 drain/C3 isolation and physical sizing at full rate, exact images/assets and
credential-compatible configuration, trusted UTC/status/collector and scoped
credentials, aggregate reservations and independent backup/restore, clean source
adoption, physical target binding and owner authority. The plan specifies one
bounded artifact/host/trust/measurement/backup action at a time and original-owner
cleanup. New calibration infrastructure needs its own Platform-authorized plan;
the measurement-dependent full `prepare` cannot bootstrap that missing evidence.
No launch authorization is requested before the actual inputs and prepared result
exist. Existing WINDOW, historical profiles, gate rows, pins, workload/timing,
security and cleanup requirements retain their original identities.

**Final artifact verification.** The retained command
`python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/full-campaign-offline-20261025-buc4thfg/offline-artifact-verification.py`
checks actual references/tools, strict JSON, existing limits,25 gate parents,
all34 unchanged campaign rows, original document bytes and private custody.
[verification.json](../../../docs/operations/qualification/full-campaign-preparation-2026-10-25/verification.json)
records `artifact-checks-passed-with-external-git-change`. `git diff --check`
passed in all3 owners. The first `/tmp/verify_story274_operational_inputs.py`
invocation exited1 with `AssertionError: memoriesHeadUnchanged`: an external
process advanced Memories7b0c8b56->39be0009 (via5fd20e5f),
Platformfbac461->935d74b and Buildse171c5a->7f740a8, and committed initial input
JSON files. This agent performed no Git writes or reversal. Root gitlink pins
remain at their original commits; actual submodule checkouts advanced externally.
The receipt preserves this discrepancy; it does not claim unchanged HEADs/index.
Inventoried source/tool/configuration and protected package/profile bytes still
match. No operational state or admitted runtime identity was created.

## Supported full-campaign lifecycle implementation — 2026-10-25

The lifecycle continuation implements Platform's full-campaign mode on its existing
kind qualifier, with Builds descriptor validation and a Memories admission consumer.
This is offline implementation evidence. No infrastructure was launched, cluster
contacted, real credential issued, reservation made, or campaign gate passed. Story
27.4 remains in progress, A41 remains open, and historical profiles/WINDOW evidence
below remain unchanged. Dependency pins, HEADs and staged content are unchanged
in all three owning repositories.

The implementation supplies real TLS PostgreSQL/OpenBao configuration,
replicated persistent Placement/Scheduler, application/reporter bindings, private
run ownership and supported `prepare`, `inspect`, `launch`, `monitor`, `stop`, and
`remove` commands. The exact operational plan, provider/reporter/CNI inputs,
credentials, independent clock/authority/monitoring sources and measured reserved
capacity remain prerequisites. The single ordinary review is complete and all
seven substantive findings are resolved; none is deferred.
The [Spec 14 triage](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md#review-triage--lifecycle-continuation-one-ordinary-review)
records each finding. Earlier reviews were not repeated.

### Focused pre-review checks

All commands were run from the owning repository. Local .NET and Helm processes
validated artifacts; tests used temporary files and process/resource doubles.

| Check | Command / result |
| :--- | :--- |
| Builds descriptor reader CLI | `dotnet build src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj --no-restore -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -p:RunAnalyzers=false` — passed, zero warnings/errors. `/tmp/story274-builds-validator-build-final.log`. |
| Platform lifecycle | `PYTHONPATH=eng/helm-qualification python3 -B -m unittest test_campaign_lifecycle -v` — 17/17, 2.016s. `/tmp/story274-lifecycle-final.log`. |
| Platform capacity/configuration | `python3 -B -m unittest discover -s eng/helm-qualification -p test_campaign_capacity.py -v` — 18/18, 1.051s. `/tmp/story274-capacity-chart-tests.log`. |
| Affected lifecycle/qualifier subset | 20/20 before the final additional allocation case; exact selection/output in `/tmp/story274-focused-final.log`. |
| Application chart | `HELM_BIN=/tmp/story27-pinned-helm-w8usbu5u/helm PYTHONPATH=eng/helm-qualification python3 -B -m unittest test_chart.ChartTests.test_finite_campaign_resources_replicas_and_sidecars_are_explicit test_chart.ChartTests.test_identity_readiness_and_shared_dapr_security -v` — 2/2. `/tmp/story274-chart-render-corrected.log`. |
| Pinned Dapr chart | Local template render of the SHA256-verified 1.18.7 archive passed with three persistent Placement and Scheduler members, finite 256Mi/1Gi claims and immutable image references. `/tmp/story274-dapr-render-first.log`. No chart install occurred. |
| Memories handoff/capacity/cleanup | The selected command below passed 17/17, 5.937s. `/tmp/story274-consumer-focused-final.log`. |

```bash
PYTHONPATH=tests/tooling/access_telemetry_lifecycle python3 -B -m unittest   test_campaign_environment test_campaign_profile   test_campaign_review_fixes.CampaignReviewFixTests.test_c3_requires_original_before_dispatch_and_authenticated_three_day_selection_keeps_origin   test_campaign_review_fixes.CampaignReviewFixTests.test_parent_fallback_and_failure_packet_survive_drift   test_campaign_review_fixes.CampaignReviewFixTests.test_actual_disable_only_survives_component_and_measurement_drift_but_denies_foreign_scope -v
```

Failed/intermediate checks are preserved: the initial Builds `--no-restore` build
reported `NETSDK1004` (missing assets); an unchanged-pin Debug restore followed by
the build above passed. The first generated descriptor was rejected by the actual
reader with `HXT104`; corrected artifact/suite shapes pass. The initial lifecycle
fixture lacked a Helm tool binding and was corrected. An introduced qualifier
timeout-message regression was corrected and its focused case passes. A broad
qualifier run remains blocked by absent
`references/Hexalith.Platform/references/Hexalith.Builds/Props/Directory.Packages.props`;
no nested submodule was initialized. Broad consumer discovery returned 19/20:
the historical-closure fixture selects current HEAD while assuming that it predates
the campaign import; the affected selected cases above pass. A first application
chart invocation ran from the wrong cwd and failed imports; the corrected owning
cwd invocation passed. Discovery using `test_finite*.py` found zero tests and earns
no verification credit. These results do not imply a broad-suite pass.

### Final review-fix verification

All results below are offline; repeated runs are not added together as independent
coverage. The one ordinary review resolved readiness/Job handling, exact
quiescence/disposal, prepared/live input drift, descendant cancellation, safe
failure capture, complete authenticated monitoring and immutable paired handoff.
Acceptance corrections within those findings cover stop/reopen/remove, the exact
existing C4 fault/restoration path, cumulative expected-error allowances and
`monitoring` provider namespace ownership. The proposed ephemeral reporter-Job
finding was withdrawn after tracing the fixed existing producer Job; undeclared
Jobs continue to refuse. No second review was performed.

| Owner / final check | Exact command / result |
| :--- | :--- |
| Platform lifecycle | `PYTHONPATH=eng/helm-qualification python3 -B -m unittest test_campaign_lifecycle -v` — **37/37**, 3.848s, exit0. `/tmp/spec14-lifecycle-coordinator-final.log`. |
| Platform process cancellation/timeout | `PYTHONPATH=eng/helm-qualification python3 -B -m unittest test_qualify.QualifierFailureTests.test_early_leader_exit_and_cancellation_reap_descendant_writer test_qualify.QualifierFailureTests.test_timeout_terminates_spawned_process_group_and_disables_node_reuse test_qualify.QualifierFailureTests.test_sigterm_during_command_is_recorded_and_cleans_private_files_and_children -v` — **3/3**, exit0. `/tmp/story274-review-process-final.log`; also passed in the coordinator's 32-case lifecycle/process selection, `/tmp/spec14-lifecycle-coordinator-tests.log`. |
| Memories handoff/profile/cleanup | The selected Memories command above — **18/18**, 7.345s, exit0. `/tmp/spec14-lifecycle-coordinator-consumers.log`. Includes actual Builds contract-reader acceptance and coherent immutable observation consumption. |

The earlier18/18 capacity,2/2 application rendering, pinned Dapr rendering and
Builds Debug build remain applicable; no dependency/input changes required reruns.
The intermediate review-fix lifecycle lane first passed26/27 before a test fixture's
observation sequence reset was corrected. The C4/provider lane first passed36/37
before disabled-writer readiness/restoration expectations were corrected; the
final37-case coordinator run above includes both fixes. These failures were not
live environment runs.

The two broader checks were not repeated or claimed green:

- Platform: `python3 -B -m unittest discover -s eng/helm-qualification -p test_qualify.py -v`
  remains blocked by the absent nested Builds package catalog noted above;
  `/tmp/story274-qualifier-regression.log` retains the result. No nested dependency
  was initialized to bypass that boundary.
- Memories: `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_campaign*.py' -v`
  returned19/20 because the historical-source fixture's HEAD assumption is stale;
  `/tmp/story274-consumer-regression-second.log`. The affected18-case selection
  passes; this does not imply historical fixture repair.

Final preservation checks compare all three session HEADs, staged content and26
protected build/package/toolchain/sprint/deferred-work files. They are unchanged.
An initial byte-for-byte index-file assertion detected Git status's stat-cache
refresh; the corrected check verifies staged content against the initially clean
HEAD instead of treating index metadata as staged changes. No staging occurred.
The original report from `Smaller finite campaign implementation` onward and
original Spec14 from `Smaller full campaign profile` onward are unchanged after
newline normalization; completed WINDOW evidence remains intact. Every existing
campaign gate row, including all25 C1 gates, is unchanged. `git diff --check`
passes in each owner. Historical profiles, workload/timing/security/cleanup
requirements and dependency pins retain their existing identities.

### Operational handoff

Supported lifecycle commands and the incomplete operational input list are in the
[current campaign register](../../../docs/operations/access-telemetry-full-qualification.md#supported-platform-lifecycle-commands--2026-10-25).
Actual rollout, credential compatibility, trusted-source renewal, reservations,
backup recovery and physical disposal remain unexecuted. No launch authorization
is requested while those inputs are absent. The implementation and focused offline
verification are complete; actual campaign execution and qualification remain open.


## Smaller finite campaign implementation — 2026-10-25

**Implementation, focused verification and one ordinary review complete.** No full
campaign was launched or target contacted. The new `PG-FINITE-CAMPAIGN-1` path
is provisional pending actual measurements, source/command/input adoption,
reservations and Platform's full environment/lifecycle. Story27.4 remains in
progress; all34 full campaign gate rows, Production and A41 remain open. The
completed WINDOW evidence below is preserved byte-for-byte.

Platform owns `eng/helm-qualification/campaign_capacity.py`, additive maintained
chart configuration and offline `qualify.py --campaign-capacity` dispatch.
Memories owns `access_telemetry_campaign_profile.py`, explicit owner-run/v2
profile binding, finite C1.13/C1.18/runtime semantics and the actual existing
producer/offline/terminal/frozen-source consumers. Legacy constructors, default
KVM identity `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`,
400GiB70/80/90% arithmetic, multiplier2,128GiB rewrite floor and seven-day
exclusion remain historical and unchanged. (The exact full historical hash is
retained in the campaign register and canonical manifest.)

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

Admission binds actual tool/toolchain, immutable runtime/backend images, complete
chart, values/render, component/application/reporter, source/storage configuration
and finite command inventory. Exact retained receipt contents must match measured
operands, reservations, backing identities and competing demands. Missing full
heap/index/support/WAL/rewrite/compute observations, unreserved or non-Linux
backing, insufficient aggregate resources or same-domain backup refuses. Fresh
outside-pinned storage/compute receipts expire after300s and name the immutable
profile; observations may refresh only the original backing/allocation identities.

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

Self-reference is avoided deliberately. Values/render contain a named reference
to `access-telemetry-campaign-identity/profileSha256`, shared by the configured
lifecycle component and Qualification approved hash. After canonical hashing,
Platform's `runtime_binding` produces the exact late-bound ConfigMap descriptor;
its data is outside its own hashed inputs. C0 and every producer read it back
and bind profile/capacity identity before mutation, with physical target/source/
session authority unchanged. Server's runtime gate permits the finite hash only
in Qualification with matching approved/component/mounted short gate. Partial,
foreign, historical-as-finite, mixed local and Production intent refuse. The
actual target-readback test constructs values -> bound profile -> canonical hash
-> descriptor -> real consumer readback without mocking the calculation.

### Accessible sizing evidence and missing operands

- Read-only retained WINDOW
  `/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-preexpiry-window-20261025-1gcua2ah/result-artifacts/functional-observations.json`,
  SHA256`982a61627276a2fd3c71e13aaa47b5cecab14994b15e145c2c03b5aaab16f8e0`:
 250 actual retained canonical records encode to692 UTF-8 bytes each using
 `json.dumps(record, ensure_ascii=False, separators=(',', ':'))`. This is offline
 serialization of retained values, not physical PostgreSQL/index/support-state
 or full-rate measurement. The sizing calculation reports aggregate counts and encoded byte lengths.
- WINDOW `before-disposal/native-tests/pre-expiry-physical-cohort-a.json` contains
 timing/status fields, with no physical capacity/amplification operands.
- `/home/administrator/evidence/hexalith-memories` exists and contains C1.16 only;
 C1.13/C1.18 paths and corresponding measurements are absent.
- Read-only `/proc/meminfo`, `os.sched_getaffinity(0)` and `os.statvfs(Path.cwd())`
 at2026-10-25T11:29:44.721523Z:14CPU slots;97021218816 total /
 67109687296 available memory bytes;282774736896 workspace available bytes,
 device2096. CPU is below16250m. These readings reserve nothing and do not
 establish aggregate competing demand, Linux-backed durable data allocation,
 independent backup or performance. No existing reservation is claimed.

The [campaign register's conditional plan](../../../docs/operations/access-telemetry-full-qualification.md#conditional-concrete-launch-and-disposition-plan--not-ready-for-authorization)
records exact logical cluster/context/namespace, proposed private custody,
336-hour phase bounds, no retries, input/capacity/authority admission, failure
capture, owned stop/removal verification and prior-resource/Production/publication
exclusions. Actual native full-environment provision/stop routes, run/engine/API/
CA/UID identities, new Dapr/application/reporter digests, complete render/commands,
measurements/reservations and canonical operational profile remain missing.
The synthetic chart qualifier cannot fill those gaps. The plan is not deployable
or ready for authorization; no operational approval is requested. Platform still
must implement/supply those real environment/lifecycle/monitoring prerequisites.

### Executed focused checks

Commands below run from Memories root unless the cwd is stated. Unit capacity,
private files, command doubles and simulated elapsed time confer no live credit.

| Exact command / cwd | Result and limit |
| :--- | :--- |
| Platform root: `python3 -B -m unittest discover -s eng/helm-qualification -p test_campaign_capacity.py -v` |12/12 passed, final0.226s, exit0. Math, all component allocations, same-pool aggregate competition, independent backup, missing/drifted/stale operands and identity-preserving fresh capacity receipts. |
| `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_campaign_profile.py -v` | 9/9 passed1.042s, exit0; actual all25 owner gates, finite/old separation, frozen selection, refusal before child/target and actual descriptor readback. |
| Platform root: `HELM_BIN=/tmp/story27-pinned-helm-w8usbu5u/helm python3 -B -m unittest discover -s eng/helm-qualification -p test_chart.py -v` |19/19 passed127.235s, exit0. All historical chart cases and added finite resource/replica/sidecar case. Final affected late-binding/resource case rerun1/1 passed2.101s, exit0: `HELM_BIN=/tmp/story27-pinned-helm-w8usbu5u/helm python3 -B -m unittest -v test_chart.ChartTests.test_finite_campaign_resources_replicas_and_sidecars_are_explicit`, cwd Platform `eng/helm-qualification`. |
| `PYTHONPATH=tests/tooling/access_telemetry_lifecycle python3 -B -m unittest -v test_owner_review_fixes test_pg_onprem_2_adoption.ApprovedProfileAdoptionTests.test_authenticated_platform_child_runtime_passes_c0_and_downstream_inventory test_pg_onprem_2_adoption.ApprovedProfileAdoptionTests.test_both_python_cli_preflights_refuse_bound_input_drift_before_any_kubectl` |10/10 passed30.260s, exit0, after source/export fixture corrections. Existing C2 renewal, C3 original deadline/resume, cleanup/fallback, terminal/pre/postflight and C0/adoption paths. |
| `tests/tooling/access_telemetry_c1_interchange`: `python3 -B -m unittest -v test_owner_run test_owner_dispatch test_owner_custody` |106/106 passed1.513s, exit0; historical pure inspection/custody behavior. |
| `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_retention_verification.py -v` |41/41 passed79.878s, exit0, before final finite-only runtime/command-plan additions. Relevant export/adoption/authority paths were covered by the10-case rerun above; no final whole-suite claim is made. |
| Platform root: `python3 -B eng/helm-qualification/qualify.py --campaign-capacity > /tmp/story27-finite-candidate.json` | Exit0; emitted provisional status and missing measurements; after the renewal correction sizing is blocked with null PG/backup requests until a compatible schedule binds without a Qualification object or cluster call. No bound operational profile was emitted. |
| `dotnet run --project /tmp/story27-finite-runtime-gate-6bsli4lt/GateChecks.csproj -c Debug -p:NuGetAudit=false -p:UseSharedCompilation=false` | Exit0,12 runtime checks passed. Temporary project links the actual edited Gate.cs and current source-built Contracts DLL; positive finite and historical acceptance plus partial/foreign/uppercase/component/local/Production/short-gate/expiry refusals. No application topology launched. |

Helm was absent from PATH. A temporary copy of the already-pinned4.2.0 distribution
was fetched from the tracked toolchain URL; archive SHA256
`97dbeb971be4ac4b27e3839976d9564c0fb35c6f3b1da89dd1e292d236af4096` matched before
extracting its binary into `/tmp/story27-pinned-helm-w8usbu5u/helm`. No repository
dependency/toolchain pin changed and no cluster was contacted.

**Broad C# blocker.** Exact command:
`dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:RunAnalyzers=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -m:1 --no-restore`
failed15.55s with0 warnings/3 errors. Each is`NETSDK1004: Assets file .../obj/project.assets.json not found`
for existing EventStore.Contracts, EventStore.Client and EventStore.DomainService
source projects under`references/Hexalith.EventStore/src/`. No dependencies were
changed or submodules initialized. The narrow linked-source fallback above passed.
The added repository xUnit runtime-gate case could not run through that blocked
project; its complete owning-project build/test remains a disclosed prerequisite.

**Failed-check disclosure.** The initial25-case authority run had1 failure when
an uncommitted edited owner-run source was compared to the old fixture's HEAD hash.
The corrected real frozen-source fixture's named test passed1/1 in0.363s. The
initial20-case review/adoption run had6 failures/1 error: the same source-fixture
assumption and missing loader in old closed exports; corrected fixtures bind the
new import set and an unchanged actual C1 source. The affected10-case rerun above
passes without weakening refusal/assertion behavior. The new actual descriptor
readback test first exposed a missing `name` in the added runtime inventory entry;
that production defect was fixed. Two subsequent fixture errors used an old Dapr
digest and the test-only inline script's cached first bytes; explicit new digest
and distinct current/foreign fixture paths corrected them. The named test then
passed1/1 in0.482s. No pre-implementation red run is claimed; initial failures and
final passes are separate evidence. No live performance/retention/campaign credit
is inferred from these checks.

Changed files are the two profile/capacity modules; existing Memories verifier,
producer, owner-run/dispatch/custody/authority and gate semantics; the runtime
Server gate and its xUnit case; focused Python tests; Platform qualifier/chart/
tests/README; this report, Spec14, adapter appendix, campaign register and handoff.
The one ordinary coordinator review is complete and its four findings are fixed;
final coordinator verification passes, as recorded below. No
infrastructure, cluster, Git index/ref/commit/push, submodule initialization,
dependency or sprint-state mutation occurred. Parent baseline/frontmatter and
completed local evidence remain preserved.


### Coordinator final verification — smaller campaign

Final checks ran against the completed renewal-aware implementation. The one
ordinary review is complete: three high and one medium finding were accepted,
fixed and verified; none is deferred. The coordinator also corrected the omitted
C1 recapture inventory and authenticated the Platform module's approved source
bytes before executing its module body. An actual import-effect regression
confirms tampered source is refused before its side effect.

| Exact command / cwd | Final result |
| :--- | :--- |
| Memories root: `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_owner*.py' -v` |33/33 passed43.760s, exit0, no failures/errors/skips; `/tmp/story27-smaller-final-authority.log`. |
| Memories root: `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_retention_verification.py -v` |41/41 passed55.478s, exit0, no failures/errors/skips; `/tmp/story27-smaller-final-retention.log`. |
| Platform root: `python3 -B -m unittest discover -s eng/helm-qualification -p test_campaign_capacity.py -v` |18/18 passed1.408s, exit0, no failures/errors/skips; `/tmp/story27-smaller-final-capacity.log`. Includes the real offline original-admission CLI and renewal schedule/evidence refusal paths. |
| Memories root: `PYTHONPATH=tests/tooling/access_telemetry_lifecycle python3 -B -m unittest -v test_campaign_profile test_campaign_review_fixes` |16/16 passed5.813s, exit0, no failures/errors/skips; `/tmp/story27-smaller-final-campaign.log`. Includes all25 gates, actual round binding, frozen imports, original C3 deadline, stale first use, backing drift, actual disable/fallback after input drift, authenticated historical imports and rejection before changed Platform source executes. |
| Compile the11 changed production Python modules with `compile()`; call `validate_current_profile_inputs(Path.cwd())`; both owning repositories' `git diff --check` | Passed. Default historical hash remains `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`. |

The original18/15 focused fix runs passed1.367s/8.637s;25 affected
comparison/authority/offline/frozen cases passed42.276s. Two affected legacy
cleanup cases initially exposed a missing in-process Lease nonce; it was fixed,
and those cases passed2/2 in80.535s before the final41-case lane above. Earlier
33/41 coordinator runs passed52.045s/66.773s. No pre-implementation red run is
claimed. The latest finite-only source-import guard was followed by the16-case
rerun; legacy lanes do not enter that branch. The previously executed19 chart,
106 owner-custody and12 actual-source C# checks remain applicable to unchanged
paths. The owning C# test-project NETSDK1004 result and its linked-source fallback
are disclosed above; no passing complete C# project build is claimed.

Preservation checks match all four pre-change hashes: the completed report from
`## Functional result` onward, historical KVM profile JSON, sprint status and all34
full gate rows remain byte-identical. Spec14's original frontmatter/baseline and
in-progress parent state remain unchanged. No live elapsed, capacity reservation,
new operational canonical identity or full qualification credit follows.

Read-only final Git inspection found an unrelated Memories commit during this
work: HEAD advanced from`b29f3d67f90d4abead744f4f689d6be80ca40f8a` to
`d9826761def835880ee352b9df3517fdf4ac41e0` (foreign semantic hashes during erasure).
Its12 unrelated files were preserved. Platform HEAD remains
`b9cf0e78c42ca8bfe1aa5488cc4340c238887acb`. This workflow performed no staging,
commit, push, ref, dependency or submodule-initialization operation.

The implementation tasks are complete. Full operational PG/backup allocation and
canonical adoption remain blocked on real renewal/workload/timing/isolation,
physical amplification, issuer/UTC/key custody, reservations and Platform's native
full environment/lifecycle. Fixed non-data primary storage is33GiB and aggregate
compute16250m/31GiB; observed14-CPU capacity is insufficient. The conditional
launch/disposal plan and dates remain provisional and unapproved. Overall
Story27.4, full campaign, Production and A41 stay open.

The [active Spec 14](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) governs this functional run. The [full campaign checklist](../../../docs/operations/access-telemetry-full-qualification.md) remains separate.

## Owner authority and continuation implementation — 2026-10-25

The two Memories prerequisites are implemented and focused offline checks pass. The **one ordinary review is complete**; its four accepted integration findings are corrected and the focused fix checks pass. Coordinator final verification passed: **33/33 authority/continuation tests** and **41/41 retention/cleanup tests**; no finding is deferred. This section supersedes the earlier preparation note's implementation status; all actual WINDOW artifacts and the local functional result below remain unchanged. Full qualification, Production and A41 are open; Story 27.4 stays in progress and proposed campaign dates remain provisional. No infrastructure, cluster, dependency, Git index/ref or sprint-state operation was performed.

`tools/access_telemetry_c1_owner_authority.py` adds explicit owner-predecessor/v1 authorization through the existing lifecycle boundary. It reuses held private owner custody, outside plan approval, exact source/profile/session/physical target, all 25 distinct packets and their approved source/command contracts, retained output bytes, cleanup, complete parent graph and distinct accepted owner roles. `access_telemetry_c1_gate_semantics.py` checks structured output facts, including strong-read/end-state/ETag/atomicity, one-hour TTL bounds, request limits, two writers/30 minutes/250 accepted records per second and at least500 component operations per second,150000 backlog, tenant denials, TLS/runtime/backend identities, recomputed 400GiB capacity/7-day exclusion, allocator reuse and recovery/failure-domain/acceptance bindings. Summary pass labels cannot replace technical output or select their own approval root. Legacy v2 refusal and stronger independent-review semantics survive.

The existing launcher, offline validator, terminal/preflight/postflight/publication routes and frozen import closure use current authority. Terminal additionally requires the outside-approved digest of its complete result bundle, including exact distinct C5/C6 records; close-out and publish actions remain separate. The current KVM/Dapr1.18.1 canonical profile still passes input validation; the proposed kind/Dapr1.18.7 target cannot use it. Producer transport reads bind the API server/CA, kube-system and qualification namespace UIDs; the existing runtime inventory must match the approved initial digest before enable.

C3's existing locked/fsynced journal now persists an approved fixed UTC/boottime origin, deadline, exact session/context/prefix, runtime identity and in-flight command in a private companion state file. The outside plan binds that origin and fixed journal identity; a continuation separately approves both journal/state hashes. Interrupted read-only waits can resume over multiple days under renewed explicit same-session authority. Uncertain mutations, missing/tampered/truncated/reordered state, wrong context, concurrent lock, reboot/clock regression and exhaustion refuse. Original cohort/reclamation bounds and the777600-second ceiling remain; child waits clamp to the remaining original budget. C2/C4 remain50400seconds. Current authority is rechecked before operations and every five seconds during waits; existing disable/final-state and exact fault restoration remain available for bounded cleanup after expiry/withdrawal. A status writer cannot extend the finite outside-approved run grant; current verdicts may refresh within that unchanged grant while retaining the15-minute verdict and300-second status limits.

### Single ordinary review — four findings resolved

The one ordinary review identified four accepted high-severity integration defects: the immutable15-minute window prevented C2 completion, terminal code read legacy reviewer fields from owner ArtifactRefs, cleanup was blocked by expired execution authority and lost fallback context, and the parent C3 supervisor reset its ceiling. All four are corrected within the same scope; no additional reviewer, planning loop, code reversal, infrastructure action or Git mutation was used.

The approved `authorization` is now a finite run grant, while private current verdicts may renew only within that unchanged grant, owner record/session/action set and earliest applicable expiry. Each verdict still lasts at most900seconds, is at most15minutes old and has status at most300seconds old. A status writer cannot extend the approved grant, change sessions or add an action. The actual parent C2 path successfully supervises a local controlled producer across its first short verdict boundary and validates the existing two-writer/30-minute C2 fixture; this test does not claim1800real elapsed seconds.

The unused legacy C1 reviewer extraction is removed. A positive fixture exercises the actual complete owner terminal, recoverable preflight and staged postflight consumers with distinct C5/C6 records and outside-approved exact bundle/action scope. Source/packet/snapshot bytes and semantic validators are real; clean/index observations are simulated over an isolated model directory, and no repository is staged or closed. Changing the approved bundle digest refuses.

Cleanup now uses a dedicated original-plan/private-custody check that grants only fixed disable/final-state recovery. It survives an expired finite grant and withdrawal while still requiring the original approval anchors, current physical API/CA/cluster/namespace identity, approved runtime inputs and exact owned Lease. Per-execution nonces distinguish concurrent invocations even in the same approved session; primary and fallback retain the same nonce and predecessor/repository context. Actual CLI cleanup, an actual failed-primary/fallback subprocess pair, active foreign/same-session foreign-invocation denial and changed-target denial pass. Entirely missing original approval roots continue to refuse instead of treating a scenario file as authority.

Parent C3 launch and every supervision recheck now authenticate the original same-boot UTC/elapsed origin and clamp to the original remaining777600-second ceiling. Exhaustion refuses before child work. A genuine2-second local child is terminated/refused with1second remaining. The actual `_c3` orchestration reaches the registered prefix `retention-controls`, `c3-empty-preflight`, `cohort-168h-seed`, `cohort-24h-seed`, `cohort-1h-seed`; after an injected read-only wait interruption, an explicit approved journal/state continuation advances simulated time by3days, reaches that wait again, preserves exact prefix bytes and original origin, and does not replay seeds.

Focused fix verification from Memories root:

```bash
PYTHONPATH=tests/tooling/access_telemetry_lifecycle python3 -B -m unittest -v \
  test_owner_review_fixes \
  test_owner_authority.OwnerAuthorityTests.test_current_reference_revocation_unknown_expiry_clock_and_withdrawal_refuse \
  test_owner_authority.OwnerAuthorityTests.test_child_wait_cannot_reset_the_original_c3_deadline
```

**10/10 passed in27.455s**, zero failures/errors/skips; `/tmp/story27-review-fixes-final.log`. Compilation of the three edited production Python modules and two affected test modules passed. Only the tests covering edited authority/producer/consumer files ran for this fix handoff; coordinator verification completed below. Earlier focused fixture failures are retained in `/tmp/story27-review-fixes-initial.log` and `/tmp/story27-review-terminal.log`: a finite test-clock iterator exhausted, simulated future UTC/boottime did not match the original origin, the test host had less than9days uptime so its first proposed old-boottime operand was invalid, and a terminal fixture used `+00:00` instead of canonical `Z`. Corrected fixture inputs pass without weakening production checks.

### Coordinator final verification

All commands ran from the Memories root against the settled code after all four review fixes. These are offline fixture checks; simulated multi-day time and controlled target facts provide no live qualification credit.

| Exact command / check | Final result |
| :--- | :--- |
| `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_owner*.py' -v` | **33/33 passed in 65.539 s**, exit 0, no failures/errors/skips; `/tmp/story27-coordinator-authority-final.log`. Includes all 25-gate authority checks and all eight review regressions. |
| `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_retention_verification.py -v` | **41/41 passed in 86.559 s**, exit 0, no failures/errors/skips; `/tmp/story27-coordinator-retention-final.log`. Retention, reclamation, strong reads, newer controls, credential safety and all cleanup/Lease assertions remain. |
| Compile the five changed production Python files and three affected test files with Python `compile()`; import and call `validate_current_profile_inputs(Path.cwd())`; `git diff --check` | All passed. No dependency or profile pin changed. |
| Compare against the initial continuation snapshot | Completed local functional evidence section and every full-campaign gate row are byte-identical. Spec 14 frontmatter, original baseline and `in-progress` state are unchanged; no sprint/index/ref mutation. |

The unchanged owner run/dispatch/custody lane's **106/106** pass remains applicable. The single ordinary review and its four accepted findings are complete, with none deferred. The current prerequisite tasks are complete; the overall story, all campaign gates, Production and A41 remain open. Actual Platform services, signed independent UTC/status inputs, lifecycle/disposition support, reserved capacity, exact new profile and the operational launch decision remain outstanding. Proposed dates remain provisional.

| Pre-review verification, exact cwd/command | Executed result / retained local log |
| :--- | :--- |
| Memories root: `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_owner_authority.py -v` | **25/25 passed in20.397s**, no failures/errors/skips; `/tmp/story27-authority-focused-final.log`. Includes all25 packets and rehashed bad facts for each, wrong owner/profile/source/session/physical target, key replacement/resigning, stale/revoked/unknown/expired authority, chain-end expiry, CLI denial, frozen imports, mixed-session offline checkpoint, real child-wait revocation/deadline and three-day journal continuation/interruption/exhaustion. |
| Memories root: `python3 -B -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_retention_verification.py -v` | **41/41 passed in67.913s**, no failures/errors/skips; `/tmp/story27-retention-final.log`. Existing retention/strong-read/newer-control/physical-reclamation/credential/Lease/cleanup assertions remain. This full lane preceded the final origin/window and deadline hardening; affected cleanup paths were rerun below. |
| `tests/tooling/access_telemetry_c1_interchange`: `python3 -B -m unittest -v test_owner_run test_owner_dispatch test_owner_custody` | **106/106 passed in0.602s**, no failures/errors/skips; `/tmp/story27-owner-prerequisites.log`. Existing inspection helpers retain their read-only contracts. |
| Memories root: `PYTHONPATH=tests/tooling/access_telemetry_lifecycle python3 -B -m unittest -v test_retention_verification.RetentionVerificationTests.test_producer_restores_disabled_state_when_body_fails_after_enable test_retention_verification.RetentionVerificationTests.test_producer_restores_disabled_state_when_enable_response_is_malformed test_retention_verification.RetentionVerificationTests.test_producer_refuses_unverifiable_target_identity_without_mutating_target test_retention_verification.RetentionVerificationTests.test_producer_recovers_only_an_expired_story_lease test_retention_verification.RetentionVerificationTests.test_producer_rejects_active_foreign_lease_without_mutating_target` | **5/5 passed in50.817s**, no failures/errors/skips; `/tmp/story27-retention-final-cleanup-root.log`. |
| Memories root: `git diff --check`; `python3 -m py_compile tools/access_telemetry_c1_owner_authority.py tools/access_telemetry_c1_gate_semantics.py tools/access_telemetry_producer_common.py tools/verify_access_telemetry_lifecycle.py tools/verify-access-telemetry-lifecycle.py tests/tooling/access_telemetry_lifecycle/test_owner_authority.py tests/tooling/access_telemetry_lifecycle/test_retention_verification.py`; import `validate_current_profile_inputs` with `tools` on `sys.path`, then call `validate_current_profile_inputs(Path.cwd())` | Passed. The current profile is unchanged. No .NET, target or broad infrastructure lane was needed for these Python consumer changes. |

**Failed-check disclosure.** Initial new fixtures had seven setup errors from missing byte units; the first actual positive path then exposed immutable mapping serialization, zero-byte stderr-reference and bounded stream-shape handling, all corrected with executed passing reruns. The initial existing41-test run had two assertions and five errors because the producer entry now requires actual private authority; its body/Lease/cleanup fixtures were extended with that authority, retaining their original assertions. An intermediate positive offline checkpoint failed an argument hash after the fixture mutated shared arguments; the fixture now rebinds both hashes. One edit had an unmatched parenthesis caught by compilation and corrected before the passing runs. A final targeted five-test command was initially run from the test directory, so the source-owned producer refused its wrong cwd and the tests errored on absent operation logs; the root-cwd command above passed without changing an acceptance assertion. Earlier logs remain `/tmp/story27-authority-new-initial.log`, `/tmp/story27-authority-regression-initial.log`, `/tmp/story27-retention-final-cleanup.log` and intermediate named `/tmp/story27-authority-*.log`. No pre-implementation red run or live multi-day elapsed evidence is claimed.

**Proof limits and remaining work.** Tests use real private temporary files and actual consumer/journal/process paths, controlled target command doubles and simulated multi-day time. The positive offline payload test models the committed-source resolver with real frozen source files because this task may not commit the new imports; it confers no running-target credit. Frozen `--help` checks prove import closure only. No complete live C0–C6/terminal campaign, new full-profile producer registration, independent UTC/status provider, supported Platform multi-day environment lifecycle or real400GiB reservation was created. Platform must supply the actual independently authenticated fresh clock/status inputs, API/Namespace read authority, PostgreSQL/OpenBao/Scheduler/security/storage, persistent lifecycle/disposition and measured capacity. Memories' exact new kind-profile adoption remains separate work. The [runbook contract](../../../docs/operations/access-telemetry-lifecycle.md#owner-authority-and-c3-continuation-consumer--2026-10-25) and [campaign register](../../../docs/operations/access-telemetry-full-qualification.md) retain these requirements. Coordinator verification confirms the same single ordinary review and all four resolved findings; full-campaign prerequisites remain open.

Tenant/evidence scope surfaces are the owner source/profile/session/target boundary, exact command projections and continuation state. Named negative proof includes `test_wrong_owner_profile_session_source_target_and_phase_refuse`, `test_actual_api_ca_cluster_and_namespace_mismatch_deny_before_mutation`, `test_offline_checkpoint_uses_actual_consumer_and_refuses_mixed_session`, `test_rehashed_gate_facts_cannot_override_technical_policy` (including all25 running-profile tenant-denial combinations) and the unchanged strong-read/newer-control/credential cleanup lane above. These preserve the Story20.2 denial-before-dependency and Story24.3 verifier/tenant-marker boundaries; fixture results do not discharge their live campaign gates.

## Next campaign preparation — 2026-10-25

This preparation snapshot predates the implementation section above; its original evidence and then-open prerequisites are retained.

The owner delegated target/schedule selection with **“I dont know, do recommended”**. The [concrete recommendation](../../../docs/operations/access-telemetry-full-qualification.md#recommended-target-and-provisional-schedule--2026-10-25) selects Platform's maintained isolated kind/chart foundation and proposes **2026-10-26T08:00:00Z–2026-11-09T08:00:00Z** (336 hours). This contains the unchanged C3 216-hour and C2/C4 14-hour ceilings, prerequisites, review and final disposition. The proposal expires if readiness or a phase boundary is missed; it is not an operational grant or capacity reservation.

Read-only investigation at Memories **`98383a95b36638399f7e48b29dd009ab6125be2d`** and root-declared Platform **`ce90c4215f3343c2d78d8aee2ebada92405227ff`** found tested chart/admission isolation and owned cleanup, but no supported full Memories PostgreSQL/OpenBao environment, full resource reservation or multi-day campaign lifecycle. Platform's toolchain is kind0.31.0/Kubernetes1.35.0/Dapr1.18.7/Helm4.2.0/Aspire13.6.1. Its Scheduler-disabled synthetic qualifier cannot supply full-profile evidence. The checklist records exact owning source paths, required changes and the Linux workspace's **120,523,268,096-byte** free-space observation, below400GiB before overhead. No cluster was contacted or resource allocated.

Offline canonical validation passed: from Memories root, `python3 -B` with `tools` on `sys.path`, import `validate_current_profile_inputs` and `canonical_pg_onprem_2_profile` from `verify_access_telemetry_lifecycle`, then call `validate_current_profile_inputs(Path.cwd())`. The recomputed former profile hash is **`95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`**. New kind/Dapr1.18.7 adoption must replace that identity through reviewed source/input changes; neither a new hash nor full gate acceptance is claimed here. No version pin changed.

Root independently re-read the authentic WINDOW result/TRX and recomputed both retained observation/TRX hashes below: native **1 executed / 1 passed / 0 failed / 0 skipped**, artifact verification passed, cleanup verified with empty owned processes and **5.366938s** stopping. The implementation agent also re-ran the existing focused offline command from `tests/tooling/access_telemetry_functional_retention`:

```bash
python3 -B -m unittest -v test_selected_read_retry \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_complete_sixteen_second_observations_and_sql_finish_before_expiry \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_complete_near_budget_observations_and_ten_second_sql_finish_before_expiry \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_late_preexpiry_response_is_refused_even_when_the_record_is_still_present \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_late_scheduling_is_refused_before_dispatch_and_retains_unobserved_ordinals \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_response_time_precedes_slow_guard_completion_without_implying_late_absence
```

**33/33 passed in18.058s, exit0** (28 retry cases plus five named acceptance cases); `git diff --check` also passed before these documentary additions. This rerun is offline regression evidence. The completed local outcome remains unchanged. Full preparation is **blocked on Platform-owned environment/capacity and Memories' authority/continuation implementation**, with exact profile adoption and a complete authorized launch plan still pending; all full/Production/A41 states remain open.

One ordinary reviewer independently checked the target/toolchain, profile distinction, source capability, capacity and UTC arithmetic. The accepted substantive finding is now an explicit Memories-owned prerequisite: `_validate_predecessor` refuses structurally valid v2 predecessors until actual I3–I5 authority, retained-reference custody, session status and P7 bounds are verified; the runbook also requires verified owner-mode migration and multi-day continuation. Environment/profile changes cannot bypass that refusal. Documentary checks preserve all34 original full gate rows byte-for-byte, resolve added local links/anchors, and reconcile the phase seconds to **172800 + 50400 + 777600 + 50400 + 157500 + 900 = 1209600** (336hours). An initial ad-hoc row comparison also selected the new schedule table; scoping it to the original `Gate` table corrected that check without changing a gate. The existing local pass and all open execution prerequisites remain accurately recorded.

### Ordinary-review fixes and renewal acceptance correction

The four accepted findings in Spec14 are resolved. Strict initial freshness and
outside-approved original admission prevent a new48h-old profile from entering
actual selection/CLI launch; actual three-day C3 selection retains profile and
original origin, while missing/wrong issuer/receipt denies before dispatch.
Pool/failure-domain/allocation substitution refuses. Original owned disable-only,
real parent fallback and failure packet construction survive component/measurement
drift, with foreign API/session/Lease still denied. Committed-source import
inspection preserves authentic historical exact closures and requires all current
imports without transferring historical credit to the new finite profile.

The coordinator acceptance correction also supersedes the initial single-C1
sizing above. Actual renewal/timing/noninterference/physical evidence is absent;
no operational N, PG/backup allocation, issuer/key/clock or adopted receipt exists.
No second review or new planning workflow was run. Final coordinator checks remain
pending. The exact additional focused commands/results are recorded below.

| Exact focused fix command / cwd | Result and scope |
| :--- | :--- |
| Platform `eng/helm-qualification`: `python3 -B -m unittest -v test_campaign_capacity` | Final18/18 passed1.367s, exit0. Real offline qualifier CLI forwards original-admission anchors and denies foreign issuer. Fresh initial admission, immutable three-day identity, foreign backing denial, all-round inventory/coverage, missing/incompatible timing/isolation/drain and capacity refusal are covered. All receipts/schedules are temporary simulations. |
| `PYTHONPATH=tests/tooling/access_telemetry_lifecycle python3 -B -m unittest -v test_campaign_profile test_campaign_review_fixes` | Final15/15 passed8.637s, exit0. Actual finite owner round/capture binding, all25 packets, real frozen CLI, initial48h denial, C3 mandatory original proof before dispatch, authenticated three-day selection with unchanged origin, input-drift disable-only/fallback and foreign scope denials; authentic old committed C0/C2-C4 closures pass and omitted current imports/new-profile archival credit refuse. |
| `PYTHONPATH=tests/tooling/access_telemetry_lifecycle python3 -B -m unittest -v test_campaign_profile test_campaign_review_fixes test_owner_review_fixes test_owner_authority.OwnerAuthorityTests.test_offline_checkpoint_uses_actual_consumer_and_refuses_mixed_session test_owner_authority.OwnerAuthorityTests.test_frozen_source_import_closure_is_complete` |25/25 passed42.276s, exit0 after renewal-round wiring; before the final source-bound C3 operation-reservation detail, whose affected15 finite cases were rerun above. Covers existing short-verdict renewal, multi-day journal, cleanup, fallback and terminal/offline/frozen paths. |
| `PYTHONPATH=tests/tooling/access_telemetry_lifecycle python3 -B -m unittest -v test_retention_verification.RetentionVerificationTests.test_producer_restores_disabled_state_when_body_fails_after_enable test_retention_verification.RetentionVerificationTests.test_producer_restores_disabled_state_when_enable_response_is_malformed` |2/2 passed80.535s, exit0 after the in-process owned Lease nonce correction. No broad suite was rerun by the implementer during review fixes. |
| Coordinator pre-fix compatibility lanes, retained `/tmp/story27-smaller-coordinator-authority.log` and `/tmp/story27-smaller-coordinator-retention.log` |33/33 authority in52.045s and41/41 retention in66.773s passed before these four fixes/renewal correction. These are accurate pre-fix results, not final settled-code claims. Root will perform final compatibility verification. |
| `git diff --check` (Memories and Platform) | Passed. Original Spec frontmatter, historical KVM bytes, sprint bytes and all34 full-gate rows are preserved; report bytes from `Functional result` have SHA256`0459e3b2893a5ee665ce4b1622d84a8db565cd69b10c31440c33524f5274e76a`. Tracked CRLF documentation format is retained. |

**Fix-run failure disclosure.** An initial21-case focused compatibility run passed
19 and failed two legacy cleanup cases: the new cleanup Lease comparison omitted
the nonce stored on the in-process target. It now uses that owned target nonce
or the inherited execution nonce; both failed cases reran green above. Two
intermediate unittest commands omitted the needed cwd/PYTHONPATH and failed import
before tests; an initial renewal test invocation used the old fixture before its
schedule update (15 errors). Corrected focused commands/results are recorded
separately. No production failure or live campaign credit is inferred from setup
errors or simulated checks. The existing C# restore-assets limitation and12-check
linked-source fallback above remain unchanged; no C# file changed in these fixes.

## Functional result

### Corrected-window execution — 2026-10-25; local functional retention passed, cleanup verified

**Local functional completion is proved.** Authorized run **`0673d50339f4e1058c26e3a9b2d680a2`** completed **07:26:20.7391127Z**, about **62 minutes 5 seconds** after native request. Authentic native result is **completed / Success / 0 / HXI005**; native TRX and reported counts are **1 executed / 1 passed / 0 failed / 0 skipped**. `functional-observations.json` reports `functional-observations-passed`. Platform's actual selected-run terminal reports **removed / empty / unverified:false**, and exact private-engine disposal is independently verified. This completes the local functional track only. **Overall Story 27.4 remains in progress; full qualification, Production and A41 remain open.**

The owner said **“i authorize a live run”** after concrete preparation and one ordinary review with no substantive findings. Coordinator binding, admission and dispatch are recorded below. One launch, zero restarts/retries; the single allowance is consumed. Approved plan/command bytes and native markers remain unchanged. Actual dispatch was **06:24:15.597776Z**; the **10:24:15.597776Z** execution deadline and separate cumulative stopping900 were respected. Native/coordinator/private-disposal exit codes are all **0**.

| Actual observation | Retained result |
| :--- | :--- |
| Cohorts A and B | Each125 attempted/enqueued/acknowledged/persisted/transaction acknowledgements; zero rejected/dropped/conflicted. Both initial strong reads found125 objects. |
| Actual emission-clock separation | **1800.002 s UTC / 1800.087879501 s monotonic**, from validated `cohortClocks`; response timestamps alone are not used. |
| First A pre-expiry read, age3300 | **07:20:36.308–07:20:53.197Z**, **16.890377 s**, all125 present. Every response precedes earliest validated expiry by at least **283665 ms**. |
| Pre-expiry stored-row/TTL observation | Read-only SQL completes **07:20:53.456Z**; all125 original rows/TTL bounds pass. TLS1.3 / observer role / PostgreSQL180006 remain bound. |
| Final A pre-expiry read, age3450 | **07:23:06.308–07:23:23.151Z**, **16.844647 s**, all125 present. Every response precedes earliest validated expiry by at least **133674 ms**, and its own expiry by at least **133795 ms**. |
| Validated A expiry range | **07:25:36.307–07:25:36.431Z**, unchanged3600-second source-age retention. |
| Post-expiry A strong reads | **07:25:36.432–07:25:52.781Z**, all125 absent. |
| Due-index and reminder/purge progress | All57 addressed bucket reads absent, covering all125 due IDs. Reminder sequence **0→17**; stored purge progress **07:25:38.179Z**, **1748 ms** after last logical expiry, within900-second allowance. |
| Canonical B preservation | **07:26:00.669–07:26:17.666Z** strong reads find all125 newer records, unchanged by full canonical/hash/identity/expiry validation. |

**Assurance limit retained.** This is actual local PostgreSQL/Dapr, source/Debug, signed development UTC and private-file-secret evidence, `sole-owner/self-reviewed`, local profile **`f17bd8da9c6104df75c192d100bb9c42645307d5ce2f4f89dafc5135dadcd382`** with `qualificationCredit:false`. The last SQL observation still contains **125 expired physical tuples**; all125 have strong TTL absence and due-index cleanup, while `actorDeletedRecordIds` is empty. No physical deletion mechanism, physical reclamation, independent UTC, full-profile, Production or A41 pass is inferred. The native environment reports physical-snapshot revision `74e4434fe2bffa7d850df99c5919706ba080cb31` / dirty; attribution uses the frozen selected inputs and actual Platform mapping, not a claim that every current-root file was executed.

**Actual terminal and disposition.** Selected-run terminal is **07:26:17.9891075Z**, completed/successful. The existing graph collector then records `container-event-kill` at **07:26:18.417821Z**. It follows the successful selected terminal; no initiating kill caller is inferred. The controller enters bounded stopping, supported same-prefix exact-run `down` returns **0** at **07:26:20.516002Z**, and native process exit0 is observed **07:26:21.831130Z**. Actual native result remains completed/Success, with **resourceDisposition:removed**, no remaining containers/processes and `unverified:false`. At **07:26:23.784748Z**, two exact private empty-engine checks and recorded PID/start cleanup verify **no owned processes, namespace members, socket, native workspace or run state**. Cumulative stopping is **5.366938 s**, within900. Earlier/shared resources remain excluded.

**Retained authentic artifacts.** `WINDOW=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-preexpiry-window-20261025-1gcua2ah`. `WINDOW/result-artifacts/` holds byte-identical native `functional.json`, `functional.python3.trx`, `native.stdout.json` and `functional-observations.json` (the native export's original name is `functional.functional-observations.json`). Native observation SHA-256 **`982A61627276A2FD3C71E13AAA47B5CECAB14994B15E145C2C03B5AAAB16F8E0`** and TRX SHA-256 **`EF2838182C0A25ADE419188201141386F6FBCBF7327E8695BF0D9DEB20EE10EF`** match the actual native `artifactHashes`/selected-run hashes. All ten private accounting/read/SQL packets are preserved byte-identically in `live-native-tests/` and `before-disposal/native-tests/`; `retained-phase-artifacts.json` binds them. `artifact-verification.json` and `run-result.json` record coordinator verification of success/TRX/hashes, both cohorts/clocks, expiry/phase timing, index/reminder/B behavior and actual disposition. The authentic observation's `lifecycleOutcome:pending-real-Platform-outcome` remains its consumer-time snapshot; the separate actual native removed terminal and private disposal receipts establish the later outcome without rewriting it.

The first three offline tasks and coordinator invocation/disposition task are complete. No review finding was deferred. No further native launch is authorized. The full campaign stays separately scheduled under its existing per-gate inventory; no sprint/Git/dependency state or A41 residual is closed by this functional result.

### Corrected-window launch plan — 2026-10-25; historical preparation, executed once

**The one authorized native invocation completed successfully; actual results and disposition are recorded above.** Private **`WINDOW=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-preexpiry-window-20261025-1gcua2ah`** contains the concrete `run-plan.json`, full-prefix `launch-command.txt`, existing bounded controller/disposal helpers, preparation/control/source proofs and exact offline logs below. Earlier SENTRY evidence and its consumed authorization remain unchanged. One ordinary review completed with no substantive findings. The owner then said **"i authorize a live run"**; `approval.json` records coordinator binding to the final prepared plan/command, and the one launch is now consumed. This does not claim owner review of later hashes. Actual supported run **`0673d50339f4e1058c26e3a9b2d680a2`** dispatched **2026-10-25T06:24:15.597776Z** after once-only actual admission:14 CPUs,97,021,218,816 Docker memory bytes,81,758,130,176 Linux available bytes and both disk floors passed. Execution deadline **10:24:15.597776Z** includes startup600; stopping900 remains separate. Startup admission persisted **06:25:35.857558Z**, before A accounting observation **06:25:37.353Z**. Both A and B subsequently passed125/125 accounting with zero rejected/dropped/conflicted, and both initial strong reads found125 objects. B accounting observation is **06:55:37.320Z**; response timestamps alone do not prove exact emission spacing. Immutable phase/accounting packets are also copied read-only to `WINDOW/live-native-tests/` before normal native disposal can remove the workspace. The subsequent pre-expiry/TTL/strong-absence/index/reminder/B assertions and actual disposition completed as recorded above. `coordinator-context.json` retains the active foreground controller/disposal session; no second dispatch is permitted.

**Concrete command and identities.** Cwd `WINDOW/compatible-platform`, direct owner `WINDOW/compatible-platform/references/Hexalith.Memories`, fresh state `WINDOW/native-run`. The existing full-prefix command is retained verbatim in `WINDOW/launch-command.txt`; its supported suffix is:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-preexpiry-window-20261025-1gcua2ah/functional.json \
  --output json
```

Independent Docker **29.8.2**, engine **`7d069d02-174a-4fea-a904-5c36296748f4`**, socket **`unix:///run/hx-retention-1gcua2ah/docker.sock`**, namespace keeper **70428**, daemon **70460**, has **seven identical immutable images / zero containers**. Seven recorded PID/start/namespace identities bind this empty preparation engine; host loopback remains disabled. No earlier/shared daemon is reused. Source/Debug, tool **4.35.2**, Platform **`7021c41bea55bd6c690ba96082cf3dcfc95c9fca`**, Builds **`007bb9bc2806a0ead02d4831555bf2134a38d256`**, EventStore **3.127.0**, Dapr **1.18.1**, PostgreSQL **18.6**, SQLite discovery workaround, ACL/mTLS, signed development time and all original retention/assertion limits remain. The selected Python consumer alone changes among **330 selected inputs**; the two test files are also adopted in the isolated copy. The selected mapping has **17 direct entries / no findings**, hash **`010e4f9555675d99adc0909a56aa35c944dd46252a42aefaff271969ae0b1020`**.

**Affected preparation only.** Existing native offline model build command succeeds in **13.374 s**; its **36 outputs / 1089 source documents** bind the new physical paths. Sixteen DLL/PDB outputs change with those paths; no source/version update is inferred. Reused application source proof passes **14 Server checks**, **35 Contracts documents** and **50 lifecycle documents** (plus the lifecycle Contracts copy), with nonzero signatures matching the prior genuine source proofs. Existing compatible application binaries are reused. Readback verifies **330 selected / 36 model / 58 operational / 489 runtime** hashes, source helpers, images, ownership and fresh absent evidence/state. Actual installed prerequisite and resolver probes pass, including Aspire **13.6.1**. Coordinator independently reconciled all **59 current affected test IDs** to executed passing evidence across the disclosed runs and verified the three source hashes against root/candidate; no single final59-test suite claim.

Private controls: `python3 -B -m unittest -v test_operator_bounds` passes **23/23**; `python3 -B -m unittest -v test_observe_startup test_native_host_admission test_additional_sources test_discovery_admission` passes **54/54**; `python3 -B verify-routing.py` passes **16/16**. Commands run from `WINDOW`; receipts are `preparation-control-checks-final.json`, `docker-routing-checks.json`, `context-probes.stdout.json`, `binding-commands.json`, `root-preparation-readback.json` and `coordinator-task-verification.json`. Initial control preparation had a gratuitous authorization-field assertion incompatible with an unchanged fixture and an omitted copied generation fixture; remove the extra assertion, preserving the original approval-file/hash requirement, and copy unchanged fixture bytes. Original **23-case / 1 error** and **54-case / 1 error** logs remain beside the final passes. Initial readback refused the inherited SENTRY evidence basename; `readback-refused-inherited-evidence-basename.json` is retained, and the fresh evidence path above passes. None launched native work or relaxed an admission guard.

**One bounded proposed invocation.** New observations target **3300 / 3450 s**, explicitly refusing late dispatch/response; real **125/125, 1800/3600/900 s**, concurrency16, read120, TTL/index/reminder/B preservation remain. One launch, **zero retries/restarts**, **14400 s execution including 600 s startup**, separate **900 s cumulative stopping**. Latest dispatch **2026-10-25T10:27:22Z**, absolute execution end **14:27:22Z**, outer disposal end **14:42:22Z**. At dispatch, once-only actual admission requires **8 CPUs, 16 GiB Docker/Linux available memory and both 20 GiB disk floors**; PostgreSQL retains **8 CPU / 16 GiB** limits. There is no whole-graph reservation. No capacity pass is claimed before that check.

After authorization the existing controller checks the exact plan/command/input hashes, capacity, isolation and fresh paths, starts the unchanged bounded captures/observer and dispatches the supported native tool once. Preserve genuine result/TRX/read/TTL/accounting/sidecar evidence on every outcome. Invoke same-prefix supported `down --run-id <actual-new-run-id>` when an actual run exists, then `sudo -n python3 -B dispose-owned.py` from `WINDOW`, verifying the exact private engine/process/socket/namespace/workspace/state absence within the original stopping budget. Early refusal without a native identity records supported-down inapplicable; never invent a run ID. Earlier/shared resources remain excluded. Parent Story27.4, local functional retention, full qualification, Production and A41 remain open pending their own actual evidence.

### Pre-expiry observation window — 2026-10-25; offline correction

**Diagnosis and scope.** The authentic SENTRY failure below establishes that its final 16.996-second read began at age 3590 and crossed the validated expiry; all 45 absent requests began after their own expiry. Historical evidence and native markers remain unchanged. This correction uses controlled offline clocks/HTTP/SQL only, launches no application or infrastructure, and earns no live retention credit. Preparation baseline is `22ff0afe00ccb337b0d76074c976f1a49ed61913`. Private check logs are under `/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-preexpiry-window-20261025-1gcua2ah` (called `WINDOW` below); coordinator preparation/approval remains separate.

**Changed behavior.** The selected `_PlatformRetentionConsumer` now targets A observations at 3300 and 3450 seconds, with truthful `pre-expiry-3300` / `pre-expiry-3450` evidence phases. At the existing bounds, the first 120-second read plus 10-second SQL observation ends by 3430, and the second 120-second read by 3570. These targets do not guarantee admission after scheduler delays. The consumer binds the earliest previously validated record expiry to the original emission UTC/monotonic pair, clamps the shared read deadline and each request to the remaining usable interval, and rechecks before batches, attempts and backoff. Dispatch at/after either expiry clock and a response at/after either expiry clock refuse `functional-selected-preexpiry-window`, including a still-present record. Retries remain three attempts with 2/5-second backoff, sixteen workers and the original 120-second aggregate ceiling.

A response's recorded UTC/monotonic time determines its eligibility before the following guard. A genuine early absent/non-object pre-expiry response preserves `functional-selected-record-missing` after the existing aggregate/input/gate checks, without scheduling more work that could mask that failure as a later expiry. A guard that exceeds 120 seconds still fails the unchanged aggregate budget. The pre-expiry read-only SQL timeout is capped by both expiry clocks and 10 seconds; late dispatch/completion refuses. Its one fixed, exclusive 0600 `native-tests/pre-expiry-physical-cohort-a.json` packet retains only segment, fixed status and bounded start/completion/expiry timestamps. Existing read packets retain response versus completion times, ordinals and expected expiry without keys, values, endpoints, credentials or exception text.

Changed implementation paths are `tools/access_telemetry_functional_retention.py`, `tests/tooling/access_telemetry_functional_retention/test_selected_active_sequence.py`, `tests/tooling/access_telemetry_functional_retention/test_selected_read_retry.py`, and this current report. The retired VM `_run_sequence` is unchanged. The deterministic existing selected fixture advances 2 seconds per sixteen-key batch (16 seconds total), or 14.875 seconds per batch (119 seconds total), with 10 seconds for SQL. Both corrected observations and the physical TTL proof must precede the earliest validated expiry. Initial A absence, early pre-expiry absence with a 70-second slow guard crossing expiry, B loss/mutation, physical TTL/hash drift, source/gate drift, strong consistency, authenticated requests and exclusive/private evidence remain checked. Cohort accounting 125/125, separation 1800, retention 3600, purge 900, signed-clock/security, due-index/reminder and canonical B assertions are unchanged.

**Executed checks.** Commands below run from `/home/administrator/projects/hexalith/memories/tests/tooling/access_telemetry_functional_retention`, except the whitespace command from Memories root. Test clocks provide offline evidence only.

```bash
python3 -B -m unittest -v \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_complete_sixteen_second_observations_and_sql_finish_before_expiry \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_complete_near_budget_observations_and_ten_second_sql_finish_before_expiry
```

Before changing production, that exact command executed 2 checks with 2 assertion failures in 4.748 seconds, exit 1 (`WINDOW/observation-window-red.log`). The 16-second check detected the absent corrected phase, and the 119-second check reached the old schedule's genuine `functional-selected-record-missing` failure. The first test setup attempt used the wrong cwd for its edit script; its subsequent command contained four not-yet-added test names and a missing-artifact KeyError. These are retained in `observation-window-initial-test-setup.log`, without counting setup/loader errors as red behavior. An early boundary run refused the newly named unsupported phase; `observation-window-boundary-red-initial.log` is not proof that an old valid phase exercised expiry guards. Only the two named schedule checks establish meaningful pre-change red.

The first five-check selected verification was 4/5 (`observation-window-sequence-green-initial.log`): the late-response fixture advanced time before dispatch, so the correct new dispatch guard refused before any late response existed. Moving that fixture latency inside HTTP produces the intended still-present late response; no acceptance guard was weakened. Initial focused boundary checks passed 4/4 (`observation-window-boundary-green-initial.log`). The remaining new/renamed boundary cases did not have a meaningful pre-change red and are disclosed as such.

The affected command `python3 -B -m unittest -v test_selected_active_sequence test_selected_read_retry` passed the then-current **31 selected + 25 retry = 56/56 in 440.578 seconds**, exit 0 (`observation-window-modules-green-initial.log`). It imported before the final request-deadline refinement that also uses the smaller UTC remaining interval, and before three additional HTTP/SQL/scope cases. The exact final retry command `python3 -B -m unittest -v test_selected_read_retry` passes **28/28 in 0.220 seconds**, exit 0 (`observation-window-retry-final.log`). On settled source, the following named selected checks pass **5/5 in 10.558 seconds**, exit 0 (`observation-window-sequence-final.log`):

```bash
python3 -B -m unittest -v \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_complete_sixteen_second_observations_and_sql_finish_before_expiry \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_complete_near_budget_observations_and_ten_second_sql_finish_before_expiry \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_late_preexpiry_response_is_refused_even_when_the_record_is_still_present \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_late_scheduling_is_refused_before_dispatch_and_retains_unobserved_ordinals \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_response_time_precedes_slow_guard_completion_without_implying_late_absence
```

`git diff --check` from Memories root passes, exit 0, with empty output (`observation-window-diff-check.log`). No single all-green 59-test execution against the final bytes is claimed; the affected refinement is covered by the final 28-test retry module and five selected cases. Inventory is selected sequence 28→31 and retry 21→28: 49→59 methods (+10 net; one obsolete 3590 boundary test replaced by four corrected schedule/late-work checks, plus seven retry/SQL/scope checks). Earlier reports and their actual counts are unchanged.

Named fail-closed cases include `SelectedReadRetryTests.test_preexpiry_response_boundary_checks_both_utc_and_monotonic`, `test_preexpiry_late_dispatch_or_scheduler_delay_refuses_before_any_request`, `test_preexpiry_retries_share_the_clamped_expiry_and_backoff_cannot_cross_it`, `test_preexpiry_early_response_survives_slow_guard_but_shared_budget_still_applies`, `test_preexpiry_sql_is_clamped_refuses_late_completion_and_retains_private_timing`, `test_preexpiry_scope_refuses_other_segments_and_buckets_before_requests`, and the existing `test_diagnostic_phase_and_poll_scope_refuse_unbounded_or_untrusted_names`, `test_authentication_security_and_server_http_failures_are_never_retried`, `test_withdrawal_after_failure_or_during_backoff_admits_no_retry_or_later_key`. Selected native output checks continue to require a failed TRX and no passing observations on late work. Story 20.2 tenant authority and Story 24.3 inspection/admin authority boundaries are unchanged; timing diagnostics confer no authority, and no application authorization code changed.

**Handoff limits.** This section records the offline implementation handoff. The subsequent coordinator review, affected-input preparation, actual new authorization and successful native invocation/disposition are recorded above; offline tests alone grant no live credit. Unbounded scheduling can still make a run refuse, so the complete plan must retain actual admission/deadlines. Story 27.4 stays in progress; functional retention, full qualification, Production and A41 remain open.

### Sentry-admission execution — 2026-10-25; final pre-expiry read crossed expiry, cleanup verified

**Result: local retention is not proved.** The owner-authorized single launch **`76c939886c135f6fbfa78e80b98cb7ff`** ran about **61 minutes 28 seconds**, passed startup and both cohorts, then failed **`functional-selected-record-missing`** in the final **`pre-expiry-3590`** A read. Authentic native result is **failed / 4 / HXT006**. Native TRX records **1 executed / 0 passed / 1 failed**; the wrapper explicitly reports test counts unreported. No passing `functional-observations.json` or final observation hash exists. No retention, full qualification, Production or A41 pass is claimed.

**Execution and admission.** From `SENTRY`, **`python3 -B execute-approved.py`** dispatched the exact supported command in `launch-command.txt` at **00:14:18.924174Z** after one actual capacity/input/isolation admission. All **330 selected / 36 model / 58 operational / 489 runtime** input hashes and seven immutable images matched. Admission observed **14 CPUs**, **97,021,218,816 Docker memory bytes** and **91,305,676,800 Linux available bytes**, with both 20-GiB disk floors passing. One launch, zero retries/restarts, unchanged source/Debug/tool/images/security and assertions. Its original execution deadline was **04:14:18.924174Z**; startup600 was included and stopping900 remained separate. The owner’s actual **“approve next lunch”** scope authorization is bound in `approval.json`; it is now consumed.

Startup admission persisted at **00:15:38.466081Z**, before cohort A response observation at **00:15:40.055Z**. All 15 resources reached readiness, and controller graph/ownership checks remained healthy through test completion. Cohort B response observation was **00:45:40.010Z**. These response timestamps alone are not claimed as exact emission separation proof.

| Actual retained observation | Result |
| :--- | :--- |
| A accounting | 125 attempted/enqueued/acknowledged/persisted/transaction acknowledgements; zero rejected/dropped/conflicted |
| B accounting | Same complete 125/125 accounting; zero rejected/dropped/conflicted |
| Initial A and B strong reads | Each 125 objects; zero missing/non-object values |
| A `pre-expiry-3540` read | **01:14:39.003–01:14:55.945Z**; 125 objects, zero missing/non-object values |
| Final A `pre-expiry-3590` read | **01:15:29.003–01:15:45.999Z**, **16.996 s**; 80 objects / **45 absent**, zero non-object values |
| Validated expected A expiry range | **01:15:39.002–01:15:39.126Z** |
| All absent ordinals | **81–125**; every one’s request started **788–5125 ms after its own expected expiry**, response observed **2223–6306 ms after expiry**, one attempt each |

**Attribution established for this run.** The final scheduled “pre-expiry” batch began about ten seconds before expiry but took about seventeen seconds. All 45 absent reads started after their records’ previously validated expected expiry. The test therefore asserted continued presence after expiry for those records. The earlier initial and 3540-second reads were complete. This establishes the **test timing defect** for this invocation; it does not establish early durable loss, and it cannot retroactively attribute older runs without these diagnostics. Per-key consumer timestamps are intervals around requests, not atomic backend timestamps. `SENTRY/read-attribution-summary.json` retains this classification, derived only from the authentic per-key packet.

No post-expiry purge/due-index/reminder/B-preservation assertion was reached, and none receives credit. Correcting the last pre-expiry observation window is the concrete next Memories issue; the active contract currently fixes that schedule, read concurrency and budget, so this invocation changed none of them. No further native run is prepared or authorized, and there is no automatic restart or budget reset.

**Actual supported stop and disposition.** Native test completed **01:15:46.0435019Z**; controller observed exit 4 at **01:15:47.070762Z**. It preserved native tests/runtime receipts before supported exact-run **`down --run-id 76c939886c135f6fbfa78e80b98cb7ff`**, which returned **0 / HXI001** at **01:15:49.738977Z**. Then **`sudo -n python3 -B dispose-owned.py`**, cwd `SENTRY`, verified two empty-engine checks and stopped only recorded PID/start identities and verified descendants. At **01:16:13.219361Z**, disposal was **verified**: no owned processes or namespace members, private socket, native workspace or run state remained. Stopping consumed **26.149464 seconds**, within the original cumulative **900 s** allowance. The graph-loss receipt at **01:15:47.803267Z** follows the native terminal and records teardown, not the initiating test failure. Earlier/shared resources were untouched.

The authentic native `functional.json` remains its **failed / retained / unverified:true** terminal snapshot. It was not rewritten; separate supported-down and private-disposition receipts establish actual cleanup. Exact native artifact bytes are copied into `SENTRY/result-artifacts/functional.json`, `functional.python3.trx` and `native.stdout.json`. `run-result.json`, `before-disposal/native-tests/`, `read-attribution-summary.json`, `native-run/` diagnostic/source/configuration receipts, `down-terminal.json` and `private-engine-disposition.json` retain outcomes and limits. The one ordinary review and offline correction evidence below remain valid. **Launch allowance consumed; private engine stopped; Story 27.4 remains in progress, and full qualification/Production/A41 remain open.**

### Sentry admission — 2026-10-25; historical preparation, executed once

**Preparation state before the executed launch.** Private **`SENTRY=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-sentry-admission-20261025-z0opew38`**, preparation baseline **`947f2f118d9b0266d4028e455e4d8b05ba2e47c1`**, contains the new physical candidate, bounded diagnostics, existing preparation/control scripts, exact `run-plan.json` and full-prefix `launch-command.txt`. The owner's **“approve next lunch”** supplies one next-launch scope authorization; the coordinator still performs the single ordinary review and binds the final files. This does not claim owner review of later hashes. The consumed `READ` candidate/evidence remains untouched.

**Review and authorization binding.** One context-free ordinary review found no substantive findings and independently verified all 330/36/58/489 bound hashes, source proof, capture ordering, unchanged limits and exact command/cleanup. `SENTRY/root-review.json` records the review. At **2026-10-25T00:14:17.947586+00:00**, coordinator binding in `approval.json` associates the owner's actual **“approve next lunch”** statement with final plan SHA-256 **`c2ecad3bf9a4f58823c025b94e3f3efa9b296e019c689c2e92b2e93642e1406e`** and command SHA-256 **`e89d3e1a9d0468c4139788e76b56fb96c20b58defd5120172daa2d4c5e51e84d`**. Review administration fields were corrected without operational changes; this is coordinator binding, not owner review of later hashes. The one supported native dispatch is next; actual result/disposition remain pending.

**Demonstrated operator defect; historical cause unproved.** Matched installed 4.35.2 `CompositionEngine` composes with `--validate-platform`, then starts the actual AppHost. `AppHostRunner` invokes composition before its validation early return. Both compositions allocate a secondary Sentry port and sequentially write the same four configuration files. An offline probe invokes the actual candidate `PlatformDaprBindings.Render`/`MergeRunDiscovery` at two controlled localhost ports. Replaying that sequential rewrite makes the previous observer refuse a mixed pre-runtime generation. The producer fixture uses controlled empty invocation rules, with no Compose/startup, backend or secret creation. It establishes this observation defect, not which unrecovered values caused `READ`'s refusal (`diagnosis.json`, `generation-probe-receipt.json`).

Only the private observer changes behavior: it waits for the **existing PID/freshness-bound native-host `BeforeStart` capture** before reading discovery configuration. Actual composition has completed its four writes at that point. No partial or differing set is admitted. Exact localhost-port agreement, SQLite registry/zero-cleanup/default liveness, ACL/mTLS/configuration equality, the 600-second startup bound and all retention assertions remain. A subsequent refusal retains an exclusive **0600**, bounded four-file projection: file SHA-256/size and fixed address classifications, with a numeric port only for an exact valid localhost address. It retains no raw YAML, arbitrary address text, ACL values, credentials or exception details. The diagnostic reread is explicitly **non-atomic** and attributes no cause.

Commands below run from `SENTRY` unless stated otherwise; exact argv and results are in `preparation-validation.json`, `binding-commands.json` and named receipts:

| Verification | Executed result |
| :--- | :--- |
| `dotnet build generation-probe/Probe.csproj -c Debug --nologo -m:1`; probe command in `generation-probe-receipt.json` | Build succeeds with zero warnings/errors; actual production rendering produces four files at each of two controlled ports. |
| `python3 -B retain-red.py` against the unchanged `READ` observer | **2 executed / 2 assertion failures / 0 errors**, `observer-red-assertions.log`; no compile failure counted as red. Initial harness retained one refusal error plus one assertion failure in `observer-red.log`. |
| `python3 -B -m unittest -v test_observe_startup test_native_host_admission test_additional_sources test_discovery_admission` | **54/54**, 3.587 s, exit 0, `observer-final-tests.log`. Discovery inventory **12 → 18**, +6; four additional cases first ran green, named in `preparation-validation.json`. |
| `python3 -B -m unittest -v test_operator_bounds` | **23/23**, exit 0, `operator-controls.log`; unchanged elapsed/absolute stopping and single-dispatch controls. |
| `python3 -B verify-routing.py`; `python3 -B context-probes.py` | Routing **16/16**; actual installed prerequisite child returns **0 / Aspire 13.6.1**, and actual selected-manifest resolver matches the fresh offline map. No native invocation. |
| Native offline model command in `native-model-build-command.json` | Exit **0**; **36** outputs, **1089/1089** source documents; selected map **`b1d673e04dd20475f90a7704ee6d1f2f943cf326d61a176980f62262422e27b5`**, 17 entries/no findings. |
| Existing Server source inspector; copied PDB inspector commands in `binding-commands.json`; `python3 -B verify-copied-source-negatives.py` | Server **14/14** retained sources. Unchanged Contracts **35** and lifecycle **50** source documents match genuine prior root-relative signatures; source-mutation and zero-document refusals **2/2**. |
| `python3 -B readback.py` | Matching **330 selected / 36 model / 58 operational / 489 runtime** inputs, seven images, seven owned PID/start identities, empty engine and absent fresh native state/evidence; `root-preparation-readback.json`. |

**Named fail-closed evidence.** Affected surfaces are native-host identity/freshness, discovery registry/security admission and copied-source attribution. The 54/54 command above executed passing `SchedulingTests.test_foreign_accounting_is_refused`, `test_foreign_plan_or_native_state_is_refused`; `DiscoveryAdmissionTests.test_foreign_registry_refuses`, `test_acl_or_mtls_change_refuses`, `test_mixed_generation_after_native_host_refuses`; `NativeHostAdmissionTests.test_wrong_pid_refuses`; and `AdditionalSources.test_stale_source_or_zero_document_pdb_refuses`. These retain the Story 20.2 denial-before-dependency and Story 24.3 verifier/tenant-marker boundaries without claiming a new tenant-routing or full qualification pass. Exact case IDs/command/result are also in `SENTRY/run-result.json`.

**Preparation corrections and limits.** The inherited application proof rerooted old `CAPABILITY` PDBs to the fresh path and returned **zero** source documents; those initial JSON results are retained. A copy of the existing read-only PDB inspector now maps original document roots to the new physical copy and refuses zero coverage. Both nonzero signatures equal the genuine prior proofs; unchanged application DLLs are reused. Operational inputs increase **54 → 58** solely for that copied proof-helper source/DLL/deps/runtime files; all **330 selected inputs stay byte-identical**. Initial generator dependency-loader errors, unsupported file-app `-m:1`, a negative-harness argument error and inherited runtime/evidence basename corrections are disclosed in `preparation-validation.json`; none launched a native test. Existing historical failures and broader build limitations remain below. No new application build or full-suite pass is claimed for this observer-only change.

**Concrete plan.** Cwd **`SENTRY/compatible-platform`**, owner **`SENTRY/compatible-platform/references/Hexalith.Memories`**, fresh state **`SENTRY/native-run`**, fresh owner-relative evidence **`artifacts/retention/repair-spec14-sentry-admission-20261025-z0opew38/functional.json`**. The supported suffix is:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-sentry-admission-20261025-z0opew38/functional.json \
  --output json
```

Independent Docker **29.8.2**, engine **`9671e626-c44d-43eb-96e3-8bd53f644149`**, socket **`unix:///run/hx-retention-z0opew38/docker.sock`**, keeper **8837**, daemon **8858**, owns fresh private data/exec/containerd/network/mount state and slirp with host loopback disabled. Seven unchanged immutable image identities match; **zero containers/native launches**. Reused namespace inode numbers do not substitute for the fresh PID/start, socket and engine identities. Every lifecycle/observer/capture/cleanup route uses the same verified full prefix. Genuine Platform **`7021c41bea55bd6c690ba96082cf3dcfc95c9fca`**, Builds **`007bb9bc2806a0ead02d4831555bf2134a38d256`**, tool **4.35.2**, EventStore **3.127.0**, Dapr **1.18.1**, PostgreSQL **18.6**, source/Debug, real 125/125 cohorts and 1800/3600/900-second assertions remain unchanged.

Plan bounds remain **one launch / zero retries/restarts**, **14400 s execution including 600 s startup / separate cumulative 900 s stopping**. Latest dispatch **2026-10-25T10:27:22Z**, absolute execution end **14:27:22Z**, outer disposal end **14:42:22Z**. Actual capacity/isolation admission runs once at launch: **8 CPUs / 16 GiB Docker and Linux available memory / both 20 GiB disk floors**, retaining PostgreSQL **8 CPU / 16 GiB** caps. Supported same-prefix exact-run `down` uses only an actual new run identity; missing identity is explicitly inapplicable. Existing bounded disposal then verifies the empty engine and exact owned process/socket/namespace/workspace/state absence. Preparation-only withdrawal stops this exact owned engine under the same 900-second allowance; earlier/shared resources remain excluded. Coordinator review/approval binding and the single launch/disposition subsequently completed as recorded above. Parent Story 27.4 remains in progress; offline preparation itself earns no retention, full qualification, Production or A41 credit.

### Read-attribution execution — 2026-10-24; startup admission refused, cleanup verified

**Result: no retention test executed.** The owner-authorized single launch **`2807e9becd49e1353453bfd7b21c24f2`** passed once-before-launch capacity/input/isolation admission and dispatched at **23:53:13.988984Z**. The unchanged native supported command used the fresh `READ` plan below. One launch; zero retries/restarts. All 330 selected / 36 model / 54 operational / 489 runtime inputs and seven images matched before dispatch. No source/version/security/assertion change was made during the invocation.

At **23:53:28.679105Z**, the startup observer refused **`retention-discovery-sentry-mismatch`** before startup admission/cohort A. The controller invoked supported exact-run `down`. Native result was **`withdrawn` / 130 / `HXC130`**; its `HXR021` unready-resource diagnostics describe the canceled startup, not an independently established resource fault. No cohort accounting, state-read diagnostic, native test TRX or passing `functional-observations.json` was produced. The wrapper's test counts are explicitly unreported; it is not a one-test retention failure or a retention pass.

**Attribution limit.** `observe-startup.py::discovery_configuration` emits this reason for either an invalid/non-loopback/zero/out-of-range Sentry address, or differing valid Sentry addresses across the four generated files. Actual refused addresses/configuration bytes were not retained: the input collector kept the earlier model phase, and normal `down` removed the transient workspace. The retained reason therefore establishes an observer admission refusal only. It does not establish wrong service configuration, a generation race, Sentry process failure, or the previous missing-record cause. No security check or admission behavior was weakened on an inference. A subsequent investigation needs bounded address/hash evidence retained at the refusal and an offline reproduction before choosing a correction.

**Actual disposal.** Supported `down --run-id 2807e9becd49e1353453bfd7b21c24f2` returned **0 / `HXI001`** at **23:53:31.143045Z**. Exact private-engine cleanup completed **23:53:44.486073Z**, **15.808138 seconds** after stopping began, within the cumulative 900-second ceiling. Two private empty-engine checks passed; recorded processes/descendants and namespace members are absent, along with the private socket, native workspace and run state. No replaced identity was signaled. Earlier resources and shared engines were untouched.

The native `functional.json` remains its authentic **withdrawn / retained / unverified:true** snapshot. Separate actual `down` and private disposition receipts establish removal; the native marker was not rewritten. `READ/run-result.json`, `native-run/startup-admission-failure.json`, `result-artifacts/functional.json`, `native.stdout.json`, `before-disposal/`, `down-terminal.json` and `private-engine-disposition.json` retain authentic evidence and limits. Preparation code/plan/test receipts and all earlier evidence remain intact.

**The one authorized launch is consumed; the private engine is stopped; no further launch is authorized.** Local retention and parent Story 27.4 remain in progress. Full qualification, Production and A41 remain separate/open. The offline diagnostic implementation and its passing case evidence remain valid, but its new read diagnostics were not exercised live.

### Read-attribution launch plan — 2026-10-24; historical preparation, executed once

The owner said **“authorize next lunch”**, interpreted in context as authorization for **one next launch**, after this preparation and review. This is scope authorization received before final file binding, not a claim that the owner reviewed later hashes. No further confirmation is required. Earlier consumed permissions are not reused.

Private **`READ=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-read-attribution-20261024-uo6hlya3`** holds the concrete `run-plan.json`, exact full-prefix `launch-command.txt`, `execute-approved.py` and `dispose-owned.py`. The source directory is `READ/compatible-platform`; the declared owner is its direct `references/Hexalith.Memories`. Fresh owner-relative evidence is **`artifacts/retention/repair-spec14-read-attribution-20261024-uo6hlya3/functional.json`**; fresh native state is `READ/native-run`. The supported suffix is:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-read-attribution-20261024-uo6hlya3/functional.json \
  --output json
```

The candidate is a new physical copy of `CAPABILITY` with only the read-attribution consumer and two tests adopted. All **330** selected inputs were checked; **329** remain byte-identical and only the intended Python consumer changes. The supported resolver reports 17 direct entries, no identity findings and mapping **`af281d097a17e6c530d06e1d778a5cd40f9a01b79053c6a3841040cc0eaaf541`**. Model build passes; 36 outputs and their source documents are rebound to the fresh source paths. Unchanged application DLL/PDB proof is reused and its source documents verified against this new copy; the existing startup observer must still bind actual loaded binaries before cohort A. No new application behavior or admission bypass is adopted.

Tool **4.35.2**, Platform **`7021c41bea55bd6c690ba96082cf3dcfc95c9fca`**, Builds **`007bb9bc2806a0ead02d4831555bf2134a38d256`**, EventStore **3.127.0**, Dapr **1.18.1**, PostgreSQL **18.6**, all seven immutable images, source/Debug, scoped SQLite discovery workaround and ACL/mTLS are unchanged. Installed prerequisite and exact private-context resolver checks pass, including genuine Aspire 13.6.1. Current private engine **`9f7f3360-bc72-4223-b8c3-9e74149dc0a6`**, endpoint **`unix:///run/hx-retention-uo6hlya3/docker.sock`**, has seven matching images and **zero containers**. PID/start/namespace ownership is retained for seven preparation processes. This is an independent engine with host loopback disabled; previous engines/resources are excluded.

Keep seven containers / 15 resources, no VM; admission checks at least eight CPUs, 16 GiB Docker and Linux available memory, and 20 GiB staging/backing free space once immediately before launch. PostgreSQL keeps eight-CPU/16-GiB limits; no whole-graph reservation is claimed. **One launch / zero retries or restarts**, at most **14400 s including 600 s startup**, plus separate **900 s cumulative stopping**. Latest dispatch **2026-10-25T10:27:22Z**, absolute execution end **14:27:22Z**, outer end **14:42:22Z**. Preserve real 125/125 cohorts, 1800/3600/900-second observation and every original assertion.

On native terminal, failure, withdrawal or graph loss: preserve authentic native/TRX/read/accounting/sidecar receipts, invoke supported `down --run-id <actual-new-run-id>` in the same context, then verify the private engine empty and stop only recorded PID/start identities and verified descendants. Confirm socket, processes, namespace, exact workspace and run state absent within the original stopping budget. No fabricated run identity, broad prune, process-name kill or earlier-resource disposal.

Private routing checks pass **16/16**, operator bounds **23/23**. Observer checks were **47/48** with one missing copied fixture; copying the unchanged retained-container-shapes fixture and rerunning its named case passed **1/1**, covering all 48 without an all-green single-run claim. Initial readback refused an inherited historical evidence-relative basename; it was corrected to the fresh path above, keeping prior evidence and the refusal receipt. Final readback checks **330 selected / 36 model / 54 operational / 489 runtime inputs**, images, ownership, helpers, source proof, fresh paths and open window successfully. Neither preparation error caused a live launch. Exact receipts are `preparation-validation.json`, `preparation-readback-refused-historical-evidence-path.json` and `root-preparation-readback.json`.

One context-free ordinary review completed with **no substantive findings** and independently rechecked all bound file hashes. `READ/root-review.json` records it. The owner scope authorization is now bound in `approval.json` to final plan SHA-256 **`abd7262d50d1a14798395a44be810964157390cba22abcd844dd4a1961edc8fa`** and exact command SHA-256 **`a6e337116e3dbf7be4b2ae69e80de8aff0d9e5d8c7f0dd7751f3b504d832abb4`**; this is coordinator file binding, not a claim that the owner reviewed later hashes. The review changed only plan administration fields. Parent Story 27.4 remains in progress; full qualification, Production and A41 remain open.

### Missing-record attribution — 2026-10-24; offline diagnostics

**Scope.** Preparation baseline `15be1d88b2f7113acea3ad01f092a1e06c0cb7f5`. Private **`READ=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-read-attribution-20261024-uo6hlya3`**, with commands, logs and controlled observations in `owning-checks/`. The earlier live result below is unchanged. This implementation used controlled clocks, HTTP/SQL and child-free doubles only; it launched no application or infrastructure.

**Changed behavior and files.** `tools/access_telemetry_functional_retention.py` retains a read packet before cohort validation can throw. Fixed phases identify initial A/B, A at 3540 and 3590 seconds, post-expiry A records/buckets and B preservation. Each packet records aggregate start/completion UTC and monotonic time, fixed key ordinals, per-key start/response-observation/completion times, attempt count, and absent/non-object counts. Response-observation time is sampled immediately after the HTTP request returns, before the subsequent input/gate guard; guard latency cannot be mistaken for response latency. Per-key statuses are only `unobserved`, `failed`, `object`, `absent` or `non-object`; aggregate status is `completed` or `failed`. A completed read packet describes read completion and can precede a failed retention assertion.

Known per-record expiry is copied only from an earlier fully validated cohort; it is null for initial observations and bucket keys. No record values, keys, record IDs, endpoints, credentials, exception text or raw responses enter these packets. The existing held-descriptor private writer requires exclusive creation, 0600 files and a private owner directory. Paths are fixed phase/segment/kind names with bounded numeric observation slots. At most 32 slots per repeating phase and 125 ordinals per file bound the selected sequence to 100 files; each uses the existing 1 MiB output limit. The unchanged 900-second grace and 30-second poll interval already permit at most 31 polls. Existing transport-failure evidence remains separate and unchanged.

The two changed test files are `tests/tooling/access_telemetry_functional_retention/test_selected_active_sequence.py` and `test_selected_read_retry.py`. Nine added selected-sequence cases cover every phase, late A boundary crossing, initial and first pre-expiry A absence, B preservation loss, non-object/secret exclusion, exclusive evidence refusal, response versus slow-guard timing, and repeated polls. Two added retry cases cover failed/unobserved ordinals and refusal of untrusted or out-of-bound diagnostic names/slots before any request. The existing sixteen-worker concurrency, strong consistency, retry/backoff and 120-second shared read budget are unchanged, as are 125/125 accounting, real 1800/3600/900-second timing, scheduled reads, TTL/due-index/reminder/B-preservation and signed-clock assertions. This report is the fourth changed file owned by the implementation; the coordinator owns spec/preparation/live updates.

**Controlled reproduction, retained evidence.** `owning-checks/offline-read-late-a.json` retains the real selected consumer's final pre-expiry A read starting at age 3590 seconds and ending at 3606 seconds. A deterministic batch executor advances both controlled clocks two seconds per unchanged batch: 45 responses are absent, ordinals 81–125, with response times beyond their previously validated expiry. Initial A/B and the 3540-second A read succeed first. `offline-read-early-a.json` instead retains one missing ordinal (7) during initial A; `offline-read-b-loss.json` retains 125 absent B responses during preservation while their validated expiry is still future. All three fail the original `functional-selected-record-missing` assertion and write no passing observations. `offline-read-early-a-slow-guard.json` receives an absent A response before its validated expiry, then advances 70 seconds in the following guard: the response time stays early while completion is late. These establish distinguishable offline shapes, not the cause of historical run `55eb672…`.

The exact artifact-retention replay is `python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-read-attribution-20261024-uo6hlya3/owning-checks/retain-offline-read-observations.py`, cwd `/home/administrator/projects/hexalith/memories`; all four controlled failures are retained with exclusive 0600 files. The independent pre-change boundary replay remains in `offline-boundary-reproduction.py`, `.stdout.jsonl` and `.receipt.json` under the same private checks directory.

**Verification.** Python module commands run from `/home/administrator/projects/hexalith/memories/tests/tooling/access_telemetry_functional_retention`. The pre-change command below executes the unchanged production consumer with the new test harness. The initial version produced three missing-artifact `KeyError` errors (`python-read-attribution-red.log`); explicit artifact-presence assertions then produced **3 executed / 3 assertion failures** (`python-read-attribution-red-assertions.log`), all for absent phase evidence after the original missing-record failure. No compile failure is counted as red evidence.

```bash
python3 -B -m unittest -v \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_late_preexpiry_a_read_crossing_expiry_retains_phase_missing_ordinals_and_timing \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_initial_a_absence_is_distinct_from_preexpiry_boundary_and_b_loss \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_b_preservation_loss_retains_segment_and_all_missing_ordinals
```

Only those three added tests had an executed pre-change red; the other eight added cases first ran after diagnostics existed. Initial focused verification was **6/7** (`python-read-attribution-focused.log`): the phase-shape test incorrectly expected one purge poll, while the fixture's physical TTL extends logical expiry by one second and already needs two. The next focused command added response/guard and repeated-poll coverage; it exposed a second fixture-only assumption that expiry bucket count equals worker count. Buckets use the existing 64-shard record hash. The corrected named phase-shape check passes **1/1 in 35.596 s** (`python-read-attribution-phase-shape-corrected.log`). Both production assertions and fixture TTL/sharding remain unchanged.

| Check | Exact command | Result / retained log |
| :--- | :--- | :--- |
| Focused read phases | Nine named cases in the command below | **8/9**, 139.508 s, exit 1; sole failure is the stale fixture expectation `16 != 60` (`python-read-attribution-focused-final.log`) |
| Corrected phase-shape fixture | `python3 -B -m unittest -v test_selected_active_sequence.SelectedActiveSequenceTests.test_read_diagnostics_cover_all_selected_phases_and_real_key_ordinals` | **1/1**, 35.596 s, exit 0 (`python-read-attribution-phase-shape-corrected.log`) |
| Existing retry lane plus two new cases | `python3 -B -m unittest -v test_selected_read_retry` | **21/21**, 0.150 s, exit 0 (`python-read-retry-final.log`) |
| All affected modules | `python3 -B -m unittest -v test_functional_retention test_selected_active_sequence test_selected_read_retry` | **103 executed / 102 passed / 1 failed**, 473.508 s, exit 1 (`python-read-attribution-affected.log`). The process imported the earlier shape fixture before its correction; the only failure is the same `16 != 60` assertion. No errors or skips. |
| Final whitespace | `git diff --check`, cwd `/home/administrator/projects/hexalith/memories` | Exit 0 |

The corrected named run covers the one affected-lane failure against unchanged production bytes. Passing evidence therefore covers all 103 current cases across those runs; a single 103/103 run is not claimed. Inventory is **92 → 103**, **+11**: functional module 54 unchanged, selected active sequence 19 → 28, retry module 19 → 21. `READ/implementation-files.json` binds the three production/test inputs; consumer SHA-256 is `5f780b79a7618b61808707097528efe3047ff41cfd973eeeb2933533693a3976`. No unaffected tests were repeated after the corrected named pass. Exact argv for every red/focused/affected command, including the initial seven-case prefix, is retained in `owning-checks/verification-commands.json`.

```bash
python3 -B -m unittest -v \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_read_diagnostics_cover_all_selected_phases_and_real_key_ordinals \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_late_preexpiry_a_read_crossing_expiry_retains_phase_missing_ordinals_and_timing \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_initial_a_absence_is_distinct_from_preexpiry_boundary_and_b_loss \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_first_preexpiry_a_absence_retains_read_well_before_known_expiry \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_b_preservation_loss_retains_segment_and_all_missing_ordinals \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_non_object_response_is_bounded_without_retaining_its_secret_value \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_read_observation_cannot_replace_existing_evidence \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_response_time_precedes_slow_guard_completion_without_implying_late_absence \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_repeated_purge_and_preservation_observations_use_distinct_bounded_slots
```

**Limits and handoff.** Sampling occurs in the consumer process after request completion, not inside Dapr/PostgreSQL; per-key start/completion includes retries and guard work, with the distinct response-observation time excluding the following guard. Initial observations lack a previously validated expiry. Packets contain no inferred live cause. Broad .NET and unrelated deployment-render suites were not rerun for this Python-only change. This offline implementation earns no local live retention, full qualification, Production or A41 credit. Coordinator preparation, one ordinary review, the owner-authorized single next launch (or concrete admission blocker), authentic result and bounded supported cleanup remain separate tasks; Story 27.4 stays in progress.

### Capability-diagnostic execution — 2026-10-24 UTC; missing-record failure, cleanup verified

**Result: local retention is not proved.** The approved attempt **`55eb672cd9f30d006caa815eae60a867`** ran for about **61 minutes 19 seconds**, reached both cohorts, then failed **`functional-selected-record-missing`** near the expiry boundary. Native result: **failed / 4 / HXT006**. The native TRX records **1 executed / 0 passed / 1 failed**; the wrapper explicitly reports no test counts. No passing `functional-observations.json` exists.

**Authorization and execution.** The owner replied **“approve”**. `CAPABILITY=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-capability-diagnostic-20261024-uywszy91`. Its `approval.json` at **22:18:22.547286Z** binds the reviewed plan SHA-256 `7b62a542f5f6d54e48d0a6aa36d73c946a606298f9d806a9cd98ac1704bb446c` and exact command SHA-256 `5c5ce4c672a7c6bb06794033a200b956f1714bed84a858c06729cbff3ae53a79`. From `CAPABILITY`, **`python3 -B execute-approved.py`** checked the unchanged 330 selected / 36 model / 54 operational / 489 runtime inputs, seven images, empty isolated engine, owned processes and capacity once, then dispatched the supported command in `launch-command.txt` at **22:18:25.191694Z**. One launch, zero retries/restarts; no input, version, security or timing changes during execution.

Startup admission at **22:19:37.162859Z** passed the model, discovery configuration, native host, console, graph, Server and both additional source proofs. The persisted ordering receipt confirms admission before cohort A. The resource graph remained healthy through the test terminal.

| Retained observation | Result |
| :--- | :--- |
| Cohort A accounting, observed 22:19:38.786Z | 125 attempted, enqueued, acknowledged, persisted and transaction acknowledgements; zero rejected, dropped or conflicted |
| Cohort B accounting, observed 22:49:38.751Z | Same 125/125 accounting, zero rejected, dropped or conflicted |
| Capability diagnostic in both responses | `Passed` / preflight `None`; all ten recorded outcomes `Passed` / category `None`; diagnostic complete |
| Writer bootstrap in both responses | Two completed attempts, `Published`, no failure stage/kind; first publication 22:19:28.564Z; lifecycle Healthy/None |
| Native terminal | Test completed **23:19:44.6982478Z**; controller observed exit 4 at **23:19:45.625357Z** |

Accounting timestamps are response-observation times, not emission clocks. Their difference is not used as proof of exact cohort separation. A current local capability pass does not establish why the earlier `BOOTSTRAP` invocation refused capabilities and earns no full-profile or Production credit.

**Failure attribution and limit.** The sole `selected-record-missing` assertion is in `_PlatformRetentionConsumer.cohort()` in `tools/access_telemetry_functional_retention.py`: at least one returned state value was not an object. That helper serves cohort A's pre-expiry checks (scheduled at 3540 and 3590 seconds) and cohort B's preservation checks after A expires. The retained failure lacks the segment, phase, missing count and per-read timing. No final canonical-record, TTL-bound, purge or signed-clock observation packet was written, so none of those final assertions is credited.

A pre-expiry read crossing the expiry boundary is a **hypothesis**, not an established cause: `states()` permits a 120-second aggregate read, whereas the last planned pre-expiry A check starts only ten seconds before expiry. Early loss, a late A read, or loss of B cannot be distinguished from this receipt. No lifecycle/TTL/read-timing behavior was changed on that inference. The next Memories investigation should retain bounded phase/segment/read timing and reproduce the boundary offline before choosing a fix; no new run or plan is prepared here.

**Actual cleanup.** The controller invoked the existing supported `down --run-id 55eb672cd9f30d006caa815eae60a867` under the same prefix/cwd; it returned **0 / HXI001** at **23:19:48.494843Z**. Then **`sudo -n python3 -B dispose-owned.py`**, cwd `CAPABILITY`, verified the engine was empty and stopped only recorded PID/start identities and verified descendants. At **23:19:57.016910Z**, disposal was **verified**, with no remaining owned processes or namespace members and no private socket, native workspace or run state. Total stopping time: **11.391555 seconds**, within the original cumulative 900 seconds. The graph-loss receipt at 23:19:46.484248Z follows the native terminal and records teardown, not the initiating test failure.

The authentic native `functional.json` remains its pre-disposal **failed / retained / unverified:true** snapshot. It was not rewritten to claim a removed marker. The separate actual `down` success and exact private-engine disposition receipts establish cleanup. Earlier retained resources and shared engines were untouched.

Evidence: **`CAPABILITY/run-result.json`**, `result-artifacts/functional.json`, `result-artifacts/functional.python3.trx`, both `before-disposal/native-tests/*-accounting.json`, startup/graph receipts under `native-run/`, retained native sidecar logs, and `private-engine-disposition.json`. Original evidence remains intact. **Approval consumed; private engine stopped; no further launch authorized.** Story 27.4, local retention, full qualification, Production and A41 remain open.

### Capability-refusal diagnostics — 2026-10-24; offline implementation

**Scope and evidence.** Baseline `c7957a641067be2d28c8697caa2a90c671685f22`. New private sibling **`CAPABILITY=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-capability-diagnostic-20261024-uywszy91`** holds `owning-checks/` and `implementation-files.json`. Earlier candidates, results and consumed approvals are unchanged. No Aspire, Dapr, PostgreSQL or retention topology was launched. Test hosts and controlled doubles supply offline evidence only.

**Changed behavior.** The existing lifecycle capability runner now exposes immutable observations: `NotStarted`, `PreflightRefused`, `Running`, `Passed`, `Failed`, `Timeout` or `Canceled`, plus a bounded preflight reason and the ten fixed capability names. Per-capability outcomes distinguish `NotStarted`, `Running`, `Passed`, `ReturnedFalse`, `Exception` and `Canceled`; exception categories contain no messages, keys, URLs, paths, values or identities. The Dapr probe uses the same calls and cleanup/cancellation behavior, and its exception log now carries only the category. The runner still requires its six local capabilities, ten unique results, exact binding and original 30-second budget; its once-per-process decision and full-profile behavior are unchanged. Terminal snapshots ignore late asynchronous completions.

The authenticated `/v1/access-telemetry/validate` response adds optional `capabilityDiagnostic` only for a ready, exact epoch/profile request. Its existing two-argument constructor/deconstruction stays intact. Server bootstrap retains that observation inside the existing qualification `rejectionBreakdown.bootstrap`; absence in an older response clears the optional observation. Dictionaries and outcome records are owned immutable copies, including after deserialization and with-expressions. These observations do not participate in `AllowsWrites`, health/readiness, retries, the gate or retention assertions.

Python projects only the bounded vocabulary and ten known capability keys. Unknown/oversized/missing fields and duplicate diagnostic JSON objects are disclosed by `rejectionBreakdownComplete: false`, without persisting arbitrary text. The optional diagnostic never changes accounting acceptance. **125 rejections still fail `functional-accounting-loss`**. Existing 125/125, real 1800/3600/900-second, signed-clock, strong-read, TTL/due-index/reminder and newer-record assertions remain intact.

**File list.** `implementation-files.json` lists all **20** source/test files: **15** production/tool and **5** test files. New Contracts files are `AccessTelemetryCapabilityDiagnosticValues.cs`, `AccessTelemetryCapabilityOutcome.cs` and `AccessTelemetryCapabilityDiagnosticSnapshot.cs`; new lifecycle files are `Capability/AccessTelemetryCapabilityDiagnostics.cs` and `AccessTelemetryCapabilityFailureClassifier.cs`. Existing changes are the probe interface/result/runner/Dapr implementation, lifecycle `Program.cs`, validation response, three Server bootstrap files, `tools/access_telemetry_functional_retention.py`, the two existing Capability test files, `AccessTelemetryQualificationRejectionBreakdownTests.cs`, and the two named Python test modules. The existing active spec and this single current report are also updated; no new numbered specification is created.

**Exact verification.** Commands below run from `/home/administrator/projects/hexalith/memories`, except Python module commands, whose cwd is `/home/administrator/projects/hexalith/memories/tests/tooling/access_telemetry_functional_retention`. Logs are under `CAPABILITY/owning-checks/`.

| Check | Command | Observed result |
| :--- | :--- | :--- |
| Pre-change C# build | `dotnet build tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 --nologo -v:m` | 0 warnings/errors (`red-build.log`) |
| Executed C# red | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Debug/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll -noLogo -noColor -method '*CapabilityDiagnostic_IsObservableBeforeTheFirstAttempt'` | 1 executed, 1 assertion failure: snapshot absent (`red-diagnostic.log`); not a compile failure |
| Final Capability build | Same owning Debug build above | 0 warnings/errors (`build-tests.log`) |
| Affected Capability namespace | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.AccessTelemetry.Tests/bin/Debug/net10.0/Hexalith.Memories.AccessTelemetry.Tests.dll -noLogo -noColor -namespace Hexalith.Memories.AccessTelemetry.Tests.Capability` | **83/83**, 15.292 s, no errors/failures/skips (`capability-lane.log`); 22 added cases |
| Source-flag Server build | `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 --nologo -v:m` | Blocked: `EventStoreMemoriesCommandStore.cs(22,7): error CS0246: ... IEventStoreGatewayBoundCommandProofSource ... could not be found`; 0 warnings, 1 error (`server-build.log`) |
| Existing owning fallback | `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Debug -m:1 --nologo -v:m` | 0 warnings/errors (`server-build-fallback.log`, final `server-build-final.log`). Memories projects are built from source; external dependencies use the repository's package lane. No fully external-source build is claimed. |
| Existing Server telemetry namespaces | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -noLogo -noColor -namespace Hexalith.Memories.Server.Tests.Telemetry -namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle` | 297 executed: 296 pass, 1 new-fixture failure (`server-telemetry-lane.log`). Its helper asserted the old five-field bootstrap shape when the optional sixth field was present. |
| Final affected Server class after helper correction | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -noLogo -noColor -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryQualificationRejectionBreakdownTests` | **7/7**, no errors/failures/skips (`server-propagation-final.log`). The former single remote-denial fact now covers both old and new responses: net +1 case. The final wider namespace run below also passed. |
| Final Server telemetry namespaces | Same two-namespace command above, after the fixture correction | **297/297**, 6.278 s, no errors/failures/skips (`server-telemetry-final.log`). The initial 296/297 result remains recorded. |
| Executed Python red | `python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-capability-diagnostic-20261024-uywszy91/owning-checks/red-python.py` | 1 executed, 1 assertion failure: bounded projection drops the diagnostic (`python-red.log`). Script executes the unchanged baseline tool from `git show c7957a64:tools/access_telemetry_functional_retention.py` against the new test. |
| New Python diagnostics | The five named tests in the command below | **5/5**, no errors/failures (`python-focused.log`) |
| Affected Python modules | `python3 -B -m unittest -v test_functional_retention test_selected_active_sequence` | **73/73**, 321.307 s, no errors/failures (`python-affected-lanes.log`). Inventory is **68 → 73**, +5 tests. |

```bash
python3 -B -m unittest -v \
  test_functional_retention.FunctionalTests.test_capability_diagnostic_is_retained_without_changing_accounting \
  test_functional_retention.FunctionalTests.test_capability_diagnostic_rejects_unknown_missing_oversized_and_secret_fields \
  test_functional_retention.FunctionalTests.test_duplicate_capability_diagnostic_json_is_incomplete_without_altering_other_fields \
  test_functional_retention.FunctionalTests.test_capability_diagnostic_vocabulary_matches_contract_and_runner \
  test_selected_active_sequence.SelectedActiveSequenceTests.test_capability_refusal_and_secret_projection_are_retained_before_unchanged_loss_failure
```

The source-flag blocker is outside the changed diagnostic path: `git diff --exit-code c7957a641067be2d28c8697caa2a90c671685f22 -- src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreMemoriesCommandStore.cs src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj Directory.Build.targets` returned 0. The existing conditional interface and unconditional external package references are unchanged. No dependency or build gate was changed. The earlier broad `kubectl` blockers remain historical; those unrelated full suites were not rerun.

**Named negative evidence.** `CapabilityDiagnostic_PreflightRefusalRunsNoProbeAndContainsNoBinding` covers production/version/boundary/expiry/incorrect local binding; existing hosted-binding tests now also require the correct refusal disposition. `ValidationDiagnostic_IsDisclosedOnlyForTheReadyExactEpochAndProfile` exercises the actual authenticated endpoint for foreign epoch, foreign profile, unready options and an exact request. `CapabilityDiagnostic_ActualDaprProbeDistinguishesFalseExceptionAndCancellation` exercises actual Dapr-probe code with returned-false, transport exception, RPC denial and cancellation doubles. `CapabilityDiagnostic_TimeoutOrCancellationFreezesCompletedAndPendingOutcomes` proves late completion cannot replace terminal evidence or enable the retained decision. `CapabilityDiagnostic_MalformedResultsRemainFailClosedAndUnknownNamesNeverEscape` covers missing, duplicate, unknown, null and mismatched results. `CapabilityDiagnostic_OwnsItsDictionariesAcrossWithExpressionsAndJson` covers immutable ownership, secret exclusion and old-response constructor/JSON compatibility. Server and selected Python replays retain diagnostics before the unchanged 125-rejection failure.

Only the named C# snapshot-absence and Python retained-projection tests had executed pre-change reds. All other new cases first ran against the implementation; they are not claimed as failing-before evidence. An initial implementation compile attempt (`build-implementation.log`) had ten `CS0103` errors from referencing the application-only profile type in Contracts; fixed by fixed contract names and a parity test. That compile failure is not counted as a red test. The corrected build and all Capability cases passed. The Server fixture failure and its correction remain recorded above.

| Observed capability diagnostic | Interpretation / next action |
| :--- | :--- |
| Absent | Older response or no successful validation response; no local probe inference. |
| `NotStarted` | Runner has not begun; investigate lifecycle options/hosted-service binding. |
| `PreflightRefused` | No new probes were admitted by that call. Read the bounded reason. A changed binding can retain outcomes from the earlier attempt. |
| `Running` with pending names | At response sampling, those operations were in flight; no false-return claim. |
| `Failed` with `ReturnedFalse` | That probe completed and returned false. Check its existing behavioral condition; optional non-required failures can coexist with a local `Passed`. |
| `Failed` with `Exception`/`Canceled` | A call threw or canceled; use the bounded category. A failed runner with missing or pending names can also indicate malformed result registration. |
| `Timeout`/`Canceled` with some names still `Running` | Deadline/cancellation froze the snapshot before those callbacks completed. Late completion is deliberately ignored; no success or false return is inferred. |
| `Passed` | Only the existing local capability decision passed. This proves no retention, full qualification, Production admission or A41 closure. |
| Incomplete diagnostic | Use only retained fields. Missing/unknown/duplicate material earns no evidence credit and does not alter the accounting verdict. |

**Limits and next step.** Validation decision, capability snapshot and Server lifecycle/bootstrap snapshots are sampled separately, not atomically. No probe-attempt timestamps or exception details are added. Completed diagnostic outcomes identify a branch; they do not by themselves establish why the prior live run failed. One ordinary review found no substantive application defect. It identified two private run-control gaps (absolute deadline enforcement and cleanup before launch); their corrections and the concrete next plan are recorded below. No new launch is authorized. Retention, Story 27.4, full qualification, Production and A41 remain open.

### Capability-diagnostic preparation — 2026-10-24; historical plan, authorization consumed

**Concrete plan.** `CAPABILITY/run-plan.json` and `launch-command.txt` are the complete plan and exact command. The fresh private candidate is `CAPABILITY/compatible-platform`, copied physically from `BOOTSTRAP` and updated only with the 20 listed diagnostic source/test files. `pre-adoption-check.json` confirms the prior versions matched the owning baseline; `adopted-inputs.json` records the new bytes. Selected inputs are **330**: 315 unchanged, four updated and eleven newly selected relative to the prior 319. The supported selected mapping names `Hexalith.Memories`, 17 entries, hash `c51721bd61eff00bb26479a8e5de1e484ec507a371fe844caf2db529db0ed61b`.

The exact command's cwd is that Platform candidate. It includes the full recorded private namespace/user/environment prefix, guarded Docker client and this unchanged supported test suffix:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-capability-diagnostic-20261024-uywszy91/functional.json \
  --output json
```

Tool **4.35.2**, Platform **`7021c41bea55bd6c690ba96082cf3dcfc95c9fca`**, Builds **`007bb9bc2806a0ead02d4831555bf2134a38d256`**, EventStore **3.127.0**, Dapr **1.18.1**, PostgreSQL **18.6** and the seven immutable image identities are unchanged. The scoped SQLite discovery `cleanupInterval: "0s"` workaround, ACL/mTLS, source/Debug, assertions and real timings are unchanged. No native handoff is constructed. The installed prerequisite checker passed in the exact private prefix/cwd/user context, including the genuine Aspire 13.6.1 child; this was an offline prerequisite check, not a native test.

**Isolation and limits.** The fresh engine is **`fce19cc3-fdd4-4230-b312-77a3cd48f821`**, endpoint **`unix:///run/hx-retention-uywszy91/docker.sock`**; keeper **36350/start 3645341**, daemon **36375/start 3645351**, seven recorded owned processes. `preparation-owned-processes.json` binds every PID/start/namespace. The kernel reused historical namespace inode numbers, but the engine, process starts, endpoint and state are new; no stopped engine was restarted. Private slirp networking disables host loopback, with separate Docker/containerd/cgroup state and no host routing/firewall changes. Routing controls passed **16/16**. The engine contains seven prepared images and **zero containers**.

The existing graph remains seven containers / 15 resources, no VM. Admission requires at least eight Docker CPUs, 16 GiB Docker/Linux available memory, and 20 GiB staging/backing free space; PostgreSQL has eight-CPU/16-GiB limits. These are not a whole-graph reservation. The preparation capacity receipt passed (14 CPUs, about 90 GiB Docker memory); exact available memory/disk bytes are in `preparation-capacity.json`. Capacity, identities and unchanged inputs are checked once immediately before the approved launch.

**Time and attempt contract.** One launch, **zero retries/restarts**. Execution is at most **14400 s**, including **600 s startup**; stopping/preservation/disposal share a separate, nonrenewing **900 s**. Latest full-budget dispatch is **2026-10-25T10:27:22Z**. Absolute execution end is **14:27:22Z** and outer end **14:42:22Z** that day. A late admission must not dispatch. The existing real 4501-second selected observation allowance is retained. An expired plan is not extended.

**Affected builds and runtime admission.** From the candidate's `references/Hexalith.Memories`, both commands passed with zero warnings/errors:

```bash
dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -c Debug \
  -p:UseHexalithProjectReferences=true --artifacts-path "$CAPABILITY/application-proof-build" -m:1 --nologo -v:m
dotnet build src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj -c Debug \
  -p:UseHexalithProjectReferences=true --artifacts-path "$CAPABILITY/application-proof-build" -m:1 --nologo -v:m
```

The reused native model builder produced 36 bound outputs and 1089 matching source documents. Existing source/PDB inspectors passed 14 Server source checks, 35 Contracts documents, and 85 lifecycle-plus-Contracts documents; exact commands are in `binding-commands.json`. The existing startup observer now requires the actual mapped lifecycle/Server process and corresponding Contracts/lifecycle source document proofs before cohort A. Missing, foreign, stale, duplicate, replaced or disappeared process/proof evidence cannot admit. Original observer regressions plus eight new source-binding cases passed **48/48** (`python3 -B -m unittest -v test_observe_startup test_native_host_admission test_additional_sources test_discovery_admission`, cwd `CAPABILITY`; `observer-tests.log`). These are controlled offline tests; actual loaded-process admission remains for the authorized run.

**One ordinary review.** A context-free reviewer checked the actual source diff and concrete private plan. It found no substantive application diagnostic defect. Two operational findings were accepted:

| Finding | Correction / verification |
| :--- | :--- |
| Latest start was checked before admission, and execution/stopping used only relative deadlines | Dispatch rechecks the launch window after admission/collectors and the exclusive launch receipt. Execution/stopping deadlines use both UTC and monotonic clocks, clamped to the absolute endpoints; subprocess/poll waits cannot extend them. Covered by final private control tests. |
| Empty-engine cleanup required a stopping receipt absent before launch | Explicit disposal or controller failure creates the missing stopping receipt once, then reuses it. Collector launcher PID/start is persisted before releasing execution, so failed startup remains owned. Failure handlers attempt supported `down` only for an actual plan; absent IDs are inapplicable and cleanup failures stay unverified. Covered by final private control tests. |

Final private controls: `python3 -B -m unittest -v test_operator_bounds` from `CAPABILITY` passed **23/23**, 0.020 s (`operator-control-tests.log`). Tests cover near-cutoff dispatch, UTC jumps, cumulative stopping, pre-launch failure, owned collector release, supported-down failure handling and replaced-PID refusal. Root caught and corrected a helper-name collision in `main()` during patch inspection; the admission-path smoke test passes. No executed pre-change red is claimed for these private cases, and no real controller, engine or signal was exercised. Both review findings are resolved; none deferred. Review triage is in `root-review.json`; no second review cycle or unrelated workspace census was performed.

**Disposition.** On terminal, failure or withdrawal, preserve real native/evidence receipts; invoke supported `down` with the actual new run ID under the same prefix when one exists. If no native identity exists, record that `down` is inapplicable. Verify this private engine is empty, stop only its recorded PID/start identities and verified descendants, then verify processes, namespace, socket and actual native workspace/state absent. All steps share the first stopping budget and absolute outer end. Previous resources, shared Docker engines and Production are excluded. Keep private source/logs/evidence; no broad prune or process-name kill.

**Preparation corrections and limits.** A broad file-mode operation during parallel preparation inadvertently removed executable bits in the new copy and wrappers. The copied modes were restored from unchanged `BOOTSTRAP`, fresh wrappers restored, no external file symlinks found, and all 16 routing checks passed afterwards (`preparation-mode-restoration.json`). The new evidence-relative basename was corrected after readback caught an inherited historical path; old evidence was preserved. Two initial observer fixture errors were corrected before 48/48 passed; the initial log remains. These preparation defects were not live test failures. The source-flag owning Server blocker remains separately disclosed above; the compatible isolated candidate builds passed.

**Final readback (2026-10-24T22:02:17.910287+00:00): passed.** All **330 selected / 36 model / 54 operational / 489 runtime** hashes match; source proofs, seven images, seven owned process identities, mapping, empty engine, fresh absent state/evidence and open window pass. Plan SHA-256 **`7b62a542f5f6d54e48d0a6aa36d73c946a606298f9d806a9cd98ac1704bb446c`**; exact command SHA-256 **`5c5ce4c672a7c6bb06794033a200b956f1714bed84a858c06729cbff3ae53a79`**. `root-preparation-readback.json` is the receipt.

**Preparation snapshot before the execution above:** implementation, ordinary review, affected candidate preparation and final checks were complete; no approval or native launch existed yet. The subsequent owner approval was consumed by `55eb672cd9f30d006caa815eae60a867`. Its failed result and verified private cleanup are recorded above. Story 27.4, retention, full qualification, Production and A41 remain open.

### Bootstrap-stage execution — 2026-10-24; lifecycle capability refusal, disposal verified

The owner replied **“approve”**. `BOOTSTRAP/approval.json` (19:57:35.569071Z) binds plan `100bb6613d88bfe14564bca273a5ddd41fe42cb0cf6a50a83b5e302151b9cb3b` and command `2d35f059ba6dd2721444a36477deb2854c4326b161f3fbfaab163fb9eebe9f3f`. `python3 -B execute-approved.py`, run from `BOOTSTRAP`, did three things:

1. Once-before-launch admission passed at **19:57:44.543784Z**: 319 selected / 36 model / 39 operational / 489 runtime inputs, seven images, the empty engine `6dd698f6-b6a6-4c9e-9729-4f2e744c3b3d`, owned PID/start identities, and capacity.
2. Captures and the observer were made ready.
3. The unchanged supported command in `launch-command.txt` was dispatched once at **19:57:44.677179Z**, as native run **`1441c0dbdd973a0f9c0cbf6a8c8eb817`**.

Startup admission persisted at 19:59:14.424736Z and was confirmed to precede cohort A.

| Evidence | Actual result |
| :--- | :--- |
| Native terminal | **failed / 4 / HXT006**, completed 19:59:16.038388Z. TRX: 1 executed, 0 passed, 1 failed, `functional-accounting-loss`. Wrapper counts were not reported. |
| Cohort A counters | 125 attempted / 125 rejected / zero enqueued, acknowledged, persisted, dropped or conflicted. |
| `rejectionBreakdown` (complete) | `sanitizerUnavailable` 125, `sanitationFailed` 0; by reason `{CapabilityUnproven: 125}`; by stage `{RemoteValidation: 125}`; lifecycle Degraded/`CapabilityUnproven`. |
| `bootstrap` | `attempts` 3, `stage` `RemoteValidation`, `failureStage` `RemoteValidation`, `failureKind` `RemoteDenied`, `firstPublishedUtcMs` null. |

**Reading (per the outcome table).** Persistent stage failure with no publication. A lifecycle reason of `CapabilityUnproven` beside `RemoteDenied` means the lifecycle application refused writes, not a 401/403 (which would read `RemoteValidationPending`).

- **Established:** the writer bootstrap reached remote validation and repeatedly received the lifecycle app's refusal before sanitizer publication. Sanitation failure and writer-side configuration/secret failure are excluded for these attempts. This is not evidence that any lifecycle capability probe ran.
- **What the refusal means:** `/v1/access-telemetry/validate` returns the runtime-gate reason only when epoch and component-profile hash match, so the gate held its fail-closed `CapabilityUnproven`. In the local functional profile, `AccessTelemetryCapabilityProbeRunner` can refuse local preflight without invoking any probes; otherwise it attempts probes once per process within 30 s and retains failure. Exact endpoint epoch/profile equality does not establish the hosted Development/deployment/run/source binding. The retained plan deadline is later than failure, but process-local binding and probe outcomes were not captured.
- **Hypotheses, not established:** local preflight refusal, failed/malformed probe results, exception, cancellation or timeout can all produce this gate reason on the `RESOLVER`-derived candidate. `MAPPING` and this run share the refusal shape, while `GUARD` accepted 125/125. The SQLite discovery `cleanupInterval: "0s"` edit is the only known structural difference.
- **Limits:** the lifecycle app's console, per-capability results and gate metrics were not captured. The 19:59:17.888Z owned-graph-loss receipt is teardown after the terminal.

**Disposition.** Supported `down --run-id 1441c0dbdd973a0f9c0cbf6a8c8eb817` (same prefix and cwd) returned **0 / HXI001** at 19:59:19.969503Z. `sudo -n python3 -B dispose-owned.py` was copied from `MAPPING`, with guards added only for a missing run ID or receipts. It verified the empty engine, stopped the captures, observer and exact owned daemon/keeper/slirp identities, and confirmed absence at **19:59:31.791139Z**: **14.794657 s** from the native terminal, within **900 s**. No owned processes or namespace members remain, and the socket, workspace and run state are absent. Evidence: `BOOTSTRAP/run-result.json`, `private-engine-disposition.json`, `before-disposal/`, `native.stdout.json`, `down.stdout.log`, `native-terminal.json`, and the owner's `functional.json`/`functional.python3.trx`.

**One launch, zero retries; approval consumed; engine stopped; no further launch authorized or prepared.** Next owner: Memories, to obtain per-capability evidence for the lifecycle app's one-shot local probe offline before any fix. Story 27.4, local retention, full qualification, Production and **A41 remain open**. The preparation below is historical.

### Bootstrap-stage rejection breakdown — 2026-10-24; reviewed preparation awaiting one authorization

**Scope.** This section implements the Spec 14 loop-1 `bad_spec` amendment after consumed run **`dd8512086bb377547e89eda7d028b986`** (cohort A **125 attempted / 125 rejected / zero enqueued**). Preparation baseline is Memories **`014a4ad365f6d4d6e47c806aba1621c85da2757d`**. No launch, topology, Production test, dependency change, staging, commit or push occurred. The loop-1 `BREAKDOWN` plan (SHA-256 `ef9bcdcb…`) stays withdrawn and was never authorized. Its report section is not restored. `DIAG` diagnosis evidence remains valid; no cause is attributed, and emission ordering is unchanged.

**Why loop 1 was not enough.** The lifecycle status starts as Degraded/`RemoteValidationPending`, and the bootstrap catch-all publishes the same pair for every unexpected exception. A breakdown keyed only by lifecycle reason would therefore read `{RemoteValidationPending: 125}` both for emission before publication and for a persistently failing Dapr configuration, secret or remote-validation call.

**Change (owning Memories).** The loop-1 implementation (`DIAG/loop1-reverted-implementation.patch`, SHA-256 `561f37cc…`) was re-applied to `014a4ad3` without its report hunk, then extended:

- **New process-local singleton.** `AccessTelemetryLifecycleBootstrapProgress` lives in its own file, with supporting `AccessTelemetryLifecycleBootstrapSnapshot`, `AccessTelemetryLifecycleBootstrapStage` and `AccessTelemetryLifecycleBootstrapFailureKind`. It is registered unconditionally.
- **What it records.** `AccessTelemetryLifecycleBootstrapService` records:
  - completed attempts;
  - the in-flight or last stage: `NotStarted`, `Configuration`, `Secret`, `RemoteValidation` or `Published`;
  - the last failure stage and its allow-listed kind: `ConfigurationInvalid`, `SecretMissing`, `RemoteDenied` (a lifecycle refusal, or HTTP 401/403 or gRPC `PermissionDenied`/`Unauthenticated` on a secret, configuration or invocation call; the failure stage names the call), `Timeout`, `Canceled`, `Transport`, `DaprApi`, `Format` or `Other`;
  - the first sanitizer publication time in UTC milliseconds.
- **No detail is retained.** Only the enum name is kept; exception text never leaves the catch. `AccessTelemetryLifecycleStatusSnapshot`, health, metrics, status values and retry delays are unchanged.
- **Logger.** It now reads the lifecycle status and the bootstrap stage before the sanitizer. Each sanitizer-unavailable rejection is counted both by lifecycle reason and by bootstrap stage.
- **Response.** `rejectionBreakdown` adds `sanitizerUnavailableBootstrapStages` and a `bootstrap` snapshot taken when the response is built. Every breakdown dictionary is an owned immutable copy, including in `with` copies and `Empty`.
- **Runner construction.** DI selects the public runner constructor that takes both the status and the bootstrap progress, and the logger provider also receives the progress.
- **Selected verifier.** `tools/access_telemetry_functional_retention.py` allow-lists and bounds the new fields and retains them in `cohort-*-accounting.json` before validation. A passing segment must report an exact, empty breakdown, and its bootstrap snapshot is checked for shape and allow-list validity only. `rejectionBreakdownComplete` also checks the stage sum.
- **Unchanged guards.** Any rejection still fails `functional-accounting-loss`.

**Files.** Production:

- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- in `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/`:
  - the four new bootstrap files;
  - `AccessTelemetryQualificationRejectionBreakdown.cs` (new);
  - `AccessTelemetryLifecycleBootstrapService.cs`, `AccessTelemetryLifecycleLogger.cs`, `AccessTelemetryLifecycleLoggerProvider.cs`, `AccessTelemetryQualificationAccounting.cs`, `AccessTelemetryQualificationWorkloadResult.cs`, `AccessTelemetryQualificationWorkloadRunner.cs`;
- `tools/access_telemetry_functional_retention.py`.

Tests:

- New: `AccessTelemetryLifecycleBootstrapProgressTests.cs`, `AccessTelemetryQualificationRejectionBreakdownTests.cs`, `AccessTelemetryQualificationRejectionAccountingTests.cs`.
- Updated: `AccessTelemetryLifecycleRegistrationTests.cs`, `AccessTelemetryQualificationWorkloadTests.cs`, `test_functional_retention.py`, `test_selected_active_sequence.py`.

**Exact focused commands and results.** The cwd is `/home/administrator/projects/hexalith/memories`. `DLL=tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll`, `NS=Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle`. `O=<BOOTSTRAP>/owning-checks`, where `BOOTSTRAP` is defined below.

| Check | Exact command | Result |
| :--- | :--- | :--- |
| Build | `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Debug -m:1 --nologo -v:m` | **0 warnings / 0 errors** (`O/review-fix/build.log`) |
| Focused classes | `DiffEngine_Disabled=true dotnet exec $DLL -noLogo -noColor -class $NS.AccessTelemetryLifecycleBootstrapProgressTests -class $NS.AccessTelemetryQualificationRejectionBreakdownTests -class $NS.AccessTelemetryQualificationRejectionAccountingTests -class $NS.AccessTelemetryLifecycleRegistrationTests -class $NS.AccessTelemetryQualificationWorkloadTests -class $NS.AccessTelemetryDeliveryCheckpointTests` | **90/90**; loop 1 was 80/80, and 10 cases are new (`O/review-fix/focused.log`) |
| Telemetry namespaces | `DiffEngine_Disabled=true dotnet exec $DLL -noLogo -noColor -namespace Hexalith.Memories.Server.Tests.Telemetry -namespace $NS` | **295/295** (`O/review-fix/telemetry-namespaces.log`) |
| Repeat loop | `for i in $(seq 1 20); do DiffEngine_Disabled=true dotnet exec $DLL -noLogo -noColor -class $NS.AccessTelemetryLifecycleBootstrapProgressTests -class $NS.AccessTelemetryQualificationRejectionBreakdownTests -class $NS.AccessTelemetryQualificationRejectionAccountingTests -class $NS.AccessTelemetryLifecycleRegistrationTests; done` | **20/20** runs pass, 17 cases each (`O/review-fix/repeat-summary.log`) |
| Python named tests, cwd `tests/tooling/access_telemetry_functional_retention` | `python3 -B -m unittest -v test_functional_retention.FunctionalTests.test_real_workload_response_contract_and_no_loss_are_required test_functional_retention.FunctionalTests.test_bounded_breakdown_drops_out_of_range_and_unknown_values test_functional_retention.FunctionalTests.test_rejection_breakdown_allow_lists_match_the_server_enums test_selected_active_sequence.SelectedActiveSequenceTests.test_pre_enqueue_rejection_retains_bounded_breakdown_and_still_fails_accounting test_selected_active_sequence.SelectedActiveSequenceTests.test_rejection_breakdown_values_are_allow_listed_and_must_be_consistent test_selected_active_sequence.SelectedActiveSequenceTests.test_consistent_nonzero_breakdown_is_complete_and_still_fails_accounting test_selected_active_sequence.SelectedActiveSequenceTests.test_inconsistent_breakdown_is_retained_but_disclosed_incomplete test_selected_active_sequence.SelectedActiveSequenceTests.test_accounting_failure_retains_bounded_rejection_counters_before_validation test_selected_active_sequence.SelectedActiveSequenceTests.test_accounting_capture_excludes_untrusted_fields_values_and_foreign_identities` | **9/9** before the coordinator review; the 11-test covering set below replaced it |
| Python directory (before the coordinator review) | `python3 -B -m unittest discover -v -s tests/tooling/access_telemetry_functional_retention -p 'test_*.py'` | `Ran 129 tests`, all 129 `ok` (loop 1's 126 plus 3 new), but the run exits **`FAILED (errors=1)`**: `setUpClass (test_functional_target.RenderTests)` errors on the `kubectl` blocker below, so those class tests never execute (`O/python-functional-retention-dir.log`; the candidate run is identical) |
| Coordinator-review fixes: covering C# tests only | the build above, then `DiffEngine_Disabled=true dotnet exec $DLL -noLogo -noColor -class $NS.AccessTelemetryLifecycleBootstrapProgressTests -class $NS.AccessTelemetryQualificationRejectionBreakdownTests` | build **0 warnings / 0 errors**; **11/11**, including the new in-place key-change test and the `GetConfiguration` case (`O/coordinator-fix/build.log`, `O/coordinator-fix/focused.log`) |
| Coordinator-review fixes: covering Python tests only, cwd `tests/tooling/access_telemetry_functional_retention` | `python3 -B -m unittest -v` with the 9 names above plus `test_functional_retention.FunctionalTests.test_breakdown_completeness_requires_internally_consistent_bootstrap_progress` and `test_selected_active_sequence.SelectedActiveSequenceTests.test_real_selected_sequence_reconciles_cleanup_before_first_poll` | **11/11** owning and **11/11** in the candidate (`O/coordinator-fix/python-focused.log`, `BOOTSTRAP/candidate-python-focused.log`). Wider suites were not rerun after these fixes; the coordinator runs full verification. |

**Red evidence.** The new tests depend on new types, so they cannot compile against `014a4ad3` or the loop-1 code. Red runs therefore use sequential in-place mutants. Each mutant reverts one new behaviour, rebuilds, runs only the named tests, then restores the file. Hashes are verified after each restore, and the worktree diff and untracked-file hashes were identical before and after.

- First round: `python3 -B <BOOTSTRAP>/owning-checks/mutation/run_mutations.py <BOOTSTRAP>/owning-checks/mutation`.
- Review-fix round: `python3 -B <BOOTSTRAP>/owning-checks/mutation/review-fix/run_mutations_review_fix.py <BOOTSTRAP>/owning-checks/mutation/review-fix`.

All 15 mutants failed their named tests:

| Mutant | Failing tests |
| :--- | :--- |
| Progress not registered in DI | `RegisteredRunner_IsStatusAndBootstrapAwareInTheRealComposition` |
| Logger ignores the stage | mixed-segment logger, unpublished, persistent-denial and publication-during-burst replays (4/4) |
| Bootstrap catch-all not recorded | both hosted bootstrap tests |
| Remote denial not recorded | `PersistentRemoteDenial_…` |
| Runner omits the bootstrap snapshot | published replay and composition test |
| Breakdown shares a mutable input | `Breakdown_ExposesOwnedReadOnlyDictionariesThatNoCallerCanMutate` |
| `Since` keeps cumulative stages | delta test and `EarlierRejectedBaseline_…` |
| Python pass ignores bootstrap shape or stage counts | `test_real_workload_response_contract_and_no_loss_are_required` |
| Completeness ignores stage sum, reason sum or excess | `test_inconsistent_breakdown_is_retained_but_disclosed_incomplete` (3 mutants) |
| Bootstrap allows any stage | allow-list tests |
| Catch-all status not published (review fix) | both hosted bootstrap tests |
| HTTP JSON ignores nulls (review fix) | composition test |

The first round ran against the pre-review test versions. The review fixes only added assertions, and two further mutants cover them.

These new tests never had a red run:

- C#: `Classify_MapsEachFailureToTheFixedAllowListWithoutDetail`, `Progress_KeepsTheFirstPublicationAndOnlyBoundedNames`, `Breakdown_ReportsOnlyBoundedLifecycleAndBootstrapNames`, `Logger_WithoutBootstrapProgressCountsUnavailableRejectionsAsUnobservedStage`, and `SanitizerRefusal_IsCountedSeparatelyFromSanitizerUnavailability`. The last one had only loop 1's absent-breakdown red.
- C#, added for the coordinator review: `HostedBootstrap_InPlaceMarkerKeyChangeIsTerminalAndRecordedAtTheStageInFlight`, the `GetConfiguration` case in `HostedBootstrap_ReportsEachRefusalAtItsStageWithItsBoundedKind`, and the new `Classify` cases (gRPC `PermissionDenied`/`Unauthenticated` to `RemoteDenied`, `NotFound` to `DaprApi`).
- Python: `test_bounded_breakdown_drops_out_of_range_and_unknown_values`, `test_consistent_nonzero_breakdown_is_complete_and_still_fails_accounting`, the extended enum-parity test, `test_breakdown_completeness_requires_internally_consistent_bootstrap_progress` and the `breakdown-bootstrap-inconsistent` variant.
- Recording sites that tests reach but no mutant ever reverted: `CompleteFailure(ConfigurationInvalid)` for invalid configuration, `CompleteFailure(ConfigurationInvalid)` for the in-place key change, `CompleteFailure(SecretMissing)`, `CompleteFailure(Format)`, and the hosted bootstrap's first-publication time (`CompletePublished`).
- The status-before-sanitizer read-order swap is an inherent race. It has no deterministic regression and is source-inspected only.

**Broad-lane blockers.** These are recorded separately from the focused fallback above.

- **.NET.** `DiffEngine_Disabled=true dotnet exec $DLL -noLogo -noColor` runs **3053**: 3044 pass, 1 skipped, 8 fail. All 8 are `Deployment.ProductionDeploymentArtifactsTests` (`O/review-fix/server-full-assembly.log`), each failing with `System.ComponentModel.Win32Exception : An error occurred trying to start process 'kubectl' with working directory '/home/administrator/projects/hexalith/memories'. No such file or directory`. `which kubectl` exits 1.
- **Python.** `setUpClass (test_functional_target.RenderTests)` raises `RuntimeError: offline-render-requires-kubectl` (`test_functional_target.py` line 38). A pristine `git archive 014a4ad3` export reproduces it unchanged with `python3 -B -m unittest -v test_functional_target.RenderTests` (`O/python-baseline-render-tests.log`).

**Count changes.**

- **Selected inputs.** RESOLVER had **308**, loop 1 had 312, and this preparation has **319**: 306 unchanged, the same 2 changed (`AccessTelemetryLifecycleLogger.cs`, `tools/access_telemetry_functional_retention.py`) and **11 newly selected** production sources. These are loop 1's 4 plus `MemoriesServerServiceCollectionExtensions.cs`, `AccessTelemetryLifecycleBootstrapService.cs`, `AccessTelemetryLifecycleLoggerProvider.cs` and the 4 new bootstrap types.
- **Operational inputs.** These stay at **39**: `MAPPING`'s 37 plus loop 1's two extended-inspector entries, rebased. The inspector entries now name this preparation's copy. The four reused-engine entries (guard, client configuration, `daemon.json`, `start-private-daemon.sh`) keep their withdrawn-preparation paths and unchanged hashes.
- **Server PDB documents.** These rise from **441** in the withdrawn loop-1 `BREAKDOWN` Server proof (`BREAKDOWN/server-whole-pdb-binding.json`) to **445**, one per new bootstrap type file.

**Outcome interpretation for the next run.** A failed result still consumes the launch, and the breakdown is diagnostic only. Rows can combine; read each part by its own row. The snapshot keeps no stage-entry or attempt time, so it shows neither duration nor persistence over time. Nor can it separate a hung retry from waiting for the next retry at the same stage.

| Retained `cohort-a-accounting.json` result | Reading | Next step |
| :--- | :--- | :--- |
| All rejected; stage counts only at `NotStarted`/`Configuration`/`Secret`/`RemoteValidation`; `bootstrap.attempts` 0; `firstPublishedUtcMs` null | The first bootstrap attempt had not completed when cohort A was emitted. It was not yet run, still in progress, or hung at that stage; the snapshot cannot say which. | Emission before publication is demonstrated. Offline, make the existing selected path wait, bounded inside the 600 s startup, for existing lifecycle readiness before cohort A. Then prepare one new plan. |
| Partly rejected; `firstPublishedUtcMs` between `startedUtcMs` and `emitFinishedUtcMs`; stage counts only pre-publication | Publication during the burst | Same bounded readiness wait before cohort A |
| All rejected; `bootstrap.attempts` ≥ 1 with no publication; `failureStage`/`failureKind` set; stage counts at that stage | A dependency call failed and never succeeded. `attempts` ≥ 2 shows repetition. Only the last failure is kept. The kind names the class: `RemoteDenied` for a lifecycle refusal or a 401/403 or gRPC permission/authentication denial (the stage names the denied call: secret scope, configuration or invocation ACL/app token); `Transport`/`Timeout`/`DaprApi` for a sidecar or API failure; `SecretMissing`/`ConfigurationInvalid`/`Format` for configuration or secret content. | Diagnose that dependency class offline from the retained sidecar and generated configuration. The owner is Memories for Server configuration/secret wiring, or Platform/Builds for generated ACL/component configuration. No ordering change. |
| Kind `DaprApi` at stage `Secret` | Includes a secret store that reports the key absent as an error. `SecretMissing` means only that the store returned no or blank value for the key. | Check the secret store and key reference |
| Kind `Canceled` or `Other` | `Canceled` is a dependency call canceled without host stop and without a timeout cause; `Other` is an unclassified exception. Neither names a dependency beyond its stage. | Use the retained sidecar logs for the stage; draw no class inference |
| Stage `RemoteValidation` with kind `Format` or `ConfigurationInvalid` and lifecycle reason `ConfigurationInvalid` | The post-validation steps are attributed to the stage still in flight: sanitizer construction (for example a marker key under 256 bits, `Format`) or an in-place key change under the same generation (`ConfigurationInvalid`, terminal). A malformed validation response is instead `Format` with `RemoteValidationPending`. | Fix the secret content or response contract in its owner |
| `firstPublishedUtcMs` before `startedUtcMs`, yet sanitizer-unavailable counts | The sanitizer was published, then cleared by a terminal refresh path. The per-rejection reason can read `None`, because `Clear()` precedes `PublishTerminal`; the response-time status reads Unhealthy/`ConfigurationInvalid`. | Diagnose the configuration or secret change that made the refresh terminal. The Server process must restart. |
| Both `sanitizerUnavailable` and `sanitationFailed` > 0 | A mixed segment: part rejected before publication (or after a clear), part refused by a published sanitizer | Read each part by its own row |
| `sanitationFailed` > 0 and `sanitizerUnavailable` 0 | The published sanitizer refused the records: schema, key or clock/expiry | Replay offline with the retained identity and published retention/key generation, then fix the sanitizer input in Memories |
| `rejected` > `sanitizerUnavailable` + `sanitationFailed` | A post-queue remainder: records rejected after enqueue (delivery-side rejection), which the pre-queue breakdown does not explain | Diagnose delivery from the retained lifecycle/sidecar evidence; no bootstrap inference |
| `Unobserved` stage, lifecycle health or reason | The runner or logger provider was built without the status or bootstrap progress: a miswired composition | Treat the breakdown as unattributable and fix the composition in Memories offline |
| Response-time lifecycle health/reason and `bootstrap` disagree by one transition | The two snapshots, and each per-rejection pair, are separate non-atomic reads | Prefer the per-rejection stage counts and allow one transition of skew |
| `rejectionBreakdownComplete: false` | A field was dropped, counts are inconsistent, or the bootstrap fields contradict each other | Use only the retained values; draw no further inference |
| 125/125 accepted | Empty breakdown | The real retention assertions decide the run |

**Fresh candidate and binding.** **`BOOTSTRAP=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-bootstrap-stage-20261024-nj4ub5_x`** physically copies `RESOLVER/compatible-platform` (`cp -a`, 17.6 s). `adopt-inputs.py` adopts the 20 affected files (13 production/tool, 7 test), recorded in `adopted-inputs.json`:

- 18 files were adopted whole from the owning working tree: 8 are new files absent from both `014a4ad3` and RESOLVER, and 10 existing RESOLVER copies equalled `014a4ad3` apart from line endings.
- `MemoriesServerServiceCollectionExtensions.cs` and `AccessTelemetryQualificationWorkloadTests.cs` are an older Memories snapshot in RESOLVER. Only the owning 3-line and 2-line hunks were applied, by unique exact context, so no unrelated owning change entered the candidate. After the coordinator review, `adopt-inputs.py` re-adopted only the 7 changed files (`adopted-inputs.pre-coordinator.json` keeps the previous record).
- A whole-tree comparison shows only those files, rebuilt `obj/` outputs of the in-place offline model build, and a `references/Hexalith.Builds/.git/index` stat refresh with the same entries and HEAD.

Genuine Platform `7021c41…` / Builds `007bb9b…`, EventStore 3.127.0, tool 4.35.2, Dapr 1.18.1, PostgreSQL 18.6 and source/Debug are unchanged.

- **Mapping.** The installed resolver on the selected manifest yields **`Hexalith.Memories` / 17 entries / no findings / `79e432172896c1104e9609c360ca84d72dfe1f88022d54d8fcf97592a01fdb86`**, offline and inside the exact prefix. After root rebasing it equals the withdrawn and consumed mappings except the derived hash.
- **Model.** The supported offline model build has no identity findings. Of its 36 outputs, 20 are byte-identical; the 16 path-dependent DLL/PDBs bind 1089/1089 documents with root-relative sets equal to RESOLVER's.
- **Server proof.** The offline source/Debug Server build has 0 warnings/errors and its whole PDB binds **445/445**. A copy of the existing read-only inspector changes lines 17 and 22 so the Hosting file can be named, and checks **14/14** affected and retained sources. It refuses the withdrawn `BREAKDOWN` Server DLL as a negative control.
- **Commands.** `verification.json` lists every exact command.

**Reused empty private engine.** The spec allows reuse only if exact identity and emptiness checks pass again. `reused-engine.py` (read-only) passed at **18:53:35Z** and again after the coordinator-review rebuild:

- Docker 29.8.2 engine **`6dd698f6-b6a6-4c9e-9729-4f2e744c3b3d`** on `unix:///run/hx-retention-sv84pzjt/docker.sock`;
- **zero containers**, and the seven image identities match;
- all seven owned PID/start/namespace identities match (keeper **63126**, daemon **63148**);
- the copied engine records are byte-identical.

The engine's unchanged guard, client configuration, data root and logs stay in the withdrawn directory. The plan references that directory for nothing else. Further context checks:

- Routing passes **16/16**.
- The genuine installed prerequisite checker passes in this exact context: diagnostic null, Aspire **13.6.1** exit 0, UID/GID 1000, private namespace, fresh cwd and `XDG_DATA_HOME`.
- Context/plan checks pass **16/16** (`test_preparation_context.py`).
- Observer script, MON collectors and guard parser are reused unchanged. Observer configuration changes only the mapping hash, model/server proofs, inspector identity and fresh paths.

**Concrete plan.** `BOOTSTRAP/run-plan.json` and the full `launch-command.txt`:

- cwd `BOOTSTRAP/compatible-platform`, owner its `references/Hexalith.Memories`;
- fresh absent state `BOOTSTRAP/native-run`;
- owner-relative evidence `artifacts/retention/repair-spec14-bootstrap-stage-20261024-nj4ub5_x/functional.json`.

It runs under the exact prefix: privileged `nsenter --net=/proc/63126/ns/net`, then `runuser -u administrator -- env`, the guarded client, the reused `DOCKER_HOST`/`DOCKER_CONFIG` and a fresh `XDG_DATA_HOME`.

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-bootstrap-stage-20261024-nj4ub5_x/functional.json --output json
```

- **Budget and window.** **One launch / zero retries**, **14400 s including 600 s startup / separate cumulative 900 s stopping**. The start window runs from **2026-10-24T19:44:10Z** (final preparation build) to the latest start **2026-10-25T10:27:22Z**; the outer end is **14:42:22Z**.
- **Unchanged guards.** Resource floors, PostgreSQL caps, the 7-container/15-resource graph, real timing and every assertion are unchanged.
- **Admission and monitoring.** Admission checks capacity and isolation once immediately before launch. The existing captures/observer must admit before cohort A and the long wait, and the first loss, detection, stop or terminal anchors stopping.
- **Cleanup.** Supported same-prefix `down --run-id <actual-new-run-id>`, then PID/start-checked disposal of only this private engine. An early refusal with no run ID gets no invented identity.
- **Executor.** `execute-approved.py` is the loop-1 executor byte for byte. It binds this directory's inventories and permits a single exclusive dispatch.

**Single ordinary review and readback.** An independent read-only reviewer found **no High or Medium issues**. It recomputed all 394 pinned hashes with 0 mismatches and confirmed DI, read order, single completion, owned dictionaries, the adoption hunks and the plan bounds. Every Low finding and nit was verified and resolved (`BOOTSTRAP/root-review.json`):

- **L1, L2.** Attribution and interpretation limits are now stated in the plan and the table above. No field beyond the spec list was added, and the remote call order is unchanged.
- **L3.** This report and the spec are updated.
- **L4.** The never-red read-order fix is disclosed.
- **L5.** The composition test now serializes through the composed HTTP JSON options and requires explicit nulls.
- **Nits.** Hosted tests start from `NoData` so the published status is proved, a trivial assertion was removed, and the Builds index refresh is disclosed.

Post-fix checks repeat 90/90, 295/295, 3053 (8 `kubectl`), 20/20 repeats, 2/2 review-fix reds, context 16/16, and re-adoption of only the two changed test files.

**Coordinator review.** It found twelve further issues, all resolved (`root-review.json` `coordinatorReview`):

- gRPC secret, configuration and invocation denials now map to `RemoteDenied`.
- The status races in two tests are fixed, and `WaitForAsync` now allows about 30 s.
- New tests cover the in-place key change and a failing `GetConfiguration` call.
- The verifier's `failureStage` allow-list is now `Configuration`/`Secret`/`RemoteValidation`/null. Bootstrap cross-field checks were added to the diagnostic completeness flag only.
- Fixtures now use valid states, and an inconsistent-bootstrap variant was added.
- The approved gate profile constant replaces the hard-coded hash.
- This outcome table and the verification statements are corrected.
- The candidate was refreshed: re-adoption of the 7 changed files, selected inputs **319**, and a rebuilt Server proof (0 warnings/errors, inspector **14/14**, whole PDB **445/445**, `BREAKDOWN` DLL refused). The plan was regenerated, the reused engine rechecked, and context passes **16/16**.

The independent readback at **2026-10-24T19:45:20Z** (`root-preparation-readback.json`) passed:

- correct engine, zero containers and seven images;
- seven owned identities;
- **319/36/39/489** inventories;
- helpers, mapping and Server sources;
- absent fresh state, evidence, approval and launch;
- an open window.

The reviewed plan SHA-256 is **`100bb6613d88bfe14564bca273a5ddd41fe42cb0cf6a50a83b5e302151b9cb3b`** and the launch-command SHA-256 is **`2d35f059ba6dd2721444a36477deb2854c4326b161f3fbfaab163fb9eebe9f3f`**; `readyForOperationalApproval:true`, `launchAuthorized:false`.

**Limits.**

- Bootstrap progress keeps one in-flight/last snapshot and the last failure only. It keeps no stage-entry or attempt timestamp, so it shows neither duration nor persistence over time and cannot separate a hung retry from waiting to retry. A refresh after publication cycles the stage, and the lifecycle and bootstrap snapshots are read non-atomically.
- A shutdown-interrupted attempt is not completed.
- The existing unsynchronised `RecordActivity` can leave a lifecycle reason stale.
- The upstream SQLite GC defect and the historical AppHost-exit/HXP020 causes remain.

**One operational authorization remains; no launch is authorized.** Story 27.4 stays **in progress**; local retention, full qualification, Production and **A41 remain open**.

### Corrected-mapping execution — 2026-10-24; cohort A rejected, disposal verified

The owner replied **“approve”** for the reviewed plan in **`MAPPING=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-selected-mapping-20261024-0bzfsk_f`**. Approval **2026-10-24T16:26:12.182975Z** binds plan SHA-256 **`cff42780e9b295e3a658235fa7534f8fe72f87d7b52d278025fa099c9e684d31`** and the exact command in `launch-command.txt`. Once-before-launch admission passed at **16:26:13.927358Z**: **308 selected inputs, 36 model outputs, 37 operational inputs and 489 runtime bindings**, immutable images, fresh private-engine ownership/isolation and resource capacity. Captures and observer were ready before the single native invocation at **16:26:14.064099Z**, run **`dd8512086bb377547e89eda7d028b986`**. The unchanged command below used the complete reviewed namespace/user/guarded-Docker prefix in `MAPPING/live-execution.json`, from `RESOLVER/compatible-platform`; owner is its physical `references/Hexalith.Memories` directory.

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-selected-mapping-20261024-0bzfsk_f/functional.json --output json
```

**Startup passed.** All **15 resources** reported healthy. Actual mapping **`bc9396119bf71dbbdb2e8abfa4501468decedd878e295c2a6d6bb17bbc867502`**, Platform/catalog identity, model and loaded Server binding, all four effective SQLite discovery files (`cleanupInterval: "0s"`, exact run-owned registry, preserved security), native host, real sidecar consoles and complete owned graph passed startup admission at **16:27:52.007551Z**. Ordering was confirmed at **16:27:53.647482Z**, using the persisted admission preceding cohort A accounting. This resolves the selected-manifest refusal for this execution. HXP020 did not recur; its previous cause remains unresolved. The scoped discovery workaround ran only through startup and initial cohort submission, so long-duration effectiveness is unproved.

**Authentic test failure.** The retained response counters at **16:27:53.609Z** are:

| Cohort A counter | Observed |
| :--- | ---: |
| Attempted | 125 |
| Rejected | 125 |
| Enqueued / acknowledged / persisted / transaction acknowledgements | 0 each |
| Dropped / conflicted | 0 each |

The unchanged assertion returned **`functional-accounting-loss`**. Native terminal **16:27:53.6427089Z** is **failed / exit 4 / HXT006**; the controller observed exit at **16:27:53.883866Z**. Native `functional.python3.trx` reports **one failed test, zero passed**; native wrapper test counts are unreported, which is retained without reinterpretation. No cohort B, retention wait or passing `functional-observations.json` exists. Retention, strong pre-expiry reads, expiry/purge, due-index/reminder progress and final unchanged B remain unproved.

**Diagnosis limit.** Returned accounting establishes rejection before enqueue, unlike the earlier partial-delivery failures. The matched logger source records rejection when its sanitizer is unavailable or sanitation fails; this response retains no reason breakdown to distinguish them. Business readiness and the qualification gate alone do not establish sanitizer readiness. No bootstrap-race, schema-defect or discovery causal claim is made, and no speculative product/assertion change was applied. A sidecar subscription-endpoint 401 was also captured before cohort A, but it does not establish the rejection cause. Memories owns the remaining pre-enqueue diagnosis. The post-terminal AppHost-loss receipt at **16:27:54.238523Z** follows the failed native outcome and stopping; it is not evidence of an initiating process loss.

**Verified disposition.** Supported same-prefix `down --run-id dd8512086bb377547e89eda7d028b986` started **16:27:53.885590Z** and returned **0/HXI001** at **16:27:56.756168Z**. The authoritative final native selected-test record is **removed / empty container and process sets / unverified:false**. Original owner `functional.json` preserves its earlier **retained / unverified:true** snapshot. Both are retained; neither result was manufactured or rewritten. The correct private engine was empty before exact PID/start-checked collector/observer and daemon/containerd/slirp/keeper disposal. Private-engine verification completed **16:31:18.115893Z**; final verification **16:32:01.432644Z** confirms all **29** bound native graph process identities and the recorded operator/preparation identities absent, no private-namespace processes, and absent private socket, native workspace and state. Controller stopping charge is **247.558814s**; the more conservative charge from the preceding failed accounting observation is **247.823644s**, within the separate cumulative **900s**. No retry, restart, shared-engine cleanup, prior-resource disposal or other-session interruption occurred.

`MAPPING` preserves approval, command/admission/execution receipts, native/down outputs, original functional evidence and TRX, final native disposition, actual generated inputs, all four captured native sidecar streams, pre-disposal plan/readiness/accounting, startup admission/order and graph evidence, exact process disposal, `final-disposition.json` and `run-result.json`. Captured public sidecar snapshots end at Running and do not independently prove each sidecar's terminal state; native cleanup plus exact process/namespace absence establishes removal. Approved **4.35.2 / Dapr 1.18.1 / PostgreSQL 18.6**, source/Debug, real **125/125 and 1800/3600/900s** assertions, resource/security limits, **14400s including 600s startup / separate 900s stopping**, and **2026-10-25T14:42:22Z** outer end remained unchanged. **One launch, zero retries; this approval is consumed and the private engine is stopped. No further launch was authorized or prepared at this result; the bootstrap-stage rejection breakdown preparation above is the later correction.** Story 27.4 stays **in progress**; local retention, full qualification, Production and **A41 remain open**. The mapping preparation below is historical.

### SQLite-workaround invocation — 2026-10-24; startup refusal, disposal verified

The owner replied **“approe”**, approving the complete reviewed plan once at **2026-10-24T15:53:32.436048Z**. `RESOLVER/approval.json` binds that exact reply, plan and command. Once-before-launch admission passed; captures and observer were ready before the single native invocation at **15:53:34.306040Z**, run **`011cf1200ded0150b2d21d06d4d086a8`**. Approved versions, source/Debug, assertions, resource limits and **14400s / 600s included startup / separate cumulative 900s stopping** remained unchanged.

At **15:53:48.159316Z**, the observer latched **`native-catalog-or-source-mismatch`** before startup admission. Its prepared mapping hash was **`114b131d59b9648f56603cbe797c77d90447a713daa153271e7921f49df2db7f`**; the actual native plan records **`bc9396119bf71dbbdb2e8abfa4501468decedd878e295c2a6d6bb17bbc867502`**. Stopping began at **15:53:48.386628Z**. Authentic native terminal followed at **15:53:48.6318229Z**, **failed / exit 3 / HXP020**; its diagnostic says no resources were started. The diagnosis below establishes that preparation used the Platform-root manifest instead of the selected Memories manifest. Whether HXP020 resulted from teardown or an independent preflight failure remains unresolved. No completed startup admission, cohort evidence, native TRX or passing observations exists; native test counts are unreported. This run does not establish live discovery, retention, expiry/purge or unchanged-newer-cohort behavior.

Same-prefix supported `down --run-id 011cf1200ded0150b2d21d06d4d086a8` returned **0 / HXI001** at **15:53:49.704683Z**. The correct private engine was empty before stopping. Exact owned collectors, observer, daemon/containerd/slirp/keeper and their captured descendants were stopped; initial private-engine disposal was verified **15:55:46.090526Z**, **117.935169s** after the first observer failure. Final independent readback at **15:58:09.225265Z** verifies all **18** recorded PID/start identities absent, no private-namespace processes, absent private socket, native workspace and native run-state, and stopped captures/children. Total cumulative charge **261.074841s**, within **900s**. The actual native selected-test record remains **`retained` / empty sets / `unverified:true`**; preserve that marker alongside the independent verified-disposal receipt rather than rewriting it to removed.

`RESOLVER/run-result.json`, `final-disposition.json`, `private-engine-disposition.json`, `native-selected-test-final.json`, native/down outputs, startup failure, `before-disposal/plan.json` and `after-supported-disposal` retain authentic evidence. Original approved plan/candidate and prior results are preserved. **The one launch approval is consumed, the private engine is stopped, and no retry or further launch is authorized.** Story 27.4, local retention, full qualification, Production and **A41 remain open**. The older SQLite-workaround preparation below is a historical snapshot of this consumed invocation.

### Selected-manifest mapping correction — 2026-10-24; historical reviewed preparation

**Historical reviewed preparation; the approved execution above consumed this plan and its one launch. The preparation engine is now stopped.**

**Demonstrated input correction.** Private **`MAPDIAG=/home/administrator/.local/state/hexalith/memories/access-telemetry/diagnose-spec14-selected-mapping-20261024-spye8evn`** executes the actual installed **4.35.2** resolver on the same unchanged approved candidate. `hexalith-platform.json` exactly reproduces prepared **`114b131d59b9648f56603cbe797c77d90447a713daa153271e7921f49df2db7f`**, active module **`compatible-platform`**, **18 entries**. The command's selected `references/Hexalith.Memories/module/hexalith.module-manifest.v2.json` exactly reproduces native **`bc9396119bf71dbbdb2e8abfa4501468decedd878e295c2a6d6bb17bbc867502`**, **`Hexalith.Memories`**, **17 entries**. Only active module and the extra Platform-root entry differ, besides the derived hash. The preparation caller supplied the wrong manifest. New observer configuration takes the independently resolved selected-manifest hash; no live hash adoption or relaxed comparison is introduced.

**Native startup limit.** Six offline tests reproduce the old observer refusal, pass mapping discovery with the correct expectation and retain wrong-hash/Platform/catalog/replacement refusals. All **36** captured actual model files are identical to the prepared model. A fresh loader-only process loads the actual captured model with installed host/Aspire/Tooling identities and finds the exact `ComposeRun` signature; it invokes no host entry point, composition, builder, native plan, signing key or startup. Nine relevant installed sources are DLL/PDB/checksum-bound. HXP020 came from blank nonzero native preflight output; observer failure **15:53:48.159316Z**, supported down **15:53:48.387282Z**, native terminal **15:53:48.6318229Z**. Matched source shows exact down can kill the run-tagged preflight child and produce this result. Its PID/Started/exit/kill event was not retained, so causation remains unresolved. Model identity/loader success does not prove full composition/preflight or live retention. `MAPDIAG/REPORT.md`, `diagnosis.json`, resolver/loader execution receipts and `captured-model-comparison.json` retain the evidence.

**Concrete replacement plan.** **`MAPPING=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-selected-mapping-20261024-0bzfsk_f`** reuses **`RESOLVER/compatible-platform`** in place, including existing **308 selected-input / 36 model-output / 489 Aspire-runtime** proof. No source copy or rebuild; compilation, observer, guard and graph proof are reused. The observer script remains the original unchanged `RESOLVER/observe-startup.py`; only its new configuration's expected mapping and fresh operational paths change. Platform/Builds/catalog/runner/EventStore/Dapr/PostgreSQL versions, source/Debug, SQLite discovery `cleanupInterval: "0s"` workaround, ACL/mTLS, PostgreSQL cleanup/TTL, all real assertions and resource bounds remain unchanged.

| Affected check | Command / evidence in `MAPPING` | Result |
| :--- | :--- | :--- |
| Selected manifest in fresh context | Exact new prefix plus unchanged installed resolver probe and selected manifest; `selected-mapping-check.json` | Entire mapping equals actual native mapping; **`Hexalith.Memories` / 17 / `bc939611…`**. |
| Guard routes | `python3 -B verify-routing.py`; `docker-routing-checks.json` | **16/16**; private read-only queries/refusal doubles, no shared-engine access. Existing guard parser bytes unchanged outside four isolation constants. |
| Context and plan | `python3 -B -m unittest -v test_preparation_context`; `preparation-context-tests.log` | **13/13**; full user context, every actual route/cleanup prefix, fresh state/evidence, selected manifest/hash and reused candidate/model proof. |
| Genuine installed prerequisite checker | Exact prefix/cwd plus unchanged `CTX/prerequisite-probe/bin/Debug/net10.0/Probe.dll`; `installed-prerequisite-check.json` | **Exit 0**, null diagnostic, real scrubbed child **Aspire 13.6.1**, correct UID/GID/groups/home/cwd/private namespace/client/engine. |
| Fresh preparation | `isolation-receipt.json`, `preparation-owned-processes.json`, `prepared-container-images.json`, `fresh-preparation-state.json` | Seven exact owned preparation process identities, seven matching immutable images, **zero containers/native launches**, absent fresh state/evidence. |

Fresh Docker **29.8.2** engine **`a745268a-dcce-4242-96a7-c056868a39e0`** owns **`unix:///run/hx-retention-0bzfsk_f/docker.sock`**, new data/exec/containerd/cgroup paths and private network/mount namespaces. Keeper **76930 / start 1674967**, daemon **76955 / start 1674976** and their exact descendants are recorded. Reused namespace inode numbers do not imply reuse of a previous engine; new PID/start/socket/engine identities distinguish ownership. Older engines stay stopped and historical resources are excluded. At **16:18:03.619940Z**, preparation observed **14 CPUs**, **97,021,218,816 Docker bytes**, **91,834,994,688 Linux available bytes**, **189,255,393,280 staging / 1,220,271,427,584 backing free bytes**. These are preparation observations; once-before-launch capacity/isolation admission remains mandatory.

`MAPPING/run-plan.json`, `launch-command.txt`, **37** bound operational inputs and `verification.json` retain the complete plan. Native command remains `dotnet tool run hexalith-module -- test --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --profile functional-retention --mode source --evidence artifacts/retention/repair-spec14-selected-mapping-20261024-0bzfsk_f/functional.json --output json`, from the unchanged candidate using the exact new prefix. Fresh state is `MAPPING/native-run`; evidence is owner-relative. Earliest **2026-10-24T16:19:26.377530Z**, latest **2026-10-25T10:27:22Z**, outer end **14:42:22Z**; **one launch / zero retries**, **14400s including 600s startup / separate cumulative 900s stopping**. Supported exact-run down and independent exact owned preparation-engine disposal retain the prior fail-closed rules. **Root's single ordinary changed-input/context review is complete; `readyForOperationalApproval:true`. No unresolved or deferred finding remains; no launch is authorized.** Independent readback at **2026-10-24T16:23:51.644070Z** confirms the complete selected mapping, only the three intended observer-configuration changes, all **37** operational hashes, seven matching images, seven live owned PID/start/namespace identities, zero containers, exact monitor/cleanup/probe prefixes and absent fresh state/evidence/approval/launch. The **36** actual-model correspondence and prior source/observer evidence are reused. `root-review.json` binds the final plan/command; `root-preparation-readback.json` retains the checks. Actual capacity/isolation admission remains once immediately before a separately approved launch. The consumed result and authentic native retained/unverified marker above remain unchanged. Story 27.4, functional retention, full qualification, Production and **A41 remain open**.

### Selected SQLite discovery workaround — 2026-10-24; historical reviewed preparation

**Historical preparation snapshot; this plan was subsequently approved and consumed by the invocation above.** At preparation, the last `b5f8ad4f27345347ca7e857e8b2bfa2f` result below remains withdrawn/130/HXC130 with independently verified cleanup. Its approval is consumed. Fresh **`RESOLVER=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-sqlite-discovery-20261024-qxzlfc5f`** retains this correction and complete replacement plan; preparation baseline is Memories **`7859332ac6d787415329ae65b6013594071e9f9f`**. The spec's historical frontmatter baseline is unchanged.

**Changed behavior.** Platform's selected local `functional-retention` profile changes only the four effective SQLite discovery configurations to `cleanupInterval: "0s"`, after verifying SQLite v1 and the exact run-owned registry. The [pinned garbage collector](https://github.com/dapr/components-contrib/blob/v1.18.0/common/component/sql/cleanup.go) supports a no-op for zero; the [pinned resolver](https://github.com/dapr/components-contrib/blob/v1.18.0/nameresolution/sqlite/sqlite.go) still renews registrations and filters stale addresses. This is a scoped workaround for defective discovery GC, not an upstream fix or a delay past the test. The original runner configuration is retained; Platform merges the correction into the actual sidecar files. Unchanged renewal scheduling, the default five-second freshness window and one-second timeout, registry isolation, declared ACL/mTLS, signed-clock and retention assertions remain. PostgreSQL telemetry `cleanupInterval: "5m"`, TTL, due-index/reminder/purge and 125/125 cohorts remain unchanged. The existing one-launch/no-restart and exact workspace disposal bounds the local discovery registry. Normal profiles are unchanged.

**Source and binary proof.** A fresh physical `RESOLVER/compatible-platform` adopts only the discovery selection call in `PlatformAppModel.cs` and the same `PlatformRetentionProfile.ConfigureDiscovery` method as owning Platform. The older compatible merge code is preserved. **306/308** selected inputs remain identical. Supported installed **4.35.2** resolves genuine Platform **`7021c41bea55bd6c690ba96082cf3dcfc95c9fca`** / Builds **`007bb9bc2806a0ead02d4831555bf2134a38d256`**, no identity findings, mapping **`114b131d59b9648f56603cbe797c77d90447a713daa153271e7921f49df2db7f`**. The actual offline model is **`658f06573cb6b6c126be5fe05dc2ebb77a030d19e65bad138246b3bfbf852802`**, with matching PDB/seven relevant sources and 36 bound output files. EventStore remains **3.127.0**, native Dapr **1.18.1**, PostgreSQL **18.6**, immutable images and source/Debug. Installed tool payload and prior candidates are unchanged. `selected-input-identities.json`, `affected-source-binary-binding.json`, `prepared-model-binding.json` and `verification.json` retain exact evidence.

| Offline check | Command / evidence in `RESOLVER` | Result |
| :--- | :--- | :--- |
| Owning compilation and regressions | Platform cwd: `dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj -c Debug --artifacts-path artifacts/spec14-sqlite-discovery -m:1 --nologo`; built xUnit assembly with five exact method filters listed in `verification.json`; `owning-final-build2.log`, `owning-final-tests2.xml` | Build zero warnings/errors; **10/10** pass. Tests cover all four selected configs, normal run unchanged, independent security renderer comparison, exact registry/refusal and workspace removal. |
| Supported native build and installed-model tests | Reused actual `PlatformManifestCommand.BuildModelAsync` probe; `native-model-build-corrected.log`; `dotnet build installed-api-tests/Tests.csproj -c Debug -m:1 --nologo`, then its built test assembly; `installed-api-build2.log`, `installed-api-tests2.xml` | Native model pass; focused fallback zero warnings/errors and **5/5** pass against the actual model and installed host APIs. |
| Exact resolver behavior | Private Go command in `verification.json`; `go-resolver-runtime-tests.log`, `resolver-workaround-verification.json` | **4/4** subchecks pass using actual v1.18.0 component sources and embedded kit **0.18.1**, mapstructure **2.4.0**, SQLite **1.34.5**. Actual metadata decoding, no-op GC, fresh renewal/lookup and boundary/stale refusal execute against a new in-memory fixture. No sidecar/topology or live backend is used. |
| Existing observer plus generated config admission | `python3 -B -m unittest -v test_observe_startup test_native_host_admission test_discovery_admission`; `observer-final-tests.log` | **40/40** pass: 28 retained cases and 12 config cases. All four actual YAML/JSON configs, exact registry/zero cleanup/default liveness/security are required before A/long wait. Partial files cannot admit; unavailable/replaced proof after capture refuses. |
| Fresh routing/context | `python3 -B verify-routing.py`; `python3 -B -m unittest -v test_preparation_context`; `docker-routing-checks.json`, `preparation-context-final.log` | **16/16** routing and **11/11** context/plan checks pass, including every actual monitoring/cleanup command prefix and fresh owner-relative evidence. |
| Real installed prerequisite and images | `installed-prerequisite-check.json` exact command/cwd; `python3 -B prepare-images.py` | Actual checker diagnostic null, genuine Aspire **13.6.1** child exit **0**; exact fresh namespace/user/engine context. **7/7** immutable images match prior identities, zero containers. |

**Retained failure limits.** The initial fixture/setup failures are retained and corrected; `owning-red-tests3.log` is the meaningful pre-fix failure (selected config lacks zero cleanup, normal case passes). The broad candidate command `dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj -c Debug --artifacts-path artifacts/spec14-sqlite-discovery -p:BuildsToolingAssemblyPath=/home/administrator/.nuget/packages/hexalith.builds.module.cli/4.35.2/tools/net10.0/any/Hexalith.Builds.Tooling.dll -m:1 --nologo`, cwd `RESOLVER/compatible-platform`, exits **1** with **1 warning / 53 errors**: MSB3277 OpenAPI version conflict and CS0234/CS0246 missing Builds references/types in that forced installed-assembly lane. `candidate-test-build.log` remains the broad-lane blocker; the ordinary owning project and focused installed-model fallback pass. No gate, dependency version or installed payload was changed. Initial upstream tests using the component module's older kit are superseded by the final runtime-version-aligned test. Private Go 1.26.3 is SHA-verified test tooling; native Dapr was built with Go 1.26.4 and remains unchanged. No live retention or historical caller attribution follows.

**Concrete replacement plan.** `RESOLVER/run-plan.json` and full `launch-command.txt` have completed the single ordinary root review. Cwd is `RESOLVER/compatible-platform`; fresh state is `RESOLVER/native-run`; fresh owner is that candidate's `references/Hexalith.Memories`; evidence is **`artifacts/retention/repair-spec14-sqlite-discovery-20261024-qxzlfc5f/functional.json`** relative to that owner. The exact prefix enters **`/proc/30223/ns/net`** while privileged, then `runuser -u administrator -- env` with complete intended user context, guarded `DOCKER_HOST=unix:///run/hx-retention-qxzlfc5f/docker.sock`, fresh Docker config, guarded PATH and fresh XDG state. It invokes the unchanged supported command:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-sqlite-discovery-20261024-qxzlfc5f/functional.json --output json
```

The prepared engine is Docker **29.8.2**, ID **`341db0c8-1943-4ded-a2c6-f3670b5e8291`**, keeper **30223**, daemon **30245**, new data/exec/PID/containerd state and seven exact owned PID/start identities. Private slirp disables host loopback; no shared-engine/other-session lifecycle action occurred. Kernel reuse of an old disposed namespace inode is disclosed; no old daemon/socket/state was restarted. **Zero containers/native launches** and absent fresh state/evidence are recorded. The preparation capacity snapshot at **15:35:48Z** had **14 CPUs**, Docker memory **97,021,218,816 bytes**, Linux available **91,836,301,312 bytes**, staging free **191,054,073,856 bytes**, backing free **1,220,469,813,248 bytes**; actual admission checks capacity/isolation once immediately before an authorized launch.

Start window **2026-10-24T15:41:19.298997Z–2026-10-25T10:27:22Z**; outer end **2026-10-25T14:42:22Z**. Preserve **one launch / zero retries**, **14400s execution including 600s startup / separate cumulative 900s stopping**, original seven-container/fifteen-resource graph, 8 CPU/16 GiB/20 GiB floors, PostgreSQL caps and all real 1800/3600/900s assertions. All existing monitor/capture/observer/down routes share the exact fresh prefix. The observer must bind actual generated configs, loaded files, native host/four consoles and complete graph before A/long wait. First loss/detection/stop/terminal anchors stopping. Preserve authentic results, use supported exact-run `down` for an actual new run ID only, verify empty engine, then dispose exact owned daemon/slirp/keeper and verify process/socket/namespace/workspace absence within the same allowance. An early prerequisite refusal with no run ID needs no invented down identity. Old retained resources stay excluded.

Root's single ordinary review is complete. All three findings (independent security comparison, partial-config publication and inherited draft routes) are resolved, with none unresolved or deferred. Independent readback at **2026-10-24T15:47:00.557731Z** verifies the correct empty engine, seven unchanged images, all seven live owned PID/start/namespace identities, 37 unchanged operational inputs, 36 model outputs, seven source/PDB bindings, matching monitor/cleanup prefixes and actual installed-probe context, and absent fresh native state/evidence/approval/launch. `root-review.json` binds the final plan/command; `root-preparation-readback.json` retains this preparation check. Actual capacity/isolation admission remains once immediately before an approved launch. **At preparation, one operational authorization remained and no native run had started; the consumed invocation above supersedes that state.** `verification.json` records the full command/result inventory and 37 bound operational inputs. Local retention, Story 27.4, full qualification, Production and **A41 remain open**.

### Docker-guard corrected invocation — 2026-10-24; withdrawn after SQLite resolver failure, cleanup verified

The owner's **“approve”** authorized the reviewed plan once at **13:49:36.146279Z**. Private **`GUARD=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-docker-guard-20261024-lejuikdo`** retains that approval and its unchanged plan/command hashes. Admission passed **13:49:37.858056Z**: **308 selected inputs / 36 model outputs / 19 operational inputs / 489 Aspire runtime files**, all seven immutable images, installed tool/native Dapr hashes, private engine/process identities, capacity and absent new state/evidence. The exact reviewed command below launched once at **13:49:37.990865Z**, native run **`b5f8ad4f27345347ca7e857e8b2bfa2f`**. The approved stack, source/Debug, assertions, resource limits and **14400s execution / 600s included startup / separate cumulative 900s stopping** stayed unchanged.

| Native evidence | Actual result (UTC) |
| :--- | :--- |
| Startup | All **15 resources** healthy; native startup **110.5548725s**. Complete input/console/AppHost/graph admission persisted **13:51:28.723956Z**, before A and the long wait. Observer completed successfully. |
| Cohort A | Accounting **13:51:30.163Z**: **125** attempted, enqueued, acknowledged, persisted and transaction acknowledgements; **zero** conflicted, rejected or dropped. |
| Cohort B | Accounting **14:21:30.134Z**: the same **125/125** and zero-error counters. Accounting observations are not the emission UTC/monotonic clocks; their timestamp difference does not independently establish the required separation. |
| Resolver failure | EventStore Dapr console reports four removed registrations at **14:49:59.608869772Z**, renewal failure and fatal **`Host registration lost`** at **14:49:59.610859502Z**. Native resource **PID 14608** reports **Finished / exit 1 / stop 14:49:59.614183Z**. |
| Withdrawal | Graph monitor detects `eventstore-dapr-cli` loss **14:50:00.662516Z**; operator stopping starts **14:50:00.831182Z**. Authoritative native selected result is **withdrawn / HXC130**, terminal **14:50:01.0305199Z**; CLI returns **130** (its envelope says `failed`). No native TRX or passing `functional-observations.json` was produced; native test counts are unreported. |

**Immediate cause and bounded mechanism proof.** The retained native fatal log and reported exit identify this run's resolver failure. AppHost SIGTERM/stopping follows operator stopping, so it does not establish an earlier external killer. All four actual generated Dapr configurations select **SQLite v1** with the same run-owned database and no interval overrides. The executed **Dapr 1.18.1** binary embeds **components-contrib v1.18.0**. That exact [resolver source](https://github.com/dapr/components-contrib/blob/v1.18.0/nameresolution/sqlite/sqlite.go) deletes registrations whose age is **less than** the update interval and makes a zero-row renewal fatal; the [metadata defaults](https://github.com/dapr/components-contrib/blob/v1.18.0/nameresolution/sqlite/sqlite_metadata.go) are **5 seconds update / 1 hour cleanup**. An isolated Python/SQLite in-memory fixture using the exact source SQL removed both fresh rows, retained the boundary/stale rows, then renewed zero live rows. This supports the reversed cleanup predicate as the mechanism. No post-cleanup live database snapshot was retained; earlier runs are not retroactively attributed. `sqlite-name-resolution-diagnosis/source-and-sql-reproduction.json` retains source URLs/hashes, executed-binary hash/module version, exact SQL and actual fixture results. No live database or runtime configuration was modified.

**Disposition, including the first error.** Initial same-prefix supported `down --run-id b5f8ad4f27345347ca7e857e8b2bfa2f` returned **3 / HXR028**, `stdin-listener` / `blocked-after-stop`. Subsequent supported disposal verification for that **same run**, inside the same stopping allowance, returned **0 / HXI001** at **14:51:18.592920Z**; this was not a topology launch or startup retry. The engine was empty before its exact owned daemon/slirp/keeper were stopped. Engine/process/socket/private-namespace absence was verified **14:51:51.548357Z**, **110.889808s** from detected loss. Final independent verification at **14:54:40.726118Z** found all **38** recorded native/collector/launcher/down PID/start identities absent, capture children stopped and workspace/native state absent. Total monotonic charge from detected loss was **280.073106s**; the conservative UTC charge from the earlier reported sidecar exit was **281.111935s**, both within **900s**. The authoritative native record still says **`resourceDisposition: remaining`** with **empty sets / unverified:false**; preserve that marker alongside independent verified disposal, without rewriting it to `removed`.

`GUARD/run-result.json`, `final-disposition.json`, `private-engine-disposition.stdout.json`, `native-selected-test-final.json`, both supported-down receipts and `before-disposal` / `after-supported-disposal` retain the result and original captures. `GUARD/native-run/runtime-inputs/b5f8ad4f27345347ca7e857e8b2bfa2f` retains generated inputs; `native-run/startup-admission-ordering.json` and `owned-graph-loss.json` retain timing proof. The private daemon is stopped, the **one approval is consumed**, and no retry/restart or further launch is authorized. Earlier resources, other sessions and shared engines were untouched.

**Local retention remains unproved:** both cohorts were accounted for, but final pre-expiry/expiry/purge/reminder and unchanged-newer-cohort evidence is absent. Story **27.4 remains in progress**; full qualification, Production and **A41 remain open**. Next repair owner is **Platform/Builds**, where `CompositionDaprComponentRenderer` selects the local resolver and `PlatformAppModel` merges that configuration. This diagnosis changes no dependency, assertion, security setting or cleanup interval. The following preparation is a historical snapshot of the consumed run.

### Docker argument-boundary correction — 2026-10-24; historical reviewed preparation

The owner subsequently approved this complete reviewed `GUARD/run-plan.json` at **2026-10-24T13:49:36.146279Z**, bound in `GUARD/approval.json`. The single invocation and verified disposal above consumed that authorization. The plan remains the original hash-bound preparation snapshot; its `launchAuthorized:false` field was not rewritten, and the separate approval receipt records the decision. No further launch is authorized.

Fresh private **`GUARD=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-docker-guard-20261024-lejuikdo`** corrects a demonstrated operator input after consumed run **`12e991cbd8284e5784656fe0d11d28e5`**. The original private Docker guard rejected routing-like tokens anywhere in argv. The unchanged actual candidate passes **Sentry `--config`**, **PostgreSQL server `-c`**, and **read-only `psql -c`** after its image/container boundary. Executing the exact preserved guard with controlled OS/client boundaries rejects all three before any Docker client call. This is a deterministic defect compatible with the observed Sentry/dependency startup failure; actual failed-run DCP argv/guard stderr were not retained, so it is **not established as the sole historical cause**. The earlier one-hour AppHost-loss cause remains unresolved.

The corrected **`GUARD/client-bin/docker`** consumes known pinned-client option values before the **IMAGE** (`create`/`run`) or **CONTAINER** (`exec`) boundary, then preserves the payload unchanged. Unknown pre-boundary options and routing overrides still fail closed. Namespace, explicit endpoint/configuration, no ambient context, engine identity and bounded identity probe remain enforced. Only this existing private operator guard and fresh preparation routes change; the candidate, installed tool, native runner, source/Debug outputs, dependencies, real assertions and security/resource/lifecycle/retry behavior remain unchanged. The original consumed guard and all prior failed evidence remain intact.

| Verification | Exact command / authentic result |
| :--- | :--- |
| Guard and actual pinned-client parsing | Cwd `GUARD`: `python3 -B -m unittest -v test_docker_guard test_real_docker_parser` — **40/40**, **3.662s**, `fresh-bound-guard-tests.log`. Includes original rejection, corrected Sentry/PostgreSQL/psql shapes, option-value/cluster/delimiter handling, pre-boundary override/unknown/missing-argument refusal and namespace/endpoint/configuration/engine refusal. Five cases run actual Docker **29.8.2** against a private Unix-socket double which always refuses POST; it cannot create resources and receives no start/image request. Payload JSON retains exact arguments and PostgreSQL CPU/memory caps. |
| Fresh actual routing/refusals | Cwd `GUARD`: `python3 -B verify-routing.py` — **16/16**, `docker-routing-checks.json`; actual read-only private-engine queries plus refusal doubles, no shared-engine access. |
| Fresh context/plan | Cwd `GUARD`: `python3 -B -m unittest -v test_preparation_context` — **9/9**, **0.005s**, `fresh-context-tests.log`. |
| Actual installed checker | Exact new prefix/cwd plus the unchanged `CTX/prerequisite-probe/bin/Debug/net10.0/Probe.dll`, retained in `installed-prerequisite-check.json` — **exit 0**, **null checker diagnostic**, genuine Aspire **13.6.1**. The actual installed scrubbed child proves UID/GID **1000**, intended home/user/cwd, new private namespace/client/socket/engine. Installed payload/probe hashes remain bound; no probe or candidate rebuild. |
| Existing proof reused | Unchanged **308 selected inputs / 36 native model outputs / 489 Aspire runtime files**, prior owning/installed capture **17/17** each, observer **28/28**, graph **16/16**, lifetime **7/7**. Three actual candidate argument-source checksums still match retained selected inputs; `affected-input-binding.json` and **19** current operational inputs are retained. Actual input/capacity/freshness admission remains once immediately before an authorized launch. |

The ordinary root review identified one verified omission: the first correction handled create/run but still rejected **exec's psql `-c`**. Exec/container exec parsing and its exact native/physical-read regressions now pass, including the real-client payload check. Root's single ordinary behavior/plan review is complete, with no unresolved or deferred finding. `root-review.json` records the result; `root-preparation-readback.json` independently verifies all seven images, zero containers, seven owned PID/start/namespace identities, 19 operational inputs, the actual installed checker context and absent fresh state/evidence at **2026-10-24T13:45:55Z**. This preparation review is separate from once-before-launch admission. `guard-diagnosis.json` and `verification.json` retain evidence, commands and limits. Two initial test-fixture errors (exception tuple/list storage and an incomplete fake exec-inspect response) are retained separately; neither is claimed as production failure. No compilation is needed for this private Python guard/configuration change.

**Fresh independent engine.** Docker **29.8.2**, ID **`2880e4ca-c119-47c2-bf1d-b321a6004a07`**, owns **`unix:///run/hx-retention-lejuikdo/docker.sock`**, new `GUARD/engine-data`, private `/run/hx-retention-lejuikdo/exec`, PID/containerd state and private slirp with host loopback disabled. Keeper **97205 / start 740243**, daemon **97227 / start 740247** and all **seven** exact owned process identities are retained. Network **`net:[4026532296]`** is private; daemon mount **`mnt:[4026532958]`** is private. Kernel reuse of disposed namespace inode numbers is disclosed; fresh PID/start/socket/engine/data/exec identities distinguish this engine, and stopped CTX/EXIT daemons were not reused. No host firewall/routes/system package/service changed. All **seven** immutable images match prior identities; **zero containers/native invocations**, absent fresh state/evidence. Preparation observes **14 CPUs / 97,021,218,816 Docker bytes / 92,366,737,408 Linux available bytes**, **202,210,566,144 staging / 1,220,073,947,136 backing free bytes**. These are preparation observations, not launch admission.

**Concrete proposed attempt.** `GUARD/run-plan.json` and **`GUARD/launch-command.txt`** contain the complete resolved prefix, command, routes, ownership and cleanup. Cwd remains **`EXIT/compatible-platform`**; owner remains **`EXIT/compatible-platform/references/Hexalith.Memories`**. Fresh state is **`GUARD/native-run/local-share`**; fresh evidence is owner-relative **`artifacts/retention/repair-spec14-docker-guard-20261024-lejuikdo/functional.json`**. The full prefix is privileged **`nsenter --net=/proc/97205/ns/net` → `runuser -u administrator -- env`**, with explicit intended-user context, guarded client, new `DOCKER_HOST`/`DOCKER_CONFIG` and fresh `XDG_DATA_HOME`, reused unchanged Dapr home and removed ambient Docker context/TLS variables. Under that exact prefix:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-docker-guard-20261024-lejuikdo/functional.json --output json
```

Proposed start **2026-10-24T13:45:00Z–2026-10-25T10:27:22Z**, checked feasible **2026-10-24T13:42:29Z**. Preserve **one launch / zero startup retries / no restart**, **14400s execution including 600s startup**, separate cumulative **900s stopping**, execution/disposal ends **2026-10-25T14:27:22Z / 14:42:22Z**. Keep **4.35.2 / EventStore 3.127.0 / Dapr 1.18.1 / PostgreSQL 18.6**, source/Debug, **125/125**, real **1800/3600/900s**, every strong-read/stored-row/TTL/due-index/reminder/newer-record/signed-clock assertion, **7 containers / 15 resources**, **8 CPU / 16 GiB Docker and Linux / 20 GiB staging and backing** floors and unchanged PostgreSQL caps. No expired window or consumed approval is reused.

All native CLI/Aspire/DCP/selected-test/capture/observer/graph-gate/cleanup routes share the new exact prefix. Existing MON collectors and unchanged startup observer are reused with fresh paths; require actual loaded-input/all-four-console/native-host/complete-graph admission before the long wait and continue graph checks at **≤30s**. Any loss/failure/terminal/deadline anchors evidence preservation and the same cumulative **900s** disposal. With an actual new native run ID, use same-prefix supported **`down --run-id <actual-new-run-id>`**, preserve authentic markers, and verify empty exact-run resources/workspace. A real prerequisite refusal without a run ID needs no invented down identity. Stop only PID/start-checked fresh daemon/containerd/slirp/keeper and capture/observer descendants; verify empty engine plus socket/process/private-namespace absence. An unused withdrawn/expired plan likewise requires bounded stopping of only this owned empty preparation. Prior resources and other sessions remain excluded.

**Historical preparation only:** the authorized invocation above consumed this plan and its one launch; the private engine is now stopped. No further launch is authorized. Story 27.4 stays **in progress**; local retention, historical process-loss attribution, full qualification, Production and A41 remain **open**.

### Corrected-context native invocation — 2026-10-24; failed startup, cleanup verified

The owner replied **“yes”** for the complete reviewed `CTX/run-plan.json`, bound in `CTX/approval.json`. Its single invocation began **2026-10-24T13:15:40.844234Z**, actual native run **`12e991cbd8284e5784656fe0d11d28e5`**. Admission passed at **13:15:40.709066Z**: **308 selected inputs / 36 model outputs / 18 operational inputs / 489 Aspire runtime files**, all seven immutable image identities, private engine/process/start identities and inspectors, capacity and absent new state/evidence. Captures and startup observer were ready first. The exact reviewed command, cwd and corrected `nsenter` → `runuser` environment were used. The genuine Aspire prerequisite passed; the native runner created its own plan and AppHost. The approved stack, source/Debug mode, real assertions and resource limits remained unchanged.

**Authentic outcome:** native **exit 3 / HXR025**, terminal **13:16:10.2928079Z** (CLI exit detected **13:16:10.500999Z**), startup duration **29.1392963s** within the unchanged **600s** bound. Dapr Sentry, Placement and Scheduler reported **FailedToStart**; EventStore and dependent services failed or remained waiting, and PostgreSQL was still Starting. Complete graph/startup admission was never reached. The native record reports **tests not reported / zero executed**, with **zero cohort submissions, no native TRX and no passing `functional-observations.json`**. Retention, expiry/purge and newer-record preservation remain unproved. Three exact-run container IDs were observed by the collector; this is not a claim that the whole seven-container graph ran.

**Native capture and limits:** the actual AppHost **PID 80528** has private capture-started evidence at **13:15:57.088112Z**, followed by reported resource failures around **13:16:10.05Z**, application stopping, observed **SIGTERM**, application stopped and reported process exit **3** at **13:16:10.7570846Z**. All four native sidecar console/state streams persist dependency-startup failures. These records improve attribution evidence but do not expose the initiating caller or the underlying Dapr control-plane startup error. The retained AppHost log still contains fixed output-observed markers. Observer `unsafe-or-missing-input` and graph container-kill receipts follow native failure/teardown; they do not establish an initiating cause. No lifetime/security/assertion fix or causal attribution to the historical one-hour AppHost loss is inferred.

**Disposition:** supported same-prefix **`down --run-id 12e991cbd8284e5784656fe0d11d28e5`** returned **exit 0** at **13:16:12.571932Z**. The authoritative retained native `selected-test.json` preserves terminal **failed/HXR025** and now records **removed / empty container and process sets / unverified:false**. Original owner `functional.json` preserves its earlier **retained / unverified:true** snapshot; it was not rewritten to claim disposal. Both originals and a final native snapshot are retained. The new native state and workspace are absent; actual private-engine `ps -aq` was empty before stopping. Collectors/observer and children, native AppHost/CLI/down, exact daemon/containerd/slirp/keeper and launch parents are stopped. Privileged scan finds no private-namespace process and the private socket is absent. Disposition verified **2026-10-24T13:18:41.634865Z / monotonic 6273.258006244**, charging **180.797185s** conservatively from the original invocation, inside the cumulative **900s**. No escalation, shared-engine access, old-resource disposal, Production action or other-session interruption occurred.

`CTX` retains exact command/approval/admission/native output, generated inputs, startup diagnostics, complete final native captures, original failed markers, supported-down receipts, `stopping-budget.json`, `final-disposition.json` and `run-result.json`. One initial post-down state read found the already removed state file; the retained native terminal/disposition record was then read without a second lifecycle action. The approved **14400s execution / 600s included startup / separate cumulative 900s cleanup** and outer end **2026-10-25T14:42:22Z** were preserved. **The one approval is consumed. No retry, restart or further launch is authorized; the private daemon is stopped.** The preparation below is a historical reviewed snapshot. Story 27.4 remains **in progress**, A41 **open**, historical AppHost-loss attribution unresolved; full qualification and Production remain separate. Next owner: Platform/Builds for this independent-engine Dapr control-plane startup failure, using the retained actual diagnostics.

### Corrected user context and fresh engine — 2026-10-24; reviewed preparation snapshot

Fresh private **`CTX=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-user-context-20261024-biuqwy81`** corrects the demonstrated operator user-context defect and reuses unchanged **`EXIT/compatible-platform`**, source/Debug outputs, resolver/mapping and prior capture/assertion evidence. Original consumed command, plan and refusal evidence remain preserved. No native retention/topology test or handoff was invoked.

**Actual installed prerequisite proof.** `CTX/prerequisite-probe/Program.cs` calls the installed **4.35.2** `CompositionPrerequisiteProbe.ProbeAspireAsync("aspire", TimeSpan.FromSeconds(30), CancellationToken.None)` directly, and separately records its real `CompositionProcess.CreateStartInfo("aspire", ["--version"], null, null)` / bounded `RunAsync` version child. The probe uses no wrapper, mock, synthesized output or changed installed payload. `installed-prerequisite-binding.json` binds three release sources at **`4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`** to the matching installed PDB, records installed DLL/PDB/CLI hashes and proves the actual loaded Tooling copy has the installed hash. The exact probe invocation and cwd are retained in `installed-prerequisite-check.json`.

At **2026-10-24T13:01:26.181169Z**, inside the new private network namespace, the actual checker returns **diagnostic null**, and its genuine scrubbed version-child path returns **exit 0 / `13.6.1+3751e615ad660621f2a7aa4c390ef48e08a0eed8`**, with empty stderr, no timeout or truncation. The installed scrubbed context child proves UID/GID **1000**, supplementary groups, **`HOME=/home/administrator`**, **`USER=LOGNAME=administrator`**, the exact retained candidate cwd, guarded Docker client, explicit private endpoint/configuration and new engine identity. The corrected prefix is **privileged `nsenter --net=/proc/75911/ns/net`, then `runuser -u administrator -- env`**, with explicit intended-user environment; no bare UID/GID-switch substitute. The installed allowlist drops `SHELL` and `XDG_DATA_HOME` from the version child while preserving the proven home/user/routing inputs. That real behavior is recorded, with no bypass. Version success does not establish topology readiness or retention.

**Fresh independent engine.** Docker **29.8.2**, ID **`8f84f434-573e-4dfb-8f63-b8c8b6e72aab`**, owns **`unix:///run/hx-retention-biuqwy81/docker.sock`**, **`CTX/engine-data`**, **`/run/hx-retention-biuqwy81/exec`**, daemon PID file and private managed containerd socket/namespaces. Namespace keeper **75911 / start 512795**, daemon **75936 / start 512805** and containerd **75963 / start 512826** are bound in `isolation-receipt.json` / `preparation-owned-processes.json`; seven exact owned launcher/child identities include slirp. Network **`net:[4026532296]`** differs from host **`net:[4026531840]`**, daemon mount **`mnt:[4026532958]`** is private, cgroup v2 is mounted privately, and slirp disables host loopback. The kernel reused the disposed old namespace inode number; new keeper/start/engine/socket/data/exec/containerd/mount ownership binds this fresh engine, and the stopped EXIT daemon was not reused or restarted. No host firewall, route, system package or service was changed; shared engines were not contacted.

All seven immutable images were pulled and inspected on this fresh engine; their complete identities match prior evidence (`prepared-container-images.json`). **Zero containers were created.** The native state **`CTX/native-run`** and owner-relative evidence **`artifacts/retention/repair-spec14-user-context-20261024-biuqwy81/functional.json`** remain absent. The owner remains **`EXIT/compatible-platform/references/Hexalith.Memories`**. `client-routing-map.json` assigns the same exact prefix/cwd/endpoint/guard to native CLI, Aspire, DCP, selected test, both captures, observer, graph gate and cleanup. The guarded client verifies namespace and engine ID, requires explicit correct endpoint/configuration and refuses context/host/config overrides. Direct SDK clients receive the same explicit endpoint; no live DCP/client topology is claimed.

| Focused verification | Command / authentic result |
| :--- | :--- |
| Actual installed checker probe build | `dotnet build CTX/prerequisite-probe/Probe.csproj --configuration Debug -m:1 -p:NuGetAudit=false`: **exit 0, 0 warnings/errors**, `prerequisite-probe-build.log`. Exact absolute command retained in `verification.json`. |
| Actual checker and scrubbed child | Exact corrected prefix plus `dotnet CTX/prerequisite-probe/bin/Debug/net10.0/Probe.dll`, cwd `EXIT/compatible-platform`: **exit 0**, checker diagnostic null, genuine Aspire **0 / 13.6.1**, private engine/user/cwd proof; full code/command/stdout in `installed-prerequisite-check*`. |
| Routing and refusal paths | `python3 -B CTX/verify-routing.py`: **16/16**, including missing/wrong endpoint/configuration, ambient context, endpoint/config overrides, wrong engine, wrong namespace and raw-client-not-called refusal doubles. Actual read-only calls target the private engine only. |
| Corrected context and concrete plan | Cwd `CTX`: `python3 -B -m unittest -v test_preparation_context`: **9/9 in 0.005s**, no failures/errors/skips, `focused-context-tests.log`. Checks actual installed results, user/namespace/routing, fresh paths, unchanged observer sources, launch command, cleanup and original window/assertions. |
| Unchanged evidence reused | Owning and installed capture **17/17** each, observer **28/28**, MON graph **16/16**, existing selected 308-input / 36-output and 489 Aspire-file identities; no unrelated rebuild or census. `verification.json` links exact retained proof. |

Initial preparation failures are retained separately: a receipt read lacked traversal of the newly root-owned runtime directory after the daemon was already ready (corrected to root:administrator **0710**, no restart), and the offline probe first failed **CS0411** on a dictionary lookup (probe-only correction; original build log retained). Current `dotnet-inspect` rejected its documented `--oneline` option; supported `--library -m` inspection succeeded. These are preparation errors, not live startup or retention outcomes.

**Concrete proposed command and bounds.** `CTX/run-plan.json` contains every resolved environment, command, owner, route, resource and cleanup bound; **`CTX/launch-command.txt`** contains the full privileged namespace / runuser / explicit environment prefix. Cwd stays **`EXIT/compatible-platform`**. The exact command is:

```bash
cd /home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-native-exit-20261024-rdaale7k/compatible-platform
umask 077
sudo -n nsenter --net=/proc/75911/ns/net runuser -u administrator -- env -u DOCKER_CONTEXT -u DOCKER_TLS_VERIFY -u DOCKER_CERT_PATH DOCKER_HOST=unix:///run/hx-retention-biuqwy81/docker.sock DOCKER_CONFIG=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-user-context-20261024-biuqwy81/docker-client-config PATH=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-user-context-20261024-biuqwy81/client-bin:/home/administrator/.dotnet:/home/administrator/.aspire/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin DOTNET_CLI_HOME=/home/administrator DOTNET_ROOT=/home/administrator/.dotnet NUGET_PACKAGES=/home/administrator/.nuget/packages ASPIRE_CONTAINER_RUNTIME=docker XDG_DATA_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-user-context-20261024-biuqwy81/native-run/local-share HEXALITH_DAPR_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home DOTNET_CLI_TELEMETRY_OPTOUT=1 HOME=/home/administrator USER=administrator LOGNAME=administrator SHELL=/bin/bash dotnet tool run hexalith-module -- test --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --profile functional-retention --mode source --evidence artifacts/retention/repair-spec14-user-context-20261024-biuqwy81/functional.json --output json
```

Proposed start **2026-10-24T13:15:00Z–2026-10-25T10:27:22Z**, checked feasible **13:06:05Z**. Keep **one launch / zero startup retries / no restart**, **14400s execution including 600s startup**, separate cumulative **900s stopping**, absolute execution/disposal ends **2026-10-25T14:27:22Z / 14:42:22Z**. No expired window is extended. Preserve **Platform/Builds 4.35.2 / EventStore 3.127.0 / Dapr 1.18.1 / PostgreSQL 18.6**, source/Debug, **125/125**, real **1800/3600/900s** and every strong-read/stored-row/TTL/due-index/reminder/newer-record/signed-clock assertion. Keep **7 containers / 15 resources**, **8 CPU / 16 GiB Docker and Linux / 20 GiB staging and backing** floors, existing PostgreSQL **8 CPU / 16 GiB including swap / 100 connections / pool 40 / 400 GiB tmpfs maximum** caps. Preparation at **13:04:07Z** observes **14 CPUs / 97,021,218,816 Docker bytes / 92,291,543,040 Linux available bytes / 203,961,651,200 staging / 1,220,084,072,448 backing free bytes**, zero containers and seven images. Actual capacity/isolation/frozen-input/freshness admission remains once immediately before an authorized launch.

The plan gives exact retained collector commands, fresh observer configuration, same-prefix graph gate and **≤30s** monitoring of graph health, first loss, exact engine/process/namespace/slirp survival and bounded logs. Start captures/observer only after authorization, record their actual PID/start identities and require ready; retain loaded-model/Server, all-four-console/native-host/complete-graph proof before the long wait. After any loss, failure, terminal, stopping or deadline, preserve actual evidence and begin cumulative cleanup from the earliest applicable anchor. **If prerequisite refusal creates no native run ID/state/plan, `down` is inapplicable and no identity is fabricated.** Otherwise use same-prefix supported **`down --run-id <actual-new-run-id>`** and preserve authentic terminal/disposition markers. Verify empty private engine, stop only exact PID/start-checked daemon/containerd/slirp/keeper and capture/observer identities, and prove socket/namespace/process/resource absence inside the same **900s** and absolute end. On withdrawal or an unused expired launch window, dispose only these owned empty preparation resources; all prior retained resources remain excluded.

**Historical preparation snapshot:** root's single ordinary review completed with no unresolved or deferred findings. The later one approved invocation and verified stopped-engine disposition are recorded above; no further launch is authorized. `CTX/root-review.json` records the **2026-10-24T13:09:01Z** read-only review: exact probe/launch prefix and cwd agree, the loaded DLL matches installed 4.35.2, every monitor/cleanup route agrees, the unchanged assertions/resource limits are preserved, the private engine still has **zero containers / seven images**, all **seven** recorded PID/start/namespace identities match, and fresh state/evidence remain absent. UTC remains before the existing latest start; once-before-launch admission remains required after approval. Current File List is this existing report and Spec 14; private operational/probe/verification artifacts live under CTX. No candidate, dependency, Git index/ref, other session or Production change occurred. Overall Story 27.4 stays **in progress**; local retention, historical AppHost-loss attribution, full qualification, Production and A41 remain **open**.

### Independent-engine invocation — 2026-10-24; HXR015 before startup, cleanup verified

The owner's **“oui”** authorized the complete reviewed `EXIT/run-plan.json` once; `EXIT/approval.json` binds the plan digest. The exact supported command in `EXIT/launch-command.txt` was invoked from `EXIT/compatible-platform` at **12:31:19.420986Z / monotonic 3430.942646755**, under the approved private-engine namespace/environment. The once-before-launch admission passed at **12:30:01.384634Z**: **308** selected inputs, **36** native model outputs, **12** operational inputs and **489** same-version Aspire runtime files matched, along with inspectors, installed 4.35.2/native Dapr, seven immutable image IDs, private engine/namespace/process-start identities, capacity and absent new state/evidence. Both collectors and the startup observer were ready at **12:30:38.177552Z**. A preceding tool PTY allocation request failed before creating any process; its receipt is separate and was not a native retry.

**Authentic result:** native command **exit 2 / HXR015**, `PrerequisiteUnavailable`, because `aspire --version` returned **exit 20 / empty stdout**, expected **Aspire.AppHost.Sdk/13.6.1**. The native Aspire log at **12:31:21Z** records `ArgumentException` for an empty path in `Path.GetFullPath` / `Aspire.Cli.CliExecutionContext`. Prerequisite refusal occurred before native plan/run-state/run-ID/AppHost/container creation. Consequently there are **zero created containers, zero cohort submissions, no native TRX and no passing observations**. Native AppHost exit capture was not exercised; the historical process-loss cause and retention remain unproved. The approved stack/assertions/resources were not changed to bypass admission.

**Bounded offline diagnosis:** `aspire-prerequisite-context-reproduction.json` reproduces exact **20 / empty stdout** with the approved bare UID/GID-switch context, substituting only the current namespace after the private namespace was removed. The same installed Aspire returns **0 / 13.6.1+3751e615ad660621f2a7aa4c390ef48e08a0eed8** in the intended user's complete context. `aspire-prerequisite-context-correction.json` verifies the narrow recipe `sudo nsenter --net=<selected namespace> runuser -u administrator -- env <same approved environment> aspire --version`, tested only with the current namespace: **0 / 13.6.1**. This is a demonstrated prerequisite-context defect in the prepared operator command; the exact empty folder remains unidentified. It is unrelated evidence to the previous graph-wide loss. The initial probe without any namespace was refused by `nsenter` and is retained separately, not counted as a causal reproduction. The original consumed launch command/plan remains unchanged; no corrected private-engine topology or second native test invocation occurred. The preparation's version/help/manifest checks did not exercise this actual Aspire prerequisite child.

**Disposition:** verified **2026-10-24T12:33:08.534820Z / monotonic 3540.061142369**. The private engine was empty before stopping. Collectors/observer and their children, exact private daemon/containerd, slirp and namespace keeper/launch parents are stopped; privileged scan finds no process in the private namespace and the private socket is absent. No native plan/state/workspace was created. Supported `down --run-id` is inapplicable without a real native run ID; none was invented. `final-disposition.json` records **unverified:false**, with **109.118481s** total stopping charged conservatively from the original invocation, within **900s**. Private source/configuration/images/logs and previous evidence remain retained. Old resources, shared engines, other sessions and Production were untouched.

Private `EXIT/native.stdout.json`, `native-terminal.json`, `prerequisite-logs`, `live-execution.json`, `live-admission.json`, `pre-engine-stop.json`, `final-disposition.json` and `run-result.json` retain exact commands, native error, actual timestamps, diagnosis, limits and disposition. **This one launch approval is consumed. No retry, restart or further launch is authorized.** Local functional retention and Story 27.4 remain **in progress**, full qualification/Production separate and A41 **open**. The prepared daemon is now stopped; the preparation below is a historical reviewed snapshot, not a ready-to-launch current engine.

### Native AppHost exit capture and independent-engine preparation — 2026-10-24; reviewed

Private **`EXIT=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-native-exit-20261024-rdaale7k`** contains the completed offline correction and concrete `run-plan.json` / `launch-command.txt`. No retention/topology or test container has been launched. The private daemon has **zero containers** and all seven unchanged immutable images prepared. Root's **one ordinary review is complete**, with one corrected capture-disposal finding and none unresolved/deferred; the proposed run still needs **one new operational authorization**. Historical launch permissions and the expired shared-engine reservation remain consumed.

**Diagnosis.** The installed AppHost/Tooling DLL/PDB hashes still match executed **4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0**. All **11** narrowly selected release sources match their actual PDB checksums (`installed-release-pdb-binding.json`). The retained `OBS` run had a four-hour execution deadline; its roughly one-hour process loss and Parties overlap still establish no initiating actor. `native-exit-diagnosis.json` preserves the exact path matrix and limits:

| Native path | Matched source and controlled evidence | Attribution limit |
| :--- | :--- | :--- |
| Healthy host wait / shutdown | `AppHostRunner` waits for host shutdown without a one-hour timer. Actual installed normal-exit lifecycle check calls bounded stop once. | Historical shutdown notification/caller absent. |
| Stdin / parent EOF | Actual installed `StopSignal`: retained EOF and ignored-line/EOF do not stop; nonretained EOF and explicit retained `stop` do. Four checks pass. | Historical input bytes / IO failure absent. |
| Owner cancellation / execution budget | Matched `ModuleTestExecutionBudget`, `ModuleCommandExecutionService` and `CompositionProcess` preserve the actual **14400s** deadline and distinguish owner withdrawal. Controlled public host cancellation is captured. | Historical cancellation source unavailable. |
| Renewal / watchdog | Selected gate renews every five minutes; no one-hour AppHost timeout was found in the inspected matching paths. DCP monitors the AppHost PID, explaining dependent loss after parent exit without identifying its cause. | An unlogged external watchdog/manager cannot be excluded. |
| Supported down / escalation | Actual engine writes stdin `stop` for its session or sends TERM to a recorded process, then uses its existing bounded owned-tree escalation. Installed explicit-stop lifecycle and real controlled SIGTERM checks pass. | Historical signal/caller/escalation unavailable. |
| Child failure / abrupt death | Real public owned-child exit **23** is captured; foreign/secret properties excluded. Controlled SIGKILL retains start and explicit unavailability, with no invented exit callback. | Managed hooks cannot identify SIGKILL caller or recover an unavailable parent-reaped exit code. |

**Correction.** Owning Platform adds `PlatformNativeHostDiagnostics` through the existing selected-profile `PlatformDaprDiagnostics` registration, without changing installed 4.35.2 or native lifetime, retry, security or retention behavior. The retained **`diagnostics/native-sidecars/apphost.lifecycle.jsonl`** observes native AppHost PID, UTC/monotonic elapsed time, process exit when delivered, host stopping/stopped, observed POSIX signals and allowlisted public owned-resource terminal/PID/exit data. It never suppresses signals or records arguments, environment, exception text, output or arbitrary properties. The private tail is **65536 bytes**, with **32** terminal-resource observations before an explicit truncation marker and **128** other observations plus final process exit. Process hooks remain until actual process exit, after DI disposal. Caller, cancellation/watchdog source and uncatchable-death limits are explicit. No historical cause or complete native shutdown is inferred from a missing callback.

The fresh physical **`EXIT/compatible-platform`** adopts this capture, its necessary existing registration and the reviewed capture-disposal correction below; **306/307** prior selected runtime inputs are unchanged, and the new capture source makes **308** inputs. Two focused test files also change in owning Platform. Installed 4.35.2's actual offline native model builder passes with genuine Platform **`7021c41bea55bd6c690ba96082cf3dcfc95c9fca`** / Builds **`007bb9bc2806a0ead02d4831555bf2134a38d256`**, source mapping **`b1391e2e57d936d35f7645c72a920ac7f447aaa853c1d228320fc5329408f402`**, and EventStore **3.127.0.0**. Native model SHA-256 is **`de8220b1e6ba681b2ad6e9229a7d53ac5b91042eb0576c68b36aa9e9fd68c55d`**; its actual matching PDB binds all **six** capture/registration sources. `affected-source-binary-binding.json`, `prepared-model-binding.json` (**36 files**) and `selected-input-identities.json` retain proof. Existing Server correction/assertions are reused. Missing Aspire runtime prerequisites were restored at the same pinned **13.6.1**, without changing a dependency version; `aspire-runtime-inputs.json` binds those files.

**Independent engine.** Docker **29.8.2**, ID **`7b35cea8-c40b-4847-812c-b5e773ff8000`**, uses **`unix:///run/hx-retention-rdaale7k/docker.sock`**, `EXIT/engine-data`, private `/run/hx-retention-rdaale7k/exec`, daemon PID file and its own managed containerd socket/namespace. Its bridge/firewall are in **`net:[4026532296]`**, separate from the operator's **`net:[4026531840]`**; its cgroup-v2 mount is in private **`mnt:[4026532929]`**. Namespace keeper PID **44529** and daemon PID **46585** have recorded start identities. Userspace slirp supplies private networking with host loopback disabled; no host veth, firewall or route was configured. Docker/slirp/iptables binaries were downloaded/extracted only into this private directory; no system package or service was installed. Initial private-daemon configuration/cgroup startup refusals are retained. `isolation-receipt.json` and `preparation-owned-processes.json` record exact ownership.

Every proposed Docker/Aspire/DCP/selected-test/capture/observer/cleanup process uses the same recorded namespace prefix, explicit `DOCKER_HOST`, private Docker configuration and guarded client at `EXIT/client-bin/docker`. The guard verifies engine ID and rejects endpoint/context overrides or missing explicit routing. An actual child created by installed 4.35.2's scrubbed process API proves the correct namespace, endpoint, guarded client and engine (`installed-child-routing.json`). Supported version, manifest validation and selected-command help also pass under that prefix; these are not startup or retention passes. The host Docker Desktop client currently reports an unavailable WSL integration; the plan never falls back to it.

| Verification | Exact command / retained result |
| :--- | :--- |
| Ordinary owning source/Debug | `verification.json` records the exact test-project build with private `--artifacts-path`, `UseHexalithProjectReferences=true`, `NuGetAudit=false`, `-m:1`; **0 warnings/errors**, final `root-final-build.log`. Built assembly invoked with `-class Hexalith.Platform.Composition.Tests.PlatformNativeHostDiagnosticsTests -class Hexalith.Platform.Composition.Tests.PlatformDaprDiagnosticsTests`: **17/17**, final `root-final-owning-tests.xml`; initial 16/16 evidence remains retained. |
| Installed 4.35.2 API lane | From `EXIT`: `dotnet build native-api-tests/native-api-tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false`, then `dotnet native-api-tests/bin/Debug/net10.0/native-api-tests.dll -class Hexalith.Platform.Composition.Tests.PlatformNativeHostDiagnosticsTests -class Hexalith.Platform.Composition.Tests.PlatformDaprDiagnosticsTests -result-xml root-final-installed-api-tests.xml`: **17/17**, **0 build warnings/errors**. |
| Actual installed lifetime types | From `EXIT`: `dotnet installed-lifetime-probe/bin/Debug/net10.0/Probe.dll`: **7/7** controlled stream/delegate checks, final `root-installed-lifetime-checks.json`. Installed-type reflection is confined to this offline probe; no native runner or handoff is invoked. |
| Startup admission | From `EXIT`: `python3 -B -m unittest -v test_observe_startup test_native_host_admission`: final **28/28 in 3.168s**, `root-observer-tests.log`; earlier `observer-tests2.log` retained. Existing **23** assertions remain; five new cases cover required native-host capture, wrong PID, stale or stopping/failed evidence. The copied observer requires that new proof before admitting the existing four-console/loaded-input/complete-graph checks. No lifecycle action is added. Unchanged **16/16** MON proof is reused. |
| Engine / images / fresh state | `docker-routing-checks.json`: **4 expected outcomes**; seven exact immutable pulls/inspections pass in `prepared-container-images.json`. `fresh-preparation-state.json`: **zero containers/topology launches**, absent new native state and owner evidence. |

`verification.json` retains exact commands and initial failed compile/fixture/probe/setup attempts separately. An extra unhandled-thread probe was intercepted by xUnit and timed out; unhandled-exit coverage is not claimed. No broad owning build blocker remains. No Git index/ref, dependency version, prior resource, other session or Production change occurred.

**Ordinary review and correction.** Root's initial owning check exposed a published-console-backlog loss during immediate disposal (**15/16**, `root-focused-tests.xml`). `PlatformDaprDiagnostics` now reconciles already published public allocation snapshots before closing subscription admission, within the existing three-second drain budget; exact owned model and actual stream completion remain. New `DisposalReconcilesPublishedAllocationWhileStateDeliveryIsBlockedAsync` deterministically holds background allocation delivery using test-only reflection: the preserved pre-fix collector fails and the correction passes. `review-disposal-fix.json` retains red/green commands and the refreshed actual native model/PDB proof. Root's final owning Debug build passes with zero warnings/errors; both final owning and installed-API lanes pass **17/17**, zero failures/errors/skips/unrun. No verified finding remains unresolved or deferred. This race is a demonstrated capture defect, not attribution of historical process loss.

`root-review.json` records the completed single behavior/plan review and exact final commands. `root-preparation-readback.json` at **2026-10-24T12:06:43Z** independently verifies the expected engine ID, **zero containers**, all recorded process start identities, matching affected source bytes and absent new native state/evidence. Capacity is **14 CPUs / 97,021,218,816 Docker bytes / 92,212,391,936 Linux available bytes**, with **205,766,852,608 staging / 1,220,273,582,080 backing free bytes**. After the review correction, `root-affected-input-readback.json` separately verifies all six current source bindings, 36 model output identities and absent new state/evidence against the final model hash. Actual admission remains a once-before-launch check after authorization. Platform's plain whitespace check flags existing CRLF convention; the explicit CR-aware check passes, as does Memories' ordinary `git diff --check`. No index/ref was changed.

**Concrete proposed attempt.** Cwd **`EXIT/compatible-platform`**; owner **`EXIT/compatible-platform/references/Hexalith.Memories`**; fresh state **`EXIT/native-run/local-share`** and owner-relative evidence **`artifacts/retention/repair-spec14-native-exit-20261024-rdaale7k/functional.json`**. The complete namespace/environment prefix and exact command are in **`EXIT/launch-command.txt`**; under that prefix the unchanged supported command is:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-native-exit-20261024-rdaale7k/functional.json --output json
```

Proposed start **2026-10-24T12:30:00Z–2026-10-25T10:27:22Z**, checked feasible at **12:01:50Z**. Preserve **one launch / zero retries / no automatic restart**, **14400s execution including 600s startup**, separate cumulative **900s stopping**, absolute execution/disposal ends **2026-10-25T14:27:22Z / 14:42:22Z**. Keep **4.35.2 / 3.127.0 / Dapr 1.18.1 / PostgreSQL 18.6**, source/Debug, real **125/125**, **1800/3600/900s**, every strong-read/TTL/due-index/reminder/newer-record/signed-time assertion, **7 containers / 15 resources**, **8 CPU / 16 GiB Docker and Linux / 20 GiB staging and backing** floors and existing PostgreSQL **8 CPU / 16 GiB including swap / 100 connections / pool 40 / 400 GiB tmpfs maximum** caps. Preparation capacity passes; actual capacity/isolation/input/freshness is checked once immediately before an authorized launch.

The reviewed MON graph/input collectors are reused with the fresh run root, followed by `EXIT/observe-startup.py EXIT/observer-config.json` under the same prefix. Before native execution require observer ready; before the long wait require actual loaded model/Server, all four allocated console streams, fresh native-host capture PID proof, complete graph health and timestamped admission/ordering. During the wait use the existing graph gate at **≤30s** checkpoints and observe the private daemon/namespace/slirp plus bounded log storage. Any loss, failed/missing admission, native terminal or deadline starts immediate evidence preservation and supported exact-run `down --run-id <actual-new-run-id>` under the same prefix. Disposal, capture/observer stopping and exact empty-engine daemon/containerd/namespace/slirp stopping share the **same 900s** from the **earliest applicable loss/detection/stop/terminal** and the absolute end. Verify actual native removed/unverified:false/empty sets and all exact owned resource/process/workspace/socket absence; preserve failed/withdrawn/stale markers honestly. No broad prune or earlier-resource disposal is in scope.

**Root ordinary review is complete. No run has started and no further launch is authorized.** Story 27.4 stays **in progress**; local retention, full qualification, Production admission and A41 remain **open**.

### Corrected-observer native run — 2026-10-24; failed, disposal verified

Executed command from `/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-allocated-console-20261023-beps5_uj/compatible-platform`, with the environment in `OBS/run-plan.json`:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-startup-observer-20261024-c5wxrk9k/functional.json --output json
```

The owner approved the reviewed `OBS/run-plan.json` once. Native run **`86c304ce2363ee92f36b6be38a2cf058`** requested **07:59:40.1969943Z**, using the unchanged **4.35.2 / EventStore 3.127.0 / Dapr 1.18.1 / PostgreSQL 18.6** source/Debug stack. Once-before-launch capacity, exclusive engine and **307 inputs / 133 outputs** plus reviewed observer identity checks passed. Actual loaded Server/model/catalog, four allocated console streams and complete native resource graph admission finished **08:01:07.119088Z / monotonic 267107.683016878**, **1.629912s before** A's accounting boundary. The startup observer completed successfully; the existing graph monitor continued. This fixes the prior operator-sequencing failure.

Both A and B accounted for **125/125 attempted/enqueued/persisted/acknowledged/transaction acknowledgements**, with zero rejected/dropped/conflicted. Their native accounting observations were **08:01:08.749Z / 08:31:08.720Z**; those completion timestamps alone are not the cohort-start separation proof. At **09:00:01.345743Z / monotonic 270642.063312323**, monitoring latched **`process-uninspectable-or-stopped`** for **`eventstore-dapr-cli`**. The subsequent snapshot found every bound native AppHost/DCP/project/sidecar/loaded-Server PID absent. No initiating caller, actual AppHost exit code or timeout cause is established by the approximately one-hour timing.

Authentic native outcome is **exit 4 / HXT006**, `failed` at **09:00:16.9960493Z**. Exported **`functional.python3.trx`** contains **1 executed / 1 failed / 0 passed / 0 skipped**: **`functional-selected-state-read-transport-exhausted`**. The retained fixed diagnostic has **48 connection-refused attempts** after resource loss. There is no passing observations artifact. One-hour retention, expiry/purge/due-index/reminder completion and canonical newer-record preservation remain **unproved**; no full-profile, Production or A41 credit follows.

Same-run supported `down` returned **0**. Independent verification at **09:03:21.259757Z** found empty exact-run Docker and process sets, stopped capture/observer children and no native workspace. The actual native record is **failed/removed**, **unverified:false**, with empty sets. Disposal took **199.914014 UTC seconds / 199.924057 monotonic seconds** from the earliest observed loss, within the cumulative **900s** allowance. No retry/restart, selected-source rebuild, dependency or runtime/lifetime/security change occurred; Tenants stays stopped under the earlier exact-target authorization. All previous resources/evidence were preserved.

Retained private **`OBS=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-startup-observer-20261024-c5wxrk9k`** holds the approval, `live-admission.json`, `live-execution.json`, actual startup/ordering/graph receipts, `cohort-accounting`, `cohort-milestone.json`, `failure-native-evidence`, native sidecar snapshots, TRX/native outputs and terminal, `failure-process-snapshot.json`, `stopping-budget.json`, `final-disposition.json` and `run-result.json`. Read-only diagnosis found only fixed `apphost.output.observed` markers in the retained AppHost log; no final public sidecar capture-stopped receipts were delivered. Exact journal/dmesg queries in `host-exit-window.json` contain no OOM/AppHost-PID entry in the selected interval, which does not attribute the exit. Earlier malformed dmesg-time and unsupported inspection-option attempts remain recorded separately. The actual initiating AppHost/process-loss cause is unresolved; no speculative lifetime change was made. Next owner: Platform/Builds for attributable native AppHost exit/lifetime diagnosis before another launch. The approval is consumed, and today's full **600s startup + 4501s observation** latest start **09:05:59Z** has passed. A further run needs a fresh concrete decision/window; none is prepared or requested here. Story 27.4 stays **in progress** and A41 stays **open**.

### Concurrent Codex and Claude session check — 2026-10-24

At the owner's request, root examined **198 recently modified local Codex/Claude transcripts**, including subagent logs, using payload UTC timestamps. The execution/disposal interval **07:59:00–09:03:30Z** contains **nine active Codex sessions**: this Memories session, **six Parties sessions** and **two Platform sessions**. The **561 tool calls from other sessions** contain no explicit recorded stop/kill/Aspire lifecycle or Docker cleanup command affecting this run. Earlier same-day calls were also checked for lifecycle/background activity. This is a bounded session-log finding; it cannot exclude indirect test behavior, unlogged clients or external signals.

Parties ran Memories integration tests using Redis Stack/FalkorDB Testcontainers fixtures. The nearest confirmed batch ran **08:59:02.3441595Z–08:59:11.8524704Z**, **26 passed / 0 failed**, ending **49.4932726s before** the first observed native loss. Its reviewed wrapper invokes `dotnet`; the selected fixtures stop/dispose their own container. Neither path contains a global process stop or Docker cleanup. The nearly coincident **09:00:01.331Z** Parties command only reads source/log files. A separate source build started **08:59:50Z**, but its client tests began **09:01:27.8381841Z**, after the loss.

The Parties fixture defaults and reviewed calls record no separate Docker endpoint; no user/repository Testcontainers routing file exists at inspection. **Shared-engine use is inferred**, because the historical engine query returned no retained near-loss events and cannot independently prove the test endpoint. Container-test overlap is established; continuous shared-engine exclusivity is **not established** by the initial admission check. This weakens the exclusive-window assumption without identifying a deletion caller or proving the AppHost exit cause.

Platform's other sessions ran separate source/build/test campaigns. The reviewed top-level campaign termination receipts record normal exits with **no termination signals**. Its retained process helper starts a separate child session and pins that session's exact members for signaling; no link to the retention AppHost loss was found. No Claude transcript activity appears in the execution interval; the latest recorded Claude activity is **05:18:40.502Z**. All **12** earlier background candidates have completed notifications: **eight** dated before this run and **four** undated, whose exact completion times remain unavailable. Claude's retained job timelines also predate this run.

Private **`OBS/concurrent-agent-session-investigation.json`** links the inventories, exact call locators, sanitized XML execution receipts, wrapper/fixture hashes, Platform termination receipts and Claude background completion checks. Relevant raw command records remain private. Root contacted or interrupted no other session, launched no topology and changed no runtime/dependency. The initiating exit caller remains **unproved**; the failed native result, verified disposal and consumed authorization remain unchanged. Story 27.4 and A41 remain open.

### Corrected startup observation — 2026-10-24; reviewed preparation and successful live admission

The subsequent corrected native run above passed this startup admission, then failed after native process loss; both launch approvals are consumed. The earlier withdrawn run remains retained below. Fresh private **`OBS=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-startup-observer-20261024-c5wxrk9k`** corrects only read-only admission scheduling. It preloads retained model/source proof and existing compiled inspectors, follows partial readiness as files/PIDs appear, discovers and graph-binds the actual mapped Server child, and checks four allocated console streams plus native container/AppHost/DCP ownership before the real A/wait boundary. Exact DCP creator/UID/run and declared custom/allocated identities supplement native readiness; labels or Docker display-name guesses alone cannot pass. Missing/uninspectable or late proof signals operator stopping. The observer awaits bound collector health before the existing gate, tolerates only an incomplete trailing JSONL record, and completes after persisted pre-A admission plus actual A ordering. The existing graph collector/operator gate then owns the long wait. No lifecycle runner, topology, handoff, selected output rebuild, assertion, security, resource, dependency or Git change occurred.

Historical preparation: root single ordinary code/plan review was complete and the replacement plan was ready for one new operational authorization; the approved run above subsequently consumed it. Its launcher/custom-name/dependency-location/JSONL findings are fixed, with none unresolved/deferred. Final `python3 -B -m unittest -v test_observe_startup`, cwd `OBS`, passes **23/23 in 3.154s**, exit **0**, zero failures/errors/skips; exact log **`OBS/root-final-tests.log`** and review **`OBS/root-review.json`**. Coverage includes narrow readiness-to-A timing, proof/publication races and late refusal, partial progress/collector reload, actual loaded-child binding, retained custom-container shapes, foreign/replaced identities, missing/failed helper/capture, bounded CLI/output/private receipts and successful observer completion. `verification.json` preserves the initial fixture timing error and earlier green results. `native-shape-provenance.json` records exact installed **4.35.2** atomic whole-JSON writer/field proof, retained native shapes and actual existing Client/Contracts helper checks; DomainService proof stays at its real native-model location. Unchanged **16/16** graph tests and candidate/source/PDB/image proof are reused.

Replacement **`OBS/run-plan.json`**, `launch-command.txt` and `observation-usage.json` resolve the unchanged `FIX/compatible-platform` cwd, fresh absent **`OBS/native-run`** / `local-share` state and owner-relative **`artifacts/retention/repair-spec14-startup-observer-20261024-c5wxrk9k/functional.json`**. After a new operational authorization, start the two unchanged `MON` captures with that run root, record their exact script/PID pairs in `OBS/capture-processes.json`, then start `python3 -B OBS/observe-startup.py OBS/observer-config.json`. Require its ready receipt and actual observer/capture survival before the exact native command. During startup check failure/admission/commit/ordering receipts; successful `startup-observer-stopped.json` is expected after ordering. The observer never invokes lifecycle actions; the operator uses supported exact-run stopping/down on failure, terminal or deadline.

The same owner-reserved shared engine ends **2026-10-24T10:46:00Z**, with execution cutoff **10:31:00Z**. Native **14400s execution / 600s included startup / cumulative separate 900s stopping** remain ceilings; apply the earlier absolute limit. Existing **600s startup + 4501s real observation** fits only if launched by **09:05:59Z**; preparation/authorization delays reduce time. Preserve **one launch / zero retries**, **4.35.2 / EventStore 3.127.0 / Dapr 1.18.1 / PostgreSQL 18.6**, source/Debug, all assertions/resources/security and earlier evidence/resources. This prepared plan was subsequently approved and consumed by the native result above. No further launch or Tenants restart is authorized. Retention, Story 27.4 overall, full qualification, Production and A41 stay **open**.

### Exclusive-window run — 2026-10-24; withdrawn, disposal verified

The owner approved the reviewed plan, then explicitly authorized stopping the exact Tenants AppHost after admission found its shared-engine lifecycle controllers active. Supported exact-target `aspire stop --non-interactive --apphost /home/administrator/projects/hexalith/tenants/src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` returned **0**; refreshed admission found no conflicting lifecycle process. Capacity passed and **307 relevant inputs / 133 outputs** were unchanged. No Tenants source, dependency or Git state was changed, and no restart is authorized.

Run **`17a84801715eae686512262767f4b535`** requested **07:12:39.0101223Z**, reached all **15** resources healthy, and accounted for A **125/125** at **07:13:55.714Z**, with zero rejected/dropped/conflicted. The actual model/catalog/EventStore bytes matched the retained prepared proof. **Operator admission failed:** root did not complete loaded-Server/all-console/complete-graph binding before the native long wait; no timely `owned-graph-binding.json` exists. This is an operator sequencing failure, not an established product or retention failure. Root withdrew through supported native cancellation; actual terminal is **130 / HXC130**, `withdrawn` at **07:14:54.8067988Z**. No B accounting, native TRX or passing retention observations exist.

Same-run supported `down` returned **0**. At **07:16:38.556338Z**, the actual native record was **withdrawn/removed**, **unverified:false**, with empty sets; fresh exact-run Docker/process/workspace checks and stopped captures independently verified absence. Disposal took **162.842338s** from the conservative A timestamp anchor, within **900s**. All four native capture-stopped receipts report delivered-record persistence succeeded but final public exit/terminal data unavailable; no clean process-exit claim is inferred.

Evidence: **`PREP=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-exclusive-window-20261024-zfqwjbny`** contains approval, initial refused admission, exact Tenants stop logs, refreshed admission, `live-execution.json`, actual model proof, retained native readiness/A accounting/console snapshots, authentic native output and terminal, `stopping-budget.json`, `run-result.json`, and `final-disposition.json`. The initial broad process scan included its own transient inspection shell; that scan is retained, and the final scan excludes only current inspection ancestry. No resource was stopped by the inspection. The **one launch is consumed; no retry/restart is authorized**. Local retention, overall Story 27.4, full qualification, Production and A41 remain open. The reservation still ends **10:46Z today**; unused time is not another launch permission.

### Reserved shared-engine plan — 2026-10-24; historical consumed plan

This is the pre-launch preparation snapshot; the authorized withdrawal above consumed it. The corrected observer replacement plan is current.

The owner chose the **shared Docker engine during an exclusive window**, then answered **“now for 4 hours”** to the reservation question. Root uses the conservative **2026-10-24T06:46:00Z–10:46:00Z** interval; the first post-reply UTC observation was 06:46:53Z, and the earlier whole minute avoids extending the owner's reservation. The choice and interval authorize preparation only. All other Aspire/Docker lifecycle, test and cleanup activity sharing that engine, including clients in other namespaces, must remain paused through verified disposition. Confirm actual engine identity/exclusivity, capacity, freshness and relevant frozen inputs once immediately before an authorized launch; conflicting activity or failed admission refuses launch.

Fresh private **`PREP=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-exclusive-window-20261024-zfqwjbny`** contains `run-plan.json`, `launch-command.txt` and `owner-window.json`; retained `MON/conditional-run-plan.json`, graph verification/review and all prior source/evidence remain unchanged. Root's single ordinary plan review is complete: concrete plan and full two-file diff inspected, current baseline and historical deadlines clarified, no unresolved or deferred finding. **This historical plan was subsequently authorized and executed once; its approval is consumed by the withdrawn run above**. No test/topology, native source rebuild, dependency/Git mutation or resource disposal occurred during this preparation. Unchanged behavior reuses the reviewed **16/16** graph regressions and existing native/source/PDB/image proof; no repeat test or whole-workspace census was needed.

| Item | Prepared scope |
| :--- | :--- |
| Candidate / sources | Unchanged `FIX/compatible-platform`; supported **4.35.2**, genuine Platform `7021c41bea55bd6c690ba96082cf3dcfc95c9fca` / Builds `007bb9bc2806a0ead02d4831555bf2134a38d256`, EventStore **3.127.0**, immutable Dapr **1.18.1** / PostgreSQL **18.6**, source/Debug. Reuse existing relevant input/output, mapping and PDB proof; reserve selected outputs. |
| Fresh state / evidence | Absent `PREP/native-run` with `XDG_DATA_HOME=PREP/native-run/local-share`; absent owner-relative `artifacts/retention/prepare-spec14-exclusive-window-20261024-zfqwjbny/functional.json` under `FIX/compatible-platform/references/Hexalith.Memories`. No run path has been activated. |
| Reservation / ceilings | Shared engine `unix:///var/run/docker.sock`, previously observed identity `afcd365d-1578-410b-af0b-98550207ef1c`, to be confirmed at admission. Owner reservation **06:46–10:46Z today**, absolute execution cutoff **10:31:00Z**, verified disposal by **10:46:00Z**. Native **14400s / 600s included startup / separate cumulative 900s stopping** remain ceilings; apply whichever elapsed/absolute bound is earlier. Preparation/authorization delay reduces remaining time. Retain the existing **4501s** selected observation allowance before the operator cutoff; withdraw honestly if the unchanged scenario cannot finish. |
| Attempts / behavior / resources | **One launch, zero startup retries, no automatic restart**. Existing real **125/125 A/B**, ≥**1800s** separation, **3600s retention / 900s purge grace**, signed UTC/monotonic time, strong reads/absence, stored rows/TTL, due-index/reminder progress and unchanged canonical B. Existing **7 containers / 15 resources**, **8 CPU / 16 GiB Docker/Linux / 20 GiB staging and backing** admission floors and PostgreSQL caps remain unchanged. No new VM/disks, backend writes or previous-resource disposal. |
| Admission / monitoring | After authorization, start retained `MON/capture-runtime-inputs.py` and corrected `MON/capture-container-logs.py` with `PREP/native-run` as their argument, before the native command. Bind actual catalog/model/EventStore/loaded Server, all four allocated real console/PID/state streams and complete exact-run container/process graph before the native long wait, retaining UTC/monotonic admission proof. Use unchanged `MON/integration.json`; require fresh `operator_gate` and inspect terminal/capture/graph/deadline at checkpoints ≤30s. Missing/late proof consumes the launch and triggers stopping. |
| Stopping / disposition | Stop waiting immediately on first loss/detection/withdrawal/terminal or **10:31Z**, whichever applies first. Preserve authentic inputs/TRX/accounting/logs and earliest applicable obligation; use same-environment supported exact-run `down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --run-id <actual-new-run-id> --output json`. Native stopping, captures/children, down and fresh verification share **900s**, ending no later than **10:46Z**. Require actual terminal/disposition, empty owned container/process/workspace sets and stopped capture receipts. Record failed, withdrawn, stale or unverified markers honestly. Preserve earlier resources and all candidate/evidence. |

Prepared command, executable **only after operational authorization**:

```bash
cd /home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-allocated-console-20261023-beps5_uj/compatible-platform
umask 077
XDG_DATA_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-exclusive-window-20261024-zfqwjbny/native-run/local-share \
HEXALITH_DAPR_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home \
DOCKER_HOST=unix:///var/run/docker.sock DOCKER_CONTEXT= \
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/prepare-spec14-exclusive-window-20261024-zfqwjbny/functional.json --output json
```

The four-hour native ceiling does not enforce the earlier reservation cutoff by itself: the operator must use existing supported stopping by **10:31Z**. No deadline, retry, assertion or security change is made to the native runner. Preparation leaves retention, overall Story 27.4, full qualification, Production and A41 **open**. `PREP/preparation-verification.json` records passing JSON/command/path/deadline/private-mode/source-preservation checks, `bash -n PREP/launch-command.txt` and `git diff --check` (both exit **0**), exact two-file scope and current preparation baseline **`38128624763c313238904c30aaeb88eb4fd9a6d0`**. Actual engine/capacity/input admission remains unexecuted until authorization.

### Offline complete-graph monitoring correction — 2026-10-23; reviewed

The resumed investigation confirmed that the first retained checkpoint after all seven container kills still reported four living sidecars and contained no container-graph check. A concurrent MCP run at `19:46:42Z` used private PID/user/network/mount namespaces and Aspire **13.6.3**, without demonstrated Docker-engine isolation; its first resource log is `19:46:44.4180693Z`. All seven killed containers shared a host creator PID. This supports a cross-namespace cleanup **hypothesis**, not attribution: the initiating Docker caller and actual loaded native DCP bytes were not retained. The bounded scan of **4561** tool calls found no literal Docker mutation. Sanitized evidence is in `/home/administrator/.local/state/hexalith/memories/access-telemetry/diagnose-spec14-container-removal-20261023-_9kzv6id/diagnosis.json` and linked receipts. No unrelated project was changed.

Fresh private **`MON=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-owned-graph-20261023-paues9lm`** corrects the existing container collector/operator check only. It binds all **15** native resources (**7 containers / 8 project and sidecar PIDs**) plus actual AppHost/proven owned long-lived DCP processes; checks exact container membership, running/start/restart identity and PID/executable identity; and atomically retains the first event/loss with UTC/monotonic anchors. Foreign runs are excluded. Missing, stale, stopped or failed observers refuse the operator wait. Event loss is supplemented by projected membership reads, so missed events cannot preserve a false healthy result. This helper observes/signals; the operator still must begin supported preservation/stopping. It does not implement another runner or change native retention behavior.

`MON/verification.json` retains the exact commands, source hashes, private modes, red/green results and ordinary review. The identical seven-loss replay fails against the old collector (**one expected failure**, **3.047s**). Root's final `python3 -B -m unittest -v test_capture_container_logs`, cwd `MON`, passes **16/16** in **3.021s**, exit **0**, zero failures/errors/skips. Coverage includes actual collector/CLI doubles, missed events, foreign-run denial, pause/restart/replacement/PID reuse, real controlled process pause/exit, observer EOF/parse/timeout/flood, finite private stdout/stderr tails and bounded disposal. Root found and corrected two issues: concurrent loss publication and freshness with no scheduling margin; their focused regressions and the final suite pass. No review finding is unresolved or deferred. No live Docker command or namespace reproduction is part of these tests; product tenant/authentication behavior is unchanged.

`MON/integration.json` supplies exact binding/check usage and bounds: **1s** between snapshot polls, at most two **2s** Docker reads, **7s** health freshness (including **2s** margin), private event/log tails totalling at most **2,883,584 bytes**, and a shared **3s** observer-child disposal phase after any in-flight reads. The disposal receipt is not the beginning of the overall stopping obligation; use the earlier applicable resource-loss event/detection, stopping request or native terminal anchor for the cumulative **900s** allowance. Both original capture scripts and all historical candidate/evidence remain unchanged. No source rebuild, dependency change, topology, Git mutation or prior-resource disposal occurred.

**Historical conditional plan — superseded by the reserved plan above:** `MON/conditional-run-plan.json` resolves the unchanged `FIX/compatible-platform` candidate, fresh absent `MON/conditional-next-run` state and owner-relative `artifacts/retention/repair-spec14-owned-graph-20261023-paues9lm/functional.json`. It preserves **4.35.2 / EventStore 3.127.0 / Dapr 1.18.1 / PostgreSQL 18.6**, source/Debug, all assertions/resources/security, one launch/zero retries, **14400s** execution with **600s** included startup, separate cumulative **900s** stopping, latest full-budget start **2026-10-25T10:27:22Z** and outer end **14:42:22Z**. This was the unresolved isolation state when the correction completed; the owner-selected shared-engine reservation and fresh plan above now supersede its preparation state. The retained conditional plan remains historical; no launch authority is inferred and no launch occurred. Functional retention, overall Story 27.4, full qualification, Production and A41 remain open.

### Corrected native run — 2026-10-23; failed, resources removed

The owner's **“I approve”** authorized the reviewed `FIX` plan once. Run **`a03c93f5d7838c56b377422c5edd05fc`** launched **19:39:37.580401Z / monotonic 222616.365262128** after capacity/isolation and all **307 input / 133 output** checks. Actual catalog/model/EventStore and loaded Server DLL/PDB/source binding passed; all four allocated sidecars delivered real console output with attributable PID/state. Admission **19:41:22.511121Z / monotonic 222721.29986427** preceded A accounting by **3.193879s**. A reports **125/125 attempted/enqueued/persisted/acknowledged/transaction acknowledgements**, zero rejected/dropped/conflicted. All **203 actual loaded DLL/PDB files** still match their captured hashes before disposal.

Native execution returned **4 / HXT006 / ProductOrTest** at **20:15:26.140611Z**, elapsed **2148.661941s**. Authentic TRX has **one failed case**, `PostgreSQL functional retention / functional-selected-emit-budget`, with no detailed error text. B accounting and passing `functional-observations.json` are absent; B persistence, expiry/purge and newer-record preservation are **unproved**. The unchanged native B request has a 120s timeout and at most one same-identity reconciliation; no operator retry or new emission occurred.

**Concrete failure evidence:** retained Docker events show all **seven** owned containers, including PostgreSQL, received **signal 9 at 19:46:44Z**, then exited **137** and were destroyed by **19:46:47Z**, before B. No `oom` event is retained. Surviving sidecar processes continued delivering placement/scheduler handshake failures and Redis connection refusals. Their Running snapshots did not establish survival of the container graph; the periodic check missed this earlier loss. Docker events do not identify the kill initiator. Both bounded read-only A/B physical snapshot attempts later failed **before `psql` ran**, with Docker `No such container` for the recorded PostgreSQL ID; this establishes neither absence nor persistence of B records. Actual bounded sidecar/container tails are preserved. No regular application log files were available in the native store/workspace or owned DCP descendants; the retained AppHost log contains observation markers only.

Supported exact-run `down` ran **20:19:29.301235Z–20:19:32.583027Z**, exit **0 / HXI001**. Fresh verification **20:20:07.128856Z / monotonic 225046.030157372** found no exact-run containers, recorded processes, capture groups/children or workspace. The actual native selected-test record is **`failed` / `resourceDisposition: removed`**, **`unverified:false`**, empty owned sets. Exported `functional.json` remains its pre-down `retained` snapshot; it is not rewritten. All four capture-stopped receipts report **`CaptureFailed:false`** and unavailable terminal/exit metadata. Operator disposal used **281.002955s**, leaving **618.997045s** from the recorded native-return stopping anchor. Earlier container loss was discovered after disposal: verified absence was **2003.128856s after its first kill event**, so a 900s stop bound measured from that loss was **not met**. No earlier retained resources were disposed.

Retained under **`FIX=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-allocated-console-20261023-beps5_uj`**: `live-admission.json`, `live-execution.json`, `runtime-admission.json`, `runtime-admission-ordering.json`, `actual-loaded-input-final-readback.json`, native terminal/TRX and accounting in `failure-native-evidence` / `failure-runtime-workspace`, pending-request and final bounded diagnostics in `pending-b-diagnostics`, `failure-native-sidecars`, `failure-container-logs` and `failure-window-diagnostics`, plus `retained-container-terminal-events.json`, `failure-physical-snapshot.json`, `failure-diagnosis.json`, preserved operator `stopping-budget.json`, `retrospective-resource-loss-timing.json`, `run-result.json`, supported-down receipts, `actual-terminal-after-down.json` and `final-disposition.json`. The command/stack/resource bounds below remain the executed plan. **This launch is consumed; no retry/restart is authorized.** The concrete unresolved input is the container graph's signal-9 removal and its detection/stop path; no initiator or retention success is inferred. Overall Story 27.4, full qualification, Production and A41 remain open.

**Root result review:** native failed TRX/hash, genuine failed/removed terminal, empty ownership/process/capture/workspace evidence and the separate timing records are verified in `FIX/root-live-result-review.json`. No retention completion is claimed. Read-only attribution checks found no Linux Docker journal entries for the kill window; available current-date Docker Desktop backend logs begin at 20:16:14Z and do not cover the 19:46 loss. `kill-initiator-audit.json` and its narrowly retained log checks establish no caller.

**Reopening:** identify the signal-9 removal source or establish an effective exclusive container-runtime window before another concrete run is proposed. Future operator monitoring must check the complete owned container/process graph and retained Docker events during waits, rather than sidecar PIDs alone, and begin bounded preservation/stopping promptly on any resource loss. These are unresolved operational inputs; no retry, new plan approval or extra launch is inferred.

### Offline allocated-console correction — 2026-10-23; reviewed

The concrete console failure is reproduced and corrected in owning `/home/administrator/projects/hexalith/platform`, preserving its concurrent owner changes. `PlatformDaprDiagnostics` now subscribes from the exact owned public `ResourceEvent.ResourceId`, retaining each validated instance key and completing that same string key. Backlog/live line-number cursors belong to each stream; console records include the instance ID. Final snapshot lookup uses the retained instance ID and exact model-object identity, preserving allocated terminal/exit metadata. Public services, bounded/private tails, redaction, foreign exclusion and explicit failure/terminal-unavailability semantics remain. No reflection/internal Aspire API, sidecar wrapper, new runner, lifetime/retry/security change or topology was added.

The existing public-service fixture now publishes an initial allocation notification after `BeforeStart`; every earlier assertion is retained. The new case then introduces a distinct later instance, with a real pre-notification backlog, live output, overlapping line numbers, foreign output and terminal/exit metadata. The **same final test** fails against preserved pre-fix production on the missing allocated backlog (**1 expected failure, 0.295s**) and passes with the correction. Retained initial fixture failure did not reach allocation and is not counted as this proof. An intermediate unsupported `KnownResourceStates.Terminated` test constant was corrected to the existing supported literal before final green verification.

| Verification | Actual result / evidence in `FIX` below |
| :--- | :--- |
| Installed 4.35.2 public-service source/Debug lane | **9/9**, zero failures/errors/skips/unrun, **0.535s**. Build has zero warnings/errors; `final2-focused-commands.json`, `final2-public-capture-tests.xml`. |
| Ordinary owning source/Debug project and focused class | Build passes **45.617s**, zero warnings/errors; existing class **9/9**, zero failures/errors/skips/unrun, **0.547s**. `final-owning-authoritative-test-project-build.json`, `owning-public-capture-tests.xml`. This uses the owner's authoritative current source setup and isolated artifacts. |
| Forced-old-tool build, retained separately | Forcing installed 4.35.2 Tooling into current owning Platform produces six unrelated API mismatches (`RenderConfiguration`, resource-claim members and `SelectedTests`). This is a forced compatibility mismatch, **not an unresolved ordinary owning build failure**. The installed-API lane above separately proves capture compatibility. |
| Fresh supported candidate/native model | Version/resolver/manifest validation and actual installed native offline model build pass (**19.659s**); no AppHost/DCP/topology. Model SHA-256 **`eb220802b0fbf5a01d5ecccd2fd9fe36fcb88b1a62ea51ed6e346ffd4df0a1e2`**; matching DLL/PDB binds all five capture/registration files. Four inspected native EventStore DLLs remain **3.127.0.0**; catalog versions are unchanged. |

Fresh private **`FIX=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-allocated-console-20261023-beps5_uj`** physically copies the preserved withdrawn `READY` candidate and adopts only the two capture source files plus their existing test class. Genuine Platform **`7021c41bea55bd6c690ba96082cf3dcfc95c9fca`** and Builds **`007bb9bc2806a0ead02d4831555bf2134a38d256`** authority remain. Mapping **`f437fa8972df35d4bd7da17b718f608eb0b0f6965c7d8bf03888d29cdbf1d615`** selects physical sources; **305/307** selected inputs are unchanged, and exactly the two capture source files change. Server assertions/correction, prepared Server SHA-256 **`f7b434f2fe98f38ff400be404b497fa341db1b1d35795d311a086fbdeddf7ec3`**, EventStore 3.127.0, tool 4.35.2, native Dapr 1.18.1, PostgreSQL 18.6 and immutable images are reused. No source link points into shared repositories; prior `READY` source/results and withdrawn/remaining marker are untouched.

Exact commands, source/binary/identity proof and retained failures: `candidate-adoption.json`, `candidate-verification.json`, `affected-source-binary-binding.json`, `affected-binary-provenance.json`, `selected-input-readback.json`, `physical-source-isolation.json`, `prepared-output-identities.json` (**133 files**), red/green XML/logs and owning-build receipts. The plain owning `git diff --check` treats existing required CRLF bytes as trailing whitespace; the scoped check retaining standard whitespace rules plus `cr-at-eol` passes. No repository configuration or line-ending convention was changed.

#### Corrected console plan — reviewed, authorized and executed once

`FIX/run-plan.json` and `FIX/launch-command.txt` retain the reviewed plan; `FIX/root-review.json` records its completed ordinary review. The owner subsequently authorized it once, and the actual result above consumes that launch. Its approved start window was **2026-10-23T18:45:00Z–2026-10-25T10:27:22Z**. No unused time or earlier permission grants a retry, restart or budget reset.

| Item | Approved scope at launch |
| :--- | :--- |
| Paths / freshness at launch | Cwd **`FIX/compatible-platform`**; owner **`FIX/compatible-platform/references/Hexalith.Memories`**. Fresh empty **`FIX/local-share`** and absent owner-relative evidence **`artifacts/retention/repair-spec14-allocated-console-20261023-beps5_uj/functional.json`**. Preserve prior resources, source and evidence; reserve this isolated candidate's selected outputs. |
| Budgets / attempts | **One new launch, zero startup retries, no automatic restart**. **14400s execution including 600s startup**, separate cumulative **900s stopping/capture/disposal** from first stopping or terminal return, whichever is earlier. Absolute latest execution/cleanup **2026-10-25T14:27:22Z / 14:42:22Z**; actual UTC/monotonic anchors are nonrenewing. |
| Stack / resources | Supported **4.35.2**, source/**Debug**, EventStore **3.127.0**, immutable Dapr **1.18.1** and PostgreSQL **18.6**, TLS `verify-full`. Existing **7 containers / 15 resources**, no VM/disks. Once-before-launch actual admission: **8 CPUs**, Docker/Linux available **16 GiB**, staging and `/mnt/c` **20 GiB each**. PostgreSQL caps **8 CPUs / 16 GiB including swap / 100 connections / Dapr pool 40 / 400 GiB tmpfs maximum**; no whole-graph reservation. |
| Behavior / startup admission | Unchanged **125/125 A and B**, ≥**1800 real seconds** separation, **3600s retention / 900s purge grace**, strong reads/absence, stored rows/TTL, due-index/reminder progress, canonical B preservation and signed UTC/monotonic measurements. Observe from native plan creation: bind actual generated model/catalog/EventStore and all five capture/registration files immediately, then actual run-scoped Server DLL/PDB once built. Reuse prepared hash/PDB source proof for unchanged Server files and check each actual model/service/PID as it appears, using the existing read-only helpers. Require **all four actual console streams, capture-started and attributable allocated ID/PID/state**, with a timestamped **UTC/monotonic admission receipt completed before the long wait**; if the wait begins first, stop the launch. Do not defer until cohort submission or create another runner/handoff/framework. Missing proof consumes the launch and triggers bounded supported stopping. Existing bounded reads and one same-identity uncertain-submission reconciliation remain. |
| Capture / exact-run disposition | After authorization only, start the two unchanged scripts as `python3 -B FIX/<script> FIX`, record PIDs, preserve native results/accounting/transport logs and actual loaded/generated inputs before same-environment supported `down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --run-id <actual-new-run-id> --output json`. Native stop, captures/children, down and verification share **900s**. Require native `removed`, `unverified:false`, empty owned sets, fresh exact-run Docker scan and four stopped receipts. Preserve withdrawal/failure and stale native markers honestly; disclose missing terminal/exit evidence, truncation and capture failure. No broad prune or earlier-resource disposal. |

Exact executed command (**one authorized launch; consumed**):

```bash
cd /home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-allocated-console-20261023-beps5_uj/compatible-platform
umask 077
XDG_DATA_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/repair-spec14-allocated-console-20261023-beps5_uj/local-share \
HEXALITH_DAPR_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home \
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/repair-spec14-allocated-console-20261023-beps5_uj/functional.json --output json
```

Root ordinary review checked the three-file correction, identical pre-fix red case, both 9/9 green lanes, native DLL/PDB/source binding, all 133 frozen outputs and the unchanged plan limits. All four offline correction/preparation tasks are complete. The subsequent once-authorized live result is recorded above; no new authorization or retry is implied. No staging/commit/push/branch or dependency change occurred during this correction/execution. Retention, historical one-hour sidecar exit cause, full qualification, Production and A41 remain open.

### Authorized native run — 2026-10-23; admission failed, withdrawn

The owner's **“I approve”** authorized the reviewed `READY` plan once. Its exact native command launched **2026-10-23T18:10:23.552340Z** as run **`97f7d0542335ba34c85f5a3b4e868577`** and returned **18:13:44.342673Z**. Once-before-launch admission passed: **14 CPUs**, Docker memory **97,021,218,816 bytes**, Linux available **56,915,718,144 bytes**, staging **112,824,188,928 bytes**, backing **1,417,286,885,376 bytes**; no existing G-4 containers or active candidate processes, fresh state/evidence, and unchanged **307 selected inputs / 133 prepared files**. The nonrenewing execution ceiling was **22:10:23.552340Z**, including startup until **18:20:23.552340Z**.

All **15 resources** reached healthy readiness. Actual native model DLL/PDB/source hashes matched preparation, with approved full Platform/Builds identities and EventStore **3.127.0.0**. Cohort A's authentic accounting at **18:12:28.875Z** reports **125/125 attempted/enqueued/persisted/acknowledged**, zero rejected/dropped/conflicted. The native test entered its existing wait; complete pre-wait admission was **not satisfied**: all four sidecars had capture-started and attributable Running/PID records, but each console file held only its initial empty-tail marker, with **zero real console records**. Supported stopping withdrew the run rather than continuing without that required proof. B accounting, native TRX and `functional-observations.json` are absent. CLI **130 / HXC130** reports cancellation, with **HXT002** for the missing/uninspectable selected native report and no reported test counts. This is a runtime admission failure and native withdrawal, **not a measured retention assertion failure or pass**. Post-withdrawal read-only inspection binds the actual loaded Server to all three correction sources and its actual EventStore dependencies to **3.127.0.0**; it does not retroactively satisfy pre-wait admission.

Stopping began **18:13:34.451283Z / monotonic 217452.968614486**, with one cumulative **900s** allowance ending **18:28:34.451283Z**. First supported exact-run `down` returned **3 / HXR028**, `stdin-listener` blocked after stop; a subsequent supported **same-run disposal verification**, not a startup retry, returned **0** at **18:14:39.918384Z**. Fresh verification at **18:16:22.777953Z** found empty exact-run Docker and recorded process sets, both retained capture groups/children stopped, and all four capture-stopped receipts: **168.334162s total**, **731.665838s remaining**. Later read-only confirmation also finds all three actual mapped AppHost/service processes absent. Prior retained resources remain untouched.

**Native disposition limitation:** the authentic selected-test record remains **`withdrawn` / `resourceDisposition: remaining`**, with empty sets and **`unverified:false`** after the first down failure. The later successful absent-run `down` does not rewrite that marker; no `removed` marker is fabricated. Actual resource removal is independently verified, while the native removed-marker completion criterion remains unmet. All four receipts report **`console-drain-failed` / `CaptureFailed:true`**, and unavailable terminal state/exit metadata. No initiating sidecar exit cause is inferred.

Retained under **`READY=/home/administrator/.local/state/hexalith/memories/access-telemetry/adopt-spec14-compatible31270-20261023-l4em38o7`**: exact admission/command/UTC/monotonic anchors in `live-admission.json`, `live-execution.json`, `runtime-admission-failure.json`, `native-terminal.json`, `stopping-budget.json` and both supported-down receipts; native `native-functional-evidence.json` / `actual-terminal-after-down.json`; `admission-evidence/native-evidence/native-tests/cohort-a-accounting.json`; original state/console tails, generated model/Dapr/components, loaded binary proofs, logs, `final-disposition.json` and `actual-loaded-process-exit-readback.json`. The one launch is consumed. **No retry/restart or further launch is authorized.** Local retention, historical sidecar-loss cause, full qualification, Production and A41 remain open.

#### Concrete public-console diagnosis — read-only

Executed `PlatformDaprDiagnostics` subscribes with `WatchAsync(IResource)` during `BeforeStart` and later calls `Complete(IResource)`. Installed **Aspire.Hosting 13.6.1** decompilation/IL confirms each call resolves names at its own call time: initial logical names before allocation, later allocated instance IDs. All four retained IDs are suffixed, first observed about **0.5s after capture-started**. Official [ResourceLoggerService](https://raw.githubusercontent.com/dotnet/aspire/v13.6.1/src/Aspire.Hosting/ApplicationModel/ResourceLoggerService.cs), [name resolution](https://raw.githubusercontent.com/dotnet/aspire/v13.6.1/src/Aspire.Hosting/ApplicationModel/ResourceExtensions.cs) and [DCP console routing](https://raw.githubusercontent.com/dotnet/aspire/v13.6.1/src/Aspire.Hosting/Dcp/DcpResourceWatcher.cs) confirm that subscriptions and native log production must share the allocated instance key.

**Supported inference:** the early logical subscriptions miss the later DCP producer keys; disposal completes those later keys while the original watches wait until the existing 3s cancellation bound. This explains the zero-console/drain-failure pattern, but no live logger-key dictionary trace was retained. `console-capture-diagnosis.json` records facts, inference, actual IDs, installed DLL hash/API inspection and source links separately. No unexpected sidecar death occurred here: stopping was intentional. The historical one-hour exit cause remains unknown. No source/runtime behavior correction or new topology was performed by this read-only diagnosis.

### Compatible EventStore adoption — 2026-10-23; prepared

The owner authorized the two isolated local commits with **“yes”**. Fresh private **`READY=/home/administrator/.local/state/hexalith/memories/access-telemetry/adopt-spec14-compatible31270-20261023-l4em38o7`** contains independent commit copies and a physical copy of retained `ADOPT/compatible-platform`. Genuine Builds **`007bb9bc2806a0ead02d4831555bf2134a38d256`** changes only the EventStore family property on `66b9136a0b223925e51177a51eb48852e2f12787`; genuine Platform **`7021c41bea55bd6c690ba96082cf3dcfc95c9fca`** changes only its corresponding gitlink on `261779bfa862a40a8db648cfe3f7c2619704c2ad`. Both messages pass each owning repository's pinned commitlint **before and after** committing; no hook/check was bypassed. Root reviewed both committed diffs and all four lint receipts without findings. No remote Git operation occurred; current owning checkouts, indexes/branches, previous candidates and evidence are preserved.

**The native catalog blocker is resolved in this candidate.** Supported 4.35.2 resolver and installed native `BuildModelAsync` both select those genuine identities without identity findings. Mapping **`3e5855749491f954b8554ecf8ba1a8b26d4c61ad802a607cc536babba15dc774`** selects the physical source projects. The actual verified native catalog changes exactly the **13 EventStore family rows to 3.127.0**; every other version and the security/source exclusions remain unchanged. Four inspected native EventStore assemblies report **3.127.0.0** with matching PDBs; **1269 compiled release-document occurrences** match approved release `e1d8b17160202761ab5c9606cafbcf4d1a4b92c3`. All **307** selected assertion/capture inputs remain byte-equal, and the actual native composition binds all five unchanged capture/registration sources. There are no links into shared sources or alternates in the new Platform/Builds Git authorities.

| Offline verification | Result / evidence in `READY` |
| :--- | :--- |
| Actual installed native model build | Exit 0, **20.878s**, source/Debug; composition SHA-256 **`3f11205855bbe1c3555928b351a0f2f1edff4e1829150a01019984bcecdda146`**. No AppHost/DCP/topology launch. Native API does not return build warning totals. |
| Final mapped Server / configuration-project builds | Exit 0, **10.526s / 10.346s**, zero warnings/errors. Server SHA-256 **`f7b434f2fe98f38ff400be404b497fa341db1b1d35795d311a086fbdeddf7ec3`**; approved queue/logger/worker source checksums and DLL/PDB identity match. Final assets select EventStore Client/Contracts **3.127.0 as projects**. |
| Existing configuration / catalog checks | **11/11**, zero failures/errors/skips/unrun, **0.115s**, against ordinary 3.127.0 package assets; final mapped assets restored separately. Same byte-equal catalog fixture was removed, without nested initialization or changed expectations. Both catalog validators pass (**310 entries**, unchanged hosting **13.6.1-beta.935**). |
| Unaffected capture behavior | Reuse retained `ADOPT/public-capture-tests.xml` **8/8** and its completed review: capture sources are unchanged and rebound in the actual new native model. Historical failed runs/broad blockers retain their stated limits; no full-suite or live pass is claimed. |

Exact commands/results and commit scopes: `git-preparation.json`, `commit-verification.json`, `committed-identities.json`, `verification.json` and catalog receipts. Runtime/source binding: `native-admission-binding.json`, `native-model-binary-provenance.json`, `final-binary-provenance.json`, `prepared-server-correction-binding.json`, `selected-input-readback.json`, `physical-source-isolation.json` and `prepared-output-identities.json` (**133 actual output/entry/configuration files**). Reuse prior `ADOPT/input-adoption.json`; native Dapr remains **1.18.1**, SHA-256 **`be0d0ee7362c6b8626538876fa2342d1c0789d126a60129a7bf27009a072769c`**. Dapr/PostgreSQL immutable image identities, TLS/security and real retention assertions are unchanged.

#### Replacement run plan — awaiting one operational authorization

`READY/run-plan.json` and `READY/launch-command.txt` contain the complete reviewed replacement proposal; `READY/root-review.json` records the ordinary root review with no unresolved findings or deferred work. At **2026-10-23T17:26:24Z** its **2026-10-23T17:35:00Z–2026-10-25T10:27:22Z** start window is feasible. Preparation grants **no launch/restart authority**; all earlier launch permissions remain consumed.

| Item | Exact proposed scope |
| :--- | :--- |
| Paths / freshness | Cwd **`READY/compatible-platform`**; owner **`READY/compatible-platform/references/Hexalith.Memories`**. Fresh, empty **`READY/local-share`**; absent owner-relative evidence **`artifacts/retention/adopt-spec14-compatible31270-20261023-l4em38o7/functional.json`**. Reserve this isolated candidate's selected outputs; preserve all prior source/evidence/resources. |
| Budgets / attempts | **One launch, zero startup retries, no automatic restart**. **14400s execution including 600s startup**, separate cumulative **900s stopping/capture/disposal** from first stopping or terminal return, whichever is earlier. Latest execution/cleanup **2026-10-25T14:27:22Z / 14:42:22Z**. Record actual UTC/monotonic anchors; no reset or reuse of old budgets. |
| Stack / resource bounds | Supported tool **4.35.2**, source/**Debug**, EventStore **3.127.0**; immutable Dapr **1.18.1**, PostgreSQL **18.6**, TLS `verify-full`. Existing **7 containers / 15 resources**, no VM/disks. Admission once immediately before an authorized launch: **8 CPUs**, Docker/Linux available **16 GiB**, staging and `/mnt/c` **20 GiB each**. PostgreSQL caps **8 CPUs / 16 GiB including swap / 100 connections / Dapr pool 40 / 400 GiB tmpfs maximum**; no whole-graph reservation. |
| Assertions / admission before long wait | Unchanged **125/125 A and B**, ≥**1800 real seconds** separation, **3600s retention / 900s purge grace**, strong reads/absence, stored rows/TTL, due-index/reminder progress, canonical B preservation and signed UTC/monotonic measurements. Before the long wait require actual per-run verified catalog/loaded EventStore 3.127.0, Server correction and five composition-source DLL/PDB bindings, generated configuration/dependencies, and **all four sidecars' real console output, capture-started plus attributable PID/state evidence**. Missing/mismatched proof consumes the launch and triggers bounded stopping; no retry. Existing bounded read retries and one same-identity uncertain-submission reconciliation remain unchanged. |
| Capture / exact-run disposition | After authorization, start the two unchanged capture scripts as `python3 -B READY/<script> READY` and record PIDs. Preserve native TRX, observations if passing, accounting/transport diagnostics, logs and actual loaded/generated inputs before supported `down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --run-id <actual-new-run-id> --output json`, same cwd/environment. Native stop, captures/children, down and verification share **900s**. Require `removed`, `unverified:false`, empty owned sets, fresh exact-run Docker-label scan empty, and four capture-stopped receipts. Disclose unavailable terminal/exit evidence, truncation or capture failure; DCP ends public observation before native cleanup. No broad prune or disposal of earlier retained resources. |

Proposed command (**not executed**):

```bash
cd /home/administrator/.local/state/hexalith/memories/access-telemetry/adopt-spec14-compatible31270-20261023-l4em38o7/compatible-platform
umask 077
XDG_DATA_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/adopt-spec14-compatible31270-20261023-l4em38o7/local-share \
HEXALITH_DAPR_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home \
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/adopt-spec14-compatible31270-20261023-l4em38o7/functional.json --output json
```

Root ordinary review is complete: canonical commit scopes/messages, actual native catalog/dependency/PDB/source binding, 11/11 fresh configuration XML, unchanged 8/8 capture proof, command/freshness, all 133 selected output identities and unchanged operational bounds were checked. All three preparation tasks are complete; one operational authorization remains required. No topology was launched/restarted; no retention observation, initiating sidecar cause, full qualification, Production or A41 credit follows. Story 27.4 remains in progress. Earlier blocked proposals below are preserved as historical evidence.

### Compatible-catalog recheck — 2026-10-23; blocked

At **2026-10-23T17:13:47Z**, the affected-input adoption remains blocked; the [existing conditional plan](#conditional-replacement-plan--not-ready-for-authorization) is not ready for operational authorization. The owning Platform now has genuine commit **`ae932d038a03846e1349fc550a3ae1a3a5d14ab0`**, pinning Builds **`8c9caa77feb85a2dce03153519d81684128890a6`** with EventStore **3.127.0**. Relative to the retained compatible catalog **`66b9136a0b223925e51177a51eb48852e2f12787`**, it also selects Aspire SDK/Hosting **13.6.1 → 13.6.3**, OpenTelemetry **1.20.0 → 1.20.1**, StyleCop **1.3.109-beta → 1.3.151-beta** and other unrelated updates. Adopting those inputs exceeds the EventStore-only scope. Memories' direct Platform remains `f7c62ca1007f5218dc52d6856395324704564e3a`; neither checkout nor the retained candidate was changed.

A read-only check of **445 local catalog-changing commits / 440 unique catalogs** found no committed catalog equal to `66b9136` plus only the approved EventStore change. Earlier genuine **`230a26fcdab99e8bdd9ebbbdf9785cb1b0e74077`** preserves the hosting versions but removes the Works source exclusion, Works.Contracts and PKCS/XML security pins. Fresh private **`UNBLOCK=/home/administrator/.local/state/hexalith/memories/access-telemetry/unblock-spec14-eventstore31270-20261023-m_0si23g`** retains exact commands/results, both catalog diffs and `catalog-reopen.json`. Its **`builds-eventstore-only.patch`** changes only the family property on `66b9136`, preserving those security/compatibility entries. `git apply --check` passes; existing candidate central and platform catalog validators pass for that private proposal (**310 entries**, EventStore Aspire **3.127.0**, unchanged Dapr hosting **13.6.1-beta.935**). This is a checked proposal, not an adopted or committed native catalog. Prior `ADOPT` builds, **11/11** configuration and **8/8** public capture checks remain unaffected and were reused.

**Concrete reopening:** provide an approved genuine Builds commit containing that one-line compatible-catalog correction, and a genuine compatible Platform commit whose `references/Hexalith.Builds` gitlink selects it. No matching existing commit was found; no target hash or Git identity was invented. Preserve current owner changes rather than resetting their checkouts. Then adopt only affected inputs in a fresh physical candidate, run supported mapping/native model and DLL/PDB/source binding checks, and complete the root review of a ready replacement plan before its single operational authorization. The existing start window **2026-10-23T15:30:00Z–2026-10-25T10:27:22Z** remains feasible at this readback; **14400s execution / 600s included startup / separate cumulative 900s stopping** and **2026-10-25T14:42:22Z** outer end remain unchanged. No topology, dependency adoption, nested-submodule update, staging, commit, push or branch change occurred. The unchecked tasks, local retention, initiating sidecar cause, full qualification, Production and A41 remain open.

### Post-pull EventStore alignment — 2026-10-23

All nine root-declared submodules were pulled with `git -c submodule.recurse=false pull --ff-only --no-recurse-submodules`: five advanced and four were already current. Nested checkout initialization and the pre-existing report/spec edits were preserved. The subsequent owner request **“update to eventstore 3.127.0”** selects exact tag `v3.127.0`, source commit **`e1d8b17160202761ab5c9606cafbcf4d1a4b92c3`**; EventStore is detached at that release and its updated `main` branch is preserved. Owning Builds **`38de8cc6c8dc7f14bec94781e050a2721f4e2c1c`** already pins the family to **3.127.0**, and the existing configuration guard already expects that version.

`dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 --nologo` passes in **10.80s**, and the corresponding `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` build passes in **8.41s**; both have zero warnings/errors. `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*AppHostSecurityConfigurationTests' -result-xml /tmp/memories-pull-submodules-cpzyziyu/eventstore-3.127.0-configuration-tests.xml` passes **11/11**, zero failures/errors/skips/unrun, **0.119s**. Server restore assets contain only EventStore **3.127.0** libraries. Builds' `pwsh -NoLogo -NoProfile -File Tools/validate-central-package-versions.ps1` passes **307 entries**. Pull receipts and final version/test readback are retained under `/tmp/memories-pull-submodules-cpzyziyu/`.

The native candidate blocker remains: pulled Platform **`f7c62ca1007f5218dc52d6856395324704564e3a`** commits a Builds gitlink to **`4acb6e6b7074b2994c3abaa89b08b2b07e8e94a2`**, whose committed catalog selects **3.126.1**. This was checked with `git -C references/Hexalith.Platform ls-tree HEAD references/Hexalith.Builds` and `git -C references/Hexalith.Builds show 4acb6e6b7074b2994c3abaa89b08b2b07e8e94a2:Props/Directory.Packages.props`. Root source/catalog alignment does not replace the native model's committed Platform catalog authority. No nested submodule, Platform pin, private candidate or installed runner was changed for this follow-up; no launch, staging, commit or push occurred. Story 27.4 and A41 remain open.

### EventStore candidate adoption — 2026-10-23; blocked by native catalog pin

**Preparation is incomplete; no operational authorization is ready.** Fresh private `ADOPT=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-eventstore31270-20261023-fz54y5zo` physically reuses `DIAG/compatible-platform`. It adopts only approved EventStore release **`e1d8b17160202761ab5c9606cafbcf4d1a4b92c3`** source/build/test inputs, the **3.127.0** family pin and the existing catalog guard. All **3929** adopted release files match the Git archive except `Directory.Packages.props`, which retains the earlier version-preserving private metadata correction. Prior compatible capture code, tool/configuration and all **307** retained assertion/capture inputs are byte-equal. There are no links into shared source, nested submodule initialization or changes to previous evidence/candidates.

| Actual offline check | Result / retained evidence under `ADOPT` |
| :--- | :--- |
| Supported 4.35.2 version, direct source mapping and native manifest validation | Exit 0. Mapping **`1c6be394c76dce70a17114e8a642fdb5ff96e2459f9dc26190ae3adbd72fd3f3`** selects the physical owner/references. **HXW009** reports the intentionally dirty private Builds catalog; it is a source-mode warning, not evidence that the native model uses the edited catalog. Resolver and native-model probe Tooling hashes match installed 4.35.2 **`a923a77ea4488d1b7162134ed89d951dfebd2398ccae92f74d0a087df336fd9a`**. |
| Final mapped Server / compatible working-catalog model / configuration-project Debug builds | Pass, respectively **11.853s / 6.409s / 13.180s**, zero warnings/errors. Final mapped Server assets resolve EventStore Client/Contracts **3.127.0 as projects**. Server SHA-256 **`4917b74d4fd4b58fc9cfc49a5e6d56b39592bd6d226a44689cbb080e8a95600a`**; working-catalog model **`664169310a3b5f87eebd8d8448ce43a84af55c645a5f85e1eb5a17be90a925c2`**. Matching DLL/PDB identities, source checksums, unchanged Server correction and capture/registration sources are retained. These are preparation outputs. |
| Existing configuration / public capture regressions | Ordinary package-contract configuration **11/11**, zero failures/errors/skips/unrun, **0.145s**; actual public capture-service tests **8/8**, zero failures/errors/skips/unrun, **0.449s**. A single byte-equal ordinary catalog fixture supplied the standalone-layout guard without `.git`; it was removed after checks. No test expectation changed. |
| Existing central / platform catalog validators | Both exit 0; **310** candidate entries; EventStore Aspire **3.127.0** and unchanged hosting tuple accepted. The three extra candidate entries are retained preparation compatibility entries, not newly updated dependency versions. |
| Installed native `PlatformManifestCommand.BuildModelAsync` | Actual installed public API, offline only: exit 0, **31.357s**, no AppHost/DCP/topology launch. It verifies/materializes Platform's committed catalog and excludes the consumer source-mapping imports. The resulting model SHA-256 **`00666773b7fb5e82774a4c3110e44d1902ac5aaab4b12f54aff5f97ba6618c3f`** binds the capture sources, but all four inspected EventStore assemblies report **3.126.1.0**. The materialized catalog also selects **3.126.1**. Native build stdout/warning totals are not returned by this public API. |

**Concrete blocker:** candidate Platform identity **`261779bfa862a40a8db648cfe3f7c2619704c2ad`** pins Builds **`66b9136a0b223925e51177a51eb48852e2f12787`**, whose verified catalog selects 3.126.1. Editing the working catalog does not change the native model's committed catalog. The concurrent owner push is preserved: Memories HEAD **`494d130400cf3e06ce24410584b03c45596d892b`** now selects committed owning Builds **`58cebcca755eb80b678a1d2870e3779ea7eb5349`** with 3.127.0. Current Platform **`858d02c05eb837a4ee85c5215430201f117dc24d`** still pins Builds **`ba5e371472c832261ae2c3f6f99c1928b6cac780`**, also 3.126.1. No matching approved compatible Platform pin has been selected. Required reopening is that matching genuine Platform/catalog identity, preserving approved source compatibility and other dependency versions, followed by supported re-resolution and affected native model/DLL/PDB binding. This preparation creates no commit, staged entry, branch, synthetic identity or catalog bypass.

Exact commands/cwds/timestamps/results: `verification.json`; source/archive proof: `input-adoption.json`, `catalog-adoption.diff`, `selected-input-readback.json`; artifacts: `prepared-output-identities.json`, `final-binary-provenance.json`, `prepared-server-correction-binding.json`, `native-model-binary-provenance.json`; native contract: `installed-release-PlatformManifestCommand.cs`, `installed-release-CompositionProcess.cs`, `installed-native-model-build.log`, `native-catalog-blocker.json`. The native catalog is in `native-model-workspace/source-mapping/catalog-versions.json`. Probe/installed identities are retained separately. Native Dapr SHA-256 remains **`be0d0ee7362c6b8626538876fa2342d1c0789d126a60129a7bf27009a072769c`**.

Both additional failed checks remain authentic. Applying the Memories consumer mapping to Platform's own composition project produced **HXW006** for `Platform.Works.Runtime`; exact installed source confirms the actual native model build excludes those imports, and the actual API succeeds. The first configuration run passed **10/11** and failed the unchanged package-only assets assertion because correct mapped source assets were projects. The same unchanged guard passed **11/11** against its intended ordinary package restore at 3.127.0; final mapped source outputs/assets were then restored and rebound. Initial XML/logs, ordinary package assets and final mapped assets remain distinct. No guard was weakened. Historical broad-gate failures elsewhere in this report remain historical; these focused results do not claim a full suite pass.

#### Conditional replacement plan — not ready for authorization

`ADOPT/run-plan.json` and `ADOPT/launch-command.txt` retain this concrete conditional proposal. Resolve the native catalog blocker and obtain one root review of the ready plan before seeking its single operational authorization. **No launch/restart is authorized by this preparation.**

| Item | Conditional scope |
| :--- | :--- |
| Cwd / owner / state / evidence | Cwd **`ADOPT/compatible-platform`**; owner **`ADOPT/compatible-platform/references/Hexalith.Memories`**. Fresh **`ADOPT/local-share`** is empty; owner-relative result **`artifacts/retention/prepare-spec14-eventstore31270-20261023-fz54y5zo/functional.json`** is absent. Preserve all earlier resources and source/evidence. |
| Absolute window | At **2026-10-23T15:04:06Z** the proposed **2026-10-23T15:30:00Z–2026-10-25T10:27:22Z** start window is feasible. **14400s execution**, including **600s startup**, plus separate cumulative **900s stopping/capture/disposal**. Absolute execution/cleanup ceilings **2026-10-25T14:27:22Z / 14:42:22Z**. Recheck actual UTC after identity correction; a missed window requires replacement and grants no budget reset. |
| Stack / resources | Supported tool **4.35.2**, source/**Debug**, approved EventStore **3.127.0 after native identity correction**; unchanged Dapr **1.18.1** / PostgreSQL **18.6** immutable images, TLS `verify-full` and security. Existing **7 containers / 15 resources**, no VM/disks; admission floors **8 CPUs**, Docker/Linux available **16 GiB**, staging and `/mnt/c` **20 GiB each**. PostgreSQL caps **8 CPUs / 16 GiB including swap / 100 connections / Dapr pool 40 / 400 GiB tmpfs maximum**; no whole-graph reservation. Check actual capacity/isolation once immediately before an authorized launch. |
| Attempts / assertions | **One launch; zero startup retries; no automatic restart.** Preserve 125/125 A and B, ≥1800 real seconds separation, 3600s retention / 900s purge grace, strong reads/absence, stored rows/TTL, due-index/reminder progress, canonical B preservation and signed UTC/monotonic observations. Existing bounded reads and one same-identity uncertain-submission reconciliation are unchanged. |
| Admission before the long wait | Require corrected actual native pinned catalog and loaded EventStore **3.127.0** source/version binding, loaded Server DLL/PDB to approved correction sources, loaded composition DLL/PDB to all five capture/registration sources, and **all four sidecars' real console output plus capture-started and attributable PID/state evidence**. Preserve generated runtime configuration and loaded dependencies. Missing/mismatched proof consumes the launch and triggers bounded stopping; no retry. |
| Capture / exact-run disposition | After authorization only, start the two retained capture scripts as `python3 -B ADOPT/<script> ADOPT` and record PIDs. Preserve native evidence before supported exact-run `down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --run-id <actual-new-run-id> --output json` under the same cwd/environment. Native stopping, captures/children, down and verification share **900s** from first stopping or terminal return, whichever is earlier. Require `removed`, `unverified:false`, empty ownership sets, fresh exact-run Docker-label scan empty and four stopped-capture receipts. DCP can end observation before native cleanup: disclose unavailable terminal/exit evidence and truncation/persistence failures; never infer clean native exits. No broad prune or old-resource disposal. |

Conditional native command (**not executed; blocked**):

```bash
cd /home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-eventstore31270-20261023-fz54y5zo/compatible-platform
umask 077
XDG_DATA_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-eventstore31270-20261023-fz54y5zo/local-share \
HEXALITH_DAPR_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home \
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/prepare-spec14-eventstore31270-20261023-fz54y5zo/functional.json --output json
```

No retention test, AppHost or real topology was launched/restarted; no shared source changes, staging, commits, pushes or branches were made by this preparation. Local retention, historical initiating sidecar cause, full qualification, Production and A41 remain open. The unchecked Spec 14 tasks stay incomplete while native admission is blocked.

**Root ordinary review:** the documentary diff, exact installed catalog/build contract, native dependency hash readback and all **19** executed passing configuration/capture test records were reviewed. No documentary finding remains unresolved. `ADOPT/root-review.json` accepts the blocked evidence only; native catalog admission and review of a ready operational plan remain incomplete. Scoped documentary whitespace checks pass.

### Owner-directed EventStore update — 2026-10-23

The owner requested **“update to latest eventstore version”**, authorizing this EventStore dependency change after the earlier no-version-change instruction. The latest stable [EventStore release **3.127.0**](https://github.com/Hexalith/Hexalith.EventStore/releases/tag/v3.127.0) was published **2026-10-23T11:39:50Z** and was verified on NuGet for Client and Aspire. Memories' direct source reference now selects release commit **`e1d8b17160202761ab5c9606cafbcf4d1a4b92c3`**, a descendant of the previous `61122209dea0fcdd2d64bf5eaa903c4a8242df4f`. No nested submodule was initialized or updated. The owning Builds central `HexalithEventStoreVersion` moves **3.126.1 → 3.127.0**; all EventStore package-family rows still use that single property, and the existing Memories catalog/restore guard now expects 3.127.0.

Workspace verification passes: Server Debug build **7.14s**, test-project Debug build **6.47s**, AppHost Debug build **7.43s**, each **zero warnings/errors**; existing `AppHostSecurityConfigurationTests` **11/11**, zero failures/skips, **0.115s**. The Server assets resolve Client and Contracts **3.127.0**; the source-mode AppHost builds the updated EventStore Aspire project. Central package validation passes **307 entries**, and the native platform-catalog validator accepts EventStore Aspire **3.127.0** with the existing hosting tuple. Exact commands/results, release receipts, native XML and patches are retained in `DIAG/eventstore-3.127.0-update/verification.json` and adjacent logs. These are build/configuration checks, not live retention or sidecar-cause evidence.

**The retained private candidate and unexecuted run proposal below predate this workspace update.** They remain unchanged as evidence and are not ready to authorize a 3.127.0 execution until the affected EventStore source/catalog inputs are adopted, rebuilt and rebound in that target. Reuse unaffected preparation; do not repeat the preparation ceremony. The owner has not authorized a launch/restart. Installed Platform tool **4.35.2**, native Dapr **1.18.1**, PostgreSQL **18.6**, source/Debug requirements, real timing, assertions, retries and resource limits remain unchanged. No staging, commit or push occurred. The historical sidecar exit cause remains unknown; full qualification, Production admission and A41 remain separate/open.

### Sidecar-loss diagnosis and capture correction — 2026-10-23

**Initiating exit cause remains unproved; a native evidence-capture defect is corrected offline. No new topology was launched or restarted.** Run `7448fa8d180acc30f866d87a3612e667` remains failed with both 125/125 accounting records, 48 connection refusals and verified disposal. No retention, full qualification, Production admission or A41 credit is claimed.

New private diagnostic/plan directory: `DIAG=/home/administrator/.local/state/hexalith/memories/access-telemetry/diagnose-spec14-native-capture-20261023-7wlh50dh`. Historical evidence remains at the original `PREP` below. The new directory contains commands, source patches, native XML, provenance and the proposed `run-plan.json`; it grants no execution authority.

| Executed provenance / diagnosis | Finding |
| :--- | :--- |
| Installed runner | Retained AppHost process command names installed tool **4.35.2**. Actual AppHost SHA-256 `4ec10ca0ae745cf55e4370ac78c34308d2941a358cd7afa1f6ef0fc0a8b01b5e`; Tooling `a923a77ea4488d1b7162134ed89d951dfebd2398ccae92f74d0a087df336fd9a`. Both match the failed run's selected identities. DLL/PDB identities agree; informational version and SourceLink identify **`4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`**. All **175 non-generated source documents** match that Git revision using the recorded Git/LF/CRLF working-tree byte form. The packaged tool is Release; modeled applications/composition remain source/Debug. Current Builds HEAD is not substituted for executed source. |
| Executed Platform model | Retained **runtime** model DLL SHA-256 **`077d37627678ed1b3c0069472610aaadd1c3bc05e2885014a03a942f0adb1bfd`**, Debug; its PDB identity agrees and **26/26 available source documents** match the retained private tree. Its assembly revision is `261779bfa862a40a8db648cfe3f7c2619704c2ad`, but the private composition includes the earlier compatible source and corrections; that revision alone does not describe its source. The preparation DLL `66c8d67b…` is a separate artifact, not the executed DLL. `PlatformDaprEndpointSubscriber` in this actual model selects the verified native runtime directly. |
| Actual sidecars | Readiness reports verified native runtime hash **`be0d0ee7362c6b8626538876fa2342d1c0789d126a60129a7bf27009a072769c`**; retained listener ownership names `daprd`. The `-dapr-cli` resource suffix does not identify the executed binary as Dapr CLI. Earlier pinned OCI/native verification remains applicable. |
| Exit-cause limits | EventStore was missing at about **3600.604s**, but the other sidecars at about **3568.9s** of their own lifetimes. This does not establish a common one-hour process timeout. The pinned AppHostRunner/StopSignal/retained-stop paths inspected contain no one-hour lifetime bound; the actual four-hour test deadline had not elapsed. Retained Sentry configuration uses **24h** workload certificates. Seven retained container inspections have `OOMKilled:false`; accessible kernel records for the failure interval have no matching owned PID. These observations do **not** rule out a native signal, OOM, fatal runtime error or other initiating cause. Native exit status/full console output was not retained and cannot be reconstructed. |
| Supported log-access defect | Official exact-AppHost logs refused its nonexistent registered synthetic project path; the retained AppHost writer emits only `apphost.output.observed`. Native process stdout/stderr were pipes; no competing pipe reader was introduced. This explains missing evidence, not sidecar termination. |

`DIAG/executed-provenance.json`, `release-source-checks.json`, `executed-runner-hash-readback.json`, `retained-container-state-summary.json`, `kernel-evidence-availability.json` and the retained pinned lifecycle source extracts support these findings. Source provenance was established before choosing capture-only correction; no lifetime, restart, retry, security, certificate or retention behavior was changed.

**Owning correction:** `/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Composition/PlatformAppModel.cs` now enrolls the selected retention sidecars in `PlatformDaprDiagnostics`, with separate resource/file/redactor types. It uses public Aspire **`ResourceLoggerService.WatchAsync`** and **`ResourceNotificationService.WatchAsync`**, compatible with the installed 4.35.2 hosting assemblies (**Aspire.Hosting 13.6.1**). It subscribes before resources start and retains already-delivered public records through disposal with a three-second drain bound. **DCP stops its own observation producers before native cleanup**, so ordinary host stopping can omit shutdown console/exit snapshots; retaining our subscriptions cannot recover those events. Native failures reported while DCP still observes the run remain capturable. No installed tool patch, process wrapper, synthetic AppHost or second runtime runner is required. The [upstream public logging implementation](https://github.com/dotnet/aspire/blob/v13.6.1/src/Aspire.Hosting/ApplicationModel/ResourceLoggerService.cs) was consulted; actual installed signatures and tests determine compatibility.

Evidence stays outside disposable runtime workspaces at `<Memories-owner>/artifacts/retention/<actualRunId>/diagnostics/native-sidecars/`. Each of four sidecars has a **262144-byte console tail** and **65536-byte state tail**, with explicit dropped-record counts; total retained payload at most **1310720 bytes**. Directory/files use **0700/0600** on this Linux target. Messages are bounded and redacted before persistence. Metadata admits resource identity, reported current/last PID, state, start/stop timestamps, exit code and observed UTC/elapsed time; arguments, environment and arbitrary resource properties are excluded. Public snapshots do not expose an initiating OS exit reason: metadata explicitly says **`unavailable-from-public-resource-snapshot`**. Console messages may establish a native cause on the next run; no such future result is assumed. Receipts separately report `TerminalStateObserved`, `ExitCodeObserved` and `TerminalEvidence`; a stale `Running` snapshot is explicitly incomplete even when delivered-record persistence has `CaptureFailed:false`. The reported controller state `Terminated` is preserved. Capture failures/missing receipts or unavailable terminal evidence prevent claiming complete terminal capture. Cleanup emptiness is independently verified; it is not inferred from these receipts.

**Focused verification:** the final linked-source Debug build against the installed tool assemblies passes with **zero warnings/errors**. Direct xUnit command `dotnet DIAG/native-api-tests/bin/Debug/net10.0/native-api-tests.dll -class '*PlatformDaprDiagnosticsTests' -result-xml DIAG/final-capture-tests.xml` passes **8/8**, zero failures/errors/skips/unrun, **0.647s**. It exercises the actual public log/notification services, first and shutdown output, reported PID/exit metadata, foreign-resource exclusion, private permissions/redaction, bounded truncation preserving the last error, capture failure, idle cancellation/drain, refusal to overwrite existing evidence, idempotence, DI-owned application disposal, reported `Terminated` state, and explicit terminal incompleteness for a final stale `Running` snapshot. No DCP/topology is started by these tests; emitted fixture events establish delivered-event handling, not native cleanup observation.

The compatible isolated Platform source build against installed 4.35.2 passes **zero warnings/errors, 3.951s** after review fixes (initial successful source build **14.135s**). Exact final commands/cwds/timestamps: `DIAG/final-verification.json`; output: `final-model-build.log`. Prepared model SHA-256 **`7d3ade28ab36d05700a718eef378ecc577990c7a30c9f27896d58e5b22714108`**; DLL/PDB and all five capture/registration source checks match in `prepared-model-final-provenance.json`. Native manifest validation returns `validated`, exit 0, no diagnostics; supported final resolver (`source-resolution-final.{json,stdout,stderr}`) returns direct Memories ownership, no identity findings, mapping **`923f1e0b1678ffab9eb47124b697245d7e98497808451ca359eca1a18f957e89`**. Of the prior **303 selected inputs**, **302 are byte-equal** in the selected physical copy; only compatible `PlatformAppModel.cs` changes, plus four new capture files. Unchanged assertions/imports, native Dapr, manifest and packaged runner are reused; no unused-module census or repeated source-freeze ceremony was imposed.

The ordinary owning test-project command `dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1 --nologo` remains blocked by pre-existing EventStore `CS0234` at `EventIdentityValidator.cs:1` (`Hexalith.Commons` missing), **0 warnings / 1 error**; `DIAG/shared-test-project-build-blocker.log`. The focused fallback above is separate from that broad gate. Initial capture tests failed **4/5** because final `GetAllAsync` lacked its native console service in the offline fixture; the correction drains public streams instead. Initial copy builds exposed an absolute `references` link and missing root metadata; the fresh copy was repaired to physical direct references with genuine resolver metadata. Those failed commands/logs and the later sixth-test fixture compiler correction remain retained. Temporary builds through the initial link touched generated intermediates; original selected source and retained model/loaded evidence are checked separately in `historical-input-artifact-readback.json`. No historical failure was replaced with passing evidence.

**One ordinary review is complete.** Both findings were verified and corrected: preserve the actual `Terminated` state, and distinguish delivered-record drain from unavailable native shutdown/terminal evidence. Final focused regressions pass **8/8**; no review finding is deferred or unresolved. Source-based verification confirms [DCP stops watchers before cleanup](https://github.com/dotnet/aspire/blob/v13.6.1/src/Aspire.Hosting/Dcp/DcpExecutor.cs#L253), so shutdown completeness is a disclosed limitation, not a guaranteed future result. Review/triage and final commands: `DIAG/ordinary-review.json`, `final-verification.json`. Functional retention and initiating cause remain open; no topology was launched. Memories and scoped capture whitespace checks pass. Whole-Platform `git diff --check` returns **2** for existing concurrent trailing whitespace at `PlatformRetentionProfile.cs:243`; that unrelated edit is preserved (`DIAG/whitespace-checks.json`).

### Fresh native-capture run plan — awaiting one operational authorization

**Superseded for execution by the owner-directed EventStore 3.127.0 update above; retained proposal only until affected-input adoption/revalidation.** This is one proposed diagnostic execution of the existing test, following capture correction. **All previous launch permissions are consumed. No launch/restart is authorized yet.** The plan is `DIAG/run-plan.json`; command is `DIAG/launch-command.txt`.

| Plan item | Concrete scope |
| :--- | :--- |
| Source / runner | Cwd **`DIAG/compatible-platform`**, owner **`DIAG/compatible-platform/references/Hexalith.Memories`**. Reuse prior compatible source/configuration and the small capture patch. Supported **4.35.2**, source/**Debug**, unchanged immutable **Dapr 1.18.1 / PostgreSQL 18.6**, verify-full TLS, original resource/credential ownership. |
| Fresh state / result | **`DIAG/local-share`**, currently empty. Fresh owner-relative evidence **`artifacts/retention/diagnose-spec14-native-capture-20261023-7wlh50dh/functional.json`**, currently absent. Native sidecar tails are under owner `artifacts/retention/<actualRunId>/diagnostics/native-sidecars`. Preserve original `PREP` and all earlier resources/evidence. |
| Absolute window | One start **2026-10-23T12:15:00Z–2026-10-25T10:27:22Z**. Nonrenewing **14400s execution**, including **600s startup**, plus separate cumulative **900s stopping/capture/disposal**. Absolute execution/cleanup ceilings **2026-10-25T14:27:22Z / 14:42:22Z**. A missed window requires a new plan; old budgets are not reset. |
| Resources | Same **7 containers / 15 resources**, no VM/new disks. Admission floors: Docker ≥8 CPUs and ≥16 GiB; Linux available ≥16 GiB; staging and `/mnt/c` free ≥20 GiB each. PostgreSQL caps **8 CPUs / 16 GiB including swap / 100 connections / Dapr pool 40 / 400 GiB tmpfs maximum**. No whole-graph reservation is implied. Check actual capacity/isolation once immediately before the approved launch; no fresh capacity claim is made now. |
| Attempts / assertions | **One launch, zero startup retries, no automatic restart**. Uncertain cohort submission may reconcile the same identity once. Preserve **125/125**, ≥1800 real seconds cohort gap, **3600s retention / 900s purge grace**, all strong reads, stored rows/TTL, due-index/reminder progress, canonical B preservation and signed UTC/monotonic checks. Reads remain three attempts/key, 2s/5s backoff, 16 workers, one shared 120s observation budget. |
| Actual-input admission | Require supported direct mapping, relevant input identities, fresh state/result and allocated-port/retained-resource isolation. During startup/first cohort, bind actual loaded Server DLL/PDB to the retained approved correction sources and actual loaded composition DLL/PDB to capture sources/registration. Before entering the long wait, require actual console output plus capture-started/PID evidence from **all four** sidecars. Missing/mismatched proof or capture consumes the launch and triggers bounded stopping; never an automatic retry. |
| Capture / disposition | After approval start the two existing capture scripts with `python3 -B DIAG/<script> DIAG`, record PIDs, then the native command below. Preserve native plan/model/configuration, loaded artifacts, both accounting files, failed transport diagnostic if present, native TRX and passing-only observations. On terminal/failure stop only this actual run with the supported `down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --run-id <actual-new-run-id> --output json` under the same cwd/environment. Native stopping, capture/child termination, down and fresh verification share **900s** from first stopping activity or terminal return, whichever is earlier. Require `removed`, `unverified:false`, empty process/container sets, fresh exact-run Docker label scan empty, and all four capture-stopped receipts with explicit persistence, truncation and terminal-state/exit-code availability. DCP can end observation before ordinary cleanup; unavailable terminal evidence must be recorded as incomplete and cannot prove clean exits or an initiating cause. Record incomplete disposal/capture honestly; no broad prune or old-resource cleanup. |

Proposed native command (**not executed**):

```bash
cd /home/administrator/.local/state/hexalith/memories/access-telemetry/diagnose-spec14-native-capture-20261023-7wlh50dh/compatible-platform
umask 077
XDG_DATA_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/diagnose-spec14-native-capture-20261023-7wlh50dh/local-share \
HEXALITH_DAPR_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home \
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/diagnose-spec14-native-capture-20261023-7wlh50dh/functional.json --output json
```

The single requested authorization covers this setup, test and ordinary exact-run stopping/disposition. It does not admit Production or close A41. No sidecar lifetime fix is proposed without an established cause.

### Diagnostic live run — 2026-10-23; failed, disposal verified

**Executed once; functional retention did not pass.** The owner replied **“I approve”** to the complete prepared plan below. Native run **`7448fa8d180acc30f866d87a3612e667`** executed **09:18:03.098695Z–10:19:11.191117Z**, returning **CLI exit 4 / HXT006**. The native TRX has **one executed, one failed, zero passed** and assertion **`functional-selected-state-read-transport-exhausted`** at A's first pre-expiry strong read. No passing `functional-observations.json` exists. Expiry/purge and final canonical B preservation remain unproved; the failure does not establish record loss.

| Actual observation | Evidence / result |
| :--- | :--- |
| Admission and startup | All **303 selected inputs / 141 prepared output files** unchanged; supported direct mapping valid and fresh state/evidence. Docker 14 CPUs / 97,021,218,816 bytes; Linux 55,705,808,896 available bytes; staging/backing 83,081,265,152 / 1,440,928,604,160 free bytes. Six other labelled containers excluded. All **15 resources** healthy at **09:19:56.0679046Z**, within 600s; 21 allocated ports clear of prelaunch listeners. |
| Runtime and limits | Supported **4.35.2**, source/Debug; immutable Dapr 1.18.1 and PostgreSQL 18.6 unchanged. Actual PostgreSQL **8 CPUs / 16 GiB including swap**; all three services report **3600s retention**. The actual Server DLL/PDB binds all three correction checksums; all **16 native loaded hashes** match. All **201 retained output files** and the **303 selected input identities** remain unchanged at final readback. |
| Cohorts | A at **09:19:57.998Z**, B at **09:49:57.875Z**: attempted/enqueued/persisted/acknowledged/transaction acknowledgements **125** each, rejected/dropped/conflicted **0**. These response observations are not separate emission-clock measurements. The native sequence advanced through the real cohort gap and initial reads; later retention assertions did not complete. |
| Sidecar availability | At **10:18:31.478441Z**, EventStore's sidecar launcher was first observed missing, approximately **3600.604s** after its retained kernel process start. By **10:18:35.506679Z**, all three Memories sidecar launchers were missing too; selected HTTP listeners fell **6 → 3**. AppHost and application processes remained present. The bounded read-only trace records this sequence; the initiating exit cause is unresolved. |
| Read failure | **48 `connection-refused` / `open` failures**, covering the first 16-key batch and three attempts per key. Existing attempts, 2s/5s backoff, 16 workers, shared 120s budget, security refusals and retention assertions were unchanged. The native failed terminal is **10:19:09.6853236Z**. Aggregate native counters have `reported: false`; their zeros are not a passing result. |
| Disposition | Supported exact-run `down` returned **exit 0** at **10:22:38.073994Z** in **3.681s**. At **10:23:39.855987Z**, actual disposition was **`removed`**, `unverified: false`, owned process/container sets empty, and a fresh exact-run Docker scan empty. All capture processes and children stopped. Conservatively charging from the earliest sidecar disappearance, total stopping/capture/disposal was **308.377546s**, within **900s**. Earlier resources/evidence were excluded. |

The native execution deadline was **13:18:03.098695Z**, with startup inside the same 14400s allowance; it was not reached or reset. **One launch is consumed; zero startup retries, no automatic restart and no further launch occurred.** The original native report preserves its pre-disposal retained snapshot; the actual later terminal/disposition is recorded separately, without rewriting original evidence.

Private evidence root remains `PREP=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-transport-20261023-6yl941ny`. Authentic native files remain at `PREP/platform/references/Hexalith.Memories/artifacts/retention/prepare-spec14-transport-20261023-6yl941ny/functional.{json,python3.trx}` and unchanged copies `PREP/original-native-functional.{json,python3.trx}`. `operational-approval.json`, `native-invocation.json`, `actual-run.json`, `native-completion.json`, `loaded-server-proof.json`, `loaded-artifacts.json`, both terminal readbacks, `native-down.{json,stdout}`, `actual-terminal-{before,after}-down.json`, `capture-stop.json`, `final-disposition.json` and `launch-result.json` retain commands, identities and results.

`runtime-inputs/7448fa8d180acc30f866d87a3612e667/terminal/` preserves the actual plan/readiness, platform/Dapr configuration, both accounting files, and private sanitized transport diagnostic before disposal. The bounded native-evidence collector retained the native JSON outputs as they appeared; `native-evidence-receipts.json` supplies source/artifact hashes. `owned-listener-process-trace.jsonl`, `selected-listener-midrun.json`, `sidecar-lifetime-observation.json`, container logs/inspections and owned Docker events retain the availability timeline. No process pipe was consumed. The official exact-AppHost Aspire log command refused its nonexistent registered project path; that limit and the non-mutating observer-helper refusals are retained. Inspected public certificate validity extended beyond the failure, but native sidecar exit codes/full console logs are unavailable.

**Outcome and next owner:** the new evidence establishes unavailable sidecar endpoints before the failed read, not the reason they exited. Platform/Builds owns diagnosis of that sidecar loss and attributable native process-log access before another concrete run is prepared. This attempt proves neither one-hour retention nor full qualification; **Story 27.4, Production credit and A41 remain open**. No new launch authority is inferred.

### Prepared diagnostic run — 2026-10-23; historical snapshot

**Preparation snapshot; the owner-approved execution is recorded above.** At preparation there were zero launches and no operational authorization. All earlier launch permissions remain consumed. The latest live failure below stays authentic: both cohorts accounted for 125/125, but the first pre-expiry strong read exhausted transport attempts. Its retained container logs do not establish the cause; native Dapr/.NET process logs and per-attempt classifications were not retained. No runtime repair or retention success is claimed.

The existing consumer now writes an exclusive **0600** `native-tests/state-read-transport-failure.json` when a strong-read observation fails after transport uncertainty. It contains only fixed category/phase, ordinal/attempt, UTC and elapsed time, bounded to 125 fixed keys × three attempts. Keys, URLs, credentials, exception text and response bytes are excluded. Three attempts, 2s/5s backoff, 16 workers, the shared 120s read budget, HTTP security refusals and write semantics remain unchanged. Original two-cohort, signed-clock, stored-row/TTL, strong-read, expiry/purge and unchanged-B assertions remain the acceptance criteria.

| Prepared item | Concrete plan / evidence |
| :--- | :--- |
| Isolated source | `PREP=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-transport-20261023-6yl941ny`; Platform `PREP/platform`; owner `PREP/platform/references/Hexalith.Memories`. Fresh copy of the earlier ready source/configuration and real Git metadata; no original checkout, prior evidence or nested submodule changed. Compatible composition and existing private EventStore metadata correction retained. |
| Tool/configuration | Supported **4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0**, source/Debug, immutable **Dapr 1.18.1 / PostgreSQL 18.6**, verify-full TLS and **3600s retention**. Seven container images available; installed runtime verifier confirms the native daprd against the pinned OCI archive. No version changes. |
| Fresh paths | State `PREP/local-share/`; owner-relative evidence `artifacts/retention/prepare-spec14-transport-20261023-6yl941ny/functional.json`. Both empty; capture scripts unstarted. `run-plan.json` and `launch-command.txt` contain this proposal; no operational approval or native run exists. |
| Absolute window | One approved start **2026-10-23T09:15:00Z–2026-10-25T10:27:22Z**. Nonrenewing **14400s** from the initial native request, including **600s** startup; separate cumulative **900s** stopping/capture/disposal. Absolute execution/cleanup ceilings **2026-10-25T14:27:22Z / 14:42:22Z**. No old budget is extended or reset. |
| Resources / attempts | Existing **7 containers / 15 resources**, no VM/new disks. Docker ≥8 CPUs; Docker memory and Linux available each ≥16 GiB; staging and `/mnt/c` free each ≥20 GiB. PostgreSQL caps **8 CPUs / 16 GiB including swap / 100 connections / Dapr pool 40 / 400 GiB tmpfs maximum**; these are not whole-graph reservations. **One launch, zero startup retries, no automatic restart.** Uncertain cohort submission may reconcile the same identity once. |
| Admission / ownership | Check actual capacity/isolation, relevant inputs, supported direct mapping, fresh state/evidence and allocated ports once immediately before the approved launch; refuse a failed check without retry. Preview at **09:01:05.832684Z**: Docker 14 CPUs/97,021,218,816 bytes; Linux58,511,392,768 available bytes; staging/backing90,301,444,096/1,434,703,368,192 free bytes. Six other labelled containers are excluded. This preview is not admission. |
| Input / binary binding | Existing **303 selected inputs** retained as the relevant set; preparation outputs separately recorded. Supported resolver has no identity findings and direct mapping `147771b387b93c6bd12933f1f9705afd849798dc55781a4c7b3e0d36739b5e42`. Prepared Server DLL/PDB binds all three unchanged admission-correction source checksums. During startup/first cohort, bind actual per-run loaded DLL/PDB/dependencies from native readiness and repeat the retained binary proof; missing/mismatched proof consumes the launch and requires bounded stopping. Unrelated documentation cannot invalidate the input set. |
| Disposition | Preserve actual inputs/logs, both accounting files, transport diagnostic if present, native TRX and passing-only observations before supported exact-run `down`. Native stopping, capture/child termination, down and fresh verification share 900s from first stopping activity or terminal return, whichever is earlier. Require actual `removed`, `unverified: false`, empty owned process/container sets and fresh exact-run Docker labels empty. Record failure/withdrawal/unverified disposal explicitly. Preserve this source/evidence copy; all earlier resources/disks/evidence stay excluded. |

Proposed command, **not executed; requires fresh operational authorization**:

```bash
RETENTION_PREP=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-transport-20261023-6yl941ny
cd "$RETENTION_PREP/platform"
umask 077
XDG_DATA_HOME="$RETENTION_PREP/local-share" \
HEXALITH_DAPR_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home \
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/prepare-spec14-transport-20261023-6yl941ny/functional.json \
  --output json
```

After approval, start the existing capture scripts as `python3 -B "$RETENTION_PREP/capture-runtime-inputs.py" "$RETENTION_PREP"` and `python3 -B "$RETENTION_PREP/capture-container-logs.py" "$RETENTION_PREP"`; record PIDs and stop them plus children within the same stopping budget. Record actual invocation UTC/monotonic start/deadline. Under the same cwd/environment, cleanup is `dotnet tool run hexalith-module -- down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --run-id <actual-new-run-id> --output json`, using only the ID returned by this invocation. Do not construct a handoff or use the historical VM checkpoint.

**Verification:** selected consumer suite **49/49** (228.838s), isolated retry/diagnostic lane **19/19**, existing source/archive guard tests **4/4**. Isolated Debug Server and compatible model builds pass with **0 warnings/errors**, **6.666s / 6.667s**. Fresh build exposed a concrete guard gap: Memories did not recognize its genuine enclosing Platform checkout. `Directory.Build.props` now supplies that checkout as `ResolvedRoot` only when the enclosing `Hexalith.Platform.slnx` and `.git` exist. `CheckSubmodules` remains active; an explicitly nonexistent root still fails. No nested submodule or fabricated metadata was used. The initial build refusal and resolver copy-omission findings are retained; exact omitted catalog documentary files were restored for the supported bounded Git-status check, without treating them as frozen runtime inputs.

All commands/results are in `PREP/{selected-suite,candidate-diagnostics,source-archive-guards,server-build-corrected,model-build,missing-platform-root-refusal,source-resolution-complete,binary-proof,runtime-artifact-verification}.{json,log}`; identities, current image observations and preparation preview/check remain beside the plan. Existing external packages remain despite source-reference selection; no wholly external-source build is claimed. **Local retention remains unproved; Story 27.4, full qualification, Production credit and A41 remain open.**

Ordinary preparation review (2026-10-23): root inspected the complete five-file continuation diff against `b55db82c060caf917a142fdb6585fa7340b01647`; no blocking finding remains. The inherited Spec 14 baseline and sprint status are unchanged. This completes preparation review only; the live acceptance criteria remain pending.

Focused negative/privacy evidence was executed by `python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_selected*.py' -v` (49/49, zero failures/errors/skips). Passing cases include `test_authentication_security_and_server_http_failures_are_never_retried`, `test_withdrawal_after_failure_or_during_backoff_admits_no_retry_or_later_key`, `test_foreign_partial_malformed_source_and_short_lifetime_refuse_before_access`, `test_foreign_postgresql_claim_refuses_even_with_claimed_complete_observations` and `test_failed_read_retains_only_bounded_private_transport_categories`. The changed surface is private failure attribution; tenant routing and state key/partition selectors are unchanged. Logs and exact command metadata: `PREP/selected-suite.{log,json}`.

### Corrected-queue live run — 2026-10-22; failed, disposal verified

**Executed once; functional retention did not pass.** The owner replied **“approve”** to the complete prepared plan below. The exact command launched at **19:52:29.939073Z**, native run **`7323f05526580a313a045fc305def117`**, and returned **CLI exit 4 / failed / HXT006** at **20:53:32.663803Z**. Native TRX reports **`functional-selected-state-read-transport-exhausted`** at A's first pre-expiry strong-read checkpoint, before the stored-row/TTL, final pre-expiry, expiry/purge and newer-record-preservation checks. No passing `functional-observations.json` was produced. The exhausted transport attempts do not establish the underlying cause or prove record loss.

| Verified observation | Actual result |
| :--- | :--- |
| Admission | **19:51:58.546144Z:** all 303 recorded inputs unchanged, direct mapping valid, state/evidence fresh. Docker 14 CPUs / 97,021,218,816 bytes; Linux available 52,489,097,216 bytes; staging/backing free 183,043,448,832 / 1,727,382,372,352 bytes. No other run-labelled containers observed. Native and live container ports do not overlap prelaunch listeners. |
| Startup and limits | All **15 resources** healthy at **19:54:20.6444623Z**, within 600s. All three Memories services report **3600s retention**. Seven actual containers; PostgreSQL **8 CPUs**, **16 GiB memory including swap**, approved tmpfs cap; live configuration retained. |
| Actual loaded correction | All **16 loaded DLL hashes** match native readiness. Loaded Server SHA-256 **`f0ff0c40d2890396e78da31cc511e0300d11ca12ab892fcff2054f31e929fca2`**; matching DLL/PDB identity and all three source checksums prove the corrected queue/worker/logger. All 16 remained byte-equal at terminal capture. This is the per-run binary, distinct from the preparation build. |
| Cohort A | At **19:54:22.774Z**, attempted/enqueued/persisted/acknowledged/transaction acknowledgements **125** each; rejected/dropped/conflicted **0**. |
| Cohort B | At **20:24:22.240Z**, the same exact **125/125** accounting and zero rejected/dropped/conflicted. The native sequence advanced through its real 1800-second separation and initial cohort reads. These accounting timestamps are observations, not the emission clocks or a separate separation measurement. |
| Native failure | TRX: one executed, one failed, zero passed. The first pre-expiry strong read exhausted its existing bounded transport attempts. The aggregate JSON correctly has `testCounts.reported: false`; its zero counters are not a successful test count. |
| Disposal | Exact-run supported `down` started **20:54:33.038634Z**, returned **exit 0 / completed** in **4.126s**. Verified at **20:55:22.938586Z**: actual terminal **`removed`**, `unverified: false`, empty owned process/container sets, and a fresh exact-run Docker label scan empty. Both capture scripts and their children stopped. |

The native initial request/deadline was **19:52:30.2420139Z / 23:52:30.2420139Z**; the invocation guard's earlier **23:52:29.939073Z** deadline governed. The run ended before that deadline. Stopping/capture/disposal shared the single 900-second allowance, charged conservatively from native failed terminal **20:53:32.2878992Z**: **110.650687 seconds** through final verification. No earlier native stopping activity was observed. The one launch permission is consumed; **zero startup retries, no automatic restart and no further launch** occurred. Earlier resources and evidence were excluded from disposal. No tool/dependency version was changed.

Private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-93307043-20261022-3yvewjo3`. Native report/TRX: `platform/references/Hexalith.Memories/artifacts/retention/prepare-spec14-93307043-20261022-3yvewjo3/functional.{json,python3.trx}`. The original native report retains its pre-disposal `retained`/unverified snapshot; it is not rewritten. The updated actual terminal is retained in `actual-terminal-after-down.json` and native `local-share/hexalith-builds/g4-runs/diagnostics/7323f05526580a313a045fc305def117/selected-test.json`.

`operational-approval.json`, `prelaunch-admission.json`, `native-invocation.json`, `allocated-port-isolation.json`, `live-resource-limits.json`, `loaded-server-proof.json`, `loaded-artifacts.json`, `terminal-binary-readback.json`, `native-test.stdout`, `native-down.{json,stdout}`, `capture-stop.json` and `final-disposition.json` retain authentic commands, identities, results and disposition. Existing capture preserved generated model/source mapping/Dapr/platform-components; `runtime-inputs/7323f05526580a313a045fc305def117/` also contains both accounting records and the terminal snapshot before disposal. `loaded-artifacts/` retains actual DLL/PDB/dependency files; `container-logs/`, `owned-docker-events.jsonl` and terminal snapshots preserve all seven container logs/inspections. Capture scripts were unchanged.

**Outcome:** corrected runtime binaries and both exact 125/125 cohort accounting results are established. One-hour retention, expiry/purge and final newer-record preservation remain **unproved** because the native run failed before those assertions completed. Full qualification, Production credit, A41 and overall Story 27.4 completion remain separate/open. No repair or new live attempt is inferred from this result.

### Prepared corrected-queue run — 2026-10-22; historical snapshot

**Historical preparation snapshot, superseded by the approved execution result above.** At preparation, no live launch was authorized or executed. This proposal continues from `93307043953a02e3629488785cbedcec2ff888ac`, reusing Spec 14, its selected runner and diagnostic capture. Earlier permissions are consumed. Their latest complete-observation start has passed; this proposal requests one fresh bounded run and does not reset the old continuation.

The selected source is now an **isolated private copy**, because shared Platform inputs changed during preparation and failed compilation against the pinned tool. The private Platform composition directory and `DaprSelfHostedMtls.cs` reuse source from `4ebe6798fe8bd6c22258f20c90d6861b7137c927`, the previous Spec 14 run that reached all-resource readiness. The snapshot's root Git metadata remains `261779bfa862a40a8db648cfe3f7c2619704c2ad`; that metadata is not a claim that the restored composition bytes originate there. Root-declared dependency commits are unchanged; no nested submodule was initialized. Memories is `74e4434fe2bffa7d850df99c5919706ba080cb31`, which an external sync created with the exact three production corrections from `93307043953a02e3629488785cbedcec2ff888ac`. Original concurrent edits were preserved.

| Item | Concrete proposal |
| :--- | :--- |
| Source/tool | Private root `PREP/platform`; owner `PREP/platform/references/Hexalith.Memories`. Local tool **4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0**, source/Debug; immutable Dapr **1.18.1** and PostgreSQL **18.6**. `PREP` is the exact absolute path in the command below. |
| Fresh evidence | Owner-relative `artifacts/retention/prepare-spec14-93307043-20261022-3yvewjo3/functional.json`; parent created empty. Private diagnostics/state use `PREP` and its empty `local-share/`. No native JSON/TRX exists. The superseded shared-owner empty evidence directory was removed. |
| Current proposed window | Start once after approval, **2026-10-22T18:20:00Z–20:00:00Z** (**20:20–22:00 Paris**). Execution ≤**14400s from initial request**, including startup ≤**600s**. Separate cumulative stopping allowance **900s**. Absolute latest execution/cleanup: **2026-10-23T00:00:00Z / 00:15:00Z**. Inside Spec 14's outer window; new budget requires approval. |
| Resource bounds | Existing **7 containers / 15 resources**; no VM or new virtual disks. Docker ≥8 CPUs; Docker memory and Linux available memory each ≥16 GiB; staging and `/mnt/c` backing free each ≥20 GiB. PostgreSQL caps: **8 CPUs, 16 GiB memory including swap, 100 connections, Dapr pool 40, 400 GiB ephemeral tmpfs maximum**. These are PostgreSQL caps, not a whole-graph reservation. |
| Admission | Once immediately before approved launch: capacity, fresh ownership/evidence, relevant inputs, supported direct mapping and allocated-port isolation. Refuse if a check fails; no automatic retry. Latest preparation snapshot is `isolated-capacity-preview.json` at **18:23:34.211134Z**: 14 CPUs; Docker 97,021,218,816 bytes; Linux available 28,096,630,784 bytes; staging/backing free 172,989,685,760/1,727,818,448,896 bytes. Eighteen other run-labelled containers were observed and are excluded from cleanup. This is not launch admission. |
| Acceptance | A and B each **125/125**, zero rejected/dropped/conflicted; ≥**1800 real seconds** separation; **3600s retention**; pre-expiry strong reads and stored-row/TTL checks; A strongly absent, due-index cleanup and reminder/purge progress within **900s**; full canonical B unchanged. Signed clocks and UTC/monotonic measurements stay. |
| Attempts | **Exactly one launch, zero startup retries, no automatic restart.** Existing transport semantics remain: uncertain writes may reconcile the same cohort identity once; failed-key reads retain three attempts, 2s/5s backoff, 16 workers and one shared 120s budget. These are not test relaunches. |
| Disposal | Capture actual inputs/logs before supported exact-run `down`; native stopping, capture termination, down and verification share 900s from first stopping activity or terminal return, whichever is earlier. Require actual terminal `removed`, `unverified: false`, empty owned process/container sets and an empty fresh exact-run Docker label scan. Record failure/unverified disposal explicitly. Earlier resources/disks, original checkouts and retained evidence are excluded. Keep this private source/evidence copy. |

Exact command — **executed once under the approval above; no relaunch authorized**:

```bash
RETENTION_PREP=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-93307043-20261022-3yvewjo3
cd "$RETENTION_PREP/platform"
umask 077
XDG_DATA_HOME="$RETENTION_PREP/local-share" \
HEXALITH_DAPR_HOME=/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home \
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/prepare-spec14-93307043-20261022-3yvewjo3/functional.json \
  --output json
```

The native runner owns the nonrenewing 14400s lifetime from the initial request. Record actual UTC/monotonic start, deadline and owner process identity. Use supported cancellation on deadline/withdrawal; the old invocation guard is not reused. Cleanup under the same cwd/environment is `dotnet tool run hexalith-module -- down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --run-id <actual-new-run-id> --output json`. Obtain that ID from this invocation's native plan/result; never use a guessed ID or broad cleanup.

**Binary and compatibility proof.** Isolated Server build passed **0 warnings/errors**, exit 0, **17.685s**. Its actual DLL SHA-256 is `3a962cd4b6e877fe2a55438a7c90b4f3cd78383b10a71f51b6f07d2906876ab4`; matching DLL/PDB identity and all three portable-PDB source checksums bind it to the committed queue/worker/logger correction. Metadata also contains the incoming queue, pending FIFO, atomic capacity and marker fields. Isolated Platform composition built against the installed **4.35.2** tooling assembly with **0 warnings/errors**, exit 0, **6.281s**. Logs, exact commands and identities: `isolated-server-build-final.*`, `isolated-binary-proof*.json`, `compatible-isolated-model-build.*`, and `isolated-{server,model}-output-identities.json` under `PREP`. Existing external package references remain; no wholly external-source claim is made.

During launch, Platform builds its separate `artifacts/retention/<actual-run-id>/build/<resource-id>` outputs. They do not yet exist. In startup/first cohort, bind the **actual loaded Server DLL path/hash** from native readiness and repeat `inspect-binary.cs` against `PREP/correction-source` before accepting the functional result. Retain actual loaded DLL/PDB/dependencies and preserve existing held-file guards through completion. Missing/mismatched proof consumes the launch and causes bounded stopping without retry. The prepared DLL is not represented as a future loaded binary.

**Other preparation checks.** Supported manifest validation passes. Final installed-tool resolver exits 0 with no identity findings, direct isolated owner and mapping `f2732fa07898fd852cbdb438e09b3414aa17238df4f962be0a54517985a3eaae`. Unchanged accounting diagnostics pass **3/3 in 0.242s** via `python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_selected_active_sequence.py' -k accounting -v` from the isolated owner. The correction's prior **73/73** remains offline evidence, not a newly rerun suite or live result. Seven container image pins are available. The installed runtime verifier validates the retained native daprd against the pinned OCI archive; its unused Docker-cache lookup returned `No such image`, preserved in `immutable-images.json`. No tool/image version changed.

Preparatory failures remain authentic in `model-compatibility-*.{json,log}`, `isolated-model-build.*` and `isolated-server-build.*`: shared source failed **CS1501/CS1061**, resolved by the compatible private composition source; fresh restore failed **NU1506** for two duplicate declarations, resolved only in the private EventStore `Directory.Packages.props` by changing `Include` to `Update`, with both package versions unchanged at **10.0.13**. Initial wrong-context/inspection-helper failures are retained too. No repository framework, dependency-version update, commit/push or live retry was made by this preparation. `compatible-platform-source.json`, `isolated-source.json` and `isolated-eventstore-duplicate-metadata.json` record the exact private source/configuration choices.

`run-plan.json`, `launch-command.txt` and `run-input-identities.json` are the current proposal; earlier shared-checkout plans/identities are superseded preparation evidence. The unchanged capture scripts remain unstarted: after approval run each as `python3 -B "$RETENTION_PREP/capture-runtime-inputs.py" "$RETENTION_PREP"` and `python3 -B "$RETENTION_PREP/capture-container-logs.py" "$RETENTION_PREP"` before the native command. Retain their PIDs; stop both and their Docker events/log children inside the stopping allowance. Generated model/source-mapping/Dapr configuration, cohort accounting diagnostics, native TRX, passing-only `functional-observations.json`, actual terminal and disposal records stay owner-private.

Final ordinary review: no blocking issue. `isolated-final-preparation-check.json` passes every preparation check; source/binary inputs remain unchanged, evidence/state directories are empty, capture processes are unstarted and live launches are **0**. `git diff --check` passes.

**Functional retention remains unproved.** Full qualification, Production credit, A41 and overall Story 27.4 completion remain separate/open. Only this one local functional run is proposed for approval.

### Offline reliable-admission correction — 2026-10-22

The [focused correction spec](../spec-27-4-reliable-telemetry-admission.md) is implemented and its focused offline checks pass. Admission now uses a standard concurrent producer queue and an atomic reservation of both records and canonical bytes. A separate pending FIFO retains those reservations through delivery/retry; acknowledgement and expiry release them exactly once. Consumer scans and marker-key counts no longer share admission's synchronization, and logger queue metrics read counters/head snapshots without acquiring the consumer gate. Complete worker drain passes are serialized. Direct overlapping `DrainOnceAsync` calls are tested; source inspection shows that `StopAsync` reuses that method, while hosted background-loop/shutdown overlap has not been exercised. Qualification correlation is registered before publication so immediate delivery cannot lose a segment's persistence accounting. Concurrent marker ownership counters include accepted reservations before publication, release on acknowledgement/expiry/rollback, and cap heartbeat observations at the configured queue limit. The pending head is published before transfer and metric readers recheck it; ordinary logging passes no qualification callback when accounting is absent.

Barrier-controlled regressions establish admission and completion of logger metric reads before a paused consumer resumes; bounded marker counts during paused publication and throwing-callback rollback; simultaneous producers at both bounds and reuse of released capacity with different record sizes while removal remains paused; FIFO retry/partial acknowledgement; absolute expiry and maximum retry-age removal; cancellation of a semaphore waiter and active delivery before the held dependency responds, followed by a successful drain; and head/age advancement through partial and final acknowledgement. The unchanged fixed-workload runner emits **125** records through the real provider/sanitizer/queue/worker. Its first logger call is paused after publication while the worker acknowledges that record. A second barrier at the 125th publication allows the remaining records to be acknowledged before emission returns, avoiding an acknowledgement-timer race. The successful in-memory delivery substitute retains serialized canonical bytes; assertions compare all **125 expected record IDs** and verify the **1,024-byte size ceiling**, with attempted/enqueued/persisted/acknowledged **125**, rejected/dropped/conflicted **0**, matching aggregate and segment accounting, and zero remaining records/bytes. Payload bodies are not compared with independently constructed expected content. This proves process-local delivery/accounting under the controlled overlap; it is not Dapr/PostgreSQL persistence or retention evidence.

Commands from the Memories repository root:

```bash
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1
dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryDeliveryCheckpointTests -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryQualificationWorkloadTests
dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryDeliveryCheckpointTests
python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_selected_active_sequence.py' -k accounting -v
dotnet msbuild src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -nologo -p:Configuration=Debug -p:UseHexalithProjectReferences=true -getItem:ProjectReference -getItem:PackageReference
git diff --check
```

Final results: Debug build **exit 0, 0 warnings/errors, 4.38 seconds**; both affected classes together **73/73 passed** (44 checkpoint and 29 qualification cases), **0 errors/failures/skips/unrun cases, 1.701 seconds, exit 0**. Unchanged Python accounting diagnostic guards passed **3/3, 0.147 seconds, exit 0**. All review findings were resolved; none was deferred. Logs: `/tmp/story27-4-admission-review-build.log`, `/tmp/story27-4-admission-final-tests.log`, `/tmp/story27-4-admission-final-accounting.log`. The first review-patch test run had two checkpoint fixture failures (`envelope_hash_invalid` after changing a marker); the existing reidentification helper corrected them before the final passing run. Earlier failure/rerun logs remain at `/tmp/story27-4-admission-review-tests.log` and `/tmp/story27-4-admission-review-checkpoint-tests.log`. Reference inspection and whitespace checks returned **exit 0**, with the reference graph at `/tmp/story27-4-admission-reference-graph.json`. The build compiles the changed Memories code and repository project references in Debug. The reference inspection confirms existing unconditional external Hexalith package references remain despite the source-reference flag; dependencies/configuration were preserved, and a fully external-source build is not claimed.

The executed class coverage retains privacy/fail-closed negatives `Sanitizer_RejectedTenantUsesOnlyTenantSentinelAndNullsCorrelationMarkers`, `Sanitizer_BlankTenantPreservesRejectedScopeEvidenceWithoutCorrelationMarkers`, `Sanitizer_WrongTypedStateAndUnknownActionSubtype_FailClosed`, and `Provider_InvalidStateNeverEscapesIntoBusinessLogging`. No tenant routing, authorization, sanitization catalog, metric labels, retained-row contract or live acceptance assertion changed. The existing Python scenario and its bounded diagnostic capture are unchanged.

**Local retention remains unproved.** This correction addresses independently reproduced consumer-contention rejection and the demonstrated publication/accounting race; it does not establish the cause of the prior 124/125 live observation. No live launch, resource creation/disposal, dependency update, Production activation or A41 completion occurred. A newly authorized run under Spec 14 must still produce passing native TRX and `functional-observations.json`, actual expiry/purge/newer-record assertions, and verified Platform terminal/disposition evidence. Earlier launch permissions remain consumed; Story 27.4 and Epic 27 remain in progress, with full qualification and A41 open.

### Latest authorized native result — 2026-10-22

**Failed: local retention remains unproved.** The owner replied **“approve”** for one additional launch with the reviewed mapping repair and the original deadline. Run `42f30245ad0e0cbf77e3ec400ad4e74e` launched at **15:52:57.859407Z** using supported Platform **4.35.2**, source/Debug, pinned Dapr **1.18.1** and PostgreSQL **18.6**, unchanged container/security/resource bounds and one-hour retention. Admission passed at **15:52:42.733291Z**: Docker 14 CPUs / 97,021,218,816 bytes; Linux available 45,025,275,904 bytes; staging/backing free 317,724,954,624 / 1,742,043,693,056 bytes; reviewed inputs unchanged and prior exact-run labels empty. All **15 resources reached healthy**. The mapping repair passed, then the first cohort failed **`functional-accounting-loss`**: native terminal **`failed/HXT006`** at **15:54:44.142275Z**, CLI **exit 4** at **15:54:44.313444Z**. Native TRX total/executed **1**, passed **0**, failed **1**; no `functional-observations.json`, B cohort, retention/expiry or purge proof.

A read-only TLS PostgreSQL snapshot at **15:56:00.861Z** observed **124 of 125 expected A rows**, with ordinal **30** absent. This is a point-in-time observation. The exact returned accounting counters were not retained before validation failed; the saved evidence cannot distinguish rejection, dropped delivery, incomplete accounting or durable loss, and establishes no rejection reason or underlying cause. No causal delivery fix is claimed.

The demonstrated evidence gap is repaired offline: before accounting validation, the selected consumer writes an exclusive **0600** `cohort-a-accounting.json` / `cohort-b-accounting.json` under the runner's private `native-tests/`. It records only trusted request run/cohort identity, observation UTC and the eight allowlisted integer counters bounded **0–125**; unsafe/foreign response values and arbitrary fields are excluded. Original schema/identity/count assertions and success criteria remain unchanged. Existing-fixture acceptance, rejection-counter, malformed/private/foreign response and occupied-output coverage passes; the full selected suite passes **43/43 in 118.290 seconds**, **16:03:43–16:05:42Z**, exit 0. The checked direct-candidate copy passes the focused **3/3** in 0.387 seconds. This diagnostic repair has no subsequent live validation and cannot recover the earlier response. Rejection counters in its offline fixture do not establish this run's unknown counters.

Generated inputs were captured before cleanup: **57** model/source-mapping/Dapr files and **34** runtime configuration files, with retained bytes verified against capture hashes. All seven owned-container logs and inspections were retained. Supported exact-run `down` returned **exit 0 / completed** at **15:57:27.023081Z**. Fresh verification at **15:57:27.053258Z** records **`removed`**, `unverified: false`, `isEmpty: true`, empty process/container sets and zero exact-run-label containers. This run used a conservative **162.910983 seconds** of cleanup; cumulative charge is **282.870915 seconds**, leaving **617.129085 seconds** of the original 900. Prior retained resources were untouched.

Private evidence root remains `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261022-containers-mh92gyml`: `additional-launch-approval.json`, `additional-input-check.json`, `additional-prelaunch-admission.json`, `additional-invocation.json`, `additional-test.stdout`, `additional-down.{stdout,json}`, `additional-final-disposition.json`, `additional-first-cohort-postgresql.*`, `additional-cohort-accounting-diagnosis.json`, `runtime-inputs/42f30245ad0e0cbf77e3ec400ad4e74e/`, `container-logs/` and `additional-preserved-evidence-check.json`. Native report/TRX remain at owner-relative `artifacts/retention/resume-spec14-20261022-containers-mh92gyml/after-mapping-fix.{json,python3.trx}`; actual terminal/readiness/mapping/TRX remain under private `local-share/hexalith-builds/g4-runs/diagnostics/42f30245ad0e0cbf77e3ec400ad4e74e/`. Diagnostic source/copy proof and `accounting-capture-selected-suite.{log,json}` preserve offline verification separately.

**Both authorized retention launches are consumed; zero diagnostic-only startup attempts were used. No further launch or restart is authorized or prepared.** The shared execution deadline remains **2026-10-22T19:08:27.343741Z**, without reset. The additional authorization is marked consumed and its invocation guard refuses reuse. Functional completion, full qualification, Production credit and A41 remain open.

### Native result and mapping repair — 2026-10-22

**Failed: no passing live retention result.** The owner approved Platform **4.35.2** with **“I approve”**. The existing command launched at **15:08:27.343741Z**, run `8b77763653bf84d6ff2b35c02fffda74`; all **15 resources reached healthy**, including Sentry, the three Memories services and real PostgreSQL **18.6**, using Dapr **1.18.1**, source/Debug, one-hour retention and read-only observer/TLS 1.3. Admission passed at **15:07:57.284211Z**: Docker 14 CPUs / 97,021,218,816 bytes; Linux available 46,111,612,928 bytes; staging/backing free 287,047,307,264 / 1,671,353,987,072 bytes. The native test failed **`functional-selected-mapping-hash`** before cohorts: CLI **exit 4**, terminal **`failed/HXT006`** at **15:10:59.4379Z**, invocation ended 15:10:59.609558Z. TRX total/executed **1**, passed **0**, failed **1**. No `functional-observations.json`, retention or purge proof was produced. The original Sentry exit was not reproduced and remains unexplained.

The authentic mapping proves the concrete cause: .NET escaped two apostrophes in its disclosed `HXW009` catalog warning as `\u0027`; Python's reconstruction left them literal. Matching the producer encoding reproduces its exact hash. The narrow Memories repair retains standard-library JSON, normalizes producer string escapes and preserves full-payload, plan/handoff and direct-owner checks. The authentic mapping fixture is protected against line-ending conversion; the failed TRX stays private and unchanged. Focused encoding/tamper/binding regressions pass **4/4**, including a real .NET encoding reference. Existing suite `python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_selected*.py' -v` passes **40/40 in 87.888 seconds**, **15:19:41–15:21:09Z**, exit 0. The checked repair/regression copy in the direct candidate is byte-equal; its focused **4/4** pass. These were offline results at that point. The later approved native run above accepted the mapping repair, then failed first-cohort accounting.

Actual logs and inspections for **all seven** owned containers were preserved before supported exact-run `down`, which returned **exit 0 / completed** at **15:12:16.851759Z**. Fresh verification at **15:12:59.397832Z** records **`removed`**, `unverified: false`, `isEmpty: true`, empty process/container sets and zero exact-run-label containers. Conservatively **119.959932 seconds** from terminal to verification count against cumulative cleanup, leaving **780.040068 seconds**. Prior retained resources were untouched. Supported cleanup deleted this failed run's workspace: generated Dapr/model files were not separately archived. Actual plan/mapping/readiness, **16 loaded DLL hashes**, pinned image identities/arguments and resource logs remain preserved; deleted configuration is not reconstructed as authentic evidence.

Private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261022-containers-mh92gyml`. Actual approval/admission/commands/results/disposition are in `operational-approval.json`, `prelaunch-admission.json`, `native-invocation.json`, `native-test.stdout`, `native-down.{stdout,json}`, `container-logs/` and `final-disposition.json`. Native report/TRX: owner-relative `artifacts/retention/resume-spec14-20261022-containers-mh92gyml/functional.{json,python3.trx}`; report counters are `reported: false`, not a pass. Actual terminal/mapping/readiness/TRX: private `local-share/hexalith-builds/g4-runs/diagnostics/8b77763653bf84d6ff2b35c02fffda74/`. Encoding proof, test logs, input identities and copy proof remain in that private root.

At that point, **one retention launch was consumed; zero diagnostic-only attempts were used.** The subsequent retry required the separate owner approval recorded below; both launches are now consumed as recorded above. The shared budget remains anchored at **15:08:27.343741Z**, ending **19:08:27.343741Z**, without reset. Functional completion, full qualification, Production credit and A41 remain open.

### Additional launch approved — 2026-10-22

The owner replied **“approve”** for exactly one additional retention launch with the reviewed repair and the unchanged bounds below. From `/home/administrator/projects/hexalith/platform`, with the existing private `XDG_DATA_HOME` and pinned `HEXALITH_DAPR_HOME` in `continuation-plan.json`:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence artifacts/retention/resume-spec14-20261022-containers-mh92gyml/after-mapping-fix.json --output json
```

**Executed once and failed; this authorization is consumed.** At approval, `additional-launch-plan.json` retained the same tool/images/container/security/resource limits, 600-second startup, original **19:08:27.343741Z** execution deadline and **780.040068 seconds** cleanup remaining. The invocation guard recorded the exact native owner PID/start time and was configured to send that verified owner group SIGINT at the shared deadline minus one second; this run ended before cancellation was needed. It required enough time for full startup/observation, with the stated latest start **17:43:26.343741Z**. Actual generated Platform/Dapr inputs and logs were preserved before supported disposal. The plan is now marked consumed; no further launch, budget reset or automatic restart is authorized.

### Tool substitution approved — 2026-10-22

**The owner approved the narrow tool substitution with “I approve” on 2026-10-22.** The selected Platform checkout pins **4.35.2** and requires `[4.35.2, 5.0.0-0)`; active Spec 14 now selects that version for the existing container continuation. The installed command reports `4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`. Neither version pin was changed. The approved decision is limited to using that currently supported 4.35.2 payload, retaining the approved container target, source/Debug mode, immutable Dapr 1.18.1/PostgreSQL 18.6 images, security checks, 16 GiB Docker/Linux memory floors, 8 CPU and 20 GiB disk floors, two diagnostic startup attempts, one retention launch, shared 14400-second execution budget, 600-second startup bounds, cumulative 900-second cleanup allowance and existing outer window. The explicit owner reply authorizes this substitution; successful preparation itself supplies no runtime proof.

Current preparation in `/home/administrator/projects/hexalith/platform` established:

- `dotnet tool run hexalith-module --version` reports the exact version above; `test --help` exposes the existing selected-test options.
- `dotnet tool run hexalith-module -- validate --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --output json` returned exit 0, `validated`, with no diagnostics.
- The installed 4.35.2 `WorkspaceRootResolver.ResolveAsync` and `SourceMappingMaterializer.WriteAsync/Environment` passed at **11:00:28.474513–11:00:32.500616Z** in **4.027 seconds**, with no identity findings. Source mapping is `d636195d134a00ddef45236f23e3fab65c5b5cc5766a6aa15a54eab22609f8c2`; the direct source owner remains `/home/administrator/projects/hexalith/platform/references/Hexalith.Memories`. Platform/composition/checkout identity is `4ebe6798fe8bd6c22258f20c90d6861b7137c927`; catalog identity is `4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`.
- At 11:01:49Z, of the 76 previously prepared relevant inputs, 74 remain byte-equal. Only Platform's `.config/dotnet-tools.json` and `hexalith-platform.json` changed. The original installed 4.35.1 payload, selected test/imports/manifest and selected Platform composition inputs remain unchanged. This comparison does not bind newly built runtime artifacts or establish model/startup compatibility.

The 4.35.2 package SHA-256 is `637bebd547f45b5f2012c2f43af79b9b01772f1e81af02809babe62f4aa7cf1d`; its Tooling DLL SHA-256 is `a923a77ea4488d1b7162134ed89d951dfebd2398ccae92f74d0a087df336fd9a`. Private preparation root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261022-containers-mh92gyml`. `installed-tool-identities.json`, `tool-version.{stdout,json}`, `manifest-validation.{stdout,json}`, `source-resolution.{stdout,json}`, `preparation/source-mapping/` and `prepared-input-comparison.json` retain actual payload, command and supported mapping results. The resolver probe uses the installed Tooling assembly without replacing its checks or constructing a selected-test handoff. Proposed fresh owner-relative evidence is `artifacts/retention/resume-spec14-20261022-containers-mh92gyml/functional.json`; no native report existed there at preparation time. The later failed native report is recorded above.

Read-only examination of saved run `2397010f0d8da3011f1c7a1ead881586` confirms its Sentry container started and exited with code 1, then was destroyed; `saved-sentry-events.json` preserves that extraction from the authentic events. `startup-failure.json` records Sentry `Exited` and dependent `FailedToStart`; `apphost-retained.log` contains only sanitized output markers. Sentry stdout was not retained. These files do not establish the startup cause, and no speculative code fix is claimed.

The 11:00–11:01Z preparation itself performed no capacity admission, startup, retention launch or disposal. The later authorized native run and consumed launch are recorded above; prior retained resources remain untouched. Local functional completion, full qualification and A41 remain open.

### Continuation admission — 2026-10-22

**Refused before launch: no diagnostic startup and no retention run.** At **2026-10-22T07:32:14.751678Z** the continuation's once-before-launch capacity check failed the existing Linux available-memory floor. Docker had 14 CPUs and 97,021,218,816 bytes, staging had 180,398,264,320 bytes free, and `/mnt/c` had 348,689,457,152 bytes free. Linux `MemAvailable` was **34,637,570,048** bytes, below the **42,949,672,960**-byte (40 GiB) floor. Admission is `admitted: false`. No supported `test` or `down` command was started, so the shared 14400-second budget was not anchored, none of the two diagnostic attempts was used, and the one retention launch was not used. No container or run process was created. Cleanup was not required. Prior retained runs `f482f16095f3027867cec221b0a35220` and `b399a0389f7a8383af4a30339761ade2`, and failed run `2397010f0d8da3011f1c7a1ead881586`, had zero containers under their exact `hexalith.g4.run` labels and were not disposed.

The same observation counted **1,048,539** inotify watches against `fs.inotify.max_user_watches` **1,048,576**. `fluentui-mcp` held 1,045,105 of them. Those processes were left running. The watch count is an observation only. It does not establish why run `2397010f0d8da3011f1c7a1ead881586` recorded Dapr Sentry exiting with code 1. That run destroyed its Sentry container before stdout was retained, and its sanitized diagnostics still do not establish the process-start cause. No Platform or Memories code change was made. The directory `/mnt/wsl/story49-qualification-20261021T072935Z` occupies 39G on the `/mnt/wsl` tmpfs and was left untouched.

Private snapshot: `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261022-admission/prelaunch-admission.json`. Local functional completion, full qualification, and A41 remain open. The earlier startup failure recorded below is unchanged.

### Container memory floor — owner decision 2026-10-22

The functional run stays on the existing container graph. It does not create a KVM or QEMU guest. The qualification guest profile remains a separate full-qualification record and is not started here.

For the next container admission, Docker memory and Linux `MemAvailable` must each be at least **17,179,869,184** bytes (16 GiB). That is the PostgreSQL container `--memory` and `--memory-swap` cap in `PlatformRetentionProfile`. The 8 CPU floor and the 20 GiB staging and backing-disk floors stay. The 40 GiB and 56 GiB figures belong to the qualification guest and the retired host-before-guest check. This decision changes the next admission floor. It does not start a run, and it leaves the 07:32:14Z refusal above as the check that actually ran.


**Failed during startup: no passing live retention result.** The owner approved the prepared one-launch plan with **“.approve”** on 2026-10-22. The supported command launched at **06:20:19.081832Z**, run `2397010f0d8da3011f1c7a1ead881586`, and returned **CLI exit 3 / failed / HXR025** at 06:20:45.441233Z (native terminal 06:20:45.2583461Z). Startup failed before the selected retention test: **no native TRX, no `functional-observations.json`, no cohorts and no retention/expiry proof**. No startup retry or second launch was attempted.

Capacity/isolation admission passed: Docker 14 CPUs / 97,021,218,816 bytes memory, Linux available 43,445,129,216 bytes, staging 181,214,040,064 bytes free and the existing `/mnt/c` Docker Desktop backing volume 347,847,163,904 bytes free. All 76 prepared relevant inputs were unchanged. The supported runner generated its actual source mapping and isolated workspace; model/startup compatibility did not pass. Real exact-run Docker events show **Dapr Sentry exited with code 1** before the startup refusal. EventStore, Placement, Scheduler and its sidecar were `FailedToStart`, dependent Memories services waited, and PostgreSQL was running/unhealthy. The preserved sanitized diagnostics do not establish the underlying Sentry/process-start cause; no speculative fix or dependency change is claimed.

The approved supported exact-run `down` returned **exit 0 / completed**. Actual terminal disposition is **`removed`**, with `unverified: false`, `isEmpty: true` and empty process/container sets; a fresh exact `hexalith.g4.run` label scan found zero containers. Verified at **06:23:26.816465Z**, conservatively 161.375 seconds after native completion, within the single shared 900-second stopping allowance. Earlier retained resources were excluded. All **ten** environments from this and the preceding exercise are recorded as removed/verified empty; historical unrelated retained resources below remain excluded.

Current private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261021-transport-7nkxc6pt`. Exact command/start/deadlines/exit are in `native-invocation.json`; owner decision in `operational-approval.json`; actual admission in `prelaunch-admission.json`. Authentic command/cleanup results are `native-test.stdout` and `native-down.stdout`. The supported tool wrote its separate native evidence report to the fresh direct owner path `artifacts/retention/resume-spec14-20261021-transport-7nkxc6pt/functional.json`; its zero counters have `reported: false` and are not a passing test. That original invocation report remains unchanged after cleanup; the actual terminal record and final disposition provide the later verified removal. Native startup summary, source mapping/plan and mutable terminal record remain under `local-share/hexalith-builds/g4-runs/diagnostics/2397010f0d8da3011f1c7a1ead881586/`. `owned-docker-events.jsonl` and `final-disposition.json` preserve the actual exit/disposition evidence. The one launch has been consumed; no automatic retry or new operational authorization is inferred.

Final verification: `git diff --check` passed, and the ordinary diff review covers only the four approved spec/report/consumer/regression paths. The legacy scope command `python3 tools/check-story-file-scope.py --story-key 27-4-retention-verification-operations-runbook-and-a41-close-out` (with those four repeated `--changed-file` arguments) returned exit 1: `Story artifact not found: _bmad-output/implementation-artifacts/27-4-retention-verification-operations-runbook-and-a41-close-out.md`. It expects the removed historical story artifact rather than active Spec 14; no replacement artifact or gate bypass was introduced. Exact command/result: private `current-verification.json`. This tooling blocker is separate from the actual startup failure.

### Previous 2026-10-21 native results

**Failed: no passing live retention result.** The supported native test in run `9832a889c6b2cdf441cf52d66bd3cea0` executed once and failed with `functional-selected-transport-uncertain` at the first pre-expiry strong-read checkpoint. Platform recorded `failed/HXT006`; CLI exit 4. Both real cohorts were observed in PostgreSQL, but A expiry/index cleanup and final canonical B preservation were not proved. No passing `functional-observations.json` was produced.

The final input readback also found all 16 readiness-bound DLLs replaced during the run (filesystem times 18:40–18:42Z). The selected entry and manifest remained unchanged. This independently invalidates the executable freeze; the advisory reservation did not prevent another build from writing those outputs. The transport root cause and the actor responsible for those writes are not established. No further live attempt fits the complete observation window inside the original four-hour ceiling.

All **nine** environments created by this exercise are actually `removed`, with `unverified: false`, empty process/container sets and zero containers under their exact `hexalith.g4.run` labels. Previously retained resources were excluded from disposal. Three startup failures, five native failures and one deliberately withdrawn startup are preserved.

Private evidence root: `/home/administrator/.local/state/hexalith/memories/access-telemetry/functional-spec14-20261021-nymfther` (owner-private). Latest authentic native artifacts are `serialization.python3.trx` and `serialization.json` under the direct candidate's `artifacts/retention/functional-spec14-20261021-nymfther/`. TRX counters are total/executed 1, passed 0, failed 1; the failed command summary separately has `testCounts.reported: false` and zero counts. Neither is a passing result. Byte-equal copies of the actual latest native report/TRX and final terminal record are also retained in private `native-results/9832a889c6b2cdf441cf52d66bd3cea0/`. Captured real configuration/readiness is in `serialization-serialization-inputs/`; actual terminal disposition is in `local-share/hexalith-builds/g4-runs/diagnostics/<runId>/selected-test.json`. Later supported `down` updates that terminal record; original failed invocation reports remain unchanged. `owned-disposition-summary.json` now records all nine native outcomes and fresh exact-label scans.

| Actual invocation on 2026-10-21 (UTC) | Native outcome | Actual disposition |
| :--- | :--- | :--- |
| `b0cd3d675e968ea7015a8254e967b721`, 16:13:22.9004007; terminal 16:15:13.9102673 | Startup `HXR025`; EventStore exited. Absolute evidence path also rejected (`HXE160`); CLI exit 3. Native test not run. | Supported exact-run `down` 16:16:54.910412–16:16:58.608623, exit 0, 3.699s; `removed`, verified empty; zero run-label containers. |
| `b99690afce897c015c9a6a5f54617f9b`, 16:37:13.449066; terminal 16:38:49.8430193 | Startup `HXR025`, CLI exit 3. Actual Dapr CLI used Sentry port 50001 instead of its allocated port. Native test not run. | `down` 16:40:08.017095–16:40:12.052106, exit 0, 4.035s; `removed`, verified empty; zero run-label containers. |
| `fbbb15cc25a730c4baaa29413e5090cd`, 16:50:05.3499227; terminal 16:51:59.5338565 | All 15 resources healthy, complete real runtime/backend observations. Native TRX: executed 1, passed 0, failed 1; `HXT006`, CLI exit 4. No cohorts. | `down` 16:56:08.819356–16:56:12.696316, exit 0, 3.877s; `removed`, verified empty; zero run-label containers. |
| `f446d364e0ff2d931ea567778bf90f78`, 17:04:01.7739468; terminal 17:05:37.3367154 | All 15 resources healthy. Native TRX: executed 1, passed 0, failed 1; `HXT006`, CLI exit 4. No cohorts. | `down` 17:08:33.376778–17:08:33.793285, exit 0, 0.417s; `removed`, verified empty; zero run-label containers. |
| Diagnostic command 17:12:13.757357–17:12:15.973284 | Prerequisite refusal `HXW004`, CLI exit 2: concurrent shared-checkout gitlink mismatch. No run ID/resources/native test. | Nothing created. Shared Git state preserved; subsequent supported resolver passed after external reconciliation. |
| `08a17382ce16a148cd7a41ff61fb3ab9`, 17:19:19.4776214; terminal 17:19:50.8488173 | Startup `HXR025`, CLI exit 3; several processes `FailedToStart`, Placement/Scheduler exit 1. Native test not run; no OOM event. Root cause not established from that attempt. | Immediate exact-run `down` 17:19:51.041871–17:19:54.383585, exit 0, 3.342s; `removed`, verified empty; zero owned containers. |
| `130ad05dee681ff2134a616b135ebfbc`, 17:22:07.319772; withdrawn 17:27:17.5302959 | `withdrawn/HXC130`, CLI exit 130. Actual actor metadata probe failed 17:23:42.124, then Dapr registered actor 17:23:44.739; the cached one-attempt failure correctly kept readiness closed. Native test not run. | Verified native owner SIGINT; supported exact-run `down` receipts 17:27:21.800161–17:27:22.165183 and final idempotent check 17:27:34.092830–17:27:34.463678, exit 0; actual `removed`, verified empty; zero owned containers. |
| `bfc889516e88426e8612e9c767030721`, 17:30:48.333945; terminal 17:35:01.1707578 | All 15 resources healthy with complete observations. Native TRX: executed 1, passed 0, failed 1; `HXT006`, CLI exit 4; retained assertion `functional-selected-input-refused` after the fixed-workload request timed out at 120s. Server logged 125 A emissions at 17:33:01; read-only PG projection at 17:34:02.467 contained zero A rows. No B/expiry proof. | Immediate exact-run `down` 17:35:01.516897–17:35:06.624471, exit 0, 5.108s; `removed`, verified empty; zero containers under actual ownership label. |
| `70451dc1b8c28b791dfea29954969e56`, 17:46:11.894307; terminal 17:49:47.6267495 | All 15 resources healthy; native TRX executed 1, passed 0, failed 1, `HXT006`, CLI exit 4. Clock attestation HTTP200 proves repaired token delivery. Actual actor remoting raised `SerializationException` for `List<AccessTelemetryRecord>`; fixed-workload acknowledgement timed out. PG observer at 17:48:17.820 showed zero A rows. No B/expiry proof. | Immediate exact-run `down` 17:49:47.940293–17:49:51.615250, exit 0, 3.675s; actual `removed`, verified empty; zero owned-label containers. |
| `9832a889c6b2cdf441cf52d66bd3cea0`, launched 18:04:07.450451Z; terminal 19:04:31.2215417 | All 15 resources initially healthy. Native TRX: executed 1, passed 0, failed 1; `functional-selected-transport-uncertain`, `HXT006`, CLI exit 4 at the first pre-expiry read. A/B each observed at 125 rows; no expiry/purge/final B proof. All 16 bound DLLs changed during the run. | Immediate supported exact-run `down` 19:04:31.610887–19:04:37.881420, exit 0, 6.271s; actual `removed`, verified empty; zero owned-label containers. |

The pre-cohort blocker was found deterministically in the saved real inputs: loaded DLLs were hashed through the strict JSON reader's 1,048,577-byte capped read. Contracts DLL (1,316,864 bytes) and Server DLL (2,597,888 bytes) had correct full hashes, so the capped hash falsely rejected unchanged artifacts before `consumer.execute()`. This explains the two native failures; the sanitized TRX itself contains no detailed assertion message. Separately, a read-only query observed empty PostgreSQL preflight rows and HTTP 204 for fresh actor state; these were diagnostic observations, not proof the failed test reached actor access.


## Observed two-cohort progress and limits

All times below are actual UTC observations on 2026-10-21; these partial observations do not replace the failed native result.

| Observation | Actual result |
| :--- | :--- |
| A emission / first SQL read | 125 records, emitted 18:05:29.754–18:05:29.878Z; SQL 18:06:46.798Z, `telemetry_observer`, read-only, TLS 1.3. |
| A intermediate read | At 18:21:19.595Z, all 125 stored records and TTLs were unchanged. |
| B emission / SQL read | 125 records, emitted 18:35:29.755–18:35:29.879Z; SQL 18:36:19.160Z. First-emission UTC separation 1800.001s. Native monotonic measurements were not retained because execution failed. |
| Logical retention / stored TTL | Each logical expiry was emission +3600s: A 19:05:29.754–19:05:29.878Z, B 19:35:29.755–19:35:29.879Z. A's initial actual PostgreSQL TTLs were 339–1975ms after logical expiry. |
| Intermediate reminder progress | Read-only actual actor state: sequence 4 at about 18:25:30Z, sequence 8 at 18:49:12.249Z. This is pre-expiry activity, not post-expiry purge proof. |
| Failed final observation | Native TRX written 19:04:31.014353Z, before A expiry; assertion `functional-selected-transport-uncertain`. No qualifying strong absence, removed-index, post-expiry progress or final B preservation observation. |
| Actual disposition | Native command finished 19:04:31.608133Z; supported cleanup finished 19:04:37.881420Z. Time from native report completion to cleanup completion was about 6.867s; at least 192s of the separate stopping allowance remain conservatively. |

Raw observations: `serialization-cohort-{a-initial,a-middle,b-initial}-observer.stdout.json`, `serialization-actor-{progress,later-progress}.json`, `serialization-stopping-witness.json` and `serialization-down-result.json`. Input drift is recorded in `serialization-postrun-input-verification.json` and `serialization-postrun-input-times.json`; readiness preserves the original loaded hashes. Raw owned logs are in `serialization-resource-logs/`. They show no service exception before the final captured snapshot; they do not establish the failed request's underlying transport cause.

Functional configuration identities: profile `f005b8a08f0650a86415b65db2452cb800a8b33e1441b6073896334e89e7cade`; source mapping `4adefb97991e02c4601d63a5105842d95793d752d9657162ab39be01d08d76b5`; initial observed candidate `870c44e6b5b1d4d4ccfdad5844ee3ed83f421bb21067f09e8cae32a2eb906d23`; runtime `be0d0ee7362c6b8626538876fa2342d1c0789d126a60129a7bf27009a072769c`. These identify the initial runtime; later output replacement prevents an unchanged-input claim for the full interval.

## Previous executed plan — budget expired

The earlier user authorization covered local preparation, Debug builds, isolated Platform/Builds startup, the existing real test and bounded disposal without further confirmation. Its operational budget expired at **2026-10-21T20:13:22.9004007Z**; it supplies no authorization for a further launch. Owner `github:user:6775094`; assurance `sole-owner/self-reviewed`. `run-plan.json` preserves the initial plan; corrected plans bind each concrete failing input. Current executed plan: private `serialization-run-plan.json`; no automatic restart.

- Cwd `/home/administrator/projects/hexalith/platform`; direct candidate `references/Hexalith.Memories`; source mode, Debug project references. Supported local tool `4.35.1+f8599224457dda99e7c0ae4870147dd51b464feb`, accepted range `[4.35.1,5.0.0-0)`. The native report observed .NET SDK `10.0.402` on Ubuntu 26.04 LTS.
- Original absolute execution ceiling **2026-10-21T20:13:22.9004007Z**, anchored to the first command; startup 600s inside execution. Corrective commands do not receive a fresh operator budget. Separate stopping allowance started at 900s; 199s remained at the ninth launch, and conservatively **192s remain after its disposal**, counting the time from native report completion through cleanup. The executed plan's latest launch was 18:45Z, with latest possible stopping end 20:16:41.9004007Z; actual disposal finished 19:04:37.881420Z. The original execution deadline remains unchanged, within Spec 14's window ending 2026-10-25T14:42:22Z.
- Existing seven-container graph. PostgreSQL ceilings: 8 CPUs, 16 GiB memory including swap, 100 connections, Dapr pool 40, 400 GiB ephemeral tmpfs. Admission floors: Docker 8 CPUs/40 GiB, Linux available 40 GiB, staging/backing free 20 GiB. No new VM/disks. Capacity refusals create no resources; unrelated processes remain untouched.
- The plan reserved selected executable outputs using `selected-platform-memories-debug.lock`; final readback proves this advisory reservation was insufficient against concurrent writes. `executable-inputs.json` records 491 relevant runner/application/dependency/test/manifest/Platform configuration files; later fix manifests refresh only affected inputs. Actual generated model/mapping/Dapr configuration identities and readiness are retained per run. No whole-workspace source freeze or HEAD-based invalidation.
- Existing test: one writer, 125 A and 125 B records separated by at least 1800 real UTC/monotonic seconds; retention 3600s. Strong A reads before expiry; A absent with due-index/reminder progress within 900s afterward; canonical B unchanged. Keep real signed-clock and stored-row/TTL checks. No fake time/direct backend writes.
- Platform/Builds owns lifecycle. Success requires native passing TRX and observations plus actual removed/verified-empty disposition. Failed new runs are disposed only by their exact run ID. Prior retained resources below remain excluded.

```bash
RETENTION_EVIDENCE=artifacts/retention/functional-spec14-20261021-nymfther/serialization.json
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence "$RETENTION_EVIDENCE" --output json
```

Evidence is a canonical repository-relative JSON path resolved under the manifest owner; the supported writer refuses absolute paths. Immutable pins are unchanged: Dapr `ghcr.io/dapr/daprd:1.18.1@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8`; PostgreSQL `docker.io/library/postgres:18.6-trixie@sha256:5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722`. Full eight-artifact identity list: `immutable-images.json`. Real ready runs observed PostgreSQL `180006|t|TLSv1.3|telemetry_observer|on`, verified native Dapr 1.18.1, and all three service retention settings 3600s.

## Approved plan — executed once; startup failed

Prepared 2026-10-21 after the narrow transport repair below. Preparation started no resources; the approved invocation executed once on 2026-10-22 and failed before the selected test. Its actual disposition is verified removed above. The owner approved this complete plan with **“.approve”** on 2026-10-22. Its fresh one-launch/four-hour/15-minute limits apply; the previous budget remains expired. Owner `github:user:6775094`; assurance `sole-owner/self-reviewed`.

| Plan item | Concrete scope and limit |
| :--- | :--- |
| Target and command | Cwd `/home/administrator/projects/hexalith/platform`; direct Memories owner `/home/administrator/projects/hexalith/platform/references/Hexalith.Memories`; supported local tool `4.35.1+f8599224457dda99e7c0ae4870147dd51b464feb`; source mode, Debug project references. Use the existing selected-test command below. |
| Fresh evidence | Owner-relative `artifacts/retention/resume-spec14-20261021-transport-7nkxc6pt/functional.json`. Its canonical owner directory was created empty; the JSON does not exist. Private preparation/log root `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261021-transport-7nkxc6pt`; run state uses its fresh `local-share/`. |
| Absolute window and fresh budgets | One launch after the owner's decision, no earlier than **2026-10-21T21:15:00Z** and no later than **2026-10-25T10:27:22Z**. Its execution deadline is actual launch +14400s, including startup at most 600s; stopping has a separate fresh 900s. Absolute execution/stopping ceilings are **2026-10-25T14:27:22Z / 14:42:22Z**. Earlier launches receive the same four-hour/15-minute bounds, never the whole outer window. |
| Resource ownership and admission | Existing seven-container graph; no VM or new disks. PostgreSQL at most 8 CPUs, 16 GiB memory including swap, 100 connections, Dapr pool 40 and 400 GiB ephemeral tmpfs. Immediately before the one launch, check Docker at least 8 CPUs/40 GiB, Linux available memory at least 40 GiB, staging/backing free at least 20 GiB, fresh ownership and allocated-port isolation from prior retained resources. The once-before-launch capacity/isolation check passed on 2026-10-22; private `prelaunch-admission.json` and `prelaunch-listening-ports.txt` retain actual capacity and ownership/listeners. The supported runner checks its newly allocated ports. Refusal admits no launch or retry. |
| Build and actual input binding | Reuse Platform's existing per-run/per-resource SDK `--artifacts-path <mapped-owner>/artifacts/retention/<actual-runId>/build/<resourceId>` for EventStore and the three Memories resources. The supported launch builds those Debug inputs once and captures actual loaded application/dependency hashes, generated Platform model/Dapr configuration, images and readiness. Current shared Debug outputs are not frozen or offered as the next run's binaries. Preserve supported direct mapping and held-file checks; rebuild/recheck only an affected actual input. No Markdown/HEAD/unused-module freeze. |
| Retries and behavior | Exactly one launch; **zero startup retries and no automatic restart**. A write is submitted once; uncertain submission may reconcile the same cohort identity once. Existing strong reads retain three attempts for a failed key, 2s/5s backoff, sixteen workers and one shared 120s observation budget. Keep the actual 125/125 cohorts, 1800s separation, 3600s retention, 900s purge grace, signed clocks, strong state reads and stored-row/TTL checks unchanged. |
| Terminal result and disposition | Platform/Builds owns setup, readiness and disposal. Record native TRX, `functional-observations.json`, actual terminal outcome and disposition. On failure/withdrawal keep authentic evidence and use supported `down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --run-id <this actual run ID> --output json` within the shared 900s stopping allowance; no budget reset between native stopping and explicit down. Verify the actual terminal `removed`, empty process/container sets and no containers under that exact `hexalith.g4.run` label. Unknown/failed disposal remains explicit. Prior retained runs/disks below are excluded. |

From the Platform cwd, with `XDG_DATA_HOME` set to the fresh private `local-share/` and `HEXALITH_DAPR_HOME` set to the existing pinned `/home/administrator/.local/state/hexalith/memories/access-telemetry/spec14-prerequisites-20261017T063912Z-n7caakwr/artifact-preparation/dapr-home`:

```bash
RETENTION_EVIDENCE=artifacts/retention/resume-spec14-20261021-transport-7nkxc6pt/functional.json
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence "$RETENTION_EVIDENCE" --output json
```

Supported preparation passed without starting resources: native `validate --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json --output json` returned `validated`, exit 0, no diagnostics (0.509s). The installed tool's existing `WorkspaceRootResolver.ResolveAsync` and `SourceMappingMaterializer` returned source mapping `4c2b2933ca74c8b642b8a8f3990fd69395abc767a8f981b0101facfc484ecc24`, the direct Memories owner above and no identity findings (6.695s). Platform/composition/checkout identity was `d7651fcd29e67df9dfc140217c201d64ebcbe72e`; catalog identity `359fee24aeca4d9eabed4b425d4cfa2520b2adda`. These are observations, not a requirement that unrelated HEAD changes invalidate runtime inputs.

`prepared-input-identities.json` records 76 relevant runner, selected Python/import/test/manifest, Platform composition/build configuration, generated source-mapping and pinned Dapr artifact identities. Installed tool archive SHA-256: `330c33fc006dbed31c5c685a8dbbcc08a11ae7d1e3c3d0d08715473d5e2b263e`; manifest `1265d887617a15b904d88f0ff42af3e118d418b449b3e3f73796ee509d1a361f`; selected entry `8447131da01635cbc87645c33a1b56fff4ceeb03f7561fab5516c031bd12f0e1`; repaired consumer `73d1b4ac3d758bfdddaf33d2320e38b486a2fa9f8379f1ca22343c2c576adb45`. All 14 selected Python/test/manifest comparisons between primary and direct candidate are byte-equal. Eight actual installed image inspections passed and are recorded in `immutable-images.json`; Dapr/PostgreSQL immutable pins above remain unchanged. Tool/help, source resolution, manifest validation and image inspection establish preparation only. The authorized launch failed during startup; no passing runtime compatibility, loaded-observation or retention result was established.

## Concrete fixes and focused verification

The current resumption received one ordinary review of the complete source, regression, spec and report diff; no issue was found and `git diff --check` passed. The owner approved the prepared launch on 2026-10-22; it executed once, failed during startup and was removed/verified empty. Live retention remains pending and no retry is authorized.

| Owning repository / change | Observed check |
| :--- | :--- |
| Memories: selected request-boundary response-body transport repair (2026-10-21 resumption). | The new request-boundary regressions reproduced escaping raw timeout/stream failures before the repair. Raw `TimeoutError`, `ConnectionError` and `http.client.HTTPException` now become static sanitized `TransportUncertain`; HTTP status, malformed JSON and output-limit refusals remain immediate. Focused retry suite: **13/13 passed**, primary 0.200s, direct candidate 0.104s. Existing full selected-consumer suite: **36/36 passed in 137.021s**, 20:58:43–21:01:00Z, exit 0. Command: `python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_selected*.py' -v`. Log/command/timestamps: new private preparation root `selected-consumer-tests.{log,json}`. Only the exact two-file repair was copied to the already-dirty direct candidate after confirming all other bytes matched; before/after proof is `candidate-patch.json`. These are offline request-boundary checks, not proof of the authentic failed run's cause or live retention success. |
| EventStore: bounded initial index-write retry only for wrapped gRPC `Unavailable`; 30 attempts, cancellable one-second delays; persistent/permanent failure propagates. | Debug source build: zero warnings/errors. Native focused class: 22 passed, zero failures/errors/skips/not-run. `eventstore-startup-tests.xml`, `eventstore-startup-fix.diff`. |
| Platform: selected retention sidecars use the verified native daprd with mTLS and the allocated Sentry endpoint; existing resource ownership/ports/tokens/configuration retained. | Debug build: zero warnings/errors. Two native callback/listener/token tests passed. `platform-sentry-tests.xml`, `platform-sentry-fix.diff`; real subsequent all-resource readiness confirmed. |
| Memories primary/direct candidate: complete bounded held-file binary hashing; explicit fresh HTTP 204 actor baseline; later stored reminder/progress still required; bounded static failed assertion identity in secret-inspected native test metadata. Captured output remains redacted. | Initial selected suite: 14 passed in 147.014s. Final metadata refusal checks: 5 passed in 0.040s. Binary >1-MiB acceptance and tail-drift rejection: 1 focused test, two variants, passed in 13.032s. Complete held hashes match all 16 actual loaded artifacts; `binary-hash-focused-proof.json`. Candidate copies byte-equal. Actor metadata now polls at most 30 actual responses with 500ms cancellable waits, inside the unchanged 30s probe budget; exact actor proof and cached decision remain required. Mapped Debug build passed with zero warnings/errors; 57 native capability tests passed (14.929s), including absence, success and cancellation. `memories-actor-probe-tests-20261021.xml`; affected outputs: `actor-fix-inputs.json`. These focused checks are not live retention success. |
| Memories: scoped Dapr HTTP token supplied explicitly to the pinned SDK; retain actual invocation handler and token authentication. | Baseline actual-handler tests reproduced 2 missing-token failures; after fix, 67 focused native checks passed in 1.900s. Mapped Debug build: zero warnings/errors, 10.28s; selected Server rebuilt. `memories-token-baseline-tests-20261021.xml`, `memories-token-factory-tests-20261021.xml`, `delivery-fix-inputs.json`. Real subsequent clock HTTP200 observed. DI/logger path also checked; a missing sanitizer alone would produce counted rejection, so the startup-bootstrap hypothesis was not implemented. |
| Memories Contracts: known concrete HTTP-decoded list/dictionary types and strict query scalar converter reusing canonicalizer rules; same typed/DataContract actor protocol and hash/clock checks. | Baseline actual SDK serializer reproduced 3 graph failures. After repair, AccessTelemetry 96/96 and Server 67/67 passed; full graph preserves canonical bytes/hash and rejects tampered hashes and invalid scalars. All 3 selected Debug outputs rebuilt with zero warnings/errors. `memories-actor-serialization-final-{access,server}-tests-20261021.xml`; inputs `serialization-fix-inputs.json`. |
| Existing Memories Debug document guards and scope checker | Final Debug document guards: 5/5 passed in 0.142s; scope checker passed; relevant whitespace checks passed in Memories, the direct candidate, Platform and EventStore. Native retention remains failed. Exact final commands/results: private `final-verification.json`; native document XML: `final-document-guards.xml`. |


Post-run Memories repairs are separate from the authentic failed result:

- State reads now reuse the existing read-only limit: three attempts for a failed key, 2s/5s backoff, sixteen workers and one shared 120s observation budget. Every attempt and backoff rechecks authority; per-request timeout is capped by remaining time. HTTP authorization/server failures and malformed data fail immediately. Write submission behavior is unchanged.
- The sixteen initially hash-verified loaded files now retain non-following descriptors and bound file identities for the whole consumer execution. Checks reject replacement and in-place writes, including restored mtime; ancestor checks bind only directory identity/type. Unrelated sibling files, directory contents and permissions remain outside the freeze. A final check runs after observations return and before recording success. Descriptors close on all outcomes. This detects shared-output changes; it does not prevent another process from writing shared outputs.
- Focused selected-consumer regressions: **31 passed in 170.014s**, including 9 read-retry and 7 actual-file guard tests. A separate final-return drift check passed **1/1 in 24.202s**, proving no success observation file is written after final input drift. Four source/test files are byte-equal in primary and direct candidate. Actual patches, logs and copy proof are retained under private `postrun-fixes/`.


Platform now also isolates selected retention build outputs, using the supported SDK `--artifacts-path <mapped-owner>/artifacts/retention/<actual-runId>/build/<resourceId>` argument for EventStore and all three Memories source resources. Separate roots prevent ordinary shared Debug builds and the four selected builds from writing one another's outputs. The existing source mapping, real process observations, Debug configuration, tokens, mTLS and lifecycle ownership remain in use. Run/resource segments are validated; ordinary composition arguments remain unchanged. No workspace source freeze was added.

The exact owning check was `dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1 --nologo`, cwd `/home/administrator/projects/hexalith/platform`. The first invocation inherited the Memories-specific build environment and refused with exit 1, `HXW006` (Platform's own project was outside that selected map). The same command in the owning Platform context passed in **32.36s**, zero errors, **9 `MSB3277` assembly-conflict warnings** involving IdentityModel 8.19.2/8.23.0 and Redis 2/3 references. Dependencies were not changed. Native focused graph/path/Sentry/control-port checks passed **13/13 in 2.844s**, followed by the existing scoped listener/token check **1/1 in 0.651s**. Tests cover two runs, all four resources, owner containment, unchanged ordinary arguments and eight invalid path cases. Patch, exact commands, warning log, source hashes and native XML are retained under private `postrun-fixes/platform-retention-output-isolation-*`.

These post-run repairs have no subsequent live retention validation. The observed transport cause remains unproved, and no passing evidence has been substituted for the failed native run.

One ordinary review of changed behavior is complete. This execution did not stage or commit files, update dependencies, modify the installed tool, bypass security checks or request approval-by-digest. Concurrent shared-checkout changes were preserved. Full qualification, Production activation and A41 closure remain separate/open.

## Retained resources

Latest `WINDOW` run `0673d50339f4e1058c26e3a9b2d680a2` completed successfully with native **removed / empty / unverified:false** and verified exact private cleanup at **2026-10-25T07:26:23.784748Z**; no resources owned by that run remain. Its actual result and disposition are recorded in [the functional result](#corrected-window-execution--2026-10-25-local-functional-retention-passed-cleanup-verified). Earlier resources below were not queried or disposed by this run.

Historical `CAPABILITY` run `55eb672cd9f30d006caa815eae60a867` completed supported down and verified exact private cleanup at 2026-10-24T23:19:57.016910Z. Its native report retains the authentic pre-cleanup `retained`/`unverified` marker alongside separate actual disposition evidence.

Documentation removal does not dispose of resources. Prior failed runs `f482f16095f3027867cec221b0a35220` and `b399a0389f7a8383af4a30339761ade2` remain recorded as `retained`, `unverified: true`, `isEmpty: false`, with cleanup unauthorized. These are last-recorded dispositions, not a fresh census. Source: private `current-held-functional-retention-invocation.json` under `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261021-current-5clz3zwi` (`retainedFailedRuns`).

The [bootstrap recovery result](../27-4-bootstrap-recovery-result-2026-10-16.md) records retained 40/600-GiB disks and a stopped QMP socket. Private raw receipts and these resources remain untouched. Before any new run, Platform must establish isolation from retained resources; old attempts/permissions cannot be replayed.

## Evidence posture

Latest `WINDOW` run `0673d50339f4e1058c26e3a9b2d680a2` supplies passing local functional evidence: native completed/Success, TRX 1/1, the real retention assertions and verified disposition recorded above. Its local profile is `f17bd8da9c6104df75c192d100bb9c42645307d5ce2f4f89dafc5135dadcd382`, assurance is `sole-owner/self-reviewed`, and `qualificationCredit:false` remains unchanged. Strong TTL absence and due-index cleanup do not establish physical tuple deletion or reclamation. Earlier failed runs, partial observations and changed-input limitations retain their historical outcomes; preparation and offline fixtures earn no live credit. No target was queried. Production lifecycle writes remain disabled; `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward/open. Overall Story 27.4 stays in progress pending its separately scheduled full campaign.

The identity and C0-C6 matrix below preserve the former PG-ONPREM-2 record. Repository-validation labels are historical offline results, not current-profile revalidation. The [current full campaign checklist](../../../docs/operations/access-telemetry-full-qualification.md) is the requirements/state register; its exact full Platform target/profile still needs adoption before scheduling. Local Debug, development UTC and file-backed secrets earn only the disclosed functional evidence.

## Immutable decision identity

| Field | Value |
| :---- | :---- |
| Profile ID | `postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2` |
| Profile SHA-256 | `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` |
| Workload SHA-256 | `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` |
| Production lifecycle writes | `disabled` |
| A41 status | `carried-forward/open` |
| Repository mode | offline; no target query and no A41 mutation |


## Canonical C0-C6 matrix

| Checkpoint | State | Repository validation | Required external evidence | Owner | Blocker / operator action |
| :--------- | :---- | :-------------------- | :------------------------- | :---- | :------------------------ |
| C0 exact adapter profile | `repository-validated` | Exact `PG-ONPREM-2` profile, hash, workload, parser, and pre-query identity contracts have deterministic guards. This is not Production proof. | Immutable exact-profile qualification packet with executed commands and recomputed source/artifact identity. | Platform Operations | `operator-pending`: execute the authorized C0 producer on the declared target. |
| C1 canonical predecessor | `operator-pending` | The legacy verifier structurally checks each canonical `C1.1` through `C1.25` input and rejects a synthetic aggregate; it does not authenticate reviewer identity or establish accepted C1 evidence. | Twenty-five individually attributable passing gates with named owners and eligible session, plus actual Operations and Security decisions bound to the exact manifest and profile hash. | Gate owners, Platform Operations, Security | Complete all 25 gates and the approved authenticated interchange. The required authenticated interchange must refuse missing owners, skips, zero-result commands and bare reviewer labels. The approved sole-owner policy applies; distinct records remain required without claiming independent reviewers. |
| C2 production replacement | `operator-pending` | Producer schema, immutable packet writer, same-profile validator, concurrent fixed two-writer accounting, zero-default gate/Lease transition, and exact per-instance replacement selectors are repository-validated. | Controlled two-writer execution; replacement of both Servers and their sidecars, lifecycle/clock services and their sidecars, actor activation, all three Placement and Scheduler members; and adapter-fault execution with exact acknowledgements, recovery, and audit continuity. | Platform Operations | Run the reviewed C2 producer only after C1 passes and the separately scoped P7 runtime migration is approved and verified, with a named shared-system approval; its target-identity observation must prove an initially disabled exact-profile qualification namespace, empty Lease, and zero lifecycle/clock replicas. Zero acknowledged loss and a final disabled/empty/zero state are required. |
| C3 retention and reclamation | `operator-pending` | Cohort, 1/24/168-hour bounds, attestation negatives, interrupted-purge, newer-record, tuple-attribution, and logical/physical separation guards are repository-validated. | Executed expiry/purge and adapter reclamation commands bound to each of the three independent cohorts and its database/schema/table; newer records preserved and reusable allocator free-space increase observed within 86,400 seconds. | Lifecycle owner and adapter owner | Run the reviewed C3 producer after C1 and approved, verified separately scoped P7 runtime migration; an OS disk-shrink claim is prohibited. |
| C4 failure, privacy, and observability | `operator-pending` | Complete failure inventory, health precedence, NoData/last-evidence timestamp, bounded labels, and Story 20.2/24.3 denial guards are repository-validated. | Every declared dependency/fault lane, nonzero business samples with zero business failure, console/configured-OTLP continuity, alerts, and tenant denial before dependency access. | Platform Operations and Security | Run the reviewed C4 producer after C1 and approved, verified separately scoped P7 runtime migration; missing scenarios, raw/secret aliases, or dependency calls after denial reject. |
| C5 operations acceptance | `operator-pending` | Neutral and PostgreSQL-specific runbook structure, ownership, monitoring, RPO/RTO, rollback, rotation, and decommission contracts are repository-validated. | Named operations acceptance of the exact immutable profile, evidence set, capacity/cost, incident, restore, and maintenance procedures. | Platform Operations reviewer | Review actual C0-C4 packets and record the actual same-hash Operations decision under the approved sole-owner policy. |
| C6 security acceptance | `operator-pending` | Least-privilege, Dapr-only data plane, TLS/secret, bounded observability, evidence redaction, and tenant-isolation documentation guards are repository-validated. | Named security acceptance of the same profile and immutable evidence hashes, with a distinct Security role record under the approved sole-owner policy. | Security reviewer | Review actual packets and record the actual same-hash Security decision; disclose sole-owner/self-reviewed. |

Only authentic external packets in state `passed` can satisfy C2-C6. The current sole-owner policy permits one named owner to accept distinct Operations/Security records; no independent-review claim is made. All full qualification and A41 completion remains pending.

