# Access-telemetry full qualification — pending campaign

This is the current requirements register for the separately scheduled full campaign. The [local functional contract](../../_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) has its own finish line. Functional success cannot discharge these gates or close A41. The [current report](../../_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md) records results.

Adopt one exact full Platform target/profile before scheduling this campaign. Prior KVM/PG captures do not transfer. Each gate needs eligible same-session evidence, its own review and completion state; no additional story-file registration is required. Jérôme Piquot owns the gates with Platform/Builds environment support and Memories assertions. The approved sole-owner policy permits distinct Operations/Security records labelled `sole-owner/self-reviewed` without independent-review claims.

C1.24 precedes C1.23/C1.25. Full C2 retains two writers, 30 minutes and 150,000 backlog records. Full C3 retains real 1/24/168-hour cohorts, 24-hour allocator proof and the 216-hour producer bound. No deadline extension is implicit. Use the existing [lifecycle runbook](access-telemetry-lifecycle.md) and [adapter contract](access-telemetry-adapter-production.md). Production activation/publication remain separate decisions.

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
