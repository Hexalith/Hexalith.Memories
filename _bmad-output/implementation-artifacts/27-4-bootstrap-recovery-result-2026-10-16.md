# Story 27.4 bounded bootstrap recovery result

Supporting operational record, 2026-10-16. Story 27.4 remains in-progress;
its six live tasks and A41 remain open. This record grants no qualification,
terminal gate/Lease/replica, publication or Production credit.

## Actual approval and execution

The owner replied **“I appeove”** to the immediately preceding exact recovery
proposal. The decision was retained with principal `github:user:6775094` and
assurance `sole-owner/self-reviewed`; no external or independent receipt is
claimed. Standing policy and isolated-target profile approvals persist.

- Approved recovery plan SHA-256:
  `50321897837eec8ca816dd5f66ccf370e98045fbc78ac0e9812304efc2292b20`.
- Actual decision SHA-256:
  `7c4a9ef380dcf7cb492c19ac3eb4a2e9564481b5147f29553cf9bb79ef389939`.
- Executed supervisor SHA-256:
  `996681d66ff22caa1b88aa26969ad63dc8aff387300b329a1ea4ccca1eb86e89`.
- Execution: `2026-10-16T10:37:41.599813Z` through approximately
  `2026-10-16T10:41:29.594350Z`; measured execution 227.456 seconds before
  terminal metadata/status finalization.

Exact host invocation:

```bash
/usr/bin/timeout --signal=TERM --kill-after=60s 3600s /usr/bin/python3 -B /home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc/control/recovery-supervision.py
```

It exited **1**. The original attempt remains closed; this recovery also consumed
its single attempt and now has `executableAuthority: false`. Neither failure
withdraws the standing policy or adopted profile.

## Measured results

- Fresh plan/decision/status/tool/seed/key and native host/NTFS/DACL checks passed.
  Ten offline denial-before-spawn cases and no-guest restoration checks passed
  before execution; they provide preparation evidence only.
- The complete 42,949,672,960-byte OS read passed in 225.890 seconds. SHA-256
  remained `074502ca51d9cb20a79cdabd715f5e06afbbbec298e19b84a6770104da6361de`;
  file identity and metadata remained unchanged.
- The exact new 644,245,094,400-byte data image was created, exit 0, in 0.108
  seconds. Both Linux allocated-block accounting and Windows
  `GetCompressedFileSizeW` reported the full allocation for both disk files.
  Neither file was sparse, compressed or a reparse point. Native owner and
  inherited owner/SYSTEM/Administrators DACL readback passed.
- Terminal readback retained both 40/600-GiB disks, with 191,945,895,936 bytes
  (about 178.76 GiB) free on C:. No disk was deleted or reformatted.
- QEMU exited **1** before a bound running guest or SSH was observed. Its exact
  error was: `Block format 'raw' does not support the option 'serial'`.
  The prepared command put each disk's serial in the raw-drive backend.
  Zero guest metadata commands ran.
- Independent process inspection found no matching QEMU/qemu-img/timeout
  process; both loopback ports were unbound and no QEMU PID file remained.
  A task-owned QMP socket file remained, but connection returned
  `ECONNREFUSED`. It was retained rather than reused or removed.

The raw supervisor's `vmStarted: true` denotes launch-process invocation. It
does **not** establish guest execution, QMP binding or completed boot. The
independent postflight explicitly records `runningGuestObserved: false`.
This stopped-process proof cannot satisfy qualification gate/Lease/replica
cleanup evidence for a deployed application.

## Evidence custody

Private root:
`/home/administrator/.local/state/hexalith/memories/access-telemetry/isolated-target-20261016T064337Z-506bfc`.
Exact argv, times, exits, bounded streams and metadata are retained under
`evidence/bootstrap-recovery-execution/`. Sixteen completed evidence files had
their private regular-file/single-link metadata and bytes independently read
back. Command stream hashes/lengths and the QEMU stream receipt match.

| Artifact | SHA-256 |
| :------- | :------ |
| `result.json` | `422dd84accde80bd1f181e4f56ae5a5078863a6d097dcc1196028f868f19856a` |
| `independent-postflight.json` | `0492c220206609d5e48e33d1a7d3ee61f1d70c907fcbad2634d51a3b7ddaba5d` |
| `evidence-manifest.json` | `5556c189191c774b3fa47880063f2311836773495721731ee71e63ddae1e87fe` |

The repository advanced externally to
`61074e981bd92019488db19da0c8572bd4bf8b97` during execution. This agent performed
no stage, commit, push, branch or dependency update. The bootstrap claims no
qualification-source eligibility for that revision.

Current execution uses the [Platform functional contract](spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md). The superseded boot-only proposal is removed; this result grants no retry or resource disposal.
