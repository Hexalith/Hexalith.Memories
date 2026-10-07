# Story 27.4 Retention Verification Evidence

## Evidence posture

This is the single canonical Story 27.4 C0-C6 status matrix. It records the
repository validation posture only; it is not a running-target evidence packet,
approval, or Production enablement decision. No target was queried while this
document was produced. Producer templates and fixture results are never represented
as executed Production evidence.

Production lifecycle writes remain disabled and
`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward/open until the complete
same-profile evidence, approval, terminal-validation, close-out, and remote-publish
chain passes. The [PG-ONPREM-2 adoption record](../../planning-artifacts/c1-security-prerequisites-2026-10-04/adoption.md)
and its [verification receipt](../../planning-artifacts/c1-security-prerequisites-2026-10-04/adoption-verification-evidence.json)
confirm repository bytes offline only; they provide no running-target gate pass or
independent approval. Historical PG-ONPREM-1 C1.15 capture grants no PG2 credit.

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
| C1 canonical predecessor | `operator-pending` | The verifier requires each canonical `C1.1` through `C1.25` result and rejects a synthetic aggregate. | Twenty-five individually attributable passing gates plus two different named reviewers approving the exact same profile hash. | Gate owners, Platform Operations, Security | Complete all 25 gates; no missing owner, skip, zero-result command, or shared reviewer can pass. |
| C2 production replacement | `operator-pending` | Producer schema, immutable packet writer, same-profile validator, concurrent fixed two-writer accounting, zero-default gate/Lease transition, and exact per-instance replacement selectors are repository-validated. | Controlled two-writer execution; replacement of both Servers and their sidecars, lifecycle/clock services and their sidecars, actor activation, all three Placement and Scheduler members; and adapter-fault execution with exact acknowledgements, recovery, and audit continuity. | Platform Operations | Run the reviewed C2 producer only after C1 passes with a named shared-system approval; its target-identity observation must prove an initially disabled exact-profile qualification namespace, empty Lease, and zero lifecycle/clock replicas. Zero acknowledged loss and a final disabled/empty/zero state are required. |
| C3 retention and reclamation | `operator-pending` | Cohort, 1/24/168-hour bounds, attestation negatives, interrupted-purge, newer-record, tuple-attribution, and logical/physical separation guards are repository-validated. | Executed expiry/purge and adapter reclamation commands bound to each of the three independent cohorts and its database/schema/table; newer records preserved and reusable allocator free-space increase observed within 86,400 seconds. | Lifecycle owner and adapter owner | Run the reviewed C3 producer after C1; an OS disk-shrink claim is prohibited. |
| C4 failure, privacy, and observability | `operator-pending` | Complete failure inventory, health precedence, NoData/last-evidence timestamp, bounded labels, and Story 20.2/24.3 denial guards are repository-validated. | Every declared dependency/fault lane, nonzero business samples with zero business failure, console/configured-OTLP continuity, alerts, and tenant denial before dependency access. | Platform Operations and Security | Run the reviewed C4 producer after C1; missing scenarios, raw/secret aliases, or dependency calls after denial reject. |
| C5 operations acceptance | `operator-pending` | Neutral and PostgreSQL-specific runbook structure, ownership, monitoring, RPO/RTO, rollback, rotation, and decommission contracts are repository-validated. | Named operations acceptance of the exact immutable profile, evidence set, capacity/cost, incident, restore, and maintenance procedures. | Platform Operations reviewer | Review actual C0-C4 packets and record an independent same-hash decision. |
| C6 security acceptance | `operator-pending` | Least-privilege, Dapr-only data plane, TLS/secret, bounded observability, evidence redaction, and tenant-isolation documentation guards are repository-validated. | Named security acceptance of the same profile and immutable evidence hashes, independent of the Platform Operations reviewer. | Security reviewer | Review actual packets and record a different named same-hash decision. |

Current C1 blockers, re-derived 2026-10-06 after Story 27.22 completed: the
separately owned, unregistered PG-ONPREM-2 C1.15 producer renewal and independent
review, and twenty-three unregistered gate owners. The existing C1.15/PG2
dispatcher rejects with `unsupported-successor-gate-or-historical-opt-in` before
target calls or output-directory creation; renewing it requires a separate approved
scope owned by Deployment Adapter Developer. Story 27.21's accepted PG1 capture
provides no renewal credit.

C1.16 is no longer a capture or linkage blocker. Story 27.22 is done, and its
independent disposition, SHA-256
`ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70`, accepts captured
component/backend identity and full-set connection linkage for one closed window.
Its canonical single-session refusal stays failed and owned by Deployment Adapter
Developer. That acceptance does not change the C1 row above and gives no Story
27.4, A41, Production or other-gate credit. It counts only when it is part of an
approved C1 predecessor bundle. Approved/done current-profile gate-owner
registrations and twenty-five distinct passed artifacts with independent named
Platform Operations and Security approvals of the same hash are still required.

None of these live inputs has been supplied for this offline pass: an accepted
current-profile C1 predecessor bundle (external bundle path), approved/done gate
owners and the two independent approvals, an authorized non-Production kube context
and namespace, an external evidence root and custody location, credential-file paths
(paths only, never credential values), and fault and purge authority.
Live C0/C2-C4 execution, independent post-evidence C5/C6 acceptance, terminal
validation, the exact four-path close-out, staged postflight, and authenticated
remote containment therefore remain pending. Story 27.4 remains incomplete, A41
and its sprint action remain open, and Production lifecycle writes stay disabled.

The only permitted states are `repository-validated`, `operator-pending`, `passed`,
and `rejected`. Only authentic external packets in state `passed` can satisfy C2-C6.
`repository-validated` means that the offline machinery is ready; it never advances
Production or A41.

## Close-out prerequisites

| Prerequisite | Current state | Required transition |
| :----------- | :------------ | :------------------ |
| Terminal validation | `operator-pending` | Authenticate the exact same-profile passing C0-C6 artifacts, including both independent post-evidence approvals. |
| A41 inventory and recoverable preflight snapshot | `operator-pending` | Run on a clean open commit; classify every A41 reference and approve exactly the four mutable paths. |
| Exact staged postflight | `operator-pending` | Stage only the approved semantic transitions; preserve Epic 20, Story 20.5, and `sprint-status.yaml` bytes. |
| Remote publish verification | `operator-pending` | Prove the exact close-out commit is contained by the authenticated intended remote branch. A local commit is insufficient. |

## Offline repository verification

Run from the repository root after the [README clone/setup step](../../../README.md#2-clone-and-initialize-submodules-2-min-cold).
The Debug/source-reference build requires those dependency checkouts. Only submodules
declared in the root `.gitmodules` may be initialized, without `--recursive`; this
verification block does not initialize or update any checkout.

These commands validate implementation structure and deterministic fixtures only.
The scoped Bash block stops on any command or pipeline failure and retains each
command, exit code, stdout/stderr log, result XML, source revision/worktree diff,
nonrecursive dependency revisions, and built assembly hash in a unique local
`/tmp` folder. Keep the printed folder for review. It is offline repository
verification output, not an external custody archive or live evidence authority.

```bash
(
    set -euo pipefail
    verification_dir=$(mktemp -d /tmp/story-27-4-offline.XXXXXXXX)
    trap 'verification_status=$?; printf "%s\n" "$verification_status" > "$verification_dir/exit-code"; printf "Offline verification folder: %s (exit %s)\n" "$verification_dir" "$verification_status"; exit "$verification_status"' EXIT

    run_logged() {
        local name=$1
        shift
        printf '%q ' "$@" > "$verification_dir/$name.command"
        printf '\n' >> "$verification_dir/$name.command"
        local command_status=0
        "$@" > "$verification_dir/$name.stdout.log" 2> "$verification_dir/$name.stderr.log" || command_status=$?
        printf '%s\n' "$command_status" > "$verification_dir/$name.exit-code"
        return "$command_status"
    }

    run_logged source-revision git rev-parse HEAD
    run_logged source-status git status --porcelain=v1 --untracked-files=all
    run_logged source-diff git diff --binary HEAD
    run_logged dependency-revisions git submodule status
    run_logged lifecycle env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v
    run_logged build dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true
    assembly=tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll
    run_logged assembly-sha256 sha256sum "$assembly"
    run_logged architecture env DiffEngine_Disabled=true dotnet exec "$assembly" -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo -failSkips -result-xml "$verification_dir/architecture.xml"
    run_logged architecture-results python3 - "$verification_dir/architecture.xml" <<'PY'
from collections import Counter
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

report = Path(sys.argv[1])
if not report.is_file():
    raise SystemExit("Missing architecture result XML")
expected = {
    "Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests": 12,
    "Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests": 5,
}
tests = ET.parse(report).getroot().findall(".//test")
counts = Counter(test.get("type") for test in tests)
if counts != Counter(expected):
    raise SystemExit(f"Wrong architecture per-class counts: {dict(counts)}; expected {expected}")
if any(test.get("result") != "Pass" for test in tests):
    raise SystemExit("Architecture results contain a non-Pass test")
print("12 retention-decision and 5 A41 guards passed; every result is Pass.")
PY
    run_logged whitespace git diff --check
)
```

Initial offline execution from the repository root on 2026-10-06 produced the
streamed results below; no retained command/source receipt was created for that
initial run. Use the block above to retain a fresh receipt. These historical counts
do not establish verification of a later worktree.

| Check | Result |
| :---- | :----- |
| Lifecycle tooling | 79 passed; zero failures, errors, or skips. Includes the existing scenario, predecessor-denial, disabled-state cleanup, and complete close-out fixtures. |
| Debug/source-reference Server test build | Zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision guards plus 5 A41 guards: 17 passed; zero errors, failures, skips, or not-run tests. |
| Whitespace | `git diff --check` passed. |

Final parent execution of the reviewed block on 2026-10-06 also passed: 79
lifecycle tests, exact 12/5 architecture counts with every result `Pass`, a Debug
build with zero warnings/errors, and clean whitespace. All ten logged commands
and the scoped block exited zero. The retained local receipt is
`/tmp/story-27-4-offline.4PZnqjZw`; it contains the executed commands, exit codes,
stdout/stderr, architecture XML, full source revision and execution-time diff,
nonrecursive dependency revisions, and assembly SHA-256. Its revision and assembly
hash were independently checked. Subsequent changes only record these results and
the review disposition; the receipt is offline validation, not live gate credit.

The receipt's identifiers, re-read on 2026-10-06, so the record does not depend on
the ephemeral `/tmp` folder:

| Item | Identifier |
| :--- | :--------- |
| Source revision | `b7a4377aaebce4b2d5f9e66d583ce9022f2f9fca`, with this file and the Story 27.4 spec modified in the worktree |
| Execution-time diff (`source-diff.stdout.log`) | SHA-256 `e5678e0479bdb0ed4bf718a66be8d6f3afda84be599c42848582579852f011dd` |
| Built assembly | SHA-256 `c14d72222e20f4964e9fe928189f143ae2328cda3ac1a558889815cc54932700` (build identity only) |
| `architecture.xml` | SHA-256 `f86ffcc3b3b64b4e3ab038b9c6e2b9bc1372a640a9722f38e8bbea67d46a8e3c`; 17 total = 12 + 5, 17 passed, 0 failed, skipped or not run; run 2026-10-06 08:20:57 UTC |
| `lifecycle.stderr.log` | SHA-256 `e8b46a4515ea4cd3d58c1c994c6582a923b9fc907df8180276abe8d5a2c396ff`; `Ran 79 tests`, `OK` |
| `build.stdout.log` | SHA-256 `d067f4d063840a2d88f437e4b5fbd8dc79655a2f883b8d74d4cdb6a337a0b417`; 0 warnings, 0 errors |
| Exit codes | All ten logged commands and the scoped block: `0` |

The folder also holds two files that the documented block did not produce. They
were added after it finished and had not been disclosed before:

- `final-a41.xml`, SHA-256 `c01b4a152f2066d6cb88e8b8ddd57cf20a8048c446587c800ada8baf5fb08a42`:
  a separate `AccessTelemetryA41CloseOutTests`-only rerun at 2026-10-06 08:22:40 UTC
  after the results were recorded; 5 total, 5 passed, 0 failed, skipped or not run.
- `final-worktree.diff`, SHA-256 `51bce7acc36bf9e3d8fd09b79d581ff8e49c405ec72b54ddf9bed3392510b570`:
  the worktree diff after the results were recorded, byte-identical to the reviewed
  `b7a4377a..95a38fd8` change.

Neither file is part of the ten-command receipt, and neither grants live credit.

Post-patch execution of the unchanged block on 2026-10-06, after the second-pass
review patches were applied, also passed. Receipt
`/tmp/story-27-4-offline.al6C55hg` (local, not custody):

| Item | Identifier |
| :--- | :--------- |
| Source revision | `6f3c727af1c9733fffb182b342f471f19b213b39`, with the review-patched Story 27.4 spec, this file and `deferred-work.md` modified in the worktree |
| Execution-time diff (`source-diff.stdout.log`) | SHA-256 `f54084f65feab5cd2a8297d6f04d6af331a5b9e749e4ec68f0ee85d30f66d3b3` |
| Built assembly | SHA-256 `5f537e2be4b18f4c3e5c5054604d2d76ce112018f1813f4fd37104f29096f0b4` (build identity only; dependency gitlinks moved since the first receipt) |
| `architecture.xml` | SHA-256 `240fd6244aa6982782057f25e685cf6ae58c57b2b0db0c133de83420f707c122`; 17 total = 12 + 5, 17 passed, 0 failed, skipped or not run; run 2026-10-06 19:09:10 UTC |
| `lifecycle.stderr.log` | SHA-256 `d52bebbfa1cb5797f8414be1c6e4b938323441570fc3b231ee64d9cf6bcf71d8`; `Ran 79 tests`, `OK` |
| `build.stdout.log` | SHA-256 `fdcdd141083bb83633d7fd206511e1ee440c003a2c4c866f6f49ff3bce1e445f`; 0 warnings, 0 errors |
| Exit codes | All ten logged commands and the scoped block: `0` |

A fail-fast check ran the same block text with only the lifecycle command replaced
by `false` and a separate folder prefix. Receipt
`/tmp/story-27-4-failfast.njdCm7qY`: the source and dependency commands exited 0,
the substituted lifecycle command exited 1, and the block exited 1. The folder
has no build, assembly, architecture, validator or whitespace file, so nothing ran
after the failure. It also holds `failfast-block.bash`, SHA-256
`69a51b4c9d1811b4c8e33a7ed5758dbe96ef4844564b1ddb8c03a431f597a7cb`, the exact
modified block, copied in after the run.

The post-patch execution-time diff was captured before these identifiers were
recorded, so it shows placeholders where they now appear. Recording them is the
only later change to this file. Both receipts are offline validation, not live
gate credit.

The fixture close-out chain uses temporary repositories and mocked targets; it
grants no live checkpoint, approval, A41 transition, or publication credit. No new
tests were added for this command correction. The C0-C6 matrix and all close-out
prerequisites above retain their recorded states.

The Production producers, close-out preflight/postflight, and publish verifier are
documented in
[Access Telemetry Lifecycle Operations](../../../docs/operations/access-telemetry-lifecycle.md).
