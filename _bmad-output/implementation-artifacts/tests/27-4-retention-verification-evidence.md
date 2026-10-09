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
| C1 canonical predecessor | `operator-pending` | The legacy verifier structurally checks each canonical `C1.1` through `C1.25` input and rejects a synthetic aggregate; it does not authenticate reviewer identity or establish accepted C1 evidence. | Twenty-five individually attributable passing gates with registered/done owners and eligible session, plus authenticated Operations and Security decisions bound to the exact manifest and profile hash. | Gate owners, Platform Operations, Security | Complete all 25 gates and the approved authenticated interchange. The required authenticated interchange must refuse missing owners, skips, zero-result commands and bare reviewer labels. The named-owner bundle exception below applies only after authenticated role, scope and producer-exclusion checks; the legacy guard remains stricter. |
| C2 production replacement | `operator-pending` | Producer schema, immutable packet writer, same-profile validator, concurrent fixed two-writer accounting, zero-default gate/Lease transition, and exact per-instance replacement selectors are repository-validated. | Controlled two-writer execution; replacement of both Servers and their sidecars, lifecycle/clock services and their sidecars, actor activation, all three Placement and Scheduler members; and adapter-fault execution with exact acknowledgements, recovery, and audit continuity. | Platform Operations | Run the reviewed C2 producer only after C1 passes and the separately scoped P7 runtime migration is approved and verified, with a named shared-system approval; its target-identity observation must prove an initially disabled exact-profile qualification namespace, empty Lease, and zero lifecycle/clock replicas. Zero acknowledged loss and a final disabled/empty/zero state are required. |
| C3 retention and reclamation | `operator-pending` | Cohort, 1/24/168-hour bounds, attestation negatives, interrupted-purge, newer-record, tuple-attribution, and logical/physical separation guards are repository-validated. | Executed expiry/purge and adapter reclamation commands bound to each of the three independent cohorts and its database/schema/table; newer records preserved and reusable allocator free-space increase observed within 86,400 seconds. | Lifecycle owner and adapter owner | Run the reviewed C3 producer after C1 and approved, verified separately scoped P7 runtime migration; an OS disk-shrink claim is prohibited. |
| C4 failure, privacy, and observability | `operator-pending` | Complete failure inventory, health precedence, NoData/last-evidence timestamp, bounded labels, and Story 20.2/24.3 denial guards are repository-validated. | Every declared dependency/fault lane, nonzero business samples with zero business failure, console/configured-OTLP continuity, alerts, and tenant denial before dependency access. | Platform Operations and Security | Run the reviewed C4 producer after C1 and approved, verified separately scoped P7 runtime migration; missing scenarios, raw/secret aliases, or dependency calls after denial reject. |
| C5 operations acceptance | `operator-pending` | Neutral and PostgreSQL-specific runbook structure, ownership, monitoring, RPO/RTO, rollback, rotation, and decommission contracts are repository-validated. | Named operations acceptance of the exact immutable profile, evidence set, capacity/cost, incident, restore, and maintenance procedures. | Platform Operations reviewer | Review actual C0-C4 packets and record an independent same-hash decision. |
| C6 security acceptance | `operator-pending` | Least-privilege, Dapr-only data plane, TLS/secret, bounded observability, evidence redaction, and tenant-isolation documentation guards are repository-validated. | Named security acceptance of the same profile and immutable evidence hashes, independent of the Platform Operations reviewer. | Security reviewer | Review actual packets and record a different named same-hash decision. |

Current C1 blockers, re-derived 2026-10-07 after the separate PG2 C1.15
preparation completed: accepted PG-ONPREM-2 C1.15 renewal with an authenticated
independent disposition, twenty-three registered/done gate owners, and executable
authenticated predecessor interchange. The C1.15/PG2 dispatcher now supports a
neutral v2 capture with a valid `QualificationSessionId` and the shared preflight;
repository preparation is complete. The earlier
`unsupported-successor-gate-or-historical-opt-in` refusal described historical
dispatch behavior before that preparation and is no longer a blocker for this
supported mode. A missing or invalid PG2 C1.15 session still fails with
`qualification-session-required-or-invalid` before target calls or output-directory
creation. The [capture/disposition contract](../../../docs/operations/access-telemetry-c1-pg2-c1-15-contract.md)
and separate preparation remain distinct from live capture, custody and accepted
renewal. Story 27.21's accepted PG1 capture provides no PG2 renewal credit.

C1.16 capture and linkage are accepted for the closed window. Story 27.22 is
done, and its independent disposition, SHA-256
`ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70`, accepts captured
component/backend identity and full-set connection linkage for one closed window.
Its canonical single-session refusal stays failed and owned by Deployment Adapter
Developer. That acceptance does not change the C1 row above and gives no Story
27.4, A41, Production or other-gate credit. It counts only when it is part of an
approved C1 predecessor bundle. A future bundle must establish the capture's
session eligibility and cannot reuse the closed scope as execution authority.
A bundle using a new session requires a separately authorized fresh C1.16 capture
and independent disposition; historical acceptance remains preserved.
Approved/done current-profile gate-owner registrations and twenty-five distinct
passed artifacts with two authenticated Platform Operations and Security bundle
decisions for the exact manifest/profile remain required. The owner-approved
exception permits Jérôme Piquot (`github:user:6775094`) to approve both C1 bundle
roles only through distinct decisions/receipts and authenticated role/scope/time
checks, excluding every capture producer. Other bundle reviewers must be distinct.
Reconsider the exception before Production activation or account/role/producer
changes. The existing label-only legacy predecessor still rejects a shared
reviewer, but accepts two distinct nonempty reviewer labels with claimed approval
fields as structurally valid. That structural verdict authenticates no decision
and cannot satisfy the required C1 acceptance. The prepared authority checks
supply no legacy bypass or accepting predecessor. Post-evidence C5/C6 still require separate reviewers.

The runtime profile mismatch identified on 2026-10-07 at
`95dd8758f2f36d262a2b5c23269a0d73a2d7825b` now has a separately scoped
[repository source/test correction](../spec-migrate-runtime-qualification-gate-to-pg-onprem-2.md).
`AccessTelemetryQualificationGate.ApprovedProfileSha256` pins current PG2
`7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`, matching
the canonical Python producer profile. Runtime tests independently author the
published PG2 and historical PG1 hashes rather than deriving inputs from the
runtime constant. They prove PG2 acceptance and PG1 rejection, Qualification-only
access, bounded expiry, fresh-file renewal and revocation. This source/test slice
is `repository-validated`; its dated receipt below is offline evidence only.

