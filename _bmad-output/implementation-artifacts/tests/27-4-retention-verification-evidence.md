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

Current C1 blockers are the separately owned, unregistered PG-ONPREM-2 C1.15
producer renewal and independent review, pending Story 27.22/C1.16 connection
linkage/capture/review, and twenty-three unregistered gate owners. The existing
C1.15/PG2 dispatcher rejects with `unsupported-successor-gate-or-historical-opt-in`
before target calls or output-directory creation; renewing it requires a separate
approved scope owned by Deployment Adapter Developer. Story 27.21's accepted PG1
capture provides no renewal credit. Approved/done current-profile gate-owner
registrations and twenty-five distinct passed artifacts with independent named
Platform Operations and Security approvals of the same hash are still required.

No accepted current-profile C1 predecessor, authorized non-Production target scope,
or external evidence-custody location has been supplied for this offline pass.
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

The fixture close-out chain uses temporary repositories and mocked targets; it
grants no live checkpoint, approval, A41 transition, or publication credit. No new
tests were added for this command correction. The C0-C6 matrix and all close-out
prerequisites above retain their recorded states.

The Production producers, close-out preflight/postflight, and publish verifier are
documented in
[Access Telemetry Lifecycle Operations](../../../docs/operations/access-telemetry-lifecycle.md).
