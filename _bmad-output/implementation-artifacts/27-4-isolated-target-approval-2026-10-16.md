# Story 27.4 isolated target approval and bounded bootstrap result

Date: 2026-10-16. Actual owner response: **“yes I approve”** to the immediately
preceding exact bootstrap and target-profile decision. Principal
`github:user:6775094`; assurance `sole-owner/self-reviewed`. This is the actual
conversation decision, with no independent-review or external-receipt claim.
Standing `sole-owner-qualification/2026-10-12` approval persists.

## Approved scope

- Exact bootstrap plan SHA-256:
  `0ced06b1c116de9f9155aba832150f69172fecd17d3e938d01aad36bb512160a`.
- Adopt the specific isolated-target PG2 profile SHA-256
  `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`. Canonical profile and three-path delta are in the
  target plan (superseded document removed; [current contract](spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md))
  and [approved manifest](../../docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json).
- One new local guest: 8 vCPUs, 40 GiB RAM, 40 GiB OS plus 600 GiB data;
  planned instance `2fe71a5c-bdda-4767-8780-8f27f5bcd520` on `DESKTOP-VIOG240`.
  At most one hour, one attempt, before `2026-10-17T16:30:00Z`; twelve fixed
  guest metadata commands; restricted guest egress and loopback-only forwarding.
- The decision excludes source access, Kubernetes installation, application
  deployment, faults/purge/reclaim, gate acceptance, A41/Git publication and
  Production activation. Technical gates, time bounds and protected history
  remain required. The target-profile adoption permits the necessary source
  migration under the existing implementation delegation; it supplies no
  qualification execution or acceptance authority.

This explicitly renegotiates Spec 14's current target/profile selection only.
Its other frozen intent and all six live execution requirements are unchanged.
The previous `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` PG2 approval, PG1, dated source inputs and captures
remain historical; none can satisfy the newly selected current target/session.
The approved new manifest retains all other technical identity fields,
capabilities and workload SHA-256
`71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.

## Actual bootstrap result

The actual decision was retained at `2026-10-16T07:05:33.486792Z` in private
`/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/evidence/decisions/bootstrap-owner-approval.json`, SHA-256
`f0f35950e7ff3711815c9fb3e5c6d977ff31d18b2d13ac5891d0657232642c2b`.
Fresh exact-byte, expiry, withdrawal, owner, classification, scope, private
credential/ACL, source/tool and available-resource checks preceded target writes.

- Base conversion completed, exit 0, 45.787 seconds.
- OS expansion returned exit **124** after **300.264 seconds**, hitting the
  approved deadline. The run stopped; no deadline extension or retry occurred.
- The 40-GiB task-owned OS raw file is retained. Linux and recovered Windows
  native allocation readback both show 42,949,672,960 physically allocated bytes,
  no sparse/compressed flag, with the prepared private owner/SYSTEM/Admin DACL.
- Data disk was not created; the VM was never launched; zero guest commands ran.
  Process inspection found no matching QEMU, qemu-img or timeout child; no QEMU
  PID or QMP socket exists. This proves bootstrap stopped, not gate/Lease/replica
  cleanup for a qualification run.
- Post-failure Windows metadata first failed with a transient WSL
  `UtilAcceptVsock` error 110. A fresh host command and native ACL/allocation
  readback subsequently succeeded. Both facts are retained.
- The one-attempt budget is consumed and executable status closed. `withdrawn`
  remains false: closure follows the execution failure, not owner withdrawal of
  either approved profile adoption or standing policy.

Read exact argv/times/streams/hashes in private `bootstrap-execution/`, including
`allocation-0.json`, `allocation-1.json`, `allocation-failure.json`,
`stopped-state.json` and `windows-stopped-readback.json`. Do not replay the old
plan. Prepare any bounded recovery from measured allocation state and the failed
step, then obtain only the genuinely missing changed execution scope.

Current execution follows the [Platform functional contract](spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md). This expired bootstrap decision is retained only for the actual result and resource ownership; it grants no new action.
