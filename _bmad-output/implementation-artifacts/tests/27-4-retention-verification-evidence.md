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
chain passes.

## Immutable decision identity

| Field | Value |
| :---- | :---- |
| Profile ID | `postgresql-v2-dapr-1.18.1-postgresql-18.4-onprem-k8s1-openebs-local-retain-400g-v1` |
| Profile SHA-256 | `dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14` |
| Workload SHA-256 | `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` |
| Production lifecycle writes | `disabled` |
| A41 status | `carried-forward/open` |
| Repository mode | offline; no target query and no A41 mutation |

## Canonical C0-C6 matrix

| Checkpoint | State | Repository validation | Required external evidence | Owner | Blocker / operator action |
| :--------- | :---- | :-------------------- | :------------------------- | :---- | :------------------------ |
| C0 exact adapter profile | `repository-validated` | Exact `PG-ONPREM-1` profile, hash, workload, parser, and pre-query identity contracts have deterministic guards. This is not Production proof. | Immutable exact-profile qualification packet with executed commands and recomputed source/artifact identity. | Platform Operations | `operator-pending`: execute the authorized C0 producer on the declared target. |
| C1 canonical predecessor | `operator-pending` | The verifier requires each canonical `C1.1` through `C1.25` result and rejects a synthetic aggregate. | Twenty-five individually attributable passing gates plus two different named reviewers approving the exact same profile hash. | Gate owners, Platform Operations, Security | Complete all 25 gates; no missing owner, skip, zero-result command, or shared reviewer can pass. |
| C2 production replacement | `operator-pending` | Producer schema, immutable packet writer, same-profile validator, concurrent fixed two-writer accounting, zero-default gate/Lease transition, and exact per-instance replacement selectors are repository-validated. | Controlled two-writer execution; replacement of both Servers and their sidecars, lifecycle/clock services and their sidecars, actor activation, all three Placement and Scheduler members; and adapter-fault execution with exact acknowledgements, recovery, and audit continuity. | Platform Operations | Run the reviewed C2 producer only after C1 passes with a named shared-system approval; its target-identity observation must prove an initially disabled exact-profile qualification namespace, empty Lease, and zero lifecycle/clock replicas. Zero acknowledged loss and a final disabled/empty/zero state are required. |
| C3 retention and reclamation | `operator-pending` | Cohort, 1/24/168-hour bounds, attestation negatives, interrupted-purge, newer-record, tuple-attribution, and logical/physical separation guards are repository-validated. | Executed expiry/purge and adapter reclamation commands bound to each of the three independent cohorts and its database/schema/table; newer records preserved and reusable allocator free-space increase observed within 86,400 seconds. | Lifecycle owner and adapter owner | Run the reviewed C3 producer after C1; an OS disk-shrink claim is prohibited. |
| C4 failure, privacy, and observability | `operator-pending` | Complete failure inventory, health precedence, NoData/last-evidence timestamp, bounded labels, and Story 20.2/24.3 denial guards are repository-validated. | Every declared dependency/fault lane, nonzero business samples with zero business failure, console/configured-OTLP continuity, alerts, and tenant denial before dependency access. | Platform Operations and Security | Run the reviewed C4 producer after C1; missing scenarios, raw/secret aliases, or dependency calls after denial reject. |
| C5 operations acceptance | `operator-pending` | Neutral and PostgreSQL-specific runbook structure, ownership, monitoring, RPO/RTO, rollback, rotation, and decommission contracts are repository-validated. | Named operations acceptance of the exact immutable profile, evidence set, capacity/cost, incident, restore, and maintenance procedures. | Platform Operations reviewer | Review actual C0-C4 packets and record an independent same-hash decision. |
| C6 security acceptance | `operator-pending` | Least-privilege, Dapr-only data plane, TLS/secret, bounded observability, evidence redaction, and tenant-isolation documentation guards are repository-validated. | Named security acceptance of the same profile and immutable evidence hashes, independent of the Platform Operations reviewer. | Security reviewer | Review actual packets and record a different named same-hash decision. |

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

These commands validate implementation structure and deterministic fixtures only:

```bash
PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -parallel none -noLogo
git diff --check
```

The Production producers, close-out preflight/postflight, and publish verifier are
documented in
[Access Telemetry Lifecycle Operations](../../../docs/operations/access-telemetry-lifecycle.md).
