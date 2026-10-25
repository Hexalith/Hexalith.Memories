# Access-telemetry full qualification — pending campaign

This is the current requirements register for the separately scheduled full campaign. The [local functional contract](../../_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) has its own finish line. Functional success cannot discharge these gates or close A41. The [current report](../../_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md) records results.

Adopt one exact full Platform target/profile before scheduling this campaign. Prior KVM/PG captures do not transfer. Each gate needs eligible same-session evidence, its own review and completion state; no additional story-file registration is required. Jérôme Piquot owns the gates with Platform/Builds environment support and Memories assertions. The approved sole-owner policy permits distinct Operations/Security records labelled `sole-owner/self-reviewed` without independent-review claims.

C1.24 precedes C1.23/C1.25. Full C2 retains two writers, 30 minutes and 150,000 backlog records. Full C3 retains real 1/24/168-hour cohorts, 24-hour allocator proof and the 216-hour producer bound. No deadline extension is implicit. Use the existing [lifecycle runbook](access-telemetry-lifecycle.md) and [adapter contract](access-telemetry-adapter-production.md). Production activation/publication remain separate decisions.

## Recommended target and provisional schedule — 2026-10-25

After the local pass, the owner answered **“I dont know, do recommended”** when asked which full Platform target/profile and UTC window to prepare. This selects the following preparation direction. A deployable full profile, capacity reservation and operational launch plan remain prerequisites; no campaign has started.

Use a fresh, single-node **Platform-owned kind qualification environment**, built from Platform's maintained application chart and isolated-cluster ownership. Reserve the proposed cluster name `memories-qualification-27-4-20261026`, explicit context `kind-memories-qualification-27-4-20261026`, and application namespace `hexalith-memories-qualification` in the eventual plan; refuse a pre-existing name rather than attaching or deleting it. This creates a new full-profile identity. The retained KVM target and its disks remain historical resources outside this proposal's ownership.

The starting toolchain is Platform's existing [toolchain.json](../../references/Hexalith.Platform/eng/helm-qualification/toolchain.json): kind **0.31.0**, Kubernetes **1.35.0** at the recorded node-image digest, Helm **4.2.0**, Dapr **1.18.7**, and Aspire **13.6.1**. Preserve the PostgreSQL **18.6** and OpenBao **2.6.4** immutable image identities, two-writer **500 events/s** workload, **400 GiB** retained storage and no-HA boundary from the [former full profile](qualification/pg2-kvm-approved-profile-2026-10-16.json). Bind the new runtime images, chart/values/render, application/reporter images, storage and exact input hashes before adoption. These are preparation inputs, not an adopted or qualified replacement profile.

The current verifier still accepts canonical profile **`95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`**, including KVM and Dapr 1.18.1. Offline `validate_current_profile_inputs(Path.cwd())` passes on 2026-10-25. A kind/Dapr 1.18.7 target must receive a new canonical identity and corresponding semantic checks; it cannot use that hash or WINDOW's local evidence. No dependency, manifest or version pin was changed during this preparation.

### Prerequisites and owning work

Platform's [maintained chart documentation](../../references/Hexalith.Platform/eng/helm-qualification/README.md) and `eng/helm-qualification/qualify.py` provide a tested foundation: unique cluster, private kubeconfig, explicit context, bounded commands and owned cleanup. Their retained 2026-10-22 qualification establishes chart/admission behavior only. The qualifier disables Scheduler, uses synthetic bindings, and provides no real PostgreSQL/OpenBao environment. Running that qualifier alone cannot discharge this campaign.

