# Story 27.4 separate qualification target

Prepared 2026-10-16; exact bootstrap/profile approved subsequently. The bounded
bootstrap stopped at the OS expansion timeout before VM launch. See the
[actual decision and result](../../_bmad-output/implementation-artifacts/27-4-isolated-target-approval-2026-10-16.md).
The dated proposal below is retained; its one-attempt budget cannot be replayed.

The owner said **“apply recommended”**, selecting the
separate-target recommendation. The existing `jpiquot@local` target is excluded
from the qualification fault plan. Its environment remains unclassified; prior
discovery provides no permission to stop its workloads or shared Dapr members.
Standing `sole-owner-qualification/2026-10-12` approval persists.

## Applied preparation

The selected local option is one native KVM guest. It provides a distinct
Kubernetes API, Dapr control plane, PostgreSQL store and test credentials after
bootstrap. Single-node, local-storage limits still apply; no node, volume,
control-plane or site-loss availability is claimed. This is operational
preparation, not an executed qualification or an accepted replacement profile.

| Item | Prepared value |
| :--- | :------------- |
| Host | Local WSL2 host `DESKTOP-VIOG240`; Linux KVM, QEMU 10.2.1 |
| Guest | `memories-qualification-27-4`, planned instance UUID `2fe71a5c-bdda-4767-8780-8f27f5bcd520` |
| CPU / memory | 8 vCPUs / 40 GiB RAM |
| Disks | Fully allocated 40 GiB OS + 600 GiB data; 640 GiB total |
| Storage layout after later approved Kubernetes setup | Original 400 GiB PostgreSQL PVC; allow 60 GiB OpenBao, 51 GiB Dapr, 30 GiB Redis/FalkorDB; 59 GiB data-volume allowance remains before filesystem overhead |
| Context | Planned `owner@memories-qualification-27-4` in a new private kubeconfig; ambient kubeconfig is never changed |
| API / SSH | Planned `https://127.0.0.1:16443` / `127.0.0.1:12274` |
| Network | QEMU `restrict=on`; explicit inbound loopback forwards only; no bridge/TAP, source credentials, source data or guest external egress |
| OS | Ubuntu 24.04 amd64 cloud image dated 2026-09-26, SHA-256 `6a81c37564db9b1ee84e141922625e1d7c5b389b99bb3c572e0243607d5bb4d2` |
| Evidence | `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence` |
| Proposed bootstrap window | One attempt, at most one hour, before `2026-10-17T16:30:00Z` |

The native guest uses installed tools and existing KVM access; no Hyper-V
administrator grant, cloud subscription, new orchestration framework or
proprietary CLI is introduced. Kind was considered but not selected: its shared
Docker resource/storage accounting and additional network-policy setup do not
simplify this retained-storage qualification. No kind cluster was created.

Current local metadata shows about 799.8 GiB available on Windows C:, with
about 159.8 GiB estimated after full allocation. The Linux workspace has
only about 241 GiB free and is unsuitable for the disk images. The private
Windows directory is `/mnt/c/Users/JeromePiquot/AppData/Local/Hexalith/Qualification/memories-27-4-2fe71a5c`. Its DACL was prepared and read back with
only the current owner, SYSTEM and Administrators; disk images are not created.
Do not confuse virtual disk length, a PVC request or a thin image with reserved
space. Before allocation require C: free space >=790 GiB and MemAvailable >=56
GiB. After allocation require >=150 GiB C: free, >=16 GiB host available memory,
and actual non-sparse, uncompressed 640 GiB allocation. Recheck at execution;
these measurements are snapshots, not reservations or capacity gate passes.

The 600 GiB guest data disk is a backing pool, not a larger PostgreSQL profile.
The existing 400 GiB boundary and its 70/80/90-percent thresholds stay required.
Measured amplification, reclamation workspace, competing-volume use and full
Kubernetes resource admission must pass before lifecycle writes. An inadequate
8-vCPU/40-GiB envelope is a failed prerequisite; do not resize silently.

