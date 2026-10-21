# Story 27.4 — current retention results

The [active contract](../spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) implements the owner-approved simplification of 2026-10-21. Obsolete preparation history has been removed at the owner's request. The [full campaign checklist](../../../docs/operations/access-telemetry-full-qualification.md) preserves outstanding requirements.

## Functional result

**Pending real run.** The latest historical preparation passed seven offline operations, including three owning Builds tests, then stopped when an unrelated Platform planning file appeared. The new contract removes that documentary hold. No live retention pass or new resource disposal is claimed.

| Preparation check on 2026-10-21 | Result |
| :--- | :--- |
| From Platform: `dotnet tool run hexalith-module --version` | Exit 0; `4.35.1+f8599224457dda99e7c0ae4870147dd51b464feb`, matching Platform's local pin and accepted range. |
| From Platform: `dotnet tool run hexalith-module -- test --help` | Exit 0; existing `test --manifest --profile --mode --evidence` command available. |
| `python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_selected*.py' -v` | Exit 0; 11 passed in 116.390s. Covers the active sequence, newer-cohort loss/mutation, TTL/clock failures, withdrawal and handoff refusals using offline doubles. |
| Existing Debug document guards: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -noLogo` | Exit 0; 5 passed, zero errors/failures/skips/not-run. Reads the changed documents; no application rebuild claimed. |
| `python3 -B tools/check-story-slice-scope.py --story-key 27-4-retention-verification-operations-runbook-and-a41-close-out-14 --require-record --repo-root .` | Exit 0; current Spec 14 passes.  |
| Link/status checks; `git diff --check` | Current links and unchanged sprint data checked after obsolete-history removal; whitespace check passes. |

Next: freeze the relevant build/runtime inputs, verify the supported model, and prepare the one bounded operational plan in the active contract. The installed version is supported; compatibility of the complete runtime has not yet been demonstrated. Do not substitute the retained diagnostic 4.31.2 package or regenerate a whole-workspace approval digest.

For the actual run, replace this pending paragraph with the run ID and input identities, exact command and times, A/B observations, native result, log paths and actual Platform disposition. Use the existing runner output; do not create another human evidence report or receipt-verification hierarchy.

## Retained resources

Documentation removal does not dispose of resources. Prior failed runs `f482f16095f3027867cec221b0a35220` and `b399a0389f7a8383af4a30339761ade2` remain recorded as `retained`, `unverified: true`, `isEmpty: false`, with cleanup unauthorized. These are last-recorded dispositions, not a fresh census. Source: private `current-held-functional-retention-invocation.json` under `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261021-current-5clz3zwi` (`retainedFailedRuns`).

The [bootstrap recovery result](../27-4-bootstrap-recovery-result-2026-10-16.md) records retained 40/600-GiB disks and a stopped QMP socket. Private raw receipts and these resources remain untouched. Before any new run, Platform must establish isolation from retained resources; old attempts/permissions cannot be replayed.

## Evidence posture

No target was queried during this simplification. Preparation checks and offline fixtures provide no live retention evidence. Production lifecycle writes remain disabled; `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward/open. Full qualification is separately scheduled and does not gate a local functional result.

The identity and C0-C6 matrix below retain the full campaign's recorded status. Repository-validation labels are historical offline results, not revalidation in this change. The former PG-ONPREM-2 identity is preserved; a full Platform campaign needs its own adopted exact target/profile. Local Debug, development UTC and file-backed secrets earn only disclosed functional evidence.

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