The [authenticated predecessor interchange specification](../../specs/spec-pg2-c1-authenticated-predecessor-interchange/SPEC.md)
has completed I1 structural readers, separately reviewed narrow GitHub
policy/session/gate/bundle observation preparation, the offline source-receipt
label/digest correction, offline I2 registry/source inspection, and offline
C1.15 declared-observation pin inspection. Those slices authenticate structure,
observations, isolated fixture bindings, or declared pins. They confer no
registration, deployed accepting entry, semantic gate verdict, or accepted
predecessor. Closed I2 registration and provenance, I3–I6 authority consumption,
gate semantics, assembly, consumer migration, and final verification remain
incomplete. Operational P1–P7 inputs/decisions remain unresolved; the full
interchange is not implementation-ready for live acceptance. See the
[authority preparation contract](../../../docs/operations/c1-github-authority-contract.md),
the [source-receipt correction](../spec-pg2-c1-15-source-receipt-labels.md),
the [producer-binding inspection contract](../../../docs/operations/c1-producer-binding-inspection-contract.md),
and the [observation-pin inspection contract](../../../docs/operations/c1-observation-pin-inspection-contract.md). Its
[P7 migration prerequisite](../../specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md#decisions-and-ownership-still-required)
requires a separately scoped runtime correction. The source/test correction above
satisfies that repository behavior check, while Architecture and Operations runtime
migration approval and authorized deployment remain pending before live C2-C4
execution. P7's remaining consumer inventory, compatibility/dispatch and
authenticated-interchange decisions remain unresolved. Its proposed decision route
assigns the handoff to Deployment Adapter Developer and the Story 27.4 machinery
owner; accountable people remain unassigned. These role labels and offline test
results confer no approval, completed ownership or running-target qualification.
The historical handoff and receipts below preserve the earlier mismatch as observed.

None of these live inputs has been supplied for this offline pass: an accepted
current-profile C1 predecessor bundle (external bundle path), approved/done gate
owners and two authenticated C1 bundle-role decisions under the named-owner
policy above, an authorized non-Production kube context
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

### 2026-10-07 offline resumption verification

The unchanged canonical block above passed again from the repository root before
these dated receipt notes were recorded. Retained local receipt:
`/tmp/story-27-4-offline.sSe9i00R`; all ten logged commands and the scoped block
exited `0`. Its logs confirm `Ran 79 tests` and bare `OK`, zero build warnings
and errors, and exactly 12 retention-decision plus 5 A41 results, every result
`Pass`, with zero failures, errors, skips or not-run tests.

| Item | Identifier |
| :--- | :--------- |
| Source revision | `5ccacbe5b017b1967dbd9e7b6a253cbab9d8b278`; the only execution-time worktree change was the existing, user-owned `references/Hexalith.Builds` gitlink at `3107b18de0b740fe77e274d06c79b47d52dd7b83`, preserved unchanged |
| Execution-time diff (`source-diff.stdout.log`) | SHA-256 `c5af86641ddeb8afec282d230a3c15e634a69dd6affb81149dc51a72c1f37d73` |
| Built assembly | SHA-256 `226e529497658c3514e402cdb21390057fb07f753808d2d2445bbd9153846594` (build identity only) |
| `architecture.xml` | SHA-256 `509b8bf874c667613fde4ac3c7aad613abd99a34f6a44da283638f1aee1d5a52`; 17 total = 12 + 5, every result `Pass`; run 2026-10-07 04:50:48 UTC |
| `lifecycle.stderr.log` | SHA-256 `99e944ffa2818df20b6fffd985a88eebde5130c9f223f58501edaad8960b5366`; `Ran 79 tests`, `OK` |
| `build.stdout.log` | SHA-256 `bed98c98aa3604a100b5a6157f8c148ee49f03002d166fe0244b4e66a334df73`; 0 warnings, 0 errors |
| Exit codes | All ten logged commands and the scoped block: `0` |

Current prerequisites remain absent: separately approved PG2 C1.15 renewal,
twenty-three registered/done gate owners, the accepted 25-artifact same-PG2 C1
bundle and independent Platform Operations/Security approvals, authorized
non-Production context/namespace, external evidence custody, credential-file
paths, and fault/purge authority. Story 27.22's accepted C1.16 capture remains
accepted for its captured window only. Tasks 3-4, C0-C6 live qualification,
terminal/postflight/remote verification and the exact four-path close-out remain
pending; Story 27.4 stays in progress, A41 and its sprint action stay open, and
Production writes stay disabled. This run contacted no live target and performed
no staging, commit or publication. No source, test or verification command changed.

The fixture close-out chain uses temporary repositories and mocked targets; it
grants no live checkpoint, approval, A41 transition, or publication credit. No new
tests were added for this command correction. The C0-C6 matrix and all close-out
prerequisites above retain their recorded states.

The Production producers, close-out preflight/postflight, and publish verifier are
documented in
[Access Telemetry Lifecycle Operations](../../../docs/operations/access-telemetry-lifecycle.md).

### 2026-10-07 PG2 C1.15 repository producer preparation

The separate [PG2 C1.15 preparation spec](../spec-pg2-c1-15-runtime-control-plane-renewal.md)
now has an offline-testable v2 runtime/control-plane collector, a shared sixteen-input
preflight and a [capture/independent-disposition contract](../../../docs/operations/access-telemetry-c1-pg2-c1-15-contract.md).
Successful fixtures bind the exact profile/workload/session/target, full source
HEAD, all three producer/helper sources and sixteen approved input hashes,
effective invocation, actual Kubernetes and Git command receipts and final source
recheck. Dirty development captures stay labelled; all packets remain neutral.
Live capture, external archive custody and independently authenticated acceptance
remain pending. Production is disabled and the C0-C6 matrix, Story 27.21/27.22,
Story 27.4 and A41 states remain unchanged.

| Offline check | Receipt and result |
| :--- | :--- |
| Review-patched PG2 C1.15 lane | `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'pg2_runtime_control_plane_identity_test.py' -v`; 22 cases passed in 547.824s, zero failures/errors/skips. Log `/tmp/pg2-c1-15-review-patch-focused.log`, SHA-256 `3edf582c882aaf8eed3dbe64532d8709b5759ab4f33a82b8bde6ecee4f82a9ac`. Covers exact profile/workload/target and unique 19-source bindings; parsed-source/helper load and final-query races; late dirty refresh; all sixteen input refusals; index/child/bare and mixed/reordered captures; harmless metadata changes; embedded/direct/decoded credential safety and selected secret references; scalar/array/UTF-8/malformed output; absolute LF/CRLF identity anchors; token/identity drift; Git/Kubernetes size/deadline refusals; immutable neutral output and PG1 v1 preservation. |
| Required-session legacy guard | The updated C1.16 mode-denial method passed separately: 1 test, zero failures/errors/skips; `/tmp/pg2-c1-15-rederived-session-denial.log`, SHA-256 `8daa346844e6cc7915155759de96e8f768224efa26f9359c8c4f66d27a440034`. Missing PG2 C1.15 session is explicitly refused before calls/output. |
| Earlier canonical lifecycle/architecture boundary block | The unchanged block above passed with receipt `/tmp/story-27-4-offline.7ETQL5cv`; all ten commands and the block exited 0. Lifecycle: 79 passed. Architecture: exactly 12 retention-decision + 5 A41 guards, all `Pass`, zero failures/errors/skips/not-run. Debug source-reference build: zero warnings/errors. This historical receipt predates source-snapshot/Git transport correction; the final-source receipt below supersedes it for current verification. |
| Earlier canonical execution source | Full HEAD `cc754ab1487ddcec40340f9afa370aa3114851f1` with implementation changes in the worktree. Its execution-time tracked diff hash is `aa5fc14da52419b4e389708561b9c7b679d1f07d063c79e5aa9fd20c350229e9`; untracked paths are listed in the retained status receipt. This boundary receipt predates the source-snapshot/Git transport correction; the corrected PG2 lane above validates the re-derived producer paths. |
| Final-source canonical lifecycle/architecture boundary block | The unchanged block above passed with receipt `/tmp/story-27-4-offline.jZZMVwe1`; all ten commands and the block exited 0. Lifecycle: 79 passed. Architecture: exactly 12 retention-decision + 5 A41 guards, all Pass, zero failures/errors/skips/not-run. Debug source-reference build: zero warnings/errors. XML SHA-256 `945fab28cbdd337e292f17bc57c5a3305997b54d968057d01624634a3f2d7fa9`; execution-time tracked diff SHA-256 `85415f9b95d6d974d8aaaf6704b8cbc550ea331dbdba987632bcff6c8d623adc`. Dated receipt metadata was recorded afterward; these are offline checks only. |
| Boundary refresh after external workspace commit | A separate `/pushall` operation committed the preparation and updated the Platform gitlink in `796710937e3dd85e06ed83056b9b36ee06ff96c3`; this build session performed no commit, push or dependency update. Collector/test bytes and all 72 protected files stayed unchanged. Receipt `/tmp/story-27-4-offline.sEO6mFSP` passed at that full HEAD and Platform `9cb2dc13f8e993293810ccbd984772a1e2feb019`: all ten commands/block exit 0, 79 lifecycle tests and exactly 12 + 5 architecture guards passed, zero failures/errors/skips/not-run; build zero warnings/errors. XML SHA-256 `58435d7e0547676b4e7cd75e61dee6ec5ba0632226d77441b7773ac2d3c1d1c8`. The execution-time tracked diff was empty; these dated receipt notes were recorded afterward. |
| Complete C1 regression | `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v`; 108 unittest cases = the existing 86 + 22 PG2 cases passed in 1957.806s, zero failures/errors/skips, exit 0 and bare OK. Parent-owned receipt `/tmp/pg2-c1-15-reviewed-verification-lo9pmav6/c1.stdout-stderr.log`, SHA-256 `4f59d7744389b2f6526867a93c8077d819e77358c1ed3c42b95cd0828792d5ca`. Earlier incomplete/stopped runs grant no full-suite verification. Three review layers completed; confirmed fixes verified, uncommon low findings rejected with evidence in the spec, no deferrals. |

These `/tmp` receipts are local repository verification, not live evidence or a
custody archive. The implementation makes no reviewer-authentication, C1.17,
aggregate C1, successor-registration or activation claim. No deployment input,
historical capture or independent disposition was changed.

### 2026-10-07 Story 27.4 current handoff reconciliation

The current handoff now distinguishes the callable neutral PG2 C1.15 v2 producer
from its missing live capture, custody and authenticated independent acceptance.
The unsupported-dispatch statement above has been reconciled with the completed
separate preparation; its historical refusal and the accepted PG1 packet remain
historical only. No producer, test, profile, registration or verification command
changed in this Story 27.4 slice.

The unchanged canonical ten-command Bash block passed after that correction and
before these dated receipt notes were appended. Retained local receipt:
`/tmp/story-27-4-offline.tCpEtJN6`. All ten commands and the scoped block exited
`0`; 79 lifecycle cases passed in 47.979 seconds with bare `OK`, zero failures,
errors or skips. The Debug/source-reference build had zero warnings/errors.
Architecture XML contains exactly 12 retention-decision and 5 A41 guards, all
`Pass`, with zero failures, errors, skips or not-run tests. Whitespace passed.

| Item | Identifier |
| :--- | :--------- |
| Source revision | `b350c094ab4bc10bd2daf723f9e2c90ffdddae2a`; execution-time changes were the existing Story 27.4 spec and this handoff file; no dependency revision changed |
| Execution-time diff (`source-diff.stdout.log`) | SHA-256 `cbb03717ce9903db2f41d91eb5dc08e614aeab0c824346dc940ee46560d6de6c` |
| Built assembly | SHA-256 `54975db810c985922775a6392a113bed4dba80e7ab2f2e2b16b0575550dc48ad` (build identity only) |
| `architecture.xml` | SHA-256 `04a6ed02217099de59dd52eeb896de136b890804aba5ba84c36a1ed292090c2c`; run 2026-10-07 07:42:22 UTC, 17 total = 12 + 5, every result `Pass` |
| `lifecycle.stderr.log` | SHA-256 `16243498c2a1d4185286a74a098ac66ce2ec699583e059ce04972e3fb1298970`; `Ran 79 tests`, `OK` |
| `build.stdout.log` | SHA-256 `0cc511f4b843f173010d258b9444b753316bc1ce80a20e87fbc7f15c668f8c63`; 0 warnings, 0 errors |
| Exit codes | All ten logged commands and the scoped block: `0` |

A separate initial-handoff post-edit recheck passed in
`/tmp/story-27-4-handoff.o88ERlot` at 2026-10-07 07:43:56 UTC. Its exact four logged
commands were `assembly-sha256`, `architecture`, `architecture-results` and
`whitespace`; each command and the scoped block exited `0`. Both architecture
classes passed with exactly 12 retention-decision + 5 A41 guards, every result
`Pass`, zero failures, errors, skips or not-run tests. Its `architecture.xml`
SHA-256 is `4fd0c2ec49cb87d097c445a6c9416e73fbd784290b5cfe6d76a0d0409eeff657`.
This four-command receipt records the initial handoff's post-edit checks separately
from the earlier ten-command `/tmp/story-27-4-offline.tCpEtJN6` receipt; it predates
these review clarifications.

The existing offline cases exercise structural/fixture behavior relevant to the
scenario rows below; every named method reports `ok` in the ten-command receipt's
lifecycle log. Temporary repositories and mocked targets confer no live credit.
Authenticated refusal of an unaccepted prerequisite before target access awaits
executable authenticated interchange. This run's no-target outcome was a deliberate
hold on live execution, not proof of that automated enforcement.

| Scenario | Existing passing fixture methods | Structural/fixture boundary |
| :--- | :--- | :--- |
| Denial | `test_current_target_predecessor_approvals_and_c0_reject_old_or_mixed_evidence`; `test_c1_requires_unique_25_gate_artifacts_disabled_production_and_authorization`; `test_both_python_cli_preflights_refuse_bound_input_drift_before_any_kubectl` | Reject historical/mixed identities, reused gate artifacts, enabled Production, missing qualification authorization and bound-input drift. Drift rejection precedes mocked Kubernetes calls. |
| Qualification | `test_complete_c2_c3_and_c4_packets_validate`; `test_producer_restores_disabled_state_when_body_fails_after_enable`; `test_producer_restores_disabled_state_when_enable_response_is_malformed`; `test_qualification_renewal_rejects_lost_lease_ownership`; `test_registered_producers_and_complete_close_out_chain` | Validate fixture packets, refuse source/Lease drift, restore the disabled gate, release the owned Lease and leave zero lifecycle/clock replicas. The complete-chain fixture also checks exact producer/source bindings and checkpoint artifact hashes. |
| Closure | `test_registered_producers_and_complete_close_out_chain` | Reject tampered terminal artifacts and dirty preflight; accept the exact four-path fixture transition; reject unpublished, wrong-branch and remapped-remote publication; accept containment in the fixture's local bare remote. |

The existing predecessor validator checks structure, hashes and command ledgers;
these fixture results do not authenticate reviewers, establish artifact semantics
or register gate producers. Executable authenticated interchange remains separate
prerequisite work under the [D3 contract](../../planning-artifacts/c1-security-prerequisites-2026-10-04/predecessor-interchange-contract.md).

Accepted PG2 C1.15 renewal, twenty-three registered/done owners, a genuine accepted
25-gate same-profile/session bundle and independent Operations/Security decisions
remain absent. The authorized non-Production context, namespace, deployment and
session, custody root, credential-file paths and shared-system/fault/purge scope
are also absent. Story 27.22 acceptance is limited to its closed C1.16 window.
Live C0/C2-C4, C5/C6 acceptance, terminal/postflight/publication and A41 closure
therefore remain pending. Story 27.4 stays incomplete; A41 and its sprint action
stay open; Production writes stay disabled. No live target was contacted and no
repository staging, commit, publication or dependency update was performed.

Parent verification after the three review clarifications passed using the unchanged
canonical ten-command block. Receipt `/tmp/story-27-4-offline.L9PhHyQb`: all ten
commands and the scoped block exited `0`; 79 lifecycle cases passed in 36.988s
with bare `OK`, zero failures/errors/skips; Debug/source-reference build zero
warnings/errors; exactly 12 retention-decision + 5 A41 guards, every result `Pass`,
zero failures/errors/skips/not-run. The XML run was 2026-10-07 07:53:27 UTC.
Source HEAD was `b350c094ab4bc10bd2daf723f9e2c90ffdddae2a`, with only this handoff,
its spec and an appended pre-existing dependency verification deferral changed.
Execution-time diff SHA-256 `2ffc7a3fe5449176b26d1dad574e4a1559ffff6e59c134168e3c29e118e35ff5`;
XML SHA-256 `efc7e061185c40c729d3fbeb2da559675a6d88efa9b69354600530e7600e327b`;
lifecycle log SHA-256 `b6d42dce0899fb7cb2fe742a1e9f20527e6f81f244240163a6bdfd80e3c2763d`;
build log SHA-256 `22171dcfcd072c2610830b777e590ee1628f05d2862921bbe7a760170c19a4e5`.
The assembly SHA-256 remained `54975db810c985922775a6392a113bed4dba80e7ab2f2e2b16b0575550dc48ad`.
These final receipt notes were recorded afterward. Three review layers completed;
three handoff clarifications were applied, two findings were rejected with evidence,
and one earlier dependency-composition verification gap was deferred. No live
checkpoint, acceptance, A41 transition or publication is claimed.

### 2026-10-07 runtime qualification prerequisite handoff

Reverified the runtime-versus-producer profile mismatch against source HEAD
`95dd8758f2f36d262a2b5c23269a0d73a2d7825b`. The canonical C0-C6 states remain
unchanged. The current interchange proposal and separately scoped P7 runtime
migration are explicit prerequisites, not executable authenticated acceptance.

Read-only source comparison, run from the repository root:

```bash
env PYTHONDONTWRITEBYTECODE=1 python3 - <<'PY'
import re, sys
from pathlib import Path
sys.path.insert(0, str(Path.cwd() / 'tools'))
import verify_access_telemetry_lifecycle as v
v.validate_current_profile_inputs(Path.cwd())
assert v.canonical_pg_onprem_2_profile().manifest()['profile_sha256'] == '7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe'
assert v.STORY_27_4_WORKLOAD_SHA256 == '71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f'
text = Path('src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs').read_text()
runtime = re.search(r'ApprovedProfileSha256 = "([0-9a-f]{64})"', text).group(1)
print('Current profile, sixteen bound inputs and workload hashes match.')
print('Runtime profile:', runtime)
print('Producer profile:', v.STORY_27_4_PROFILE_SHA256)
print('Runtime matches producer:', runtime == v.STORY_27_4_PROFILE_SHA256)
PY
```

Exit 0; current profile, sixteen bound inputs and workload hashes matched. Runtime
and producer hashes were the full PG1 and PG2 values above, respectively;
`Runtime matches producer: False`. This comparison confirms a blocker, not a
successful qualification.

The unchanged canonical ten-command offline block ran before these documentation
edits, from the clean full HEAD above. Receipt
`/tmp/story-27-4-offline.DDRI57kg`: all ten commands and the block exited 0;
79 lifecycle cases passed in 36.053s, zero failures/errors/skips; Debug/source-reference
build succeeded with zero warnings/errors; exactly 12 retention-decision and 5 A41
architecture guards passed, every result `Pass`, zero failures/errors/skips/not-run.
XML run: 2026-10-07 10:14:17 UTC; XML SHA-256
`6707e37848c88a0a4346220da5ae481eea6dec6e4c17aaa35ef1ecd1e656d1bd`.
The receipt retains nonrecursive dependency revisions, command exits/logs and built
assembly identity; its source status and tracked diff were empty.

Durable identifiers from that pre-edit receipt:

| Artifact | SHA-256 |
| :--- | :--- |
| Built Server test assembly | `da26b4aef31549254c70cefb12608fac23a572797f4a0e57ff6184252ab82562` |
| `lifecycle.stdout.log` | `9ce7c22d0cd1bd995512adf2a81a8a30f539a888ab081ad57da18095e6698179` |
| `lifecycle.stderr.log` | `1d8002977625c076ebff9ba7eb45e44d7a71d6cca74af497a096bab3d362ee79` |
| `build.stdout.log` | `7a6c4df171722048b8359899d11924b674fb8e750b466fba61ca97bc91ec3ff8` |
| `build.stderr.log` (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `dependency-revisions.stdout.log` | `ed4d7c9f62b89011aff302c0039f448f62ed6d20fbbacb756cf663850c9f3cd1` |

The recorded nonrecursive dependency revisions were:

| Root-declared dependency | Full revision |
| :--- | :--- |
| Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| Hexalith.Builds | `397c94a4e246c90b21cf408790fa0d55bf32d795` |
| Hexalith.Commons | `116d26815eb81e35b3c161e1799e5ee12805fc0a` |
| Hexalith.EventStore | `4e4ee8587ee1f6e73f704cd548734e963b2af3e9` |
| Hexalith.FrontComposer | `c561b3210f15206a90c39c82c58f2e5b1005cd60` |
| Hexalith.McpCli | `e159f82b7528797fc245045625ff387d65294ba9` |
| Hexalith.Platform | `eb16638491809df631723d60ed9884e1e2acf040` |
| Hexalith.PolymorphicSerializations | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |
| Hexalith.Tenants | `811447342e8f44b644a2074565e83f45519528fd` |

The existing gate-mechanics lane also passed:

```bash
env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -method 'Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryQualificationWorkloadTests.Gate_*' -parallelMode none -noLogo -failSkips -result-xml /tmp/story-27-4-runtime.Lp1vjnt4/runtime-gate.xml
```

Receipt `/tmp/story-27-4-runtime.Lp1vjnt4`, exit 0: exactly
`Gate_AcceptsOnlyCurrentExactProfileInQualification` and
`Gate_AcceptsAConfigMapProjectionSymlinkOnlyInsideItsMount` passed; zero
failures/errors/skips/not-run. XML SHA-256
`307f1ec86ee9c2acf5da6bb12c61bf032b60a2c6551e71c6fcd9fe811dbebc14`.
These tests cover existing gate mechanics using the runtime constant; they do not
resolve the mismatch or supply current-profile compatibility evidence.
The XML records 2026-10-07 10:16:11 UTC. This earlier receipt did not capture its
own execution-time source revision or assembly hash and therefore does not
independently establish which build it executed. The reviewed rerun below records
those identities rather than retroactively assigning them to this receipt.

The source-bound rerun at 2026-10-07 10:21:46 UTC passed the same exact two tests,
zero failures/errors/skips/not-run, all eight logged commands and the block exit 0.
Receipt `/tmp/story-27-4-runtime-bound.6lk0pkyl` records command arguments,
exit codes and stdout/stderr, nonrecursive dependency revisions, execution-time
status/diff, and matching source HEAD and assembly hashes before and after the run.
Source HEAD: `95dd8758f2f36d262a2b5c23269a0d73a2d7825b`; assembly SHA-256
`da26b4aef31549254c70cefb12608fac23a572797f4a0e57ff6184252ab82562`;
XML SHA-256 `088301d9f4015d4950fb335312533b3b0cab0d255f0bae4d7366c002b018cd1d`;
execution-time tracked diff SHA-256
`058dc0aeb8eade5a08660bfa60e86dc5a11a4048a752a70513e64b2a5fd2ddfb`.
That diff contains the then-current handoff edits; the untracked supporting spec
is listed by the retained source status. Later receipt/review notes are recorded
after the rerun. These are local offline identities, not external custody or live
qualification evidence.

No target was queried, no qualification/fault/purge operation ran and no acceptance,
registration or publication was inferred. Story 27.4 remains `in-progress`, A41 and
its sprint action remain open, and Production lifecycle writes remain disabled.

Final review-patch verification at 2026-10-07 10:23:44 UTC: receipt
`/tmp/story-27-4-handoff-final.u1japj0y`, six logged commands and the script checks
exited 0; exactly 12 retention-decision + 5 A41 architecture cases passed, zero
failures/errors/skips/not-run. XML SHA-256
`c847f2257ffbc67c83c5f34d42fb910f372ec3245d57eaf8e1dd45e96097e1c6`;
execution-time tracked diff SHA-256
`31abf301c4417c8850e517463af8fe986327381d881fb0753f3d1c2c92ac0265`.
The source HEAD and assembly identity match those above. Link resolution, retained
log/dependency identifiers, protected-file byte comparison, two-file mutation scope,
CRLF and whitespace checks passed. The one-shot blind review's four findings were
corrected; no finding was deferred. These final receipt notes were recorded after
that check and provide no live gate, authority or closure credit.

### 2026-10-07 runtime PG2 source/test correction

This separately scoped runtime prerequisite was verified against full baseline
`f98e3f82b4b3f8ed804724811248556c2176139a` with the gate source and tests modified
in the worktree. Local receipt `/tmp/pg2-runtime-gate-4vo_ukjf` retains command
arguments, exits, logs, result XML, dependency revisions and build/source hashes.
The source change replaces only the runtime profile pin and its XML summary.
All valid gate fixtures use independently published PG2 bytes, including runner,
endpoint and ConfigMap projection coverage.

The corrected Debug/source-reference test build passed with zero warnings/errors.
Before changing the runtime pin, direct `Gate_*` execution discovered eight cases:
four failed, including independent PG2 acceptance and historical PG1 rejection;
four passed, zero errors/skips/not-run. This proves the independent assertions
catch the old pin. After the pin change, all eighteen workload-class cases passed,
including `Gate_RejectsHistoricalOrInexactProfile`,
`Gate_RejectsPg2OutsideQualification`, and
`Gate_RevalidatesPg2ExpiryRenewalAndRevocationWithoutCaching`. The latter checks
exact expiry, rejection of PG1 renewal, a valid PG2 renewal at the 15-minute plus
five-second bound, refusal one millisecond beyond it, disabled-state revocation,
re-enablement and file removal on the same gate instance. The twelve retention
decision and five A41 architecture guards also passed, with zero failures, errors,
skips or not-run tests. An independent canonical-profile comparison passed for the
runtime, producer and exact PG2 profile hash, all sixteen bound inputs and the
unchanged historical PG1 canonical hash.

Commands from the repository root:

```bash
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true
env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle.AccessTelemetryQualificationWorkloadTests -parallelMode none -noLogo -failSkips -result-xml /tmp/pg2-runtime-gate-4vo_ukjf/green-workload.xml
env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo -failSkips -result-xml /tmp/pg2-runtime-gate-4vo_ukjf/architecture.xml
```

Identifiers for the initial passing run, before review coverage patches:

| Artifact | SHA-256 |
| :--- | :--- |
| Built Server test assembly | `576311a00f3b155db8214aa7fbab6ea5d70f159aaf9f72982fda55c18f9e85e2` |
| Runtime gate source bytes | `97b4c83000d4bb81ab8a33e1f46cc64a31ef16085f1527798693ea1a2ae9a8dd` |
| Workload test source bytes | `d8e22d95d178684184752d8e52b29b8af0fa6c8bdd1c7d13830c88f18086a8c5` |
| `green-workload.xml` | `ae6e2d2a770ba670ca0627f69dd03c4e7cc60fde70fe6b547da3c6ed5877457c` |
| `architecture.xml` | `858a7cef33ac2bddc2461194d2359b72038a1669ac66b4caaba622c5d49c8fbc` |

This receipt covers repository behavior, not live approval or qualification.
Documentation was recorded after these test runs; final documentation verification
and review are recorded in the supporting spec. The canonical C0-C6 states,
Production disabled state, open A41/action, sprint tracker and historical receipts
remain unchanged. No qualification target, fault or purge was executed. Architecture
and Operations runtime migration approval remains pending with the other P7 and
live prerequisites; this correction does not complete Story 27.4 or close A41.

Review added mounted-projection renewal through `gate.json -> ..data/gate.json`
on one gate instance, including expiry, independent PG2 renewal, historical PG1
rejection, disabled-state revocation and the original outside-mount refusal.
The managed fixture switches directory symlinks synchronously between reads;
it does not claim to test Kubernetes atomic publication. Runner/endpoint fixtures
now share their existing fake clock with the gate. The runner refuses a previously
successful cached segment after expiry without emitting another record; the
endpoint refuses an authenticated cached replay after disabled-state revocation
with HTTP 503, a bounded reason and unchanged accounting.

After these review patches the Debug/source-reference build passed again with
zero warnings/errors, and all eighteen workload cases passed with zero failures,
errors, skips or not-run tests. The final workload command uses the same selector
and arguments above with `-result-xml /tmp/pg2-runtime-gate-4vo_ukjf/final-workload.xml`.
The profile comparison below also ran and exited zero; its exact script, command,
exit and stdout/stderr are retained as `profile-comparison.*` in the local receipt.

```bash
env PYTHONDONTWRITEBYTECODE=1 python3 - <<'PY'
import re
import sys
from pathlib import Path
sys.path.insert(0, str(Path.cwd() / "tools"))
import verify_access_telemetry_lifecycle as v
expected_pg2 = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"
expected_pg1 = "dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14"
v.validate_current_profile_inputs(Path.cwd())
assert v.canonical_pg_onprem_2_profile().manifest()["profile_sha256"] == expected_pg2
assert v.canonical_pg_onprem_profile().manifest()["profile_sha256"] == expected_pg1
assert v.STORY_27_4_PROFILE_SHA256 == expected_pg2
source = Path("src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs").read_text()
runtime = re.search(r'ApprovedProfileSha256 = "([0-9a-f]{64})"', source).group(1)
assert runtime == expected_pg2
assert runtime != expected_pg1
print("Runtime, producer and canonical PG2 identities and sixteen bound inputs match; historical PG1 is unchanged and distinct.")
PY
```

| Review-patched artifact | SHA-256 |
| :--- | :--- |
| `tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` | `124b6e459789c375d178833902d5a31d121fce2bc28e97f02a7b7d8b8be46665` |
| `tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.dll` | `abd56e9ef0bdb506afdf6796afde41fddd60349d49003a3352a352df9b17803c` |
| `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationWorkloadTests.cs` | `7dc059d635811abce6c9fa0a2cc43dbef6bbe772f6d353cabcfa779f9321a321` |
| `final-workload.xml` | `bd0ed6982270247337ebbc95dff9ed9ef90cad02f31cb842b7db002bdf066452` |

Final documentation checks and review triage are recorded in the supporting spec.


### 2026-10-08 current readiness handoff reconciliation

The current C1 handoff now separates completed I1, narrow authenticated GitHub
observations and source-receipt correction from incomplete I2–I6/P1–P7 and live
acceptance. The named-owner C1 bundle exception requires authenticated separate
Operations/Security receipts and producer exclusion. It changes neither the
legacy label-only predecessor refusal nor the separate C5/C6 reviewer rule.
The compiled epic context includes the current tenant-principal/erasure constraints.
The parent `-7` draft and its frozen intent remain unchanged.

At source `dbe4ce0a97c6a3a883af800f7873a99b79434d44`, the unchanged ten-command
canonical block passed: `/tmp/story-27-4-offline.FKA9f5hE`; all commands and block
exit 0, 80 lifecycle cases in 31.490s, exactly 12 retention + 5 A41 guards all
Pass with zero failures/errors/skips/not-run, and Debug/source-reference build
with zero warnings/errors. Execution-time tracked diff SHA-256:
`c148449fc8b8615dba96c7d4d4f775808398e2aba2d3d621d5a4947f83f39a33`; result XML SHA-256: `ce754554a10e606109e5cd9adbcb54550c95a1af4c855cb630ea2e0a83185694`;
assembly SHA-256: `8a9e11c489f78dc745e898077d282c02a6c0d52d9dcdd8d8bec092a863310013`.

Five existing real-TLS authority cases also passed in 1.764s, zero failures,
errors or skips. Exact executed command:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 PYTHONPATH=/home/administrator/projects/hexalith/memories/tests/tooling/access_telemetry_c1_interchange python3 -m unittest -v test_github_authority.GitHubAuthorityTests.test_real_tls_root_policy_session_parent_gate_and_owner_bundle_chain test_github_authority.GitHubAuthorityTests.test_verified_grants_drive_distinct_bundle_reviewers test_github_authority.GitHubAuthorityTests.test_reviewer_producer_overlap_including_root_session_and_owner_refuses test_github_authority.GitHubAuthorityTests.test_non_owner_same_bundle_reviewer_refuses_even_with_two_verified_roles test_github_authority.GitHubAuthorityTests.test_cross_target_tenant_session_profile_workload_and_source_denials_before_dependents
```

`test_reviewer_producer_overlap_including_root_session_and_owner_refuses`,
`test_non_owner_same_bundle_reviewer_refuses_even_with_two_verified_roles`, and
`test_cross_target_tenant_session_profile_workload_and_source_denials_before_dependents`
attach negative evidence to the current reviewer/scope-attribution claims. The
canonical lifecycle lane also passed
`test_c1_owner_exception_does_not_authorize_label_only_legacy_predecessor`.
Logs/commands/exits and 1,818 baseline file hashes:
`/tmp/story-27-4-readiness-92sb2tz2`. All existing user changes, source/dependency
revisions, matrix states, sprint/history/Production bytes and historical receipts
were preserved; current links and CRLF passed. These are local offline fixtures,
not live grants or custody. Dated receipt notes were added after execution.
Live gates, Story 27.4, A41 close-out and Production activation remain pending.


Read-only review follow-up confirmed Platform publication: the freshly observed
`origin/main` is `2c4f788a6ffde2646de1686492dc817f5505c922`; it contains the
authenticated-decision commit `48d5c6e64087bb33232651d8b59422e95185c699`,
transport commit `794e8c63fe945bf689064edb8a406801810cb4cf`, root-recorded pin
`495d1d0dfceb33e6bb628809a42a284526707ff6` and the preserved user checkout
`94c0359ffd4da1c186df7ad4218b20c0eae99a89`. The root-recorded pin already
contains both exact executed authority/transport files. The older prerequisite
publication wording does not establish a current missing handoff; provenance is
recorded in `platform-publication-check.json` in the readiness receipt. No
fetch, gitlink change, publication action or operational acceptance occurred.
Review corrections clarified the legacy structural-label limitation and added
AD-17 token stability/collision safeguards to the compiled context. All executable
sources remained identical; the historical receipt prefix and matrix states were
preserved. The next separate implementation dependency is I2 closed
registry/source binding, with genuine registration/command/role receipts still
required; this supporting slice supplies none of those approvals.

### 2026-10-08 Story 27.4 draft readiness recheck

The unchanged ten-command offline block passed at source revision
`906bc07ad6a8e4912a7222d9d097da434148266a`. Receipt `/tmp/story-27-4-offline.fq11FV9r`
retains all commands, exit codes, source/worktree and dependency identities,
logs, assembly hash and XML. All ten commands and the block exited `0`:
80 lifecycle tests, exactly 12 retention-decision plus 5 A41 guards, every XML
result `Pass`, zero failures/errors/skips/not-run; Debug/source-reference build
had zero warnings and errors.

| Receipt item | SHA-256 |
| :--- | :--- |
| Execution-time diff | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Built assembly (build identity only) | `7f4c851d0c4306937e8b4302be53bc36e8e6ec9aea033b5390e4e46e3ae28d89` |
| Architecture XML | `26c934d0544125bd72ea843207b4ca664bb8d348351600f8d0c657f671de0698` |
| Lifecycle stderr | `31bb90df5b743e447f862fddc67731880c9ccad17fad2bc63b20b23e5417a01f` |

`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`
exited `0`: 167 passes, zero failures/errors/skips. Logs and protected-file
baselines: `/tmp/story-27-4-current-readiness-3gv1qqwq` (local offline receipt).
The retained named negatives include
`test_c1_owner_exception_does_not_authorize_label_only_legacy_predecessor`,
`test_producer_refuses_unverifiable_target_identity_without_mutating_target`,
`test_producer_restores_disabled_state_when_body_fails_after_enable`, and
`test_business_and_privacy_canaries_use_curl_config_and_tenant_routes`;
all passed. They test refusal, cleanup and tenant/privacy boundaries using
fixtures; they confer no target authority or live gate acceptance.

Investigation found no executable remaining live task within the current draft.
I2 closed registry/source-binding preparation can be proposed separately with
isolated fixture contracts and no deployed accepting entries; the current
27.4 intent excludes implementing separate prerequisites. Semantic admission,
assembly, consumer migration and genuine accepted evidence/execution inputs
remain outstanding. Runtime PG2 correction is already complete offline.
No checkpoint state, A41/action status, Production deployment, sprint or
historical Epic 20/Story 20.5 byte changed; no target was contacted.

### 2026-10-09 current-HEAD readiness recheck

The owner session scope is recheck only: rerun the offline block, refresh this
readiness record, and keep Story 27.4 held. Current HEAD
`5d0cbefedc828ab829b0b9104c5ab77788dcd5b2` contains the investigation commit
`d172165ebe1ab39efebd38b875b94f6d4ef6d191`. No live target was contacted. C0–C6
matrix states, close-out prerequisites, A41, the sprint action, Production
lifecycle writes, and Epic 20/Story 20.5 bytes stay as recorded above.

Offline I2 registry/source inspection and C1.15 declared-observation pin
inspection are complete as isolated fixture contracts. Deployed lookup still
refuses, and neither slice registers an accepting entry or accepts a gate.
Closed I2 registration and provenance, I3–I6, and P1–P7 remain held. Accepted
PG-ONPREM-2 C1.15 renewal, twenty-three registered/done gate owners, an accepted
25-gate predecessor, authenticated Operations/Security decisions, and scoped
target/custody/credential/fault/purge grants remain absent. Live C0/C2–C4,
independent C5/C6, terminal validation, the exact four-path close-out, and
remote containment therefore stay pending.

The unchanged canonical ten-command block passed before these dated notes were
appended. Receipt `/tmp/story-27-4-offline.RJE0jKex`; all ten commands and the
block exited `0`. Lifecycle: 80 tests in 29.041s, bare `OK`, zero failures,
errors, or skips. Debug/source-reference build: zero warnings and zero errors.
Architecture XML run `2026-10-08T23:47:07Z`: exactly 12 retention-decision and
5 A41 guards, every result `Pass`, zero failures, errors, skips, or not-run
tests. `git diff --check` passed.

| Receipt item | SHA-256 |
| :--- | :--- |
| Execution-time diff | `16099db9e36a15fc5967b15a0f3cb8533b2b08d011935dd70c2d38146190e376` |
| Built assembly (build identity only) | `c183e5f3f6a8521e2cac5d1d3597769973b78d61838bcd4ca69433ae61c2f1b4` |
| Architecture XML | `dba7c7bc64fecffa20255976e475ee013d29264fc0c2ddb1d3e10d0286637299` |
| Lifecycle stderr | `a63616603754e00c6330d25ebc840b073c0c99b31fb14f7d783bec80e650f56d` |
| Build stdout | `9b8f6db3715cf60a0b7b859dcf34e27400bddf5783776b94d5d28d173a8ed183` |
| Dependency revisions | `fad42ddc1834631026c88e64caaf732c71f18e580c4b056249a5923bf45c920d` |

The execution-time worktree change was only this story's spec status field,
`ready-for-dev` to `in-progress`. Nonrecursive dependency revisions were
Hexalith.AI.Tools `3f194e17174994d308ec84af9ee2b5aa68674d0d`, Hexalith.Builds
`fef031806321793c9effb17235c2465118984432`, Hexalith.Commons
`b247ed116c6523f8c596ec0a933eff8973d11568`, Hexalith.EventStore
`07d1e23a6c5b06bbbb1fc8ddb5174cc3382d3d93`, Hexalith.FrontComposer
`0e114214007c22f5cdbac21a6853cff4208340ee`, Hexalith.McpCli
`1b1012d099b18067e8b915c6bf00b48ac6ce458d`, Hexalith.Platform
`f6f95cdf5a63e264ada564c4cbb5a5b40b0d5a9a`, Hexalith.PolymorphicSerializations
`98de6e013840ece9f0fa7c68ab7dcdf2bba3b375`, and Hexalith.Tenants
`10c9f6f66632f6861fe13fb9f41ab05b90c37f67`.

`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`
exited `0`: 219 tests in 48.603s, bare `OK`, zero failures, errors, or skips.
Receipt `/tmp/story-27-4-interchange.48qhOxQj`; stderr SHA-256
`a58b70d0cf17fe6ce31308076c776ebe41d48d601e712184d104ebcff8b43451`.

The lifecycle log again records `ok` for
`test_c1_owner_exception_does_not_authorize_label_only_legacy_predecessor`,
`test_producer_refuses_unverifiable_target_identity_without_mutating_target`,
`test_producer_restores_disabled_state_when_body_fails_after_enable`,
`test_business_and_privacy_canaries_use_curl_config_and_tenant_routes`,
`test_current_target_predecessor_approvals_and_c0_reject_old_or_mixed_evidence`,
and `test_qualification_renewal_rejects_lost_lease_ownership`. These fixture
results prove offline refusal, drift, cleanup, and tenant-boundary behavior.
They confer no live gate, custody, or publication credit.

Protected bytes read before this readiness text was appended, and left unchanged
by it: `sprint-status.yaml`
`0e288f08029843870db90402c0cc3d3a1ea5b661dc3c11282729e397a7fcc05a`;
`20-5-inbound-rate-limiting-quotas-and-audit-completeness.md`
`5bfdb89ef34f8cc8f113df5a0638865bf246e49c084f1c608159e1ea71d820dd`;
`deferred-work.md`
`8acedc1f0563b34baf2409e94fad13688d4e0cdfd7c38b2dcc7c864d82f9d48f`;
`deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml`
`0c2b4b836d14be457ab8ccc79a534cd9997193f9c7b10f3d11b8b100a8b40ab2`;
`_bmad-output/project-context.md`
`0b62f5ad02d34cb4ad5ddacb5ad0693d0251874f5733fbb4de87c7593636e665`;
`docs/dev/telemetry.md`
`2f7a252dd2c9d229058b929b90e2dc3b6e7ee338cec28d9f4eb494dbe7846b2a`.
The Production patch hash matches the lifecycle verifier's pinned disabled
overlay. Story 27.4 remains incomplete, A41 and its sprint action remain open,
and Production lifecycle writes remain disabled.

### 2026-10-09 follow-up recheck at `1ead5a1b`

The approved session remains `RECHECK_ONLY`. Source HEAD was
`1ead5a1b2432e24211b18060fe1d2da6126b31b3`, with a clean execution-time
worktree. The unchanged canonical ten-command block above exited `0`; every
logged command exited `0`. Receipt: `/tmp/story-27-4-offline.XLdeHWCY`.
The separately logged authenticated-interchange command exited `0`; receipt:
`/tmp/story-27-4-interchange.yDRybdwi`. These local receipts are offline
verification output and confer no operational or custody authority.

| Check | Observed result |
| :---- | :-------------- |
| Lifecycle tooling | 80 tests in 38.205s; bare `OK`, zero failures, errors or skips. Refusal, drift, cleanup and tenant-negative fixtures remain passing. |
| Debug/source-reference Server test build | Succeeded with zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision plus 5 A41 guards, all `Pass`; zero failures, errors, skips or not-run cases. |
| Authenticated interchange | 219 tests in 69.050s; bare `OK`, zero failures, errors or skips. |
| Whitespace | `git diff --check` exited `0`. |

| Receipt item | SHA-256 |
| :----------- | :------ |
| Execution-time tracked diff (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Nonrecursive dependency revisions | `299a9c30c2c462b0b5e5e8f4d2be34169668c10f0034bda198a5d5b623afd8c1` |
| Built assembly (build identity only) | `0fadef08ae1184f2ea006eead3cbcee5614538343f66f8c9ff14544e8ce4194f` |
| Architecture XML | `39a97b4dd5f25cb88ce74b0c348cc90b90559343d4bfdc0a13262cd0f50edd80` |
| Lifecycle stderr | `28b43af8172f975fe491fdc138f3fd39afff96220440a6032f2c5e2a7d4978f4` |
| Build stdout | `741edeb7005778fceb9b56e50fda6483d41dad94062ef216dc135f18179d3f82` |
| Interchange stderr | `2c0cb85603c0becd533d20c4635cfc4b9f5e44f93474ab7370a222fd77679b71` |

The protected sprint, Story 20.5, deferred-work, Production-disabled overlay,
project-context and telemetry-document hashes match the prior 2026-10-09
readiness record exactly. The root-recorded nonrecursive dependencies include
Hexalith.McpCli `ecf8952513eaf20e09a8ecb4f6fcc56dc0e829df`; the receipt
retains every revision. No source, test, target, sprint, history, protected or
Production byte changed during this recheck.

Closed I2 registration/provenance, I3-I6, P1-P7, accepted PG-ONPREM-2 C1.15
renewal, twenty-three registered/done gate owners, an accepted eligible
25-gate predecessor, authenticated Operations/Security decisions and scoped
target/custody/credential/fault/purge grants remain absent. Live C0/C2-C4,
independent C5/C6, terminal validation, exact four-path A41 close-out and
remote containment remain pending. Story 27.4 remains incomplete, A41 and its
sprint action remain open, and Production lifecycle writes remain disabled.

### 2026-10-09 recheck at `1cb117c5`

The approved Story 27.4 session remains `RECHECK_ONLY`. Source HEAD was
`1cb117c517e2644e239a22a82c1a9a9c876a21cb`, with a clean execution-time
worktree and an empty tracked diff. The unchanged canonical ten-command block
exited `0`; every logged command exited `0`. Receipt:
`/tmp/story-27-4-offline.uLtgJrsf`. The separately logged authenticated
interchange lane exited `0`; receipt:
`/tmp/story-27-4-interchange.dGuoGMS2`. These local receipts are offline
verification output only.

| Check | Observed result |
| :---- | :-------------- |
| Lifecycle tooling | 80 tests in 29.061s; bare `OK`, zero failures, errors or skips. Refusal, drift, cleanup and tenant-negative fixtures passed. |
| Debug/source-reference Server test build | Succeeded with zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision plus 5 A41 guards, all `Pass`; zero failures, errors, skips or not-run cases. |
| Authenticated interchange | 242 tests in 55.410s; bare `OK`, zero failures, errors or skips, including offline I2 provenance and I3 authority-boundary tests. |
| Whitespace | `git diff --check` exited `0`. |

| Receipt item | SHA-256 |
| :----------- | :------ |
| Execution-time tracked diff (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Nonrecursive dependency revisions | `d9be8ddf152d03571503787ee235b30aa0ab911fb60608e43d379592a66bcd2d` |
| Built assembly (build identity only) | `037b305d123eadea7901b2ebc234389c66ea29952403ad1a74dd1c456395c497` |
| Architecture XML | `a673502d4219717c0fdbcf95f6e19e73f273b9ce877430e031759a18be8c446b` |
| Lifecycle stderr | `63ddde3eab12852a27da05e1c5b45f9d85134b070a5cfb06f9041679ea357f10` |
| Build stdout | `eb25a81ab0d2fceced8b85051e4bf3c413b4f5f5f6f4094ee4b3706e8117468d` |
| Interchange stderr | `7af4974b62a9925541e6628865ef6c3bfe72f2d85453b8174822a45c2e22483d` |

Root-declared nonrecursive dependencies changed since the previous receipt;
its revision log records the exact current set. The protected sprint,
Story 20.5, deferred-work, Production-disabled overlay, project-context and
telemetry-document SHA-256 values still match the preceding 2026-10-09 record.
The Production patch remains at the lifecycle verifier's pinned disabled hash.
No source, test, target, sprint, history, protected or Production byte changed
during this recheck.

The [P1–P4 owner decision packet](../../specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md)
now records owner `APPROVE` dispositions and selected offline policy values.
It explicitly denies operational bootstrap, non-GitHub receipt issuer,
authenticated target/session grant, custody root and gate-reviewer grants; its
policy is not operationally effective. The offline I3 boundary always refuses
acceptance. Closed I2 registration/provenance, I3–I6 operational authority and
semantics, P5–P7, accepted PG-ONPREM-2 C1.15 renewal, twenty-three
registered/done gate owners, an eligible accepted 25-gate predecessor,
authenticated Operations/Security bundle decisions and scoped
target/custody/credential/fault/purge grants remain absent. Live C0/C2–C4,
independent C5/C6, terminal validation, exact four-path A41 close-out and
remote containment remain pending. Story 27.4 remains incomplete, A41 and its
sprint action remain open, and Production lifecycle writes remain disabled.

### 2026-10-09 current-revision offline recheck at `d5feac61`

The approved Story 27.4 scope remains `RECHECK_ONLY`. The canonical ten-command
block above ran against root HEAD
`d5feac61bb6fe90f13b338ab7c10a9220b6d7602` and exited `0`; every logged
command exited `0`. Its execution-time tracked diff was empty. Source status
recorded only the new, untracked one-shot spec for this recheck. The separate
authenticated-interchange command also exited `0`. Local receipts:
`/tmp/story-27-4-offline.NHHGW9aS` and
`/tmp/story-27-4-interchange.RurXxP9g`. They are offline logs, not target or
custody evidence.

The separate interchange invocation was
`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`.

| Check | Observed result |
| :---- | :-------------- |
| Lifecycle tooling | 80 tests in 49.971s; bare `OK`, zero failures, errors or skips. |
| Debug/source-reference Server test build | Succeeded with zero warnings and zero errors. |
| Exact architecture selectors | 12 retention-decision plus 5 A41 guards, all `Pass`; zero failures, errors, skips or not-run cases. |
| Authenticated interchange | 242 tests in 73.871s; bare `OK`, zero failures, errors or skips. |
| Whitespace | Canonical `git diff --check` exited `0` before the evidence edit. |

| Receipt item | SHA-256 |
| :----------- | :------ |
| Execution-time tracked diff (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Nonrecursive dependency revisions | `38e19c88fc2694dd8dc6432bd88adc3b82f459a0ffc443eeeb52e6abf859c3ea` |
| Built assembly (build identity only) | `dfa0b03dea7a1a43544fa55d3da1b5e2d3fe2817ed5bc34121cf2c90cb2eb326` |
| Architecture XML | `33f6d812b80fb92d321ec4c827df5242e39f464e1205c5e509fbae83d8df454a` |
| Lifecycle stderr | `8555f3c09440020a5b97719e1e90288ae1544a02ccd6a56e7833e051942b34fc` |
| Build stdout | `1d53666167779357d021b1d94f353b1411d874fec12e422c4d5b86acabd83c5b` |
| Interchange stderr | `88684acf7c0389dcac98255a196a8d21da6a8055762f9f4967eab84f013b97a7` |

The receipt records all nine root-declared, nonrecursive dependency revisions:

| Dependency | Revision |
| :--------- | :------- |
| Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| Hexalith.Builds | `468fdbba04e2d9a27d251298875125b57fa6d836` |
| Hexalith.Commons | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| Hexalith.EventStore | `5e5d2305d870a03d6d63f52470ce9f7a1ec058da` |
| Hexalith.FrontComposer | `0e114214007c22f5cdbac21a6853cff4208340ee` |
| Hexalith.McpCli | `bfb2ae376bfe0243e6283d24a6cad44315c48208` |
| Hexalith.Platform | `c489268fb347e7a65eedc6f2a4a40ab3ed4ce118` |
| Hexalith.PolymorphicSerializations | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |
| Hexalith.Tenants | `96cc6f115865f9e2307aedc3748e60e0f99edeae` |

The protected sprint, Story 20.5, Production-disabled overlay, project-context
and telemetry-document hashes still match the earlier 2026-10-09 record.
`deferred-work.md` now hashes to
`5aac0ee356fca0bb91f8271ccbfc0d27b5a0aa97e77c614f5132eadfc670bfad`
after the separately committed P1 receipt hardening; this recheck did not edit it.

Operational P1-P4 roots and grants, closed I2 registration/provenance, I3-I6,
P5-P7, accepted PG2 C1.15 renewal, twenty-three registered/done gate owners,
an eligible accepted 25-gate C1 predecessor, authenticated bundle decisions,
and scoped target/custody/credential/fault/purge grants remain absent. The
offline I3 boundary still refuses acceptance. No live target was contacted;
C0-C6 states, Production writes, Story 27.4, A41 and its sprint action remain
unchanged.

After the evidence edit and review corrections, `git diff --check` exited `0`.
The two exact architecture selectors were rerun from the built Debug assembly:
17 passed with zero errors, failures, skips or not-run cases. These are
post-edit repository checks only.