| Owner | Required change before a full run | Completion evidence |
| :--- | :--- | :--- |
| Platform / Builds | Extend the existing environment lifecycle and descriptor handoff to own this isolated Kubernetes campaign, with finite startup/execution/stopping budgets, persistent run identity, monitoring, exact stop/disposal and failure disposition. Declare the full suites; the current Memories manifest declares only `functional-retention` at 14400 seconds. | Supported command and actual descriptor/loaded-input proof, exercised cancellation/failure/cleanup/isolation checks; no new Memories environment runner. |
| Platform | Supply real PostgreSQL, OpenBao/secret references, authenticated independent UTC, component configuration, working Scheduler/reminders and the full declared Placement/Scheduler fault topology. Build and bind actual application/reporter images and authority inputs. | Readiness and runtime/security/identity evidence over the exact deployed inputs; fresh C1 packets, including C1.15/C1.16. |
| Platform Operations | Reserve actual CPU/memory and Linux-backed storage for the whole interval, including application/control-plane load, PostgreSQL's existing 8 CPU / 16 GiB cap, 400 GiB retained data capacity, measured amplification/reclamation workspace and a separate recoverable backup destination. Replace the chart fixture's 10m/32Mi requests and 1 CPU/512Mi limits with measured full-profile allocations. | Capacity/cost admission and backup/restore proof; a PVC request or free space on another filesystem is insufficient. |
| Memories | The explicit owner-predecessor/v1 authority consumer and durable C3 continuation are implemented and offline verified; see the [runbook contract](access-telemetry-lifecycle.md#owner-authority-and-c3-continuation-consumer--2026-10-25) and [current report](../../_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md#owner-authority-and-continuation-implementation--2026-10-25). The single ordinary review is complete; all four findings are fixed and final focused verification passes. Supply actual outside-approved authority, fresh independent UTC/status and physical target/runtime identity before use. Historical predecessor/v2 still refuses. | Focused temporary-file authority/continuation verification is recorded in the report, including distinct owner roles and unchanged full-mode bounds. These fixtures provide no live gate credit. Actual Platform-supplied authority/time/target inputs and separately adopted kind profile remain required; profile rebinding alone cannot enable C2–C4. |
| Memories | Adopt the exact new canonical profile and source/input bindings in `tools/verify_access_telemetry_lifecycle.py`, the C1 identity/authority consumers and qualification overlays, with mismatch, tenant-denial and fail-closed regressions. Preserve all existing behavioral gates and historical profile/evidence identities. | Reviewed profile manifest, source-bound C0 and all 25 eligible same-session C1 packets before C2–C4. |
| Jérôme Piquot | Bind the concrete plan to the actual reserved target/window, including bounded retries, fault authority, prior-resource exclusions and disposition; later record distinct Operations/Security acceptance. | One operational decision on the complete launch plan; later C5/C6 acceptance over actual evidence under `sole-owner/self-reviewed`. |

A read-only host check found **120,523,268,096 bytes** free on the current Linux workspace filesystem, less than the required **429,496,729,600-byte** data capacity alone. `/mnt/c` had **1,214,942,855,168 bytes** free, but that does not establish a reserved Linux-backed retained volume, durability or backup capacity. No storage was allocated, cluster contacted or resource reserved. Platform must resolve this concrete capacity gap before admission.

### Provisional UTC reservation

Prepare a **336-hour / 14-day** reservation, **2026-10-26T08:00:00Z–2026-11-09T08:00:00Z**. This is a proposal, not a booked window or launch authorization. It contains C3's 216-hour ceiling plus the other sequential producer bounds and explicit setup/acceptance/disposition time. A nine-day reservation alone cannot contain the full campaign. Every phase requires its actual predecessors; unused time grants no retry or budget reset.

| Phase | Proposed UTC interval | Bound / condition |
| :--- | :--- | :--- |
| Provision, capacity/security admission, C0 and C1 | Oct 26 08:00 – Oct 28 08:00 | 48 hours; all prerequisites and 25 fresh C1 passes required before C2. |
| C2 replacement and catch-up | Oct 28 08:00 – Oct 28 22:00 | Existing 50400-second / 14-hour producer ceiling; two writers, 30 minutes and 150000 backlog records unchanged. |
| C3 retention and reclamation | Oct 28 22:00 – Nov 6 22:00 | Existing 777600-second / 216-hour ceiling; real 1/24/168-hour cohorts and 24-hour allocator proof unchanged. |
| C4 failure/privacy/observability | Nov 6 22:00 – Nov 7 12:00 | Existing 50400-second / 14-hour producer ceiling; every declared lane required. |
| C5/C6 review and terminal verification | Nov 7 12:00 – Nov 9 07:45 | Actual evidence and distinct owner decisions; verify disabled gate, empty Lease and terminated workloads. Publication remains separately authorized. |
| Final owned-environment disposition | Nov 9 07:45 – Nov 9 08:00 | Proposed 900-second final stopping allowance inside the reservation; per-producer failure/disable bounds must also be explicit in the final plan. |

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