## Required profile decision

**QT1: a separate node/resource envelope cannot use the old PG2 hash.**
Spec 14 pins `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`;
the ADR states: “A node, storage class, size, or resource-envelope substitution
changes the profile hash.” See
[capacity contract](../dev/adr-27.1-001-access-telemetry-lifecycle.md#capacity-contract).

The exact candidate canonical profile hash is
`95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`. The private `proposal/profile-delta.json` records exactly:

1. `identity.kubeContext`: `jpiquot@local` to `owner@memories-qualification-27-4`.
2. `identity.profileId`: a distinct `qualification-kvm-v3` identity, preserving
   the Dapr/PostgreSQL/OpenEBS/400-GiB descriptors.
3. New `identity.qualificationEnvironment`: explicit native-guest name,
   host, resource envelope, loopback API and no source-cluster contact.

All remaining technical identity inputs, capabilities and workload bytes are
identical to the current canonical profile. Workload SHA-256 stays
`71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.
This is a **proposal**, not authority or evidence. Source constants, frozen
intent, protected ADR/history and all acceptance/refusal behavior remain intact.
Never disguise the new API behind the old `jpiquot@local` label to preserve a hash.

Explicit owner agreement is needed to adopt this specific target/profile change.
After that decision, implement and verify the profile migration and preserve old
records as history. Recapture all 25 gates in one eligible new session; no prior
gate is carried across the profile change. Actual guest/API/CA/namespace UIDs,
signed runtime-image identities, version compatibility and capacity must be
observed and bound in the later qualification plan. This snapshot does not prove
them or make qualification evidence portable to Production.

## Concrete bounded bootstrap for approval

Exact private plan: `/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/proposal/bootstrap-plan.json`.

SHA-256: `0ced06b1c116de9f9155aba832150f69172fecd17d3e938d01aad36bb512160a`.

The plan binds exact QEMU/qemu-img executable hashes, prepared guest seed,
cloud-init files, fixed command vectors, fresh bootstrap credentials, instance
UUID, resource/disk paths and target-specific candidate profile. It currently has
`planDecision: null`; `control/current-status.json` grants no executable authority.
No approval record or matching non-withdrawn run status has been fabricated.

Bootstrap scope is deliberately finite:

1. Re-read actual owner decision and withdrawal, exact plan/tool/seed hashes,
   expiry, private paths/ACL, available resources and unused target disk paths.
2. Fully allocate only the two fresh task-owned disk images; verify physical
   allocation and free-space floor. Refuse overwrites or target/ancestor drift. The three allocation children have
   300/300/900-second deadlines and 60-second termination graces. Launch the VM
   only if aggregate elapsed time is <=1740 seconds, leaving its full
   1800-second process bound and 60-second grace inside the one-hour total.
3. Boot only the named new guest as UID 1000 with the existing KVM device group.
   Use the fresh pinned SSH host key and client key, private serial/QMP controls,
   `restrict=on` networking and loopback-only forwards. The short QMP path fits
   Linux Unix-socket limits. No host group, global ACL or service is changed.
4. Execute at most twelve fixed guest OS/instance/disk/time/cloud-init metadata
   commands from `proposal/bootstrap-commands.json`; retain bounded raw results,
   UTC times, exact argv, exits and hash/readback receipts. No Kubernetes install,
   package update, application deployment, source access or qualification test is
   in this bootstrap scope. The data disk is left unformatted.
5. Stop the new guest within the one-hour aggregate bootstrap; cloud-init also schedules automatic
   poweroff after 30 minutes. A host `timeout` independently bounds the foreground
   QEMU process to 30 minutes with a 60-second termination grace, including a
   failed guest boot. Verify its UUID/PID/argv before restoration; use
   only its QMP shutdown, then verified-process termination after 60 seconds if
   needed. Retain the new disks, seed and evidence privately. Bootstrap key
   expires `2026-10-17T18:00:00Z`; later work needs fresh run-specific authority.

Expired/withdrawn approval, malformed records, drift, output overflow or any
failed check stops new work. Streams and the in-memory serial ring buffer are bounded to 1 MiB,
and total execution to one hour. Only the separately named restoration of this
new run-owned guest may finish after a stop. Never prune Docker assets, delete
other VM files, touch `192.168.1.30`, or fall back to the ambient kubeconfig.
The agent must retain the actual decision and perform fresh checks at execution;
the proposal JSON alone is not an enforcement mechanism or a live grant.

## Measured verification and preserved work

- Installed KVM API probe returned 12 while running the planned child identity
  UID 1000/GID 991; no VM was launched. Windows Hyper-V inventory was denied and
  is not used.
- `gpgv --keyring /usr/share/keyrings/ubuntu-cloudimage-keyring.gpg` verified
  Ubuntu's retained SHA256SUMS signature, exit 0. The 625,612,288-byte dated image
  matched that signed digest; `qemu-img check -f qcow2` exited 0.
- `cloud-init schema --config-file <private user-data>` exited 0; `cloud-localds`
  built the private seed, exit 0. Fresh keys are confined to the private control
  directory and seed; they are not placed in Git or exposed in this document.
- A disposable 16-MiB allocation probe on the selected Windows filesystem passed
  full-allocation and direct-I/O checks. Both Linux block accounting and Windows
  GetCompressedFileSizeW returned 16,777,216 allocated bytes; the exact probe file
  was removed. This validates tooling/filesystem preparation, not the future
  640-GiB reservation, PostgreSQL durability or a live gate.
- Windows PowerShell `Set-Acl` initially failed loading its module because of
  duplicate TypeData entries. The normal same-owner System.IO.Directory ACL API
  succeeded and the DACL was independently read back; no Windows elevation or
  policy bypass was used. Exact initial failure and successful checks are retained.
- Candidate canonical hash/delta checks preserve the current profile, all
  capabilities and the exact workload; new-profile evidence remains refused by
  current fixed-profile consumers. No C1 registration or acceptance is created.

The private `validation/` directory retains these exact measured results;
`local/baseline.json` pins protected bytes and frozen intent before this change.
The 2026-10-15 archive is retained separately and unchanged. Its 12-query budget
remains consumed/held; it cannot be replayed for this new target.

## Next execution after bootstrap and profile decision

Prepare the actual approved Kubernetes/runtime installation using existing
Platform-owned components and Memories manifests. Inventory exact packages,
images, CNI enforcement, OpenEBS Retain behavior, three dedicated Placement and
Scheduler members, OpenBao, new test credentials and all resource UIDs. Guest
Internet remains blocked; public installation materials must be verified on the
host and transferred over the pinned private transport. Scope any new outbound
access explicitly rather than opening a path to the protected cluster.

Then finish the named real collector/owner-consumer integration blocker using
observed facts and the adopted profile; retain predecessor refusal until actual
eligible C1 exists. Verify all 25 C1 gates in one session, obtain distinct owner
Operations/Security decisions, run C0/C2-C4 and obtain C5/C6. Preserve the existing
168-hour observation, 24-hour reclamation and 72-hour phase-authority constraints;
renewal integration and actual phase decisions remain necessary. A VM startup,
offline suite or prepared profile grants none of these passes.

Prove disabled gate, empty Lease and zero lifecycle/clock desired/ready replicas
with actual terminal readback after the executed run. Only accepted C0-C6 plus
terminal and exact A41/publication authority can advance the governed transition.
All six Spec 14 live tasks and A41/sprint holds remain open. Production keeps its
separate plan, qualification applicability checks, activation decision and
rollback requirements from the
[operational execution plan](story-27-4-execution-plan-2026-10-15.md#separate-production-rollout).
